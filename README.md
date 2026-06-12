# MacroWatch

A minimal Windows system tray utility for unattended game or Roblox monitoring.

MacroWatch periodically captures one selected monitor at native resolution and uploads a PNG screenshot to a Discord webhook.

## Features

- 🪟 Windows system tray integration
- 📸 Periodic monitor screenshot capture
- 🔗 Discord webhook uploads
- ⚙️ Configurable capture interval (minimum 15 seconds)
- 🖥️ Multi-monitor support
- 📝 Local logging and configuration storage
- 🔄 Auto-startup with Windows option

## Quick Start

1. Download the latest release executable
2. Run `MacroWatch.exe` - it will appear in your system tray
3. Right-click the tray icon and set your Discord webhook URL
4. Configure the capture interval
5. Select your monitor
6. Toggle monitoring ON/OFF from the tray menu

## Installation from Source

Requirements:
- Windows 10 or newer
- .NET 8 SDK with Windows Desktop support

Build and run:

```powershell
dotnet run --project .\MacroWatch.csproj
```

Build a release executable:

```powershell
dotnet publish .\MacroWatch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

The executable will be in `bin\Release\net8.0-windows\win-x64\publish\MacroWatch.exe`

## Configuration

Settings are stored in `%LOCALAPPDATA%\MacroWatch\` with the following options:

- `webhook_url` - Discord webhook URL for uploading screenshots
- `interval_seconds` - Capture interval in seconds (minimum 15)
- `selected_monitor` - Selected monitor device identifier
- `start_with_windows` - Auto-launch on Windows startup

## Discord Webhook Setup

1. In Discord, open your target server channel settings
2. Go to Integrations → Webhooks
3. Create or copy a webhook URL
4. In MacroWatch, right-click the tray icon and choose `Set Webhook...`
5. Use `Take Screenshot Now` to verify

## Behavior

- Starts in the system tray (no main window)
- Always starts in OFF state
- Gray icon = OFF, Green icon = ON, Red icon = ERROR
- When enabled, sends a screenshot immediately, then continues on the configured interval
- Upload failures are logged locally and indicated by the red tray icon
- Manual screenshots can be sent anytime via the tray menu

## Minimal by Design

MacroWatch intentionally excludes:
- Dashboard or web interface
- Account system
- OCR or AI features
- Video recording
- Multi-monitor batch capture
- Quality or resolution settings
- Scheduler
- Discord error notifications

It does one thing well: capture and upload screenshots to Discord on a schedule.

## License

See LICENSE file for details.
