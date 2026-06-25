using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using VoiceTray.Contracts.Speech;

namespace VoiceTray.Infrastructure.Speech;

public sealed class WhisperCppSpeechRecognizer(ILogger<WhisperCppSpeechRecognizer> logger) : ISpeechRecognizer
{
    public async Task<SpeechRecognitionResult> RecognizeAsync(
        string audioFilePath,
        SpeechRecognitionOptions options,
        CancellationToken cancellationToken)
    {
        var executablePath = ResolvePath(options.ExecutablePath);
        var modelPath = ResolvePath(options.ModelPath);

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("whisper.cpp executable was not found.", executablePath);
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Whisper model file was not found.", modelPath);
        }

        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException("Audio file was not found.", audioFilePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add(modelPath);
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(audioFilePath);
        startInfo.ArgumentList.Add("-otxt");

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add(options.Language);
        }

        foreach (var argument in CommandLineArgumentSplitter.Split(options.ExtraArguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        logger.LogInformation("Starting whisper.cpp: {ExecutablePath}", executablePath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start whisper.cpp process.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var standardOutput = await outputTask.ConfigureAwait(false);
        var standardError = await errorTask.ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            logger.LogWarning("whisper.cpp stderr: {StandardError}", standardError);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"whisper.cpp exited with code {process.ExitCode}: {standardError}");
        }

        var textFromFile = await TryReadOutputTextFileAsync(audioFilePath, cancellationToken).ConfigureAwait(false);
        var text = !string.IsNullOrWhiteSpace(textFromFile) ? textFromFile : standardOutput;

        return new SpeechRecognitionResult(CleanWhisperText(text), standardError);
    }

    private static string ResolvePath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static async Task<string?> TryReadOutputTextFileAsync(string audioFilePath, CancellationToken cancellationToken)
    {
        var textPath = $"{audioFilePath}.txt";
        if (!File.Exists(textPath))
        {
            textPath = Path.ChangeExtension(audioFilePath, ".txt");
        }

        return File.Exists(textPath)
            ? await File.ReadAllTextAsync(textPath, cancellationToken).ConfigureAwait(false)
            : null;
    }

    private static string CleanWhisperText(string text)
    {
        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("whisper_", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }
}
