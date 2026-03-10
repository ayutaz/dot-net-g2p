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
│ FrenchNormalizer  │  テキスト正規化（数字・通貨・日付・時刻・略語・記号展開）
└──────────────────┘
    │
    ▼
┌──────────────────┐
│ Tokenize         │  単語分割（空白分割 + アポストロフ保持）
└──────────────────┘
    │  単語リスト
    ▼
┌──────────────────────────────────┐
│ ExceptionDictionary.TryLookup    │  例外辞書ルックアップ（hit→スキップ）
└──────────────────────────────────┘
    │  miss
    ▼
┌──────────────────────────────────┐
│ GraphemeToPhonemeRules.Convert   │  コアG2Pルール変換
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
│ FrenchSyllabifier.Syllabify      │  音素ベース音節分割
└──────────────────────────────────┘
    │  音節付き音素列
    ▼
┌──────────────────────────────────┐
│ AllophoneProcessor.Apply         │  異音規則（オプション）
│   - /ʁ/→[χ] 無声化             │
│   - 阻害音有声性同化             │
│   - その他オプション規則          │
└──────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────┐
│ IpaConverter / XSampaConverter   │  出力フォーマット変換
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

## 4. プロジェクト構成（ファイル構成案）

```
src/DotNetG2P.French/
├── DotNetG2P.French.csproj             # .NET Standard 2.1
├── FrenchG2PEngine.cs                  # メインAPI (sealed class, IDisposable)
├── FrenchG2POptions.cs                 # イミュータブルオプション
├── FrenchAllophoneFeatures.cs          # [Flags] enum : byte
├── Models/
│   ├── FrenchIpaPhoneme.cs             # IPA音素 enum : byte (40種)
│   ├── FrenchPhoneme.cs                # 音素 readonly struct (Phoneme + IsSyllableNucleus)
│   ├── FrenchPronunciation.cs          # 発音クラス (音素配列 + 音節オフセット)
│   └── FrenchDialect.cs               # 方言 enum : byte (Metropolitan, Conservative)
├── Rules/
│   ├── GraphemeToPhonemeRules.cs       # コアG2Pルール (6フェーズ)
│   ├── FrenchOrthography.cs            # 正書法ヘルパー (母音判定、ダイグラフ判定等)
│   ├── NasalVowelizer.cs              # 鼻母音化ロジック (独立static class、GraphemeToPhonemeRulesから呼び出し)
│   ├── FrenchSyllabifier.cs            # 音素ベース音節分割
│   ├── AllophoneProcessor.cs           # 異音規則
│   └── LiaisonProcessor.cs            # リエゾン + enchaînement処理 (Phase2)
├── Normalization/
│   ├── FrenchNormalizer.cs             # テキスト正規化ファサード
│   └── NumberToWords.cs               # フランス語数詞変換 (20進法)
├── Data/
│   ├── FrenchExceptionDictionary.cs    # 例外辞書ルックアップ
│   ├── french_exceptions.master.tsv    # 例外辞書TSV (EmbeddedResource)
│   └── h_aspire.txt                   # h aspiré語リスト (EmbeddedResource)
├── Conversion/
│   ├── IpaConverter.cs                 # IPA文字列変換
│   └── XSampaConverter.cs             # X-SAMPA文字列変換
├── package.json                        # UPM (com.dotnetg2p.french)
└── DotNetG2P.French.asmdef            # Unity Assembly Definition
```

---

## 5. 各コンポーネントの設計

### 5.1 FrenchG2PEngine

スペイン語の `SpanishG2PEngine` と同一パターンで実装する。

```csharp
namespace DotNetG2P.French
{
    public sealed class FrenchG2PEngine : IDisposable
    {
        private readonly FrenchG2POptions _options;
        private int _disposed;  // Interlocked.CompareExchange + Volatile.Read パターン

        public FrenchG2PEngine();
        public FrenchG2PEngine(FrenchG2POptions options);

        // --- 基本API ---
        public string ToPhonemes(string text);
        public string ToIPA(string text);
        public string ToIPAWithoutStress(string text);  // ストレスマークなし
        public string ToXSampa(string text);
        public string ToXSampaWithoutStress(string text);
        public IReadOnlyList<FrenchPhoneme> ToPhonemeList(string text);
        public IReadOnlyList<FrenchSyllable> ToSyllables(string text);

        // --- バッチAPI ---
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts);
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts);
        public IReadOnlyList<IReadOnlyList<FrenchPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts);

        public void Dispose();
    }
}
```

**注意**: フランス語は語レベルストレスを持たないため `ToIPAWithoutStress` は `ToIPA` と同一出力になるが、API一貫性のために提供する。将来的に句ストレス対応を追加する場合のAPIフックとしても機能する。

### 5.2 FrenchG2POptions

```csharp
namespace DotNetG2P.French
{
    public sealed class FrenchG2POptions
    {
        public static readonly FrenchG2POptions Default = new FrenchG2POptions();

        public FrenchDialect Dialect { get; }                  // デフォルト: Metropolitan
        public bool IncludeStress { get; }                     // デフォルト: false（フランス語は語レベルストレスなし。API一貫性のために保持。将来の句ストレス対応用フック）
        public bool EnableAllophones { get; }                  // デフォルト: false
        public bool EnableTextNormalization { get; }           // デフォルト: true
        public bool EnableExceptionDictionary { get; }         // デフォルト: true
        public bool EnableLiaison { get; }                     // デフォルト: false (Phase2)
        public FrenchAllophoneFeatures AllophoneFeatures { get; }
        public string Separator { get; }                       // デフォルト: " "

        public FrenchG2POptions(
            FrenchDialect dialect = FrenchDialect.Metropolitan,
            bool includeStress = false,
            bool enableAllophones = false,
            bool enableTextNormalization = true,
            bool enableExceptionDictionary = true,
            bool enableLiaison = false,
            string separator = " ",
            FrenchAllophoneFeatures allophoneFeatures = FrenchAllophoneFeatures.Default);
    }
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
        public static IReadOnlyList<int> Syllabify(FrenchPhoneme[] phonemes);
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

### 5.6 FrenchNormalizer

`SpanishNormalizer` のサブモジュール分割パターンを採用する。

```csharp
namespace DotNetG2P.French.Normalization
{
    internal static class FrenchNormalizer
    {
        public static string Normalize(string text);
        public static IReadOnlyList<string> Tokenize(string text);
    }
}
```

#### 正規化規則

| カテゴリ | 規則 | 例 |
|---------|------|-----|
| Unicode正規化 | NFC + 小文字化 | 常時 |
| 数字 | 20進法（vigesimal） | 70=soixante-dix, 80=quatre-vingts, 90=quatre-vingt-dix |
| 小数点 | カンマが小数点 | 3,14 → trois virgule quatorze |
| 桁区切り | スペースが桁区切り | 1 000 → mille |
| 通貨 | euro(s), centime(s) | 5,50€ → cinq euros cinquante centimes |
| 時刻 | heure(s) | 14h30 → quatorze heures trente |
| 日付 | 日-月-年 | 25/12/2024 → le vingt-cinq décembre deux mille vingt-quatre |
| 単位 | km, kg, m, °C 等 | 100km → cent kilomètres |
| 略語 | M., Mme, Dr, etc. | M. → monsieur |
| 記号 | @, &, %, # 等 | & → et |
| アポストロフ | 保持（エリジオン） | l'homme → l'homme（アポストロフで分割しない） |

#### NumberToWords: フランス語20進法

```
70: soixante-dix (60+10)
71: soixante et onze (60+11)
72-79: soixante-douze ... soixante-dix-neuf
80: quatre-vingts (4×20, 末尾s)
81: quatre-vingt-un (4×20+1, sなし)
82-89: quatre-vingt-deux ... quatre-vingt-neuf
90: quatre-vingt-dix (4×20+10)
91: quatre-vingt-onze
92-99: quatre-vingt-douze ... quatre-vingt-dix-neuf
```

### 5.7 AllophoneProcessor

```csharp
namespace DotNetG2P.French
{
    [Flags]
    public enum FrenchAllophoneFeatures : byte
    {
        None = 0,

        /// <summary>/ʁ/→[χ] 無声阻害音前後で無声化。</summary>
        RDevoicing = 1 << 0,

        /// <summary>阻害音の有声性同化（逆行同化）。</summary>
        ObstruentVoicingAssimilation = 1 << 1,

        /// <summary>有声摩擦音前の母音長母音化。</summary>
        VowelLengthening = 1 << 2,

        /// <summary>/l/ の軟口蓋化（コーダ位置）。</summary>
        LVelarization = 1 << 3,

        /// <summary>語末阻害音の無声化（標準フランス語では非体系的。ドイツ語やロシア語と異なり、
        /// フランス語の語末有声阻害音は有声のまま発音される: robe /ʁɔb/, rouge /ʁuʒ/。
        /// デフォルト無効。特殊な音声コンテキストでのみ使用）。</summary>
        FinalDevoicing = 1 << 4,

        Obligatory = RDevoicing | ObstruentVoicingAssimilation,
        Default = Obligatory,
        All = Default | VowelLengthening | LVelarization | FinalDevoicing,
    }
}
```

#### 必須異音規則

1. **/ʁ/ 無声化**: 無声阻害音の前後で /ʁ/ → [χ]
   - 例: `arbre` /aʁbʁ/ → [aʁbχ]（語末 /ʁ/ が無声化）, `prendre` /pʁɑ̃dʁ/ → [pχɑ̃dʁ]（語頭 /pʁ/ の /ʁ/ が無声阻害音 /p/ 後で無声化、語末 /dʁ/ の /ʁ/ は有声阻害音 /d/ 後のため無声化しない）

2. **阻害音有声性同化（逆行同化）**: 阻害音クラスタ内で最後の阻害音の有声性に統一
   - 例: `absent` /absɑ̃/ → [apsɑ̃], `anecdote` /anɛkdɔt/ → [anɛɡdɔt]

#### オプション異音規則

3. **母音長母音化**: 有声摩擦音 /v, z, ʒ, ʁ/ の前の母音が長母音化
4. **/l/ 軟口蓋化**: コーダ位置の /l/ が暗い l に
5. **語末阻害音無声化**: 語末の阻害音が無声化

### 5.8 ExceptionDictionary

スペイン語 `SpanishExceptionDictionary` と同一設計。TSV形式の埋め込みリソース。

#### TSVフォーマット

```
# surface	dialect	category	stress_index	phonemes	source	note
camping	*	loanword	0	k ɑ̃|p i ŋ	manual	英語借用語
```

| カラム | 説明 |
|--------|------|
| surface | 表層形（小文字） |
| dialect | `*`（全方言）/ `metropolitan` / `conservative` |
| category | `loanword` / `academic` / `homograph` / `irregular` |
| stress_index | 強勢音節インデックス（フランス語では通常 -1） |
| phonemes | 音節区切り `\|` + スペース区切り音素 |
| source | 出典 |
| note | 備考 |

#### 例外辞書の想定規模

| カテゴリ | 語数 | 例 |
|---------|------|-----|
| 外来語 | 200-400 | camping, parking, football, pizza |
| 学術語（語末子音例外） | 150-300 | bus, index, anus, atlas |
| 同綴異音語 | ~70 | -tions (動詞/名詞), -ent (動詞/形容詞) |
| その他不規則 | 50-100 | monsieur, femme, oignon |
| **合計** | **500-1000** | |

### 5.9 IpaConverter / XSampaConverter

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

## 6. Multilingual統合設計

### 6.1 Language enum 拡張

```csharp
public enum Language : byte
{
    Japanese = 0,
    English = 1,
    Chinese = 2,
    Spanish = 3,
    French = 4,    // 追加
}
```

### 6.2 DefaultLatinLanguage の拡張

現在 `DefaultLatinLanguage` は `English` / `Spanish` のみ許可。フランス語追加で3言語のラテン文字振り分けが必要。

```csharp
public MultilingualG2POptions(
    ...
    Language defaultLatinLanguage = Language.English)  // English, Spanish, French を許可
{
    if (defaultLatinLanguage != Language.English
        && defaultLatinLanguage != Language.Spanish
        && defaultLatinLanguage != Language.French)
        throw new ArgumentOutOfRangeException(...);
}
```

### 6.3 TextSegmenter のフランス語対応

#### フランス語判定シグナル

```csharp
private static readonly string[] s_frenchWordSignals =
{
    "bonjour", "merci", "salut", "bonsoir", "comment", "pourquoi",
    "parce", "aussi", "beaucoup", "toujours", "jamais", "monsieur",
    "madame", "mademoiselle", "oui", "avec", "dans", "pour",
    "chez", "entre", "sans", "depuis", "voici", "voila"
};

private static readonly string[] s_frenchSuffixSignals =
{
    "ment", "tion", "sion", "ance", "ence", "eux", "euse",
    "eur", "euse", "oir", "oire", "ais", "aise", "ique"
};
```

#### ラテン文字3言語振り分けアルゴリズム

1. アクセント文字で判定: `ñ` → Spanish、`ç`/`ù`/`œ`/`æ` → French（ただし `ç` はトルコ語等にもあるため複合判定）
2. 高頻度語リストマッチ
3. 接尾辞パターンマッチ
4. フォールバック: `DefaultLatinLanguage`

### 6.4 MultilingualG2PEngine の拡張

```csharp
public sealed class MultilingualG2PEngine : IDisposable
{
    private readonly FrenchG2PEngine _frenchEngine;  // 追加

    // ConvertSegment 拡張
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

### 6.5 MultilingualG2POptions の拡張

```csharp
public sealed class MultilingualG2POptions
{
    public FrenchG2POptions? FrenchOptions { get; }  // 追加
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
| Allophone flags | `[Flags] SpanishAllophoneFeatures : byte` | `[Flags] FrenchAllophoneFeatures : byte` |
| Exception dict | TSV埋め込み、`TryLookup(word, dialect, out pron)` | 同一 |
| G2P rules | `internal static class GraphemeToPhonemeRules` | 同一 |
| Syllabifier | `internal static class SpanishSyllabifier` | `internal static class FrenchSyllabifier`（音素ベース） |
| StressAssigner | `internal static class StressAssigner` | **不要** |
| Normalizer | `internal static class SpanishNormalizer` + `NumberToWords` | 同一構造 |
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

## 付録B: 評価パイプライン設計

`tools/DotNetG2P.SpanishEval` をコピー改変して `tools/DotNetG2P.FrenchEval` を作成する。

```
tools/
├── DotNetG2P.FrenchEval/              # フランス語PER/WER評価ツール
│   ├── DotNetG2P.FrenchEval.csproj
│   └── Program.cs
├── refresh_french_eval_data.ps1       # 評価データ取得スクリプト
├── run_french_full_evaluation.ps1     # 全量評価実行スクリプト
└── generate_french_exceptions.ps1     # 例外辞書生成スクリプト
```

#### 評価メトリクス

- **PER (Phone Error Rate)**: 音素レベル編集距離 / 参照音素数
- **WER (Word Error Rate)**: 完全一致しなかった語数 / 全語数
- **カテゴリ別集計**: 外来語 / 鼻母音 / 黙字 / 不規則語 等のサブセット分析
