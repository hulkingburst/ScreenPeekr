using ScreenPeekr.Models;

namespace ScreenPeekr.Services;

internal sealed class ScreenshotCleanupService
{
    private readonly string _directory;
    private DateTime _lastCleanup = DateTime.MinValue;

    public ScreenshotCleanupService(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public string CreatePath()
    {
        return Path.Combine(_directory, $"ScreenPeekr_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png");
    }

    public void Cleanup(AppConfig config, string? activePath = null)
    {
        if (activePath is not null && (DateTime.UtcNow - _lastCleanup) < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _lastCleanup = DateTime.UtcNow;
        var activeFullPath = string.IsNullOrWhiteSpace(activePath) ? null : Path.GetFullPath(activePath);
        var files = Directory.EnumerateFiles(_directory, "ScreenPeekr_*.png")
            .Select(path => new FileInfo(path))
            .Where(file => !string.Equals(file.FullName, activeFullPath, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ToList();

        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, config.ScreenshotRetentionDays));
        var keepCount = Math.Max(0, config.ScreenshotRetentionCount);

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (file.CreationTimeUtc >= cutoff && i < keepCount)
            {
                continue;
            }

            TryDelete(file.FullName);
        }
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
