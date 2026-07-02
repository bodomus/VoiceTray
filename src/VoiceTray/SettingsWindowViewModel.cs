using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceTray.Contracts.Settings;
using VoiceTray.Infrastructure.HotKeys;
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
        var validationResult = Validate();
        if (!validationResult.IsValid)
        {
            StatusMessage = validationResult.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(HotKeyGesture))
        {
            HotKeyGesture = CreateGesturePreview(HotKeyModifiers, HotKeyKey);
        }

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

    internal SettingsValidationResult Validate()
    {
        var hotKey = new HotKeySettings(
            HotKeyGesture,
            HotKeyModifiers,
            HotKeyKey,
            HotKeyNoRepeat);
        var hotKeyResult = HotKeyConfigurationParser.TryParse(hotKey);
        if (!hotKeyResult.IsValid)
        {
            return SettingsValidationResult.Invalid($"Invalid hotkey: {hotKeyResult.ErrorMessage}");
        }

        if (string.IsNullOrWhiteSpace(WhisperExecutablePath))
        {
            return SettingsValidationResult.Invalid("Invalid whisper settings: executable path must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(WhisperModelPath))
        {
            return SettingsValidationResult.Invalid("Invalid whisper settings: model path must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(WhisperLanguage))
        {
            return SettingsValidationResult.Invalid("Invalid whisper settings: language must not be empty.");
        }

        if (string.Equals(WhisperLanguage, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return SettingsValidationResult.Invalid("Invalid whisper settings: language auto is not supported in Settings. Use ru, en, or uk.");
        }

        if (!IsAllowedLanguage(WhisperLanguage))
        {
            return SettingsValidationResult.Invalid("Invalid whisper settings: language must be ru, en, or uk.");
        }

        if (TemporaryFileRetentionDays <= 0)
        {
            return SettingsValidationResult.Invalid("Invalid storage settings: temporary file retention days must be greater than 0.");
        }

        if (RecognitionTimeoutSeconds < 5)
        {
            return SettingsValidationResult.Invalid("Invalid cancellation settings: recognition timeout must be at least 5 seconds.");
        }

        return SettingsValidationResult.Valid();
    }

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

    private static bool IsAllowedLanguage(string language)
        => string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "uk", StringComparison.OrdinalIgnoreCase);

    private static string CreateGesturePreview(string modifiers, string key)
        => string.Join(
            '+',
            modifiers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static modifier => string.Equals(modifier, "Control", StringComparison.OrdinalIgnoreCase)
                    ? "Ctrl"
                    : modifier))
            + $"+{key.Trim()}";
}

internal sealed record SettingsValidationResult(bool IsValid, string Message)
{
    public static SettingsValidationResult Valid()
        => new(true, string.Empty);

    public static SettingsValidationResult Invalid(string message)
        => new(false, message);
}
