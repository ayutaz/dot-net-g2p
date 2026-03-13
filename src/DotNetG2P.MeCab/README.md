# DotNetG2P.MeCab

Pure C# MeCab-compatible tokenizer for DotNetG2P and Unity.

## What It Includes

- `ITokenizer` implementation used by the Japanese `DotNetG2P` engine
- Double-array trie and Viterbi-based tokenization in managed code
- No native binary dependency

## Quick Start

```csharp
using DotNetG2P.MeCab;

using var tokenizer = new MeCabTokenizer();
var tokens = tokenizer.Tokenize("こんにちは");
```

## Notes

- This package tokenizes Japanese text. Pair it with `DotNetG2P` when you need phoneme conversion.
- `MeCabTokenizer` requires an OpenJTalk-compatible `naist-jdic` dictionary directory.

## Thread Safety

`MeCabTokenizer` is not thread-safe.
Do not call `Tokenize` concurrently on the same instance.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
