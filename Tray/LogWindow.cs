using ScreenPeekr.Models;
using ScreenPeekr.Services;

namespace ScreenPeekr.Tray;

internal sealed class LogWindow : Form
{
    private readonly TextBox _textBox = new();

    public LogWindow(AppConfig config, RuntimeStats stats, string status, string selectedMonitor, EventLogStore eventLog)
    {
        Text = "ScreenPeekr Log";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(720, 560);

        _textBox.Multiline = true;
        _textBox.ReadOnly = true;
        _textBox.ScrollBars = ScrollBars.Both;
        _textBox.Dock = DockStyle.Fill;
        _textBox.Font = new Font(FontFamily.GenericMonospace, 10);
        Controls.Add(_textBox);

        _textBox.Text = BuildText(config, stats, status, selectedMonitor, eventLog);
    }

    private static string BuildText(AppConfig config, RuntimeStats stats, string status, string selectedMonitor, EventLogStore eventLog)
    {
        var monitorText = string.Equals(config.SelectedMonitor, "ALL", StringComparison.OrdinalIgnoreCase)
            ? "All Monitors"
            : selectedMonitor;

        var preKeysText = config.PreScreenshotKeys.Count == 0
            ? "None"
            : string.Join(", ", config.PreScreenshotKeys);
        var postKeysText = config.PostScreenshotKeys.Count == 0
            ? "None"
            : string.Join(", ", config.PostScreenshotKeys);

        var lines = new List<string>
        {
            $"Status: {status}",
            $"Selected Monitor: {monitorText}",
            $"Interval: {config.IntervalSeconds} seconds",
            $"Change Sensitivity: {config.ChangeDetectionSensitivity}",
            $"Away-Only Mode: {config.AwayOnlyMode}",
            $"Away Idle Threshold: {config.AwayIdleThresholdSeconds} seconds",
            $"Pre-screenshot Inputs: {preKeysText}",
            $"Post-screenshot Inputs: {postKeysText}",
            $"Input Delay: {config.InputDelayMs} ms",
            $"Key Hold Duration: {config.KeyHoldDurationMs} ms",
            $"Screenshot Retention: {config.ScreenshotRetentionDays} days, {config.ScreenshotRetentionCount} files",
            $"Webhook Healthy: {stats.WebhookHealthy}",
            $"Screenshots Sent: {stats.ScreenshotsSent}",
            $"Skipped No Change: {stats.ScreenshotsSkippedNoChange}",
            $"Skipped User Active: {stats.ScreenshotsSkippedActive}",
            $"Upload Errors: {stats.UploadErrors}",
            $"Consecutive Upload Failures: {stats.ConsecutiveUploadFailures}",
            $"Last Upload Time: {(stats.LastUploadTime.HasValue ? stats.LastUploadTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}",
            string.Empty,
            "Events:",
            eventLog.ReadAll()
        };

        return string.Join(Environment.NewLine, lines);
    }
}
