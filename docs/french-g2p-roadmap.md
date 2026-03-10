# フランス語G2P (DotNetG2P.French) 実装ロードマップ

## 1. 概要

### 目標
C#/.NET（Unity対応）向けのルールベースフランス語G2P（Grapheme-to-Phoneme）エンジンを実装し、`DotNetG2P.Multilingual` パッケージに統合する。

### 方針
- **ルールベース + 例外辞書**アプローチを採用
  - フランス語は正書法と発音の対応がスペイン語より不規則だが、英語ほど不透明ではない
  - LIA_Phon（フランス語G2Pシステム）が99.3%をルールベースで達成した実績があり、ルールベースで高い精度が期待できる
- 既存のスペイン語G2P（S1-S4）アーキテクチャを踏襲し、パイプライン構成を統一
- 外部依存なし（Core参照なし、独立パッケージ）
- .NET Standard 2.1（Unity 2021.2+互換）

### 変換パイプライン
```
Normalize → Tokenize → G2PRules → Syllabifier → (StressAssigner) → AllophoneProcessor → Format
```

### フランス語G2Pの主要な技術的課題
1. **黙字（lettres muettes）**: 語末子音の大半が黙字（例: "temps" → /tɑ̃/）、"h muet" と "h aspiré" の区別
2. **リエゾン（liaison）**: 語境界を超えた連結発音（本プロジェクトでは単語単位のためスコープ外、ただし将来の拡張ポイント）
3. **鼻母音化**: "an/en" → /ɑ̃/, "in" → /ɛ̃/, "on" → /ɔ̃/, "un" → /œ̃/ の規則と例外
4. **位置の法則（loi de position）**: 閉音節 vs 開音節による母音の開閉（/e/ vs /ɛ/, /o/ vs /ɔ/）
5. **半母音化**: /i/ → /j/, /u/ → /w/, /y/ → /ɥ/ の環境依存規則
6. **外来語・学術語**: ラテン語/ギリシャ語/英語由来語の不規則発音
7. **同綴異音語（homographes hétérophones）**: "fils"（/fis/ 息子 vs /fil/ 糸）、"est"（/ɛ/ 動詞 vs /ɛst/ 方角）等

---

## 2. マイルストーン一覧

| マイルストーン | 名称 | 主な成果物 | テスト目標 | PER目標 |
|:-:|:--|:--|:-:|:-:|
| **F1** ✅ | コアG2Pルールエンジン + 基本MVP | 基本的なフランス語G2P動作 | 218件 | 8-12% |
| **F2** ✅ | 精度向上・異音規則・テキスト正規化 | 高精度フランス語G2P | 366件（累計） | 3-6% |
| **F3** | X-SAMPA・大規模精度評価・拡張テスト | 評価済みフランス語G2P | 80-100件追加 | 3-6% (確定値) |
| **F4** | Multilingual統合・パッケージング | 多言語G2Pにフランス語統合 | 40-50件追加 | - |
| | **合計** | | **400-430件** | |

> **注**: PER目標はアーキテクチャ設計書・テスト戦略書と統一済み。F3のPERはF2からの変更なし（X-SAMPA追加がメインのため）。将来のリエゾン対応後はPER 2-4%を目標とする。

---

## 3. 各マイルストーン詳細

### F1: コアG2Pルールエンジン + 基本MVP ✅ 完了

#### 内部サブフェーズ

F1はスペイン語S1に比べG2Pルールの複雑さが2-3倍あるため、内部を2つのサブフェーズに分割して段階的に実装する。マイルストーン数は増やさないが、PERを段階的に改善する。

| サブフェーズ | 内容 | PER目安 |
|:-:|:--|:-:|
| **F1a** | 基本G2P: ダイグラフ/トリグラフ + 文脈依存子音 + 単純対応 + 黙字基本（CaReFuL） | ~15-20% |
| **F1b** | 高度G2P: 鼻母音化精密化 + 位置の法則 + 半母音化 + `-tion`/`-ill-`系処理 | 8-12% |

#### スコープ

**プロジェクト構成**
- `src/DotNetG2P.French/DotNetG2P.French.csproj` (.NET Standard 2.1)
- `src/DotNetG2P.French/package.json` (UPM: com.dotnetg2p.french)
- `src/DotNetG2P.French/DotNetG2P.French.asmdef` (Unity Assembly Definition)
- `DotNetG2P.slnx` にプロジェクト追加
- `tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj` にプロジェクト参照追加

**モデル定義**
- `FrenchIpaPhoneme` enum : byte（約40種）
  - 口母音: /a/, /ɑ/, /e/, /ɛ/, /i/, /o/, /ɔ/, /u/, /y/, /ə/, /ø/, /œ/ (12種)
  - 鼻母音: /ɑ̃/, /ɛ̃/, /ɔ̃/, /œ̃/ (4種)
  - 半母音: /j/, /w/, /ɥ/ (3種)
  - 閉鎖音: /p/, /b/, /t/, /d/, /k/, /ɡ/ (6種)
  - 摩擦音: /f/, /v/, /s/, /z/, /ʃ/, /ʒ/ (6種)
  - 鼻音: /m/, /n/, /ɲ/ (3種)
  - 側音: /l/ (1種)
  - 接近音: /ʁ/ (1種)
  - 異音・補助: 無声化/ʁ̥/、/ŋ/（借用語）、連結子音等（数種）
- `FrenchPhoneme` readonly struct（音素 + 音節核フラグ）
- `FrenchPronunciation` class（音素配列ラッパー）
- `FrenchDialect` enum : byte
  - `Metropolitan`（パリ標準フランス語、/a/-/ɑ/統合、/œ̃/-/ɛ̃/合流）
  - `Conservative`（保守的標準フランス語、/a/-/ɑ/区別、/œ̃/-/ɛ̃/区別）

> **注**: `/ŋ/` は借用語のみに出現する周辺的音素であり、鼻音カテゴリではなく異音カテゴリに配置する（アーキテクチャ設計書と統一）。

**メインAPI**
- `FrenchG2PEngine` (sealed class, IDisposable)
  - `ToIPA(string text)` → IPA文字列
  - `ToPhonemes(string text)` → スペース区切り音素列
  - `ToPhonemeList(string text)` → `FrenchPhoneme[]`
  - `ToSyllables(string text)` → 音節分割済みIPA
  - バッチAPI: `ToIPABatch()`, `ToPhonemesBatch()`, `ToPhonemeListBatch()`, `ToSyllablesBatch()`
- `FrenchG2POptions`
  - `Dialect` (FrenchDialect, デフォルト: Metropolitan)
  - `IncludeStress` (bool, デフォルト: false)
  - `EnableAllophones` (bool, デフォルト: false)
  - `Separator` (string, デフォルト: " ")

> **注**: `IncludeStress` はフランス語では語レベルストレスを持たないため実質的に効果がない（`ToIPAWithoutStress` は `ToIPA` と同一出力）。スペイン語・英語とのAPI一貫性のためプロパティを保持し、デフォルト `false` とする。将来的に句ストレス対応を追加する場合のAPIフックとしても機能する。

**G2Pルールエンジン**
- `GraphemeToPhonemeRules` (static class)
  - **フェーズ1: マルチグラフ認識**（4文字→3文字→2文字の順）
    - 4文字: "eaux" → /o/
    - 3文字: "eau" → /o/, "ain" → /ɛ̃/, "ein" → /ɛ̃/, "oin" → /wɛ̃/, "oeu" → /ø/ or /œ/（位置の法則依存）, "sch" → /ʃ/, "ill" → /ij/ 等
    - 2文字: "ou" → /u/, "eu" → /ø|œ/, "oi" → /wa/, "ai" → /ɛ/, "ei" → /ɛ/, "au" → /o/, "an" → /ɑ̃/, "en" → /ɑ̃/, "in" → /ɛ̃/, "on" → /ɔ̃/, "un" → /œ̃/, "ch" → /ʃ/, "ph" → /f/, "gn" → /ɲ/, "qu" → /k/, "gu" + 前舌母音 → /ɡ/ 等
    - 接尾辞パターン: "-tion" → /sjɔ̃/, "-sion" → /zjɔ̃/, "-ssion" → /sjɔ̃/
    - `-ill-`/`-aille`/`-eille`/`-euille`/`-ouille` 系パターン: "-aille" → /aj/, "-eille" → /ɛj/, "-euille" → /œj/, "-ouille" → /uj/
  - **フェーズ2: 文脈依存規則**
    - "c" → /s/ (e,i,y前) / /k/ (その他)
    - "g" → /ʒ/ (e,i,y前) / /ɡ/ (その他)
    - "gu" + 前舌母音 → /ɡ/（例: "guerre" → /ɡɛʁ/）
    - "sc" + 前舌母音 → /s/（例: "science" → /sjɑ̃s/）
    - "s" → /z/ (母音間) / /s/ (その他)
    - "x" → /ɛɡz/ ("ex-" + 母音、例: "examen" → /ɛɡzamɛ̃/) / /ks/ (その他)
    - "n/m" → 鼻母音化 vs 子音 /n/, /m/ の判別
    - 語末子音の黙字処理（"CaReFuL" ルール: c, r, f, l は語末で発音されることが多い）
    - 語末 "-er" → /e/（動詞不定形等）、"-et" → /ɛ/、"-ed" → /e/（"pied" 等）、"-ez" → /e/
    - "e" の発音判別: /ə/, /e/, /ɛ/, 黙字
    - "h" の処理: h muet（無視）vs h aspiré（リエゾン阻止）
    - 語末 "-ent" のデフォルト動作: 鼻母音 /ɑ̃/ をデフォルトとし、動詞3人称複数活用形（黙字）は例外辞書で対応する（単語単位G2Pでは品詞判別が不可能なため、名詞/形容詞での頻度が高い鼻母音を優先）
  - **フェーズ3: 単純対応**
    - 残りの1文字→1音素マッピング

**音節分割**
- `FrenchSyllabifier` (static class)
  - 音素ベース音節分割（Onset Maximization Principle）
  - フランス語の許容onset cluster: /pl/, /bl/, /kl/, /ɡl/, /fl/, /pʁ/, /bʁ/, /tʁ/, /dʁ/, /kʁ/, /ɡʁ/, /fʁ/, /vʁ/ 等

**テスト**
- `tests/DotNetG2P.Tests/FrenchG2P/` ディレクトリ
  - `FrenchG2PEngineTests.cs`: エンジン統合テスト
  - `GraphemeToPhonemeRulesTests.cs`: G2Pルール単体テスト（各フェーズ）
  - `FrenchSyllabifierTests.cs`: 音節分割テスト
  - `FrenchIpaTests.cs`: IPA変換テスト
  - `FrenchPhonemeTests.cs`: 音素モデルテスト
- **目標**: 150-180件

#### 成果物
- 基本的なフランス語G2Pが動作（単語単位のIPA変換）
- 主要なフランス語正書法規則をカバー（F1a: 基本ルール、F1b: 鼻母音化・位置の法則・半母音化）

#### 依存関係
- なし（新規プロジェクト、他のマイルストーンに依存しない）

#### 目標PER
- **8-12%**（基本ルールのみ、例外辞書なし）

---

### F2: 精度向上・異音規則・テキスト正規化 ✅ 完了

#### ステータス
- **完了**（テスト366件合格）

#### 実装内容

**テキスト正規化**
- `FrenchNormalizer` (static class): 11段階正規化パイプライン
  1. NFC正規化 + 小文字化
  2. 略語展開（M./Mme/Mlle/Dr/Me/Prof/etc./p.ex./n°/St/Ste/av.J.-C./ap.J.-C.）
  3. 日付展開（DD/MM/YYYY, DD-MM-YYYY, DD.MM.YYYY → "le premier janvier deux mille vingt-six" 等、バリデーション付き）
  4. 時刻展開（NNhNN → "quatorze heures trente"、0h→minuit/12h→midi対応）
  5. 通貨展開（€/$ 前置・後置、整数部+小数部、単複形変化: euro/euros/centime/centimes/dollar/dollars/cent/cents）
  6. パーセント展開（N% → "N pour cent"、小数パーセントも対応）
  7. 単位展開（km/kg/cm/mm/m/l + °C、単複形変化: kilomètre/kilomètres 等）
  8. 小数展開（N,N → "N virgule N"、フランス語のカンマ小数点対応）
  9. 数字展開（残りの整数をフランス語数詞に変換）
  10. 記号展開（&→et, @→arobase, §→paragraphe, #→dièse, +→plus, =→égal）
  11. 空白正規化 + trim
- `Tokenize()`: アポストロフ・ハイフン保持のトークン分割

- `NumberToWords` (static class): フランス語数詞変換
  - **20進法（vigesimal）完全対応**: 70=soixante-dix, 71=soixante et onze, 80=quatre-vingts, 81=quatre-vingt-un, 90=quatre-vingt-dix
  - 序数詞: premier/première、N→Nième変換（neuf→neuv, cinq→cinqu 等の特殊変換）
  - 小数桁読み: `ConvertDigits()` による1桁ずつ読み上げ
  - 負数: "moins N"
  - 十億（milliard）単位まで対応
  - 百（cent/cents）・千（mille）の複数形ルール準拠

**異音規則**
- `AllophoneProcessor` (static class)
  - **R無声化**: 無声阻害音（/p/,/t/,/k/,/f/,/s/,/ʃ/）に隣接する /ʁ/ を /χ/ に変換（語末Rは除外）
  - **閉鎖音有声性同化**: 阻害音クラスタ内の逆行同化（後方の有声性に前方を統一: /p/↔/b/, /t/↔/d/, /k/↔/ɡ/, /f/↔/v/, /s/↔/z/, /ʃ/↔/ʒ/）
- `FrenchAllophoneFeatures` flags enum : byte（5規則、ON/OFF制御）
  - `RDevoicing`: /ʁ/ 無声化
  - `ObstruentVoicingAssimilation`: 阻害音有声性同化
  - `VowelLengthening`: 閉音節母音長化（オプション）
  - `LVelarization`: /l/ 軟口蓋化（オプション）
  - `FinalDevoicing`: 語末阻害音無声化（オプション）
  - `Obligatory = RDevoicing | ObstruentVoicingAssimilation`
  - `Default = Obligatory`

**例外辞書**
- `Data/french_exceptions.master.tsv`: 571行（ヘッダ・コメント含む、約550+エントリ）
  - 外来語、学術語・ラテン語由来、同綴異音語、不規則語をカバー
  - TSV形式: surface / dialect / category / stress_index / pronunciation / note
- `Data/FrenchExceptionDictionary.cs` (static class): 埋め込みリソースTSV読み込み + 方言別ルックアップ
  - `TryLookup(word, dialect, out pronunciation)`: 方言指定ルックアップ（方言固有→任意方言のフォールバック）
  - IPA音素パーサ（36音素対応）、音節区切り `|` 記法サポート

**F2で追加したファイル**
- `src/DotNetG2P.French/Normalization/NumberToWords.cs` — フランス語数詞変換（280行）
- `src/DotNetG2P.French/Normalization/FrenchNormalizer.cs` — 11段階正規化パイプライン（365行）
- `src/DotNetG2P.French/Rules/AllophoneProcessor.cs` — 異音規則処理（145行）
- `src/DotNetG2P.French/FrenchAllophoneFeatures.cs` — 異音規則flags enum（31行）
- `src/DotNetG2P.French/Data/FrenchExceptionDictionary.cs` — 例外辞書クラス（164行）
- `src/DotNetG2P.French/Data/french_exceptions.master.tsv` — 例外辞書TSVデータ（571行）
- `tests/DotNetG2P.Tests/FrenchG2P/FrenchNumberToWordsTests.cs` — 数詞変換テスト
- `tests/DotNetG2P.Tests/FrenchG2P/FrenchNormalizerTests.cs` — 正規化テスト
- `tests/DotNetG2P.Tests/FrenchG2P/AllophoneProcessorTests.cs` — 異音テスト
- `tests/DotNetG2P.Tests/FrenchG2P/FrenchExceptionDictionaryTests.cs` — 例外辞書テスト

#### 成果物
- 高精度フランス語G2P（正規化 + 例外辞書 + 異音規則）

#### 依存関係
- F1完了が前提

#### 目標PER
- **3-6%**（例外辞書により不規則語をカバー）

---

### F3: X-SAMPA・大規模精度評価・拡張テスト

#### スコープ

**X-SAMPA変換**
- `XSampaConverter` (static class)
  - `ToXSampa()`: ストレス付きX-SAMPA
  - `ToXSampaWithoutStress()`: ストレスなしX-SAMPA
  - `ToXSampaBatch()`: バッチAPI

**精度評価ツール**
- `tools/DotNetG2P.FrenchEval/`: 評価用コンソールプロジェクト
  - PER (Phoneme Error Rate) / WER (Word Error Rate) 計算
  - カテゴリ別集計（母音/子音/鼻母音/黙字/外来語）
  - エラー分析レポート生成
- `tools/refresh_french_eval_data.ps1`: ipa-dict/WikiPronからの評価データ取得
- `tools/run_french_full_evaluation.ps1`: 全量評価実行スクリプト
- `tools/french_eval_thresholds.json`: PER/WER閾値設定

**評価コーパス**
- ipa-dict (fr_FR): フランス語IPA辞書
- WikiPron (fra): Wiktionaryベース発音辞書

**テスト追加**
- `FrenchXSampaTests.cs`: X-SAMPA変換テスト
- `FrenchEdgeCaseTests.cs`: エッジケーステスト
  - 空文字列、null、特殊文字、超長文
  - アクセント付き文字（e, e, e, e, a, a, i, i, o, u, u, u, c）
  - ハイフン付き語（aujourd'hui, peut-etre）
  - アポストロフィ（l'homme, d'accord, n'est-ce pas）
- `FrenchPerformanceTests.cs`: パフォーマンステスト
  - スループット、バッチ比較、例外辞書初期化、メモリ
- `FrenchAccuracyTests.cs`: 精度・回帰テスト
  - ipa-dict PER回帰テスト
  - WikiPron PER回帰テスト
  - 高頻度語精度テスト
- **目標**: 80-100件追加（合計400-430件）

#### 成果物
- X-SAMPA出力対応
- 大規模コーパスでの精度評価結果
- PER回帰テストによる品質保証

#### 依存関係
- F2完了が前提

#### 目標PER
- **3-6%** (ipa-dict fr_FR)（F2からの変更なし。X-SAMPA追加がメインのため精度自体は変化しない）
- **5-8%** (WikiPron fra)

#### ipa-dict fr_FRとの評価整合方針

フランス語の ipa-dict はスペイン語（ルールベース生成、PER 0%が理論上可能）と異なり、人手転記を含む可能性がある。以下のバリエーションに対する正規化方針を定める:

| バリエーション | 方針 |
|:--|:--|
| /a/ vs /ɑ/ | Metropolitan方言（デフォルト）では統合されるため、評価時に /ɑ/ → /a/ に正規化して比較する |
| シュワー /ə/ の有無 | 本G2Pではシュワー保持（脱落予測なし）を基本方針とする。ipa-dictがシュワー脱落形を正解とする場合は正規化なしでPER計算し、シュワー関連のPER悪化は許容範囲として別途カテゴリ集計する |
| /œ̃/ vs /ɛ̃/ | Metropolitan方言では /œ̃/ → /ɛ̃/ に合流するため、評価時に /œ̃/ → /ɛ̃/ に正規化して比較する |

---

### F4: Multilingual統合・パッケージング

#### スコープ

**Multilingual統合**
- `Language.cs` に `French` 追加
- `ScriptKind.cs` に必要に応じて拡張
- `LanguageDetector` のフランス語判定強化
  - フランス語特有の文字パターン: e, e, e, a, u, i, o, c, oe, ae 等
  - 高頻度語リスト: "le", "la", "les", "de", "des", "un", "une", "est", "et", "en", "que", "qui" 等
  - 英語/スペイン語/フランス語のラテン文字振り分けロジック
- `TextSegmenter` のフランス語対応
  - ラテン文字テキスト内での言語判定精度向上
  - フランス語特有のアポストロフィ処理（l', d', n', qu', j' 等を語の一部として扱う）
  - `DefaultLatinLanguage` オプションとの連携
- `MultilingualG2PEngine` に `FrenchG2PEngine` 統合
  - `IDisposable` パターン（既存パターンに準拠）
  - lock保護によるスレッドセーフティ

**テスト追加**
- `MultilingualFrenchTests.cs`: フランス語統合テスト
  - 単純フランス語テキストの変換
  - `DefaultLatinLanguage = French` 設定での動作
  - Dispose後の例外テスト
- `MultilingualMixedLanguageTests.cs` への追加
  - 日仏混在: "東京の la tour Eiffel"
  - 英仏混在: "the weekend a Paris"
  - 西仏混在: "la casa de la mer"
  - 中仏混在: "巴黎 est une belle ville"
  - 5言語混在テスト
- **目標**: 40-50件追加

#### 成果物
- `DotNetG2P.Multilingual` でフランス語G2Pが利用可能
- 5言語（日英中西仏）混在テキストの処理

#### 依存関係
- F3完了が前提（F2完了時点で統合開始も可能だが、精度評価後が望ましい）

---

## 3.5 スコープ外（将来マイルストーン）

以下の機能はF1-F4のスコープ外とし、将来のマイルストーン（F5以降）として位置づける。

### リエゾン処理 (liaison)

- 語境界を超えた連結発音（例: "les amis" → /le.z‿ami/）
- 必須リエゾン6カテゴリ（限定詞+名詞、代名詞+動詞、前置形容詞+名詞、前置詞+名詞句、副詞+形容詞、est/sont+X）
- 禁止リエゾン（`et` の後、名詞主語+動詞、h aspiré語の前、`onze` の前）
- `h_aspire.txt` 埋め込みリスト（約200語）
- アーキテクチャ設計書では `LiaisonProcessor`（Phase2）として設計済み
- `FrenchG2POptions.EnableLiaison` フラグで制御（デフォルト: false）
- **将来のPER目標**: リエゾン対応後 2-4%

### アンシェヌマン (enchaînement)

- 語境界を超えた再音節化（例: "elle aime" → /ɛ.lɛm/）
- リエゾンより基本的な音韻現象だが、単語単位G2Pではスコープ外
- リエゾン対応と同時に検討する

> **注**: アーキテクチャ設計書では LiaisonProcessor を「Phase2」として詳細設計しており、F4完了後に追加マイルストーンとして実装可能。テスト戦略書の `LiaisonTests.cs` も将来マイルストーン用として位置づける。

---

## 4. リスクと対策

### 技術的リスク

| リスク | 影響度 | 対策 |
|:--|:-:|:--|
| 黙字規則の複雑さ（語末子音の発音/非発音判別） | 高 | "CaReFuL"ルールを基本とし、例外辞書で補完。LIA_Phonの規則体系を参考 |
| 鼻母音化の境界判定ミス（"n/m" が鼻母音の一部か独立子音か） | 高 | 後続文字パターンによる明確な判定ルール実装。"nn", "nm" 等の非鼻母音化パターンを網羅 |
| **ipa-dict fr_FRの品質・一貫性** | **高** | ipa-dictの転記方針がG2P出力と一致しない可能性（/a/ vs /ɑ/、シュワー保持/脱落、外来語の転記揺れ）。スペイン語ipa-dictはルールベース生成だったがフランス語は人手転記を含む可能性あり。F3の評価整合方針で正規化ルールを定義し、カテゴリ別集計で影響を分析する |
| 外来語の不規則発音 | 中 | 例外辞書（500-1000語）でカバー。頻出外来語を優先的に収録 |
| 位置の法則の精度（/e/ vs /ɛ/, /o/ vs /ɔ/） | 中 | 音節構造に基づく基本ルール + 接尾辞パターンによる補正 |
| ラテン文字言語間の判別精度（英/西/仏） | 中 | アクセント文字パターン + 高頻度語リストの組み合わせ。`DefaultLatinLanguage` によるフォールバック |
| **シュワーの脱落予測** | **中** | 本G2Pではシュワー保持（脱落予測なし）を基本方針とする。評価コーパスがシュワー脱落形を正解とする場合はPERが悪化する可能性がある。カテゴリ別集計で影響を分析し、F2以降で三子音の法則に基づく最小限のシュワー規則を検討する |
| **語末-entの品詞判別** | **中** | "parlent"（動詞→黙字）vs "accent"（名詞→/ɑ̃/）の区別は単語単位G2Pでは不可能。デフォルト鼻母音 /ɑ̃/ を採用し、動詞活用形は例外辞書で対応する |
| 同綴異音語の文脈判別 | 低 | 単語単位G2Pの制約上、例外辞書で最頻出発音を採用。将来の文脈解析拡張ポイントとして記録 |
| **テストデータの著作権** | **低** | ipa-dict fr_FR（MIT）はApache-2.0互換。WikiPronデータの利用条件（CC-BY-SA 3.0）を確認し、テストデータとしての利用がライセンス上許容されることを明示する |
| PER目標未達 | 低 | LIA_Phonの実績（99.3%ルールベース）から、ルール+例外辞書で3-6%は十分達成可能。ただしLIA_Phonは数千の手作業規則を持つ大規模システムであり、500-1000語の例外辞書+簡潔なルール体系でこのレベルに到達するのは困難。PER 3-6%目標はルール+例外辞書の規模を考慮すると現実的 |

### スケジュールリスク

| リスク | 対策 |
|:--|:--|
| G2Pルールの規模がスペイン語より大きい | フランス語はスペイン語（S1-S4）より複雑だが英語（E1-E6）より単純。S1-S4と同等の4マイルストーン構成で実現可能 |
| 例外辞書の構築工数 | ipa-dictからの自動抽出ツール（generate_french_exceptions.ps1）で効率化 |
| 評価コーパスの準備 | スペイン語S3の評価パイプライン（refresh/run/thresholds）を再利用 |

---

## 5. 既存言語との比較

### アーキテクチャ比較

| 項目 | スペイン語 (S1-S4) | 英語 (E1-E6) | フランス語 (F1-F4) |
|:--|:--|:--|:--|
| **アプローチ** | ルールベース | 辞書+LTS CARTツリー | ルールベース+例外辞書 |
| **マイルストーン数** | 4 | 6 | 4 |
| **音素数** | 35 (byte) | 39 (byte) | ~40 (byte) |
| **方言** | 2 (LatinAmerican, Castilian) | 1 (General American) | 1+1 (Metropolitan, Conservative) |
| **辞書依存** | 例外辞書のみ | CMU辞書 (135K語) | 例外辞書のみ (500-1000語) |
| **正規化** | 数字/日付/時刻/単位/略語/記号 | 数字/通貨/時刻/略語/頭字語/記号 | 数字(20進法)/通貨/時刻/日付/単位/略語/記号 |
| **特有の難しさ** | 方言差異、ü処理 | OOV語、同綴異音語 | 黙字、鼻母音化、外来語 |
| **テスト目標** | 355件 | 511件 | 400-430件 |

### パイプライン比較

```
[スペイン語]
  Normalize → Tokenize → G2PRules → Syllabifier → StressAssigner → AllophoneProcessor → Format

[英語]
  Normalize → Tokenize → HomographResolve → DictLookup/LTS → Format

[フランス語]
  Normalize → Tokenize → ExceptionLookup → G2PRules → Syllabifier → AllophoneProcessor → Format
```

フランス語パイプラインはスペイン語に最も近いが、例外辞書ルックアップを G2P ルール適用前に挟む点が異なる。英語のような大規模辞書やCARTツリーは不要。

### コード再利用

以下のパターンをスペイン語G2Pから流用可能:
- プロジェクト構成（csproj, package.json, asmdef）
- `IpaPhoneme` enum + `Phoneme` struct + `Pronunciation` class のモデル設計
- `Syllabifier` の Onset Maximization アルゴリズム
- `AllophoneProcessor` の flags enum パターン
- `XSampaConverter` のアーキテクチャ
- `Normalizer` のカテゴリ別展開パターン
- 評価ツール（FrenchEval）のスクリプト群
- テストの構成パターン

---

## 6. 成功基準

### 機能要件
- [ ] 単語単位のフランス語G2P変換が正しく動作する
- [ ] IPA / X-SAMPA / 音節分割の3形式で出力できる
- [ ] テキスト正規化（数字・通貨・時刻・日付・単位・略語・記号）が動作する
- [ ] 例外辞書による不規則語の正しい変換
- [ ] `DotNetG2P.Multilingual` で5言語混在テキストを処理できる

### 品質要件
- [ ] PER 3-6% (ipa-dict fr_FR)
- [ ] PER 5-8% (WikiPron fra)
- [ ] テスト400-430件、全件通過
- [ ] パフォーマンス: 1000語/秒以上のスループット

### パッケージング要件
- [ ] NuGet パッケージ (`DotNetG2P.French`) が生成できる
- [ ] UPM パッケージ (`com.dotnetg2p.french`) として利用できる
- [ ] .NET Standard 2.1 準拠（Unity 2021.2+互換）
- [ ] 外部依存なし（独立パッケージ）
- [ ] Apache-2.0 ライセンス

### プロジェクト構成（最終形）
```
src/DotNetG2P.French/
  ├── DotNetG2P.French.csproj
  ├── FrenchG2PEngine.cs
  ├── FrenchG2POptions.cs
  ├── Models/
  │   ├── FrenchIpaPhoneme.cs
  │   ├── FrenchPhoneme.cs
  │   ├── FrenchPronunciation.cs
  │   └── FrenchDialect.cs
  ├── Rules/
  │   ├── GraphemeToPhonemeRules.cs
  │   ├── SyllableParser.cs
  │   └── AllophoneProcessor.cs
  ├── Normalization/
  │   └── FrenchNormalizer.cs
  ├── Conversion/
  │   ├── IpaConverter.cs
  │   └── XSampaConverter.cs
  ├── Dictionary/
  │   └── ExceptionDictionary.cs
  ├── Data/
  │   └── french_exceptions.master.tsv
  ├── package.json
  └── DotNetG2P.French.asmdef

tests/DotNetG2P.Tests/FrenchG2P/
  ├── FrenchG2PEngineTests.cs
  ├── GraphemeToPhonemeRulesTests.cs
  ├── FrenchSyllabifierTests.cs
  ├── FrenchIpaTests.cs
  ├── FrenchPhonemeTests.cs
  ├── FrenchNormalizerTests.cs
  ├── AllophoneProcessorTests.cs
  ├── ExceptionDictionaryTests.cs
  ├── FrenchXSampaTests.cs
  ├── FrenchEdgeCaseTests.cs
  ├── FrenchPerformanceTests.cs
  └── FrenchAccuracyTests.cs
```
