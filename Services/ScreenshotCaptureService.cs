using System.Drawing.Imaging;
using ScreenPeekr.Models;

namespace ScreenPeekr.Services;

internal sealed class ScreenshotCaptureService : IDisposable
{
    private readonly ScreenshotCleanupService _cleanup;

    public ScreenshotCaptureService(ScreenshotCleanupService cleanup)
    {
        _cleanup = cleanup;
    }

    public string CaptureToTempPng(MonitorInfo monitor)
    {
        var path = _cleanup.CreatePath();
        using var bitmap = new Bitmap(monitor.Bounds.Width, monitor.Bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        try
        {
            graphics.CopyFromScreen(monitor.Bounds.Left, monitor.Bounds.Top, 0, 0, monitor.Bounds.Size, CopyPixelOperation.SourceCopy);
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }
        catch
        {
            ScreenshotCleanupService.TryDelete(path);
            throw;
        }
    }

    public void Dispose()
    {
    }
}
