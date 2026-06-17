namespace ScreenPeekr.Tray;

internal static class SimpleInput
{
    public static string? Prompt(string title, string label, string value = "")
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(520, 135)
        };

        var labelControl = new Label { Left = 12, Top = 14, Width = 496, Text = label };
        var input = new TextBox { Left = 12, Top = 40, Width = 496, Text = value };
        var ok = new Button { Text = "OK", Left = 352, Width = 75, Top = 88, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 433, Width = 75, Top = 88, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { labelControl, input, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
    }
}
