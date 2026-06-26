using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Settings;
using VoiceTray.Shared.Storage;

namespace VoiceTray.Infrastructure.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ILogger<JsonSettingsService> _logger;
    private readonly string _settingsPath;

    public JsonSettingsService(ILogger<JsonSettingsService> logger)
        : this(logger, Path.Combine(AppContext.BaseDirectory, "settings.json"))
    {
    }

    public JsonSettingsService(ILogger<JsonSettingsService> logger, string settingsPath)
    {
        _logger = logger;
        _settingsPath = settingsPath;
    }

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
            var normalized = AppSettingsNormalizer.Normalize(settings ?? AppSettings.Default);
            if (settings != normalized)
            {
                await SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
            }

            return normalized;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to load settings.json. Defaults will be used.");
            return AppSettings.Default;
        }
    }

    private async Task SaveDefaultAsync(CancellationToken cancellationToken)
        => await SaveAsync(AppSettings.Default, cancellationToken).ConfigureAwait(false);

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        settings = AppSettingsNormalizer.Normalize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
