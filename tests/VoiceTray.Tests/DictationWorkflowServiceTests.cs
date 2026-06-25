using Microsoft.Extensions.Logging.Abstractions;
using VoiceTray.Application.Dictation;
using VoiceTray.Contracts.Audio;
using VoiceTray.Contracts.Clipboard;
using VoiceTray.Contracts.Settings;
using VoiceTray.Contracts.Speech;

namespace VoiceTray.Tests;

public sealed class DictationWorkflowServiceTests
{
    [Fact]
    public async Task StopAndRecognizeAsync_ReturnsRecognizedText_ForStartStopRecognizeFlow()
    {
        var recorder = new FakeAudioRecorder();
        var recognizer = new FakeSpeechRecognizer("привет");
        var workflow = CreateWorkflow(recorder, recognizer);

        await workflow.StartAsync(CancellationToken.None);
        var result = await workflow.StopAndRecognizeAsync("старый текст", CancellationToken.None);

        Assert.Equal("старый текст\r\nпривет", result.RecognizedText);
        Assert.Equal("Text recognized", result.Status);
        Assert.False(workflow.IsRecognizing);
        Assert.False(recorder.IsRecording);
        Assert.Equal("recording.wav", recognizer.AudioFilePath);
    }

    [Fact]
    public async Task StopAndRecognizeAsync_ThrowsTimeoutException_WhenRecognitionExceedsConfiguredTimeout()
    {
        var recorder = new FakeAudioRecorder();
        var recognizer = new BlockingSpeechRecognizer();
        var workflow = CreateWorkflow(
            recorder,
            recognizer,
            AppSettings.Default with
            {
                Cancellation = new CancellationSettings(1)
            });

        await workflow.StartAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => workflow.StopAndRecognizeAsync(string.Empty, CancellationToken.None));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(recognizer.WasCancelled);
        Assert.False(workflow.IsRecognizing);
    }

    private static DictationWorkflowService CreateWorkflow(
        IAudioRecorder recorder,
        ISpeechRecognizer recognizer,
        AppSettings? settings = null)
        => new(
            recorder,
            recognizer,
            new NoopClipboardService(),
            new NoopTextPasteService(),
            new AppSettingsHolder { Current = settings ?? AppSettings.Default },
            NullLogger<DictationWorkflowService>.Instance);

    private sealed class FakeAudioRecorder : IAudioRecorder
    {
        public bool IsRecording { get; private set; }

        public Task<AudioRecordingResult> StartAsync(AudioRecordingOptions options, CancellationToken cancellationToken)
        {
            IsRecording = true;
            return Task.FromResult(new AudioRecordingResult("recording.wav", TimeSpan.Zero));
        }

        public Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken)
        {
            IsRecording = false;
            return Task.FromResult(new AudioRecordingResult("recording.wav", TimeSpan.FromSeconds(1)));
        }

        public void DeleteOldTemporaryFiles(AudioRecordingOptions options)
        {
        }
    }

    private sealed class FakeSpeechRecognizer(string text) : ISpeechRecognizer
    {
        public string? AudioFilePath { get; private set; }

        public Task<SpeechRecognitionResult> RecognizeAsync(
            string audioFilePath,
            SpeechRecognitionOptions options,
            CancellationToken cancellationToken)
        {
            AudioFilePath = audioFilePath;
            return Task.FromResult(new SpeechRecognitionResult(text, string.Empty));
        }
    }

    private sealed class BlockingSpeechRecognizer : ISpeechRecognizer
    {
        public bool WasCancelled { get; private set; }

        public async Task<SpeechRecognitionResult> RecognizeAsync(
            string audioFilePath,
            SpeechRecognitionOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Unexpected recognition completion.");
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                throw;
            }
        }
    }

    private sealed class NoopClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
        }

        public string? GetText() => null;
    }

    private sealed class NoopTextPasteService : ITextPasteService
    {
        public void CaptureTargetWindow()
        {
        }

        public Task PasteAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
