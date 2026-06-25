using VoiceTray.Contracts.Settings;

namespace VoiceTray.Shared.Storage;

public static class StoragePathResolver
{
    public static string ResolveRecordingDirectory(StorageSettings storageSettings)
    {
        if (string.IsNullOrWhiteSpace(storageSettings.RecordingDirectory))
        {
            return AppDataPaths.DefaultRecordingDirectory;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(storageSettings.RecordingDirectory);
        return string.IsNullOrWhiteSpace(expandedPath) ? AppDataPaths.DefaultRecordingDirectory : expandedPath;
    }
}
