using System.Windows.Forms;

namespace ScreenPeekr.Models;

internal sealed class AppConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 60;
    public string SelectedMonitor { get; set; } = "ALL";
    public bool StartWithWindows { get; set; }
    public Keys PreScreenshotKey { get; set; } = Keys.None;
    public List<Keys> PreScreenshotKeys { get; set; } = new();
    public List<Keys> PostScreenshotKeys { get; set; } = new();
    public int InputDelayMs { get; set; } = 100;
    public int KeyHoldDurationMs { get; set; } = 50;
    public bool EnableChangeDetection { get; set; } = false;
    public int ChangeDetectionSensitivity { get; set; } = 50;
    public bool AwayOnlyMode { get; set; }
    public int AwayIdleThresholdSeconds { get; set; } = 600;
    public int ScreenshotRetentionDays { get; set; } = 3;
    public int ScreenshotRetentionCount { get; set; } = 30;
}
