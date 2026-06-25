using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;

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
            return Normalize(settings ?? AppSettings.Default);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Failed to load settings.json. Defaults will be used.");
            return AppSettings.Default;
        }
    }

    private async Task SaveDefaultAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, AppSettings.Default, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        if (string.Equals(settings.Whisper.Language, "auto", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(settings.Whisper.Language))
        {
            return settings with
            {
                Whisper = settings.Whisper with { Language = AppSettings.Default.Whisper.Language }
            };
        }

        return settings;
    }
}
