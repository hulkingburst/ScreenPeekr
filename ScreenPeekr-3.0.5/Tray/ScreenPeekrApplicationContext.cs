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
    private readonly RuntimeStats _stats = new();
    private readonly NotifyIcon _notifyIcon;
    
    private Task? _monitoringTask;
    private CancellationTokenSource? _monitoringCancellation;
    private CancellationTokenSource _intervalChangeCts = new();
    private bool _monitoring;
    private bool _uploadInProgress;
    private TrayState _trayState = TrayState.Off;

    public ScreenPeekrApplicationContext(
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

        _monitoring = true;
        _monitoringCancellation?.Dispose();
        _monitoringCancellation = new CancellationTokenSource();
        _eventLog.Record("Monitoring enabled");
        SetTrayState(TrayState.On);

        // Bug fix: send first screenshot immediately before entering the timed loop,
        // as documented: "sends one screenshot immediately, then continues on the configured interval."
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
                // Interval changed — restart the wait loop without taking a screenshot
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

    private void OpenSettings()
    {
        using var settingsWindow = new SettingsWindow(_configStore, _monitorCatalog, _startup, _eventLog);
        settingsWindow.ShowDialog();
        
        // Trigger interval change if it was modified
        TriggerIntervalChange();
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
