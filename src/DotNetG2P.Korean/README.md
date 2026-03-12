# DotNetG2P.Korean

Hangul-first Korean grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- Rule-based Korean pronunciation conversion for modern Hangul
- `ToPhonemes` and `ToJamo` APIs
- Standard-pronunciation-oriented rule coverage for neutralization, resyllabification, tensification, nasalization, liquidization, and `ㅎ`-driven changes
- Exact-match exception dictionary for lexical overrides
- Lightweight normalization for whitespace, punctuation, and compatibility Unicode forms

## Quick Start

```csharp
using DotNetG2P.Korean;

using var engine = new KoreanG2PEngine();

string phonemes = engine.ToPhonemes("좋다");
// => "ㅈ ㅗ ㅌ ㅏ"

string jamo = engine.ToJamo("한글");
// => "ㅎㅏㄴ ㄱㅡㄹ"

using var colloquial = new KoreanG2PEngine(
    new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial));

string uiVariant = colloquial.Analyze("나의").ToHangulString();
// => "나에"
```

## Options

- `EnableUnicodeNormalization`: apply compatibility Unicode normalization before conversion
- `EnableTextNormalization`: collapse repeated whitespace and remove punctuation-oriented noise for Hangul-first input
- `EnableExceptionDictionary`: enable exact lexical pronunciation overrides
- `PreserveNonHangul`: keep normalized non-Hangul characters in the intermediate output
- `UiVariationMode`: choose `Standard` or `Colloquial` handling for supported `의`-variation entries

## Benchmarks And Tests

- Benchmark datasets: `g2pk_parity`, `official_gold`, `weak_rules`
- Current seed status as of 2026-03-12:
  - `g2pk_parity`: `8/8`
  - `official_gold`: `21/21`
  - `weak_rules`: `14/14`
- Korean regression status as of 2026-03-12: `178 passed, 1 skipped`
- Targeted regression command:

```bash
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Korean --no-restore
```

- Performance guard command:

```bash
dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter FullyQualifiedName~KoreanPerformanceTests --no-build --no-restore -m:1
```

- External corpus gate:

```bash
DOTNETG2P_KOREAN_EXTERNAL_CORPUS_PATHS=tests/TestData/KoreanG2P/official_gold.tsv DOTNETG2P_KOREAN_EXTERNAL_MIN_CASES=20 DOTNETG2P_KOREAN_EXTERNAL_ACCURACY_THRESHOLD=1.0 dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter FullyQualifiedName~KoreanExternalBenchmarkTests --no-build --no-restore -m:1
```

## Thread Safety

`KoreanG2PEngine` is stateless after construction and can be shared across threads for conversion calls.
Dispose the engine when you are done with it to follow the same lifetime pattern as the other package engines.

## Known Limitations

- The package is `Hangul-first`. Full Hanja conversion is out of scope for v1.
- Context-sensitive number reading and English-to-Hangul expansion are not implemented yet.
- Some compound-boundary pronunciations still rely on exact lexical exceptions instead of a mandatory morph analyzer.

## License And Notice

- License: Apache-2.0
- Third-party notice: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
