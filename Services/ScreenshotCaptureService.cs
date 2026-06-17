using System.Drawing.Imaging;
using ScreenPeekr.Models;

namespace ScreenPeekr.Services;

internal sealed class ScreenshotCaptureService : IDisposable
{
    public string CaptureToTempPng(MonitorInfo monitor)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ScreenPeekr_{Guid.NewGuid():N}.png");
        using var bitmap = new Bitmap(monitor.Bounds.Width, monitor.Bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(monitor.Bounds.Left, monitor.Bounds.Top, 0, 0, monitor.Bounds.Size, CopyPixelOperation.SourceCopy);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    public void Dispose()
    {
    }
}
