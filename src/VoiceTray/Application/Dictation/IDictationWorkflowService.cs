namespace VoiceTray.Application.Dictation;

public interface IDictationWorkflowService
{
    bool IsRecording { get; }

    bool IsRecognizing { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task<DictationWorkflowResult> StopAndRecognizeAsync(string currentText, CancellationToken cancellationToken);

    Task CancelAsync(CancellationToken cancellationToken);
}
