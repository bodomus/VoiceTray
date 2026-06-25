using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        _serviceProvider.GetRequiredService<IAudioRecorder>().DeleteOldTemporaryFiles(TimeSpan.FromDays(3));

        var window = MainWindowInstance;
        MainWindow = window;

        var trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
        trayIconService.OpenRequested += (_, _) => ShowMainWindow();
        trayIconService.StartRequested += async (_, _) => await MainViewModel.StartAsync();
        trayIconService.StopRequested += async (_, _) => await MainViewModel.StopAsync();
        trayIconService.SettingsRequested += (_, _) => System.Windows.MessageBox.Show(window, "Settings UI is not implemented yet.", "VoiceTray", MessageBoxButton.OK, MessageBoxImage.Information);
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
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

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
