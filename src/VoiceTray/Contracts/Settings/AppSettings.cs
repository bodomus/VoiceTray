namespace VoiceTray.Contracts.Settings;

public sealed record AppSettings(
    HotKeySettings HotKey,
    WhisperSettings Whisper,
    StorageSettings Storage,
    CancellationSettings Cancellation,
    BehaviorSettings Behavior)
{
    public static AppSettings Default { get; } = new(
        new HotKeySettings("Ctrl+Alt+Space", "Control,Alt", "Space", true),
        new WhisperSettings(
            "tools/whisper/whisper-cli.exe",
            "models/ggml-base.bin",
            "ru",
            string.Empty),
        new StorageSettings(@"A:\VoiceTray\Recordings", 3),
        new CancellationSettings(120),
        new BehaviorSettings(false, false, false));
}

public sealed record HotKeySettings(
    string Gesture,
    string Modifiers,
    string Key,
    bool NoRepeat);

public sealed record WhisperSettings(
    string ExecutablePath,
    string ModelPath,
    string Language,
    string ExtraArguments);

public sealed record StorageSettings(
    string RecordingDirectory,
    int TemporaryFileRetentionDays);

public sealed record CancellationSettings(
    int RecognitionTimeoutSeconds);

public sealed record BehaviorSettings(
    bool AutoCopyAfterRecognition,
    bool AutoPasteAfterRecognition,
    bool HideWindowOnPaste);

public sealed class AppSettingsHolder
{
    public AppSettings Current { get; set; } = AppSettings.Default;
}
