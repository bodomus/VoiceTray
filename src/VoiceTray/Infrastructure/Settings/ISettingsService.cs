namespace VoiceTray.Infrastructure.Settings;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
}
