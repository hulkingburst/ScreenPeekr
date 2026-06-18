using ScreenPeekr.Models;
using ScreenPeekr.Services;

namespace ScreenPeekr.Tray;

internal sealed class ScreenPeekrApplicationContext : ApplicationContext
{
    private readonly ConfigStore _configStore;
    private readonly EventLogStore _eventLog;
    private readonly StartupService _startup;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly ScreenshotCaptureService _capture;
    private readonly DiscordWebhookClient _uploader;
    private readonly ScreenshotChangeDetector _changeDetector;
    private readonly ScreenshotCleanupService _cleanup;
    private readonly RuntimeStats _stats = new();
    private readonly NotifyIcon _notifyIcon;

    private Task? _monitoringTask;
    private CancellationTokenSource? _monitoringCancellation;
    private CancellationTokenSource _intervalChangeCts = new();
    private bool _monitoring;
    private bool _uploadInProgress;
    private bool _usingMonitorFallback;
    private TrayState _trayState = TrayState.Off;

    public ScreenPeekrApplicationContext(
        ConfigStore configStore,
        EventLogStore eventLog,
        StartupService startup,
        MonitorCatalog monitorCatalog,
        ScreenshotCaptureService capture,
        DiscordWebhookClient uploader,
        ScreenshotChangeDetector changeDetector,
        ScreenshotCleanupService cleanup)
    {
        _configStore = configStore;
        _eventLog = eventLog;
        _startup = startup;
        _monitorCatalog = monitorCatalog;
        _capture = capture;
        _uploader = uploader;
        _changeDetector = changeDetector;
        _cleanup = cleanup;

        if (string.IsNullOrWhiteSpace(_configStore.Config.SelectedMonitor))
        {
            _configStore.Config.SelectedMonitor = _monitorCatalog.GetSelectedOrDefault(string.Empty).Id;
            _configStore.Save();
        }

        _notifyIcon = new NotifyIcon
        {
            Text = "ScreenPeekr",
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
        menu.Items.Add(new ToolStripMenuItem("ScreenPeekr") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem($"Status: {GetStatusText()}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Toggle ON/OFF", null, async (_, _) => await ToggleMonitoringAsync()));
        menu.Items.Add(new ToolStripMenuItem("Set Webhook...", null, (_, _) => SetWebhook()));
        menu.Items.Add(new ToolStripMenuItem("Set Interval...", null, (_, _) => SetInterval()));
        menu.Items.Add(new ToolStripMenuItem("Select Monitor...", null, (_, _) => SelectMonitor()));
        menu.Items.Add(new ToolStripMenuItem("Set Pre-Screenshot Input...", null, (_, _) => SetPreScreenshotInput()));
        menu.Items.Add(new ToolStripMenuItem("Set Post-Screenshot Inputs...", null, (_, _) => SetPostScreenshotInputs()));
        menu.Items.Add(new ToolStripMenuItem("Set Input Delay...", null, (_, _) => SetInputDelay()));
        menu.Items.Add(new ToolStripMenuItem("Set Key Hold Duration...", null, (_, _) => SetKeyHoldDuration()));
        menu.Items.Add(new ToolStripMenuItem("Set Change Sensitivity...", null, (_, _) => SetChangeSensitivity()));
        menu.Items.Add(new ToolStripMenuItem("Away-Only Mode", null, (_, _) => ToggleAwayOnlyMode())
        {
            Checked = _configStore.Config.AwayOnlyMode
        });
        menu.Items.Add(new ToolStripMenuItem("Set Away Idle Threshold...", null, (_, _) => SetAwayIdleThreshold()));
        menu.Items.Add(new ToolStripMenuItem("Set Screenshot Retention...", null, (_, _) => SetScreenshotRetention()));
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

        if (string.IsNullOrWhiteSpace(_configStore.Config.WebhookUrl))
        {
            _eventLog.Record("Monitoring blocked: webhook URL is not configured");
            SetTrayState(TrayState.Error);
            return;
        }

        using (var validationCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            try
            {
                await _uploader.ValidateWebhookAsync(_configStore.Config.WebhookUrl, validationCts.Token);
                _stats.WebhookHealthy = true;
                _stats.ConsecutiveUploadFailures = 0;
                _eventLog.Record("Webhook validation passed");
            }
            catch (Exception ex)
            {
                _stats.WebhookHealthy = false;
                _eventLog.Record($"Webhook validation failed; monitoring will retry uploads: {ex.Message}");
                SetTrayState(TrayState.Error);
            }
        }

        _monitoring = true;
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = new CancellationTokenSource();
        _changeDetector.Reset();
        _eventLog.Record("Monitoring enabled");
        SetTrayState(_stats.WebhookHealthy ? TrayState.On : TrayState.Error);

        var token = _monitoringCancellation.Token;
        _monitoringTask = Task.Run(async () =>
        {
            try
            {
                await ExecuteScreenshotCycleAsync(token);
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _eventLog.Record($"Cycle error: {ex.Message}");
            }

            await RunMonitoringLoopAsync(token);
        }, token);
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
            }

            _monitoringTask = null;
        }

        _monitoringCancellation?.Dispose();
        _monitoringCancellation = null;
        _changeDetector.Reset();
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
        string? screenshotPath = null;
        try
        {
            if (_configStore.Config.AwayOnlyMode)
            {
                var idleThreshold = TimeSpan.FromSeconds(_configStore.Config.AwayIdleThresholdSeconds);
                if (!IdleStateService.IsIdle(idleThreshold))
                {
                    _stats.ScreenshotsSkippedActive++;
                    _eventLog.Record("Capture skipped: user is active");
                    return;
                }
            }

            await SendConfiguredInputsAsync(_configStore.Config.PreScreenshotKeys, "Pre-screenshot input", token);

            var monitor = ResolveSelectedMonitor();
            screenshotPath = _capture.CaptureToTempPng(monitor);
            await SendConfiguredInputsAsync(_configStore.Config.PostScreenshotKeys, "Post-screenshot input", token);

            if (!_changeDetector.HasMeaningfulChange(screenshotPath, _configStore.Config.ChangeDetectionSensitivity))
            {
                _stats.ScreenshotsSkippedNoChange++;
                _eventLog.Record("Upload skipped: no meaningful screenshot change detected");
                ScreenshotCleanupService.TryDelete(screenshotPath);
                screenshotPath = null;
                return;
            }

            await _uploader.UploadScreenshotAsync(_configStore.Config.WebhookUrl, screenshotPath, "Screenshot", token);
            RecordUploadSuccess();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RecordUploadFailure(ex);
        }
        finally
        {
            _cleanup.Cleanup(_configStore.Config, screenshotPath);
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
            var monitor = ResolveSelectedMonitor();
            screenshotPath = _capture.CaptureToTempPng(monitor);
            await _uploader.UploadScreenshotAsync(
                _configStore.Config.WebhookUrl,
                screenshotPath,
                manual ? "Manual Screenshot" : "Screenshot",
                cancellationToken);

            RecordUploadSuccess();

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
            RecordUploadFailure(ex);
        }
        finally
        {
            _cleanup.Cleanup(_configStore.Config, screenshotPath);
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
            MessageBox.Show("Enter a whole number of seconds.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show("No monitors detected.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _usingMonitorFallback = false;
            _eventLog.Record($"Monitor mode changed: {selected.DisplayName}");
        }
    }

    private void SetPreScreenshotInput()
    {
        var value = SimpleInput.Prompt("Set Pre-Screenshot Inputs", "Comma-separated keys, or blank for none:", FormatKeys(_configStore.Config.PreScreenshotKeys));
        if (value is null)
        {
            return;
        }

        var keys = ParseKeys(value);
        _configStore.Config.PreScreenshotKeys = keys;
        _configStore.Config.PreScreenshotKey = keys.FirstOrDefault(Keys.None);
        _configStore.Save();
        _eventLog.Record($"Pre-screenshot inputs changed: {FormatKeys(keys)}");
    }

    private void SetPostScreenshotInputs()
    {
        var value = SimpleInput.Prompt("Set Post-Screenshot Inputs", "Comma-separated keys, or blank for none:", FormatKeys(_configStore.Config.PostScreenshotKeys));
        if (value is null)
        {
            return;
        }

        var keys = ParseKeys(value);
        _configStore.Config.PostScreenshotKeys = keys;
        _configStore.Save();
        _eventLog.Record($"Post-screenshot inputs changed: {FormatKeys(keys)}");
    }

    private void SetInputDelay()
    {
        var value = SimpleInput.Prompt("Set Input Delay", "Delay between input keys in milliseconds:", _configStore.Config.InputDelayMs.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var ms) || ms < 0)
        {
            MessageBox.Show("Enter a non-negative number of milliseconds.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_configStore.Config.InputDelayMs != ms)
        {
            _configStore.Config.InputDelayMs = ms;
            _configStore.Save();
            _eventLog.Record($"Input delay changed: {ms} ms");
        }
    }

    private void SetKeyHoldDuration()
    {
        var value = SimpleInput.Prompt("Set Key Hold Duration", "Key hold duration in milliseconds:", _configStore.Config.KeyHoldDurationMs.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var ms) || ms < 0)
        {
            MessageBox.Show("Enter a non-negative number of milliseconds.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configStore.Config.KeyHoldDurationMs = ms;
        _configStore.Save();
        _eventLog.Record($"Key hold duration changed: {ms} ms");
    }

    private void SetChangeSensitivity()
    {
        var value = SimpleInput.Prompt("Set Change Sensitivity", "0-100. Higher is stricter and uploads less often:", _configStore.Config.ChangeDetectionSensitivity.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var sensitivity))
        {
            MessageBox.Show("Enter a whole number from 0 to 100.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configStore.Config.ChangeDetectionSensitivity = Math.Clamp(sensitivity, 0, 100);
        _configStore.Save();
        _eventLog.Record($"Change sensitivity changed: {_configStore.Config.ChangeDetectionSensitivity}");
    }

    private void ToggleAwayOnlyMode()
    {
        _configStore.Config.AwayOnlyMode = !_configStore.Config.AwayOnlyMode;
        _configStore.Save();
        _eventLog.Record($"Away-only mode {(_configStore.Config.AwayOnlyMode ? "enabled" : "disabled")}");
    }

    private void SetAwayIdleThreshold()
    {
        var value = SimpleInput.Prompt("Set Away Idle Threshold", "Idle seconds before capture is allowed:", _configStore.Config.AwayIdleThresholdSeconds.ToString());
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var seconds) || seconds < 1)
        {
            MessageBox.Show("Enter a positive whole number of seconds.", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configStore.Config.AwayIdleThresholdSeconds = seconds;
        _configStore.Save();
        _eventLog.Record($"Away idle threshold changed: {seconds} seconds");
    }

    private void SetScreenshotRetention()
    {
        var current = $"{_configStore.Config.ScreenshotRetentionDays},{_configStore.Config.ScreenshotRetentionCount}";
        var value = SimpleInput.Prompt("Set Screenshot Retention", "Days,count. Example: 1,50", current);
        if (value is null)
        {
            return;
        }

        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var days) || !int.TryParse(parts[1], out var count) || days < 0 || count < 0)
        {
            MessageBox.Show("Enter retention as two non-negative numbers: days,count", "ScreenPeekr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configStore.Config.ScreenshotRetentionDays = days;
        _configStore.Config.ScreenshotRetentionCount = count;
        _configStore.Save();
        _cleanup.Cleanup(_configStore.Config);
        _eventLog.Record($"Screenshot retention changed: {days} days, {count} files");
    }

    private void ToggleStartup()
    {
        _configStore.Config.StartWithWindows = !_configStore.Config.StartWithWindows;
        _configStore.Save();
        _startup.SetStartWithWindows(_configStore.Config.StartWithWindows);
    }

    private void ShowLog()
    {
        var monitor = ResolveSelectedMonitor();
        var window = new LogWindow(_configStore.Config, _stats, GetStatusText(), monitor.DisplayName, _eventLog);
        window.Show();
    }

    private async Task SendConfiguredInputsAsync(IReadOnlyCollection<Keys> keys, string label, CancellationToken token)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var activeWindow = InputSimulator.GetForegroundWindow();
        if (activeWindow == IntPtr.Zero)
        {
            _eventLog.Record($"{label} skipped: no active window");
            return;
        }

        await InputSimulator.SendKeysAsync(keys, _configStore.Config.InputDelayMs, _configStore.Config.KeyHoldDurationMs, token);
        _eventLog.Record($"{label} sent: {FormatKeys(keys)}");
    }

    private MonitorInfo ResolveSelectedMonitor()
    {
        if (string.Equals(_configStore.Config.SelectedMonitor, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            return _monitorCatalog.GetSelectedOrDefault(_configStore.Config.SelectedMonitor);
        }

        try
        {
            var monitors = _monitorCatalog.GetMonitors();
            var selected = monitors.FirstOrDefault(m => string.Equals(m.Id, _configStore.Config.SelectedMonitor, StringComparison.OrdinalIgnoreCase));
            if (selected is not null)
            {
                if (_usingMonitorFallback)
                {
                    _eventLog.Record($"Selected monitor reconnected: {selected.DisplayName}");
                    _usingMonitorFallback = false;
                }

                return selected;
            }
        }
        catch (Exception ex)
        {
            _eventLog.Record($"Monitor enumeration failed: {ex.Message}");
        }

        var primary = Screen.PrimaryScreen;
        if (primary is null)
        {
            return _monitorCatalog.GetSelectedOrDefault(_configStore.Config.SelectedMonitor);
        }

        if (!_usingMonitorFallback)
        {
            _eventLog.Record("Selected monitor unavailable; using primary display until it returns");
            _usingMonitorFallback = true;
        }

        return new MonitorInfo(primary.DeviceName, "Primary Display (fallback)", primary.Bounds);
    }

    private void RecordUploadSuccess()
    {
        _stats.ScreenshotsSent++;
        _stats.LastUploadTime = DateTime.Now;

        if (_stats.LastUploadFailed)
        {
            _eventLog.Record("Upload recovered");
        }

        _stats.LastUploadFailed = false;
        _stats.WebhookHealthy = true;
        _stats.ConsecutiveUploadFailures = 0;
        SetTrayState(_monitoring ? TrayState.On : TrayState.Off);
    }

    private void RecordUploadFailure(Exception ex)
    {
        _stats.UploadErrors++;
        _stats.LastUploadFailed = true;
        _stats.ConsecutiveUploadFailures++;

        if (_stats.ConsecutiveUploadFailures >= 3)
        {
            _stats.WebhookHealthy = false;
            _eventLog.Record("Webhook marked unhealthy after repeated failures");
        }

        _eventLog.Record($"Upload failed: {ex.Message}");
        SetTrayState(TrayState.Error);
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

    private static string FormatKeys(IEnumerable<Keys> keys)
    {
        var text = string.Join(",", keys.Where(key => key != Keys.None).Select(key => key.ToString()));
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }

    private static List<Keys> ParseKeys(string value)
    {
        var keys = new List<Keys>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Keys>(part, true, out var key) && key != Keys.None)
            {
                keys.Add(key);
            }
            else if (int.TryParse(part, out var numeric) && numeric != (int)Keys.None)
            {
                keys.Add((Keys)numeric);
            }
        }

        return keys;
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
