# 日本語G2Pシステム総合比較と最適アーキテクチャ提案

## 1. 調査対象システムの概要

本調査では、以下の日本語G2P関連システム・ライブラリを調査した。

| # | システム | 言語 | アプローチ | 調査ドキュメント |
|---|---------|------|-----------|----------------|
| 1 | OpenJTalk | C | ルールベース（MeCab+NJD+JPCommon） | 01, 02 |
| 2 | pyopenjtalk | Python/Cython | OpenJTalkラッパー | 02 |
| 3 | jpreprocess | Rust | OpenJTalk再実装（型安全設計） | 03 |
| 4 | ESPnet G2P | Python | pyopenjtalk + 韻律記号抽出 | 04 |
| 5 | VOICEVOX | Python | pyopenjtalk + 独自拡張 | 08 |
| 6 | AivisSpeech | Python | VOICEVOX派生 + 高度テキスト正規化 | 09 |
| 7 | Style-Bert-VITS2 | Python | BERT統合G2P | 09 |
| 8 | NMeCab/MeCab.DotNet | C# | MeCab互換形態素解析 | 06 |
| 9 | UniDic | - | 29フィールド辞書（アクセント情報内蔵） | 07 |
| 10 | naist-jdic | - | IPADIC+2フィールド拡張辞書 | 14 |
| 11 | ONNX Runtime | C# | ニューラルモデル推論基盤 | 12 |
| 12 | KokoroSharp等 | C# | 既存C#日本語処理ライブラリ群 | 13 |

---

## 2. G2Pアプローチの比較

### 2.1 ルールベース vs ニューラル vs ハイブリッド

| 観点 | ルールベース（OpenJTalk系） | ニューラル（ONNX等） | ハイブリッド |
|------|--------------------------|--------------------|-----------|
| **精度（既知語）** | 高い（辞書依存） | 中〜高 | 最高 |
| **精度（未知語）** | 低い | 高い | 高い |
| **推論速度** | 非常に高速 | 中程度（CPU: 1-10ms/文） | 高速 |
| **辞書サイズ** | 約80MB（naist-jdic） | 5-50MB（モデル依存） | 80MB + α |
| **実装複雑度** | 中（NJD処理6段階） | 低（ONNX推論のみ） | 高 |
| **Unity WebGL** | 動作可能 | **ONNX Runtime未対応** | 制限あり |
| **デバッグ容易性** | 高（ルール明示的） | 低（ブラックボックス） | 中 |

### 2.2 結論

**ルールベース（OpenJTalk互換）をコアとし、ニューラルは未知語補助としてオプション提供するのが最適。**

根拠:
- 日本語は漢字の多読み・形態素境界の曖昧性があり、辞書+ルールベースが最も信頼性が高い
- ESPnet・VOICEVOX等の主要TTSシステムも全てOpenJTalkベースのルールG2Pを採用
- Unity WebGLでのONNX Runtime未対応を考慮すると、ルールベースのみで完結する設計が必須
- ニューラルG2Pはニューラルモデル単体での日本語G2Pの十分な精度は未確認

---

## 3. 辞書選択の比較

### 3.1 naist-jdic vs UniDic

| 観点 | naist-jdic（OpenJTalk拡張） | UniDic |
|------|---------------------------|--------|
| **フォーマット** | IPADIC + 2フィールド（15フィールド） | 29フィールド |
| **アクセント情報** | `核位置/モーラ数` + `C1-C5` | `aType` + `aConType` + `aModType` |
| **既存NJD処理** | OpenJTalk/jpreprocessで実装済み | **独自実装が必要** |
| **辞書サイズ** | 約80MB | 約133MB |
| **語彙カバー率** | 約39万語 | UniDicの方が大きい |
| **ライセンス** | BSD | GPL/LGPL/BSD（トリプル） |
| **エコシステム互換性** | OpenJTalk/VOICEVOX互換 | 独自 |

### 3.2 結論

**naist-jdic拡張フォーマットを採用する。**

根拠:
- OpenJTalk/jpreprocessのNJD処理実装をそのまま参考にできる
- VOICEVOX等の既存エコシステムとの互換性が保たれる
- BSDライセンスで商用・Unity Asset Store配布に制約なし
- 辞書サイズが小さい（モバイル・Unity環境で重要）

---

## 4. 形態素解析エンジンの選択

### 4.1 選択肢の比較

| 選択肢 | ライセンス | Asset Store | 実装コスト | 推奨度 |
|--------|-----------|-------------|-----------|--------|
| A: NMeCab/MeCab.DotNet依存 | GPL/LGPL | **不可** | 低 | プロトタイプ用 |
| B: MeCab互換エンジン独自C#実装 | BSD | 可能 | 高 | **本番推奨** |
| C: NMeCabコード参考+独自実装 | 要注意 | 条件付き | 中〜高 | 中 |

### 4.2 結論

**短期: NMeCab/MeCab.DotNetでプロトタイプ開発。中長期: BSD互換の独自C#実装。**

独自実装に必要な要素:
1. ダブル配列Trie（DARTS互換）
2. ラティス構築
3. ビタビデコーディング
4. MeCabバイナリ辞書読み込み（sys.dic, matrix.bin, char.bin, unk.dic）
5. 未知語処理

---

## 5. 推奨アーキテクチャ

### 5.1 パッケージ構成

```
DotNetG2P/
├── DotNetG2P.Core/              # コアライブラリ（ルールベースG2P）
│   ├── Tokenizer/               # 形態素解析抽象化
│   │   ├── ITokenizer.cs        # Tokenizerインターフェース
│   │   └── IToken.cs            # Tokenインターフェース
│   ├── Core/                    # コアデータ構造
│   │   ├── POS.cs               # 品詞enum（ネスト構造）
│   │   ├── MoraKind.cs          # モーラenum（約150種）
│   │   ├── Mora.cs              # モーラ構造体（MoraKind + IsVoiced）
│   │   ├── Pronunciation.cs     # 発音（List<Mora> + AccentPosition）
│   │   ├── WordDetails.cs       # 単語詳細情報
│   │   ├── WordEntry.cs         # 辞書エントリ
│   │   └── Phoneme.cs           # 音素定義
│   ├── NJD/                     # NJD処理（各ステップ独立モジュール）
│   │   ├── NjdNode.cs           # NJDノード
│   │   ├── Njd.cs               # NJDコンテナ（List<NjdNode>）
│   │   ├── SetPronunciation.cs  # 発音生成
│   │   ├── DigitSequence.cs     # 数字列処理
│   │   ├── SetDigit.cs          # 数字読み変換
│   │   ├── SetAccentPhrase.cs   # アクセント句結合（18ルール）
│   │   ├── SetAccentType.cs     # アクセント結合（C1-C5, F1-F5, P系列）
│   │   └── SetUnvoicedVowel.cs  # 無声音化（6ルール）
│   ├── JPCommon/                # フルコンテキストラベル生成（オプション）
│   │   ├── Utterance.cs         # 発話
│   │   ├── BreathGroup.cs       # 呼気段落
│   │   ├── AccentPhrase.cs      # アクセント句
│   │   └── FullContextLabel.cs  # HTS full-context label生成
│   ├── TextNormalization/       # テキスト正規化
│   │   ├── TextNormalizer.cs    # 全角/半角変換、濁点結合等
│   │   └── DigitRules.cs        # 数字・助数詞変換テーブル
│   ├── PhonemeConverter/        # 音素変換
│   │   ├── MoraMapping.cs       # カタカナ⇔音素マッピング（247種）
│   │   └── ProsodyExtractor.cs  # 韻律記号抽出（ESPnet方式）
│   └── G2PEngine.cs             # メインエントリーポイント
│
├── DotNetG2P.MeCab/             # MeCab互換形態素解析エンジン（BSD）
│   ├── DoubleArrayTrie.cs       # ダブル配列Trie
│   ├── Lattice.cs               # ラティス構築
│   ├── Viterbi.cs               # ビタビデコーディング
│   ├── DictionaryReader.cs      # MeCabバイナリ辞書読み込み
│   ├── UnknownWordHandler.cs    # 未知語処理
│   └── MeCabTokenizer.cs        # ITokenizer実装
│
├── DotNetG2P.Neural/            # ニューラルG2P拡張（オプション）
│   ├── OnnxG2PModel.cs          # ONNX推論ラッパー
│   └── UnknownWordEstimator.cs  # 未知語読み推定
│
└── DotNetG2P.Unity/             # Unity向けアダプター
    ├── G2PManager.cs            # MonoBehaviourラッパー
    └── DictionaryLoader.cs      # StreamingAssets辞書ロード
```

### 5.2 処理パイプライン

```
入力テキスト
    │
    ▼
[1] TextNormalizer               全角/半角正規化、濁点結合
    │
    ▼
[2] ITokenizer.Tokenize()        形態素解析（MeCab互換）
    │                            辞書: naist-jdic（15フィールド）
    ▼
[3] NjdNode.FromTokens()         トークン → NJDノード変換
    │
    ▼
[4] NJD処理パイプライン（順序厳守）
    │  ├─ SetPronunciation       発音生成（辞書にない語の読み推定）
    │  ├─ DigitSequence          数字列の位取り処理
    │  ├─ SetDigit               数字→日本語読み変換
    │  ├─ SetAccentPhrase        アクセント句結合（18ルール）
    │  ├─ SetAccentType          アクセント結合（C1-C5）
    │  └─ SetUnvoicedVowel       無声音化（6ルール）
    │
    ▼
[5] 出力選択（複数形式対応）
    ├─ ToPhonemes()              音素列: "k o N n i ch i w a"
    ├─ ToKana()                  カタカナ: "コンニチワ"
    ├─ ToProsody()               韻律記号付き: "^ k o [ N n i ch i w a $"
    ├─ ToAccentPhrases()         AccentPhrase構造体列
    └─ ToFullContextLabels()     HTS full-context label列
```

### 5.3 設計原則（jpreprocessから学んだ知見）

1. **型安全性**: 品詞をネストenum、発音をMoraEnum構造体で表現（文字列ベースの処理を最小化）
2. **モジュール分離**: NJD処理の各ステップを独立クラスにし、個別テストを可能にする
3. **Tokenizer抽象化**: `ITokenizer`インターフェースで形態素解析エンジンの差し替えを容易にする
4. **複数出力形式**: ESPnetの5種G2Pバックエンドに倣い、用途に応じた出力形式を提供
5. **OpenJTalk互換性**: naist-jdic辞書とNJD処理ルールの互換性を維持

---

## 6. 実装フェーズ計画

### Phase 1: プロトタイプ（最小実装）

**目標**: 基本的なG2P変換が動作するプロトタイプ

- NMeCab/MeCab.DotNetを形態素解析に使用
- naist-jdic辞書の読み込み（Feature文字列パース）
- NJDNode データ構造の実装
- カタカナ→音素変換テーブル（247種MoraMapping）
- 最小限のNJD処理（SetPronunciation, SetAccentPhrase）

**成果物**: `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作

### Phase 2: NJD処理の完全実装

**目標**: OpenJTalk互換のNJD処理パイプライン完成

- NJD 6段階処理の完全実装
  - SetPronunciation（発音生成）
  - DigitSequence + SetDigit（数字読み変換）
  - SetAccentPhrase（18ルール）
  - SetAccentType（C1-C5, F1-F5, P系列）
  - SetUnvoicedVowel（6ルール）
- テキスト正規化（全角/半角変換）
- ユーザー辞書機能
- 韻律記号抽出（ESPnet prosody方式）

**成果物**: pyopenjtalkと同等のG2P出力

### Phase 3: 独自MeCabエンジン（ライセンス自由化）

**目標**: GPL/LGPL依存の排除

- ダブル配列Trieの独自C#実装
- ラティス構築 + ビタビデコーディング
- MeCabバイナリ辞書読み込み
- 未知語処理
- NMeCab依存の完全排除

**成果物**: 完全BSDライセンスのG2Pライブラリ

### Phase 4: Unity最適化・拡張

**目標**: Unity環境での実用化

- Unity Package Manager対応
- StreamingAssets/Addressables辞書ロード
- IL2CPP対応検証
- メモリ最適化（Span<T>, MemoryMappedFile）
- （オプション）ONNX Runtime/Unity Sentisによるニューラル未知語推定

**成果物**: Unity Package

---

## 7. 各調査から得られた重要な知見

### 7.1 OpenJTalk/pyopenjtalk（#01, #02）
- NJD処理の6段階は**順序が厳密**に決まっている
- NJDNodeは16フィールドの双方向連結リスト（C#ではList<NjdNode>で十分）
- pyopenjtalkのg2p()には2パス（音素パス=full-context label経由、カタカナパス=pron連結）がある
- C#ではカタカナ→音素直接変換テーブルによる「第3のパス」が有効

### 7.2 jpreprocess（#03）
- 品詞をネストenum、発音をMoraEnum（約150種）で構造化 → **最も参考にすべき設計**
- Tokenizerトレイトによる形態素解析エンジン抽象化
- njd_set_long_vowelは非推奨・未実装（OpenJTalkでもコメントアウト）
- ウィンドウイテレータはC#では不要（インデックスアクセスで代替可能）

### 7.3 ESPnet（#04）
- pyopenjtalk_prosody の韻律記号体系（`^ $ ? _ # [ ]`）は移植推奨
- ニューラルG2Pは使用しておらず、完全にルールベース/辞書ベース
- 用途に応じた5種類のG2Pバリエーション設計が参考になる

### 7.4 MeCab系ライブラリ（#06）
- NMeCab/MeCab.DotNetはGPL/LGPL → **Unity Asset Store配布不可**
- MeCab互換エンジンのBSD独自実装を推奨
- naist-jdic辞書はバイナリ辞書としてNMeCab/MeCab.DotNetで読み込み可能

### 7.5 UniDic（#07）
- アクセント情報内蔵だが、OpenJTalkのC1-C5形式とは非互換
- 辞書サイズが大きい（133MB vs 80MB）
- naist-jdicベースを推奨、UniDicは補助参照の可能性

### 7.6 VOICEVOX/派生（#08, #09）
- mora_mapping.py の247種マッピング（BSDライセンス）は直接移植可能
- AccentPhrase/Moraデータモデル設計が参考になる
- AivisSpeechのテキスト正規化が最も高度（大規模内蔵辞書、英語・新語対応）
- 辞書品質がG2P精度に最も直結する

### 7.7 テキスト正規化（#10）
- 助数詞処理が最も複雑（Class1-6の分類、音便変化テーブル）
- 日付の特殊読み（1日→ツイタチ〜20日→ハツカ）の完全テーブルが必要
- jpreprocessのRust実装が最も体系的な参考実装

### 7.8 アクセント結合規則（#11）
- C1-C5に加え、F1-F5（付属語）、P1/P2/P6/P14（特殊）の3系列
- njd_set_accent_phraseの18ルールの完全一覧
- tdmelodic（ニューラルアクセント推定）が将来の精度向上手段

### 7.9 ONNX/ニューラル（#12）
- Unity WebGLでONNX Runtime **未対応** → ルールベースのみで完結する設計が必須
- ニューラルG2Pはルールベースの補助（未知語推定）としてオプション提供
- パッケージは Core + Neural + Unity の3分割推奨

### 7.10 C#日本語処理ライブラリ（#13）
- LibNMeCab が G2P用途に最有力（naist-jdic互換、Unity実績あり）
- 自前実装が必要な領域: NJD処理、カタカナ→音素変換、アクセント情報処理
- .NET標準のstring.Normalize()でUnicode正規化対応可能

### 7.11 naist-jdic辞書構造（#14）
- sys.dic: 72バイトヘッダ + ダブル配列 + Token配列(16B/Token) + Feature文字列
- DictionaryMagicID = `0xef718f77`, DIC_VERSION = `0x66` (102)
- matrix.bin: lsize(2B) + rsize(2B) + short配列（連接コスト）
- 約39万語、合計約80.5MB

---

## 8. リスク・課題

| リスク | 影響度 | 対策 |
|--------|--------|------|
| NJD処理の実装精度 | 高 | jpreprocessのテストケースを移植して検証 |
| MeCab独自実装の工数 | 高 | Phase1ではNMeCab依存、Phase3で独自実装 |
| 辞書サイズ（80MB） | 中 | 不要エントリ削除、圧縮、StreamingAssets |
| GPL/LGPLライセンス制約 | 中 | Phase3で完全BSD化 |
| Unity IL2CPP互換性 | 中 | 早期にIL2CPPビルドテスト |
| 未知語の読み精度 | 低〜中 | Phase4でニューラル未知語推定を追加 |

---

## 9. 参考にすべき主要プロジェクト（優先順）

1. **jpreprocess**（Rust） - アーキテクチャ設計・データ構造の最良の参考実装
2. **OpenJTalk**（C） - NJD処理のオリジナルアルゴリズム・ルールテーブル
3. **pyopenjtalk**（Python） - API設計・処理フローの参考
4. **VOICEVOX**（Python） - モーラマッピング・AccentPhraseモデル
5. **ESPnet**（Python） - 韻律記号抽出アルゴリズム
6. **NMeCab**（C#） - MeCab C#実装の参考（ただしライセンス注意）
