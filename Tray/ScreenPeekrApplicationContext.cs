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
        menu.Items.Add(new ToolStripMenuItem("Take Screenshot Now", null, async (_, _) => await SendScreenshotAsync(manual: true)));
        menu.Items.Add(new ToolStripMenuItem("Open Settings", null, (_, _) => OpenSettings()));
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

    private void OpenSettings()
    {
        using var settingsWindow = new SettingsWindow(_configStore, _monitorCatalog, _startup, _eventLog);
        settingsWindow.ShowDialog();
        
        // Trigger interval change if it was modified
        TriggerIntervalChange();
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
