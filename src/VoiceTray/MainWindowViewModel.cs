using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoiceTray.Application.Dictation;
using VoiceTray.Contracts.Clipboard;

namespace VoiceTray;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IDictationWorkflowService _dictationWorkflow;
    private readonly IClipboardService _clipboardService;
    private readonly ITextPasteService _textPasteService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private CancellationTokenSource? _operationCancellationTokenSource;
    private Task? _currentOperationTask;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _isRecognizing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private string _memoText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    public MainWindowViewModel(
        IDictationWorkflowService dictationWorkflow,
        IClipboardService clipboardService,
        ITextPasteService textPasteService,
        ILogger<MainWindowViewModel> logger)
    {
        _dictationWorkflow = dictationWorkflow;
        _clipboardService = clipboardService;
        _textPasteService = textPasteService;
        _logger = logger;
    }

    public void SetStatus(string status) => StatusText = status;

    public bool TryStartFromCommand()
    {
        if (!StartCommand.CanExecute(null))
        {
            return false;
        }

        StartCommand.Execute(null);
        return true;
    }

    public bool TryStopFromCommand()
    {
        if (!StopCommand.CanExecute(null))
        {
            return false;
        }

        StopCommand.Execute(null);
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    public async Task StartAsync()
    {
        _currentOperationTask = StartCoreAsync();
        await _currentOperationTask;
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    public async Task StopAsync()
    {
        if (!IsRecording)
        {
            return;
        }

        _currentOperationTask = StopCoreAsync();
        await _currentOperationTask;
    }

    private async Task StartCoreAsync()
    {
        try
        {
            _operationCancellationTokenSource?.Dispose();
            _operationCancellationTokenSource = new CancellationTokenSource();

            await _dictationWorkflow.StartAsync(_operationCancellationTokenSource.Token);
            IsRecording = true;
            StatusText = "Recording...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording.");
            StatusText = $"Error: {ex.Message}";
        }
    }

    private async Task StopCoreAsync()
    {
        var cancellationToken = _operationCancellationTokenSource?.Token ?? CancellationToken.None;

        try
        {
            StatusText = "Recognizing...";
            IsRecording = false;
            IsRecognizing = true;
            var result = await _dictationWorkflow.StopAndRecognizeAsync(MemoText, cancellationToken);
            IsRecognizing = false;

            if (!string.IsNullOrWhiteSpace(result.RecognizedText))
            {
                MemoText = result.RecognizedText;
            }

            StatusText = result.Status;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Ready";
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Recognition timed out.");
            StatusText = $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recognition failed.");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRecording = false;
            IsRecognizing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasText))]
    private void Copy()
    {
        _clipboardService.SetText(MemoText);
        StatusText = "Copied";
    }

    [RelayCommand(CanExecute = nameof(HasText))]
    private async Task PasteAsync()
    {
        try
        {
            await _textPasteService.PasteAsync(MemoText, CancellationToken.None);
            StatusText = "Pasted";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste failed.");
            _clipboardService.SetText(MemoText);
            StatusText = "Copied. Press Ctrl+V in target app.";
        }
    }

    [RelayCommand(CanExecute = nameof(HasText))]
    private void Clear()
    {
        MemoText = string.Empty;
        StatusText = "Ready";
    }

    public async Task CancelAsync()
    {
        _operationCancellationTokenSource?.Cancel();

        if (IsRecording || IsRecognizing)
        {
            await _dictationWorkflow.CancelAsync(CancellationToken.None);
        }

        if (_currentOperationTask is not null)
        {
            await _currentOperationTask;
        }
    }

    private bool CanStart() => !IsRecording && !IsRecognizing;

    private bool CanStop() => IsRecording && !IsRecognizing;

    private bool HasText() => !string.IsNullOrWhiteSpace(MemoText);

}
