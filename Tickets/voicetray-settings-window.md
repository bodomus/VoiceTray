# VoiceTray: settings window

## Context

The tray menu currently shows a placeholder message when the user opens Settings:

`Settings UI is not implemented yet.`

VoiceTray already has structured settings in `settings.json`, but users must edit them manually. The app needs a real settings window so the current configuration can be reviewed and changed from the UI.

## Requirements

- Replace the placeholder Settings message with a real WPF settings window opened from the tray menu.
- Edit all existing settings groups:
  - `HotKey`
  - `Whisper`
  - `Storage`
  - `Cancellation`
  - `Behavior`
- Keep the UI in English to match the current main window and tray menu.
- Add Save, Cancel, and Reset to defaults actions.
- Save changes back to `settings.json`.
- Update `AppSettingsHolder.Current` after save so whisper, storage, cancellation, and behavior changes apply to the next dictation operation.
- Do not dynamically re-register the global hotkey in this task. If hotkey values changed, show a message that hotkey changes take effect after restart.
- Add browse buttons for executable/model/file-system path fields where practical.
- Update `README.md` to document the Settings window.

## Proposed Implementation

- Extend `ISettingsService` with a public `SaveAsync(AppSettings settings, CancellationToken cancellationToken)` method.
- Reuse the existing `JsonSettingsService` serialization options and `AppSettingsNormalizer`.
- Add `SettingsWindow` and `SettingsWindowViewModel`.
- Register the settings window/view model dependencies in `App.xaml.cs`.
- Replace the tray `SettingsRequested` placeholder handler with modal settings window opening.
- Keep the implementation simple WPF without adding new UI libraries.

## Acceptance Criteria

1. Tray menu `Settings` opens a settings window instead of the placeholder message box.
2. The user can edit all existing `settings.json` groups from the UI.
3. `Save` writes the normalized settings to `settings.json`.
4. `Cancel` closes the window without writing changes.
5. `Reset to defaults` restores values from `AppSettings.Default` in the form before saving.
6. Runtime settings except hotkey are used by subsequent dictation operations after saving.
7. Hotkey changes are saved but clearly marked as requiring application restart.
8. `README.md` documents the new settings UI.
9. `dotnet build` succeeds.
10. `dotnet test` succeeds.

## Test Plan

- Add focused tests for settings save/load behavior.
- Add focused tests for settings view model save/reset behavior where practical.
- Run `dotnet build`.
- Run `dotnet test`.

## Estimate

4h
