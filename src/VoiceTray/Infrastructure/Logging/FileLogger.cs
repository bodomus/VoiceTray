using System.IO;
using Microsoft.Extensions.Logging;

namespace VoiceTray.Infrastructure.Logging;

public sealed class FileLogger(string logDirectory, string categoryName, object gate) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, $"voicetray-{DateTimeOffset.Now:yyyyMMdd}.log");
        var line = $"{DateTimeOffset.Now:O} [{logLevel}] {categoryName}: {formatter(state, exception)}";
        if (exception is not null)
        {
            line = $"{line}{Environment.NewLine}{exception}";
        }

        lock (gate)
        {
            File.AppendAllText(logPath, $"{line}{Environment.NewLine}");
        }
    }
}
