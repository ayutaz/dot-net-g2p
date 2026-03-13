# DotNetG2P.English

English grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- CMU Dictionary lookup for in-vocabulary words
- Flite LTS fallback for out-of-vocabulary estimation
- ARPAbet, IPA, and X-SAMPA output APIs
- Batch conversion APIs and structured phoneme lookup helpers

## Quick Start

```csharp
using DotNetG2P.English;

using var engine = new EnglishG2PEngine();

string phonemes = engine.ToPhonemes("hello world");
string ipa = engine.ToIPA("dictionary");
string xsampa = engine.ToXSampa("phoneme");
```

## Thread Safety

`EnglishG2PEngine` is stateless after construction and can be shared across threads for conversion calls.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
