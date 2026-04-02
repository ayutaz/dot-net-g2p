# DotNetG2P.Multilingual

Multilingual grapheme-to-phoneme conversion for .NET and Unity with automatic segment routing.

## Supported Languages

- Japanese
- English
- Chinese
- Korean
- Spanish
- French
- Portuguese
- Swedish

## Quick Start

```csharp
using DotNetG2P.Korean;
using DotNetG2P.Multilingual;

using var engine = new MultilingualG2PEngine();

string phonemes = engine.ToPhonemes("今日は안녕하세요 hello");
// Japanese -> Japanese phonemes, Korean -> Korean phonemes, English -> ARPAbet

var options = new MultilingualG2POptions(
    defaultLatinLanguage: Language.French,
    koreanOptions: new KoreanG2POptions(
        uiVariationMode: KoreanUiVariationMode.Colloquial));

using var custom = new MultilingualG2PEngine(options);
var segments = custom.ToSegments("나의 bonjour 世界");
```

## Requirements

- Japanese-capable constructors require a `naist-jdic` dictionary, because Japanese routing uses `DotNetG2P` + `DotNetG2P.MeCab`.
- Korean segments are routed to `DotNetG2P.Korean` automatically when Hangul syllables or Jamo are detected.

## Key Options

- `DefaultLatinLanguage`: choose the default routing target for Latin-script segments
- `DefaultCjkLanguage`: choose the fallback target for ambiguous CJK ideograph segments
- `KoreanOptions`: pass Korean-specific normalization and pronunciation options through to `KoreanG2PEngine`

## Validation

- Current multilingual regression status: `448 passed`
- `MultilingualPerformanceTests`: `8 passed`
- `MultilingualKoreanPerformanceTests`: `2 passed`

```bash
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Multilingual --no-build --no-restore -m:1
```

## Thread Safety

`MultilingualG2PEngine` is safe to call from multiple threads.
Its internal Japanese engine is protected with a lock, while English, Chinese, Korean, Spanish, French, Portuguese, and Swedish conversions are stateless.

## Known Limitations

- Mixed-script Korean words that depend on full English-to-Hangul expansion are not handled yet.
- Hanja-heavy Korean text still falls back to the existing CJK routing heuristics.
- Japanese conversion still depends on an external `naist-jdic` install.

## License And Notice

- License: Apache-2.0
- Third-party notice: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
