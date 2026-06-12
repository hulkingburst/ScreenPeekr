# MacroWatch

MacroWatch is a minimal Windows system tray utility for unattended game or Roblox monitoring. It periodically captures one selected monitor at native resolution and uploads a PNG screenshot to a Discord webhook.

There is no dashboard, account system, OCR, AI, video recording, multi-monitor capture, quality setting, resolution setting, scheduler, pause timer, or Discord error notification path.

## Tray Menu

```text
MacroWatch
─────────────────
Status: OFF/ON/ERROR

Toggle ON/OFF
Set Webhook...
Set Interval...
Select Monitor...

Take Screenshot Now

Show Log

Start With Windows ✓

Exit
```

## Behavior

- The app has no main window and starts in the system tray.
- It always starts OFF, including when launched by Windows startup.
- Gray icon means OFF, green means ON, and red means ERROR.
- When turned ON, it sends one screenshot immediately, then continues on the configured interval.
- When turned OFF, it stops immediately.
- The interval is stored in whole seconds with a minimum of 15 seconds.
- Manual screenshots can be sent while monitoring is OFF or ON.
- Temporary PNG files are deleted after upload.
- Upload failures are logged locally, increment the error counter, switch the tray icon red, and do not stop monitoring.
- When a later upload succeeds after a failure, the tray icon returns to green while ON and the recovery is logged.

## Local Files

MacroWatch stores human-readable settings and logs in:

```text
%LOCALAPPDATA%\MacroWatch\
```

Configuration file:

```text
webhook_url=
interval_seconds=60
selected_monitor=\\.\DISPLAY1
start_with_windows=false
```

## Build From Source

Requirements:

- Windows 10 or newer
- .NET 8 SDK with Windows Desktop support

Build a normal debug run:

```powershell
dotnet run --project .\MacroWatch.csproj
```

Build a release executable:

```powershell
dotnet publish .\MacroWatch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

The published executable will be in:

```text
bin\Release\net8.0-windows\win-x64\publish\MacroWatch.exe
```

Build a self-contained executable that does not require the .NET runtime to be installed:

```powershell
dotnet publish .\MacroWatch.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

## Discord Webhook Setup

1. In Discord, open the target server channel settings.
2. Choose Integrations, then Webhooks.
3. Create or copy a webhook URL.
4. In MacroWatch, right-click the tray icon and choose `Set Webhook...`.
5. Use `Take Screenshot Now` to verify the webhook.

## Notes

- Start With Windows writes to the current user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registry key.
- Monitor selection is persisted between launches.
- MacroWatch prefers Windows monitor friendly names when available and falls back to `Display 1`, `Display 2`, and so on.
