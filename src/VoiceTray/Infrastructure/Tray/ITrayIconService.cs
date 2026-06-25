namespace VoiceTray.Infrastructure.Tray;

public interface ITrayIconService : IDisposable
{
    event EventHandler? OpenRequested;

    event EventHandler? StartRequested;

    event EventHandler? StopRequested;

    event EventHandler? SettingsRequested;

    event EventHandler? ExitRequested;

    void Initialize();
}
