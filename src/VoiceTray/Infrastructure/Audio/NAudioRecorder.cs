using System.IO;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using VoiceTray.Contracts.Audio;

namespace VoiceTray.Infrastructure.Audio;

public sealed class NAudioRecorder : IAudioRecorder
{
    private readonly object _gate = new();
    private readonly ILogger<NAudioRecorder> _logger;
    private readonly Func<IWaveInDevice> _waveInDeviceFactory;
    private IWaveInDevice? _waveIn;
    private WaveFileWriter? _writer;
    private TaskCompletionSource<StoppedEventArgs>? _recordingStopped;
    private DateTimeOffset _startedAt;
    private string? _currentFilePath;

    public NAudioRecorder(ILogger<NAudioRecorder> logger)
        : this(logger, static () => new WaveInDevice())
    {
    }

    internal NAudioRecorder(ILogger<NAudioRecorder> logger, Func<IWaveInDevice> waveInDeviceFactory)
    {
        _logger = logger;
        _waveInDeviceFactory = waveInDeviceFactory;
    }

    public bool IsRecording { get; private set; }

    public Task<AudioRecordingResult> StartAsync(AudioRecordingOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Recording is already running.");
            }

            Directory.CreateDirectory(options.RecordingDirectory);
            _currentFilePath = Path.Combine(options.RecordingDirectory, $"voicetray-{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}.wav");
            _startedAt = DateTimeOffset.Now;

            _waveIn = _waveInDeviceFactory();
            _writer = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);
            _recordingStopped = new TaskCompletionSource<StoppedEventArgs>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();

            IsRecording = true;
            _logger.LogInformation("Recording started. WAV: {FilePath}", _currentFilePath);
            return Task.FromResult(new AudioRecordingResult(_currentFilePath, TimeSpan.Zero));
        }
    }

    public async Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IWaveInDevice waveIn;
        Task<StoppedEventArgs> recordingStopped;
        string filePath;
        DateTimeOffset startedAt;

        lock (_gate)
        {
            if (!IsRecording || _waveIn is null || _currentFilePath is null)
            {
                throw new InvalidOperationException("Recording is not running.");
            }

            waveIn = _waveIn;
            recordingStopped = _recordingStopped?.Task
                ?? throw new InvalidOperationException("Recording stop signal is not initialized.");
            filePath = _currentFilePath;
            startedAt = _startedAt;
            IsRecording = false;
        }

        waveIn.StopRecording();
        var stoppedEvent = await recordingStopped.ConfigureAwait(false);
        DisposeRecordingObjects();

        if (stoppedEvent.Exception is not null)
        {
            throw new InvalidOperationException("Recording stopped with an error.", stoppedEvent.Exception);
        }

        var duration = DateTimeOffset.Now - startedAt;
        _logger.LogInformation("Recording stopped. WAV: {FilePath}", filePath);
        return new AudioRecordingResult(filePath, duration);
    }

    public void DeleteOldTemporaryFiles(AudioRecordingOptions options)
    {
        if (!Directory.Exists(options.RecordingDirectory))
        {
            return;
        }

        var threshold = DateTimeOffset.Now - TimeSpan.FromDays(options.TemporaryFileRetentionDays);
        foreach (var filePath in Directory.EnumerateFiles(options.RecordingDirectory, "*.wav"))
        {
            try
            {
                if (File.GetLastWriteTime(filePath) < threshold)
                {
                    File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file {FilePath}", filePath);
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_gate)
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
            _writer?.Flush();
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            _logger.LogError(e.Exception, "Recording stopped with an error.");
        }

        _recordingStopped?.TrySetResult(e);
    }

    private void DisposeRecordingObjects()
    {
        lock (_gate)
        {
            if (_waveIn is not null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
            }

            _writer?.Dispose();
            _waveIn = null;
            _writer = null;
            _recordingStopped = null;
            _currentFilePath = null;
        }
    }

    internal interface IWaveInDevice : IDisposable
    {
        event EventHandler<WaveInEventArgs>? DataAvailable;

        event EventHandler<StoppedEventArgs>? RecordingStopped;

        WaveFormat WaveFormat { get; }

        void StartRecording();

        void StopRecording();
    }

    private sealed class WaveInDevice : IWaveInDevice
    {
        private readonly WaveInEvent _waveIn = new()
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100
        };

        public event EventHandler<WaveInEventArgs>? DataAvailable
        {
            add => _waveIn.DataAvailable += value;
            remove => _waveIn.DataAvailable -= value;
        }

        public event EventHandler<StoppedEventArgs>? RecordingStopped
        {
            add => _waveIn.RecordingStopped += value;
            remove => _waveIn.RecordingStopped -= value;
        }

        public WaveFormat WaveFormat => _waveIn.WaveFormat;

        public void StartRecording() => _waveIn.StartRecording();

        public void StopRecording() => _waveIn.StopRecording();

        public void Dispose() => _waveIn.Dispose();
    }
}
