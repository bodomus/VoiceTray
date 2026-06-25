namespace VoiceTray.Infrastructure.Audio;

public interface IAudioRecorder
{
    bool IsRecording { get; }

    Task<AudioRecordingResult> StartAsync(CancellationToken cancellationToken);

    Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken);

    void DeleteOldTemporaryFiles(TimeSpan maxAge);
}
