using VoiceTray.Contracts.Settings;
using VoiceTray.Application.Dictation;
using VoiceTray.Shared.Storage;

namespace VoiceTray.Tests;

public sealed class StoragePathResolverTests
{
    [Fact]
    public void ResolveRecordingDirectory_UsesLocalAppDataDefault_WhenSettingIsEmpty()
    {
        var result = StoragePathResolver.ResolveRecordingDirectory(new StorageSettings(null, 3));

        Assert.Contains("VoiceTray", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recordings", result, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveRecordingDirectory_ExpandsEnvironmentVariables()
    {
        var result = StoragePathResolver.ResolveRecordingDirectory(
            new StorageSettings("%LocalAppData%\\VoiceTray\\Recordings", 3));

        Assert.DoesNotContain("%LocalAppData%", result, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AudioRecordingOptionsFactory_ResolvesStorageSettingsBeforePassingToRecorder()
    {
        var result = AudioRecordingOptionsFactory.FromSettings(
            new StorageSettings("%LocalAppData%\\VoiceTray\\Recordings", 7));

        Assert.DoesNotContain("%LocalAppData%", result.RecordingDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(7, result.TemporaryFileRetentionDays);
    }
}
