# 06. ポルトガル語の鼻母音化と特殊音韻プロセス

## 1. 鼻母音の生成条件

### 1.1 鼻母音インベントリ

ポルトガル語には5つの鼻母音が存在する:

| 鼻母音 | IPA | 正書法での表記 | 例 |
|--------|-----|---------------|-----|
| 鼻 a | [ɐ̃] | ã, an, am | manhã [mɐˈɲɐ̃], campo [ˈkɐ̃pu] |
| 鼻 e | [ẽ] | en, em | vento [ˈvẽtu], tempo [ˈtẽpu] |
| 鼻 i | [ĩ] | in, im | cinco [ˈsĩku], limpo [ˈlĩpu] |
| 鼻 o | [õ] | on, om, õ | conta [ˈkõtɐ], onda [ˈõdɐ] |
| 鼻 u | [ũ] | un, um | mundo [ˈmũdu], um [ˈũ] |

### 1.2 鼻母音化の条件（G2Pルール観点）

鼻母音化は以下の正書法パターンで発生する:

#### パターン A: チルダ付き母音（明示的鼻母音マーカー）
- `ã` → [ɐ̃]: irmã [iɾˈmɐ̃], lã [ˈlɐ̃]
- `õ` → [õ]: põe [ˈpõj̃], aviões [ɐviˈõj̃ʃ]
- チルダは常に鼻母音化を示す。G2Pでは最優先で処理。

#### パターン B: 母音 + m/n（音節末位置）
母音の後ろに m または n が来て、その m/n が音節末（コーダ位置）にある場合、母音が鼻母音化し、m/n 自体は発音されない:

- am/an + 子音: campo [ˈkɐ̃pu], canto [ˈkɐ̃tu]
- em/en + 子音: tempo [ˈtẽpu], vento [ˈvẽtu]
- im/in + 子音: limpo [ˈlĩpu], cinco [ˈsĩku]
- om/on + 子音: compra [ˈkõpɾɐ], conta [ˈkõtɐ]
- um/un + 子音: mundo [ˈmũdu], junto [ˈʒũtu]

**鼻母音化しない条件**（フランス語と類似）:
- 母音間の n/m: cama [ˈkɐmɐ], cana [ˈkɐnɐ]（鼻子音として発音）
- nn, mm（ポルトガル語では稀だが外来語で出現）

**m vs n の使い分け規則**（正書法）:
- m は語末、および p/b の前で使用
- n はそれ以外の位置で使用
- G2Pでは m/n の区別は鼻母音化の判定に影響しない（どちらも同じ鼻母音を生成）

#### パターン C: 語末の -am, -em（鼻二重母音化）
語末の -am と -em は単純な鼻母音ではなく、鼻二重母音になる（後述セクション2参照）:
- -am（無アクセント）→ [ɐ̃w̃]: falam [ˈfalɐ̃w̃]
- -ão（アクセントあり）→ [ɐ̃w̃]: não [ˈnɐ̃w̃]
- -em → [ẽj̃]: bem [ˈbẽj̃], dizem [ˈdizẽj̃]

### 1.3 鼻子音の同化（コーダ位置）

**表記方針について**: セクション1.2では G2P出力としての鼻母音のみの表記（例: campo [ˈkɐ̃pu]）を使用している。本セクションでは表層的な鼻子音の同化を含む音声表記（例: campo [ˈkɐ̃mpu]）を併記し、鼻音コーダの任意的実現を説明する。G2P実装では鼻母音のみを出力し、コーダ鼻子音は省略する方針を採用する。

音節末の鼻音は後続子音の調音点に同化する:
- n/m + [p, b] → [m]: campo [ˈkɐ̃mpu]（表層的に鼻子音が残る場合あり）
- n/m + [t, d] → [n]: canto [ˈkɐ̃ntu]
- n/m + [k, g] → [ŋ]: banco [ˈbɐ̃ŋku]

ただし、多くの音韻分析では鼻母音を /VN/（母音+鼻音原型素）として扱い、表層の鼻子音は任意的（方言差あり）とする。G2Pでは鼻母音のみを出力し、コーダ鼻子音は省略するアプローチが一般的。


## 2. 鼻二重母音

### 2.1 鼻二重母音インベントリ

ポルトガル語には5つの確立された鼻二重母音と、分析が分かれる /õw̃/ がある:

| 鼻二重母音 | IPA | 正書法 | 例 |
|-----------|-----|--------|-----|
| ão | [ɐ̃w̃] | ão, am | não [ˈnɐ̃w̃], falam [ˈfalɐ̃w̃] |
| ãe | [ɐ̃j̃] | ãe, ães | mãe [ˈmɐ̃j̃], cães [ˈkɐ̃j̃ʃ] |
| õe | [õj̃] | õe, ões | põe [ˈpõj̃], canções [kɐ̃ˈsõj̃ʃ] |
| em | [ẽj̃] | em, ens | bem [ˈbẽj̃], parabéns [pɐɾɐˈbẽj̃ʃ] |
| ui (鼻) | [ũj̃] | uim, uins | muito [ˈmũj̃tu] |
| om (語末強勢) | [õw̃] | om | bom [ˈbõw̃], tom [ˈtõw̃] |

**注記**: /õw̃/ を独立の鼻二重母音とするかは分析によって異なる。01_phoneme_inventory.md では /ɐ̃w̃/, /ẽj̃/, /õw̃/, /ũj̃/ の4種を基本セットとしている。本ドキュメントでは /ɐ̃j̃/ と /õj̃/ を加えた包括的リストを採用する。語末強勢位置の -om（例: bom, tom）は鼻二重母音 [õw̃] として実現するが、非強勢位置や語中の -om は単純鼻母音 [õ] にとどまる場合がある。G2P実装では語末強勢 -om を鼻二重母音として処理する。

**グライド表記について**: 本ドキュメントでは鼻二重母音のグライド要素に鼻化付きの半母音記号 [j̃], [w̃] を使用する。一部の文献では [ĩ], [ũ] を用いて表記する場合がある（例: [ɐ̃w̃] を [ɐ̃ũ] と表記）。

### 2.2 動詞活用形での頻出パターン

鼻二重母音は動詞活用において体系的に出現する:

#### -ão [ɐ̃w̃]（3人称複数・未来形等）
- 未来形: farão [fɐˈɾɐ̃w̃], dirão [diˈɾɐ̃w̃]
- 現在形3複（不規則）: são [ˈsɐ̃w̃], estão [iʃˈtɐ̃w̃], vão [ˈvɐ̃w̃]

#### -am [ɐ̃w̃]（3人称複数・過去形等、無アクセント）
- 過去形3複: falaram [fɐˈlaɾɐ̃w̃], fizeram [fiˈzeɾɐ̃w̃]
- 現在形3複: falam [ˈfalɐ̃w̃], comem [ˈkomẽj̃]
- **重要**: -am は常に無アクセント、-ão は常にアクセントあり

#### -em [ẽj̃]（3人称複数等）
- têm [ˈtẽj̃], vêm [ˈvẽj̃], dizem [ˈdizẽj̃]

#### -ões [õj̃ʃ]（名詞複数形）
- 単数 -ão → 複数 -ões が最も一般的な複数化パターン:
  - coração → corações [kuɾɐˈsõj̃ʃ]
  - opinião → opiniões [opiɲiˈõj̃ʃ]

### 2.3 G2P変換規則（鼻二重母音）

```
ão → [ɐ̃w̃]    （語末、常にストレスあり）
am → [ɐ̃w̃]    （語末、常にストレスなし）
ãe → [ɐ̃j̃]    （語末）
ães → [ɐ̃j̃ʃ]   （語末、+シビラント）
õe → [õj̃]     （語末）
ões → [õj̃ʃ]   （語末、+シビラント）
om → [õw̃]     （語末、強勢位置: bom, tom 等）
em → [ẽj̃]     （語末）
ens → [ẽj̃ʃ]   （語末、+シビラント）
```

**注意**: 語末でない位置の -em, -en は単純な鼻母音 [ẽ] になる。


## 3. フランス語の鼻母音化との比較

### 3.1 構造的差異

| 特性 | フランス語 | ポルトガル語 |
|------|-----------|-------------|
| 鼻母音の数 | 3（Metropolitan: /ɑ̃, ɛ̃, ɔ̃/）から4（Conservative: + /œ̃/） | 5 |
| 鼻二重母音 | なし | 4-5種 |
| コーダ鼻音の実現 | 完全消失 | 任意的（方言差あり） |
| 鼻母音化の条件 | V + n/m + {子音, 語末} | V + n/m + {子音, 語末} |
| 非鼻母音化条件 | V + nn/mm, V + n/m + V | 同様 |
| 音韻論的分析 | 純粋な鼻母音音素 | V + N 系列（鼻音原型素） |

### 3.2 NasalVowelizer実装との比較

フランス語 `NasalVowelizer.cs` の判定ロジック:
1. 母音 + n/m の次に母音 → 非鼻母音化 (**ポルトガル語でも同じ**)
2. nn/mm → 非鼻母音化 (**ポルトガル語でも適用可能**)
3. n + h + 母音 → 非鼻母音化 (**フランス語固有: h が無音のため実質的に n + 母音 = 母音間の n と同等。例: inhaler → 非鼻母音化。ポルトガル語では nh がダイグラフ [ɲ] であり、Phase 1 で先行消費されるため、NasalVowelizer 段階では n+h パターンが出現しない。このルールはポルトガル語版では不要。**)
4. それ以外 → 鼻母音化 (**ポルトガル語でも同じ方向性**)

**ポルトガル語で追加が必要な処理**:
- 鼻二重母音の検出（語末 -ão, -am, -ãe, -õe, -em）→ フランス語にはない
- nh はダイグラフとして Phase 1 で先行処理されるため、NasalVowelizer は nh を扱わない（処理順序の保証が重要）
- チルダ `~` の直接処理: ã, õ → 即座に鼻母音
- 鼻音の調音点同化（オプション、方言レベル）

### 3.3 コード設計への示唆

フランス語の `NasalVowelizer` をベースにポルトガル語版を設計できるが、以下の拡張が必要:

```
// フランス語（既存）
TryNasalize(word, vowelIndex, nasalConsonant, dialect, out nasalPhoneme, out charsConsumed)

// ポルトガル語（必要な拡張）
TryNasalize(word, vowelIndex, nasalConsonant, dialect, out phonemes, out charsConsumed)
// - phonemes が鼻母音1つ or 鼻二重母音（鼻母音 + 鼻化グライド j̃ or w̃）を返す
// - 鼻二重母音出力には JNasal (/j̃/) と WNasal (/w̃/) の両方の enum メンバーが必要
//   （01_phoneme_inventory.md の enum に JNasal の追加が必要 — 同ドキュメントのレビューで指摘済み）
// - 語末判定ロジックの追加
// - チルダ付き母音の前処理
```


## 4. 母音調和・Metaphony（語幹母音変化）

### 4.1 Metaphony の概要

ポルトガル語の metaphony は、語幹の中舌母音 /e, o/ が屈折形態素の影響で開閉が変化する現象:

- 閉鎖 [e] ↔ 開放 [ɛ]
- 閉鎖 [o] ↔ 開放 [ɔ]

### 4.2 名詞・形容詞の metaphony

性・数変化に伴う語幹母音の交替:
- novo [ˈnovu] (m.sg.) → nova [ˈnɔvɐ] (f.sg.) → novos [ˈnɔvuʃ] (m.pl.)
- porco [ˈpoɾku] (m.sg.) → porca [ˈpɔɾkɐ] (f.sg.)
- ovo [ˈovu] (m.sg.) → ovos [ˈɔvuʃ] (m.pl.)

規則: 女性形 -a / 男性複数形 -os の場合、語幹 o が [ɔ] に開く

### 4.3 動詞の metaphony

語幹変化動詞（stem-changing verbs）:
- dormir: durmo [ˈduɾmu], dormes [ˈdɔɾmɨʃ], dorme [ˈdɔɾmɨ]
- sentir: sinto [ˈsĩtu], sentes [ˈsẽtɨʃ], sente [ˈsẽtɨ]

高母音 /i, u/ が1人称単数に出現し、中母音 /ɛ, ɔ/ が2・3人称に出現する。

### 4.4 G2Pでの取り扱い

Metaphony は正書法に反映されないため、ルールベースG2Pでは正確に処理するのが困難:
- **推奨アプローチ**: 例外辞書で metaphony が発生する語を登録
- **代替アプローチ**: 頻出パターン（名詞 -o/-os, 動詞活用形）に限定したルール
- **注意**: 完全なルール化は品詞情報・形態素解析が必要で、初期実装では例外辞書に委ねるのが現実的


## 5. 語末 -l → [w] 変化（BP特有: L-Vocalization）

### 5.1 規則の概要

ブラジルポルトガル語（BP）では、音節末（コーダ位置）の /l/ が半母音 [w] に変化する:

| 正書法 | EP (ヨーロッパ) | BP (ブラジル) |
|--------|----------------|--------------|
| Brasil | [bɾɐˈziɫ] | [bɾaˈziw] |
| mal | [ˈmaɫ] | [ˈmaw] |
| sol | [ˈsɔɫ] | [ˈsɔw] |
| alto | [ˈaɫtu] | [ˈawtu] |
| azul | [ɐˈzuɫ] | [aˈzuw] |
| difícil | [diˈfisiɫ] | [dʒiˈfisiw] |
| futebol | [futɨˈbɔɫ] | [futʃiˈbɔw] |

### 5.2 規則の適用条件

- **適用**: 音節末の /l/（語末、子音前）
  - 語末: mal, sol, Brasil
  - 子音前（語中コーダ）: alto, calça, volta
- **非適用**: 音節頭の /l/（オンセット位置）
  - 語頭: lua [ˈluɐ], lado [ˈladu]
  - 母音間: calo [ˈkalu], bola [ˈbɔlɐ]
  - 子音+l クラスタ（オンセット）: plano [ˈplanu], claro [ˈklaɾu]

### 5.3 G2Pパイプラインでの処理

```
// 方言分岐ポイント
if (dialect == Dialect.Brazilian)
{
    // 音節末 /l/ → [w]
    // 音節構造解析後に適用（コーダ位置の判定が必要）
}
else // European
{
    // 音節末 /l/ → [ɫ]（velarized lateral、暗い l）
    // 注記: 一部のEP方言（語末位置等）では [ɬ]（voiceless lateral fricative）
    //       として実現する場合もある（01_phoneme_inventory.md セクション7.2参照）
}
```

### 5.4 方言差の詳細

- **BP（大半の方言）**: 完全な [w] 化
- **BP（南部国境地域）**: [ɫ] または [l] を保持する方言あり
- **EP（標準）**: [ɫ]（velarized lateral）
- **EP（カジュアル発話）**: 一部で [w] 化が起きることもある


## 6. Epenthesis（母音挿入）

### 6.1 BP での母音挿入

ブラジルポルトガル語では、許容されない子音クラスタを修復するために母音を挿入する。挿入母音は主に [i] だが、一部の環境（破裂音 /p, k/ の後など）では [u] が現れる場合もある。挿入の適用は任意的(variable)であり、社会言語学的要因（教育レベル、フォーマリティ）にも依存する。

| 正書法 | EP | BP |
|--------|----|----|
| apto | [ˈaptu] | [ˈapitu] ~ [ˈaptʃu] |
| afta | [ˈaftɐ] | [ˈafitɐ] ~ [ˈaftɐ] |
| psicologia | [psikuɫuˈʒiɐ] | [pisikɔlɔˈʒiɐ] |
| pneu | [ˈpnew] | [piˈnew] |
| ritmo | [ˈʁitmu] | [ˈʁitʃimu] |

### 6.2 許容される子音クラスタ（BP）

BPで epenthesis なしに許容されるクラスタ:
- **オンセット**: 阻害音 + /ɾ, l/ （prato, claro, bloco, grande, flor）
- **コーダ**: /s, ʃ, ɾ, w/（語末の /ʃ/ は元 /s/ の異音）
- **コーダ+オンセット**: /s/ + C, /ɾ/ + C（pasta, porta）

それ以外のクラスタで epenthesis が適用される可能性あり。

### 6.3 G2Pでの取り扱い

Epenthesis は任意的かつ方言依存度が高い:
- **推奨**: BP方言では epenthesis をオプション機能として実装
- **デフォルト**: 正書法通りのクラスタを保持（epenthesis なし）
- **理由**: 教育的・標準的な発音では epenthesis なしが好まれることも多く、フォーマル発音ではクラスタを保持


## 7. その他の特殊音韻プロセス

### 7.1 語末母音の弱化・上昇（Vowel Reduction）

#### EP（ヨーロッパポルトガル語）
無アクセント母音の大幅な弱化:
- /a/ → [ɐ]: casa [ˈkazɐ]
- /e/ → [ɨ] or 脱落: verde [ˈveɾdɨ], telefone [tɨlɨˈfɔnɨ]
- /o/ → [u]: bonito [buˈnitu]

#### BP（ブラジルポルトガル語）
弱化は EP ほど極端ではない:
- /e/ → [i]: leite [ˈlejtʃi]
- /o/ → [u]: bonito [boˈnitu]
- /a/ は比較的保持: casa [ˈkazɐ]

### 7.2 口蓋化（Palatalization）— BP特有

BP の多くの方言で /t, d/ + [i] → [tʃ, dʒ]:
- tia [ˈtʃiɐ]（EP: [ˈtiɐ]）
- dia [ˈdʒiɐ]（EP: [ˈdiɐ]）
- noite [ˈnojtʃi]（語末 e → [i] により連鎖適用）

### 7.3 シビラント（Sibilant）の異音

語末・音節末の /s/:
- EP: [ʃ]（子音前）, [ʒ]（有声子音前）: esta [ˈɛʃtɐ], mesmo [ˈmeʒmu]
- BP（リオ等一部方言）: EP と同様
- BP（サンパウロ等）: [s]（子音前）, [z]（有声子音前）: esta [ˈɛstɐ], mesmo [ˈmezmu]


## 8. G2Pパイプラインでの処理段階提案

上記の音韻プロセスを G2P パイプラインに統合する場合、以下の9段階の処理順序を提案する。

**02_g2p_rules.md との関係**: 02_g2p_rules.md で提案されている「7フェーズ」構成（Phase 1: ダイグラフ → Phase 2: 鼻母音化 → Phase 3: 文脈依存子音 → Phase 4: 母音変換 → Phase 5: 半母音化 → Phase 6: 母音弱化 → Phase 7: 黙字）は、本パイプラインの**ステップ4 GraphemeToPhonemeRules の内部構造**である。つまり、本ドキュメントの9段階は全体アーキテクチャ（パイプラインレベル）を記述し、02_g2p_rules.md の7フェーズはその中の1ステップ（G2Pルール変換）の詳細設計を記述している。

具体的な対応関係:
- 02_g2p_rules.md Phase 1-5, 7（ダイグラフ〜半母音化、黙字）→ 本パイプライン ステップ4 GraphemeToPhonemeRules 内部
- 02_g2p_rules.md Phase 6（母音弱化）→ 本パイプライン ステップ7 VowelReducer（独立段階として分離、下記参照）
- 鼻母音化: 02_g2p_rules.md Phase 2 = ステップ4 内の NasalVowelizer 呼び出し

```
1. Normalize（テキスト正規化）
   - 数字・略語・記号の展開

2. Tokenize（トークン化）
   - 単語分割

3. ExceptionDictionary（例外辞書ルックアップ）
   - Metaphony 語、不規則語の事前解決

4. GraphemeToPhonemeRules（基本G2P変換）
   - 02_g2p_rules.md の Phase 1-5, 7 に対応
   - チルダ付き母音 → 鼻母音（最優先）
   - 鼻二重母音検出（語末 -ão, -am, -ãe, -õe, -em, -ens, 強勢 -om）
   - 母音+n/m コーダ → 鼻母音化（NasalVowelizer）
   - nh → [ɲ], lh → [ʎ] 等のダイグラフ処理
   - 通常の書記素→音素変換

5. SyllableParser（音節分割）
   - onset maximization
   - コーダ位置の判定（l-vocalization, epenthesis に必要）

6. StressAssigner（ストレス位置決定）
   - アクセント記号ベース or デフォルトルール

7. VowelReducer（母音弱化）
   - 02_g2p_rules.md の Phase 6 に対応（VowelReducer.cs として独立クラスで実装）
   - 無アクセント母音の弱化（EP: /a/→[ɐ], /e/→[ɨ], /o/→[u] / BP: 語末 /e/→[i], /o/→[u]）
   - 母音弱化は音素レベルの変換であり、DialectProcessor 内の異音変化とは性質が異なるため独立段階とする

8. DialectProcessor（方言依存規則）
   - L-Vocalization: 音節末 /l/ → [w]（BP）or [ɫ]（EP）
   - Palatalization: /t,d/ + [i] → [tʃ, dʒ]（BP）— 母音弱化（ステップ7）の結果に依存
   - Sibilant Assimilation: 語末 /s/ の実現（方言別）
   - Epenthesis: 子音クラスタ修復（BP、オプション）

9. AllophoneProcessor（異音規則）
   - 鼻音の調音点同化
   - その他の一般的異音規則

10. Format（出力整形）
    - IPA / X-SAMPA / 音素列出力
```

### 8.1 処理順序の根拠

1. **鼻母音化は G2P ルール段階で処理**: 正書法から直接判定可能であり、音節構造に依存しない
2. **L-Vocalization は音節分割後**: コーダ位置の判定が必要
3. **母音弱化は独立段階（ステップ7）**: ストレス位置が確定した後に適用する必要があり、かつ後続の口蓋化の入力となるため、DialectProcessor（ステップ8）の前に実行する。02_g2p_rules.md の VowelReducer.cs 設計と整合
4. **Palatalization は母音弱化後**: 語末 /e/ → [i] が先に適用され、それにより /t, d/ の口蓋化が連鎖的に発生
5. **Epenthesis は DialectProcessor 内の最後**: 他のプロセスで生成された子音クラスタも対象になる可能性
6. **Metaphony は例外辞書**: 正書法に反映されないため、ルールでの完全な処理は困難

### 8.2 フランス語実装からの設計借用

| ポルトガル語コンポーネント | フランス語の対応コンポーネント | 借用可能性 |
|--------------------------|-------------------------------|-----------|
| PortugueseNasalVowelizer | NasalVowelizer.cs | 高（拡張して鼻二重母音対応） |
| PortugueseG2PRules | GraphemeToPhonemeRules.cs | 中（6フェーズ構成を参考） |
| PortugueseSyllabifier | FrenchSyllabifier.cs | 高（onset maximization は共通） |
| PortugueseAllophoneProcessor | AllophoneProcessor.cs | 中（方言別規則の構造を参考） |
| PortugueseExceptionDictionary | FrenchExceptionDictionary.cs | 高（TSVフォーマット共用可能） |
| PortugueseNormalizer | FrenchNormalizer.cs | 高（パイプライン構成を参考） |
