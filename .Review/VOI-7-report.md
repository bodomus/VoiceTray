# VOI-7 Report: Settings validation

## Summary

Implemented explicit Settings window validation before saving. Invalid interactive input is now rejected before normalization and does not update the saved settings or runtime settings holder.

## Completed Work

- Created branch `codex/voi-7-settings-validation` from latest `master`.
- Moved `VOI-7` to `In Progress` before implementation.
- Added validation to `SettingsWindowViewModel.SaveAsync`.
- Kept `AppSettingsNormalizer` behavior unchanged for loading damaged or legacy config files.
- Generated a hotkey gesture from modifiers and key when the gesture field is empty.
- Preserved the existing successful save flow for valid settings.

## Validation Rules Implemented

- Hotkey modifiers must include at least one supported modifier.
- Hotkey key must be a valid WPF `Key`.
- Empty hotkey gesture is generated from modifiers and key before save.
- Whisper executable path, model path, and language must not be empty.
- Whisper language `auto` is rejected in Settings; accepted values are `ru`, `en`, and `uk`.
- Temporary file retention days must be greater than 0.
- Recognition timeout must be at least 5 seconds.
- Empty recording directory remains allowed and is normalized to the default recording folder on save.

## Tests Added

- Invalid timeout does not save and shows status.
- Invalid retention days do not save and show status.
- Invalid hotkey key does not save and shows status.
- Invalid hotkey modifier does not save and shows status.
- Empty whisper executable path does not save and shows status.
- Empty whisper model path does not save and shows status.
- `auto` whisper language does not save and shows status.
- Empty recording directory saves after normalization.
- Empty hotkey gesture generates `Ctrl+Alt+Space` before save.

## Verification

```text
dotnet build
Build succeeded.
0 warnings, 0 errors.

dotnet test
Passed: 25, Failed: 0, Skipped: 0.
```

## Known Limitations

- Settings validation does not check whether whisper executable/model files exist.
- Behavior checkbox consistency remains non-blocking as requested.
- Hotkey changes are still saved for restart-time application; live re-registration remains out of scope.
