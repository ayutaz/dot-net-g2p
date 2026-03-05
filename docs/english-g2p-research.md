# 英語G2P実装に向けた調査レポート

Issue: [#1 espeak-ngと同等の精度の英語のg2p for C#を実装する](https://github.com/ayutaz/dot-net-g2p/issues/1)

## 目次

1. [espeak-ngの英語G2Pアーキテクチャ](#1-espeak-ngの英語g2pアーキテクチャ)
2. [英語G2Pの一般的なアプローチ比較](#2-英語g2pの一般的なアプローチ比較)
3. [既存のC#/.NET向け英語G2Pライブラリ](#3-既存のcnet向け英語g2pライブラリ)
4. [辞書データ・音素体系・ライセンス互換性](#4-辞書データ音素体系ライセンス互換性)
5. [推奨アプローチ](#5-推奨アプローチ)

---

## 1. espeak-ngの英語G2Pアーキテクチャ

### 概要

espeak-ngは1995年にJonathan Duddingtonが作成した元のeSpeakから2015年にフォークされた、C言語実装（コードベースの77.9%）のオープンソース音声合成エンジン。100以上の言語・アクセントに対応し、フォルマント合成方式を採用。英語G2Pは**ルールベース + 例外辞書のハイブリッド方式**。

- リポジトリ: https://github.com/espeak-ng/espeak-ng
- 最新版: 1.52.0（2024年12月）
- ライセンス: **GPL v3 or later**

### 処理パイプライン（5段階）

```
Stage 1: テキストパーサ (readclause.c)
  → 入力テキストをクローズ（文・句）に分割、SSML解析、数字・略語・記号の前処理

Stage 2: 翻訳レイヤ (translate.c, dictionary.c) ← G2Pの中核
  → TranslateWord() が中核関数
  → 1. LookupDictList(): ハッシュテーブル（1024エントリ）で例外辞書(en_dict)を検索
  → 2. TranslateRules(): 辞書ミス時、ルールベースの文字→音素変換をフォールバック適用
  → TranslateNumber(): 数字の音素変換を専門に処理

Stage 3: 音素処理 (phonemelist.c)
  → 韻律・ストレス・タイミング情報の付加
  → MakePhonemeList() でPHONEME_LIST2→PHONEME_LISTへ変換

Stage 4: 音声合成 (synthesize.c) ← G2Pでは不要
Stage 5: 波形生成 (wavegen.c, klatt.c) ← G2Pでは不要
```

**G2P（テキスト→音素）としてはStage 1〜3が該当。** Stage 4〜5は音声合成固有。

### ルールファイル・辞書ファイルの構造

espeak-ngの英語G2Pデータは `dictsource/` ディレクトリに格納:

| ファイル | 内容 | 行数 | サイズ |
|---------|------|------|--------|
| `en_rules` | Letter-to-Sound変換ルール（コンテキスト依存パターンマッチ） | 7,131行 | 158KB |
| `en_list` | 例外単語辞書（不規則発音、略語、固有名詞、記号名） | 5,789行 | 100KB |
| `en_extra` | 追加例外辞書 | — | ~20KB |
| `en_emoji` | 絵文字→テキスト変換 | — | 小 |

**コンパイルプロセス**: `en_rules` + `en_list` + `en_emoji` → `espeak-ng-data/en_dict`（バイナリ辞書）

#### ルール記法（en_rules）

`.group`による文字グループ構成でルールを整理:

```
.group a      // 'a'の発音ルール群
.group ab     // 'ab'の発音ルール群
.group b      // 'b'の発音ルール群
...
```

ルール構文:
```
[<pre>) <match> [(<post>] <phoneme string>
```
- `<pre>`: 前方コンテキスト（既に消費された文字）
- `<match>`: マッチする文字列（消費される）
- `<post>`: 後方コンテキスト（先読み、消費されない）
- `<phoneme string>`: 出力音素

特殊条件記号:

| 記号 | 意味 |
|------|------|
| `_` | 語境界 |
| `A` | 任意の母音 |
| `C` | 任意の子音 |
| `K` | 母音でない文字 |
| `?3` | General American方言条件 |
| `?5` | 母音バリエーション条件 |

ルール例:
```
     _) a (_       A:    // 単独の'a'は長母音
        a (tion    eI    // '-ation'の'a'は/eɪ/
   C) a (te _      eI    // 子音+a+te（語末）は/eɪ/
        a          a     // default "a" → /æ/
```

マッチングは**最長一致＋コンテキスト条件**でスコアリングされ、最高スコアのルールが適用される。

#### 例外辞書（en_list）

エントリ形式: `<word>\t<phoneme>\t[flags]`

登録内容:
- 文字名: `a eI`, `z zEd`
- 記号名: `@ at saIn`, `# haSh`
- 略語: `CEO`, `API`, `NASA`
- 外来語: `cafe`, `resume`
- 固有名詞: `Boise`, `Leonardo`
- 不規則動詞・名詞

主要フラグ:

| フラグ | 意味 |
|--------|------|
| `$verb` / `$noun` | 品詞条件（同綴異音語対応） |
| `$alt1`〜`$alt6` | 発音バリエーション |
| `$abbrev` | 略語処理 |
| `$pause` | 韻律マーカー |
| `$u` | 無ストレス |

### 音素体系

espeak-ngは**Kirshenbaum記法ベース**の独自音素体系を内部で使用し、4つの転写スキーム（IPA, X-SAMPA, CXS, Kirshenbaum）で出力可能。`--ipa`フラグでIPA出力。

**子音（24音素）:**

| espeak記号 | IPA | 例 |
|-----------|-----|-----|
| p | p | pat |
| b | b | bat |
| t | t | tap |
| d | d | dad |
| tS | tʃ | church |
| dZ | dʒ | judge |
| k | k | kit |
| g | g | got |
| f | f | fat |
| v | v | vat |
| T | θ | thin |
| D | ð | this |
| s | s | sit |
| z | z | zoo |
| S | ʃ | shop |
| Z | ʒ | pleasure |
| h | h | hat |
| m | m | mat |
| n | n | nap |
| N | ŋ | sing |
| l | l | lit |
| r | r | red |
| j | j | yes |
| w | w | wit |

**母音**: John Wells' Lexical Setsに基づく体系:
- 短母音 (7): KIT, DRESS, TRAP, LOT, STRUT, FOOT, BATH/CLOTH
- 長母音 (4): FLEECE, PALM, THOUGHT, GOOSE
- 二重母音 (5): FACE, PRICE, CHOICE, GOAT, MOUTH
- 弱化母音 (6+): HAPPY, COMMA, LETTER, EXPLORE, ROSES, BLESSED, RABBIT
- r母音 (6): NURSE, START, NORTH, FORCE, CURE, NEAR
- 合計約25〜30音素

**超音節要素**: ストレスマーク `'`（主強勢）, `,`（副強勢）, `%`（無ストレス）、ポーズ `_:`（短い休止）, `||`（語境界）

**方言対応**: `?3`（General American）等の条件フラグで同一ルールセットから複数方言（RP, GenAm, Scottish等7アクセント）の音素を出力可能。

### 主要ソースコード

```
src/libespeak-ng/          # コアライブラリ（68ファイル）
├── translate.c / .h       # テキスト→音素変換メインロジック (TranslateWord())
├── dictionary.c / .h      # 辞書検索＋ルール適用 (LookupDictList(), TranslateRules())
├── readclause.c / .h      # テキストパーサ（クローズ分割）
├── phonemelist.c          # 音素リスト構築 (MakePhonemeList())
├── synthesize.c / .h      # 音声合成パラメータ生成
├── wavegen.c              # 波形生成（フォルマント）
├── klatt.c                # Klatt合成器
├── phoneme.c / .h         # 音素定義・テーブル
├── espeak_api.c           # 公開API
├── speech.c / .h          # スピーチ制御
└── langopts.c             # 言語固有オプション（ストレスパターン・文字処理）

dictsource/                # 辞書ソースファイル
├── en_rules               # 英語発音ルール（7,131行 / 158KB）
├── en_list                # 英語例外辞書（5,789行 / 100KB）
└── en_emoji               # 絵文字辞書

phsource/                  # 音素定義
├── phonemes               # マスター音素テーブル
└── ph_english             # 英語音素定義
```

コード総量: 英語G2P関連部分は約10,000〜15,000行（共通インフラ含む）

### ライセンス制約

- espeak-ngは**GPL v3 or later**
- **コアソースコード、ルールファイル（en_rules）、辞書データ（en_list）すべてGPL v3が適用**
- 一部コンポーネント: BSD-2-Clause（getopt実装）、Unicode-DFS-2016、CC-BY-SA-3.0

| 方向 | 可否 | 説明 |
|------|------|------|
| Apache-2.0 → GPL v3 | OK | Apache-2.0コードをGPLプロジェクトに含められる |
| GPL v3 → Apache-2.0 | **NG** | GPLコードをApacheプロジェクトに含められない |
| GPLデータ → Apache-2.0 | **NG** | ルールファイル・辞書データもGPLの対象 |

**取りうる選択肢**:
1. **別ライセンスのデータ+独自実装（推奨）**: CMU辞書（BSD）+ 独自ルールエンジン → Apache-2.0でOK
2. **クリーンルーム再実装**: espeak-ngの仕様を読み独立にルールを実装 → 可能だが7,131行分を書き直すのは非現実的
3. **プロセス分離**: espeak-ngバイナリを外部プロセスとして呼び出し → 法的にグレー、ネイティブ依存が発生

### 精度（ベンチマーク）

2025年論文 "Fast, Not Fancy: Rethinking G2P" (EMNLP 2025) より:

| モデル | PER (音素エラー率) | 同綴異音語精度 | 推論速度 |
|--------|-------------------|---------------|---------|
| espeak-ng（ベースライン） | **6.92%** | 43.87% | 0.0169秒/文 |
| HomoFast eSpeak（改良版） | 6.33% | 74.53% | 0.0084秒/文 |
| GE2PE（ニューラル） | 3.98% | 76.89% | 0.4473秒/文 |

**強み**: 極めて高速（ニューラルの50倍以上）、小フットプリント（ルール+辞書で約258KB）、方言対応
**弱み**: 同綴異音語（homograph）の識別が弱い（43.87%）、文脈情報を利用しない

---

## 2. 英語G2Pの一般的なアプローチ比較

### アプローチ一覧

| アプローチ | PER | WER | 速度 | 未知語対応 | 実装難度(.NET) | モデルサイズ |
|-----------|-----|-----|------|-----------|--------------|------------|
| 辞書のみ | 0% (辞書内) | — | 極速 | 不可 | 極低 | ~3MB (CMUDict) |
| ルールベース | 6-7% | — | 極速 (0.01秒) | 可 | 中〜高 | ルール ~100KB |
| Neural (Seq2Seq/LSTM) | 4-5% | 23-28% | 遅い | 可（強） | 高 | 数MB〜数十MB |
| Neural (Transformer) | 2-5% | 20-25% | 遅い (0.4秒) | 可（強） | 高 | 数MB〜数百MB |
| ハイブリッド (辞書+ルール) | ~5-6% | — | 速い | 可 | 中 | ~3MB + ルール |
| ハイブリッド (辞書+Neural) | ~3-5% | — | 辞書ヒット時速/OOV時遅 | 可（強） | 高 | ~3MB + モデル |

### 各アプローチの詳細

#### 2.1 辞書ベース（Dictionary Lookup）

**CMU Pronouncing Dictionary**を使った単純なルックアップ方式。

- **利点**: 収録語に対しては100%正確、実装が極めてシンプル（Dictionary<string, string[]>への読み込みだけ）、レイテンシがほぼゼロ
- **欠点**: 未知語（辞書にない単語）に対応不可。CMUdictは約134,000語だが固有名詞・新語・技術用語のカバレッジが不足。同綴異音語（homograph: lead, bow, tear等）の文脈依存判別ができない
- **カバレッジ**: 英語一般テキストの90-95%がCMUDict内
- **使用例**: 多くのTTSシステムの第一段階

#### 2.2 ルールベース（Letter-to-Sound Rules）

espeak-ng方式。文字パターンに基づくルールで音素変換。

- **利点**: 未知語にも対応可能、モデルサイズが極小、推論エンジン不要、極めて高速
- **欠点**: ルール作成・メンテナンスに専門知識が必要、英語の不規則な綴りへの対応が困難、同綴異音語の文脈判別が弱い
- **精度**: PER 6-7%（espeak-ng）、同綴異音語精度43.87%
- **速度**: 0.01-0.02秒/文（ニューラルの50倍以上）
- **代表例**: espeak-ng、NETtalk（1987年）

#### 2.3 機械学習ベース

Seq2Seq（LSTM/GRU）やTransformerモデルを使用。

| モデル/手法 | PER (%) | WER (%) | 備考 |
|---|---|---|---|
| Joint n-gram (Phonetisaurus) | — | ~8% (精度~92%) | WFST方式 |
| Deep Bi-LSTM | 5.37 | 23.23 | — |
| CNN Encoder + Bi-LSTM Decoder | 4.81 | 25.13 | 残差接続あり |
| Transformer 4x4 | <5.0 | <23 | RNN系を上回る |
| Homo-GE2PE (Transformer) | 3.98 | — | 同綴異音語対応強化 |
| Multimodal (最先端) | 2.46 | — | 65%以上のPER削減 |

- **利点**: 最高精度（PER 2-5%）、未知語への強い汎化性能、同綴異音語のコンテキスト依存判別が可能
- **欠点**: 推論レイテンシが大きい（0.4秒/文）、モデルサイズ（数MB〜数百MB）、ランタイム依存、ブラックボックス
- **代表例**: DeepPhonemizer（Transformer）、Phonetisaurus（WFST）、g2pE

##### .NET/Unity環境でのML推論

| ランタイム | Unity対応 | モバイル | WebGL |
|-----------|----------|---------|-------|
| ONNX Runtime | △（ネイティブDLL、C# API公式あり） | △ | ✗ |
| Unity Inference Engine (旧Sentis) | ○（ONNX opset 7-15） | ○ | △ |
| TensorFlow Lite | △ | ○ | ✗ |
| 純C#推論 | ◎ | ◎ | ◎ |

MLアプローチは精度は高いが、Unity/WebGL環境での実現性に大きな制約がある。

#### 2.4 ハイブリッド（辞書 + ルール/ML）

実用上最も優れたアプローチ。辞書で既知語を処理し、未知語にはルールまたはMLでフォールバック。

**代表的実装:**

| 実装 | 方式 | ライセンス | 特徴 |
|-----|------|----------|------|
| **g2p_en** (Kyubyong/Mintplex-Labs) | CMU辞書 + LSTM seq2seq | Apache-2.0 | v2.0でTF依存除去（NumPy推論のみ） |
| **misaki** (hexgrad) | ルックアップ + ルール + configurable fallback | Apache-2.0 | Kokoro TTSのG2Pエンジン。Swiftポート(MisakiSwift)あり |
| **gruut** (Rhasspy) | SQLite辞書 + CRFモデル + POS tagging | MIT | 同綴異音語判別対応。13言語対応 |
| Festival/MaryTTS | 辞書 + LTSルール | MIT相当 | 学術系の標準 |

- **精度**: 辞書ヒット時100% + OOVフォールバック（ルール: PER ~6-7%, neural: PER ~4-5%）→ 実用的にPER 5%前後
- **利点**: 段階的に精度改善可能、解釈可能性が高い、フォールバック柔軟
- **短所**: 辞書+フォールバック両方のメンテナンスが必要

### .NET/Unity環境での推奨

Unity（特にモバイル・WebGL）を考慮すると、**ネイティブ依存やML推論エンジンを避ける**ことが重要:

1. **推奨**: ハイブリッド（CMU辞書 + ルールベースLTS）
   - 外部依存なし、軽量、Unity全プラットフォーム対応
   - espeak-ng自体がPER 6-7%のルールベースであり、辞書+ルールのハイブリッドなら**PER 5-6%でespeak-ng以上**が期待
2. **次善**: 辞書ベース（CMU辞書のみ）+ 簡易LTSルール
3. **高精度が必要な場合**: ハイブリッド（CMU辞書 + 軽量Seq2Seqモデル）+ Unity Sentis

---

## 3. 既存のC#/.NET向け英語G2Pライブラリ

### C#/.NET実装

**C#で英語G2Pを本格的に実装したライブラリは実質的に存在しない。**

| ライブラリ | 方式 | ライセンス | 状態 | 評価 |
|-----------|------|----------|------|------|
| **Microsoft.PhoneticMatching** | IPA変換(EnPronouncer) | MIT | 2018年以降更新なし | G2Pは副次的機能。主目的は音声類似度マッチング |
| **espeak-ng-wrapper** | P/Invoke | MIT | 2018年放棄（5コミット） | G2P機能なし。TTS合成のみ。Win64限定 |
| **eSpeak.NET** | コマンドラインラップ | 不明 | 2021年放棄（4コミット、未完成） | 利用不可 |
| **Phonix** | Soundex/Metaphone | — | — | G2Pではない。名前類似度比較用 |
| NuGetで「g2p」検索 | — | — | — | 英語G2P専用パッケージなし |

**espeak-ngの純C#ポート（ネイティブ依存なし）も存在しない。**

### 他言語の主要実装（移植候補）

#### Python

| ライブラリ | 方式 | ライセンス | 移植性 | 備考 |
|-----------|------|----------|--------|------|
| **g2p_en** | CMU辞書 + LSTM seq2seq | Apache-2.0 | ★★★★ | 辞書部分は即移植可。v2.0でTF除去 |
| **misaki** | ルックアップ + ルール + fallback | Apache-2.0 | ★★★ | Kokoro TTS用。Swift移植(MisakiSwift)の前例あり |
| **gruut** | SQLite辞書 + CRF + POS | MIT | ★★★★★ | 最も移植しやすい。ニューラルネット不要 |
| **epitran** | ルールベース + Flite | MIT | ★★ | 英語ではFlite(ネイティブ)依存 |
| **phonemizer** | espeak-ng/Festivalラッパー | GPL-3.0 | ★ | ネイティブ依存 |
| **DeepPhonemizer** | Transformer | MIT | ★★ | Transformerモデル必要 |
| **Aquila-Resolve** | CMU辞書 + Transformer + n-gram | — | ★★ | 同綴異音語に強いがTransformer依存 |

#### Rust

| ライブラリ | 方式 | ライセンス | 移植性 | 備考 |
|-----------|------|----------|--------|------|
| **Celosia** | AMEPD辞書 + POS tagger + Transformer | Apache-2.0/MIT | ★★★ | WIP（"DO NOT USE"）。アーキテクチャは参考に |
| **phonetisaurus-g2p-rs** | FST(有限状態トランスデューサ) | 不明 | ★★★ | FSTはC#再実装可能 |
| **deepphonemizer-rs** | DeepPhonemizerモデル | MIT | ★ | PyTorch依存 |
| **voirs-g2p** | VoiRS TTSの一部 | — | ★★ | alpha、未成熟 |
| espeak-ng Rustバインディング群 | FFIラッパー | — | ★ | 純RustのeSpeakは存在しない |

#### JavaScript/TypeScript

| ライブラリ | 方式 | ライセンス | 備考 |
|-----------|------|----------|------|
| **phonemize** (hans00) | 辞書 + LLM生成ルール | ISC | >10,000 words/sec。精度にムラ |
| **en-cmu-dict** | CMU辞書ルックアップ | MIT | — |
| **flite.js** | Flite移植（LTSルール含む） | MIT相当 | — |

#### その他の注目プロジェクト

| プロジェクト | 特徴 | ライセンス |
|-------------|------|----------|
| **OpenPhonemizer** (NeuralVox) | eSpeak互換、GPL非依存のIPA音素化（DeepPhonemizerベース） | BSD-3-Clause Clear |
| **piper-without-espeak** | Piper TTSからeSpeak依存除去 | — |

### 移植候補の優先順位

| 優先度 | 移植元 | 理由 |
|--------|--------|------|
| **1位** | **gruut（Python）** | MIT、辞書+CRF、ニューラルネット不要、POS-aware。CRFはC#実装が現実的。データ充実 |
| **2位** | **g2p_en（Python）** | Apache-2.0、CMU辞書+LSTM。辞書部分は即移植可。OOVモデルはONNX化で対応 |
| **3位** | **misaki（Python）** | Apache-2.0、アクティブ開発（415 stars）。Swift移植の前例あり。ただしspaCy依存 |
| **4位** | **Celosia（Rust）** | Apache-2.0/MIT、アーキテクチャ参考。ただしWIPで未成熟 |
| **参考** | **Flite/Festival LTSルール** | MIT相当。CARTツリー形式のLTSルールが公開。C#移植しやすい |

---

## 4. 辞書データ・音素体系・ライセンス互換性

### 4.1 CMU Pronouncing Dictionary

- **最新版**: 0.7b（2014年最終更新、コミュニティによる継続的更新あり）
- **収録語数**: 134,000語超（北米英語）
- **音素体系**: ARPAbet（39音素 + ストレスマーカー0/1/2）
- **フォーマット**: プレーンテキスト、`WORD  phoneme1 phoneme2 ...` 形式
- **ライセンス**: **「研究・商用を問わず使用は完全に無制限（completely unrestricted）」**（CMU著作権）
- **サイズ**: テキスト約3.69MB、ZIP圧縮で約926KB
- **入手先**: https://github.com/cmusphinx/cmudict
- **Apache-2.0互換**: **完全に可能**

```
HELLO  HH AH0 L OW1
WORLD  W ER1 L D
COMPUTER  K AH0 M P Y UW1 T ER0
```

### 4.2 espeak-ngの辞書データ

- **構造**: ルールベース（`en_rules`）+ 例外辞書（`en_list`/`en_extra`）→ コンパイル済み`en_dict`
- **サイズ**: espeak-ng全体で約2MB（全言語含む）。英語単独のen_dictは推定300-500KB
- **ライセンス**: **GPL v3**
- **Apache-2.0互換**: **不可能**

### 4.3 その他の英語発音辞書

| 辞書 | 収録語数 | 音素体系 | ライセンス | Apache-2.0互換 |
|-----|---------|---------|----------|---------------|
| **CMU Pronouncing Dictionary** | ~134,000 | ARPAbet | 無制限(BSD的) | **可能** |
| **Britfone** | ~16,000 | IPA(ストレス付き) | MIT | **可能** |
| **IPA-Dict (en-US)** | — | IPA | MIT | **可能** |
| **Gruut辞書** | — | IPA | MIT | **可能**（※GPL汚染リスクあり） |
| BEEP (British English) | ~250,000 | 独自 | 研究のみ | 不可 |
| UniSyn | ~120,000 | UniSyn | 非商用 | 不可 |
| IPA-Dict (en-GB) | — | IPA | GPL 3.0(一部) | 要確認 |

**結論**: Apache-2.0互換で実用的な英語発音辞書は**CMU Pronouncing Dictionaryが最有力**。英国英語対応にはBritfone（MIT、16K語）で将来的に補完可能。

### 4.4 音素体系の比較

| 体系 | 音素数 | 特徴 | 用途 |
|-----|-------|------|------|
| **IPA** | 多数 | 国際標準、Unicode対応 | 学術、多言語 |
| **ARPAbet** | 39 | ASCII、ストレス付き | CMUdict、米英語TTS |
| **espeak独自** | ~50 | Kirshenbaum拡張、長音記号(`:`)使用 | espeak-ng内部 |
| **X-SAMPA** | 多数 | ASCII表記のIPA | HTS、Amazon Polly |

#### ARPAbet音素一覧（39音素）

**母音（15種 + ストレス3段階: 0=無強勢, 1=第一強勢, 2=第二強勢）:**

| ARPAbet | IPA | 例 |
|---------|-----|------|
| AA | ɑ | f**a**ther |
| AE | æ | c**a**t |
| AH | ʌ/ə | b**u**t / **a**bout |
| AO | ɔ | c**au**ght |
| AW | aʊ | c**ow** |
| AY | aɪ | b**i**te |
| EH | ɛ | b**e**d |
| ER | ɝ | b**ir**d |
| EY | eɪ | f**a**ce |
| IH | ɪ | b**i**t |
| IY | i | fl**ee**ce |
| OW | oʊ | b**oa**t |
| OY | ɔɪ | b**oy** |
| UH | ʊ | b**oo**k |
| UW | u | g**oo**se |

**子音（24種）:**

| ARPAbet | IPA | 例 |
|---------|-----|------|
| B | b | **b**ed |
| CH | t͡ʃ | **ch**art |
| D | d | **d**ig |
| DH | ð | **th**en |
| F | f | **f**ive |
| G | ɡ | **g**ame |
| HH | h | **h**ouse |
| JH | d͡ʒ | **j**ump |
| K | k | **c**at |
| L | l | **l**ay |
| M | m | **m**ouse |
| N | n | **n**ap |
| NG | ŋ | thi**ng** |
| P | p | **p**in |
| R | ɹ | **r**ed |
| S | s | **s**eem |
| SH | ʃ | **sh**ip |
| T | t | **t**rap |
| TH | θ | **th**in |
| V | v | **v**est |
| W | w | **w**est |
| Y | j | **y**es |
| Z | z | **z**ero |
| ZH | ʒ | vi**s**ion |

#### 主要音素体系の対応表（米国英語）

| IPA | ARPAbet | X-SAMPA | espeak-ng |
|-----|---------|---------|-----------|
| ɑ | AA | A | A: |
| æ | AE | { | a |
| ʌ | AH | V | V |
| ə | AH0 | @ | @ |
| ɔ | AO | O | O: |
| aʊ | AW | aU | aU |
| aɪ | AY | aI | aI |
| ɛ | EH | E | E |
| ɝ | ER | 3` | 3: |
| eɪ | EY | eI | eI |
| ɪ | IH | I | I |
| i | IY | i | i: |
| oʊ | OW | oU | oU |
| ɔɪ | OY | OI | OI |
| ʊ | UH | U | U |
| u | UW | u | u: |

### 4.5 辞書サイズとUnity配布への影響

| データ | 非圧縮 | 圧縮後 | Unity適性 |
|-------|--------|--------|----------|
| CMU辞書（全量） | ~3.7MB | ~0.9-1.0MB (ZIP), ~0.7-0.8MB (Brotli) | ◎ 全プラットフォームOK |
| CMU辞書（高頻度語のみ） | ~1MB | ~500KB | ◎ |
| LTSルール | ~100-300KB | ~50-150KB | ◎ |
| Seq2Seqモデル（小） | — | 5-20MB | ○ モバイルでギリギリ |
| Transformerモデル | — | 50-500MB | ✗ |

**プラットフォーム別制約:**
- **デスクトップ**: 数MBは全く問題なし
- **モバイル**: CMUdict 3.7MB（圧縮1MB）は許容範囲。メモリ: 134K語×平均30バイト≒4MB
- **WebGL**: 最も制約が厳しい。Brotli圧縮で0.7-0.8MB。必要なら頻出語のみの縮小辞書も選択肢

**推奨**: CMUdictをバイナリTrie形式に変換して内蔵（naist-jdicと同様のアプローチ）。圧縮後1MB以内。

---

## 5. 推奨アプローチ

### 結論: CMU辞書 + ルールベースLTSのハイブリッド方式

### アーキテクチャ

```
英語テキスト入力
  → テキスト正規化（大文字→小文字、略語展開、数字読み等）
  → CMU辞書ルックアップ（134,000語）
  → ヒット → ARPAbet音素列を返す
  → ミス → LTS（Letter-to-Sound）ルールで音素推定
  → 音素列出力（ARPAbet / IPA / X-SAMPA変換可能）
```

### 選定理由

| 評価軸 | CMU辞書+LTSルール |
|-------|-------------------|
| **精度** | PER 5-6%（既知語100% + 未知語ルール推定）。espeak-ng（PER 6.92%）と同等以上 |
| **ライセンス** | CMU辞書=無制限(BSD的)、ルール=独自実装 → Apache-2.0 ✓ |
| **外部依存** | なし（純C#） |
| **Unity対応** | 全プラットフォーム ✓（モバイル・WebGL含む） |
| **サイズ** | ~2MB以下（辞書圧縮1MB + ルール100KB） |
| **日本語G2Pとの一貫性** | 辞書参照→ルール処理という同じアーキテクチャパターン |
| **段階的拡張性** | 辞書拡充、ルール改善、将来的にMLフォールバック追加も可能 |

### LTSルールの実装方法の選択肢

| 方法 | 利点 | 欠点 |
|-----|------|------|
| **Flite/Festival LTSツリー移植** | MIT相当ライセンス、CARTツリー形式で移植しやすい | — |
| **gruut方式（辞書+CRF）移植** | MIT、ニューラルネット不要、POS-aware | CRFの実装が必要 |
| **CMU辞書からLTSルール自動学習** | Phonetisaurus等のWFST方式 | 訓練パイプラインが必要 |
| **独自コンテキスト依存ルール** | 完全にライセンスクリーン | ルール作成の工数大 |

### 段階的実装計画（案）

| フェーズ | 内容 | 期待精度 |
|---------|------|---------|
| Phase 1 | CMU辞書ルックアップ + 基本テキスト正規化 | 辞書内100%、OOV 0% |
| Phase 2 | 基本LTSルール追加（最頻出パターン） | PER ~85% |
| Phase 3 | LTSルール拡充（コンテキスト依存） | PER ~92% |
| Phase 4 | ストレス推定、同綴異音語（POS判別） | PER ~95% |
| Phase 5 | IPA/X-SAMPA出力対応、日本語G2Pとの統合API | — |

### パッケージ構成（案）

```
src/
├── DotNetG2P.Core/           # 既存（日本語G2P）
├── DotNetG2P.MeCab/          # 既存（MeCabエンジン）
└── DotNetG2P.EnglishG2P/     # 新規（英語G2P）
    ├── CmuDictionary.cs      # CMU辞書ルックアップ
    ├── LetterToSound.cs      # LTSルールエンジン
    ├── EnglishG2PEngine.cs   # メインAPI
    ├── ArpabetToIpa.cs       # ARPAbet→IPA変換
    ├── Models/
    │   └── ArpabetPhoneme.cs # ARPAbet音素enum
    └── Data/
        └── cmudict.txt       # CMU辞書データ（BSD的ライセンス）
```

### リスクと課題

1. **LTSルールの品質**: espeak-ngの7,131行ルールに匹敵するルールセットの構築には相当な工数が必要
2. **同綴異音語**: "read"(リード/レッド)、"live"(ライヴ/リヴ)等の判別にはPOSタグ等の文脈情報が必要
3. **ストレス推定**: 英語の強勢パターンは複雑で、ルールだけでは限界がある
4. **固有名詞**: 人名・地名等は辞書にもルールにも対応困難
5. **英語方言**: CMUdictは北米英語のみ。英国英語対応は将来課題
6. **テスト基盤**: 精度評価のためのテストセット（gold standard）の準備が必要

---

## 参考文献・ソース

- [A Survey of Grapheme-to-Phoneme Conversion Methods (MDPI 2024)](https://www.mdpi.com/2076-3417/14/24/11790)
- [Fast, Not Fancy: Rethinking G2P (EMNLP 2025)](https://arxiv.org/html/2505.12973v1)
- [espeak-ng GitHub](https://github.com/espeak-ng/espeak-ng)
- [CMU Pronouncing Dictionary](https://github.com/cmusphinx/cmudict)
- [g2p_en (Kyubyong Park)](https://github.com/Kyubyong/g2p)
- [Misaki G2P](https://github.com/hexgrad/misaki)
- [MisakiSwift (Swift port)](https://github.com/mlalma/MisakiSwift)
- [Gruut](https://rhasspy.github.io/gruut/)
- [Microsoft.PhoneticMatching](https://github.com/microsoft/PhoneticMatching)
- [DeepPhonemizer](https://github.com/as-ideas/DeepPhonemizer)
- [Phonetisaurus](https://github.com/AdolfVonKleist/Phonetisaurus)
- [ONNX Runtime C# API](https://onnxruntime.ai/docs/get-started/with-csharp.html)
- [Unity Inference Engine (Sentis)](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.2/manual/)
- [G2P Shrinks Speech Models (Hugging Face Blog)](https://huggingface.co/blog/hexgrad/g2p)
