# ポルトガル語 異音規則（Allophone Rules）調査

## 概要

ポルトガル語には豊富な異音規則があり、特にヨーロッパポルトガル語（EP）とブラジルポルトガル語（BP）で大きく異なる。
本ドキュメントでは、G2P実装に必要な主要異音規則を網羅的にまとめ、既存のスペイン語/フランス語実装パターンとの対応、
および `PortugueseAllophoneFeatures` flags enum の設計提案を記す。

---

## 1. 閉鎖音の弱化（Lenition / Spirantization）

### 概要

スペイン語と同様、有声閉鎖音 /b/, /d/, /ɡ/ が特定の環境で摩擦音/接近音に弱化する。
ただし、ポルトガル語では方言差が大きい。

### 異音分布

| 音素 | 閉鎖音保持 [b, d, ɡ] | 弱化 [β, ð, ɣ] |
|------|----------------------|----------------|
| /b/  | 語頭、鼻音後、/l/後 | 母音間、その他の環境 |
| /d/  | 語頭、鼻音後、/l/後 | 母音間、その他の環境 |
| /ɡ/  | 語頭、鼻音後、/l/後 | 母音間、その他の環境 |

**注**: /b/, /d/, /ɡ/ のいずれもEP方言では側面音 /l/ の後で閉鎖音を保持する（スペイン語と同パターン）。スペイン語の既存実装では /b/ の /l/ 後保持条件は含まれていないが、ポルトガル語では音声学文献に基づき /b/ にも /l/ 後の保持を適用する。

### 方言差

- **EP（ポルトガル北部・中部）**: 弱化が顕著。スペイン語とほぼ同じパターンで [β, ð, ɣ] が出現する。
- **EP（リスボン以南）**: 弱化は存在するが、程度は地域により異なる。
- **BP（ブラジル）**: 弱化はほとんど発生しない。/b, d, ɡ/ は全環境で閉鎖音 [b, d, ɡ] を保持する傾向が強い。
- **北ポルトガル・東ティモール・フローレス島**: /b/ と /v/ が合流し、どちらも [b ~ β] で発音される（betacismo）。

### 実装方針

- スペイン語の `AllophoneProcessor.ApplyLenition()` とほぼ同じロジックを流用可能。
- BP方言では弱化を無効にする（フラグ制御）。
- `/d/` の弱化では、`/l/` の後でも閉鎖音を保持する点を追加（スペイン語と同様）。

### 参考: スペイン語実装（AllophoneProcessor.cs:60-88）

```csharp
// スペイン語の弱化ロジック - ポルトガル語でもほぼ同じ条件
case SpanishIpaPhoneme.B:
    return IsWordInitial(index) || IsNasal(previous)
        ? SpanishIpaPhoneme.B
        : SpanishIpaPhoneme.Beta;
```

---

## 2. 鼻音の調音位置同化（Nasal Place Assimilation）

### 概要

/n/ は後続の子音の調音位置に同化する。これはスペイン語やフランス語と同様の普遍的プロセス。

### 異音分布

| 後続子音 | /n/ の異音 | 例 |
|---------|-----------|-----|
| /p, b, m/ | [m] 両唇 | um prato [ũm ˈpɾatu] |
| /f, v/ | [ɱ] 唇歯 | enfim [ẽˈfĩ] |
| /t, d, s, z, l, n/ | [n] 歯茎 | antes [ˈɐ̃tɨʃ] |
| /ʃ, ʒ/ | [n̠] 後部歯茎 | encher [ẽn̠ˈʃeɾ] |
| /ɲ/ | [ɲ] 硬口蓋 | — |
| /k, ɡ/ | [ŋ] 軟口蓋 | un gato [ũŋ ˈɡatu] |

**注**: /ʃ, ʒ/ は後部歯茎音（postalveolar）であり硬口蓋音（palatal）ではないため、/ʃ, ʒ/ の前では [n̠]（後部歯茎化した歯茎鼻音）に同化する。/ɲ/ の前のみ [ɲ] に同化する。ポルトガル語では鼻音がコーダで鼻母音化に吸収されるケースが多いため、鼻音同化が明確に観察される語境界の例を併記した。

### BP の /ɲ/ の半母音的実現

BP方言では /ɲ/ が [j̃]（鼻音化半母音）として実現され、先行母音が鼻音化する。例: `banho` /ˈbaɲu/ → [ˈbɐ̃j̃u]。この規則はBP方言固有であり、EPでは /ɲ/ は硬口蓋鼻音 [ɲ] として保持される。初期実装ではこの変換を省略し、方言差として扱わないことも選択肢だが、より正確なBP音声表記のために将来的に `TDPalatalization` と同様の方言フラグ制御で実装することを検討する。

### 実装方針

- スペイン語の `AllophoneProcessor.AssimilateNasal()` とほぼ同じマッピングテーブルを使用。
- ポルトガル語固有の音素（/ʃ, ʒ/ が基本音素として存在する点）を追加対応。
- /ʃ, ʒ/ の前では [n̠]（後部歯茎鼻音）に同化させる。`PortugueseIpaPhoneme` enum に `NPostalveolar` の追加が必要。

### 参考: スペイン語実装（AllophoneProcessor.cs:134-169）

```csharp
// 鼻音同化 - ポルトガル語にもほぼそのまま適用可能
private static SpanishIpaPhoneme AssimilateNasal(SpanishIpaPhoneme next)
{
    switch (next)
    {
        case SpanishIpaPhoneme.P:
        case SpanishIpaPhoneme.B:
            return SpanishIpaPhoneme.M;
        case SpanishIpaPhoneme.F:
            return SpanishIpaPhoneme.NLabiodental;
        case SpanishIpaPhoneme.K:
        case SpanishIpaPhoneme.G:
            return SpanishIpaPhoneme.Eng;
        // ...
    }
}
```

---

## 3. 母音弱化・中和（Vowel Reduction）

### 概要

ポルトガル語の最も特徴的な異音規則の一つ。特にEPでは劇的な母音弱化が起こる。

### EP（ヨーロッパポルトガル語）の母音弱化

ストレスのない音節で、母音が大幅に弱化（中和・中央化・高舌化）する。

| ストレスあり | ストレスなし | 例 |
|-------------|-------------|-----|
| /a/ → [a, ɐ] | → [ɐ] | casa [ˈkazɐ] |
| /e, ɛ/ → [e, ɛ] | → [ɨ] (または消失) | pequeno [pɨˈkenu] |
| /o, ɔ/ → [o, ɔ] | → [u] | morar [muˈɾaɾ] |
| /i/ → [i] | → [i] (変化なし) | |
| /u/ → [u] | → [u] (変化なし) | |

**重要**: EPでは無ストレスの /e/ が [ɨ]（中舌高母音）に弱化し、さらに速い発話では完全に消失（syncope）する場合がある。

### BP（ブラジルポルトガル語）の母音弱化

BPの母音弱化はEPより穏やか。

| ストレスあり | ストレスなし | 例 |
|-------------|-------------|-----|
| /a/ → [a, ɐ] | → [ɐ] | casa [ˈkazɐ] |
| /e, ɛ/ → [e, ɛ] | → [i] | menino [miˈninu] |
| /o, ɔ/ → [o, ɔ] | → [u] | bonito [buˈnitu] |

### 語末母音弱化

| 綴り | EP | BP |
|------|-----|-----|
| -e | [ɨ] (消失も可) | [i] |
| -o | [u] | [u] |
| -a | [ɐ] | [ɐ] |

### 実装方針

- 母音弱化は**G2Pルール段階**で処理する。セクション10の設計原則に基づき、母音弱化はルール段階で方言に応じた弱化先を適用し、`PortugueseAllophoneFeatures` には含めない。これにより、異音フラグを `byte` 基底（8ビット）に収めることができ、スペイン語・フランス語との一貫性を維持できる。
- EPとBPで弱化先の母音が異なる（EP: /e/→[ɨ], BP: /e/→[i]）ため、G2Pルール段階で方言フラグに基づき切り替える。
- 母音消失（syncope）は実装の複雑さが高いため、初期実装では省略する選択肢もある。

---

## 4. 有声/無声の同化（Voicing Assimilation）

### 概要

コーダ位置の歯擦音が後続子音の有声性に同化する。

### 規則

| 環境 | 結果 | 例 |
|------|------|-----|
| /s/ + 有声子音 | [z] or [ʒ] | mesmo [ˈmeʒmu] (EP), [ˈmezmu] (BP一部) |
| /s/ + 無声子音 | [s] or [ʃ] | festa [ˈfɛʃtɐ] (EP), [ˈfɛstɐ] (BP一部) |
| /s/ + ポーズ/語末 | [ʃ] or [s] | rapaz [ʁɐˈpaʃ] (EP), [haˈpas] (BP一部) |

### 方言差

- **EP・リオデジャネイロ・東北部BP**: コーダの /s/ は後部歯茎音 [ʃ, ʒ] で実現
- **サンパウロ・南部BP**: コーダの /s/ は歯茎音 [s, z] で実現
- どちらの方言でも、**有声性は後続子音に同化**する

### 実装方針

- フランス語の `AllophoneProcessor.ApplyObstruentVoicingAssimilation()` と類似のロジック。
- ただし、ポルトガル語ではコーダの歯擦音のみに適用される点が特殊。
- 歯擦音の後部歯茎化（[s]→[ʃ], [z]→[ʒ]）は方言フラグで制御。

### 参考: フランス語実装（AllophoneProcessor.cs:54-73）

```csharp
// 阻害音有声性同化 - ポルトガル語では歯擦音に限定した版が必要
private static void ApplyObstruentVoicingAssimilation(FrenchPhoneme[] phonemes)
{
    for (var i = phonemes.Length - 2; i >= 0; i--)
    {
        if (!IsObstruent(phonemes[i].Phoneme) || !IsObstruent(phonemes[i + 1].Phoneme))
            continue;
        var nextVoiced = IsVoicedObstruent(phonemes[i + 1].Phoneme);
        // 後続子音の有声性に前の阻害音を統一
    }
}
```

---

## 5. 連声・サンディ（Sandhi）

### 概要

ポルトガル語には、語境界で発生する複数の音韻変化プロセスがある。

### 5.1 歯擦音の連声

コーダの歯擦音は、語境界を越えて後続語の先頭子音の有声性に同化する。

| 環境 | 結果 | 例 |
|------|------|-----|
| -s + 有声子音 | [z] / [ʒ] | bons dias [bõʒ ˈdiɐʃ] (EP) |
| -s + 無声子音 | [s] / [ʃ] | bons tempos [bõʃ ˈtẽpuʃ] (EP) |
| -s + 母音 | [z] | os amigos [uz‿ɐˈmiɡuʃ] (EP) |

### 5.2 母音サンディ（Vowel Sandhi）

BPで特に顕著な3種類のプロセスがある。

| プロセス | 説明 | 例 |
|---------|------|-----|
| **エリジオン（Elision）** | 同一母音の片方が消失 | casa amarela → cas[a]marela |
| **二重母音化（Diphthongization）** | 2母音が二重母音に融合 | me esqueci → m[jɨ]squeci |
| **融合（Coalescence）** | 2母音が単一母音に合流 | minha amiga → minh[ɐ]miga |

### 実装方針

- **語レベルG2P**ではサンディは基本的に不要（単語単位の変換では語境界プロセスが発生しない）。
- ただし、将来的なフレーズレベルG2Pを見据えて、`EnableSandhi` フラグを設計に含めておく。
- 歯擦音の語境界同化は、コーダ歯擦音の有声性同化として実装し、語境界フラグで制御可能にする。

---

## 6. /r/ と /ʁ/ の異音分布（Rhotic Allophony）

### 概要

ポルトガル語には2つのロティック（流音r）音素があり、異音分布が非常に複雑。

### 音素体系

| 音素 | 名前 | 機能 |
|------|------|------|
| /ɾ/ | 歯茎はじき音（flap/tap） | 母音間の単独 `r` |
| /ʁ/ | 口蓋垂摩擦音（uvular fricative）※ | 語頭の `r`、二重子音 `rr`、/n,l,s/ の後の `r` |

※歴史的には歯茎ふるえ音 /r/ だが、現代のEP・多くのBP方言では口蓋垂音に変化。

### 異音分布（正書法→音声の対応）

| 環境 | EP | BP（多くの方言） | BP（南部） |
|------|-----|-----------------|-----------|
| 語頭 `r-` | [ʁ] | [h] / [x] / [ʁ] | [r] / [ʁ] |
| 母音間 `-rr-` | [ʁ] | [h] / [x] / [ʁ] | [r] / [ʁ] |
| 母音間 `-r-` | [ɾ] | [ɾ] | [ɾ] |
| /n,l,s/ + `r` | [ʁ] | [h] / [x] | [r] |
| コーダ `-r` (音節末) | [ɾ] / [ʁ] | [h] / [x] / [ɾ] | [ɾ] |
| 子音群 onset `Cr-` | [ɾ] | [ɾ] | [ɾ] |

### BPの主な /ʁ/ 実現形

| 異音 | IPA | 分布 |
|------|-----|------|
| 口蓋垂摩擦音（有声） | [ʁ] | リオデジャネイロ |
| 口蓋垂摩擦音（無声） | [χ] | 一部地域 |
| 軟口蓋摩擦音（無声） | [x] | サンパウロ等 |
| 声門摩擦音（無声） | [h] | 東北部等（最も広範） |
| 歯茎ふるえ音 | [r] | 南部（保守的） |

### 実装方針

- G2Pルール段階で `/ɾ/` と `/ʁ/` を音素レベルで区別して出力。
- 異音規則段階で `/ʁ/` の具体的実現形を方言に応じて選択:
  - EP: [ʁ] (デフォルト)
  - BP: [h] (デフォルト) または [x], [ʁ] (オプション)
- コーダの /r/ の実現形は方言差が大きいため、フラグ制御が望ましい。

---

## 7. 歯擦音の異音分布（Sibilant Allophony）

### 概要

/s/ の異音分布はポルトガル語で最も方言変異の大きい領域の一つ。

### コーダ位置の歯擦音

| 方言 | コーダ /s/ の実現形 | 地域 |
|------|-------------------|------|
| 後部歯茎型 | [ʃ] (無声) / [ʒ] (有声) | EP全域、リオ、東北部BP |
| 歯茎型 | [s] (無声) / [z] (有声) | サンパウロ、南部BP |

### 有声性による変異

| 後続環境 | 後部歯茎型方言 | 歯茎型方言 |
|---------|-------------|-----------|
| + 無声子音 | [ʃ] | [s] |
| + 有声子音 | [ʒ] | [z] |
| + 母音（語境界） | [ʒ] / [z] | [z] |
| + ポーズ | [ʃ] | [s] |

### 実装方針

- 基本G2Pでは /s/ を歯茎音素として出力。
- 異音規則で、方言フラグに応じてコーダ位置の [s]→[ʃ], [z]→[ʒ] への後部歯茎化を適用。
- 有声性同化は前述の「有声/無声の同化」ルールと統合。

---

## 8. /t, d/ の破擦音化（Stop Palatalization / Affrication）

### 概要

BPの多くの方言で、/t/ と /d/ が高前舌母音 /i/ の前で破擦音化する。

### 異音分布

| 音素 | 環境 | EP | BP（破擦音化方言） | BP（非破擦音化方言） |
|------|------|-----|------------------|-------------------|
| /t/ | + /i/ | [t] | [tʃ] | [t] |
| /d/ | + /i/ | [d] | [dʒ] | [d] |
| /t/ | その他 | [t] | [t] | [t] |
| /d/ | その他 | [d] | [d] | [d] |

### トリガー環境

- 基底の /i/: tia [ˈtʃiɐ] ("aunt"), dia [ˈdʒiɐ] ("day")
- 弱化した /e/ → [i]: noite [ˈnojtʃi] ("night"), verdade [veʁˈdadʒi] ("truth")
- EPでは発生しない

### 実装方針

- BP方言専用の異音規則として実装。
- `TDPalatalization` フラグで制御。
- 後続母音が /i/（または弱化した /e/→[i]）であるかを判定条件とする。

---

## 9. /l/ の異音分布（Lateral Allophony）

### 概要

/l/ のコーダ位置での実現は、EP と BP で根本的に異なる。

### 異音分布

| 環境 | EP | BP（大多数） | BP（南部国境地域） |
|------|-----|------------|------------------|
| オンセット (onset) | [l] ～ [ɫ] | [l] | [l] |
| コーダ (coda) | [ɫ] (軟口蓋化) | [w] (半母音化) | [ɫ] |

### 詳細

- **EP**: 近年の研究では、EPの /l/ はオンセットでもコーダでも一貫して軟口蓋化 [ɫ] (dark l) であり、位置による明確な二項対立はない（連続体として存在）。
- **BP**: コーダの /l/ は [w] に半母音化（l-vocalization）する。例: `mal` [maw], `Brasil` [bɾaˈziw], `alto` [ˈawtu]。
- これは音韻レベルの変化であり、BP話者にとって `mal`（副詞）と `mau`（形容詞）は同音語 [maw]。

### 実装方針

- BP方言では異音規則でコーダの /l/ → [w] 変換を適用。
- EP方言では /l/ → [ɫ] 変換をオプションで適用（IPAの詳細表記レベル）。
- `LAllophony` 単一フラグで制御し、方言に応じて自動的に BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ] を適用する。両規則は排他的であるため、単一フラグ + 方言判定で切り替える設計とする。

---

## 10. AllophoneFeatures flags enum 設計提案

### 設計原則

スペイン語 (`SpanishAllophoneFeatures`) とフランス語 (`FrenchAllophoneFeatures`) の既存パターンに準拠し、
`[Flags] enum : byte` で定義する。

### 必須規則 vs 可変規則

| 規則 | 分類 | 根拠 |
|------|------|------|
| **鼻音の調音位置同化** | 必須 | 全方言共通、IPA転写で常に反映 |
| **コーダ歯擦音の有声性同化** | 必須 | 全方言共通（実現形は方言で異なるが有声性同化自体は普遍的） |
| **閉鎖音弱化（Lenition）** | 可変 | EPで顕著、BPではほぼ不在 |
| **コーダ歯擦音の後部歯茎化** | 可変 | EP全域 + 一部BP方言、他のBP方言では歯茎音のまま |
| **/t,d/ 破擦音化** | 可変 | BP多数方言で発生、EPでは不在 |
| **/l/ 異音（半母音化 or 軟口蓋化）** | 可変 | BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ]（排他的、方言で自動選択） |
| **/ʁ/ の実現形選択** | 可変 | 方言ごとに [ʁ]/[h]/[x]/[χ] が異なる |

**注**: 母音弱化（VowelReduction）はG2Pルール段階で処理するため、異音フラグには含めない（セクション3の実装方針参照）。

### enum 定義案

```csharp
using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語の異音規則セット。
    /// 母音弱化（VowelReduction）はG2Pルール段階で処理するため含めない。
    /// </summary>
    [Flags]
    public enum PortugueseAllophoneFeatures : byte
    {
        /// <summary>異音規則を適用しない。</summary>
        None = 0,

        // === 必須規則（全方言共通） ===

        /// <summary>鼻音の調音位置同化を適用する。</summary>
        NasalAssimilation = 1 << 0,

        /// <summary>コーダ歯擦音の有声性同化を適用する。</summary>
        SibilantVoicingAssimilation = 1 << 1,

        // === 可変規則 ===

        /// <summary>/b,d,ɡ/ の母音間弱化（EP向け）を適用する。</summary>
        Lenition = 1 << 2,

        /// <summary>コーダ歯擦音の後部歯茎化（[s]→[ʃ], [z]→[ʒ]）を適用する。</summary>
        SibilantPalatalization = 1 << 3,

        /// <summary>/t,d/ + /i/ の破擦音化（[tʃ], [dʒ]）を適用する（BP向け）。</summary>
        TDPalatalization = 1 << 4,

        /// <summary>コーダ /l/ の異音を適用する（BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ]、方言で自動選択）。</summary>
        LAllophony = 1 << 5,

        /// <summary>/ʁ/ を [h] で実現する（BP向け）。</summary>
        RhoticDebuccalization = 1 << 6,

        // === プリセット ===

        /// <summary>全方言共通の必須規則セット。</summary>
        Obligatory = NasalAssimilation | SibilantVoicingAssimilation,

        /// <summary>EP（ヨーロッパポルトガル語）向けデフォルト。</summary>
        EuropeanDefault = Obligatory | Lenition | SibilantPalatalization | LAllophony,

        /// <summary>BP（ブラジルポルトガル語）向けデフォルト。</summary>
        BrazilianDefault = Obligatory | TDPalatalization | LAllophony | RhoticDebuccalization,

        /// <summary>全規則適用。</summary>
        All = Obligatory | Lenition | SibilantPalatalization | TDPalatalization
            | LAllophony | RhoticDebuccalization,
    }
}
```

### 型の選択: `byte` 基底

- スペイン語 (`SpanishAllophoneFeatures : byte`) とフランス語 (`FrenchAllophoneFeatures : byte`) に合わせ、`byte`（8ビット）を使用する。
- 母音弱化（VowelReduction）をG2Pルール段階に移動し、`LVocalization` と `LVelarization` を排他的な単一フラグ `LAllophony`（方言で自動切り替え）に統合したことで、7フラグに収まり `byte` で統一できる。

---

## 11. 異音規則の適用順序

異音規則には相互作用があるため、適用順序が重要。

### 推奨適用順序

```
[前提] 母音弱化 (VowelReduction) はG2Pルール段階で既に適用済み
   - /t,d/ 破擦音化のトリガー条件に影響するため、G2Pルール段階で先行適用
   - 例: noite → /nojte/ → 弱化で /nojti/ → 破擦音化で /nojtʃi/

1. /t,d/ 破擦音化 (TDPalatalization)
   - 弱化後の母音を参照するため、母音弱化（G2Pルール段階）の後に適用

2. 閉鎖音弱化 (Lenition)
   - 音素の種類を変えるため、同化規則の前に適用
   - 注: 弱化後の [β] 等も後続の鼻音同化テーブルでマッチさせる必要がある

3. 鼻音の調音位置同化 (NasalAssimilation)
   - 後続子音を参照するため、子音の変化後に適用

4. コーダ歯擦音の後部歯茎化 (SibilantPalatalization)
   - 有声性同化の前に適用する
   - EP/リオ方言ではコーダ歯擦音はまず後部歯茎化し（/s/→[ʃ]）、
     次に後続環境で有声性が決まる（[ʃ]+有声子音→[ʒ]）
   - 例: /s/ → [ʃ] (後部歯茎化) → [ʒ] (有声性同化)

5. コーダ歯擦音の有声性同化 (SibilantVoicingAssimilation)
   - 後部歯茎化後の歯擦音に対して有声性を同化

6. /l/ 異音 (LAllophony)
   - BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ]（方言で自動選択）
   - 他の規則に影響しないため後半に適用

7. /ʁ/ 実現形選択 (RhoticDebuccalization)
   - 最終段階で適用
```

---

## 12. 既存実装との構造対応

### スペイン語 AllophoneProcessor との対応

| スペイン語 | ポルトガル語 | 備考 |
|-----------|------------|------|
| `Lenition` (β,ð,ɣ) | `Lenition` | 同じロジック、BP方言では無効化 |
| `NasalAssimilation` | `NasalAssimilation` | ほぼ同一マッピング（/ʃ,ʒ/ 前の [n̠] を追加） |
| `SVoicing` | `SibilantVoicingAssimilation` | 拡張版（コーダ歯擦音全般） |
| `YeAffrication` | ― | ポルトガル語には該当なし |
| `FinalDSoftening` | ― | ポルトガル語には該当なし |
| ― | `TDPalatalization` | ポルトガル語固有 |
| ― | `LAllophony` | ポルトガル語固有（BP: [w], EP: [ɫ]） |
| ― | `SibilantPalatalization` | ポルトガル語固有 |

### フランス語 AllophoneProcessor との対応

| フランス語 | ポルトガル語 | 備考 |
|-----------|------------|------|
| `RDevoicing` | ― | ポルトガル語では /ʁ/ の無声化は別メカニズム |
| `ObstruentVoicingAssimilation` | `SibilantVoicingAssimilation` | 類似（ただし範囲が異なる） |
| `VowelLengthening` | ― | ポルトガル語には該当なし |
| `LVelarization` | `LAllophony` | 同種の規則。ただしフランス語側は `FrenchAllophoneFeatures` にフラグ定義はあるが `Apply()` メソッドに処理ロジックが未実装である点に注意 |
| `FinalDevoicing` | ― | ポルトガル語には該当なし |
| ― | 母音弱化（G2Pルール段階） | ポルトガル語固有の重要規則。異音フラグではなくG2Pルール段階で処理 |

### 実装構造

```csharp
internal static class AllophoneProcessor
{
    public static PortuguesePronunciation Apply(
        PortuguesePronunciation pronunciation,
        PortugueseAllophoneFeatures features,
        PortugueseDialect dialect)
    {
        if (features == PortugueseAllophoneFeatures.None)
            return pronunciation;

        var result = /* copy phonemes */;

        // 適用順序に従って処理
        // 注: 母音弱化（VowelReduction）はG2Pルール段階で既に適用済み

        if (HasFeature(features, PortugueseAllophoneFeatures.TDPalatalization))
            ApplyTDPalatalization(result);

        if (HasFeature(features, PortugueseAllophoneFeatures.Lenition))
            ApplyLenition(result);

        if (HasFeature(features, PortugueseAllophoneFeatures.NasalAssimilation))
            ApplyNasalAssimilation(result);

        if (HasFeature(features, PortugueseAllophoneFeatures.SibilantPalatalization))
            ApplySibilantPalatalization(result);

        if (HasFeature(features, PortugueseAllophoneFeatures.SibilantVoicingAssimilation))
            ApplySibilantVoicingAssimilation(result);

        if (HasFeature(features, PortugueseAllophoneFeatures.LAllophony))
            ApplyLAllophony(result, dialect); // BP: [w], EP: [ɫ] を方言で自動選択

        if (HasFeature(features, PortugueseAllophoneFeatures.RhoticDebuccalization))
            ApplyRhoticDebuccalization(result);

        return new PortuguesePronunciation(result, ...);
    }
}
```

**注意**: `/l/` の異音は `LAllophony` 単一フラグで制御し、`PortugueseDialect` に基づいて BP: 半母音化 [w]、EP: 軟口蓋化 [ɫ] を自動選択する。排他的な規則を別フラグにすると `[Flags] enum` 上で両方セット可能になり矛盾が生じるため、単一フラグ + 方言判定の設計を採用した。

---

## 13. PortugueseIpaPhoneme に必要な異音素

異音規則の出力として、以下の追加音素が `PortugueseIpaPhoneme` enum に必要。

| 異音 | IPA | 元の音素 | 規則 |
|------|-----|---------|------|
| Beta | β | /b/ | Lenition |
| Dh | ð | /d/ | Lenition |
| Gh | ɣ | /ɡ/ | Lenition |
| NLabiodental | ɱ | /n/ | NasalAssimilation |
| NPostalveolar | n̠ | /n/ | NasalAssimilation (/ʃ, ʒ/ の前) |
| NPalatal | ɲ | /n/ | NasalAssimilation (/ɲ/ の前) |
| NVelar | ŋ | /n/ | NasalAssimilation |
| NDental | n̪ | /n/ | NasalAssimilation (歯音の前) |
| Tsh | tʃ | /t/ | TDPalatalization |
| Dzh | dʒ | /d/ | TDPalatalization |
| LDark | ɫ | /l/ | LAllophony (EP) |
| H | h | /ʁ/ | RhoticDebuccalization |
| X | x | /ʁ/ | RhoticDebuccalization (変種) |
| SchwaHigh | ɨ | /e/ | VowelReduction (EP、G2Pルール段階で処理) |

**注**: `Beta`, `Dh`, `Gh` は `01_phoneme_inventory.md` のセクション8の enum 提案（45種）には含まれていない。異音規則の実装にあたり、音素インベントリ側にもこれらの弱化異音を追加する必要がある。

---

## 参考文献・ソース

- [Portuguese phonology - Wikipedia](https://en.wikipedia.org/wiki/Portuguese_phonology)
- [Help:IPA/Portuguese - Wikipedia](https://en.wikipedia.org/wiki/Help:IPA/Portuguese)
- [LDLD: Portuguese Language. Spoken Language. Phonology.](https://ldldproject.net/languages/portuguese/spoken/phonology.html)
- [The enigmatic Portuguese R - Hacking Portuguese](http://hackingportuguese.com/pronunciation/portuguese-r-the-long-version/)
- [/l/ velarisation as a continuum (PLOS ONE)](https://journals.plos.org/plosone/article/file?type=printable&id=10.1371/journal.pone.0213392)
- [Palatalization in Brazilian Portuguese (Semantics Scholar)](https://pdfs.semanticscholar.org/454a/ddcd7aedcd795f53a62d91d7a90c75ad6709.pdf)
- [Brazilian Portuguese external sandhi (Filologia e Linguistica Portuguesa)](https://revistas.usp.br/flp/en/article/view/223628)
- [Some sandhi rules in Portuguese (ResearchGate)](https://www.researchgate.net/publication/298731363_Some_sandhi_rules_in_Portuguese)
- 既存実装: `DotNetG2P.Spanish/Rules/AllophoneProcessor.cs`
- 既存実装: `DotNetG2P.French/Rules/AllophoneProcessor.cs`
- 既存実装: `DotNetG2P.French/FrenchAllophoneFeatures.cs`
