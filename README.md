<div align="center">

# PhoneDesk

### Your Android phone. Your Windows desktop. Fewer commands.

Camera controls and screen mirroring, together in a simple local app.

**Windows x64 · Android 12+ · V1.1 beta · MIT**

[**Download V1.1**](https://github.com/Yashahant/PhoneDesk/releases/tag/v1.1) &nbsp; / &nbsp; [Quick start](#connect) &nbsp; / &nbsp; [Report a bug](https://github.com/Yashahant/PhoneDesk/issues) &nbsp; / &nbsp; [Build from source](#source-and-build)

</div>

---

## What is PhoneDesk?

PhoneDesk puts scrcpy's camera and mirroring options into a Windows interface, so you can spend less time remembering commands and more time using your phone.

| Camera | Mirror | Connect |
| :--- | :--- | :--- |
| Choose a camera and adjust resolution, FPS, bitrate, and rotation. | View and control your phone screen on your computer. | Use USB first, then switch to Wi-Fi from the app. |

*Your terminal can take a short break.*

## Download

Get **PhoneDesk-V1.1-Public.zip** from the [V1.1 release](https://github.com/Yashahant/PhoneDesk/releases/tag/v1.1). Extract the complete ZIP and open `PhoneDesk.exe`. The Windows x64 package includes scrcpy and the .NET runtime.

**Before you start:** use an Android 12+ phone, a Windows x64 computer, and a USB cable that supports data. USB debugging must be enabled. Camera options depend on what your phone exposes.

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

## Feedback and contributions

Found a bug or have an idea? [Open an issue](https://github.com/Yashahant/PhoneDesk/issues). For a bug report, include:

- Your Android version and Windows version.
- Whether you used USB or Wi-Fi.
- What you did, what you expected, and what happened.
- Any relevant error message, with private information removed.

Remove IP addresses, device serial numbers, usernames, and other personal details from logs or screenshots before sharing. Focused pull requests are welcome; for a large change, start with an issue so we can discuss the approach.

## Credits

PhoneDesk provides the desktop interface. [scrcpy](https://github.com/Genymobile/scrcpy) and ADB provide the underlying Android connection and streaming capabilities. Thank you to their maintainers and the developers of the bundled dependencies.

## License

PhoneDesk V1.1's own source code and documentation are open source under the [MIT License](LICENSE), copyright (c) 2026 Yashahant. This also covers PhoneDesk's own code in the V1.1 release downloads. You may use, modify and redistribute it, including commercially, provided you retain the copyright and license notice.

Bundled third-party components, including scrcpy, ADB, .NET, FFmpeg, SDL and libusb, remain under their respective licenses; the PhoneDesk MIT license does not replace those terms. See the license and notice files included in the downloadable package. When redistributing PhoneDesk, include this repository's LICENSE alongside the applicable third-party notices.
