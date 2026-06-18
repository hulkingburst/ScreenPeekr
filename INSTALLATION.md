# ScreenPeekr Installation Guide

## Prerequisites

- Windows 10 or later (64-bit)
- Administrator privileges (for installation)

## Installation Methods

### Method 1: Using the Installer (Recommended)

1. Download `ScreenPeekr.exe` and `install.bat` from the release
2. Right-click `install.bat` and select "Run as administrator"
3. Follow the prompts:
   - The installer will copy files to `C:\Program Files\ScreenPeekr`
   - A desktop shortcut will be created
   - You can choose whether to start ScreenPeekr with Windows
   - You can choose to run ScreenPeekr immediately after installation

### Method 2: Manual Installation

1. Download `ScreenPeekr.exe` from the release
2. Create a folder for ScreenPeekr (e.g., `C:\Program Files\ScreenPeekr`)
3. Copy `ScreenPeekr.exe` to that folder
4. (Optional) Create a desktop shortcut:
   - Right-click `ScreenPeekr.exe` → Send to → Desktop (create shortcut)

## Configuration

### First-Time Setup

1. Run ScreenPeekr (double-click the executable or desktop shortcut)
2. The application will appear in your system tray (bottom-right corner)
3. Right-click the tray icon and select "Open Settings"
4. Configure the following:

#### Required Settings

- **Discord Webhook URL**: Your Discord webhook URL for screenshot uploads
- **Capture Interval**: How often to take screenshots (minimum 5 seconds)

#### Optional Settings

- **Monitor**: Select which monitor to capture (or "All Monitors")
- **Pre-Screenshot Key**: Press a key before each screenshot (e.g., to minimize windows)
- **Input Delay**: Delay between key presses in milliseconds
- **Start with Windows**: Automatically launch ScreenPeekr when Windows starts

### Advanced Settings (via config.txt)

For advanced configuration, you can edit `config.txt` located in:
```
%LOCALAPPDATA%\ScreenPeekr\config.txt
```

Advanced options include:
- Change detection sensitivity
- Away-only mode (only capture when idle)
- Screenshot retention settings
- Post-screenshot inputs

## Usage

### Basic Controls

Right-click the ScreenPeekr tray icon to access:
- **Toggle ON/OFF**: Start or stop screenshot monitoring
- **Take Screenshot Now**: Capture and upload immediately
- **Open Settings**: Configure application settings
- **Exit**: Close the application

### Monitoring Status

The tray icon shows the current status:
- **Green icon**: Monitoring is active
- **Red icon**: Monitoring is stopped
- **Yellow icon**: Error (e.g., webhook validation failed)

## Troubleshooting

### Application Won't Start

- Ensure you're running Windows 10 or later (64-bit)
- Check that your antivirus isn't blocking the executable
- Run as administrator if you encounter permission errors

### Screenshots Not Uploading

- Verify your Discord webhook URL is correct
- Check your internet connection
- Ensure Discord webhook is not rate-limited
- Check the event log for error messages

### Monitor Selection Issues

- If monitor selection shows long text instead of names, ensure you're using v5.0.0 or later
- Try restarting the application

### Config File Location

If you need to reset settings, delete the config file at:
```
%LOCALAPPDATA%\ScreenPeekr\config.txt
```

The application will recreate it with defaults on next launch.

## Uninstallation

### Using the Installer

1. Delete the desktop shortcut
2. Delete the installation folder: `C:\Program Files\ScreenPeekr`
3. (Optional) Delete the config folder: `%LOCALAPPDATA%\ScreenPeekr`
4. (Optional) Remove startup entry:
   - Open Task Manager → Startup tab
   - Disable ScreenPeekr if present

### Manual Uninstallation

1. Close ScreenPeekr (right-click tray icon → Exit)
2. Delete the ScreenPeekr folder
3. Delete the desktop shortcut
4. (Optional) Delete the config folder: `%LOCALAPPDATA%\ScreenPeekr`

## Support

For issues or questions:
- Check the GitHub repository: https://github.com/hulkingburst/ScreenPeekr
- Review the documentation in the `docs/` folder
- Open an issue on GitHub for bugs or feature requests
