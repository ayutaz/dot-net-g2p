---
ticket: T01
title: PinyinToMisaki マッピングテーブル設計・実装
milestone: Mi1
status: 未着手
depends_on: []
blocks: [T02]
---

# T01: PinyinToMisaki マッピングテーブル設計・実装

## 1. タスク目的とゴール

### 背景

Kokoro TTS の G2P フロントエンド Misaki は、中国語音素表記に独自の IPA バリアント（破擦音の合字記号使用、二重母音の非音節化符号、矢印型声調記号）を採用している。DotNetG2P.Chinese は現在 3 種類の出力形式（標準 IPA、piper-plus 互換 IPA、注音符号）を提供しているが、Misaki 互換形式には未対応であり、Kokoro TTS ユーザーが DotNetG2P を G2P フロントエンドとして利用できない状況にある（Issue #56）。

### ゴール

既存の `PinyinToIpa.cs` / `PinyinToPiperIpa.cs` / `PinyinToZhuyin.cs` と同じ変換クラスパターンで `PinyinToMisaki.cs` を新規作成するための、**声母・韻母・声調の全マッピングテーブルを確定する**。本チケットのスコープはテーブル定義のみであり、Convert メソッドの統合は後続 T02 で行う。

### 達成基準

- 声母 22 エントリ、韻母 32 エントリ、声調 5 エントリすべてのマッピングが確定し、コードに `Dictionary<Initial, string>` / `Dictionary<Final, string>` / `string[]` として実装されていること
- PinyinToIpa との差異が明確にドキュメント化されていること
- 全マッピングのユニットテストが通過すること

## 2. 実装する内容の詳細

### 2.1 声母テーブル（22 エントリ）

`PinyinToIpa.cs` の `s_initialIpa` を基準とし、Misaki で異なる表記を使用する箇所を太字で示す。

| # | Initial enum | ピンイン | PinyinToIpa（標準 IPA） | PinyinToMisaki | Unicode シーケンス | 差異 |
|---|-------------|---------|------------------------|----------------|-------------------|------|
| 1 | `B` | b | p | p | `p` | |
| 2 | `P` | p | ph | ph | `p\u02B0` | |
| 3 | `M` | m | m | m | `m` | |
| 4 | `F` | f | f | f | `f` | |
| 5 | `D` | d | t | t | `t` | |
| 6 | `T` | t | th | th | `t\u02B0` | |
| 7 | `N` | n | n | n | `n` | |
| 8 | `L` | l | l | l | `l` | |
| 9 | `G` | g | k | k | `k` | |
| 10 | `K` | k | kh | kh | `k\u02B0` | |
| 11 | `H` | h | x | x | `x` | |
| 12 | `J` | j | t\u0255 (tc) | **\u02A8** (**cc**) | **`\u02A8`** | **tc -> cc (U+02A8 合字)** |
| 13 | `Q` | q | t\u0255\u02B0 (tch) | **\u02A8\u02B0** (**cch**) | **`\u02A8\u02B0`** | **tch -> cch (U+02A8 合字 + 有気)** |
| 14 | `X` | x | \u0255 (c) | c | `\u0255` | |
| 15 | `Zh` | zh | \u0288\u0282 (ts) | \u0288\u0282 (ts) | `\u0288\u0282` | |
| 16 | `Ch` | ch | \u0288\u0282\u02B0 (tsh) | \u0288\u0282\u02B0 (tsh) | `\u0288\u0282\u02B0` | |
| 17 | `Sh` | sh | \u0282 (s) | \u0282 (s) | `\u0282` | |
| 18 | `R` | r | \u027B (r) | \u027B (r) | `\u027B` | |
| 19 | `Z` | z | ts | **\u02A6** (**ts**) | **`\u02A6`** | **ts 2文字 -> U+02A6 合字** |
| 20 | `C` | c | ts\u02B0 (tsh) | **\u02A6\u02B0** (**tsh**) | **`\u02A6\u02B0`** | **tsh -> U+02A6 合字 + 有気** |
| 21 | `S` | s | s | s | `s` | |
| 22 | `Y` | y | j | j | `j` | |
| 23 | `W` | w | w | w | `w` | |

**差異まとめ（声母）:**

- **j** (Initial.J): 標準 IPA `tɕ` (U+0074 U+0255) -> Misaki `ʨ` (U+02A8、ラテン小文字 TC ダイグラフ)
- **q** (Initial.Q): 標準 IPA `tɕʰ` (U+0074 U+0255 U+02B0) -> Misaki `ʨʰ` (U+02A8 U+02B0)
- **z** (Initial.Z): 標準 IPA `ts` (U+0074 U+0073) -> Misaki `ʦ` (U+02A6、ラテン小文字 TS ダイグラフ)
- **c** (Initial.C): 標準 IPA `tsʰ` (U+0074 U+0073 U+02B0) -> Misaki `ʦʰ` (U+02A6 U+02B0)

### 2.2 韻母テーブル（32 エントリ）

`PinyinToIpa.cs` の `s_finalIpa` を基準とし、Misaki で異なる表記を使用する箇所を太字で示す。Misaki の主な差異は、二重母音の滑り音（off-glide/on-glide）に非音節化符号 (U+032F, COMBINING INVERTED BREVE BELOW) を付与する点にある。具体的には `ɪ` -> `i̯`、`ʊ` -> `u̯` に変換される。

| # | Final enum | ピンイン | PinyinToIpa（標準 IPA） | PinyinToMisaki | Unicode シーケンス | 差異 |
|---|-----------|---------|------------------------|----------------|-------------------|------|
| 1 | `A` | a | a | a | `a` | |
| 2 | `O` | o | o | o | `o` | |
| 3 | `E` | e | \u0264 (ɤ) | \u0264 (ɤ) | `\u0264` | |
| 4 | `Ai` | ai | a\u026A (aɪ) | **ai\u032F** (**ai̯**) | **`ai\u032F`** | **ɪ -> i + 非音節化符号** |
| 5 | `Ei` | ei | e\u026A (eɪ) | **ei\u032F** (**ei̯**) | **`ei\u032F`** | **ɪ -> i + 非音節化符号** |
| 6 | `Ao` | ao | a\u028A (aʊ) | **au\u032F** (**au̯**) | **`au\u032F`** | **ʊ -> u + 非音節化符号** |
| 7 | `Ou` | ou | o\u028A (oʊ) | **ou\u032F** (**ou̯**) | **`ou\u032F`** | **ʊ -> u + 非音節化符号** |
| 8 | `An` | an | an | an | `an` | |
| 9 | `En` | en | \u0259n (ən) | \u0259n (ən) | `\u0259n` | |
| 10 | `Ang` | ang | a\u014B (aŋ) | a\u014B (aŋ) | `a\u014B` | |
| 11 | `Eng` | eng | \u0259\u014B (əŋ) | \u0259\u014B (əŋ) | `\u0259\u014B` | |
| 12 | `Ong` | ong | \u028A\u014B (ʊŋ) | **u\u032F\u014B** (**u̯ŋ**) | **`u\u032F\u014B`** | **ʊ -> u + 非音節化符号** |
| 13 | `I` | i | i | i | `i` | |
| 14 | `Ia` | ia | ia | ia | `ia` | |
| 15 | `Ie` | ie | i\u025B (iɛ) | i\u025B (iɛ) | `i\u025B` | |
| 16 | `Iao` | iao | ia\u028A (iaʊ) | **iau\u032F** (**iau̯**) | **`iau\u032F`** | **ʊ -> u + 非音節化符号** |
| 17 | `Iu` | iu (iou) | io\u028A (ioʊ) | **iou\u032F** (**iou̯**) | **`iou\u032F`** | **ʊ -> u + 非音節化符号** |
| 18 | `Ian` | ian | i\u025Bn (iɛn) | i\u025Bn (iɛn) | `i\u025Bn` | |
| 19 | `In` | in | in | in | `in` | |
| 20 | `Iang` | iang | ia\u014B (iaŋ) | ia\u014B (iaŋ) | `ia\u014B` | |
| 21 | `Ing` | ing | i\u014B (iŋ) | i\u014B (iŋ) | `i\u014B` | |
| 22 | `Iong` | iong | i\u028A\u014B (iʊŋ) | **iu\u032F\u014B** (**iu̯ŋ**) | **`iu\u032F\u014B`** | **ʊ -> u + 非音節化符号** |
| 23 | `U` | u | u | u | `u` | |
| 24 | `Ua` | ua | ua | ua | `ua` | |
| 25 | `Uo` | uo | uo | uo | `uo` | |
| 26 | `Uai` | uai | ua\u026A (uaɪ) | **uai\u032F** (**uai̯**) | **`uai\u032F`** | **ɪ -> i + 非音節化符号** |
| 27 | `Ui` | ui (uei) | ue\u026A (ueɪ) | **uei\u032F** (**uei̯**) | **`uei\u032F`** | **ɪ -> i + 非音節化符号** |
| 28 | `Uan` | uan | uan | uan | `uan` | |
| 29 | `Un` | un (uen) | u\u0259n (uən) | u\u0259n (uən) | `u\u0259n` | |
| 30 | `Uang` | uang | ua\u014B (uaŋ) | ua\u014B (uaŋ) | `ua\u014B` | |
| 31 | `Ueng` | ueng | u\u0259\u014B (uəŋ) | u\u0259\u014B (uəŋ) | `u\u0259\u014B` | |
| 32 | `V` | u | y | y | `y` | |
| 33 | `Ve` | ue | y\u025B (yɛ) | y\u025B (yɛ) | `y\u025B` | |
| 34 | `Van` | uan | yan | yan | `yan` | |
| 35 | `Vn` | un | yn | yn | `yn` | |
| 36 | `Er` | er | \u0259\u027B (əɻ) | \u0259\u027B (əɻ) | `\u0259\u027B` | |

**差異まとめ（韻母）:**

全 7 箇所の差異は以下の 2 パターンに分類される:

1. **ɪ (U+026A) -> i + 非音節化符号 (U+032F)**: Ai, Ei, Uai, Ui の 4 韻母
2. **ʊ (U+028A) -> u + 非音節化符号 (U+032F)**: Ao, Ou, Ong, Iao, Iu, Iong の 6 韻母

**特殊韻母（そり舌・歯茎）について:**

PinyinToIpa では `zh/ch/sh/r + i` をそり舌母音 `ɻ̩` (U+027B U+0329)、`z/c/s + i` を歯茎母音 `ɹ̩` (U+0279 U+0329) に変換するが、Misaki ではこれらをそのまま踏襲する（変更なし）。

### 2.3 声調テーブル（5 エントリ）

PinyinToIpa が IPA tone letters を使用するのに対し、Misaki は矢印記号を使用する。

| # | Tone enum | 声調名 | PinyinToIpa（IPA tone letters） | PinyinToMisaki（矢印記号） | Unicode シーケンス | 差異 |
|---|----------|-------|-------------------------------|--------------------------|-------------------|------|
| 1 | `Neutral` (0) | 軽声 | (なし) | (なし) | `""` | |
| 2 | `First` (1) | 陰平 (55) | **\u02E5\u02E5** (**˥˥**) | **\u2192** (**→**) | **`\u2192`** | **tone letters -> 矢印** |
| 3 | `Second` (2) | 陽平 (35) | **\u02E7\u02E5** (**˧˥**) | **\u2197** (**↗**) | **`\u2197`** | **tone letters -> 矢印** |
| 4 | `Third` (3) | 上声 (214) | **\u02E8\u02E9\u02E6** (**˨˩˦**) | **\u2193** (**↓**) | **`\u2193`** | **tone letters -> 矢印** |
| 5 | `Fourth` (4) | 去声 (51) | **\u02E5\u02E9** (**˥˩**) | **\u2198** (**↘**) | **`\u2198`** | **tone letters -> 矢印** |

**差異まとめ（声調）:**

全 4 声調（軽声を除く）が異なる。IPA tone letters（複数文字の声調レベル記号）から、単一の Unicode 矢印記号に変更される。

### 2.4 実装ファイル

**新規作成:** `src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs`

```csharp
internal static class PinyinToMisaki
{
    // 声母テーブル: Dictionary<Initial, string>
    // 韻母テーブル: Dictionary<Final, string>
    // 声調テーブル: string[]
    // そり舌母音・歯茎母音: PinyinToIpa と同一
}
```

テーブルのみを定義し、Convert メソッドは T02 で実装する。ただし、テーブル参照のための internal static なアクセサ（`GetInitialIpa`, `GetFinalIpa`, `GetToneMarker` 等）は本チケットで定義してもよい。

## 3. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 名 | `PinyinToMisaki.cs` のテーブル定義コード作成、Unicode エスケープシーケンスの正確な記述 |
| テストエージェント | 1 名 | マッピングテーブルの全エントリに対するユニットテスト作成 |
| レビューエージェント | 1 名 | Unicode コードポイントの正確性検証、Misaki 公式実装との照合、言語学的正確性確認 |

**合計: 3 名**

実装自体はテーブル定義のみのため小規模だが、Unicode 文字の正確性が極めて重要であるため、レビューエージェントの参加が必須である。

## 4. 提供範囲とテスト項目

### スコープ

- `PinyinToMisaki.cs` 内のマッピングテーブル（`Dictionary<Initial, string>`、`Dictionary<Final, string>`、`string[]`）の定義
- テーブルのキーが全 enum 値を網羅していることの保証
- 各テーブルエントリに対するユニットテスト

### スコープ外

- Convert メソッドの実装（T02）
- ChineseG2PEngine への統合（T02 以降）
- 既存の ToIpa / ToPiperIpa / ToZhuyin API への影響（なし）

### ユニットテスト項目

**テストクラス:** `tests/DotNetG2P.Tests/Chinese/PinyinToMisakiMappingTests.cs`

#### 声母テスト（22 件）

各 `Initial` enum 値に対して、テーブルから取得した文字列が期待する Unicode シーケンスと完全一致することを検証する。

| テストケース | 入力 | 期待出力 | 検証ポイント |
|------------|------|---------|------------|
| `InitialB_MapsToP` | `Initial.B` | `"p"` | 同一 |
| `InitialP_MapsToPh` | `Initial.P` | `"p\u02B0"` | 同一 |
| `InitialM_MapsToM` | `Initial.M` | `"m"` | 同一 |
| `InitialF_MapsToF` | `Initial.F` | `"f"` | 同一 |
| `InitialD_MapsToT` | `Initial.D` | `"t"` | 同一 |
| `InitialT_MapsToTh` | `Initial.T` | `"t\u02B0"` | 同一 |
| `InitialN_MapsToN` | `Initial.N` | `"n"` | 同一 |
| `InitialL_MapsToL` | `Initial.L` | `"l"` | 同一 |
| `InitialG_MapsToK` | `Initial.G` | `"k"` | 同一 |
| `InitialK_MapsToKh` | `Initial.K` | `"k\u02B0"` | 同一 |
| `InitialH_MapsToX` | `Initial.H` | `"x"` | 同一 |
| **`InitialJ_MapsToTcLigature`** | `Initial.J` | `"\u02A8"` | **合字 U+02A8** |
| **`InitialQ_MapsToTcLigatureAspirated`** | `Initial.Q` | `"\u02A8\u02B0"` | **合字 + 有気** |
| `InitialX_MapsToAlveolopalatalFricative` | `Initial.X` | `"\u0255"` | 同一 |
| `InitialZh_MapsToRetroflexAffricate` | `Initial.Zh` | `"\u0288\u0282"` | 同一 |
| `InitialCh_MapsToRetroflexAffricateAspirated` | `Initial.Ch` | `"\u0288\u0282\u02B0"` | 同一 |
| `InitialSh_MapsToRetroflexFricative` | `Initial.Sh` | `"\u0282"` | 同一 |
| `InitialR_MapsToRetroflexApproximant` | `Initial.R` | `"\u027B"` | 同一 |
| **`InitialZ_MapsToTsLigature`** | `Initial.Z` | `"\u02A6"` | **合字 U+02A6** |
| **`InitialC_MapsToTsLigatureAspirated`** | `Initial.C` | `"\u02A6\u02B0"` | **合字 + 有気** |
| `InitialS_MapsToS` | `Initial.S` | `"s"` | 同一 |
| `InitialY_MapsToPalatalApproximant` | `Initial.Y` | `"j"` | 同一 |
| `InitialW_MapsToLabialVelarApproximant` | `Initial.W` | `"w"` | 同一 |

#### 韻母テスト（32 件）

各 `Final` enum 値に対して、テーブルから取得した文字列が期待する Unicode シーケンスと完全一致することを検証する。差異のある 10 韻母を重点的にテストする。

| テストケース | 入力 | 期待出力 | 検証ポイント |
|------------|------|---------|------------|
| `FinalA_MapsToA` | `Final.A` | `"a"` | 同一 |
| `FinalO_MapsToO` | `Final.O` | `"o"` | 同一 |
| `FinalE_MapsToRamishorn` | `Final.E` | `"\u0264"` | 同一 |
| **`FinalAi_MapsToAiWithInvertedBreve`** | `Final.Ai` | `"ai\u032F"` | **非音節化符号** |
| **`FinalEi_MapsToEiWithInvertedBreve`** | `Final.Ei` | `"ei\u032F"` | **非音節化符号** |
| **`FinalAo_MapsToAuWithInvertedBreve`** | `Final.Ao` | `"au\u032F"` | **非音節化符号** |
| **`FinalOu_MapsToOuWithInvertedBreve`** | `Final.Ou` | `"ou\u032F"` | **非音節化符号** |
| `FinalAn_MapsToAn` | `Final.An` | `"an"` | 同一 |
| `FinalEn_MapsToSchwan` | `Final.En` | `"\u0259n"` | 同一 |
| `FinalAng_MapsToAng` | `Final.Ang` | `"a\u014B"` | 同一 |
| `FinalEng_MapsToSchwaEng` | `Final.Eng` | `"\u0259\u014B"` | 同一 |
| **`FinalOng_MapsToUInvertedBreveNg`** | `Final.Ong` | `"u\u032F\u014B"` | **非音節化符号** |
| `FinalI_MapsToI` | `Final.I` | `"i"` | 同一 |
| `FinalIa_MapsToIa` | `Final.Ia` | `"ia"` | 同一 |
| `FinalIe_MapsToIOpenE` | `Final.Ie` | `"i\u025B"` | 同一 |
| **`FinalIao_MapsToIauWithInvertedBreve`** | `Final.Iao` | `"iau\u032F"` | **非音節化符号** |
| **`FinalIu_MapsToIouWithInvertedBreve`** | `Final.Iu` | `"iou\u032F"` | **非音節化符号** |
| `FinalIan_MapsToIOpenEn` | `Final.Ian` | `"i\u025Bn"` | 同一 |
| `FinalIn_MapsToIn` | `Final.In` | `"in"` | 同一 |
| `FinalIang_MapsToIaEng` | `Final.Iang` | `"ia\u014B"` | 同一 |
| `FinalIng_MapsToIEng` | `Final.Ing` | `"i\u014B"` | 同一 |
| **`FinalIong_MapsToIuInvertedBreveNg`** | `Final.Iong` | `"iu\u032F\u014B"` | **非音節化符号** |
| `FinalU_MapsToU` | `Final.U` | `"u"` | 同一 |
| `FinalUa_MapsToUa` | `Final.Ua` | `"ua"` | 同一 |
| `FinalUo_MapsToUo` | `Final.Uo` | `"uo"` | 同一 |
| **`FinalUai_MapsToUaiWithInvertedBreve`** | `Final.Uai` | `"uai\u032F"` | **非音節化符号** |
| **`FinalUi_MapsToUeiWithInvertedBreve`** | `Final.Ui` | `"uei\u032F"` | **非音節化符号** |
| `FinalUan_MapsToUan` | `Final.Uan` | `"uan"` | 同一 |
| `FinalUn_MapsToUSchwan` | `Final.Un` | `"u\u0259n"` | 同一 |
| `FinalUang_MapsToUaEng` | `Final.Uang` | `"ua\u014B"` | 同一 |
| `FinalUeng_MapsToUSchwaEng` | `Final.Ueng` | `"u\u0259\u014B"` | 同一 |
| `FinalV_MapsToY` | `Final.V` | `"y"` | 同一 |
| `FinalVe_MapsToYOpenE` | `Final.Ve` | `"y\u025B"` | 同一 |
| `FinalVan_MapsToYan` | `Final.Van` | `"yan"` | 同一 |
| `FinalVn_MapsToYn` | `Final.Vn` | `"yn"` | 同一 |
| `FinalEr_MapsToSchwaRetroflex` | `Final.Er` | `"\u0259\u027B"` | 同一 |

#### 声調テスト（5 件）

| テストケース | 入力 | 期待出力 | 検証ポイント |
|------------|------|---------|------------|
| `ToneNeutral_MapsToEmpty` | `Tone.Neutral` (0) | `""` | 軽声は空文字 |
| **`ToneFirst_MapsToRightArrow`** | `Tone.First` (1) | `"\u2192"` | **→** |
| **`ToneSecond_MapsToNorthEastArrow`** | `Tone.Second` (2) | `"\u2197"` | **↗** |
| **`ToneThird_MapsToDownArrow`** | `Tone.Third` (3) | `"\u2193"` | **↓** |
| **`ToneFourth_MapsToSouthEastArrow`** | `Tone.Fourth` (4) | `"\u2198"` | **↘** |

#### テーブル網羅性テスト（3 件）

| テストケース | 検証内容 |
|------------|---------|
| `InitialTable_CoversAllEnumValues` | `Initial` enum の `None` 以外の全 22 値がテーブルのキーに存在する |
| `FinalTable_CoversAllEnumValues` | `Final` enum の `None` 以外の全 35 値がテーブルのキーに存在する |
| `ToneTable_HasCorrectLength` | 声調配列の長さが 5（Neutral=0 ~ Fourth=4） |

#### E2E テスト（本チケット外、T02 で実施）

T02 で Convert メソッド統合後に以下を検証する:

- `"mā"` -> `"ma\u2192"` (第 1 声、矢印)
- `"jīn"` -> `"\u02A8in\u2192"` (j の合字 + 第 1 声)
- `"zài"` -> `"\u02A6ai\u032F\u2198"` (z の合字 + 非音節化 + 第 4 声)
- `"zhōng"` -> `"\u0288\u0282u\u032F\u014B\u2192"` (そり舌 + ong 非音節化 + 第 1 声)

## 5. 実装に関する懸念事項とレビュー項目

### Unicode 文字の正確性

1. **合字文字のコードポイント確認**: `ʨ` (U+02A8) と `ʦ` (U+02A6) は IPA Extensions ブロック (U+0250-U+02AF) に属する合字文字である。これらが正しく .NET の `char` / `string` で扱えることを確認すること（BMP 内のため問題ないはず）。

2. **非音節化符号の結合文字性**: U+032F (COMBINING INVERTED BREVE BELOW) は結合文字であり、先行する基底文字に付与される。テーブル内で `"ai\u032F"` のように末尾に配置した場合、`i` に結合することを確認すること。

3. **矢印記号のフォント互換性**: U+2192/U+2197/U+2193/U+2198 は Arrows ブロックに属し、IPA 文字と同列に出力される。Kokoro TTS が実際にこれらのコードポイントを期待しているかを Misaki の公式実装と照合すること。

### 言語学的正確性

4. **ong の Misaki 表記**: 標準 IPA では `ʊŋ` だが、Misaki が `u̯ŋ` を採用する場合、音韻論的には非音節化 u を明示する表記となる。PinyinToIpa との意味的一貫性を確認すること。

5. **iu/ui の展開形**: PinyinToIpa では `iu` を `ioʊ`、`ui` を `ueɪ` と展開しているが、Misaki でも同様に `iou̯` / `uei̯` と展開するかを確認すること。Misaki が `iu̯` / `ui̯` のような縮約形を使用している可能性がある。

### コード品質

6. **既存変換クラスとの整合性**: `PinyinToIpa.cs` / `PinyinToPiperIpa.cs` と同一の `internal static class` パターン、同一のフィールド命名規則（`s_initialIpa`, `s_finalIpa`, `s_toneLetters`）に従うこと。ただし、フィールド名は `s_initialMisaki`, `s_finalMisaki`, `s_toneMisaki` 等に変更してもよい。

7. **テーブルの不変性**: テーブルは `static readonly` で宣言し、実行時に変更されないことを保証すること。

## 6. 一から作り直すとしたら

### 現行設計の評価

現在の変換クラス群（`PinyinToIpa`, `PinyinToPiperIpa`, `PinyinToZhuyin`）は、各クラスが独立した `Dictionary<Initial/Final, string>` テーブルを持つ「コピー&修正」パターンを採用している。このアプローチは以下の利点がある:

- 各変換形式が完全に自己完結しており、依存関係がない
- 新しい形式の追加が既存コードに影響しない
- デバッグ時にテーブルを直接参照できる

一方で以下の課題がある:

- 22+35+5 = 62 エントリ x 4 形式 = 248 テーブルエントリの管理が必要（今後さらに増加）
- テーブル間の差異が暗黙的であり、どのエントリが異なるか一覧しにくい
- 新しい Initial/Final が追加された場合、全変換クラスを更新する必要がある

### 代替設計案

#### A案: 差分テーブル方式

PinyinToIpa を基準テーブルとし、Misaki は差分のみ定義する。

```csharp
internal static class PinyinToMisaki
{
    // PinyinToIpa のテーブルを基準に、差異のあるエントリのみ上書き
    private static readonly Dictionary<Initial, string> s_initialOverrides = new()
    {
        [Initial.J] = "\u02A8",         // tɕ -> ʨ
        [Initial.Q] = "\u02A8\u02B0",   // tɕʰ -> ʨʰ
        [Initial.Z] = "\u02A6",         // ts -> ʦ
        [Initial.C] = "\u02A6\u02B0",   // tsʰ -> ʦʰ
    };

    internal static string GetInitial(Initial i) =>
        s_initialOverrides.TryGetValue(i, out var v) ? v : PinyinToIpa.GetInitial(i);
}
```

利点: 差異が明示的、メンテナンスコストが低い。欠点: PinyinToIpa への依存が発生し、PinyinToIpa の変更が Misaki に波及する。

#### B案: 型安全 enum + 属性方式

各音素 enum 値に属性で全形式のマッピングを定義する。

```csharp
public enum Initial : byte
{
    [IpaMapping("tɕ")]
    [MisakiMapping("ʨ")]
    [PiperMapping("tɕ")]
    J,
    // ...
}
```

利点: 音素と全マッピングが一箇所に集約される。欠点: 属性ベースはリフレクション依存でパフォーマンスに影響、.NET Standard 2.1 でのソース生成器非対応、既存設計との大幅な乖離。

#### C案: TSV/CSV 駆動

マッピングを外部 TSV ファイルとして管理し、起動時にロードする。

```tsv
Initial	IPA	Misaki	Piper	Zhuyin
J	tɕ	ʨ	tɕ	ㄐ
Q	tɕʰ	ʨʰ	tɕʰ	ㄑ
```

利点: マッピングの一覧性が最高、非プログラマでも編集可能。欠点: 起動時パースコスト、Unicode 文字の TSV 内表現が不安定（エディタ依存）、embedded resource 管理の複雑化。

### 推奨

**現行の独立テーブルパターン（コピー&修正方式）を継続する。** 理由:

1. 変換形式は 4 種類にとどまり、管理負荷は許容範囲内
2. 既存の 3 クラスとの一貫性を維持できる
3. 各テーブルが自己完結しており、バグの局所化が容易
4. ランタイムのパフォーマンスオーバーヘッドがゼロ

ただし、本チケットのテーブル設計時に差異を明確にドキュメント化し（本チケット自体がその役割を果たす）、将来的にマッピング形式が 6 種以上に増えた場合は A 案（差分テーブル方式）への移行を検討する。

## 7. 後続タスクへの連絡事項

### T02（Convert メソッド統合）に伝える情報

1. **テーブルフィールド名**: `s_initialMisaki`, `s_finalMisaki`, `s_toneMisaki` を使用（PinyinToIpa の `s_initialIpa` 等と区別するため）。

2. **そり舌・歯茎母音の処理**: `s_retroflexApical` (`ɻ̩`) と `s_alveolarApical` (`ɹ̩`) は PinyinToIpa と同一値を使用する。PinyinToMisaki 内にも同じフィールドを定義するか、共通化するかは T02 で判断すること。

3. **声調の配置位置**: Misaki の矢印声調は音節末に付加する（PinyinToIpa と同じ位置）。

4. **Convert メソッドのシグネチャ**: `PinyinToIpa.Convert(string pinyin, bool includeTones)` と同一のシグネチャを推奨。PinyinToPiperIpa のように声調なし固定にはしない（Misaki は声調を使用するため）。

5. **ShouldOmitSemivowel ロジック**: Y/W 声母の省略判定は PinyinToIpa と同一ロジック。共通化するか PinyinToMisaki にコピーするかは T02 で判断すること。

6. **韻母テーブルの `Iu` / `Ui` の展開形**: PinyinToIpa は `Iu` -> `ioʊ`、`Ui` -> `ueɪ` と展開する。Misaki では `iou̯` / `uei̯` とした。T02 実装時に Misaki 公式出力と照合し、縮約形を使う場合はテーブルを修正すること。

## 8. 紐づけ

- **マイルストーン**: Mi1（PinyinToMisaki 変換クラス）
- **依存**: なし
- **後続**: T02（Convert メソッド統合・ChineseG2PEngine への組み込み）
- **関連 Issue**: #56
