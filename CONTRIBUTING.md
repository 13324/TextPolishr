# Contributing to Text Polishr

Thank you for helping improve Text Polishr.

## Before opening a pull request

1. Discuss substantial behavior or architecture changes in an issue first.
2. Keep changes focused and preserve the target-validation safety boundary.
3. Never add telemetry or send document context without explicit user consent.
4. Preserve third-party attribution, especially the Handy MIT notice.
5. Add or update dependency-free tests for changed behavior.

## Development

Text Polishr requires Windows and the .NET 10 SDK.

```powershell
dotnet restore TextPolishr.slnx
dotnet format TextPolishr.slnx --verify-no-changes --no-restore
dotnet build TextPolishr.slnx -c Release --no-restore
dotnet run --project tests/TextPolishr.Tests/TextPolishr.Tests.csproj -c Release --no-build
```

## Pull requests

- Explain the user-visible change and its failure behavior.
- Include screenshots for UI changes.
- Note which Windows applications were tested for clipboard changes.
- Do not include API keys, selected text, history files, or diagnostic logs.

By contributing, you agree that your contribution is licensed under the MIT
License used by this repository.
