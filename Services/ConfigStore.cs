using System.Windows.Forms;
using ScreenPeekr.Models;

namespace ScreenPeekr.Services;

internal sealed class ConfigStore : IDisposable
{
    private const int MinimumIntervalSeconds = 5;
    private readonly string _configPath;
    private readonly Mutex _configMutex;

    public ConfigStore()
    {
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenPeekr");
        Directory.CreateDirectory(AppDataDirectory);
        _configPath = Path.Combine(AppDataDirectory, "config.txt");
        _configMutex = new Mutex(false, "ScreenPeekr_ConfigMutex");
        Config = Load();
    }

    public string AppDataDirectory { get; }
    public AppConfig Config { get; private set; }

    public void Save()
    {
        _configMutex.WaitOne();
        try
        {
            Config.IntervalSeconds = Math.Max(MinimumIntervalSeconds, Config.IntervalSeconds);
            Config.InputDelayMs = Math.Max(0, Config.InputDelayMs);
            Config.KeyHoldDurationMs = Math.Max(0, Config.KeyHoldDurationMs);
            Config.ChangeDetectionSensitivity = Math.Clamp(Config.ChangeDetectionSensitivity, 0, 100);
            Config.AwayIdleThresholdSeconds = Math.Max(1, Config.AwayIdleThresholdSeconds);
            Config.ScreenshotRetentionDays = Math.Max(0, Config.ScreenshotRetentionDays);
            Config.ScreenshotRetentionCount = Math.Max(0, Config.ScreenshotRetentionCount);
            
            // Sync PreScreenshotKeys from PreScreenshotKey for backward compatibility
            if (Config.PreScreenshotKeys.Count == 0 && Config.PreScreenshotKey != Keys.None)
            {
                Config.PreScreenshotKeys = new List<Keys> { Config.PreScreenshotKey };
            }
            else if (Config.PreScreenshotKeys.Count > 0 && Config.PreScreenshotKey == Keys.None)
            {
                Config.PreScreenshotKey = Config.PreScreenshotKeys[0];
            }
            
            var lines = new[]
            {
                $"webhook_url={Config.WebhookUrl}",
                $"interval_seconds={Config.IntervalSeconds}",
                $"selected_monitor={Config.SelectedMonitor}",
                $"start_with_windows={Config.StartWithWindows.ToString().ToLowerInvariant()}",
                $"pre_screenshot_key={Config.PreScreenshotKey}",
                $"pre_screenshot_keys={FormatKeys(Config.PreScreenshotKeys)}",
                $"post_screenshot_keys={FormatKeys(Config.PostScreenshotKeys)}",
                $"input_delay_ms={Config.InputDelayMs}",
                $"key_hold_duration_ms={Config.KeyHoldDurationMs}",
                $"enable_change_detection={Config.EnableChangeDetection.ToString().ToLowerInvariant()}",
                $"change_detection_sensitivity={Config.ChangeDetectionSensitivity}",
                $"away_only_mode={Config.AwayOnlyMode.ToString().ToLowerInvariant()}",
                $"away_idle_threshold_seconds={Config.AwayIdleThresholdSeconds}",
                $"screenshot_retention_days={Config.ScreenshotRetentionDays}",
                $"screenshot_retention_count={Config.ScreenshotRetentionCount}"
            };
            File.WriteAllLines(_configPath, lines);
        }
        finally
        {
            _configMutex.ReleaseMutex();
        }
    }

    private AppConfig Load()
    {
        _configMutex.WaitOne();
        try
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
                    case "pre_screenshot_key":
                        if (Enum.TryParse<Keys>(value, out var parsedKey))
                        {
                            config.PreScreenshotKey = parsedKey;
                        }
                        else if (int.TryParse(value, out var keyVal))
                        {
                            config.PreScreenshotKey = (Keys)keyVal;
                        }
                        break;
                    case "pre_screenshot_keys":
                        var parsedKeys = ParseKeys(value);
                        config.PreScreenshotKeys = parsedKeys;
                        // Sync PreScreenshotKey from PreScreenshotKeys for backward compatibility
                        if (parsedKeys.Count > 0)
                        {
                            config.PreScreenshotKey = parsedKeys[0];
                        }
                        break;
                    case "post_screenshot_keys":
                        config.PostScreenshotKeys = ParseKeys(value);
                        break;
                    case "input_delay_ms":
                        if (int.TryParse(value, out var delay))
                        {
                            config.InputDelayMs = Math.Max(0, delay);
                        }
                        break;
                    case "key_hold_duration_ms":
                        if (int.TryParse(value, out var hold))
                        {
                            config.KeyHoldDurationMs = Math.Max(0, hold);
                        }
                        break;
                    case "enable_change_detection":
                        config.EnableChangeDetection = bool.TryParse(value, out var enableDetection) && enableDetection;
                        break;
                    case "change_detection_sensitivity":
                        if (int.TryParse(value, out var sensitivity))
                        {
                            config.ChangeDetectionSensitivity = Math.Clamp(sensitivity, 0, 100);
                        }
                        break;
                    case "away_only_mode":
                        config.AwayOnlyMode = bool.TryParse(value, out var awayOnly) && awayOnly;
                        break;
                    case "away_idle_threshold_seconds":
                        if (int.TryParse(value, out var idleSeconds))
                        {
                            config.AwayIdleThresholdSeconds = Math.Max(1, idleSeconds);
                        }
                        break;
                    case "screenshot_retention_days":
                        if (int.TryParse(value, out var retentionDays))
                        {
                            config.ScreenshotRetentionDays = Math.Max(0, retentionDays);
                        }
                        break;
                    case "screenshot_retention_count":
                        if (int.TryParse(value, out var retentionCount))
                        {
                            config.ScreenshotRetentionCount = Math.Max(0, retentionCount);
                        }
                        break;
                }
            }

            if (config.PreScreenshotKeys.Count == 0 && config.PreScreenshotKey != Keys.None)
            {
                config.PreScreenshotKeys.Add(config.PreScreenshotKey);
            }
            else if (config.PreScreenshotKeys.Count > 0)
            {
                config.PreScreenshotKey = config.PreScreenshotKeys[0];
            }

            return config;
        }
        finally
        {
            _configMutex.ReleaseMutex();
        }
    }

    private void SaveDefault(AppConfig config)
    {
        Config = config;
        Save();
    }

    public void Dispose() => Save();

    private static string FormatKeys(IEnumerable<Keys> keys)
    {
        return string.Join(",", keys.Where(key => key != Keys.None).Select(key => key.ToString()));
    }

    private static List<Keys> ParseKeys(string value)
    {
        var keys = new List<Keys>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Keys>(part, out var parsed) && parsed != Keys.None)
            {
                keys.Add(parsed);
            }
            else if (int.TryParse(part, out var numeric) && numeric != (int)Keys.None)
            {
                keys.Add((Keys)numeric);
            }
        }

        return keys;
    }
}
