using MacroWatch.Models;
using MacroWatch.Services;

namespace MacroWatch.Tray;

internal sealed class LogWindow : Form
{
    private readonly TextBox _textBox = new();

    public LogWindow(AppConfig config, RuntimeStats stats, string status, string selectedMonitor, EventLogStore eventLog)
    {
        Text = "MacroWatch Log";
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

        var keyText = config.PreScreenshotKey == System.Windows.Forms.Keys.None 
            ? "None" 
            : config.PreScreenshotKey.ToString();

        var lines = new List<string>
        {
            $"Status: {status}",
            $"Selected Monitor: {monitorText}",
            $"Interval: {config.IntervalSeconds} seconds",
            $"Pre-screenshot Input Key: {keyText}",
            $"Input Delay: {config.InputDelayMs} ms",
            $"Screenshots Sent: {stats.ScreenshotsSent}",
            $"Upload Errors: {stats.UploadErrors}",
            $"Last Upload Time: {(stats.LastUploadTime.HasValue ? stats.LastUploadTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}",
            string.Empty,
            "Events:",
            eventLog.ReadAll()
        };

        return string.Join(Environment.NewLine, lines);
    }
}
