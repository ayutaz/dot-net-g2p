# DotNetG2P.Spanish

Spanish grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- Rule-based IPA conversion with syllabification and stress assignment
- Castilian and Latin American dialect options
- Optional allophone handling and text normalization support
- IPA, X-SAMPA, structured phoneme, and batch conversion APIs

## Quick Start

```csharp
using DotNetG2P.Spanish;

using var engine = new SpanishG2PEngine();

string ipa = engine.ToIPA("vergüenza");
string xsampa = engine.ToXSampa("guion");

using var allophones = new SpanishG2PEngine(
    new SpanishG2POptions(enableAllophones: true));

string detailed = allophones.ToIPA("uva");
```

## Thread Safety

`SpanishG2PEngine` is stateless after construction and can be shared across threads for conversion calls.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
