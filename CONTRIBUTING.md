# Contributing

Thank you for contributing to DotNetG2P.
This repository contains packable libraries, test projects, evaluation tools, and a solution in `.slnx` format.

## Development Prerequisites

- .NET SDK 9.0 or later for the root `DotNetG2P.slnx` workflow
- PowerShell 7 for the helper scripts under `tools/`
- `naist-jdic` when working on Japanese or multilingual flows that route through Japanese tokenization

## Initial Setup

Install the Japanese dictionary when you need Japanese or multilingual coverage:

```powershell
pwsh -File tools/install_naist_jdic.ps1
```

The default install target is `%USERPROFILE%\naist-jdic`.
The Japanese and multilingual engines also honor `DOTNETG2P_NAIST_JDIC_PATH` and `NAIST_JDIC_PATH`.

## Build And Test

Use the solution file for the main contributor workflow:

```bash
dotnet restore DotNetG2P.slnx
dotnet build DotNetG2P.slnx --configuration Release
dotnet test DotNetG2P.slnx --configuration Release --filter "Category!=Performance"
```

The root solution is `.slnx`, so the full solution workflow requires a .NET SDK with SLNX support.
The CI matrix also validates the library and test projects with .NET 8 by building project files directly.

Useful targeted commands:

```bash
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Korean --no-restore
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Multilingual --no-restore -m:1
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --list flat
dotnet restore tests/DotNetG2P.PublishSmoke/DotNetG2P.PublishSmoke.csproj -r win-x64 -p:BuildProjectReferences=false
dotnet publish tests/DotNetG2P.PublishSmoke/DotNetG2P.PublishSmoke.csproj -c Release -f net8.0 -r win-x64 --self-contained true -p:PublishTrimmed=true -p:BuildProjectReferences=false --no-restore -o ./artifacts/publish-smoke/trim
dotnet pack src/DotNetG2P.Core/DotNetG2P.Core.csproj -c Release --no-build -p:EnablePackageValidation=true -o ./artifacts/package-validation
```

## Coding Guidelines

- Keep public API additions aligned with the package-specific README and root README examples.
- Package-specific usage docs live under `src/*/README.md`; update them alongside any user-visible package changes.
- Add or update tests for behavior changes, especially when modifying normalization, dictionaries, or batch conversion APIs.
- Prefer small, reviewable commits over broad refactors that mix unrelated concerns.
- Treat package compatibility carefully. If an API returns `IReadOnlyList<T>`, do not introduce a concrete collection requirement in callers or docs.

## Pull Requests

- Describe behavior changes, compatibility notes, and verification commands in the PR body.
- Update docs when installation, package layout, or user-visible behavior changes.
- CI publishes per-matrix test results and a coverage summary on PRs; use those reports when validating your change.
- Keep generated artifacts and local-only files out of commits.
- Make sure CI is green before requesting review.

## Release Notes

If a change alters public behavior, package dependencies, or required setup, add an entry to `MIGRATION.md` as part of the same PR.
