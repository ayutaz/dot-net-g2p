# DotNetG2P

[日本語](README.md) | **English** | [中文](README_ZH.md)

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

A multilingual G2P (Grapheme-to-Phoneme) library for C#/.NET covering Japanese, English, Chinese, Korean, Spanish, French, and Portuguese.
It natively reimplements the OpenJTalk-compatible Japanese G2P pipeline, CMU dictionary-based English G2P, pinyin-data dictionary-based Chinese pinyin conversion, Hangul-first Korean G2P, and rule-based Romance-language G2P in C#, without depending on Python or native binaries.

```csharp
using var engine = new G2PEngine(new MeCabTokenizer());

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"

// English G2P
using var enEngine = new EnglishG2PEngine();
enEngine.ToPhonemes("hello world");  // => "HH AH0 L OW1 W ER1 L D"

// Chinese G2P (Pinyin conversion)
using var zhEngine = new ChineseG2PEngine();
zhEngine.ToPinyin("你好世界");  // => "ní hǎo shì jiè"

// Korean G2P
using var koEngine = new KoreanG2PEngine();
koEngine.ToPhonemes("좋다");  // => "ㅈ ㅗ ㅌ ㅏ"

// Spanish G2P
using var esEngine = new SpanishG2PEngine();
esEngine.ToIPA("vergüenza");  // => "beɾˈɡwensa"

// French G2P
using var frEngine = new FrenchG2PEngine();
frEngine.ToIPA("bonjour");  // => "bɔ̃ʒuʁ"

// Portuguese G2P
using var ptEngine = new PortugueseG2PEngine();
ptEngine.ToIPA("obrigado");  // => "obɾiˈɡadu"

// Mixed Japanese-Korean-English text
using var multiEngine = new MultilingualG2PEngine();
multiEngine.ToPhonemes("今日は안녕하세요 hello");  // Japanese => Japanese phonemes, Korean => Hangul phonemes, English => ARPAbet

var multiEsOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Spanish);
using var multiEsEngine = new MultilingualG2PEngine(multiEsOptions);
multiEsEngine.ToPhonemes("hola世界");  // Spanish => IPA phonemes, Japanese => Japanese phonemes
```

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Processing Pipeline](#processing-pipeline)
- [Dictionary Setup](#dictionary-setup)
- [Spanish Evaluation](#spanish-evaluation)
- [French Evaluation](#french-evaluation)
- [Portuguese Evaluation](#portuguese-evaluation)
- [Configuration Options](#configuration-options)
- [Related Documentation](#related-documentation)
- [Building](#building)
- [Thread Safety](#thread-safety)
- [License](#license)

## Features

- **Pure C# implementation** — No native binaries required. The built-in MeCab engine (`DotNetG2P.MeCab`) eliminates NuGet package dependencies (a [naist-jdic dictionary](#dictionary-setup) is required at runtime)
- **OpenJTalk-compatible pipeline** — Six-stage NJD processing: pronunciation generation, digit reading, accent phrase grouping, accent type assignment, and vowel devoicing
- **Multiple output formats** — Phoneme sequences / Katakana / ESPnet prosody symbols / VOICEVOX-compatible AccentPhrase / HTS full-context labels / Prosody features (A1/A2/A3)
- **Unity support** — Targets .NET Standard 2.1 (Unity 2021.2+) with UPM packages available
- **Extensible design** — Swap out the morphological analysis engine via the `ITokenizer` interface
- **English G2P support** — CMU dictionary (135,000 words) + Flite LTS rules for OOV estimation, IPA/X-SAMPA output, text normalization, and heteronym resolution
- **Chinese G2P support** — pinyin-data character dictionary (44,000 entries) + phrase-pinyin-data phrase dictionary (411,000 entries) for automatic polyphone resolution, tone sandhi (third tone, 一/不 rules), 3 output styles, IPA (International Phonetic Alphabet) and Zhuyin (Bopomofo) output
- **Korean G2P support** — Hangul-first rule-based conversion with Jamo decomposition, standard-pronunciation-oriented phonological rules, exact exception dictionary overrides, lightweight normalization, benchmark harnesses for `g2pk_parity`, `official_gold`, and `weak_rules`, plus external corpus gates and performance tests
- **Spanish G2P support** — Rule-based IPA conversion with syllabification, stress assignment, Castilian/Latin American options, optional allophone processing, normalization, an exception dictionary, and a full-corpus evaluation toolchain. The normalizer now also distinguishes grouping separators from decimal separators and safely falls back on invalid dates/times
- **French and Portuguese G2P support** — Rule-based IPA conversion with exception dictionaries, dialect options, normalization pipelines, and dataset-driven evaluation toolchains
- **Mixed Japanese-English-Chinese-Korean-Spanish-French-Portuguese text support** — Automatic language detection and segment splitting based on Unicode character categories, with Hangul routed directly to Korean segments and `DefaultLatinLanguage` controlling English/Spanish/French/Portuguese Latin-script routing. Pure CJK ideograph runs are further disambiguated with markers, Japanese lexical hints, and embedded Chinese dictionaries, and the embedded Chinese dictionaries are shared with `ChineseG2PEngine` to avoid duplicate residency

## Installation

### NuGet

```bash
# Core library + built-in MeCab engine (Japanese G2P)
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab

# English G2P
dotnet add package DotNetG2P.English

# Chinese G2P (Pinyin conversion)
dotnet add package DotNetG2P.Chinese

# Korean G2P
dotnet add package DotNetG2P.Korean

# Spanish G2P
dotnet add package DotNetG2P.Spanish

# French G2P
dotnet add package DotNetG2P.French

# Portuguese G2P
dotnet add package DotNetG2P.Portuguese

# Mixed Japanese-English-Chinese-Korean-Spanish-French-Portuguese text support
dotnet add package DotNetG2P.Multilingual
```

### Package Overview

| Package | License | Description |
|---------|---------|-------------|
| `DotNetG2P` | Apache-2.0 | Core library (G2P engine, NJD processing, phoneme conversion) |
| `DotNetG2P.MeCab` | Apache-2.0 | Built-in MeCab engine (no external dependencies) |
| `DotNetG2P.English` | Apache-2.0 | English G2P engine (CMU dictionary + LTS rules) |
| `DotNetG2P.Chinese` | Apache-2.0 | Chinese G2P engine (pinyin-data dictionary + tone sandhi) |
| `DotNetG2P.Korean` | Apache-2.0 | Korean G2P engine (Hangul-first rule engine + exception dictionary + normalization) |
| `DotNetG2P.Spanish` | Apache-2.0 | Spanish G2P engine (rule-based + optional allophones) |
| `DotNetG2P.French` | Apache-2.0 | French G2P engine (rule-based + exception dictionary + optional allophones) |
| `DotNetG2P.Portuguese` | Apache-2.0 | Portuguese G2P engine (rule-based + exception dictionary + optional allophones) |
| `DotNetG2P.Multilingual` | Apache-2.0 | Multilingual G2P engine (mixed Japanese-English-Chinese-Korean-Spanish-French-Portuguese text support) |

## Related Documentation

- [CONTRIBUTING.md](CONTRIBUTING.md): contributor setup, build/test workflow, and PR expectations
- [ARCHITECTURE.md](ARCHITECTURE.md): package boundaries, shared internals, and multilingual routing
- [CHANGELOG.md](CHANGELOG.md): release history, including unreleased infrastructure and API updates
- Package-specific READMEs:
  - [`DotNetG2P`](src/DotNetG2P.Core/README.md)
  - [`DotNetG2P.MeCab`](src/DotNetG2P.MeCab/README.md)
  - [`DotNetG2P.English`](src/DotNetG2P.English/README.md)
  - [`DotNetG2P.Chinese`](src/DotNetG2P.Chinese/README.md)
  - [`DotNetG2P.Korean`](src/DotNetG2P.Korean/README.md)
  - [`DotNetG2P.Spanish`](src/DotNetG2P.Spanish/README.md)
  - [`DotNetG2P.French`](src/DotNetG2P.French/README.md)
  - [`DotNetG2P.Portuguese`](src/DotNetG2P.Portuguese/README.md)
  - [`DotNetG2P.Multilingual`](src/DotNetG2P.Multilingual/README.md)

### Unity (UPM)

Add the following URLs via Unity Package Manager's **Add package from git URL**:

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.English
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Chinese
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Korean
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Spanish
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.French
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Portuguese
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Multilingual
```

> **Dependency note:** If you install `DotNetG2P.MeCab`, also add `DotNetG2P.Core`. If you install `DotNetG2P.Multilingual`, add its dependent packages to `manifest.json` as well.
> **Note:** Japanese and multilingual engines require a separate naist-jdic dictionary. See [Dictionary Setup](#dictionary-setup) for details.

## Quick Start

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

// 1. Resolve the dictionary from the default install path or environment variables
using var tokenizer = new MeCabTokenizer();
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

// === Chinese G2P (Pinyin Conversion) ===
using DotNetG2P.Chinese;

using var zhEngine = new ChineseG2PEngine();

// Basic conversion (with tone marks)
string pinyin = zhEngine.ToPinyin("你好世界");
// => "ní hǎo shì jiè"

// Tone number format
string toneNum = zhEngine.ToPinyin("你好世界", PinyinStyle.ToneNumber);
// => "ni2 hao3 shi4 jie4"

// Per-character pinyin array
string[] list = zhEngine.ToPinyinList("中国");
// => ["zhōng", "guó"]

// Automatic polyphone resolution
string bank = zhEngine.ToPinyin("银行");  // => "yín háng" (háng = bank)
string act = zhEngine.ToPinyin("行为");   // => "xíng wéi" (xíng = behavior)

// IPA (International Phonetic Alphabet) output
string ipa = zhEngine.ToIPA("你好");
// => IPA transcription

// Zhuyin (Bopomofo) output
string zhuyin = zhEngine.ToZhuyin("你好");
// => Zhuyin transcription

// === English G2P ===
using DotNetG2P.English;

using var enEngine = new EnglishG2PEngine();
string enPhonemes = enEngine.ToPhonemes("hello world");
// => "HH AH0 L OW1 W ER1 L D"

// === Korean G2P ===
using DotNetG2P.Korean;

using var koEngine = new KoreanG2PEngine();
string koPhonemes = koEngine.ToPhonemes("좋다");
// => "ㅈ ㅗ ㅌ ㅏ"

string koJamo = koEngine.ToJamo("한글");
// => "ㅎㅏㄴ ㄱㅡㄹ"

using var koColloquial = new KoreanG2PEngine(
    new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial));
string koColloquialHangul = koColloquial.Analyze("나의").ToHangulString();
// => "나에"

// === Spanish G2P ===
using DotNetG2P.Spanish;

using var esEngine = new SpanishG2PEngine();
string esIpa = esEngine.ToIPA("guion");
// => "ɡiˈon"

// === French G2P ===
using DotNetG2P.French;

using var frEngine = new FrenchG2PEngine();
string frIpa = frEngine.ToIPA("bonjour");
// => "bɔ̃ʒuʁ"

string frIpa2 = frEngine.ToIPA("merci");
// => "mɛʁsi"

using var frAlloEngine = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: true));
string frAllo = frAlloEngine.ToIPA("autre");
// => IPA transcription with allophone rules applied

string frXsampa = frEngine.ToXSampa("bonjour");

// === Portuguese G2P ===
using DotNetG2P.Portuguese;

using var ptEngine = new PortugueseG2PEngine();
string ptIpa = ptEngine.ToIPA("obrigado");
// => "obɾiˈɡadu"

string ptIpa2 = ptEngine.ToIPA("coração");
// => "koɾaˈsɐ̃w̃"

using var ptEpEngine = new PortugueseG2PEngine(new PortugueseG2POptions(dialect: PortugueseDialect.European));
string ptEpIpa = ptEpEngine.ToIPA("obrigado");

using var ptAlloEngine = new PortugueseG2PEngine(new PortugueseG2POptions(enableAllophones: true));
string ptAllo = ptAlloEngine.ToIPA("cidade");
// => IPA transcription with allophone rules applied

string ptXsampa = ptEngine.ToXSampa("obrigado");
string ptXsampaNoStress = ptEngine.ToXSampaWithoutStress("obrigado");

var ptBatch = ptEngine.ToIPABatch(new[] { "bom dia", "boa noite" });

// === Mixed Japanese-English-Chinese-Korean-Spanish-French-Portuguese Text ===
using DotNetG2P.Multilingual;

using var multiEngine = new MultilingualG2PEngine();
string mixed = multiEngine.ToPhonemes("今日は안녕하세요 good dayです");
// Japanese segments => Japanese phonemes, Korean segments => Hangul phonemes, English segments => ARPAbet phonemes

var segments = multiEngine.ToSegments("今日はgood dayです");
// List of segments with language tags

// For text containing Chinese
var zhOptions = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
using var multiZhEngine = new MultilingualG2PEngine(zhOptions);
multiZhEngine.ToPhonemes("你好hello");
// Chinese segments => Pinyin, English segments => ARPAbet phonemes

// Pass Korean-specific options through the multilingual engine
var koOptions = new MultilingualG2POptions(
    koreanOptions: new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial));
using var multiKoEngine = new MultilingualG2PEngine(koOptions);
multiKoEngine.ToPhonemes("나의 hello");
// Korean segments => KoreanG2PEngine output, English segments => ARPAbet phonemes

// For text containing Spanish
var esOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Spanish);
using var multiEsEngine = new MultilingualG2PEngine(esOptions);
multiEsEngine.ToPhonemes("hola世界");
// Spanish segments => IPA phonemes, Japanese segments => Japanese phonemes

// For text containing French
var frOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.French);
using var multiFrEngine = new MultilingualG2PEngine(frOptions);
multiFrEngine.ToPhonemes("bonjour世界");
// French segments => IPA phonemes, Japanese segments => Japanese phonemes

// For text containing Portuguese
var ptOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Portuguese);
using var multiPtEngine = new MultilingualG2PEngine(ptOptions);
multiPtEngine.ToPhonemes("obrigado世界");
// Portuguese segments => IPA phonemes, Japanese segments => Japanese phonemes
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

### EnglishG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | ARPAbet phoneme sequence (`"HH AH0 L OW1"`) |
| `ToIPA(text)` | `string` | IPA transcription |
| `ToIPAWithoutStress(text)` | `string` | IPA transcription without stress marks |
| `ToXSampa(text)` | `string` | X-SAMPA transcription |
| `ToXSampaWithoutStress(text)` | `string` | X-SAMPA transcription without stress marks |
| `ToPhonemeList(text)` | `IReadOnlyList<EnglishPhoneme>` | Structured phoneme list |
| `LookupWord(word)` | `IReadOnlyList<EnglishPhoneme>` | Single-word lookup |
| `LookupAllPronunciations(word)` | `IReadOnlyList<EnglishPronunciation>` | Get all pronunciation variants |
| `ContainsWord(word)` | `bool` | Dictionary existence check |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch ARPAbet conversion |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | Batch IPA conversion |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | Batch X-SAMPA conversion |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<EnglishPhoneme>>` | Batch structured phoneme list conversion |

### ChineseG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPinyin(text)` | `string` | Tone-marked pinyin string (`"nǐ hǎo"`) |
| `ToPinyin(text, style)` | `string` | Pinyin string with specified style |
| `ToPinyinList(text)` | `string[]` | Per-character pinyin array |
| `ToPinyinList(text, style)` | `string[]` | Per-character pinyin array with specified style |
| `ContainsChar(c)` | `bool` | Dictionary existence check |
| `LookupChar(c)` | `string[]` | Get all pinyin candidates |
| `ToIPA(text)` | `string` | IPA (International Phonetic Alphabet) transcription |
| `ToIPA(text, includeTones)` | `string` | IPA transcription with tone control |
| `ToZhuyin(text)` | `string` | Zhuyin (Bopomofo) transcription |
| `ToZhuyin(text, includeTones)` | `string` | Zhuyin transcription with tone control |
| `ToPinyinBatch(texts)` | `string[]` | Batch pinyin conversion |
| `ToPinyinBatch(texts, style)` | `string[]` | Batch pinyin conversion (with style) |
| `ToPinyinListBatch(texts)` | `string[][]` | Batch per-character pinyin conversion |
| `ToPinyinListBatch(texts, style)` | `string[][]` | Batch per-character pinyin conversion (with style) |
| `ToIPABatch(texts)` | `string[]` | Batch IPA conversion |
| `ToIPABatch(texts, includeTones)` | `string[]` | Batch IPA conversion (tone control) |
| `ToZhuyinBatch(texts)` | `string[]` | Batch Zhuyin conversion |
| `ToZhuyinBatch(texts, includeTones)` | `string[]` | Batch Zhuyin conversion (tone control) |

### KoreanG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Space-separated compatibility Jamo phoneme sequence (`"ㅈ ㅗ ㅌ ㅏ"`) |
| `ToJamo(text)` | `string` | Syllable-separated Jamo output (`"ㅎㅏㄴ ㄱㅡㄹ"`) |
| `Analyze(text)` | `KoreanPronunciation` | Structured pronunciation model with normalized text |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch phoneme conversion |
| `ToJamoBatch(texts)` | `IReadOnlyList<string>` | Batch Jamo conversion |

### SpanishG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Space-separated IPA phoneme sequence |
| `ToIPA(text)` | `string` | IPA transcription |
| `ToXSampa(text)` | `string` | X-SAMPA transcription |
| `ToXSampaWithoutStress(text)` | `string` | X-SAMPA transcription without stress marks |
| `ToPhonemeList(text)` | `IReadOnlyList<SpanishPhoneme>` | Structured phoneme list |
| `ToSyllables(word)` | `IReadOnlyList<SpanishSyllable>` | Syllabification result |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch phoneme conversion |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | Batch IPA conversion |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | Batch X-SAMPA conversion |

### FrenchG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Space-separated IPA phoneme sequence |
| `ToIPA(text)` | `string` | IPA transcription (`"bɔ̃ʒuʁ"` style) |
| `ToIPAWithoutStress(text)` | `string` | IPA transcription without stress marks |
| `ToXSampa(text)` | `string` | X-SAMPA transcription |
| `ToXSampaWithoutStress(text)` | `string` | X-SAMPA transcription without stress marks |
| `ToPhonemeList(text)` | `IReadOnlyList<FrenchPhoneme>` | Structured phoneme list |
| `ToSyllables(word)` | `IReadOnlyList<FrenchPhoneme[]>` | Syllabification result |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch phoneme conversion |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | Batch IPA conversion |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | Batch X-SAMPA conversion |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<FrenchPhoneme>>` | Batch structured phoneme list conversion |

### PortugueseG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Space-separated IPA phoneme sequence |
| `ToIPA(text)` | `string` | IPA transcription (`"obɾiˈɡadu"` style) |
| `ToIPAWithoutStress(text)` | `string` | IPA transcription without stress marks |
| `ToXSampa(text)` | `string` | X-SAMPA transcription |
| `ToXSampaWithoutStress(text)` | `string` | X-SAMPA transcription without stress marks |
| `ToPhonemeList(text)` | `IReadOnlyList<PortuguesePhoneme>` | Structured phoneme list |
| `ToSyllables(text)` | `IReadOnlyList<PortugueseSyllable>` | Syllabification result |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch phoneme conversion |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | Batch IPA conversion |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | Batch X-SAMPA conversion |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<PortuguesePhoneme>>` | Batch structured phoneme list conversion |

### MultilingualG2PEngine

| Method | Return Type | Description |
|--------|-------------|-------------|
| `ToPhonemes(text)` | `string` | Mixed Japanese-English-Chinese-Korean-Spanish-French-Portuguese phoneme sequence |
| `ToSegments(text)` | `IReadOnlyList<G2PSegment>` | Language-tagged segments |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | Batch phoneme conversion |
| `ToSegmentsBatch(texts)` | `IReadOnlyList<IReadOnlyList<G2PSegment>>` | Batch segment conversion |

Multilingual notes:

- Hangul syllables / Jamo / compatibility jamo / halfwidth Hangul are classified as Korean and routed to `DotNetG2P.Korean`
- Latin-script tokens default to `DefaultLatinLanguage`, then switch English / Spanish / French / Portuguese using language-specific markers and high-frequency lexical patterns
- Pure CJK ideograph runs are further classified with Chinese strong/weak markers, Japanese markers, Japanese lexical hints, and embedded Chinese phrase/character dictionaries
- The embedded Chinese dictionaries are shared with `ChineseG2PEngine`, so additional `TextSegmenter`-only dictionary residency is about `0.02MB` in the current measurement
- Only ambiguous pure ideograph runs with weak evidence fall back to `DefaultCjkLanguage`
- `MultilingualG2POptions.KoreanOptions` passes Korean normalization and `UiVariationMode` settings through to `KoreanG2PEngine`
- Current multilingual regression status: `448 passed`
- `MultilingualPerformanceTests`: `8 passed`
- `MultilingualKoreanPerformanceTests`: `2 passed`

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

### Recommended Setup

```powershell
pwsh -File tools/install_naist_jdic.ps1
```

This script downloads the dictionary from the OpenJTalk distribution and extracts it to `%USERPROFILE%\naist-jdic` by default.
`MeCabTokenizer()` and `MultilingualG2PEngine()` search for the dictionary in this order:

1. `DOTNETG2P_NAIST_JDIC_PATH`
2. `NAIST_JDIC_PATH`
3. `%USERPROFILE%\naist-jdic`
4. `naist-jdic` or `open_jtalk_dic_utf_8-1.11` under the current directory

### Manual Setup

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
using var multiEngine = new MultilingualG2PEngine(dicPath);
```

## Spanish Evaluation

The Spanish G2P package includes a full-corpus evaluation pipeline backed by `ipa-dict` and `WikiPron`.

```powershell
pwsh -File tools/refresh_spanish_eval_data.ps1 -Mode Full
pwsh -File tools/run_spanish_full_evaluation.ps1 -EnforceThresholds
```

- Corpus output: `artifacts/spanish-eval/corpora`
- Report output: `artifacts/spanish-eval/reports/latest`
- Main artifacts:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

Measured on March 9, 2026:

- `ipa_dict_es_es_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_es_full/allophones`: PER `1.37%`, WER `13.69%`
- `ipa_dict_es_mx_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_mx_full/allophones`: PER `1.37%`, WER `13.69%`
- `wikipron_spa_latn_ca_broad_filtered_full/base`: PER `1.38%`, WER `11.14%`
- `wikipron_spa_latn_la_broad_filtered_full/base`: PER `1.43%`, WER `11.46%`

Additional regression checks on March 10, 2026:

- `SpanishG2P`: `227 passed`
- `SpanishNormalizer` treats `1.234` as a grouped integer and does not semantically expand invalid dates/times

## French Evaluation

The French G2P package includes a full-corpus evaluation pipeline backed by `ipa-dict` and `WikiPron`.

```powershell
pwsh -File tools/refresh_french_eval_data.ps1 -Mode Full
pwsh -File tools/run_french_full_evaluation.ps1 -EnforceThresholds
```

- Corpus output: `artifacts/french-eval/corpora`
- Report output: `artifacts/french-eval/reports/latest`
- Main artifacts:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

Thresholds (`tools/french_eval_thresholds.json`):

- `ipa_dict_fr_fr_sample`: base/allophones PER `< 8%`, no_exceptions PER `< 12%`
- `ipa_dict_fr_fr_full`: base/allophones PER `< 12%`, no_exceptions PER `< 18%`
- `wikipron_fra_latn_broad_filtered_sample`: base PER `< 8%`
- `wikipron_fra_latn_broad_filtered_full`: base PER `< 12%`

Regression coverage:

- `FrenchG2P`: `707 passed, 12 skipped` (`719` total)
- `FrenchDatasetEvaluationTests`: `6` threshold-backed corpus checks
- `FrenchAllophoneEvaluationTests`: `6` allophone profile checks

## Portuguese Evaluation

The Portuguese G2P package combines rule-based conversion with a 560+ entry exception dictionary and ships with a full-corpus evaluation pipeline.

```powershell
pwsh -File tools/refresh_portuguese_eval_data.ps1 -Mode Full
pwsh -File tools/run_portuguese_full_evaluation.ps1 -EnforceThresholds
```

- Corpus output: `artifacts/portuguese-eval/corpora`
- Report output: `artifacts/portuguese-eval/reports/latest`
- Main artifacts:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

Regression coverage:

- `PortugueseG2P`: `1294 passed, 16 skipped` (`1310` total)
- `PortugueseDatasetEvaluationTests`: `9` threshold-backed corpus checks
- `PortugueseAllophoneEvaluationTests`: `7` allophone profile checks

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

- `.slnx` contributor workflows require .NET SDK 9.0 or later
- CI also validates .NET 8 compatibility by building `tests/`, `samples/`, and `tools/` project files directly

### Commands

```bash
# Build
dotnet build DotNetG2P.slnx

# Run tests
dotnet test DotNetG2P.slnx

# Console sample (without dictionary: MoraMapping verification only)
dotnet run --project samples/DotNetG2P.Console

# Install the dictionary to the default location
pwsh -File tools/install_naist_jdic.ps1

# Console sample (dictionary auto-resolved: full G2P)
dotnet run --project samples/DotNetG2P.Console

# Console sample (explicit dictionary path)
dotnet run --project samples/DotNetG2P.Console -- /path/to/naist-jdic
```

## Thread Safety

`G2PEngine` and `MeCabTokenizer` are not thread-safe.
In multi-threaded environments, create a separate instance for each thread.

Dictionary data (`DictionaryBundle`) is automatically shared via an internal WeakReference cache,
so creating multiple instances incurs minimal memory overhead.

`EnglishG2PEngine`, `ChineseG2PEngine`, `KoreanG2PEngine`, `SpanishG2PEngine`, `FrenchG2PEngine`, and `PortugueseG2PEngine` perform stateless conversions,
so a single instance can safely be called from multiple threads.

`MultilingualG2PEngine` protects its internal Japanese engine with a `lock`,
so it can safely be called from multiple threads. However, Japanese text conversions are serialized.

## License

| Package | License | Notes |
|---------|---------|-------|
| **DotNetG2P** | [Apache-2.0](LICENSE) | Core library |
| **DotNetG2P.MeCab** | [Apache-2.0](LICENSE) | Built-in MeCab engine |
| **DotNetG2P.English** | [Apache-2.0](LICENSE) | English G2P engine |
| **DotNetG2P.Chinese** | [Apache-2.0](LICENSE) | Chinese G2P engine |
| **DotNetG2P.Korean** | [Apache-2.0](LICENSE) | Korean G2P engine |
| **DotNetG2P.Spanish** | [Apache-2.0](LICENSE) | Spanish G2P engine |
| **DotNetG2P.French** | [Apache-2.0](LICENSE) | French G2P engine |
| **DotNetG2P.Portuguese** | [Apache-2.0](LICENSE) | Portuguese G2P engine |
| **DotNetG2P.Multilingual** | [Apache-2.0](LICENSE) | Multilingual G2P engine (Japanese-English-Chinese-Korean-Spanish-French-Portuguese) |

All components are available under the **Apache-2.0 License**.
For third-party component licenses, see the [NOTICE](NOTICE) file.
