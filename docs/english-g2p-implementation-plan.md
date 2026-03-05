# 英語G2P 実装計画書

Issue: [#1 espeak-ngと同等の精度の英語のg2p for C#を実装する](https://github.com/ayutaz/dot-net-g2p/issues/1)

## 目標

- espeak-ngと同等以上の精度（**PER 7%以下**）の英語G2PをC#で実装
- 純C#実装（外部依存なし）
- .NET Standard 2.1（Unity 2021.2+互換）
- Apache-2.0ライセンス
- 段階的にリリース可能な構成

---

## アーキテクチャ概要

```
英語テキスト入力
  → テキスト正規化（数字→読み、略語展開、記号処理）
  → 単語分割（スペース+句読点）
  → 各単語ごとに:
    → CMU辞書ルックアップ（134,000語、完全一致O(1)）
    → ヒット → ARPAbet音素列を返す
    → ミス → Flite LTS CARTツリーで音素推定
  → 同綴異音語解決（品詞ベースルール）
  → 音素列出力（ARPAbet / IPA変換可能）
```

## パッケージ構成

`DotNetG2P.English` として **DotNetG2P.Coreに依存しない独立パッケージ** とする。

理由:
- 既存のConsonant/Vowel enumは日本語音素体系に完全特化（byte基底型で最適化済み）
- IToken/NjdNode/WordDetailsは日本語辞書フォーマット特化で英語で再利用不可
- CMU辞書（~1MB）を使わないユーザに不要なデータを含めない
- 既存テスト950件に影響を与えない

```
src/DotNetG2P.English/
├── DotNetG2P.English.csproj   # netstandard2.1, Core参照なし
├── Models/
│   └── ArpabetPhoneme.cs      # ARPAbet音素enum (39音素, byte基底)
├── Dictionary/
│   ├── CmuDictionary.cs       # CMU辞書ローダー・ルックアップ
│   └── cmudict.bin            # バイナリ化辞書 (EmbeddedResource)
├── LTS/
│   ├── LtsEngine.cs           # Flite CARTツリーLTSエンジン
│   └── lts_rules.bin          # CARTツリーデータ (EmbeddedResource)
├── Normalization/
│   └── EnglishNormalizer.cs   # 英語テキスト正規化
├── Homograph/
│   └── HomographResolver.cs   # 同綴異音語解決（品詞ルール）
├── EnglishG2PEngine.cs        # メインAPI
├── EnglishG2POptions.cs       # オプション
├── package.json               # UPM (com.dotnetg2p.english)
└── DotNetG2P.English.asmdef   # Unity Assembly Definition
```

## 辞書データ設計

### CMU辞書

- **格納方式**: `Dictionary<string, EnglishPronunciation[]>`（完全一致検索のみなのでTrieは不要）
- **音素表現**: `PhonemeWithStress`（1バイト: 上位6bit=音素ID, 下位2bit=ストレス）
- **配布方式**: バイナリ化してEmbeddedResource（~1MB）。外部辞書パスも受付可
- **複数発音**: `LEAD` / `LEAD(2)` 形式で同一単語に複数エントリ → 配列で保持
- **メモリ見積もり**: Dictionary方式で約12-15MB（ランタイム）

### LTSルール

- **方式**: **Flite CARTツリー**（推奨）
  - Carnegie Mellon発、MIT相当ライセンス
  - CMUdictで学習済みのCARTツリーがFliteソースコードに同梱
  - 各文字について「文字コンテキスト→音素」の決定木
  - C#への移植が比較的容易（ツリーのトラバースのみ）
  - 精度: CMUdictテストセットで約72%正解率（単語レベル）、PER推定5-8%
- **データサイズ**: ~100KB（バイナリ化後）
- **代替案**: Phonetisaurus WFST（高精度だが実装複雑）、独自ルール（工数大）

## 主要API設計

```csharp
namespace DotNetG2P.English
{
    public sealed class EnglishG2PEngine : IDisposable
    {
        // 埋め込み辞書使用
        public EnglishG2PEngine(EnglishG2POptions? options = null);
        // 外部辞書パス指定
        public EnglishG2PEngine(string cmuDictPath, EnglishG2POptions? options = null);

        // ARPAbet文字列出力: "HH AH0 L OW1"
        public string ToPhonemes(string text);
        // 構造体配列出力（ストレス情報付き）
        public IReadOnlyList<EnglishPhoneme> ToPhonemeList(string text);
        // IPA出力: "hʌˈloʊ"
        public string ToIPA(string text);
        // 単一単語ルックアップ
        public IReadOnlyList<EnglishPhoneme> LookupWord(string word);
        // 辞書存在確認
        public bool ContainsWord(string word);
        // バッチ処理
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
    }

    public sealed class EnglishG2POptions
    {
        public bool IncludeStress { get; }           // ストレスマーカー出力（default: true）
        public bool EnableNormalization { get; }      // テキスト正規化（default: true）
        public UnknownWordStrategy UnknownWordHandling { get; }  // OOV戦略
        public PhonemeFormat OutputFormat { get; }    // Arpabet / IPA
    }
}
```

---

## マイルストーン

### E1: CMU辞書ルックアップ（MVP）

**目標**: CMU辞書による基本的な英語→音素変換が動作する

**成果物**:
- `DotNetG2P.English` プロジェクト骨格
- `ArpabetPhoneme` enum（39音素 + ストレス）
- `CmuDictionary` クラス（テキストパーサー + Dictionary格納）
- `EnglishG2PEngine` 基本API（`ToPhonemes`, `LookupWord`, `ContainsWord`）
- CMU辞書のEmbeddedResource組み込み
- 単体テスト ~40件

**完了条件**:
- `engine.ToPhonemes("hello world")` → `"HH AH0 L OW1 W ER1 L D"` が動作
- CMUdict収録語に対して100%正確
- 辞書ロード時間 < 500ms

**対応する精度**: 辞書内100%、OOV 0%（LTSなし）

---

### E2: Flite LTS CARTツリー

**目標**: 辞書にない単語（OOV）もLTSルールで音素推定できる

**成果物**:
- `LtsEngine` クラス（CARTツリートラバース）
- FliteのLTSデータをバイナリ化して組み込み
- OOV単語のLTS変換パイプライン
- EnglishG2PEngineへの統合（辞書ミス→LTSフォールバック）
- 単体テスト ~80件
- PER測定テスト

**完了条件**:
- OOV単語（CMUdictにない語）に対して音素推定が動作
- LTS単体で PER < 10%（CMUdict hold-outセットで評価）
- 辞書+LTS全体で PER < 7%

**LTSデータ取得方法**:
1. Fliteソースコード（`cmu_lts_rules.c` / `cmu_lts_model.c`）からCARTツリーデータを抽出
2. C#で読み込めるバイナリ形式に変換
3. EmbeddedResourceとして組み込み

---

### E3: テキスト正規化

**目標**: 数字・略語・記号等を英語読みに展開する

**成果物**:
- `EnglishNormalizer` クラス
  - 数字→英語読み変換（基数、序数、小数、通貨、時刻）
  - 略語展開（Dr./Mr./Mrs./etc./vs.等）
  - 頭字語判別（NASA→1語、API→1文字ずつ）
  - 記号→名前変換（@→at、#→hash等）
  - アポストロフィ短縮形（don't, it's, I'm等）
- 単体テスト ~60件

**完了条件**:
- `"Dr. Smith has $100"` → 適切に正規化されて音素変換
- `"1st 2nd 3rd"` → `"first second third"` として処理
- `"NASA"` → 1語として発音、`"API"` → 1文字ずつ発音

**数字読み主要ルール**:
- 基数: 0-19個別、20-90は十の位+一の位、100/1000/1000000...
- 序数: 1st→first, 2nd→second, 3rd→third, 4th→fourth...
- 小数: 3.14→"three point one four"
- 通貨: $5→"five dollars", $1.50→"one dollar fifty cents"
- 時刻: 3:14→"three fourteen", 3:00→"three o'clock"

---

### E4: 同綴異音語対応

**目標**: read/lead/live等の同綴異音語を文脈から判別する

**成果物**:
- `HomographResolver` クラス
  - 主要同綴異音語データベース（30-50語）
  - 品詞ルールベース判別（接尾辞規則 + 位置ルール）
  - デフォルト発音選択戦略
- 単体テスト ~50件

**完了条件**:
- 同綴異音語正解率 > 70%（espeak-ngの43.87%を大幅に上回る）
- 主要同綴異音語（read, lead, live, wind, tear, bow, close, record）の基本的な判別

**品詞推定の軽量実装**:
- 接尾辞ルール: -ing→動詞, -tion→名詞, -ly→副詞, -ed→過去形 等
- 位置ルール: 文頭の動詞、冠詞(a/the)の後は名詞 等
- 前置詞(to/will/can/would)の後は動詞 等
- 完全なPOS taggerは不要。同綴異音語の判別に最低限必要な情報のみ

---

### E5: IPA出力・精度改善・パッケージング

**目標**: 出力形式の充実、精度改善、リリース準備

**成果物**:
- ARPAbet→IPA変換（`ToIPA()` API）
- ARPAbet→X-SAMPA変換
- バッチAPI（`ToPhonemesBatch`）
- バイナリ辞書最適化（テキストパース→バイナリ読み込みへ高速化）
- espeak-ng比較テスト（Docker期待値データ）
- エッジケーステスト
- パフォーマンステスト
- NuGet/UPMパッケージ設定
- CI/CD更新

**完了条件**:
- 全体PER < 7%（espeak-ng同等）
- 同綴異音語正解率 > 70%
- 辞書ロード時間 < 100ms（バイナリ化後）
- メモリ使用量 < 50MB
- テスト総数 ~330件
- NuGet `DotNetG2P.English` パッケージ生成

---

### E6（将来）: 日英混在テキスト対応

**目標**: 日本語と英語が混在するテキストの処理

**成果物**:
- `MultilingualG2PEngine` ファサードクラス
- 文字種ベースの言語判定（ASCII→英語、ひらがな/カタカナ/漢字→日本語）
- 日英それぞれのG2Pエンジンへの振り分け

**完了条件**:
- `"私はhelloと言った"` → 日本語部分は日本語音素、英語部分はARPAbet/IPA

---

## マイルストーン依存関係

```
E1 (CMU辞書) ──→ E2 (LTS) ──→ E5 (IPA・精度・パッケージ)
     │                              ↑
     └──→ E3 (正規化) ────→ E4 (同綴異音語) ──┘
```

- E1は必須の土台（他の全マイルストーンが依存）
- E2とE3は並行開発可能
- E4はE3（テキスト正規化）完了後に着手
- E5は全マイルストーン統合

---

## テスト戦略

### テスト構成

```
tests/DotNetG2P.Tests/
├── EnglishG2P/
│   ├── Dictionary/
│   │   ├── CmuDictLookupTests.cs       # 辞書ルックアップ (~20件)
│   │   └── CmuDictVariantTests.cs      # 複数発音バリアント (~20件)
│   ├── Lts/
│   │   ├── LtsRuleTests.cs            # 個別ルール検証 (~40件)
│   │   └── LtsOovTests.cs             # OOV変換テスト (~40件)
│   ├── Normalization/
│   │   ├── NumberExpansionTests.cs      # 数字読み (~30件)
│   │   └── AbbreviationTests.cs        # 略語展開 (~30件)
│   ├── Homograph/
│   │   └── HomographResolutionTests.cs # 同綴異音語 (~50件)
│   └── Integration/
│       ├── EnglishPipelineTests.cs     # パイプライン統合 (~30件)
│       ├── EspeakComparisonTests.cs    # espeak-ng比較 (~50件)
│       ├── EnglishEdgeCaseTests.cs     # エッジケース (~30件)
│       └── EnglishPerformanceTests.cs  # パフォーマンス (~10件)
├── TestData/
│   ├── english_expected.json           # CMUdict期待値 (500件)
│   ├── english_oov.json               # OOVテストセット (200件)
│   ├── english_homographs.json        # 同綴異音語テスト (50件)
│   └── espeak_expected.json           # espeak-ng期待値 (Docker生成)
```

**合計: ~330件のテスト**

### 精度評価指標

| 指標 | 定義 | 目標値 |
|------|------|--------|
| PER | 音素レベルLevenshtein距離/参照長 | < 7% |
| WER | 音素列不一致の単語数/全単語数 | < 30% |
| 同綴異音語正解率 | 文脈付きテストでの正解率 | > 70% |
| espeak-ng一致率 | espeak-ng出力との一致率（参考値） | > 85% |

---

## リスクと対策

| リスク | 影響度 | 対策 |
|--------|--------|------|
| FliteのLTSデータ抽出が困難 | 高 | Phonetisaurus WFSTを代替案として準備。最悪の場合、独自ルールで基本パターンのみ対応 |
| LTSの精度がPER 7%に届かない | 中 | CMUdictカバレッジ（一般テキストの90-95%）で補い、LTSは補助的位置付け |
| 同綴異音語の判別精度が低い | 中 | 段階的に改善。まずデフォルト発音（主エントリ）を返し、品詞ルールで段階的に向上 |
| CMU辞書のメモリ消費が大きい | 低 | Phase 2でバイナリ最適化、頻出語のみの縮小辞書オプション |
| 数字読みの英語ルールが複雑 | 低 | 基本パターンから段階的に拡充。完全対応は後回し |
| Unity WebGLでのサイズ制約 | 低 | 辞書圧縮（Brotli ~0.8MB）、頻出語のみの縮小辞書 |

---

## ライセンス

| コンポーネント | ライセンス | Apache-2.0互換 |
|-------------|----------|---------------|
| CMU辞書 | 無制限(BSD的) | ✓ |
| Flite LTSデータ | MIT相当 | ✓ |
| 独自実装コード | Apache-2.0 | — |
| espeak-ng（テスト期待値生成のみ） | GPL v3 | ✓（バイナリ同梱しない） |

---

## 参考資料

- [調査レポート](./english-g2p-research.md)
- [espeak-ng出力検証](./espeak-ng-output-verification.md)
- [CMU Pronouncing Dictionary](https://github.com/cmusphinx/cmudict)
- [Flite (Festival Lite)](https://github.com/festvox/flite)
