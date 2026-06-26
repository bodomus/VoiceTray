# VOI-6 Report: Harden recording stop lifecycle and paste reliability

## Summary

Implemented lifecycle and reliability hardening for recording stop, paste focus handling, and hotkey failure status text.

## Completed Work

- Created branch `codex/voi-6-harden-recording-stop-lifecycle-paste` from `master`.
- Moved `VOI-6` to `In Progress` before implementation.
- Updated `NAudioRecorder.StopAsync` to:
  - use a `TaskCompletionSource` tied to `RecordingStopped`;
  - wait for `RecordingStopped` before returning;
  - dispose the recorder and WAV writer only after `RecordingStopped`;
  - ensure the WAV writer is closed before the workflow starts whisper recognition.
- Updated `WindowsTextPasteService` to:
  - validate that the captured target HWND still exists;
  - check `SetForegroundWindow` success;
  - verify the foreground window after the focus delay;
  - avoid sending Ctrl+V when focus did not return, returning a controlled failure instead.
- Updated hotkey registration failure status to use the configured gesture.
- Added test seams for NAudio and Windows paste behavior without changing public application contracts.

## Tests Added

- `NAudioRecorderTests.StopAsync_WaitsUntilRecordingStopped_BeforeDisposingDevice`
- `WindowsTextPasteServiceTests.PasteAsync_DoesNotSendPasteShortcut_WhenTargetWindowIsUnavailable`
- `AppHotKeyStatusTests.CreateHotKeyRegistrationFailureStatus_UsesConfiguredGesture`

## Verification

```text
dotnet build
Build succeeded.
0 warnings, 0 errors.

dotnet test
Passed: 16, Failed: 0, Skipped: 0.
```

## Tracker

- Progress comment added during implementation.
- Final tracker status should be set after this report is written.
