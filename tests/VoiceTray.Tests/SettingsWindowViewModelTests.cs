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
