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
        File.AppendAllLines(_logPath, new[] { $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}" });
    }

    public string ReadAll()
    {
        return File.Exists(_logPath) ? File.ReadAllText(_logPath) : string.Empty;
    }

    public void Dispose()
    {
    }
}
