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

### マッピング戦略の再検討（追加レビュー）

本セクションは、マッピング戦略の観点から現行の「独立テーブル×4」方式を再検討し、より保守性の高い代替案を具体化するためのものである。観点は以下の 4 つ:

1. TSV 外部ファイル化（スキーマ・ロード方式・埋め込みリソース化）
2. 差分マッピング方式（BaseIpaTable + DialectDiff）の具体例
3. Source Generator によるコンパイル時マッピング生成
4. 各方式のパフォーマンス・保守性・可読性の比較

#### D案: TSV 外部ファイル化の具体案

##### スキーマ設計

`src/DotNetG2P.Chinese/Data/pinyin_mapping.master.tsv` を単一のマスターファイルとして配置し、全変換形式のエントリを横並びで保持する。

```tsv
# pinyin_mapping.master.tsv
# type: initial | final | tone
# key: enum 名（Initial.J, Final.Ai, Tone.First 等）
# ipa: PinyinToIpa の値
# misaki: PinyinToMisaki の値
# piper: PinyinToPiperIpa の値
# zhuyin: PinyinToZhuyin の値
# comment: Unicode コードポイント・音韻的コメント
type	key	ipa	misaki	piper	zhuyin	comment
initial	B	p	p	p	ㄅ	U+0070
initial	J	tɕ	ʨ	tɕ	ㄐ	Misaki: U+02A8 (tc ligature)
initial	Z	ts	ʦ	ts	ㄗ	Misaki: U+02A6 (ts ligature)
final	Ai	aɪ	ai̯	aɪ	ㄞ	Misaki: i + U+032F
final	Ao	aʊ	au̯	aʊ	ㄠ	Misaki: u + U+032F
final	Ong	ʊŋ	u̯ŋ	ʊŋ	ㄨㄥ	Misaki: u + U+032F + ŋ
tone	First	˥˥	→	˥˥	ˉ	Misaki: U+2192
tone	Second	˧˥	↗	˧˥	ˊ	Misaki: U+2197
```

**スキーマ設計ポイント:**

- **1 行 1 エントリ**: type/key を複合キーとして一意に特定
- **空セル許容**: zhuyin など一部形式でエントリが存在しない場合は空セル
- **コメント列**: Unicode コードポイント・音韻的メモを保持（レビュー時の視認性向上）
- **UTF-8 BOM なし**: エディタ依存を減らすため BOM なし UTF-8 で統一
- **`#` 行コメント**: ファイル冒頭で型定義を説明

##### ロード方式

```csharp
internal static class PinyinMappingTable
{
    // 起動時に 1 度だけロード（lazy initialization）
    private static readonly Lazy<MappingData> s_data = new(LoadFromResource);

    private static MappingData LoadFromResource()
    {
        var asm = typeof(PinyinMappingTable).Assembly;
        using var stream = asm.GetManifestResourceStream(
            "DotNetG2P.Chinese.Data.pinyin_mapping.master.tsv");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return TsvParser.Parse(reader);
    }

    internal static string GetInitial(Initial i, MappingFormat fmt)
        => s_data.Value.Initials[(i, fmt)];
}
```

**埋め込みリソース化:**

```xml
<!-- DotNetG2P.Chinese.csproj -->
<ItemGroup>
  <EmbeddedResource Include="Data/pinyin_mapping.master.tsv" />
</ItemGroup>
```

他言語パッケージ（Spanish/French/Portuguese）で採用済みの例外辞書 TSV と同一の埋め込み方式を採用することで、プロジェクト全体の一貫性が保たれる。

**利点:**

- マッピング一覧性が最高（全形式を横並びで確認可能）
- 差分が視覚的に明確（差異のあるセルが目立つ）
- 非プログラマ（言語学者・翻訳者）でも編集可能
- 新形式追加時は列を 1 つ追加するだけ

**欠点:**

- 起動時パースコスト（ただし Lazy + 62 エントリで実測 < 1ms）
- TSV 内の Unicode 結合文字（U+032F）はエディタで不可視になりがち → コメント列で補う必要
- コンパイル時型安全性の喪失（enum 名の typo がランタイムエラーに）

#### E案: 差分マッピング方式（BaseIpaTable + DialectDiff）の具体例

A 案をさらに具体化し、BaseTable/DiffTable 構造を正規化する。

```csharp
// 基底テーブル（標準 IPA、すべての形式の起点）
internal static class BaseIpaTable
{
    internal static readonly IReadOnlyDictionary<Initial, string> Initials = new Dictionary<Initial, string>
    {
        [Initial.B] = "p",
        [Initial.J] = "t\u0255",   // tɕ
        [Initial.Z] = "ts",
        // ... 全 22 エントリ
    };

    internal static readonly IReadOnlyDictionary<Final, string> Finals = new Dictionary<Final, string>
    {
        [Initial.Ai] = "a\u026A",  // aɪ
        // ... 全 35 エントリ
    };
}

// 差分（Misaki 方言）
internal static class MisakiDiff
{
    internal static readonly IReadOnlyDictionary<Initial, string> InitialOverrides = new Dictionary<Initial, string>
    {
        [Initial.J] = "\u02A8",         // ʨ
        [Initial.Q] = "\u02A8\u02B0",
        [Initial.Z] = "\u02A6",         // ʦ
        [Initial.C] = "\u02A6\u02B0",
    };

    internal static readonly IReadOnlyDictionary<Final, string> FinalOverrides = new Dictionary<Final, string>
    {
        [Final.Ai] = "ai\u032F",
        [Final.Ei] = "ei\u032F",
        [Final.Ao] = "au\u032F",
        [Final.Ou] = "ou\u032F",
        [Final.Ong] = "u\u032F\u014B",
        [Final.Iao] = "iau\u032F",
        [Final.Iu] = "iou\u032F",
        [Final.Iong] = "iu\u032F\u014B",
        [Final.Uai] = "uai\u032F",
        [Final.Ui] = "uei\u032F",
    };

    internal static readonly string[] ToneOverrides = new[] { "", "\u2192", "\u2197", "\u2193", "\u2198" };
}

// ルックアップ（差分優先、なければ Base）
internal static class PinyinToMisaki
{
    internal static string GetInitial(Initial i)
        => MisakiDiff.InitialOverrides.TryGetValue(i, out var v) ? v : BaseIpaTable.Initials[i];

    internal static string GetFinal(Final f)
        => MisakiDiff.FinalOverrides.TryGetValue(f, out var v) ? v : BaseIpaTable.Finals[f];

    internal static string GetTone(int t) => MisakiDiff.ToneOverrides[t];
}
```

**起動時マージによる事前計算（パフォーマンス最適化）:**

```csharp
private static readonly IReadOnlyDictionary<Initial, string> s_initialMerged = MergeBaseAndDiff(
    BaseIpaTable.Initials, MisakiDiff.InitialOverrides);

private static Dictionary<TKey, string> MergeBaseAndDiff<TKey>(
    IReadOnlyDictionary<TKey, string> @base,
    IReadOnlyDictionary<TKey, string> diff)
{
    var result = new Dictionary<TKey, string>(@base);
    foreach (var kvp in diff) result[kvp.Key] = kvp.Value;
    return result;
}
```

この事前マージ方式により、ランタイムのルックアップは基底テーブルと同等のコスト（`TryGetValue` 1 回）となる。

**利点:**

- Misaki の差異が 15 エントリ（initial 4 + final 10 + tone 4、軽声除く）のみに集約され、視認性が最高
- Base の変更が全方言に自動波及（意図した一貫性）
- テストで `Diff.Count` を検証することで「想定外の差分」を検出可能
- ランタイムオーバーヘッドなし（事前マージ時）

**欠点:**

- 方言間の予期せぬ結合（Base 変更の波及）が時に問題になる
- Zhuyin のように全エントリが Base と異なる形式（ラテン→漢字由来記号）では差分方式の利点が消失
- 「どの値が Base 由来か Diff 由来か」の区別が API 越しには見えない

#### F案: Source Generator によるコンパイル時マッピング生成

C# Source Generator を用いて、TSV ファイルをコンパイル時に読み込み、強型付けされた `static readonly` フィールドを自動生成する。

##### 設計

```csharp
// Generators/PinyinMappingGenerator.cs
[Generator]
public class PinyinMappingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // AdditionalFiles から TSV を取得
        var tsvFiles = context.AdditionalTextsProvider
            .Where(f => f.Path.EndsWith("pinyin_mapping.master.tsv"));

        context.RegisterSourceOutput(tsvFiles, (spc, file) =>
        {
            var content = file.GetText()?.ToString();
            var entries = ParseTsv(content);
            var source = GenerateCode(entries);
            spc.AddSource("PinyinMappingTable.g.cs", source);
        });
    }
}
```

##### 生成される出力例

```csharp
// PinyinMappingTable.g.cs (auto-generated)
namespace DotNetG2P.Chinese.Conversion;

internal static class PinyinToMisakiGenerated
{
    internal static readonly Dictionary<Initial, string> Initials = new()
    {
        [Initial.B] = "p",
        [Initial.J] = "\u02A8",
        [Initial.Z] = "\u02A6",
        // ... 全エントリがコンパイル時に埋め込まれる
    };
}
```

##### プロジェクト設定

```xml
<ItemGroup>
  <AdditionalFiles Include="Data/pinyin_mapping.master.tsv" />
  <ProjectReference Include="..\DotNetG2P.Chinese.Generators\*.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

**利点:**

- TSV の編集容易性と、コンパイル時生成によるランタイム高速性を両立
- enum 名の typo がコンパイルエラーとして検出される（ジェネレータ側でチェック実装可能）
- 起動時パースコストゼロ（コード生成済み）
- IDE での F12 でジェネレート済みコードへ跳べる（デバッグ容易）
- Base/Diff 関係をジェネレータ内で計算し、最終形式を生成可能

**欠点:**

- **.NET Standard 2.1 ターゲットとの互換性問題**: Source Generator は `netstandard2.0` ターゲットの Generator プロジェクトが必要。Unity IL2CPP ビルドとの相性も要検証
- ジェネレータプロジェクトの追加によるビルド複雑化
- デバッグ時の可読性低下（生成コードが見慣れた形と異なる場合）
- 既存の他言語パッケージ（TSV を Runtime ロードしている）との一貫性が崩れる
- Roslyn API の学習コスト（特に Incremental Generator）

**Unity 互換性の懸念:**

Unity 2021.2+ の Roslyn バージョンで Incremental Generator が動作するかは要検証。Unity パッケージ側では UPM 経由で配布するため、Generator を同梱しない「ビルド済みコード＋TSV リソース」方式のハイブリッドも検討すべき。

#### 各方式のパフォーマンス・保守性・可読性比較

| 方式 | ランタイム性能 | 起動時コスト | 保守性 | 可読性 | 型安全性 | Unity 互換 |
|------|--------------|------------|--------|--------|---------|-----------|
| **現行（独立テーブル）** | ★★★ (最速) | 無 | ★ (4 箇所同期) | ★ (差分不明瞭) | ★★★ | ★★★ |
| **A案: 差分テーブル（遅延）** | ★★ (TryGet 2 回) | 無 | ★★★ | ★★★ | ★★★ | ★★★ |
| **E案: 差分テーブル（事前マージ）** | ★★★ | 微小 (< 1ms) | ★★★ | ★★★ | ★★★ | ★★★ |
| **B案: 属性方式** | ★ (リフレクション) | 中 (初回のみキャッシュ) | ★★ | ★★ | ★★★ | ★★ (IL2CPP strip 注意) |
| **C/D案: TSV ロード** | ★★★ (ロード後は Dict 参照) | 小 (数ms) | ★★★ | ★★★★ (全形式横並び) | ★ (ランタイム検証) | ★★★ (Embedded Resource) |
| **F案: Source Generator** | ★★★ (最速) | 無 | ★★★ | ★★★ | ★★★ | ★ (要検証) |

**評価軸の詳細:**

- **ランタイム性能**: 1 回のルックアップコスト。DictionaryTryGetValue は O(1) なので実際の差は微小だが、TSV 方式は起動後は埋め込み方式と同等
- **保守性**: マッピング追加・修正時の影響範囲。差分方式と TSV 方式が最良
- **可読性**: 全形式の差分を一覧する際の容易さ。TSV > 差分 > 独立
- **型安全性**: enum 名の typo がコンパイル時に検出されるか
- **Unity 互換**: IL2CPP/AOT/Embedded Resource 制約への適合度

#### マッピング戦略の推奨（追加レビューの結論）

**短期（T01/T02 本チケット）: 現行の独立テーブル方式を維持する。** 理由は既存の「### 推奨」セクションに記載の通り。ただし以下の追加措置を推奨:

1. **Unicode 定数クラスの導入**: `Internal/ChineseUnicode.cs` に `NonSyllabicMark = "\u032F"`, `TcLigature = "\u02A8"` 等の名前付き定数を定義し、テーブル定義時に使用する。エスケープシーケンスの散在を防ぎ、レビュー時の誤読を削減する。

   ```csharp
   internal static class ChineseUnicode
   {
       internal const string NonSyllabicMark = "\u032F"; // COMBINING INVERTED BREVE BELOW
       internal const string TcLigature = "\u02A8";      // ʨ
       internal const string TsLigature = "\u02A6";      // ʦ
       internal const string Aspirated = "\u02B0";       // ʰ
       internal const string RightArrow = "\u2192";      // →
       // ...
   }
   ```

2. **差分検証テストの追加**: `PinyinToIpa` と `PinyinToMisaki` のテーブルを比較し、差異エントリ数が想定値（initial 4 + final 10 + tone 4 = 18）と一致することを検証するメタテストを追加。想定外の差分を早期検出する。

3. **マスター TSV ドキュメント化**: T01 本チケットの表を元に、`docs/chinese/pinyin_mapping_reference.md` として全形式の横並び表を作成・維持する。コードとは独立したドキュメントとし、コード変更時の同期は CI で差分チェックする（目視レビュー）。

**中期（5 形式目追加時・例: Kaldi lexicon / LEX 形式等）: E 案（差分テーブル・事前マージ）へ移行する。** 理由:

- 形式数が 5 以上になると独立テーブルの同期コストが許容範囲を超える
- 事前マージ方式ならランタイム性能の劣化なし
- Unity 互換性の懸念なし（通常のコードのみ）
- TSV 方式ほど大掛かりな変更ではなく、段階的移行が容易

**長期（10 形式以上・例: 複数 TTS エンジン対応）: D 案（TSV 外部ファイル）へ移行する。** 理由:

- 非プログラマによる編集が可能になり、言語学者の貢献を受け入れやすい
- 他言語パッケージ（Es/Fr/Pt）と一貫した方式となる
- Source Generator（F 案）は Unity 互換性の懸念があるため、ランタイムロード方式（Lazy 初期化）を推奨

**Source Generator（F 案）は現時点では採用非推奨。** .NET Standard 2.1 / Unity IL2CPP 環境との互換性検証コストが高く、T01/T02 のスコープを大幅に超える。将来的に Unity が Roslyn Incremental Generator を正式サポートした時点で再検討する。

### アーキテクトレビュー（統合的まとめ）

上記「現行設計の評価」「代替設計案（A/B/C 案）」「推奨」および「マッピング戦略の再検討（D/E/F 案）」は網羅的だが、**ディクショナリ定義の物理構造**にしか注目していない。本節では、「`PinyinToZhuyin` を含めた 4 クラス全体の本質的な構造差」と、「既存レビューで触れられていない C# 言語機能の活用余地」の観点からレビューを補完する。

#### 4 クラスの構造分類 — 対称な抽象化の限界

現行 4 クラス（追加予定の Misaki 含む）の構造を分類すると、見かけ上「4 つのコピペ」に見える状況は、実は **2 つの異なるパターン** に分離できる。

| クラス | キー型 | マッピング構造 | 抽象化対象 |
|-------|-------|--------------|----------|
| `PinyinToIpa` | `Initial`/`Final` enum | 声母/韻母/声調の 3 テーブル | **IPA ファミリ** |
| `PinyinToPiperIpa` | `Initial`/`Final` enum | 声母/韻母の 2 テーブル（声調なし） | **IPA ファミリ** |
| `PinyinToMisaki` (予定) | `Initial`/`Final` enum | 声母/韻母/声調（矢印） | **IPA ファミリ** |
| `PinyinToZhuyin` | `string` (pinyin) | `string`→注音符号 | **文字列変換ファミリ**（別系統） |

**重要な示唆:** `PinyinToZhuyin` は `Dictionary<string, string>` でピンイン文字列を直接変換しており、`Initial`/`Final` enum を経由しない。これは音韻論的にも正当で（注音符号は中国語固有の表記で IPA 的な音素分解が不要）、構造的に IPA ファミリとの統一は不自然である。

したがって、**抽象化の対象は「IPA ファミリ 3 クラス」に限定すべき**であり、Zhuyin を巻き込む共通化は設計目標として適切ではない。本セクション以降の「共通基盤」は IPA ファミリの話に限る。

#### C# 言語機能ベース 4 方式の比較

本チケット既存セクション（A/B/C/D/E/F 案）は **テーブル配置戦略** に焦点を当てていたが、ここでは **共通処理の抽象化メカニズム** として C# が提供する 4 つのアプローチを比較する。T02 と共通する論点だが、T01 の視点では「マッピングテーブルをどう型として表現するか」が主題となる。

##### 方式 1: interface ベース

```csharp
internal interface IPinyinMapping
{
    string GetInitial(Initial initial);
    string GetFinal(Final final_);
    string GetTone(Tone tone);
    string RetroflexApical { get; }
    string AlveolarApical { get; }
}

internal sealed class MisakiMapping : IPinyinMapping
{
    private static readonly Dictionary<Initial, string> s_initials = /* ... */;
    public string GetInitial(Initial i) => s_initials[i];
    // ...
}
```

**利点:** モック化が自然（テスト時にフェイク実装注入可能）、依存反転原則（DIP）に忠実、将来の DI 導入と整合。
**欠点:** インスタンスメソッド呼び出しが仮想メソッドディスパッチになる（JIT 最適化で軽減されるが hot path では計測差が出る）。既存 `internal static` クラス群との様式不整合。

##### 方式 2: abstract class ベース

```csharp
internal abstract class PinyinMappingBase
{
    protected abstract IReadOnlyDictionary<Initial, string> Initials { get; }
    protected abstract IReadOnlyDictionary<Final, string> Finals { get; }
    protected abstract string[] ToneMarkers { get; }

    // 共通ロジックを基底クラスに集約（テンプレートメソッド）
    public virtual string Convert(PinyinSyllable s, bool includeTones)
    {
        // 全クラス共通の変換フロー
    }

    // 差異を許容する拡張ポイント
    protected virtual string HandleRetroflexApical() => "\u027B\u0329";
    protected virtual string HandleAlveolarApical() => "\u0279\u0329";
}

internal sealed class MisakiMapping : PinyinMappingBase
{
    protected override IReadOnlyDictionary<Initial, string> Initials => s_initials;
    // ...
}
```

**利点:** 共通ロジックを基底に集約できる（interface だけでは default interface methods を使わない限り不可能、default interface methods は .NET Standard 2.1 で限定的）。オーバーライドによる柔軟な差分実装が可能。
**欠点:** 単一継承の制約、`sealed class` でない限りさらなる派生を招きやすい、interface より結合度が高い。

##### 方式 3: record + switch 式ベース（推奨候補）

```csharp
// マッピングを不変の値オブジェクトとして表現
internal sealed record PinyinMappingTable(
    IReadOnlyDictionary<Initial, string> Initials,
    IReadOnlyDictionary<Final, string> Finals,
    IReadOnlyList<string> ToneMarkers,
    string RetroflexApical,
    string AlveolarApical,
    bool IncludeTonesByDefault);

internal static class PinyinMappingTables
{
    public static readonly PinyinMappingTable Ipa = new(
        Initials: BuildIpaInitials(),
        Finals: BuildIpaFinals(),
        ToneMarkers: new[] { "", "\u02E5\u02E5", "\u02E7\u02E5", "\u02E8\u02E9\u02E6", "\u02E5\u02E9" },
        RetroflexApical: "\u027B\u0329",
        AlveolarApical: "\u0279\u0329",
        IncludeTonesByDefault: true);

    public static readonly PinyinMappingTable Misaki = Ipa with
    {
        Initials = new Dictionary<Initial, string>(Ipa.Initials)
        {
            [Initial.J] = "\u02A8",
            [Initial.Q] = "\u02A8\u02B0",
            [Initial.Z] = "\u02A6",
            [Initial.C] = "\u02A6\u02B0",
        },
        Finals = new Dictionary<Final, string>(Ipa.Finals)
        {
            [Final.Ai] = "ai\u032F",
            [Final.Ei] = "ei\u032F",
            [Final.Ao] = "au\u032F",
            [Final.Ou] = "ou\u032F",
            [Final.Ong] = "u\u032F\u014B",
            [Final.Iao] = "iau\u032F",
            [Final.Iu] = "iou\u032F",
            [Final.Iong] = "iu\u032F\u014B",
            [Final.Uai] = "uai\u032F",
            [Final.Ui] = "uei\u032F",
        },
        ToneMarkers = new[] { "", "\u2192", "\u2197", "\u2193", "\u2198" },
    };
}

// 変換エンジンは record を受け取る純粋関数群
internal static class PinyinConversionEngine
{
    public static string Convert(PinyinSyllable syllable, PinyinMappingTable table, bool includeTones)
    {
        // 中央集権的な変換ロジック
        var sb = new StringBuilder(16);

        if (syllable.Initial != Initial.None)
        {
            var skipSemivowel =
                (syllable.Initial == Initial.Y || syllable.Initial == Initial.W)
                && ShouldOmitSemivowel(syllable.Initial, syllable.Final);

            if (!skipSemivowel)
                sb.Append(table.Initials[syllable.Initial]);
        }

        if (syllable.Final != Final.None)
        {
            // そり舌/歯茎母音の分岐も record の値を参照
            var finalStr = (syllable.Final, syllable.Initial) switch
            {
                (Final.I, var i) when IsRetroflex(i) => table.RetroflexApical,
                (Final.I, var i) when IsAlveolar(i) => table.AlveolarApical,
                _ => table.Finals[syllable.Final],
            };
            sb.Append(finalStr);
        }

        if (includeTones && syllable.Tone != Tone.Neutral)
            sb.Append(table.ToneMarkers[(int)syllable.Tone]);

        return sb.ToString();
    }
}

// 既存の内部 API は record を渡す薄いラッパーとして維持（公開 API の互換性保証）
internal static class PinyinToMisaki
{
    public static string Convert(string pinyin) => Convert(pinyin, true);

    public static string Convert(string pinyin, bool includeTones)
    {
        if (string.IsNullOrEmpty(pinyin)) return string.Empty;
        string normalized = ToneConverter.ToToneMarked(pinyin);
        if (!PinyinParser.TryParse(normalized, out var syllable)) return string.Empty;
        return PinyinConversionEngine.Convert(syllable, PinyinMappingTables.Misaki, includeTones);
    }
}
```

**利点:**
- **`record` の値セマンティクスで差分記述が自然**: `Ipa with { ... }` 構文で親テーブルからの差分のみを表現できる（既存 A 案「差分テーブル」の型安全版）
- **C# 8.0+ switch 式**でそり舌/歯茎分岐が簡潔に記述でき、分岐の網羅性チェックをコンパイラが行う
- **.NET Standard 2.1 互換**: `record` は C# 9.0 の機能だが、`LangVersion` 指定で .NET Standard 2.1 でも使用可能（DotNetG2P.Chinese の現状設定を確認すること）
- **純粋関数**として `PinyinConversionEngine.Convert` を実装でき、テストが容易（副作用なし、内部状態なし）
- **既存の公開 API 互換性を維持**: `PinyinToIpa.Convert(...)` 等のファサードは薄いラッパーとして残せる
- **テーブル網羅性テストを 1 箇所で書ける**: `PinyinMappingTables.Ipa`, `Misaki`, `PiperIpa` を `IEnumerable` で列挙し、メタテストで全テーブルを同一基準で検証

**欠点:**
- `record` の `with` 式は浅いコピーのため、Dictionary の中身を深くコピーする必要がある（上記コードでは明示的に `new Dictionary<...>(Ipa.Initials) { ... }` と書いている）
- `IReadOnlyDictionary` プロパティへのアクセスは仮想呼び出しになるため、`Dictionary<>` 直接参照より数 ns 遅い（実測では意味のない差）

##### 方式 4: source generator ベース

既存 F 案で詳述済みのため簡略化するが、T01 視点では「TSV やコード片から `PinyinMappingTable` record を自動生成する」という統合的な使い方が有望。ただし、Unity/IL2CPP 互換性の懸念があるため短期的な採用は見送り。

#### 4 方式の比較表（T01 視点）

| 観点 | 方式 1: interface | 方式 2: abstract class | **方式 3: record+switch** | 方式 4: generator |
|------|----------------|---------------------|-------------------------|-----------------|
| 既存 `internal static` 様式との整合 | 低 | 低 | **中（ファサード維持）** | 高 |
| 差分オーバーライド構文 | 手動 (override) | 手動 (override) | **`with` 式で自然** | コード生成 |
| コンパイル時型安全性 | 中 | 中 | **高 (switch網羅性)** | 高 |
| 既存公開 API 互換 | 要書換 | 要書換 | **維持可** | 維持可 |
| テスト容易性 | 高 (モック) | 中 | **高 (純粋関数)** | 中 |
| ランタイムコスト | 中 (仮想呼び出し) | 中 | **低 (record は class)** | 最低 |
| Unity/IL2CPP 互換 | 高 | 高 | **高** | 要検証 |
| .NET Standard 2.1 互換 | 高 | 高 | **高 (LangVersion 要設定)** | 要検証 |
| リファクタ規模 | 大 | 大 | **中** | 大 |

#### T01 のマッピング設計への実践的推奨

上記の分析を踏まえ、T01 で確定すべきマッピング定義の「書き方」として以下を推奨する。

**推奨 1: 現行の独立テーブル方式を踏襲しつつ、将来の `record` 化を見据えた「機械的移行可能」な書式に揃える**

- マッピングは `private static readonly Dictionary<Initial, string>` で宣言する（既存 2 クラスと同一）
- **エントリ順序を `Initial` enum の宣言順に厳密に揃える**（将来 `record PinyinMappingTable` に機械的に変換する際、diff レビューが容易になる）
- 差異エントリには `// Misaki差異: tɕ→ʨ (U+02A8)` の形式でインラインコメントを必須化

**推奨 2: Unicode 定数の名前付き化（既存レビューでも言及済み、さらに具体化）**

```csharp
// src/DotNetG2P.Chinese/Internal/ChineseUnicode.cs
internal static class ChineseUnicode
{
    // IPA 修飾子
    public const string Aspirated = "\u02B0";           // ʰ
    public const string SyllabicMark = "\u0329";        // 音節主音記号（下付き）
    public const string NonSyllabicMark = "\u032F";     // 非音節化記号（下付き反転ブレーブ）

    // IPA 合字（Misaki 用）
    public const string TcLigature = "\u02A8";          // ʨ
    public const string TsLigature = "\u02A6";          // ʦ

    // 声調矢印（Misaki 用）
    public const string ArrowRight = "\u2192";          // →
    public const string ArrowNE = "\u2197";             // ↗
    public const string ArrowDown = "\u2193";           // ↓
    public const string ArrowSE = "\u2198";             // ↘
}
```

これにより、T01 のマッピング定義は:

```csharp
private static readonly Dictionary<Initial, string> s_initialMisaki = new Dictionary<Initial, string>
{
    // ... (IPA と同一のエントリは IPA と同じ順序で)
    [Initial.J] = ChineseUnicode.TcLigature,                              // Misaki差異: tɕ→ʨ
    [Initial.Q] = ChineseUnicode.TcLigature + ChineseUnicode.Aspirated,   // Misaki差異: tɕʰ→ʨʰ
    [Initial.Z] = ChineseUnicode.TsLigature,                              // Misaki差異: ts→ʦ
    [Initial.C] = ChineseUnicode.TsLigature + ChineseUnicode.Aspirated,   // Misaki差異: tsʰ→ʦʰ
    // ...
};
```

と書ける。レビュー時の Unicode 誤読リスクが大幅に減り、将来 `record` 化する際もエントリの意味が明示的に保たれる。

**推奨 3: 差分メタテストの導入**

T01 のテスト項目に以下を追加:

```csharp
[Fact]
public void MisakiDiffersFromIpa_OnlyAtKnownPositions()
{
    // PinyinToIpa と PinyinToMisaki の全エントリを比較し、
    // 差異のあるエントリ数・位置が想定値と一致することを検証
    var ipaInitials = PinyinToIpa.GetInitialMapSnapshot();     // internal テスト API
    var misakiInitials = PinyinToMisaki.GetInitialMapSnapshot();

    var diffKeys = ipaInitials
        .Where(kv => misakiInitials[kv.Key] != kv.Value)
        .Select(kv => kv.Key)
        .ToHashSet();

    Assert.Equal(
        new HashSet<Initial> { Initial.J, Initial.Q, Initial.Z, Initial.C },
        diffKeys);
}
```

想定外のエントリ差異を早期検出でき、コピペミスや意図せぬ挙動変更をブロックできる。

#### アーキテクトレビューとしての最終推奨

**T01/T02 のスコープ内では、現行の独立テーブルパターンを踏襲し、上記「推奨 1〜3」を実装する**。理由:

1. `record + switch` 式の方式 3 は将来の最有力候補だが、3 クラス同時リファクタは Mi1 のスコープ外
2. 「推奨 1」の**エントリ順序統一**と「推奨 2」の**Unicode 定数化**により、将来の `record` 化が機械的に可能な状態で残せる
3. 「推奨 3」の**差分メタテスト**により、コピペ方式の最大の弱点（テーブル間不整合の見落とし）を補完できる
4. `PinyinToZhuyin` は IPA ファミリとは別系統であり、共通化の対象から除外することで設計目標が明確化される

**Mi1 完了後の後続タスクとして、別チケット「PinyinConverter 共通抽象化リファクタ」を起票する。** このチケットで:

- `PinyinMappingTable` record 型の導入
- `PinyinConversionEngine.Convert(syllable, table, includeTones)` の抽出
- `PinyinToIpa` / `PinyinToPiperIpa` / `PinyinToMisaki` をファサードに変更
- 既存 936 件 + Misaki 追加分のテストが全件通過することで安全性を保証

この段階的アプローチにより、**現時点では既存パターンを維持してリスクを最小化**しつつ、**将来の構造改善への布石を残す**ことができる。

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
