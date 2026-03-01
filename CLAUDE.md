# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

C#/.NET（Unity対応）向けの日本語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk/pyopenjtalkの処理パイプラインをC#でネイティブに再実装し、Pythonやネイティブバイナリへの依存を排除する。

## 進捗状況

- **M1（最小動作プロトタイプ）**: 完了
  - `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作確認済み
  - naist-jdic辞書によるフルパイプライン（形態素解析→NJD→音素変換）が動作
- **M2（NJD処理パイプライン完成）**: 完了
  - NJDパイプライン6段階すべて実装（TextNormalizer→SetPronunciation→DigitSequence/SetDigit→SetAccentPhrase→SetAccentType→SetUnvoicedVowel）
  - 無声音化（`s U k i`）、アクセント句結合、数字読み変換が動作
  - G2POptionsによる各処理段階のON/OFF制御、Analyze APIを追加
- **M3（出力形式の充実）**: 完了
  - ToProsody()（ESPnet韻律記号付き出力）、ToAccentPhrases()（VOICEVOX互換）、ToFullContextLabels()（HTSフルコンテキストラベル）を追加
  - JPCommon階層モデル（JPUtterance→JPBreathGroup→JPAccentPhrase→JPWord→JPMora→JPPhoneme）を実装
  - WordAttr（POS/CType/CForm→ID変換テーブル、jpreprocess word_attr.rs準拠）を実装
  - 全310テスト成功
- **M4〜M6**: 未着手（docs/roadmap.md 参照）

## ビルド・実行

```bash
# ビルド
dotnet build DotNetG2P.slnx

# テスト
dotnet test DotNetG2P.slnx

# コンソールサンプル実行（辞書なし: MoraMappingのみ確認）
dotnet run --project samples/DotNetG2P.Console/DotNetG2P.Console.csproj

# コンソールサンプル実行（辞書あり: フルG2P）
dotnet run --project samples/DotNetG2P.Console/DotNetG2P.Console.csproj -- <naist-jdic辞書パス>
```

## プロジェクト構成

```
DotNetG2P.slnx                          # ソリューションファイル（.NET 10 .slnx形式）
├── src/
│   ├── DotNetG2P.Core/                  # コアライブラリ（.NET Standard 2.1）
│   │   ├── Models/                      # データ構造
│   │   │   ├── Phoneme.cs               # Consonant enum (35種) + Vowel enum (10種)
│   │   │   ├── MoraKind.cs              # MoraKind enum (~165種) + カタカナ変換
│   │   │   ├── POS.cs                   # POSType enum (14種) + POS class (品詞4フィールド)
│   │   │   ├── Mora.cs                  # Mora readonly struct (子音+母音+種類)
│   │   │   ├── Pronunciation.cs         # Pronunciation class (モーラ列+アクセント位置)
│   │   │   ├── WordDetails.cs           # WordDetails class (品詞・活用・読み)
│   │   │   ├── WordEntry.cs             # WordEntry class (表層形+詳細+アクセント情報)
│   │   │   ├── NjdNode.cs              # NjdNode class (NJD処理中間表現)
│   │   │   └── AccentPhrase.cs          # AccentPhrase class (VOICEVOX互換)
│   │   ├── Tokenizer/                   # 形態素解析抽象化
│   │   │   ├── ITokenizer.cs            # ITokenizer interface
│   │   │   └── IToken.cs               # IToken interface (naist-jdic 15フィールド)
│   │   ├── NJD/                         # NJD処理（6段階パイプライン）
│   │   │   ├── SetPronunciation.cs      # 1. 発音設定（完全版5段階処理）
│   │   │   ├── DigitSequence.cs         # 2a. 数字列検出・グループ化
│   │   │   ├── DigitLut.cs              # 2b. 数字読みLUTテーブル
│   │   │   ├── SetDigit.cs              # 2c. 数字読み変換メインロジック
│   │   │   ├── SetAccentPhrase.cs       # 3. アクセント句結合（18ルール）
│   │   │   ├── SetAccentType.cs         # 4. アクセント結合型（C1-C5, F1-F5, P系列）
│   │   │   └── SetUnvoicedVowel.cs      # 5. 無声音化（6ルール）
│   │   ├── TextNormalization/           # テキスト正規化
│   │   │   └── TextNormalizer.cs        # 全角/半角変換、濁点結合
│   │   ├── PhonemeConverter/            # 音素変換
│   │   │   ├── MoraMapping.cs           # カタカナ⇔音素マッピング (162種)
│   │   │   ├── AccentPhraseConverter.cs # VOICEVOX互換アクセント句変換
│   │   │   └── ProsodyExtractor.cs      # ESPnet韻律記号付き出力
│   │   ├── JPCommon/                    # HTSフルコンテキストラベル生成
│   │   │   ├── Models.cs               # 階層モデル (JPUtterance/JPBreathGroup/JPAccentPhrase/JPWord/JPMora/JPPhoneme)
│   │   │   ├── JPCommonBuilder.cs       # NjdNode列→JPCommon階層構築
│   │   │   ├── FullContextLabel.cs      # HTSフルコンテキストラベル生成
│   │   │   └── WordAttr.cs             # POS/CType/CForm→ID変換テーブル (jpreprocess準拠)
│   │   ├── G2PEngine.cs                # メインAPI (ToPhonemes, ToKana, ToProsody, ToAccentPhrases, ToFullContextLabels, Analyze)
│   │   └── G2POptions.cs               # 処理オプション（各段階ON/OFF）
│   │
│   └── DotNetG2P.NMeCab/               # NMeCabアダプター（LGPL依存）
│       ├── DotNetG2P.NMeCab.csproj      # LibNMeCab 0.10.2 参照
│       └── NMeCabTokenizer.cs           # ITokenizer実装
│
├── tests/
│   └── DotNetG2P.Tests/                 # xUnit テストプロジェクト (net8.0)
│       ├── DotNetG2P.Tests.csproj
│       ├── G2PEngineApiTests.cs         # G2PEngine API統合テスト
│       ├── Models/                      # モデルテスト
│       │   ├── NjdNodeTests.cs
│       │   └── PronunciationTests.cs
│       ├── NJD/                         # NJD処理テスト
│       │   └── SetUnvoicedVowelTests.cs
│       ├── TextNormalization/           # テキスト正規化テスト
│       │   └── TextNormalizerTests.cs
│       ├── PhonemeConverter/            # 音素変換テスト
│       │   ├── MoraMappingTests.cs
│       │   ├── AccentPhraseConverterTests.cs
│       │   └── ProsodyExtractorTests.cs
│       ├── JPCommon/                    # JPCommonテスト
│       │   ├── JPCommonBuilderTests.cs
│       │   ├── WordAttrTests.cs
│       │   └── FullContextLabelTests.cs
│       └── Integration/                # 統合テスト
│           └── G2PPipelineTests.cs
│
└── samples/
    └── DotNetG2P.Console/               # コンソールサンプル (net8.0)
        ├── DotNetG2P.Console.csproj
        └── Program.cs
```

## 背景・動機

- OpenJTalkやpyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難
- 既存のC#向け日本語G2Pライブラリは存在しない
- Unity（ゲーム・VTuber・音声合成等）での日本語TTS前処理として需要がある

## アーキテクチャ方針

OpenJTalkの処理パイプラインに準拠した4段階処理:

1. **形態素解析**: LibNMeCab 0.10.2 によるMeCab互換解析（ITokenizer抽象化で将来差し替え可能）
2. **NJD処理（日本語ルール処理）**: 読み生成、数字読み変換、アクセント句結合、アクセント結合、無声音化、長音化
3. **音素変換**: カタカナ読み → 音素列（例: `コンニチワ` → `k o N n i ch i w a`）
4. **アクセント情報付与**（オプション）: モーラ数・アクセント核位置の出力

### 日本語音素体系

| 種別 | 音素 |
|------|------|
| 母音 | a, i, u, e, o (+ 無声母音 A, I, U, E, O) |
| 半母音 | y, w |
| 子音 | k, g, s, z, t, d, n, h, b, p, m, r, ch, sh, j, f, ts, ky, gy, ny, hy, by, py, my, ry, v, dy, ty, gw, kw |
| 特殊 | N（撥音）, cl（促音）, -（長音） |

### 辞書

OpenJTalk用のnaist-jdic辞書フォーマット（IPADIC + アクセント情報2フィールド拡張）を使用:
- フィールド13: `アクセント核位置/モーラ数`（例: `3/4`）
- フィールド14: アクセント結合タイプ（C1〜C5）

## 技術スタック

- **言語**: C#
- **ターゲット**: .NET Standard 2.1（Unity 2021.2+互換）
- **形態素解析**: LibNMeCab 0.10.2（LGPL、将来自前実装で置換予定）
- **辞書**: naist-jdic（BSD License）
- **テスト**: xUnit 2.5.3 (net8.0)
- **ソリューション形式**: .slnx（.NET 10）

## 開発言語

コード内コメント・ドキュメント・コミットメッセージ・PR・Issueはすべて**日本語**で記述する。
