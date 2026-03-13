# DotNetG2P.Portuguese

Portuguese grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- Rule-based IPA conversion with syllabification and stress assignment
- Brazilian and European dialect options
- Optional allophone handling, normalization, and exception-dictionary support
- IPA, X-SAMPA, phoneme-list, and batch conversion APIs

## Quick Start

```csharp
using DotNetG2P.Portuguese;

using var engine = new PortugueseG2PEngine();

string ipa = engine.ToIPA("obrigado");
string xsampa = engine.ToXSampa("cidade");

using var european = new PortugueseG2PEngine(
    new PortugueseG2POptions(dialect: PortugueseDialect.European));

string epIpa = european.ToIPA("coração");
```

## Thread Safety

`PortugueseG2PEngine` is stateless after construction and can be shared across threads for conversion calls.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
