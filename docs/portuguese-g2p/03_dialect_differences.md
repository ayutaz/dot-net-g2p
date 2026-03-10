# ポルトガル語 方言差調査: ブラジル(BP) vs ヨーロッパ(EP)

## 概要

ポルトガル語のG2P実装において、ブラジルポルトガル語(BP)とヨーロッパポルトガル語(EP)の間には
音韻体系に多数の体系的差異がある。これらの差異は G2P ルールの分岐点として設計に直接影響する。

---

## 1. 母音体系の違い

### 1.1 ストレス母音

BP/EP ともにストレス位置では以下の7口母音を基本体系として区別する:

| 音素 | 例語 | IPA |
|------|------|-----|
| /a/ | p**a**to | [ˈpatu] |
| /e/ | s**e**co | [ˈseku] |
| /ɛ/ | s**e**co (開) | [ˈsɛku] |
| /o/ | f**o**go | [ˈfogu] |
| /ɔ/ | m**ó**vel | [ˈmɔvɛl] |
| /i/ | v**i**da | [ˈvidɐ] |
| /u/ | l**u**z | [ˈluʃ] |

**EP における /ɐ/ の音素地位:**

EP ではストレス位置で /a/ と /ɐ/ が対立する場合があるが、その出現環境は限定的で、以下の3文脈に制限される（Cruz-Ferreira 1999）:
1. 鼻子音の前（例: c**a**ma [ˈkɐmɐ]）
2. 硬口蓋子音の前
3. 前舌わたり音 [j] の前

BP では /ɐ/ は /a/ と相補分布に近く、独立音素として認めない分析が一般的である（BP の口母音体系は /a, ɛ, e, i, ɔ, o, u/ の7音素）。G2P実装では EP モードのみ /ɐ/ を限定的な環境で区別し、BP モードでは /a/ のアロフォンとして扱う。

### 1.2 無ストレス母音弱化（最大の方言差）

**EP（激しい弱化 -- stress-timed リズム）:**

| ストレス音素 | 無ストレス実現 | 例 |
|-------------|--------------|-----|
| /a/, /ɐ/ | [ɐ] (中央化) | p**a**gar → [pɐˈɡaɾ] |
| /e/, /ɛ/ | [ɨ] (高中央母音、しばしば脱落) | p**e**dir → [pɨˈðiɾ] |
| /o/, /ɔ/ | [u] (上昇) | c**o**mer → [kuˈmeɾ] |
| 語末 /e/ | [ɨ] → しばしば無音化 | part**e** → [ˈpaɾtɨ] ~ [ˈpaɾt] |
| 語末 /o/ | [u] | tud**o** → [ˈtudu] |
| 語末 /a/ | [ɐ] | cas**a** → [ˈkazɐ] |

- EP の [ɨ] は非常に弱く、高速発話ではしばしば完全に脱落する
- この母音脱落が EP の stress-timed リズムの主要因

**BP（穏やかな弱化 -- syllable-timed リズム）:**

| ストレス音素 | 前ストレス位置 | 語末位置 | 例 |
|-------------|-------------|---------|-----|
| /a/, /ɐ/ | [a] (保持) | [ɐ] | cas**a** → [ˈkazɐ] |
| /e/, /ɛ/ | [e] ~ [i] (方言差) | [i] | part**e** → [ˈpaɾtʃi] |
| /o/, /ɔ/ | [o] ~ [u] (方言差) | [u] | tud**o** → [ˈtudu] |

- BP は母音をより明瞭に保持し、syllable-timed なリズムを維持
- 語末 /e/ → [i] は BP 全域でほぼ共通（EP の [ɨ] と対照的）

**BP前ストレス位置の地域差:**

前ストレス母音弱化は BP 内でも地域によって大きく異なる:

- **南部・中西部BP**: 前ストレス /e/→[i], /o/→[u] への上昇（raising）が広く見られる（例: menino [miˈninu]）
- **北東部BP**: 前ストレス /e/→[ɛ], /o/→[ɔ] と逆に開口化する方言がある
- **リオ/サンパウロ標準**: [e]/[o] を維持する傾向

G2P実装のデフォルト（BP標準）はサンパウロ/ブラジリア標準に合わせ、前ストレス位置では [e]/[o] を維持する方針とする。

### 1.3 G2P実装への影響

```
// 無ストレス母音弱化の方言分岐（擬似コード）
if (dialect == EP) {
    // /e/,/ɛ/ → [ɨ], /o/,/ɔ/ → [u], /a/ → [ɐ]
    // 語末 [ɨ] の任意脱落はモデリング対象外（標準発音を出力）
} else { // BP
    // 前ストレス: 方言差あるが標準的に /e/→[e], /o/→[o]
    // 語末: /e/→[i], /o/→[u], /a/→[ɐ]
}
```

---

## 2. 子音の違い

### 2.1 /t/ と /d/ の口蓋化（BP特有）

BP では /t/ と /d/ が前舌母音 /i/ の前で破擦音化する:

| 環境 | EP | BP | 例 |
|------|-----|-----|-----|
| /ti/ | [ti] | [tʃi] | **ti**po → EP [ˈtipu], BP [ˈtʃipu] |
| /di/ | [di] | [dʒi] | **di**a → EP [ˈdiɐ], BP [ˈdʒiɐ] |
| /tĩ/ | [tĩ] | [tʃĩ] | **tin**ta → EP [ˈtĩtɐ], BP [ˈtʃĩtɐ] |
| /dĩ/ | [dĩ] | [dʒĩ] | **din**heiro → EP [diˈɲɐjɾu], BP [dʒĩˈɲejɾu] |
| /te/ 語末 | [tɨ] | [tʃi] | not**e** → EP [ˈnɔtɨ], BP [ˈnɔtʃi] |
| /de/ 語末 | [dɨ] | [dʒi] | cidad**e** → EP [siˈðaðɨ], BP [siˈdadʒi] |

- BP ではほぼ全方言で生産的（一部南部方言を除く）
- EP では発生しない
- 語末 /e/ → [i]（BP）が先行し、結果として /t/+[i] → [tʃi] が連鎖適用

**G2P実装:**
- BP モードでは /t,d/ + /i/ または /ĩ/（および無ストレス /e/ → [i]）の環境で破擦音化ルールを適用
- EP モードでは適用しない

### 2.2 語末・音節末 /s/ の発音差

| 環境 | EP | BP（多数派） | BP（Rio/一部） |
|------|-----|------------|--------------|
| 語末（休止前） | [ʃ] | [s] | [ʃ] |
| + 無声子音前 | [ʃ] | [s] | [ʃ] |
| + 有声子音前 | [ʒ] | [z] | [ʒ] |

- EP: 音節末 /s/ は一律にポスト歯茎音 [ʃ]/[ʒ]（有声性同化あり）
- BP 多数方言: 歯茎音 [s]/[z] を保持
- BP Rio de Janeiro/一部北東部: EP と同じ [ʃ]/[ʒ]（カリオカ方言）

**G2P実装の推奨:**
- EP: 音節末 /s/ → [ʃ] (無声環境) / [ʒ] (有声環境)
- BP デフォルト: 音節末 /s/ → [s] (無声環境) / [z] (有声環境)
- （Rio 方言は将来拡張として別に対応可能）

### 2.3 /r/（流音R）の発音差

ポルトガル語のR音は最も方言差が大きい子音の一つ:

| 環境 | EP | BP（標準的） |
|------|-----|------------|
| 語頭 /r-/ | [ʁ] (口蓋垂摩擦音) | [h] ~ [x] (声門/軟口蓋摩擦音) |
| 二重 /rr/ | [ʁ] | [h] ~ [x] |
| 介母音 /-r-/ | [ɾ] (歯茎はじき音) | [ɾ] (歯茎はじき音) |
| 音節末 /-r/ | [ɾ] | [ɾ] ~ [ɹ] ~ [h] (方言差大) |

- 「強いR」（語頭、rr）: EP [ʁ], BP [h]/[x]（地域差大、歴史的に [r]→[x]→[h] の変化）
- 「弱いR」（介母音）: BP/EP 共通で [ɾ]
- 音節末R: EP [ɾ], BP は地域差が非常に大きい（サンパウロ内陸部では英語風 [ɹ] も）

**BP語末 /r/ の脱落傾向:**

BP 全域で語末 /r/ の脱落（deletion）が極めて広範に見られる。特に不定詞語尾 -ar, -er, -ir で顕著であり、日常会話では脱落率が非常に高い:
- falar → [faˈla], comer → [koˈme], partir → [paɾˈtʃi]

G2P実装としては標準発音（/r/ を出力）を基本とするが、この脱落傾向は将来的なオプション対応の候補として留意する。

**G2P実装の推奨:**
- 2種の R 音素を区別: /ʁ/（強いR）と /ɾ/（弱いR）
- EP: /ʁ/ → [ʁ], /ɾ/ → [ɾ]
- BP: /ʁ/ → [h], /ɾ/ → [ɾ]（音節末の方言変異は標準的な [ɾ] で統一）

### 2.4 語末・音節末 /l/ の扱い

| 環境 | EP | BP |
|------|-----|-----|
| 語頭・介母音 /l/ | [l] (歯茎側面音) | [l] (歯茎側面音) |
| 音節末 /l/ | [ɫ] (暗いL、軟口蓋化側面音) | [w] (半母音化) |

- EP: coda /l/ は暗い L [ɫ]（英語の dark L に類似）を維持
- BP: coda /l/ は完全に半母音 [w] に変化（l-vocalization）
  - 例: Brasil → EP [bɾɐˈziɫ], BP [bɾaˈziw]
  - 例: sal → EP [saɫ], BP [saw]
  - 例: alto → EP [ˈaɫtu], BP [ˈawtu]
- BP の l-vocalization は都市部を中心にほぼ全国に普及
- 一部南部国境地帯のみ [ɫ] または [l] を維持

**G2P実装:**
- EP: 音節末 /l/ → [ɫ]
- BP: 音節末 /l/ → [w]
- 音節初頭の /l/ は共通で [l]

### 2.5 /ɲ/ の実現

| 方言 | 実現 | 例 |
|------|------|-----|
| EP | [ɲ] (硬口蓋鼻音) | ninho → [ˈniɲu] |
| BP | [j̃] (鼻音化半母音) + 先行母音鼻音化 | ninho → [ˈnĩj̃u] |

- EP では完全な子音 [ɲ] を維持
- BP (およびアンゴラ) では半母音的実現 [j̃] となり、先行母音を鼻音化

### 2.6 閉鎖音弱化（lenition）

EP（特に北部・中部方言）では、有声閉鎖音 /b, d, ɡ/ が母音間・流音後などの環境で摩擦音化（spirantization）する。これはスペイン語と同様の弱化パターンである:

| 音素 | EP（弱化環境） | BP |
|------|--------------|-----|
| /b/ | [β] (有声両唇摩擦音) | [b] (閉鎖音維持) |
| /d/ | [ð] (有声歯摩擦音) | [d] (閉鎖音維持) |
| /ɡ/ | [ɣ] (有声軟口蓋摩擦音) | [ɡ] (閉鎖音維持) |

- EP では母音間・流音（/ɾ/, /l/）後の環境で弱化が生産的に発生する
- 弱化の程度は北部で最も顕著、南部では程度差がある
- BP では閉鎖音弱化はほとんど発生しない

**G2P実装:**
- EP モードでは AllophoneProcessor で母音間・流音後の /b,d,ɡ/ → [β,ð,ɣ] 弱化ルールを適用
- BP モードでは適用しない

---

## 3. その他の体系的差異

### 3.1 母音挿入（epenthesis）

**BP:**
- 子音連続の間に挿入母音 [i] を挿入する傾向が強い
- 特に語頭 sC-クラスタ: sp-, st-, sk- の前に [i] が挿入されやすい
  - 例: esporte [isˈpɔɾtʃi], psicologia [pisikoloˈʒiɐ]
- 語末の非許容子音の後にも [i] を挿入
  - 例: McDonald's → [mɛkiˈdõnawdʒis]
- 借用語や学術語で特に顕著

**EP:**
- 母音挿入の傾向は弱い
- 代わりに子音連続を維持するか、母音脱落（[ɨ] の消失）で CCC クラスタも許容
- 北部方言では [ɨ] の挿入が見られるが、南部では稀

### 3.2 わたり音挿入（glide insertion）

**EP:**
- 北部・中部を中心に、母音連続（hiatus）の回避のためにわたり音 [j] や [w] が挿入される現象がある
- 語境界や形態素境界で発生しやすい
- G2P としては語レベル処理では影響が小さいが、連続発話の合成では考慮が必要になる可能性がある

**BP:**
- 母音連続の回避にはわたり音挿入よりも母音融合（crasis）やリエゾンを用いる傾向がある

G2P実装では当面モデリング対象外とするが、将来的な連続発話対応の際に検討する。

### 3.3 リズム特性

| 特性 | EP | BP |
|------|-----|-----|
| リズム類型 | stress-timed（強弱拍リズム） | syllable-timed（音節拍リズム） |
| 母音持続時間 | 不均等（ストレスで長い） | 比較的均等 |
| 母音脱落 | 頻繁（特に [ɨ]） | 稀 |
| 音節明瞭度 | 低い（弱化で不明瞭） | 高い（母音が明瞭） |

- EP は英語やロシア語に近いリズム、BP はスペイン語やイタリア語に近いリズム
- G2P では直接モデリングしないが、EP の母音脱落ルールに間接的に影響

### 3.4 鼻母音の方言差

鼻母音・鼻二重母音は BP/EP 共通の音素体系を持つが、実現に微細な差がある:

| 特徴 | EP | BP |
|------|-----|-----|
| 鼻母音 /ɐ̃/ | [ɐ̃] | [ɐ̃] ~ [ã]（やや開口） |
| 鼻二重母音 /ɐ̃w̃/ | [ɐ̃w̃] | [ɐ̃w̃] ~ [ãw̃] |
| 無ストレス鼻母音 | やや弱い鼻音化 | 比較的一定の鼻音化 |

- 基本的な音韻対立は共通。G2P レベルでの方言分岐は不要

---

## 4. 方言差の実装影響度まとめ

G2P ルールへの影響度で分類:

### 高影響（必ず方言分岐が必要）

| 差異 | EP ルール | BP ルール |
|------|----------|----------|
| 無ストレス /e/,/ɛ/ | → [ɨ] | → [i] (語末), [e] (前ストレス) |
| 無ストレス /o/,/ɔ/ | → [u] | → [u] (語末), [o] (前ストレス) |
| /t/ + /i,ĩ/ | [ti] | [tʃi] |
| /d/ + /i,ĩ/ | [di] | [dʒi] |
| 音節末 /l/ | [ɫ] | [w] |
| 音節末 /s/ | [ʃ]/[ʒ] | [s]/[z] |
| 強いR | [ʁ] | [h] |
| 閉鎖音弱化 | [β,ð,ɣ] (母音間) | 弱化なし |

### 中影響（AllophoneProcessor レベル）

| 差異 | EP ルール | BP ルール |
|------|----------|----------|
| /ɲ/ 実現 | [ɲ] | [j̃] + 先行母音鼻音化 |
| 音節末R | [ɾ] | [ɾ]（BP内地域差大、標準は [ɾ]） |
| 母音挿入 | 挿入なし | [i] 挿入（借用語限定） |
| BP語末 /r/ 脱落 | 脱落なし | 高頻度で脱落（標準発音では出力） |

### 低影響（モデリング不要、または将来拡張）

| 差異 | 備考 |
|------|------|
| リズム差 | G2P 出力では表現しない |
| 鼻母音の微細差 | 音素レベルでは同一 |
| EP [ɨ] の脱落（syncope） | 母音弱化（/e/→[ɨ]）自体は高影響で必ず実装する。その先の任意脱落（syncope）のみ低影響としてモデリング対象外とし、標準発音（[ɨ] を出力）を採用する |
| わたり音挿入 | EP北部の母音連続回避、語レベルでは影響小 |

---

## 5. Dialect enum の設計提案

### 基本設計

```csharp
namespace DotNetG2P.Portuguese
{
    /// <summary>ポルトガル語の方言モード。</summary>
    public enum PortugueseDialect : byte
    {
        /// <summary>
        /// ブラジル標準（口蓋化、l半母音化、歯茎摩擦音coda、穏やかな母音弱化）。
        /// サンパウロ/ブラジリア系の「標準ブラジル語」を基準とする。
        /// </summary>
        Brazilian = 0,

        /// <summary>
        /// ヨーロッパ標準（リスボン基準、[ɨ]弱化、暗いL、ポスト歯茎coda s）。
        /// </summary>
        European = 1,
    }
}
```

### 設計根拠

1. **byte 基底型**: 既存の `SpanishDialect : byte`, `FrenchDialect : byte` と統一
2. **デフォルト値 = Brazilian (0)**: 話者人口が圧倒的に多い（約2.15億人 vs 約1,050万人）、TTS用途での需要が高い
3. **2値 enum**: スペイン語（LatinAmerican/Castilian）、フランス語（Metropolitan/Conservative）と同じ2値パターン
4. **XML doc コメント**: 各方言の主要特徴を要約

### 将来拡張の可能性

```csharp
// 将来的な拡張案（現時点では実装しない）
public enum PortugueseDialect : byte
{
    Brazilian = 0,
    European = 1,
    // African = 2,       // アンゴラ/モザンビーク共通基盤（EPベース + BP的母音弱化）
    // Carioca = 3,       // Rio de Janeiro方言（BP + coda [ʃ]/[ʒ]）
}
```

**アフリカ系ポルトガル語について:**
- アンゴラ語・モザンビーク語は EP を書記基準とするが、音韻的には BP に近い特徴を持つ（母音弱化が穏やか）
- 現状は BP/EP の2方言で十分カバー可能
- 将来的に需要があれば `African` を追加し、EP ベースに BP 的母音弱化を適用する形で実装可能

### Options クラスでの使用パターン

```csharp
/// <summary>ポルトガル語G2Pの設定オプション。</summary>
public sealed class PortugueseG2POptions
{
    /// <summary>方言モード。既定値は <see cref="PortugueseDialect.Brazilian"/>。</summary>
    public PortugueseDialect Dialect { get; set; } = PortugueseDialect.Brazilian;

    /// <summary>ストレス記号を出力に含めるか。</summary>
    public bool IncludeStress { get; set; } = true;

    /// <summary>異音規則を適用するか。</summary>
    public bool EnableAllophones { get; set; }

    /// <summary>IPA出力の区切り文字。既定値はドット "."。</summary>
    public string Separator { get; set; } = ".";
}
```

---

## 6. 方言別G2Pルール分岐の設計指針

### ルール適用フロー

```
入力テキスト
  ↓
[Normalize]         ← 共通（正書法は統一）
  ↓
[Tokenize]          ← 共通
  ↓
[GraphemeToPhonemeRules] ← 基本共通、一部方言分岐
  │ ├─ 強いR: EP → /ʁ/, BP → /h/       ★方言分岐
  │ └─ 基本子音・母音: 共通
  ↓
[SyllableParser]    ← 共通
  ↓
[StressAssigner]    ← 共通
  ↓
[AllophoneProcessor] ← ★方言分岐ポイント
  │ ├─ 母音弱化（必須規則）:
  │ │   ├─ EP: /e/→[ɨ], /o/→[u], /a/→[ɐ]
  │ │   └─ BP: 語末 /e/→[i], /o/→[u]; 前ストレス保持
  │ ├─ BP: t/d口蓋化、l半母音化、歯茎coda s
  │ └─ EP: 暗いL、ポスト歯茎coda s、閉鎖音弱化
  ↓
[IPA出力]
```

### 共通処理 vs 方言固有処理

| 処理段階 | 共通/方言固有 | 備考 |
|---------|-------------|------|
| テキスト正規化 | ほぼ共通 | 1990年正書法協定で統一方向だが部分的。BP では `ü`（トレマ）が廃止済み（旧正書法テキストの互換性対応が必要）。一部の黙字処理（例: EP `facto` vs BP `fato`）は協定後も残存し、正規化段階で方言分岐が必要な場合がある |
| 基本G2Pルール | ほぼ共通 | 強いRの音素値のみ分岐 |
| 音節分割 | 共通 | 音節構造の規則は同一 |
| ストレス付与 | 共通 | ストレス規則は同一 |
| 異音規則（母音弱化含む） | 方言固有 | 母音弱化（最大の分岐）、口蓋化、l処理、coda s、閉鎖音弱化を AllophoneProcessor 内で統一処理 |
| IPA変換 | 共通 | 音素→IPA文字列は共通 |

---

## 参考文献・情報源

- [Portuguese phonology - Wikipedia](https://en.wikipedia.org/wiki/Portuguese_phonology)
- [Help:IPA/Portuguese - Wikipedia](https://en.wikipedia.org/wiki/Help:IPA/Portuguese)
- [Phonological Processes Affecting Vowels - Penn Linguistics](https://www.ling.upenn.edu/courses/Spring_2021/ling521/HandbookPortugueseVowels.pdf)
- [Phonetic and phonological vowel reduction in Brazilian Portuguese - De Gruyter](https://www.degruyterbrill.com/document/doi/10.1515/phon-2021-2012/html)
- [Lateral vocalization in Brazilian Portuguese - JASA](https://pubs.aip.org/asa/jasa/article/152/1/281/2838337/Lateral-vocalization-in-Brazilian-Portuguese)
- [Rhotic Variation in Brazilian Portuguese - MDPI](https://www.mdpi.com/2226-471X/9/12/364)
- [European Portuguese as a stress-timed language](https://mastereuropeanportuguese.com/european-portuguese-stress-timed-language)
- [Angolan Portuguese - Wikipedia](https://en.wikipedia.org/wiki/Angolan_Portuguese)
- [Appendix:Portuguese pronunciation - Wiktionary](https://en.wiktionary.org/wiki/Appendix:Portuguese_pronunciation)

