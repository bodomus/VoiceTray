using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using VoiceTray.Contracts.Audio;
using VoiceTray.Infrastructure.Audio;

namespace VoiceTray.Tests;

public sealed class NAudioRecorderTests
{
    [Fact]
    public async Task StopAsync_WaitsUntilRecordingStopped_BeforeDisposingDevice()
    {
        var device = new ControlledWaveInDevice();
        var recorder = new NAudioRecorder(
            NullLogger<NAudioRecorder>.Instance,
            () => device);
        var recordingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        await recorder.StartAsync(new AudioRecordingOptions(recordingDirectory, 1), CancellationToken.None);
        var stopTask = recorder.StopAsync(CancellationToken.None);

        await Task.Delay(100);

        Assert.False(stopTask.IsCompleted);
        Assert.False(device.IsDisposed);

        device.RaiseRecordingStopped();
        var result = await stopTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(device.StopRecordingCalled);
        Assert.True(device.IsDisposed);
        Assert.True(File.Exists(result.FilePath));
    }

    private sealed class ControlledWaveInDevice : NAudioRecorder.IWaveInDevice
    {
        public event EventHandler<WaveInEventArgs>? DataAvailable
        {
            add { }
            remove { }
        }

        public event EventHandler<StoppedEventArgs>? RecordingStopped;

        public WaveFormat WaveFormat { get; } = new(16000, 16, 1);

        public bool StopRecordingCalled { get; private set; }

        public bool IsDisposed { get; private set; }

        public void StartRecording()
        {
        }

        public void StopRecording() => StopRecordingCalled = true;

        public void RaiseRecordingStopped()
            => RecordingStopped?.Invoke(this, new StoppedEventArgs());

        public void Dispose() => IsDisposed = true;
    }
}
