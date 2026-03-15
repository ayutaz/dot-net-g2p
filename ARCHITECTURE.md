# Architecture

DotNetG2P is organized as a set of focused language packages plus a multilingual orchestrator.
The design goal is to keep each language implementation independently usable while sharing only the minimum internal infrastructure needed for contributor workflows, testing, and packaging.

## Package Boundaries

- `src/DotNetG2P.Core`: Japanese G2P pipeline built around tokenization, NJD processing, and multiple output formats.
- `src/DotNetG2P.MeCab`: Japanese dictionary and tokenizer integration.
- `src/DotNetG2P.English`, `src/DotNetG2P.Chinese`, `src/DotNetG2P.Korean`, `src/DotNetG2P.Spanish`, `src/DotNetG2P.French`, `src/DotNetG2P.Portuguese`: standalone language engines with language-specific rule systems and output formats.
- `src/DotNetG2P.Multilingual`: mixed-language router that segments text and dispatches to the underlying language engines.

Each package is published independently so consumers can take only the language support they need.
The multilingual package is the exception: it depends on the individual language packages because its job is orchestration rather than shared core logic.

## Japanese Pipeline

The Japanese engine is the deepest pipeline in the repository.
Its flow is:

1. Normalize input text.
2. Tokenize with `ITokenizer`.
3. Convert tokens into `NjdNode` objects.
4. Run NJD passes such as pronunciation setup, digit handling, accent phrase resolution, accent type assignment, and unvoiced vowel processing.
5. Materialize the requested output such as phonemes, kana, prosody, accent phrases, or HTS full-context labels.

This pipeline is intentionally concentrated in `DotNetG2P.Core` so higher-level packages do not need to understand NJD internals.

## Language Engines

The non-Japanese language packages follow a simpler structure:

- normalize text
- split into words or syllabic units
- apply rule-based pronunciation logic
- optionally apply dialect- or allophone-specific post-processing
- format the result as phoneme strings, IPA, X-SAMPA, or language-specific forms such as pinyin or zhuyin

These engines are designed to stay rule-based and self-contained.
That keeps them usable in Unity and other .NET environments without native or Python dependencies.

## Shared Internal Patterns

- Batch conversion helpers are linked into the package projects from `src/Shared/`.
- Embedded resources are used for dictionaries and static language data to keep package deployment simple.
- `InternalsVisibleTo` is enabled selectively so the test project can validate internal behavior without opening the public API surface.
- Build output is redirected under `.build/` through `Directory.Build.props` so generated files do not pollute package source directories.

The repository avoids a large cross-language inheritance hierarchy on purpose.
Public APIs stay language-shaped, and internal reuse is kept at the helper or adapter level.

## Multilingual Routing

`DotNetG2P.Multilingual` does not try to flatten all languages into one lowest-common-denominator API.
Instead it exposes mixed-language conversion and segment inspection while routing each segment to the best matching engine.

Important implications:

- Japanese conversion remains lock-protected because the tokenizer is not thread-safe.
- CJK and Latin defaults are configurable through multilingual options.
- Segment conversion is the stable seam for future internal adapter and test-contract work.

## Tooling And Quality Gates

- `DotNetG2P.slnx` is the main contributor solution.
- CI validates `.NET 8` and `.NET 9` across Windows, Linux, and macOS.
- The Ubuntu `.NET 9` lane is the quality gate for coverage, package validation, DocFX build, AOT/trim smoke checks, and SBOM generation.
- Benchmarks live under `tests/DotNetG2P.Benchmarks/` so performance work can evolve separately from correctness tests.

## Current Design Direction

The next architectural step is capability-based internal adapters.
The intent is to improve shared test fixtures, benchmarking seams, and batch conversion contracts without forcing every language into one public interface.
