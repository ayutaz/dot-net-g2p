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
The DocFX configuration reads the Release assemblies under `.build/bin`, so run the Release build before generating docs.

Useful targeted commands:

```bash
dotnet tool restore
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Korean --no-restore
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Multilingual --no-restore -m:1
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --list flat
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Japanese*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Multilingual*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Romance*"
dotnet run -c Release --project samples/DotNetG2P.Console -- "$HOME/naist-jdic"
dotnet restore tests/DotNetG2P.PublishSmoke/DotNetG2P.PublishSmoke.csproj -r win-x64 -p:BuildProjectReferences=false
dotnet publish tests/DotNetG2P.PublishSmoke/DotNetG2P.PublishSmoke.csproj -c Release -f net8.0 -r win-x64 --self-contained true -p:PublishTrimmed=true -p:BuildProjectReferences=false --no-restore -o ./artifacts/publish-smoke/trim
dotnet pack src/DotNetG2P.Core/DotNetG2P.Core.csproj -c Release --no-build -p:EnablePackageValidation=true -o ./artifacts/package-validation
dotnet tool run docfx docs/docfx.json
dotnet tool run dotnet-CycloneDX DotNetG2P.slnx -o ./artifacts/sbom -t --disable-hash-computation
```

## Architecture

### Package Boundaries

- `src/DotNetG2P.Core`: Japanese G2P pipeline built around tokenization, NJD processing, and multiple output formats.
- `src/DotNetG2P.MeCab`: Japanese dictionary and tokenizer integration.
- `src/DotNetG2P.English`, `src/DotNetG2P.Chinese`, `src/DotNetG2P.Korean`, `src/DotNetG2P.Spanish`, `src/DotNetG2P.French`, `src/DotNetG2P.Portuguese`, `src/DotNetG2P.Swedish`: standalone language engines with language-specific rule systems and output formats.
- `src/DotNetG2P.Multilingual`: mixed-language router that segments text and dispatches to the underlying language engines.

Each package is published independently so consumers can take only the language support they need.
The multilingual package is the exception: it depends on the individual language packages because its job is orchestration rather than shared core logic.

### Japanese Pipeline

The Japanese engine is the deepest pipeline in the repository.
Its flow is:

1. Normalize input text.
2. Tokenize with `ITokenizer`.
3. Convert tokens into `NjdNode` objects.
4. Run NJD passes such as pronunciation setup, digit handling, accent phrase resolution, accent type assignment, and unvoiced vowel processing.
5. Materialize the requested output such as phonemes, kana, prosody, accent phrases, or HTS full-context labels.

This pipeline is intentionally concentrated in `DotNetG2P.Core` so higher-level packages do not need to understand NJD internals.

### Language Engines

The non-Japanese language packages follow a simpler structure:

- normalize text
- split into words or syllabic units
- apply rule-based pronunciation logic
- optionally apply dialect- or allophone-specific post-processing
- format the result as phoneme strings, IPA, X-SAMPA, or language-specific forms such as pinyin or zhuyin

These engines are designed to stay rule-based and self-contained.
That keeps them usable in Unity and other .NET environments without native or Python dependencies.

### Shared Internal Patterns

- Batch conversion helpers are placed directly in each package's `Internal/` directory rather than shared across projects.
- Embedded resources are used for dictionaries and static language data to keep package deployment simple.
- `InternalsVisibleTo` is enabled selectively so the test project can validate internal behavior without opening the public API surface.
- Build output is redirected under `.build/` through `Directory.Build.props` so generated files do not pollute package source directories.

The repository avoids a large cross-language inheritance hierarchy on purpose.
Public APIs stay language-shaped, and internal reuse is kept at the helper or adapter level.

### Multilingual Routing

`DotNetG2P.Multilingual` does not try to flatten all languages into one lowest-common-denominator API.
Instead it exposes mixed-language conversion and segment inspection while routing each segment to the best matching engine.

Important implications:

- Japanese conversion remains lock-protected because the tokenizer is not thread-safe.
- CJK and Latin defaults are configurable through multilingual options.
- Segment conversion is the stable seam for future internal adapter and test-contract work.

### Tooling And Quality Gates

- `DotNetG2P.slnx` is the main contributor solution.
- CI validates `.NET 8` and `.NET 9` across Windows, Linux, and macOS.
- The Ubuntu `.NET 9` lane is the quality gate for coverage, package validation, DocFX build, AOT/trim smoke checks, and SBOM generation.
- Benchmarks live under `tests/DotNetG2P.Benchmarks/` so performance work can evolve separately from correctness tests.

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

破壊的変更がある場合は CHANGELOG.md の Breaking Changes セクションと PR 本文に明記してください。
