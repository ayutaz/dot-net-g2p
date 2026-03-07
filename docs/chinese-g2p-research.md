# 中国語G2P（ピンイン変換）調査報告

> 調査日: 2026-03-07
> 目的: pypinyin同等精度の中国語G2PをC#で実装するための技術調査

---

## 目次

1. [主要ライブラリ比較](#1-主要ライブラリ比較)
2. [精度ベンチマーク](#2-精度ベンチマーク)
3. [辞書データ](#3-辞書データ)
4. [コアアルゴリズム](#4-コアアルゴリズム)
5. [既存C#ライブラリ](#5-既存c中国語ライブラリ)
6. [pypinyinソースコード分析](#6-pypinyinソースコード分析)
7. [DotNetG2P.Chinese設計案](#7-dotnetg2pchinese設計案)
8. [実装ロードマップ](#8-実装ロードマップ)
9. [リスクと課題](#9-リスクと課題)

---

## 1. 主要ライブラリ比較

### 概要一覧

| ライブラリ | 言語 | アーキテクチャ | 精度(CPP) | ライセンス | GitHub Stars | 依存の重さ |
|-----------|------|---------------|-----------|-----------|-------------|-----------|
| **pypinyin** | Python | 辞書+フレーズマッチ | ~86-87% | MIT | ~5,200 | なし（純Python） |
| **g2pM** | Python | Bi-LSTM | 97.31% | Apache-2.0 | ~350 | NumPyのみ |
| **g2pW** | Python | BERT+Weighted Softmax | 99.07% | Apache-2.0 | - | transformers, onnxruntime |
| **g2pC** | Python | CRF | 84.84% | Apache-2.0 | ~206 | pkuseg, sklearn |
| **xpinyin** | Python | 単純辞書 | 最低 | MIT | ~830 | なし |
| **phonemizer** | Python | espeak-ngラッパー | 低 | GPL-3.0 | - | espeak-ng |
| **csharp-pinyin** | C# | 辞書マッチ最適化 | 90.3% | Apache-2.0 | ~7 | なし |

### 詳細

#### pypinyin (python-pinyin)
- **リポジトリ**: [mozillazg/python-pinyin](https://github.com/mozillazg/python-pinyin)
- **最新版**: 0.55.0 (2025年7月)、活発にメンテナンス
- 漢字→ピンイン変換（声調記号/数字/無声調など18種の出力スタイル）
- 声母(initials)・韻母(finals)の分離出力
- 繁体字・簡体字・注音（BoPoMoFo）対応
- カスタム辞書で拡張可能（`load_phrases_dict`, `load_single_dict`）
- **多音字処理**: フレーズ辞書による文脈マッチング（ルールベースのため複雑な文脈依存は苦手）
- pypinyin-g2pWプロジェクトでg2pWモデルとの統合も可能
- **C#移植の参考として最適**: 軽量・依存なし・アーキテクチャがシンプル

#### g2pM (kakaobrain)
- **リポジトリ**: [kakaobrain/g2pM](https://github.com/kakaobrain/g2pM)
- **論文**: INTERSPEECH 2020
- Bi-LSTMニューラルネットワーク、NumPyのみで推論（PyTorch不要）
- モデルサイズ1.7MB、パッケージ2.1MB
- 99,000+文のCPPベンチマークデータセットを公開（重要な貢献）
- **C#移植**: Bi-LSTM推論をC#で実装する必要あり（技術的には可能だがモデル依存）

#### g2pW (INTERSPEECH 2022)
- **リポジトリ**: [GitYCC/g2pW](https://github.com/GitYCC/g2pW)
- BERT + Conditional Weighted Softmax、現時点で最高精度の公開モデル
- ONNX Runtime推論対応
- PaddleSpeechにも統合済み
- **C#移植**: ONNX Runtimeで可能だがモデルサイズが大きくUnity対応困難

#### csharp-pinyin (wolfgitpr) ★注目
- **リポジトリ**: [wolfgitpr/csharp-pinyin](https://github.com/wolfgitpr/csharp-pinyin)
- **ライセンス**: Apache-2.0
- C#ネイティブ実装、外部依存なし
- **精度**: 90.3%（with-tone）、99.9%（without-tone）
- **速度**: 約50万字/秒
- 広東語対応、声調スタイル複数、繁→簡変換
- **DotNetG2P.Chineseの参考・統合候補として最も有力**

---

## 2. 精度ベンチマーク

### CPPベンチマーク（g2pM提供、~99,000文）

| ライブラリ | With-Tone精度 | Without-Tone精度 | アーキテクチャ |
|-----------|-------------|-----------------|---------------|
| LLM-based (最新研究) | 99.29% | - | LLM |
| **g2pW** | **99.07%** | - | BERT |
| Chinese BERT | ~97.85% | - | BERT |
| **g2pM** | **97.31%** | - | Bi-LSTM |
| **csharp-pinyin** | **90.3%** | **99.9%** | 辞書マッチ |
| Majority vote | 92.08% | - | 統計 |
| **pypinyin** | **~86-87%** | - | フレーズ辞書 |
| g2pC | 84.84% | - | CRF |
| xpinyin | 78.56% | - | 単純辞書 |

### 精度の解釈
- **辞書+ルールベース**: 87-90%が現実的上限
- **ニューラルモデル**: 97-99%だがモデル依存・重い
- **csharp-pinyin方式**: 辞書最適化で90.3%を達成（C#で到達可能な最高水準）

---

## 3. 辞書データ

### 利用可能なオープンデータ

| 辞書 | エントリ数 | ライセンス | Apache-2.0互換 | 用途 |
|------|----------|-----------|---------------|------|
| **pinyin-data** (mozillazg) | ~42,000字 | MIT | 互換 | 単字ピンイン |
| **phrase-pinyin-data** (mozillazg) | 数万フレーズ | MIT | 互換 | フレーズピンイン（多音字解決） |
| **Unicode Unihan** | kHanyuPinyin: ~34,131字 | Unicode License | 互換 | 漢字→ピンイン（最も網羅的） |
| **CC-CEDICT** | ~124,000語句 | CC BY-SA 4.0 | 要注意（帰属表示+SA） | 語句レベルピンイン |

### 推奨データソース
- **メイン**: pinyin-data + phrase-pinyin-data（共にMIT、pypinyinと同一ソース）
- **補完**: Unicode Unihan（レア漢字カバー）
- **CC-CEDICT**: 使用する場合はNOTICEに帰属表示が必要（CC BY-SA 4.0）

### データフォーマット

**pinyin-data**: `U+4E2D: zhōng,zhòng  # 中` 形式
**phrase-pinyin-data**: `世界: shì jiè` 形式
**CC-CEDICT**: `國 国 [guo2] /country/` 形式

### 辞書サイズ見積もり（埋め込みリソース）
- 単字辞書: ~42,000エントリ → テキスト ~500KB / バイナリ ~300KB
- フレーズ辞書: ~100,000エントリ → テキスト ~3MB / バイナリ ~2MB
- **合計: ~2.5MB**（CMU辞書3.6MBより小さい）

---

## 4. コアアルゴリズム

### 4.1 多音字（Polyphone）解決

中国語G2Pの最大の技術課題。約982字の多音字が存在（新華字典で734字、全体の約10%）。

#### アプローチ比較

| 手法 | 精度 | ML依存 | 実装難易度 | C#実装 |
|------|------|--------|-----------|--------|
| 最頻出読み選択 | 92% | なし | 極低 | 容易 |
| 前方最長一致（pypinyin） | 86-87% | なし | 中 | 容易 |
| 最適化辞書マッチ（csharp-pinyin） | 90.3% | なし | 中 | 既存実装あり |
| Bi-LSTM（g2pM） | 97.3% | あり | 高 | 可能だが重い |
| BERT（g2pW） | 99.1% | あり | 極高 | ONNX経由で可能 |

#### pypinyin方式の詳細アルゴリズム
1. **PrefixSet構築**: フレーズ辞書の全語句の全接頭辞をSetに格納（O(1)検索）
2. **前方最長一致（Forward Maximum Matching）**: テキストを左→右にスキャン、最長辞書一致語を優先
3. **フォールバック**: 語句未マッチ → 単字辞書から最頻出読みを返す

### 4.2 分詞（Word Segmentation）

| 手法 | 精度 | 実装コスト | 備考 |
|------|------|-----------|------|
| 前方最長一致 | ~95% | 低 | pypinyinのmmsegモジュールが採用 |
| jieba（DAG+DP+HMM） | ~97% | 高 | フル実装は複雑 |
| pkuseg | ~97%+ | 極高 | ニューラルモデル |

**推奨**: フレーズ辞書の前方最長一致で十分（分詞とピンイン変換を一体化）。

### 4.3 声調サンディ（Tone Sandhi）

ルールベースで100%正確に実装可能。

| ルール | 条件 | 変化 | 例 |
|--------|------|------|-----|
| **三声変調** | 3声+3声 | → 2声+3声 | 你好 nǐhǎo → níhǎo |
| **"一"変調** | 一+4声 | → 2声 | 一次 yīcì → yícì |
| **"一"変調** | 一+1/2/3声 | → 4声 | 一般 yībān → yìbān |
| **"一"軽声** | 動詞+一+動詞 | → 軽声 | 看一看 kàn yi kàn |
| **"不"変調** | 不+4声 | → 2声 | 不是 bùshì → búshì |
| **"不"軽声** | 動詞+不+動詞 | → 軽声 | 走不走 zǒu bu zǒu |
| **三声連続** | 3+連続 | 最後以外→2声 | 語境界依存 |

### 4.4 軽声処理

辞書+ルールで~95%以上の精度が可能。

| カテゴリ | 例 | 判定方法 |
|---------|-----|---------|
| **助詞** | 了(le)、吗(ma)、的(de)、着(zhe)、过(guo) | 常に軽声 |
| **接尾辞** | 子(zi)、们(men)、头(tou) | 通常軽声 |
| **重ね型** | 妈妈(māma)、爸爸(bàba) | 第2音節が軽声 |
| **特定語彙** | 朋友(péngyou)、聪明(cōngming) | 辞書で指定 |

### 4.5 儿化音（Erhua）

辞書ベースで処理可能。CC-CEDICTは儿化語彙を含む。

- ピンイン末尾に `-r` を付加（例: 花儿 huār）
- テキスト中の「儿」が接尾辞か独立字かの判別が必要（辞書マッチングで対応）

---

## 5. 既存C#中国語ライブラリ

| ライブラリ | ライセンス | .NET Standard 2.1 | 多音字対応 | メンテナンス | 評価 |
|-----------|-----------|-------------------|-----------|------------|------|
| **csharp-pinyin** (wolfgitpr) | **Apache-2.0** | 要確認 | **対応**（候補返却） | 低活動 | **最有力参考** |
| pinyin4net | MIT | 対応 | 文字単位のみ | 低活動 | 限定的 |
| TinyPinyin.Net | 不明 | 対応 | 非対応 | 低活動 | 高速だが機能不足 |
| ToolGood.Words.Pinyin | 不明 | 要確認 | 部分対応 | 中程度 | 高性能フィルタ付き |
| NPinyin.Core | 不明 | .NET Core対応 | 非対応 | 低活動 | 基本のみ |
| NChinese | LGPL v2.1 | 要確認 | 非対応 | 中程度 | 注音対応 |

### 評価
**既存C#ライブラリにはpypinyin相当のフレーズ辞書ベース多音字解決を実装したものがない。** csharp-pinyinが90.3%精度で最も近いが、DotNetG2P.Chineseとして統合的なTTS向けG2Pパッケージを新規設計する価値がある。

---

## 6. pypinyinソースコード分析

### モジュール構成

```
pypinyin/
├── core.py              # メインAPI（pinyin(), lazy_pinyin()）
├── converter.py         # DefaultConverter/UltimateConverter（変換ロジック）
├── constants.py         # Style enum（18種の出力形式）
├── pinyin_dict.py       # 単字ピンイン辞書（~42,000エントリ）
├── phrases_dict.py      # フレーズ辞書（多音字解決用、数万エントリ）
├── phonetic_symbol.py   # 声調記号処理
├── standard.py          # ピンイン標準規則
├── seg/                 # 分詞モジュール（jieba等と統合）
├── style/               # 出力スタイル変換（18種）
│   ├── _utils.py
│   ├── _constants.py
│   └── _tone_convert.py
├── contrib/
│   └── tone_sandhi.py   # 声調変調
└── legacy/              # 後方互換
```

### 変換パイプライン

```
入力テキスト
  → 分詞（seg/: 前方最長一致 or jieba）
  → フレーズ辞書照合（phrases_dict: 最長一致で多音字解決）
  → 単字辞書フォールバック（pinyin_dict: コードポイント→ピンイン）
  → スタイル変換（style/: TONE/NORMAL/INITIALS等18形式）
  → [オプション] 声調変調（contrib/tone_sandhi）
```

### 出力スタイル（18種）

| Style | 例（"中"） | 説明 |
|-------|----------|------|
| NORMAL | zhong | 声調なし |
| TONE | zhōng | 声調記号付き |
| TONE2 | zho1ng | 声調数字（母音後） |
| TONE3 | zhong1 | 声調数字（音節末） |
| INITIALS | zh | 声母のみ |
| FINALS | ong | 韻母のみ |
| FINALS_TONE | ōng | 韻母+声調記号 |
| FIRST_LETTER | z | 頭文字のみ |
| BOPOMOFO | ㄓㄨㄥ | 注音符号 |
| CYRILLIC | чжун1 | キリル文字 |

---

## 7. DotNetG2P.Chinese設計案

### パッケージ構造

```
src/DotNetG2P.Chinese/
├── DotNetG2P.Chinese.csproj             # .NET Standard 2.1、Core参照なし（独立）
├── ChineseG2PEngine.cs                  # メインAPI（IDisposable）
├── ChineseG2POptions.cs                 # オプション
├── Models/
│   ├── PinyinStyle.cs                   # 出力スタイルenum
│   ├── PinyinSyllable.cs               # 音節構造体（initial + final + tone）
│   ├── PinyinResult.cs                  # ピンイン結果
│   └── Tone.cs                          # 声調enum (1-4 + 軽声)
├── Dictionary/
│   ├── PinyinDictionary.cs              # 単字辞書ルックアップ
│   ├── PhraseDictionary.cs              # フレーズ辞書（多音字解決）
│   └── Data/
│       ├── pinyin_dict.dat              # 単字辞書（EmbeddedResource）
│       └── phrases_dict.dat             # フレーズ辞書（EmbeddedResource）
├── Conversion/
│   ├── ToneConverter.cs                 # 声調記号⇔数字変換
│   ├── BopomofoConverter.cs             # ピンイン→注音変換
│   └── IpaConverter.cs                  # ピンイン→IPA変換
├── ToneSandhi/
│   └── ToneSandhiProcessor.cs           # 声調変調処理
├── package.json                         # UPM (com.dotnetg2p.chinese)
└── DotNetG2P.Chinese.asmdef             # Unity Assembly Definition
```

### 設計方針
- **DotNetG2P.Englishと同じパターン**: Core参照なし（独立パッケージ）
- **辞書埋め込み**: EmbeddedResource（合計~2.5MB）
- **IDisposable**: 辞書メモリ解放
- **.NET Standard 2.1**: Unity 2021.2+互換

### Multilingualパッケージへの統合
- `Language` enumに `Chinese = 2` を追加
- `LanguageDetector`: 漢字のみの場合の判定ロジック改修（ひらがな/カタカナ併存→Japanese、それ以外→Chinese or 設定依存）
- 日中英混在テキスト対応

---

## 8. 実装ロードマップ

### C1: 基本ピンイン変換MVP
**目標**: `g2p("你好世界")` → `"nǐ hǎo shì jiè"`
- 単字ピンイン辞書（pinyin-dataベース）の埋め込み
- PinyinDictionary: コードポイント→ピンイン配列ルックアップ
- ChineseG2PEngine: 基本API（ToPinyin, ToLazyPinyin）
- PinyinStyle: TONE/NORMAL/TONE3の3形式
- テスト: 基本変換100件

### C2: フレーズ辞書と多音字解決
**目標**: `g2p("重要")` → `"zhòng yào"` (×`chóng yào`)
- フレーズ辞書（phrase-pinyin-dataベース）の埋め込み
- PhraseDictionary: 前方最長一致フレーズ検索
- 多音字解決: フレーズ辞書→単字辞書のフォールバック
- テスト: 多音字テスト50件

### C3: 出力形式の充実
**目標**: 7+出力スタイル対応
- INITIALS/FINALS/FINALS_TONE: 声母・韻母分離
- BOPOMOFO: 注音符号変換
- FIRST_LETTER: 頭文字抽出
- IPA: 国際音声記号変換
- テスト: 全スタイル検証

### C4: 声調変調（Tone Sandhi）
**目標**: 自然な声調処理
- 三声連読変調、"一"/"不"の変調ルール
- 軽声処理、儿化音処理
- テスト: 声調変調30件

### C5: テスト・品質保証
**目標**: pypinyinとの比較検証
- pypinyin出力との比較テスト（100文以上）
- CPPベンチマークデータセットでの精度測定
- エッジケース: 句読点、英数字混在、空文字列、繁体字
- パフォーマンステスト
- バッチAPI追加

### C6: パッケージング・Multilingual統合
**目標**: NuGet + UPM + Multilingual対応
- NuGet: DotNetG2P.Chinese
- UPM: com.dotnetg2p.chinese
- DotNetG2P.Multilingual: Language.Chinese追加、LanguageDetector改修
- 日中英混在テキスト対応

---

## 9. リスクと課題

### 精度に関するリスク
| リスク | 影響 | 対策 |
|--------|------|------|
| 辞書ベースの多音字精度上限（~90%） | TTS品質に影響 | csharp-pinyin方式で90%確保、将来的にONNXモデル統合オプション |
| 声調サンディの語境界依存 | 自然さの低下 | 分詞精度向上、ルール精緻化 |
| 繁体字対応 | カバー率低下 | 繁→簡変換テーブル追加 |

### 技術的課題
| 課題 | 対策 |
|------|------|
| 辞書データサイズ（~2.5MB） | バイナリ圧縮、遅延読み込み |
| 漢字の日中判別（Multilingual統合時） | 文脈ヒューリスティック + デフォルト設定 |
| .NET Standard 2.1制約 | Span<T>等の制限を考慮した設計 |

### ライセンスリスク
| データ | ライセンス | リスク | 対策 |
|--------|-----------|--------|------|
| pinyin-data / phrase-pinyin-data | MIT | なし | メインデータソースとして使用 |
| CC-CEDICT | CC BY-SA 4.0 | ShareAlike条項 | 使用する場合はNOTICE記載、またはMITデータのみで構築 |
| Unicode Unihan | Unicode License | 低 | 帰属表示 |
| csharp-pinyin | Apache-2.0 | なし | 参考実装として活用可能 |

### 推奨戦略
1. **Phase 1（C1-C2）**: pypinyin方式（MIT辞書 + 前方最長一致）で ~87-90% 精度を達成
2. **Phase 2（C3-C6）**: csharp-pinyin のアルゴリズム参考で最適化、90%+ を目指す
3. **Phase 3（将来）**: g2pMのBi-LSTMモデル統合オプション（97%精度、オプトイン）
