# VOI-4 Follow-up: State, Hotkey Hook, Audio Contract

## Summary

Addressed follow-up review items:

- `MainWindowViewModel` now moves from `IsRecording=true` to `IsRecording=false` and `IsRecognizing=true` before awaiting recognition, so Stop is disabled during recognition.
- `CanStop()` now requires `IsRecording && !IsRecognizing`.
- Hotkey configuration is parsed and validated before creating the HWND handle/hook.
- `IAudioRecorder` no longer accepts `StorageSettings`; it accepts resolved `AudioRecordingOptions`.
- Added `AudioRecordingOptionsFactory` in the application layer to translate app storage settings before calling the recorder.
- Moved storage path resolving into `Contracts.Storage` so both `Application` and `Infrastructure` can use it without reverse layer dependencies.
- `MainWindowViewModel.CancelAsync` now cancels and awaits the active operation task, including recognition cleanup.
- `WhisperCppSpeechRecognizer` now drains/suppresses cancelled stdout/stderr read tasks after killing the process tree.
- Storage path helpers moved from `Contracts.Storage` to `Shared.Storage` so `Contracts` remains focused on DTOs and interfaces.
- Added tests for Start -> Stop -> Recognize happy path.
- Added tests for recognition timeout.
- Added tests for legacy storage normalization.
- Added regression coverage for the options factory.
- Added regression coverage for Start -> Stop -> cancel/exit lifecycle while recognition cleanup is still active.

## Verification

```bash
dotnet build
dotnet test
```

Results:

- Build succeeded with 0 warnings and 0 errors.
- Tests passed: 10 passed, 0 failed.
- Verified that `Application` and `Contracts` do not reference `Infrastructure`.
