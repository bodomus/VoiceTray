using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceTray.Contracts.Settings;
using VoiceTray.Infrastructure.Settings;

namespace VoiceTray;

public sealed partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettingsHolder _settingsHolder;
    private readonly AppSettings _originalSettings;

    [ObservableProperty]
    private string _hotKeyGesture = string.Empty;

    [ObservableProperty]
    private string _hotKeyModifiers = string.Empty;

    [ObservableProperty]
    private string _hotKeyKey = string.Empty;

    [ObservableProperty]
    private bool _hotKeyNoRepeat;

    [ObservableProperty]
    private string _whisperExecutablePath = string.Empty;

    [ObservableProperty]
    private string _whisperModelPath = string.Empty;

    [ObservableProperty]
    private string _whisperLanguage = string.Empty;

    [ObservableProperty]
    private string _whisperExtraArguments = string.Empty;

    [ObservableProperty]
    private string _recordingDirectory = string.Empty;

    [ObservableProperty]
    private int _temporaryFileRetentionDays;

    [ObservableProperty]
    private int _recognitionTimeoutSeconds;

    [ObservableProperty]
    private bool _autoCopyAfterRecognition;

    [ObservableProperty]
    private bool _autoPasteAfterRecognition;

    [ObservableProperty]
    private bool _hideWindowOnPaste;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsWindowViewModel(ISettingsService settingsService, AppSettingsHolder settingsHolder)
    {
        _settingsService = settingsService;
        _settingsHolder = settingsHolder;
        _originalSettings = settingsHolder.Current;
        LoadFromSettings(_originalSettings);
    }

    public event EventHandler<bool?>? CloseRequested;

    public bool WasSaved { get; private set; }

    public bool RestartRequired { get; private set; }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var updatedSettings = AppSettingsNormalizer.Normalize(ToSettings());
        await _settingsService.SaveAsync(updatedSettings, CancellationToken.None);
        _settingsHolder.Current = updatedSettings;

        RestartRequired = _originalSettings.HotKey != updatedSettings.HotKey;
        WasSaved = true;
        StatusMessage = RestartRequired
            ? "Settings saved. Hotkey changes will take effect after restart."
            : "Settings saved.";
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        LoadFromSettings(AppSettings.Default);
        StatusMessage = "Default settings loaded. Press Save to apply.";
    }

    [RelayCommand]
    private void Cancel()
        => CloseRequested?.Invoke(this, false);

    private void LoadFromSettings(AppSettings settings)
    {
        HotKeyGesture = settings.HotKey.Gesture;
        HotKeyModifiers = settings.HotKey.Modifiers;
        HotKeyKey = settings.HotKey.Key;
        HotKeyNoRepeat = settings.HotKey.NoRepeat;

        WhisperExecutablePath = settings.Whisper.ExecutablePath;
        WhisperModelPath = settings.Whisper.ModelPath;
        WhisperLanguage = settings.Whisper.Language;
        WhisperExtraArguments = settings.Whisper.ExtraArguments;

        RecordingDirectory = settings.Storage.RecordingDirectory ?? string.Empty;
        TemporaryFileRetentionDays = settings.Storage.TemporaryFileRetentionDays;
        RecognitionTimeoutSeconds = settings.Cancellation.RecognitionTimeoutSeconds;

        AutoCopyAfterRecognition = settings.Behavior.AutoCopyAfterRecognition;
        AutoPasteAfterRecognition = settings.Behavior.AutoPasteAfterRecognition;
        HideWindowOnPaste = settings.Behavior.HideWindowOnPaste;
    }

    private AppSettings ToSettings()
        => new(
            new HotKeySettings(HotKeyGesture, HotKeyModifiers, HotKeyKey, HotKeyNoRepeat),
            new WhisperSettings(
                WhisperExecutablePath,
                WhisperModelPath,
                WhisperLanguage,
                WhisperExtraArguments),
            new StorageSettings(
                string.IsNullOrWhiteSpace(RecordingDirectory) ? null : RecordingDirectory,
                TemporaryFileRetentionDays),
            new CancellationSettings(RecognitionTimeoutSeconds),
            new BehaviorSettings(
                AutoCopyAfterRecognition,
                AutoPasteAfterRecognition,
                HideWindowOnPaste));
}
