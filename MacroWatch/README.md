# ScreenPeekr

ScreenPeekr is a lightweight Windows system tray application for periodic screen capture and remote monitoring via Discord webhooks. It is designed to provide simple, low-overhead visibility of your screen while away from your machine.

The application runs silently in the system tray and operates entirely through a right-click menu—no main window or dashboard is required.

---

## Features

- System tray-only application (no main window)
- Periodic screenshot capture at a configurable interval
- Discord webhook integration for remote viewing
- Multi-monitor support (single monitor or all monitors)
- Optional pre-capture keyboard input with configurable delay
- PNG screenshot output (native resolution capture)
- Lightweight background execution
- Persistent configuration storage

---

## System Tray Controls

Right-click the tray icon to access:

- Toggle monitoring ON/OFF
- Set Discord webhook URL
- Set capture interval (minimum 5 seconds)
- Select monitor (or all monitors)
- Set pre-capture input key
- Set input delay
- Take screenshot manually
- View local logs
- Enable/disable start with Windows
- Exit application

---

## How It Works

When enabled, ScreenPeekr runs a capture loop:

1. Wait for the configured interval
2. Optionally send a configured keypress
3. Wait for the configured input delay
4. Capture screenshot (selected monitor or full desktop)
5. Upload image to Discord webhook
6. Repeat

If disabled, the application remains idle in the system tray.

---

## Configuration

All settings are stored locally and persist between sessions:

- Webhook URL
- Capture interval (seconds)
- Selected monitor mode
- Pre-capture input key
- Input delay (ms)
- Start with Windows setting

No external accounts or cloud services are required.

---

## Logging

ScreenPeekr includes a lightweight local logging system.

- Successful screenshots are tracked via counters (not individual log spam)
- Important events are logged, including:
  - Application start/stop
  - Monitoring enabled/disabled
  - Webhook changes
  - Interval changes
  - Monitor selection changes
  - Upload failures and recovery events

---

## Requirements

- Windows 10/11
- Internet connection (for Discord webhook uploads)

---

## Notes

ScreenPeekr is designed for personal monitoring and remote visibility use cases. It prioritizes simplicity, stability, and low resource usage over feature complexity.

It is intentionally tray-based and does not include a traditional graphical interface.
