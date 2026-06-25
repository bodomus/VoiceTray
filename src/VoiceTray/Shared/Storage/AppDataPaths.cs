using System.IO;

namespace VoiceTray.Shared.Storage;

public static class AppDataPaths
{
    public static string DefaultRecordingDirectory
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoiceTray",
            "Recordings");
}
