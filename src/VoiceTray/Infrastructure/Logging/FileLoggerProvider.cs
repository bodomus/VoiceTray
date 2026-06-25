using Microsoft.Extensions.Logging;

namespace VoiceTray.Infrastructure.Logging;

public sealed class FileLoggerProvider(string logDirectory) : ILoggerProvider
{
    private readonly object _gate = new();

    public ILogger CreateLogger(string categoryName) => new FileLogger(logDirectory, categoryName, _gate);

    public void Dispose()
    {
    }
}
