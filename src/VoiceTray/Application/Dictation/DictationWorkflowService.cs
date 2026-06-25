using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Audio;
using VoiceTray.Contracts.Clipboard;
using VoiceTray.Contracts.Settings;
using VoiceTray.Contracts.Speech;

namespace VoiceTray.Application.Dictation;

public sealed class DictationWorkflowService(
    IAudioRecorder audioRecorder,
    ISpeechRecognizer speechRecognizer,
    IClipboardService clipboardService,
    ITextPasteService textPasteService,
    AppSettingsHolder settingsHolder,
    ILogger<DictationWorkflowService> logger) : IDictationWorkflowService
{
    public bool IsRecording => audioRecorder.IsRecording;

    public bool IsRecognizing { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await audioRecorder.StartAsync(settingsHolder.Current.Storage, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DictationWorkflowResult> StopAndRecognizeAsync(string currentText, CancellationToken cancellationToken)
    {
        using var recognitionTimeout = CreateRecognitionTimeout();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            recognitionTimeout.Token);

        var recordingResult = await audioRecorder.StopAsync(linkedCancellation.Token).ConfigureAwait(false);
        IsRecognizing = true;

        try
        {
            var options = SpeechRecognitionOptions.FromSettings(settingsHolder.Current.Whisper);
            var result = await speechRecognizer
                .RecognizeAsync(recordingResult.FilePath, options, linkedCancellation.Token)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                return new DictationWorkflowResult(null, "Text recognized: empty result", false, false);
            }

            var recognizedText = AppendText(currentText, result.Text.Trim());
            var status = "Text recognized";
            var wasAutoCopied = false;
            var wasAutoPasted = false;

            if (settingsHolder.Current.Behavior.AutoCopyAfterRecognition)
            {
                clipboardService.SetText(recognizedText);
                wasAutoCopied = true;
                status = "Copied";
            }

            if (settingsHolder.Current.Behavior.AutoPasteAfterRecognition)
            {
                await textPasteService.PasteAsync(recognizedText, linkedCancellation.Token).ConfigureAwait(false);
                wasAutoPasted = true;
                status = "Pasted";
            }

            return new DictationWorkflowResult(recognizedText, status, wasAutoCopied, wasAutoPasted);
        }
        catch (OperationCanceledException) when (recognitionTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Recognition timed out after {TimeoutSeconds} seconds.",
                settingsHolder.Current.Cancellation.RecognitionTimeoutSeconds);
            throw new TimeoutException($"Recognition timed out after {settingsHolder.Current.Cancellation.RecognitionTimeoutSeconds} seconds.");
        }
        finally
        {
            IsRecognizing = false;
        }
    }

    public async Task CancelAsync(CancellationToken cancellationToken)
    {
        if (audioRecorder.IsRecording)
        {
            await audioRecorder.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private CancellationTokenSource CreateRecognitionTimeout()
    {
        var timeout = TimeSpan.FromSeconds(settingsHolder.Current.Cancellation.RecognitionTimeoutSeconds);
        return new CancellationTokenSource(timeout);
    }

    private static string AppendText(string currentText, string newText)
        => string.IsNullOrWhiteSpace(currentText) ? newText : $"{currentText}{Environment.NewLine}{newText}";
}
