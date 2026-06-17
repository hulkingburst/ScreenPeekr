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
    
    private Task? _monitoringTask;
    private CancellationTokenSource? _monitoringCancellation;
    private CancellationTokenSource _intervalChangeCts = new();
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
        menu.Items.Add(new ToolStripMenuItem("Set Pre-Screenshot Input...", null, (_, _) => SetPreScreenshotInput()));
        menu.Items.Add(new ToolStripMenuItem("Set Input Delay...", null, (_, _) => SetInputDelay()));
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
            await DisableMonitoringAsync();
            return;
        }

        _monitoring = true;
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = new CancellationTokenSource();
        _eventLog.Record("Monitoring enabled");
        SetTrayState(TrayState.On);

        var token = _monitoringCancellation.Token;
        _monitoringTask = Task.Run(() => RunMonitoringLoopAsync(token), token);
    }

    private async Task DisableMonitoringAsync()
    {
        if (!_monitoring) return;

        _monitoring = false;
        _monitoringCancellation?.Cancel();

        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch
            {
                // Ignore task cancellation exceptions
            }
            _monitoringTask = null;
        }

        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _eventLog.Record("Monitoring disabled");
        SetTrayState(TrayState.Off);
    }

    private async Task RunMonitoringLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _intervalChangeCts.Token);
                await Task.Delay(TimeSpan.FromSeconds(_configStore.Config.IntervalSeconds), linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                continue;
            }

            try
            {
                await ExecuteScreenshotCycleAsync(token);
            }
            catch (Exception ex)
            {
                _eventLog.Record($"Cycle error: {ex.Message}");
            }
        }
    }

    private async Task ExecuteScreenshotCycleAsync(CancellationToken token)
    {
        if (_uploadInProgress)
        {
            return;
        }

        _uploadInProgress = true;
        try
        {
            var preInputKey = _configStore.Config.PreScreenshotKey;
            if (preInputKey != Keys.None)
            {
                var activeWindow = InputSimulator.GetForegroundWindow();
                if (activeWindow == IntPtr.Zero)
                {
                    _eventLog.Record("Input skipped: no active window");
                }
                else
                {
                    InputSimulator.SendKey((ushort)preInputKey);
                    _eventLog.Record($"Input sent: {preInputKey}");
                }

                var delayMs = _configStore.Config.InputDelayMs;
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, token);
                }
            }
            else
            {
                _eventLog.Record("Input skipped: disabled");
            }

            string? screenshotPath = null;
            try
            {
                var monitor = _monitorCatalog.GetSelectedOrDefault(_configStore.Config.SelectedMonitor);
                screenshotPath = _capture.CaptureToTempPng(monitor);
                await _uploader.UploadScreenshotAsync(
                    _configStore.Config.WebhookUrl,
                    screenshotPath,
                    "📸 Screenshot",
                    token);

                _stats.ScreenshotsSent++;
                _stats.LastUploadTime = DateTime.Now;

                if (_stats.LastUploadFailed)
                {
                    _eventLog.Record("Upload recovered");
                }

                _stats.LastUploadFailed = false;
                SetTrayState(_monitoring ? TrayState.On : TrayState.Off);
            }
            catch (OperationCanceledException)
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
                    }
                }
            }
        }
        finally
        {
            _uploadInProgress = false;
        }
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
        var value = SimpleInput.Prompt("Set Interval", "Interval in seconds (minimum 5):", _configStore.Config.IntervalSeconds.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var seconds))
        {
            MessageBox.Show("Enter a whole number of seconds.", "MacroWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var oldInterval = _configStore.Config.IntervalSeconds;
        var newInterval = Math.Max(5, seconds);
        if (oldInterval != newInterval)
        {
            _configStore.Config.IntervalSeconds = newInterval;
            _configStore.Save();
            TriggerIntervalChange();
            _eventLog.Record($"Interval changed: {_configStore.Config.IntervalSeconds} seconds");
        }
    }

    private void SelectMonitor()
    {
        var monitors = new List<MonitorInfo>();
        try
        {
            monitors.Add(new MonitorInfo("ALL", "All Monitors", SystemInformation.VirtualScreen));
        }
        catch
        {
        }

        try
        {
            monitors.AddRange(_monitorCatalog.GetMonitors());
        }
        catch (Exception ex)
        {
            _eventLog.Record($"Failed to enumerate monitors: {ex.Message}");
        }

        if (monitors.Count == 0)
        {
            MessageBox.Show("No monitors detected.", "MacroWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var selected = MonitorPicker.Pick(monitors, _configStore.Config.SelectedMonitor);
        if (selected is null)
        {
            return;
        }

        var oldSelection = _configStore.Config.SelectedMonitor;
        if (!string.Equals(oldSelection, selected.Id, StringComparison.OrdinalIgnoreCase))
        {
            _configStore.Config.SelectedMonitor = selected.Id;
            _configStore.Save();
            _eventLog.Record($"Monitor mode changed: {selected.DisplayName}");
        }
    }

    private void SetPreScreenshotInput()
    {
        using var binder = new KeyBinderForm(_configStore.Config.PreScreenshotKey);
        if (binder.ShowDialog() == DialogResult.OK)
        {
            var oldKey = _configStore.Config.PreScreenshotKey;
            var newKey = binder.SelectedKey;
            if (oldKey != newKey)
            {
                _configStore.Config.PreScreenshotKey = newKey;
                _configStore.Save();
                _eventLog.Record($"Input binding changed: {newKey}");
            }
        }
    }

    private void SetInputDelay()
    {
        var value = SimpleInput.Prompt("Set Input Delay", "Delay in milliseconds:", _configStore.Config.InputDelayMs.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var ms) || ms < 0)
        {
            MessageBox.Show("Enter a non-negative number of milliseconds.", "MacroWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var oldDelay = _configStore.Config.InputDelayMs;
        if (oldDelay != ms)
        {
            _configStore.Config.InputDelayMs = ms;
            _configStore.Save();
            _eventLog.Record($"Input delay changed: {ms} ms");
        }
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

    private void TriggerIntervalChange()
    {
        var oldCts = _intervalChangeCts;
        _intervalChangeCts = new CancellationTokenSource();
        oldCts.Cancel();
        oldCts.Dispose();
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
        _monitoring = false;
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _intervalChangeCts.Cancel();
        _intervalChangeCts.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitoringCancellation?.Dispose();
            _intervalChangeCts.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
