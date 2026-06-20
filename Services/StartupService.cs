using Microsoft.Win32;

namespace ScreenPeekr.Services;

internal sealed class StartupService : IDisposable
{
    private const string AppName = "ScreenPeekr";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void SetStartWithWindows(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
    }
}
