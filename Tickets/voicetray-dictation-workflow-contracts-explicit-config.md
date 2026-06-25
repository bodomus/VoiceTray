# VoiceTray: application-level dictation workflow, contracts, explicit config

## Context

The current MVP works, but orchestration is too close to the UI/ViewModel and some configuration is implicit:

- `MainWindowViewModel` directly coordinates recording, recognition, clipboard behavior, cancellation, and status transitions.
- Interface contracts live under `Infrastructure`, which blurs boundaries between abstractions and implementations.
- Storage/cancellation/hotkey behavior is partly hardcoded.

## Requirements

- Introduce one application-level dictation workflow service.
- Move interface contracts out of `Infrastructure`.
- Keep implementation classes under `Infrastructure`.
- Make cancellation configuration explicit.
- Make audio storage configuration explicit.
- Make hotkey configuration explicit instead of hardcoding key/modifiers in the service.
- Keep the MVP behavior intact:
  - `Start` starts recording.
  - `Stop` stops recording and recognizes text.
  - recognized text appears after `Stop`.
  - UI stays responsive.
  - tray and hotkey behavior continue to work.
- Update `README.md`.

## Proposed Shape

- Add application layer:
  - `Application/Dictation/IDictationWorkflowService.cs`
  - `Application/Dictation/DictationWorkflowService.cs`
  - result DTOs for start/stop/cancel workflow operations if useful.
- Move contracts to a non-infrastructure namespace/folder:
  - `Contracts/Audio`
  - `Contracts/Speech`
  - `Contracts/Clipboard`
  - `Contracts/HotKeys`
  - `Contracts/Tray`
  - `Contracts/Settings`
- Add explicit settings:
  - `Cancellation`
  - `Storage`
  - expanded `HotKey`

## Acceptance Criteria

1. `MainWindowViewModel` no longer directly orchestrates recorder + recognizer.
2. There is one application-level dictation workflow service responsible for start/stop/cancel dictation flow.
3. Interface contracts no longer live under `Infrastructure`.
4. Storage directory and retention are configured through settings.
5. Cancellation timeout/behavior is represented in settings.
6. Hotkey key/modifier/no-repeat settings are represented explicitly.
7. Existing app behavior remains intact.
8. `dotnet build` succeeds with 0 errors.
9. `README.md` is updated.

## Estimate

4h
