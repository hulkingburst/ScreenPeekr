using MacroWatch.Models;

namespace MacroWatch.Tray;

internal static class MonitorPicker
{
    public static MonitorInfo? Pick(IReadOnlyList<MonitorInfo> monitors, string selectedId)
    {
        using var form = new Form
        {
            Text = "Select Monitor",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(420, 250)
        };

        var list = new ListBox { Left = 12, Top = 12, Width = 396, Height = 185, DisplayMember = nameof(MonitorInfo.DisplayName) };
        foreach (var monitor in monitors)
        {
            list.Items.Add(monitor);
        }

        var selected = monitors.FirstOrDefault(m => string.Equals(m.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        list.SelectedItem = selected ?? monitors.FirstOrDefault();

        var ok = new Button { Text = "OK", Left = 252, Width = 75, Top = 210, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 333, Width = 75, Top = 210, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { list, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK ? list.SelectedItem as MonitorInfo : null;
    }
}
