# ScreenPeekr

ScreenPeekr is a lightweight Windows system tray application for periodic screen capture and remote monitoring via Discord webhooks.

It is designed for simple, unattended screen visibility while away from your computer, with a focus on reliability, minimal resource usage, and a tray-first workflow.
---

# [WEBSITE FOR EASY DOWNLOADS](hulkingburst.github.io/screenpeekr/)

---

## Features

- System tray-only application with no main window
- Periodic screenshot capture at configurable intervals
- Discord webhook integration for image delivery
- Multi-monitor support with single monitor, all monitors, and primary-display fallback
- Screenshot change detection with configurable sensitivity
- Away-only capture mode based on Windows idle input tracking
- Pre-screenshot and post-screenshot keyboard input sequences
- Configurable input delay and key hold duration
- Webhook validation and repeated-failure health tracking
- Screenshot retention cleanup by age and count
- Persistent local configuration
- Lightweight background operation

---

## System Tray Controls

All interaction is handled via right-click tray menu:

- Toggle monitoring ON/OFF
- Set Discord webhook URL
- Set capture interval (minimum 5 seconds)
- Select monitor or all monitors mode
- Set pre-screenshot input sequence
- Set post-screenshot input sequence
- Set input delay and key hold timing
- Set change detection sensitivity
- Enable away-only mode and idle threshold
- Set screenshot retention
- Take manual screenshot
- View logs
- Enable/disable start with Windows
- Exit application

---

## How It Works

When monitoring is enabled, ScreenPeekr runs a continuous capture loop:

1. Validate the configured webhook.
2. Wait for the configured interval.
3. If away-only mode is enabled, skip capture while the user is active.
4. Send configured pre-screenshot inputs.
5. Capture the selected monitor, all monitors, or a safe fallback display.
6. Send configured post-screenshot inputs.
7. Compare against the previous screenshot and skip upload when no meaningful change is detected.
8. Upload PNG image to Discord webhook.
9. Clean up retained screenshots by age and count.
10. Repeat, logging failures and retrying on later intervals.

When disabled, the application remains idle in the system tray.

---

## Configuration

All settings are stored locally and persist between sessions:

```text
webhook_url=
interval_seconds=60
selected_monitor=\\.\DISPLAY1
start_with_windows=false
pre_screenshot_key=None
pre_screenshot_keys=
post_screenshot_keys=
input_delay_ms=250
key_hold_duration_ms=50
change_detection_sensitivity=50
away_only_mode=false
away_idle_threshold_seconds=300
screenshot_retention_days=1
screenshot_retention_count=50
```

Existing config files remain compatible. The legacy `pre_screenshot_key` value is still read and is migrated into the newer sequence setting at runtime.

No accounts, cloud services, or external dependencies are required beyond Discord webhooks.

---

## Logging

ScreenPeekr maintains a lightweight local log system focused on meaningful events:

- Application start and shutdown
- Monitoring enabled/disabled
- Webhook validation, failures, and recovery
- Interval changes
- Monitor selection, fallback, and reconnect events
- Input configuration changes
- Change-detection and away-mode skips
- Upload failures and recovery events

To avoid clutter, individual screenshot events are tracked using counters rather than per-event log entries.

---

## Requirements

- Windows 10 / Windows 11
- .NET 8 runtime for framework-dependent builds
- Active internet connection for Discord webhook uploads

---

## Notes

ScreenPeekr is intentionally minimal and tray-based. It is designed to run quietly in the background without requiring user interaction beyond initial configuration.

The focus of the application is stability, simplicity, and long-duration unattended operation rather than feature complexity.
