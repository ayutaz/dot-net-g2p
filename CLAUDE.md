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
- **M4（テスト・品質保証）**: 完了
  - 502件の新規テストを追加（合計1,600超テスト）
  - NJD各処理の単体テスト（SetPronunciation/SetAccentPhrase/SetAccentType/DigitSequence/SetDigit）
  - MoraMapping全165パターン検証、piper-plusテスト移植（87件）、pyopenjtalk比較テスト（20件）
  - エッジケーステスト（記号/英字/空文字列/長文/混在スクリプト）
- **M5（パッケージング）**: 完了
  - NuGetパッケージ設定（Directory.Build.props、Core/NMeCab csproj更新、`dotnet pack`で.nupkg生成確認済み）
  - GitHub Actions CI/CD（ci.yml: push/PR時ビルド・テスト・パック、release.yml: NuGet push + GitHub Release）
  - UPMパッケージ構造（package.json、DotNetG2P.asmdef、DotNetG2P.NMeCab.asmdef）
  - LICENSE（MIT）、README.md（126行）、.editorconfig、.gitattributes
- **M6（独自MeCabエンジン）**: 完了
  - 純C#でMeCab互換形態素解析エンジンを実装（`DotNetG2P.MeCab`パッケージ）
  - LibNMeCab（LGPL-2.1）依存を排除し完全MIT化を達成
  - DoubleArrayTrie + Viterbiデコーダ + 未知語処理の完全実装
  - NMeCabTokenizerと100+文で全15フィールド出力一致を検証済み
  - NuGet (`DotNetG2P.MeCab`) + UPM (`com.dotnetg2p.mecab`) パッケージ対応

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
├── Directory.Build.props                # NuGet共通メタデータ
├── LICENSE                              # MIT License
├── README.md                            # プロジェクトREADME（126行）
├── .editorconfig                        # コーディング規約
├── .gitattributes                       # Git属性設定
├── .github/workflows/                   # GitHub Actions
│   ├── ci.yml                           # CI（push/PR: ビルド・テスト・パック）
│   └── release.yml                      # リリース（NuGet push + GitHub Release）
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
│   │   ├── G2POptions.cs               # 処理オプション（各段階ON/OFF）
│   │   ├── package.json                # UPM パッケージ定義 (com.dotnetg2p.core)
│   │   └── DotNetG2P.asmdef            # Unity Assembly Definition
│   │
│   ├── DotNetG2P.NMeCab/               # NMeCabアダプター（LGPL依存）
│   │   ├── DotNetG2P.NMeCab.csproj      # LibNMeCab 0.10.2 参照
│   │   ├── NMeCabTokenizer.cs           # ITokenizer実装
│   │   └── DotNetG2P.NMeCab.asmdef      # Unity Assembly Definition
│   │
│   └── DotNetG2P.MeCab/                # 独自MeCabエンジン（MIT、外部依存なし）
│       ├── DotNetG2P.MeCab.csproj       # .NET Standard 2.1、DotNetG2P.Core参照のみ
│       ├── MeCabTokenizer.cs            # ITokenizer実装（公開API）
│       ├── Dictionary/                  # 辞書読み込み層
│       │   ├── DictionaryHeader.cs      # 72バイトヘッダパーサ
│       │   ├── DicToken.cs              # トークン構造体（16バイト）
│       │   ├── SystemDictionary.cs      # sys.dic読み込み
│       │   ├── ConnectionMatrix.cs      # matrix.bin読み込み（連接コスト行列）
│       │   ├── CharProperty.cs          # char.bin読み込み（文字カテゴリ）
│       │   ├── UnknownDictionary.cs     # unk.dic読み込み（未知語テンプレート）
│       │   └── DictionaryBundle.cs      # 全辞書ファイル集約管理
│       ├── Trie/                        # DoubleArray Trie
│       │   ├── DoubleArrayTrie.cs       # NMeCab互換 共通接頭辞検索
│       │   └── Utf8CharMap.cs           # UTF-8バイト⇔char オフセット変換
│       ├── Lattice/                     # ラティス＋Viterbi
│       │   ├── LatticeNode.cs           # ラティスノード
│       │   ├── LatticeBuilder.cs        # Trie検索+未知語生成→ラティス構築
│       │   └── ViterbiDecoder.cs        # 前向きパス+後ろ向きトレース
│       ├── DotNetG2P.MeCab.asmdef       # Unity Assembly Definition
│       └── package.json                 # UPM パッケージ定義 (com.dotnetg2p.mecab)
│
├── tests/
│   ├── TestData/                        # テストデータ
│   │   ├── expected_phonemes.json       # pyopenjtalk期待値データ（18件）
│   │   └── generate_expected.py         # テストデータ生成スクリプト
│   └── DotNetG2P.Tests/                 # xUnit テストプロジェクト (net8.0, 1,600超テスト)
│       ├── DotNetG2P.Tests.csproj
│       ├── G2PEngineApiTests.cs         # G2PEngine API統合テスト
│       ├── Models/                      # モデルテスト
│       │   ├── NjdNodeTests.cs
│       │   └── PronunciationTests.cs
│       ├── NJD/                         # NJD処理テスト
│       │   ├── SetPronunciationTests.cs # 発音設定テスト（25件）
│       │   ├── SetAccentPhraseTests.cs  # アクセント句結合テスト（37件）
│       │   ├── SetAccentTypeTests.cs    # アクセント結合型テスト（39件）
│       │   ├── DigitSequenceTests.cs    # 数字列検出テスト（14件）
│       │   ├── SetDigitTests.cs         # 数字読み変換テスト（32件）
│       │   ├── DigitReadingTests.cs     # 数字読み網羅テスト（25件、辞書依存）
│       │   └── SetUnvoicedVowelTests.cs
│       ├── TextNormalization/           # テキスト正規化テスト
│       │   └── TextNormalizerTests.cs
│       ├── PhonemeConverter/            # 音素変換テスト
│       │   ├── MoraMappingTests.cs
│       │   ├── MoraMappingFullTests.cs  # 全165パターン検証（166件）
│       │   ├── AccentPhraseConverterTests.cs
│       │   └── ProsodyExtractorTests.cs
│       ├── JPCommon/                    # JPCommonテスト
│       │   ├── JPCommonBuilderTests.cs
│       │   ├── WordAttrTests.cs
│       │   └── FullContextLabelTests.cs
│       ├── MeCab/                       # MeCabエンジンテスト
│       │   ├── MeCabTokenizerTests.cs   # 基本動作テスト（~30件）
│       │   ├── TokenizerComparisonTests.cs # NMeCab出力一致テスト（100+文×3）
│       │   ├── G2PComparisonTests.cs    # G2Pパイプライン比較テスト（20件×6）
│       │   ├── Utf8CharMapTests.cs      # UTF-8オフセット変換テスト
│       │   ├── DictionaryErrorTests.cs  # エラーハンドリングテスト
│       │   ├── MeCabIndependentTests.cs # 独立仕様検証テスト（21件）
│       │   └── PerformanceTests.cs      # パフォーマンステスト（5件）
│       └── Integration/                # 統合テスト
│           ├── G2PPipelineTests.cs
│           ├── EdgeCaseTests.cs         # エッジケーステスト（~57件）
│           ├── PiperPlusTests.cs        # piper-plus移植テスト（87件）
│           └── PyOpenJTalkComparisonTests.cs  # pyopenjtalk比較テスト（20件）
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

1. **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、MIT）をデフォルト使用。互換性維持のためLibNMeCab版（`DotNetG2P.NMeCab`、LGPL）も利用可能（ITokenizer抽象化）
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
- **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、MIT、外部依存なし）。互換オプションとしてLibNMeCab 0.10.2（`DotNetG2P.NMeCab`、LGPL）
- **辞書**: naist-jdic（BSD License）
- **テスト**: xUnit 2.5.3 (net8.0)
- **パッケージング**: NuGet (`DotNetG2P`, `DotNetG2P.NMeCab`, `DotNetG2P.MeCab`) + UPM (`com.dotnetg2p.core`, `com.dotnetg2p.mecab`)
- **CI/CD**: GitHub Actions (ci.yml, release.yml)
- **ソリューション形式**: .slnx（.NET 10）

## 開発言語

コード内コメント・ドキュメント・コミットメッセージ・PR・Issueはすべて**日本語**で記述する。
