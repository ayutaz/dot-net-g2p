# スペイン語G2P 実装計画

## 概要

`DotNetG2P.Spanish` パッケージとして、スペイン語のG2P（書記素→音素変換）をC#でネイティブ実装する。
スペイン語は正書法が非常に規則的なため、**ルールベースアプローチ**を採用し、大規模辞書を不要とする。

## 実装状況（2026-03-09）

- **S1: 完了**
  - `SpanishG2PEngine`, `SpanishG2POptions`, IPA音素モデル、音節分割、ストレス付与、ラテンアメリカ/カスティーリャ切り替えを実装済み
  - `ch / ll / rr / qu / gu / gü / c / g / r / x / y / z / h` を含む基本ルールベース変換を実装済み
  - `DotNetG2P.Spanish` パッケージ、UPMメタデータ、ソリューション接続、Spanish専用テスト群を追加済み
- **S2: 初版実装済み**
  - `AllophoneProcessor` による `/b d g/` 弱化、鼻音同化、`/s/` の有声化を実装済み
  - `SpanishNormalizer` による Unicode正規化、小文字化、句読点除去、略語展開、数値展開、通貨/割合/記号展開を実装済み
  - 埋め込み例外辞書 `spanish_exceptions.txt` を追加し、`y / guion / truhan / whisky / wifi / show / México / Xochimilco / Wagner` などを補正
- **S3: 初版実装済み**
  - `XSampaConverter` と `ToXSampa / ToXSampaWithoutStress / ToXSampaBatch` を実装済み
  - `SpanishXSampaTests / SpanishEdgeCaseTests / SpanishPerformanceTests / SpanishAccuracyTests` を追加済み
  - ASCII-only X-SAMPA、バッチ整合性、回帰コーパス、性能しきい値を検証済み
  - `ipa-dict / WikiPron` サンプルコーパスと PER 回帰テストを追加済み
- **検証状況**
  - `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter SpanishG2P`
  - 結果: **177 passed**
- **未実装**
  - WikiPron / ipa-dict 全量を使った大規模精度評価
  - `DotNetG2P.Multilingual` 統合

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
- 状態: **初版実装済み**
- 異音処理（/b,d,g/ 弱化、鼻音同化、摩擦音有声化）
- テキスト正規化（数字・記号・略語の展開）
- 外来語例外辞書（少数、EmbeddedResource）
- 今後の残課題: 正規化語彙の拡充、例外辞書の追加、大規模精度評価

### S3: 出力形式拡張・テスト充実
- 状態: **初版実装済み**
- X-SAMPA出力
- `ToXSampa / ToXSampaWithoutStress / ToXSampaBatch`
- エッジケーステスト、パフォーマンステスト、精度テスト
- WikiPron/ipa-dictサンプルデータによる PER 回帰検証
- テスト 79件追加（累計 177件）

### S4: 多言語統合・パッケージング
- 状態: **未着手**
- DotNetG2P.Multilingual への統合（Language.Spanish）
- LanguageDetector/TextSegmenter のスペイン語対応
- NuGet + UPM パッケージ構成
- テスト 50件追加

---

## アーキテクチャ

### 変換パイプライン

```
入力テキスト
  → Normalize (SpanishNormalizer: テキスト正規化)         [S2 初版実装済み]
  → Tokenize (単語分割)                                    [S1]
  → RuleConvert (ルールベース: 正書法→音素変換)            [S1]
      → 例外辞書 lookup（loanword / hiato / 固有名詞）      [S2 初版実装済み]
      → ダイグラフ処理（ch, ll, rr, qu, gu, gü）
      → 文脈依存ルール（c, g, r, x, y, z, h）
      → 単純対応ルール
  → Syllabify (音節分割)                                   [S1]
  → AssignStress (ストレス付与: アクセント記号+デフォルトルール) [S1]
  → ApplyAllophones (異音規則: β/ð/ɣ, 鼻音同化等)          [S2 初版実装済み]
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
    SpanishNormalizer.cs             # テキスト正規化 [S2 初版実装済み]
    NumberToWords.cs                 # 数字→スペイン語読み [S2 初版実装済み]
  Conversion/
    IpaConverter.cs                  # 内部表現→IPA文字列変換
    XSampaConverter.cs               # 内部表現→X-SAMPA文字列変換 [S3]
  Data/                              # 例外辞書 [S2]
    spanish_exceptions.txt           # 外来語・hiato・固有名詞例外 (EmbeddedResource)
    SpanishExceptionDictionary.cs    # 埋め込み辞書ローダ
  package.json                       # UPM (com.dotnetg2p.spanish)
  DotNetG2P.Spanish.asmdef           # Unity Assembly Definition
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
  SpanishIpaTests.cs                 # IPA変換テスト [S1]
  SpanishXSampaTests.cs             # X-SAMPA変換テスト [S3]
  SpanishEdgeCaseTests.cs           # エッジケーステスト [S3]
  SpanishPerformanceTests.cs        # パフォーマンステスト [S3]
  SpanishAccuracyTests.cs           # 精度・回帰テスト [S3]
```

現行テスト数: 177件（2026-03-09 時点）

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

## Multilingual統合（S4）

### 修正が必要なファイル

| ファイル | 変更内容 |
|---------|---------|
| `Language.cs` | `Spanish = 3` を追加 |
| `ScriptKind.cs` | 変更不要（Latin は既存） |
| `LanguageDetector.cs` | スペイン語固有文字（ñ, ¿, ¡）検出、または `DefaultLatinLanguage` オプション追加 |
| `TextSegmenter.cs` | `LangSpanish = 4` 定数追加、`FromLangByte` に case 追加 |
| `MultilingualG2PEngine.cs` | `SpanishG2PEngine` フィールド追加、`ConvertSegment` に case 追加 |
| `MultilingualG2POptions.cs` | `SpanishOptions` プロパティ追加 |
| `DotNetG2P.Multilingual.csproj` | `DotNetG2P.Spanish` への ProjectReference 追加 |

### スペイン語・英語の区別

スペイン語と英語は同じラテン文字を共有するため、文字種だけでは区別できない。
`DefaultCjkLanguage` と同様のパターンで `DefaultLatinLanguage` オプションを追加する方式を推奨。

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
