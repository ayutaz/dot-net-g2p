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

## 機能

- 5フェーズG2P規則（トリグラフ/ダイグラフ → 子音軟化 → 母音変換 → そり舌化 → 語末処理）
- ピッチアクセント予測（Accent 1/2）
- Central / FinlandSwedish 方言対応
- IPA / X-SAMPA / PUA / Prosody 出力
- テキスト正規化（11段階）
- 例外辞書（500+語）
- テスト399件

## ステータス

Sw1-Sw4完了。
