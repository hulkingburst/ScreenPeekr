using MacroWatch.Models;
using MacroWatch.Services;

namespace MacroWatch.Tray;

internal sealed class MacroWatchApplicationContext : ApplicationContext
{
    private readonly ConfigStore _configStore;
    private readonly EventLogStore _eventLog;
    private readonly StartupService _startup;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly ScreenshotCaptureService _capture;
    private readonly DiscordWebhookClient _uploader;
    private readonly RuntimeStats _stats = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer = new();
    private CancellationTokenSource? _monitoringCancellation;
    private bool _monitoring;
    private bool _uploadInProgress;
    private TrayState _trayState = TrayState.Off;

    public MacroWatchApplicationContext(
        ConfigStore configStore,
        EventLogStore eventLog,
        StartupService startup,
        MonitorCatalog monitorCatalog,
        ScreenshotCaptureService capture,
        DiscordWebhookClient uploader)
    {
        _configStore = configStore;
        _eventLog = eventLog;
        _startup = startup;
        _monitorCatalog = monitorCatalog;
        _capture = capture;
        _uploader = uploader;

        if (string.IsNullOrWhiteSpace(_configStore.Config.SelectedMonitor))
        {
            _configStore.Config.SelectedMonitor = _monitorCatalog.GetSelectedOrDefault(string.Empty).Id;
            _configStore.Save();
        }

        _timer.Tick += async (_, _) => await SendScreenshotAsync(manual: false, _monitoringCancellation?.Token ?? CancellationToken.None);

        _notifyIcon = new NotifyIcon
        {
            Text = "MacroWatch",
            ContextMenuStrip = BuildMenu(),
            Visible = true
        };
        SetTrayState(TrayState.Off);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => RefreshMenu(menu);
        RefreshMenu(menu);
        return menu;
    }

    private void RefreshMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        menu.Items.Add(new ToolStripMenuItem("MacroWatch") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem($"Status: {GetStatusText()}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Toggle ON/OFF", null, async (_, _) => await ToggleMonitoringAsync()));
        menu.Items.Add(new ToolStripMenuItem("Set Webhook...", null, (_, _) => SetWebhook()));
        menu.Items.Add(new ToolStripMenuItem("Set Interval...", null, (_, _) => SetInterval()));
        menu.Items.Add(new ToolStripMenuItem("Select Monitor...", null, (_, _) => SelectMonitor()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Take Screenshot Now", null, async (_, _) => await SendScreenshotAsync(manual: true)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Show Log", null, (_, _) => ShowLog()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Start With Windows", null, (_, _) => ToggleStartup())
        {
            Checked = _configStore.Config.StartWithWindows
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Exit()));
    }

    private async Task ToggleMonitoringAsync()
    {
        if (_monitoring)
        {
            DisableMonitoring();
            return;
        }

        _monitoring = true;
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = new CancellationTokenSource();
        _eventLog.Record("Monitoring enabled");
        ApplyTimerInterval();
        _timer.Start();
        SetTrayState(TrayState.On);
        await SendScreenshotAsync(manual: false, _monitoringCancellation.Token);
    }

    private void DisableMonitoring()
    {
        _timer.Stop();
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _monitoring = false;
        _eventLog.Record("Monitoring disabled");
        SetTrayState(TrayState.Off);
    }

    private async Task SendScreenshotAsync(bool manual, CancellationToken cancellationToken = default)
    {
        if (_uploadInProgress)
        {
            return;
        }

        _uploadInProgress = true;
        string? screenshotPath = null;
        try
        {
            var monitor = _monitorCatalog.GetSelectedOrDefault(_configStore.Config.SelectedMonitor);
            screenshotPath = _capture.CaptureToTempPng(monitor);
            await _uploader.UploadScreenshotAsync(
                _configStore.Config.WebhookUrl,
                screenshotPath,
                manual ? "📸 Manual Screenshot" : "📸 Screenshot",
                cancellationToken);

            _stats.ScreenshotsSent++;
            _stats.LastUploadTime = DateTime.Now;

            if (_stats.LastUploadFailed)
            {
                _eventLog.Record("Upload recovered");
            }

            _stats.LastUploadFailed = false;
            SetTrayState(_monitoring ? TrayState.On : TrayState.Off);

            if (manual)
            {
                _eventLog.Record("Manual screenshot taken");
            }
        }
        catch (OperationCanceledException) when (!manual)
        {
        }
        catch (Exception ex)
        {
            _stats.UploadErrors++;
            _stats.LastUploadFailed = true;
            _eventLog.Record($"Upload failed: {ex.Message}");
            SetTrayState(TrayState.Error);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(screenshotPath) && File.Exists(screenshotPath))
            {
                try
                {
                    File.Delete(screenshotPath);
                }
                catch
                {
                    // Temporary screenshots are best-effort cleanup.
                }
            }

            _uploadInProgress = false;
        }
    }

    private void SetWebhook()
    {
        var value = SimpleInput.Prompt("Set Webhook", "Discord webhook URL:", _configStore.Config.WebhookUrl);
        if (value is null)
        {
            return;
        }

        _configStore.Config.WebhookUrl = value;
        _configStore.Save();
        _eventLog.Record("Webhook updated");
    }

    private void SetInterval()
    {
        var value = SimpleInput.Prompt("Set Interval", "Interval in seconds (minimum 15):", _configStore.Config.IntervalSeconds.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var seconds))
        {
            MessageBox.Show("Enter a whole number of seconds.", "MacroWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configStore.Config.IntervalSeconds = Math.Max(15, seconds);
        _configStore.Save();
        ApplyTimerInterval();
        _eventLog.Record($"Interval changed: {_configStore.Config.IntervalSeconds} seconds");
    }

    private void SelectMonitor()
    {
        var selected = MonitorPicker.Pick(_monitorCatalog.GetMonitors(), _configStore.Config.SelectedMonitor);
        if (selected is null)
        {
            return;
        }

        _configStore.Config.SelectedMonitor = selected.Id;
        _configStore.Save();
        _eventLog.Record($"Monitor changed: {selected.DisplayName}");
    }

    private void ToggleStartup()
    {
        _configStore.Config.StartWithWindows = !_configStore.Config.StartWithWindows;
        _configStore.Save();
        _startup.SetStartWithWindows(_configStore.Config.StartWithWindows);
    }

    private void ShowLog()
    {
        var monitor = _monitorCatalog.GetSelectedOrDefault(_configStore.Config.SelectedMonitor);
        var window = new LogWindow(_configStore.Config, _stats, GetStatusText(), monitor.DisplayName, _eventLog);
        window.Show();
    }

    private void ApplyTimerInterval()
    {
        _timer.Interval = Math.Max(15, _configStore.Config.IntervalSeconds) * 1000;
        if (_monitoring)
        {
            _timer.Stop();
            _timer.Start();
        }
    }

    private void SetTrayState(TrayState state)
    {
        _trayState = state;
        var oldIcon = _notifyIcon.Icon;
        _notifyIcon.Icon = IconFactory.Create(state);
        oldIcon?.Dispose();
    }

    private string GetStatusText()
    {
        return _trayState switch
        {
            TrayState.Error => "ERROR",
            TrayState.On => "ON",
            _ => "OFF"
        };
    }

    private void Exit()
    {
        _timer.Stop();
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _monitoringCancellation?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
