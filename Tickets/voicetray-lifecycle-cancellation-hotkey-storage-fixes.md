# VoiceTray: lifecycle, cancellation, hotkey validation, safe storage defaults

## Context

Review found several lifecycle and configuration issues after introducing the application-level dictation workflow:

1. `whisper-cli.exe` cancellation is incomplete. `WaitForExitAsync(cancellationToken)` can throw while the child process keeps running.
2. UI `IsRecognizing` is not set early enough in the ViewModel, so `CanStart()` can allow actions while recognition is already in progress.
3. Tray menu calls ViewModel methods directly instead of going through the same command availability path as UI buttons.
4. Default recording path still points to `A:\VoiceTray\Recordings`, which is unsafe as a default on ordinary machines.
5. Hotkey config silently ignores unknown modifiers and silently falls back to `Space` for unknown keys.
6. There is no test project covering workflow/cancel/hotkey regressions yet.

## Requirements

- Kill `whisper-cli.exe` process tree on cancellation/timeout and await process exit cleanup.
- Set `IsRecognizing = true` in `MainWindowViewModel` before awaiting workflow recognition.
- Route tray `Start Dictation` / `Stop Dictation` through commands or one shared command availability mechanism.
- Change default storage path to a safe user-local directory such as `%LocalAppData%\VoiceTray\Recordings`.
- Add explicit hotkey validation/logging instead of silent fallback.
- Add focused regression coverage where practical.
- Update `README.md`.

## Acceptance Criteria

1. Cancelling or timing out recognition terminates the external `whisper-cli.exe` process tree.
2. UI command availability reflects recognizing state immediately after `Stop`.
3. Tray start/stop uses the same command path as UI start/stop.
4. Default recording directory no longer depends on drive `A:`.
5. Invalid hotkey modifiers/keys are logged as warnings and hotkey registration fails clearly instead of silently changing behavior.
6. `dotnet build` succeeds.
7. Tests pass if a test project is added.
8. `README.md` documents the new defaults and lifecycle behavior.

## Estimate

4h
