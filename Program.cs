using System.Windows.Forms;
using ScreenPeekr.Services;
using ScreenPeekr.Tray;

namespace ScreenPeekr;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Check for stress test mode
        if (args.Length > 0 && args[0] == "--stress-test")
        {
            StressTest.Run();
            return;
        }

        ApplicationConfiguration.Initialize();

        using var configStore = new ConfigStore();
        using var eventLog = new EventLogStore(configStore.AppDataDirectory);
        using var startup = new StartupService();
        using var monitorCatalog = new MonitorCatalog();
        var cleanup = new ScreenshotCleanupService(configStore.AppDataDirectory);
        using var capture = new ScreenshotCaptureService(cleanup);
        using var uploader = new DiscordWebhookClient();
        var changeDetector = new ScreenshotChangeDetector();

        eventLog.Record("App started");
        Application.Run(new ScreenPeekrApplicationContext(configStore, eventLog, startup, monitorCatalog, capture, uploader, changeDetector, cleanup));
        eventLog.Record("App exited");
    }
}
