# スウェーデン語G2P マイルストーン計画

> **作成日**: 2026-04-02
> **ブランチ**: `feature/swedish-g2p`
> **関連**: [技術調査レポート](swedish-g2p-research.md) | [チケット一覧](../tickets/README.md)

---

## 概要

DotNetG2P.Swedish パッケージの実装を4つのマイルストーン（Sw1-Sw4）で段階的に進める。既存言語パッケージ（スペイン語S1-S4、フランス語F1-F4、ポルトガル語P1-P4）と同一の設計パターンを踏襲し、各マイルストーンで動作するG2Pエンジンを段階的に拡張する。

### 全体目標

| 指標 | 目標値 |
|------|--------|
| 最終PER（ipa-dict base） | < 4% |
| 最終PER（allophones） | < 3% |
| テスト総数 | 400+ |
| 例外辞書規模 | 500+ エントリ |
| 音素数（enum） | 41 |
| 方言 | Central / FinlandSwedish |
| 対応出力形式 | IPA / X-SAMPA / PUA / Prosody |

### マイルストーン一覧

| # | 名称 | 主要成果物 | テスト目標 | PER目標 |
|---|------|-----------|----------|---------|
| **Sw1** | コアルールエンジン + 基本MVP | G2P規則5フェーズ、音節分割、基本ストレス、IPA出力 | 150+ | < 15% |
| **Sw2** | 例外辞書 + テキスト正規化 + X-SAMPA | 例外辞書300+語、正規化11段階、X-SAMPA変換 | 250+ | < 8% |
| **Sw3** | ピッチアクセント + 方言 + PUA + Prosody | Accent 1/2予測、Dialect enum、異音処理、PUA変換 | 350+ | < 4% |
| **Sw4** | Multilingual統合 + 評価ツール + リリース準備 | Language.Swedish統合、SwedishEval、フル評価 | 400+ | < 4% |

---

## Sw1: コアルールエンジン + 基本MVP

> **チケット**: [SW1-001](../tickets/SW1-001-project-scaffolding.md) | [SW1-002](../tickets/SW1-002-phoneme-models.md) | [SW1-003](../tickets/SW1-003-orthography-syllabifier.md) | [SW1-004](../tickets/SW1-004-g2p-rules-engine.md) | [SW1-005](../tickets/SW1-005-stress-ipa-converter.md) | [SW1-006](../tickets/SW1-006-engine-main-api.md) | [SW1-007](../tickets/SW1-007-sw1-tests-validation.md)

### 目的

`SwedishG2PEngine.ToPhonemes("hej världen")` → `"h ɛ j v æ ɭ d ɛ n"` が動作する最小限のG2Pエンジンを構築する。

### 成果物

#### プロジェクト骨格

```
src/DotNetG2P.Swedish/
├── DotNetG2P.Swedish.csproj          — netstandard2.1, IsPackable=true
├── DotNetG2P.Swedish.asmdef          — Unity用アセンブリ定義
├── package.json                       — UPM (com.dotnetg2p.swedish)
├── README.md
├── LICENSE.md
├── THIRD-PARTY-NOTICES.md
├── SwedishG2PEngine.cs                — メインエンジン（IDisposable）
├── SwedishG2POptions.cs               — イミュータブルオプション
├── Models/
│   ├── SwedishIpaPhoneme.cs           — byte基底enum（41音素）
│   ├── SwedishPhoneme.cs              — ストレス付き音素struct
│   ├── SwedishPronunciation.cs        — 発音情報（音素配列+音節+ストレス）
│   ├── SwedishSyllable.cs             — 音節struct
│   └── SwedishDialect.cs              — 方言enum（Central=0のみ使用）
├── Rules/
│   ├── GraphemeToPhonemeRules.cs      — 5フェーズG2P統合処理
│   ├── SwedishSyllabifier.cs          — Onset最大化音節分割
│   ├── StressAssigner.cs              — 基本ストレス付与
│   └── SwedishOrthography.cs          — 正書法ユーティリティ
├── Conversion/
│   └── IpaConverter.cs                — IPA文字列変換
└── Internal/
    ├── BatchConversionHelper.cs       — sync-shared-internals管理
    └── PreserveAttribute.cs           — sync-shared-internals管理
```

#### SwedishIpaPhoneme enum（41音素）

| 範囲 | カテゴリ | 音素 |
|------|---------|------|
| 0-8 | 長母音 | iː, yː, ʉː, uː, eː, øː, ɛː, oː, ɑː |
| 9-17 | 短母音 | ɪ, ʏ, ɵ, ʊ, ɛ, œ, ɔ, a, ə |
| 18-23 | 破裂音 | p, b, t, d, k, ɡ |
| 24-29 | 摩擦音 | f, v, s, h, ɧ(sj), ɕ(tj) |
| 30-32 | 鼻音 | m, n, ŋ |
| 33-35 | 接近音/ふるえ音 | l, r, j |
| 36-40 | そり舌音 | ʈ, ɖ, ɳ, ɭ, ʂ |

#### G2P規則 5フェーズ

| Phase | 処理 | 主要規則 |
|-------|------|---------|
| 1 | トリグラフ/ダイグラフ認識 | stj→ɧ, skj→ɧ, sj→ɧ, sk+軟母音→ɧ, tj→ɕ, kj→ɕ, ng→ŋ, nk→ŋk, ck→kː, dj→j, gj→j, hj→j, lj→j |
| 2 | 子音軟化 | k+軟母音→ɕ, g+軟母音→j, sk+軟母音→ɧ |
| 3 | 母音変換 | 相補的数量法則（V+CC→短母音, V+C→長母音）、各書記素→音素マッピング |
| 4 | そり舌化 | rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ |
| 5 | 黙字処理 | 語頭dj/gj/hj/lj→j, 語末-ig/-lig の g 黙字 |

#### 音節分割（Onset最大化）

- 最大Onset: 3子音（spr-, str-, skr- 等）
- 有効な2子音Onset: pl, bl, pr, br, tr, dr, kl, gl, kr, gr, fr, fl, sl, sm, sn, sp, st, sk, sv, kv 等
- Coda: /h/と/ɕ/以外の全子音

#### ストレス付与（基本）

- デフォルト: 第1音節にprimary stress（ゲルマン語規則）
- 外来語接尾辞: -tion, -sion, -ell, -ent, -ör, -ör 等でストレスシフト

#### Public API（Sw1時点）

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| ToPhonemes(text) | string | スペース区切り音素列 |
| ToIPA(text) | string | IPA文字列（ストレス付き） |
| ToIPAWithoutStress(text) | string | IPA文字列（ストレスなし） |
| ToPhonemeList(text) | IReadOnlyList&lt;SwedishPhoneme&gt; | 音素リスト |
| ToSyllables(word) | IReadOnlyList&lt;SwedishSyllable&gt; | 音節リスト |
| ToPhonemesBatch(texts) | IReadOnlyList&lt;string&gt; | バッチ音素変換 |
| ToIPABatch(texts) | IReadOnlyList&lt;string&gt; | バッチIPA変換 |
| Dispose() | void | リソース解放 |

### テスト（Sw1: 150+）

```
tests/DotNetG2P.Tests/SwedishG2P/
├── SwedishG2PEngineTests.cs                — エンジン基本機能 (15テスト)
│   ├── ToPhonemes_基本単語_期待される音素列を返す
│   ├── ToIPA_基本単語_期待されるIPA文字列を返す
│   ├── ToIPAWithoutStress_ストレスマークなし
│   ├── ToPhonemeList_構造化された音素リスト
│   ├── ToSyllables_正しい音節分割
│   ├── Batch系_複数テキスト処理
│   ├── Dispose後_ObjectDisposedException
│   └── null/空文字入力_適切な戻り値
│
├── GraphemeToPhonemeRulesTests.cs           — G2P規則 (40テスト)
│   ├── Phase1_トリグラフ_stj→ɧ, skj→ɧ, sj→ɧ
│   ├── Phase1_ダイグラフ_tj→ɕ, kj→ɕ, ng→ŋ, ck→kː
│   ├── Phase2_子音軟化_k+軟母音→ɕ, g+軟母音→j
│   ├── Phase2_子音軟化_sk+軟母音→ɧ
│   ├── Phase2_硬母音前_k→k, g→ɡ, sk→sk
│   ├── Phase3_長母音_開音節→長母音
│   ├── Phase3_短母音_二重子音前→短母音
│   ├── Phase3_oの特殊対応_o→uː/ʊ/ɔ
│   ├── Phase4_そり舌化_rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ
│   └── Phase5_黙字_dj→j, hj→j, lj→j, gj→j
│
├── SwedishSyllabifierTests.cs              — 音節分割 (21テスト)
│   ├── 単音節語_正しく分割
│   ├── 2音節語_Onset最大化で分割
│   ├── 3音節語_連続子音クラスタの分割
│   ├── 子音連結Onset_spr/str/skr
│   └── 複合語_音節境界
│
├── StressAssignerTests.cs                  — ストレス付与 (15テスト)
│   ├── 固有語_第1音節にストレス
│   ├── 外来語接尾辞_-tion→最終音節前
│   ├── 単音節語_ストレスあり
│   └── 複合語_primary+secondary
│
├── SwedishOrthographyTests.cs              — 正書法ユーティリティ (20テスト)
│   ├── IsSoftVowel_e/i/y/ä/ö→true
│   ├── IsHardVowel_a/o/u/å→true
│   ├── IsVowelChar_母音判定
│   └── IsConsonantChar_子音判定
│
├── SwedishIpaTests.cs                      — IPA変換 (15テスト)
│   ├── ToSymbol_各音素→正しいIPA記号
│   ├── Convert_発音情報→IPA文字列
│   └── ストレスマーク配置_音節先頭
│
├── SwedishPhonemeTests.cs                  — 音素struct (14テスト)
│   ├── enum値_byte基底で正しい値
│   ├── IsVowel/IsConsonant判定
│   ├── Equals/GetHashCode
│   └── IsSyllableNucleus判定
│
├── SwedishAccuracyTests.cs                 — キュレーション精度 (25テスト)
│   ├── [Theory] 基本語彙20語の変換精度
│   ├── sj音_各綴りパターン
│   ├── tj音_各綴りパターン
│   └── そり舌音_各パターン
│
└── SwedishEdgeCaseTests.cs                 — エッジケース (5テスト)
    ├── 空文字/null入力
    ├── 数字のみの入力
    ├── 記号のみの入力
    └── 非スウェーデン語文字
```

### 完了条件

- [x] `dotnet build` が成功
- [x] `dotnet test --filter "ClassName~SwedishG2P"` で 150+ テスト pass
- [x] `SwedishG2PEngine.ToIPA("hej")` → `"hɛj"` が正しく動作
- [x] `SwedishG2PEngine.ToIPA("köpa")` → `"ɕøːpa"` （子音軟化）
- [x] `SwedishG2PEngine.ToIPA("sjuk")` → `"ɧʉːk"` （sj音）
- [x] `SwedishG2PEngine.ToIPA("bord")` → `"buːɖ"` （そり舌化）
- [x] `SwedishG2PEngine.ToIPA("ljus")` → `"jʉːs"` （黙字）
- [x] Internal/ ファイルが既存パッケージ（Chinese等）のマスターファイルと同一内容であること（sync-shared-internals.ps1 へのリスト追加はSw4で実施）
- [ ] ipa-dict サンプル(256語)で PER < 15%

---

## Sw2: 例外辞書 + テキスト正規化 + X-SAMPA

> **チケット**: [SW2-001](../tickets/SW2-001-exception-dictionary.md) | [SW2-002](../tickets/SW2-002-normalizer-number-to-words.md) | [SW2-003](../tickets/SW2-003-xsampa-function-words.md) | [SW2-004](../tickets/SW2-004-eval-data-sample-tsv.md) | [SW2-005](../tickets/SW2-005-sw2-tests-validation.md)

### 目的

例外辞書によりsj音・外来語・機能語の精度を大幅に向上させる。テキスト正規化により数字・略語・記号を含むテキストを処理可能にする。X-SAMPA出力形式を追加する。

### 成果物

#### 追加・変更ファイル

```
src/DotNetG2P.Swedish/
├── Data/
│   ├── SwedishExceptionDictionary.cs  — 例外辞書ローダー
│   └── swedish_exceptions.master.tsv  — 埋め込みリソース（300+語）
├── Normalization/
│   ├── SwedishNormalizer.cs           — 11段階テキスト正規化
│   └── NumberToWords.cs               — 数値→単語変換（en/ett性区別）
├── Conversion/
│   ├── XSampaConverter.cs             — X-SAMPA変換
│   └── FunctionWordList.cs            — 機能語リスト（ストレス除去用）
└── SwedishG2POptions.cs               — EnableExceptionDictionary, EnableTextNormalization 追加
```

#### 例外辞書（300+語）

**TSV形式:**
```tsv
# surface	dialect	category	accent	stress_index	phonemes	source	note
och	*	function_word	1	-1	ɔ	manual	ch黙字
det	*	function_word	1	-1	d eː	manual	t黙字
de	*	function_word	1	-1	d ɔ m	manual	完全不規則
dem	*	function_word	1	-1	d ɔ m	manual	deと同音
mig	*	function_word	1	-1	m ɛ j	manual	ig→ej
dig	*	function_word	1	-1	d ɛ j	manual	ig→ej
sig	*	function_word	1	-1	s ɛ j	manual	ig→ej
jag	*	function_word	1	-1	j ɑː	manual	g弱化
chef	*	loanword_fr	1	0	ɧ eː f	manual	フランス語由来sj音
garage	*	loanword_fr	1	1	ɡ a|r ɑː ɧ	manual	フランス語由来
station	*	sj_exception	2	1	s t a|ɧ uː n	manual	-tion語尾
mission	*	sj_exception	2	1	m ɪ|ɧ uː n	manual	-sion語尾
kille	*	softening_exception	1	0	k ɪ|l ɛ	manual	軟母音前だがk硬い
Göteborg	*	place_name	2	1	j øː t ɛ|b ɔ r j	manual	不規則
Stockholm	*	place_name	1	0	s t ɔ k|h ɔ l m	manual	
```

**カテゴリ別規模:**

| カテゴリ | 推定数 | 内容 |
|---------|-------|------|
| function_word | 30-40 | 代名詞, 前置詞, 接続詞, 助動詞の弱形 |
| loanword_fr | 40-50 | フランス語由来（chef, garage, restaurant 等） |
| loanword_en | 40-50 | 英語由来（show, team, design 等） |
| loanword_other | 10-15 | ドイツ語・ラテン語由来 |
| sj_exception | 30-40 | -tion/-sion語尾、ch→ɧ のパターン |
| softening_exception | 15-20 | 子音軟化の例外（kille, gem 等） |
| place_name | 40-50 | 主要都市・県名 |
| silent_letter | 10-15 | gn-, ps- 等のギリシャ語由来 |
| irregular | 15-20 | その他不規則語 |

#### テキスト正規化（11段階）

| 段階 | メソッド | 例 |
|------|---------|-----|
| 1 | NormalizeUnicode | NFC正規化 + 小文字化 |
| 2 | ExpandAbbreviations | t.ex.→till exempel, dvs.→det vill säga, bl.a.→bland annat |
| 3 | ExpandOrdinals | 1:a→första, 3:e→tredje, 10:e→tionde |
| 4 | ExpandDates | 2026-04-02→andra april tvåtusentjugosex |
| 5 | ExpandTimes | 15:30→femton trettio, kl. 3→klockan tre |
| 6 | ExpandCurrencies | 5 kr→fem kronor, 29:99 kr→tjugonio kronor och nittionio öre |
| 7 | ExpandPercentages | 50%→femtio procent |
| 8 | ExpandDecimals | 3,14→tre komma fjorton |
| 9 | ExpandNumbers | 42→fyrtiotvå（en/ett性区別: en bil / ett hus） |
| 10 | ExpandSymbols | @→snabel-a, &→och, %→procent |
| 11 | NormalizeWhitespace | 連続スペース統一, trim |

#### NumberToWords 特殊対応

- **en/ett性区別**: 1 = en（共性名詞前）/ ett（中性名詞前）。単独カウントは通常 ett
- **長大数制（Long Scale）**: miljard = 10^9, biljon = 10^12
- **複合数は1語**: tjugoett(21), nittionio(99)
- **小数点はカンマ**: 3,14 = tre komma fjorton
- **千の区切りはスペース**: 1 000 000 = en miljon

#### 追加 Public API

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| ToXSampa(text) | string | X-SAMPA文字列 |
| ToXSampaWithoutStress(text) | string | ストレスなしX-SAMPA |
| ToXSampaBatch(texts) | IReadOnlyList&lt;string&gt; | バッチX-SAMPA変換 |

#### SwedishG2POptions 拡張

```csharp
public sealed class SwedishG2POptions
{
    public SwedishDialect Dialect { get; }              // Central(default)
    public bool IncludeStress { get; }                  // default: true
    public bool EnableTextNormalization { get; }         // default: true
    public bool EnableExceptionDictionary { get; }       // default: true
    public string Separator { get; }                     // default: " "
}
```

### テスト（Sw2追加分: +100 = 累計250+）

```
tests/DotNetG2P.Tests/SwedishG2P/
├── SwedishNormalizerTests.cs               — 正規化 (40テスト)
│   ├── NFC正規化_å/ä/öのNFD入力が正規化される
│   ├── 略語展開_t.ex./dvs./bl.a./kl.
│   ├── 序数展開_1:a/3:e/10:e
│   ├── 日付展開_ISO/スウェーデン形式
│   ├── 時刻展開_15:30/kl.3
│   ├── 通貨展開_kr/SEK/:-
│   ├── 数字展開_基数/序数/en-ett区別
│   └── 記号展開_@/&/%
│
├── NumberToWordsTests.cs                   — 数値変換 (20テスト)
│   ├── 基数_0-20/30-90/100/1000/miljon/miljard
│   ├── en_ett性区別
│   ├── 複合数_tjugoett/nittionio
│   ├── 小数_カンマ区切り
│   └── 序数_första/andra/tredje/...-nde
│
├── SwedishExceptionDictionaryTests.cs      — 例外辞書 (15テスト)
│   ├── TryLookup_機能語_och/det/de/dem
│   ├── TryLookup_フランス語外来語_chef/garage
│   ├── TryLookup_英語外来語_show/team
│   ├── TryLookup_sj例外_station/mission
│   ├── TryLookup_軟化例外_kille/gem
│   ├── TryLookup_地名_Göteborg/Stockholm
│   ├── TryLookup_存在しない語_false返却
│   └── 方言フィルタ_dialect=*で全方言マッチ
│
├── SwedishXSampaTests.cs                   — X-SAMPA変換 (15テスト)
│   ├── ToSymbol_各音素→正しいX-SAMPA記号
│   ├── Convert_発音情報→X-SAMPA文字列
│   └── ストレスマーク_"(primary)
│
└── SwedishDatasetEvaluationTests.cs        — 評価テスト (10テスト)
    ├── IpaDict_サンプル_baseプロファイル_PER閾値内
    ├── IpaDict_サンプル_noExceptionsプロファイル_PER閾値内
    ├── WikiPron_サンプル_baseプロファイル_PER閾値内
    └── 最小サンプル数チェック

tests/TestData/SwedishG2P/
├── README.md
├── ipa_dict_sv_se_sample.tsv              (256件)
└── wikipron_swe_latn_broad_filtered_sample.tsv  (256件)
```

### 完了条件

- [x] 例外辞書 300+ エントリがアセンブリに埋め込み
- [x] `ToIPA("och")` → `"ɔ"`（例外辞書経由）
- [x] `ToIPA("station")` → `"staˈɧuːn"`（sj例外辞書経由）
- [x] `Normalize("3:e april 2026")` → `"tredje april tvåtusentjugosex"`
- [x] `Normalize("5 kr")` → `"fem kronor"`
- [x] `ToXSampa("hej")` → 正しいX-SAMPA出力
- [x] 正規化テスト 40+ pass
- [ ] ipa-dict サンプル PER < 8%（base）
- [ ] ipa-dict サンプル PER < 15%（no_exceptions）

---

## Sw3: ピッチアクセント + 方言 + PUA + Prosody

> **チケット**: [SW3-001](../tickets/SW3-001-pitch-accent-prediction.md) | [SW3-002](../tickets/SW3-002-allophone-processor.md) | [SW3-003](../tickets/SW3-003-dialect-finland-swedish.md) | [SW3-004](../tickets/SW3-004-pua-prosody-api.md) | [SW3-005](../tickets/SW3-005-sw3-tests-dict-expansion.md)

### 目的

スウェーデン語固有のピッチアクセント（accent 1/2）を予測し、方言差異（Central / FinlandSwedish）を処理し、piper-plus互換のPUA出力とProsody APIを実装する。

### 成果物

#### 追加・変更ファイル

```
src/DotNetG2P.Swedish/
├── Models/
│   ├── SwedishProsodyInfo.cs          — 韻律情報struct（A1=accent, A2=stress, A3=syllables）
│   └── SwedishProsodyResult.cs        — 韻律結果（IPA音素配列 + ProsodyInfo配列）
├── Rules/
│   ├── StressAssigner.cs              — ★拡張: ピッチアクセント予測ロジック追加
│   └── AllophoneProcessor.cs          — ★新規: 方言別異音処理
├── Conversion/
│   └── SwedishPuaMapper.cs            — ★新規: PUA変換
├── SwedishAllophoneFeatures.cs        — ★新規: 異音規則フラグ [Flags] enum
└── SwedishG2POptions.cs               — ★拡張: EnableAllophones, AllophoneFeatures 追加
```

#### ピッチアクセント予測

```
StressAssigner.AssignAccent() フロー:

1. 単音節語チェック → Accent 1（常に）
2. 例外辞書のaccent情報を優先
3. 複合語検出 → Accent 2（常に）
4. Accent 2 誘発接尾辞チェック:
   -ar（複数）, -or（複数）, -te/-de（過去形）,
   -het（派生名詞）, -are（行為者）, -ande/-ende（現在分詞）,
   語幹末尾 -e → Accent 2
5. デフォルト → Accent 1
```

#### 方言対応（SwedishDialect enum）

```csharp
public enum SwedishDialect : byte
{
    Central = 0,        // デフォルト。そり舌あり、ピッチアクセントあり
    FinlandSwedish = 1, // そり舌なし、ピッチアクセントなし、帯気なし
}
```

**方言別処理差異:**

| 処理 | Central | FinlandSwedish |
|------|---------|----------------|
| そり舌化 (rt→ʈ etc.) | 適用 | **スキップ** → rt, rd, rn, rl, rs のまま |
| ピッチアクセント | Accent 1/2 出力 | **A1=0固定**（アクセント区別なし） |
| tj音 | 摩擦音 [ɕ] | 破擦音（PUA対応検討） |

#### 異音処理（AllophoneProcessor）

```csharp
[Flags]
public enum SwedishAllophoneFeatures : byte
{
    None = 0,
    Retroflexion = 1 << 0,          // r+歯茎→そり舌（Central有効、Finland無効）
    VowelLengthMarking = 1 << 1,    // 長母音へのːマーク
    RBeforeVowelLowering = 1 << 2,  // /r/前でɛ→æ, œ→œ̞

    CentralDefault = Retroflexion | VowelLengthMarking,
    FinlandDefault = VowelLengthMarking,
    All = Retroflexion | VowelLengthMarking | RBeforeVowelLowering,
}
```

#### PUA変換（SwedishPuaMapper）

スウェーデン語は多文字IPA音素が少なく、PUA追加は最小限:

| 音素 | IPA | PUA候補 | 備考 |
|------|-----|---------|------|
| tɕ | t͡ɕ | 0xE023 | 韓国語/中国語と共有（FinlandSwedish tj音） |

#### Prosody API

```csharp
public readonly struct SwedishProsodyInfo
{
    public byte A1 { get; }  // ピッチアクセント: 0=不明, 1=accent1, 2=accent2
    public byte A2 { get; }  // ストレス: 0=なし, 1=primary, 2=secondary
    public byte A3 { get; }  // 語の音節数
}
```

#### 追加 Public API

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| ToPuaPhonemes(text) | string[] | PUA音素配列 |
| ToPuaString(text) | string | PUA音素文字列 |
| ToPuaStringBatch(texts) | IReadOnlyList&lt;string&gt; | バッチPUA変換 |
| ToIpaWithProsody(text) | SwedishProsodyResult | IPA＋韻律情報 |
| ToIpaWithProsodyBatch(texts) | IReadOnlyList&lt;SwedishProsodyResult&gt; | バッチ韻律変換 |

### テスト（Sw3追加分: +100 = 累計350+）

```
tests/DotNetG2P.Tests/SwedishG2P/
├── StressAssignerTests.cs                  — ★拡張: アクセント予測テスト (+20テスト)
│   ├── 単音節語_常にAccent1
│   ├── 複合語_常にAccent2
│   ├── -ar複数形_Accent2
│   ├── -te/-de過去形_Accent2
│   ├── -het派生名詞_Accent2
│   ├── -(e)n定冠詞_Accent1
│   ├── 外来語_Accent1
│   └── 最小対語_期待されるアクセント
│
├── AllophoneProcessorTests.cs              — 異音処理 (20テスト)
│   ├── Central_そり舌化_rt→ʈ
│   ├── Finland_そり舌化スキップ_rt→rt
│   ├── Central_全異音プロファイル
│   └── Finland_全異音プロファイル
│
├── SwedishAllophoneEvaluationTests.cs      — 異音参照テスト (5テスト)
│   ├── swedish_allophone_reference.tsv に基づく完全一致検証
│   └── central/finland プロファイル別検証
│
├── SwedishProsodyTests.cs                  — 韻律テスト (15テスト)
│   ├── A1_ピッチアクセント番号_正しい値
│   ├── A2_ストレスレベル_正しい値
│   ├── A3_音節数_正しい値
│   └── バッチ版_正しい結果配列
│
├── SwedishPuaMappingTests.cs               — PUA変換 (10テスト)
│   ├── MapToPua_各音素→正しいPUA文字
│   ├── ApplyPuaMapping_配列変換
│   └── ToPuaString_文字列出力
│
├── SwedishDialectTests.cs                  — 方言テスト (15テスト)
│   ├── Central_デフォルト設定
│   ├── Finland_そり舌化なし
│   ├── Finland_ピッチアクセントなし
│   └── オプション切り替え_正しい出力差異
│
└── SwedishPerformanceTests.cs              — パフォーマンス (6テスト)
    ├── 初期化時間_閾値内
    ├── 単文変換速度_閾値内
    ├── バッチ変換速度_閾値内
    ├── メモリ成長_閾値内
    └── Dispose後メモリ解放

tests/TestData/SwedishG2P/
└── swedish_allophone_reference.tsv        (15-20件)
```

### 完了条件

- [x] `ToIpaWithProsody("anden")` → accent 情報付きIPA出力
- [x] `new SwedishG2PEngine(new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish))` で方言切替
- [x] FinlandSwedish方言: `ToIPA("bord")` → `"buːrd"`（そり舌なし）
- [x] Central方言: `ToIPA("bord")` → `"buːɖ"`（そり舌あり）
- [x] PUA変換が動作
- [x] Prosody API (A1/A2/A3) が正しい値を返す
- [ ] ipa-dict サンプル PER < 4%（base, Central方言）
- [ ] 例外辞書 500+ エントリに拡充

---

## Sw4: Multilingual統合 + 評価ツール + リリース準備

> **チケット**: [SW4-001](../tickets/SW4-001-multilingual-integration.md) | [SW4-002](../tickets/SW4-002-text-segmenter-detection.md) | [SW4-003](../tickets/SW4-003-eval-tool.md) | [SW4-004](../tickets/SW4-004-cicd-solution-docs.md) | [SW4-005](../tickets/SW4-005-sw4-full-eval-release.md)

### 目的

DotNetG2P.Multilingual パッケージにスウェーデン語を統合し、Language.Swedish として8言語対応を完成させる。評価ツールを整備してフルコーパスでPER目標を達成する。NuGet/UPMパッケージのリリース準備を行う。

### 成果物

#### Multilingual統合（変更ファイル）

```
src/DotNetG2P.Multilingual/
├── Language.cs                     — Swedish = 7 追加
├── MultilingualG2PEngine.cs        — Lazy<SwedishG2PEngine> フィールド追加
├── MultilingualG2POptions.cs       — SwedishG2POptions? プロパティ追加
├── TextSegmenter.cs                — LangSwedish byte定数 + 言語判定ロジック
├── LanguageDetector.cs             — スウェーデン語文字範囲（å検出）
├── CapabilityAdapters.cs           — スウェーデン語エンジン登録
├── DotNetG2P.Multilingual.csproj   — ProjectReference追加
├── DotNetG2P.Multilingual.asmdef   — references追加
└── package.json                    — com.dotnetg2p.swedish 依存追加
```

#### 言語判定ロジック

```csharp
// TextSegmenter.ResolveLatinLanguage() に追加:

// 確定信号: å はスウェーデン語/ノルウェー語/デンマーク語の明確マーカー
// （現在ノルウェー語・デンマーク語は非サポートのためスウェーデン語に分類）
if (ContainsExplicitSwedishCharacter(token))  // å (U+00E5)
    return LangSwedish;

// ヒューリスティクス（hasLatinExtended=false の場合のみ）
private static readonly string[] s_swedishWordSignals =
{
    "och", "att", "hej", "tack", "hur", "dag", "inte", "som",
    "det", "den", "ett", "har", "ska", "kan", "vill"
};

private static readonly string[] s_swedishSuffixSignals =
{
    "tion", "ighet", "ning", "skap", "lig", "ande", "else"
};
```

#### 評価ツール

```
tools/
├── DotNetG2P.SwedishEval/
│   ├── DotNetG2P.SwedishEval.csproj   — net8.0, ProjectReference→DotNetG2P.Swedish
│   └── Program.cs                      — フル評価CLI
├── refresh_swedish_eval_data.ps1       — ipa-dict/WikiPron DL・フィルタ
├── run_swedish_full_evaluation.ps1     — フル評価実行
└── swedish_eval_thresholds.json        — PER閾値設定
```

**swedish_eval_thresholds.json:**
```json
{
  "ipa_dict_sv_se": {
    "base": 0.04,
    "allophones": 0.03,
    "no_exceptions": 0.12
  },
  "wikipron_swe_latn_broad": {
    "base": 0.05,
    "allophones": 0.04,
    "no_exceptions": 0.15
  }
}
```

#### ソリューション・CI更新

```
DotNetG2P.slnx:
  /src/ フォルダに DotNetG2P.Swedish.csproj 追加
  /tools/ フォルダに DotNetG2P.SwedishEval.csproj 追加

.github/workflows/ci.yml:
  ビルド・テスト対象に DotNetG2P.SwedishEval 追加
  dotnet pack 対象に DotNetG2P.Swedish 追加

tools/sync-shared-internals.ps1:
  DotNetG2P.Swedish/Internal/ をコピー先リストに追加
```

#### CLAUDE.md 更新

進捗状況テーブルにスウェーデン語行を追加:

```markdown
| スウェーデン語 | DotNetG2P.Swedish | Sw1-Sw4完了 | 400+ | ルールベース+例外辞書500+語、Central/FinlandSwedish方言 |
```

### テスト（Sw4追加分: +45 = 累計395+）

```
tests/DotNetG2P.Tests/
├── SwedishG2P/
│   └── SwedishDatasetEvaluationTests.cs   — ★拡張: フル評価追加 (+10テスト)
│       ├── IpaDict_フル_baseプロファイル_PER閾値内
│       ├── IpaDict_フル_allophoneプロファイル_PER閾値内
│       ├── IpaDict_フル_noExceptionsプロファイル_PER閾値内
│       ├── WikiPron_フル_baseプロファイル_PER閾値内
│       └── WikiPron_フル_allophoneプロファイル_PER閾値内
│
├── Multilingual/
│   ├── MultilingualSwedishTests.cs         — 言語統合 (20テスト)
│   │   ├── Language_Swedish_値は7
│   │   ├── Segment_å含むテキスト_Swedishに分類
│   │   ├── Segment_ochキーワード_Swedishに分類
│   │   ├── Engine_スウェーデン語のみ_正しいセグメント
│   │   ├── Engine_日瑞混在_分割される
│   │   └── Engine_英瑞混在_分割される
│   │
│   ├── MultilingualSwedishMixedLanguageTests.cs  — 混在言語 (10テスト)
│   │   ├── 日英中韓西仏葡瑞8言語混在テスト
│   │   └── 方言設定がMultilingual経由で正しく伝達
│   │
│   └── MultilingualSwedishPerformanceTests.cs    — パフォーマンス (5テスト)
│       ├── Lazy初期化_使用まで未初期化
│       ├── 初期化時間_閾値内
│       └── バッチ処理速度_閾値内
│
└── TestData/SwedishG2P/
    ├── ipa_dict_sv_se_full.tsv            (21,107件)
    └── wikipron_swe_latn_broad_filtered_full.tsv  (4,631件)
```

### 完了条件

- [x] `Language.Swedish == 7`
- [x] `TextSegmenter.Segment("hej världen", Language.Japanese, Language.English)` → `[{Swedish, "hej världen"}]`
- [x] `MultilingualG2PEngine` 経由でスウェーデン語G2Pが動作
- [x] `MultilingualG2PEngine` の Dispose でスウェーデン語エンジンも解放
- [x] 8言語混在テスト pass
- [x] `DotNetG2P.slnx` にプロジェクト追加済み
- [x] `sync-shared-internals.ps1 -Check` pass
- [x] CI (ci.yml) でビルド・テスト・パッケージ検証 pass
- [ ] ipa-dict フル PER < 4%（base）
- [ ] ipa-dict フル PER < 3%（allophones）
- [ ] WikiPron フル PER < 5%（base）
- [x] CLAUDE.md 進捗テーブル更新
- [x] CHANGELOG.md 更新

---

## 付録

### A. PER閾値一覧

| マイルストーン | ipa-dict base | ipa-dict allophones | ipa-dict no_exceptions | WikiPron base |
|-------------|-------------|---------|---------|---------|
| Sw1 | < 15% | - | < 15% | - |
| Sw2 | < 8% | - | < 15% | < 8% |
| Sw3 | < 4% | < 3% | < 12% | < 5% |
| Sw4 | < 4% | < 3% | < 12% | < 5% |

### B. テスト数推移

| マイルストーン | 新規テスト | 累計テスト |
|-------------|----------|----------|
| Sw1 | 150+ | 150+ |
| Sw2 | 100+ | 250+ |
| Sw3 | 100+ | 350+ |
| Sw4 | 45+ | 395+ |

### C. 依存関係

```
DotNetG2P.Swedish (独立、Core参照なし)
  └── 埋め込みリソース: swedish_exceptions.master.tsv

DotNetG2P.Multilingual
  ├── DotNetG2P.Core
  ├── DotNetG2P.MeCab
  ├── DotNetG2P.English
  ├── DotNetG2P.Chinese
  ├── DotNetG2P.Korean
  ├── DotNetG2P.Spanish
  ├── DotNetG2P.French
  ├── DotNetG2P.Portuguese
  └── DotNetG2P.Swedish          ← Sw4で追加
```

### D. NuGet/UPMパッケージ情報

| 項目 | 値 |
|------|-----|
| NuGet PackageId | DotNetG2P.Swedish |
| UPM名 | com.dotnetg2p.swedish |
| TargetFramework | netstandard2.1 |
| ライセンス | Apache-2.0 |
| Unity最小版 | 2021.2 |
| 依存 | なし（Pure C#） |
| タグ | g2p, swedish, tts, phoneme, ipa, unity, pitch-accent |
