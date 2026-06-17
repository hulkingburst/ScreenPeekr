# MacroWatch v2.0.0

A minimal Windows system tray utility for unattended game or Roblox monitoring.

MacroWatch periodically captures one selected monitor at native resolution and uploads a PNG screenshot to a Discord webhook.

## Features

- 🪟 Windows system tray integration
- 📸 Periodic monitor screenshot capture
- 🔗 Discord webhook uploads
- ⚙️ Configurable capture interval (minimum 5 seconds)
- 🖥️ Multi-monitor support with friendly names
- 📝 Local logging and configuration storage
- 🔄 Auto-startup with Windows option
- ⌨️ **NEW in v2.0:** Pre-screenshot input simulation (send keyboard input before capture)
- ⏱️ **NEW in v2.0:** Configurable input delay
- 🎯 **NEW in v2.0:** Virtual screen capture support (all monitors at once)

## What's New in v2.0.0

- **Pre-Screenshot Input**: Optionally send a keyboard input (e.g., Alt+Tab, Space) before each screenshot capture
- **Input Delay Configuration**: Set a delay between sending input and capturing the screenshot
- **Virtual Screen Support**: Capture all monitors combined as a single image
- **Minimum Interval Reduced**: Changed from 15 seconds to 5 seconds for more frequent captures
- **Improved Monitor Detection**: Better WMI-based friendly name resolution with fallbacks
- **Enhanced Configuration**: New config options stored alongside existing settings

## Quick Start

1. Download the latest release executable
2. Run `MacroWatch.exe` - it will appear in your system tray
3. Right-click the tray icon and set your Discord webhook URL
4. Configure the capture interval
5. (Optional) Set pre-screenshot input if you need to send a key before each capture
6. (Optional) Adjust input delay if needed
7. Select your monitor
8. Toggle monitoring ON/OFF from the tray menu

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

Build a self-contained executable (no .NET runtime required):

```powershell
dotnet publish .\MacroWatch.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

## Configuration

Settings are stored in `%LOCALAPPDATA%\MacroWatch\config.txt` with the following options:

- `webhook_url` - Discord webhook URL for uploading screenshots
- `interval_seconds` - Capture interval in seconds (minimum 5)
- `selected_monitor` - Selected monitor device identifier (or "ALL" for virtual screen)
- `start_with_windows` - Auto-launch on Windows startup (true/false)
- `pre_screenshot_key` - Keyboard key to send before capture (Keys enum name or numeric value)
- `input_delay_ms` - Delay in milliseconds between sending input and capturing (default 250)

## Discord Webhook Setup

1. In Discord, open your target server channel settings
2. Go to Integrations → Webhooks
3. Create or copy a webhook URL
4. In MacroWatch, right-click the tray icon and choose `Set Webhook...`
5. Use `Take Screenshot Now` to verify

## Tray Menu

```
MacroWatch
─────────────────────────────────
Status: OFF/ON/ERROR

Toggle ON/OFF
Set Webhook...
Set Interval...
Select Monitor...
Set Pre-Screenshot Input...
Set Input Delay...

Take Screenshot Now

Show Log

Start With Windows ✓

Exit
```

## Behavior

- Starts in the system tray (no main window)
- Always starts in OFF state
- Gray icon = OFF, Green icon = ON, Red icon = ERROR
- When enabled, sends a screenshot immediately, then continues on the configured interval
- Upload failures are logged locally and indicated by the red tray icon
- Manual screenshots can be sent anytime via the tray menu
- Pre-screenshot input is only sent when monitoring is enabled and a key is configured
- Temporary PNG files are deleted after upload
- Monitor friendly names are resolved from Windows device information

## Version History

### v2.0.0 (Current)
- Added pre-screenshot input simulation
- Added configurable input delay
- Added virtual screen support ("ALL" monitors capture)
- Reduced minimum interval from 15 to 5 seconds
- Improved monitor friendly name detection

### v1.0.0
- Initial release
- Basic screenshot capture and Discord upload
- System tray integration
- Multi-monitor support
- Configuration and logging

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
