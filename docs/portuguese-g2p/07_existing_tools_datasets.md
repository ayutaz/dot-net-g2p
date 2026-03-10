# 既存のポルトガル語G2Pツール・データセット調査

## 1. ipa-dict ポルトガル語データ

### 概要
- リポジトリ: https://github.com/open-dict-data/ipa-dict
- ポルトガル語データファイル: `data/pt_BR.txt`（ブラジルポルトガル語のみ）
- ヨーロッパポルトガル語（pt_PT）のデータは**存在しない**
- ダウンロードURL: `https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/pt_BR.txt`
- フォーマット: TSV（単語\tIPA転写）、1行1エントリ

### 特徴
- ブラジルポルトガル語のみカバー（EP非対応）
- IPA転写はルールベース生成（espeak-ng由来の可能性が高い）
- スペイン語（es_ES, es_MX）やフランス語（fr.txt）と同じ構造
- 既存プロジェクトのスペイン語/フランス語評価と同じパイプラインで利用可能

### DotNetG2Pでの利用方針
- PER回帰テスト用のベースコーパスとして利用可能
- スペイン語/フランス語と同様に `refresh_portuguese_eval_data.ps1` で取得・フィルタリング
- EP非対応のため、EP評価には別データセット（WikiPron等）が必要

---

## 2. WikiPron ポルトガル語データ

### 概要
- リポジトリ: https://github.com/CUNY-CL/wikipron
- Wiktionaryから自動抽出したIPA発音データ

### 利用可能ファイル（data/scrape/tsv/）
| ファイル名 | 変種 | 転写タイプ | 備考 |
|---|---|---|---|
| `por_latn_bz_broad.tsv` | ブラジル (BP) | broad | 全データ |
| `por_latn_bz_broad_filtered.tsv` | ブラジル (BP) | broad | フィルタ済み |
| `por_latn_bz_narrow.tsv` | ブラジル (BP) | narrow | 詳細転写 |
| `por_latn_po_broad.tsv` | ヨーロッパ (EP) | broad | 全データ |
| `por_latn_po_broad_filtered.tsv` | ヨーロッパ (EP) | broad | フィルタ済み |
| `por_latn_po_narrow.tsv` | ヨーロッパ (EP) | narrow | 詳細転写 |

### 特徴
- **BP/EP両方のデータあり** -- ipa-dictと異なりEPもカバー
- broad（音素）とnarrow（異音含む）の2種類
- filtered版はノイズ除去済みで品質が高い
- Wiktionary由来のため人手検証されたデータが多い
- フォーマット: TSV（単語\tIPA転写）

### DotNetG2Pでの利用方針
- `por_latn_bz_broad_filtered.tsv`: BP方言のPER評価に最適
- `por_latn_po_broad_filtered.tsv`: EP方言のPER評価に最適
- broad版をメインの回帰テストに使用（narrow版は異音レベルの音声学的詳細を含むため、broad版との表記体系差が大きく直接的なPER比較は困難。AllophoneProcessorの定性的検証に限定して使用する）
- スペイン語のCastilian/LatinAmericanと同様のBP/EP方言別評価が可能

---

## 3. espeak-ng のポルトガル語G2P

### 概要
- リポジトリ: https://github.com/espeak-ng/espeak-ng
- ポルトガル語関連ファイル（`dictsource/`内）:
  - `pt_rules` -- 正書法→音素変換ルール（メインファイル）
  - `pt_list` -- 例外語リスト（不規則語・頻出語の直接マッピング）
  - `pt_emoji` -- 絵文字読み上げ定義

### ルールベースの構造
- espeak-ng標準のルールファイル形式を使用
- `.group`ディレクティブで各文字/文字列グループのルールを定義
- `.L##`ディレクティブで文字グループ（母音群、子音群等）を定義
- ルールはコンテキストマッチング（左文字列_対象文字列_右文字列）形式
- 最もマッチスコアの高いルールが適用される

### 既知の課題
- 開/閉母音（e/E、o/O）の曖昧性解決が不完全
  - アクセント記号なしの場合、例外辞書（pt_list）に依存
  - ルールだけでは正確な母音品質の判定が困難
- MBrola音声との互換性に調整が必要な箇所あり（Issue #1054: MBrola diphone の未知ペア生成問題。BP用MBrolaルールが未知のdiphone組み合わせを出力するため、音声合成時にエラーが発生する）
- BP中心の実装で、EPとの差異対応が限定的

### DotNetG2Pへの示唆
- ルール構造はスペイン語G2P（`GraphemeToPhonemeRules.cs`）と類似のswitch文ベースで実装可能
- 開/閉母音の曖昧性は例外辞書（`portuguese_exceptions.master.tsv`）で対応する方針が有効
- espeak-ngのルールファイルは参考になるが、C#実装では独自の最適化されたルールエンジンを構築するのが良い

---

## 4. その他のオープンソースポルトガル語G2Pツール

### 4.1 phonemizer
- リポジトリ: https://github.com/bootphon/phonemizer
- Python製、espeak-ng/festival/segmentsバックエンド
- `phonemize("texto", language="pt-br")` でBPのIPA出力
- 100+言語対応、ポルトガル語はespeak-ngバックエンドで動作
- **ルールベース（espeak-ng依存）** -- 独自ルールなし

### 4.2 XphoneBR
- リポジトリ: https://github.com/traderpedroso/xphoneBR
- **ブラジルポルトガル語専用**のTransformerベースG2P
- DeepPhonemizerをベースに改良
- Forward Transformer（CTC）とAutoregressive版の2モデル
- Python 3.7-3.11対応
- 具体的な精度メトリクス（PER等）はリポジトリ上で公開されていない
- **ニューラルモデル** -- DotNetG2Pのルールベースアプローチとは異なるが、精度比較の参考に

### 4.3 neural-g2p-portuguese
- リポジトリ: https://github.com/fabianoluzbr/neural-g2p-portuguese
- Portal da Lingua Portuguesa由来の発音データで訓練
- Seq2SeqモデルベースのニューラルG2P
- **BP専用**

### 4.4 gruut
- リポジトリ: https://github.com/rhasspy/gruut
- Python製、CRFモデルベースのG2P
- ポルトガル語対応あり（辞書+G2Pモデル）
- espeak-ng由来のIPA辞書を含む
- **機械学習ベース（CRF）** -- ルールベースではない
- **注: 2025年10月アーカイブ済み（rhasspyプロジェクト全体が同日にアーカイブ）。メンテナンスは終了しているが、既存のモデルは利用可能**

### 4.5 CharsiuG2P
- リポジトリ: https://github.com/lingjzhu/CharsiuG2P
- ByT5ベースの100言語対応多言語G2Pモデル
- ポルトガル語を含む
- **ニューラルモデル** -- PER比較用ベースラインとして参考に

### 4.6 FalaBrasil (UFPA)
- リポジトリ: https://gitlab.com/falabrasil （GitLabに移行済み。旧GitHubリポジトリ https://github.com/falabrasil はレガシー）
- ブラジル・パラ連邦大学のFalaBrasil研究グループ
- Java製のルールベースG2Pコンバータ提供
- 音節分割システムも含む
- 200,000語のブラジルポルトガル語音声辞書を構築
- **ルールベース（Java）** -- C#実装の参考になる可能性

### 4.7 DeepPhonemizer
- リポジトリ: https://github.com/spring-media/DeepPhonemizer
- Transformerベースの多言語G2P
- ポルトガル語モデルあり
- Rust実装（deepphonemizer-rs）も存在
- **ニューラルモデル**

### 4.8 multilingual-g2p
- リポジトリ: https://github.com/jcsilva/multilingual-g2p
- espeak-ngベースの多言語G2P
- デフォルト設定がブラジルポルトガル語
- **espeak-ng依存**

### 4.9 Portal da Lingua Portuguesa
- URL: http://www.portaldalinguaportuguesa.org/
- ポルトガル語の公式正書法リソース
- 音声辞書（phonetic dictionary）をオンラインで提供
- **ヨーロッパポルトガル語**の正式発音データ
- ダウンロード可能な形式での提供は限定的（Webインターフェース中心）
- バルクダウンロードは提供されていないため、PER評価コーパスとしてではなく参考リソースとして位置づける

### 4.10 Epitran
- リポジトリ: https://github.com/dmort27/epitran
- Python製、61言語対応のルールベースG2Pツール
- ポルトガル語は `por-Latn` コードでサポート
- mapping-and-repairs アプローチ: 正書法→IPA のルールマッピングと後処理修正の2段階
- ACL 2018 (LREC) 論文 "Epitran: Precision G2P for Many Languages" で発表
- **ルールベース** -- DotNetG2Pと同じルールベースパラダイムに属し、PER比較のベースラインとして有用

### 4.11 g2p-decision-trees
- リポジトリ: https://github.com/cassiotbatista/g2p-decision-trees
- FalaBrasil (UFPA) の研究者による決定木ベースのブラジルポルトガル語G2P
- Scikit-Learn使用
- **機械学習ベース（決定木）** -- ルールベースではないが、FalaBrasil関連ツールとして参考に

### 4.12 g2p (NRC-ILT)
- リポジトリ: https://github.com/roedoejet/g2p
- カナダ国立研究会議（NRC）のルールベースG2Pフレームワーク
- インデックス保持のG2P変換を提供、MIT License
- カスタム言語の追加が容易なフレームワーク設計
- ポルトガル語の直接サポートは未確認だが、カスタム言語定義で対応可能
- **ルールベースフレームワーク** -- 言語追加の設計パターンとして参考に

---

## 5. 精度評価用ベンチマークデータセット候補

### 既存プロジェクトの評価ツール構成（参考）

スペイン語/フランス語では以下の構成で評価を実施:

| 項目 | スペイン語 | フランス語 |
|---|---|---|
| 評価スクリプト | `tools/refresh_spanish_eval_data.ps1` | `tools/refresh_french_eval_data.ps1` |
| 全量評価 | `tools/run_spanish_full_evaluation.ps1` | `tools/run_french_full_evaluation.ps1` |
| 閾値定義 | `tools/spanish_eval_thresholds.json` | `tools/french_eval_thresholds.json` |
| 評価ツール | `tools/DotNetG2P.SpanishEval/` | `tools/DotNetG2P.FrenchEval/` |
| サンプルデータ | `tests/TestData/SpanishG2P/` | `tests/TestData/FrenchG2P/` |
| 全量データ | `artifacts/spanish-eval/corpora/` | `artifacts/french-eval/corpora/` |
| キャッシュ | `.cache/spanish-eval/` | `.cache/french-eval/` |
| データセット数 | 4（ipa-dict x2, WikiPron x2） | 2（ipa-dict x1, WikiPron x1） |

### ポルトガル語向けベンチマークデータセット提案

| # | データセット | ソース | 方言 | 用途 | 優先度 |
|---|---|---|---|---|---|
| 1 | `ipa_dict_pt_br` | ipa-dict `pt_BR.txt` | BP | PER回帰テスト（BP） | 高 |
| 2 | `wikipron_por_latn_bz_broad_filtered` | WikiPron | BP | PER回帰テスト（BP） | 高 |
| 3 | `wikipron_por_latn_po_broad_filtered` | WikiPron | EP | PER回帰テスト（EP） | 高 |
| 4 | `wikipron_por_latn_bz_narrow` | WikiPron | BP | AllophoneProcessor精度評価用（narrow転写は異音レベルの音声学的詳細を含むが、broad版との表記体系差が大きく直接比較は困難。異音規則の定性的検証に限定して使用） | 低 |
| 5 | `wikipron_por_latn_po_narrow` | WikiPron | EP | AllophoneProcessor精度評価用（同上） | 低 |

### データセットURL

```
# ipa-dict (BP)
https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/pt_BR.txt

# WikiPron (BP broad filtered)
https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_bz_broad_filtered.tsv

# WikiPron (EP broad filtered)
https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_po_broad_filtered.tsv

# WikiPron (BP narrow)
https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_bz_narrow.tsv

# WikiPron (EP narrow)
https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/por_latn_po_narrow.tsv
```

### PER閾値の目安

スペイン語/フランス語の実績値を参考にすると:

| 言語 | データセット | 全量実測PER (base) | 全量実測PER (allophones) | 回帰テスト閾値 (base) |
|---|---|---|---|---|
| スペイン語 | ipa-dict es_ES/es_MX | 1.69% | 1.37% | 12% (0.12) |
| スペイン語 | WikiPron CA/LA | 1.38-1.43% | -- | 8% (0.08) |
| フランス語 | ipa-dict fr_FR | 12%未満（閾値内通過） | 12%未満（閾値内通過） | 12% (0.12) |
| フランス語 | WikiPron fra_latn | 12%未満（閾値内通過） | -- | 12% (0.12) |

**注: PERの2つの指標について**
- **全量実測PER**: 全コーパスに対するPER実測値（パーセント表記: 1.69%等）。精度の絶対的な指標
- **回帰テスト閾値**: `*_eval_thresholds.json` で設定される上限値（小数表記: 0.12 = 12%）。サンプルデータ（256-500件）での回帰テスト用で、実測PERより大幅に高い値に設定されている（サンプルサイズの分散を考慮）

ポルトガル語はスペイン語より開/閉母音の曖昧性が大きく、鼻母音化も複雑なため、初期目標としては:
- **base PER 目標**: 3-5%（ipa-dict BP、全量実測値）
- **allophones PER 目標**: 2-4%（全量実測値）
- **WikiPron PER 目標**: 3-5%（BP/EP各方言、全量実測値）
- **回帰テスト閾値**: 12-15% (0.12-0.15)（サンプルデータ用）

成熟度が上がれば2%以下を目指すことも可能だが、開/閉母音の予測精度が律速要因となる。

### 評価ツール構成案

```
tools/
  refresh_portuguese_eval_data.ps1    # データ取得・フィルタリング
  run_portuguese_full_evaluation.ps1  # 全量PER/WER評価
  portuguese_eval_thresholds.json     # PER閾値定義
  DotNetG2P.PortugueseEval/           # 評価コンソールアプリ

tests/TestData/PortugueseG2P/         # サンプルデータ（256-500件）
artifacts/portuguese-eval/corpora/    # 全量データ
.cache/portuguese-eval/               # ダウンロードキャッシュ
```

---

## 6. まとめ・DotNetG2P実装への示唆

### ルールベース実装の参考ツール
1. **espeak-ng** (`pt_rules`, `pt_list`) -- 最も直接的な参考。ルール構造は解析可能だがC#への直接移植は不適
2. **FalaBrasil G2P** (Java, GitLab) -- ルールベースで200,000語辞書構築実績あり。ルール設計の参考に
3. **Epitran** (Python) -- 61言語対応のルールベースG2P。`por-Latn` でポルトガル語サポート。DotNetG2Pと同じルールベースパラダイムであり、PER比較のベースラインとして最も直接的な比較対象

### 評価データセット
1. **ipa-dict** (`pt_BR.txt`) -- BP評価のメインコーパス
2. **WikiPron** (`por_latn_bz/po_broad_filtered.tsv`) -- BP/EP両方の評価に不可欠

### 精度ベースライン（比較対象）
- espeak-ng: ルールベース、開/閉母音に課題あり
- Epitran: ルールベース（mapping-and-repairs）、`por-Latn` 対応。DotNetG2Pと同じルールベースパラダイムでの最も直接的なPER比較対象
- CharsiuG2P: ニューラルモデル、多言語対応
- XphoneBR: BP専用Transformerモデル（精度メトリクス未公開のため、定量的比較には実測評価が必要）

### 課題認識
- **ipa-dictにEPデータなし** -- EP評価はWikiPronのみに依存
- **開/閉母音の曖昧性** -- ルールベースの限界。例外辞書の規模が精度に直結
- **鼻母音化の複雑さ** -- スペイン語より大幅に複雑。専用モジュールが必要
- **BP/EP方言差** -- 子音弱化、母音還元、sの実現等で大きな差異。方言パラメータ設計が重要
