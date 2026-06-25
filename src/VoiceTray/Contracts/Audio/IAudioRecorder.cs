using VoiceTray.Contracts.Settings;

namespace VoiceTray.Contracts.Audio;

public interface IAudioRecorder
{
    bool IsRecording { get; }

    Task<AudioRecordingResult> StartAsync(StorageSettings storageSettings, CancellationToken cancellationToken);

    Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken);

    void DeleteOldTemporaryFiles(StorageSettings storageSettings);
}
