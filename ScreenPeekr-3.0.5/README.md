# ScreenPeekr

ScreenPeekr is a lightweight Windows system tray application for periodic screen capture and remote monitoring via Discord webhooks.

It is designed for simple, unattended screen visibility while away from your computer, with a focus on reliability, minimal resource usage, and a tray-first workflow.

---

## Features

- System tray-only application (no main window)
- Periodic screenshot capture at configurable intervals
- Discord webhook integration for image delivery
- Multi-monitor support (single monitor or full desktop / all monitors mode)
- Pre-capture keyboard input support (optional)
- Configurable input delay before capture
- Native resolution PNG screenshots
- Persistent local configuration
- Lightweight background operation

---

## System Tray Controls

All interaction is handled via right-click tray menu:

- Toggle monitoring ON/OFF
- Set Discord webhook URL
- Set capture interval (minimum 5 seconds)
- Select monitor or all monitors mode
- Set pre-capture input key
- Set input delay (ms)
- Take manual screenshot
- View logs
- Enable/disable start with Windows
- Exit application

---

## How It Works

When monitoring is enabled, ScreenPeekr runs a continuous capture loop:

1. Wait for configured interval
2. Optionally send a configured keypress
3. Wait for configured input delay
4. Capture screenshot (selected monitor or full desktop)
5. Upload PNG image to Discord webhook
6. Repeat

When disabled, the application remains idle in the system tray.

---

## Configuration

All settings are stored locally and persist between sessions:

- Webhook URL
- Capture interval (seconds)
- Monitor selection mode
- Pre-capture input key
- Input delay (milliseconds)
- Start with Windows option

No accounts, cloud services, or external dependencies are required beyond Discord webhooks.

---

## Logging

ScreenPeekr maintains a lightweight local log system focused on meaningful events:

Tracked events include:
- Application start and shutdown
- Monitoring enabled/disabled
- Webhook updates
- Interval changes
- Monitor selection changes
- Input configuration changes
- Upload failures and recovery events

To avoid clutter, individual screenshot events are tracked using counters rather than per-event log entries.

---

## Requirements

- Windows 10 / Windows 11
- Active internet connection (for Discord webhook uploads)

---

## Notes

ScreenPeekr is intentionally minimal and tray-based. It is designed to run quietly in the background without requiring user interaction beyond initial configuration.

The focus of the application is stability, simplicity, and long-duration unattended operation rather than feature complexity.
