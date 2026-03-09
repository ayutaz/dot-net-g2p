# スペイン語G2P 実装計画

## 概要

`DotNetG2P.Spanish` パッケージとして、スペイン語のG2P（書記素→音素変換）をC#でネイティブ実装する。
スペイン語は正書法が非常に規則的なため、**ルールベースアプローチ**を採用し、大規模辞書を不要とする。

## 実装状況（2026-03-10）

- **S1: 完了**
  - `SpanishG2PEngine`, `SpanishG2POptions`, IPA音素モデル、音節分割、ストレス付与、ラテンアメリカ/カスティーリャ切り替えを実装済み
  - `ch / ll / rr / qu / gu / gü / c / g / r / x / y / z / h` を含む基本ルールベース変換を実装済み
  - `DotNetG2P.Spanish` パッケージ、UPMメタデータ、ソリューション接続、Spanish専用テスト群を追加済み
- **S2: 完了**
  - `SpanishNormalizer` を段階型パイプラインへ整理し、日付/時刻/単位/略語/記号の正規化語彙を拡張済み
  - `NumberToWords` に文脈依存の性・省略形（`un/uno`, `una`, `veintiún/veintiuna`）を追加済み
  - 例外辞書を `spanish_exceptions.master.tsv` ソース + `generate_spanish_exceptions.ps1` 生成運用へ移行済み
  - `AllophoneProcessor` を `SpanishAllophoneFeatures` で必須規則と可変規則に分離済み
  - curated allophone corpus と metadata 整合テストを追加済み
- **S3: 完了**
  - `XSampaConverter` と `ToXSampa / ToXSampaWithoutStress / ToXSampaBatch` を実装済み
  - `SpanishXSampaTests / SpanishEdgeCaseTests / SpanishPerformanceTests / SpanishAccuracyTests` を追加済み
  - ASCII-only X-SAMPA、バッチ整合性、回帰コーパス、性能しきい値を検証済み
  - `ipa-dict / WikiPron` サンプルコーパスと PER 回帰テストを追加済み
  - `refresh_spanish_eval_data.ps1` + `DotNetG2P.SpanishEval` + `run_spanish_full_evaluation.ps1` により全量 PER 評価、カテゴリ別集計、不一致 TSV 出力を実装済み
- **S4: 初版実装済み**
  - `DotNetG2P.Multilingual` に `Language.Spanish` と `DefaultLatinLanguage` を実装済み
  - `MultilingualG2PEngine` に `SpanishG2PEngine` を統合済み
  - `TextSegmenter` がラテン文字列を `English / Spanish` に振り分け可能
  - `MultilingualSpanishTests` を追加済み
  - `MultilingualMixedLanguageTests` を追加し、日英中西4言語混在、句読点・数字入り混在、バッチAPI整合性を検証済み
  - 日本語辞書は `tools/install_naist_jdic.ps1` でダウンロード可能になり、`MeCabTokenizer()` / `MultilingualG2PEngine()` は `NaistJdicLocator` により既定パスから自動解決可能
- **検証状況**
  - `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter SpanishG2P`
  - 結果: **223 passed**
  - `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter Multilingual`
  - 結果: **328 passed**
- **未実装**
  - なし（S1-S4 は計画範囲を実装済み）

---

## マイルストーン

### S1: 基本ルールベースG2P（MVP）
- 状態: **完了**
- SpanishG2PEngine メインAPI
- 正書法→IPA音素変換ルール（ダイグラフ、文脈依存、単純対応）
- 音節分割（Syllabification）
- ストレス位置決定
- 方言オプション（seseo/distinción）
- テスト 98件（2026-03-09 時点）

### S2: 異音規則・テキスト正規化
- 状態: **完了**
- P1: 正規化仕様の拡張
  - `SpanishNormalizer` をカテゴリ別の展開ステージに分割
  - `NumberToWords` に性・省略形を追加
  - 日付・時刻・単位・略語・記号の対象範囲を拡張
- P2: 例外辞書運用の整備
  - `spanish_exceptions.master.tsv` に `dialect / category / stress / phonemes / source / note` を保持
  - `tools/generate_spanish_exceptions.ps1` でランタイム向け `spanish_exceptions.txt` を生成
  - 固有名詞・外来語・hiato 例外を追加
- P3: 異音規則の評価強化
  - `SpanishAllophoneFeatures` で `Obligatory / Default / All` を切替可能にした
  - curated allophone reference corpus を追加し、プロファイル別 exact match を検証
  - metadata 同期テストと文脈数詞テストを追加

### S3: 出力形式拡張・テスト充実
- 状態: **完了**
- X-SAMPA出力
- `ToXSampa / ToXSampaWithoutStress / ToXSampaBatch`
- エッジケーステスト、パフォーマンステスト、精度テスト
- WikiPron/ipa-dictサンプルデータによる PER 回帰検証
- `tools/refresh_spanish_eval_data.ps1`
  - `Sample / Full / Both` モード
  - `.cache/spanish-eval` にダウンロードキャッシュ
  - `artifacts/spanish-eval/corpora` に全量 TSV を生成
- `tools/DotNetG2P.SpanishEval`
  - `base / allophones / no_exceptions` プロファイル
  - `summary.tsv/json`, `category_summary.tsv/json`, `mismatches/*.tsv` を出力
- `tools/run_spanish_full_evaluation.ps1`
  - しきい値ファイル `tools/spanish_eval_thresholds.json` を使った全量評価ラッパー
- 実測値（2026-03-09）
  - `ipa_dict_es_es_full/base`: PER `1.69%`, WER `16.49%`
  - `ipa_dict_es_es_full/allophones`: PER `1.37%`, WER `13.69%`
  - `ipa_dict_es_mx_full/base`: PER `1.69%`, WER `16.49%`
  - `ipa_dict_es_mx_full/allophones`: PER `1.37%`, WER `13.69%`
  - `wikipron_ca_full/base`: PER `1.38%`, WER `11.14%`
  - `wikipron_la_full/base`: PER `1.43%`, WER `11.46%`
  - しきい値判定: すべて `pass`

### S4: 多言語統合・パッケージング
- 状態: **初版実装済み**
- `DotNetG2P.Multilingual` への統合（`Language.Spanish`）
- `DefaultLatinLanguage` と `SpanishOptions` を追加
- `LanguageDetector / TextSegmenter / MultilingualG2PEngine` のスペイン語対応
- NuGet + UPM パッケージ構成更新
- `MultilingualSpanishTests` を追加
- `MultilingualMixedLanguageTests` を追加し、日英中西4言語同時混在と句読点・数字入り混在を回帰化
- `tools/install_naist_jdic.ps1` による辞書導入と `NaistJdicLocator` による既定辞書解決を追加

---

## アーキテクチャ

### 変換パイプライン

```
入力テキスト
  → Normalize (SpanishNormalizer: テキスト正規化)         [S2 完了]
  → Tokenize (単語分割)                                    [S1]
  → RuleConvert (ルールベース: 正書法→音素変換)            [S1]
      → 例外辞書 lookup（loanword / hiato / 固有名詞）      [S2 完了]
      → ダイグラフ処理（ch, ll, rr, qu, gu, gü）
      → 文脈依存ルール（c, g, r, x, y, z, h）
      → 単純対応ルール
  → Syllabify (音節分割)                                   [S1]
  → AssignStress (ストレス付与: アクセント記号+デフォルトルール) [S1]
  → ApplyAllophones (異音規則: β/ð/ɣ, 鼻音同化等)          [S2 完了]
  → Format (IPA / X-SAMPA)                                 [S1/S3]
出力
```

### G2Pアプローチの比較（なぜルールベースか）

| アプローチ | スペイン語適性 | 理由 |
|-----------|-------------|------|
| **ルールベース** | **最適** | 正書法が規則的、辞書不要、高速・軽量 |
| 辞書ベース | 不要 | 正書法→音素がルールで予測可能、メモリ浪費 |
| ハイブリッド | 過剰 | 複雑性に見合わない |
| ニューラル | 過剰 | C#/Unityでの推論コスト高、オーバーキル |

英語G2Pが13万語のCMU辞書を必要とするのに対し、スペイン語は**ルールのみで高精度**を達成できる。

---

## ディレクトリ構成

```
src/DotNetG2P.Spanish/
  DotNetG2P.Spanish.csproj           # .NET Standard 2.1、独立パッケージ
  SpanishG2PEngine.cs                # メインAPI (sealed class, IDisposable)
  SpanishG2POptions.cs               # イミュータブルオプション
  Models/
    SpanishIpaPhoneme.cs             # IPA音素enum : byte
    SpanishPhoneme.cs                # readonly struct (音素型+ストレス)
    SpanishPronunciation.cs          # 発音クラス (音素配列ラッパー)
    SpanishDialect.cs                # 方言enum : byte
    SpanishSyllable.cs               # 音節 readonly struct
  Rules/
    GraphemeToPhonemeRules.cs        # 正書法→音素変換ルールエンジン
    SpanishOrthography.cs            # 母音/二重母音/無音u 判定
    SpanishSyllabifier.cs            # 音節分割アルゴリズム
    StressAssigner.cs                # ストレス位置決定
    AllophoneProcessor.cs            # 異音規則適用 [S2 初版実装済み]
  Normalization/
    SpanishNormalizer.cs             # テキスト正規化 [S2 完了]
    NumberToWords.cs                 # 数字→スペイン語読み + 文脈依存数詞 [S2 完了]
  Conversion/
    IpaConverter.cs                  # 内部表現→IPA文字列変換
    XSampaConverter.cs               # 内部表現→X-SAMPA文字列変換 [S3]
  Data/                              # 例外辞書 [S2]
    spanish_exceptions.master.tsv    # 例外辞書ソース（dialect/category/source metadata付き）
    spanish_exceptions.txt           # ランタイム用生成辞書
    SpanishExceptionDictionary.cs    # 例外辞書ローダ
  package.json                       # UPM (com.dotnetg2p.spanish)
  DotNetG2P.Spanish.asmdef           # Unity Assembly Definition
tools/
  refresh_spanish_eval_data.ps1      # サンプル/全量評価コーパス生成 [S3]
  run_spanish_full_evaluation.ps1    # 全量評価ラッパー [S3]
  spanish_eval_thresholds.json       # PERしきい値設定 [S3]
  DotNetG2P.SpanishEval/
    DotNetG2P.SpanishEval.csproj     # 全量評価CLI [S3]
    Program.cs                       # PER/カテゴリ別集計/不一致出力
```

### テスト構成

```
tests/DotNetG2P.Tests/SpanishG2P/
  SpanishG2PEngineTests.cs           # エンジン統合テスト [S1]
  GraphemeToPhonemeRulesTests.cs     # 変換ルールテスト [S1]
  SpanishSyllabifierTests.cs         # 音節分割テスト [S1]
  StressAssignerTests.cs             # ストレスルールテスト [S1]
  SpanishPhonemeTests.cs             # 音素モデルテスト [S1]
  AllophoneProcessorTests.cs         # 異音規則テスト [S2]
  SpanishNormalizerTests.cs          # テキスト正規化テスト [S2]
  NumberToWordsTests.cs              # 文脈依存数詞テスト [S2]
  SpanishExceptionDictionaryMetadataTests.cs  # 例外辞書メタデータ整合性 [S2]
  SpanishAllophoneEvaluationTests.cs # allophone reference corpus 評価 [S2]
  SpanishIpaTests.cs                 # IPA変換テスト [S1]
  SpanishXSampaTests.cs             # X-SAMPA変換テスト [S3]
  SpanishEdgeCaseTests.cs           # エッジケーステスト [S3]
  SpanishPerformanceTests.cs        # パフォーマンステスト [S3]
  SpanishAccuracyTests.cs           # 精度・回帰テスト [S3]
```

現行テスト数: 223件（2026-03-10 時点）

追加の Multilingual 検証（2026-03-10）:

- `MultilingualMixedLanguageTests`: 6件追加
- `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --no-build --filter "FullyQualifiedName~DotNetG2P.Tests.Multilingual&FullyQualifiedName!~Performance"`
  - **320 passed**
- `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --no-build --filter Multilingual`
  - **328 passed**
- `MultilingualPerformanceTests.メモリ圧迫なし`
  - ウォームアップと `PerformanceThresholds` ベースの relaxed 閾値へ調整済み

### 全量評価の実行方法

```powershell
pwsh -File tools/refresh_spanish_eval_data.ps1 -Mode Full
pwsh -File tools/run_spanish_full_evaluation.ps1 -EnforceThresholds
```

- 入力コーパス: `artifacts/spanish-eval/corpora`
- 評価レポート: `artifacts/spanish-eval/reports`
- ダウンロードキャッシュ: `.cache/spanish-eval`
- 主な出力:
  - `summary.tsv`
  - `summary.json`
  - `category_summary.tsv`
  - `category_summary.json`
  - `mismatches/*.tsv`

---

## 音素体系

### 推奨音素インベントリ

G2Pシステムで使用する音素セット。基本はラテンアメリカ標準（seseo + yeísmo）。

**子音音素（17種、基本セット）**:

| enum名 | IPA | 書記素 | 備考 |
|--------|-----|--------|------|
| P | /p/ | p | |
| B | /b/ | b, v | [b]〜[β] 異音交替 |
| T | /t/ | t | |
| D | /d/ | d | [d]〜[ð] 異音交替 |
| K | /k/ | c(+a,o,u), qu(+e,i), k | |
| G | /ɡ/ | g(+a,o,u), gu(+e,i) | [ɡ]〜[ɣ] 異音交替 |
| F | /f/ | f | |
| S | /s/ | s, c(+e,i), z | seseo基準 |
| X | /x/ | j, g(+e,i) | 軟口蓋摩擦音 |
| Ch | /tʃ/ | ch | |
| Y | /ʝ/ | y, ll | yeísmo基準 |
| M | /m/ | m | |
| N | /n/ | n | 鼻音同化の異音あり |
| Ny | /ɲ/ | ñ | |
| L | /l/ | l | |
| Rr | /r/ | rr, r(語頭) | ふるえ音 |
| R | /ɾ/ | r(その他) | はじき音 |

**カスティーリャ方言追加（2種）**:

| enum名 | IPA | 書記素 | 備考 |
|--------|-----|--------|------|
| Th | /θ/ | c(+e,i), z | distinciónモードのみ |
| Ll | /ʎ/ | ll | lleísmoモードのみ（オプション） |

**母音音素（5種）**:

| enum名 | IPA | 書記素 |
|--------|-----|--------|
| A | /a/ | a, á |
| E | /e/ | e, é |
| I | /i/ | i, í |
| O | /o/ | o, ó |
| U | /u/ | u, ú, ü |

**半母音（2種）**:

| enum名 | IPA | 環境 |
|--------|-----|------|
| J | [j] | 二重母音中の /i/ |
| W | [w] | 二重母音中の /u/ |

**合計**: 基本24音素 + カスティーリャ拡張2 = 最大26音素

---

## パブリックAPI設計

### SpanishG2PEngine（実装済み）

```csharp
public sealed class SpanishG2PEngine : IDisposable
{
    // コンストラクタ
    public SpanishG2PEngine();
    public SpanishG2PEngine(SpanishG2POptions options);

    // メイン変換
    public string ToPhonemes(string text);              // IPA音素列（スペース区切り）
    public string ToIPA(string text);                    // IPA表記
    public IReadOnlyList<SpanishPhoneme> ToPhonemeList(string text);

    // 音節分割
    public IReadOnlyList<SpanishSyllable> ToSyllables(string word);

    // バッチ
    public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
    public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts);

    // X-SAMPA [S3]
    public string ToXSampa(string text);
    public string ToXSampaWithoutStress(string text);
    public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts);

    // IDisposable
    public void Dispose();
}
```

### SpanishG2POptions（実装済み）

```csharp
public sealed class SpanishG2POptions
{
    public static readonly SpanishG2POptions Default;

    public SpanishDialect Dialect { get; }               // Castilian / LatinAmerican（デフォルト: LatinAmerican）
    public bool IncludeStress { get; }                   // ストレスマーク（デフォルト: true）
    public bool EnableAllophones { get; }                 // 異音規則適用（デフォルト: false、実装済み）
    public bool EnableTextNormalization { get; }          // テキスト正規化（デフォルト: true）
    public bool EnableExceptionDictionary { get; }        // 例外辞書適用（デフォルト: true、評価用切替）
    public string Separator { get; }                     // 音素区切り（デフォルト: " "）
}
```

### SpanishDialect

```csharp
public enum SpanishDialect : byte
{
    LatinAmerican = 0,  // seseo + yeísmo（デフォルト）
    Castilian = 1,      // distinción + yeísmo
}
```

---

## 変換ルール詳細

### 処理順序

1. **テキスト正規化**: Unicode正規化、全角→半角変換、小文字化
   - 実装済み: 略語展開 (`Sr.` / `Sra.` / `Dr.` / `Dra.` / `Ud.` / `Uds.`)、数値、通貨、割合、`& + @`
2. **例外辞書**: 外来語・固有名詞・hiato例外の先行解決
2. **ダイグラフ展開**: ch, ll, rr, qu+e/i, gu+e/i, gü+e/i を先に処理
3. **文脈依存ルール**: c, g, r, x, y, z, h のコンテキスト判定
4. **単純対応**: 残りの文字を1:1で変換
5. **二重母音判定**: 弱母音+強母音 / 強母音+弱母音 → 半母音化
6. **音節分割**: onset maximization原理 + 不可分クラスタ
7. **ストレス付与**: アクセント記号 or デフォルトルール
8. **異音処理**（オプション）: β/ð/ɣ 弱化、鼻音同化等

### 正書法→音素変換ルール（優先順位順）

```
[ダイグラフ]
ch      → tʃ
ll      → ʝ (yeísmo) / ʎ (lleísmo)
rr      → r
qu + e  → k (uは無音)
qu + i  → k (uは無音)
gü + e  → ɡw
gü + i  → ɡw
gu + e  → ɡ (uは無音)
gu + i  → ɡ (uは無音)

[文脈依存]
c + e,i → s (seseo) / θ (distinción)
c + a,o,u,子音 → k
g + e,i → x
g + a,o,u → ɡ
z       → s (seseo) / θ (distinción)
r (語頭) → r (ふるえ音)
r (n,l,s後) → r (ふるえ音)
r (その他) → ɾ (はじき音)
x (語頭) → s
x (その他) → ks
y (母音前) → ʝ
y (語末/単独) → i
h → ∅ (無音)

[例外辞書で補正]
y → i
guion → ɡi.ˈon
truhan → tɾu.ˈan
whisky → ˈwiski
wifi → ˈwifi
show → ˈʃow
México / mexico → ˈmexiko / meˈxiko
Xochimilco → ʃo.tʃi.ˈmil.ko
Wagner → ˈbaɡner

[単純対応]
a,á → a    e,é → e    i,í → i    o,ó → o    u,ú,ü → u
b → b      d → d      f → f      j → x      k → k
l → l      m → m      n → n      ñ → ɲ      p → p
s → s      t → t      v → b      w → w
```

---

## Multilingual統合（S4, 実装済み）

### 修正が必要なファイル

| ファイル | 変更内容 |
|---------|---------|
| `Language.cs` | `Spanish = 3` を追加 |
| `ScriptKind.cs` | 変更不要（Latin は既存） |
| `LanguageDetector.cs` | `ToLanguage(kind, defaultLatinLanguage)` を追加し、Latin系の既定言語を切替 |
| `TextSegmenter.cs` | `LangSpanish = 4` 定数追加、`FromLangByte` に case 追加 |
| `MultilingualG2PEngine.cs` | `SpanishG2PEngine` フィールド追加、`ConvertSegment` に case 追加 |
| `MultilingualG2POptions.cs` | `SpanishOptions` / `DefaultLatinLanguage` プロパティ追加 |
| `DotNetG2P.Multilingual.csproj` | `DotNetG2P.Spanish` への ProjectReference 追加 |

### 日本語辞書導入の改善

S4 完了後、Japanese / Multilingual 利用時のセットアップも改善された。

- `tools/install_naist_jdic.ps1` で OpenJTalk 由来の `naist-jdic` をダウンロードし、既定で `%USERPROFILE%\\naist-jdic` に展開できる
- `NaistJdicLocator` が以下の順で辞書を探索する
  1. `DOTNETG2P_NAIST_JDIC_PATH`
  2. `NAIST_JDIC_PATH`
  3. `%USERPROFILE%\\naist-jdic`
  4. カレントディレクトリ配下の `naist-jdic` / `open_jtalk_dic_utf_8-1.11`
- これにより `MeCabTokenizer()` および `MultilingualG2PEngine()` を引数なしで初期化できる

### スペイン語・英語の区別

スペイン語と英語は同じラテン文字を共有するため、文字種だけでは区別できない。
現実装では `DefaultCjkLanguage` と同様に `DefaultLatinLanguage` を導入し、アクセント付きスペイン語文字を含む語は英語既定時でも Spanish に寄せる。

```csharp
public Language DefaultLatinLanguage { get; } = Language.English;
```

---

## ライセンス考慮

### 利用可能なリソース（Apache-2.0互換）

| リソース | ライセンス | 用途 |
|---------|----------|------|
| Epitran マッピング | MIT | ルール設計の参考 |
| NRC G2P | MIT | アーキテクチャ参考 |
| WikiPron | Apache 2.0 | テストデータ |
| ipa-dict | MIT | テストデータ |

### 利用不可（GPL）

| リソース | ライセンス | 理由 |
|---------|----------|------|
| espeak-ng ルールファイル | GPLv3 | Apache-2.0と非互換 |
| Phonemizer | GPLv3 | espeak-ng依存 |

---

## 参考文献

- 技術調査詳細: [spanish-g2p-research.md](spanish-g2p-research.md)
- Epitran: https://github.com/dmort27/epitran
- WikiPron: https://github.com/CUNY-CL/wikipron
- ipa-dict: https://github.com/open-dict-data/ipa-dict
