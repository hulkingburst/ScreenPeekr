using System.Drawing;
using System.Windows.Forms;

namespace ScreenPeekr.Tray;

internal sealed class KeyBinderForm : Form
{
    public Keys SelectedKey { get; private set; } = Keys.None;

    public KeyBinderForm(Keys currentKey)
    {
        Text = "Bind Pre-Screenshot Input";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(400, 180);
        KeyPreview = true;

        var labelTitle = new Label
        {
            Text = "Press a key to bind it as the pre-screenshot input.",
            Left = 20,
            Top = 20,
            Width = 360,
            Height = 30,
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
        };

        var labelHelp = new Label
        {
            Text = "Supported: A-Z, 0-9, F1-F12, Tab, Enter, Space, Arrows.\nPress ESC to disable (Set to None).",
            Left = 20,
            Top = 50,
            Width = 360,
            Height = 40,
            TextAlign = ContentAlignment.TopCenter,
            ForeColor = Color.Gray
        };

        var currentKeyFormatted = currentKey == Keys.None ? "None" : currentKey.ToString();
        var labelCurrent = new Label
        {
            Text = $"Current Binding: {currentKeyFormatted}",
            Left = 20,
            Top = 100,
            Width = 360,
            Height = 20,
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Italic)
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Left = 160,
            Top = 135,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] { labelTitle, labelHelp, labelCurrent, cancelBtn });
        CancelButton = cancelBtn;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var key = keyData & Keys.KeyCode;

        if (key == Keys.Escape)
        {
            SelectedKey = Keys.None;
            DialogResult = DialogResult.OK;
            Close();
            return true;
        }

        if (IsSupportedKey(key))
        {
            SelectedKey = key;
            DialogResult = DialogResult.OK;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static bool IsSupportedKey(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z) return true;
        if (key >= Keys.D0 && key <= Keys.D9) return true;
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return true;
        if (key >= Keys.F1 && key <= Keys.F12) return true;

        return key is Keys.Tab
            or Keys.Enter
            or Keys.Return
            or Keys.Space
            or Keys.Up
            or Keys.Down
            or Keys.Left
            or Keys.Right;
    }
}
