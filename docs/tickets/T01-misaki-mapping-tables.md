---
ticket: T01
title: PinyinToMisaki マッピングテーブル設計・実装
milestone: Mi1
status: 完了
depends_on: []
blocks: [T02]
---

# T01: PinyinToMisaki マッピングテーブル設計・実装

## 1. タスク目的とゴール

### 背景

Kokoro TTS の G2P フロントエンド Misaki は、中国語音素表記に独自の IPA バリアントを採用している。DotNetG2P.Chinese は現在 3 種類の出力形式（標準 IPA、piper-plus 互換 IPA、注音符号）を提供しているが、Misaki 互換形式には未対応であり、Kokoro TTS ユーザーが DotNetG2P を G2P フロントエンドとして利用できない状況にある（Issue #56）。

初期の Phase 1（現行仕様）では Misaki の独自表記を一部推測で構成していたが、**Phase 1-R で Misaki 公式実装 (`hexgrad/misaki` の `misaki/zh.py` + `misaki/transcription.py`) を fetch し、`uv run python` で実測した 137 件の gold standard (`.claude/tmp/misaki-gold.txt`) により完全な仕様が verified された**。本チケットはこの verified 仕様に基づきマッピングテーブルを再定義する。

### Phase 1-R で verified された主要ポイント

1. **J/Q は ligature U+02A8 `ʨ`** を使用する（旧仕様の `tɕ` は誤り）。
2. **Z/C は ligature U+02A6 `ʦ`** を使用する（旧仕様の `ts` 2 文字は誤り）。
3. **Zh/Ch は Unicode U+AB67 `ꭧ`** を使用する（旧仕様の `ʈʂ` (U+0288 U+0282) は誤り）。
4. **retroflex/alveolar apical の I は直接 `ɨ` (U+0268)** である（旧仕様の `ɻ̩` / `ɹ̩` は誤り）。
5. **Ong は `ʊŋ` (U+028A U+014B)** である（旧仕様の `u̯ŋ` (U+0075 U+032F U+014B) は誤り）。
6. **Ai/Ei/Ao/Ou 等は U+032F 非音節化符号なし** で `ai`/`ei`/`au`/`ou` である（旧仕様は誤り）。
7. **Ian/Van は `jɛn` / `ɥɛn`** (U+025B) である（旧仕様の `ian`/`yan` は誤り）。
8. **Iong は `jʊŋ`**、**Ve/Van は `ɥe` / `ɥɛn`** (U+0265) である。
9. **声調矢印は韻母の Prefix と Suffix の間に挿入する**（韻母末尾ではない）。例: `man1` → `ma→n`（`man→` ではない）。
10. **Y/W は声母層ではなく複合韻母層** で処理される。Misaki では "wang" は `wa→ŋ` であり、Initial.W の 1 文字マッピングでは対応できない。
11. **bpmf + o は pwo/pʰwo/mwo/fwo** 形式（`bo1` → `pwo→`）である。単独感嘆詞 `o` は `ɔ` (U+0254) である（bpmf + o の `wo` とは異なる）。
12. **Er は `ɚ` (U+025A)** 単独である（旧仕様の `əɻ` は誤り）。

### ゴール

既存の `PinyinToIpa.cs` / `PinyinToPiperIpa.cs` / `PinyinToZhuyin.cs` と同じ変換クラスパターンで `PinyinToMisaki.cs` を新規作成するための、**声母・韻母・声調・Y/W 複合韻母の全マッピングテーブルを Misaki 公式準拠で確定する**。本チケットのスコープはテーブル定義のみであり、Convert メソッドの統合は後続 T02 で行う。

### 達成基準

- 声母 22 エントリ、韻母 36 エントリ（Prefix + Suffix 方式）、声調 5 エントリ、特殊母音 3 ケース、Y/W 複合韻母 23 エントリすべてのマッピングが確定し、コードに実装されていること
- Phase 1-R gold standard（137 件、`.claude/tmp/misaki-gold.txt`）との照合が通ること
- Kokoro 82M vocab 互換性（Inv6 verified）が確認されていること
- PinyinToIpa との差異が明確にドキュメント化されていること
- 全マッピングのユニットテストが通過すること

## 2. 実装する内容の詳細

### 2.1 声母テーブル（22 エントリ）

`PinyinToIpa.cs` の `s_initialIpa` を基準とし、Misaki で異なる表記を使用する箇所を太字で示す。**Y/W は声母層ではなく複合韻母層で処理するため、声母テーブル本体には含めないが enum としては残す**（`ConvertSyllable` 側で Y/W 複合韻母テーブルを優先的に参照する）。

Phase 1-R gold standard より検証済み（`uv run python -c "import misaki.zh; ..."` で全 22 エントリを実測）:

| # | Initial enum | ピンイン | PinyinToIpa（標準 IPA） | PinyinToMisaki | Unicode シーケンス | 差異 | gold 例 |
|---|-------------|---------|------------------------|----------------|-------------------|------|--------|
| 1 | `B` | b | p | p | `p` | 同一 | `ba1` → `pa→` |
| 2 | `P` | p | pʰ | pʰ | `p\u02B0` | 同一 | `pa2` → `pʰa↗` |
| 3 | `M` | m | m | m | `m` | 同一 | `ma1` → `ma→` |
| 4 | `F` | f | f | f | `f` | 同一 | `fa3` → `fa↓` |
| 5 | `D` | d | t | t | `t` | 同一 | `da4` → `ta↘` |
| 6 | `T` | t | tʰ | tʰ | `t\u02B0` | 同一 | `ta1` → `tʰa→` |
| 7 | `N` | n | n | n | `n` | 同一 | `na2` → `na↗` |
| 8 | `L` | l | l | l | `l` | 同一 | `la3` → `la↓` |
| 9 | `G` | g | k | k | `k` | 同一 | `ga4` → `ka↘` |
| 10 | `K` | k | kʰ | kʰ | `k\u02B0` | 同一 | `ka1` → `kʰa→` |
| 11 | `H` | h | x | x | `x` | 同一 | `ha2` → `xa↗` |
| 12 | **`J`** | j | **tɕ** (t\u0255) | **ʨ** | **`\u02A8`** | **tɕ → ʨ 合字 (U+02A8)** | `ji1` → **`ʨi→`** |
| 13 | **`Q`** | q | **tɕʰ** (t\u0255\u02B0) | **ʨʰ** | **`\u02A8\u02B0`** | **tɕʰ → ʨʰ (U+02A8 + 有気)** | `qi2` → **`ʨʰi↗`** |
| 14 | `X` | x | ɕ (\u0255) | ɕ | `\u0255` | 同一 | `xi3` → `ɕi↓` |
| 15 | **`Zh`** | zh | **ʈʂ** (\u0288\u0282) | **ꭧ** | **`\uAB67`** | **ʈʂ 2 文字 → U+AB67 合字** | `zhi4` → **`ꭧɨ↘`** |
| 16 | **`Ch`** | ch | **ʈʂʰ** (\u0288\u0282\u02B0) | **ꭧʰ** | **`\uAB67\u02B0`** | **ʈʂʰ → U+AB67 + 有気** | `chi1` → **`ꭧʰɨ→`** |
| 17 | `Sh` | sh | ʂ (\u0282) | ʂ | `\u0282` | 同一 | `shi2` → `ʂɨ↗` |
| 18 | `R` | r | ɻ (\u027B) | ɻ | `\u027B` | 同一 | `ri3` → `ɻɨ↓` |
| 19 | **`Z`** | z | **ts** (2 文字) | **ʦ** | **`\u02A6`** | **ts 2 文字 → U+02A6 合字** | `zi4` → **`ʦɨ↘`** |
| 20 | **`C`** | c | **tsʰ** (ts\u02B0) | **ʦʰ** | **`\u02A6\u02B0`** | **tsʰ → U+02A6 + 有気** | `ci1` → **`ʦʰɨ→`** |
| 21 | `S` | s | s | s | `s` | 同一 | `si2` → `sɨ↗` |
| 22 | **`Y`** | y | j | **—（compound final 層で処理）** | — | **声母マップから除外** | `ya1` → `ja→` |
| 23 | **`W`** | w | w | **—（compound final 層で処理）** | — | **声母マップから除外** | `wa1` → `wa→` |

**差異まとめ（声母）:**

Phase 1-R で判明した Misaki の声母差異は以下の 6 箇所（旧仕様の 4 箇所から増加）:

1. **j** (Initial.J): 標準 IPA `tɕ` (U+0074 U+0255) → Misaki **`ʨ` (U+02A8 ラテン文字 TC ligature)**
2. **q** (Initial.Q): 標準 IPA `tɕʰ` → Misaki **`ʨʰ` (U+02A8 U+02B0)**
3. **z** (Initial.Z): 標準 IPA `ts` (2 文字) → Misaki **`ʦ` (U+02A6 ラテン文字 TS ligature)**
4. **c** (Initial.C): 標準 IPA `tsʰ` → Misaki **`ʦʰ` (U+02A6 U+02B0)**
5. **zh** (Initial.Zh): 標準 IPA `ʈʂ` (U+0288 U+0282) → Misaki **`ꭧ` (U+AB67 単一文字)** ※Phase 1-R で新規確定
6. **ch** (Initial.Ch): 標準 IPA `ʈʂʰ` → Misaki **`ꭧʰ` (U+AB67 U+02B0)** ※Phase 1-R で新規確定

加えて **Y / W は声母層の 1 文字マッピングでは対応不可** であることが判明した。Misaki の "wang" は `wa→ŋ` だが、もし W を `w` 単独マップで処理すると韻母 Ang と組み合わせて `wa→ŋ` にならず `w` + `aŋ` = `waŋ→`（末尾声調）となる。そのため Y/W は後述の「Y/W 複合韻母マッピング」(セクション 3) で個別処理する。

### 2.2 韻母テーブル（36 エントリ、Prefix + Suffix 方式）

**Phase 1-R で判明した最重要設計変更**: 声調矢印は Misaki では**韻母末尾ではなく韻母の中間**に挿入される。具体的には `man1` → `ma→n`（`man→` ではない）、`mang1` → `ma→ŋ`（`mang→` ではない）となる。これを扱うため、韻母テーブルは **Prefix + Suffix タプル** として定義する:

```csharp
private static readonly Dictionary<Final, (string Prefix, string Suffix)> s_finalMisaki = new()
{
    [Final.Ai]  = ("ai",   ""),   // prefix + tone + "" = ai→
    [Final.An]  = ("a",    "n"),  // prefix + tone + suffix = a→n
    [Final.Ang] = ("a",    "\u014B"), // a→ŋ
    // ...
};
```

`ConvertSyllable` では `prefix + toneArrow + suffix` の順に結合する。Suffix が空文字の韻母は末尾付加と等価になる。

| # | Final enum | ピンイン | Prefix | Suffix | tone 1 例 | 備考 | gold 例 |
|---|-----------|---------|--------|--------|-----------|------|--------|
| 1 | `A` | a | `a` | `` | `a→` | | `la1` → `la→` |
| 2 | **`O`** | o | **`wo`** | `` | `wo→` | **bpmf + o は pwo/pʰwo/mwo/fwo 形式** | `bo1` → **`pwo→`** |
| 3 | `E` | e | `\u0264` (ɤ) | `` | `ɤ→` | | `le1` → `lɤ→` |
| 4 | **`Ai`** | ai | **`ai`** | `` | `ai→` | **U+032F strip 後（非音節化符号なし）** | `lai1` → `lai→` |
| 5 | **`Ei`** | ei | **`ei`** | `` | `ei→` | **U+032F strip 後** | `lei1` → `lei→` |
| 6 | **`Ao`** | ao | **`au`** | `` | `au→` | **Misaki "au"、strip 後** | `lao1` → `lau→` |
| 7 | **`Ou`** | ou | **`ou`** | `` | `ou→` | **strip 後** | `lou1` → `lou→` |
| 8 | **`An`** | an | **`a`** | **`n`** | **`a→n`** | **声調が中間** | `lan1` → **`la→n`** |
| 9 | **`En`** | en | **`\u0259`** (ə) | **`n`** | **`ə→n`** | **声調が中間、U+0259** | `len1` → **`lə→n`** |
| 10 | **`Ang`** | ang | **`a`** | **`\u014B`** (ŋ) | **`a→ŋ`** | **声調が中間、U+014B** | `lang1` → **`la→ŋ`** |
| 11 | **`Eng`** | eng | **`\u0259`** (ə) | **`\u014B`** (ŋ) | **`ə→ŋ`** | **声調が中間** | `leng1` → **`lə→ŋ`** |
| 12 | **`Ong`** | ong | **`\u028A`** (ʊ) | **`\u014B`** (ŋ) | **`ʊ→ŋ`** | **U+028A ʊ（旧 `u̯` は誤り）、声調が中間** | `long1` → **`lʊ→ŋ`** / `dong1` → `tʊ→ŋ` |
| 13 | `I` | i | `i` | `` | `i→` | | `li1` → `li→` |
| 14 | **`Ia`** | ia | **`ja`** | `` | `ja→` | **j 半母音** | `lia1` → **`lja→`** |
| 15 | **`Ie`** | ie | **`je`** | `` | `je→` | **Misaki "je" (標準 IPA の `iɛ` とは違う、NOT `jɛ`)** | `lie1` → **`lje→`** |
| 16 | **`Iao`** | iao | **`jau`** | `` | `jau→` | **j 半母音、strip 後** | `liao1` → **`ljau→`** |
| 17 | **`Iu`** | iu (iou) | **`jou`** | `` | `jou→` | **Misaki "iou"、strip 後** | `liu1` → **`ljou→`** |
| 18 | **`Ian`** | ian | **`j\u025B`** (jɛ) | **`n`** | **`jɛ→n`** | **j 半母音、ɛ (U+025B)、声調が中間** | `lian1` → **`ljɛ→n`** |
| 19 | **`In`** | in | **`i`** | **`n`** | **`i→n`** | **j なし、声調が中間** | `lin1` → **`li→n`** |
| 20 | **`Iang`** | iang | **`ja`** | **`\u014B`** (ŋ) | **`ja→ŋ`** | **j 半母音、声調が中間** | `liang1` → **`lja→ŋ`** |
| 21 | **`Ing`** | ing | **`i`** | **`\u014B`** (ŋ) | **`i→ŋ`** | **j なし、声調が中間** | `ling1` → **`li→ŋ`** |
| 22 | **`Iong`** | iong | **`j\u028A`** (jʊ) | **`\u014B`** (ŋ) | **`jʊ→ŋ`** | **j + ʊ、声調が中間** | `xiong2` → **`ɕjʊ↗ŋ`** |
| 23 | `U` | u | `u` | `` | `u→` | | `lu1` → `lu→` |
| 24 | **`Ua`** | ua | **`wa`** | `` | `wa→` | **w 半母音** | `lua1` → **`lwa→`** |
| 25 | **`Uo`** | uo | **`wo`** | `` | `wo→` | **w 半母音** | `luo1` → **`lwo→`** |
| 26 | **`Uai`** | uai | **`wai`** | `` | `wai→` | **strip 後** | `guai1` → **`kwai→`** |
| 27 | **`Ui`** | ui (uei) | **`wei`** | `` | `wei→` | **Misaki "uei"、strip 後** | `guei1` → **`kwei→`** |
| 28 | **`Uan`** | uan | **`wa`** | **`n`** | **`wa→n`** | **声調が中間** | `luan1` → **`lwa→n`** / `guan1` → `kwa→n` |
| 29 | **`Un`** | un (uen) | **`w\u0259`** (wə) | **`n`** | **`wə→n`** | **Misaki "uen"、声調が中間** | `lun1` → **`lwə→n`** / `guen1` → `kwə→n` |
| 30 | **`Uang`** | uang | **`wa`** | **`\u014B`** (ŋ) | **`wa→ŋ`** | **声調が中間** | `luang1` → **`lwa→ŋ`** |
| 31 | **`Ueng`** | ueng | **`w\u0259`** (wə) | **`\u014B`** (ŋ) | **`wə→ŋ`** | **声調が中間** | `gueng1` → **`kwə→ŋ`** |
| 32 | `V` (ü) | u | `y` | `` | `y→` | U+0079 | `lv1` → `ly→` |
| 33 | **`Ve`** (üe) | ue | **`\u0265e`** (ɥe) | `` | `ɥe→` | **ɥ = U+0265（NOT y）** | `lve1` → **`lɥe→`** / `jve1` → `ʨɥe→` |
| 34 | **`Van`** (üan) | uan | **`\u0265\u025B`** (ɥɛ) | **`n`** | **`ɥɛ→n`** | **ɥ + ɛ + n、声調が中間** | `jvan1` → **`ʨɥɛ→n`** |
| 35 | `Vn` (ün) | un | `y` | **`n`** | **`y→n`** | **声調が中間** | `jvn1` → **`ʨy→n`** |
| 36 | **`Er`** | er | **`\u025A`** (ɚ) | `` | `ɚ→` | **U+025A 単一記号（NOT `əɻ`）** | `er1` → **`ɚ→`** |

**差異まとめ（韻母）:**

1. **声調位置**: An/En/Ang/Eng/Ong/In/Ing/Ian/Iang/Iong/Uan/Un/Uang/Ueng/Van/Vn の **16 韻母で声調が Prefix と Suffix の間に挿入される**（旧仕様の「末尾付加」は誤り）。
2. **U+032F 非音節化符号なし**: Ai/Ei/Ao/Ou/Iao/Iu/Uai/Ui の 8 韻母は Misaki ではシンプルな `ai`/`ei`/`au`/`ou`/`jau`/`jou`/`wai`/`wei` である（旧仕様の `ai̯`/`ei̯`/`au̯`/`ou̯` は誤り）。
3. **Ong は `ʊŋ`**: Misaki は `ʊ` (U+028A) を使用する（旧仕様の `u̯ŋ` (U+0075 U+032F U+014B) は誤り）。
4. **Ian は `jɛn`**: Misaki は `j + ɛ + n` を使用する（旧仕様の `iɛn` は誤り、先頭が半母音 j）。
5. **Ie は `je` 単純形**: Misaki は `je` であり `jɛ` ではない（旧仕様の `iɛ` とは異なる）。
6. **Ve は `ɥe`、Van は `ɥɛn`**: Misaki は ɥ (U+0265 LATIN SMALL LETTER TURNED H) を使用する（旧仕様の `y`/`yan` は誤り）。
7. **bpmf + o は `wo`**: Misaki で `bo1` → `pwo→` のように w が挿入される。韻母 O は Prefix=`wo` として定義する。
8. **Er は `ɚ` 単独**: Misaki は U+025A (SCHWA WITH HOOK) 単一記号（旧仕様の `əɻ` (U+0259 U+027B) は誤り）。

### 2.3 声調矢印テーブル（5 エントリ）

PinyinToIpa が IPA tone letters を使用するのに対し、Misaki は矢印記号を使用する。矢印の挿入位置は韻母の Prefix と Suffix の間（セクション 2.2 参照）。

| # | Tone enum | 声調名 | PinyinToIpa（IPA tone letters） | PinyinToMisaki（矢印記号） | Unicode シーケンス |
|---|----------|-------|-------------------------------|--------------------------|-------------------|
| 1 | `Neutral` (0) | 軽声 | (なし) | (なし) | `""` |
| 2 | `First` (1) | 陰平 (55) | `\u02E5\u02E5` (˥˥) | **`→`** | **`\u2192`** |
| 3 | `Second` (2) | 陽平 (35) | `\u02E7\u02E5` (˧˥) | **`↗`** | **`\u2197`** |
| 4 | `Third` (3) | 上声 (214) | `\u02E8\u02E9\u02E6` (˨˩˦) | **`↓`** | **`\u2193`** |
| 5 | `Fourth` (4) | 去声 (51) | `\u02E5\u02E9` (˥˩) | **`↘`** | **`\u2198`** |

全 4 声調（軽声を除く）が異なる。IPA tone letters（複数文字の声調レベル記号）から、単一の Unicode 矢印記号に変更される。Phase 1-R 検証済み: `ma1/2/3/4/5` → `ma→ / ma↗ / ma↓ / ma↘ / ma`

### 2.4 特殊母音ケース（Final enum ではない差し替え、3 ケース）

以下は Final enum に一律マップするのではなく、`(Initial, Final)` コンテキストで差し替える特殊ケース:

| # | Context | Prefix | Suffix | 備考 | gold 例 |
|---|---------|--------|--------|------|--------|
| 1 | **Zh/Ch/Sh/R + Final.I** | `\u0268` (ɨ) | `` | **そり舌そり頂母音 (retroflex apical) → U+0268 直接**（旧仕様の `ɻ̩` (U+027B U+0329) は誤り） | `zhi4` → `ꭧɨ↘` / `ri3` → `ɻɨ↓` |
| 2 | **Z/C/S + Final.I** | `\u0268` (ɨ) | `` | **歯茎そり頂母音 (alveolar apical) → U+0268 直接**（旧仕様の `ɹ̩` (U+0279 U+0329) は誤り） | `zi4` → `ʦɨ↘` / `si2` → `sɨ↗` |
| 3 | **Initial.None + Final.O** | `\u0254` (ɔ) | `` | **単独感嘆詞 ō → U+0254 ɔ**（bpmf + o の `wo` とは異なる） | `o1` → **`ɔ→`** / `o4` → `ɔ↘` |

**retroflex / alveolar apical の新仕様**: Phase 1-R で Misaki 公式実装は zh/ch/sh/r + i と z/c/s + i の両方で直接 `ɨ` (U+0268 CLOSE CENTRAL UNROUNDED VOWEL) を出力することが判明した。旧仕様の `ɻ̩` / `ɹ̩`（結合音節主音記号 U+0329）は誤りで、Misaki は区別せず ɨ 単一記号を使用する。

**単独感嘆詞 o の分岐**: Misaki では bpmf + o (`bo`/`po`/`mo`/`fo`) は `pwo`/`pʰwo`/`mwo`/`fwo` 形式で Final.O = `(wo, "")` を使う一方、単独の `o` (`ō`/`ó`/`ǒ`/`ò`) は `ɔ` (U+0254) となる。`Initial.None + Final.O` のコンテキストでのみ `(ɔ, "")` に差し替える。

### 2.5 実装ファイル

**新規作成:** `src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs` のみ。

```csharp
internal static class PinyinToMisaki
{
    // 声母テーブル: Dictionary<Initial, string>（Y/W は含めない、None も含めない）
    private static readonly Dictionary<Initial, string> s_initialMisaki = new()
    {
        [Initial.B] = "p",
        [Initial.P] = "p\u02B0",
        // ...
        [Initial.J] = "\u02A8",             // Misaki差異: tɕ→ʨ (U+02A8 ligature)
        [Initial.Q] = "\u02A8\u02B0",       // Misaki差異: tɕʰ→ʨʰ
        [Initial.X] = "\u0255",
        [Initial.Zh] = "\uAB67",            // Misaki差異: ʈʂ→ꭧ (U+AB67)
        [Initial.Ch] = "\uAB67\u02B0",      // Misaki差異: ʈʂʰ→ꭧʰ
        [Initial.Sh] = "\u0282",
        [Initial.R] = "\u027B",
        [Initial.Z] = "\u02A6",             // Misaki差異: ts→ʦ (U+02A6 ligature)
        [Initial.C] = "\u02A6\u02B0",       // Misaki差異: tsʰ→ʦʰ
        [Initial.S] = "s",
        // Y/W は含めない（compound final 層で処理）
    };

    // 韻母テーブル: Dictionary<Final, (Prefix, Suffix)>
    private static readonly Dictionary<Final, (string Prefix, string Suffix)> s_finalMisaki = new()
    {
        [Final.A]    = ("a",               ""),
        [Final.O]    = ("wo",              ""),           // bpmf+o 用、単独 o は特殊ケース
        [Final.E]    = ("\u0264",          ""),           // ɤ
        [Final.Ai]   = ("ai",              ""),
        [Final.Ei]   = ("ei",              ""),
        [Final.Ao]   = ("au",              ""),
        [Final.Ou]   = ("ou",              ""),
        [Final.An]   = ("a",               "n"),          // 声調が中間
        [Final.En]   = ("\u0259",          "n"),          // 声調が中間
        [Final.Ang]  = ("a",               "\u014B"),     // 声調が中間
        [Final.Eng]  = ("\u0259",          "\u014B"),     // 声調が中間
        [Final.Ong]  = ("\u028A",          "\u014B"),     // ʊŋ、声調が中間
        [Final.I]    = ("i",               ""),
        [Final.Ia]   = ("ja",              ""),
        [Final.Ie]   = ("je",              ""),           // NOT jɛ
        [Final.Iao]  = ("jau",             ""),
        [Final.Iu]   = ("jou",             ""),
        [Final.Ian]  = ("j\u025B",         "n"),          // jɛn、声調が中間
        [Final.In]   = ("i",               "n"),          // 声調が中間
        [Final.Iang] = ("ja",              "\u014B"),     // 声調が中間
        [Final.Ing]  = ("i",               "\u014B"),     // 声調が中間
        [Final.Iong] = ("j\u028A",         "\u014B"),     // jʊŋ、声調が中間
        [Final.U]    = ("u",               ""),
        [Final.Ua]   = ("wa",              ""),
        [Final.Uo]   = ("wo",              ""),
        [Final.Uai]  = ("wai",             ""),
        [Final.Ui]   = ("wei",             ""),
        [Final.Uan]  = ("wa",              "n"),          // 声調が中間
        [Final.Un]   = ("w\u0259",         "n"),          // wən、声調が中間
        [Final.Uang] = ("wa",              "\u014B"),     // 声調が中間
        [Final.Ueng] = ("w\u0259",         "\u014B"),     // 声調が中間
        [Final.V]    = ("y",               ""),
        [Final.Ve]   = ("\u0265e",         ""),           // ɥe (U+0265)
        [Final.Van]  = ("\u0265\u025B",    "n"),          // ɥɛn、声調が中間
        [Final.Vn]   = ("y",               "n"),          // 声調が中間
        [Final.Er]   = ("\u025A",          ""),           // ɚ
    };

    // 声調矢印テーブル: string[]
    private static readonly string[] s_toneArrows = new[]
    {
        "",         // Neutral (0)
        "\u2192",   // First  (1) →
        "\u2197",   // Second (2) ↗
        "\u2193",   // Third  (3) ↓
        "\u2198",   // Fourth (4) ↘
    };

    // Y/W 複合韻母テーブル（セクション 3 参照）
    // キー: (Initial, Final)、値: (Prefix, Suffix, OmitInitial)
    private static readonly Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)>
        s_yWCompoundMisaki = new()
    {
        // Y 系
        [(Initial.Y, Final.A)]    = ("ja",           "",        false),
        [(Initial.Y, Final.An)]   = ("j\u025B",      "n",       false),
        [(Initial.Y, Final.Ang)]  = ("ja",           "\u014B",  false),
        [(Initial.Y, Final.Ao)]   = ("jau",          "",        false),
        [(Initial.Y, Final.E)]    = ("je",           "",        false),
        [(Initial.Y, Final.I)]    = ("i",            "",        true),
        [(Initial.Y, Final.In)]   = ("i",            "n",       true),
        [(Initial.Y, Final.Ing)]  = ("i",            "\u014B",  true),
        [(Initial.Y, Final.Ong)]  = ("j\u028A",      "\u014B",  false),
        [(Initial.Y, Final.Ou)]   = ("jou",          "",        false),
        [(Initial.Y, Final.V)]    = ("y",            "",        true),
        [(Initial.Y, Final.Ve)]   = ("\u0265e",      "",        false),
        [(Initial.Y, Final.Van)]  = ("\u0265\u025B", "n",       false),
        [(Initial.Y, Final.Vn)]   = ("y",            "n",       true),
        // W 系
        [(Initial.W, Final.A)]    = ("wa",           "",        false),
        [(Initial.W, Final.Ai)]   = ("wai",          "",        false),
        [(Initial.W, Final.An)]   = ("wa",           "n",       false),
        [(Initial.W, Final.Ang)]  = ("wa",           "\u014B",  false),
        [(Initial.W, Final.Ei)]   = ("wei",          "",        false),
        [(Initial.W, Final.En)]   = ("w\u0259",      "n",       false),
        [(Initial.W, Final.Eng)]  = ("w\u0259",      "\u014B",  false),
        [(Initial.W, Final.O)]    = ("wo",           "",        false),
        [(Initial.W, Final.U)]    = ("u",            "",        true),
    };
}
```

テーブルのみを定義し、Convert メソッドは T02 で実装する。ただし、テーブル参照のための internal static なアクセサ（`GetInitialMisaki`, `GetFinalMisaki`, `GetToneArrow` 等）は本チケットで定義してもよい。

## 3. Y/W 複合韻母変換表（23 エントリ、Phase 1-R 新規導入セクション）

### 3.1 背景

DotNetG2P の `PinyinParser` は "wang" を `Initial.W + Final.Ang`、"yan" を `Initial.Y + Final.An` のように parse する。これに対し Misaki の元実装では "wang" は `uang` という複合韻母として、"yan" は `ian` として扱われる。構造が異なるため、T02 の `ConvertSyllable` で `(Initial, Final)` ペアが Y/W 系の場合は以下の複合韻母テーブルを優先参照する必要がある。

Phase 1-R gold standard での検証済み（`ya1/ye1/yi1/wa1/wo1/wu1` 等 23 パターン全件実測）:

| # | Initial | Final | → Misaki 等価 | Prefix | Suffix | Initial 省略? | gold 検証 |
|---|---------|-------|--------------|--------|--------|---------------|----------|
| 1 | Y | A | Ia | `ja` | `` | No | `ya1` → `ja→` |
| 2 | Y | An | Ian | `j\u025B` (jɛ) | `n` | No | `yan1` → `jɛ→n` |
| 3 | Y | Ang | Iang | `ja` | `\u014B` (ŋ) | No | `yang1` → `ja→ŋ` |
| 4 | Y | Ao | Iao | `jau` | `` | No | `yao1` → `jau→` |
| 5 | Y | E | Ie | `je` | `` | No | `ye1` → `je→` |
| 6 | Y | I | I | `i` | `` | **Yes（j 省略）** | `yi1` → `i→`（`ji→` ではない） |
| 7 | Y | In | In | `i` | `n` | **Yes** | `yin1` → `i→n` |
| 8 | Y | Ing | Ing | `i` | `\u014B` (ŋ) | **Yes** | `ying1` → `i→ŋ` |
| 9 | Y | Ong | Iong | `j\u028A` (jʊ) | `\u014B` (ŋ) | No | `yong1` → `jʊ→ŋ` |
| 10 | Y | Ou | Iu (iou) | `jou` | `` | No | `you1` → `jou→` |
| 11 | Y | V | V (ü) | `y` | `` | **Yes（ɥ 省略）** | `yu1` → `y→`（`ɥy→` ではない） |
| 12 | Y | Ve | Ve (üe) | `\u0265e` (ɥe) | `` | No | `yue1` → `ɥe→` |
| 13 | Y | Van | Van (üan) | `\u0265\u025B` (ɥɛ) | `n` | No | `yuan1` → `ɥɛ→n` |
| 14 | Y | Vn | Vn (ün) | `y` | `n` | **Yes（ɥ 省略）** | `yun1` → `y→n` |
| 15 | W | A | Ua | `wa` | `` | No | `wa1` → `wa→` |
| 16 | W | Ai | Uai | `wai` | `` | No | `wai1` → `wai→` |
| 17 | W | An | Uan | `wa` | `n` | No | `wan1` → `wa→n` |
| 18 | W | Ang | Uang | `wa` | `\u014B` (ŋ) | No | `wang1` → `wa→ŋ` |
| 19 | W | Ei | Ui (uei) | `wei` | `` | No | `wei1` → `wei→` |
| 20 | W | En | Un (uen) | `w\u0259` (wə) | `n` | No | `wen1` → `wə→n` |
| 21 | W | Eng | Ueng | `w\u0259` (wə) | `\u014B` (ŋ) | No | `weng1` → `wə→ŋ` |
| 22 | W | O | Uo | `wo` | `` | No | `wo1` → `wo→` |
| 23 | W | U | U | `u` | `` | **Yes（w 省略）** | `wu1` → `u→`（`wu→` ではない） |

### 3.2 Initial 省略ルールの要点

計 5 エントリで Initial を省略する:

- **`yi/yin/ying` (Y + I/In/Ing)**: j を出力せず `i`/`i→n`/`i→ŋ` となる
- **`yu/yun` (Y + V/Vn)**: ɥ を出力せず `y`/`y→n` となる
- **`wu` (W + U)**: w を出力せず `u→` となる

残り 18 エントリは Initial 省略なし（prefix がすでに半母音 `j` または `w` を含む）。

### 3.3 ConvertSyllable パイプライン（T02 スコープだが T01 でマッピング設計の根拠として記載）

```
ConvertSyllable(syllable, includeTones):
  1. 声調矢印決定: includeTones && tone != Neutral ? s_toneArrows[(int)tone] : ""
  2. 特別ケース判定:
     a. Initial.None + Final.O → return "ɔ" + toneArrow  (単独感嘆詞)
     b. Final.Er → return "ɚ" + toneArrow  (Er 単独)
     c. Zh/Ch/Sh/R + Final.I → return s_initialMisaki[initial] + "ɨ" + toneArrow
     d. Z/C/S + Final.I → return s_initialMisaki[initial] + "ɨ" + toneArrow
  3. Y/W + Final 変換判定: ルックアップ s_yWCompoundMisaki[(initial, final)]
     hit → (prefix, suffix, omitInitial) を取得
     miss → standard path: prefix = s_initialMisaki[initial] (if any), (prefix, suffix) = s_finalMisaki[final]
  4. 構築:
     if (!omitInitial && initial != None) sb.Append(s_initialMisaki[initial])
     sb.Append(prefix)
     sb.Append(toneArrow)
     sb.Append(suffix)
  5. return sb.ToString()
```

**注意**: U+032F は事前に strip 済みのテンプレートを使うので、`ConvertSyllable` 最後で `.Replace("\u032F", "")` は不要。同様に `ɻ̩`/`ɹ̩` → `ɨ` は retroflex/alveolar テンプレを直接 `("ɨ", "")` にすることで対応済み。

## 4. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 名 | `PinyinToMisaki.cs` の 4 テーブル定義コード作成、Unicode エスケープシーケンスの正確な記述、Prefix+Suffix タプル実装 |
| テストエージェント | 1 名 | マッピングテーブルの全エントリに対するユニットテスト作成、gold standard 137 件との照合テスト |
| Unicode レビューエージェント | 1 名 | Unicode コードポイントの正確性検証（U+02A8 / U+02A6 / U+AB67 / U+0265 / U+028A / U+025A / U+0254 / U+0268 等）、Kokoro vocab 互換性確認、Misaki 公式実装との再照合 |

**合計: 3 名**

実装自体はテーブル定義のみのため小規模だが、Unicode 文字の正確性が極めて重要であるため、Unicode レビューエージェントの参加が必須である。Phase 1-R で判明した 12 項目の差分（セクション 1 参照）を全てテストで検出する必要がある。

## 5. 提供範囲とテスト項目

### スコープ

- `PinyinToMisaki.cs` 内の 4 マッピングテーブル:
  - 声母テーブル 22 エントリ（`Dictionary<Initial, string>`、Y/W を除く、None を除く）
  - 韻母テーブル 36 エントリ（`Dictionary<Final, (string Prefix, string Suffix)>`）
  - 声調矢印テーブル 5 エントリ（`string[]`）
  - Y/W 複合韻母テーブル 23 エントリ（`Dictionary<(Initial, Final), (string, string, bool)>`）
- 特殊母音 3 ケース（Zh/Ch/Sh/R + I、Z/C/S + I、Initial.None + O）の定数定義
- テーブルのキーが全 enum 値を網羅していることの保証（メタテスト）
- 各テーブルエントリに対するユニットテスト
- gold standard 137 件（`.claude/tmp/misaki-gold.txt`）との照合テスト（T02 で実施、T01 ではテーブル単体の検証のみ）

### スコープ外

- `ConvertSyllable` メソッドの実装（T02）
- `ChineseG2PEngine` への統合（T02 以降）
- 既存の ToIpa / ToPiperIpa / ToZhuyin API への影響（なし）

### ユニットテスト項目

**テストクラス:** `tests/DotNetG2P.Tests/ChineseG2P/PinyinToMisakiMappingTests.cs`

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
| **`InitialJ_MapsToTcLigature`** | `Initial.J` | `"\u02A8"` | **U+02A8 ligature** |
| **`InitialQ_MapsToTcLigatureAspirated`** | `Initial.Q` | `"\u02A8\u02B0"` | **U+02A8 + 有気** |
| `InitialX_MapsToAlveolopalatalFricative` | `Initial.X` | `"\u0255"` | 同一 |
| **`InitialZh_MapsToAb67`** | `Initial.Zh` | `"\uAB67"` | **U+AB67 単一文字** |
| **`InitialCh_MapsToAb67Aspirated`** | `Initial.Ch` | `"\uAB67\u02B0"` | **U+AB67 + 有気** |
| `InitialSh_MapsToRetroflexFricative` | `Initial.Sh` | `"\u0282"` | 同一 |
| `InitialR_MapsToRetroflexApproximant` | `Initial.R` | `"\u027B"` | 同一 |
| **`InitialZ_MapsToTsLigature`** | `Initial.Z` | `"\u02A6"` | **U+02A6 ligature** |
| **`InitialC_MapsToTsLigatureAspirated`** | `Initial.C` | `"\u02A6\u02B0"` | **U+02A6 + 有気** |
| `InitialS_MapsToS` | `Initial.S` | `"s"` | 同一 |

※Y/W は声母テーブルに含めない。`InitialY_NotInInitialTable` / `InitialW_NotInInitialTable` としてテーブル非含有を検証する。

#### 韻母テスト（36 件、Prefix + Suffix タプル）

各 `Final` enum 値に対して、テーブルから取得した `(Prefix, Suffix)` タプルが期待する Unicode シーケンスと完全一致することを検証する。

| テストケース | 入力 | 期待 Prefix | 期待 Suffix | 検証ポイント |
|------------|------|------------|------------|------------|
| `FinalA_MapsToAEmpty` | `Final.A` | `"a"` | `""` | 同一 |
| **`FinalO_MapsToWoEmpty`** | `Final.O` | `"wo"` | `""` | **bpmf+o 用** |
| `FinalE_MapsToRamishornEmpty` | `Final.E` | `"\u0264"` | `""` | ɤ |
| **`FinalAi_MapsToAiNoNonSyllabic`** | `Final.Ai` | `"ai"` | `""` | **U+032F なし** |
| **`FinalEi_MapsToEiNoNonSyllabic`** | `Final.Ei` | `"ei"` | `""` | **U+032F なし** |
| **`FinalAo_MapsToAu`** | `Final.Ao` | `"au"` | `""` | **Misaki au** |
| **`FinalOu_MapsToOu`** | `Final.Ou` | `"ou"` | `""` | **U+032F なし** |
| **`FinalAn_MapsToASplitN`** | `Final.An` | `"a"` | `"n"` | **声調が中間** |
| **`FinalEn_MapsToSchwaSplitN`** | `Final.En` | `"\u0259"` | `"n"` | **声調が中間** |
| **`FinalAng_MapsToASplitNg`** | `Final.Ang` | `"a"` | `"\u014B"` | **声調が中間** |
| **`FinalEng_MapsToSchwaSplitNg`** | `Final.Eng` | `"\u0259"` | `"\u014B"` | **声調が中間** |
| **`FinalOng_MapsToUpperUSplitNg`** | `Final.Ong` | `"\u028A"` | `"\u014B"` | **ʊŋ** |
| `FinalI_MapsToIEmpty` | `Final.I` | `"i"` | `""` | 同一 |
| **`FinalIa_MapsToJa`** | `Final.Ia` | `"ja"` | `""` | **j 半母音** |
| **`FinalIe_MapsToJe`** | `Final.Ie` | `"je"` | `""` | **Misaki je (not jɛ)** |
| **`FinalIao_MapsToJau`** | `Final.Iao` | `"jau"` | `""` | **j + au** |
| **`FinalIu_MapsToJou`** | `Final.Iu` | `"jou"` | `""` | **Misaki iou** |
| **`FinalIan_MapsToJEpsilonSplitN`** | `Final.Ian` | `"j\u025B"` | `"n"` | **jɛn 声調中間** |
| **`FinalIn_MapsToISplitN`** | `Final.In` | `"i"` | `"n"` | **j なし** |
| **`FinalIang_MapsToJaSplitNg`** | `Final.Iang` | `"ja"` | `"\u014B"` | **声調が中間** |
| **`FinalIng_MapsToISplitNg`** | `Final.Ing` | `"i"` | `"\u014B"` | **j なし、声調が中間** |
| **`FinalIong_MapsToJUpperUSplitNg`** | `Final.Iong` | `"j\u028A"` | `"\u014B"` | **jʊŋ** |
| `FinalU_MapsToUEmpty` | `Final.U` | `"u"` | `""` | 同一 |
| **`FinalUa_MapsToWa`** | `Final.Ua` | `"wa"` | `""` | **w 半母音** |
| **`FinalUo_MapsToWo`** | `Final.Uo` | `"wo"` | `""` | **w 半母音** |
| **`FinalUai_MapsToWai`** | `Final.Uai` | `"wai"` | `""` | **U+032F なし** |
| **`FinalUi_MapsToWei`** | `Final.Ui` | `"wei"` | `""` | **Misaki uei** |
| **`FinalUan_MapsToWaSplitN`** | `Final.Uan` | `"wa"` | `"n"` | **声調が中間** |
| **`FinalUn_MapsToWSchwaSplitN`** | `Final.Un` | `"w\u0259"` | `"n"` | **Misaki uen** |
| **`FinalUang_MapsToWaSplitNg`** | `Final.Uang` | `"wa"` | `"\u014B"` | **声調が中間** |
| **`FinalUeng_MapsToWSchwaSplitNg`** | `Final.Ueng` | `"w\u0259"` | `"\u014B"` | **声調が中間** |
| `FinalV_MapsToYEmpty` | `Final.V` | `"y"` | `""` | 同一 |
| **`FinalVe_MapsToTurnedHE`** | `Final.Ve` | `"\u0265e"` | `""` | **ɥe (U+0265)** |
| **`FinalVan_MapsToTurnedHEpsilonSplitN`** | `Final.Van` | `"\u0265\u025B"` | `"n"` | **ɥɛn (U+0265 + U+025B)** |
| **`FinalVn_MapsToYSplitN`** | `Final.Vn` | `"y"` | `"n"` | **声調が中間** |
| **`FinalEr_MapsToSchwaHookEmpty`** | `Final.Er` | `"\u025A"` | `""` | **ɚ (U+025A 単一)** |

#### 声調テスト（5 件）

| テストケース | 入力 | 期待出力 | 検証ポイント |
|------------|------|---------|------------|
| `ToneNeutral_MapsToEmpty` | `Tone.Neutral` (0) | `""` | 軽声は空文字 |
| **`ToneFirst_MapsToRightArrow`** | `Tone.First` (1) | `"\u2192"` | **→** |
| **`ToneSecond_MapsToNorthEastArrow`** | `Tone.Second` (2) | `"\u2197"` | **↗** |
| **`ToneThird_MapsToDownArrow`** | `Tone.Third` (3) | `"\u2193"` | **↓** |
| **`ToneFourth_MapsToSouthEastArrow`** | `Tone.Fourth` (4) | `"\u2198"` | **↘** |

#### 特殊母音テスト（3 件）

| テストケース | 入力 | 期待出力 | 検証ポイント |
|------------|------|---------|------------|
| **`RetroflexApical_MapsToBarredI`** | Zh/Ch/Sh/R + Final.I context | `"\u0268"` | **U+0268 直接** |
| **`AlveolarApical_MapsToBarredI`** | Z/C/S + Final.I context | `"\u0268"` | **U+0268 直接** |
| **`StandaloneO_MapsToOpenO`** | Initial.None + Final.O context | `"\u0254"` | **U+0254 ɔ** |

#### Y/W 複合韻母テスト（23 件）

セクション 3.1 の全 23 エントリに対する `(Prefix, Suffix, OmitInitial)` の検証。

| テストケース | 入力 | 期待出力 |
|------------|------|---------|
| **`Ya_MapsToJaNoOmit`** | `(Y, A)` | `("ja", "", false)` |
| **`Yan_MapsToJEpsilonSplitN`** | `(Y, An)` | `("j\u025B", "n", false)` |
| **`Yang_MapsToJaSplitNg`** | `(Y, Ang)` | `("ja", "\u014B", false)` |
| **`Yao_MapsToJau`** | `(Y, Ao)` | `("jau", "", false)` |
| **`Ye_MapsToJeNoOmit`** | `(Y, E)` | `("je", "", false)` |
| **`Yi_MapsToIWithOmit`** | `(Y, I)` | `("i", "", true)` |
| **`Yin_MapsToISplitNWithOmit`** | `(Y, In)` | `("i", "n", true)` |
| **`Ying_MapsToISplitNgWithOmit`** | `(Y, Ing)` | `("i", "\u014B", true)` |
| **`Yong_MapsToJUpperUSplitNg`** | `(Y, Ong)` | `("j\u028A", "\u014B", false)` |
| **`You_MapsToJou`** | `(Y, Ou)` | `("jou", "", false)` |
| **`Yu_MapsToYWithOmit`** | `(Y, V)` | `("y", "", true)` |
| **`Yue_MapsToTurnedHE`** | `(Y, Ve)` | `("\u0265e", "", false)` |
| **`Yuan_MapsToTurnedHEpsilonSplitN`** | `(Y, Van)` | `("\u0265\u025B", "n", false)` |
| **`Yun_MapsToYSplitNWithOmit`** | `(Y, Vn)` | `("y", "n", true)` |
| **`Wa_MapsToWaNoOmit`** | `(W, A)` | `("wa", "", false)` |
| **`Wai_MapsToWai`** | `(W, Ai)` | `("wai", "", false)` |
| **`Wan_MapsToWaSplitN`** | `(W, An)` | `("wa", "n", false)` |
| **`Wang_MapsToWaSplitNg`** | `(W, Ang)` | `("wa", "\u014B", false)` |
| **`Wei_MapsToWei`** | `(W, Ei)` | `("wei", "", false)` |
| **`Wen_MapsToWSchwaSplitN`** | `(W, En)` | `("w\u0259", "n", false)` |
| **`Weng_MapsToWSchwaSplitNg`** | `(W, Eng)` | `("w\u0259", "\u014B", false)` |
| **`Wo_MapsToWoNoOmit`** | `(W, O)` | `("wo", "", false)` |
| **`Wu_MapsToUWithOmit`** | `(W, U)` | `("u", "", true)` |

#### テーブル網羅性テスト（5 件）

| テストケース | 検証内容 |
|------------|---------|
| `InitialTable_CoversAllNonYWNonNoneEnumValues` | 声母テーブルが Y/W/None を除く 20 値を網羅する（全 22 - Y - W = 20） |
| `FinalTable_CoversAllNonNoneEnumValues` | 韻母テーブルが None を除く全 36 値を網羅する |
| `ToneTable_HasCorrectLength` | 声調配列の長さが 5（Neutral=0 ~ Fourth=4） |
| `YWCompoundTable_Has23Entries` | Y/W 複合テーブルが厳密に 23 エントリであることを検証 |
| `NoEntryContainsU032F` | いずれのテーブルにも U+032F（非音節化符号）が含まれていないことを検証（Phase 1-R で全 strip 済み） |

#### E2E テスト（本チケット外、T02 で実施）

T02 で `ConvertSyllable` 実装後に `.claude/tmp/misaki-gold.txt` の 137 件全件を通過することを検証する。

## 6. 実装に関する懸念事項とレビュー項目

### Unicode 文字の正確性

1. **合字文字のコードポイント確認**: `ʨ` (U+02A8) と `ʦ` (U+02A6) は IPA Extensions ブロック (U+0250-U+02AF)、`ꭧ` (U+AB67) は Latin Extended-E ブロック (U+AB30-U+AB6F) に属する。これらが正しく .NET の `char` / `string` で BMP 内文字として扱えることを確認すること（全て BMP 内のため UTF-16 単一 code unit で表現可能）。

2. **U+0265 ɥ、U+025A ɚ、U+0254 ɔ、U+0268 ɨ、U+028A ʊ、U+025B ɛ、U+0259 ə、U+0264 ɤ の正確性**: Phase 1-R で判明した追加の IPA 特殊文字。すべて Kokoro vocab に含まれる（Inv6 verified）ことを確認済み。

3. **矢印記号のフォント互換性**: U+2192/U+2197/U+2193/U+2198 は Arrows ブロックに属し、IPA 文字と同列に出力される。Misaki 公式実装と Kokoro vocab が実際にこれらのコードポイントを期待していることは Phase 1-R で verified。

4. **U+032F 非音節化符号の非使用**: 旧仕様では Ai/Ei/Ao/Ou/Iao/Iu/Uai/Ui/Ong/Iong の 10 韻母で U+032F を使用していたが、Phase 1-R で **Misaki 公式実装は U+032F を全く使わない** ことが判明。テーブル定義時に U+032F を含めないこと、および `NoEntryContainsU032F` テストで全テーブル検証すること。

### 言語学的正確性（Phase 1-R 検証済み）

5. **Ong の音韻表記**: 標準 IPA / PinyinToIpa は `ʊŋ` (U+028A U+014B)、Misaki も同じ `ʊŋ` を使用する（旧仕様の `u̯ŋ` は Phase 1 の推測誤り）。

6. **Ian/Iong は半母音 j 開始**: Misaki は "ian" を `jɛn`、"iong" を `jʊŋ` として出力する（i ではなく j 開始）。対照的に Misaki "ie" は `je` であり、`jɛ` ではない（ここは標準 IPA の `iɛ` とも異なる特殊な Misaki 仕様）。

7. **Ve/Van は ɥ 開始**: Misaki は "üe" を `ɥe`、"üan" を `ɥɛn` として出力する（y ではなく U+0265 ɥ 開始）。

8. **Er は ɚ 単独**: Misaki は "er" を `ɚ` (U+025A) 単一記号で出力する（`əɻ` の 2 文字ではない）。

9. **retroflex/alveolar apical は共に ɨ**: Misaki は zh/ch/sh/r + i と z/c/s + i の両方で `ɨ` (U+0268) を直接使用する（`ɻ̩`/`ɹ̩` の結合音節主音記号付き 2 文字形式ではない）。

### コード品質

10. **既存変換クラスとの整合性**: `PinyinToIpa.cs` / `PinyinToPiperIpa.cs` と同一の `internal static class` パターン、同一のフィールド命名規則（`s_*Misaki`）に従うこと。ただし韻母テーブルは Prefix/Suffix タプルのため、既存 2 クラスの `string` 単純マップとは構造が異なる。

11. **テーブルの不変性**: テーブルは `static readonly` で宣言し、実行時に変更されないことを保証すること。

12. **Y/W 複合韻母テーブルの存在理由の明示**: コードコメントで「Y/W は DotNetG2P PinyinParser の都合で声母にアサインされるが、Misaki では複合韻母として扱われる」ことを明記する。

## 7. 一から作り直すとしたら

### 現行設計の評価

現在の変換クラス群（`PinyinToIpa`, `PinyinToPiperIpa`, `PinyinToZhuyin`）は、各クラスが独立した `Dictionary<Initial/Final, string>` テーブルを持つ「コピー&修正」パターンを採用している。このアプローチは以下の利点がある:

- 各変換形式が完全に自己完結しており、依存関係がない
- 新しい形式の追加が既存コードに影響しない
- デバッグ時にテーブルを直接参照できる

一方で以下の課題がある:

- 22+36+5+23 = 86 エントリ × 4 形式 = 多数のテーブルエントリの管理が必要（今後さらに増加）
- テーブル間の差異が暗黙的であり、どのエントリが異なるか一覧しにくい
- 新しい Initial/Final が追加された場合、全変換クラスを更新する必要がある

### Phase 1-R の教訓（最重要、Phase 1 を一から作り直すとしたら何を変えるか）

本セクションは Phase 1 で策定した旧 T01/T02 の設計が 12 項目にわたって誤っており、Phase 1-R で全面改訂となった経験を踏まえ、後続プロジェクトへの教訓として残すもの。

#### 教訓 1: 公式実装を必ず fetch して実測してから設計する

**Phase 1 の失敗**: Misaki の独自表記（合字・矢印声調・非音節化符号）を Kokoro リポジトリの README / Python パッケージ概要・公開サンプル出力から「推測」して設計した。結果:

- `J` → `ʨ` は偶然正解だったが、`Zh` → `ʈʂ`（誤）/ 正 `ꭧ` U+AB67 を見逃した
- 非音節化符号 U+032F の大量使用（誤）を仕様に組み込んでしまった
- 声調位置を「末尾付加」（誤）と設計したが、実際は「韻母中間挿入」だった
- Retroflex apical を `ɻ̩`（誤）と想定したが、実際は `ɨ` 単一文字だった

**Phase 1-R での対応**:

```bash
# 公式実装を fetch
gh api repos/hexgrad/misaki/contents/misaki/zh.py
gh api repos/hexgrad/misaki/contents/misaki/transcription.py
# uv で実環境を構築
uv init misaki-verify
uv add misaki
# 137 件のテストケースを実測
uv run python -c "import misaki.zh; g = misaki.zh.ZHG2P(); print(g('ma1'))"
# → 実際の出力から逆算してマッピングを再構成
```

**後続プロジェクトへのアクション**:

- Misaki/piper-plus/Kokoro/Flite 等の他言語 G2P 実装を参考にする場合、**必ず公式 GitHub リポジトリから実装ファイルを fetch し、uv / pip / docker 等で実行環境を作って実測する**
- 実測できない場合は「推測」を明記し、T02 での実測検証を必須タスクとする
- 最小 50 件程度の gold standard（`.claude/tmp/<lang>-gold.txt`）を作成し、T01 の時点からテストに組み込む

#### 教訓 2: 声調位置・韻母構造を最初から設計に入れる

**Phase 1 の失敗**: 韻母テーブルを `Dictionary<Final, string>`（単純な文字列マップ）として設計した。結果:

- `man1` を `mAn→`（末尾声調）と想定したが、実際は `ma→n`（中間声調）
- 16 韻母（An/En/Ang/Eng/Ong/In/Ing/Ian/Iang/Iong/Uan/Un/Uang/Ueng/Van/Vn）で全面修正が必要となった

**Phase 1-R での対応**:

韻母テーブルを `Dictionary<Final, (string Prefix, string Suffix)>`（Prefix + Tone + Suffix 方式）に変更。`ConvertSyllable` は `prefix + toneArrow + suffix` の順に結合する。

**後続プロジェクトへのアクション**:

- 韻母 / 音節 / スタック構造を扱う言語では、声調位置・ストレス位置・アクセント核位置を「どこに挿入するか」を最初から設計に組み込む
- 単純な `string` マップは後から構造変更しにくい → 最初から `(Prefix, Suffix)` タプルまたは `record PhonemeTemplate(string Prefix, string Suffix, int ToneInsertPosition)` で構造化する
- 「末尾に付加」という仮定を最初から疑う

#### 教訓 3: Y/W は声母ではなく複合韻母として扱うのが Misaki 公式の設計思想

**Phase 1 の失敗**: DotNetG2P の `PinyinParser` が "wang" を `Initial.W + Final.Ang` に parse するため、`W` を `w` 1 文字、`Ang` を `aŋ` とマップし、連結すれば `waŋ→` になると想定した。実際には Misaki は:

- "wa" → `wa→`、"wai" → `wai→`、"wang" → `wa→ŋ`（`w` が複合韻母の一部）
- "yu" → `y→`（`ɥy→` ではなく、ɥ は省略される）
- "yi" → `i→`（`ji→` ではなく、j は省略される）
- "wu" → `u→`（`wu→` ではなく、w は省略される）

単純な声母マップ方式では Initial 省略ルールが表現できない。

**Phase 1-R での対応**:

`s_yWCompoundMisaki : Dictionary<(Initial, Final), (Prefix, Suffix, OmitInitial)>` という **23 エントリの専用テーブル** を追加。`ConvertSyllable` は Y/W 声母の場合このテーブルを最優先で参照し、Initial を省略するかどうかを `OmitInitial` フラグで判定する。

**後続プロジェクトへのアクション**:

- Pinyin parser の内部構造（Initial + Final）と、目的の音素表記系（中国語音韻論）の構造が一致しない場合は、**複合韻母層を個別のルックアップテーブルとして用意する**
- 「Y/W を声母として 1 文字マップする」という設計パターンは DotNetG2P.Chinese の PinyinToIpa / PinyinToPiperIpa / PinyinToZhuyin 全てで採用されているが、音韻論的には正しくない（Y/W は中国語音韻論では半母音として Medial に属し、声母ではない）。将来的に PinyinParser を再設計する場合は Medial 層を独立させることを検討する

#### 教訓 4: ligature (U+02A8/U+02A6) と U+AB67 は Kokoro vocab 互換のため必須採用

**Phase 1 の失敗**: Phase 1 では `J` → `ʨ` (U+02A8) を採用したが、内部では「見た目上 tɕ と同等なのでどちらでも良い」と判断していた。同様に `Zh` → `ʈʂ` (U+0288 U+0282) を「合字にしても実質同じ」と想定していた。

**Phase 1-R で判明した事実**:

- **Kokoro 82M の vocab ファイルには U+02A8 / U+02A6 / U+AB67 が単一トークンとして含まれる**（Inv6 verified）
- **`tɕ` (U+0074 U+0255) や `ʈʂ` (U+0288 U+0282) は Kokoro vocab に含まれない**
- **非 vocab 文字を TTS に入力すると UNK トークン化され、音声品質が著しく劣化する**

したがって、Kokoro 互換性を保証するためには **必ず合字を使う必要がある**。

**後続プロジェクトへのアクション**:

- TTS vocab 互換性を前提とする G2P 実装では、**出力対象の TTS の vocab ファイル（tokenizer.json / phoneme_set.txt 等）を最初から取得し、マッピングに含まれる全文字が vocab に存在することを検証する**
- DotNetG2P.Chinese では Kokoro vocab との照合を CI で自動化することを将来的に検討（現状は Phase 1-R の人手検証）
- ligature（結合済み単一文字）vs 2 文字並置の違いは視覚的に判別しづらいため、**必ず Unicode コードポイントで明記する**（`ʨ` ではなく `U+02A8`）

#### 教訓 5: gold standard を T01 の時点でテストに組み込む

**Phase 1 の失敗**: Phase 1 では「テーブル定義の T01 ではテーブル単体のユニットテストのみ、E2E テストは T02 で実施」という方針だった。結果、T02 実装時に初めて仕様誤りに気づいた。

**Phase 1-R での対応**:

`.claude/tmp/misaki-gold.txt` に 137 件の `(pinyin → misaki IPA)` ペアを実測で取得し、T01 の時点で全テーブルエントリが gold standard と整合するかを「卓上検証」（エントリを手動で組み合わせて gold 出力と比較）する。

**後続プロジェクトへのアクション**:

- T01（テーブル定義）の段階で gold standard に対する卓上検証を必須化する
- 137 件の gold standard を直接テストにするのは T02 スコープだが、T01 ではサンプル 20 件程度をテーブル組み合わせテスト（`Prefix + ToneArrow + Suffix` 結合）として実装する
- gold standard は repository 外（`.claude/tmp/`）ではなく、`tests/DotNetG2P.Tests/ChineseG2P/Fixtures/misaki-gold.txt` に埋め込みリソースとして配置することを T02 で検討する

### 代替設計案（参考、Phase 1 からの継続）

#### A案: 差分テーブル方式

PinyinToIpa を基準テーブルとし、Misaki は差分のみ定義する。ただし Phase 1-R で判明したように **Misaki 差異は 6 + 16 + 4 + 23 + 3 = 52 箇所に及ぶ** ため、差分方式の利点（差異の局所化）は希薄化する。

#### E案: 事前マージ方式

BaseIpaTable と MisakiDiff を起動時にマージする方式。Phase 1-R での差異数増加により、現行の独立テーブル方式と実用上の差はない。

#### 推奨（Phase 1-R 後の再評価）

**現行の独立テーブルパターン（コピー&修正方式）を継続する。** 理由:

1. 変換形式は 4 種類（IPA / Piper IPA / Misaki / Zhuyin）にとどまり、管理負荷は許容範囲内
2. 既存の 3 クラスとの一貫性を維持できる
3. 各テーブルが自己完結しており、バグの局所化が容易
4. ランタイムのパフォーマンスオーバーヘッドがゼロ
5. **Phase 1-R で Misaki 差異が予想よりも多岐にわたることが判明したため、差分方式の利点は薄い**

ただし、本チケットのテーブル設計時に差異を明確にドキュメント化し（本チケット自体がその役割を果たす）、Phase 1-R gold standard 137 件との照合を T02 で実施することで、将来の仕様誤り再発を防ぐ。

### Unicode 定数クラスの導入（強く推奨）

```csharp
// src/DotNetG2P.Chinese/Internal/ChineseUnicode.cs
internal static class ChineseUnicode
{
    // IPA 修飾子
    public const string Aspirated = "\u02B0";           // ʰ

    // IPA 合字（Misaki 用）
    public const string TcLigature = "\u02A8";          // ʨ
    public const string TsLigature = "\u02A6";          // ʦ
    public const string RetroflexAffricate = "\uAB67";  // ꭧ

    // IPA 特殊母音（Misaki 用）
    public const string SchwaHook = "\u025A";           // ɚ
    public const string OpenO = "\u0254";               // ɔ
    public const string BarredI = "\u0268";             // ɨ
    public const string UpperU = "\u028A";              // ʊ
    public const string Epsilon = "\u025B";             // ɛ
    public const string Schwa = "\u0259";               // ə
    public const string Ramshorn = "\u0264";            // ɤ
    public const string TurnedH = "\u0265";             // ɥ
    public const string EngNg = "\u014B";               // ŋ

    // 声調矢印（Misaki 用）
    public const string ArrowRight = "\u2192";          // →
    public const string ArrowNE = "\u2197";             // ↗
    public const string ArrowDown = "\u2193";           // ↓
    public const string ArrowSE = "\u2198";             // ↘
}
```

これにより、T01 のマッピング定義は:

```csharp
private static readonly Dictionary<Initial, string> s_initialMisaki = new()
{
    [Initial.J] = ChineseUnicode.TcLigature,                              // Misaki差異: tɕ→ʨ
    [Initial.Q] = ChineseUnicode.TcLigature + ChineseUnicode.Aspirated,   // Misaki差異: tɕʰ→ʨʰ
    [Initial.Zh] = ChineseUnicode.RetroflexAffricate,                     // Misaki差異: ʈʂ→ꭧ
    [Initial.Z] = ChineseUnicode.TsLigature,                              // Misaki差異: ts→ʦ
    // ...
};
```

と書ける。レビュー時の Unicode 誤読リスクが大幅に減り、Phase 1-R で判明した 12 項目の差分検証が容易になる。

### Kokoro 82M vocab 互換性（Inv6 verified）

以下の全てが Kokoro base vocab に含まれることを Phase 1-R で確認済み:

- `ꭧ` (U+AB67) - Latin Extended-E
- `ʨ` (U+02A8), `ʦ` (U+02A6) - IPA Extensions
- `ɨ` (U+0268), `ɥ` (U+0265) - IPA Extensions
- `ʊ` (U+028A), `ə` (U+0259), `ɤ` (U+0264) - IPA Extensions
- `ɛ` (U+025B), `ɚ` (U+025A), `ɔ` (U+0254) - IPA Extensions
- `→` (U+2192), `↗` (U+2197), `↓` (U+2193), `↘` (U+2198) - Arrows

したがって zh/ch に U+AB67、j/q に ʨ、z/c に ʦ を採用しても全て Kokoro に正しくトークン化される。U+032F は vocab 非含有だが、Phase 1-R の仕様ではそもそも使用しないためテンプレ側で自動的に除外される。

## 8. 後続タスクへの連絡事項

### T02（Convert メソッド統合）に伝える情報

以下の要点は T02 の `ConvertSyllable` 実装時に必須で参照すること。

1. **テーブルフィールド名**: `s_initialMisaki`, `s_finalMisaki`, `s_toneArrows`, `s_yWCompoundMisaki` を使用。既存 PinyinToIpa の `s_initialIpa` 等と区別する。

2. **韻母テーブルの構造**: `Dictionary<Final, (string Prefix, string Suffix)>` タプル型。既存 PinyinToIpa / PinyinToPiperIpa の `Dictionary<Final, string>` とは異なるので、ConvertSyllable 実装時は必ず Prefix + ToneArrow + Suffix の順に結合すること。

3. **特殊ケース判定の順序** (セクション 3.3 参照):
   1. Initial.None + Final.O → `"ɔ" + toneArrow`
   2. Final.Er → `"ɚ" + toneArrow`
   3. Zh/Ch/Sh/R + Final.I → `initialMisaki[initial] + "ɨ" + toneArrow`
   4. Z/C/S + Final.I → `initialMisaki[initial] + "ɨ" + toneArrow`
   5. Y/W + Final → `s_yWCompoundMisaki` lookup
   6. それ以外 → 標準パス（initial + prefix + toneArrow + suffix）

4. **Y/W 声母の処理**: Y/W は `s_initialMisaki` に含めない。`s_yWCompoundMisaki[(initial, final)]` を参照し、`OmitInitial` フラグに従って Initial 文字の出力を制御する（yi/yin/ying/yu/yun/wu の 6 ケースで省略）。

5. **そり舌・歯茎母音の処理**: PinyinToIpa とは異なり、Misaki は両者とも `ɨ` (U+0268) 単一文字を使用する。別フィールド（`s_retroflexApical` / `s_alveolarApical`）を定義せず、特殊ケース判定内で直接 `"\u0268"` を返す実装でよい。

6. **声調矢印の位置**: 韻母の Prefix と Suffix の間に挿入する。Suffix が空文字の韻母の場合は末尾付加と等価になる（例: `Final.A` → `prefix="a"`, `suffix=""`, `ma1` → `m + a + → + "" = ma→`）。

7. **`ShouldOmitSemivowel` ロジックは廃止**: PinyinToIpa にある `ShouldOmitSemivowel` は Y/W を声母として扱う旧実装向けのロジック。PinyinToMisaki では `s_yWCompoundMisaki.OmitInitial` フラグで代替する。

8. **`U+032F` は使用しない**: Phase 1-R で判明した通り、Misaki は非音節化符号を使わない。`.Replace("\u032F", "")` 等の後処理は不要。

9. **gold standard 検証**: `.claude/tmp/misaki-gold.txt` の 137 件を `Fixtures/` にコピーして E2E テストに組み込むこと。少なくとも以下のサンプルは必ず通ること:
   - `ma1/2/3/4/5` → `ma→/ma↗/ma↓/ma↘/ma`（声調全パターン）
   - `ji1`, `qi2`, `xi3` → `ʨi→`, `ʨʰi↗`, `ɕi↓`（ligature）
   - `zhi4`, `chi1`, `shi2`, `ri3` → `ꭧɨ↘`, `ꭧʰɨ→`, `ʂɨ↗`, `ɻɨ↓`（retroflex apical）
   - `zi4`, `ci1`, `si2` → `ʦɨ↘`, `ʦʰɨ→`, `sɨ↗`（alveolar apical）
   - `bo1`, `po2`, `mo3`, `fo4` → `pwo→`, `pʰwo↗`, `mwo↓`, `fwo↘`（bpmf + o）
   - `o1/2/3/4` → `ɔ→/ɔ↗/ɔ↓/ɔ↘`（単独感嘆詞 o）
   - `man1`, `mang1` → `ma→n`, `ma→ŋ`（声調中間挿入）
   - `lian1`, `jian1` → `ljɛ→n`, `ʨjɛ→n`（ian 処理）
   - `ya1`, `yi1`, `yu1`, `yue1`, `yuan1` → `ja→`, `i→`, `y→`, `ɥe→`, `ɥɛ→n`（Y 複合）
   - `wa1`, `wu1`, `wang1`, `wen1` → `wa→`, `u→`, `wa→ŋ`, `wə→n`（W 複合）
   - `er1/2/3/4` → `ɚ→/ɚ↗/ɚ↓/ɚ↘`（Er 単独）
   - `long1`, `dong1`, `xiong2` → `lʊ→ŋ`, `tʊ→ŋ`, `ɕjʊ↗ŋ`（ong / iong）

10. **Convert メソッドのシグネチャ**: `PinyinToIpa.Convert(string pinyin, bool includeTones)` と同一のシグネチャを推奨。PinyinToPiperIpa のように声調なし固定にはしない（Misaki は声調を使用するため）。

## 9. 紐づけ

- **マイルストーン**: Mi1（PinyinToMisaki 変換クラス）
- **依存**: なし
- **後続**: T02（`ConvertSyllable` メソッド統合・`ChineseG2PEngine` への組み込み）
- **関連 Issue**: #56
- **関連 spec**: `.claude/tmp/misaki-spec.md`（Phase 1-R verified 完全仕様）
- **関連 gold standard**: `.claude/tmp/misaki-gold.txt`（uv misaki 0.9.4 で実測した 137 件）
