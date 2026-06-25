using VoiceTray.Contracts.Settings;
using VoiceTray.Infrastructure.Settings;
using VoiceTray.Shared.Storage;

namespace VoiceTray.Tests;

public sealed class AppSettingsNormalizerTests
{
    [Fact]
    public void Normalize_ReplacesLegacyARecordingPath_WithLocalAppDataPath()
    {
        var settings = AppSettings.Default with
        {
            Storage = new StorageSettings(@"A:\VoiceTray\Recordings", 3)
        };

        var normalized = AppSettingsNormalizer.Normalize(settings);

        Assert.Equal(AppDataPaths.DefaultRecordingDirectory, normalized.Storage.RecordingDirectory);
    }
}
