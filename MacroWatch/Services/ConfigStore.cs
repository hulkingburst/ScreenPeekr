using MacroWatch.Models;

namespace MacroWatch.Services;

internal sealed class ConfigStore : IDisposable
{
    private const int MinimumIntervalSeconds = 15;
    private readonly string _configPath;

    public ConfigStore()
    {
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacroWatch");
        Directory.CreateDirectory(AppDataDirectory);
        _configPath = Path.Combine(AppDataDirectory, "config.txt");
        Config = Load();
    }

    public string AppDataDirectory { get; }
    public AppConfig Config { get; private set; }

    public void Save()
    {
        Config.IntervalSeconds = Math.Max(MinimumIntervalSeconds, Config.IntervalSeconds);
        var lines = new[]
        {
            $"webhook_url={Config.WebhookUrl}",
            $"interval_seconds={Config.IntervalSeconds}",
            $"selected_monitor={Config.SelectedMonitor}",
            $"start_with_windows={Config.StartWithWindows.ToString().ToLowerInvariant()}"
        };
        File.WriteAllLines(_configPath, lines);
    }

    private AppConfig Load()
    {
        var config = new AppConfig();
        if (!File.Exists(_configPath))
        {
            SaveDefault(config);
            return config;
        }

        foreach (var line in File.ReadAllLines(_configPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator < 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            switch (key)
            {
                case "webhook_url":
                    config.WebhookUrl = value;
                    break;
                case "interval_seconds":
                    if (int.TryParse(value, out var seconds))
                    {
                        config.IntervalSeconds = Math.Max(MinimumIntervalSeconds, seconds);
                    }
                    break;
                case "selected_monitor":
                    config.SelectedMonitor = value;
                    break;
                case "start_with_windows":
                    config.StartWithWindows = bool.TryParse(value, out var enabled) && enabled;
                    break;
            }
        }

        return config;
    }

    private void SaveDefault(AppConfig config)
    {
        Config = config;
        Save();
    }

    public void Dispose() => Save();
}
