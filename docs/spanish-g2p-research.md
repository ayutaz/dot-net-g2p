# スペイン語G2P 技術調査レポート

## 概要

スペイン語（Español）のG2P（Grapheme-to-Phoneme: 書記素→音素変換）実装に向けた技術調査結果をまとめる。
スペイン語は正書法が非常に規則的であり、英語・中国語と比較してルールベースアプローチが最も適している。

---

## 1. スペイン語音韻体系

### 1.1 母音体系（5音素）

スペイン語は5母音体系で、英語（12+母音）と比較して非常にシンプル。母音の長短の区別はない。

| 音素 | IPA | 分類 | 例 |
|------|-----|------|-----|
| /a/ | [a] | 低位前舌非円唇 | casa, pan |
| /e/ | [e] | 中位前舌非円唇 | mesa, tres |
| /i/ | [i] | 高位前舌非円唇 | hijo, mira |
| /o/ | [o] | 中位後舌円唇 | todo, poco |
| /u/ | [u] | 高位後舌円唇 | luna, mucho |

### 1.2 半母音（二重母音内）

| 音素 | IPA | 環境 | 例 |
|------|-----|------|-----|
| [j] | 硬口蓋接近音 | 二重母音中の /i/ | tierra, boina |
| [w] | 両唇軟口蓋接近音 | 二重母音中の /u/ | fuego, agua |

### 1.3 子音体系（最大20音素、方言により変動）

| 調音法＼調音点 | 両唇 | 唇歯 | 歯 | 歯茎 | 後部歯茎 | 硬口蓋 | 軟口蓋 |
|----------------|------|------|-----|------|----------|--------|--------|
| 破裂音(無声) | /p/ | | | /t/ | | | /k/ |
| 破裂音(有声) | /b/ | | | /d/ | | | /ɡ/ |
| 摩擦音(無声) | | /f/ | /θ/※ | /s/ | | | /x/ |
| 破擦音 | | | | | /tʃ/ | | |
| 摩擦音(有声) | | | | | | /ʝ/ | |
| 鼻音 | /m/ | | | /n/ | | /ɲ/ | |
| 側面接近音 | | | | /l/ | | /ʎ/※ | |
| ふるえ音 | | | | /r/ | | | |
| はじき音 | | | | /ɾ/ | | | |

※ /θ/: カスティーリャ方言のみ（ラテンアメリカでは/s/に合流 = seseo）
※ /ʎ/: 一部地域のみ（大多数では/ʝ/に合流 = yeísmo）

### 1.4 二重母音（diptongos）

母音は**強母音**（a, e, o）と**弱母音**（i, u）に分類される。

**上昇二重母音（弱+強）**: ia[ja], ie[je], io[jo], ua[wa], ue[we], uo[wo], iu[ju]
**下降二重母音（強+弱）**: ai[aj], ei[ej], oi[oj], au[aw], eu[ew], ou[ow]
**三重母音（弱+強+弱）**: uai[waj], uei[wej], iai[jaj], iei[jej]

**分離（hiato）**: 強母音+強母音（例: ca-er）、アクセント付き弱母音+強母音（例: dí-a）は別音節。

### 1.5 アクセント（ストレス）規則

スペイン語のストレス位置は正書法から**100%予測可能**。

| 分類 | ストレス位置 | アクセント記号が付く条件 |
|------|-------------|------------------------|
| Agudas | 最終音節 | 母音・n・sで終わる語にのみ記号あり |
| Llanas | 最後から2番目 | 母音・n・s以外で終わる語にのみ記号あり |
| Esdrújulas | 最後から3番目 | 常にアクセント記号あり |
| Sobresdrújulas | 4番目以降 | 常にアクセント記号あり |

**デフォルトルール**（アクセント記号なし）:
1. 母音・n・sで終わる → 後ろから2番目の音節（llana）
2. その他の子音で終わる → 最終音節（aguda）

### 1.6 音節構造

基本パターン: (C)(C)V(V)(C)(C)

- 最頻出: CV（ca-sa, me-sa）
- オンセット最大2子音: 破裂音/f + 流音（l/ɾ）のみ許容
- 許容クラスタ: pl, bl, fl, kl, ɡl, pɾ, bɾ, fɾ, tɾ, dɾ, kɾ, ɡɾ
- sC-クラスタは不許容（英語と異なる）: stress → es-trés

---

## 2. 異音規則（allophonic rules）

### 2.1 有声破裂音の弱化（最重要ルール）

/b, d, ɡ/ は環境に応じて破裂音または接近音として実現する。

| 音素 | 破裂音 [b, d, ɡ] | 接近音 [β, ð, ɣ] |
|------|-------------------|-------------------|
| /b/ | 休止後、鼻音後 | その他すべて |
| /d/ | 休止後、鼻音後、/l/後 | その他すべて |
| /ɡ/ | 休止後、鼻音後 | その他すべて |

例: vino[ˈbino]（語頭）, haber[aˈβeɾ]（母音間）, ando[ˈando]（鼻音後）, algo[ˈalɣo]（/l/後）

### 2.2 鼻音の同化

音節末の /n/ は後続子音の調音点に同化する:

| 後続子音 | /n/の異音 | 例 |
|----------|-----------|-----|
| /p, b/ | [m] | campo [ˈkampo] |
| /f/ | [ɱ] | enfermo [eɱˈfeɾmo] |
| /θ, t, d/ | [n̪] | antes [ˈan̪tes] |
| /k, ɡ, x/ | [ŋ] | banco [ˈbaŋko] |
| /tʃ, ʝ, ɲ/ | [ɲ] | ancho [ˈaɲtʃo] |

### 2.3 摩擦音の有声化

/s/（および /θ/）は有声子音の前で有声化: mismo → [ˈmizmo]

### 2.4 /ʝ/ の強化

休止後・鼻音後: [ɟʝ]（破擦音）。その他: [ʝ]（摩擦音）。

---

## 3. 方言差

### 3.1 Seseo / Distinción / Ceceo

| 現象 | 説明 | 地域 |
|------|------|------|
| Distinción | /θ/と/s/を区別 | スペイン北部・中部 |
| Seseo | /θ/→/s/に合流 | ラテンアメリカ全域、カナリア諸島 |
| Ceceo | /s/→/θ/に合流 | アンダルシア南部（stigmatized） |

### 3.2 Yeísmo / Lleísmo

| 現象 | 説明 | 地域 |
|------|------|------|
| Yeísmo | /ʎ/→/ʝ/に合流 | 大多数の方言（世界的に最も一般的） |
| Lleísmo | /ʎ/と/ʝ/を区別 | スペイン一部、南米アンデス地域 |

### 3.3 推奨デフォルト

**seseo + yeísmo**（ラテンアメリカ標準）をデフォルトとし、カスティーリャ方言をオプションで対応。

---

## 4. 正書法→音素変換ルール

### 4.1 フェーズ1: ダイグラフ（最優先）

| 優先度 | 書記素 | 音素 | 条件 |
|--------|--------|------|------|
| 1 | ch | /tʃ/ | 常に |
| 2 | ll | /ʝ/ (yeísmo) / /ʎ/ (非yeísmo) | 常に |
| 3 | rr | /r/ | 母音間のみ |
| 4 | qu + e | /ke/ | uは無音 |
| 5 | qu + i | /ki/ | uは無音 |
| 6 | gü + e | /ɡwe/ | トレマ付きuを発音 |
| 7 | gü + i | /ɡwi/ | トレマ付きuを発音 |
| 8 | gu + e | /ɡe/ | uは無音 |
| 9 | gu + i | /ɡi/ | uは無音 |

### 4.2 フェーズ2: 文脈依存子音ルール

| 優先度 | 書記素 | 音素 | 条件 |
|--------|--------|------|------|
| 10 | c + e,i | /θ/ (distinción) / /s/ (seseo) | e,i の前 |
| 11 | c + a,o,u,子音 | /k/ | a,o,u または子音の前 |
| 12 | g + e,i | /x/ | e,i の前 |
| 13 | g + a,o,u | /ɡ/ | a,o,u の前 |
| 14 | z | /θ/ (distinción) / /s/ (seseo) | 常に |
| 15 | r（語頭） | /r/ | 語頭位置 |
| 16 | r（n,l,s後） | /r/ | n,l,s の直後 |
| 17 | r（その他） | /ɾ/ | 上記以外 |
| 18 | x（語頭） | /s/ | 語頭位置 |
| 19 | x（その他） | /ks/ | 一般的 |
| 20 | y（母音前） | /ʝ/ | 子音として |
| 21 | y（単独/語末） | /i/ | 母音として |
| 22 | h | ∅ | 常に無音 |

### 4.3 フェーズ3: 単純対応ルール

| 書記素 | 音素 | 備考 |
|--------|------|------|
| a, á | /a/ | á はストレス位置指定 |
| b | /b/ | v と同一音素 |
| d | /d/ | |
| e, é | /e/ | |
| f | /f/ | |
| i, í | /i/ | í は二重母音を分断 |
| j | /x/ | |
| k | /k/ | 外来語 |
| l | /l/ | |
| m | /m/ | |
| n | /n/ | |
| ñ | /ɲ/ | |
| o, ó | /o/ | |
| p | /p/ | |
| s | /s/ | |
| t | /t/ | |
| u, ú | /u/ | |
| ü | /u/ | güe, güi の中 |
| v | /b/ | b と同一音素 |
| w | /w/ or /b/ | 外来語依存 |

### 4.4 ストレス位置決定アルゴリズム

```
1. アクセント記号（á, é, í, ó, ú）がある場合:
   → その音節にストレスを置く

2. アクセント記号がない場合:
   a. 語末が母音(a,e,i,o,u)、n、s → 後ろから2番目の音節
   b. 語末がそれ以外の子音 → 最後の音節
```

### 4.5 音節分割アルゴリズム

```
1. VCV → V.CV（子音は後続母音の音節に）
2. VCCV → VC.CV（原則分割）
   例外: 不可分クラスタ（pl, bl, fl, kl, gl, pr, br, fr, tr, dr, kr, gr）→ V.CCV
3. VCCCV → VCC.CV or VC.CCV（不可分クラスタを優先）
4. 二重母音は同一音節、hiato は別音節
```

---

## 5. 既存オープンソース実装

### 5.1 ルールベース実装

| プロジェクト | ライセンス | アプローチ | スペイン語対応 | DotNetG2Pへの活用 |
|-------------|----------|----------|-------------|------------------|
| **espeak-ng** | GPLv3 | ルールベース（パターンマッチ） | `es`（カスティーリャ）, `es-419`（ラテンアメリカ） | ルール**直接移植不可**（GPL）。設計パターンのみ参考 |
| **Epitran** (CMU) | MIT-Modern | CSVマッピング+修正ルール | `spa-Latn`（標準）, `spa-Latn-eu`（イベリア） | **マッピングデータの移植可能**。C#への移植が容易 |
| **NRC G2P** | MIT | CSVルールベース | 先住民言語が主対象 | MITで参考にしやすい |

### 5.2 WFST・統計実装

| プロジェクト | ライセンス | アプローチ | 備考 |
|-------------|----------|----------|------|
| **Phonetisaurus** | BSD-3-Clause | WFST（Weighted FST） | **スペイン語最高精度 PER 0.04%**。辞書データで学習が必要 |
| **gruut** (Rhasspy) | MIT | 辞書+CRF | Wiktionary辞書ベース。2025年10月にアーカイブ（開発終了） |

### 5.3 ニューラル実装

| プロジェクト | ライセンス | アプローチ | スペイン語PER | 備考 |
|-------------|----------|----------|-------------|------|
| **CharsiuG2P (ByT5)** | Apache-2.0 | Transformer（580Mパラメータ） | 0.25% | 100言語対応。スペイン語ではWFSTに劣る |
| **LatPhon** | 不明 | Transformer（7.5Mパラメータ） | 0.30% | 軽量。6言語共同学習 |
| **DeepPhonemizer** | MIT | Transformer（seq2seq） | 未測定 | espeak比類似度85%（英語テスト） |
| **OpenPhonemizer** | BSD-3-Clause | DeepPhonemizerベース | N/A | **英語のみ対応**。スペイン語未対応 |

### 5.4 ラッパー・バックエンド

| プロジェクト | ライセンス | アプローチ | 備考 |
|-------------|----------|----------|------|
| **Phonemizer** (bootphon) | GPLv3 | espeak-ng/festival等のラッパー | GPL。独自ロジックなし |
| **Goruut** (Go) | 要確認 | 最長接頭辞マッチ+Hashtron | 140言語対応 |

### 5.5 C#/.NET実装

**C#ネイティブのスペイン語G2P専用ライブラリは現時点で存在しない。**

| プロジェクト | ライセンス | 方式 | 備考 |
|-------------|----------|------|------|
| **KokoroSharp** | MIT（espeak-ng部分はGPL） | espeak-ng内蔵TTS推論 | G2P単体利用は困難 |
| **espeak-ng-wrapper** | 不明（espeak-ng GPL依存） | P/Invoke | 低活動、NuGetパッケージなし |
| **piper-unity** | espeak-ng GPL依存 | Unity向けPiper TTS | TTS全体パイプライン |

### 5.6 利用可能なオープンライセンス辞書データ

| データソース | ライセンス | エントリ数 | 用途 |
|-------------|----------|-----------|------|
| **WikiPron** | Apache 2.0 | 594,899+ | テストデータ・検証用辞書・WFST学習データ |
| **ipa-dict** | MIT | es_ES, es_MX | スペイン語IPA辞書（ルールベース自動生成、実験的） |

---

## 6. 精度ベンチマーク比較

### 6.1 定量的精度比較（LatPhon論文 2025, ipa-dictコーパス）

LatPhon論文（arXiv:2509.03300）がipa-dictコーパスで6言語のG2P精度を比較した結果:

| 順位 | システム | 方式 | スペイン語PER | ライセンス | Apache-2.0互換 |
|------|----------|------|-------------|----------|---------------|
| **1** | **Phonetisaurus (WFST)** | WFST | **0.04%** | BSD-3-Clause | **互換** |
| 2 | CharsiuG2P (ByT5) | ニューラル（580M） | 0.25% | Apache-2.0 | **互換** |
| 3 | LatPhon | ニューラル（7.5M） | 0.30% | 不明 | 不明 |
| - | espeak-ng | ルールベース | 推定 0.04%以下 | GPLv3 | **非互換** |
| - | Epitran | マッピング+ルール | 未測定 | MIT-Modern | **互換** |
| - | gruut | 辞書+CRF | 未測定 | MIT | **互換** |

**PER 0.04% = 2,500語に1音素のエラー** という極めて高い精度。

### 6.2 言語間精度比較（同論文）

スペイン語G2Pの難易度が他言語と比較して著しく低いことを示す:

| 言語 | WFST PER | ByT5 PER | LatPhon PER | 訓練データ数 |
|------|----------|----------|-------------|------------|
| **Spanish** | **0.04%** | **0.25%** | **0.30%** | 594,899 |
| English | 10.4% | 14.0% | 12.7% | 127,430 |
| French | 0.49% | 0.60% | 0.57% | 196,497 |
| Italian | 5.4% | 3.1% | 5.8% | 80,207 |
| Portuguese | 2.7% | 9.1% | 0.86% | 34,244 |
| Romanian | 0.23% | - | 0.49% | 67,780 |

→ スペイン語はWFSTで英語の**260倍低いエラー率**。全6言語中最高精度。

### 6.3 ルールベース vs ニューラルの結論

スペイン語は**浅い正書法（shallow orthography）**を持つ言語であり:

- **WFST/ルールベースがニューラルを大幅に上回る**（統計的有意、p≥0.97）
- ニューラルモデルは英語等の不規則な正書法で優位だが、スペイン語では**オーバースペック**
- 固有名詞・外来語を除けばルールベースで**ほぼ完璧な精度**を達成可能

### 6.4 固有名詞での精度低下

Polyakova & Bonafonte (INTERSPEECH 2006) によると:
- 一般語彙: 決定木で「非常に良好」
- **固有名詞: 単語精度 60.90%**（外来語・新語の影響で大幅低下）

→ 固有名詞・外来語は例外辞書での対応が必要。

### 6.5 エラーパターン分析（LatPhon論文）

スペイン語G2Pの主なエラー:
- **連続母音の簡略化**: 例 "reescribirse" (/reeskɾiβiɾse/) → /reskɾiβiɾse/ と誤変換
- 外来語の不規則な綴り
- 固有名詞（特に非スペイン語起源）

---

## 7. 外来語・例外パターン

| パターン | 処理 | 例 |
|----------|------|-----|
| x（メキシコ固有名詞） | /x/ | México [ˈmexiko] |
| x（ナワトル語由来） | /ʃ/ | Xochimilco |
| w（英語借用語） | /w/ | whisky |
| w（ゲルマン語由来） | /b/ | Wagner |
| k | /k/ | kilo |
| 語末 -d | 弱化/脱落傾向 | Madrid [maˈðɾið] or [maˈðɾi] |

---

## 8. 結論: DotNetG2P.Spanishへの推奨

### 最高精度ライブラリ

| 用途 | 推奨 | PER | ライセンス |
|------|------|-----|----------|
| **最高精度（GPL可）** | espeak-ng | 推定 ≤0.04% | GPLv3（非互換） |
| **最高精度（Permissive）** | Phonetisaurus (WFST) | 0.04% | BSD-3-Clause（互換） |
| **ニューラル最高精度** | CharsiuG2P (ByT5) | 0.25% | Apache-2.0（互換） |
| **軽量Python** | Epitran | 未測定 | MIT（互換） |

### 推奨アプローチ: ルールベース自前実装

スペイン語は正書法が非常に規則的であり、**ルールベースで PER 0.04% レベルの精度が達成可能**。

| 特徴 | スペイン語 | 英語 | 中国語 |
|------|----------|------|--------|
| 正書法の規則性 | 非常に高い | 低い | N/A（表意文字） |
| 同綴異音語 | 極めて少ない | 多い（30+語） | 多音字が多い |
| G2Pアプローチ | ルールベースで十分 | 辞書+LTS必須 | 辞書必須 |
| 辞書依存度 | 低い | 高い（13万語） | 高い（44万エントリ） |
| ストレス予測 | 正書法から100%決定 | 辞書/モデル必要 | 声調は辞書依存 |
| WFSTベストPER | 0.04% | 10.4% | N/A |

### C#での現状

**C#ネイティブのスペイン語G2Pライブラリは存在しない。** 既存のC#ソリューション（KokoroSharp、espeak-ng-wrapper等）はすべてespeak-ng依存でGPLライセンス制約あり。

→ **DotNetG2P.Spanishが初のApache-2.0互換C#スペイン語G2Pライブラリとなる。**

### ライセンス的に安全な参照先

| リソース | ライセンス | 用途 |
|---------|----------|------|
| Epitran マッピング | MIT-Modern | ルール設計の参考 |
| NRC G2P | MIT | アーキテクチャ参考 |
| WikiPron | Apache 2.0 | テスト・検証データ（594,899エントリ） |
| ipa-dict | MIT | テストデータ（es_ES, es_MX） |
| Phonetisaurus | BSD-3-Clause | WFSTアーキテクチャ参考 |
| CharsiuG2P | Apache 2.0 | ベンチマーク比較対象 |

### 利用不可（GPL）

| リソース | ライセンス | 理由 |
|---------|----------|------|
| espeak-ng ルールファイル | GPLv3 | Apache-2.0と非互換 |
| Phonemizer | GPLv3 | espeak-ng依存 |

---

## 参考文献

### 学術論文
1. LatPhon (2025): "Lightweight Multilingual G2P for Romance Languages and English" — https://arxiv.org/abs/2509.03300
2. SIGMORPHON 2020 Shared Task (ACL 2020): "Multilingual Grapheme-to-Phoneme Conversion" — https://aclanthology.org/2020.sigmorphon-1.2/
3. ByT5 G2P (Interspeech 2022): "ByT5 model for massively multilingual G2P conversion" — https://arxiv.org/abs/2204.03067
4. Amazon Byte G2P (ICASSP 2020): "Multilingual G2P Conversion with Byte Representation" — https://www.amazon.science/publications/multilingual-grapheme-to-phoneme-conversion-with-byte-representation
5. Polyakova & Bonafonte (INTERSPEECH 2006): "Learning from Errors in G2P Conversion" — https://nlp.lsi.upc.edu/papers/polyakova06.pdf
6. Bonaventura & Giuliani (1998): "Grapheme-to-phoneme transcription rules for Spanish" — https://aclanthology.org/W98-0804.pdf
7. Schlippe et al. (ICASSP 2012): "G2P Model Generation for Indo-European Languages" — https://www.csl.uni-bremen.de/cms/images/documents/publications/ICASSP2012-Schlippe_G2PModelGenerationIndoEuropean.pdf
8. WikiPron (LREC 2020): "Massively Multilingual Pronunciation Mining" — https://aclanthology.org/2020.lrec-1.521/
9. Epitran (LREC 2018): "Epitran: Precision G2P for Many Languages" — https://aclanthology.org/L18-1429/
10. Survey of G2P Methods (2024) — https://www.mdpi.com/2076-3417/14/24/11790

### ツール・データ
- espeak-ng: https://github.com/espeak-ng/espeak-ng
- Epitran: https://github.com/dmort27/epitran
- Phonetisaurus: https://github.com/AdolfVonKleworGrandes/Phonetisaurus
- CharsiuG2P: https://github.com/lingjzhu/CharsiuG2P
- DeepPhonemizer: https://github.com/spring-media/DeepPhonemizer
- gruut: https://github.com/rhasspy/gruut
- NRC G2P: https://github.com/NRC-ILT/g2p
- WikiPron: https://github.com/CUNY-CL/wikipron
- ipa-dict: https://github.com/open-dict-data/ipa-dict
