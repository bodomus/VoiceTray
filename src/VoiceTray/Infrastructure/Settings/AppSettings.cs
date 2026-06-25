namespace VoiceTray.Infrastructure.Settings;

public sealed record AppSettings(
    HotKeySettings HotKey,
    WhisperSettings Whisper,
    BehaviorSettings Behavior)
{
    public static AppSettings Default { get; } = new(
        new HotKeySettings("Ctrl+Alt+Space"),
        new WhisperSettings(
            "tools/whisper/whisper-cli.exe",
            "models/ggml-base.bin",
            "ru",
            string.Empty),
        new BehaviorSettings(false, false, false));
}

public sealed record HotKeySettings(string Gesture);

public sealed record WhisperSettings(
    string ExecutablePath,
    string ModelPath,
    string Language,
    string ExtraArguments);

public sealed record BehaviorSettings(
    bool AutoCopyAfterRecognition,
    bool AutoPasteAfterRecognition,
    bool HideWindowOnPaste);

public sealed class AppSettingsHolder
{
    public AppSettings Current { get; set; } = AppSettings.Default;
}
