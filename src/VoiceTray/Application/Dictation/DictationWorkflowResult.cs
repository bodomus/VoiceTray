namespace VoiceTray.Application.Dictation;

public sealed record DictationWorkflowResult(
    string? RecognizedText,
    string Status,
    bool WasAutoCopied,
    bool WasAutoPasted);
