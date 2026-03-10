# フランス語G2P (DotNetG2P.French) アーキテクチャ設計

## 1. 概要・設計方針

### 1.1 目標

フランス語テキストからIPA音素列への変換を、純C#のルールベースで実装する。外部バイナリ依存やGPLコード移植を排除し、Apache-2.0ライセンスで統一する。

### 1.2 設計方針

- **ルールベース**: 辞書ルックアップではなくルールベースを主軸とし、例外辞書で補完する（LIA_Phonが99.3%達成しておりルールベースの妥当性は実証済み）
- **既存パッケージとの一貫性**: `DotNetG2P.Spanish` の設計パターンを踏襲する（Phoneme enum : byte、sealed Engine + IDisposable、イミュータブルOptions、static内部クラス群）
- **段階的実装**: Phase1でルールベースG2P、Phase2でリエゾン対応
- **独立パッケージ**: `DotNetG2P.Core` に依存せず独立動作する（スペイン語・英語・中国語と同方針）

### 1.3 精度目標

| フェーズ | 目標PER | 手法 |
|---------|---------|------|
| F1（コアルール） | 8-12% | コアG2Pルール |
| F2（例外辞書・正規化） | 3-6% | ルール + 例外辞書（500-1000語）+ テキスト正規化 |
| F3（テスト・評価確定） | 3-6%（確定値） | F2と同等（テスト拡充・品質保証フェーズ） |
| 将来（リエゾン対応後） | 2-4% | ルール + 例外辞書 + 必須リエゾン |

### 1.4 評価コーパス

| データセット | エントリ数 | ライセンス | 用途 |
|------------|-----------|----------|------|
| ipa-dict fr_FR | 約43,000 | MIT | 一次PER評価 |
| WikiPron fra_latn_broad_filtered | 約15,000-20,000 | Apache-2.0 | 交差検証 |
| Lexique 3.83 | 約140,000 | CC-BY-NC | 参照用のみ（埋め込み不可） |

---

## 2. 音素体系 (FrenchIpaPhoneme enum)

### 2.1 設計

`FrenchIpaPhoneme : byte` で40エントリ。母音を先頭ブロックに配置し、範囲比較 (`phoneme <= OeNasal`) で高速母音判定を可能にする。

```csharp
public enum FrenchIpaPhoneme : byte
{
    // === 口母音 (0-11) ===
    A = 0,        // /a/  patte
    Ah = 1,       // /ɑ/  pâte (後舌a、方言オプション)
    E = 2,        // /e/  été
    Eh = 3,       // /ɛ/  bête
    I = 4,        // /i/  lit
    O = 5,        // /o/  beau
    Oh = 6,       // /ɔ/  sort
    U = 7,        // /u/  loup
    Y = 8,        // /y/  lu
    Oe = 9,       // /ø/  peu (開音節)
    Oeh = 10,     // /œ/  peur (閉音節)
    Schwa = 11,   // /ə/  le

    // === 鼻母音 (12-15) ===
    ANasal = 12,  // /ɑ̃/  an, en
    ONasal = 13,  // /ɔ̃/  on
    ENasal = 14,  // /ɛ̃/  in, un (デフォルト3鼻母音: /œ̃/→/ɛ̃/合流)
    OeNasal = 15, // /œ̃/  un (オプション4鼻母音)

    // === 半母音 (16-18) ===
    J = 16,       // /j/  yeux
    W = 17,       // /w/  oui
    Uj = 18,      // /ɥ/  lui

    // === 閉鎖音 (19-24) ===
    P = 19,       // /p/  pain
    B = 20,       // /b/  bon
    T = 21,       // /t/  ton
    D = 22,       // /d/  don
    K = 23,       // /k/  car
    G = 24,       // /ɡ/  gare

    // === 摩擦音 (25-30) ===
    F = 25,       // /f/  fin
    V = 26,       // /v/  vin
    S = 27,       // /s/  son
    Z = 28,       // /z/  zone
    Sh = 29,      // /ʃ/  chat
    Zh = 30,      // /ʒ/  jour

    // === 鼻音 (31-33) ===
    M = 31,       // /m/  main
    N = 32,       // /n/  non
    Ny = 33,      // /ɲ/  agneau

    // === 側面音 (34) ===
    L = 34,       // /l/  lune

    // === ふるえ/はじき音 (35) ===
    R = 35,       // /ʁ/  rue (口蓋垂音: 話者・文脈により摩擦音[ʁ]/ふるえ音[ʀ]/接近音[ʁ̞]等の実現形あり)

    // === 異音 (36-39) ===
    Rh = 36,      // /χ/  /ʁ/の無声化異音
    Ng = 37,      // /ŋ/  parking (借用語)
    Ts = 38,      // /ts/ pizza (借用語)
    Dz = 39,      // /dz/ adze (借用語)
}
```

### 2.2 母音判定ヘルパー

```csharp
// 範囲比較で高速母音判定
public bool IsVowel => Phoneme <= FrenchIpaPhoneme.OeNasal;
public bool IsOralVowel => Phoneme <= FrenchIpaPhoneme.Schwa;
public bool IsNasalVowel => Phoneme >= FrenchIpaPhoneme.ANasal && Phoneme <= FrenchIpaPhoneme.OeNasal;
public bool IsSemivowel => Phoneme >= FrenchIpaPhoneme.J && Phoneme <= FrenchIpaPhoneme.Uj;
```

### 2.3 方言による音素差異

| 特徴 | Metropolitan (デフォルト) | Conservative |
|------|-------------------------|-------------|
| /a/ vs /ɑ/ | 統合: すべて /a/ | 区別: pâte=/ɑ/, patte=/a/ |
| /œ̃/ vs /ɛ̃/ | 合流: すべて /ɛ̃/ | 区別: un=/œ̃/, in=/ɛ̃/ |
| シュワー | 保持（脱落予測なし） | 保持 |

---

## 3. G2Pパイプライン設計

### 3.1 全体フロー

```
入力テキスト
    │
    ▼
┌──────────────────┐
│ FrenchNormalizer  │  テキスト正規化（11段階パイプライン）  [F2実装済み]
│  NFC→略語→日付  │  →時刻→通貨→%→単位→小数→数値→記号→空白
└──────────────────┘
    │
    ▼
┌──────────────────┐
│ Tokenize         │  単語分割（空白分割 + アポストロフ/ハイフン保持）  [F2実装済み]
└──────────────────┘
    │  単語リスト
    ▼
┌──────────────────────────────────┐
│ ExceptionDictionary.TryLookup    │  例外辞書ルックアップ（hit→スキップ）  [F2実装済み: 500+エントリ]
│  方言フォールバック: 特定方言→全方言 │
└──────────────────────────────────┘
    │  miss
    ▼
┌──────────────────────────────────┐
│ GraphemeToPhonemeRules.Convert   │  コアG2Pルール変換  [F1実装済み]
│   1. ダイグラフ/トライグラフ特定  │
│   2. c/g/s/x 文脈依存判定        │
│   3. 鼻母音化判定                │
│   4. 半母音化                    │
│   5. 位置の法則 (e/ɛ, o/ɔ, ø/œ) │
│   6. 黙字処理                    │
└──────────────────────────────────┘
    │  音素列
    ▼
┌──────────────────────────────────┐
│ FrenchSyllabifier.Syllabify      │  音素ベース音節分割  [F1実装済み]
└──────────────────────────────────┘
    │  音節付き音素列
    ▼
┌──────────────────────────────────┐
│ AllophoneProcessor.Apply         │  異音規則（オプション）  [F2実装済み: 2必須規則]
│   - /ʁ/→[χ] 無声化             │
│   - 阻害音有声性同化（逆行同化） │
└──────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────┐
│ IpaConverter                     │  出力フォーマット変換  [F1実装済み]
└──────────────────────────────────┘
    │
    ▼
  出力文字列
```

### 3.2 スペイン語G2Pとの比較

| 処理段階 | スペイン語 | フランス語 | 差異 |
|---------|----------|----------|------|
| 正規化 | SpanishNormalizer | FrenchNormalizer | 20進法、カンマ小数点 |
| トークン化 | SpanishNormalizer.Tokenize | FrenchNormalizer.Tokenize | アポストロフ保持 |
| 例外辞書 | SpanishExceptionDictionary | FrenchExceptionDictionary | 同一TSV形式 |
| G2Pルール | 3フェーズ（ダイグラフ→文脈→単純） | 6フェーズ（上記参照） | 大幅に複雑 |
| 音節分割 | 正書法ベース | 音素ベース | 分割タイミングが異なる |
| ストレス | StressAssigner（語レベル） | **不要**（フランス語は語ストレスなし） | フランス語で省略 |
| 異音処理 | AllophoneProcessor | AllophoneProcessor | 規則内容が異なる |
| リエゾン | 不要 | LiaisonProcessor（Phase2） | フランス語固有 |

### 3.3 スペイン語との重要な設計差異

1. **StressAssignerが不要**: フランス語は語レベルストレスを持たない。句末音節への自動強勢は韻律レベルの現象であり、単語G2Pでは扱わない
2. **音節分割が音素ベース**: スペイン語は正書法上の音節分割を行い、その後G2P変換を実施するが、フランス語は黙字やダイグラフが多いため先にG2P変換→音素ベース音節分割の順序が適切
3. **鼻母音化が中核処理**: 母音+n/m の後続文字によって鼻母音/非鼻母音を判定する処理がパイプラインの中核
4. **リエゾン**: フランス語固有の音韻現象。Phase2で必須リエゾンのみルールベースで対応

---

## 4. プロジェクト構成（ファイル構成）

F4実装完了時点のファイル構成。F1で作成したコアルールファイル群に加え、F2で `Normalization/`、`Data/`、`Rules/AllophoneProcessor.cs` を追加し、F3で `Conversion/XSampaConverter.cs` を追加した。F4では `DotNetG2P.Multilingual` にフランス語を統合した。

```
src/DotNetG2P.French/
├── DotNetG2P.French.csproj             # .NET Standard 2.1
├── FrenchG2PEngine.cs                  # メインAPI (sealed class, IDisposable) [F3: ToXSampa系3メソッド追加]
├── FrenchG2POptions.cs                 # イミュータブルオプション (F2: EnableAllophones, AllophoneFeatures, EnableExceptionDictionary 追加)
├── FrenchAllophoneFeatures.cs          # [Flags] enum : byte (5規則、Obligatory/Default/All プリセット)
├── Models/
│   ├── FrenchIpaPhoneme.cs             # IPA音素 enum : byte (40種)
│   ├── FrenchPhoneme.cs                # 音素 readonly struct (Phoneme + IsSyllableNucleus)
│   ├── FrenchPronunciation.cs          # 発音クラス (音素配列 + 音節オフセット)
│   └── FrenchDialect.cs               # 方言 enum : byte (Metropolitan, Conservative)
├── Rules/
│   ├── GraphemeToPhonemeRules.cs       # コアG2Pルール (6フェーズ) [F1]
│   ├── FrenchOrthography.cs            # 正書法ヘルパー (母音判定、ダイグラフ判定等) [F1]
│   ├── NasalVowelizer.cs              # 鼻母音化ロジック (独立static class) [F1]
│   ├── FrenchSyllabifier.cs            # 音素ベース音節分割 [F1]
│   └── AllophoneProcessor.cs           # 異音規則 (R無声化、阻害音有声性同化) [F2]
├── Normalization/                      # [F2 で追加]
│   ├── FrenchNormalizer.cs             # テキスト正規化ファサード (11段階パイプライン)
│   └── NumberToWords.cs               # フランス語数詞変換 (vigesimal 20進法)
├── Data/                               # [F2 で追加]
│   ├── FrenchExceptionDictionary.cs    # 例外辞書ルックアップ (方言フォールバック付き)
│   └── french_exceptions.master.tsv    # 例外辞書TSV (571行、500+エントリ、EmbeddedResource)
├── Conversion/
│   ├── IpaConverter.cs                 # IPA文字列変換 [F1]
│   └── XSampaConverter.cs             # X-SAMPA変換 (40音素マッピング) [F3]
├── package.json                        # UPM (com.dotnetg2p.french)
└── DotNetG2P.French.asmdef            # Unity Assembly Definition
```

**F1→F2 の差分**: `Normalization/` ディレクトリ一式（FrenchNormalizer + NumberToWords）、`Data/` ディレクトリ一式（FrenchExceptionDictionary + TSV）、`Rules/AllophoneProcessor.cs`、`FrenchAllophoneFeatures.cs` を新規追加。`FrenchG2POptions.cs` に異音・例外辞書関連プロパティを追加。`FrenchG2PEngine.cs` に AllophoneProcessor 呼び出しを統合。

**F2→F3 の差分**: `Conversion/XSampaConverter.cs` を新規追加。`FrenchG2PEngine.cs` に `ToXSampa()`, `ToXSampaWithoutStress()`, `ToXSampaBatch()` の3メソッドを追加。評価ツール `tools/DotNetG2P.FrenchEval/` 一式と評価スクリプト群を新規追加。

**F3→F4 の差分**: `DotNetG2P.Multilingual` にフランス語を統合。`Language.French`、`TextSegmenter` のフランス語言語判定（高頻度語46語+接尾辞23種+特有文字27種+é曖昧フォールバック）、`MultilingualG2PEngine` に `FrenchG2PEngine` 統合、`MultilingualG2POptions` に `FrenchOptions` 追加。csproj/package.json/asmdefにFrench依存追加。

---

## 5. 各コンポーネントの設計

### 5.1 FrenchG2PEngine（F3実装済み）

スペイン語の `SpanishG2PEngine` と同一パターンで実装。F2で AllophoneProcessor 呼び出しと例外辞書連携を統合。F3で X-SAMPA 変換APIを追加した。

```csharp
public sealed class FrenchG2PEngine : IDisposable
{
    private readonly FrenchG2POptions _options;
    private int _disposed;  // Interlocked.CompareExchange + Volatile.Read パターン

    public FrenchG2PEngine();
    public FrenchG2PEngine(FrenchG2POptions options);

    // --- 基本API ---
    public string ToPhonemes(string text);
    public string ToIPA(string text);
    public string ToIPAWithoutStress(string text);
    public string ToXSampa(string text);                    // [F3追加]
    public string ToXSampaWithoutStress(string text);       // [F3追加]
    public IReadOnlyList<FrenchPhoneme> ToPhonemeList(string text);
    public IReadOnlyList<FrenchPhoneme[]> ToSyllables(string word);

    // --- バッチAPI ---
    public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
    public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts);
    public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts);  // [F3追加]
    public IReadOnlyList<IReadOnlyList<FrenchPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts);

    public void Dispose();
}
```

#### エンジン内部の処理フロー（F2統合後）

エンジン内部で AllophoneProcessor を呼び出す箇所は以下の3つ:

**1. `ProcessText()` — 文字列出力API共通メソッド**

`ToPhonemes()`, `ToIPA()`, `ToIPAWithoutStress()` から呼ばれる共通メソッド。

```
入力テキスト
  → GetWords(text): Normalize → Tokenize → 単語リスト
  → 単語ごとに:
      GraphemeToPhonemeRules.ConvertWord(word, dialect, enableExceptionDictionary)
        ↓ (例外辞書ヒット時はルールをスキップ)
      if EnableAllophones:
        AllophoneProcessor.Apply(pronunciation, allophoneFeatures)
      formatter(pronunciation) → 文字列
  → 結合して返す
```

**2. `ToPhonemeList()` — 音素リスト出力**

ProcessText と同じ Normalize → Tokenize → ConvertWord → AllophoneProcessor.Apply のフローだが、formatter の代わりに `pronunciation.PhonemesInternal` をリストに追加する。

**3. `ToSyllables()` — 音節分割出力**

単一単語を対象。ConvertWord → AllophoneProcessor.Apply の後、`FrenchSyllabifier.Syllabify()` で音節分割し、音節ごとの `FrenchPhoneme[]` 配列を返す。

#### 例外辞書の統合ポイント

`EnableExceptionDictionary` オプションは `GraphemeToPhonemeRules.ConvertWord()` に引数として渡される。ConvertWord 内部で `FrenchExceptionDictionary.TryLookup()` を最初に呼び出し、ヒットした場合はルールベース変換をスキップして辞書の発音を返す。

**注意**: フランス語は語レベルストレスを持たないため `ToIPAWithoutStress` は `ToIPA` と同一出力になるが、API一貫性のために提供する。将来的に句ストレス対応を追加する場合のAPIフックとしても機能する。

### 5.2 FrenchG2POptions（F2実装済み）

```csharp
public sealed class FrenchG2POptions
{
    public static readonly FrenchG2POptions Default = new FrenchG2POptions();

    public FrenchDialect Dialect { get; }                  // デフォルト: Metropolitan
    public bool IncludeStress { get; }                     // デフォルト: false
    public bool EnableAllophones { get; }                  // デフォルト: false   [F2追加]
    public bool EnableTextNormalization { get; }           // デフォルト: true    [F2追加]
    public bool EnableExceptionDictionary { get; }         // デフォルト: true    [F2追加]
    public FrenchAllophoneFeatures AllophoneFeatures { get; } // デフォルト: Default [F2追加]
    public string Separator { get; }                       // デフォルト: " "

    public FrenchG2POptions(
        FrenchDialect dialect = FrenchDialect.Metropolitan,
        bool includeStress = false,
        bool enableAllophones = false,
        bool enableTextNormalization = true,
        bool enableExceptionDictionary = true,
        string separator = " ",
        FrenchAllophoneFeatures allophoneFeatures = FrenchAllophoneFeatures.Default);
}
```

### 5.3 FrenchDialect

```csharp
namespace DotNetG2P.French
{
    public enum FrenchDialect : byte
    {
        /// <summary>パリ標準フランス語（/a/-/ɑ/統合、/œ̃/-/ɛ̃/合流）。</summary>
        Metropolitan = 0,

        /// <summary>保守的標準フランス語（/a/-/ɑ/区別、/œ̃/-/ɛ̃/区別）。</summary>
        Conservative = 1,
    }
}
```

Metropolitan方言では:
- `/ɑ/` を `/a/` に統合（`Ah` → `A` にマッピング）
- `/œ̃/` を `/ɛ̃/` に合流（`OeNasal` → `ENasal` にマッピング）
- 3鼻母音体系（/ɑ̃/, /ɔ̃/, /ɛ̃/）

Conservative方言では:
- `/a/` と `/ɑ/` を区別（`pâte` = /ɑ/, `patte` = /a/）
- `/œ̃/` と `/ɛ̃/` を区別（4鼻母音体系: /ɑ̃/, /ɔ̃/, /ɛ̃/, /œ̃/）

鼻母音体系の選択は `Dialect` に統合する（`UseFourNasalVowels` は設けない）。Metropolitan方言で明示的に4鼻母音を使いたい場合は `Conservative` を選択する。

### 5.4 GraphemeToPhonemeRules（処理順序・規則一覧）

フランス語G2Pの中核。6フェーズで処理する。

#### フェーズ1: ダイグラフ/トライグラフ特定

最長一致でマルチグラフを認識する。

| グラフ | 音素 | 条件 |
|--------|------|------|
| eau | /o/ | 常時 |
| ain, aim | /ɛ̃/ | +子音 or 語末 |
| ein, eim | /ɛ̃/ | +子音 or 語末 |
| oin | /wɛ̃/ | +子音 or 語末 |
| ien | /jɛ̃/ | 特定コンテキスト |
| ou | /u/ | 常時 |
| oi | /wa/ | 常時 |
| ai, ei | /ɛ/ | デフォルト |
| au | /o/ | 常時 |
| eu, oeu | /ø/ or /œ/ | 位置の法則 |
| an, am, en, em | /ɑ̃/ | +子音 or 語末 |
| on, om | /ɔ̃/ | +子音 or 語末 |
| in, im, yn, ym | /ɛ̃/ | +子音 or 語末 |
| un, um | /ɛ̃/ or /œ̃/ | 方言依存 |
| tion | /sjɔ̃/ | 接尾辞 `-tion` |
| sion | /zjɔ̃/ | 接尾辞 `-sion` |
| ssion | /sjɔ̃/ | 接尾辞 `-ssion` |
| ille | /ij/ | 語中 `-ill-` + 母音（例: fille, famille） |
| aille | /aj/ | 接尾辞（例: travaille） |
| eille | /ɛj/ | 接尾辞（例: abeille） |
| euille | /œj/ | 接尾辞（例: feuille） |
| ouille | /uj/ | 接尾辞（例: mouille） |
| ey | /ɛj/ | 例: volley |
| ch | /ʃ/ | 常時 |
| gn | /ɲ/ | 常時 |
| ph | /f/ | 常時 |
| th | /t/ | 常時 |
| qu | /k/ | 常時 |
| gu + {e,i,y} | /ɡ/ | 前舌母音前（例: guerre, guide） |
| sc + {e,i,y} | /s/ | 前舌母音前（例: science, scène） |

##### トレマ（ë, ï, ü）によるダイグラフ分離

トレマ（¨）は前の母音との分離を示す記号であり、ダイグラフ認識を抑制する:

| 例 | 処理 | 出力 |
|----|------|------|
| naïf | `ai` ダイグラフとして認識**しない**（ïで分離） → /a/ + /i/ | /naif/ |
| Noël | `oe` ダイグラフとして認識**しない**（ëで分離） → /ɔ/ + /ɛ/ | /nɔɛl/ |
| aiguë | 語末 `-gue` の `u` が発音されることを示す → /ɛɡy/ | /ɛɡy/ |

フェーズ1のダイグラフ認識では、トレマ付き文字を含むグラフェム組み合わせをダイグラフから除外する処理を行う。

#### フェーズ2: c/g/s/x 文脈依存判定

| 書記素 | 音素 | 条件 |
|--------|------|------|
| c + {e,i,y,è,é,ê,î} | /s/ | 前舌母音前 |
| c + 他 | /k/ | それ以外 |
| ç | /s/ | 常時 |
| g + {e,i,y,è,é,ê,î} | /ʒ/ | 前舌母音前 |
| g + 他 | /ɡ/ | それ以外 |
| s（母音間） | /z/ | 母音-s-母音 |
| s（その他） | /s/ | それ以外 |
| ex- + 母音 | /ɛɡz/ | 接頭辞 `ex-` + 母音（例: examen /ɛɡzamɛ̃/） |
| x + 子音 | /ks/ | それ以外 |
| x（語末） | 黙字 | 多くの場合 |

#### フェーズ3: 鼻母音化判定

核心ロジック。母音+n/m の後続文字をチェック:

```
if 母音 + n/m:
    if 次の文字が母音 or 同じn/m:
        → 非鼻母音化（母音 + /n/ or /m/）
    else:
        → 鼻母音化（対応する鼻母音）
```

| 入力パターン | 出力 | 例 |
|-------------|------|-----|
| an/am/en/em + 子音/語末 | /ɑ̃/ | enfant, ample |
| an/am/en/em + 母音/nn/mm | /an/, /am/ 等 | année, ennui |
| on/om + 子音/語末 | /ɔ̃/ | bon, ombre |
| on/om + 母音/nn/mm | /ɔn/, /ɔm/ | bonne |
| in/im/yn/ym + 子音/語末 | /ɛ̃/ | fin, timbre |
| in/im + 母音/nn/mm | /in/, /im/ | fine, immeuble |
| un/um + 子音/語末 | /ɛ̃/ (Metropolitan) or /œ̃/ (Conservative) | un, parfum |

#### フェーズ4: 半母音化

| 条件 | 変換 | 例 |
|------|------|-----|
| /i/ + 母音 | /j/ + 母音 | lion → /ljɔ̃/ |
| /u/ + 母音 | /w/ + 母音 | oui → /wi/ |
| /y/ + 母音 | /ɥ/ + 母音 | lui → /lɥi/ |

##### y（イグレック）の二重母音的扱い

フランス語の `y` は語中で2つの母音に分裂する振る舞いを示す:

| パターン | 処理 | 例 |
|---------|------|-----|
| 母音 + y + 母音 | y を /i/ + /j/ として扱う（前の母音と /i/ が分離、/j/ が後の母音と結合） | payer → /peje/, voyage → /vwajaʒ/ |
| 語頭 y + 母音 | /j/ + 母音 | yeux → /jø/ |
| 語頭/語中 y + 子音 or 語末 y | /i/ | type → /tip/, pays → /pei/ |

この処理はフェーズ1のダイグラフ認識（`oy` → /waj/, `ay` → /ɛj/ 等）とフェーズ4の半母音化の組み合わせで実現する。

#### フェーズ5: 位置の法則 (loi de position)

開音節/閉音節で母音の質が決まる:

| 母音 | 開音節（CV） | 閉音節（CVC） |
|------|------------|-------------|
| e系 | /e/ | /ɛ/ |
| o系 | /o/ | /ɔ/ |
| eu系 | /ø/ | /œ/ |

**実装方針 — 接尾辞パターンヒューリスティクスによるグラフェムレベル簡易音節予測**:

位置の法則は音節構造に依存するが、フランス語の音節分割は音素ベース（G2P変換後）で行う設計であるため、そのままでは循環依存が生じる。この問題を以下の方式で解決する:

1. **グラフェムレベル簡易音節予測**: G2Pルール内で、対象母音の後続書記素パターンから開音節/閉音節を推定する。具体的には、母音の後に続く子音字の数と種類を先読みし、以下のヒューリスティクスを適用する:
   - 母音字の直後が**母音字**または**語末** → 開音節と推定
   - 母音字の直後が**子音字1つ + 母音字** → 開音節と推定（CV.CV パターン）
   - 母音字の直後が**子音字2つ以上**で、次の母音までの子音クラスタが有効なonset（閉鎖/摩擦 + l/r 等）→ 開音節と推定
   - それ以外 → 閉音節と推定
2. **接尾辞パターン優先**: 頻出接尾辞は明示的にマッピングする（例: `-tion`→/sjɔ̃/, `-ment`→/mɑ̃/, `-eur`→/œʁ/, `-euse`→/øz/, `-eux`→/ø/, `-ais/-ait`→常に/ɛ/）。形態論的条件が位置の法則に優先するケースを接尾辞パターンで捕捉する
3. **例外はデフォルト+例外辞書**: ヒューリスティクスで判定できないケースは、デフォルト母音質を仮決定し、例外辞書で上書きする

この方式により、音素ベース音節分割（FrenchSyllabifier）はG2P変換後の音素列に対して一度だけ実行すれば十分であり、循環依存は生じない。FrenchSyllabifierの結果は音節オフセット情報としてFrenchPronunciationに格納するが、位置の法則の判定には使用しない。

#### フェーズ6: 黙字処理

| パターン | 規則 | 例 |
|---------|------|-----|
| 語末 -e | 黙字（ただし単音節語は /ə/） | table, le |
| 語末 -es | 黙字 | tables |
| 語末 -ent | **デフォルト: 鼻母音 /ɑ̃/**（形容詞/副詞/名詞: prudent, lent, accent）。動詞3人称複数活用形（parlent, chantent等）は例外辞書で黙字として登録する。単語G2Pでは品詞情報がないため、出現頻度の高い形容詞/副詞をデフォルトとする | prudent→/pʁydɑ̃/, parlent→黙字(例外辞書) |
| 語末 -er | /e/（r は黙字） | parler, manger, premier |
| 語末 -et | /ɛ/（t は黙字） | ballet, billet |
| 語末 -ed | /e/（d は黙字） | pied |
| 語末 -ez | /e/（z は黙字） | chez, parlez |
| 語末子音 | CaReFuL規則: c, r, f, l は発音、他は黙字（主な例外は下記参照） | parc, par, neuf, sel |
| 語中 h | 常に黙字 | cahier |
| 語頭 h | 黙字（ただしh aspiréリスト参照） | homme, haricot* |

##### CaReFuL規則の主な例外

以下の例外は例外辞書に登録する。Phase1実装時に例外辞書の初期データとして組み込む:

| 子音 | 例外（黙字になるケース） | 例 |
|------|------------------------|-----|
| -c | 鼻子音後・一部語末 | tabac, estomac, porc, blanc, franc |
| -r | `-er` 動詞不定形、`-ier` 接尾辞 | parler, manger, premier, dossier |
| -l | `-il` 接尾辞の一部 | fusil, outil, gentil |
| -f | 極少数 | cerf, clef（clé の異綴り） |

### 5.5 FrenchSyllabifier

**音素ベース**の音節分割。スペイン語の正書法ベースとは異なる。

```csharp
namespace DotNetG2P.French.Rules
{
    internal static class FrenchSyllabifier
    {
        public static (int[] syllableOffsets, FrenchPhoneme[] phonemesWithNucleus) Syllabify(FrenchIpaPhoneme[] phonemes);
    }
}
```

#### 音節分割規則

1. **開音節優勢（Open Syllable Preference）**: 可能な限りCV構造を優先
2. **Onset Maximization**: 子音クラスタは次の音節のonsetに最大限割り当て
3. **有効Onset**:
   - 単子音: すべて有効
   - 二子音: 閉鎖/摩擦 + /l, ʁ/（ただし /tl, dl/ は不可）
   - 三子音: /stʁ/, /skʁ/ 等
4. **二重母音なし**: フランス語に音韻的二重母音はない。/wa/, /ɥi/ 等は半母音+母音

#### StressAssignerが不要な理由

フランス語は語レベルのストレスを持たない。強勢はリズムグループの最終音節に自動的に付与される韻律現象であり、単語単位のG2P処理では扱わない。このため `SpanishG2P` にある `StressAssigner` に相当するコンポーネントは設けない。

`FrenchPronunciation` クラスの `StressedSyllableIndex` は常に `-1` とする。将来的に句レベル韻律処理を追加する場合のために構造体は保持する。

#### SchwaProcessor（将来設計指針）

Phase1ではシュワー（/ə/）を保持し、脱落予測は行わない。将来のPhase（F2以降）で以下のSchwaProcessor設計を検討する:

- **三子音の法則（loi des trois consonnes）**: シュワーが脱落すると3子音連続が生じる場合は保持する
- **語境界でのシュワー脱落**: 語境界を超えたシュワー脱落はリエゾン/enchaînementと同じくPhase2スコープ
- **設計上の位置づけ**: `Rules/SchwaProcessor.cs` として独立static classに配置し、AllophoneProcessor実行前にオプション適用する
- シュワー脱落はフランス語TTS/ASR品質に大きく影響する重要な処理であり、PER改善にも寄与する

### 5.6 FrenchNormalizer（F2実装済み）

`SpanishNormalizer` のサブモジュール分割パターンを採用する。F2で11段階パイプラインとして実装完了。

```csharp
namespace DotNetG2P.French.Normalization
{
    internal static class FrenchNormalizer
    {
        public static string Normalize(string text);
        public static string[] Tokenize(string text);
    }
}
```

#### 11段階正規化パイプライン

`Normalize()` は以下の11段階を順次適用する。各段階は独立した private メソッドとして実装されており、パイプライン順序は展開の依存関係に基づく（例: 略語展開で生成された数字が後段の数値展開で処理される）。

| 段階 | メソッド | 処理内容 | 例 |
|------|---------|---------|-----|
| 1 | NFC + ToLowerInvariant | Unicode正規化 + 小文字化 | `É` → `é` |
| 2 | ExpandAbbreviations | 略語展開（13パターン） | `M.` → `monsieur`, `etc.` → `et cetera` |
| 3 | ExpandDates | 日付展開（DD/MM/YYYY、DD-MM-YYYY、DD.MM.YYYY） | `25/12/2024` → `le vingt-cinq décembre deux mille vingt-quatre` |
| 4 | ExpandTimes | 時刻展開（NNhNN形式、0h→minuit、12h→midi） | `14h30` → `quatorze heures trente` |
| 5 | ExpandCurrencies | 通貨展開（€後置/前置、$前置/後置、単複対応） | `5,50€` → `cinq euros cinquante centimes` |
| 6 | ExpandPercentages | パーセント展開（小数対応） | `3,14%` → `trois virgule un quatre pour cent` |
| 7 | ExpandUnits | 単位展開（km/kg/cm/mm/m/l/°C、単複対応） | `100km` → `cent kilomètres` |
| 8 | ExpandDecimals | 小数展開（カンマ小数点→virgule） | `3,14` → `trois virgule un quatre` |
| 9 | ExpandNumbers | 残数値展開（正規表現で全数字キャプチャ） | `42` → `quarante-deux` |
| 10 | ExpandSymbols | 記号→読み変換（&/\@/§/#/+/=） | `&` → `et` |
| 11 | NormalizeWhitespace | 空白正規化 + trim（連続空白を1つに） | `  a  b  ` → `a b` |

**設計上の要点**:
- 段階3-7は段階9（汎用数値展開）より前に実行する。これにより、日付・時刻・通貨等の構造化された数値パターンが先に適切な文脈で展開され、残った裸の数字だけが段階9で処理される
- 段階8（小数展開）は段階9の前に実行する。`N,N` パターンを先にキャプチャし、カンマ区切りの数字が誤って整数として展開されるのを防ぐ
- 略語展開（段階2）は13パターンの Regex.Replace で実装。`\b` ワードバウンダリで誤マッチを防止

#### Tokenize()

`Normalize()` の出力を受け取り、空白分割でトークン列を生成する。以下の特殊処理を含む:

- **アポストロフ保持**: `'` / `'` (U+2019) をトークン内に保持する（エリジオン: `l'homme` → `"l'homme"` として1トークン）
- **ハイフン保持**: 複合語内のハイフンを保持する（`peut-être` → `"peut-être"` として1トークン）。ただし、ハイフンの後に文字が続く場合のみ
- **句読点除去**: アポストロフ・ハイフン以外の非文字・非数字は区切りとして扱う

#### 正規化規則サマリ

| カテゴリ | 規則 | 例 |
|---------|------|-----|
| Unicode正規化 | NFC + 小文字化 | 常時 |
| 略語 | M., Mme, Dr, etc., n°, av./ap. J.-C. 等13パターン | `M.` → `monsieur` |
| 日付 | DD/MM/YYYY（1er→premier、他は基数詞） | `01/03/2024` → `le premier mars deux mille vingt-quatre` |
| 時刻 | NNhNN（0h→minuit、12h→midi） | `0h` → `minuit`, `14h30` → `quatorze heures trente` |
| 通貨 | euro(s)/centime(s), dollar(s)/cent(s)（単複自動判定） | `1€` → `un euro` |
| パーセント | 整数/小数 + % → pour cent | `50%` → `cinquante pour cent` |
| 単位 | km, kg, cm, mm, m, l, °C（単複自動判定） | `1km` → `un kilomètre` |
| 小数点 | カンマが小数点 → virgule + 各桁読み | `3,14` → `trois virgule un quatre` |
| 数字 | 20進法（vigesimal） | `70` → `soixante-dix` |
| 記号 | &→et, @→arobase, §→paragraphe, #→dièse, +→plus, =→égal | `&` → `et` |
| 空白 | 連続空白→単一スペース + trim | 常時 |

#### NumberToWords: フランス語20進法（F2実装済み）

`NumberToWords` は `long` 型の数値をフランス語読みに変換する。vigesimal（20進法）による70-99の特殊な読み方を正確に実装する。

```
70: soixante-dix (60+10)
71: soixante et onze (60+11)    ← "et" 挿入
72-79: soixante-douze ... soixante-dix-neuf
80: quatre-vingts (4×20, 末尾s)  ← 後続なしなら "s" あり
81: quatre-vingt-un (4×20+1, sなし)
82-89: quatre-vingt-deux ... quatre-vingt-neuf
90: quatre-vingt-dix (4×20+10)
91: quatre-vingt-onze
92-99: quatre-vingt-douze ... quatre-vingt-dix-neuf
```

**実装済みAPI**:

| メソッド | 説明 | 例 |
|---------|------|-----|
| `Convert(long number)` | 数値→フランス語基数詞 | `80` → `"quatre-vingts"` |
| `Convert(string text)` | 文字列→数値パース→変換 | `"42"` → `"quarante-deux"` |
| `ConvertOrdinal(string text)` | 序数詞変換（1er→premier、Ne→Nième） | `"2e"` → `"deuxième"`, `"1ère"` → `"première"` |
| `ConvertDigits(string digits)` | 個別桁読み（小数部用） | `"14"` → `"un quatre"` |

**スケール対応**: 0〜milliard（10億）まで対応。内部で `ConvertTens` → `ConvertHundreds` → `ConvertThousands` → `ConvertMillions` → `ConvertBillions` と再帰的に分解する。

**フランス語数詞の正書法ルール**:
- `et` 挿入: 21, 31, 41, 51, 61, 71 のみ（81, 91 には入らない）
- `cent` の末尾 s: `N00` で N>1 の場合のみ（`deux cents` だが `deux cent un`）
- `quatre-vingts` の末尾 s: 後続数字がない場合のみ
- `mille` は不変（`un` は付けない: `mille` not `un mille`）
- `million`/`milliard` は通常名詞として複数形 s が付く

**序数詞変換の特殊ルール**:
- `1er/1ère` → `premier/première`（不規則）
- 末尾 `e` 脱落: `quatre` → `quatr` + `ième`
- `neuf` → `neuv` + `ième`（9e = neuvième）
- `cinq` → `cinqu` + `ième`（5e = cinquième）

### 5.7 AllophoneProcessor（F2実装済み）

`Rules/AllophoneProcessor.cs` として実装。`FrenchAllophoneFeatures` flags enum で規則の有効/無効を制御する。

#### FrenchAllophoneFeatures（F2実装済み）

`FrenchAllophoneFeatures.cs` で定義。5つの異音規則をビットフラグで管理する。

```csharp
[Flags]
public enum FrenchAllophoneFeatures : byte
{
    None = 0,
    RDevoicing = 1 << 0,                    // /ʁ/→[χ] 無声化
    ObstruentVoicingAssimilation = 1 << 1,   // 阻害音有声性同化
    VowelLengthening = 1 << 2,               // 母音長化（未実装、将来用）
    LVelarization = 1 << 3,                  // /l/ 軟口蓋化（未実装、将来用）
    FinalDevoicing = 1 << 4,                 // 語末阻害音無声化（未実装、将来用）

    Obligatory = RDevoicing | ObstruentVoicingAssimilation,
    Default = Obligatory,
    All = Default | VowelLengthening | LVelarization | FinalDevoicing,
}
```

**プリセット**:
- `Obligatory`/`Default`: R無声化 + 阻害音有声性同化（F2で実装済みの2規則）
- `All`: 全5規則（VowelLengthening, LVelarization, FinalDevoicing は将来実装）

#### AllophoneProcessor の内部設計

```csharp
internal static class AllophoneProcessor
{
    public static FrenchPronunciation Apply(FrenchPronunciation pronunciation, FrenchAllophoneFeatures features);
}
```

**処理フロー**:
1. 入力 `FrenchPronunciation` の音素配列をコピー（非破壊的変換）
2. `HasFeature()` で各フラグをチェックし、有効な規則のみ適用
3. R無声化 → 阻害音有声性同化 の順で適用（順序依存: R無声化で生成された [χ] が同化の入力になりうる）
4. 変更後の音素配列と元の音節オフセットから新しい `FrenchPronunciation` を生成して返す

#### 必須異音規則（F2で実装済み）

**1. R無声化 (`ApplyRDevoicing`)**

/ʁ/ が無声阻害音（/p, t, k, f, s, ʃ/）に隣接している場合、[χ] に無声化する。

| 条件 | 変換 | 例 |
|------|------|-----|
| 無声阻害音 + /ʁ/ | /ʁ/ → [χ] | `prendre` /pʁ.../ → [pχ...] |
| /ʁ/ + 無声阻害音 | /ʁ/ → [χ] | `arche` /aʁʃ/ → [aχʃ] |
| 語末 /ʁ/ | 無声化しない | `par` /paʁ/ → [paʁ]（変化なし） |

**実装上の要点**: 語末位置の R は無声化しない（`i == phonemes.Length - 1` で除外）。これは標準フランス語の語末 R が有声のまま保持される一般的な傾向を反映する。

**2. 阻害音有声性同化 (`ApplyObstruentVoicingAssimilation`)**

阻害音クラスタ内で、後ろの阻害音の有声性に前の阻害音を統一する（逆行同化）。

| 条件 | 変換 | 例 |
|------|------|-----|
| 有声 + 無声 | 有声→無声 | `absent` /absɑ̃/ → [apsɑ̃] |
| 無声 + 有声 | 無声→有声 | `anecdote` /anɛkdɔt/ → [anɛɡdɔt] |

**実装上の要点**:
- 配列末尾から先頭に向かってスキャン（逆行同化の方向性に対応）
- `Voice()`/`Devoice()` ヘルパーで6対の有声/無声ペア変換: p↔b, t↔d, k↔ɡ, f↔v, s↔z, ʃ↔ʒ

#### オプション異音規則（将来実装）

3. **母音長母音化** (`VowelLengthening`): 有声摩擦音 /v, z, ʒ, ʁ/ の前の母音が長母音化
4. **/l/ 軟口蓋化** (`LVelarization`): コーダ位置の /l/ が暗い l に
5. **語末阻害音無声化** (`FinalDevoicing`): 語末の阻害音が無声化（フランス語では非体系的。デフォルト無効）

### 5.8 FrenchExceptionDictionary（F2実装済み）

`Data/FrenchExceptionDictionary.cs` + `Data/french_exceptions.master.tsv` として実装。スペイン語 `SpanishExceptionDictionary` と同一設計パターンで、TSV形式の埋め込みリソースを使用する。

#### アーキテクチャ

```csharp
internal static class FrenchExceptionDictionary
{
    public static bool TryLookup(string word, FrenchDialect dialect, out FrenchPronunciation pronunciation);
}
```

**データ構造**: `Dictionary<string, Dictionary<byte, FrenchPronunciation>>` の二重辞書。外側のキーは表層形（小文字）、内側のキーは方言バイト値。

**方言フォールバック**: `TryLookup` は以下の順序で検索する:
1. 指定された方言（`metropolitan` or `conservative`）の専用エントリ
2. 全方言共通エントリ（`*` = `AnyDialectKey = byte.MaxValue`）

これにより、方言固有のエントリと全方言共通エントリを同一の辞書内で共存させることができる。

**音節核設定**: TSVの音素列パース時に、各音節内の最初の母音（`phoneme <= FrenchIpaPhoneme.OeNasal`）に `IsSyllableNucleus = true` を自動設定する。これにより例外辞書エントリにも音節構造情報が保持される。

#### TSVフォーマット

```
# surface	dialect	category	stress_index	phonemes	source	note
football	*	loanword	-1	f u t|b o l	manual	English loanword
```

| カラム | 説明 |
|--------|------|
| surface | 表層形（小文字） |
| dialect | `*`（全方言）/ `metropolitan` / `conservative` |
| category | `loanword` / `academic` / `homograph` / `irregular` / `verb3pl` |
| stress_index | 強勢音節インデックス（フランス語では通常 -1） |
| phonemes | 音節区切り `\|` + スペース区切りIPA音素 |
| source | 出典 |
| note | 備考 |

**音素パーサ**: `ParsePhoneme()` で IPA 文字列→`FrenchIpaPhoneme` enum 変換。鼻母音は結合チルダ付き2文字シーケンス（例: `ɑ̃` = U+0251 + U+0303 → `ANasal`）として解析する。全40種の音素に対応。

#### 例外辞書の実績規模（F2実装時点）

571行（ヘッダ+コメント行含む）、500+実エントリ。

| カテゴリ | 主な内容 | 例 |
|---------|---------|-----|
| 外来語 (loanword) | 英語/イタリア語/ドイツ語等からの借用語 | football, pizza, parking, weekend |
| 学術語 (academic) | CaReFuL規則の例外（語末子音が発音される/されない） | bus, index, atlas |
| 動詞3人称複数 (verb3pl) | -ent が黙字になる動詞活用形 | parlent, chantent, mangent |
| 不規則語 (irregular) | 正書法と発音が大きく乖離する語 | monsieur, femme, oignon |
| 同綴異音語 (homograph) | 品詞や文脈で発音が異なる語（方言別エントリ可能） | -ent系（形容詞 vs 動詞） |

#### 例外辞書運用ワークフロー

`tools/generate_french_exceptions.ps1` でTSVを管理し、PER評価結果のエラー分析から逐次エントリを追加する運用を想定。スペイン語の `tools/generate_spanish_exceptions.ps1` と同一パターン。

### 5.9 IpaConverter / XSampaConverter（F3実装済み）

スペイン語と同一パターン。`FrenchIpaPhoneme` → IPA文字列 / X-SAMPA文字列 の静的変換。

```csharp
internal static class IpaConverter
{
    public static string Convert(FrenchPronunciation pronunciation, bool includeStress);
    public static string ConvertPhonemeSequence(FrenchPronunciation pronunciation, bool includeStress, string separator);
    public static string ToSymbol(FrenchIpaPhoneme phoneme);
}
```

#### IPA / X-SAMPA マッピング（抜粋）

| Enum | IPA | X-SAMPA |
|------|-----|---------|
| A | a | a |
| Ah | ɑ | A |
| E | e | e |
| Eh | ɛ | E |
| ANasal | ɑ̃ | A~ |
| ONasal | ɔ̃ | O~ |
| ENasal | ɛ̃ | E~ |
| OeNasal | œ̃ | 9~ |
| Y | y | y |
| Oe | ø | 2 |
| Oeh | œ | 9 |
| Schwa | ə | @ |
| Uj | ɥ | H |
| Sh | ʃ | S |
| Zh | ʒ | Z |
| Ny | ɲ | J |
| R | ʁ | R |
| Rh | χ | X |

### 5.10 LiaisonProcessor / Enchaînement (Phase2)

Phase2で実装。Phase1では省略する。

#### Enchaînement（再音節化）

Enchaînement はリエゾンより基本的な音韻現象であり、語末の発音される子音が次の語の母音と再音節化する現象:
- 例: `elle aime` → /ɛ.lɛm/（`elle` の /l/ が `aime` の母音と結合）
- リエゾンは**本来黙字の子音**が復活する現象だが、enchaînement は**常に発音される子音**の再音節化

設計上の位置づけ: LiaisonProcessor と同じPhase2スコープ。`LiaisonProcessor.ApplyMandatoryLiaison` 内でリエゾンとenchaînementの両方を処理する。両者は入力（単語列+発音列）が同じであり、同一パスで処理可能。

```csharp
namespace DotNetG2P.French.Rules
{
    internal static class LiaisonProcessor
    {
        public static IReadOnlyList<FrenchPhoneme> ApplyMandatoryLiaison(
            IReadOnlyList<string> words,
            IReadOnlyList<FrenchPronunciation> pronunciations);
    }
}
```

#### 必須リエゾン（6カテゴリ）

| カテゴリ | 例 | リエゾン子音 |
|---------|-----|------------|
| 限定詞 + 名詞 | les‿amis | /z/ |
| 代名詞 + 動詞 | vous‿avez | /z/ |
| 前置形容詞 + 名詞 | petit‿ami | /t/ |
| 前置詞 + 名詞句 | en‿été | /n/ |
| 副詞 + 形容詞 | très‿important | /z/ |
| est/sont + X | c'est‿un | /t/ |

#### 禁止リエゾン

- `et` の後（常に禁止）
- 名詞主語 + 動詞
- h aspiré語の前（h aspiréリスト参照）
- `onze` の前

#### h aspiréリスト

約200語の埋め込みリスト。`h_aspire.txt` として EmbeddedResource に格納する。

```
haricot
héros
hache
hamster
hasard
...
```

### 5.11 FrenchPhoneme / FrenchPronunciation

```csharp
public readonly struct FrenchPhoneme : IEquatable<FrenchPhoneme>
{
    public FrenchIpaPhoneme Phoneme { get; }
    public bool IsSyllableNucleus { get; }  // 音節主核フラグ

    public bool IsVowel => Phoneme <= FrenchIpaPhoneme.OeNasal;
    public bool IsNasalVowel => Phoneme >= FrenchIpaPhoneme.ANasal
                              && Phoneme <= FrenchIpaPhoneme.OeNasal;
    public bool IsSemivowel => Phoneme >= FrenchIpaPhoneme.J
                             && Phoneme <= FrenchIpaPhoneme.Uj;
}

public sealed class FrenchPronunciation
{
    internal FrenchPhoneme[] PhonemesInternal { get; }
    internal int[] SyllableOffsetsInternal { get; }
    public IReadOnlyList<FrenchPhoneme> Phonemes => PhonemesInternal;
    public int StressedSyllableIndex { get; }  // 常に -1
}
```

**スペイン語との差異**:
- `SpanishPhoneme.IsStressed` → `FrenchPhoneme.IsSyllableNucleus`（ストレスではなく音節核マーク）
- `StressedSyllableIndex` は常に `-1`（フランス語は語レベルストレスなし）

---

## 6. Multilingual統合設計（F4実装済み）

### 6.1 Language enum 拡張

```csharp
public enum Language : byte
{
    Japanese = 0,
    English = 1,
    Chinese = 2,
    Spanish = 3,
    French = 4,
}
```

### 6.2 DefaultLatinLanguage の拡張

`DefaultLatinLanguage` は `English` / `Spanish` / `French` の3言語を許可。

```csharp
public MultilingualG2POptions(
    ...
    Language defaultLatinLanguage = Language.English,
    FrenchG2POptions? frenchOptions = null)
{
    if (defaultLatinLanguage != Language.English
        && defaultLatinLanguage != Language.Spanish
        && defaultLatinLanguage != Language.French)
        throw new ArgumentOutOfRangeException(...);
}
```

### 6.3 TextSegmenter のフランス語対応

#### フランス語判定シグナル（実装済み）

```csharp
// 高頻度語シグナル（46語）
private static readonly string[] s_frenchWordSignals =
{
    "alors", "au", "aussi", "autre", "aux", "avec", "bien", "bonjour",
    "bonsoir", "ce", "cette", "comme", "dans", "depuis", "des",
    "donc", "du", "encore", "entre", "et", "faire", "ici", "jamais",
    "je", "le", "les", "leur", "mais", "merci", "monde", "ne",
    "notre", "nous", "parce", "peut", "plus", "pour", "quand",
    "sans", "seulement", "sous", "tout", "toujours", "une",
    "votre", "vous"
};

// 接尾辞シグナル（23種）
private static readonly string[] s_frenchSuffixSignals =
{
    "tion", "sion", "ment", "eux", "euse", "euses", "ence", "ance",
    "ique", "iques", "iste", "istes", "aire", "aires",
    "oire", "oires", "able", "ables", "ible", "ibles",
    "eur", "eure", "eures"
};
```

#### ラテン文字3言語振り分けアルゴリズム（実装済み）

`ResolveLatinLanguage` での判定フロー:

1. `DefaultLatinLanguage` が French/Spanish → 即座にその言語を返す
2. フランス語特有文字（27種: è/ê/ë/ô/î/ï/û/ù/ç/œ/æ/ÿ 等）→ French
3. スペイン語特有文字（ñ/á/í/ó/ú — é は除外）→ Spanish
4. é のみ（他のマーカーなし）→ French（英語圏での仏語借用語がスペイン語より多い）
5. ASCII語彙ヒューリスティクス → French / Spanish
6. フォールバック: `DefaultLatinLanguage`

**設計上の要点**: `é` (U+00E9) はフランス語・スペイン語の両方で高頻度だが、英語圏での借用語（"café", "résumé" 等）はフランス語由来が圧倒的に多いため、é のみの語はフランス語にフォールバックする。`á`/`í`/`ó`/`ú` はフランス語では使われないためスペイン語確定。

### 6.4 MultilingualG2PEngine の拡張（実装済み）

```csharp
public sealed class MultilingualG2PEngine : IDisposable
{
    private readonly FrenchG2PEngine _frenchEngine;

    private string ConvertSegment(TextSegment segment)
    {
        switch (segment.Language)
        {
            ...
            case Language.French:
                return _frenchEngine.ToPhonemes(segment.Text);
        }
    }
}
```

### 6.5 MultilingualG2POptions の拡張（実装済み）

```csharp
public sealed class MultilingualG2POptions
{
    public FrenchG2POptions? FrenchOptions { get; }
    ...
}
```

---

## 7. 方言対応設計

### 7.1 Metropolitan（パリ標準）

デフォルト方言。現代パリ方言の特徴を反映:

- `/a/`-`/ɑ/` 統合: すべて `/a/`
- `/œ̃/`-`/ɛ̃/` 合流: すべて `/ɛ̃/`（3鼻母音体系）
- シュワー保持（脱落予測なし）

### 7.2 Conservative（保守的標準）

古い規範的発音を反映:

- `/a/`-`/ɑ/` 区別: `pâte` = `/ɑ/`, `patte` = `/a/`
- `/œ̃/`-`/ɛ̃/` 区別: `un` = `/œ̃/`, `in` = `/ɛ̃/`（4鼻母音体系）

### 7.3 方言切り替えの実装箇所

| コンポーネント | 方言影響 |
|-------------|---------|
| GraphemeToPhonemeRules | 鼻母音 `/œ̃/` vs `/ɛ̃/` の選択 |
| GraphemeToPhonemeRules | `/ɑ/` vs `/a/` の選択（âを含む語） |
| FrenchExceptionDictionary | 方言別エントリの選択 |

---

## 8. 既存パッケージとの設計パターン一貫性

### 8.1 統一パターン一覧

| パターン | スペイン語での実装 | フランス語での実装 |
|---------|-------------------|-------------------|
| Engine class | `sealed class SpanishG2PEngine : IDisposable` | `sealed class FrenchG2PEngine : IDisposable` |
| Dispose | `int _disposed` + `Interlocked.CompareExchange` + `Volatile.Read` | 同一 |
| Options | `sealed class SpanishG2POptions`（イミュータブル、`Default` static field） | 同一 |
| Phoneme enum | `SpanishIpaPhoneme : byte` | `FrenchIpaPhoneme : byte` |
| Phoneme struct | `readonly struct SpanishPhoneme` | `readonly struct FrenchPhoneme` |
| Pronunciation | `sealed class SpanishPronunciation` (音素配列+音節オフセット) | 同一 |
| Dialect enum | `SpanishDialect : byte` | `FrenchDialect : byte` |
| Allophone flags | `[Flags] SpanishAllophoneFeatures : byte` | `[Flags] FrenchAllophoneFeatures : byte` (F2実装済み) |
| Exception dict | TSV埋め込み、`TryLookup(word, dialect, out pron)` | 同一 (F2実装済み、500+エントリ) |
| G2P rules | `internal static class GraphemeToPhonemeRules` | 同一 |
| Syllabifier | `internal static class SpanishSyllabifier` | `internal static class FrenchSyllabifier`（音素ベース） |
| StressAssigner | `internal static class StressAssigner` | **不要** |
| Normalizer | `internal static class SpanishNormalizer` + `NumberToWords` | 同一構造 (F2実装済み、11段階パイプライン) |
| IpaConverter | `internal static class IpaConverter` | 同一 |
| XSampaConverter | `internal static class XSampaConverter` | 同一 |
| API surface | `ToPhonemes`, `ToIPA`, `ToXSampa`, `ToPhonemeList`, `ToSyllables`, `+Batch` | 同一 |
| csproj | `.NET Standard 2.1`、独立パッケージ | 同一 |
| UPM | `package.json` + `.asmdef` | 同一 |

### 8.2 フランス語固有の新規コンポーネント

| コンポーネント | 配置 | 理由 |
|-------------|------|------|
| `NasalVowelizer` | `Rules/NasalVowelizer.cs`（独立 `internal static class`、`GraphemeToPhonemeRules` から呼び出し） | 鼻母音化ロジックが複雑なため独立クラスに分離。`SyllableParser`/`StressAssigner` と同じ配置パターン |
| `LiaisonProcessor` | `Rules/LiaisonProcessor.cs` | フランス語固有のリエゾン + enchaînement処理（Phase2） |
| `h_aspire.txt` | `Data/h_aspire.txt` (EmbeddedResource) | h aspiré語リスト（リエゾン禁止・エリジオン禁止判定用） |

### 8.3 省略するコンポーネント

| コンポーネント | 理由 |
|-------------|------|
| `StressAssigner` | フランス語は語レベルストレスを持たない |
| `HomographResolver` | 同綴異音語は例外辞書に統合（~70語のため独立クラス不要） |
| `LtsEngine` / CARTツリー | 辞書不要のルールベースで十分 |

---

## 付録A: フランス語→IPA 単純対応表

| 書記素 | IPA | 条件 |
|--------|-----|------|
| a, à | /a/ | 常時 |
| â | /ɑ/ (Conservative) or /a/ (Metropolitan) | 方言依存 |
| b | /b/ | 常時 |
| c | /s/ or /k/ | フェーズ2参照 |
| ç | /s/ | 常時 |
| d | /d/ | 語末黙字以外 |
| e | /ə/, /e/, /ɛ/ | 位置・アクセント依存 |
| é | /e/ | 常時 |
| è, ê | /ɛ/ | 常時 |
| ë | /ɛ/ | トレマ（分離記号: 前の母音とのダイグラフ形成を抑制。例: Noël→/nɔɛl/） |
| f | /f/ | 常時 |
| g | /ʒ/ or /ɡ/ | フェーズ2参照 |
| h | 黙字 | 常時 |
| i, î, ï | /i/ | 常時（ïはトレマ: 前の母音とのダイグラフ形成を抑制。例: naïf→/naif/） |
| j | /ʒ/ | 常時 |
| k | /k/ | 常時（外来語） |
| l | /l/ | 常時 |
| m | /m/ | 鼻母音化以外 |
| n | /n/ | 鼻母音化以外 |
| o | /o/ or /ɔ/ | 位置の法則 |
| ô | /o/ | 常時 |
| p | /p/ | 常時 |
| q (qu) | /k/ | 常時 |
| r | /ʁ/ | 常時 |
| s | /s/ or /z/ | フェーズ2参照 |
| t | /t/ | 常時 |
| u, û | /y/ | 常時 |
| ù | /u/ | `où` のみで使用。`ou` ダイグラフ処理で /u/ に変換される |
| v | /v/ | 常時 |
| w | /v/ or /w/ | 語源依存（多くは /v/） |
| x | /ks/, /ɡz/, 黙字 | フェーズ2参照 |
| y | /i/ or /j/ | 母音として /i/、母音前で /j/。語中で母音+y+母音は二重母音的分裂（フェーズ4参照） |
| z | /z/ | 語末黙字以外 |

## 付録B: 評価パイプライン設計（F3実装済み）

`tools/DotNetG2P.SpanishEval` をコピー改変して `tools/DotNetG2P.FrenchEval` を作成した。

```
tools/
├── DotNetG2P.FrenchEval/              # フランス語PER/WER評価ツール (F3実装済み)
│   ├── DotNetG2P.FrenchEval.csproj    # net8.0 コンソールアプリ
│   └── Program.cs                     # PER/WER評価 + フランス語IPA正規化 + 9カテゴリ別エラー分類 (~600行)
├── refresh_french_eval_data.ps1       # 評価データ取得スクリプト (F3実装済み)
├── run_french_full_evaluation.ps1     # 全量評価実行スクリプト (F3実装済み)
├── french_eval_thresholds.json        # PER閾値定義 (F3実装済み)
└── generate_french_exceptions.ps1     # 例外辞書生成スクリプト
```

#### 評価メトリクス

- **PER (Phone Error Rate)**: 音素レベルLevenshtein距離 / 参照音素数
- **WER (Word Error Rate)**: 完全一致しなかった語数 / 全語数
- **カテゴリ別集計**: nasal_vowel / silent_letter / schwa / vowel_quality / foreign_word / suffix_pattern / h_aspire / consonant / other の9カテゴリ

#### フランス語固有IPA正規化（評価時）

評価ツール内で参照IPA・仮説IPAの両方に以下の正規化を適用してから比較する:
- `/ɑ/` → `/a/`（Metropolitan方言の統合）
- `/œ̃/` → `/ɛ̃/`（鼻母音合流）

#### CLIオプション

```
--input-root    評価データルートディレクトリ
--output-root   レポート出力先ディレクトリ
--thresholds    閾値JSONファイルパス
--dataset-set   評価対象データセット（sample/full/all）
--profiles      評価プロファイル（base/allophones/no_exceptions）
--enforce-thresholds  閾値超過時に非ゼロ終了コードを返す
```
