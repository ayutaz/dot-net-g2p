---
ticket: T02
title: PinyinToMisaki Convert メソッド統合 (Phase 1-R verified)
milestone: Mi1
status: 未着手
depends_on: [T01]
blocks: [T03]
---

# T02: PinyinToMisaki Convert メソッド統合

> **Phase 1-R 仕様全面改訂版**: `misaki==0.9.4` を `uv run` で実測した 137 件の
> gold standard (`.claude/tmp/misaki-gold.txt`) と spec (`.claude/tmp/misaki-spec.md`)
> に基づく「Prefix + Tone + Suffix」テンプレ方式の新設計。
> 旧版 (U+032F 付与 + 末尾 tone letters 方式) は完全に破棄される。

## 1. タスク目的とゴール

T01 で Phase 1-R 仕様に基づき再定義された Misaki マッピングテーブル (`s_initialMisaki`,
`s_finalMisaki` [Prefix/Suffix 構造]、`s_yWCompoundMisaki` [Y/W 複合 final]、
`s_toneArrows`) を使い、`PinyinToMisaki` クラスに **新しい ConvertSyllable パイプライン**
を実装する。

**ゴール:**

- `PinyinToMisaki.Convert(string pinyin)` — 数字声調/声調記号付きピンイン文字列を受け取り、
  Misaki 互換の音素文字列を返す (声調矢印付き、デフォルト `includeTones = true`)
- `PinyinToMisaki.Convert(string pinyin, bool includeTones)` — 声調矢印の有無を制御可能
- `PinyinToMisaki.ConvertSyllable(PinyinSyllable syllable, bool includeTones)` —
  パース済み `PinyinSyllable` を受け取り、**Prefix + Tone + Suffix** の新パイプラインで
  Misaki 文字列を生成する
- `dotnet build DotNetG2P.slnx` が通過すること
- Convert 単体のユニットテスト (`PinyinToMisakiConvertTests.cs`) が 120+ ケース全件パスすること
- **全期待値は `.claude/tmp/misaki-gold.txt` の `misaki==0.9.4` 実測値と照合する**
  (verified against misaki 0.9.4 via `uv run python -c ...`)

### 新設計の核心 — なぜ書き直すのか

旧 T02 (2026-04-12 以前) は `PinyinToIpa` の構造を流用し「声母 + 韻母 + 末尾 tone letter」という
Linear-Append パイプラインを前提としていた。しかし Phase 1-R の実測で以下が判明した:

| 要素 | 旧 T02 の前提 | 実測 (misaki 0.9.4) |
|-----|-------------|------------------|
| 声調位置 | 末尾付与 (`ma` + `˥˥` → `ma˥˥`) | **韻母の prefix と suffix の間** (`ma` + `→` / `ma` + `→n`) |
| Y/W 処理 | `ShouldOmitSemivowel` 分岐で声母のみ省略 | **複合 final テーブル** 参照 (`Y+Ong` → `jʊ→ŋ`、`W+En` → `wə→n` 等) |
| そり舌 (zh/ch/sh/r + i) | `ɻ̩` (U+027B U+0329) | `ɨ` (U+0268) 直接 |
| 歯茎 (z/c/s + i) | `ɹ̩` (U+0279 U+0329) | `ɨ` (U+0268) 直接 |
| 感嘆詞 (Initial.None + Final.O) | `wo` テンプレそのまま | **特別ケース** → `ɔ` (U+0254) |
| Er 韻母 | `ɚ` 単純置換 | Initial あっても声母 + `ɚ` + tone |
| U+032F (非音節化符号) | 出力に含める | **出力に含めない** (テンプレ側で事前除去済) |

この結果、旧パイプラインでは「末尾 tone」「声母出力制御」「特殊母音の後処理」がいずれも
実測と噛み合わず、最低でも 40% のテストケースで不一致を発生させることが確定した。
Phase 1-R で Prefix + Tone + Suffix 方式に全面移行する。

## 2. 実装する内容の詳細

### 2.1 ファイル配置

```
src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs
```

T01 (Phase 1-R 版) で既に以下が定義済みの前提:

- `s_initialMisaki` (21 エントリ、Y/W 含む。Y/W の値は後述の「Y/W 処理」で詳述)
- `s_finalMisaki` — **新設計**: `Dictionary<Final, (string Prefix, string Suffix)>` として
  prefix と suffix を分離保持 (旧版の単一 string からの破壊的変更)
- `s_yWCompoundMisaki` — **新設計**: `Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)>`
  として Y/W + Final の複合パターンを保持 (T02 で参照する主要テーブル)
- `s_toneArrows` — 5 要素配列 `["", "→", "↗", "↓", "↘"]` (Neutral/1/2/3/4)

T02 では上記テーブルを参照する `Convert` / `ConvertSyllable` メソッドのみを追加する。

### 2.2 メソッドシグネチャ

```csharp
namespace DotNetG2P.Chinese
{
    internal static class PinyinToMisaki
    {
        // --- T01 (Phase 1-R) で定義済み ---
        // private static readonly Dictionary<Initial, string> s_initialMisaki;
        // private static readonly Dictionary<Final, (string Prefix, string Suffix)> s_finalMisaki;
        // private static readonly Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)> s_yWCompoundMisaki;
        // private static readonly string[] s_toneArrows;  // ["", "→", "↗", "↓", "↘"]

        // --- T02 で実装 ---

        /// <summary>
        /// 声調矢印付きピンインを Misaki 互換表記に変換する (<c>includeTones = true</c>)。
        /// </summary>
        /// <param name="pinyin">数字声調形式 ("ma1") または声調記号付き ("mā") のピンイン文字列。</param>
        /// <returns>Misaki 互換音素文字列。失敗時は空文字列。</returns>
        public static string Convert(string pinyin);

        /// <summary>
        /// 声調矢印の付与を制御しながらピンインを Misaki 互換表記に変換する。
        /// </summary>
        public static string Convert(string pinyin, bool includeTones);

        /// <summary>
        /// パース済み音節構造体を Misaki 互換表記に変換する (新 Prefix + Tone + Suffix パイプライン)。
        /// </summary>
        internal static string ConvertSyllable(PinyinSyllable syllable, bool includeTones);
    }
}
```

引数なし `Convert(string pinyin)` は `Convert(pinyin, true)` に委譲する
(既存 `PinyinToIpa` と同一パターン)。

### 2.3 ConvertSyllable 新パイプライン (5 ステップ)

**これが Phase 1-R の核心変更である。** 必ず以下の 5 ステップを **この順序で** 実行すること。

```
ConvertSyllable(syllable, includeTones):

1. 声調矢印決定
   ─────────────
   string toneArrow = (includeTones && syllable.Tone != Tone.Neutral)
                    ? s_toneArrows[(int)syllable.Tone]
                    : string.Empty;

2. 特別ケース判定 (早期 return)
   ─────────────────────────────
   a. Initial.None + Final.O
      → return "\u0254" + toneArrow;        // ɔ (単独感嘆詞 ō/ó/ǒ/ò)

   b. Final.Er
      → 声母があれば s_initialMisaki[initial] + "\u025A" + toneArrow;
        なければ "\u025A" + toneArrow;       // ɚ (U+025A)

   c. (Zh|Ch|Sh|R) + Final.I  (retroflex apical)
      → return s_initialMisaki[initial] + "\u0268" + toneArrow;   // ɨ

   d. (Z|C|S) + Final.I  (alveolar apical)
      → return s_initialMisaki[initial] + "\u0268" + toneArrow;   // ɨ

3. Y/W + Final 複合ルックアップ
   ────────────────────────────
   if (s_yWCompoundMisaki.TryGetValue((syllable.Initial, syllable.Final), out var comp))
   {
       // hit: テーブルから (prefix, suffix, omitInitial) を取得
       prefix      = comp.Prefix;
       suffix      = comp.Suffix;
       omitInitial = comp.OmitInitial;
   }
   else
   {
       // miss: standard path
       var entry   = s_finalMisaki[syllable.Final];   // (Prefix, Suffix)
       prefix      = entry.Prefix;
       suffix      = entry.Suffix;
       omitInitial = false;
   }

4. 声母出力
   ────────
   var sb = new StringBuilder(16);
   if (!omitInitial && syllable.Initial != Initial.None
       && s_initialMisaki.TryGetValue(syllable.Initial, out var initialStr))
   {
       sb.Append(initialStr);
   }

5. 構築 (Prefix → Tone → Suffix の順)
   ────────────────────────────────────
   sb.Append(prefix);
   sb.Append(toneArrow);
   sb.Append(suffix);
   return sb.ToString();
```

**ポイント:**

- **ステップ 2 の順序は固定**: `(a) ó 感嘆詞` → `(b) Er` → `(c) retroflex` → `(d) alveolar`。
  特に `(a)` は `(b)` より前にチェックする必要がある (Final.Er と Final.O は排他なので
  実際の順序影響はないが、可読性のため固定)。
- **ステップ 3 は lookup 中心**: 旧版の `ShouldOmitSemivowel` ヘルパー関数は不要。
  `s_yWCompoundMisaki` テーブルがその役割を完全に引き継ぐ。
- **ステップ 5 の順序**: 必ず `prefix → toneArrow → suffix` の順。
  Misaki の声調は「韻母の核母音の後、後続子音の前」に入るため、
  `ma→n` (man1) / `jʊ→ŋ` (yong1) のようにテンプレの中間に挟まる構造になる。
- **ステップ 1 の `toneArrow` は空文字列を許容**: `includeTones=false` や `Tone.Neutral`
  (軽声) の場合は空文字列が入り、ステップ 5 で `Append("")` されるだけ (問題なし)。

### 2.4 既存 PinyinToIpa との主要差分

| 項目 | PinyinToIpa (既存) | PinyinToMisaki (Phase 1-R) |
|------|-----------------|-------------------------|
| マッピング構造 | `Dictionary<Final, string>` (単一 string) | **`Dictionary<Final, (Prefix, Suffix)>` (タプル)** |
| 声調位置 | 末尾付与 (`s_toneLetters` を末尾 Append) | **韻母 prefix と suffix の間** (中置) |
| 声調記号 | IPA tone letters (`˥˥`/`˧˥`/`˨˩˦`/`˥˩`) | 矢印 (`→`/`↗`/`↓`/`↘`) |
| Y/W 処理 | `ShouldOmitSemivowel` ヘルパー関数 + `s_initialIpa[Y/W]` 出力 | **`s_yWCompoundMisaki` 複合 final lookup** |
| そり舌 (zh/ch/sh/r + i) | `s_retroflexApical` = `ɻ̩` (U+027B U+0329) | **`ɨ` (U+0268) 直接** |
| 歯茎 (z/c/s + i) | `s_alveolarApical` = `ɹ̩` (U+0279 U+0329) | **`ɨ` (U+0268) 直接** |
| 感嘆詞 (Initial.None + Final.O) | 標準ルートで `wo` 出力 | **特別ケース** → `ɔ` (U+0254) |
| Final.Er | `ɚ` (U+025A) 単純 | 同上 (声母ありの稀ケースもサポート) |
| U+032F (非音節化符号) | 付与する (`ai̯` 等) | **付与しない** (テンプレ側で事前除去済み) |
| includeTones=false 時 | 声調 letters のみ省略 | 矢印のみ省略、テンプレは維持 |
| StringBuilder 使用 | あり (16 容量) | あり (16 容量、同一) |
| IsRetroflex / IsAlveolar | private static メソッド | **switch 式 or 直接判定** (ヘルパー関数化しない) |

### 2.5 参照すべき既存コード

| ファイル | 参照理由 |
|---------|---------|
| `src/DotNetG2P.Chinese/Conversion/PinyinToIpa.cs` | `Convert` の入口処理 (null → ToneConverter → PinyinParser → ConvertSyllable) のテンプレート |
| `src/DotNetG2P.Chinese/Conversion/PinyinToPiperIpa.cs` | 同構造の別バリアント。構造参考用 |
| `src/DotNetG2P.Chinese/Conversion/PinyinParser.cs` | TryParse の仕様。j/q/x/y 後の u→v 正規化ロジック。lv/nv → Final.V 系への変換 |
| `src/DotNetG2P.Chinese/Conversion/ToneConverter.cs` | ToToneMarked / ExtractTone の仕様 |
| `src/DotNetG2P.Chinese/Models/PinyinSyllable.cs` | Initial/Final/Tone enum の構造体定義 |
| `.claude/tmp/misaki-spec.md` | **Phase 1-R 仕様書** (verified against misaki 0.9.4) |
| `.claude/tmp/misaki-gold.txt` | **137 件の gold standard** (uv run 実測) |

## 3. Y/W 処理ロジック詳細

Phase 1-R の最大の設計変更点は、Y/W を「声母マップ単独 + ShouldOmitSemivowel 関数」ではなく
**「複合 final テーブルによる一括 lookup」で処理する** 点である。

### 3.1 DotNetG2P PinyinParser の構造

`PinyinParser` は "wang" を `Initial.W + Final.Ang` に、"yue" を `Initial.Y + Final.Ve` に
パースする (Misaki Python 側の "uang"/"üe" とは異なる内部表現)。したがって
`ConvertSyllable` で以下の変換を行う必要がある:

### 3.2 Y/W 複合 final 変換テーブル

T01 で定義する `s_yWCompoundMisaki` は以下の 24 エントリを持つ
(仕様は `.claude/tmp/misaki-spec.md` の「Y/W 複合韻母マッピング」表と完全一致)。

| Initial | Final | Misaki 等価 | Prefix | Suffix | OmitInitial | 備考 |
|---------|-------|------------|--------|--------|------------|-----|
| Y | A | Ia | `ja` | `` | No | ya → ia |
| Y | An | Ian | `jɛ` | `n` | No | yan → ian |
| Y | Ang | Iang | `ja` | `ŋ` | No | yang |
| Y | Ao | Iao | `jau` | `` | No | yao |
| Y | E | Ie | `je` | `` | No | ye |
| Y | I | I | `i` | `` | **Yes** (j 省略) | yi |
| Y | In | In | `i` | `n` | **Yes** | yin |
| Y | Ing | Ing | `i` | `ŋ` | **Yes** | ying |
| Y | Ong | Iong | `jʊ` | `ŋ` | No | yong |
| Y | Ou | Iu (iou) | `jou` | `` | No | you |
| Y | V | V (ü) | `y` | `` | **Yes** (ɥ 省略) | yu |
| Y | Ve | Ve (üe) | `ɥe` | `` | No | yue |
| Y | Van | Van (üan) | `ɥɛ` | `n` | No | yuan |
| Y | Vn | Vn (ün) | `y` | `n` | **Yes** (ɥ 省略) | yun |
| W | A | Ua | `wa` | `` | No | wa → ua |
| W | Ai | Uai | `wai` | `` | No | wai |
| W | An | Uan | `wa` | `n` | No | wan |
| W | Ang | Uang | `wa` | `ŋ` | No | wang |
| W | Ei | Ui (uei) | `wei` | `` | No | wei |
| W | En | Un (uen) | `wə` | `n` | No | wen |
| W | Eng | Ueng | `wə` | `ŋ` | No | weng |
| W | O | Uo | `wo` | `` | No | wo |
| W | U | U | `u` | `` | **Yes** (w 省略) | wu |

### 3.3 テーブル参照コードの具体例

```csharp
// T01 で定義済み
private static readonly Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)>
    s_yWCompoundMisaki = new Dictionary<(Initial, Final), (string, string, bool)>
{
    // Y 系 (14 エントリ)
    [(Initial.Y, Final.A)]    = ("ja",  "",  false),
    [(Initial.Y, Final.An)]   = ("j\u025B", "n", false),  // jɛn
    [(Initial.Y, Final.Ang)]  = ("ja",  "\u014B", false), // jaŋ
    [(Initial.Y, Final.Ao)]   = ("jau", "",  false),
    [(Initial.Y, Final.E)]    = ("je",  "",  false),
    [(Initial.Y, Final.I)]    = ("i",   "",  true),       // yi → i (j省略)
    [(Initial.Y, Final.In)]   = ("i",   "n", true),       // yin → in (j省略)
    [(Initial.Y, Final.Ing)]  = ("i",   "\u014B", true),  // ying → iŋ (j省略)
    [(Initial.Y, Final.Ong)]  = ("j\u028A", "\u014B", false), // jʊŋ
    [(Initial.Y, Final.Ou)]   = ("jou", "",  false),      // you → iou
    [(Initial.Y, Final.V)]    = ("y",   "",  true),       // yu → y (ɥ省略)
    [(Initial.Y, Final.Ve)]   = ("\u0265e", "", false),   // yue → ɥe
    [(Initial.Y, Final.Van)]  = ("\u0265\u025B", "n", false), // yuan → ɥɛn
    [(Initial.Y, Final.Vn)]   = ("y",   "n", true),       // yun → yn (ɥ省略)

    // W 系 (9 エントリ)
    [(Initial.W, Final.A)]    = ("wa",  "",  false),
    [(Initial.W, Final.Ai)]   = ("wai", "",  false),
    [(Initial.W, Final.An)]   = ("wa",  "n", false),
    [(Initial.W, Final.Ang)]  = ("wa",  "\u014B", false),
    [(Initial.W, Final.Ei)]   = ("wei", "",  false),
    [(Initial.W, Final.En)]   = ("w\u0259", "n", false),  // wə n
    [(Initial.W, Final.Eng)]  = ("w\u0259", "\u014B", false), // wə ŋ
    [(Initial.W, Final.O)]    = ("wo",  "",  false),
    [(Initial.W, Final.U)]    = ("u",   "",  true),       // wu → u (w省略)
};
```

### 3.4 ConvertSyllable でのルックアップコード例

```csharp
// ステップ 3: Y/W + Final 複合ルックアップ
string prefix;
string suffix;
bool omitInitial;

if (s_yWCompoundMisaki.TryGetValue((syllable.Initial, syllable.Final), out var compound))
{
    prefix      = compound.Prefix;
    suffix      = compound.Suffix;
    omitInitial = compound.OmitInitial;
}
else
{
    // 標準パス: 通常の Final テーブルから取得 (Y/W 以外、または Y/W だが
    // テーブルに載っていない組み合わせ — 実際には発生しないはずだが防御的に)
    if (!s_finalMisaki.TryGetValue(syllable.Final, out var standardEntry))
    {
        // 理論上到達しない (全 Final がテーブルに定義されている前提)
        return string.Empty;
    }
    prefix      = standardEntry.Prefix;
    suffix      = standardEntry.Suffix;
    omitInitial = false;
}
```

### 3.5 判定ロジック例: yi1 (Y + I + Tone 1)

入力: `PinyinSyllable { Initial = Y, Final = I, Tone = First }`、`includeTones = true`

1. ステップ 1: `toneArrow = "→"`
2. ステップ 2: Initial.None でない、Final.Er でない、retroflex でない、alveolar でない → 通過
3. ステップ 3: `s_yWCompoundMisaki[(Y, I)]` → `("i", "", true)` hit
   - `prefix = "i"`, `suffix = ""`, `omitInitial = true`
4. ステップ 4: `omitInitial == true` → 声母出力 **スキップ**
5. ステップ 5: `sb.Append("") + Append("i") + Append("→") + Append("")` → `"i→"`

**期待値 (gold.txt):** `yi1 → i→` ✓ 一致

### 3.6 判定ロジック例: wen1 (W + En + Tone 1)

入力: `PinyinSyllable { Initial = W, Final = En, Tone = First }`、`includeTones = true`

1. ステップ 1: `toneArrow = "→"`
2. ステップ 2: いずれの特別ケースにも該当しない → 通過
3. ステップ 3: `s_yWCompoundMisaki[(W, En)]` → `("wə", "n", false)` hit
   - `prefix = "wə"`, `suffix = "n"`, `omitInitial = false`
4. ステップ 4: `omitInitial == false`、Initial != None だが **W は複合 final で吸収済み** のため
   `s_initialMisaki[Initial.W]` **を出力してはならない** 。

   ⚠ **重要**: この設計では `s_yWCompoundMisaki` にエントリがある場合、prefix 側で
   既に `w` や `j` が含まれている (`wə` の `w`) ため、`omitInitial = false` であっても
   `s_initialMisaki[Initial.W]` は出力しない。
   `omitInitial` は「声母出力をスキップするか」を示すフラグであり、テーブルに hit した
   時点で `W`/`Y` の声母は prefix 側に織り込まれている扱いとなる。

   実装としては **Y/W の場合は常に声母出力スキップ** になる。正確なコード:

   ```csharp
   // ステップ 4 (訂正版)
   if (syllable.Initial != Initial.None && syllable.Initial != Initial.Y && syllable.Initial != Initial.W)
   {
       if (s_initialMisaki.TryGetValue(syllable.Initial, out var initialStr))
           sb.Append(initialStr);
   }
   ```

   `omitInitial` フラグは「Y/W 以外で声母を省略したい特殊ケース (今のところなし)」
   のための**将来拡張用**と位置づけるか、または **Y/W 複合 final lookup hit 時は
   omitInitial の値に関わらず常に省略** とする。後者が単純で実装推奨。
5. ステップ 5: `sb.Append("wə") + Append("→") + Append("n")` → `"wə→n"`

**期待値 (gold.txt):** `wen1 → wə→n` ✓ 一致

### 3.7 omitInitial フラグの真の用途

再整理すると、`omitInitial = true` のエントリは `yi/yin/ying/yu/yun/wu` の 6 種である。
これらは **prefix 側にも `j` や `w` が含まれていない** (`"i"`, `"u"`, `"y"` で始まる)。
一方 `omitInitial = false` のエントリ (例: `ya → (ja, )`, `wa → (wa, )`) は
**prefix 側に `j` や `w` が既に含まれている**。

したがって実装としては以下のどちらでも正しく動作する:

**案 A (推奨): Y/W は常に声母出力スキップ**

```csharp
if (syllable.Initial != Initial.None
    && syllable.Initial != Initial.Y
    && syllable.Initial != Initial.W)
{
    sb.Append(s_initialMisaki[syllable.Initial]);
}
```

`omitInitial` フラグは使わず、テーブル側で `prefix` に `j`/`w` を埋め込むことで
対応する。シンプル・予測可能。**本チケットはこの案 A を採用する。**

**案 B: omitInitial フラグを使う**

```csharp
if (!omitInitial
    && syllable.Initial != Initial.None
    && s_initialMisaki.TryGetValue(syllable.Initial, out var initialStr))
{
    sb.Append(initialStr);
}
```

`s_initialMisaki[Initial.Y]` と `[Initial.W]` を空文字列にする必要があり、
それは T01 の声母マッピングの意味論と矛盾する (エンジンは Y/W を「半母音声母」として扱うため)。
→ 案 A のほうが整合性が高い。

### 3.8 最終確定: ステップ 4 のコード

```csharp
// ステップ 4: 声母出力 (Y/W 以外)
if (syllable.Initial != Initial.None
    && syllable.Initial != Initial.Y
    && syllable.Initial != Initial.W
    && s_initialMisaki.TryGetValue(syllable.Initial, out var initialStr))
{
    sb.Append(initialStr);
}
```

`omitInitial` フラグは T01 のテーブル定義としては記載を許容するが (将来の拡張用 + 仕様書の
表構造との対応のため)、T02 の実装では読み取らない方針でよい。

## 4. 実装エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| **実装エージェント** | 1名 | `PinyinToMisaki.cs` に Convert (2 オーバーロード) + ConvertSyllable (新 5 ステップパイプライン) を追加。T01 のテーブル構造を参照して StringBuilder ベースで実装 |
| **テストエージェント** | 1名 | `PinyinToMisakiConvertTests.cs` を新規作成。セクション 5 の 17 カテゴリ × 代表ケース 5-10 件、計 120+ ケース。全期待値は `.claude/tmp/misaki-gold.txt` から引用 |
| **統合レビューエージェント** | 1名 | T01 テーブルとの整合性検証 (特に `s_yWCompoundMisaki` の 24 エントリ)、ステップ順序の正しさ (特別ケースの優先度)、U+032F 残留チェック、エッジケース (null/空/不正 pinyin) のレビュー |

合計 **3 名**。実装自体は PinyinToIpa のパターン流用 + マッピング差し替え + Y/W テーブル lookup 追加
であり、1 名で対応可能。テスト作成とレビューを並行することで Mi1 マイルストーン内に収める。

## 5. スコープとテスト項目

### 5.1 スコープ

**含む:**

- `PinyinToMisaki.Convert(string)` メソッド
- `PinyinToMisaki.Convert(string, bool)` メソッド
- `PinyinToMisaki.ConvertSyllable(PinyinSyllable, bool)` メソッド
- 新規テストファイル `tests/DotNetG2P.Tests/ChineseG2P/PinyinToMisakiConvertTests.cs` (rewrite)

**含まない (別チケット):**

- `ChineseG2PEngine.ToMisaki()` API 追加 (T03)
- マッピングテーブル定義 (T01 — **Phase 1-R 版に rewrite 済みの前提**)
- バッチ API (`ConvertBatch`) (T03)
- Multilingual 統合 (T03-T05)

### 5.2 テストファイル: `PinyinToMisakiConvertTests.cs` (rewrite)

旧テスト (`PinyinToMisakiTests.cs` 等) は全て破棄し、新ファイルを起こす。

> **期待値の出所**: 以下すべてのテストケースの expected 値は
> `.claude/tmp/misaki-gold.txt` から引用しており、これは
> `uv run python -c "from misaki.zh import ZHG2P; g=ZHG2P(); print(g('...'))"` で
> **misaki==0.9.4 を直接実行** して取得した実測値 (137 件) である。
> ハードコード禁止。将来仕様が変わった場合は `.claude/tmp/misaki-gold.txt` を
> `misaki` の新バージョンで再生成してからテスト期待値を更新すること。

### 5.3 テスト項目カテゴリ

#### カテゴリ 1: 4 声 + 軽声 × ma 系

声調矢印の基本動作確認。

```csharp
[Theory]
[InlineData("ma1", "ma\u2192")]    // ma→
[InlineData("ma2", "ma\u2197")]    // ma↗
[InlineData("ma3", "ma\u2193")]    // ma↓
[InlineData("ma4", "ma\u2198")]    // ma↘
[InlineData("ma5", "ma")]          // 軽声 (矢印なし)
public void Convert_Ma_WithAllTones(string pinyin, string expected)
{
    Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
}
```

#### カテゴリ 2: 全声母 × Final.A (21 声母網羅、第 1 声基準)

```csharp
// gold.txt より
[InlineData("ba1", "pa\u2192")]       // pa→
[InlineData("pa2", "p\u02B0a\u2197")] // pʰa↗
[InlineData("fa3", "fa\u2193")]       // fa↓
[InlineData("da4", "ta\u2198")]       // ta↘
[InlineData("ta1", "t\u02B0a\u2192")] // tʰa→
[InlineData("na2", "na\u2197")]       // na↗
[InlineData("la3", "la\u2193")]       // la↓
[InlineData("ga4", "ka\u2198")]       // ka↘
[InlineData("ka1", "k\u02B0a\u2192")] // kʰa→
[InlineData("ha2", "xa\u2197")]       // xa↗
[InlineData("ma1", "ma\u2192")]       // ma→
```

#### カテゴリ 3: j/q/z/c/zh/ch 合字検証 (U+02A8 / U+02A6 / U+AB67 確認)

```csharp
[InlineData("ji1",  "\u02A8i\u2192")]        // ʨi→
[InlineData("qi2",  "\u02A8\u02B0i\u2197")]  // ʨʰi↗
[InlineData("xi3",  "\u0255i\u2193")]        // ɕi↓
[InlineData("zi4",  "\u02A6\u0268\u2198")]   // ʦɨ↘
[InlineData("ci1",  "\u02A6\u02B0\u0268\u2192")] // ʦʰɨ→
[InlineData("si2",  "s\u0268\u2197")]        // sɨ↗
[InlineData("zhi1", "\uAB67\u0268\u2192")]   // ꭧɨ→
[InlineData("chi2", "\uAB67\u02B0\u0268\u2197")] // ꭧʰɨ↗
[InlineData("shi3", "\u0282\u0268\u2193")]   // ʂɨ↓
[InlineData("ri4",  "\u027B\u0268\u2198")]   // ɻɨ↘
```

> **注**: `zi4` は歯茎母音 (U+0268 `ɨ`)。U+02A6 は `ʦ`、U+AB67 は `ꭧ` の
> Kokoro TTS vocab 互換合字 (verified against misaki 0.9.4 via `uv run`)。

#### カテゴリ 4: 二重母音 (U+032F なし)

```csharp
[InlineData("bai1", "pai\u2192")]      // pai→ (NOT pa U+032F i)
[InlineData("mei4", "mei\u2198")]      // mei↘
[InlineData("mao1", "mau\u2192")]      // mau→ (NOT mao)
[InlineData("dou4", "tou\u2198")]      // tou↘
[InlineData("lai1", "lai\u2192")]      // lai→
[InlineData("lei1", "lei\u2192")]      // lei→
[InlineData("lao1", "lau\u2192")]      // lau→
[InlineData("lou1", "lou\u2192")]      // lou→
```

> **重要**: 出力に U+032F (COMBINING INVERTED BREVE BELOW) が含まれないことを
> 明示的にアサートするテストも追加:
>
> ```csharp
> [Fact]
> public void Convert_DoesNotIncludeU032F()
> {
>     foreach (var syl in new[] { "bai1", "mao2", "dou3", "mei4", "guei1", "jiao1" })
>     {
>         var result = PinyinToMisaki.Convert(syl);
>         Assert.DoesNotContain("\u032F", result);
>     }
> }
> ```

#### カテゴリ 5: CVC 声調位置 (声調が中間に入ることを検証)

```csharp
[InlineData("man1",  "ma\u2192n")]     // ma→n (NOT man→)
[InlineData("man2",  "ma\u2197n")]     // ma↗n
[InlineData("man3",  "ma\u2193n")]     // ma↓n
[InlineData("man4",  "ma\u2198n")]     // ma↘n
[InlineData("mang1", "ma\u2192\u014B")] // ma→ŋ
[InlineData("mang2", "ma\u2197\u014B")] // ma↗ŋ
[InlineData("mang3", "ma\u2193\u014B")] // ma↓ŋ
[InlineData("mang4", "ma\u2198\u014B")] // ma↘ŋ
[InlineData("men1",  "m\u0259\u2192n")] // mə→n
[InlineData("meng2", "m\u0259\u2197\u014B")] // mə↗ŋ
[InlineData("dong1", "t\u028A\u2192\u014B")] // tʊ→ŋ  (U+028A ʊ)
[InlineData("long1", "l\u028A\u2192\u014B")] // lʊ→ŋ
```

> **注**: `dong/long/hong/tong/gong/kong/rong` 系の `Final.Ong` は `Prefix = "ʊ"`
> (U+028A、LATIN SMALL LETTER UPSILON)、`Suffix = "ŋ"` (U+014B)。
> 旧仕様の `u̯ŋ` は誤り。

#### カテゴリ 6: i 系韻母 j 半母音 (ia/ie/iao/iu/ian/in/iang/ing/iong)

```csharp
[InlineData("jia1",   "\u02A8ja\u2192")]           // ʨja→
[InlineData("jian1",  "\u02A8j\u025B\u2192n")]     // ʨjɛ→n
[InlineData("jiao1",  "\u02A8jau\u2192")]          // ʨjau→
[InlineData("jie1",   "\u02A8je\u2192")]           // ʨje→ (NOT ʨjɛ!)
[InlineData("jiu1",   "\u02A8jou\u2192")]          // ʨjou→
[InlineData("jing1",  "\u02A8i\u2192\u014B")]      // ʨi→ŋ (j 半母音なし、In と同じ)
[InlineData("jin1",   "\u02A8i\u2192n")]           // ʨi→n
[InlineData("jiong1", "\u02A8j\u028A\u2192\u014B")] // ʨjʊ→ŋ
[InlineData("jiang1", "\u02A8ja\u2192\u014B")]     // ʨja→ŋ
[InlineData("xiong2", "\u0255j\u028A\u2197\u014B")] // ɕjʊ↗ŋ
```

#### カテゴリ 7: u 系韻母 w 半母音 (ua/uai/uan/uang/uei/uen/ueng/uo)

```csharp
[InlineData("gua1",   "kwa\u2192")]            // kwa→
[InlineData("guai1",  "kwai\u2192")]           // kwai→
[InlineData("guan1",  "kwa\u2192n")]           // kwa→n
[InlineData("guang1", "kwa\u2192\u014B")]      // kwa→ŋ
[InlineData("guo1",   "kwo\u2192")]            // kwo→
[InlineData("guei1",  "kwei\u2192")]           // kwei→ (Misaki uei)
[InlineData("guen1",  "kw\u0259\u2192n")]      // kwə→n (Misaki uen)
[InlineData("gueng1", "kw\u0259\u2192\u014B")] // kwə→ŋ (Misaki ueng)
[InlineData("lua1",   "lwa\u2192")]            // lwa→
[InlineData("luo1",   "lwo\u2192")]            // lwo→
[InlineData("luan1",  "lwa\u2192n")]           // lwa→n
```

> **注**: DotNetG2P の PinyinParser が `guei` を `Initial.G + Final.Ui` にパースする
> 前提でテストを書く。ToneConverter/Parser 層で正規化済みであることを確認すること。

#### カテゴリ 8: ü 系韻母 ɥ 半母音 (üe/üan/ün、lv/nv 経由)

```csharp
[InlineData("jue1",  "\u02A8\u0265e\u2192")]       // ʨɥe→
[InlineData("juan1", "\u02A8\u0265\u025B\u2192n")] // ʨɥɛ→n
[InlineData("jun1",  "\u02A8y\u2192n")]            // ʨy→n (ɥ省略、円唇前舌母音直接)
[InlineData("jv1",   "\u02A8y\u2192")]             // ʨy→  (Final.V)
[InlineData("jve1",  "\u02A8\u0265e\u2192")]       // ʨɥe→
[InlineData("jvan1", "\u02A8\u0265\u025B\u2192n")] // ʨɥɛ→n
[InlineData("jvn1",  "\u02A8y\u2192n")]            // ʨy→n
[InlineData("lue4",  "l\u0265e\u2198")]            // lɥe↘
[InlineData("lve1",  "l\u0265e\u2192")]            // lɥe→
[InlineData("lv1",   "ly\u2192")]                  // ly→
[InlineData("lv4",   "ly\u2198")]                  // ly↘
[InlineData("nv3",   "ny\u2193")]                  // ny↓
[InlineData("nve4",  "n\u0265e\u2198")]            // nɥe↘
```

> **注**: U+0265 は `ɥ` (LATIN SMALL LETTER TURNED H)、U+025B は `ɛ`、U+0079 は `y`
> (ASCII、円唇前舌狭母音としての IPA 記号)。`jun1` の `y` は 2 番目で、
> `ʨy→n` の構造は「声母 ʨ + prefix y + tone → + suffix n」となる。

#### カテゴリ 9: Y + X (yi/yin/ying/yu/yun/ya/ye/yao/you/yan/yang/yong/yue/yuan)

```csharp
[InlineData("yi1",   "i\u2192")]              // i→
[InlineData("yin1",  "i\u2192n")]             // i→n
[InlineData("ying1", "i\u2192\u014B")]        // i→ŋ
[InlineData("yu1",   "y\u2192")]              // y→
[InlineData("yun1",  "y\u2192n")]             // y→n
[InlineData("ya1",   "ja\u2192")]             // ja→
[InlineData("ye1",   "je\u2192")]             // je→
[InlineData("yao1",  "jau\u2192")]            // jau→
[InlineData("you1",  "jou\u2192")]            // jou→
[InlineData("yan1",  "j\u025B\u2192n")]       // jɛ→n
[InlineData("yang1", "ja\u2192\u014B")]       // ja→ŋ
[InlineData("yong1", "j\u028A\u2192\u014B")]  // jʊ→ŋ
[InlineData("yue1",  "\u0265e\u2192")]        // ɥe→
[InlineData("yuan1", "\u0265\u025B\u2192n")]  // ɥɛ→n
```

> **注**: `yi/yin/ying` は `omitInitial=true` で prefix が `i` から始まるため、
> 最終出力に `j` が含まれない (j 省略)。同様に `yu/yun` は `y` から始まる。
> 一方 `ya/yao/you/yan/yang/yong` は prefix に `j` を含む。

#### カテゴリ 10: W + X (wu/wa/wai/wei/wan/wen/wang/weng/wo)

```csharp
[InlineData("wu1",   "u\u2192")]              // u→
[InlineData("wa1",   "wa\u2192")]             // wa→
[InlineData("wai1",  "wai\u2192")]            // wai→
[InlineData("wei1",  "wei\u2192")]            // wei→
[InlineData("wan1",  "wa\u2192n")]            // wa→n
[InlineData("wen1",  "w\u0259\u2192n")]       // wə→n
[InlineData("wang1", "wa\u2192\u014B")]       // wa→ŋ
[InlineData("weng1", "w\u0259\u2192\u014B")]  // wə→ŋ
[InlineData("wo1",   "wo\u2192")]             // wo→
```

> **注**: `wu1 → u→` は `omitInitial=true` で `w` 省略。

#### カテゴリ 11: そり舌 (zh/ch/sh/r + i → `ɨ`)

```csharp
[InlineData("zhi1", "\uAB67\u0268\u2192")]      // ꭧɨ→
[InlineData("chi1", "\uAB67\u02B0\u0268\u2192")] // ꭧʰɨ→
[InlineData("shi1", "\u0282\u0268\u2192")]       // ʂɨ→
[InlineData("ri1",  "\u027B\u0268\u2192")]       // ɻɨ→
```

#### カテゴリ 12: 歯茎 (z/c/s + i → `ɨ`)

```csharp
[InlineData("zi1", "\u02A6\u0268\u2192")]        // ʦɨ→
[InlineData("ci1", "\u02A6\u02B0\u0268\u2192")]  // ʦʰɨ→
[InlineData("si1", "s\u0268\u2192")]             // sɨ→
```

> **注**: そり舌と歯茎はいずれも同じ U+0268 `ɨ` (中舌非円唇狭母音) を使用する。
> 旧仕様の `ɻ̩`/`ɹ̩` (U+027B+U+0329 / U+0279+U+0329) とは異なる。
> misaki 0.9.4 は両方とも `ɨ` 直接出力。

#### カテゴリ 13: 感嘆詞 Er (er1-4)

```csharp
[InlineData("er1", "\u025A\u2192")]  // ɚ→
[InlineData("er2", "\u025A\u2197")]  // ɚ↗
[InlineData("er3", "\u025A\u2193")]  // ɚ↓
[InlineData("er4", "\u025A\u2198")]  // ɚ↘
```

> **注**: U+025A `ɚ` (R-colored schwa、R 着色シュワー)。旧仕様の `əɻ` (U+0259+U+027B) とは異なる。
> Final.Er は特別ケースとして Initial.None での直接 return となる。

#### カテゴリ 14: 感嘆詞 O (Initial.None + Final.O → `ɔ`)

```csharp
[InlineData("o1", "\u0254\u2192")]  // ɔ→
[InlineData("o2", "\u0254\u2197")]  // ɔ↗
[InlineData("o3", "\u0254\u2193")]  // ɔ↓
[InlineData("o4", "\u0254\u2198")]  // ɔ↘
```

> **重要**: 単独の "o" (声母なし) は `wo` ではなく `ɔ` (U+0254 LATIN SMALL LETTER OPEN O)
> に変換される。これは misaki 0.9.4 の特別処理 (Initial.None + Final.O のみ) であり、
> **声母ありの `bo/po/mo/fo` とは全く異なるルート** を通る (カテゴリ 15 参照)。

#### カテゴリ 15: bpmf + o (Initial.B/P/M/F + Final.O → `pwo/pʰwo/mwo/fwo`)

```csharp
[InlineData("bo1", "pwo\u2192")]       // pwo→
[InlineData("po2", "p\u02B0wo\u2197")] // pʰwo↗
[InlineData("mo3", "mwo\u2193")]       // mwo↓
[InlineData("fo4", "fwo\u2198")]       // fwo↘
```

> **注**: `Final.O` の標準 prefix は `"wo"` (空 suffix)。カテゴリ 14 の「単独 o」との
> 分岐は `Initial.None` 判定のみ。b/p/m/f が先行する場合は声母 + `wo` で構築される
> ため、標準ルートを通る (特別ケースではない)。
>
> 特に混同しがち: `bo1 → pwo→` であって `po→` でも `pɔ→` でもない。

#### カテゴリ 16: エッジケース

```csharp
[Fact]
public void Convert_Null_ReturnsEmpty()
{
    Assert.Equal(string.Empty, PinyinToMisaki.Convert(null!));
}

[Fact]
public void Convert_Empty_ReturnsEmpty()
{
    Assert.Equal(string.Empty, PinyinToMisaki.Convert(string.Empty));
}

[Fact]
public void Convert_Whitespace_ReturnsEmpty()
{
    Assert.Equal(string.Empty, PinyinToMisaki.Convert("   "));
}

[Theory]
[InlineData("xyz")]
[InlineData("123")]
[InlineData("zzzzz9")]
[InlineData("ma9")]       // 不正声調番号
[InlineData("bba1")]      // 不正声母組合
public void Convert_InvalidPinyin_ReturnsEmpty(string input)
{
    Assert.Equal(string.Empty, PinyinToMisaki.Convert(input));
}
```

#### カテゴリ 17: Issue #56 参照例

Issue #56 (Phase 1 / Inv6) で「Kokoro TTS 82M の中国語 vocab と一致する」ことが
検証された代表 2 例の再現:

```csharp
[Fact]
public void Convert_Ni3_IssueReference()
{
    // 「你」 → ni3 → ni↓
    Assert.Equal("ni\u2193", PinyinToMisaki.Convert("ni3"));
}

[Fact]
public void Convert_Hao3_IssueReference()
{
    // 「好」 → hao3 → xau↓ (h→x, ao→au)
    Assert.Equal("xau\u2193", PinyinToMisaki.Convert("hao3"));
}
```

#### カテゴリ 18: includeTones=false (声調矢印抑制)

```csharp
[Theory]
[InlineData("ma1",  "ma")]
[InlineData("ma4",  "ma")]
[InlineData("man1", "man")]        // ma + (空 tone) + n = man
[InlineData("yi1",  "i")]          // 半母音省略 + 空 tone
[InlineData("zhi1", "\uAB67\u0268")] // ꭧɨ (tone なし)
[InlineData("er1",  "\u025A")]     // ɚ (tone なし)
[InlineData("o1",   "\u0254")]     // ɔ (tone なし)
public void Convert_IncludeTonesFalse(string pinyin, string expected)
{
    Assert.Equal(expected, PinyinToMisaki.Convert(pinyin, includeTones: false));
}
```

### 5.4 テスト総数

| カテゴリ | ケース数 |
|---------|---------|
| 1: 4 声 + 軽声 | 5 |
| 2: 全声母 × Final.A | 11+ |
| 3: 合字検証 | 10 |
| 4: 二重母音 | 8 + U+032F チェック |
| 5: CVC 声調位置 | 12 |
| 6: i 系 j 半母音 | 10 |
| 7: u 系 w 半母音 | 11 |
| 8: ü 系 ɥ 半母音 | 13 |
| 9: Y + X | 14 |
| 10: W + X | 9 |
| 11: そり舌 | 4 |
| 12: 歯茎 | 3 |
| 13: 感嘆詞 Er | 4 |
| 14: 感嘆詞 O | 4 |
| 15: bpmf + o | 4 |
| 16: エッジ | 8 |
| 17: Issue #56 | 2 |
| 18: includeTones=false | 7 |
| **合計** | **約 140 ケース** |

最終的には **120+ の独立テスト** を目標とする (Theory の InlineData 単位で数える)。

### 5.5 テストファイル雛形

```csharp
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Misaki 互換 <see cref="PinyinToMisaki.Convert"/> の単体テスト。
    /// 全期待値は .claude/tmp/misaki-gold.txt に基づき、
    /// misaki==0.9.4 を uv run で直接実行した実測値 (137 件) と照合済み。
    /// </summary>
    public class PinyinToMisakiConvertTests
    {
        // カテゴリ 1: 4 声 + 軽声 × ma 系
        [Theory]
        [InlineData("ma1", "ma\u2192")]
        [InlineData("ma2", "ma\u2197")]
        [InlineData("ma3", "ma\u2193")]
        [InlineData("ma4", "ma\u2198")]
        [InlineData("ma5", "ma")]
        public void Convert_Ma_AllTones(string pinyin, string expected)
            => Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));

        // ... 他のカテゴリは同様に [Theory] + [InlineData] で列挙
    }
}
```

## 6. 実装懸念・レビュー項目

### 6.1 T01 テーブル構造の前提確認

T02 実装前に **必ず T01 の以下を確認** すること:

- [ ] `s_finalMisaki` が `Dictionary<Final, (string Prefix, string Suffix)>` (タプル) であること
- [ ] `s_yWCompoundMisaki` が `Dictionary<(Initial, Final), (string Prefix, string Suffix, bool OmitInitial)>` であること
- [ ] `s_toneArrows` が `string[]` で 5 要素 `["", "→", "↗", "↓", "↘"]` であること
- [ ] `s_initialMisaki[Initial.Zh]` = `"\uAB67"` (U+AB67、ꭧ) であること (旧 `ʈʂ` ではない)
- [ ] `s_initialMisaki[Initial.J]` = `"\u02A8"` (U+02A8、ʨ) であること
- [ ] `s_initialMisaki[Initial.Z]` = `"\u02A6"` (U+02A6、ʦ) であること
- [ ] テンプレ側で U+032F が事前除去されていること (`s_finalMisaki[Final.Ai] = ("ai", "")` の
  prefix に U+032F を含まない)
- [ ] `s_finalMisaki[Final.Ong] = ("\u028A", "\u014B")` (U+028A `ʊ` + U+014B `ŋ`) であること
  (旧 `u̯ŋ` ではない)
- [ ] `s_finalMisaki[Final.Ve] = ("\u0265e", "")` (ɥe) であること (旧 `ye` ではない)

整合していない場合、T02 の実装より先に T01 の rewrite を完了させる必要がある。

### 6.2 PinyinParser の挙動確認

`PinyinToMisaki.Convert` は `PinyinParser.TryParse` に完全依存する。以下を動作確認すること:

- [ ] `"ma1"` → `PinyinSyllable { Initial.M, Final.A, Tone.First }`
- [ ] `"ma5"` → `PinyinSyllable { Initial.M, Final.A, Tone.Neutral }`
- [ ] `"lv1"` → `PinyinSyllable { Initial.L, Final.V, Tone.First }` (lv → lü 正規化)
- [ ] `"jue1"` → `PinyinSyllable { Initial.J, Final.Ve, Tone.First }` (ju → jü 正規化 + e)
- [ ] `"wang1"` → `PinyinSyllable { Initial.W, Final.Ang, Tone.First }` (Misaki uang とは異なる内部表現)
- [ ] `"yong1"` → `PinyinSyllable { Initial.Y, Final.Ong, Tone.First }`
- [ ] `"er1"` → `PinyinSyllable { Initial.None, Final.Er, Tone.First }`
- [ ] `"o1"` → `PinyinSyllable { Initial.None, Final.O, Tone.First }`

### 6.3 U+032F 残留チェック

**絶対条件**: `PinyinToMisaki.Convert` の出力は **U+032F を含まない**。

テストで明示的にアサートする (カテゴリ 4 に含める):

```csharp
[Fact]
public void Convert_NeverContainsU032F()
{
    var pinyins = new[]
    {
        "bai1", "mao2", "dou3", "mei4", "guei1", "jiao1", "xiao2", "liu4",
        "liao3", "miu1", "niao2", "dui4", "sui1", "tui3",
    };
    foreach (var p in pinyins)
    {
        var result = PinyinToMisaki.Convert(p);
        Assert.DoesNotContain("\u032F", result);
        Assert.NotEmpty(result);  // 変換自体は成功しているべき
    }
}
```

### 6.4 特別ケースの優先度

ConvertSyllable のステップ 2 (特別ケース判定) は必ず **以下の順序** で評価すること:

1. `Initial.None + Final.O` (感嘆詞 ɔ)
2. `Final.Er` (儿化/感嘆詞 ɚ)
3. `(Zh|Ch|Sh|R) + Final.I` (retroflex ɨ)
4. `(Z|C|S) + Final.I` (alveolar ɨ)
5. 標準 Y/W 複合 final ルックアップ → 標準 final ルックアップ

相互排他なので順序が実挙動に影響することは少ないが、読み取り時の混乱を避けるため固定する。

### 6.5 Dictionary TryGetValue の使用

既存 `PinyinToIpa` は `s_initialIpa[key]` でインデクサを使っているが、T02 では
**全ての lookup で `TryGetValue` を使用** すること:

```csharp
// OK (T02)
if (s_initialMisaki.TryGetValue(syllable.Initial, out var init))
    sb.Append(init);

// NG (旧 PinyinToIpa 様式、KeyNotFoundException のリスク)
sb.Append(s_initialMisaki[syllable.Initial]);
```

理由: `s_yWCompoundMisaki` と `s_finalMisaki` の両方を参照するため、片方に漏れがあった場合
`KeyNotFoundException` がスタックの奥で発生し、原因特定が困難になる。
`TryGetValue` で明示的な empty 返却にすることで、テストで「未定義の組み合わせ」として
検出できる。

### 6.6 PinyinToIpa / PinyinToPiperIpa との構造差

**PinyinToMisaki は既存 2 クラスと構造を揃えない**。理由:

- 既存 2 クラスは「末尾 tone letters」方式で線形 append
- PinyinToMisaki は「prefix → tone → suffix」で中置構造

ファサード化 (共通エンジン + マッピングテーブル) は Mi1 完了後のリファクタチケット
(別起票) で対応する。T02 のスコープでは **PinyinToMisaki 単独で完結する** 実装とし、
既存 2 クラスには触れない。

### 6.7 InternalsVisibleTo

`ConvertSyllable` は internal static のため、テストアセンブリから直接呼び出すには
`[assembly: InternalsVisibleTo("DotNetG2P.Tests")]` が必要。

T02 実装前チェック:

- [ ] `src/DotNetG2P.Chinese/DotNetG2P.Chinese.csproj` または `Properties/AssemblyInfo.cs` に
  `InternalsVisibleTo` が設定されていること
- [ ] 設定されていなければ、まず追加してから T02 実装に入ること

(大半のケースでは `Convert(string)` public static を介すれば事足りるため、
`ConvertSyllable` を直接テストするのは 1-2 ケース程度に留める想定)

## 7. 一から作り直すとしたら

旧 T02 のセクション 6「一から作り直すとしたら」は「4 クラス目のコピペをどう抽象化するか」
という軸で設計案 A-G を提示していたが、**Phase 1-R の新仕様を受けて設計軸そのものが変わる**。
以下、旧版の教訓を踏まえつつ Phase 1-R 視点で改訂する。

### 7.1 Phase 1-R が明らかにした教訓

旧 T02 を書いた時点では以下の前提が暗黙に仮定されていた:

1. 「Misaki は IPA の変種なので、PinyinToIpa の構造を流用できる」
2. 「声調は末尾付与が自然」
3. 「Y/W の半母音省略は声母マップを空文字化すれば済む」
4. 「U+032F は IPA 精密表記なので含めるべき」

これらは **いずれも誤り** であることが `uv run misaki==0.9.4` の実測で判明した。

| 誤った前提 | Phase 1-R での実測 |
|----------|----------------|
| IPA の変種 | Kokoro TTS 専用の独自音素体系 (U+AB67 ꭧ、U+02A8 ʨ、U+02A6 ʦ、U+0254 ɔ 等、vocab は Kokoro 82M に縛られる) |
| 末尾 tone | 韻母の核母音の後、後続子音の前に挿入 (`ma→n`) |
| 声母マップ空文字化 | 複合 final テーブル (24 エントリ) による lookup が必須 |
| U+032F 含める | misaki 出力は U+032F を含まない (`bai` であって `ba u̯ i` でも `bai̯` でもない) |

教訓: **「既存の類似クラスの構造を流用する」前に、必ずソース実装を実測確認すること**。
misaki のようにドキュメント化が限定的なライブラリでは、Python 実装を `uv run` で
直接実行してサンプル入出力を取得するのが最も確実である。

### 7.2 今後の設計判断に反映すべきこと

Phase 1-R で確立したパターンを Mi1 の後続チケット (T03-T06) に引き継ぐ際の指針:

1. **仕様確認は「ドキュメント」ではなく「実装実行」で行う**
   - 参照: `.claude/tmp/misaki-gold.txt` (137 件、`uv run` で生成済み)
   - 新しい出力形式を追加する場合は、必ず同様の gold standard を生成してから仕様書を書く

2. **マッピングテーブルの構造を「単一 string」から「タプル (prefix, suffix)」に抽象化**
   - 将来 IPA 側も声調中置が必要になった場合、同じ構造を使える
   - T01 で `s_finalMisaki` が `Dictionary<Final, (string Prefix, string Suffix)>` になっているため、
     `PinyinToIpa` 側の rewrite 時に同じ形式を採用できる

3. **Y/W 処理を「ヘルパー関数」から「テーブル lookup」に移行**
   - `ShouldOmitSemivowel` のような分岐ヘルパーは「例外の例外」を生みやすい
   - 複合 final テーブルは **データとして全ケースが列挙可能** で、仕様書の表と 1 対 1 対応

4. **既存 2 クラス (`PinyinToIpa`/`PinyinToPiperIpa`) に触らない**
   - T02 のスコープでは「Misaki 単独で完結」とする
   - テスト 936 件を壊さないために、ファサード化リファクタは Mi1 完了後に別チケット化

### 7.3 将来のリファクタ候補 (Mi1 完了後)

以下は Mi1 完了後に別チケット化することを推奨:

- **新チケット: 「PinyinConverter 共通抽象化リファクタ」**
  - `PinyinMappingTable` record の導入 (旧 T02 方式 3 をベース)
  - `PinyinConversionEngine.Convert(syllable, table, includeTones)` の抽出
  - `PinyinToIpa`/`PinyinToPiperIpa`/`PinyinToMisaki` をファサードに変更
  - ただし prefix/suffix 構造と末尾 tone 構造の両方をサポートする必要があるため、
    `PinyinMappingTable` は `bool TonePosition: Suffix/Middle` のようなフラグを持つ設計になる

- **新チケット: 「テストケース自動生成」**
  - `.claude/tmp/misaki-gold.txt` を入力として xUnit `[InlineData]` を自動生成するスクリプト
  - `tools/DotNetG2P.MisakiGoldToTests/` ディレクトリに配置
  - misaki の新バージョン (0.10.x 等) が出た際に再生成できるようにする

- **新チケット: 「IPA 音素体系の vocab 互換性検証」**
  - Kokoro 82M の Chinese vocab ファイル (`kokoro-chinese.json` 等) と
    `s_initialMisaki`/`s_finalMisaki`/`s_yWCompoundMisaki` の全出力候補を
    突合するユニットテスト
  - 1 文字でも vocab 外に漏れていれば即時失敗させる

## 8. 後続タスクへの引継ぎ (T03: ChineseG2PEngine への ToMisaki API 追加)

T03 担当エージェントに伝えるべき情報:

### 8.1 利用可能なメソッド

- `PinyinToMisaki.Convert(string pinyin)` — 声調矢印あり、デフォルト
- `PinyinToMisaki.Convert(string pinyin, bool includeTones)` — 声調矢印制御可
- `PinyinToMisaki.ConvertSyllable(PinyinSyllable, bool)` — パース済み音節を直接渡す用

### 8.2 ChineseG2PEngine からの使用例

```csharp
// T03 での実装イメージ
public string ToMisaki(string text) => ToMisaki(text, includeTones: true);

public string ToMisaki(string text, bool includeTones)
{
    return RunPipeline(text, p => PinyinToMisaki.Convert(p, includeTones));
}

public string[] ToMisakiBatch(string[] texts, bool includeTones = true)
    => BatchConversionHelper.Convert(texts, t => ToMisaki(t, includeTones));
```

既存の `ToIpa` / `ToPiperIpa` と同一パターンでよい。

### 8.3 バッチ API について

`ConvertBatch` に相当するメソッドは T02 のスコープ外。ChineseG2PEngine 側の
`BatchConversionHelper` を使って T03 内で実装する想定。

### 8.4 ConvertToPhonemes 未実装

`PinyinToPiperIpa` にある `ConvertToPhonemes()` (声母と韻母を分離した配列を返す) に
相当するものは T02 のスコープ外。Misaki で音素単位処理や Prosody 処理が必要な場合は
別タスクを起票すること (現状、Mi1 の範囲では不要と判断)。

### 8.5 `includeTones=false` 時の出力特性

- 軽声 (`ma5`) と `includeTones=false` 指定時 (`ma1` with `includeTones=false`) の両方で
  `"ma"` が返る (区別不可)
- CVC の場合 (`man1` with `includeTones=false`) は `"man"` が返る (prefix + "" + suffix)
- 感嘆詞 (`o1` with `includeTones=false`) は `"ɔ"` が返る
- 特別ケース判定は `includeTones` と独立して動作するため、矢印のみ抑制される

T03 の ChineseG2PEngine API では、`ToMisaki(text, includeTones)` のデフォルトを
`true` にすること (既存 `ToIpa` と整合)。

### 8.6 期待値マスターデータ

`.claude/tmp/misaki-gold.txt` は T02 テストと T03/T04 統合テストの両方で参照する。
misaki の新バージョンが出た場合は `tools/refresh_misaki_gold.ps1` (仮) を作成して
再生成する運用とするのが望ましい (Mi1 完了後の課題)。

## 紐づけ

- **マイルストーン**: Mi1 (Misaki 互換中国語出力)
- **依存**: T01 (マッピングテーブル定義、Phase 1-R 版 rewrite 済みであること)
- **後続**: T03 (ChineseG2PEngine API 追加)
- **参照仕様**: `.claude/tmp/misaki-spec.md` (verified against misaki 0.9.4 via uv run)
- **参照 gold**: `.claude/tmp/misaki-gold.txt` (137 件実測値)
- **関連 Issue**: #56 (Phase 1 Mi1 準備、Kokoro vocab 互換性検証)
