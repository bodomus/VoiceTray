using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceTray.Application.Dictation;
using VoiceTray.Contracts.Audio;
using VoiceTray.Contracts.Clipboard;
using VoiceTray.Contracts.HotKeys;
using VoiceTray.Contracts.Settings;
using VoiceTray.Contracts.Speech;
using VoiceTray.Contracts.Tray;
using VoiceTray.Infrastructure.Audio;
using VoiceTray.Infrastructure.Clipboard;
using VoiceTray.Infrastructure.HotKeys;
using VoiceTray.Infrastructure.Logging;
using VoiceTray.Infrastructure.Settings;
using VoiceTray.Infrastructure.Speech;
using VoiceTray.Infrastructure.Tray;

namespace VoiceTray;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    private bool _isExiting;

    private MainWindow MainWindowInstance => _serviceProvider!.GetRequiredService<MainWindow>();

    private MainWindowViewModel MainViewModel => _serviceProvider!.GetRequiredService<MainWindowViewModel>();

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("VoiceTray starting.");

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = await settingsService.LoadAsync(CancellationToken.None);
        _serviceProvider.GetRequiredService<AppSettingsHolder>().Current = settings;

        _serviceProvider
            .GetRequiredService<IAudioRecorder>()
            .DeleteOldTemporaryFiles(AudioRecordingOptionsFactory.FromSettings(settings.Storage));

        var window = MainWindowInstance;
        MainWindow = window;

        var trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
        trayIconService.OpenRequested += (_, _) => ShowMainWindow();
        trayIconService.StartRequested += (_, _) => MainViewModel.TryStartFromCommand();
        trayIconService.StopRequested += (_, _) => MainViewModel.TryStopFromCommand();
        trayIconService.SettingsRequested += (_, _) => OpenSettingsWindow();
        trayIconService.ExitRequested += async (_, _) => await ExitApplicationAsync();
        trayIconService.Initialize();

        var hotKeyService = _serviceProvider.GetRequiredService<IGlobalHotKeyService>();
        hotKeyService.HotKeyPressed += (_, _) => ShowMainWindow();
        if (!hotKeyService.Register(window, settings.HotKey))
        {
            MainViewModel.SetStatus("Hotkey registration failed: Ctrl+Alt+Space is unavailable.");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppSettingsHolder>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IAudioRecorder, NAudioRecorder>();
        services.AddSingleton<ISpeechRecognizer, WhisperCppSpeechRecognizer>();
        services.AddSingleton<IGlobalHotKeyService, WindowsGlobalHotKeyService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IClipboardService, WindowsClipboardService>();
        services.AddSingleton<ITextPasteService, WindowsTextPasteService>();
        services.AddSingleton<IDictationWorkflowService, DictationWorkflowService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<SettingsWindow>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new FileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "logs")));
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    private void ShowMainWindow()
    {
        _serviceProvider!.GetRequiredService<ITextPasteService>().CaptureTargetWindow();
        MainWindowInstance.ShowAndActivate();
    }

    private void OpenSettingsWindow()
    {
        var window = MainWindowInstance;
        var settingsWindow = _serviceProvider!.GetRequiredService<SettingsWindow>();
        settingsWindow.Owner = window;

        var saved = settingsWindow.ShowDialog() == true;
        if (!saved || settingsWindow.DataContext is not SettingsWindowViewModel viewModel)
        {
            return;
        }

        MainViewModel.SetStatus(viewModel.StatusMessage);
        if (viewModel.RestartRequired)
        {
            System.Windows.MessageBox.Show(
                window,
                "Hotkey changes will take effect after restart.",
                "VoiceTray",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async Task ExitApplicationAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        var logger = _serviceProvider!.GetRequiredService<ILogger<App>>();
        logger.LogInformation("VoiceTray exiting.");

        await MainViewModel.CancelAsync();
        MainWindowInstance.AllowClose();
        var serviceProvider = _serviceProvider ?? throw new InvalidOperationException("Service provider is not initialized.");
        serviceProvider.GetRequiredService<IGlobalHotKeyService>().Dispose();
        serviceProvider.GetRequiredService<ITrayIconService>().Dispose();

        Shutdown();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
    }
}
