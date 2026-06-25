namespace VoiceTray.Contracts.Audio;

public sealed record AudioRecordingOptions(
    string RecordingDirectory,
    int TemporaryFileRetentionDays);
