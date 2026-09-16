# PhoneDesk V1.1

Windows camera controls and screen mirroring for Android 12+.

## Download

Get **PhoneDesk-V1.1-Public.zip** from the [V1.1 release](https://github.com/Yashahant/PhoneDesk/releases/tag/v1.1). Extract the complete ZIP and open `PhoneDesk.exe`. The Windows x64 package includes scrcpy and the .NET runtime.

## Connect

1. Enable USB debugging on your Android phone.
2. Connect a USB data cable and approve the computer on the phone.
3. Click **Refresh devices**, select the phone, then start a camera or screen mirror.
4. For Wi-Fi, connect one USB phone, click **Switch USB to Wi-Fi**, and wait for confirmation before unplugging. Use a trusted network that permits communication between devices.

## Features and limits

- Front/rear camera and camera-ID selection; resolution, FPS, bitrate, rotation, mirror, zoom and torch controls.
- USB mirroring and USB-first wireless setup.
- No separately installed Android app or root required.
- Changing capture settings restarts the preview. Camera capabilities differ across phones.
- Automatic recovery is disabled. This is a beta, not a compatibility guarantee.
- No Windows virtual webcam driver or OBS integration is included.

## Source and build

The C# source and project file are included here and attached to the release. Build with the .NET 10 SDK:

```powershell
dotnet publish PhoneDesk.csproj -c Release -r win-x64 --self-contained true
```

Place the official scrcpy 4.1 Windows distribution in a `scrcpy` folder beside the executable. Third-party licenses, notices and relevant source archives are included in the downloadable package. scrcpy is an independent project: https://github.com/Genymobile/scrcpy.

The public build contains no saved phone configuration, personal device identifiers, logs or debug symbols. Only V1.1 is distributed by this repository.
