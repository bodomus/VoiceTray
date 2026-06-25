using VoiceTray.Contracts.Settings;
using VoiceTray.Shared.Storage;

namespace VoiceTray.Infrastructure.Settings;

public static class AppSettingsNormalizer
{
    public static AppSettings Normalize(AppSettings settings)
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
            || IsLegacyRecordingDirectory(normalized.Storage.RecordingDirectory))
        {
            normalized = normalized with
            {
                Storage = normalized.Storage with { RecordingDirectory = AppDataPaths.DefaultRecordingDirectory }
            };
        }
        else
        {
            normalized = normalized with
            {
                Storage = normalized.Storage with
                {
                    RecordingDirectory = Environment.ExpandEnvironmentVariables(normalized.Storage.RecordingDirectory)
                }
            };
        }

        if (normalized.Storage.TemporaryFileRetentionDays <= 0)
        {
            normalized = normalized with
            {
                Storage = normalized.Storage with
                {
                    TemporaryFileRetentionDays = AppSettings.Default.Storage.TemporaryFileRetentionDays
                }
            };
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

    private static bool IsLegacyRecordingDirectory(string recordingDirectory)
        => string.Equals(
            recordingDirectory.TrimEnd('\\'),
            @"A:\VoiceTray\Recordings",
            StringComparison.OrdinalIgnoreCase);
}
