using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VoiceTray.Infrastructure.Audio;
using VoiceTray.Infrastructure.Clipboard;
using VoiceTray.Infrastructure.Settings;
using VoiceTray.Infrastructure.Speech;

namespace VoiceTray;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IAudioRecorder _audioRecorder;
    private readonly ISpeechRecognizer _speechRecognizer;
    private readonly IClipboardService _clipboardService;
    private readonly ITextPasteService _textPasteService;
    private readonly AppSettingsHolder _settingsHolder;
    private readonly ILogger<MainWindowViewModel> _logger;
    private CancellationTokenSource? _operationCancellationTokenSource;

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
        IAudioRecorder audioRecorder,
        ISpeechRecognizer speechRecognizer,
        IClipboardService clipboardService,
        ITextPasteService textPasteService,
        AppSettingsHolder settingsHolder,
        ILogger<MainWindowViewModel> logger)
    {
        _audioRecorder = audioRecorder;
        _speechRecognizer = speechRecognizer;
        _clipboardService = clipboardService;
        _textPasteService = textPasteService;
        _settingsHolder = settingsHolder;
        _logger = logger;
    }

    public void SetStatus(string status) => StatusText = status;

    [RelayCommand(CanExecute = nameof(CanStart))]
    public async Task StartAsync()
    {
        try
        {
            _operationCancellationTokenSource?.Dispose();
            _operationCancellationTokenSource = new CancellationTokenSource();

            await _audioRecorder.StartAsync(_operationCancellationTokenSource.Token);
            IsRecording = true;
            StatusText = "Recording...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording.");
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    public async Task StopAsync()
    {
        if (!IsRecording)
        {
            return;
        }

        var cancellationToken = _operationCancellationTokenSource?.Token ?? CancellationToken.None;

        try
        {
            StatusText = "Recognizing...";
            var recordingResult = await _audioRecorder.StopAsync(cancellationToken);
            IsRecording = false;
            IsRecognizing = true;

            var options = SpeechRecognitionOptions.FromSettings(_settingsHolder.Current.Whisper);
            var result = await _speechRecognizer.RecognizeAsync(recordingResult.FilePath, options, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                MemoText = AppendText(MemoText, result.Text.Trim());
                StatusText = "Text recognized";

                if (_settingsHolder.Current.Behavior.AutoCopyAfterRecognition)
                {
                    _clipboardService.SetText(MemoText);
                    StatusText = "Copied";
                }

                if (_settingsHolder.Current.Behavior.AutoPasteAfterRecognition)
                {
                    await _textPasteService.PasteAsync(MemoText, cancellationToken);
                    StatusText = "Pasted";
                }
            }
            else
            {
                StatusText = "Text recognized: empty result";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Ready";
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
        if (IsRecording)
        {
            await _audioRecorder.StopAsync(CancellationToken.None);
        }
    }

    private bool CanStart() => !IsRecording && !IsRecognizing;

    private bool CanStop() => IsRecording;

    private bool HasText() => !string.IsNullOrWhiteSpace(MemoText);

    private static string AppendText(string currentText, string newText)
        => string.IsNullOrWhiteSpace(currentText) ? newText : $"{currentText}{Environment.NewLine}{newText}";
}
