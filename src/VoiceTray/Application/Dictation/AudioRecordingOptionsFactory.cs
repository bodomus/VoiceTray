using VoiceTray.Contracts.Audio;
using VoiceTray.Contracts.Settings;
using VoiceTray.Shared.Storage;

namespace VoiceTray.Application.Dictation;

public static class AudioRecordingOptionsFactory
{
    public static AudioRecordingOptions FromSettings(StorageSettings settings)
        => new(
            StoragePathResolver.ResolveRecordingDirectory(settings),
            settings.TemporaryFileRetentionDays);
}
