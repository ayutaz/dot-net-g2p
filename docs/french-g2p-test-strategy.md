# フランス語G2P テスト戦略

## 1. テスト概要

### 目標

| 指標 | 目標値 |
|------|--------|
| テスト総数 | 400-430件（F1-F4合計）。現時点: **366件**（F1: 218件 + F2: 148件） |
| コードカバレッジ | 行カバレッジ90%以上（publicメソッド100%） |
| PER（ipa-dict fr_FR） | F1: 8-12%, F2: 3-6%, F3: 3-6%（確定値） |
| PER（WikiPron fra_latn_broad_filtered） | 交差検証用（閾値はipa-dict結果に基づき設定） |

注: PER閾値はロードマップ（french-g2p-roadmap.md）と統一済み。フランス語はスペイン語（PER 1.69%）より正書法が複雑なため、初期閾値はより緩く設定する。espeak-ngのフランス語G2PのPER実績（約5-8%）を参考値として採用している。

### テストフレームワーク

- **xUnit 2.5.3** (net8.0)
- `[Trait("Category", "Performance")]` でパフォーマンステストを分離
- `PerformanceThresholds` ヘルパーによるCI/ローカル閾値切替
- `IDisposable` でエンジンの確実な破棄

### テスト命名規則

既存プロジェクトに準拠:
```
メソッド名_条件_期待結果
```
例: `ToIPA_SimpleVowelWord_ReturnsCorrectIPA`, `Normalize_CurrencyEuro_ExpandsCorrectly`

---

## 2. マイルストーン別テスト計画

### F1: コアG2P MVP（目標: 120-150件 → 実績: **218件**）

**ステータス: 完了** — 5テストファイル、218テストケース全通過。

| テストファイル | 内容 | 計画 | 実績 |
|---------------|------|------|------|
| `FrenchG2PEngineTests.cs` | エンジン統合テスト（ToIPA, ToPhonemes, ToPhonemeList, バッチAPI） | 25-30件 | **32件** |
| `GraphemeToPhonemeRulesTests.cs` | 書記素→音素規則の個別検証（h aspire/h muet、エリジョン含む） | 50-60件 | **94件** |
| `FrenchSyllabifierTests.cs` | 音節分割テスト（旧StressAssignerTests統合分含む） | 25-30件 | **38件** |
| `FrenchIpaTests.cs` | IPA変換テスト（音素enum↔IPA文字列） | 15-20件 | **23件** |
| `FrenchPhonemeTests.cs` | 音素モデルテスト（enum値、struct equality等） | 10-15件 | **31件** |

注: フランス語は語レベルの独立したストレスを持たないため、`StressAssignerTests.cs` は設けない（音韻論レビュー指摘 M17）。語末音節への韻律的強勢は句レベルの現象であり、語レベルG2Pのスコープ外。音節構造の検証は `FrenchSyllabifierTests.cs` に統合する。

#### G2Pルール単体テストの分類

`GraphemeToPhonemeRulesTests.cs` 内で以下のカテゴリを `[Trait]` で分類:

**母音規則 (~12件)**
```csharp
[Theory]
[InlineData("ami", "ami")]        // a → /a/
[InlineData("île", "il")]         // î → /i/
[InlineData("été", "ete")]        // é → /e/
[InlineData("bête", "bɛt")]       // ê → /ɛ/
[InlineData("ou", "u")]           // ou → /u/
[InlineData("lune", "lyn")]       // u → /y/
[InlineData("feu", "fø")]         // eu（開音節）→ /ø/
[InlineData("seul", "sœl")]       // eu（閉音節）→ /œ/
```

**鼻母音規則 (~10件)**
```csharp
[Theory]
[InlineData("banc", "bɑ̃")]       // an → /ɑ̃/
[InlineData("vin", "vɛ̃")]        // in → /ɛ̃/
[InlineData("bon", "bɔ̃")]        // on → /ɔ̃/
[InlineData("brun", "bʁœ̃")]      // un → /œ̃/
[InlineData("bonne", "bɔn")]     // on+n+母音 → 非鼻母音化
[InlineData("innocent", "inɔsɑ̃")] // inn → /in/（二重子音で非鼻母音化）
```

**子音規則 (~10件)**
```csharp
[Theory]
[InlineData("chat", "ʃa")]       // ch → /ʃ/
[InlineData("gn", "ɲ")]          // gn → /ɲ/
[InlineData("roi", "ʁwa")]       // r → /ʁ/
[InlineData("cent", "sɑ̃")]       // c(e,i) → /s/
[InlineData("car", "kaʁ")]       // c(a,o,u) → /k/
[InlineData("geste", "ʒɛst")]    // g(e,i) → /ʒ/
[InlineData("garçon", "ɡaʁsɔ̃")]  // ç → /s/
```

**ダイグラフ・トリグラフ規則 (~8件)**
```csharp
[Theory]
[InlineData("eau", "o")]         // eau → /o/
[InlineData("oi", "wa")]         // oi → /wa/
[InlineData("ai", "ɛ")]          // ai → /ɛ/
[InlineData("au", "o")]          // au → /o/
[InlineData("aille", "aj")]      // aille → /aj/
[InlineData("ouille", "uj")]     // ouille → /uj/
```

**-tion / -sion / -ill- 系パターン (~8件)**

フランス語で最も頻出するパターンの一つであり、PERへの影響が大きい:

```csharp
[Theory]
// -tion / -sion 接尾辞
[InlineData("nation", "nasjɔ̃")]       // -tion → /sjɔ̃/
[InlineData("question", "kɛstjɔ̃")]    // -tion（s後）
[InlineData("passion", "pasjɔ̃")]      // -ssion → /sjɔ̃/
[InlineData("vision", "vizjɔ̃")]       // -sion → /zjɔ̃/

// -ill- 系パターン
[InlineData("fille", "fij")]          // -ille → /ij/
[InlineData("famille", "famij")]      // -ille → /ij/
[InlineData("travail", "tʁavaj")]     // -ail → /aj/
[InlineData("soleil", "sɔlɛj")]      // -eil → /ɛj/
```

**h aspire / h muet 区別規則 (~8件)**

フランス語のh aspire（有気h）とh muet（無音h）の区別は、エリジョンおよびリエゾンの発動に直接関わる最重要の語彙的特性である:

```csharp
[Theory]
// h muet（無音h）: hは発音されず、エリジョン・リエゾンが発生
[InlineData("homme", "ɔm")]          // h muet: h黙字
[InlineData("heure", "œʁ")]          // h muet: h黙字
[InlineData("habiter", "abite")]     // h muet: h黙字
[InlineData("honneur", "ɔnœʁ")]     // h muet: h黙字

// h aspire（有気h）: hは発音されないが、エリジョン・リエゾンをブロック
[InlineData("haricot", "aʁiko")]     // h aspire: h黙字（リエゾン/エリジョン不可）
[InlineData("hibou", "ibu")]         // h aspire: h黙字
[InlineData("honte", "ɔ̃t")]          // h aspire: h黙字
[InlineData("héros", "eʁo")]        // h aspire: h黙字（注: héroïne はh muet）
```

注: h aspire / h muet の区別は発音自体には影響しないが、エリジョン・リエゾンの適用可否を決定する。h aspire語彙リスト（固定リストまたは例外辞書）の管理テストも含む。

**エリジョン（elision）規則 (~8件)**

エリジョンはリエゾンとは異なり、文語・口語を問わず必須の音韻規則である:

```csharp
[Theory]
// 基本エリジョン（母音前の冠詞・代名詞の母音省略）
[InlineData("l'ami", "lami")]         // le/la + 母音語 → l'
[InlineData("j'ai", "ʒe")]           // je + ai → j'ai
[InlineData("c'est", "sɛ")]          // ce + est → c'est
[InlineData("l'homme", "lɔm")]       // le + homme (h muet) → l'homme
[InlineData("d'accord", "dakɔʁ")]    // de + accord → d'accord
[InlineData("n'est-ce pas", "nɛspɑ")] // ne + est-ce pas

// h aspire でのエリジョン非適用
[InlineData("le haricot", "ləaʁiko")] // le + haricot (h aspire) → エリジョンなし
[InlineData("le héros", "ləeʁo")]    // le + héros (h aspire) → エリジョンなし
```

**y（半母音）の多様な振る舞い (~5件)**

```csharp
[Theory]
[InlineData("yeux", "jø")]           // 語頭y → /j/
[InlineData("pays", "pei")]          // 語中y → 母音分割
[InlineData("voyage", "vwajaʒ")]     // 語中y → /j/
[InlineData("royal", "ʁwajal")]      // oy → /waj/
[InlineData("payer", "peje")]        // ay → /ej/（二重母音的扱い）
```

**黙字規則 (~12件)**
```csharp
[Theory]
// 語末子音黙字
[InlineData("petit", "pəti")]    // 語末t黙字
[InlineData("grands", "ɡʁɑ̃")]    // 語末s黙字
[InlineData("parlez", "paʁle")]  // 語末ez → /e/

// 語末-ent 黙字判定（動詞 vs 名詞/形容詞）
[InlineData("chantent", "ʃɑ̃t")]  // 語末ent → 黙字（動詞三人称複数活用）
[InlineData("accent", "aksɑ̃")]   // 語末ent → /ɑ̃/（名詞）
[InlineData("patient", "pasjɑ̃")] // -ent → /ɑ̃/（形容詞）
[InlineData("orient", "ɔʁjɑ̃")]   // -ent → /ɑ̃/（名詞）
[InlineData("content", "kɔ̃tɑ̃")]  // -ent → /ɑ̃/（形容詞、デフォルト鼻母音）
[InlineData("souvent", "suvɑ̃")]  // -ent → /ɑ̃/（副詞）
[InlineData("serpent", "sɛʁpɑ̃")] // -ent → /ɑ̃/（名詞）
[InlineData("parent", "paʁɑ̃")]   // -ent → /ɑ̃/（名詞）
[InlineData("lent", "lɑ̃")]       // -ent → /ɑ̃/（形容詞）
```

注: `-ent` の黙字/鼻母音判定は品詞に依存するフランス語G2Pの最難関テーマの一つ。デフォルト動作は鼻母音（/ɑ̃/）とし、動詞三人称複数活用形は例外辞書で対応する方針。

**位置の法則 (~5件)**
- 開音節/閉音節による母音の開閉の区別
- e/ɛ, o/ɔ, ø/œ の切替

**シュワー規則 (~10件以上)**
```csharp
[Theory]
// シュワー保持
[InlineData("le", "lə")]           // 単音節機能語
[InlineData("de", "də")]           // 単音節機能語
[InlineData("petit", "pəti")]     // 語頭子音+e+子音
[InlineData("me", "mə")]           // 単音節代名詞
[InlineData("que", "kə")]          // 単音節接続詞

// 三子音規則（loi des trois consonnes）によるシュワー保持
[InlineData("gouvernement", "ɡuvɛʁnəmɑ̃")]  // 三子音回避のためシュワー保持
[InlineData("bref rappel", "bʁɛfʁapɛl")]     // 三子音規則の適用確認

// 機能語シュワー
[InlineData("je ne sais pas", "ʒənəsɛpɑ")]  // 機能語連続でのシュワー

// シュワー脱落（オプション）
[InlineData("samedi", "samdi")]    // 口語的脱落
[InlineData("avenue", "avny")]     // 語中シュワー脱落
```

### F2: 精度向上・異音・正規化（目標: 110-140件 → 実績: **148件**）

**ステータス: 完了** — 4テストファイル、148テストケース全通過。

| テストファイル | 内容 | 計画 | 実績 |
|---------------|------|------|------|
| `FrenchNumberToWordsTests.cs` | 数値→フランス語文字列変換の単体テスト | 20-25件 | **55件** |
| `FrenchNormalizerTests.cs` | テキスト正規化テスト（略語・日付・時刻・通貨・単位・記号等） | 35-45件 | **51件** |
| `AllophoneProcessorTests.cs` | 異音規則テスト（R無声化・有声性同化） | 15-20件 | **18件** |
| `FrenchExceptionDictionaryTests.cs` | 例外辞書テスト（外来語・不規則語・動詞3複・方言） | 20-25件 | **24件** |
| `LiaisonTests.cs` | リエゾン規則テスト（オプション） | 15-20件 | 未実装（F3以降で検討） |

#### 数値変換テスト（`FrenchNumberToWordsTests.cs`）— 実績55件

フランス語の数詞は vigesimal（20進法）体系を持ち、特に複雑。独立テストファイルとして詳細に検証する。

**実装済みテストカテゴリ（55テストケース）:**

| カテゴリ | メソッド | テストケース数 | 内容 |
|---------|---------|-------------|------|
| 基本数詞 (0-19) | `Convert_BasicNumbers_ReturnsCorrect` | 6件 | 0, 1, 5, 11, 16, 19 |
| 20台 | `Convert_Twenty_ReturnsVingt` | 1件 | 20 |
| et挿入 | `Convert_EtInsertion_ReturnsCorrect` | 5件 | 21, 31, 41, 51, 61 |
| 通常の十の位 | `Convert_RegularTens_ReturnsCorrect` | 5件 | 22, 35, 48, 59, 63 |
| vigesimal 70系列 | `Convert_Seventies_ReturnsVigesimal` | 5件 | 70, 71, 72, 75, 79 |
| vigesimal 80系列 | `Convert_Eighties_ReturnsVigesimal` | 4件 | 80(末尾s), 81(sなし), 85, 89 |
| vigesimal 90系列 | `Convert_Nineties_ReturnsVigesimal` | 4件 | 90, 91, 95, 99 |
| 百の位 | `Convert_Hundreds_ReturnsCorrect` | 6件 | 100, 101, 200(末尾s), 201(sなし), 300, 999 |
| 千の位 | `Convert_Thousands_ReturnsCorrect` | 4件 | 1000, 1001, 2000, 2025 |
| million/milliard | `Convert_LargeNumbers_ReturnsCorrect` | 4件 | 100万, 200万, 10億, 20億 |
| 負の数 | `Convert_Negative_ReturnsMoins` | 1件 | -5 → "moins cinq" |
| 序数詞 | `ConvertOrdinal_ReturnsCorrect` | 6件 | 1er, 1ère, 2e, 3ème, 5e, 9e(neuvième) |
| 桁読み | `ConvertDigits_ReturnsIndividualDigits` | 1件 | "123" → "un deux trois" |
| 文字列版 | `Convert_String_ReturnsCorrect` | 3件 | 数値文字列, 非数値, 空文字列 |

#### 正規化テストの分類（`FrenchNormalizerTests.cs`）— 実績51件

スペイン語 `SpanishNormalizerTests.cs` のパターンに準拠し実装済み。

**実装済みテストカテゴリ（51テストケース）:**

| カテゴリ | メソッド | テストケース数 | 内容 |
|---------|---------|-------------|------|
| 基本動作 | `Normalize_Null/Empty/PlainText` | 3件 | null→空, 空→空, 小文字化 |
| 略語展開 | `ExpandAbbreviations_ReturnsExpanded` | 5件 | M., Mme, Dr, etc., p. ex. |
| 日付展開 | `ExpandDates_ReturnsExpanded` + `InvalidDate` | 4件 | DD/MM/YYYY形式, 無効日付フォールバック |
| 時刻展開 | `ExpandTimes_ReturnsExpanded` | 5件 | Nh/NhMM, minuit, midi |
| 通貨展開 | `ExpandCurrencies_ReturnsExpanded` | 5件 | €(単複,小数), $(単複) |
| パーセンテージ | `ExpandPercentages_ReturnsExpanded` | 3件 | 整数%, 小数% |
| 単位展開 | `ExpandUnits_ReturnsExpanded` | 8件 | km, kg, m, cm, mm, L, °C + 単数形 |
| 小数展開 | `ExpandDecimals_ReturnsExpanded` | 2件 | "virgule" + 桁読み |
| 数字展開 | `ExpandNumbers_ReturnsExpanded` | 3件 | 整数→文字列 |
| 記号展開 | `ExpandSymbols_ReturnsExpanded` | 6件 | &, @, §, #, +, = |
| 空白正規化 | `Normalize_MultipleSpaces_Collapsed` | 1件 | 連続空白圧縮 |
| 複合テスト | `Normalize_MixedContent/DateTimeCombo` | 2件 | 略語+数字+記号混在, 日付+時刻複合 |
| Tokenize | `Tokenize_Empty/Elision/CompoundWord` | 3件 | 空→空, エリジョン保持, ハイフン語保持 |
| n°展開 | `ExpandAbbreviations_NumeroSign_Expanded` | 1件 | n° → numéro |

#### 異音テスト（`AllophoneProcessorTests.cs`）— 実績18件

**実装済みテストカテゴリ（18テストケース）:**

| カテゴリ | テストケース数 | 内容 |
|---------|-------------|------|
| R無声化 (RDevoicing) | 6件 | R+無声阻害音→Rh, 無声阻害音+R→Rh, 母音間R維持, 語末R維持, 有声阻害音前R維持, 鼻音前R維持 |
| 有声性同化 (ObstruentVoicingAssimilation) | 5件 | 有声→無声(b+s→p+s), 無声→有声(k+d→g+d), 同一voicing不変, 非阻害音で分断, 3連阻害音カスケード逆行同化 |
| フラグ制御 | 4件 | None→変化なし, RDevoicingのみ, Assimilationのみ, Default(両方) |
| 空入力 | 1件 | 空配列→空返却 |
| 統合（メタデータ保持） | 2件 | 音節オフセット+ストレス保持, IsSyllableNucleus保持 |

#### 例外辞書テスト（`FrenchExceptionDictionaryTests.cs`）— 実績24件

**実装済みテストカテゴリ（24テストケース）:**

| カテゴリ | テストケース数 | 内容 |
|---------|-------------|------|
| ロード検証 | 3件 | football→True, 未知語→False, null→False |
| 外来語 (英語/イタリア語/日本語) | 3件 | weekend(5音素), pizza(5音素), sushi(4音素) — 各音素を個別Assert |
| 不規則語 | 4件 | monsieur(5音素,ə), femme(3音素,a), oignon(3音素,ɲ), fils(3音素,語末s発音) |
| 動詞3人称複数 (-ent黙字) | 3件 | parlent(4音素,-ent黙字), chantent(3音素), sont(2音素,不規則) |
| 学術語・特殊黙字 | 4件 | bus(語末s発音), album(語末m発音), fusil(語末l黙字), tabac(語末c黙字) |
| 方言 | 2件 | football→Metropolitan/Conservative 両方マッチ（ワイルドカード方言） |
| 音節核 | 1件 | pizza → 音節核フラグ(IsSyllableNucleus)の正確性検証 |
| ストレス | 1件 | monsieur → StressedSyllableIndex = -1（フランス語は語レベルストレスなし） |
| 同綴異音語 | 2件 | est(1音素,ɛ), content(4音素,k+ɔ̃+t+ɑ̃) |
| エントリ数検証 | 1件 | 辞書の基本16語全ルックアップ成功 |

### F3: X-SAMPA・精度評価（目標: 100-120件）

| テストファイル | 内容 | 件数目安 |
|---------------|------|---------|
| `FrenchXSampaTests.cs` | X-SAMPA変換テスト | 20-25件 |
| `FrenchEdgeCaseTests.cs` | エッジケーステスト | 30-35件 |
| `FrenchPerformanceTests.cs` | パフォーマンステスト | 10-15件 |
| `FrenchAccuracyTests.cs` | 精度・回帰テスト（キュレーション済みコーパス） | 20-25件 |
| `FrenchDatasetEvaluationTests.cs` | 外部TSVコーパスPER閾値テスト | 10-15件 |
| `FrenchAllophoneEvaluationTests.cs` | 異音プロファイル別PER評価テスト | 5-10件 |

#### X-SAMPA変換テスト（`FrenchXSampaTests.cs`）

IPA→X-SAMPA変換の網羅的検証:

```csharp
// 基本変換
[InlineData("bonjour", "bO~ZuR")]
[InlineData("français", "fRA~sE")]

// 特殊記号
// ɑ̃ → A~, ɛ̃ → E~, ɔ̃ → O~, œ̃ → 9~
// ʁ → R, ʃ → S, ʒ → Z, ɲ → J
// ø → 2, œ → 9, ə → @
```

#### 外部コーパスPER評価テスト（`FrenchDatasetEvaluationTests.cs`）

スペイン語 `SpanishDatasetEvaluationTests.cs` パターンに準拠。`FrenchAccuracyTests.cs`（キュレーション済みコーパスの正確性回帰テスト）とは役割を分離する:

```csharp
[Trait("Category", "Accuracy")]
public class FrenchDatasetEvaluationTests : IDisposable
{
    // ipa-dict fr_FR サンプル（500語）PER閾値テスト
    [Fact]
    public void IpaDictSample_Base_PER_BelowThreshold()
    {
        // TSVからサンプルロード → PER計算 → 閾値チェック
    }

    // ipa-dict fr_FR フル（全量）PER閾値テスト
    [Fact]
    public void IpaDictFull_Base_PER_BelowThreshold()
    {
        // 全量TSVロード → PER計算 → 閾値チェック
    }

    // WikiPron PER閾値テスト
    [Fact]
    public void WikiPronSample_Base_PER_BelowThreshold()
    {
        // WikiPronサンプルからPER計算
    }

    [Fact]
    public void WikiPronFull_Base_PER_BelowThreshold()
    {
        // WikiPronフルからPER計算
    }
}
```

#### 異音プロファイル評価テスト（`FrenchAllophoneEvaluationTests.cs`）

スペイン語 `SpanishAllophoneEvaluationTests.cs` パターンに準拠。TSVリファレンスを使い、異音プロファイル（base/allophones/no_exceptions）ごとの正確性を検証:

```csharp
[Trait("Category", "Accuracy")]
public class FrenchAllophoneEvaluationTests : IDisposable
{
    // base プロファイル（ルール+例外辞書）
    [Fact]
    public void IpaDictSample_Base_PER_BelowThreshold()

    // allophones プロファイル（異音規則有効）
    [Fact]
    public void IpaDictSample_Allophones_PER_BelowThreshold()

    // no_exceptions プロファイル（ルールのみ）
    [Fact]
    public void IpaDictSample_NoExceptions_PER_BelowThreshold()
}
```

### F4: Multilingual統合（目標: 40-50件）

| テストファイル | 内容 | 件数目安 |
|---------------|------|---------|
| `MultilingualFrenchTests.cs` | フランス語Multilingual統合テスト | 25-30件 |
| `MultilingualMixedLanguageTests.cs` (追記) | 5言語混在テスト | 15-20件 |

#### Multilingual統合テスト（`MultilingualFrenchTests.cs`）

スペイン語 `MultilingualSpanishTests.cs` パターンに準拠:

```csharp
[Collection(MultilingualSharedCollection.Name)]
public class MultilingualFrenchTests
{
    // Language enum値確認
    [Fact] Language_French_値は4()

    // LanguageDetector確認
    [Fact] LanguageDetector_ToLanguage_LatinにFrench既定を渡すとFrench()

    // TextSegmenter確認
    [Fact] Segment_DefaultLatinFrench_ASCIIフランス語をFrenchに分類()
    [Fact] Segment_DefaultEnglish_アクセント付きフランス語はFrenchに分類()

    // MultilingualG2PEngine確認
    [SkippableFact] G2P_フランス語テキスト_IPA出力が非空()
    [SkippableFact] G2P_日仏混在テキスト_両言語セグメントを処理()
}
```

#### 5言語混在テスト

```csharp
// 日英中西仏混在テキスト
[SkippableFact]
public void G2P_日英中西仏混在_全セグメント処理成功()
{
    var text = "こんにちは hello 你好 hola bonjour";
    // 各セグメントが正しい言語に分類され、G2P変換される
}
```

---

## 3. 単体テストカテゴリ一覧

### 3.1 音素変換規則テスト

| カテゴリ | テスト対象 | 件数目安 |
|---------|----------|---------|
| 口腔母音 | a, e/ɛ, i, o/ɔ, u, y, ø/œ, ə | 12件 |
| 鼻母音 | ɑ̃, ɛ̃, ɔ̃, œ̃ + 非鼻母音化境界 | 10件 |
| 破裂子音 | p, b, t, d, k, ɡ | 6件 |
| 摩擦子音 | f, v, s, z, ʃ, ʒ, ʁ | 8件 |
| 鼻子音 | m, n, ɲ, ŋ | 5件 |
| 側面/接近 | l, j, w, ɥ | 5件 |
| ダイグラフ | ch, gn, ph, ou, oi, ai, au, eau | 10件 |
| トリグラフ | eau, ain, ein, oin, aille, eille | 8件 |
| -tion/-sion/-ill- | -tion, -sion, -ssion, -ille, -ail, -eil, -euil | 8件 |
| h aspire/h muet | h muet語彙, h aspire語彙, リスト管理 | 8件 |
| エリジョン | l', j', c', d', n', qu' + h aspire非適用 | 8件 |
| y 半母音 | 語頭y, 語中y, oy, ay パターン | 5件 |

### 3.2 鼻母音化テスト

鼻母音と非鼻母音の境界ケースは最も複雑な規則の一つ:

```csharp
// 鼻母音化される場合（母音+n/m+子音 or 語末）
[InlineData("banc", "bɑ̃")]
[InlineData("vin", "vɛ̃")]
[InlineData("temps", "tɑ̃")]

// 鼻母音化されない場合（母音+n/m+母音）
[InlineData("bonne", "bɔn")]
[InlineData("femme", "fam")]        // 例外辞書
[InlineData("innocent", "inɔsɑ̃")]  // inn → /in/

// 境界ケース
[InlineData("immense", "imɑ̃s")]    // imm → /im/
[InlineData("ennui", "ɑ̃nɥi")]      // 語頭enn → /ɑ̃n/
```

### 3.3 黙字テスト

CaReFuL規則（語末で通常発音されるのはC, R, F, Lのみ）+ 例外:

```csharp
// CaReFuL: 発音される語末子音
[InlineData("sac", "sak")]         // 語末c発音
[InlineData("mer", "mɛʁ")]        // 語末r発音
[InlineData("chef", "ʃɛf")]       // 語末f発音
[InlineData("mal", "mal")]         // 語末l発音

// 黙字語末子音
[InlineData("petit", "pəti")]     // 語末t黙字
[InlineData("grands", "ɡʁɑ̃")]     // 語末ds黙字
[InlineData("nez", "ne")]          // 語末z黙字

// CaReFuL例外（発音されない場合）
[InlineData("blanc", "blɑ̃")]      // nc → 鼻母音（c黙字）
[InlineData("tabac", "taba")]     // 例外: 語末c黙字

// 語末e黙字
[InlineData("parle", "paʁl")]     // 語末e黙字
[InlineData("vie", "vi")]          // 語末e黙字（ie）

// 語末-ent 黙字判定（拡充分、詳細はセクション2のG2Pルール黙字規則を参照）
[InlineData("patient", "pasjɑ̃")]   // 形容詞: -ent → /ɑ̃/
[InlineData("orient", "ɔʁjɑ̃")]     // 名詞: -ent → /ɑ̃/
[InlineData("content", "kɔ̃tɑ̃")]    // 形容詞: -ent → /ɑ̃/（デフォルト鼻母音）
[InlineData("souvent", "suvɑ̃")]    // 副詞: -ent → /ɑ̃/
[InlineData("serpent", "sɛʁpɑ̃")]   // 名詞: -ent → /ɑ̃/
```

### 3.4 位置の法則テスト

開音節/閉音節による母音の開閉:

```csharp
// 開音節 → 閉母音
[InlineData("été", "ete")]         // é 開音節 → /e/
[InlineData("beau", "bo")]         // eau 開音節 → /o/
[InlineData("feu", "fø")]          // eu 開音節 → /ø/

// 閉音節 → 開母音
[InlineData("fer", "fɛʁ")]        // e+r閉音節 → /ɛ/
[InlineData("port", "pɔʁ")]       // o+r閉音節 → /ɔ/
[InlineData("seul", "sœl")]       // eu+l閉音節 → /œ/
```

### 3.5 シュワーテスト

シュワーの保持/脱落はフランス語G2Pの大きな難題の一つ。三子音の法則（loi des trois consonnes）を含む10件以上のテストで検証する:

```csharp
// シュワー保持（基本）
[InlineData("le", "lə")]           // 単音節機能語
[InlineData("de", "də")]           // 単音節機能語
[InlineData("petit", "pəti")]     // 語頭子音+e+子音
[InlineData("me", "mə")]           // 単音節代名詞
[InlineData("que", "kə")]          // 単音節接続詞

// 三子音規則によるシュワー保持
[InlineData("gouvernement", "ɡuvɛʁnəmɑ̃")]  // 3子音連続回避

// 機能語連続
[InlineData("je ne sais pas", "ʒənəsɛpɑ")]

// シュワー脱落（オプション）
[InlineData("samedi", "samdi")]    // 口語的脱落
[InlineData("avenue", "avny")]     // 語中シュワー脱落
```

### 3.6 音節分割テスト

フランス語は語レベルの独立したストレスを持たないため、StressAssignerは設けず、音節分割テストを拡充する（25-30件）:

```csharp
[Theory]
// 基本音節分割
[InlineData("parler", new[] { "paʁ", "le" })]
[InlineData("construction", new[] { "kɔ̃s", "tʁyk", "sjɔ̃" })]
[InlineData("écrire", new[] { "e", "kʁiʁ" })]

// onset maximization
[InlineData("apprendre", new[] { "a", "pʁɑ̃dʁ" })]

// 母音連続
[InlineData("aéroport", new[] { "a", "e", "ʁɔ", "pɔʁ" })]

// 有効Onsetリスト検証
// 二子音: 閉鎖/摩擦 + /l, ʁ/（/tl, dl/ 除外）
[InlineData("plaire", new[] { "plɛʁ" })]         // /pl/ onset
[InlineData("crise", new[] { "kʁiz" })]           // /kʁ/ onset
[InlineData("fleur", new[] { "flœʁ" })]           // /fl/ onset

// 三子音クラスタ
[InlineData("strict", new[] { "stʁikt" })]        // /stʁ/ onset
[InlineData("expliquer", new[] { "ɛks", "pli", "ke" })]  // /spl/ 検証

// 鼻母音を含む音節境界
[InlineData("enfant", new[] { "ɑ̃", "fɑ̃" })]

// 語末音節（韻律的強勢位置の確認）
// 注: フランス語の韻律的強勢は句末音節に付与される。
// 語レベルでは最終音節を「潜在的強勢位置」として記録するのみ。
```

---

## 4. エッジケーステスト

`FrenchEdgeCaseTests.cs` に集約（30-35件）:

### 入力バリデーション

```csharp
[Fact] ToIPA_EmptyString_ReturnsEmpty()
[Fact] ToIPA_Null_ThrowsArgumentNullException()
[Fact] ToIPA_WhitespaceOnly_ReturnsEmpty()
[Fact] ToPhonemeList_Null_ThrowsArgumentNullException()
```

### 特殊文字・句読点

```csharp
[Fact] PunctuationOnly_ReturnsEmptyOutputs()
[Fact] MixedCaseAndAccents_NormalizeConsistently()
[Fact] SpecialUnicodeCharacters_DoNotCrash()
[Fact] FullWidthDigits_NormalizeToHalfWidth()
```

### Unicode正規化（NFC/NFD）

フランス語はアクセント記号が多い（e, e+combining acute, precomposed e-acute の違い）ため、NFC/NFD正規化の一貫性テストが重要:

```csharp
// NFC/NFD入力の正規化一貫性
[Fact] ToIPA_NFCInput_ReturnsCorrectIPA()       // é (U+00E9) precomposed
[Fact] ToIPA_NFDInput_ReturnsSameAsNFC()         // e + ´ (U+0065 U+0301) decomposed
[Fact] ToIPA_MixedNFCNFD_NormalizesConsistently() // 混在入力の一貫性
[Fact] ToIPA_AllAccentedChars_NFDEquivalence()   // é, è, ê, ë, à, â, î, ï, ô, ù, û, ü, ç 全文字
```

### バッチAPI

```csharp
[Fact] BatchApis_EmptyInput_ReturnEmptyCollection()
[Fact] BatchApis_Null_ThrowArgumentNullException()
[Fact] BatchAndSingleApis_ReturnSameResults()
[Fact] BatchApis_LargeInput_10000Words_CompletesWithoutError()  // 大量入力安定性テスト
```

### Dispose

```csharp
[Fact] UseAfterDispose_ThrowsObjectDisposedException()
[Fact] DoubleDispose_DoesNotThrow()
```

### オプション

```csharp
[Fact] EnableAllophonesFalse_SkipsAllophoneRules()
[Fact] EnableExceptionDictionaryFalse_UsesRulesOnly()
[Fact] DialectMetropolitan_MergesOeNasalToEiNasal()
[Fact] DialectConservative_KeepsFourNasalVowels()
```

### 長文入力

```csharp
[Fact] ToIPA_VeryLongInput_100Words_CompletesWithoutError()
```

---

## 5. 精度・回帰テスト

### 5.1 ipa-dict PERテスト

一次評価データセット: `ipa-dict fr_FR`

```csharp
[Trait("Category", "Accuracy")]
public class FrenchAccuracyTests : IDisposable
{
    // キュレーション済みコーパス（代表語）
    [Theory]
    [InlineData("bonjour", "bɔ̃ʒuʁ")]
    [InlineData("merci", "mɛʁsi")]
    [InlineData("france", "fʁɑ̃s")]
    [InlineData("oui", "wi")]
    [InlineData("croissant", "kʁwasɑ̃")]
    // ... 20-30語
    public void ToIPA_CuratedCorpus_MatchesExpected(string word, string expected)
    {
        Assert.Equal(expected, _engine.ToIPA(word));
    }
}
```

注: PER閾値チェックは `FrenchDatasetEvaluationTests.cs` に分離（セクション2 F3参照）。`FrenchAccuracyTests.cs` はキュレーション済みコーパスの正確性回帰テストに専念する。

### 5.2 WikiPron PERテスト

交差検証データセット: `WikiPron fra_latn_broad_filtered`

`FrenchDatasetEvaluationTests.cs` 内で WikiPron の PER 閾値テストを実施（セクション2 F3参照）。

### 5.3 PER目標と閾値

マイルストーン別のPER目標（ロードマップと統一済み）:

| マイルストーン | 目標PER（base） | 説明 |
|--------------|----------------|------|
| F1 | 8-12% | コアルール+基本例外辞書 |
| F2 | 3-6% | 例外辞書拡充+異音規則+正規化 |
| F3 | 3-6%（確定値） | 全量評価で閾値確定 |

データセット別の閾値:

| データセット | プロファイル | 初期閾値 | 改善目標 |
|-------------|------------|---------|---------|
| ipa_dict_fr_fr_sample | base | 8% | 4% |
| ipa_dict_fr_fr_sample | allophones | 8% | 4% |
| ipa_dict_fr_fr_sample | no_exceptions | 12% | 8% |
| ipa_dict_fr_fr_full | base | 12% | 6% |
| ipa_dict_fr_fr_full | allophones | 12% | 6% |
| ipa_dict_fr_fr_full | no_exceptions | 18% | 10% |
| wikipron_fra_latn_sample | base | 8% | 4% |
| wikipron_fra_latn_full | base | 12% | 6% |

閾値設定根拠:
- espeak-ngのフランス語G2P実績（約5-8% PER）を参考値として採用
- フランス語はスペイン語（PER 1.69%）より正書法が不規則であり、ルールベース+例外辞書で5%以下が現実的な改善目標
- ipa-dictフランス語データの特性（外来語比率約5%、不規則語比率約10%）を考慮
- sampleとfullで4ポイント以上の閾値差を設定（fullは多様なエッジケースを含むため）
- 改善目標は例外辞書の充実とルール改善に応じて段階的に引き下げる

### 5.4 方言バリエーション

現時点では Metropolitan French をデフォルト方言として評価する。Conservative方言（/a/-/ɑ/ 対立保持、/œ̃/ 独立保持）のサポートはオプションとして提供し、PER評価の際にもMetropolitan/Conservativeそれぞれのプロファイルで評価可能な設計とする。将来的に Canadian French / Belgian French 等への拡張を見据え、方言パラメータの拡張性を確保する。

---

## 6. パフォーマンステスト

`FrenchPerformanceTests.cs`（10-15件）:

スペイン語 `SpanishPerformanceTests.cs` のパターンに完全準拠:

```csharp
[Trait("Category", "Performance")]
public class FrenchPerformanceTests : IDisposable
{
    // 1. 初期化時間テスト
    [Fact]
    public void Constructor_RepeatedLoads_StayWithinThreshold()
    {
        // 5回の初期化、平均閾値: strict 100ms / relaxed 500ms
        // 例外辞書ロードがあるためスペイン語より若干緩め
    }

    // 2. 短文スループットテスト
    [Fact]
    public void ToIPA_ShortSentence_10000Times_CompletesQuickly()
    {
        // "bonjour le monde" を10000回変換
        // strict 2000ms / relaxed 8000ms
    }

    // 3. 長文スループットテスト
    [Fact]
    public void ToXSampa_LongSentence_2000Times_CompletesQuickly()
    {
        // 長文（20語程度）を2000回変換
        // strict 2500ms / relaxed 10000ms
    }

    // 4. バッチAPI効率テスト
    [Fact]
    public void BatchApi_FasterOrEqualToSequential()
    {
        // 100語バッチ vs 100回個別呼出し
    }

    // 5. 例外辞書ルックアップ速度テスト
    [Fact]
    public void ExceptionDictionary_Lookup_10000Times_CompletesQuickly()
    {
        // 辞書ルックアップの単体スループット
    }
}
```

---

## 7. テストデータ構成

### テストファイル一覧

```
tests/DotNetG2P.Tests/
├── FrenchG2P/
│   ├── FrenchG2PEngineTests.cs              # F1: エンジン統合テスト (32件) ✅
│   ├── GraphemeToPhonemeRulesTests.cs        # F1: G2Pルール単体テスト (94件) ✅
│   │                                        #     h aspire/h muet, エリジョン,
│   │                                        #     -tion/-sion/-ill-系, y半母音 含む
│   ├── FrenchSyllabifierTests.cs            # F1: 音節分割テスト (38件) ✅
│   │                                        #     旧StressAssigner分を統合
│   ├── FrenchIpaTests.cs                    # F1: IPA変換テスト (23件) ✅
│   ├── FrenchPhonemeTests.cs                # F1: 音素モデルテスト (31件) ✅
│   ├── FrenchNumberToWordsTests.cs          # F2: 数値→文字列変換テスト (55件) ✅
│   ├── FrenchNormalizerTests.cs             # F2: 正規化テスト (51件) ✅
│   ├── AllophoneProcessorTests.cs           # F2: 異音テスト (18件) ✅
│   ├── FrenchExceptionDictionaryTests.cs    # F2: 例外辞書テスト (24件) ✅
│   ├── LiaisonTests.cs                      # F2: リエゾンテスト（オプション、未実装）
│   ├── FrenchXSampaTests.cs                 # F3: X-SAMPA変換テスト (20-25件)
│   ├── FrenchEdgeCaseTests.cs               # F3: エッジケーステスト (30-35件)
│   │                                        #     Unicode NFC/NFD, バッチ大量入力 含む
│   ├── FrenchPerformanceTests.cs            # F3: パフォーマンステスト (10-15件)
│   ├── FrenchAccuracyTests.cs               # F3: 精度・回帰テスト (20-25件)
│   │                                        #     キュレーション済みコーパス正確性回帰
│   ├── FrenchDatasetEvaluationTests.cs      # F3: 外部TSVコーパスPER閾値テスト (10-15件)
│   └── FrenchAllophoneEvaluationTests.cs    # F3: 異音プロファイル別PER評価 (5-10件)
├── Multilingual/
│   ├── MultilingualFrenchTests.cs           # F4: フランス語Multilingual統合テスト (25-30件)
│   └── MultilingualMixedLanguageTests.cs    # F4: 5言語混在テスト（追記）(15-20件)
```

### テストデータフォーマット

精度評価用TSVデータ（スペイン語と同一フォーマット）:

- 文字コード: **UTF-8（BOM無し）**
- 改行コード: **LF**
- 区切り文字: TAB
- コメント行: `#` で開始

```
# word<TAB>expected_ipa
bonjour	bɔ̃ʒuʁ
merci	mɛʁsi
france	fʁɑ̃s
```

評価閾値JSON（`tools/french_eval_thresholds.json`）:

```json
{
  "datasets": {
    "ipa_dict_fr_fr_sample.tsv": {
      "base": 0.08,
      "allophones": 0.08,
      "no_exceptions": 0.12
    },
    "ipa_dict_fr_fr_full.tsv": {
      "base": 0.12,
      "allophones": 0.12,
      "no_exceptions": 0.18
    },
    "wikipron_fra_latn_broad_filtered_sample.tsv": {
      "base": 0.08
    },
    "wikipron_fra_latn_broad_filtered_full.tsv": {
      "base": 0.12
    }
  }
}
```

---

## 8. 評価ツール

### tools/DotNetG2P.FrenchEval

スペイン語 `DotNetG2P.SpanishEval` と同一アーキテクチャのフランス語評価ツール:

```
tools/
├── DotNetG2P.FrenchEval/
│   ├── DotNetG2P.FrenchEval.csproj      # コンソールアプリ (net8.0)
│   └── Program.cs                        # メインエントリ
├── refresh_french_eval_data.ps1          # ipa-dict/WikiPronデータ取得スクリプト
├── run_french_full_evaluation.ps1        # 全量評価実行スクリプト
└── french_eval_thresholds.json           # PER閾値定義
```

#### refresh_french_eval_data.ps1

```powershell
# ipa-dict fr_FRデータ取得
# WikiPron fra_latn_broad_filteredデータ取得
# サンプル（500語）とフル（全量）のTSV生成
# artifacts/french-eval/corpora/ に出力
```

#### run_french_full_evaluation.ps1

```powershell
# DotNetG2P.FrenchEvalを実行
# 全データセット × 全プロファイル（base, allophones, no_exceptions）のPER/WER計算
# カテゴリ別集計（母音/子音/鼻母音/黙字/外来語等）
# artifacts/french-eval/reports/ にレポート出力
# 閾値超過時に非ゼロ終了コード
```

#### 評価プロファイル

| プロファイル名 | EnableAllophones | EnableExceptionDictionary | 説明 |
|--------------|-----------------|--------------------------|------|
| base | false | true | 基本ルール+例外辞書 |
| allophones | true | true | 異音規則有効 |
| no_exceptions | false | false | ルールのみ（辞書なし） |

#### カテゴリ別集計

ミスマッチを以下のカテゴリに分類して集計:

| カテゴリ | 説明 |
|---------|------|
| nasal_vowel | 鼻母音化の誤り |
| silent_letter | 黙字処理の誤り |
| schwa | シュワー脱落/保持の誤り |
| vowel_quality | 母音の開閉の誤り（e/ɛ, o/ɔ, ø/œ） |
| foreign_word | 外来語の誤り |
| liaison | リエゾン関連の誤り |
| consonant | 子音変換の誤り |
| suffix_pattern | -tion/-sion/-ill-系接尾辞の誤り |
| h_aspire | h aspire/h muet 関連の誤り |
| other | その他 |

---

## 9. テスト実装の優先順位

### Phase 1（F1と同時）: 最重要

1. `GraphemeToPhonemeRulesTests.cs` - コア規則の正確性確認（h aspire/h muet, エリジョン, -tion/-sion/-ill-系含む）
2. `FrenchG2PEngineTests.cs` - エンジンAPI動作確認
3. `FrenchPhonemeTests.cs` - モデルの正確性確認
4. `FrenchSyllabifierTests.cs` - 音節分割の正確性確認

### Phase 2（F2と同時）: 高優先度

5. `FrenchNormalizerTests.cs` - 正規化の正確性確認
6. `FrenchNumberToWordsTests.cs` - 数値変換の正確性確認
7. `FrenchExceptionDictionaryTests.cs` - 例外辞書の動作確認
8. `AllophoneProcessorTests.cs` - 異音規則の正確性確認

### Phase 3（F3と同時）: 中優先度

9. `FrenchAccuracyTests.cs` - キュレーション済みコーパス回帰テスト
10. `FrenchDatasetEvaluationTests.cs` - 外部コーパスPER閾値テスト（品質ゲート）
11. `FrenchAllophoneEvaluationTests.cs` - 異音プロファイル別PER評価
12. `FrenchEdgeCaseTests.cs` - ロバスト性確認（Unicode NFC/NFD含む）
13. `FrenchXSampaTests.cs` - X-SAMPA変換確認
14. `FrenchPerformanceTests.cs` - パフォーマンス回帰検出

### Phase 4（F4と同時）: 統合

15. `MultilingualFrenchTests.cs` - Multilingual統合確認
16. `MultilingualMixedLanguageTests.cs` - 5言語混在テスト

---

## 10. CI/CDとの統合

### ci.yml での実行

```yaml
# 通常テスト（Performance/Accuracy以外）
dotnet test --filter "Category!=Performance&Category!=Accuracy"

# PER回帰テスト（リリース前）
dotnet test --filter "Category=Accuracy"
```

### 全量評価（リリース前）

```powershell
# 評価データ取得
./tools/refresh_french_eval_data.ps1

# 全量PER/WER評価
./tools/run_french_full_evaluation.ps1

# 閾値超過時はCI失敗
```

---

## 付録: スペイン語G2Pとの比較

| 項目 | スペイン語 | フランス語（予測） |
|------|----------|----------------|
| 正書法の透明度 | 高い（ほぼ1対1） | 低い（多対多） |
| PER目標 | 1-2% | 3-6%（F2/F3） |
| 主な困難 | 方言差（seseo/distincion） | 黙字、鼻母音化、シュワー、位置の法則、h aspire/h muet |
| 例外辞書依存度 | 低い（~100語） | 高い（~500-1000語以上） |
| 正規化の複雑度 | 中程度 | 中程度（数字・日付規則がフランス固有、vigesimal体系） |
| テスト件数 | 355件 | 400-430件（目標）、現時点366件（F1: 218件 + F2: 148件） |
| 評価データセット | ipa-dict es_ES/es_MX + WikiPron spa | ipa-dict fr_FR + WikiPron fra |
| 方言サポート | LatinAmerican / Castilian | Metropolitan / Conservative |
