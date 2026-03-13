# DotNetG2P.Chinese

Mandarin Chinese grapheme-to-phoneme conversion for .NET and Unity.

## What It Includes

- Dictionary-based character and phrase lookup
- Automatic polyphone resolution with phrase overrides
- Tone sandhi support for third tone and `一` / `不`
- Pinyin, IPA, and Zhuyin output APIs with batch conversion helpers

## Quick Start

```csharp
using DotNetG2P.Chinese;

using var engine = new ChineseG2PEngine();

string pinyin = engine.ToPinyin("你好世界");
string toneNumbers = engine.ToPinyin("银行", PinyinStyle.ToneNumber);
string ipa = engine.ToIPA("你好");
string zhuyin = engine.ToZhuyin("你好");
```

## Thread Safety

`ChineseG2PEngine` is stateless after construction and can be shared across threads for conversion calls.

## License And Notice

- License: Apache-2.0
- Repository: https://github.com/ayutaz/dot-net-g2p
