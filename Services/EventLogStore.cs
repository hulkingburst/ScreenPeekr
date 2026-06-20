namespace ScreenPeekr.Services;

internal sealed class EventLogStore : IDisposable
{
    private readonly string _logPath;

    public EventLogStore(string appDataDirectory)
    {
        _logPath = Path.Combine(appDataDirectory, "log.txt");
    }

    public void Record(string message)
    {
        try
        {
            File.AppendAllLines(_logPath, new[] { $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}" });
        }
        catch
        {
        }
    }

    public string ReadAll()
    {
        try
        {
            return File.Exists(_logPath) ? File.ReadAllText(_logPath) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
    }
}
