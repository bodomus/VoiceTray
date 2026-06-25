using VoiceTray.Contracts.Settings;

namespace VoiceTray.Contracts.Speech;

public sealed record SpeechRecognitionOptions(
    string ExecutablePath,
    string ModelPath,
    string Language,
    string ExtraArguments)
{
    public static SpeechRecognitionOptions FromSettings(WhisperSettings settings)
        => new(settings.ExecutablePath, settings.ModelPath, settings.Language, settings.ExtraArguments);
}
