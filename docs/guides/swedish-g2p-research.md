# スウェーデン語G2P技術調査レポート

> **調査日**: 2026-04-02
> **ブランチ**: `feature/swedish-g2p`
> **目的**: DotNetG2P.Swedish パッケージの設計・実装に向けた包括的技術調査

---

## 目次

1. [エグゼクティブサマリー](#1-エグゼクティブサマリー)
2. [スウェーデン語音韻体系](#2-スウェーデン語音韻体系)
3. [正書法とG2P規則](#3-正書法とg2p規則)
4. [ピッチアクセント](#4-ピッチアクセント)
5. [方言と変種](#5-方言と変種)
6. [OSSツール・ライブラリ調査](#6-ossツールライブラリ調査)
7. [学術論文・文献調査](#7-学術論文文献調査)
8. [評価データセット](#8-評価データセット)
9. [アプローチ比較と推奨設計](#9-アプローチ比較と推奨設計)
10. [既存実装パターンの分析](#10-既存実装パターンの分析)
11. [推奨アーキテクチャ](#11-推奨アーキテクチャ)
12. [ベンチマーク・評価戦略](#12-ベンチマーク評価戦略)
13. [テスト戦略](#13-テスト戦略)
14. [Multilingual統合計画](#14-multilingual統合計画)
15. [マイルストーン案](#15-マイルストーン案)

---

## 1. エグゼクティブサマリー

### 結論

スウェーデン語は「浅い正書法（shallow orthography）」に分類され、ルールベース＋例外辞書のハイブリッドアプローチで **PER 2-4%** の達成が見込まれる。DotNetG2Pの既存7言語の設計パターン（特にスペイン語/フランス語/ポルトガル語）を踏襲することで、効率的な実装が可能。

### スウェーデン語G2Pの固有課題

| 課題 | 難度 | 対策 |
|------|------|------|
| sj音 `/ɧ/` の65種類の綴り | 高 | マルチグラフ規則＋例外辞書 |
| ピッチアクセント（accent 1/2） | 高 | 接尾辞規則＋例外辞書 |
| そり舌化（r+歯茎子音のsandhi） | 中 | 正書法レベル規則（rt/rd/rn/rl/rs） |
| 母音の長短決定 | 中 | 相補的数量法則の規則化 |
| 複合語の分解 | 中 | 例外辞書＋接尾辞パターン |
| 外来語（英語/フランス語） | 中 | 例外辞書（300-500語） |
| 18母音音素（9品質×長短） | 低 | enum定義で対応 |

### 推奨アプローチ

```
ルールベース＋例外辞書（DotNetG2Pスペイン語/フランス語/ポルトガル語と同一パターン）
├── 推定PER: 2-4%（ipa-dict基準）
├── 外部依存: なし（.NET Standard 2.1純粋C#）
├── 例外辞書規模: 初期300語→成熟500-600語
└── 音素数: 母音18+子音18+そり舌5 = 41音素（enum定義）。超分節素（ストレス/長音/アクセント）は出力時に付加
```

---

## 2. スウェーデン語音韻体系

### 2.1 母音体系（18音素 = 9品質 × 長短）

| 書記素 | 長母音 IPA | 短母音 IPA | 長の例 | 短の例 |
|--------|-----------|-----------|--------|--------|
| a | /ɑː/ | /a/ | hal（滑る） | hall（ホール） |
| e | /eː/ | /ɛ/ | hel（全体） | hell（地獄） |
| i | /iː/ | /ɪ/ | sil（ふるい） | sill（ニシン） |
| o | /uː/ | /ʊ/ ~ /ɔ/ | sol（太陽） | bott（底） |
| u | /ʉː/ | /ɵ/ | hus（家） | hund（犬） |
| y | /yː/ | /ʏ/ | syl（千枚通し） | syll（枕木） |
| å | /oː/ | /ɔ/ | båt（船） | gått（行った） |
| ä | /ɛː/ | /ɛ/ | häl（かかと） | häll（岩棚） |
| ö | /øː/ | /œ/ | öl（ビール） | höst（秋） |

**注意**: `o` の長母音は `/uː/` にマッピングされる（非直感的）。

#### 相補的数量法則（Complementary Quantity）

ストレス音節では「長母音＋短子音」か「短母音＋長子音（重子音）」のいずれか:

| 条件 | 母音 | 子音 | 正書法の手がかり | 例 |
|------|------|------|----------------|-----|
| 開音節・単子音後 | 長 | 短 | V + 単子音 | mat /mɑːt/ |
| 語末母音 | 長 | - | V# | ja /jɑː/ |
| 二重子音前 | 短 | 長 | V + CC | matt /mat:/ |
| 子音連結前 | 短 | - | V + CC... | dricka /drɪkːa/ |

### 2.2 子音体系（18音素）

| | 両唇 | 唇歯 | 歯/歯茎 | 硬口蓋 | 軟口蓋 | 声門 |
|--|------|------|----------|------|--------|------|
| 鼻音 | m | | n | | ŋ | |
| 破裂音 | p, b | | t, d | | k, ɡ | |
| 摩擦音 | | f, v | s | ɕ | ɧ | h |
| 接近音 | | | l | j | | |
| ふるえ音 | | | r | | | |

### 2.3 スウェーデン語特有の音素

#### sj音 `/ɧ/`
- IPA記号 ⟨ɧ⟩ は「同時的な [ʃ] と [x]」とされるが議論あり
- 方言による実現の変異が極めて大きい（[xʷ] ~ [ʃ] ~ [fˠʷ]）
- 約65種類の綴りパターンが存在（G2P最大の難所）

#### tj音 `/ɕ/`
- 無声歯茎硬口蓋摩擦音
- 綴り: tj, kj, k+前舌母音

#### そり舌音（Retroflexes）
- /r/ + 歯茎子音の同化で生成: /rt/→[ʈ], /rd/→[ɖ], /rn/→[ɳ], /rl/→[ɭ], /rs/→[ʂ]
- **語境界を越えて適用**される（sandhi）
- 南部方言（口蓋垂 /r/）では非適用

### 2.4 IPA音素インベントリ（推奨enum設計用）

**子音音素（18＋そり舌5）**: p, b, t, d, k, ɡ, f, v, s, h, m, n, ŋ, l, r, j, ɕ, ɧ + ʈ, ɖ, ɳ, ɭ, ʂ

**母音音素（18）**: iː, ɪ, yː, ʏ, ʉː, ɵ, uː, ʊ, eː, ɛ, øː, œ, ɛː, oː, ɔ, ɑː, a + ə（弱化母音、任意）

**超分節素**: ˈ（一次ストレス）, ˌ（二次ストレス）, ː（長音）, accent 1/2

---

## 3. 正書法とG2P規則

### 3.1 アルファベット（29文字）

a-z + å, ä, ö。**軟母音**: e, i, y, ä, ö（k, g, sk の発音に影響）。**硬母音**: a, o, u, å。

### 3.2 子音軟化規則（最重要）

| 子音 | 硬母音の前 | 軟母音の前 | 例 |
|------|-----------|-----------|-----|
| k | /k/ | /ɕ/ | katt /kat:/ vs köpa /ɕøːpa/ |
| g | /ɡ/ | /j/ | gata /ɡɑːta/ vs göra /jøːra/ |
| sk | /sk/ | /ɧ/ | skola /skuːla/ vs sked /ɧeːd/ |

### 3.3 sj音 `/ɧ/` の綴りパターン

| 綴り | 条件 | 例 |
|------|------|-----|
| sj | 常に | sjuk, sjö |
| skj | 常に | skjorta, skjuta |
| stj | 常に | stjärna, stjäla |
| sk + 軟母音 | 軟母音前 | sked, skina, skön |
| sch | 常に | schema |
| sh | 外来語 | show, shopping |
| ch | 一部外来語 | chef, chans |
| -tion/-sion | 語尾 | station, mission |
| ge/gi | フランス語由来 | garage, geni |

### 3.4 黙字規則

| 綴り | 発音 | 例 |
|------|------|-----|
| dj- | /j/ | djur（動物）, djup（深い） |
| gj- | /j/ | gjord（作った） |
| hj- | /j/ | hjärta（心臓）, hjälp（助け） |
| lj- | /j/ | ljus（光）, ljud（音） |

### 3.5 そり舌化パターン

| 正書法 | IPA | 例 |
|--------|------|-----|
| rt | [ʈ] | hjort [juːʈ] |
| rd | [ɖ] | bord [buːɖ] |
| rn | [ɳ] | barn [bɑːɳ] |
| rl | [ɭ] | Karl [kɑːɭ] |
| rs | [ʂ] | fors [fɔʂː] |

### 3.6 機能語の不規則発音

| 語 | 正書法 | 実際の発音 |
|----|--------|-----------|
| och | och | /ɔ/（chは黙字） |
| det | det | /deː/（tは黙字） |
| de/dem | de/dem | /dɔm/（完全不規則） |
| mig/dig/sig | -ig | /ɛj/ |
| jag | jag | /jɑː/（gは弱化） |

---

## 4. ピッチアクセント

### 4.1 Accent 1 vs Accent 2

| 特徴 | Accent 1 (acute) | Accent 2 (grave) |
|------|------------------|-------------------|
| ストレス音節冒頭 | 低い（L） | 高い（H） |
| f0パターン | 単峰 LHL | 双峰 HLHL |
| 出現条件 | デフォルト | 特定接尾辞が誘発 |
| 単音節語 | 常にAccent 1 | 不可能 |
| 複合語 | - | 常にAccent 2 |
| 外来語 | 常にAccent 1 | - |

### 4.2 最小対語の例（約300対）

| Accent 1 | 意味 | Accent 2 | 意味 |
|----------|------|----------|------|
| anden | そのアヒル | anden | その精霊 |
| tomten | その敷地 | tomten | サンタクロース |
| buren | そのかご | buren | 運ばれた |

### 4.3 アクセント予測規則

**Accent 2 を誘発する接尾辞**: -ar（複数）, -te/-de（過去形）, -het（派生名詞）, -are（"-er"）, -or（複数）

**Accent 1（デフォルト）**: -(e)n（定冠詞単数）, -(e)r（現在形）, 単音節語, 外来語

**形態論的原理**: 「語アクセントはほぼ完全に冗長であり、ストレスパターンと接尾辞情報から導出可能」（Roll et al. 2022）

### 4.4 Prosody API設計（推奨）

| フィールド | 用途 | 値 |
|-----------|------|-----|
| A1 | ピッチアクセント番号 | 1 = accent 1, 2 = accent 2, 0 = 不明 |
| A2 | ストレスレベル | 0 = なし, 1 = primary, 2 = secondary |
| A3 | 語の音節数 | 正整数 |

---

## 5. 方言と変種

### 5.1 方言間差異サマリー

| 特徴 | Central（標準） | Finland Swedish | Scanian（南部） |
|------|---------|---------|---------|
| ピッチアクセント | 2-peaked対立 | **なし** | 1-peaked |
| そり舌音 | あり | **なし** | **なし** |
| /r/ の実現 | 歯茎 [r] | 歯茎 [r] | **口蓋垂 [ʀ]** |
| sj音 | [xʷ]～[ɧ] | [ʃ]～[ɕ] | 軟口蓋的 |
| tj音 | [ɕ] | **[t͡ɕ]** | [ɕ] |
| 帯気 | あり | **なし** | あり |

### 5.2 推奨Dialect enum

```csharp
public enum SwedishDialect : byte
{
    /// <summary>中央標準スウェーデン語（rikssvenska）。デフォルト。</summary>
    Central = 0,
    
    /// <summary>フィンランド・スウェーデン語（finlandssvenska）。
    /// そり舌音なし、ピッチアクセントなし、帯気なし。</summary>
    FinlandSwedish = 1,
}
```

**Central (= 0) がデフォルト**: TTS学習データの大半が標準語。他言語パッケージと同一パターン。

**FinlandSwedish を初期代替方言とする理由**: 差異が「あり/なし」の二項対立で実装しやすい。BCP 47 `sv-FI` として確立。

---

## 6. OSSツール・ライブラリ調査

### 6.1 ツール比較

| ツール | 方式 | スウェーデン語 | ライセンス | 精度 | C#実装可能性 |
|--------|------|--------------|-----------|------|-------------|
| espeak-ng | ルールベース | 対応（400+規則） | GPL-3.0 | 中（一部不正確） | 規則参考可 |
| Phonetisaurus | WFST統計 | FST存在 | BSD | 中〜高 | 困難 |
| CharsiuG2P | Transformer (ByT5) | 対応（100言語中） | Apache-2.0 | PER 8.9%（平均） | 困難（Python） |
| epitran | マッピング＋規則 | **非対応** | MIT | -- | -- |
| Piper TTS | espeak-ng依存 | 対応（nst, lisa） | MIT | espeak-ng準拠 | 参考のみ |

### 6.2 espeak-ng スウェーデン語規則の分析

- `dictsource/sv_rules`: 823行、約400-450規則。29文字グループに整理
- `dictsource/sv_list`: 約1,040例外エントリ（機能語77、外来語94、地名49、人名86）
- `phsource/ph_swedish`: 母音18種＋固有子音3種（sx=ɧ, t.=ʈ, d.=ɖ）
- **限界**: ピッチアクセント（accent 1/2）の語彙レベル区別が未実装。KBLabにより品質課題が報告

### 6.3 Piper TTS / piper-plus

- piper-plusがスウェーデン語を言語コード7としてサポート済み
- モデル: sv_SE-nst-medium（NST 5,300録音）, sv_SE-lisa-medium
- PUAマッピング: スウェーデン語は多文字IPA音素が少なく追加最小限

### 6.4 発音辞書リソース

| リソース | エントリ数 | ライセンス | 形式 |
|---------|-----------|-----------|------|
| NST Lexicon (OpenSLR 29) | 822,000語 | CC0 | SAMPA |
| Folkets lexikon (KTH) | ~54,000語 | CC BY-SA 2.5 | XML |
| SALDO | 131,000語彙 | CC-BY-4.0 | XML（発音なし） |
| ipa-dict | 21,107語 | CC BY-SA 2.5 | IPA |
| WikiPron | ~4,631語 | Apache-2.0 | IPA |

---

## 7. 学術論文・文献調査

### 7.1 主要文献

| 文献 | 内容 | 重要度 |
|------|------|--------|
| Riad (2014) "The Phonology of Swedish" | 最も包括的な音韻論。ストレス・トーン・形態音韻を体系記述 | 必読 |
| Bruce (1977) "Swedish Word Accents" | ピッチアクセント研究の基盤。自己分節的分析 | 必読 |
| Roll et al. (2022) Frontiers in Psychology | アクセントの予測可能性を実証。接尾辞ベースの規則性 | 重要 |
| Zhu et al. (2022) CharsiuG2P | ByT5ベース100言語G2P。PER 8.9%（全言語平均） | 参考 |

### 7.2 多言語G2P研究でのスウェーデン語

- **CharsiuG2P**: Sprakbankenデータで学習、100言語サポート
- **Peters et al. (2017)**: 数百言語共有seq2seq。低リソース言語で7.2% PER改善
- **SIGMORPHON 2022**: スウェーデン語→ノルウェー語クロスリンガルタスクあり（WER 45%）

### 7.3 ルールベースG2Pの最新知見

"Fast, Not Fancy" (2025) 論文: espeak系ルールベースはニューラルの53倍高速。浅い正書法言語ではルールベースが十分な精度を達成可能。

---

## 8. 評価データセット

### 8.1 利用可能なデータセット（優先順）

| データセット | エントリ数 | ライセンス | 用途 |
|-------------|-----------|-----------|------|
| **ipa-dict sv.txt** | 21,107語 | CC BY-SA 2.5 | 最優先。評価パイプラインとの整合性最高 |
| **WikiPron swe_latn_broad.tsv** | 4,631語 | Apache-2.0 | 独立検証用 |
| **NST/OpenSLR 29** | 822,000語 | CC0 | 大規模評価。SAMPA→IPA変換必要 |

### 8.2 ipa-dict スウェーデン語の特徴

- ソース: Folkets lexikon（KTH）
- 声調アクセントマーク `²`（accent 2）を含む
- ストレスマーク `ˈ` 使用
- 長音記号 `ː`、sj音 `ɧ`、そり舌音 `ɳ, ɖ, ʂ` 等含有
- DotNetG2Pの他言語（スペイン語/フランス語/ポルトガル語）と同じソースで評価パイプライン整合性が最高

### 8.3 スウェーデン語固有の評価課題

- **声調アクセント**: ipa-dictは `²` マーク、WikiPronは `¹`/`²` マーク。評価時にアクセント含む/除く両方で計測
- **そり舌音**: 方言設定によりリファレンスとの一致が変わる
- **長短母音**: ストレス依存のため正確な評価にはストレス情報が必要

---

## 9. アプローチ比較と推奨設計

### 9.1 アプローチ別比較

| アプローチ | 典型PER | 速度 | サイズ | .NET適合性 | DotNetG2P実績 |
|-----------|---------|------|--------|-----------|--------------|
| **ルール＋例外辞書** | 1-3% | 最速 | 最小 | **最適** | スペイン語1.69% |
| 辞書＋CARTフォールバック | 5-7% | 高速 | 中 | 良好 | 英語5.26% |
| ニューラル（Transformer） | 2-5% | 低速 | 大 | 制約あり | なし |
| LLMベース | 2-4% | 最低速 | 最大 | 非現実的 | なし |

### 9.2 推奨: ルール＋例外辞書

**根拠**:
1. スウェーデン語は浅い正書法 → スペイン語（PER 1.69%）と類似の規則性
2. .NET Standard 2.1/Unity環境制約（ネイティブ依存排除）に最適
3. DotNetG2Pに7言語分のテンプレートが既存
4. espeak-ngの400+規則が実現可能性を実証
5. NST辞書（CC0, 822k語）が例外辞書ソースとして利用可能

**推定達成PER: 2-4%**

---

## 10. 既存実装パターンの分析

### 10.1 共通パイプライン構造（全ラテン文字言語共通）

```
Normalize → Tokenize → ExceptionDict → G2PRules → Syllabifier → StressAssigner → AllophoneProcessor → Format
```

### 10.2 共通ファイル構成

```
src/DotNetG2P.{Lang}/
├── {Lang}G2PEngine.cs          — メインAPI
├── {Lang}G2POptions.cs         — オプション
├── {Lang}AllophoneFeatures.cs  — 異音フラグ [Flags] enum
├── Models/
│   ├── {Lang}IpaPhoneme.cs     — byte基底enum
│   ├── {Lang}Phoneme.cs        — ストレス付き音素struct
│   ├── {Lang}Pronunciation.cs  — 発音情報
│   ├── {Lang}Dialect.cs        — 方言enum
│   ├── {Lang}Syllable.cs       — 音節struct
│   ├── {Lang}ProsodyInfo.cs    — 韻律情報
│   └── {Lang}ProsodyResult.cs  — 韻律結果
├── Rules/
│   ├── GraphemeToPhonemeRules.cs
│   ├── {Lang}Syllabifier.cs
│   ├── StressAssigner.cs
│   ├── AllophoneProcessor.cs
│   └── {Lang}Orthography.cs
├── Normalization/
│   ├── {Lang}Normalizer.cs
│   └── NumberToWords.cs
├── Conversion/
│   ├── IpaConverter.cs
│   ├── XSampaConverter.cs
│   ├── {Lang}PuaMapper.cs
│   └── FunctionWordList.cs
├── Data/
│   ├── {Lang}ExceptionDictionary.cs
│   └── {lang}_exceptions.master.tsv
└── Internal/
    ├── BatchConversionHelper.cs    — sync-shared-internals管理
    └── PreserveAttribute.cs        — sync-shared-internals管理
```

### 10.3 Public API標準パターン

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| ToPhonemes(text) | string | スペース区切り音素列 |
| ToIPA(text) | string | IPA文字列 |
| ToIPAWithoutStress(text) | string | ストレスなしIPA |
| ToXSampa(text) | string | X-SAMPA表記 |
| ToPhonemeList(text) | IReadOnlyList | 音素structリスト |
| ToPuaPhonemes(text) | string[] | PUA音素配列 |
| ToPuaString(text) | string | PUA文字列 |
| ToIpaWithProsody(text) | ProsodyResult | IPA＋韻律情報 |
| *Batch() | IReadOnlyList | 各メソッドのバッチ版 |

---

## 11. 推奨アーキテクチャ

### 11.1 処理パイプライン

```
入力テキスト
  ↓
1. SwedishNormalizer.Normalize()
   NFC正規化 → 小文字化 → 略語展開 → 序数展開 → 日付展開
   → 時刻展開 → 通貨展開 → 数字展開 → 記号展開
  ↓
2. SwedishNormalizer.Tokenize()
   単語境界で分割
  ↓
3. SwedishExceptionDictionary.TryLookup()
   例外辞書ルックアップ（300-600語）
  ↓
4. GraphemeToPhonemeRules.ConvertWord()
   Phase 1: トリグラフ/ダイグラフ認識（stj, skj, sj, sk+軟母音, tj, kj, ng, nk 等）
   Phase 2: 子音軟化（k→ɕ, g→j, sk→ɧ 前舌母音前）
   Phase 3: 母音変換（長短決定、相補的数量法則）
   Phase 4: そり舌化（rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ）
   Phase 5: 黙字処理（dj, gj, hj, lj → j）
  ↓
5. SwedishSyllabifier.Syllabify()
   Onset最大化原則による音節分割
  ↓
6. StressAssigner.MarkStress()
   Phase 1: デフォルト第1音節ストレス（ゲルマン語規則）
   Phase 2: 外来語接尾辞によるストレスシフト（-tion, -ell, -ent 等）
   Phase 3: ピッチアクセント付与（accent 1/2）
  ↓
7. AllophoneProcessor.Apply()
   方言別異音処理（そり舌化有無、帯気有無等）
  ↓
8. Format (IpaConverter / XSampaConverter / SwedishPuaMapper)
```

### 11.2 SwedishIpaPhoneme enum（推奨定義）

```csharp
public enum SwedishIpaPhoneme : byte
{
    // 長母音 (0-8)
    LongI = 0,    // iː
    LongY = 1,    // yː
    LongU_Central = 2,  // ʉː
    LongU = 3,    // uː
    LongE = 4,    // eː
    LongOe = 5,   // øː
    LongEh = 6,   // ɛː
    LongO = 7,    // oː
    LongA = 8,    // ɑː
    
    // 短母音 (9-17)
    ShortI = 9,   // ɪ
    ShortY = 10,  // ʏ
    ShortU_Central = 11,  // ɵ
    ShortU = 12,  // ʊ
    ShortE = 13,  // ɛ
    ShortOe = 14, // œ
    ShortO = 15,  // ɔ
    ShortA = 16,  // a
    Schwa = 17,   // ə (弱化母音、任意)
    
    // 破裂音 (18-23)
    P = 18, B = 19, T = 20, D = 21, K = 22, G = 23,
    
    // 摩擦音 (24-29)
    F = 24, V = 25, S = 26, H = 27, Sj = 28, Tj = 29,
    // Sj = ɧ (sj-sound), Tj = ɕ (tj-sound)
    
    // 鼻音 (30-32)
    M = 30, N = 31, Ng = 32,
    
    // 接近音・ふるえ音 (33-35)
    L = 33, R = 34, J = 35,
    
    // そり舌音 (36-40)
    RetroT = 36,  // ʈ
    RetroD = 37,  // ɖ
    RetroN = 38,  // ɳ
    RetroL = 39,  // ɭ
    RetroS = 40,  // ʂ
}
```

### 11.3 例外辞書TSV形式

```tsv
surface	dialect	category	accent	stress_index	phonemes	source	note
och	*	function_word	1	-1	ɔ	manual	ch黙字
chef	*	loanword_fr	1	0	ɧ eː f	manual	フランス語由来sj音
station	*	sj_exception	2	1	s t a|ɧ uː n	manual	-tion語尾
```

フィールド: surface / dialect(`*`, `central`, `finland`) / category / accent(1/2/`*`) / stress_index / phonemes(`|`で音節区切り) / source / note

### 11.4 テキスト正規化パイプライン

```
1. NFC正規化 + 小文字化
2. 略語展開（t.ex.→till exempel, dvs.→det vill säga, kl.→klockan 等）
3. 序数略記展開（1:a→första, 3:e→tredje）
4. ISO日付展開（YYYY-MM-DD）
5. 時刻展開（15:30 → femton trettio / halv fyra）
6. 通貨展開（kr→kronor, SEK, :-）
7. パーセント展開
8. 小数展開（カンマ区切り: 3,14→tre komma fjorton）
9. 数字展開（en/ett性区別対応）
10. 記号展開（@→snabel-a, &→och）
11. 空白正規化
```

---

## 12. ベンチマーク・評価戦略

### 12.1 PER計算方法

```
PER = Σ(Levenshtein距離) / Σ(リファレンス音素数)
```
DotNetG2Pの既存Evalツール（SpanishEval等）と同一手法。

### 12.2 評価プロファイル

| プロファイル | 設定 | 目的 |
|-------------|------|------|
| base | EnableAllophones=false | 基本音素精度 |
| allophones | EnableAllophones=true | 異音含む精度 |
| no_exceptions | EnableExceptionDictionary=false | 規則のみの精度 |

### 12.3 推奨PER閾値

> **注記**: 以下は調査時点の保守的な暫定目標値。最終目標値は[マイルストーン計画 付録A](swedish-g2p-milestones.md#a-per閾値一覧)を参照。

| データセット | base | allophones | no_exceptions |
|-------------|------|-----------|--------------|
| ipa-dict サンプル(256) | < 8% | < 6% | < 15% |
| ipa-dict フル | < 10% | < 8% | < 18% |
| WikiPron サンプル(256) | < 8% | < 6% | < 15% |
| WikiPron フル | < 10% | < 8% | < 18% |

### 12.4 評価ツール構成

```
tools/
├── DotNetG2P.SwedishEval/
│   └── Program.cs              — フル評価CLI
├── refresh_swedish_eval_data.ps1   — データDL・フィルタ・TSV生成
├── run_swedish_full_evaluation.ps1 — フル評価実行
└── swedish_eval_thresholds.json    — PER閾値設定
```

---

## 13. テスト戦略

### 13.1 推奨テスト構成

```
tests/DotNetG2P.Tests/SwedishG2P/
├── SwedishG2PEngineTests.cs           — エンジン基本機能 (10-15テスト)
├── SwedishAccuracyTests.cs            — キュレーション精度 (20-30テスト)
├── SwedishDatasetEvaluationTests.cs   — ipa-dict/WikiPron評価 (8-12テスト)
├── SwedishAllophoneEvaluationTests.cs — 異音プロファイル (3-4テスト)
├── SwedishNormalizerTests.cs          — テキスト正規化 (30-50テスト)
├── SwedishSyllabifierTests.cs         — 音節分割 (15-20テスト)
├── StressAssignerTests.cs             — ストレス付与 (10-15テスト)
├── GraphemeToPhonemeRulesTests.cs     — G2P規則 (30-50テスト)
├── SwedishOrthographyTests.cs         — 正書法 (10-15テスト)
├── SwedishExceptionDictionaryTests.cs — 例外辞書 (5-10テスト)
├── SwedishEdgeCaseTests.cs            — エッジケース (5-10テスト)
├── SwedishIpaTests.cs                 — IPA変換 (10-15テスト)
├── SwedishXSampaTests.cs              — X-SAMPA変換 (10-15テスト)
├── SwedishPhonemeTests.cs             — 音素struct (5-10テスト)
└── SwedishPerformanceTests.cs         — パフォーマンス (4-6テスト)

tests/TestData/SwedishG2P/
├── wikipron_swe_latn_broad_filtered_sample.tsv (256件)
├── ipa_dict_sv_se_sample.tsv (256件)
└── swedish_allophone_reference.tsv (10-20件)
```

**推定テスト総数: 200-300テスト**

### 13.2 テストデータ取得先

- **ipa-dict**: `https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/sv.txt`
- **WikiPron**: `https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/swe_latn_broad.tsv`

---

## 14. Multilingual統合計画

### 14.1 必要な変更箇所

| ファイル | 変更 |
|---------|------|
| Language.cs | `Swedish = 7` 追加 |
| MultilingualG2PEngine.cs | `Lazy<SwedishG2PEngine>` 追加 |
| MultilingualG2POptions.cs | `SwedishG2POptions?` プロパティ追加 |
| TextSegmenter.cs | `LangSwedish` byte定数 + 言語判定ロジック追加 |
| CapabilityAdapters.cs | スウェーデン語エンジン登録 |
| DotNetG2P.Multilingual.csproj | ProjectReference追加 |
| package.json (UPM) | `com.dotnetg2p.swedish` 依存追加 |

### 14.2 言語判定シグナル

```csharp
// スウェーデン語固有文字の検出
ContainsExplicitSwedishCharacter(token) → å のみ（ä, öはドイツ語等と共有）

// ASCII語のヒューリスティクス
s_swedishWordSignals = { "och", "att", "hej", "tack", "hur", "dag", "kväll" }
s_swedishSuffixSignals = { "tion", "ighet", "ning", "skap", "lig" }
```

---

## 15. マイルストーン案

### Sw1: 基盤構築（規則ベースG2P + 基本テスト）

- プロジェクト骨格: csproj, asmdef, package.json, Internal/
- SwedishIpaPhoneme enum（~41音素）
- GraphemeToPhonemeRules（5フェーズ）
- SwedishSyllabifier（Onset最大化）
- StressAssigner（第1音節デフォルト + 外来語接尾辞）
- IpaConverter, XSampaConverter
- 基本テスト100+
- **目標PER**: < 15%（no_exceptions）

### Sw2: 例外辞書 + テキスト正規化

- SwedishExceptionDictionary（300+語: 外来語, sj音例外, 機能語）
- SwedishNormalizer（11段階パイプライン）
- NumberToWords（en/ett性区別対応）
- FunctionWordList
- テスト追加（正規化30+, 辞書10+）
- **目標PER**: < 8%（base with exceptions）

### Sw3: ピッチアクセント + 方言対応

- ピッチアクセント予測（接尾辞規則 + 複合語検出）
- SwedishDialect enum（Central / FinlandSwedish）
- AllophoneProcessor（そり舌化有無, 帯気有無）
- SwedishProsodyInfo / SwedishProsodyResult
- ToIpaWithProsody() API
- テスト追加（方言テスト, Prosodyテスト）
- **目標PER**: < 4%（base）

### Sw4: Multilingual統合 + 評価ツール

- Language.Swedish 統合
- TextSegmenter スウェーデン語判定
- SwedishPuaMapper（piper-plus互換）
- tools/DotNetG2P.SwedishEval
- tools/refresh_swedish_eval_data.ps1
- ipa-dict / WikiPron フル評価
- MultilingualSwedishTests
- **最終目標PER**: < 4%（base）, < 3%（allophones）

---

## 参考文献

### 音韻論・言語学
- Riad, T. (2014). *The Phonology of Swedish*. Oxford University Press.
- Bruce, G. (1977). *Swedish Word Accents in Sentence Perspective*. Gleerups.
- Roll, M. et al. (2022). "The predictive function of Swedish word accents." *Frontiers in Psychology*.

### G2Pシステム
- espeak-ng. https://github.com/espeak-ng/espeak-ng （GPL-3.0）
- Piper TTS. https://github.com/rhasspy/piper
- KBLab (2023). "Swedish speech synthesis." https://kb-labb.github.io/posts/2023-05-24-swedish-text-to-speech/
- Zhu, J. et al. (2022). "Charsiu G2P." *Interspeech 2022*.

### データセット
- ipa-dict. https://github.com/open-dict-data/ipa-dict （CC BY-SA 2.5）
- WikiPron. https://github.com/CUNY-CL/wikipron （Apache-2.0）
- NST Pronunciation Lexicon. https://www.openslr.org/29/ （CC0）
- Folkets lexikon. https://folkets-lexikon.csc.kth.se/ （CC BY-SA 2.5）

### SAMPA
- SAMPA for Swedish. https://www.phon.ucl.ac.uk/home/sampa/swedish.htm
- Amazon Polly Swedish phoneme table. https://docs.aws.amazon.com/polly/latest/dg/ph-table-swedish.html
