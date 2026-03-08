# DotNetG2P.Chinese 再設計実装計画

> 作成日: 2026-03-08
> ベース: [chinese-g2p-research.md](./chinese-g2p-research.md) の調査結果 + 15エージェント調査レポート
> 対象: 現行C1-C6完了済み実装の再設計（feature/chinese-g2p ブランチ）

---

## 目次

1. [全体方針](#1-全体方針)
2. [精度目標](#2-精度目標)
3. [アーキテクチャ概要](#3-アーキテクチャ概要)
4. [パッケージ構造・ファイル構成](#4-パッケージ構造ファイル構成)
5. [変換パイプライン再設計](#5-変換パイプライン再設計)
6. [辞書最適化計画](#6-辞書最適化計画)
7. [分詞エンジン設計](#7-分詞エンジン設計)
8. [テキスト正規化設計](#8-テキスト正規化設計)
9. [声調変調改善計画](#9-声調変調改善計画)
10. [児化音サポート設計](#10-児化音erhuaサポート設計)
11. [IPA変換修正計画](#11-ipa変換修正計画)
12. [注音変換修正計画](#12-注音zhuyinボポモフォ変換修正計画)
13. [API再設計計画](#13-api再設計計画)
14. [Multilingual統合改善計画](#14-multilingual統合改善計画)
15. [パフォーマンス最適化計画](#15-パフォーマンス最適化計画)
16. [テスト戦略計画](#16-テスト戦略計画)
17. [フェーズ別実装ロードマップ](#17-フェーズ別実装ロードマップ)

---

## 1. 全体方針

### 1.1 再設計の目的

現行実装（C1-C6）は基本的なピンイン変換機能を備えているが、以下の課題がある:

**精度の限界（推定87%）**
- 分詞（単語分割）機能がなく、文字単位の最長一致のみ。中国語G2Pの精度は分詞品質に大きく依存する
- フレーズ辞書の最長一致は素朴なgreedy matchであり、「研究生命」のような境界曖昧ケースで誤分割
- 軽声辞書がなく、"妈妈"(māma)のような日常語の第二音節を正しく軽声にできない
- テキスト正規化がなく、数字・英字・特殊記号のピンイン読みが不可能

**コード重複（約90%）**
- `ChineseG2PEngine.cs`の`ToPinyin()`/`ToIPA()`/`ToZhuyin()`はほぼ同一の出力ループを3回記述（L129-178, L276-320, L342-386）。差分はスタイル変換の1行のみ
- バッチAPIもすべてforループの単純ラッパー（9メソッドが完全パターン重複）

**TTS統合機能の不足**
- 児化音（erhua/アール化）未対応: "花儿"(huār) → "huā er" と分離出力
- 句境界・韻律境界情報の出力がない（TTSフロントエンドとして不完全）

### 1.2 設計哲学

| 原則 | 内容 |
|------|------|
| **ML不要** | 機械学習モデルを使用しない。ルール+辞書のみで動作 |
| **辞書ベース** | pinyin-data（MIT）単字辞書 + phrase-pinyin-data（MIT）フレーズ辞書が基盤 |
| **.NET Standard 2.1** | Unity 2021.2+互換。System.Memory/Span使用可能、unsafe限定的OK |
| **ライセンス統一** | Apache-2.0。依存データはMITのみ（CC-BY等不可） |
| **パイプライン分離** | 各処理段階が独立したクラスで、テスト・差し替え・ON/OFF制御可能 |
| **ゼロ外部依存** | NuGet外部パッケージ依存なし。DotNetG2P.Coreとの依存もなし（独立パッケージ） |
| **日本語G2Pとの設計一貫性** | DotNetG2P.Core（7段階NJDパイプライン）と類似のパイプライン設計 |

### 1.3 現行実装との差分サマリ

| 項目 | 現行（C1-C6） | 再設計後 |
|------|---------------|----------|
| パイプライン段階 | 3段階（収集→声調変調→スタイル変換） | 7段階（正規化→分詞→辞書→声調→児化→軽声→出力） |
| 分詞 | なし（文字単位最長一致） | BiMM → DAG+DP段階的実装 |
| テキスト正規化 | なし | 数字・英字・特殊記号のピンイン読み展開 |
| 児化音 | 未対応 | "花儿"→"huār"の韻母変化ルール実装 |
| 軽声 | 未対応 | 軽声辞書（"妈妈""爸爸"等の日常語パターン） |
| 出力ループ | 3重複（ToPinyin/ToIPA/ToZhuyin各独立） | 統一パイプライン+出力フォーマッタ分離 |
| 声調変調 | 基本3ルール（一/不/三声連読） | 改善（V一V/A不A軽声、軽声語彙リスト、語境界認識） |
| IPA変換 | 基本動作（致命的バグあり） | iong修正、zh/ch retroflex修正、母音区別修正 |
| 注音変換 | 基本動作（致命的バグあり） | wengバグ修正、ê対応 |
| API | 個別メソッド×スタイル | RunPipeline統一+OutputFormatter戦略パターン |

---

## 2. 精度目標

### 2.1 ベンチマーク基準

**CPPデータセット**（Chinese Polyphonic Pronouncing）をベンチマークとして使用:
- g2pM論文のテストセット（約100K文、多音字を中心とした評価セット）
- 業界標準のベンチマーク。pypinyin/g2pM/prosody等が使用

### 2.2 フェーズ別精度目標

| フェーズ | 目標精度 | 主要施策 | 現行との差分 |
|----------|----------|----------|-------------|
| 現行 | ~87% | フレーズ最長一致のみ | ベースライン |
| Phase 1 | 90% | 分詞エンジン導入（BiMM→DAG+DP） | +3pt |
| Phase 2 | 92%+ | テキスト正規化+軽声辞書+児化音+声調変調改善 | +2pt |

### 2.3 精度計測方法

1. **文字単位精度（Character Accuracy）**: 各漢字のピンイン（声調込み）が正解と一致する割合
2. **文単位精度（Sentence Accuracy）**: 文中の全漢字が正しい文の割合
3. **多音字精度（Polyphonic Accuracy）**: 多音字のみを抽出した正解率（最も重要な指標）

評価テストは `tests/DotNetG2P.Tests/ChineseG2P/ChineseBenchmarkTests.cs` として実装し、CI/CDでは参考値として出力する。

### 2.4 既知の精度ボトルネック

| 原因 | 推定影響 | 対策 |
|------|----------|------|
| 分詞なし→多音字誤判定 | -5~7% | Phase 1: 分詞エンジン |
| 軽声未対応 | -2~3% | Phase 2: 軽声辞書 |
| テキスト正規化なし（数字等） | -1~2% | Phase 2: Normalizer |
| フレーズ辞書カバレッジ不足 | -1% | 継続的辞書拡充 |

---

## 3. アーキテクチャ概要

### 3.1 7段階パイプライン概要図

```
入力テキスト
    │
    ▼
┌─────────────────────────────┐
│ Stage 1: TextNormalizer     │  数字→漢数字、英字→読み、特殊記号処理
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 2: WordSegmenter      │  BiMM / DAG+DP 分詞
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 3: PinyinResolver     │  辞書ルックアップ(フレーズ→単字フォールバック)
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 4: ToneSandhiProcessor│  声調変調(三声連読/一/不 + V一V/A不A/軽声語彙)
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 5: ErhuaProcessor     │  児化音処理(花儿→huār韻母変化)
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 6: NeutralToneMarker  │  軽声マーキング(辞書ベース)
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 7: OutputFormatter    │  出力形式変換
│  ├─ PinyinFormatter         │    (ToneMarked/ToneNumber/Normal)
│  ├─ IpaFormatter            │    (IPA国際音声記号)
│  └─ ZhuyinFormatter         │    (注音符号ボポモフォ)
└─────────────────────────────┘
              │
              ▼
          出力文字列
```

### 3.2 主要コンポーネント相関図

```
ChineseG2PEngine (ファサード)
    │
    ├── ChineseG2POptions (設定、各Stage ON/OFF)
    │
    ├── Pipeline (7段階処理チェーン)
    │   ├── TextNormalizer
    │   │   ├── NumberToHanzi (数字→漢数字)
    │   │   ├── CurrencyExpander (通貨→読み)
    │   │   ├── DateTimeExpander (日時→読み)
    │   │   └── MiscExpander (パーセンテージ等)
    │   │
    │   ├── WordSegmenter
    │   │   ├── PinyinPhraseDictionary (語彙源)
    │   │   └── SegmentationStrategy (BiMM / DAG+DP)
    │   │
    │   ├── PinyinResolver
    │   │   ├── PinyinPhraseDictionary (フレーズ辞書)
    │   │   └── PinyinCharDictionary (単字辞書)
    │   │
    │   ├── ToneSandhiProcessor (改善版)
    │   ├── ErhuaProcessor (新規)
    │   └── NeutralToneMarker (新規)
    │
    └── OutputFormatter (出力形式変換)
        ├── PinyinFormatter
        ├── IpaFormatter (PinyinToIpa改善版)
        └── ZhuyinFormatter (PinyinToZhuyin改善版)
```

### 3.3 現行3段階→7段階の対応表

| 再設計Stage | 現行の対応処理 | 変更内容 |
|-------------|---------------|----------|
| Stage 1: TextNormalizer | なし | **新規追加** |
| Stage 2: WordSegmenter | なし（文字単位ループ） | **新規追加** |
| Stage 3: PinyinResolver | `CollectPinyins()` | 分詞結果に基づくルックアップに変更 |
| Stage 4: ToneSandhiProcessor | `ToneSandhiProcessor.Apply()` | V一V/A不A/軽声語彙/語境界認識追加 |
| Stage 5: ErhuaProcessor | なし | **新規追加** |
| Stage 6: NeutralToneMarker | なし | **新規追加** |
| Stage 7: OutputFormatter | `ApplyStyle()` + `PinyinToIpa` + `PinyinToZhuyin` | 統一出力ループに集約 |

### 3.4 パイプライン中間表現

```csharp
/// パイプライン中間表現（1音節分の情報）
internal struct PinyinToken
{
    public string Surface;         // 元テキスト中の文字列
    public string? Pinyin;         // 声調記号付きピンイン（非漢字の場合null）
    public TokenKind Kind;         // トークン種別（Hanzi/Punctuation/Whitespace/Foreign/Number）
    public Initial Initial;        // 声母（パース済み、Stage3以降）
    public Final Final;            // 韻母（パース済み、Stage3以降）
    public Tone Tone;              // 声調（Stage4で変調適用後の値）
    public bool IsErhua;           // 児化音フラグ（Stage5で設定）
    public bool IsNeutralTone;     // 軽声フラグ（Stage6で設定）
}
```

### 3.5 現行コードの具体的な問題点

1. **出力ループ3重複**: `ChineseG2PEngine.cs` L129-178, L276-320, L342-386 がほぼ同一構造
2. **CollectPinyinsの肥大化**: L538-644で文字分類・フレーズ検索・単字検索・非漢字処理を1メソッドに凝縮
3. **IsCjkUnifiedIdeograph重複**: `ChineseG2PEngine.cs` L709-714 と `ToneSandhiProcessor.cs` L176-180 で同一ロジック重複
4. **FindLongestMatch Substring**: `PinyinPhraseDictionary.FindLongestMatch()` L86-99 がSubstring生成を毎回行いGC圧迫

---

## 4. パッケージ構造・ファイル構成

### 4.1 再設計後ディレクトリツリー

```
src/DotNetG2P.Chinese/
├── DotNetG2P.Chinese.csproj
├── DotNetG2P.Chinese.asmdef
├── package.json
├── LICENSE.md
├── THIRD-PARTY-NOTICES.md
│
├── ChineseG2PEngine.cs               # [変更] パイプライン再構成
├── ChineseG2POptions.cs              # [変更] オプション拡張
│
├── Models/
│   ├── Initial.cs                     # [既存]
│   ├── Final.cs                       # [既存]
│   ├── Tone.cs                        # [既存]
│   ├── PinyinSyllable.cs             # [既存]
│   ├── PinyinStyle.cs                # [既存]
│   └── PinyinResult.cs              # [既存]
│
├── Dictionary/
│   ├── PinyinCharDictionary.cs       # [変更] 配列インデックス化、WeakReferenceキャッシュ
│   ├── PinyinPhraseDictionary.cs     # [変更] Trie化検討、WeakReferenceキャッシュ
│   └── Data/
│       ├── pinyin_char.txt           # [既存]
│       └── pinyin_phrase.txt         # [既存]
│
├── Conversion/
│   ├── PinyinParser.cs               # [変更] 精度改善
│   ├── ToneConverter.cs              # [既存]
│   ├── PinyinToIpa.cs               # [変更] バグ修正+精度改善
│   └── PinyinToZhuyin.cs            # [変更] バグ修正+精度改善
│
├── Segmentation/                     # [新規ディレクトリ]
│   └── WordSegmenter.cs             # [新規] 分詞エンジン
│
├── Normalization/                    # [新規ディレクトリ]
│   ├── ChineseTextNormalizer.cs     # [新規] 正規化ファサード
│   ├── NumberToHanzi.cs             # [新規] 数字→漢字変換
│   ├── CurrencyExpander.cs          # [新規] 通貨展開
│   └── DateTimeExpander.cs          # [新規] 日時展開
│
├── Erhua/                            # [新規ディレクトリ]
│   └── ErhuaProcessor.cs           # [新規] 児化音処理
│
├── ToneSandhi/
│   └── ToneSandhiProcessor.cs       # [変更] 語境界ベース声調変調
│
└── Internal/                         # [新規ディレクトリ]
    └── ValueStringBuilder.cs        # [新規] Core版コピー
```

### 4.2 新規ファイル一覧（7ファイル）

| # | ファイルパス | 概要 | 推定行数 |
|---|------------|------|---------|
| 1 | `Segmentation/WordSegmenter.cs` | BiMM / DAG+DP分詞エンジン | ~180行 |
| 2 | `Normalization/ChineseTextNormalizer.cs` | テキスト正規化ファサード | ~120行 |
| 3 | `Normalization/NumberToHanzi.cs` | 数字→漢字読み変換 | ~250行 |
| 4 | `Normalization/CurrencyExpander.cs` | 通貨表記展開 | ~100行 |
| 5 | `Normalization/DateTimeExpander.cs` | 日時表記展開 | ~150行 |
| 6 | `Erhua/ErhuaProcessor.cs` | 児化音処理 | ~200行 |
| 7 | `Internal/ValueStringBuilder.cs` | ゼロアロケーション文字列構築（Core版コピー） | ~120行 |

### 4.3 変更ファイル一覧（7ファイル）

| # | ファイルパス | 変更内容 | 変更規模 |
|---|------------|---------|---------|
| 1 | `ChineseG2PEngine.cs` | パイプライン再構成、RunPipeline共有化 | 大 |
| 2 | `ChineseG2POptions.cs` | 新規オプション7件追加 | 中 |
| 3 | `Dictionary/PinyinCharDictionary.cs` | 配列インデックス化、WeakReferenceキャッシュ | 中 |
| 4 | `Dictionary/PinyinPhraseDictionary.cs` | Span化、WeakReferenceキャッシュ | 中 |
| 5 | `Conversion/PinyinToIpa.cs` | P0/P1/P2バグ修正 | 中 |
| 6 | `Conversion/PinyinToZhuyin.cs` | wengバグ修正、ê対応 | 小 |
| 7 | `ToneSandhi/ToneSandhiProcessor.cs` | V一V/A不A/軽声語彙/語境界認識 | 大 |

### 4.4 新規テストファイル一覧（~10ファイル、~240件追加）

| # | テストファイルパス | 推定件数 |
|---|-------------------|---------|
| 1 | `ChineseG2P/ChineseSegmenterTests.cs` | ~30件 |
| 2 | `ChineseG2P/ChineseNormalizerTests.cs` | ~50件 |
| 3 | `ChineseG2P/ErhuaProcessorTests.cs` | ~20件 |
| 4 | `ChineseG2P/ToneSandhiExtendedTests.cs` | ~30件 |
| 5 | `ChineseG2P/IpaFixTests.cs` | ~30件 |
| 6 | `ChineseG2P/ZhuyinFixTests.cs` | ~30件 |
| 7 | `ChineseG2P/ChinesePipelineIntegrationTests.cs` | ~20件 |
| 8 | `ChineseG2P/ChineseBenchmarkTests.cs` | ~20件 |
| 9 | `ChineseG2P/NumberToHanziTests.cs` | ~35件 |
| 10 | `ChineseG2P/CurrencyExpanderTests.cs` | ~20件 |

### 4.5 csproj変更

```xml
<PropertyGroup>
  <!-- 追加: ValueStringBuilder用 unsafe許可 -->
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

### 4.6 ファイル数サマリ

| 区分 | 現行 | 再設計後 | 差分 |
|------|------|---------|------|
| ソースファイル | 13 | 20 | +7 |
| テストファイル | 19 | 29 | +10 |
| 辞書データ | 2 | 2 | 0 |
| **合計** | **39** | **56** | **+17** |

---

## 5. 変換パイプライン再設計

### 5.1 現行パイプライン分析

現行の3段階パイプライン:
```
CollectPinyins(text) → ApplyToneSandhiToEntries(entries) → ApplyStyle/Convert出力
```

**問題: ToPinyin/ToIPA/ToZhuyinで約90%のコード重複**

ToPinyin（L129-178）、ToIPA（L276-320）、ToZhuyin（L342-386）の唯一の差異:
- `ToPinyin`: `ApplyStyle(entry.Pinyin, style)` （L158）
- `ToIPA`: `PinyinToIpa.Convert(entry.Pinyin, includeTones)` （L302）
- `ToZhuyin`: `PinyinToZhuyin.Convert(entry.Pinyin, includeTones)` （L368）

ループ内のIsSeparator/IsUnknownHanzi/RawText処理（約20行のif-else分岐）は完全に同一コード。

### 5.2 再設計パイプライン（7段階）

```
[Stage 1] テキスト正規化 (ChineseTextNormalizer)
  → 全角→半角英数字、数字→漢数字、通貨/日時展開
[Stage 2] 分詞 (WordSegmenter)
  → テキストを「漢字語」「非漢字セグメント」に分割
[Stage 3] ピンイン収集 (PinyinCollector)
  → 各漢字語のピンイン解決（辞書ルックアップ）
[Stage 4] 声調変調 (ToneSandhiProcessor)
  → PinyinWord単位で適用
[Stage 5] 児化音処理 (ErhuaProcessor)
  → "儿"接尾辞の検出と前音節への統合
[Stage 6] スタイル変換 (StyleConverter)
  → PinyinWord列をPinyinStyle/IPA/Zhuyinに変換
[Stage 7] 出力フォーマット (OutputFormatter)
  → セパレータ挿入、非漢字パススルー、文字列構築
```

### 5.3 中間表現: PinyinWord

```csharp
internal readonly struct PinyinWord
{
    public ReadOnlyMemory<char> OriginalText { get; }
    public PinyinSyllable[] Syllables { get; }
    public bool IsChineseWord { get; }
    public bool IsSeparator { get; }
}
```

現行`PinyinEntry`（char単位）→ 新`PinyinWord`（語単位）に移行:

| 観点 | 現行 PinyinEntry | 新 PinyinWord |
|------|-----------------|---------------|
| 粒度 | 1文字 | 1語（複数文字） |
| ピンイン表現 | `string` (生文字列) | `PinyinSyllable[]` (構造化済み) |
| 元テキスト参照 | `char OriginalChar` | `ReadOnlyMemory<char>` (ゼロアロケーション) |
| 語境界情報 | なし | `IsChineseWord` で明示 |

### 5.4 RunPipeline() 共有メソッド設計

```csharp
internal delegate string SyllableConverter(PinyinSyllable syllable);

private string RunPipeline(string text, SyllableConverter converter)
{
    // Stage 1: テキスト正規化
    var normalized = _options.EnableNormalization
        ? ChineseTextNormalizer.Normalize(text) : text;

    // Stage 2+3: 分詞 + ピンイン収集
    var words = CollectPinyinWords(normalized);

    // Stage 4: 声調変調
    if (_options.EnableToneSandhi)
        ToneSandhiProcessor.Apply(words);

    // Stage 5: 児化音処理
    if (_options.EnableErhua)
        ErhuaProcessor.Apply(words);

    // Stage 6+7: スタイル変換 + 出力フォーマット
    return FormatOutput(words, converter);
}
```

リファクタリング後の公開メソッド:
```csharp
public string ToPinyin(string text, PinyinStyle style)
    => RunPipeline(text, s => ApplyStyle(s, style));

public string ToIPA(string text, bool includeTones)
    => RunPipeline(text, s => PinyinToIpa.ConvertSyllable(s, includeTones));

public string ToZhuyin(string text, bool includeTones)
    => RunPipeline(text, s => PinyinToZhuyin.ConvertSyllable(s, includeTones));
```

**コード削減効果**: 約140行の重複コード → 約60行（57%削減）

### 5.5 各ステージのON/OFFオプション対応

| ステージ | 制御オプション | デフォルト | 無効時の動作 |
|----------|---------------|-----------|-------------|
| 1. テキスト正規化 | `EnableNormalization` | false | 入力テキストをそのまま通過 |
| 2. 分詞 | 常に有効 | - | 基本処理のため無効化不可 |
| 3. ピンイン収集 | `HandleHeteronyms` | true | フレーズ辞書スキップ、単字のみ |
| 4. 声調変調 | `EnableToneSandhi` | true | 変調処理をスキップ |
| 5. 児化音処理 | `EnableErhua` | false | 児化音統合をスキップ |
| 6+7. 出力 | `DefaultStyle` / `Separator` | ToneMarked / " " | - |

### 5.6 移行戦略

1. **Phase 1**: PinyinWord導入 + RunPipeline抽出（既存テスト936件パス確認）
2. **Phase 2**: 新ステージ追加（TextNormalizer, ErhuaProcessor）
3. **Phase 3**: ToneSandhiProcessor改修（語境界認識）

**破壊的変更なし**: 全公開APIのシグネチャは変更せず、内部実装のみリファクタリング。

---

## 6. 辞書最適化計画

### 6.1 PinyinCharDictionary: Dictionary → 配列インデックス化

**現状**: `Dictionary<int, object>` (~44,435エントリ)

**最適化**: CJK統一漢字範囲(U+3400-U+9FFF)に対して連続配列でO(1)アクセス:
- 配列インデックス = `codePoint - 0x3400`（27,136要素）
- 範囲外コードポイント: `Dictionary<int, object>`フォールバック
- メモリ: ~212KB配列 + 小Dictionary（Dictionary内部ハッシュテーブル ~2MB を置換）

**期待効果**: ルックアップ ~30-50%レイテンシ削減、メモリ ~1.5MB削減

### 6.2 PinyinPhraseDictionary: Substring排除

**現状**: `FindLongestMatch()` (L90-93)で毎回 `text.Substring(startIndex, len)` を生成

**最適化方式A (Phase 2)**: ReadOnlySpan<char>ベースのカスタムハッシュ検索
- FNV-1a/xxHash32をSpanに対して計算
- ハッシュ一致時のみSubstringを生成して完全比較
- Substring生成回数を平均90%削減

**最適化方式B (Phase 3+)**: Trie構造への移行
- フレーズ辞書をTrie (Prefix Tree)に再構成
- 文字単位で走査、最長一致を1パスで検出
- Substringアロケーション完全排除

### 6.3 メモリ最適化

- **string.Intern()**: 辞書パース時に適用。100万ピンイン文字列が~1,300種に集約（メモリ ~20MB→~100KB）
- **WeakReferenceキャッシュ**: DictionaryBundleパターン適用。複数エンジンインスタンスで辞書共有
- **辞書一括読み込み**: StreamReaderバッファサイズ64KB + Spanベースパース

### 6.4 改善前後の性能見積もり

| 指標 | 現行 | 目標 | 改善率 |
|------|------|------|--------|
| 辞書メモリ (1インスタンス) | ~60MB | ~40MB | 33% |
| 辞書メモリ (5インスタンス) | ~300MB | ~45MB | 85% |
| ルックアップ | ~15ns/回 | ~5-8ns/回 | 50% |
| 辞書初期化 | ~500ms | ~300ms | 40% |

---

## 7. 分詞エンジン設計

### 7.1 現行の問題

現在の `CollectPinyins` メソッド（`ChineseG2PEngine.cs:538-644`）は **Forward Maximum Matching (FMM)** のみを使用。

FMMの具体的な問題例:

| 入力 | FMM分詞結果 | 正しい分詞 | 影響 |
|------|------------|-----------|------|
| 结合成 | 结合 / 成 | 结 / 合成 | 多音字「合」の読みが異なる |
| 研究生命 | 研究生 / 命 | 研究 / 生命 | 「生」の読みに影響 |
| 长春市长 | 长春 / 市长 | 长春 / 市 / 长 | 同綴異音語の解決に影響 |

### 7.2 Phase 1: 双方向最大マッチング (BiMM)

1. **Forward Maximum Matching (FMM)**: 左から右へ最長一致（現行の動作）
2. **Backward Maximum Matching (BMM)**: 右から左へ最長一致（新規追加）
3. **ヒューリスティック**: 分詞数が少ない方、単字詞が少ない方を採用

```csharp
internal interface IWordSegmenter
{
    List<WordSegment> Segment(
        string text, int start, int length,
        PinyinPhraseDictionary phraseDictionary,
        PinyinCharDictionary charDictionary);
}

internal sealed class BiDirectionalSegmenter : IWordSegmenter { ... }
```

- 追加コード量: ~80行
- 精度改善: +2-3%

### 7.3 Phase 2: DAG + 動的計画法 (jieba方式)

```
Step 1: DAG構築
  テキスト中の各位置iから始まるすべての辞書マッチを列挙
  DAG[i] = { j : text[i..j] が辞書に存在 }

Step 2: 動的計画法（後ろ向き）
  score[n] = 0
  for i = n-1 downto 0:
    score[i] = max over j in DAG[i]:
      log(freq(text[i..j])) - log(totalFreq) + score[j+1]

Step 3: 分詞→ピンイン付与
```

#### Trie設計（軽量ハッシュTrie）

```csharp
internal sealed class PhraseTrieNode
{
    internal Dictionary<char, PhraseTrieNode>? Children;
    internal int PhraseLength;

    internal void CommonPrefixSearch(
        string text, int start, int maxLen, List<int> results) { ... }
}
```

- 追加コード量: ~200行（Trie ~40行 + DagSegmenter ~120行 + 頻度テーブル ~40行）
- 精度改善: +5-10%
- メモリ増加: ~20-30MB（Trie構造）

#### 頻度データ戦略

1. 均等頻度 + フレーズ長ボーナス（初期実装）
2. jieba `dict.txt` 頻度データ（MIT、必要時移行）

### 7.4 WordSegment 構造体

```csharp
internal readonly struct WordSegment
{
    public int StartIndex { get; }
    public int Length { get; }
    public string?[] Pinyins { get; }
}
```

### 7.5 ChineseG2POptions への追加

```csharp
public enum SegmentationMode
{
    Forward,        // 前方最大マッチング（現行互換）
    BiDirectional,  // 双方向最大マッチング
    Dag             // DAG + 動的計画法
}
```

---

## 8. テキスト正規化設計

### 8.1 ChineseTextNormalizer (ファサード)

英語の `EnglishNormalizer` と同一の静的ファサードパターン:

```csharp
namespace DotNetG2P.Chinese.Normalization
{
    internal static class ChineseTextNormalizer
    {
        public static string Normalize(string text);
    }
}
```

### 8.2 NumberToHanzi

| パターン | 入力例 | 出力 |
|----------|--------|------|
| 基数（整数） | 123 | 一百二十三 |
| 基数（万以上） | 12345 | 一万二千三百四十五 |
| ゼロ挿入 | 1001 | 一千零一 |
| 小数 | 3.14 | 三点一四 |
| 序数 | 第1 | 第一 |
| 年号 | 2024年 | 二零二四年 |
| 電話番号 | 110 | 一一零 |
| 負数 | -5 | 负五 |

中国語数字体系: 万進法（万=10^4, 亿=10^8）

### 8.3 CurrencyExpander

| 記号 | 変換例 |
|------|--------|
| ¥ / 元 | ¥100 → 一百元、¥3.50 → 三元五角 |
| $ | $50 → 五十美元 |
| € | €20 → 二十欧元 |
| £ | £10 → 十英镑 |

### 8.4 DateTimeExpander

| パターン | 入力例 | 出力 |
|----------|--------|------|
| YYYY年MM月DD日 | 2024年1月15日 | 二零二四年一月十五日 |
| HH:MM | 3:30 | 三点三十分 |
| YYYY-MM-DD | 2024-01-15 | 二零二四年一月十五日 |

### 8.5 その他パターン (MiscExpander)

| カテゴリ | 入力例 | 出力 |
|----------|--------|------|
| パーセンテージ | 50% | 百分之五十 |
| 分数 | 1/3 | 三分之一 |
| 温度 | 36.5°C | 三十六点五度 |
| 比率 | 3:2 | 三比二 |

### 8.6 正規化パイプライン順序

```
1. 全角→半角正規化
2. 文字単位走査+パターンマッチング:
   (a) DateTimeExpander.TryExpandDate()
   (b) DateTimeExpander.TryExpandTime()
   (c) CurrencyExpander.TryExpand()
   (d) MiscExpander.TryExpand()
   (e) NumberToHanzi（数字フォールバック）
3. 出力: 正規化済みテキスト
```

### 8.7 ファイル構成

```
src/DotNetG2P.Chinese/Normalization/
├── ChineseTextNormalizer.cs   (~100行)
├── NumberToHanzi.cs           (~250行)
├── CurrencyExpander.cs        (~100行)
├── DateTimeExpander.cs        (~150行)
└── MiscExpander.cs            (~80行)
```

---

## 9. 声調変調改善計画

### 9.1 現行実装の課題

**実装済みルール（3ルールのみ）**:
1. 三声連読変調
2. "一"変調（四声前→二声、一/二/三声前→四声、序数例外）
3. "不"変調（四声前→二声）

**課題**:
- 単語境界を認識しない三声変調
- V一V/A不A構文の軽声化が未対応
- 軽声語彙リストが存在しない
- APIシグネチャが平坦な文字配列のみ受け取り、語境界情報を渡せない

### 9.2 追加ルール

#### 9.2.1 V一V 軽声化
```
看一看 → kàn yi kàn  （一が軽声に）
想一想 → xiǎng yi xiǎng
```
判定: `originalChars[i-1] == originalChars[i+1]` かつ `originalChars[i] == '一'`

#### 9.2.2 A不A 軽声化
```
是不是 → shì bu shì  （不が軽声に）
好不好 → hǎo bu hǎo
```
判定: `originalChars[i-1] == originalChars[i+1]` かつ `originalChars[i] == '不'`

#### 9.2.3 単語境界認識三声変調
- `Apply` メソッドにオプショナルな `int[]? wordLengths` を追加
- 分詞結果がある場合は、同一単語内の三声連続のみ変調対象

#### 9.2.4 軽声語彙リスト (~500語)
PaddleSpeech準拠:
```
東西 dōng xi, 意思 yì si, 便宜 pián yi, 先生 xiān sheng,
時候 shí hou, 朋友 péng you, 漂亮 piào liang, 明白 míng bai
```

### 9.3 ToneSandhiOptions

```csharp
internal sealed class ToneSandhiOptions
{
    public bool EnableThirdTone { get; set; } = true;
    public bool EnableYiSandhi { get; set; } = true;
    public bool EnableBuSandhi { get; set; } = true;
    public bool EnableYiNeutral { get; set; } = true;
    public bool EnableBuNeutral { get; set; } = true;
    public bool EnableNeutralToneWords { get; set; } = true;
}
```

### 9.4 処理順序（改訂）

```
V一V軽声化 → 一変調 → A不A軽声化 → 不変調
→ 軽声語彙リスト適用 → 三声連読（単語境界認識）
```

### 9.5 実装優先度

| 優先度 | ルール | 工数 |
|--------|--------|------|
| P1 | V一V 軽声化 | 小 |
| P1 | A不A 軽声化 | 小 |
| P2 | 軽声語彙リスト | 中（~500語データ移植） |
| P3 | 単語境界認識三声変調 | 大（分詞エンジン前提） |

---

## 10. 児化音(Erhua)サポート設計

### 10.1 児化音の概要

「儿」(U+513F, ér) が先行音節に融合し、韻母がr-colored（そり舌化）に変化する。
現在の実装は「儿」を独立音節として処理し `ér`/`er` を出力（分離出力）。
実際の発音は「花儿」= [xua˥˥ɻ]（1音節のr-colored韻母）。

### 10.2 ErhuaProcessor 設計

```csharp
internal static class ErhuaProcessor
{
    public static void Apply(List<PinyinEntry> entries, ErhuaStyle style);
}
```

処理フロー:
1. `entries[i].OriginalChar == '儿'` かつピンインが `er`/`ér` のエントリを検出
2. 先行漢字エントリを取得
3. スタイルに応じた処理（None/Suffix/Merged）

#### 韻母融合テーブル（Mergedモード用）

| 元の韻母 | 児化後 | IPA変化 | 備考 |
|----------|--------|--------|------|
| a | ar | [aɻ] | 単純付加 |
| o | or | [oɻ] | 単純付加 |
| e | er | [əɻ] | 既存Er同音 |
| ai | ar | [aɻ] | 韻尾-i脱落 |
| ei | er | [əɻ] | 韻尾-i脱落 |
| ao | aor | [aʊɻ] | 韻尾保持+r |
| ou | our | [oʊɻ] | 韻尾保持+r |
| an | ar | [aɻ] | 鼻韻尾-n脱落 |
| en | er | [əɻ] | 鼻韻尾-n脱落 |
| ang | angr | [ɑ̃ɻ] | 鼻母音化+r |
| i | ier | [iəɻ] | 母音挿入 |
| in | ier | [iəɻ] | 鼻韻尾脱落 |
| u | ur | [uɻ] | 単純付加 |
| un | uer | [uəɻ] | 鼻韻尾脱落 |
| v (ü) | ver | [yəɻ] | 母音挿入 |

### 10.3 ErhuaStyle enum

```csharp
public enum ErhuaStyle : byte
{
    None = 0,     // 処理しない（"花儿" → "huā ér"）
    Suffix = 1,   // "r"サフィックス付加（"花儿" → "huār"）
    Merged = 2,   // 韻母融合（"花儿" → "huar"）
}
```

デフォルト `None`（後方互換性維持）

### 10.4 IPA/注音出力との統合

- **IPA**: 児化韻母→IPA のマッピング追加（ar→[aɻ], or→[oɻ] 等、16エントリ）
- **注音**: 児化音マーカー `ㄦ` (U+3126) を韻母後に付加

### 10.5 児化語彙検出

- **Phase 1**: フレーズ辞書の「...X儿」パターンから検出
- **Phase 2**: ルールベース検出 + 除外リスト（~20語: 儿子, 女儿, 儿童 等）

### 10.6 パイプライン統合位置

```
CollectPinyins → ApplyToneSandhi → ApplyErhua → Style変換
```
声調変調後・スタイル変換前に配置（融合で音節数が変わるため）

---

## 11. IPA変換修正計画

### 11.1 P0 致命的バグ

#### iong → "yŋ" (正しくは "iʊŋ")

**ファイル**: `PinyinToIpa.cs` 62行目
```csharp
[Final.Iong] = "y\u014B",   // yŋ ← 誤り
```
**修正**: `[Final.Iong] = "i\u028A\u014B"` (= "iʊŋ")

### 11.2 P1 学術標準準拠

#### zh/ch: tʂ/tʂʰ → ʈʂ/ʈʂʰ

**ファイル**: `PinyinToIpa.cs` 27-28行目
- 27行目: `[Initial.Zh] = "t\u0282"` → `"\u0288\u0282"` (ʈʂ)
- 28行目: `[Initial.Ch] = "t\u0282\u02B0"` → `"\u0288\u0282\u02B0"` (ʈʂʰ)

#### Final.cs コメント不整合（6箇所）

| # | Final.cs行 | コメント | PinyinToIpa.cs | 実際値 |
|---|-----------|---------|----------------|--------|
| 1 | 26 (Ao) | `[ɑʊ]` | 46行目 | `aʊ` |
| 2 | 38 (Ang) | `[ɑŋ]` | 50行目 | `aŋ` |
| 3 | 58 (Iao) | `[iɑʊ]` | 56行目 | `iaʊ` |
| 4 | 70 (Iang) | `[iɑŋ]` | 60行目 | `iaŋ` |
| 5 | 102 (Uang) | `[uɑŋ]` | 70行目 | `uaŋ` |
| 6 | 124 (Er) | `[ɑɻ]` | 76行目 | `əɻ` |

#### そり舌母音 ɻ̩ vs 歯茎母音 ɹ̩ の区別

**ファイル**: `PinyinToIpa.cs` 89-90行目、155-159行目

現行: 両方を `ɨ` で統一
修正: zh/ch/sh/r+i → `ɻ̩` (retroflex)、z/c/s+i → `ɹ̩` (alveolar)

### 11.3 P2 精密修正

#### üan (yan) → yɛn

**ファイル**: `PinyinToIpa.cs` 74行目
`[Final.Van] = "yan"` → `"y\u025Bn"` (yɛn)

### 11.4 修正箇所一覧表

| 優先度 | ファイル | 行 | 現行値 | 修正値 | 説明 |
|--------|---------|------|--------|--------|------|
| **P0** | PinyinToIpa.cs | 62 | yŋ | iʊŋ | iong韻母の致命的バグ |
| **P1** | PinyinToIpa.cs | 27 | tʂ | ʈʂ | zh retroflex |
| **P1** | PinyinToIpa.cs | 28 | tʂʰ | ʈʂʰ | ch retroflex |
| **P1** | PinyinToIpa.cs | 89-90 | ɨ統一 | ɻ̩/ɹ̩分離 | そり舌/歯茎母音区別 |
| **P1** | Final.cs | 6箇所 | ɑ系コメント | a系に統一 | コメント不整合 |
| **P2** | PinyinToIpa.cs | 74 | yan | yɛn | üan精密転写 |

### 11.5 判断が必要な点

1. zh/ch の ʈʂ vs tʂ: 簡略転写維持 or 精密転写移行
2. ɑ vs a: 方針A (PinyinToIpaをɑに) vs 方針B (Final.csコメントをaに)
3. そり舌/歯茎母音: ɨ統一維持 or ɻ̩/ɹ̩分離

---

## 12. 注音(Zhuyin/ボポモフォ)変換修正計画

### 12.1 致命的バグ: weng → 空文字列

**根本原因**: `PinyinToZhuyin.cs` の `GetShortFinalForW()` (L315-L328) で `weng` を `"ueng"` に変換するが、`s_finalMap` (L44-L92) に `"ueng"` エントリが存在しない。

**発生フロー**:
1. `Convert("wēng")` → bare = "weng"
2. `LookupFinal("ueng")` → **null** (マップにない)
3. `return string.Empty` ← 致命的バグ

**影響範囲**: wēng/wéng/wěng/wèng の読みを持つ漢字40+字（翁/嗡/蕹等）

**修正**: `s_finalMap` L85付近に `["ueng"] = "\u3128\u3125"` (ㄨㄥ) を追加

### 12.2 中優先: ê (U+00EA) → ㄝ

- `Final` enum に `Eh` 値追加
- `PinyinParser` に ê 認識追加
- `ToneConverter` に combining声調記号処理追加
- `s_finalMap` に `["ê"] = "\u311D"` 追加
- `PinyinToIpa` にも ê → [ɛ] マッピング追加

### 12.3 低優先: n/l + üe テストカバレッジ

追加テスト:
```csharp
[InlineData("lüè", "ㄌㄩㄝˋ")]
[InlineData("nüè", "ㄋㄩㄝˋ")]
```

### 12.4 全音節テスト計画

有効音節~410種のうち既存テスト~112件、追加~253件で網羅的検証。
`ZhuyinFullSyllableTests.cs` として新規作成。

### 12.5 修正箇所一覧表

| # | 優先度 | ファイル | 修正内容 |
|---|--------|---------|---------|
| 1 | **致命的** | PinyinToZhuyin.cs (s_finalMap) | `["ueng"] = "ㄨㄥ"` 追加 |
| 2 | 中 | Final.cs | `Eh` enum値追加 |
| 3 | 中 | PinyinParser.cs | ê→Final.Ehマッピング |
| 4 | 中 | ToneConverter.cs | ê combining声調記号対応 |
| 5 | 中 | PinyinToZhuyin.cs | `["ê"] = "ㄝ"` 追加 |
| 6 | 中 | PinyinToIpa.cs | `[Final.Eh] = "ɛ"` 追加 |
| 7 | 低 | ZhuyinConversionTests.cs | n/l+üeテスト追加 |

---

## 13. API再設計計画

### 13.1 現行APIの問題

- **コード重複~90%**: ToPinyin/ToIPA/ToZhuyin の差異は変換関数1行のみ
- **バッチAPI重複**: 9メソッドが同一forループパターン
- **オプション不足**: 正規化/分詞/児化音/軽声のON/OFF制御不可

### 13.2 RunPipeline() + RunPipelineList()

```csharp
private string RunPipeline(string text, Func<string, string> converter)
{
    ThrowIfDisposed();
    if (string.IsNullOrWhiteSpace(text)) return "";
    var entries = CollectPinyins(text);
    if (_options.EnableToneSandhi) ApplyToneSandhiToEntries(entries);
    return FormatOutput(entries, converter);
}

private string[] RunPipelineList(string text, Func<string, string> converter) { ... }
```

### 13.3 ChineseG2POptions 拡張

| プロパティ | 型 | デフォルト | 説明 |
|---|---|---|---|
| `EnableNormalization` | `bool` | `false` | テキスト正規化 |
| `EnableSegmentation` | `bool` | `true` | 分詞処理 |
| `EnableErhua` | `bool` | `false` | 児化音処理 |
| `ErhuaStyle` | `ErhuaStyle` | `None` | 児化音出力スタイル |
| `EnableYiNeutral` | `bool` | `true` | "一"軽声化 |
| `EnableBuNeutral` | `bool` | `true` | "不"軽声化 |
| `EnableNeutralToneWords` | `bool` | `false` | 軽声語彙リスト |

**後方互換性**: 全て named optional parameters、既存コンストラクタ呼び出し変更不要。

### 13.4 新規追加API

| メソッド | 説明 |
|---|---|
| `ToIPAList(string text)` | IPA配列出力 |
| `ToIPAList(string text, bool includeTones)` | 声調制御付き |
| `ToZhuyinList(string text)` | 注音配列出力 |
| `ToZhuyinList(string text, bool includeTones)` | 声調制御付き |

### 13.5 バッチAPI改善

ジェネリックバッチヘルパー:
```csharp
private IReadOnlyList<T> BatchProcess<T>(IReadOnlyList<string> texts, Func<string, T> processor)
```

パラメータ型 `string[]` → `IReadOnlyList<string>` に統一（英語G2Pと整合、後方互換）。

---

## 14. Multilingual統合改善計画

### 14.1 現行実装の課題

1. **簡体字/繁体字判定の欠如**: CJK漢字を一律`ScriptKind.CJKIdeograph`に分類
2. **CJK句読点の言語帰属が不正確**: U+3001-303F一括`ScriptKind.Japanese`
3. **DefaultCjkLanguageの限界**: かながない漢字列は常にフォールバック
4. **中国語出力形式の限定**: ConvertSegmentでToPinyinのみ

### 14.2 LanguageDetector 改善

- **簡体字固有文字検出**: ~200-300文字のテーブル（HashSet<char>またはビットベクトル）
- **ScriptKind.ChineseSimplified** 新設 → Language.Chinese直接返却
- **CJK句読点細分化**: `ScriptKind.CJKPunctuation` 新設、前後の確定言語に基づき帰属
- **注音符号検出**: U+3100-312F → ScriptKind.Chinese相当

### 14.3 TextSegmenter 改善

- かなチェーンリセット条件に `ScriptKind.ChineseSimplified` 追加
- 数字フォールバックを `defaultCjkByte` に変更

### 14.4 MultilingualG2PEngine 改善

- **遅延初期化**: `Lazy<ChineseG2PEngine>` で中国語テキスト初検出時に初期化
- **ChineseOutputFormat**: `Pinyin`/`IPA`/`Zhuyin` 切替

### 14.5 MultilingualG2POptions 拡張

| プロパティ | 型 | デフォルト | 説明 |
|---|---|---|---|
| `ChineseOutputFormat` | enum | `Pinyin` | 中国語出力形式 |
| `EnableChineseG2P` | `bool` | `true` | 中国語エンジン有効/無効 |
| `SimplifiedChineseDetection` | `bool` | `false` | 簡体字自動検出 |

### 14.6 実装優先度

| 優先度 | 項目 | 工数 |
|--------|------|------|
| P1 | ChineseG2PEngine遅延初期化 | 小 |
| P1 | 数字フォールバックのDefaultCjkLanguage対応 | 小 |
| P2 | ChineseOutputFormat追加 | 小 |
| P2 | CJK句読点ScriptKind細分化 | 中 |
| P3 | 簡体字固有文字検出 | 中 |
| P4 | CJK漢字連続列内の日中境界検出 | 大 |

---

## 15. パフォーマンス最適化計画

### 15.1 辞書アクセス最適化

| 施策 | 対象 | 期待効果 |
|------|------|----------|
| 配列インデックス化 | PinyinCharDictionary | ルックアップ30-50%高速化 |
| Span+ハッシュ | PinyinPhraseDictionary | Substring 90%削減 |
| Trie構造（Phase 3+） | PinyinPhraseDictionary | Substring完全排除 |

### 15.2 文字列処理最適化

**ValueStringBuilder導入箇所** (8箇所):

| ファイル | メソッド |
|---------|---------|
| ChineseG2PEngine.cs | ToPinyin/ToIPA/ToZhuyin (L145, L289, L355) |
| ToneConverter.cs | RemoveTone/ToToneMarked/ToToneNumber (L77, L132, L105) |
| PinyinToIpa.cs | ConvertSyllable (L126) |
| PinyinToZhuyin.cs | 各所の文字列連結 |

**string.Intern()**: 辞書パース時に適用。100万ピンイン→~1,300種に集約（メモリ ~20MB→~100KB）

### 15.3 メモリ最適化

- **WeakReferenceキャッシュ**: DictionaryBundleパターン
- **辞書一括読み込み**: StreamReaderバッファ64KB + Spanパース
- **AggressiveInlining**: 9箇所（IsCjkUnifiedIdeograph, TryLookup, GetToneFromChar 等）

### 15.4 バッチ処理最適化

- List<PinyinEntry>バッファ再利用（バッチ1000件: GC 99.9%削減）
- ApplyToneSandhi内の一時配列プーリング
- StringBuilder再利用

### 15.5 改善前後の性能目標

| 指標 | 現行推定値 | 目標値 | 改善率 |
|------|-----------|--------|--------|
| 単文変換 (4文字) | ~0.05ms | ~0.03ms | 40% |
| 辞書メモリ (1インスタンス) | ~60MB | ~40MB | 33% |
| 辞書メモリ (5インスタンス) | ~300MB | ~45MB | 85% |
| 辞書初期化 | ~500ms | ~300ms | 40% |
| バッチ1000件GC圧力 | ~3000 Gen0 | ~100 Gen0 | 97% |

### 15.6 優先順位

| 優先度 | 施策 | フェーズ | 実装コスト |
|--------|------|---------|-----------|
| P0 | string.Intern() | R4 | 低 |
| P0 | WeakReferenceキャッシュ | R4 | 中 |
| P1 | ValueStringBuilder導入 | R4 | 中 |
| P1 | AggressiveInlining | R4 | 低 |
| P1 | PinyinCharDictionary配列化 | R4 | 中 |
| P2 | FindLongestMatch Span+Hash | R4 | 中-高 |
| P2 | バッチバッファ再利用 | R4 | 中 |
| P3 | Trie構造フレーズ辞書 | R4+ | 高 |

---

## 16. テスト戦略計画

### 16.1 既存テスト概要

| 分類 | 件数 |
|------|------|
| ChineseG2Pテスト (19ファイル) | ~936件 |
| Multilingual中国語テスト | ~43件 |
| **合計** | **~979件** |

### 16.2 新規テスト計画 (~240件)

| カテゴリ | 件数 | 内容 |
|----------|------|------|
| 分詞テスト | ~30 | BiMM/DAG+DP精度、OOV、境界ケース |
| テキスト正規化テスト | ~50 | 数字/通貨/日時/パーセンテージ/全角半角 |
| 児化音テスト | ~20 | 韻母変換、ErhuaStyleモード、エッジケース |
| IPA修正テスト | ~30 | P0/P1/P2修正検証、回帰テスト |
| 注音修正テスト | ~30 | wengバグ、ê、全音節テスト |
| 声調変調拡張テスト | ~30 | V一V/A不A、軽声語彙、分詞連携 |
| パイプライン統合テスト | ~20 | フルパイプライン、段階ON/OFF組み合わせ |
| パフォーマンステスト | ~10 | 初期化速度/スループット/メモリ/GC |
| CPPベンチマーク/pypinyin比較 | ~20 | 精度メトリクス、回帰ベンチマーク |

### 16.3 テストファイル構成（再設計後）

```
tests/DotNetG2P.Tests/ChineseG2P/
├── [既存維持] 19ファイル (936件)
├── [既存拡張] ChinesePerformanceTests.cs (+10件)
├── [既存拡張] IpaConversionTests.cs (+18件)
├── [既存拡張] ZhuyinConversionTests.cs (+17件)
├── [既存拡張] ToneSandhiProcessorTests.cs (+13件)
├── [新規] ChineseSegmenterTests.cs (~30件)
├── [新規] ChineseNormalizerTests.cs (~50件)
├── [新規] ErhuaProcessorTests.cs (~20件)
├── [新規] IpaFixTests.cs (~12件)
├── [新規] ZhuyinFixTests.cs (~12件)
├── [新規] ToneSandhiExtendedTests.cs (~17件)
├── [新規] ChinesePipelineIntegrationTests.cs (~20件)
└── [新規] ChineseBenchmarkTests.cs (~20件)
```

### 16.4 テスト戦略要約

| 分類 | 既存 | 新規/拡張 | 合計 |
|------|------|----------|------|
| 基本API統合 | 205 | 0 | 205 |
| モデル単体 | 161 | 0 | 161 |
| 辞書 | 34 | 0 | 34 |
| 声調変調 | 37 | ~30 | ~67 |
| IPA変換 | 152 | ~30 | ~182 |
| 注音変換 | 133 | ~30 | ~163 |
| 分詞（新規） | 0 | ~30 | ~30 |
| 正規化（新規） | 0 | ~50 | ~50 |
| 児化音（新規） | 0 | ~20 | ~20 |
| パイプライン統合（新規） | 0 | ~20 | ~20 |
| ベンチマーク（新規） | 0 | ~20 | ~20 |
| **合計** | **979** | **~240** | **~1,219** |

### 16.5 テスト実行方針

- **ユニットテスト**: 全CI実行に含める
- **パフォーマンステスト**: `[Trait("Category", "Performance")]` で分離
- **ベンチマーク/精度テスト**: `[Trait("Category", "Benchmark")]` で分離
- **pypinyin比較テスト**: `tests/TestData/chinese_expected.json` に期待値事前生成

---

## 17. フェーズ別実装ロードマップ

### 17.1 前提

- C1-C6完了済み。再設計は **R1-R4** の4フェーズに分割
- **後方互換性維持**: 既存APIシグネチャ変更なし、デフォルト動作同一
- 各フェーズは前フェーズの完了を前提（R1→R2→R3→R4）

### R1: パイプライン再構築 + バグ修正

**目標**: コード重複解消、既知バグ修正

| # | タスク | 詳細 |
|---|--------|------|
| 1 | `RunPipeline()` 共有化 | ToPinyin/ToIPA/ToZhuyinの3重複ループを統合（~150行削減） |
| 2 | IPA P0バグ修正 (iong) | `PinyinToIpa.cs:62` iong→iʊŋ |
| 3 | Zhuyin wengバグ修正 | `PinyinToZhuyin.cs` s_finalMapに"ueng"→"ㄨㄥ"追加 |
| 4 | IPA P1修正 (zh/ch) | retroflex表記修正 |
| 5 | Final.csコメント修正 | 6箇所のɑ/a不整合解消 |
| 6 | ApplyStyle拡張 | PinyinStyle enum値との整合性確認 |

**テスト**: 既存936件パス + IPA/Zhuyin修正テスト~25件追加

**依存**: なし

---

### R2: 分詞エンジン + テキスト正規化

**目標**: 精度87%→90%

| # | タスク | 詳細 |
|---|--------|------|
| 1 | IWordSegmenter + BiMMベースライン | FMMラッパー → BiMM実装 |
| 2 | DAG + DP分詞 | Trie構築、頻度ベースDP |
| 3 | ChineseTextNormalizer | 数字/通貨/日時/パーセンテージ |
| 4 | パイプライン統合 | EnableNormalizationフラグ追加 |

**テスト**: 分詞~30件 + 正規化~50件 + 統合~15件 = ~95件追加

**依存**: R1

---

### R3: 声調変調強化 + 児化音

**目標**: 精度90%→92%+

| # | タスク | 詳細 |
|---|--------|------|
| 1 | V一V / A不A 軽声パターン | 動詞/形容詞重ね型の軽声化 |
| 2 | 軽声語彙リスト | ~500語（PaddleSpeech準拠） |
| 3 | ErhuaProcessor | 韻母融合テーブル、3モード対応 |
| 4 | 単語境界認識三声変調 | R2分詞結果を活用 |

**テスト**: V一V/A不A~15件 + 軽声~20件 + 児化音~25件 + 語境界~20件 = ~80件追加

**依存**: R2（分詞結果に依存）

---

### R4: パフォーマンス最適化 + テスト充実

**目標**: メモリ・速度の最適化

| # | タスク | 詳細 |
|---|--------|------|
| 1 | 辞書配列化 | PinyinCharDictionary O(1)アクセス |
| 2 | Span化 | Substring排除 |
| 3 | ValueStringBuilder導入 | 8箇所のStringBuilder置換 |
| 4 | string.Intern() | ピンイン文字列共有 |
| 5 | WeakReferenceキャッシュ | 辞書共有 |
| 6 | CPPデータセットベンチマーク | 精度測定 |
| 7 | パフォーマンスベンチマーク | 速度・メモリ測定 |

**テスト**: パフォーマンス~10件 + ベンチマーク~20件 + 回帰テスト全件パス

**依存**: R3

---

### 17.2 各フェーズの新規テスト数見積もり

| フェーズ | 新規テスト | 累計 |
|---------|----------|------|
| 既存（C1-C6） | - | 979件 |
| R1 | ~25件 | ~1,004件 |
| R2 | ~95件 | ~1,099件 |
| R3 | ~80件 | ~1,179件 |
| R4 | ~40件 | ~1,219件 |

### 17.3 後方互換性の保証方針

- **APIシグネチャ不変**: 全公開メソッドのシグネチャは変更しない
- **コンストラクタ不変**: 既存6つのオーバーロードを維持
- **デフォルト動作不変**: 新オプションは後方互換デフォルト値
- **スナップショットテスト**: R1開始前に既存テスト出力をスナップショット保存
- **バグ修正による出力変更**: CHANGELOGに明記（「正しい動作への修正」）

| オプション | デフォルト値 | 理由 |
|-----------|------------|------|
| `EnableNormalization` | `false` | 既存動作維持 |
| `EnableErhua` | `false` | ErhuaStyle.Noneと連動 |
| `SegmentationMode` | `Forward` | 既存の前方最長一致維持 |

---

## ライセンス対応

### THIRD-PARTY-NOTICES.md

```markdown
# Third-Party Notices

## pinyin-data
- Source: https://github.com/mozillazg/pinyin-data
- License: MIT
- Copyright (c) 2016 mozillazg

## phrase-pinyin-data
- Source: https://github.com/mozillazg/phrase-pinyin-data
- License: MIT
- Copyright (c) 2016 mozillazg

## pypinyin (参考実装)
- Source: https://github.com/mozillazg/python-pinyin
- License: MIT
- Note: アーキテクチャとアルゴリズムを参考。コードの直接移植はなし。

## csharp-pinyin (参考実装)
- Source: https://github.com/wolfgitpr/csharp-pinyin
- License: Apache-2.0
- Note: アルゴリズムを参考。辞書データは使用せず独自構築。
```

| データ/コード | ライセンス | 利用方法 |
|-------------|-----------|---------|
| pinyin-data | MIT | 辞書データとして埋め込み |
| phrase-pinyin-data | MIT | 辞書データとして埋め込み |
| pypinyin | MIT | アーキテクチャ参考のみ |
| CC-CEDICT | CC BY-SA 4.0 | **使用しない** |
