# DotNetG2P

**日本語** | [English](README_EN.md) | [中文](README_ZH.md)

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

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
- [ライセンス](#ライセンス)

## 特徴

- **純C#実装** — ネイティブバイナリ不要、独自MeCabエンジン（`DotNetG2P.MeCab`）によりNuGetパッケージ依存なし（実行時に[naist-jdic辞書](#辞書の準備)が必要）
- **OpenJTalk互換パイプライン** — 発音生成・数字読み・アクセント句結合・アクセント結合型・無声音化の6段階NJD処理
- **複数の出力形式** — 音素列 / カタカナ / ESPnet韻律記号 / VOICEVOX互換AccentPhrase / HTSフルコンテキストラベル
- **Unity対応** — .NET Standard 2.1（Unity 2021.2+）ターゲット、UPMパッケージ提供
- **拡張可能な設計** — `ITokenizer`インターフェースにより形態素解析エンジンを差し替え可能

## インストール

### NuGet

```bash
# コアライブラリ + 独自MeCabエンジン
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab
```

### パッケージ構成

| パッケージ | ライセンス | 説明 |
|-----------|-----------|------|
| `DotNetG2P` | Apache-2.0 | コアライブラリ（G2Pエンジン、NJD処理、音素変換） |
| `DotNetG2P.MeCab` | Apache-2.0 | 独自MeCabエンジン（外部依存なし） |

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
using DotNetG2P.MeCab;

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

## API リファレンス

### G2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | 空白区切り音素列 (`"k o N n i ch i w a"`) |
| `ToKana(text)` | `string` | カタカナ読み (`"コンニチワ"`) |
| `ToProsody(text)` | `string` | ESPnet韻律記号付き (`"^ k o [ N n i ch i w a $"`) |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX互換アクセント句構造体 |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTSフルコンテキストラベル |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | NJD処理後のノード列 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括で音素列に変換 |
| `ToKanaBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括でカタカナ読みに変換 |
| `ToProsodyBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括で韻律記号付きに変換 |
| `ToFullContextLabelsBatch(texts)` | `IReadOnlyList<IReadOnlyList<string>>` | 複数テキストを一括でHTSラベルに変換 |

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

## ライセンス

| パッケージ | ライセンス | 備考 |
|-----------|-----------|------|
| **DotNetG2P** | [Apache-2.0](LICENSE) | コアライブラリ |
| **DotNetG2P.MeCab** | [Apache-2.0](LICENSE) | 独自MeCabエンジン |

全コンポーネントが**Apache-2.0ライセンス**で利用可能です。
