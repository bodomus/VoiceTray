# VoiceTray

VoiceTray is a small local Windows dictation utility. It lives in the system tray, records microphone audio to WAV, sends the WAV file to a local whisper.cpp executable, and puts recognized text into a simple editable memo field.

## Requirements

- Windows
- .NET 9 SDK
- whisper.cpp executable
- Whisper model file

## Run From Source

```bash
dotnet restore
dotnet build
dotnet run --project src/VoiceTray/VoiceTray.csproj
```

## Settings

On first launch the app creates `settings.json` near `VoiceTray.exe`. A reference file is also included as `settings.example.json`.

```json
{
  "HotKey": {
    "Gesture": "Ctrl+Alt+Space",
    "Modifiers": "Control,Alt",
    "Key": "Space",
    "NoRepeat": true
  },
  "Whisper": {
    "ExecutablePath": "tools/whisper/whisper-cli.exe",
    "ModelPath": "models/ggml-base.bin",
    "Language": "ru",
    "ExtraArguments": ""
  },
  "Storage": {
    "RecordingDirectory": "A:\\VoiceTray\\Recordings",
    "TemporaryFileRetentionDays": 3
  },
  "Cancellation": {
    "RecognitionTimeoutSeconds": 120
  },
  "Behavior": {
    "AutoCopyAfterRecognition": false,
    "AutoPasteAfterRecognition": false,
    "HideWindowOnPaste": false
  }
}
```

Relative paths are resolved from the application directory.

`Language` defaults to `ru` so Russian dictation is recognized as Cyrillic text. Older `settings.json` files with `Language` set to `auto` are treated as `ru` by the app.

`Storage` controls where WAV recordings are written and how many days temporary files are retained. `Cancellation` controls the recognition timeout. `HotKey` exposes the gesture, modifier list, key, and `MOD_NOREPEAT` behavior explicitly.

## Architecture

VoiceTray keeps contracts, application workflow, and infrastructure implementations separated:

- `Contracts/` contains service interfaces, settings DTOs, and shared DTOs.
- `Application/Dictation/` contains the dictation workflow service that coordinates recording, recognition, auto-copy, auto-paste, and cancellation behavior.
- `Infrastructure/` contains Windows, NAudio, whisper.cpp, JSON settings, tray, clipboard, hotkey, and logging implementations.
- `MainWindowViewModel` coordinates UI state and delegates dictation flow to `IDictationWorkflowService`.

## Hotkey

Default: `Ctrl+Alt+Space`.

If the hotkey is already used by another app, VoiceTray still starts and reports the hotkey registration failure in the status line and log file.

## Where To Put whisper.cpp

```text
tools/whisper/whisper-cli.exe
```

The local development copy currently uses the official `whisper-bin-x64.zip` release from `ggml-org/whisper.cpp`.

## Where To Put The Model

```text
models/ggml-base.bin
```

The local development copy currently uses `ggml-base.bin` from the Hugging Face `ggerganov/whisper.cpp` model repository. Model `.bin` files are ignored by Git because `ggml-base.bin` is larger than GitHub's normal file size limit.

## Recording Indicator

Pressing `Start` begins WAV recording and shows a pulsing recording indicator in the main window. Recognition is still MVP-style: text appears after `Stop`, when the saved WAV has been processed by whisper.cpp.

## App Icon

VoiceTray includes a custom microphone icon used by the executable, the WPF window, and the tray icon.

## MVP Limitations

- Recognition is not streaming.
- Text appears after `Stop`.
- Auto-paste depends on Windows accepting focus and synthetic `Ctrl+V`.
- Settings UI is a placeholder in the MVP.
