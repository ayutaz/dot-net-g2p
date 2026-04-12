---
ticket: T02
title: PinyinToMisaki Convert メソッド統合
milestone: Mi1
status: 未着手
depends_on: [T01]
blocks: [T03]
---

# T02: PinyinToMisaki Convert メソッド統合

## 1. タスク目的とゴール

T01 で定義済みの声母・韻母・声調の Misaki マッピングテーブル（`s_initialMisaki`, `s_finalMisaki`, 声調矢印テーブル）を使い、`PinyinToMisaki` クラスに `Convert()` および `ConvertSyllable()` メソッドを実装する。

**ゴール:**
- `PinyinToMisaki.Convert(string pinyin)` — ピンイン文字列を受け取り、Misaki 互換の音素文字列を返す
- `PinyinToMisaki.ConvertSyllable(PinyinSyllable syllable, bool includeTones)` — パース済み音節構造体から Misaki 文字列を生成する
- `dotnet build` が通過すること
- Convert 単体のユニットテストが全件パスすること

## 2. 実装する内容の詳細

### 2.1 ファイル配置

```
src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs
```

T01 で作成済みのファイルにメソッドを追加する形を想定。T01 で既にクラスの骨格とマッピングテーブルが定義されている前提。

### 2.2 メソッドシグネチャ

```csharp
namespace DotNetG2P.Chinese
{
    internal static class PinyinToMisaki
    {
        // --- T01 で定義済み ---
        // private static readonly Dictionary<Initial, string> s_initialMisaki;
        // private static readonly Dictionary<Final, string> s_finalMisaki;
        // private static readonly string[] s_toneArrows; // 声調矢印テーブル

        // --- T02 で実装 ---

        /// <summary>
        /// 声調記号付きピンインを Misaki 互換表記に変換する（声調矢印付き）。
        /// </summary>
        public static string Convert(string pinyin);

        /// <summary>
        /// 声調記号付きピンインを Misaki 互換表記に変換する。
        /// </summary>
        public static string Convert(string pinyin, bool includeTones);

        /// <summary>
        /// PinyinSyllable を Misaki 互換表記に変換する。
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones);
    }
}
```

### 2.3 処理フロー — Convert()

既存の `PinyinToIpa.Convert()` と同一の構造を踏襲する。

```
入力: ピンイン文字列 (例: "zhōng", "ma1", "lǜ")
  │
  ├─ 1. null/空チェック → string.Empty を返す
  │
  ├─ 2. ToneConverter.ToToneMarked(pinyin) で数字声調形式を声調記号付きに正規化
  │      例: "ma1" → "mā"
  │
  ├─ 3. PinyinParser.TryParse(normalized, out syllable)
  │      パース失敗 → string.Empty を返す
  │      例: "zhōng" → PinyinSyllable(Initial.Zh, Final.Ong, Tone.First)
  │
  └─ 4. ConvertSyllable(syllable, includeTones) を呼び出して結果を返す
```

引数なし `Convert(string pinyin)` は `Convert(pinyin, true)` に委譲する（PinyinToIpa と同一パターン）。

### 2.4 処理フロー — ConvertSyllable()

これが変換の中核ロジックである。`PinyinToIpa.ConvertSyllable()` の全ロジックフローを踏襲しつつ、マッピングテーブルのみを Misaki 用に差し替える。

以下に `PinyinToIpa.ConvertSyllable()` の全ロジックフローを詳述する。

#### ステップ A: StringBuilder 初期化

```csharp
var sb = new StringBuilder(16);
```

16 文字の初期容量で StringBuilder を作成。Misaki 表記でも十分なサイズ。

#### ステップ B: 声母の変換

```
syllable.Initial != Initial.None の場合:
  │
  ├─ (a) Initial が Y または W の場合 → 半母音省略判定
  │     │
  │     ├─ ShouldOmitSemivowel(Initial, Final) == true
  │     │   → 声母を出力しない（韻母が既に対応する半母音で始まるため）
  │     │
  │     └─ ShouldOmitSemivowel == false
  │         → s_initialMisaki[syllable.Initial] を出力
  │
  └─ (b) それ以外の声母
        → s_initialMisaki[syllable.Initial] を出力
```

**ShouldOmitSemivowel の判定ロジック（PinyinToIpa から完全再利用）:**

- `Initial.Y` の場合、以下の韻母なら半母音 j を省略:
  - `Final.I`, `Final.In`, `Final.Ing` — 韻母の IPA/Misaki が `i` で始まるため
  - `Final.V`, `Final.Ve`, `Final.Van`, `Final.Vn` — 韻母の IPA/Misaki が `y`（円唇前舌高母音）で始まるため
- `Initial.W` の場合、以下の韻母なら半母音 w を省略:
  - `Final.U`, `Final.Un` — 韻母の IPA/Misaki が `u` で始まるため
- それ以外 → 省略しない (false)

#### ステップ C: 韻母の変換（特殊母音の分岐を含む）

```
syllable.Final != Final.None の場合:
  │
  ├─ (c1) Final == Final.I かつ IsRetroflex(Initial) == true
  │     → そり舌母音を出力（後述の特殊母音処理を参照）
  │     対象声母: Initial.Zh, Initial.Ch, Initial.Sh, Initial.R
  │     例: "zhi" → 声母(zh の Misaki) + そり舌母音(Misaki)
  │
  ├─ (c2) Final == Final.I かつ IsAlveolar(Initial) == true
  │     → 歯茎母音を出力（後述の特殊母音処理を参照）
  │     対象声母: Initial.Z, Initial.C, Initial.S
  │     例: "zi" → 声母(z の Misaki) + 歯茎母音(Misaki)
  │
  └─ (c3) 上記以外
        → s_finalMisaki[syllable.Final] を出力
```

**IsRetroflex の判定:**
```csharp
Initial == Initial.Zh || Initial == Initial.Ch || Initial == Initial.Sh || Initial == Initial.R
```

**IsAlveolar の判定:**
```csharp
Initial == Initial.Z || Initial == Initial.C || Initial == Initial.S
```

#### ステップ D: 声調矢印の付与

```
includeTones == true かつ syllable.Tone != Tone.Neutral の場合:
  → s_toneArrows[(int)syllable.Tone] を出力
```

Misaki では IPA の声調文字 (˥˥ 等) の代わりに矢印文字を使用する。具体的なマッピングは T01 で定義済み。

#### ステップ E: 結果の返却

```csharp
return sb.ToString();
```

### 2.5 PinyinToIpa との構造比較 — 同一部分と差異

| 項目 | PinyinToIpa | PinyinToMisaki |
|------|-------------|----------------|
| Convert() の入口処理 | 同一 | **同一**: null チェック → ToneConverter → PinyinParser → ConvertSyllable |
| 半母音省略判定 | ShouldOmitSemivowel | **同一ロジック**: Y/W + 特定韻母で省略 |
| そり舌母音 (zh/ch/sh/r + i) | `ɻ̩` (U+027B U+0329) | **差異**: T01 で定義する Misaki 表記 |
| 歯茎母音 (z/c/s + i) | `ɹ̩` (U+0279 U+0329) | **差異**: T01 で定義する Misaki 表記 |
| 声母マッピング | s_initialIpa | **差異**: s_initialMisaki（T01 定義） |
| 韻母マッピング | s_finalIpa | **差異**: s_finalMisaki（T01 定義） |
| 声調マッピング | s_toneLetters (IPA tone letters) | **差異**: s_toneArrows（Misaki 矢印記号、T01 定義） |
| includeTones パラメータ | あり | **同一**: true/false で声調出力を制御 |
| IsRetroflex / IsAlveolar | private static メソッド | **同一ロジック**: 再実装 or 共有ヘルパーから呼び出し |

### 2.6 特殊母音の処理 — そり舌母音・歯茎母音

中国語音韻学上、以下の2つの特殊母音はピンイン表記上「i」と書かれるが、実際の発音は通常の /i/ とは全く異なる。

**そり舌母音（zhi, chi, shi, ri の韻母）:**
- IPA: `ɻ̩` (U+027B U+0329 — そり舌接近音 + 音節主音)
- piper-plus: `ɻ̩` (U+027B U+0329 — IPA と同一)
- Misaki: T01 のマッピングテーブルで定義（要確認）

**歯茎母音（zi, ci, si の韻母）:**
- IPA: `ɹ̩` (U+0279 U+0329 — 歯茎接近音 + 音節主音)
- piper-plus: `ɨ` (U+0268 — 非円唇中舌高母音、簡略表記)
- Misaki: T01 のマッピングテーブルで定義（要確認）

実装では `s_retroflexApical` および `s_alveolarApical` として static readonly string フィールドに定義する（PinyinToIpa/PinyinToPiperIpa と同一パターン）。

### 2.7 参照すべき既存コード

| ファイル | 参照理由 |
|---------|---------|
| `src/DotNetG2P.Chinese/Conversion/PinyinToIpa.cs` | Convert/ConvertSyllable の構造テンプレート、ShouldOmitSemivowel/IsRetroflex/IsAlveolar のロジック |
| `src/DotNetG2P.Chinese/Conversion/PinyinToPiperIpa.cs` | 同構造の別バリアント。歯茎母音の表記差異 (`ɹ̩` vs `ɨ`) の参考 |
| `src/DotNetG2P.Chinese/Conversion/PinyinParser.cs` | TryParse の仕様。j/q/x/y 後の u→v 正規化ロジック |
| `src/DotNetG2P.Chinese/Conversion/ToneConverter.cs` | ToToneMarked / ExtractTone の仕様 |
| `src/DotNetG2P.Chinese/Models/PinyinSyllable.cs` | Initial/Final/Tone の構造体定義 |

## 3. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1名 | PinyinToMisaki.cs に Convert/ConvertSyllable/ShouldOmitSemivowel/IsRetroflex/IsAlveolar を実装 |
| テストエージェント | 1名 | ユニットテスト作成。Convert 単体テスト、特殊母音テスト、半母音省略テスト、声調テスト |
| レビューエージェント | 1名 | PinyinToIpa との構造一貫性確認、マッピングテーブル (T01) との整合性検証、エッジケースのレビュー |

合計 **3名**。実装自体は PinyinToIpa のコピー＆マッピング差し替えが主であり、1名の実装エージェントで十分対応可能。テストとレビューを並行して進めることで効率化できる。

## 4. 提供範囲とテスト項目

### 4.1 スコープ

**含む:**
- `PinyinToMisaki.Convert(string)` メソッド
- `PinyinToMisaki.Convert(string, bool)` メソッド
- `PinyinToMisaki.ConvertSyllable(PinyinSyllable, bool)` メソッド
- 上記に必要な private ヘルパー (`ShouldOmitSemivowel`, `IsRetroflex`, `IsAlveolar`)
- そり舌母音・歯茎母音の static readonly フィールド (`s_retroflexApical`, `s_alveolarApical`)
- Convert 単体のユニットテスト

**含まない:**
- ChineseG2PEngine への `ToMisaki()` API 追加（T03 のスコープ）
- マッピングテーブルの定義（T01 のスコープ）
- バッチ API (`ConvertBatch` 等)
- `ConvertToPhonemes()` メソッド（PinyinToPiperIpa にあるが、Misaki で必要かは T03 以降で判断）

### 4.2 ユニットテスト案

テストファイル: `tests/DotNetG2P.Tests/Chinese/PinyinToMisakiTests.cs`

| テストカテゴリ | テストケース例 | 検証内容 |
|---------------|-------------|---------|
| **基本変換** | "mā" → 期待 Misaki 出力 | 声母 m + 韻母 a + 第1声矢印 |
| **全声調** | "mā", "má", "mǎ", "mà", "ma" | 各声調の矢印が正しいこと。軽声は矢印なし |
| **数字声調入力** | "ma1", "ma2", "ma3", "ma4" | ToneConverter 経由で正しく変換されること |
| **includeTones=false** | "mā" (tones=false) | 声調矢印が付かないこと |
| **そり舌母音** | "zhī", "chī", "shī", "rì" | zh/ch/sh/r + i で特殊母音が出力されること |
| **歯茎母音** | "zī", "cī", "sī" | z/c/s + i で特殊母音が出力されること |
| **半母音省略 (Y)** | "yī" (yi), "yīn" (yin) | j が省略されること |
| **半母音付与 (Y)** | "yā" (ya), "yáo" (yao) | j が付与されること |
| **半母音省略 (W)** | "wū" (wu) | w が省略されること |
| **半母音付与 (W)** | "wā" (wa), "wǒ" (wo) | w が付与されること |
| **ü 系韻母** | "lǜ", "nǚ", "jú" (→ jü) | V/Ve/Van/Vn 韻母が正しく変換されること |
| **null/空文字** | null, "", " " | string.Empty が返ること |
| **パース失敗** | "xyz", "123" | string.Empty が返ること |
| **全声母網羅** | b,p,m,f,d,t,n,l,g,k,h,j,q,x,zh,ch,sh,r,z,c,s 各1例 | 全21声母のマッピングが正しいこと |
| **全韻母網羅** | 各韻母の代表音節1例ずつ (36種) | 全36韻母のマッピングが正しいこと |
| **er 韻母** | "ér" | 特殊韻母 er の変換が正しいこと |

## 5. 実装に関する懸念事項とレビュー項目

### 5.1 PinyinParser 依存

`Convert()` は `PinyinParser.TryParse()` に完全依存している。PinyinParser は j/q/x/y 後の `u` を `v`（ü系韻母）として正規化する処理を内包しており、この挙動を前提としている。PinyinParser 自体の変更は不要だが、Misaki のマッピングが ü 系韻母 (Final.V, Ve, Van, Vn) を正しくカバーしていることを T01 レビュー時に確認すること。

### 5.2 ShouldOmitSemivowel の再利用

現在 `PinyinToIpa` と `PinyinToPiperIpa` に全く同一の `ShouldOmitSemivowel` が private static で個別に存在する。`PinyinToMisaki` にも同一ロジックを3つ目としてコピーすることになる。

**レビュー確認項目:**
- 3クラス間で ShouldOmitSemivowel のロジックが完全一致していること
- 将来 ShouldOmitSemivowel を変更する場合、3箇所を同時に更新する必要がある旨をコメントに記載すること
- 共有ヘルパー化の是非はセクション6で考察

### 5.3 そり舌母音・歯茎母音の Misaki 表記

IPA と piper-plus で歯茎母音の表記が異なる（`ɹ̩` vs `ɨ`）前例がある。Misaki でこれらの特殊母音をどう表記するかは T01 のマッピングテーブル定義に依存する。

**レビュー確認項目:**
- T01 の `s_retroflexApical` / `s_alveolarApical` が Misaki (Kokoro TTS) の期待する表記と一致していること
- Misaki の中国語音素体系のドキュメントまたはソースコードとの照合

### 5.4 声調矢印の文字コード

Misaki が使用する声調矢印（↑↓→ 等）の具体的な Unicode コードポイントを T01 マッピングで正確に定義しているか確認すること。IPA の声調文字 (U+02E5-U+02E9) とは全く異なる文字体系になる。

### 5.5 Convert のオーバーロード設計

`PinyinToIpa` は `Convert(string)` と `Convert(string, bool)` の2つのオーバーロードを持つ。`PinyinToPiperIpa` は声調マーカーを含めないため `Convert(string)` のみ。Misaki が声調矢印を含める/含めないの両方を必要とするか確認し、オーバーロード構成を決定する。デフォルトは声調矢印付き（`includeTones = true`）とする。

## 6. 一から作り直すとしたら

### 6.1 現状の問題 — コピペパターン

現在の `PinyinToIpa.cs` と `PinyinToPiperIpa.cs` は以下のコードが事実上のコピペである:

- `Convert()` メソッドの入口処理（null チェック → ToneConverter → PinyinParser → ConvertSyllable 委譲）
- `ConvertSyllable()` の制御フロー（声母判定 → 半母音省略 → 韻母判定 → そり舌/歯茎分岐 → 声調付与）
- `ShouldOmitSemivowel()` — 3クラスで完全同一のロジック
- `IsRetroflex()` / `IsAlveolar()` — 3クラスで完全同一のロジック

`PinyinToMisaki` を追加すると、これが3重コピーになる。

### 6.2 設計案 A: 抽象基底クラス

```csharp
internal abstract class PinyinConverterBase<TResult>
{
    // テンプレートメソッド
    public TResult Convert(string pinyin, bool includeTones)
    {
        // 共通: null チェック → ToneConverter → PinyinParser
        // → ConvertSyllable(syllable, includeTones)
    }

    protected abstract string GetInitial(Initial initial);
    protected abstract string GetFinal(Final final_);
    protected abstract string GetRetroflexApical();
    protected abstract string GetAlveolarApical();
    protected abstract string GetToneMarker(Tone tone);

    // 共通ロジックを基底に集約
    protected static bool ShouldOmitSemivowel(Initial initial, Final final_) { ... }
    protected static bool IsRetroflex(Initial initial) { ... }
    protected static bool IsAlveolar(Initial initial) { ... }
}
```

**メリット:** 共通ロジックの一元管理。新しい出力フォーマット追加時にマッピングテーブルだけ定義すればよい。
**デメリット:** 既存の `PinyinToIpa` / `PinyinToPiperIpa` が static class であるため、インスタンスベースの基底クラスへの移行は破壊的変更。internal なので外部 API には影響しないが、テストの修正が必要。

### 6.3 設計案 B: ジェネリック変換器 + マッピング構造体

```csharp
internal readonly struct PinyinMapping
{
    public Dictionary<Initial, string> Initials { get; }
    public Dictionary<Final, string> Finals { get; }
    public string RetroflexApical { get; }
    public string AlveolarApical { get; }
    public string[] ToneMarkers { get; }  // Tone enum のインデックスで参照
    public bool IncludeTonesByDefault { get; }
}

internal static class PinyinConverter
{
    public static string Convert(string pinyin, PinyinMapping mapping, bool includeTones) { ... }
    internal static string ConvertSyllable(PinyinSyllable syllable, PinyinMapping mapping, bool includeTones) { ... }
}
```

各フォーマットは `PinyinMapping` インスタンスを提供するだけでよい:

```csharp
internal static class PinyinToIpa
{
    private static readonly PinyinMapping s_mapping = new PinyinMapping { ... };
    public static string Convert(string pinyin) => PinyinConverter.Convert(pinyin, s_mapping, true);
}
```

**メリット:** static class のまま維持できる。既存 API との互換性が高い。マッピングデータと変換ロジックが明確に分離される。
**デメリット:** `PinyinMapping` 構造体に Dictionary を持つため、構造体としては重い（readonly struct にしても参照型フィールドがある）。class にする方が適切。

### 6.4 設計案 C: 共有ヘルパーの分離（最小限のリファクタリング）

完全同一のロジックのみを共有ヘルパーとして抽出し、各クラスの static 構造は維持する:

```csharp
internal static class PinyinConversionHelper
{
    public static bool ShouldOmitSemivowel(Initial initial, Final final_) { ... }
    public static bool IsRetroflex(Initial initial) { ... }
    public static bool IsAlveolar(Initial initial) { ... }
}
```

**メリット:** 最小限の変更で3重コピーの問題を解消。既存テストへの影響がない。
**デメリット:** ConvertSyllable の制御フロー自体は依然としてコピペのまま。

### 6.5 現時点での推奨

現在のプロジェクトでは **設計案 C（共有ヘルパー分離）** が最も現実的。理由:

1. `PinyinToIpa` / `PinyinToPiperIpa` は既に安定稼働中（テスト 936 件通過）であり、大規模リファクタリングのリスクを取る必要がない
2. Misaki 対応で3クラス目が追加される今が共有ヘルパー抽出の自然なタイミング
3. ConvertSyllable の制御フローは各フォーマットで微妙に異なる可能性がある（例: piper-plus は声調マーカーなし、Misaki は矢印、IPA は tone letters）ため、完全な抽象化は過剰

ただし、今後さらに出力フォーマットが増える場合は **設計案 B** への移行を検討すべき。4つ以上のフォーマットが並立する段階が移行の判断基準となる。

### 6.6 テスタビリティ観点の追加レビュー

上記の設計案 A/B/C はいずれもリファクタリング目線（重複排除）に偏っており、**テスタビリティとデバッグ性** の観点での改善余地が残る。以下、QA 視点での追加レビューと設計案を示す。

#### 6.6.1 現状の問題 — internal static パターンのテスト困難性

`PinyinToMisaki` は `internal static class` として実装予定であり、`Convert` / `ConvertSyllable` ともに internal/public static メソッドとなる。これは既存の `PinyinToIpa` / `PinyinToPiperIpa` と同じパターンだが、以下のテスタビリティ上の問題を抱える。

| 問題 | 具体例 | 影響 |
|------|-------|------|
| **モック化不可** | static メソッドはインターフェースを実装できないため、`ChineseG2PEngine` が `PinyinToMisaki.Convert` を直接呼ぶ箇所をユニットテストでモック差し替えできない | エンジン側テストで Misaki 変換の失敗ケースを再現困難 |
| **中間状態の隠蔽** | `ConvertSyllable` 内の声母出力・韻母出力・声調付与の各ステップが `StringBuilder` に逐次追加されるが、途中結果を観察する手段がない | バグ時にどのステップで誤出力が発生したか特定に時間を要する |
| **エラー情報の欠落** | `Convert()` は null チェック/パース失敗時に無言で `string.Empty` を返す設計。呼び出し側はエラー原因（null、空、パース失敗、未定義声母等）を区別できない | 呼び出し側でログ出力・診断ができない。テスト時もエラーパスの識別が困難 |
| **テスト時の可視性確保** | internal メンバーへのアクセスには `InternalsVisibleTo` が必須。テストアセンブリの追加・削除時に忘れやすい | リファクタ後のテスト疎通までに気付きにくい |

#### 6.6.2 設計案 D: IPinyinConverter インターフェース + DI（中〜長期）

internal static から インターフェース + インスタンスベースへ移行する案。

```csharp
public interface IPinyinConverter
{
    string Convert(string pinyin, bool includeTones = true);
    PinyinConversionResult ConvertDetailed(string pinyin, bool includeTones = true);
}

internal sealed class PinyinToMisakiConverter : IPinyinConverter { /* 実装 */ }
internal sealed class PinyinToIpaConverter : IPinyinConverter { /* 実装 */ }
internal sealed class PinyinToPiperIpaConverter : IPinyinConverter { /* 実装 */ }
```

**メリット:**
- `ChineseG2PEngine` 側テストで `IPinyinConverter` をモック化できる（Moq / NSubstitute 等）
- コンストラクタ DI により、テスト時に Fake 実装を注入可能
- 設計案 A / B と自然に組み合わせ可能（基底クラスに IPinyinConverter を実装）
- InternalsVisibleTo が不要になる（public インターフェースを介してテスト）

**デメリット:**
- 既存 `ChineseG2PEngine` が `PinyinToIpa.Convert()` を直接呼んでいるため、フィールド注入＋コンストラクタ変更が必要で破壊的
- Unity 環境では DI コンテナの追加依存を避ける必要がある → 自前でシングルトンファクトリ `PinyinConverterFactory.GetMisaki()` を返す構造が現実的
- インスタンス化のオーバーヘッド（ただしシングルトン運用で無視できる）

**コスト/メリット評価:** 中規模リファクタ（Engine 側の呼び出し箇所〜10 箇所程度の書き換え）。T02 のスコープ外だが、Mi1 マイルストーン完了後の技術的負債として別チケット化を推奨。

#### 6.6.3 設計案 E: ConvertSyllable の戻り値構造体化（中間状態の公開）

`ConvertSyllable` の戻り値を単なる `string` から、中間状態を含む構造体に拡張する案。**最小限の変更で最大のデバッグ性向上** が得られる。

```csharp
internal readonly struct PinyinConversionResult
{
    public string Output { get; }               // 最終文字列（既存互換）
    public string InitialPart { get; }          // 声母部分 (例: "m")
    public string FinalPart { get; }            // 韻母部分 (例: "a")
    public string TonePart { get; }             // 声調矢印/マーカー部分 (例: "↑")
    public bool SemivowelOmitted { get; }       // Y/W 省略が発生したか
    public bool IsRetroflexApical { get; }      // そり舌母音分岐に入ったか
    public bool IsAlveolarApical { get; }       // 歯茎母音分岐に入ったか
    public PinyinSyllable SourceSyllable { get; }  // 入力音節（デバッグ用）

    public override string ToString() => Output;  // 既存コードとの暗黙変換を期待しない
}
```

**公開方針:**
- 既存 `Convert(string)` / `Convert(string, bool)` は `string` 戻り値のまま維持（互換性）
- 新規 `ConvertDetailed(string pinyin, bool includeTones)` メソッドを追加し、構造体を返す
- テストコードは `ConvertDetailed` を呼び、`SemivowelOmitted` や `IsRetroflexApical` を直接アサート可能

**メリット:**
- 「声母が出力された／されなかった」「どの分岐を通ったか」をテストで直接検証できる（現状は最終文字列から逆推論する必要がある）
- 半母音省略の判定ミス（例: `Final.In` なのに j が付く）を `SemivowelOmitted == true` で明示的に検証可能
- そり舌/歯茎母音の分岐バグを `IsRetroflexApical` / `IsAlveolarApical` で直接検出可能
- T03 以降の Engine 側テストでも、「全音節に声調矢印が付いたか」といった集計テストが容易

**デメリット:**
- `ConvertSyllable` の内部実装で各部分を個別に追跡する必要がある（StringBuilder 一本書きから分離）
- 構造体のサイズがやや大きめ（string 3 フィールド + bool 3 フィールド）→ ref return や out 引数で回避可能

**T02 への組み込み推奨度:** **高**。T02 の実装段階でこの構造を採用すれば、追加コストは小さく、ユニットテストの品質が大きく向上する。

#### 6.6.4 設計案 F: エラーハンドリングの粒度強化

現状の「失敗時に無言で string.Empty 返却」は、**呼び出し側での診断不能** という重大な欠陥を抱える。以下の TryConvert パターンを追加することを推奨する。

```csharp
internal enum PinyinConversionError
{
    None = 0,
    NullOrEmpty,          // 入力が null または空文字
    ParseFailed,          // PinyinParser.TryParse が失敗
    UndefinedInitial,     // s_initialMisaki にキーが存在しない
    UndefinedFinal,       // s_finalMisaki にキーが存在しない
    UndefinedToneArrow,   // s_toneArrows にインデックスが存在しない
}

internal static class PinyinToMisaki
{
    public static bool TryConvert(
        string pinyin,
        bool includeTones,
        out string result,
        out PinyinConversionError error)
    {
        if (string.IsNullOrWhiteSpace(pinyin))
        {
            result = string.Empty;
            error = PinyinConversionError.NullOrEmpty;
            return false;
        }
        // ... 以下、エラー種別を区別しながら処理
    }

    // 既存 API は TryConvert への薄いラッパーとして維持
    public static string Convert(string pinyin, bool includeTones = true)
        => TryConvert(pinyin, includeTones, out var r, out _) ? r : string.Empty;
}
```

**テスト上のメリット:**
- エラーケースごとに `Assert.Equal(PinyinConversionError.ParseFailed, error)` で明示的に検証可能
- 「パース失敗」と「未定義声母」はどちらも `string.Empty` を返すが、原因が全く異なる → 現状の API ではテストで区別できない
- 将来 T01 のマッピングテーブルに漏れ（例: 新規韻母追加忘れ）があった場合、`UndefinedFinal` として早期検出できる

**実装上の注意:**
- `s_initialMisaki` / `s_finalMisaki` のルックアップで `TryGetValue` を使用し、失敗時に `UndefinedInitial` / `UndefinedFinal` を返す
- 現在の `Convert` は Dictionary インデクサ (`s_initialIpa[key]`) を使っており、キー欠落時に `KeyNotFoundException` が発生する可能性がある → T02 では `TryGetValue` への移行を検討

#### 6.6.5 設計案 G: デバッグ用トレース出力機能

開発時・バグ再現時に `ConvertSyllable` の各ステップの実行状態をコンソール/ログに出力する機能。

```csharp
internal static class PinyinToMisaki
{
    // DEBUG ビルドでのみ有効化されるトレースフック
    [Conditional("DEBUG")]
    internal static void SetTraceWriter(System.IO.TextWriter? writer)
    {
        s_traceWriter = writer;
    }

    private static System.IO.TextWriter? s_traceWriter;

    [Conditional("DEBUG")]
    private static void Trace(string step, string detail)
    {
        s_traceWriter?.WriteLine($"[PinyinToMisaki] {step}: {detail}");
    }

    internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones)
    {
        Trace("Input", $"Initial={syllable.Initial}, Final={syllable.Final}, Tone={syllable.Tone}");

        var sb = new StringBuilder(16);
        if (syllable.Initial != Initial.None)
        {
            if ((syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                && ShouldOmitSemivowel(syllable.Initial, syllable.Final))
            {
                Trace("SemivowelOmit", $"{syllable.Initial} omitted for {syllable.Final}");
            }
            else
            {
                var initialStr = s_initialMisaki[syllable.Initial];
                sb.Append(initialStr);
                Trace("InitialOut", initialStr);
            }
        }
        // ... 以下同様
        return sb.ToString();
    }
}
```

**メリット:**
- `Conditional("DEBUG")` により Release ビルドでは完全に除去される（ゼロコスト）
- テストコードから `StringWriter` を注入してトレース出力をキャプチャし、`Assert.Contains("SemivowelOmit", trace)` のように検証可能
- バグ報告時に再現コードを渡せば、どのステップで期待外れの挙動が起きたか即座に特定できる

**デメリット:**
- `[Conditional]` はコンパイル時分岐のため、NuGet パッケージを利用する側（Release ビルド）ではトレースが無効化される → 再現調査には DEBUG ビルドの再実行が必要
- テストアセンブリ自体は DEBUG ビルドで実行されるため、ユニットテストでのトレース検証は問題なく動作

#### 6.6.6 InternalsVisibleTo 設定の確認

T02 で `ConvertSyllable` を internal にする場合、既存のテストアセンブリへの可視性を確認する必要がある。

**確認事項:**
1. `src/DotNetG2P.Chinese/DotNetG2P.Chinese.csproj` または `AssemblyInfo.cs` に以下の属性があるか:
   ```csharp
   [assembly: InternalsVisibleTo("DotNetG2P.Tests")]
   ```
2. 既存の `PinyinToIpaTests` / `PinyinToPiperIpaTests` がどのようにテストしているか確認（`public static` を介しているか、`InternalsVisibleTo` を活用しているか）
3. 新規テスト `PinyinToMisakiTests` が `ConvertSyllable(syllable, includeTones)` を直接呼びたい場合、この属性が必須

**T02 実装前チェック:** `grep -r "InternalsVisibleTo" src/DotNetG2P.Chinese/` で既存設定を確認し、テストから internal メソッドへの到達経路を事前に保証すること。

#### 6.6.7 推奨優先度まとめ

| 設計案 | T02 への組込 | コスト | メリット | 推奨 |
|--------|------------|--------|---------|------|
| D: IPinyinConverter + DI | スコープ外 | 中 | モック化・疎結合 | 別チケット化 |
| **E: 戻り値構造体化 (ConvertDetailed)** | **T02 内で対応** | **小** | **中間状態のテスト検証** | **強く推奨** |
| **F: TryConvert + エラー列挙** | **T02 内で対応** | **小** | **エラーパスの明示的検証** | **強く推奨** |
| G: Conditional トレース | T02 内で対応可 | 小 | デバッグ時の可視性 | 推奨 |
| D と組み合わせる case | 設計案 B と融合 | 中 | テスト/実装両面で疎結合 | 中期的に検討 |

**T02 実装時のアクションアイテム:**
1. `ConvertSyllable` の戻り値を `PinyinConversionResult` 構造体化し、`ConvertDetailed` を internal で公開（設計案 E）
2. `TryConvert(out string, out PinyinConversionError)` を追加し、既存 `Convert` はその薄いラッパーとして実装（設計案 F）
3. Dictionary ルックアップを `TryGetValue` に統一し、キー欠落を `UndefinedInitial` / `UndefinedFinal` として返却
4. `[Conditional("DEBUG")] Trace` フックを配置（設計案 G）
5. `InternalsVisibleTo("DotNetG2P.Tests")` の存在を実装前に確認
6. ユニットテストで `SemivowelOmitted` / `IsRetroflexApical` / `PinyinConversionError` の各プロパティを明示的にアサートするケースを追加（セクション 4.2 のテスト項目を拡張）

これらの対応は T02 のスコープ内で実現可能であり、既存の `PinyinToIpa` / `PinyinToPiperIpa` に比べテスタビリティが大きく向上する。同時に、T02 で確立したパターンを既存 2 クラスに逆適用する後続チケット化も視野に入れるべきである。

### 6.7 アーキテクトレビュー（統合的まとめ）

既存 6.1〜6.6 は「共通ヘルパー分離」「インターフェース DI」「戻り値構造体化」「エラー列挙」「トレース」と個別の改善案が並列に提示されているが、**T02 の本質的な課題は「4 クラス目のコピペをどう抽象化するか」**である。本節では C# 言語機能の観点から 4 つの統合的アプローチを比較し、T02 の実装時に採用すべき方針を確定する。

#### 6.7.1 現状整理 — 何がコピペなのか

`PinyinToIpa` / `PinyinToPiperIpa` の構造を具体的に分解すると、以下の 4 層にコピペが発生している:

| 層 | 内容 | 完全同一か | 差し替え箇所 |
|---|-----|---------|------------|
| L1: 入口処理 | `Convert(string)` の null チェック → `ToneConverter.ToToneMarked` → `PinyinParser.TryParse` → `ConvertSyllable` 委譲 | **完全同一** | なし |
| L2: 分岐制御フロー | `ConvertSyllable` の声母判定 → 半母音省略 → 韻母判定 → そり舌/歯茎分岐 → 声調付与 | **完全同一** | なし |
| L3: 判定ヘルパー | `ShouldOmitSemivowel` / `IsRetroflex` / `IsAlveolar` | **完全同一** | なし |
| L4: マッピングテーブル | `s_initialIpa` / `s_finalIpa` / `s_toneLetters` / `s_retroflexApical` / `s_alveolarApical` | 差異あり | **全行** |

L1〜L3 は **4 クラスでも完全同一** であり、T02 で 3 クラス目（PinyinToMisaki）を実装すると、L1〜L3 が 3 重コピーになる。本質的に差し替えたいのは L4 のみであり、L1〜L3 は「データを引数として受け取る純粋関数」にすべきである。

#### 6.7.2 C# 言語機能ベース 4 方式の比較（T02 視点）

##### 方式 1: interface ベース（DI/モック化重視）

既存 6.6.2 の設計案 D で詳述済み。要点は以下:

```csharp
public interface IPinyinConverter
{
    string Convert(string pinyin, bool includeTones);
}

internal sealed class PinyinToMisakiConverter : IPinyinConverter
{
    private static readonly Dictionary<Initial, string> s_initials = /* ... */;

    public string Convert(string pinyin, bool includeTones)
    {
        // L1〜L3 の処理を毎回各クラスで書く必要がある
        // → default interface methods で共通化可能だが、.NET Standard 2.1 では実装上の制約あり
    }
}
```

**致命的な欠点:** interface だけでは L1〜L3 の共通ロジックを基底に置けない（default interface methods は .NET Standard 2.1 で限定的にしか使えず、Unity 2021.2 の IL2CPP との相性も不透明）。結局各実装クラスで L1〜L3 を再実装するため、**コピペ問題が解決しない**。

##### 方式 2: abstract class ベース（既存 A 案の詳細化）

```csharp
internal abstract class PinyinConverterBase
{
    // L4: 差し替えたい部分を abstract で公開
    protected abstract IReadOnlyDictionary<Initial, string> InitialMap { get; }
    protected abstract IReadOnlyDictionary<Final, string> FinalMap { get; }
    protected abstract string[] ToneMarkers { get; }
    protected abstract string RetroflexApical { get; }
    protected abstract string AlveolarApical { get; }
    protected virtual bool IncludeTonesByDefault => true;

    // L1: 入口処理（完全共通、override 不要）
    public string Convert(string pinyin) => Convert(pinyin, IncludeTonesByDefault);

    public string Convert(string pinyin, bool includeTones)
    {
        if (string.IsNullOrEmpty(pinyin)) return string.Empty;
        string normalized = ToneConverter.ToToneMarked(pinyin);
        if (!PinyinParser.TryParse(normalized, out var syllable)) return string.Empty;
        return ConvertSyllable(syllable, includeTones);
    }

    // L2: 分岐制御フロー（完全共通、override 不要）
    protected string ConvertSyllable(PinyinSyllable syllable, bool includeTones)
    {
        var sb = new StringBuilder(16);

        if (syllable.Initial != Initial.None)
        {
            var skipSemivowel =
                (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                && ShouldOmitSemivowel(syllable.Initial, syllable.Final);
            if (!skipSemivowel) sb.Append(InitialMap[syllable.Initial]);
        }

        if (syllable.Final != Final.None)
        {
            sb.Append((syllable.Final, syllable.Initial) switch
            {
                (Final.I, var i) when IsRetroflex(i) => RetroflexApical,
                (Final.I, var i) when IsAlveolar(i) => AlveolarApical,
                _ => FinalMap[syllable.Final],
            });
        }

        if (includeTones && syllable.Tone != Tone.Neutral)
            sb.Append(ToneMarkers[(int)syllable.Tone]);

        return sb.ToString();
    }

    // L3: 判定ヘルパー（完全共通、基底に集約）
    protected static bool ShouldOmitSemivowel(Initial initial, Final final_) { /* ... */ }
    protected static bool IsRetroflex(Initial initial) { /* ... */ }
    protected static bool IsAlveolar(Initial initial) { /* ... */ }
}

internal sealed class MisakiConverter : PinyinConverterBase
{
    private static readonly Dictionary<Initial, string> s_initials = /* ... */;
    private static readonly Dictionary<Final, string> s_finals = /* ... */;
    private static readonly string[] s_tones = /* ... */;

    protected override IReadOnlyDictionary<Initial, string> InitialMap => s_initials;
    protected override IReadOnlyDictionary<Final, string> FinalMap => s_finals;
    protected override string[] ToneMarkers => s_tones;
    protected override string RetroflexApical => "\u027B\u0329";
    protected override string AlveolarApical => "\u0279\u0329";
}

// 既存 API 互換のファサード
internal static class PinyinToMisaki
{
    private static readonly MisakiConverter s_instance = new MisakiConverter();
    public static string Convert(string pinyin) => s_instance.Convert(pinyin);
    public static string Convert(string pinyin, bool includeTones) => s_instance.Convert(pinyin, includeTones);
}
```

**利点:**
- L1〜L3 の **3 層が完全に基底に集約** され、派生クラスは L4 のマッピングテーブル（5 行の override）のみを書けばよい
- コピペ量が 4 クラス分から 1 クラス分 × 4 マッピングに圧縮される
- 既存 `PinyinToIpa.Convert(...)` 等の公開 API は **ファサード経由で完全維持** できる（破壊的変更なし）
- テスト時は `MisakiConverter` インスタンスを直接生成するか、`PinyinToMisaki` ファサード経由でアクセス
- 派生クラスを `sealed` にすることで、さらなる派生によるバグを防止

**欠点:**
- インスタンスメソッド呼び出しのオーバーヘッド（シングルトンで毎回同一インスタンスを返すため実質無視可能）
- 既存 `PinyinToPiperIpa.ConvertToPhonemes`（声母/韻母を配列で返すメソッド）のような**クラス固有拡張**が基底では表現できない → 派生クラスに追加メソッドとして実装する必要あり

##### 方式 3: record + switch 式ベース（関数型アプローチ）

方式 2 の「クラス継承」を「record によるデータ表現 + 静的純粋関数」に置き換える。T01 の 6.5.2 方式 3 と同じ考え方を T02 側で実装する。

```csharp
// L4: マッピングを不変レコードで表現
internal sealed record PinyinMappingTable(
    IReadOnlyDictionary<Initial, string> Initials,
    IReadOnlyDictionary<Final, string> Finals,
    IReadOnlyList<string> ToneMarkers,
    string RetroflexApical,
    string AlveolarApical,
    bool IncludeTonesByDefault);

// L1〜L3: 純粋関数として実装（静的クラス）
internal static class PinyinConversionEngine
{
    public static string Convert(string pinyin, PinyinMappingTable table)
        => Convert(pinyin, table, table.IncludeTonesByDefault);

    public static string Convert(string pinyin, PinyinMappingTable table, bool includeTones)
    {
        if (string.IsNullOrEmpty(pinyin)) return string.Empty;
        string normalized = ToneConverter.ToToneMarked(pinyin);
        if (!PinyinParser.TryParse(normalized, out var syllable)) return string.Empty;
        return ConvertSyllable(syllable, table, includeTones);
    }

    internal static string ConvertSyllable(PinyinSyllable syllable, PinyinMappingTable table, bool includeTones)
    {
        var sb = new StringBuilder(16);

        if (syllable.Initial != Initial.None
            && !(IsSemivowel(syllable.Initial) && ShouldOmitSemivowel(syllable.Initial, syllable.Final)))
        {
            sb.Append(table.Initials[syllable.Initial]);
        }

        if (syllable.Final != Final.None)
        {
            sb.Append((syllable.Final, syllable.Initial) switch
            {
                (Final.I, var i) when IsRetroflex(i) => table.RetroflexApical,
                (Final.I, var i) when IsAlveolar(i) => table.AlveolarApical,
                _ => table.Finals[syllable.Final],
            });
        }

        if (includeTones && syllable.Tone != Tone.Neutral)
            sb.Append(table.ToneMarkers[(int)syllable.Tone]);

        return sb.ToString();
    }

    // L3: 判定ヘルパーも純粋関数として static に
    private static bool IsSemivowel(Initial i) => i == Initial.Y || i == Initial.W;
    private static bool ShouldOmitSemivowel(Initial i, Final f) { /* ... */ }
    private static bool IsRetroflex(Initial i) { /* ... */ }
    private static bool IsAlveolar(Initial i) { /* ... */ }
}

// ファサード: マッピングテーブルを引数として渡すだけ
internal static class PinyinToMisaki
{
    private static readonly PinyinMappingTable s_table = PinyinMappingTables.Misaki;
    public static string Convert(string pinyin) => PinyinConversionEngine.Convert(pinyin, s_table);
    public static string Convert(string pinyin, bool includeTones) => PinyinConversionEngine.Convert(pinyin, s_table, includeTones);
}
```

**利点:**
- **継承を使わない**: データ（record）とロジック（static 関数）を完全分離
- `Ipa with { ... }` 構文で Misaki テーブルを差分定義可能（T01 側と整合）
- **switch 式の網羅性チェック**: コンパイラが `(Final, Initial)` タプルのパターンマッチを検証
- 純粋関数のためテストが極めて容易（副作用なし、状態なし）
- 既存 `static class` 様式との整合性が最も高い
- 将来の source generator 統合時、TSV からの生成ターゲットは `PinyinMappingTable` のインスタンス初期化のみ → 生成コードが最小化

**欠点:**
- 現状 C# LangVersion の確認が必要（`record` は C# 9.0+）
- 引数 `table` を毎回渡す冗長性（実際はファサード経由のため、ユーザーには見えない）

##### 方式 4: source generator ベース

T01 の F 案と同様。TSV ファイルからコンパイル時に `PinyinMappingTable` のインスタンス初期化コードを生成する。方式 3 との組み合わせで威力を発揮する（方式 3 が「受け皿」となる）。ただし Unity IL2CPP 互換性検証コストが高く、本チケットでは採用見送り。

#### 6.7.3 4 方式の比較表（T02 視点）

| 観点 | 方式 1: interface | 方式 2: abstract class | **方式 3: record+switch** | 方式 4: generator |
|------|----------------|--------------------|-----------------------|------------------|
| L1〜L3 共通化の実現 | **不可**（DIM 制約） | 可（基底に集約） | **可（純粋関数に集約）** | 可 |
| L4 差し替えの書き心地 | 手動 | 手動 override | **`with` 式で差分** | 自動生成 |
| 既存 `internal static` 様式との整合 | 低 | 中（ファサード経由で維持） | **高（static のまま）** | 高 |
| 既存公開 API 互換 | 要書換 | 維持可（ファサード） | **維持可（ファサード）** | 維持可 |
| 派生クラス固有拡張 (`ConvertToPhonemes`) | 可（インスタンスメソッド追加） | 可（インスタンスメソッド追加） | **可（静的メソッド追加）** | 可 |
| コンパイル時型安全性 | 中 | 中（override 漏れ） | **高（switch 網羅）** | 高 |
| テスト容易性 (L1〜L3 単体テスト) | 中（モック可） | 中（基底のテストが必要） | **高（純粋関数）** | 中 |
| ランタイムコスト | 中（仮想呼び出し） | 中（仮想呼び出し） | **低（static 呼び出し）** | 最低 |
| リファクタ規模 (3 クラス同時改修) | 大 | 中 | **中** | 大 |
| Unity/IL2CPP 互換 | 高 | 高 | **高** | 要検証 |
| 追加形式 5 つ目の工数 | 中 | **小** | **小** | 最小 |

**方式 3（record + switch 式）が総合的に最有力。**

#### 6.7.4 既存「設計案 E（戻り値構造体化）」との統合

既存 6.6.3 で提案されている `PinyinConversionResult` 構造体（中間状態を含む戻り値）は、方式 3 と自然に統合できる。

```csharp
internal readonly record struct PinyinConversionResult(
    string Output,
    string InitialPart,
    string FinalPart,
    string TonePart,
    bool SemivowelOmitted,
    bool IsRetroflexApical,
    bool IsAlveolarApical,
    PinyinSyllable SourceSyllable)
{
    public static implicit operator string(PinyinConversionResult r) => r.Output;
}

internal static class PinyinConversionEngine
{
    // 既存互換の string 戻り値版
    public static string Convert(string pinyin, PinyinMappingTable table, bool includeTones)
        => ConvertDetailed(pinyin, table, includeTones).Output;

    // 詳細戻り値版
    public static PinyinConversionResult ConvertDetailed(string pinyin, PinyinMappingTable table, bool includeTones)
    {
        // ... 中間状態を追跡しつつ変換
    }
}
```

`readonly record struct` を使うことで、値型の軽量性を保ちつつ `with` 式での部分更新や、コンパイラによる等価性実装を享受できる。

#### 6.7.5 T02 実装時の統合的推奨

本チケット既存セクション 6.5（現時点での推奨）では「設計案 C（共有ヘルパー分離）」が推奨されているが、アーキテクトレビューの結果、**以下の修正推奨を提示する**。

##### 短期（T02 実装時）: 設計案 C を採用しつつ、方式 3 への移行準備を整える

**やること:**

1. **L3 判定ヘルパーを `Internal/PinyinConversionHelper.cs` に抽出**（既存案 C）
   - `ShouldOmitSemivowel`, `IsRetroflex`, `IsAlveolar` を `internal static` メソッドとして配置
   - `PinyinToIpa` / `PinyinToPiperIpa` / `PinyinToMisaki` の 3 クラスから呼び出す
   - 既存 2 クラスから重複定義を削除（T02 スコープ内で軽微な書換）

2. **`PinyinToMisaki.Convert` / `ConvertSyllable` は既存 2 クラスと構造を**完全に揃える**
   - エントリの順序、コメント様式、変数名、`StringBuilder` の初期容量まで統一
   - 将来 `PinyinConversionEngine` に抽出する際、3 クラスの `ConvertSyllable` 実装が文字単位で一致するよう書く
   - 差異は L4（マッピングテーブル）の参照名のみに集約

3. **Dictionary ルックアップを `TryGetValue` に統一**（既存案 F の部分採用）
   - `s_initialMisaki[key]` ではなく `s_initialMisaki.TryGetValue(key, out var value)` を使用
   - キー欠落時のフォールバック（例: 空文字返却 or `KeyNotFoundException` 再スロー）を明示化
   - 既存 2 クラスも将来的に同じ方式に揃える

**やらないこと:**

- 設計案 E（`PinyinConversionResult` 構造体化）は T02 スコープ外にする。理由: 3 クラス分のリファクタ負荷が大きく、Mi1 マイルストーン全体を遅延させる
- 設計案 F（完全な `TryConvert` エラー列挙）は見送る。理由: エラーパスの詳細化は ChineseG2PEngine レベルで扱うほうが自然で、変換クラス単体での必要性が低い

##### 中期（Mi1 完了後、別チケット化）: 方式 3（record + switch 式）への移行

**新規チケット「PinyinConverter 共通抽象化リファクタ」を起票し、以下を実施:**

1. `PinyinMappingTable` record の導入
2. `PinyinConversionEngine.Convert(syllable, table, includeTones)` の抽出
3. `PinyinMappingTables.Ipa` / `PiperIpa` / `Misaki` の定義（`Ipa with { ... }` 構文で差分記述）
4. `PinyinToIpa` / `PinyinToPiperIpa` / `PinyinToMisaki` をファサードに変更（既存 public API は維持）
5. `PinyinToPiperIpa.ConvertToPhonemes` は `PinyinConversionEngine.ConvertToPhonemes(syllable, table)` に一般化
6. 既存 936 件 + Misaki 追加分のテストが全件通過することで安全性を保証
7. `PinyinConversionResult` 構造体化（既存案 E）も同時実施し、テスタビリティを向上

##### 長期（5 形式目追加時）: 方式 4（source generator）への移行を再検討

- Unity Roslyn Generator サポート状況を再確認
- TSV 方式（既存案 D）と組み合わせ、マスターデータの一元管理を実現
- 方式 3 の `PinyinMappingTable` を受け皿として活用

#### 6.7.6 アーキテクチャ上の本質的な教訓

本レビューで明確化された本質的な教訓を、今後の設計判断に活用するために記録する:

1. **「コピペ」は一層ではない。L1〜L4 の各層で異なる抽象化戦略が必要**
   - L1（入口処理）と L2（分岐制御）は純粋関数化が最適
   - L3（判定ヘルパー）は static ヘルパークラスが最適
   - L4（マッピングデータ）は record + `with` 式が最適

2. **`internal static class` パターンは「値と関数の分離」で抽象化できる**
   - インスタンスベース（interface/abstract class）への移行は必須ではない
   - データ（record）と関数（static）を分離することで、static 様式を保ったまま抽象化できる

3. **`PinyinToZhuyin` を抽象化の対象から除外する判断**
   - 構造が本質的に異なる（`string` キー、enum 非経由）
   - 全てを「1 つのパターンで統一」する必要はない
   - 「IPA ファミリ」という部分集合に対する抽象化にとどめるべき

4. **既存テストが 936 件ある状況でのリファクタは「ファサード保持」が鉄則**
   - 内部構造は大胆に変更してよいが、public/internal の API 面は維持する
   - ファサードパターンを挟むことで、テストの書き換えを最小化できる

5. **段階的移行の工程表を明示することで、短期・中期・長期の意思決定を整合させる**
   - 「今はやらないが、将来やる」という意思を明示することで、現在の設計判断（コピペ容認）が正当化される
   - 「将来の移行を機械的に可能にする」ための現在の制約（エントリ順序統一、命名規則統一）を明示的に課す

## 7. 後続タスクへの連絡事項

T03（ChineseG2PEngine への ToMisaki API 追加）に伝えるべき情報:

1. **Convert のシグネチャ**: `PinyinToMisaki.Convert(string pinyin)` と `PinyinToMisaki.Convert(string pinyin, bool includeTones)` の2つのオーバーロードが利用可能。前者は `includeTones = true` で委譲する
2. **ConvertSyllable の可視性**: `internal static` であるため、同一アセンブリ (`DotNetG2P.Chinese`) 内から直接呼び出し可能。ChineseG2PEngine の `RunPipeline` ラムダから `p => PinyinToMisaki.Convert(p)` の形で使用できる（`PinyinToIpa.Convert(p, includeTones)` と同一パターン）
3. **声調制御**: `includeTones` パラメータで声調矢印の有無を制御可能。ChineseG2PEngine の `ToMisaki(string text)` / `ToMisaki(string text, bool includeTones)` オーバーロードに対応付けること
4. **ConvertToPhonemes 未実装**: `PinyinToPiperIpa` にある `ConvertToPhonemes()` メソッド（声母と韻母を分離した配列を返す）に相当するものは T02 のスコープ外。Misaki で音素単位処理やProsody処理が必要な場合は別途タスクを起票すること
5. **バッチ API**: `ConvertBatch` 等のバッチ変換 API は T02 のスコープ外。ChineseG2PEngine 側で `BatchConversionHelper` を使用するパターン（既存の `ToIpaBatch` 等と同一）で T03 内に実装すること

## 8. 紐づけ

- **マイルストーン**: Mi1（Misaki 互換中国語出力）
- **依存**: T01（マッピングテーブル定義）— T01 の `s_initialMisaki`, `s_finalMisaki`, `s_toneArrows`, `s_retroflexApical`, `s_alveolarApical` が定義済みであること
- **後続**: T03（ChineseG2PEngine API 追加）— Convert メソッドが完成していることが前提
