# DotNetG2P

[日本語](README.md) | **English** | [中文](README_ZH.md)

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

A Japanese G2P (Grapheme-to-Phoneme) library for C#/.NET.
Natively reimplements the OpenJTalk-compatible rule-based G2P pipeline in C#, converting Japanese text to phoneme sequences without any dependency on Python or native binaries.

```csharp
using var engine = new G2PEngine(new MeCabTokenizer("/path/to/naist-jdic"));

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"
```

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Processing Pipeline](#processing-pipeline)
- [Dictionary Setup](#dictionary-setup)
- [Configuration Options](#configuration-options)
- [Building](#building)
- [Thread Safety](#thread-safety)
- [License](#license)

## Features

- **Pure C# implementation** — No native binaries required. The built-in MeCab engine (`DotNetG2P.MeCab`) eliminates NuGet package dependencies (a [naist-jdic dictionary](#dictionary-setup) is required at runtime)
- **OpenJTalk-compatible pipeline** — Six-stage NJD processing: pronunciation generation, digit reading, accent phrase grouping, accent type assignment, and vowel devoicing
- **Multiple output formats** — Phoneme sequences / Katakana / ESPnet prosody symbols / VOICEVOX-compatible AccentPhrase / HTS full-context labels / Prosody features (A1/A2/A3)
- **Unity support** — Targets .NET Standard 2.1 (Unity 2021.2+) with UPM packages available
- **Extensible design** — Swap out the morphological analysis engine via the `ITokenizer` interface

## Installation

### NuGet

```bash
# Core library + built-in MeCab engine
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab
```

### Package Overview

| Package | License | Description |
|---------|---------|-------------|
| `DotNetG2P` | Apache-2.0 | Core library (G2P engine, NJD processing, phoneme conversion) |
| `DotNetG2P.MeCab` | Apache-2.0 | Built-in MeCab engine (no external dependencies) |

### Unity (UPM)

Add the following URLs via Unity Package Manager's **Add package from git URL**:

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
```

> **Note:** A naist-jdic dictionary is required separately. See [Dictionary Setup](#dictionary-setup) for details.

## Quick Start

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

// 1. Initialize the engine (specify dictionary path)
using var tokenizer = new MeCabTokenizer("/path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);

// 2. Convert text to phoneme sequence
string phonemes = engine.ToPhonemes("今日は良い天気です");
// => "ky o o w a i i t e N k i d e s U"

// 3. Get katakana reading
string kana = engine.ToKana("今日は良い天気です");
// => "キョーワイーテンキデス"

// 4. Prosody-annotated output (ESPnet format)
string prosody = engine.ToProsody("こんにちは");
// => "^ k o [ N n i ch i w a $"

// 5. VOICEVOX-compatible accent phrases
var phrases = engine.ToAccentPhrases("こんにちは");

// 6. HTS full-context labels (for HMM/DNN speech synthesis)
var labels = engine.ToFullContextLabels("こんにちは");

// 7. Prosody features (per-phoneme A1/A2/A3, for speech synthesis engines like uPiper)
var features = engine.ToProsodyFeatures("こんにちは");
// features.Phonemes: ["sil","k","o","N","n","i","ch","i","w","a","sil"]
// features.A1, A2, A3: accent position info for each phoneme
```

## API Reference

### G2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Space-separated phoneme sequence (`"k o N n i ch i w a"`) |
| `ToKana(text)` | `string` | Katakana reading (`"コンニチワ"`) |
| `ToProsody(text)` | `string` | ESPnet prosody-annotated output (`"^ k o [ N n i ch i w a $"`) |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX-compatible accent phrase structures |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTS full-context labels |
| `ToProsodyFeatures(text)` | `ProsodyFeatures` | Prosody features (per-phoneme A1/A2/A3) |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | Node sequence after NJD processing |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch conversion of multiple texts to phoneme sequences |
| `ToKanaBatch(texts)` | `IReadOnlyList<string>` | Batch conversion of multiple texts to katakana readings |
| `ToProsodyBatch(texts)` | `IReadOnlyList<string>` | Batch conversion of multiple texts to prosody-annotated output |
| `ToFullContextLabelsBatch(texts)` | `IReadOnlyList<IReadOnlyList<string>>` | Batch conversion of multiple texts to HTS labels |
| `ToProsodyFeaturesBatch(texts)` | `IReadOnlyList<ProsodyFeatures>` | Batch conversion of multiple texts to prosody features |

### Japanese Phoneme System

| Category | Phonemes |
|----------|----------|
| Vowels | `a` `i` `u` `e` `o` (devoiced: `A` `I` `U` `E` `O`) |
| Consonants | `k` `g` `s` `z` `t` `d` `n` `h` `b` `p` `m` `r` `f` `v` |
| Palatalized/Affricate Consonants | `ky` `gy` `sh` `j` `ch` `ts` `ny` `hy` `by` `py` `my` `ry` `dy` `ty` `kw` `gw` |
| Semivowels | `y` `w` |
| Special | `N` (moraic nasal) `cl` (geminate) `-` (long vowel) `pau` (pause) |

## Processing Pipeline

DotNetG2P implements a six-stage NJD processing pipeline equivalent to [OpenJTalk](https://open-jtalk.sourceforge.net/).

```
Text Input
  │
  ├─ TextNormalizer        Fullwidth/halfwidth normalization, dakuten combining
  ├─ ITokenizer.Tokenize   Morphological analysis (MeCabTokenizer + naist-jdic)
  ├─ SetPronunciation      Dictionary reading & fallback pronunciation generation
  ├─ SetDigit              Digit sequence detection & counter suffix reading
  ├─ SetAccentPhrase       Accent phrase grouping by POS patterns (18 rules)
  ├─ SetAccentType         Accent type assignment via chain rules
  └─ SetUnvoicedVowel      Vowel devoicing (6 rules)
  │
  ▼
  Output (Phonemes / Katakana / Prosody Symbols / AccentPhrase / HTS Labels / Prosody Features)
```

## Dictionary Setup

DotNetG2P uses the naist-jdic dictionary (an OpenJTalk MeCab dictionary) for morphological analysis.

### How to Obtain

1. Download from the [Open JTalk official website](https://open-jtalk.sourceforge.net/)
2. Use the dictionary directory bundled with pyopenjtalk or OpenJTalk as-is

### Required Files

The dictionary directory must contain the following 4 files:

| File | Contents |
|------|----------|
| `sys.dic` | System dictionary |
| `matrix.bin` | Transition cost matrix |
| `char.bin` | Character category definitions |
| `unk.dic` | Unknown word templates |

### Placement in Unity

In Unity, place the dictionary files in the `StreamingAssets` folder and specify the path using `Application.streamingAssetsPath`.

```csharp
var dicPath = Path.Combine(Application.streamingAssetsPath, "naist-jdic");
using var tokenizer = new MeCabTokenizer(dicPath);
```

## Configuration Options

`G2POptions` allows you to toggle each processing stage individually (immutable design).

```csharp
// Example: disable vowel devoicing only
var options = new G2POptions(enableUnvoicedVowel: false);
using var engine = new G2PEngine(tokenizer, options);
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `enableTextNormalization` | `true` | Text normalization (fullwidth/halfwidth conversion) |
| `enableDigitProcessing` | `true` | Digit reading conversion & counter suffix processing |
| `enableAccentPhrase` | `true` | Accent phrase grouping (18 rules) |
| `enableAccentType` | `true` | Accent type assignment |
| `enableUnvoicedVowel` | `true` | Vowel devoicing (6 rules) |
| `expandLongVowels` | `true` | Expand long vowels as repeated vowels (`false` = use `"-"` symbol) |

## Building

### Requirements

- .NET SDK 9.0 or later

### Commands

```bash
# Build
dotnet build DotNetG2P.slnx

# Run tests
dotnet test DotNetG2P.slnx

# Console sample (without dictionary: MoraMapping verification only)
dotnet run --project samples/DotNetG2P.Console

# Console sample (with dictionary: full G2P)
dotnet run --project samples/DotNetG2P.Console -- /path/to/naist-jdic
```

## Thread Safety

`G2PEngine` and `MeCabTokenizer` are not thread-safe.
In multi-threaded environments, create a separate instance for each thread.

Dictionary data (`DictionaryBundle`) is automatically shared via an internal WeakReference cache,
so creating multiple instances incurs minimal memory overhead.

## License

| Package | License | Notes |
|---------|---------|-------|
| **DotNetG2P** | [Apache-2.0](LICENSE) | Core library |
| **DotNetG2P.MeCab** | [Apache-2.0](LICENSE) | Built-in MeCab engine |

All components are available under the **Apache-2.0 License**.
