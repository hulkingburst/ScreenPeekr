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
        var lines = new List<string>
        {
            $"Status: {status}",
            $"Selected Monitor: {selectedMonitor}",
            $"Current Interval: {config.IntervalSeconds} seconds",
            $"Screenshots Sent: {stats.ScreenshotsSent}",
            $"Upload Errors: {stats.UploadErrors}",
            $"Started Time: {stats.StartedTime:yyyy-MM-dd HH:mm:ss}",
            $"Last Upload Time: {(stats.LastUploadTime.HasValue ? stats.LastUploadTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}",
            string.Empty,
            "Events:",
            eventLog.ReadAll()
        };

        return string.Join(Environment.NewLine, lines);
    }
}
