using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ScreenPeekr.Tray;

internal enum TrayState
{
    Off,
    On,
    Error
}

internal static class IconFactory
{
    public static Icon Create(TrayState state)
    {
        var color = state switch
        {
            TrayState.On => Color.FromArgb(38, 166, 91),
            TrayState.Error => Color.FromArgb(220, 53, 69),
            _ => Color.FromArgb(118, 124, 130)
        };

        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var fill = new SolidBrush(color);
        using var border = new Pen(Color.FromArgb(40, 40, 40), 2);
        graphics.FillEllipse(fill, 4, 4, 24, 24);
        graphics.DrawEllipse(border, 4, 4, 24, 24);

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
