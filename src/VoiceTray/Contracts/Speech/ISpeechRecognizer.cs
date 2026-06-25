namespace VoiceTray.Contracts.Speech;

public interface ISpeechRecognizer
{
    Task<SpeechRecognitionResult> RecognizeAsync(
        string audioFilePath,
        SpeechRecognitionOptions options,
        CancellationToken cancellationToken);
}
