# iRacing Companion

A small Windows tray app for sim racing, written in WPF on .NET 10.

What it does:

- Turns a Home Assistant smart plug on when iRacing starts, and off a little while after it closes. The off delay is configurable.
- Starts companion apps automatically (Sound Shift, JoyToKey, RaceLab, whatever you use). Each one can launch when the iRacing UI opens or when the sim actually goes on track, and you can pass it arguments or start it minimized.
- Sits in the system tray with a Windows 11 dark theme. It's single instance, can start with Windows, and remembers its window size.

## Building

You need the .NET 10 SDK.

Run it locally:

```
dotnet run
```

Build a single self-contained x64 exe:

```
dotnet publish IRacingSmartPlug.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The resulting publish/iRacingCompanion.exe runs on its own, without .NET installed.

## Downloads

Every push to main builds the x64 exe on GitHub Actions, so you can grab it from the Actions tab under the run artifacts. Pushing a tag that starts with v (like v1.0) also attaches the exe to a GitHub release.

## Config

Settings are stored in %APPDATA%\iRacingSmartPlug\config.json, which is not committed to the repo. You set up Home Assistant, the behavior options and your companion apps from the Settings and Apps pages in the app.
