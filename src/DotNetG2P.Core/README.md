# DotNetG2P

Japanese grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- OpenJTalk-compatible NJD pipeline implemented in pure C#
- Multiple outputs: phonemes, kana, prosody markers, accent phrases, HTS labels, and prosody features
- Batch conversion APIs for the main output forms
- Immutable `G2POptions` for controlling normalization, digit reading, accent handling, and unvoiced vowels

## Quick Start

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

using var tokenizer = new MeCabTokenizer();
using var engine = new G2PEngine(tokenizer);

string phonemes = engine.ToPhonemes("こんにちは");
string kana = engine.ToKana("音声合成");
var labels = engine.ToFullContextLabels("こんにちは");
```

## Dependencies

- Install `DotNetG2P.MeCab` alongside this package for the default tokenizer implementation
- Japanese conversion requires a `naist-jdic` dictionary install

## Thread Safety

`G2PEngine` is not thread-safe.
Create one engine instance per thread or request scope.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
