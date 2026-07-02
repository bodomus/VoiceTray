using VoiceTray.Contracts.Settings;

namespace VoiceTray.Tests;

public sealed class SettingsWindowViewModelTests
{
    [Fact]
    public async Task SaveAsync_SavesSettingsAndUpdatesHolder()
    {
        var holder = new AppSettingsHolder { Current = AppSettings.Default };
        var service = new CapturingSettingsService(holder.Current);
        var viewModel = new SettingsWindowViewModel(service, holder)
        {
            WhisperLanguage = "en",
            RecognitionTimeoutSeconds = 30,
            AutoCopyAfterRecognition = true
        };

        await viewModel.SaveAsync();

        Assert.True(viewModel.WasSaved);
        Assert.Equal("en", service.SavedSettings?.Whisper.Language);
        Assert.Equal(30, holder.Current.Cancellation.RecognitionTimeoutSeconds);
        Assert.True(holder.Current.Behavior.AutoCopyAfterRecognition);
        Assert.False(viewModel.RestartRequired);
        Assert.Equal("Settings saved.", viewModel.StatusMessage);
    }

    [Fact]
    public void ResetToDefaultsCommand_LoadsDefaultSettings()
    {
        var holder = new AppSettingsHolder
        {
            Current = AppSettings.Default with
            {
                Whisper = AppSettings.Default.Whisper with { Language = "en" },
                Cancellation = new CancellationSettings(30)
            }
        };
        var viewModel = new SettingsWindowViewModel(new CapturingSettingsService(holder.Current), holder);

        viewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Equal(AppSettings.Default.Whisper.Language, viewModel.WhisperLanguage);
        Assert.Equal(AppSettings.Default.Cancellation.RecognitionTimeoutSeconds, viewModel.RecognitionTimeoutSeconds);
    }

    [Fact]
    public async Task SaveAsync_WithTimeoutLessThanFive_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.RecognitionTimeoutSeconds = 4;

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("timeout", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithRetentionDaysLessThanOne_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.TemporaryFileRetentionDays = 0;

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("retention", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithInvalidHotKeyKey_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.HotKeyModifiers = "Control,Alt";
        viewModel.HotKeyKey = "NotAKey";

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("Invalid hotkey", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WithInvalidHotKeyModifier_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.HotKeyModifiers = "Control,Meta";
        viewModel.HotKeyKey = "Space";

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("Invalid hotkey", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyWhisperExecutablePath_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.WhisperExecutablePath = string.Empty;

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("executable", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyWhisperModelPath_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.WhisperModelPath = string.Empty;

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("model", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithAutoLanguage_DoesNotSaveAndShowsStatus()
    {
        var (viewModel, service, holder, originalSettings) = CreateViewModel();
        viewModel.WhisperLanguage = "auto";

        await viewModel.SaveAsync();

        Assert.Null(service.SavedSettings);
        Assert.Same(originalSettings, holder.Current);
        Assert.False(viewModel.WasSaved);
        Assert.Contains("auto", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithEmptyRecordingDirectory_SavesDefaultRecordingDirectoryAfterNormalization()
    {
        var (viewModel, service, _, _) = CreateViewModel();
        viewModel.RecordingDirectory = string.Empty;

        await viewModel.SaveAsync();

        Assert.NotNull(service.SavedSettings);
        Assert.False(string.IsNullOrWhiteSpace(service.SavedSettings.Storage.RecordingDirectory));
    }

    [Fact]
    public async Task SaveAsync_WithEmptyHotKeyGesture_GeneratesGestureBeforeSave()
    {
        var (viewModel, service, _, _) = CreateViewModel();
        viewModel.HotKeyGesture = string.Empty;
        viewModel.HotKeyModifiers = "Control,Alt";
        viewModel.HotKeyKey = "Space";

        await viewModel.SaveAsync();

        Assert.Equal("Ctrl+Alt+Space", service.SavedSettings?.HotKey.Gesture);
    }

    private static (
        SettingsWindowViewModel ViewModel,
        CapturingSettingsService Service,
        AppSettingsHolder Holder,
        AppSettings OriginalSettings) CreateViewModel()
    {
        var originalSettings = AppSettings.Default;
        var holder = new AppSettingsHolder { Current = originalSettings };
        var service = new CapturingSettingsService(originalSettings);
        var viewModel = new SettingsWindowViewModel(service, holder);
        return (viewModel, service, holder, originalSettings);
    }

    private sealed class CapturingSettingsService(AppSettings loadedSettings) : ISettingsService
    {
        public AppSettings? SavedSettings { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(loadedSettings);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            SavedSettings = settings;
            return Task.CompletedTask;
        }
    }
}
