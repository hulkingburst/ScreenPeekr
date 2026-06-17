# ScreenPeekr v2.0.0

A minimal Windows system tray utility for automated screen capture and webhook-based PC monitoring.

ScreenPeekr periodically captures a selected display (or full virtual desktop) and uploads PNG screenshots to a configured Discord webhook. It is designed for lightweight, unattended monitoring of your own computer.

## Features

- 🪟 Windows system tray integration (runs headless)
- 📸 Scheduled screenshot capture at configurable intervals
- 🔗 Discord webhook image uploads
- ⚙️ Adjustable capture interval (minimum 5 seconds)
- 🖥️ Multi-monitor support with friendly device names
- 🧭 Full virtual desktop capture (all monitors combined)
- ⌨️ Optional pre-capture input simulation (keyboard action before capture)
- ⏱️ Configurable input delay (timing control before capture)
- 📝 Local configuration storage + logging
- 🔄 Optional Windows startup launch
- 🚫 No dashboard, no account system, no cloud backend

## What’s New in v2.0.0

- Pre-capture input simulation: send a keyboard input before each screenshot
- Input delay control: fine-tune timing between input and capture
- Virtual screen capture mode: capture entire desktop across all monitors
- Lower minimum interval: reduced from 15s to 5s minimum capture interval
- Improved monitor detection: better device naming and fallback handling
- Expanded configuration system: new options alongside existing settings

## Quick Start

1. Download the latest release
2. Run ScreenPeekr.exe (appears in system tray)
3. Right-click tray icon → set your Discord webhook URL
4. Configure capture interval
5. Select monitor or virtual screen mode
6. (Optional) configure pre-capture input + delay
7. Toggle monitoring ON

## Installation from Source

Requirements:
- Windows 10 or newer
- .NET 8 SDK with Windows Desktop workload

### Run in development

dotnet run --project .\ScreenPeekr.csproj

### Build release (framework-dependent)

dotnet publish .\ScreenPeekr.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true

Output:
bin\Release\net8.0-windows\win-x64\publish\ScreenPeekr.exe

### Build self-contained (no .NET runtime required)

dotnet publish .\ScreenPeekr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true

## Configuration

Stored at:
%LOCALAPPDATA%\ScreenPeekr\config.txt

Settings:

- webhook_url — Discord webhook URL for uploads  
- interval_seconds — screenshot interval (minimum 5)  
- selected_monitor — monitor ID or ALL for full virtual desktop  
- start_with_windows — auto-start on boot (true/false)  
- pre_screenshot_key — optional keyboard input before capture  
- input_delay_ms — delay between input and capture (default 250)

## Discord Webhook Setup

1. Open Discord server settings
2. Go to Integrations → Webhooks
3. Create or copy a webhook URL
4. Paste into ScreenPeekr via tray menu: Set Webhook...
5. Use Take Screenshot Now to test

## Tray Menu

ScreenPeekr
────────────────────────────
Status: OFF / ON / ERROR

Toggle ON/OFF
Set Webhook...
Set Interval...
Select Monitor...
Set Pre-Capture Input...
Set Input Delay...

Take Screenshot Now

Show Log

Start With Windows ✓

Exit

## Behavior

- Starts in system tray
- Defaults to OFF state
- Gray = idle, Green = active, Red = error
- Captures immediately when enabled, then continues on interval
- Upload failures are logged locally
- Temporary PNG files are deleted after upload
- Pre-capture input only runs when enabled

## Version History

### v2.0.0 (Current)

- Pre-capture input simulation added
- Configurable input delay added
- Virtual desktop capture mode added
- Minimum interval reduced to 5 seconds
- Improved monitor detection

### v1.0.0

- Initial release
- System tray screenshot capture
- Discord webhook uploads
- Multi-monitor support
- Local config + logging

## Design Philosophy

ScreenPeekr is intentionally minimal:

- No cloud backend
- No accounts or authentication
- No analytics or telemetry
- No video recording
- No OCR or AI processing
- No scheduling system beyond a simple interval timer

It focuses on one function: scheduled screenshot capture and webhook delivery.

## License

See LICENSE file for details.
