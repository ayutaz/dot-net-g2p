# ポルトガル語G2P マイルストーン設計

## 概要

本ドキュメントは、調査タスク #1〜#9 の成果を統合し、ポルトガル語G2P (`DotNetG2P.Portuguese`) の実装マイルストーンを設計する。スペイン語 (S1-S4) とフランス語 (F1-F4) の4段階マイルストーン構造を踏襲する。

### ポルトガル語G2Pの特性まとめ

| 特性 | スペイン語 | フランス語 | ポルトガル語 |
|------|----------|----------|------------|
| G2Pルール複雑度 | 低（3フェーズ） | 高（6フェーズ） | 高（5フェーズ + AllophoneProcessorで母音弱化） |
| 母音体系 | 5口母音 | 12口母音+4鼻母音 | 7-9口母音+5鼻母音 |
| 鼻母音 | なし | 4種 | 5種+鼻二重母音5種 |
| 母音弱化 | なし | schwa処理 | 大規模（EP/BP差異大） |
| 方言差の影響 | 小 | 小 | 大（音韻体系が異なる） |
| 例外辞書依存度 | 低（50-100件） | 中〜高（500+件） | 中〜高（500-1000件推定） |
| 異音規則数 | 5フラグ（byte） | 5フラグ（byte） | 7フラグ（byte） |
| enum音素数 | 35種 | 40種 | 49種 |

### 09_codebase_patterns.md の実装推奨順序との関係

09_codebase_patterns.md のセクション14では5段階（P1-P5）の実装推奨順序が提案されている（P1: Models + 基本ルール、P2: NasalVowelizer + AllophoneProcessor、P3: Normalizer + ExceptionDictionary、P4: Converter + 精度評価、P5: Multilingual統合）。本ドキュメントでは調査段階の5段階提案を統合し、以下の4段階に再構成した:

- 09のP1-P2 → 本ドキュメントのP1（コアG2P + NasalVowelizer + IpaConverter をMVPに含める）
- 09のP3 → 本ドキュメントのP2（Normalizer + AllophoneProcessor + ExceptionDictionary）
- 09のP4 → 本ドキュメントのP3（X-SAMPA + 精度評価）
- 09のP5 → 本ドキュメントのP4（Multilingual統合）

主な変更理由は、NasalVowelizer と IpaConverter がMVP段階で必要不可欠な機能であり、AllophoneProcessor への母音弱化統合（05_allophone_rules.md 準拠）によりP2のスコープが自然に拡大したため。

---

## P1: コアG2Pルールエンジン + 基本MVP

### 目標

ポルトガル語の基本的な書記素→IPA音素変換が動作するMVPを構築する。音節分割、ストレス位置決定、IPA出力を含む。

### 成果物リスト

#### プロジェクト構成（6ファイル）
- `src/DotNetG2P.Portuguese/DotNetG2P.Portuguese.csproj` — .NET Standard 2.1、独立パッケージ
- `src/DotNetG2P.Portuguese/package.json` — UPM `com.dotnetg2p.portuguese`
- `src/DotNetG2P.Portuguese/DotNetG2P.Portuguese.asmdef` — Unity Assembly Definition
- `DotNetG2P.slnx` — ソリューションにプロジェクト追加
- `tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj` — ProjectReference追加

#### モデル定義（5ファイル）
- `Models/PortugueseIpaPhoneme.cs` — IPA音素 enum : byte（49種）
  - 口母音9 + 鼻母音5 + 半母音2 + 破裂音6 + 摩擦音6 + 鼻音3 + 側面音2 + ロティック2 + BP異音4 + EP異音2 + 共通異音3 + 弱化異音3（Beta/Dh/Gh） + 鼻わたり音2（WNasal/JNasal）
- `Models/PortuguesePhoneme.cs` — readonly struct（Phoneme + IsStressed）
- `Models/PortuguesePronunciation.cs` — 発音クラス（音素配列 + 音節オフセット + ストレス位置）
- `Models/PortugueseDialect.cs` — 方言 enum : byte（Brazilian=0, European=1）
- `Models/PortugueseSyllable.cs` — 音節 readonly struct（StartIndex, Length, Text, IsStressed）

#### エンジン（2ファイル）
- `PortugueseG2PEngine.cs` — sealed class, IDisposable
  - `ToIPA()`, `ToIPAWithoutStress()`, `ToPhonemes()`, `ToPhonemeList()`, `ToSyllables()`
  - バッチAPI: `ToIPABatch()`, `ToPhonemesBatch()`, `ToPhonemeListBatch()`
- `PortugueseG2POptions.cs` — Dialect, IncludeStress, EnableAllophones, EnableTextNormalization, EnableExceptionDictionary, Separator

#### ルールエンジン（5ファイル）
- `Rules/GraphemeToPhonemeRules.cs` — コアG2Pルール（5フェーズ）
  - Phase 1: ダイグラフ・マルチグラフ認識 + 鼻母音化（ch→/ʃ/, lh→/ʎ/, nh→/ɲ/, rr, ss, qu, gu, xc, sc + ã/õ, 母音+n/m, 鼻二重母音 ão/ãe/õe/em。フランス語実装同様、マルチグラフ認識に鼻母音化を統合）
  - Phase 2: 文脈依存子音（s/z位置交替, c/ç/g/j, x 4通り, r強弱）
  - Phase 3: 母音変換（ストレス依存 e→/e/~/ɛ/, o→/o/~/ɔ/, アクセント記号処理）
  - Phase 4: 半母音化（上昇・下降二重母音, 三重母音）
  - Phase 5: 黙字処理（語頭h）
  - **注**: 母音弱化（EP: e→/ɨ/, o→/u/; BP: 語末 e→/i/, o→/u/）はP2の AllophoneProcessor.VowelReduction で処理する。05_allophone_rules.md の設計に準拠し、ストレス確定後に AllophoneProcessor の最初のステップとして適用する
- `Rules/PortugueseOrthography.cs` — 正書法ヘルパー
  - 母音判定（ã, õ, â, ê, ô 含む）、強母音/弱母音分類、アクセント記号判定、ダイグラフ判定
  - 二重母音/三重母音/分離母音判定、サイレントu判定
- `Rules/NasalVowelizer.cs` — 鼻母音化ロジック
  - フランス語 `NasalVowelizer` を拡張: 鼻二重母音検出（語末 -ão/-am/-ãe/-õe/-em/-ens）
  - チルダ付き母音の直接処理（ã→/ɐ̃/, õ→/õ/）
  - 鼻母音化判定（母音+n/m+子音or語末 → 鼻母音化、母音+n/m+母音 → 非鼻母音化）
- `Rules/PortugueseSyllabifier.cs` — 正書法ベース音節分割
  - スペイン語 `SpanishSyllabifier` 方式: onset maximization
  - 有効 onset クラスタ: obstruent + /ɾ/ or /l/（/tl/ 不許容）
  - ダイグラフ onset: ch, lh, nh, rr, qu, gu（旧正書法の gü も互換性のため対応。2009年正書法改定でトレマは廃止されたが、旧テキスト処理のため gü→gu として扱う）
  - 二重母音/三重母音の一体処理、分離母音（hiatus）の分割
- `Rules/StressAssigner.cs` — ストレス位置決定
  - Phase 1: アクセント記号検索（á/é/í/ó/ú/â/ê/ô）
  - Phase 2: チルダ検索（ã/õ — 唯一のアクセントの場合のみストレス）
  - Phase 3: デフォルトルール（-a(s)/-e(s)/-o(s)/-am/-em/-ens → 次末音節、それ以外 → 最終音節）

#### IPA変換（1ファイル）
- `Conversion/IpaConverter.cs` — enum → IPA Unicode 文字列マッピング（49音素）

### P1テスト計画

| テストファイル | 対象 | 目標件数 |
|-------------|------|---------|
| PortugueseG2PEngineTests.cs | エンジン統合テスト | 30-40件 |
| GraphemeToPhonemeRulesTests.cs | G2Pルール単体テスト | 80-120件 |
| PortugueseSyllabifierTests.cs | 音節分割テスト | 30-40件 |
| StressAssignerTests.cs | ストレステスト | 25-35件 |
| PortugueseIpaTests.cs | IPA変換テスト | 35-45件 |
| PortuguesePhonemeTests.cs | 音素モデルテスト | 25-30件 |
| PortugueseOrthographyTests.cs | 正書法テスト | 40-60件 |
| NasalVowelizerTests.cs | 鼻母音化テスト | 40-60件 |
| **P1合計** | | **305-430件** |

### P1 PER目標

- MVP段階のためPER閾値テストは設定しない
- 手動テストケースで基本的な変換精度を確認

### P1難易度・リスク

**難易度: 高**

ポルトガル語G2Pはスペイン語より大幅に複雑で、フランス語と同等の難易度を持つ。母音弱化をP2に移動したことでP1のスコープはフランス語F1（6フェーズ）と同程度（5フェーズ）に軽減されたが、鼻母音化の複雑さが追加される。主なリスク:

1. **鼻母音化の複雑さ**: 5種の鼻母音 + 5種の鼻二重母音の正確な検出。フランス語にない鼻二重母音（ão, ãe, õe, em）の処理が追加で必要
2. **開/閉母音の曖昧性**: アクセント記号なしのストレス e/o の開閉が正書法だけでは決定できない（例外辞書への依存度が高い）
3. **x の不規則性**: 4通りの発音（/ʃ/, /z/, /ks/, /s/）が位置・語源に依存し、ルールだけでは解決困難
4. **5フェーズの相互依存**: フェーズ間の適用順序が重要で、特に鼻母音化→母音変換→半母音化の順序制約。母音弱化はP2のAllophoneProcessorに移動したためP1の複雑性は軽減されるが、ストレス確定後の処理順序設計には注意が必要

**リスク緩和策**:
- P1では開/閉母音のデフォルト値（閉母音）で実装し、P2の例外辞書で精度向上
- x は基本ルール（語頭→/ʃ/, ex+母音→/z/, デフォルト→/ʃ/）で対応し、/ks//s/は例外辞書
- フランス語 `NasalVowelizer` の構造を拡張して鼻母音化を実装
- 母音弱化をP2に移動したことでP1のスコープを軽減。P1が難航した場合のさらなる分割余地を確保

---

## P2: 精度向上・異音規則・テキスト正規化

### 目標

テキスト正規化、異音規則、例外辞書を追加し、G2Pの精度と実用性を大幅に向上させる。

### 成果物リスト

#### テキスト正規化（2ファイル）
- `Normalization/PortugueseNormalizer.cs` — 13段階正規化パイプライン
  - NFC正規化 → 略語展開 → ISO日付 → 日付(DD/MM/YYYY) → 時刻(NNhNN, NN:NN) → パーセント → 通貨(R$/EUR/$) → 単位 → 数値範囲 → 小数 → 数値 → 記号 → 空白正規化
  - 方言対応: BP/EP の通貨・数詞の差異
  - 接語（clitic）のハイフン保持
  - `Tokenize()`, `TokenizeNormalized()` メソッド
- `Normalization/NumberToWords.cs` — ポルトガル語数詞変換
  - 完全10進法（フランス語の20進法とは異なる）
  - cem/cento 規則（100 → "cem", 101+ → "cento e ..."）
  - 「e」接続詞の複雑な使用規則
  - 性数一致: um/uma, dois/duas, -centos/-centas
  - 方言差: dezesseis/dezasseis, quatorze/catorze, bilhao/mil milhoes
  - `Convert()`, `ConvertAttributed()`, `ConvertDigits()`

#### 異音規則（2ファイル）
- `PortugueseAllophoneFeatures.cs` — [Flags] enum : byte（7フラグ）
  - **必須規則**: NasalAssimilation, SibilantVoicingAssimilation, VowelReduction
  - **可変規則**: Lenition, SibilantPalatalization, TDPalatalization, LAllophony（BP: l半母音化[w] / EP: l軟口蓋化[ɫ]を方言で自動切替、排他的1フラグ）, RhoticDebuccalization
  - **プリセット**: Obligatory, EuropeanDefault, BrazilianDefault, All
  - 05_allophone_rules.md の設計に準拠し、byte 基底型でスペイン語・フランス語と統一。LVocalization と LVelarization は排他的なため単一フラグ LAllophony に統合
- `Rules/AllophoneProcessor.cs` — 異音規則プロセッサ
  - 適用順序: 母音弱化（VowelReduction） → t/d破擦音化 → 閉鎖音弱化 → 鼻音同化 → 歯擦音有声性同化 → 歯擦音後部歯茎化 → l異音（LAllophony: 方言自動切替） → ロティック実現形選択
  - EP: 弱化[β,ð,ɣ] + 母音弱化[e→ɨ, o→u] + 後部歯茎coda[ʃ,ʒ] + 暗いL[ɫ] + ロティック[ʁ]
  - BP: 母音弱化[語末e→i, o→u] + 口蓋化[tʃ,dʒ] + l半母音化[w] + ロティック[h]
  - **注**: 母音弱化はP1のGraphemeToPhonemeRulesではなくAllophoneProcessorの最初のステップとして適用する。ストレス確定後に方言フラグに基づき弱化先を切り替える。t/d破擦音化は弱化後の母音（語末e→[i]）を参照するため、母音弱化の後に適用する必要がある

#### 例外辞書（2ファイル）
- `Data/PortugueseExceptionDictionary.cs` — 例外辞書ルックアップ
  - フランス語 `FrenchExceptionDictionary` と同パターン
  - 方言固有エントリ → any-dialect エントリの順で検索
  - EmbeddedResource からTSV読み込み
- `Data/portuguese_exceptions.master.tsv` — 例外辞書TSV（500-1000エントリ目標）
  - 開/閉母音の不規則語（belo→/ɛ/, pelo→/e/ 等）
  - x の不規則発音（exame→/z/, fixo→/ks/, proximo→/s/）
  - 外来語（show, pizza, shopping 等）
  - Metaphony語（novo/nova/novos, ovo/ovos 等）— Metaphony語は1語あたり2-4形態（男性/女性/単数/複数）が必要で、100語の基本語で200-400エントリを要するため、例外辞書サイズの主要因となる
  - 不規則動詞活用形

#### 評価ツール生成スクリプト（1ファイル）
- `tools/generate_portuguese_exceptions.ps1` — 例外辞書生成・管理スクリプト

### P2テスト計画

| テストファイル | 対象 | 目標件数 |
|-------------|------|---------|
| PortugueseNormalizerTests.cs | 正規化テスト | 45-60件 |
| NumberToWordsTests.cs | 数値変換テスト | 50-65件 |
| AllophoneProcessorTests.cs | 異音テスト | 30-40件 |
| PortugueseExceptionDictionaryTests.cs | 例外辞書テスト | 20-30件 |
| **P2合計** | | **145-195件** |
| **P1+P2累計** | | **450-625件** |

### P2 PER目標

P2完了時の暫定PER（ipa-dict pt_BR で手動サンプル評価）:
- base（異音なし）: 5-8% 目標
- allophones: 4-6% 目標

P2には例外辞書（500-1000エントリ）と異音規則（母音弱化含む）が含まれるため、例外辞書の効果が十分に反映された目標値を設定する。スペイン語のS2完了時点で既にPER 2%以下だった実績を踏まえ、ポルトガル語の開/閉母音の曖昧性を考慮しても5-8%を暫定目標とする。P2完了時点で ipa-dict pt_BR から100-200語をランダムサンプリングした小規模PER評価を実施し、例外辞書の方向性を早期に検証する。

### P2難易度・リスク

**難易度: 中〜高**

1. **NumberToWords の「e」接続規則**: ポルトガル語の数詞接続は複雑（千の位と百の位の間は通常「e」なし、ただし100の場合は「e」あり等）。スペイン語・フランス語より規則が多い
2. **異音規則の適用順序**: 7つのフラグの相互作用。特に母音弱化→口蓋化の連鎖（語末e→[i]が先、その後t/d+[i]→[tʃ/dʒ]）が正しい順序で適用される必要がある
3. **例外辞書のサイズ**: 開/閉母音の曖昧性により、スペイン語よりも大幅に多いエントリが必要。初期500-1000件で開始し、PER評価結果に基づいて拡充。Metaphony語（名詞・形容詞の性数変化に伴う語幹母音の開閉変化）は正書法に反映されないため、1語あたり2-4形態が必要で例外辞書エントリ数に大きく影響する。頻出Metaphonyパターンにルールベース対応を検討することで辞書サイズを抑制できる可能性がある
4. **coda s の処理分離**: coda s の基本出力（/s/ 歯茎音）は GraphemeToPhonemeRules で処理し、異音変換（EP: [ʃ] 後部歯茎化、有声性同化）は AllophoneProcessor で処理する。02_g2p_rules.md と 05_allophone_rules.md の両方に記載があるため、二重処理にならないよう設計に注意が必要

---

## P3: X-SAMPA・大規模精度評価・拡張テスト

### 目標

X-SAMPA出力対応、外部コーパスによる大規模PER評価、エッジケース・パフォーマンステストにより、品質を検証・保証する。

### 成果物リスト

#### X-SAMPA変換（1ファイル）
- `Conversion/XSampaConverter.cs` — 49音素のX-SAMPAマッピング
  - `ToXSampa()`, `ToXSampaWithoutStress()`, `ToXSampaBatch()` をエンジンに追加

#### 評価ツール（4ファイル）
- `tools/DotNetG2P.PortugueseEval/DotNetG2P.PortugueseEval.csproj` — 全量精度評価コンソール
- `tools/refresh_portuguese_eval_data.ps1` — 評価データ取得・フィルタリング
- `tools/run_portuguese_full_evaluation.ps1` — 全量PER/WER/カテゴリ別集計
- `tools/portuguese_eval_thresholds.json` — PER閾値定義

#### テストデータ（サンプル）
- `tests/TestData/PortugueseG2P/ipa_dict_pt_br_sample.tsv` — ipa-dict BPサンプル（256-500件）
- `tests/TestData/PortugueseG2P/wikipron_bp_sample.tsv` — WikiPron BPサンプル
- `tests/TestData/PortugueseG2P/wikipron_ep_sample.tsv` — WikiPron EPサンプル

### 評価データセット

| # | データセット | ソース | 方言 | 用途 |
|---|---|---|---|---|
| 1 | `ipa_dict_pt_br` | ipa-dict `pt_BR.txt` | BP | PER回帰テスト（BP） |
| 2 | `wikipron_bp_broad` | WikiPron `por_latn_bz_broad_filtered.tsv` | BP | PER回帰テスト（BP） |
| 3 | `wikipron_ep_broad` | WikiPron `por_latn_po_broad_filtered.tsv` | EP | PER回帰テスト（EP） |

### P3テスト計画

| テストファイル | 対象 | 目標件数 |
|-------------|------|---------|
| PortugueseXSampaTests.cs | X-SAMPA変換テスト | 50-65件 |
| PortugueseEdgeCaseTests.cs | エッジケーステスト | 30-40件 |
| PortuguesePerformanceTests.cs | パフォーマンステスト | 10-15件 |
| PortugueseAccuracyTests.cs | 精度・回帰テスト | 25-35件 |
| PortugueseDatasetEvaluationTests.cs | 外部コーパスPER閾値テスト | 6-9件 |
| PortugueseAllophoneEvaluationTests.cs | 異音プロファイル別PER評価 | 6-9件 |
| **P3合計** | | **127-173件** |
| **P1+P2+P3累計** | | **577-798件** |

### P3 PER目標値

ポルトガル語はスペイン語より開/閉母音の曖昧性が大きく、鼻母音化も複雑なため、PER目標はスペイン語よりやや緩く設定する。

| データセット | base PER 目標 | allophones PER 目標 | 備考 |
|---|---|---|---|
| ipa-dict pt_BR | **3.0-5.0%** | **2.5-4.0%** | BP方言、ルールベース生成データ |
| WikiPron BP broad | **3.5-5.5%** | — | Wiktionary由来、人手検証データ含む |
| WikiPron EP broad | **4.0-6.0%** | — | EP方言、母音弱化の評価精度が律速。WikiPronのみに依存するため参考値。P3で実際のデータ品質を確認後に再設定する |

**PER律速要因の分析**:
- **開/閉母音 (e/ɛ, o/ɔ)**: 正書法だけでは予測不可能なケースが多く、例外辞書のカバレッジが直接PERに影響。推定PER寄与: 1.0-2.0%
- **x の不規則性**: 4通りの発音のうち /ks/ と /s/ はルールで予測困難。推定PER寄与: 0.3-0.8%
- **鼻母音化の境界ケース**: 鼻母音vs鼻子音の判定境界が曖昧なケース。推定PER寄与: 0.2-0.5%
- **母音弱化の程度**: EP/BPの弱化パターンがコーパスの転写と合致しないケース。推定PER寄与: 0.5-1.5%

**スペイン語実績との比較**:
- スペイン語 ipa-dict: 1.69% (base) / 1.37% (allophones)
- ポルトガル語は開/閉母音の曖昧性により +1.5-3.0% のPER増加を見込む
- 例外辞書の拡充（500→1000件）により2.5%以下を目指すことも将来的に可能。特にMetaphony語の体系的カバレッジが鍵

### P3難易度・リスク

**難易度: 中**

1. **ipa-dict にEPデータなし**: EP方言の評価は WikiPron のみに依存。EP評価の信頼性がデータセット品質に左右される
2. **コーパスの転写表記揺れ**: ipa-dict と WikiPron で鼻母音の表記（/ɐ̃w̃/ vs /ɐ̃ũ/ 等）や母音弱化の表記が異なる可能性。正規化ルールの調整が必要
3. **X-SAMPA の45音素マッピング**: ポルトガル語固有の鼻母音・鼻わたり音のX-SAMPA表記に標準化されていない部分がある

---

## P4: Multilingual統合・パッケージング

### 目標

`DotNetG2P.Multilingual` にポルトガル語を統合し、6言語（日英中西仏葡）対応の多言語G2Pエンジンを完成させる。

### 成果物リスト

#### Multilingual 変更（5ファイル変更）
- `src/DotNetG2P.Multilingual/Language.cs` — `Portuguese = 5` 追加
- `src/DotNetG2P.Multilingual/MultilingualG2POptions.cs` — `PortugueseOptions` プロパティ追加
- `src/DotNetG2P.Multilingual/MultilingualG2PEngine.cs` — `_portugueseEngine` フィールド追加、Dispose統合
- `src/DotNetG2P.Multilingual/TextSegmenter.cs` — ポルトガル語言語判定シグナル追加
  - 高頻度語シグナル: "nao", "uma", "muito", "voce", "tambem", "aqui", "isso", "este", "aquele", "seu", "sua", "quando", "ja", "pode", "bem", "tem", "foi", "essa" 等（ポルトガル語固有語を優先。"de", "que", "como", "para", "mais" 等の多言語共通語はスペイン語/フランス語等でも出現するため、判定シグナルとしての重みを低く設定する）
  - 接尾辞シグナル: "-ção", "-ções", "-mente", "-dade", "-ável", "-ível", "-oso", "-osa", "-eiro", "-eira", "-ismo", "-ista" 等
  - 特有文字シグナル: ã, õ, ç（ã/õ はポルトガル語に非常に特有）
  - ラテン文字言語の判定優先度: ポルトガル語固有文字 (ã/õ) → スペイン語固有文字 (ñ/¿/¡) → フランス語固有文字 (œ/æ/«/»等) → 高頻度語マッチ → デフォルト (DefaultLatinLanguage)
- `src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.csproj` — ProjectReference追加

#### テスト（3ファイル）
- `tests/DotNetG2P.Tests/Multilingual/MultilingualPortugueseTests.cs` — ポルトガル語統合テスト
- `tests/DotNetG2P.Tests/Multilingual/MultilingualMixedLanguageTests.cs` — 6言語混在テストに拡張
- 既存テストの回帰確認

### P4テスト計画

| テストファイル | 対象 | 目標件数 |
|-------------|------|---------|
| MultilingualPortugueseTests.cs | ポルトガル語統合テスト | 20-30件 |
| MultilingualMixedLanguageTests.cs | 6言語混在テスト（拡張） | 10-15件 |
| **P4合計** | | **30-45件** |
| **P1+P2+P3+P4累計** | | **607-843件** |

### TextSegmenter のポルトガル語 vs スペイン語 判定戦略

ポルトガル語とスペイン語は同じラテン文字を使い、語彙的にも類似度が高いため、言語判定が課題となる。

**判定戦略**:
1. **固有文字による確定判定**:
   - ã, õ → ポルトガル語確定（スペイン語・フランス語には存在しない）
   - ñ, ¿, ¡ → スペイン語確定（ポルトガル語には存在しない）
   - ç はフランス語・ポルトガル語の両方に存在するため確定判定には不向き
2. **高頻度語による確率判定**:
   - ポルトガル語固有: "não", "são", "você", "também", "então", "muito", "aqui", "isso", "este"
   - スペイン語固有: "pero", "como"（英語と重複に注意）, "muy", "ahora", "aquí"（アクセント付き）
3. **接尾辞による判定**:
   - ポルトガル語: -ção, -ções, -mente, -dade, -ável
   - スペイン語: -ción, -ciones, -mente（重複）, -dad, -able
4. **DefaultLatinLanguage によるフォールバック**: 判定不能な場合はユーザー指定のデフォルト言語

### P4難易度・リスク

**難易度: 中**

1. **ポルトガル語/スペイン語の言語判定**: 語彙的類似度が高く、短いテキストでは判定困難。ã/õ がない場合はスペイン語と区別しにくい
2. **6言語Dispose管理**: エンジン数が増加するためメモリ管理に注意。Shared fixture パターンの活用
3. **DefaultLatinLanguage の振る舞い**: 英語・スペイン語・フランス語・ポルトガル語の4つのラテン文字言語があり、判定ロジックの優先度設計が重要
4. **既存テストの回帰**: Multilingual テスト372件 + 各言語テストの回帰確認

---

## 全体スケジュールまとめ

| マイルストーン | 成果物ファイル数 | テスト目標件数 | PER目標 | 難易度 |
|-------------|---------------|-------------|---------|--------|
| **P1**: コアG2Pルール + MVP | 19ファイル | 305-430件 | — | 高 |
| **P2**: 異音・正規化・例外辞書 | 7ファイル | 145-195件 | 5-8% (暫定) | 中〜高 |
| **P3**: X-SAMPA・精度評価 | 8ファイル + データ | 127-173件 | 3.0-5.0% (base) | 中 |
| **P4**: Multilingual統合 | 5ファイル変更 + 3テスト | 30-45件 | — | 中 |
| **合計** | 39+ ファイル | **607-843件** | | |

### 最終ファイル構成

```
src/DotNetG2P.Portuguese/                    # メインパッケージ (22ファイル)
├── DotNetG2P.Portuguese.csproj
├── DotNetG2P.Portuguese.asmdef
├── package.json
├── PortugueseG2PEngine.cs
├── PortugueseG2POptions.cs
├── PortugueseAllophoneFeatures.cs
├── Models/
│   ├── PortugueseIpaPhoneme.cs              # 49種 : byte
│   ├── PortuguesePhoneme.cs                 # readonly struct
│   ├── PortuguesePronunciation.cs
│   ├── PortugueseDialect.cs                 # Brazilian=0, European=1
│   └── PortugueseSyllable.cs
├── Rules/
│   ├── GraphemeToPhonemeRules.cs             # 5フェーズ（母音弱化はAllophoneProcessorで処理）
│   ├── PortugueseOrthography.cs
│   ├── NasalVowelizer.cs                    # 鼻母音化 + 鼻二重母音
│   ├── PortugueseSyllabifier.cs             # 正書法ベース音節分割
│   ├── StressAssigner.cs
│   └── AllophoneProcessor.cs                # 7規則（母音弱化含む、byte基底）
├── Normalization/
│   ├── PortugueseNormalizer.cs              # 13段階パイプライン
│   └── NumberToWords.cs                     # 10進法 + cem/cento + 性数一致
├── Conversion/
│   ├── IpaConverter.cs
│   └── XSampaConverter.cs
└── Data/
    ├── PortugueseExceptionDictionary.cs
    └── portuguese_exceptions.master.tsv     # 500-1000エントリ（Metaphony語の体系的カバレッジ含む）

tests/DotNetG2P.Tests/PortugueseG2P/         # テスト (18ファイル)
├── PortugueseG2PEngineTests.cs
├── GraphemeToPhonemeRulesTests.cs
├── PortugueseSyllabifierTests.cs
├── StressAssignerTests.cs
├── PortugueseIpaTests.cs
├── PortuguesePhonemeTests.cs
├── PortugueseOrthographyTests.cs
├── PortugueseNormalizerTests.cs
├── NumberToWordsTests.cs
├── AllophoneProcessorTests.cs
├── NasalVowelizerTests.cs
├── PortugueseExceptionDictionaryTests.cs
├── PortugueseXSampaTests.cs
├── PortugueseEdgeCaseTests.cs
├── PortuguesePerformanceTests.cs
├── PortugueseAccuracyTests.cs
├── PortugueseDatasetEvaluationTests.cs
└── PortugueseAllophoneEvaluationTests.cs

tests/DotNetG2P.Tests/Multilingual/          # Multilingual拡張
├── MultilingualPortugueseTests.cs           # 新規
└── MultilingualMixedLanguageTests.cs        # 6言語対応に拡張

tools/                                        # 評価ツール (4ファイル)
├── DotNetG2P.PortugueseEval/
│   └── DotNetG2P.PortugueseEval.csproj
├── refresh_portuguese_eval_data.ps1
├── run_portuguese_full_evaluation.ps1
├── portuguese_eval_thresholds.json
└── generate_portuguese_exceptions.ps1
```

---

## 付録A: PortugueseIpaPhoneme enum 全49種

```csharp
public enum PortugueseIpaPhoneme : byte
{
    // --- 口母音 (0-8) ---
    A = 0,           // /a/ 前舌開母音
    E = 1,           // /e/ 前舌半狭母音
    Eh = 2,          // /ɛ/ 前舌半広母音
    I = 3,           // /i/ 前舌狭母音
    O = 4,           // /o/ 後舌半狭母音
    Oh = 5,          // /ɔ/ 後舌半広母音
    U = 6,           // /u/ 後舌狭母音
    Schwa = 7,       // /ɐ/ 中舌ほぼ開母音
    HighCentral = 8, // /ɨ/ 中舌ほぼ狭母音 (EP固有)

    // --- 鼻母音 (9-13) ---
    ANasal = 9,      // /ɐ̃/ 鼻母音
    ENasal = 10,     // /ẽ/ 鼻母音
    INasal = 11,     // /ĩ/ 鼻母音
    ONasal = 12,     // /õ/ 鼻母音
    UNasal = 13,     // /ũ/ 鼻母音

    // --- 半母音 (14-15) ---
    J = 14,          // /j/ 硬口蓋接近音
    W = 15,          // /w/ 軟口蓋唇接近音

    // --- 破裂音 (16-21) ---
    P = 16,          // /p/ 無声両唇破裂音
    B = 17,          // /b/ 有声両唇破裂音
    T = 18,          // /t/ 無声歯茎破裂音
    D = 19,          // /d/ 有声歯茎破裂音
    K = 20,          // /k/ 無声軟口蓋破裂音
    G = 21,          // /ɡ/ 有声軟口蓋破裂音

    // --- 摩擦音 (22-27) ---
    F = 22,          // /f/ 無声唇歯摩擦音
    V = 23,          // /v/ 有声唇歯摩擦音
    S = 24,          // /s/ 無声歯茎摩擦音
    Z = 25,          // /z/ 有声歯茎摩擦音
    Sh = 26,         // /ʃ/ 無声後部歯茎摩擦音
    Zh = 27,         // /ʒ/ 有声後部歯茎摩擦音

    // --- 鼻音 (28-30) ---
    M = 28,          // /m/ 両唇鼻音
    N = 29,          // /n/ 歯茎鼻音
    Ny = 30,         // /ɲ/ 硬口蓋鼻音

    // --- 側面音 (31-32) ---
    L = 31,          // /l/ 歯茎側面接近音
    Lh = 32,         // /ʎ/ 硬口蓋側面接近音

    // --- ロティック (33-34) ---
    R = 33,          // /ɾ/ 歯茎はじき音
    Rr = 34,         // /ʁ/ 有声口蓋垂摩擦音

    // --- BP固有異音 (35-38) ---
    Ch = 35,         // /tʃ/ 無声後部歯茎破擦音
    Jh = 36,         // /dʒ/ 有声後部歯茎破擦音
    X = 37,          // /x/ 無声軟口蓋摩擦音
    H = 38,          // /h/ 無声声門摩擦音

    // --- EP固有異音 (39-40) ---
    DarkL = 39,      // /ɫ/ 軟口蓋化側面音
    Xh = 40,         // /χ/ 無声口蓋垂摩擦音

    // --- 共通異音 (41-43) ---
    Ng = 41,         // /ŋ/ 軟口蓋鼻音
    NLabiodental = 42, // /ɱ/ 唇歯鼻音
    NDental = 43,    // /n̪/ 歯鼻音

    // --- 弱化異音 (44-46) ---
    Beta = 44,       // /β/ 有声両唇摩擦音 (Lenition: /b/の弱化)
    Dh = 45,         // /ð/ 有声歯摩擦音 (Lenition: /d/の弱化)
    Gh = 46,         // /ɣ/ 有声軟口蓋摩擦音 (Lenition: /ɡ/の弱化)

    // --- 鼻わたり音 (47-48) ---
    WNasal = 47,     // /w̃/ 鼻化わたり音 (鼻二重母音の要素: ão, om)
    JNasal = 48,     // /j̃/ 鼻化わたり音 (鼻二重母音の要素: ãe, õe, em, ui)
}
```

## 付録B: PortugueseAllophoneFeatures flags enum

```csharp
[Flags]
public enum PortugueseAllophoneFeatures : byte
{
    None = 0,

    // === 必須規則 ===
    NasalAssimilation = 1 << 0,
    SibilantVoicingAssimilation = 1 << 1,
    VowelReduction = 1 << 2,

    // === 可変規則 ===
    Lenition = 1 << 3,
    SibilantPalatalization = 1 << 4,
    TDPalatalization = 1 << 5,

    /// <summary>コーダ /l/ の異音を適用する（BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ]、方言で自動選択）。</summary>
    LAllophony = 1 << 6,

    // 注: RhoticDebuccalization は LAllophony と同ビットを共有せず、
    // AllophoneProcessor 内で方言に基づき /ʁ/ の実現形（EP: [ʁ], BP: [h]）を自動選択する。
    // 独立フラグが必要な場合は byte の 7ビット目（1 << 7 = 未使用）を使用可能。

    // === プリセット ===
    Obligatory = NasalAssimilation | SibilantVoicingAssimilation | VowelReduction,
    EuropeanDefault = Obligatory | Lenition | SibilantPalatalization | LAllophony,
    BrazilianDefault = Obligatory | TDPalatalization | LAllophony,
    All = Obligatory | Lenition | SibilantPalatalization | TDPalatalization | LAllophony,
}
```

**設計変更の根拠**（05_allophone_rules.md レビュー指摘への対応）:

1. **byte 基底型への統一**: スペイン語 (`SpanishAllophoneFeatures : byte`) とフランス語 (`FrenchAllophoneFeatures : byte`) のパターンに合わせ、`byte`（8ビット）を使用する
2. **VowelReduction の AllophoneProcessor 統合**: 母音弱化は方言依存度が高いため AllophoneProcessor の VowelReduction フラグで制御する。02_g2p_rules.md で提案された `VowelReducer.cs` 独立クラスは採用せず、AllophoneProcessor 内のサブステップとして実装する。P1の GraphemeToPhonemeRules は Phase 1-5 に限定し、母音弱化は P2 の AllophoneProcessor に配置する
3. **LVocalization/LVelarization の単一フラグ統合**: 両規則は排他的（BP: l半母音化[w]、EP: l軟口蓋化[ɫ]）であるため、`LAllophony` 単一フラグ + 方言判定で自動切替する設計とする。`[Flags] enum` で両方を同時セットできる矛盾を解消
4. **RhoticDebuccalization の扱い**: /ʁ/ の実現形選択は方言パラメータから自動決定されるため、独立フラグとせず AllophoneProcessor 内で方言に基づき処理する。将来的に独立制御が必要になった場合は byte の残り1ビット（1 << 7）を使用可能

## 付録C: 参照した調査ドキュメント

| # | ファイル | 内容 |
|---|---------|------|
| 01 | `01_phoneme_inventory.md` | IPA音素インベントリ（口母音/鼻母音/子音/異音、enum設計提案） |
| 02 | `02_g2p_rules.md` | G2P変換規則（7フェーズ構成、各文字の変換ルール） |
| 03 | `03_dialect_differences.md` | BP/EP方言差（母音弱化/口蓋化/l処理/coda s/R音素） |
| 04 | `04_syllable_stress.md` | 音節構造・ストレス規則（onset maximization、アクセント記号処理） |
| 05 | `05_allophone_rules.md` | 異音規則（9規則、適用順序、AllophoneFeatures設計） |
| 06 | `06_nasal_vowels_special.md` | 鼻母音化・鼻二重母音・Metaphony・L-Vocalization・Epenthesis |
| 07 | `07_existing_tools_datasets.md` | 既存ツール・データセット（ipa-dict/WikiPron/espeak-ng等） |
| 08 | `08_text_normalization.md` | テキスト正規化要件（数詞/通貨/日付/時刻/略語/単位/記号） |
| 09 | `09_codebase_patterns.md` | 既存コードベースパターン分析（Engine/Options/Rules/Models/Tests構造） |

