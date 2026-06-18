using System.Windows.Forms;
using ScreenPeekr.Services;
using ScreenPeekr.Tray;

namespace ScreenPeekr;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var configStore = new ConfigStore();
        using var eventLog = new EventLogStore(configStore.AppDataDirectory);
        using var startup = new StartupService();
        using var monitorCatalog = new MonitorCatalog();
        using var capture = new ScreenshotCaptureService();
        using var uploader = new DiscordWebhookClient();

        eventLog.Record("App started");
        Application.Run(new ScreenPeekrApplicationContext(configStore, eventLog, startup, monitorCatalog, capture, uploader));
        eventLog.Record("App exited");
    }
}
