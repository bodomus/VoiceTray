using Microsoft.Extensions.Logging.Abstractions;
using VoiceTray.Application.Dictation;
using VoiceTray.Contracts.Clipboard;

namespace VoiceTray.Tests;

public sealed class MainWindowViewModelLifecycleTests
{
    [Fact]
    public async Task CancelAsync_WaitsForActiveRecognitionOperationToFinish()
    {
        var workflow = new BlockingDictationWorkflow();
        var viewModel = new MainWindowViewModel(
            workflow,
            new NoopClipboardService(),
            new NoopTextPasteService(),
            NullLogger<MainWindowViewModel>.Instance);

        await viewModel.StartAsync();
        var stopTask = viewModel.StopAsync();
        await workflow.RecognitionStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var cancelTask = viewModel.CancelAsync();

        Assert.False(cancelTask.IsCompleted);
        Assert.True(workflow.CancelCalled);

        workflow.ReleaseRecognition();
        await cancelTask.WaitAsync(TimeSpan.FromSeconds(2));
        await stopTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(viewModel.IsRecording);
        Assert.False(viewModel.IsRecognizing);
    }

    private sealed class BlockingDictationWorkflow : IDictationWorkflowService
    {
        private readonly TaskCompletionSource _releaseRecognition = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource RecognitionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CancelCalled { get; private set; }

        public bool IsRecording { get; private set; }

        public bool IsRecognizing { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            IsRecording = true;
            return Task.CompletedTask;
        }

        public async Task<DictationWorkflowResult> StopAndRecognizeAsync(string currentText, CancellationToken cancellationToken)
        {
            IsRecording = false;
            IsRecognizing = true;
            RecognitionStarted.SetResult();

            while (!_releaseRecognition.Task.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await _releaseRecognition.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Delay(10, CancellationToken.None);
            }

            IsRecognizing = false;
            return new DictationWorkflowResult("recognized", "Text recognized", false, false);
        }

        public Task CancelAsync(CancellationToken cancellationToken)
        {
            CancelCalled = true;
            return Task.CompletedTask;
        }

        public void ReleaseRecognition() => _releaseRecognition.SetResult();
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
