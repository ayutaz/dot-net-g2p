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
│   ├── ArpabetPhoneme.cs      # ARPAbet音素enum (39音素, byte基底)
│   ├── Stress.cs              # ストレスenum (None/NoStress/Primary/Secondary, byte基底)
│   ├── EnglishPhoneme.cs      # ストレス付き音素 readonly struct
│   ├── EnglishPronunciation.cs # 発音クラス (音素配列を保持)
│   └── ArpabetParser.cs       # ARPAbetトークン⇔EnglishPhoneme変換パーサー
├── Dictionary/
│   ├── CmuDictionary.cs       # CMU辞書ローダー・ルックアップ (Dictionary<string, EnglishPronunciation[]>)
│   └── (Data/)
│       └── cmudict.dict       # CMU辞書テキスト (EmbeddedResource, ~5MB)
├── LTS/
│   ├── LtsEngine.cs           # Flite CARTツリーLTSエンジン (スレッドセーフ、遅延初期化)
│   ├── LtsData.cs             # 自動生成: 音素テーブル(75種)・文字インデックス(a-z)・モデル読み込み
│   ├── LtsPhoneMapping.cs     # 自動生成: Flite音素→EnglishPhoneme変換マッピング(75エントリ)
│   └── cmu_lts_model.bin      # CARTツリーバイナリ (EmbeddedResource, 6バイト/ノード)
├── Normalization/
│   ├── EnglishNormalizer.cs   # 英語テキスト正規化ファサード
│   ├── NumberToWords.cs       # 数字→英語読み変換
│   ├── CurrencyExpander.cs    # 通貨展開
│   ├── TimeExpander.cs        # 時刻展開
│   ├── AbbreviationExpander.cs # 略語展開
│   ├── AcronymDetector.cs     # 頭字語判別
│   └── SymbolExpander.cs      # 記号→名前変換
├── Homograph/
│   ├── HomographResolver.cs   # 同綴異音語解決ファサード
│   ├── HomographDatabase.cs   # 30+語の同綴異音語データベース
│   ├── HomographEntry.cs      # 同綴異音語エントリ・ルールモデル
│   ├── PosGuesser.cs          # 軽量品詞推定（接尾辞+文脈ルール）
│   └── PosTag.cs              # 品詞タグenum
├── EnglishG2PEngine.cs        # メインAPI (ToPhonemes, ToPhonemeList, LookupWord, LookupAllPronunciations, ContainsWord)
├── EnglishG2POptions.cs       # オプション (IncludeStress, UnknownWordHandling, EnableLts, EnableNormalization, EnableHomographResolution)
├── package.json               # UPM (com.dotnetg2p.english)
└── DotNetG2P.English.asmdef   # Unity Assembly Definition

tools/
└── extract_lts.js             # Flite LTSデータ抽出スクリプト (Node.js)
                               # cmu_lts_model.h/.c + cmu_lts_rules.c → cmu_lts_model.bin + LtsData.cs + LtsPhoneMapping.cs

NOTICE                         # サードパーティライセンス表記 (CMU辞書 + Flite)
```

## 辞書データ設計

### CMU辞書

- **格納方式**: `Dictionary<string, EnglishPronunciation[]>`（完全一致検索のみなのでTrieは不要）
- **音素表現**: `EnglishPhoneme` readonly struct（`ArpabetPhoneme` enum + `Stress` enum）
- **配布方式**: テキスト形式（cmudict.dict）をEmbeddedResource（~5MB）。外部辞書パスも受付可
- **複数発音**: `LEAD` / `LEAD(2)` 形式で同一単語に複数エントリ → 配列で保持
- **辞書エントリ数**: 134,000語以上（テストで10万件超を検証済み）

### LTSルール

- **方式**: **Flite CARTツリー**
  - Carnegie Mellon発、BSD-like ライセンス
  - CMUdictで学習済みのCARTツリーがFliteソースコードに同梱
  - 各文字について「文字コンテキスト→音素」の決定木
  - C#への移植が比較的容易（ツリーのトラバースのみ）
  - **実測精度: PER 5.26%**（100語サンプルでの評価）
- **データサイズ**: 6バイト/ノード（feat, val, qtrue[2], qfalse[2]）のバイナリ形式
- **音素テーブル**: 75エントリ（epsilon + 単一音素 + 二重音素 (w-ey1, t-s, k-s, g-zh等)）
- **コンテキスト窓**: 前後4文字 + 追加特徴1（POS、デフォルト "0"）
- **抽出ツール**: `tools/extract_lts.js`（Node.js）でFliteソースから自動生成

## 主要API設計

```csharp
namespace DotNetG2P.English
{
    public sealed class EnglishG2PEngine : IDisposable
    {
        // 埋め込み辞書使用
        public EnglishG2PEngine();
        // 埋め込み辞書+オプション指定
        public EnglishG2PEngine(EnglishG2POptions options);
        // 外部辞書パス指定
        public EnglishG2PEngine(string dictPath);
        // 外部辞書パス+オプション指定
        public EnglishG2PEngine(string dictPath, EnglishG2POptions options);

        // ARPAbet文字列出力: "HH AH0 L OW1"
        public string ToPhonemes(string text);
        // 構造体配列出力（ストレス情報付き）
        public IReadOnlyList<EnglishPhoneme> ToPhonemeList(string text);
        // 単一単語ルックアップ（LTSフォールバックあり）
        public IReadOnlyList<EnglishPhoneme> LookupWord(string word);
        // 全発音バリアント検索（LTSフォールバックなし）
        public IReadOnlyList<EnglishPronunciation> LookupAllPronunciations(string word);
        // 辞書存在確認
        public bool ContainsWord(string word);
    }

    public sealed class EnglishG2POptions
    {
        public bool IncludeStress { get; }           // ストレスマーカー出力（default: true）
        public UnknownWordStrategy UnknownWordHandling { get; }  // OOV戦略（default: Skip）
        public bool EnableLts { get; }               // LTSフォールバック（default: true）
        public bool EnableNormalization { get; }      // テキスト正規化（default: true）
        public bool EnableHomographResolution { get; } // 同綴異音語解決（default: true）
    }

    public enum UnknownWordStrategy
    {
        Skip = 0,   // 未知語をスキップ
        Throw = 1,  // 未知語で例外をスロー
    }
}
```

---

## マイルストーン

### E1: CMU辞書ルックアップ（MVP） -- 完了

**目標**: CMU辞書による基本的な英語→音素変換が動作する

**成果物**:
- `DotNetG2P.English` プロジェクト骨格（`DotNetG2P.English.csproj`、netstandard2.1）
- `ArpabetPhoneme` enum（39音素: 母音15種 + 子音24種、byte基底）
- `Stress` enum（None/NoStress/Primary/Secondary、byte基底）
- `EnglishPhoneme` readonly struct（`ArpabetPhoneme` + `Stress`、`IsVowel`プロパティ付き）
- `EnglishPronunciation` class（音素配列を保持、`ToString()`でARPAbet文字列出力）
- `ArpabetParser` static class（`Parse`/`TryParse`/`PhonemeToString`、子音のストレスをNoneに強制）
- `CmuDictionary` class（テキストパーサー + Dictionary格納、埋め込みリソース/外部ファイル両対応）
- `EnglishG2PEngine` class（`ToPhonemes`, `ToPhonemeList`, `LookupWord`, `LookupAllPronunciations`, `ContainsWord`）
- `EnglishG2POptions` class（`IncludeStress`, `UnknownWordHandling`）
- CMU辞書のEmbeddedResource組み込み（`cmudict.dict`）
- UPMパッケージ設定（`package.json`, `DotNetG2P.English.asmdef`）
- CI/CD更新（`ci.yml`/`release.yml`にDotNetG2P.Englishのpackステップ追加）
- NOTICEファイルにCMU辞書のライセンス表記追加
- 単体テスト: CmuDictLookupTests（19件）、CmuDictVariantTests（10件）、ArpabetParserTests（31件）、EnglishPipelineTests（29件）= 計約89件

**完了条件（達成済み）**:
- `engine.ToPhonemes("hello world")` → `"HH AH0 L OW1 W ER1 L D"` が動作 -- 達成
- CMUdict収録語に対して100%正確 -- 達成
- 大文字小文字不問のルックアップ -- 達成
- 辞書エントリ数 > 100,000語（テストで検証済み） -- 達成
- 複数発音バリアント対応（lead, read, close, a 等） -- 達成
- Dispose後のObjectDisposedException -- 達成
- スレッドセーフティ（並行アクセステスト10スレッド） -- 達成

**対応する精度**: 辞書内100%、OOV 0%（LTSなし）

---

### E2: Flite LTS CARTツリー -- 完了

**目標**: 辞書にない単語（OOV）もLTSルールで音素推定できる

**成果物**:
- `LtsEngine` internal static class（CARTツリートラバース、スレッドセーフ遅延初期化）
- `LtsData` internal static class（自動生成: 音素テーブル75種、文字インデックス26文字、バイナリモデル読み込み）
- `LtsPhoneMapping` internal static class（自動生成: Flite音素→EnglishPhoneme変換、二重音素対応）
- `cmu_lts_model.bin` EmbeddedResource（CARTツリーバイナリデータ）
- `tools/extract_lts.js` 抽出スクリプト（Fliteソース→バイナリ+C#ソース自動生成）
- `EnglishG2POptions.EnableLts` プロパティ追加（default: true）
- OOV単語のLTS変換パイプライン統合（辞書ミス→LTSフォールバック）
- NOTICEファイルにFliteライセンス表記追加
- 単体テスト: LtsRuleTests（55件）、LtsOovTests（40件）、LtsAccuracyTests（13件）= 計約108件
- PER測定テスト（100語サンプル、Levenshtein距離ベース）

**完了条件（達成済み）**:
- OOV単語（CMUdictにない語）に対して音素推定が動作 -- 達成（造語・技術用語・新語で検証）
- LTS単体で **PER 5.26%**（100語サンプル、20/380エラー）< 10%目標 -- 達成
- 辞書+LTS全体で PER < 7% -- 達成（辞書語は100%正確、OOVもPER 5.26%）
- EnableLts=false時はOOVがSkip/Throw -- 達成
- EnableLts=true + UnknownWordHandling=Throw時、LTSで解決できれば例外なし -- 達成
- 辞書語+OOV語の混在テキスト処理 -- 達成
- IncludeStress=false時のLTS出力にストレス番号なし -- 達成
- 大文字小文字不問のLTS予測 -- 達成
- 英字以外（数字・記号・スペース）を含む単語はnull返却 -- 達成

**LTSデータ取得方法（実施済み）**:
1. Fliteソースコード（`cmu_lts_model.h`, `cmu_lts_model.c`, `cmu_lts_rules.c`）からCARTツリーデータを抽出
2. `tools/extract_lts.js`（Node.js）でマクロ展開→バイナリ変換→C#ソース自動生成
3. `cmu_lts_model.bin`（6バイト/ノード）+ `LtsData.cs` + `LtsPhoneMapping.cs` をEmbeddedResourceとして組み込み

**実測精度**:

| 指標 | 結果 | 目標 |
|------|------|------|
| LTS単体 PER | **5.26%** (20/380) | < 10% |
| 辞書+LTS全体 PER | **< 5.26%** | < 7% |

---

### E3: テキスト正規化 -- 完了

**目標**: 数字・略語・記号等を英語読みに展開する

**成果物**:
- `EnglishNormalizer` ファサードクラス + 6個のサブモジュール
  - `NumberToWords` : 数字→英語読み変換（基数、序数、小数、通貨、時刻）
  - `CurrencyExpander` : 通貨展開（$5→"five dollars"等）
  - `TimeExpander` : 時刻展開（3:14→"three fourteen"等）
  - `AbbreviationExpander` : 略語展開（Dr./Mr./Mrs./etc./vs.等）
  - `AcronymDetector` : 頭字語判別（NASA→1語、API→1文字ずつ）
  - `SymbolExpander` : 記号→名前変換（@→at、#→hash等）
- `EnglishG2POptions.EnableNormalization` プロパティ追加（default: true）
- 単体テスト 143件（NumberToWordsTests, CurrencyExpanderTests, TimeExpanderTests, AbbreviationExpanderTests, AcronymDetectorTests, SymbolExpanderTests, NormalizerIntegrationTests）

**完了条件（達成済み）**:
- `"Dr. Smith has $100"` → 適切に正規化されて音素変換 -- 達成
- `"1st 2nd 3rd"` → `"first second third"` として処理 -- 達成
- `"NASA"` → 1語として発音、`"API"` → 1文字ずつ発音 -- 達成

**数字読み主要ルール**:
- 基数: 0-19個別、20-90は十の位+一の位、100/1000/1000000...
- 序数: 1st→first, 2nd→second, 3rd→third, 4th→fourth...
- 小数: 3.14→"three point one four"
- 通貨: $5→"five dollars", $1.50→"one dollar fifty cents"
- 時刻: 3:14→"three fourteen", 3:00→"three o'clock"

---

### E4: 同綴異音語対応 -- 完了

**目標**: read/lead/live等の同綴異音語を文脈から判別する

**成果物**:
- `HomographResolver` ファサードクラス（PosGuesser + HomographDatabase を統合）
- `HomographDatabase` static class（30+語の同綴異音語データベース、CMU辞書バリアント順序に対応）
  - 母音変化型: read, lead, live, wind, tear, bow, close, bass, minute, use, abuse, excuse, house, resume, dove, does, buffet, content, desert, entrance, intern, invalid, object
  - ストレス移動型: record, present, produce, project, object, subject, conduct, conflict, contract, convert, decrease, defect, increase, insult, perfect, permit, progress, protest, rebel, refund, refuse, reject, survey, suspect, transport, upset
  - -ate語尾型: separate, moderate, deliberate, alternate, approximate, associate, duplicate, elaborate, estimate, graduate, intimate
- `PosGuesser` static class（軽量品詞推定: 接尾辞ルール + 文脈ルール）
- `PosTag` enum（Unknown, Noun, Verb, Adjective, Adverb, Preposition, Determiner, Pronoun, Conjunction）
- `HomographEntry` / `HomographRule` データモデル
- `EnglishG2POptions.EnableHomographResolution` プロパティ追加（default: true）
- 単体テスト 154件（PosGuesserTests, HomographDatabaseTests, HomographResolverTests, HomographIntegrationTests, HomographAccuracyTests）

**完了条件（達成済み）**:
- 同綴異音語正解率 > 70%（espeak-ngの43.87%を大幅に上回る） -- 達成
- 主要同綴異音語（read, lead, live, wind, tear, bow, close, record）の基本的な判別 -- 達成

**品詞推定の軽量実装**:
- 接尾辞ルール: -ing→動詞, -tion→名詞, -ly→副詞, -ed→過去形 等
- 位置ルール: 文頭の動詞、冠詞(a/the)の後は名詞 等
- 前置詞(to/will/can/would)の後は動詞 等
- 完全なPOS taggerは不要。同綴異音語の判別に最低限必要な情報のみ

---

### E5: IPA出力・テスト充実 -- 完了

**目標**: 出力形式の充実、テスト充実

**成果物**:
- `IpaConverter` internal static class（ARPAbet→IPA変換、ストレスマーク付き/なし）
- `XSampaConverter` internal static class（ARPAbet→X-SAMPA変換、ASCII出力）
- `EnglishG2PEngine` に8メソッド追加（ToIPA, ToIPAWithoutStress, ToXSampa, ToXSampaWithoutStress, ToPhonemesBatch, ToIPABatch, ToXSampaBatch, ToPhonemeListBatch）
- 単体テスト: IpaConverterTests（68件）、XSampaConverterTests（34件）、EngineConversionTests（20件）、BatchApiTests（15件）
- 統合テスト: EnglishEdgeCaseTests（35件）、EnglishPerformanceTests（10件）、EnglishAccuracyTests（15件）
- 計197件の新規テスト追加

**完了条件（達成済み）**:
- ToIPA/ToXSampa APIが動作 -- 達成
- バッチAPIが動作 -- 達成
- エッジケーステスト通過 -- 達成
- パフォーマンステスト通過（並行10スレッド含む） -- 達成
- 全1662テスト合格（失敗0） -- 達成

---

### E6: 日英混在テキスト対応 -- 完了

**目標**: 日本語と英語が混在するテキストの処理

**詳細調査**: [E6詳細調査レポート](./e6-multilingual-research.md)

**アーキテクチャ**:
```
入力テキスト
  → LanguageSegmenter（文字種ベース言語判定・セグメント分割）
  → 各セグメントごとに:
      Japanese → TextNormalizer → MeCab → NJD → 日本語音素
      English  → EnglishNormalizer → CMU辞書/LTS → 英語音素
  → 音素列結合
```

**パッケージ**: `DotNetG2P.Multilingual`（新規パッケージ、Core + English に依存）

**成果物**:
- `DotNetG2P.Multilingual` プロジェクト骨格（`DotNetG2P.Multilingual.csproj`、netstandard2.1）
- `LanguageDetector` static class（Unicode文字種ベース言語判定）
- `TextSegmenter` static class（混在テキスト→言語タグ付きセグメント分割）
- `MultilingualG2PEngine` sealed class（日英G2Pエンジンのファサード）
  - `ToPhonemes(string)` → 日英音素の単純結合
  - `ToSegments(string)` → `IReadOnlyList<G2PSegment>`（構造化出力）
  - `ToIPA(string)` → IPA統一出力（補助）
  - バッチAPI
- `G2PSegment` / `TextSegment` データモデル
- IDisposable + ThreadLocal<G2PEngine>（スレッドセーフ設計）
- UPMパッケージ設定（`package.json`, `DotNetG2P.Multilingual.asmdef`）
- CI/CD更新（`ci.yml`/`release.yml`にpackステップ追加）
- 単体テスト: LanguageDetectorTests（~25件）、TextSegmenterTests（~30件）
- 統合テスト: MultilingualEngineTests（~35件）、LanguageConsistencyTests（~20件）
- エッジケーステスト: MultilingualEdgeCaseTests（~30件）
- パフォーマンス/Disposeテスト: ~23件
- 計~163件のテスト

**完了条件（達成済み）**:
- `"私はhelloと言った"` → 日本語部分は日本語音素、英語部分はARPAbet -- 正しい分割 -- 達成
- 日本語のみの入力 → G2PEngine単独と同一結果 -- 達成
- 英語のみの入力 → EnglishG2PEngine単独と同一結果 -- 達成
- 混在テキスト内の英語部分のPER < 7%（単独エンジンと同等） -- 達成
- 空入力/記号のみ/絵文字等でクラッシュしない -- 達成
- Dispose後のObjectDisposedException -- 達成
- 並行アクセスが安全（lockパターン） -- 達成
- 2エンジン同時ロード時のメモリ < 200MB -- 達成
- テスト合計 162件 -- 達成

**設計上の重要判断**:
1. **言語判定はTextNormalization前に実行**（TextNormalizerがASCIIを全角化するため）
2. **出力形式は各言語体系維持**（JA=OpenJTalk音素、EN=ARPAbet）+ セグメント分離API
3. **パッケージは新規作成**（Core/Englishの独立性を維持）
4. **日本語エンジンはThreadLocal**、英語エンジンは共有（スレッドセーフティの非対称性に対応）
5. **辞書パスの非対称性**: 日本語=外部辞書必須、英語=埋め込みリソース（引数なし可）

**実装フェーズ**:
- Phase 1: LanguageDetector + TextSegmenter + 単体テスト ~55件
- Phase 2: MultilingualG2PEngine + 統合テスト ~35件
- Phase 3: 拡張API + エッジケーステスト ~45件
- Phase 4: パフォーマンス + パッケージング ~28件

---

## マイルストーン依存関係

```
E1 (CMU辞書) ✅ ─→ E2 (LTS) ✅ ─→ E5 (IPA・テスト) ✅ ─→ E6 (日英混在) ✅
     │                                  ↑                       ↑
     └──→ E3 (正規化) ✅ ──→ E4 (同綴異音語) ✅ ─┘               │
                                                  DotNetG2P.Core ─┘
```

- E1は必須の土台（他の全マイルストーンが依存）-- **完了**
- E2は**完了**
- E3は**完了**
- E4はE3完了後に着手し**完了**
- E5は全マイルストーン統合 -- **完了**
- E6はE5 + DotNetG2P.Coreに依存（新パッケージ`DotNetG2P.Multilingual`） -- **完了**

---

## テスト戦略

### テスト構成（現在の実装状況）

```
tests/DotNetG2P.Tests/
├── EnglishG2P/
│   ├── Dictionary/
│   │   ├── CmuDictLookupTests.cs       # (~19件)
│   │   └── CmuDictVariantTests.cs      # (~10件)
│   ├── Models/
│   │   └── ArpabetParserTests.cs       # (~31件)
│   ├── Lts/
│   │   ├── LtsRuleTests.cs            # (~55件)
│   │   └── LtsOovTests.cs             # (~40件)
│   ├── Normalization/
│   │   ├── NumberToWordsTests.cs        # 数字読み変換
│   │   ├── CurrencyExpanderTests.cs     # 通貨展開
│   │   ├── TimeExpanderTests.cs         # 時刻展開
│   │   ├── AbbreviationExpanderTests.cs # 略語展開
│   │   ├── AcronymDetectorTests.cs      # 頭字語判別
│   │   ├── SymbolExpanderTests.cs       # 記号変換
│   │   └── NormalizerIntegrationTests.cs # 正規化統合
│   ├── Homograph/
│   │   ├── PosGuesserTests.cs           # 品詞推定
│   │   ├── HomographDatabaseTests.cs    # データベース検証
│   │   ├── HomographResolverTests.cs    # 同綴異音語解決
│   │   ├── HomographIntegrationTests.cs # エンジン統合
│   │   └── HomographAccuracyTests.cs    # 精度評価
│   ├── Conversion/
│   │   ├── IpaConverterTests.cs         # IPA変換 (~68件)        [E5]
│   │   ├── XSampaConverterTests.cs      # X-SAMPA変換 (~34件)    [E5]
│   │   ├── EngineConversionTests.cs     # エンジン変換API (~20件) [E5]
│   │   └── BatchApiTests.cs             # バッチAPI (~15件)       [E5]
│   └── Integration/
│       ├── EnglishPipelineTests.cs      # (~29件)
│       ├── LtsAccuracyTests.cs          # (~13件)
│       ├── EnglishEdgeCaseTests.cs      # エッジケース (~35件)    [E5]
│       ├── EnglishPerformanceTests.cs   # パフォーマンス (~10件)  [E5]
│       └── EnglishAccuracyTests.cs      # 精度評価 (~15件)       [E5]
```

**英語G2Pテスト合計: ~870件**（E1: ~214件、E3: 143件、E4: 154件、E5: ~197件、E6: 162件）
**プロジェクト全体テスト合計: 1,824件**

### E6で追加されたテスト

```
├── Multilingual/
│   ├── LanguageDetectorTests.cs        # 言語判定 (29件)
│   ├── TextSegmenterTests.cs           # セグメント分割 (30件)
│   ├── MultilingualEngineTests.cs      # エンジン統合 (28件)
│   ├── LanguageConsistencyTests.cs     # 単独一致検証 (27件)
│   ├── MultilingualEdgeCaseTests.cs    # エッジケース (25件)
│   ├── MultilingualPerformanceTests.cs # パフォーマンス (8件)
│   └── MultilingualDisposeTests.cs     # Dispose/スレッド (15件)
```

### 精度評価指標

| 指標 | 定義 | 目標値 | E2時点の実測値 |
|------|------|--------|---------------|
| PER | 音素レベルLevenshtein距離/参照長 | < 7% | **5.26%**（LTS単体、100語サンプル） |
| WER | 音素列不一致の単語数/全単語数 | < 30% | - |
| 同綴異音語正解率 | 文脈付きテストでの正解率 | > 70% | 154件のテストで検証済み（E4で実装完了） |
| espeak-ng一致率 | espeak-ng出力との一致率（参考値） | > 85% | -（E5で評価予定） |

---

## リスクと対策

| リスク | 影響度 | 対策 | 状態 |
|--------|--------|------|------|
| FliteのLTSデータ抽出が困難 | 高 | Phonetisaurus WFSTを代替案として準備。最悪の場合、独自ルールで基本パターンのみ対応 | **解決済み**: `tools/extract_lts.js`で自動抽出成功 |
| LTSの精度がPER 7%に届かない | 中 | CMUdictカバレッジ（一般テキストの90-95%）で補い、LTSは補助的位置付け | **解決済み**: PER 5.26%で目標達成 |
| 同綴異音語の判別精度が低い | 中 | 段階的に改善。まずデフォルト発音（主エントリ）を返し、品詞ルールで段階的に向上 | **解決済み**: PosGuesser（接尾辞+文脈ルール）+ HomographDatabase（30+語）による品詞ベース判別を実装。154件のテストで検証 |
| CMU辞書のメモリ消費が大きい | 低 | Phase 2でバイナリ最適化、頻出語のみの縮小辞書オプション | 継続監視 |
| 数字読みの英語ルールが複雑 | 低 | 基本パターンから段階的に拡充。完全対応は後回し | **解決済み**: NumberToWords/CurrencyExpander/TimeExpander等6モジュールで対応。143件のテストで検証 |
| Unity WebGLでのサイズ制約 | 低 | 辞書圧縮（Brotli ~0.8MB）、頻出語のみの縮小辞書 | 継続監視 |
| 日英混在時のTextNormalizer競合 | 中 | 言語判定をTextNormalization前に実行。セグメントごとに適切なNormalizerを適用 | **解決済み**: LanguageDetectorで言語判定後にセグメント単位でG2P処理 |
| 2エンジン同時ロード時のメモリ | 中 | 日英合計~90-120MB。遅延初期化で片方だけロードする選択肢を提供 | **解決済み**: MultilingualG2PEngine内で両エンジンを管理、Dispose時に解放 |
| 日本語エンジンの非スレッドセーフ | 中 | ThreadLocal<G2PEngine>パターンでスレッドごとにインスタンス生成。DictionaryBundleの参照カウント共有で辞書メモリは共有 | **解決済み**: lockパターンで日本語エンジンを保護 |

---

## ライセンス

| コンポーネント | ライセンス | Apache-2.0互換 | NOTICEファイル記載 |
|-------------|----------|---------------|-------------------|
| CMU辞書 | 無制限(BSD的) | ✓ | ✓ |
| Flite LTSデータ | BSD-like (Carnegie Mellon University) | ✓ | ✓ |
| 独自実装コード | Apache-2.0 | -- | -- |
| espeak-ng（テスト期待値生成のみ） | GPL v3 | ✓（バイナリ同梱しない） | -- |

---

## CI/CD

E1/E2完了時点で以下のCI/CD更新を実施済み:

- **ci.yml**: `dotnet pack src/DotNetG2P.English/DotNetG2P.English.csproj` + `dotnet pack src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.csproj` ステップを追加
- **release.yml**: 同上
- NuGet `DotNetG2P.English`, `DotNetG2P.Multilingual` パッケージ生成・アップロードに対応

---

## 参考資料

- [調査レポート](./english-g2p-research.md)
- [espeak-ng出力検証](./espeak-ng-output-verification.md)
- [E6 日英混在テキスト調査レポート](./e6-multilingual-research.md)
- [CMU Pronouncing Dictionary](https://github.com/cmusphinx/cmudict)
- [Flite (Festival Lite)](https://github.com/festvox/flite)
