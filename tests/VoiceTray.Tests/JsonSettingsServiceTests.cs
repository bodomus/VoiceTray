using Microsoft.Extensions.Logging.Abstractions;
using VoiceTray.Contracts.Settings;
using VoiceTray.Infrastructure.Settings;

namespace VoiceTray.Tests;

public sealed class JsonSettingsServiceTests
{
    [Fact]
    public async Task SaveAsync_WritesSettings_LoadAsyncReadsSavedSettings()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}", "settings.json");
        var service = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, settingsPath);
        var settings = AppSettings.Default with
        {
            Whisper = AppSettings.Default.Whisper with { Language = "en" },
            Storage = AppSettings.Default.Storage with
            {
                RecordingDirectory = Path.Combine(Path.GetTempPath(), "VoiceTrayTests"),
                TemporaryFileRetentionDays = 9
            },
            Cancellation = new CancellationSettings(45),
            Behavior = new BehaviorSettings(true, true, true)
        };

        await service.SaveAsync(settings, CancellationToken.None);

        var loaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal("en", loaded.Whisper.Language);
        Assert.Equal(9, loaded.Storage.TemporaryFileRetentionDays);
        Assert.Equal(45, loaded.Cancellation.RecognitionTimeoutSeconds);
        Assert.True(loaded.Behavior.AutoCopyAfterRecognition);
        Assert.True(loaded.Behavior.AutoPasteAfterRecognition);
        Assert.True(loaded.Behavior.HideWindowOnPaste);
    }
}
