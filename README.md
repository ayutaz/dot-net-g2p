# DotNetG2P

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

C#/.NET向け日本語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk互換のルールベースG2Pパイプラインを C# でネイティブに再実装し、Pythonやネイティブバイナリへの依存なしに日本語テキストを音素列に変換します。

```csharp
using var engine = new G2PEngine(new MeCabTokenizer("/path/to/naist-jdic"));

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"
```

## 目次

- [特徴](#特徴)
- [インストール](#インストール)
- [クイックスタート](#クイックスタート)
- [API リファレンス](#api-リファレンス)
- [処理パイプライン](#処理パイプライン)
- [辞書の準備](#辞書の準備)
- [オプション設定](#オプション設定)
- [ビルド](#ビルド)
- [ロードマップ](#ロードマップ)
- [ライセンス](#ライセンス)
- [謝辞・関連プロジェクト](#謝辞関連プロジェクト)
- [Contributing](#contributing)

## 特徴

- **完全MIT** — 独自MeCabエンジン（`DotNetG2P.MeCab`）により外部LGPL依存を排除し、全コンポーネントMITライセンスで利用可能
- **OpenJTalk互換** — NJD処理6段階（発音生成→数字読み→アクセント句結合→アクセント結合→無声音化）を完全実装
- **5種類の出力形式** — 音素列 / カタカナ / 韻律記号付き / VOICEVOX互換AccentPhrase / HTSフルコンテキストラベル
- **Unity対応** — .NET Standard 2.1（Unity 2021.2+）ターゲット、IL2CPP/AOT安全設計
- **ITokenizer抽象化** — 形態素解析エンジンを差し替え可能。デフォルトは独自MeCabTokenizer（MIT）、互換オプションとしてNMeCab（LGPL）も利用可能
- **1,600超テストで品質保証** — pyopenjtalk比較テスト、piper-plus移植テスト、NJD単体テスト、MeCabエンジン一致検証

## インストール

### NuGet

```bash
# コアライブラリ + 独自MeCabエンジン（推奨・完全MIT）
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab

# 互換オプション: LibNMeCab版（LGPL-2.1）
# dotnet add package DotNetG2P.NMeCab
```

### パッケージ構成

| パッケージ | ライセンス | 説明 |
|-----------|-----------|------|
| `DotNetG2P` | MIT | コアライブラリ（G2Pエンジン、NJD処理、音素変換） |
| `DotNetG2P.MeCab` | MIT | 独自MeCabエンジン（**推奨**、外部依存なし） |
| `DotNetG2P.NMeCab` | LGPL-2.1 | LibNMeCab版アダプター（互換オプション） |

### Unity (UPM)

Unity Package Managerの **Add package from git URL** で以下を追加:

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
```

> **Note:** 別途 naist-jdic 辞書が必要です。詳細は[辞書の準備](#辞書の準備)を参照してください。

## クイックスタート

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;  // 独自MeCabエンジン（推奨・MIT）

// 1. エンジン初期化（辞書パスを指定）
using var tokenizer = new MeCabTokenizer("/path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);

// 2. テキストから音素列を取得
string phonemes = engine.ToPhonemes("今日は良い天気です");
// => "ky o o w a i i t e N k i d e s U"

// 3. カタカナ読みを取得
string kana = engine.ToKana("今日は良い天気です");
// => "キョーワイーテンキデス"

// 4. 韻律記号付き出力（ESPnet方式）
string prosody = engine.ToProsody("こんにちは");
// => "^ k o [ N n i ch i w a $"

// 5. VOICEVOX互換アクセント句
var phrases = engine.ToAccentPhrases("こんにちは");

// 6. HTSフルコンテキストラベル（HMM/DNN音声合成用）
var labels = engine.ToFullContextLabels("こんにちは");
```

> **互換オプション:** LibNMeCab版を使用する場合は `using DotNetG2P.NMeCab;` + `new NMeCabTokenizer(...)` に置き換えてください（LGPL-2.1が適用されます）。

## API リファレンス

### G2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | 空白区切り音素列 (`"k o N n i ch i w a"`) |
| `ToKana(text)` | `string` | カタカナ読み (`"コンニチワ"`) |
| `ToProsody(text)` | `string` | ESPnet韻律記号付き (`"^ k o [ N n i ch i w a $"`) |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX互換アクセント句構造体 |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTSフルコンテキストラベル |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | NJD処理後のノード列（デバッグ・拡張用） |

### 日本語音素体系

| 種別 | 音素 |
|------|------|
| 母音 | `a` `i` `u` `e` `o` （無声: `A` `I` `U` `E` `O`） |
| 子音 | `k` `g` `s` `z` `t` `d` `n` `h` `b` `p` `m` `r` `f` `v` |
| 拗音子音 | `ky` `gy` `sh` `j` `ch` `ts` `ny` `hy` `by` `py` `my` `ry` `dy` `ty` `kw` `gw` |
| 半母音 | `y` `w` |
| 特殊 | `N`（撥音） `cl`（促音） `-`（長音） `pau`（ポーズ） |

## 処理パイプライン

DotNetG2Pは[OpenJTalk](https://open-jtalk.sourceforge.net/)と同等の6段階NJD処理パイプラインを実装しています。

```
テキスト入力
  │
  ├─ TextNormalizer        全角/半角正規化、濁点結合
  ├─ ITokenizer.Tokenize   形態素解析（MeCabTokenizer + naist-jdic）
  ├─ SetPronunciation      辞書読み・フォールバック発音生成
  ├─ SetDigit              数字列検出・助数詞読み変換
  ├─ SetAccentPhrase       品詞パターンによるアクセント句結合（18ルール）
  ├─ SetAccentType         チェインルールによるアクセント結合型決定
  └─ SetUnvoicedVowel      無声母音化（6ルール）
  │
  ▼
  出力（音素列 / カタカナ / 韻律記号 / AccentPhrase / HTSラベル）
```

## 辞書の準備

DotNetG2Pは形態素解析にnaist-jdic辞書（OpenJTalk用MeCab辞書）を使用します。

### 入手方法

1. [Open JTalk公式サイト](https://open-jtalk.sourceforge.net/)からダウンロード
2. pyopenjtalkやOpenJTalkに同梱の辞書ディレクトリをそのまま使用

### 必要なファイル

辞書ディレクトリに以下の4ファイルが含まれている必要があります:

| ファイル | 内容 |
|---------|------|
| `sys.dic` | システム辞書 |
| `matrix.bin` | 遷移コスト行列 |
| `char.bin` | 文字カテゴリ定義 |
| `unk.dic` | 未知語テンプレート |

### Unity での配置

Unityでは `StreamingAssets` フォルダに辞書ファイルを配置し、`Application.streamingAssetsPath` を使用してパスを指定します。

```csharp
var dicPath = Path.Combine(Application.streamingAssetsPath, "naist-jdic");
using var tokenizer = new MeCabTokenizer(dicPath);
```

## オプション設定

`G2POptions` で各処理段階を個別にON/OFFできます（イミュータブル設計）。

```csharp
// 無声音化のみ無効にする例
var options = new G2POptions(enableUnvoicedVowel: false);
using var engine = new G2PEngine(tokenizer, options);
```

| パラメータ | デフォルト | 説明 |
|-----------|-----------|------|
| `enableTextNormalization` | `true` | テキスト正規化（全角/半角変換） |
| `enableDigitProcessing` | `true` | 数字読み変換・助数詞処理 |
| `enableAccentPhrase` | `true` | アクセント句結合（18ルール） |
| `enableAccentType` | `true` | アクセント結合型決定 |
| `enableUnvoicedVowel` | `true` | 無声母音化（6ルール） |

## ビルド

### 要件

- .NET SDK 9.0 以上

### コマンド

```bash
# ビルド
dotnet build DotNetG2P.slnx

# テスト実行
dotnet test DotNetG2P.slnx

# コンソールサンプル（辞書なし: MoraMappingのみ確認）
dotnet run --project samples/DotNetG2P.Console

# コンソールサンプル（辞書あり: フルG2P）
dotnet run --project samples/DotNetG2P.Console -- /path/to/naist-jdic
```

## ロードマップ

| フェーズ | 状態 | 内容 |
|---------|------|------|
| Phase 1: 基盤構築 | 完了 | データモデル、ITokenizer、NMeCabアダプター、MoraMapping |
| Phase 2: NJDパイプライン | 完了 | 6段階NJD処理、TextNormalizer、G2POptions |
| Phase 3: 出力形式 | 完了 | ToProsody、AccentPhrase、JPCommon、HTSラベル |
| Phase 4: テスト | 完了 | 1,600超テスト（NJD単体・pyopenjtalk比較・エッジケース・MeCab一致検証） |
| Phase 5: パッケージング | 完了 | NuGet/UPM設定、CI/CD、ドキュメント |
| Phase 6: 独自MeCabエンジン | **完了** | DoubleArrayTrie、Viterbiデコーダ、未知語処理、NMeCab依存排除→完全MIT化 |

## ライセンス

| パッケージ | ライセンス | 備考 |
|-----------|-----------|------|
| **DotNetG2P** | [MIT](LICENSE) | コアライブラリ |
| **DotNetG2P.MeCab** | [MIT](LICENSE) | 独自MeCabエンジン（**推奨**） |
| **DotNetG2P.NMeCab** | LGPL-2.1-or-later | [LibNMeCab](https://github.com/komutan/NMeCab)依存のため（互換オプション） |

`DotNetG2P` + `DotNetG2P.MeCab` の組み合わせで**完全MITライセンス**で利用可能です。`DotNetG2P.NMeCab` を使用する場合のみLGPL-2.1が適用されます。

## 謝辞・関連プロジェクト

DotNetG2Pは以下のプロジェクトの成果物・知見に基づいています。

| プロジェクト | 関連 |
|-------------|------|
| [OpenJTalk](https://open-jtalk.sourceforge.net/) | NJD処理パイプラインのオリジナル実装（C/C++） |
| [jpreprocess](https://github.com/jpreprocess/jpreprocess) | 本プロジェクトの主要設計参考（Rust再実装） |
| [pyopenjtalk](https://github.com/r9y9/pyopenjtalk) | テストデータ生成・比較検証に使用 |
| [VOICEVOX](https://voicevox.hiroshiba.jp/) | AccentPhrase出力形式・MoraMapping参考 |
| [LibNMeCab](https://github.com/komutan/NMeCab) | 形態素解析エンジン（C# MeCab実装） |
| [ESPnet](https://github.com/espnet/espnet) | 韻律記号抽出アルゴリズム参考 |

## Contributing

Issue・Pull Requestを歓迎します。バグ報告や機能提案は [Issues](https://github.com/ayutaz/dot-net-g2p/issues) からお気軽にどうぞ。

コード内コメント・コミットメッセージ・Issue・PRはすべて**日本語**で記述してください。
