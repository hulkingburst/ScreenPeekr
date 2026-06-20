using System.Drawing.Imaging;
using ScreenPeekr.Models;

namespace ScreenPeekr.Services;

internal sealed class ScreenshotChangeDetector
{
    private const int SampleWidth = 64;
    private const int SampleHeight = 36;
    private const int PixelDeltaThreshold = 30;
    private readonly object _sync = new();
    private Bitmap? _previousSample;

    public bool HasMeaningfulChange(string screenshotPath, int sensitivity)
    {
        sensitivity = Math.Clamp(sensitivity, 0, 100);
        using var current = CreateSample(screenshotPath);

        lock (_sync)
        {
            if (_previousSample is null)
            {
                _previousSample = (Bitmap)current.Clone();
                return true;
            }

            var changedPercent = CalculateChangedPercent(_previousSample, current);
            _previousSample.Dispose();
            _previousSample = (Bitmap)current.Clone();

            return changedPercent >= GetThresholdPercent(sensitivity);
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _previousSample?.Dispose();
            _previousSample = null;
        }
    }

    private static Bitmap CreateSample(string path)
    {
        using var source = Image.FromFile(path);
        var sample = new Bitmap(SampleWidth, SampleHeight, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(sample);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
        graphics.DrawImage(source, 0, 0, SampleWidth, SampleHeight);
        return sample;
    }

    private static double CalculateChangedPercent(Bitmap previous, Bitmap current)
    {
        var changed = 0;
        var total = SampleWidth * SampleHeight;

        for (var y = 0; y < SampleHeight; y++)
        {
            for (var x = 0; x < SampleWidth; x++)
            {
                var a = previous.GetPixel(x, y);
                var b = current.GetPixel(x, y);
                var delta = Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                if (delta > PixelDeltaThreshold)
                {
                    changed++;
                }
            }
        }

        return changed * 100.0 / total;
    }

    private static double GetThresholdPercent(int sensitivity)
    {
        if (sensitivity <= 0)
        {
            return 0.01;
        }

        return 0.25 + (sensitivity / 100.0 * 24.75);
    }
}
