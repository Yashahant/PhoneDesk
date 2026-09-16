using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

ApplicationConfiguration.Initialize();
Application.Run(new Desk());

class Preferences {
 public string Folder {get;set;} = Path.Combine(AppContext.BaseDirectory,"scrcpy");
 public string Device {get;set;} = "";
 public string Quality {get;set;} = "1920x1080";
 public string Fps {get;set;} = "30";
 public string Rotation {get;set;} = "0";
 public bool Flip {get;set;}
 public string Bitrate {get;set;} = "8M";
}
class Desk : Form {
 readonly string prefsFile = Path.Combine(AppContext.BaseDirectory,"settings.json");
 Preferences prefs = new();
 TextBox folder=new(), address=new(), log=new();
 ComboBox cameraId=new(), bitrate=new(); NumericUpDown zoom=new(){Minimum=0.6M,Maximum=10,DecimalPlaces=1,Increment=0.1M,Value=1,Width=65}; CheckBox torch=new(){Text="Torch",AutoSize=true}; string lastMode="id";
 ComboBox devices=new(), quality=new(), fps=new(), rotation=new();
 CheckBox flip=new(){Text="Mirror camera",AutoSize=true}, audio=new(){Text="Screen audio",AutoSize=true};
 Label status=new(){Text="Ready",AutoSize=true};
 FlowLayoutPanel actions=new(){AutoSize=true,Dock=DockStyle.Fill,WrapContents=true};
 Process? stream;
 readonly System.Collections.Concurrent.ConcurrentDictionary<Process,bool> disconnected = new();
 bool busy, closing; CancellationTokenSource? recoveryCancel; string[] activeArgs=Array.Empty<string>(); string activeSerial=""; string activeFolder=""; bool streamReady, streamInterrupted; CheckBox reconnect=new(){Text="Reconnect Wi-Fi",Checked=false,AutoSize=true}; CheckBox autoRecover=new(){Text="Recover interrupted camera",Checked=false,AutoSize=true};
 public Desk() {
  try { if(File.Exists(prefsFile)) prefs=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(prefsFile))??new(); } catch {}
  Text="PhoneDesk V1.1 • Android 12+"; Size=new(1000,820); MinimumSize=new(950,780); StartPosition=FormStartPosition.CenterScreen;
  Font=new("Segoe UI",10); BackColor=Color.FromArgb(245,247,251);
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24),ColumnCount=1,RowCount=12};
  root.RowStyles.Add(new(SizeType.AutoSize));
  Controls.Add(root);
  root.Controls.Add(new Label{Text="PhoneDesk",Font=new("Segoe UI",24,FontStyle.Bold),AutoSize=true});
  root.Controls.Add(new Label{Text="Connect your Android phone by USB or Wi-Fi • Change settings, then click Apply settings",AutoSize=true,Margin=new(0,0,0,18)});
  folder.Text=prefs.Folder; folder.Width=530;
  root.Controls.Add(Row("scrcpy folder",folder,Button("Browse",()=> { using var d=new FolderBrowserDialog(); if(d.ShowDialog()==DialogResult.OK) folder.Text=d.SelectedPath; return Task.CompletedTask;})));
  address.Text=prefs.Device; address.Width=260;
  root.Controls.Add(Row("Wireless IP:port",address,Button("Connect",Connect),Button("Refresh devices",RefreshDevices)));
  devices.Width=320; devices.DropDownStyle=ComboBoxStyle.DropDownList;
  root.Controls.Add(Row("USB / Wi-Fi device",devices,Button("Switch USB to Wi-Fi",SwitchUsbToWifi)));
  SetCombo(quality,new[]{"1280x720","1920x1080","3840x2160"},prefs.Quality);
  SetCombo(fps,new[]{"30","60"},prefs.Fps); SetCombo(rotation,new[]{"0","90","180","270"},prefs.Rotation);
  flip.Checked=prefs.Flip;
  root.Controls.Add(Row("Quality",quality,new Label{Text="FPS",AutoSize=true},fps,new Label{Text="Rotate",AutoSize=true},rotation,flip));
  root.Controls.Add(Row("Orientation",Button("Landscape",()=>Orient("0")),Button("Portrait left",()=>Orient("270")),Button("Portrait right",()=>Orient("90"))));
  cameraId.Width=320; cameraId.DropDownStyle=ComboBoxStyle.DropDownList;  SetCombo(bitrate,new[]{"4M","8M","12M","20M"},prefs.Bitrate);
  root.Controls.Add(Row("Lens",cameraId,Button("Refresh lenses",ListCameras),Button("Supported sizes",ListSizes)));
  root.Controls.Add(Row("Camera options",new Label{Text="Bitrate",AutoSize=true},bitrate,new Label{Text="Zoom",AutoSize=true},zoom,torch,audio));
  actions.Controls.AddRange(new Control[]{Button("Start selected lens",()=>StartStream("id")),Button("Front camera",()=>StartStream("front")),Button("Rear camera",()=>StartStream("back")),Button("Apply settings",()=>StartStream(lastMode)),Button("Mirror screen",()=>StartStream("screen")),Button("Stop / cancel retry",Stop)});
  root.Controls.Add(actions);
  root.Controls.Add(status);
  log.Multiline=true; log.ReadOnly=true; log.ScrollBars=ScrollBars.Vertical; log.Dock=DockStyle.Fill; log.BackColor=Color.White; log.Font=new("Consolas",9);
  root.Controls.Add(log); root.RowStyles.Clear(); for(int i=0;i<11;i++)root.RowStyles.Add(new(SizeType.AutoSize));root.RowStyles.Add(new(SizeType.Percent,100));
  Shown+=async(_,_)=>await Run(RefreshDevices);
  FormClosing+=(_,_)=>{closing=true;recoveryCancel?.Cancel();try{Save(); if(stream is {HasExited:false}) stream.Kill(true);}catch{} };
 }
 static void SetCombo(ComboBox c,string[] values,string selected){c.DropDownStyle=ComboBoxStyle.DropDownList;c.Items.AddRange(values);c.SelectedItem=selected;if(c.SelectedIndex<0)c.SelectedIndex=0;c.Width=105;}
 FlowLayoutPanel Row(string label,params Control[] controls){var r=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,Margin=new(0,4,0,5)};r.Controls.Add(new Label{Text=label,Width=145,AutoSize=false,Height=30,TextAlign=ContentAlignment.MiddleLeft});r.Controls.AddRange(controls);return r;}
 Button Button(string text,Func<Task> action){var b=new Button{Text=text,AutoSize=true,Height=36,Padding=new(8,4,8,4),FlatStyle=FlatStyle.Flat,BackColor=Color.White};b.Click+=async(_,_)=>{if(text=="Stop / cancel retry")await Stop();else await Run(action);};return b;}
 async Task Run(Func<Task> action){if(busy)return;busy=true;try{await action();}catch(Exception e){Write("ERROR: "+e.Message);status.Text="Action failed — see details below";}finally{busy=false;}}
 void Write(string? s){if(string.IsNullOrWhiteSpace(s)||closing||IsDisposed)return;if(InvokeRequired){try{BeginInvoke(()=>Write(s));}catch{}return;}if(log.TextLength>100000)log.Clear();log.AppendText(s+Environment.NewLine);}
 Process Make(string exe,IEnumerable<string> args){var file=Path.Combine(folder.Text.Trim(),exe);if(!File.Exists(file))throw new FileNotFoundException("Select the folder containing "+exe);var info=new ProcessStartInfo(file){WorkingDirectory=folder.Text.Trim(),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var a in args) info.ArgumentList.Add(a);return new Process{StartInfo=info};}
 async Task<string> CaptureOutput(string exe,params string[] args){using var p=Make(exe,args);p.Start();var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();try{await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token);}catch(OperationCanceledException){p.Kill(true);throw new Exception("Connection timed out. Check Wi-Fi and wireless debugging.");}var result=await stdout+await stderr;Write(result);if(p.ExitCode!=0)throw new Exception(exe+" exited with code "+p.ExitCode);return result;}
 async Task RefreshDevices(){var old=devices.SelectedItem?.ToString();var output=await CaptureOutput("adb.exe","devices","-l");devices.Items.Clear();foreach(var line in output.Split('\n')){var m=Regex.Match(line,@"^(\S+)\s+device\s");if(m.Success)devices.Items.Add(m.Groups[1].Value);}if(old!=null&&devices.Items.Contains(old))devices.SelectedItem=old;else if(devices.Items.Contains(address.Text.Trim()))devices.SelectedItem=address.Text.Trim();else if(devices.Items.Count>0)devices.SelectedIndex=0;status.Text=devices.Items.Count>0?$"{devices.Items.Count} connected device(s)":"No authorized device. Connect wirelessly or attach USB and accept debugging.";}
 async Task Connect(){var a=address.Text.Trim();if(!Regex.IsMatch(a,@"^[a-zA-Z0-9.\-]+:\d{1,5}$"))throw new Exception("Enter your phone IP and port, for example 192.168.1.100:5555.");await CaptureOutput("adb.exe","connect",a);Save();await RefreshDevices();}
async Task SwitchUsbToWifi(){
 var serial=Device();
 var list=await CaptureOutput("adb.exe","devices","-l");
 var selected=list.Split('\n').FirstOrDefault(line=>Regex.IsMatch(line,@"^"+Regex.Escape(serial)+@"\s+device\s"));
 var usbSerial=await CaptureOutput("adb.exe","-d","get-serialno");
 if(selected==null||usbSerial.Trim()!=serial)throw new Exception("Connect the phone by USB, approve USB debugging, click Refresh devices and select the USB entry first.");
 var routes=await CaptureOutput("adb.exe","-s",serial,"shell","ip","-4","route");
 var candidates=Regex.Matches(routes,@"\bdev\s+(?:wlan\d+|wifi\d+)\b[^\r\n]*\bsrc\s+(\d+\.\d+\.\d+\.\d+)").Select(m=>m.Groups[1].Value).Distinct().ToArray();
 if(candidates.Length!=1)throw new Exception("Could not identify one Wi-Fi address. Connect the phone to Wi-Fi and try again.");
 var endpoint=candidates[0]+":5555";
 await Stop();
 status.Text="Enabling Wi-Fi connection — keep USB connected…";
 await CaptureOutput("adb.exe","-s",serial,"tcpip","5555");
 address.Text=endpoint;
 for(int attempt=1;attempt<=5;attempt++){
  await Task.Delay(1500);if(closing)return;
  status.Text=$"Connecting to {endpoint} ({attempt}/5) — keep USB connected…";
  try{
   await CaptureOutput("adb.exe","connect",endpoint);
   var state=await CaptureOutput("adb.exe","-s",endpoint,"get-state");
   if(state.Trim()!="device")continue;
   await RefreshDevices();
   if(!devices.Items.Contains(endpoint))continue;
   devices.SelectedItem=endpoint;Save();
   status.Text="Wi-Fi connected — you can now unplug USB and start your camera/screen.";
   Write(status.Text);return;
  }catch(Exception e){Write("Connection attempt failed: "+e.Message);}
 }
 throw new Exception("Wi-Fi connection could not be verified. Keep USB connected. Check both devices are on the same reachable Wi-Fi network, then try again.");
}

 string Device(){return devices.SelectedItem?.ToString()??throw new Exception("Connect or refresh devices first.");}
  string LensId()=>cameraId.Text.Split(' ')[0];
 async Task ListCameras(){var output=await CaptureOutput("scrcpy.exe","-s",Device(),"--list-cameras");var matches=Regex.Matches(output,@"--camera-id=(\d+)\s+\(([^\r\n]+)\)");if(matches.Count>0){cameraId.Items.Clear();foreach(Match m in matches)cameraId.Items.Add(m.Groups[1].Value+" — "+m.Groups[2].Value);cameraId.SelectedIndex=0;}}
 async Task ListSizes(){await CaptureOutput("scrcpy.exe","-s",Device(),"--camera-id="+LensId(),"--list-camera-sizes");}
 async Task Orient(string angle){rotation.SelectedItem=angle;if(stream!=null)await StartStream(lastMode);else Save();}
 async Task StartStream(string mode){var serial=Device();if(mode=="id"&&!Regex.IsMatch(LensId(),@"^\d+$"))throw new Exception("Click Refresh lenses and select a lens, or use Front camera / Rear camera.");var androidVersion=await CaptureOutput("adb.exe","-s",serial,"shell","getprop","ro.build.version.sdk");if(!int.TryParse(androidVersion.Trim(),out var sdk)||sdk<31)throw new Exception("PhoneDesk requires Android 12 or newer.");Save();var args=new List<string>{"-s",serial,"--video-codec=h264","--video-bit-rate="+bitrate.Text,"--window-width="+((mode!="screen"&&(rotation.Text=="90"||rotation.Text=="270"))?"450":"960")};if(mode=="screen"){args.Add("--window-title=PhoneDesk Screen");args.Add("--max-size="+quality.Text.Split('x')[0]);args.Add("--max-fps="+fps.Text);if(!audio.Checked)args.Add("--no-audio");}else{args.AddRange(new[]{"--window-title=PhoneDesk Camera","--video-source=camera","--no-audio","--camera-size="+quality.Text,"--camera-fps="+fps.Text,"--camera-zoom="+zoom.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),"--capture-orientation="+(flip.Checked?"flip":"")+rotation.Text});if(torch.Checked)args.Add("--camera-torch");args.Add(mode=="id"?"--camera-id="+LensId():"--camera-facing="+mode);}
  await Stop();lastMode=mode;activeArgs=args.ToArray();activeSerial=serial;activeFolder=folder.Text.Trim();Launch();
 }
 void Launch(){streamReady=false;streamInterrupted=false;var p=Make("scrcpy.exe",activeArgs);p.StartInfo.FileName=Path.Combine(activeFolder,"scrcpy.exe");p.StartInfo.WorkingDirectory=activeFolder;p.OutputDataReceived+=(_,e)=>StreamOutput(p,e.Data);p.ErrorDataReceived+=(_,e)=>StreamOutput(p,e.Data);p.Start();stream=p;p.BeginOutputReadLine();p.BeginErrorReadLine();status.Text="Starting "+lastMode+"…";_=Watch(p);}
 void StreamOutput(Process p,string? line){if(line?.Contains("disconnected",StringComparison.OrdinalIgnoreCase)==true)disconnected[p]=true;Write(line);if(line==null||closing)return;try{BeginInvoke(async()=>{if(stream!=p)return;if(line.Contains("Texture:"))streamReady=true;if(line.Contains("disconnected",StringComparison.OrdinalIgnoreCase))streamInterrupted=true;if(line.Contains("Camera disconnected",StringComparison.OrdinalIgnoreCase)&&autoRecover.Checked)await Recover(p);else if(line.Contains("Device disconnected",StringComparison.OrdinalIgnoreCase)&&reconnect.Checked)await Recover(p);else if(line.Contains("Texture:")&&recoveryCancel==null)status.Text="Connected — "+lastMode;});}catch{}}
 async Task<string> Probe(string[] args,CancellationToken token){using var p=Make("adb.exe",args);p.StartInfo.FileName=Path.Combine(activeFolder,"adb.exe");p.StartInfo.WorkingDirectory=activeFolder;p.Start();var a=p.StandardOutput.ReadToEndAsync();var b=p.StandardError.ReadToEndAsync();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(8));try{await p.WaitForExitAsync(timeout.Token);}catch{try{p.Kill(true);}catch{}throw;}return (await a)+(await b);}
 async Task Recover(Process failed){if(closing||stream!=failed||recoveryCancel!=null)return;bool wireless=Regex.IsMatch(activeSerial,@"^[a-zA-Z0-9.\-]+:\d{1,5}$");if(!(wireless&&reconnect.Checked)&&!(lastMode!="screen"&&autoRecover.Checked))return;var cts=new CancellationTokenSource();recoveryCancel=cts;var token=cts.Token;try{await EndStream();for(int attempt=1;attempt<=6;attempt++){token.ThrowIfCancellationRequested();status.Text="Connection interrupted — retry "+attempt+"/6 in 3 seconds (Stop cancels)";Write(status.Text);await Task.Delay(3000,token);string state="";try{state=await Probe(new[]{"-s",activeSerial,"get-state"},token);if(state.Trim()!="device"&&wireless&&reconnect.Checked){Write("Reconnecting to "+activeSerial);Write(await Probe(new[]{"connect",activeSerial},token));state=await Probe(new[]{"-s",activeSerial,"get-state"},token);}}catch(OperationCanceledException) when(!token.IsCancellationRequested){Write("Connection attempt timed out.");continue;}token.ThrowIfCancellationRequested();if(state.Trim()!="device")continue;Write("ADB reachable. Restarting previous camera/screen stream…");Launch();status.Text="Reconnected — checking stream…";for(int check=0;check<15&&!streamReady&&!streamInterrupted&&stream is {HasExited:false};check++)await Task.Delay(1000,token);token.ThrowIfCancellationRequested();if(stream is {HasExited:false}&&streamReady&&!streamInterrupted){status.Text="Connection restored — "+lastMode;Write(status.Text);return;}await EndStream();}status.Text="Could not reconnect. Check phone Wi-Fi / IP:port, then Connect and start again.";}catch(OperationCanceledException){}catch(Exception e){Write(e.Message);status.Text="Reconnect failed — check connection and start again.";}finally{if(recoveryCancel==cts)recoveryCancel=null;cts.Dispose();}}
 async Task Watch(Process p){try{await p.WaitForExitAsync();if(stream!=p||closing)return;if(recoveryCancel!=null)return;if(p.ExitCode!=0||disconnected.ContainsKey(p))await Recover(p);if(stream==p){status.Text="Stream ended (code "+p.ExitCode+")";stream=null;p.Dispose();}}catch(ObjectDisposedException){}catch(Exception e){Write(e.Message);}}
 async Task EndStream(){var p=stream;stream=null;if(p==null)return;try{if(!p.HasExited){p.CloseMainWindow();await Task.Delay(300);if(!p.HasExited)p.Kill(true);await p.WaitForExitAsync();}}catch(InvalidOperationException){}finally{disconnected.TryRemove(p,out _);p.Dispose();}}
 async Task Stop(){recoveryCancel?.Cancel();recoveryCancel=null;await EndStream();status.Text="Stopped — reconnect cancelled";}
 void Save(){prefs.Folder=folder.Text.Trim();prefs.Device=address.Text.Trim();prefs.Quality=quality.Text;prefs.Fps=fps.Text;prefs.Rotation=rotation.Text;prefs.Flip=flip.Checked;prefs.Bitrate=bitrate.Text;File.WriteAllText(prefsFile,JsonSerializer.Serialize(prefs,new JsonSerializerOptions{WriteIndented=true}));}
}











