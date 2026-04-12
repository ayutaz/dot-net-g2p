# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

C#/.NET（Unity対応）向けの日英中韓西仏葡瑞多言語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk互換の日本語G2Pパイプライン、CMU辞書ベースの英語G2P、pinyin-data辞書ベースの中国語ピンイン変換、Hangul-firstの韓国語G2P、ルールベースのスペイン語G2P、ルールベース+例外辞書のフランス語G2P、ルールベース+例外辞書のポルトガル語G2P、ルールベース+例外辞書のスウェーデン語G2PをC#でネイティブに再実装し、Pythonやネイティブバイナリへの依存を排除する。

## 進捗状況

全マイルストーン完了済み。現在 v1.10.0。

| 言語 | パッケージ | 状態 | テスト数 | 備考 |
|------|-----------|------|---------|------|
| 日本語 | DotNetG2P.Core + MeCab | M1-M7完了 | 950+ | OpenJTalk互換パイプライン、独自MeCabエンジン、パフォーマンス最適化済み |
| 英語 | DotNetG2P.English | E1-E7完了 | 511 | CMU辞書135k語 + Flite LTS CARTツリー(PER 5.26%)、同綴異音語解決、piper-plus互換IPA |
| 中国語 | DotNetG2P.Chinese | C1-C6完了 | 1776+ | pinyin-data 44k + phrase-pinyin-data 412kエントリ、声調変調、IPA/注音/piper-plus互換、Misaki互換IPA出力(Kokoro TTS向け) |
| 韓国語 | DotNetG2P.Korean | K1-K4完了 | 375 | Hangul-first規則ベース、IPA/PUA/Prosody API |
| スペイン語 | DotNetG2P.Spanish | S1-S4完了 | 355 | ipa-dict PER 1.69%(base)/1.37%(allophones)、LatinAmerican/Castilian方言 |
| フランス語 | DotNetG2P.French | F1-F4完了 | 719 | 例外辞書500+語、Metropolitan/Conservative方言 |
| ポルトガル語 | DotNetG2P.Portuguese | P1-P4完了 | 1310 | 例外辞書560+語、Brazilian/European方言、異音7規則 |
| スウェーデン語 | DotNetG2P.Swedish | Sw1-Sw4完了 | 400+ | ルールベース+例外辞書500+語、Central/FinlandSwedish方言 |
| 多言語 | DotNetG2P.Multilingual | 完了 | 450+ | 8言語ファサード、Lazy初期化、言語自動判定+セグメント分割 |

その他の完了済み作業:
- Unity統合対応 (v1.8.0): embedded resource代替ロード、PUA/Prosody API、piper-plus互換IPA、[Preserve]属性
- Unity パッケージ修正 (v1.7.0): Internal直接配置、PreserveAttribute名前空間修正、.meta整合性チェックCI
- LanguageDetector拡張: CJK互換漢字/カタカナ音声拡張対応

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
DotNetG2P.slnx                          # ソリューションファイル（.NET SDK 9.0+ 対応の .slnx 形式）
├── Directory.Build.props                # NuGet共通メタデータ
├── .github/workflows/                   # CI (ci.yml) + Release (release.yml)
├── src/
│   ├── DotNetG2P.Core/                  # 日本語G2Pコア（.NET Standard 2.1）
│   ├── DotNetG2P.MeCab/                 # 独自MeCab形態素解析エンジン（Apache-2.0）
│   ├── DotNetG2P.Chinese/               # 中国語G2P（独立、Core参照なし）
│   ├── DotNetG2P.English/               # 英語G2P（独立、Core参照なし）
│   ├── DotNetG2P.Korean/                # 韓国語G2P（独立、Core参照なし）
│   ├── DotNetG2P.Spanish/               # スペイン語G2P（独立、Core参照なし）
│   ├── DotNetG2P.French/                # フランス語G2P（独立、Core参照なし）
│   ├── DotNetG2P.Portuguese/            # ポルトガル語G2P（独立、Core参照なし）
│   ├── DotNetG2P.Swedish/               # スウェーデン語G2P（独立、Core参照なし）
│   └── DotNetG2P.Multilingual/          # 多言語ファサード（全パッケージ依存）
├── tests/DotNetG2P.Tests/               # xUnit テスト (net8.0)
├── tools/                               # sync-shared-internals.ps1, 評価ツール等
└── samples/DotNetG2P.Console/           # コンソールサンプル
```

各言語パッケージの共通構造（言語により一部差異あり）:
- `{Lang}G2PEngine.cs` — メインAPI (ToIPA, ToPhonemes, ToXSampa, ToPuaPhonemes, ToIpaWithProsody等 + バッチAPI)
- `{Lang}G2POptions.cs` — オプション設定
- `Models/` — 音素enum, Phoneme struct, Dialect enum, Prosody Info/Result等
- `Rules/` — GraphemeToPhonemeRules, Syllabifier, StressAssigner, AllophoneProcessor等
- `Normalization/` — テキスト正規化、NumberToWords
- `Conversion/` — IPA/X-SAMPA/PUA変換、FunctionWordList（En/Es/Fr/Ptのみ）
- `Data/` — 例外辞書TSV (Es/Fr/Pt/Ko)、埋め込みリソース (Zh/En)
- `Internal/PreserveAttribute.cs` — Unity IL2CPP strip防止（非Core言語パッケージのみ）

## 背景・動機

- OpenJTalkやpyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難
- 既存のC#向け日本語G2Pライブラリは存在しない
- Unity（ゲーム・VTuber・音声合成等）での日本語TTS前処理として需要がある

## アーキテクチャ方針

OpenJTalkの処理パイプラインに準拠した4段階処理:

1. **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、Apache-2.0）を使用（ITokenizer抽象化により差し替え可能）
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
- **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、Apache-2.0、外部依存なし）
- **辞書**: naist-jdic（BSD License）
- **テスト**: xUnit 2.5.3 (net8.0)
- **パッケージング**: NuGet (`DotNetG2P`, `DotNetG2P.MeCab`, `DotNetG2P.Chinese`, `DotNetG2P.English`, `DotNetG2P.Korean`, `DotNetG2P.Spanish`, `DotNetG2P.French`, `DotNetG2P.Portuguese`, `DotNetG2P.Swedish`, `DotNetG2P.Multilingual`) + UPM (`com.dotnetg2p.core`, `com.dotnetg2p.mecab`, `com.dotnetg2p.chinese`, `com.dotnetg2p.english`, `com.dotnetg2p.korean`, `com.dotnetg2p.spanish`, `com.dotnetg2p.french`, `com.dotnetg2p.portuguese`, `com.dotnetg2p.swedish`, `com.dotnetg2p.multilingual`)
- **CI/CD**: GitHub Actions (ci.yml, release.yml)
- **ソリューション形式**: .slnx（.NET 10）

## 開発言語

コード内コメント・ドキュメント・コミットメッセージ・PR・Issueはすべて**日本語**で記述する。
