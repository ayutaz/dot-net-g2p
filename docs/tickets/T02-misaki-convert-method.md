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
