# iRacing Companion

A native Windows (WPF / .NET 10) tray utility for sim racing:

- **Smart plug automation** — turns a Home Assistant switch **on** when iRacing launches and **off** a configurable delay after it closes.
- **Companion app launcher** — starts apps like Sound Shift, JoyToKey, or RaceLab automatically, each with its own trigger (iRacing UI vs. on-track simulator), arguments, and "start minimized" option.
- **Fluent UI** — Windows 11 dark theme, lives in the system tray, single instance, run-at-login, remembers its window size.

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
# run locally
dotnet run

# self-contained single exe (x64)
dotnet publish IRacingSmartPlug.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The published `publish/iRacingCompanion.exe` runs standalone (no .NET install needed).

## Releases

Every push to `main` builds the x64 exe via GitHub Actions (see the **Actions** tab → artifacts).
Pushing a `v*` tag additionally attaches the exe to a GitHub Release.

## Configuration

Settings live in `%APPDATA%\iRacingSmartPlug\config.json` (not tracked in git). Configure Home
Assistant, behavior, and companion apps from the app's **Settings** and **Apps** pages.
