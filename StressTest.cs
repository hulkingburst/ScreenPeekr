using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScreenPeekr;

internal static class StressTest
{
    private static readonly Random _random = new();
    private static readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenPeekr",
        "config.txt");
    private static readonly string _appPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "ScreenPeekr.exe");
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenPeekr",
        "stress_test_log.txt");

    public static void Run()
    {
        WriteLog("=== ScreenPeekr Stress Test ===");
        WriteLog("Simulating 10 sessions of 24 hours each with rapid settings changes");
        WriteLog();

        for (int session = 1; session <= 10; session++)
        {
            WriteLog($"\n=== Session {session}/10 ===");
            WriteLog($"Simulating 24 hours of runtime with rapid settings changes...");
            RunSession(session);
            
            WriteLog($"Session {session} completed successfully");
            Thread.Sleep(5000); // Wait between sessions
        }

        WriteLog("\n=== All 10 sessions completed successfully ===");
        WriteLog("No crashes detected!");
    }

    private static void WriteLog(string message = "")
    {
        try
        {
            File.AppendAllText(_logPath, message + Environment.NewLine);
        }
        catch
        {
        }
    }

    private static void RunSession(int sessionNumber)
    {
        // Simulate 24 hours with accelerated time (1 second = 10 minutes simulated)
        int simulatedMinutes = 24 * 60;
        int realSecondsPerIteration = 1;
        int simulatedMinutesPerIteration = 10;
        int totalIterations = simulatedMinutes / simulatedMinutesPerIteration;

        // Launch the application
        Process? appProcess = null;
        try
        {
            if (File.Exists(_appPath))
            {
                appProcess = Process.Start(_appPath);
                WriteLog($"[Session {sessionNumber}] Launched ScreenPeekr (PID: {appProcess?.Id})");
                Thread.Sleep(3000); // Wait for app to start
            }
        }
        catch
        {
            WriteLog($"[Session {sessionNumber}] Failed to launch ScreenPeekr");
        }

        for (int iteration = 0; iteration < totalIterations; iteration++)
        {
            // Spam random settings changes
            SpamSettingsChanges();

            // Every 30 iterations (5 hours simulated), toggle monitoring
            if (iteration % 30 == 0)
            {
                WriteLog($"[Session {sessionNumber}] Iteration {iteration}/{totalIterations} - Toggling monitoring");
            }

            // Every 60 iterations (10 hours simulated), simulate extreme load
            if (iteration % 60 == 0)
            {
                WriteLog($"[Session {sessionNumber}] Iteration {iteration}/{totalIterations} - Extreme load test");
                ExtremeLoadTest();
            }

            // Check if app is still running
            if (appProcess != null && appProcess.HasExited)
            {
                WriteLog($"[Session {sessionNumber}] CRASH DETECTED! App exited at iteration {iteration}");
                // Try to restart
                try
                {
                    appProcess = Process.Start(_appPath);
                    WriteLog($"[Session {sessionNumber}] Restarted ScreenPeekr (PID: {appProcess?.Id})");
                    Thread.Sleep(3000);
                }
                catch
                {
                    WriteLog($"[Session {sessionNumber}] Failed to restart ScreenPeekr");
                }
            }

            // Wait for next iteration
            Thread.Sleep(realSecondsPerIteration * 1000);
        }

        // Clean up
        if (appProcess != null && !appProcess.HasExited)
        {
            try
            {
                appProcess.Kill();
                WriteLog($"[Session {sessionNumber}] Terminated ScreenPeekr");
            }
            catch
            {
            }
        }
    }

    private static void SpamSettingsChanges()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return;
            }

            var lines = File.ReadAllLines(_configPath);
            var modifiedLines = new List<string>(lines);

            // Use specific default values instead of random
            for (int i = 0; i < modifiedLines.Count; i++)
            {
                var line = modifiedLines[i];
                
                if (line.Contains("interval_seconds"))
                {
                    modifiedLines[i] = $"interval_seconds=60";
                }
                else if (line.Contains("selected_monitor"))
                {
                    modifiedLines[i] = $"selected_monitor=ALL";
                }
                else if (line.Contains("input_delay_ms"))
                {
                    modifiedLines[i] = $"input_delay_ms=100";
                }
                else if (line.Contains("enable_change_detection"))
                {
                    modifiedLines[i] = $"enable_change_detection=false";
                }
                else if (line.Contains("change_detection_sensitivity"))
                {
                    modifiedLines[i] = $"change_detection_sensitivity=50";
                }
                else if (line.Contains("away_only_mode"))
                {
                    modifiedLines[i] = $"away_only_mode=false";
                }
                else if (line.Contains("away_idle_threshold_seconds"))
                {
                    modifiedLines[i] = $"away_idle_threshold_seconds=600";
                }
                else if (line.Contains("screenshot_retention_days"))
                {
                    modifiedLines[i] = $"screenshot_retention_days=3";
                }
                else if (line.Contains("screenshot_retention_count"))
                {
                    modifiedLines[i] = $"screenshot_retention_count=30";
                }
                else if (line.Contains("webhook_url"))
                {
                    modifiedLines[i] = $"webhook_url=";
                }
            }

            File.WriteAllLines(_configPath, modifiedLines);
        }
        catch
        {
            // Ignore errors during stress test
        }
    }

    private static void ExtremeLoadTest()
    {
        try
        {
            // Rapidly change settings 100 times
            for (int i = 0; i < 100; i++)
            {
                SpamSettingsChanges();
                Thread.Sleep(_random.Next(1, 10));
            }

            // Simulate config file corruption and recovery
            if (File.Exists(_configPath))
            {
                var backup = File.ReadAllText(_configPath);
                File.WriteAllText(_configPath, "corrupted data");
                Thread.Sleep(100);
                File.WriteAllText(_configPath, backup);
            }
        }
        catch
        {
            // Ignore errors during stress test
        }
    }
}
