namespace VoiceTray.Contracts.Audio;

public interface IAudioRecorder
{
    bool IsRecording { get; }

    Task<AudioRecordingResult> StartAsync(AudioRecordingOptions options, CancellationToken cancellationToken);

    Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken);

    void DeleteOldTemporaryFiles(AudioRecordingOptions options);
}
