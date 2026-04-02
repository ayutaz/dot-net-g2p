# DotNetG2P.Swedish

スウェーデン語G2P（Grapheme-to-Phoneme）ライブラリ。ルールベースのIPA変換、音節分割、ストレス付与、ピッチアクセント予測、Central/Finland Swedish方言対応。

## インストール

```bash
dotnet add package DotNetG2P.Swedish
```

## 基本的な使い方

```csharp
using DotNetG2P.Swedish;

using var engine = new SwedishG2PEngine();
string ipa = engine.ToIPA("hund");
```

## ステータス

現在開発中（Sw1）。
