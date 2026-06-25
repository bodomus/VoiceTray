using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Settings;

namespace VoiceTray.Infrastructure.Settings;

public sealed class JsonSettingsService(ILogger<JsonSettingsService> logger) : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            await SaveDefaultAsync(cancellationToken).ConfigureAwait(false);
            return AppSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            var normalized = Normalize(settings ?? AppSettings.Default);
            if (settings != normalized)
            {
                await SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
            }

            return normalized;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Failed to load settings.json. Defaults will be used.");
            return AppSettings.Default;
        }
    }

    private async Task SaveDefaultAsync(CancellationToken cancellationToken)
        => await SaveAsync(AppSettings.Default, cancellationToken).ConfigureAwait(false);

    private async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var normalized = settings;

        if (normalized.HotKey is null)
        {
            normalized = normalized with { HotKey = AppSettings.Default.HotKey };
        }

        if (normalized.Whisper is null)
        {
            normalized = normalized with { Whisper = AppSettings.Default.Whisper };
        }

        if (normalized.Storage is null)
        {
            normalized = normalized with { Storage = AppSettings.Default.Storage };
        }

        if (normalized.Cancellation is null)
        {
            normalized = normalized with { Cancellation = AppSettings.Default.Cancellation };
        }

        if (normalized.Behavior is null)
        {
            normalized = normalized with { Behavior = AppSettings.Default.Behavior };
        }

        if (string.Equals(normalized.Whisper.Language, "auto", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(normalized.Whisper.Language))
        {
            normalized = normalized with
            {
                Whisper = normalized.Whisper with { Language = AppSettings.Default.Whisper.Language }
            };
        }

        if (string.IsNullOrWhiteSpace(normalized.Storage.RecordingDirectory)
            || normalized.Storage.TemporaryFileRetentionDays <= 0)
        {
            normalized = normalized with { Storage = AppSettings.Default.Storage };
        }

        if (normalized.Cancellation.RecognitionTimeoutSeconds <= 0)
        {
            normalized = normalized with { Cancellation = AppSettings.Default.Cancellation };
        }

        if (string.IsNullOrWhiteSpace(normalized.HotKey.Key)
            || string.IsNullOrWhiteSpace(normalized.HotKey.Modifiers))
        {
            normalized = normalized with { HotKey = AppSettings.Default.HotKey };
        }

        return normalized;
    }
}
