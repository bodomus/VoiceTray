using System.IO;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using VoiceTray.Contracts.Audio;

namespace VoiceTray.Infrastructure.Audio;

public sealed class NAudioRecorder(ILogger<NAudioRecorder> logger) : IAudioRecorder
{
    private readonly object _gate = new();
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private DateTimeOffset _startedAt;
    private string? _currentFilePath;

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

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };
            _writer = new WaveFileWriter(_currentFilePath, _waveIn.WaveFormat);

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();

            IsRecording = true;
            logger.LogInformation("Recording started. WAV: {FilePath}", _currentFilePath);
            return Task.FromResult(new AudioRecordingResult(_currentFilePath, TimeSpan.Zero));
        }
    }

    public Task<AudioRecordingResult> StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WaveInEvent waveIn;
        string filePath;
        DateTimeOffset startedAt;

        lock (_gate)
        {
            if (!IsRecording || _waveIn is null || _currentFilePath is null)
            {
                throw new InvalidOperationException("Recording is not running.");
            }

            waveIn = _waveIn;
            filePath = _currentFilePath;
            startedAt = _startedAt;
            IsRecording = false;
        }

        waveIn.StopRecording();
        DisposeRecordingObjects();

        var duration = DateTimeOffset.Now - startedAt;
        logger.LogInformation("Recording stopped. WAV: {FilePath}", filePath);
        return Task.FromResult(new AudioRecordingResult(filePath, duration));
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
                logger.LogWarning(ex, "Failed to delete temporary file {FilePath}", filePath);
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
            logger.LogError(e.Exception, "Recording stopped with an error.");
        }
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
            _currentFilePath = null;
        }
    }
}
