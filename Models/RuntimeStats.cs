namespace ScreenPeekr.Models;

internal sealed class RuntimeStats
{
    public int ScreenshotsSent { get; set; }
    public int UploadErrors { get; set; }
    public DateTime StartedTime { get; } = DateTime.Now;
    public DateTime? LastUploadTime { get; set; }
    public bool LastUploadFailed { get; set; }
    public bool WebhookHealthy { get; set; } = true;
    public int ConsecutiveUploadFailures { get; set; }
    public int ScreenshotsSkippedNoChange { get; set; }
    public int ScreenshotsSkippedActive { get; set; }
}
