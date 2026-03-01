# DotNetG2P

[![CI](https://github.com/dotnetg2p/DotNetG2P/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnetg2p/DotNetG2P/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

C#/.NET向け日本語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk互換のルールベースG2Pパイプラインをc#でネイティブに再実装。
Pythonやネイティブバイナリへの依存なしに、.NETおよびUnityから日本語テキストを音素列に変換できます。

## 特徴

- **OpenJTalk互換**: NJD処理6段階（発音生成→数字読み→アクセント句結合→アクセント結合→無声音化）を完全実装
- **5種類の出力形式**: 音素列、カタカナ、韻律記号付き、VOICEVOX互換AccentPhrase、HTSフルコンテキストラベル
- **Unity対応**: .NET Standard 2.1（Unity 2021.2+）、IL2CPP/AOT安全設計
- **812テスト**: pyopenjtalk比較テスト、piper-plus移植テスト、NJD単体テストで品質保証
- **ITokenizer抽象化**: 形態素解析エンジンを差し替え可能

## インストール

```bash
dotnet add package DotNetG2P
dotnet add package DotNetG2P.NMeCab
```

別途naist-jdic辞書が必要です（下記「辞書セットアップ」参照）。

## クイックスタート

```csharp
using DotNetG2P;
using DotNetG2P.NMeCab;

using var tokenizer = new NMeCabTokenizer("/path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);

// 音素列
string phonemes = engine.ToPhonemes("こんにちは");
// => "k o N n i ch i w a"

// カタカナ
string kana = engine.ToKana("今日は天気がいいですね");
// => "キョーワテンキガイーデスネ"

// 韻律記号付き（ESPnet方式）
string prosody = engine.ToProsody("こんにちは");
// => "^ k o [ N n i ch i w a $"

// VOICEVOX互換アクセント句
var phrases = engine.ToAccentPhrases("こんにちは");

// HTSフルコンテキストラベル
var labels = engine.ToFullContextLabels("こんにちは");
```

## API一覧

| メソッド | 戻り値型 | 出力例 | 用途 |
|---------|---------|--------|------|
| `ToPhonemes(text)` | `string` | `"k o N n i ch i w a"` | 基本音素列 |
| `ToKana(text)` | `string` | `"コンニチワ"` | カタカナ読み |
| `ToProsody(text)` | `string` | `"^ k o [ N n i ch i w a $"` | ESPnet韻律記号付き |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX互換構造体 | 音声合成前処理 |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTSラベル | HMM/DNN音声合成 |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | NJDノード列 | デバッグ・拡張 |

## 辞書セットアップ

DotNetG2Pはnaist-jdic辞書（OpenJTalk用MeCab辞書）を使用します。

### 辞書の入手方法

1. [Open JTalk](https://open-jtalk.sourceforge.net/)のダウンロードページからnaist-jdic辞書をダウンロード
2. または、pyopenjtalkやOpenJTalkに同梱の辞書ディレクトリを使用

辞書ディレクトリには以下のファイルが必要です:
- `sys.dic` - システム辞書
- `matrix.bin` - 遷移コスト行列
- `char.bin` - 文字カテゴリ定義
- `unk.dic` - 未知語テンプレート

## G2POptions

```csharp
var options = new G2POptions
{
    EnableTextNormalization = true,  // テキスト正規化（デフォルト: true）
    EnableDigitProcessing = true,    // 数字読み変換（デフォルト: true）
    EnableAccentPhrase = true,       // アクセント句結合（デフォルト: true）
    EnableAccentType = true,         // アクセント結合型（デフォルト: true）
    EnableUnvoicedVowel = true,      // 無声音化（デフォルト: true）
};
using var engine = new G2PEngine(tokenizer, options);
```

## 処理パイプライン

```
テキスト入力
  → TextNormalizer（全角/半角変換、濁点結合）
  → ITokenizer.Tokenize()（形態素解析）
  → NjdNode.FromTokens()
  → SetPronunciation（発音生成）
  → DigitSequence + SetDigit（数字読み変換）
  → SetAccentPhrase（アクセント句結合）
  → SetAccentType（アクセント結合型）
  → SetUnvoicedVowel（無声音化）
  → 出力形式変換
```

## ビルド

```bash
dotnet build DotNetG2P.slnx
dotnet test DotNetG2P.slnx
```

## ライセンス

- **DotNetG2P** (コアライブラリ): MIT License
- **DotNetG2P.NMeCab** (NMeCabアダプター): LGPL-2.1-or-later（LibNMeCab依存のため）

将来的にPhase 6で独自MeCab実装に置き換え、全コンポーネントをMITライセンスにする予定です。

## 関連プロジェクト

- [OpenJTalk](https://open-jtalk.sourceforge.net/) - 日本語TTS（C/C++）
- [pyopenjtalk](https://github.com/r9y9/pyopenjtalk) - OpenJTalkのPythonラッパー
- [jpreprocess](https://github.com/jpreprocess/jpreprocess) - OpenJTalkのRust再実装（本プロジェクトの設計参考）
- [VOICEVOX](https://voicevox.hiroshiba.jp/) - 日本語音声合成ソフトウェア
