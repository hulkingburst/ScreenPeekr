namespace MacroWatch.Models;

internal sealed class AppConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 60;
    public string SelectedMonitor { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; }
}
