# DotNetG2P.French

French grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- Rule-based IPA conversion with nasal vowels, semivowels, and silent-letter handling
- Metropolitan and Conservative dialect options
- Exception dictionary support and optional allophone processing
- IPA, X-SAMPA, phoneme-list, and batch conversion APIs

## Quick Start

```csharp
using DotNetG2P.French;

using var engine = new FrenchG2PEngine();

string ipa = engine.ToIPA("bonjour");
string xsampa = engine.ToXSampa("merci");

using var allophones = new FrenchG2PEngine(
    new FrenchG2POptions(enableAllophones: true));

string detailed = allophones.ToIPA("autre");
```

## Thread Safety

`FrenchG2PEngine` is stateless after construction and can be shared across threads for conversion calls.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
