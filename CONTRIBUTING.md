# Contributing

VoiceTray uses small, ticket-focused pull requests against `master`.

## Requirements

- Windows for running the app locally.
- .NET 9 SDK.
- Local whisper.cpp executable and model file when testing recognition manually.

## Restore, Build, and Test

Run these commands from the repository root before opening a pull request:

```bash
dotnet restore
dotnet build
dotnet test
```

## Branches and Pull Requests

- Use the branch naming convention `codex/VOI-<ticket>-short-description`.
- Target pull requests at `master`.
- Keep Codex pull requests small and focused on the YouTrack ticket.
- Merge only after GitHub Actions are green.

## Local Files

Do not commit whisper model files, generated WAV recordings, logs, or local `settings.json` files.
