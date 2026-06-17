using System.Windows.Forms;

namespace MacroWatch.Models;

internal sealed class AppConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 60;
    public string SelectedMonitor { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; }
    public Keys PreScreenshotKey { get; set; } = Keys.None;
    public int InputDelayMs { get; set; } = 250;
}
