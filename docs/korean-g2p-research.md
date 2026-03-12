# 韓国語 G2P 調査メモ

更新日: 2026-03-12

## 目的

`dot-net-g2p` に韓国語 G2P を追加する前提で、10観点の並行調査結果を 1 つに統合したメモ。  
実装可否ではなく、「このリポジトリでどう作るのが妥当か」を決めるための資料として整理する。

## 結論

- 韓国語 G2P は `ルールベース + 例外辞書 + 将来 optional な形態素解析` がこのリポジトリに最も合う。
- MVP は `Hangul 入力中心` に絞るのが妥当。数字・英字・漢字・外来語の全面対応を最初から入れると正規化コストが急増する。
- 規範の一次ソースは国立国語院の発音規則と辞書系 API に置き、`g2pK` と `Kss` は参照実装として使うのが安全。
- `KoG2P` は参考にはなるが `GPL-3.0` のため、Apache-2.0 系の本リポジトリへコードを持ち込む前提では使わない方がよい。
- 多言語側の組み込みは比較的容易。Hangul は Unicode 上で日本語/中国語より判別しやすく、`TextSegmenter` への追加難度は低い。
- 既存ライブラリの実態としては、Python 側は `g2pK` 系譜が事実上の中心で、`Kss`, `g2pkk`, `g2pk2`, `g2pkiwi`, `kokorog2p`, `misaki` の Korean backend もかなりの部分を `g2pK` 系またはその派生に依存している。
- 一方で、2026-03-11 時点の手動調査では、C#/.NET には公開精度付きの専用 Korean G2P ライブラリを見つけられなかった。見つかったのは `Naramal` のような Hangul 処理ユーティリティで、G2P 本体ではない。

## 実装反映状況

2026-03-12 時点で、計画上の `M0` から `M3` までは実装済み。

### 現在の実装到達点

- `tests/TestData/KoreanG2P/` に `g2pk_parity`, `official_gold`, `weak_rules` の 3 系統 benchmark seed を配置済み
- `src/DotNetG2P.Korean` に Hangul-first の pure C# Korean core を追加済み
- `tests/DotNetG2P.Tests/KoreanG2P/Benchmarking/` に benchmark loader / evaluator / report writer を追加済み
- M2 の rule engine では以下を実装済み
  - 終声中和
  - 連音
  - 口蓋化付き resyllabification
  - 濃音化
  - 鼻音化
  - 流音化
  - `ㅎ` 系変化
    - 母音前脱落
    - 後続子音の激音化
    - 鼻音前処理
  - 一般化した `ㄴ` 添音
  - 子音前の `ㄼ` surface coda 処理

### 直近で埋めた M2 の欠落

以前の review で見つかった次の欠落は 2026-03-12 の修正で反映済み。

- `좋아`, `좋다`, `좋지`, `놓고`, `않다`, `싫어` を通す `ㅎ` 系変化
- `담요`, `검열`, `색연필`, `막일`, `한여름`, `솜이불` を通す一般化 `ㄴ` 添音
- whitespace を保持し、単語境界を跨いだ規則過剰適用を止める boundary-aware pipeline
- `밟다`, `밟고`, `밟는` を通す `ㄼ` 系の子音前処理
- seed benchmark 側の `ui-variation` 復帰と、複数許容発音を `|` で持てる評価形式

### M3 で追加した benchmark harness

- `g2pk_parity`, `official_gold`, `weak_rules` を一括評価する harness を追加
- rule tag 単位の pass/fail 集計を追加
- `tests/DotNetG2P.Tests/TestResults/KoreanG2P/` に以下の artifact を生成
  - `korean-benchmark-summary.json`
  - `korean-benchmark-dataset-summary.tsv`
  - `korean-benchmark-rule-summary.tsv`
  - `korean-benchmark-mismatches.tsv`
- 2026-03-12 時点の current seed 結果
  - `g2pk_parity`: `8/8`
  - `official_gold`: `15/15`
  - `weak_rules`: `14/14`
  - mismatch report は空

### 残る制約

- morphology なしのため、bare `이` 系の `ㄴ` 添音はまだヒューリスティック
- `ㄼ` 系 lexical variation は一般規則化し切っておらず、M4 の例外辞書でさらに補強が必要
- `의` 변이 は benchmark では監視しているが、規範上の許容形をどう public API で返すかはまだ固定していない
- 숫자, 英字, Hanja, descriptive mode は未着手

### 次に見るべきもの

- `M4`: 例外辞書と正規化層
- multilingual 統合前に `의` 변이 と exception policy を固定する

## 追加調査: 既存 Python / C# ライブラリと精度

### 10-agent 追加サマリ

| Agent | 担当 | 主要結論 |
|---|---|---|
| 1 | Python 地図 | 実用系は `g2pK` 派生が中心 |
| 2 | `g2pK` 調査 | 機能は厚いが、PyPI 更新は 2020-08-05 で止まっている |
| 3 | `KoG2P` 調査 | 単純で比較用に向くが GPL-3.0 |
| 4 | `Kss` 調査 | `g2pk` を取り込み拡張しているが、独立ベンチマークは見当たらない |
| 5 | fork 群調査 | `g2pkk`, `g2pk2`, `g2pkiwi` は互換・保守 fork として見るのが妥当 |
| 6 | multilingual 実装 | `kokorog2p` と `misaki` の Korean backend も `g2pK` 系を参照している |
| 7 | C#/.NET 調査 | 専用 Korean G2P は見つからず、Hangul utility 止まり |
| 8 | 公開精度収集 | 直接比較できる公開値は `g2pK` と `KoG2P` の論文比較が中心 |
| 9 | 歴史的ベースライン | 学術系では 97.5% sentence / 98.70% phoneme accuracy の歴史的報告がある |
| 10 | 実装示唆 | 新規 C# 実装は未開拓領域で、評価基盤の自前整備が重要 |

### ライブラリ比較表

| ライブラリ | 言語 | 現状 | 実装系譜 | 公開精度情報 |
|---|---|---|---|---|
| `g2pK` | Python | PyPI `0.9.4`、2020-08-05、Apache-2.0 | MeCab + rule-based + 数字/英字補助 | 2022年比較論文で平均 `0.700` |
| `KoG2P` | Python | GitHub release `v1.0`、2018-03-22、GPL-3.0 | pure rulebook 系 | 2022年比較論文で平均 `0.731` |
| `Kss` | Python | GitHub/PyPI `6.0.6`、2025-11-13、BSD-3-Clause | `g2pk` copied and modified | 独立した公開精度は未確認 |
| `g2pkk` | Python | PyPI `0.1.2`、2022-09-05、Apache-2.0 | `g2pK` の cross-platform 化 | 独立した公開精度は未確認 |
| `g2pk2` | Python | PyPI `0.0.3`、2023-08-18、Apache-2.0 | `g2pK` / `g2pkk` 系の更新 fork | 独立した公開精度は未確認 |
| `g2pkiwi` | Python | PyPI `0.1.0`、2022-10-26、Apache-2.0 | `g2pK` を `kiwipiepy` 化 | 独立した公開精度は未確認 |
| `kokorog2p` | Python | PyPI 公開中、Korean は `g2pK rule-based` と明記 | `g2pK` ベースの IPA 出力 | 独立した Korean 精度は未確認 |
| `misaki` | Python | PyPI `0.9.4`、2025-04-05、Apache-2.0 | Korean tokenizer は `g2pkc` / `g2pK` 系 | 独立した Korean 精度は未確認 |
| `Naramal` | C# | NuGet `1.0.1`、2020-07-31、MIT | Hangul 分解/結合・助詞処理 | G2P ではないため対象外 |
| 専用 Korean G2P for .NET | C# | 手動調査で未確認 | - | 公開精度なし |

### Python エコシステムの実態

#### `g2pK`

公開情報だけ見ると、依然として最重要の参照実装。  
GitHub では `266 stars`、PyPI は `0.9.4` が `2020-08-05` に公開されている。

特徴:

- `python-mecab-ko`, `konlpy`, `nltk`, `jamo` に依存
- 형태소 분석을使って文脈で発音を変える
- `descriptive`, `group_vowels`, `to_syl` を持つ
- 英字 to Hangul、数字読み分けにも対応

評価:

- 機能はもっとも厚い
- ただしメンテナンスは鈍い
- 現在の Python 韓国語 G2P は多くが `g2pK` を中心に派生している

#### `KoG2P`

`KoG2P` は軽量で、任意の Hangul 列に規則を適用できる基礎系。  
GitHub では `133 stars`、release は `2018-03-22`、ライセンスは `GPL-3.0`。

評価:

- ルールブック型で比較対象に向く
- 形態素解析や正規化の厚みでは `g2pK` に劣る
- ライセンス上、Apache-2.0 系の本 repo に取り込む対象としては不適

#### `Kss`

`Kss` の `g2p` は README 上で `This was copied from g2pk and modified by Kss` と明記されている。  
GitHub release は `v6.0.6` が `2025-11-13`、ライセンスは `BSD-3-Clause`。

評価:

- 実用上は有力
- ただし Korean G2P 単体としての独立評価値は見当たらない
- `g2pK` と別系統の精度とみなすより、`g2pK` 派生の運用パッケージとみる方が正確

#### `g2pkk`, `g2pk2`, `g2pkiwi`

この 3 つは `g2pK` 系の保守/移植 fork と理解するのがよい。

- `g2pkk`: Windows 対応を主目的にした cross-platform 版
- `g2pk2`: `g2pK` が長く更新されていない前提での updated fork
- `g2pkiwi`: 形態素解析 backend を `kiwipiepy` に変えた fork

重要なのは、いずれも「新しい独自 G2P モデル」ではなく、精度面では原系統の `g2pK` にかなり依存している点。

### multilingual / TTS 系ライブラリ

#### `kokorog2p`

公式 docs と PyPI では、Korean は `g2pK rule-based` と明記されている。  
さらに docs では `MeCab for morphological analysis and custom phonological rules based on Korean Standard Pronunciation` と説明されている。

評価:

- Korean backend を独自ベンチマークで売っているわけではない
- Kokoro 向けの IPA / phoneme inventory 変換レイヤとしては有用
- core の Korean 精度は基本的に `g2pK` 系の制約を引き継ぐとみるべき

#### `misaki`

`misaki` は TTS 向け G2P engine で、Korean tokenizer は `g2pkc fork of Kyubyong's widely used g2pK` をコピーしたと明記している。

評価:

- Korean backend は独立実装ではない
- これも `g2pK` 系譜の一部として扱うのが妥当

### C# / .NET エコシステム

2026-03-11 時点の手動調査では、GitHub の repository search で次がすべて `0 results` だった。

- `"korean" "g2p" language:C#`
- `"hangul" "phoneme" language:C#`
- `"hangul" "g2p" language:C#`

加えて NuGet では `Naramal` のような Hangul utility は見つかるが、README 上の説明は Hangul 分解/재조합と助詞処理であり、G2P ライブラリではない。

推定:

- 専用 Korean G2P for .NET は公開 OSS としてかなり空白
- `dot-net-g2p` がここに入る価値は十分ある

### 公開精度で比較できるもの

今回の調査で、現行 OSS ライブラリ同士を直接比較している公開ソースとして有用だったのは 2022 年の論文 1 本だった。

#### 2022 年比較論文

文性民ほか 2022 は、手作業の golden corpus を作り、`g2pK` と `KoG2P` を 13 の音韻規則で比較している。

平均値:

- `g2pK`: `0.700`
- `KoG2P`: `0.731`
- 論文著者の改善版: `0.938`

規則別の要点:

- 両者とも高い: 자음 중화, 자음군 단순화, 비음화, 유음화, 경음화, 재음절화
- 両者とも弱い: `ㅎ` 탈락 (`0.4`), `ㅎ,ㅌ` 중화 (`0.7`), `ㄴ` 첨가 (`0.3` / `0.5`)
- 両者とも未対応に近い: `의` 변이 (`0`), 자음 위치동화 (`0`)

この比較は現在でもかなり重要。  
理由は、韓国語 G2P の「どの規則が弱点になりやすいか」をライブラリ単位で可視化しているため。

#### 歴史的ベースライン

現行 OSS そのものではないが、韓国語 G2P の歴史的ベースラインとして次は参考になる。

- 1998年: 4,973 sentences で grapheme-to-phoneme `99.9%`、sentence conversion `97.5%`
- 2009年の sound-pattern rule-based system: phoneme accuracy `98.70%`

ただし、これらは評価条件もデータも現在の OSS 比較とは揃っていない。  
`g2pK` や `KoG2P` の数値と直接比較して「どちらが上」と言うのは不適切。

### 精度に関する実務判断

公開情報だけで順位付けするなら、こうなる。

1. **もっとも実戦向きな参照実装**: `g2pK`
2. **ライセンスを含めた参照候補**: `Kss`, `g2pkk`, `g2pk2`, `g2pkiwi`, `kokorog2p`
3. **比較用の純ルール系ベースライン**: `KoG2P`
4. **C# 側の直接再利用候補**: 実質なし

ただし、`Kss` や `kokorog2p` の Korean backend を `g2pK` と別物として精度比較するのは危険。  
ソース上の系譜が近すぎるため、独立モデル比較というより「同系統の実装差」と考えるべき。

### ベンチマーク対象の選定

`dot-net-g2p` で最初に置く主ベンチマークは `g2pK` がよい。

理由:

- Korean G2P 専用ライブラリとして最も参照実装らしい
- 形態素解析、数字、英字変換まで含むため、実戦投入時の振る舞いを比較しやすい
- `Kss` や `kokorog2p` は `g2pK` 系譜で独立性が弱い
- `KoG2P` は pure rule 系比較には有用だが、実用機能とライセンスで主 benchmark にしにくい

結論:

- **主 benchmark**: `g2pK`
- **副 benchmark**: `KoG2P`
- **gold oracle**: 国立国語院辞書発音 + 規則例

### 推奨ベンチマーク構成

`g2pK` を使う場合でも、「`g2pK` に一致したら正解」とはしない方がよい。  
論文上、`g2pK` 自体にも弱い規則があるため。

推奨構成:

1. **Benchmark A: g2pK parity set**
   - 対象: Hangul-only 単語・短句
   - 目的: 既存実装との互換性確認
   - 指標: exact match, rule-wise accuracy

2. **Benchmark B: official gold set**
   - 対象: 国立国語院辞書 API の `pronunciation`、Q&A の規則例
   - 目的: 規範準拠の確認
   - 指標: exact match, phoneme error analysis

3. **Benchmark C: weakness stress set**
   - 対象: `ㅎ` 탈락, `ㄴ` 첨가, `의` 변이, 자음 위치동화 など
   - 目的: `g2pK` 追従で埋もれやすい弱点の監視
   - 指標: rule-wise pass rate

運用ルール:

- `g2pK` と official gold が衝突したら、原則として official gold を優先する
- `KoG2P` は third opinion として使い、規則実装の単純系比較に回す
- `Kss` / `kokorog2p` / `misaki` は独立 benchmark ではなく、派生実装の挙動確認に留める

### 最初に作るべき評価セット

実装開始時点では、最低でも次の 3 セットがあれば十分。

- `tests/TestData/KoreanG2P/g2pk_parity.tsv`
- `tests/TestData/KoreanG2P/official_gold.tsv`
- `tests/TestData/KoreanG2P/weak_rules.tsv`

列案:

- `input`
- `expected`
- `source`
- `rule_tag`
- `notes`

`rule_tag` は少なくとも次を持たせるとよい。

- `neutralization`
- `resyllabification`
- `tensification`
- `nasalization`
- `liquidization`
- `h-deletion`
- `n-insertion`
- `ui-variation`
- `place-assimilation`

## 10-agent 統合サマリ

| Agent | 担当 | 主要結論 | 実装への影響 |
|---|---|---|---|
| 1 | 規範ソース | 韓国語は表音的だが、標準発音規則は単純な文字置換では足りない | 規則エンジン前提 |
| 2 | 必須音韻規則 | 終声中和・連音・濃音化・鼻音化・流音化・ㅎ系・ㄴ添音が最低ライン | MVP でも規則数はそれなりに必要 |
| 3 | OSS 比較 | `g2pK` は高機能、`KoG2P` は軽量、`Kss` は統合スイート | 直接移植ではなく設計参照が妥当 |
| 4 | 形態素解析 | 数詞読み分け・複合語境界・一部の発音規則は形態素情報があると精度が上がる | interface 化して optional にすべき |
| 5 | 正規化 | 数字・英字・漢字・記号展開で難易度が跳ねる | v1 は Hangul 優先、v1.1 以降で拡張 |
| 6 | 出力表現 | 内部は Jamo/音節構造ベース、外部は phoneme list + Jamo/IPA 変換が扱いやすい | 既存言語パッケージの流儀に合わせやすい |
| 7 | 評価 | 公開の純 G2P gold corpus は見つけにくい。辞書発音 + 規則例 + 参照実装比較が現実的 | テスト基盤は自前構築寄り |
| 8 | ライセンス | `g2pK` は Apache-2.0、`Kss` は BSD-3-Clause、`KoG2P` は GPL-3.0 | KoG2P コード流用は避ける |
| 9 | repo 組み込み | 新規 `DotNetG2P.Korean` と multilingual ルーティング追加で収まりがよい | 既存構成を踏襲できる |
| 10 | 推奨方針 | まず規範準拠の rule-based core を作り、例外辞書と評価セットを後から厚くする | 段階導入が最善 |

## Agent 1: 規範ソースの確認

国立国語院系の情報から見ると、韓国語 G2P は「Hangul は表音的だから簡単」という類の問題ではない。  
`g2pK` の README 自体も、Hangul は phonetic だが pronunciation rules are notoriously complicated と明示している。

実装時の一次ソース優先順位は以下がよい。

1. 国立国語院の標準発音法・公式 Q&A
2. 国立国語院辞書系 API の発音フィールド
3. OSS 実装 (`g2pK`, `Kss`, `KoG2P`) の挙動比較

補足:
- 今回の調査では、国立国語院の Q&A ページが各条文を直接引用しており、実装観点では十分有用だった
- 辞書 API では `pronunciation` フィールドを検索/取得できる

## Agent 2: MVP で外せない音韻規則

最低でも次は必要。

| 優先度 | 規則 | 内容 |
|---|---|---|
| High | 終声中和 | 語末・子音前で終声が代表音に寄る |
| High | 連音 | 終声が後続母音に移る |
| High | 濃音化 | 例えば `국밥 -> 국빱` のような硬音化 |
| High | 鼻音化 | `먹는 -> 멍는`, `국물 -> 궁물` 型 |
| High | 流音化 | ㄴ/ㄹ 接触で [ll] 系へ寄る規則 |
| High | ㅎ 系変化 | 激音化・弱化・脱落 |
| Medium | 口蓋化 | ㄷ/ㅌ + i/j 系など |
| Medium | ㄴ 添音 | 複合語や i/j 系開始音節前での挿入 |
| Medium | 二重終声処理 | ㄳ, ㄵ, ㄺ, ㄼ などの扱い |
| Medium | 사이시옷 | 複合語での発音変化 |

国立国語院 Q&A では次の例が確認できる。

- 第9項系: 終声で実現可能な代表音に収束する
- 第18項系: `먹는[멍는]`, `국물[궁물]`, `깎는[깡는]`
- 第23項系: `국밥[국빱]` のような濃音化
- 第29項系: `ㄴ` 添音

推定:
- この repo の MVP でも、スペイン語やフランス語の rule engine に近い粒度で規則を積む必要がある
- 逆に言えば、辞書フル依存でなくても rule-based core は成立しやすい

## Agent 3: OSS 実装の比較

### `KoG2P`

- Python 実装
- 任意の Hangul 文字列に対して規則ベースで phone 列を返す
- `rulebook.txt` と `testset.txt` を持つ
- 出力は独自 phone set
- GitHub 上では `GPL-3.0`

良い点:
- 軽量で理解しやすい
- Jamo/phone ベースの発想がはっきりしている

注意点:
- ライセンス上、コード流用の候補にはしにくい
- 形態素解析やテキスト正規化まで厚くやる設計ではない

### `g2pK`

- Python 実装
- `python-mecab-ko`, `konlpy`, `nltk`, `jamo` に依存
- 英単語の Hangul 化、文脈依存の数詞読み分け、`idioms.txt` による例外補強がある
- `verbose=True` でどの規則を適用したか追跡できる
- GitHub 上では `Apache-2.0`

良い点:
- 現実的な Korean TTS front-end にかなり近い
- 例外辞書と正規化の重要性が見える

注意点:
- Python 依存が重い
- この repo の「純 C# / Unity で使いやすい」方向とはそのままでは合わない

### `Kss`

- 総合 Korean string processing suite
- `g2p` 機能を持つ
- `descriptive`, `group_vowels`, `to_syllable`, `convert_english_to_hangul_phonemes` などの引数を持つ
- 形態素解析 backend として `mecab`, `pecab` を扱う
- PyPI 上では 2025-11-13 時点の `6.0.6`
- ライセンスは `BSD-3-Clause`

良い点:
- 実運用で欲しくなるオプション設計の参考になる
- `descriptive` と `group_vowels` は将来の option 設計に流用しやすい考え方

注意点:
- suite 全体の中の一機能であり、C# へ直接持ち込む対象ではない

## Agent 4: 形態素解析の必要性

`g2pK` が形態素解析依存を持っている時点で、韓国語 G2P の精度向上に形態素情報が効くのはほぼ確実。  
特に次で効く。

- 数字の読み分け
- 複合語境界
- 助詞・語尾接続
- 一部の不規則・慣用発音

ただし、この repo の方針を考えると `MeCab for Korean を必須依存にする` のは重い。

推奨:

- v1: 形態素解析なしでも動く pure C# rule engine
- v1.1+: `IKoreanMorphAnalyzer` のような optional interface を追加
- fallback: 空白境界 + 辞書ヒント + 例外辞書

## Agent 5: 正規化の難所

韓国語 G2P の難しさは音韻規則だけではなく、正規化にもある。

`g2pK` と `Kss` の情報から、少なくとも以下は別レイヤで扱うべき。

- 숫자: 文脈依存の数詞読み
- 英字: 英単語や略語の Hangul 化
- 漢字: Hanja to Hangul
- 記号: 句読点・記号の読み飛ばし/読み上げ方針
- 外来語: 正書法のままでは発音がずれる語

推奨段階分け:

1. v1: Hangul-only + 基本文字種の透過
2. v1.1: 数字正規化
3. v1.2: 英字/略語/外来語
4. v1.3: Hanja 補助

## Agent 6: 出力表現

内部表現の候補:

- 分解 Jamo
- onset / nucleus / coda の三部構造
- 独自 phoneme enum

この repo に合わせるなら、内部は `phoneme enum + syllable/segment model` が最も自然。  
ただし韓国語は音節境界と終声情報が重要なので、英語やスペイン語以上に syllable-aware な内部表現が向く。

公開 API の候補:

- `ToPhonemes(string text)` : repo 既存流儀に合わせた space-separated phoneme sequence
- `ToJamo(string text)` : デバッグ・TTS 前処理向け
- `ToIPA(string text)` : 後段ツール連携用

推奨:
- MVP は `ToPhonemes` と `ToJamo`
- `ToIPA` は後追いでもよい

## Agent 7: 評価データ

今回確認できた一次ソースだけだと、英語の CMU dict のような「そのまま gold にできる大規模公開 G2P 辞書」は見つけにくい。  
一方で、評価材料は作れる。

候補:

1. 国立国語院 `한국어기초사전` API の `pronunciation` フィールド
2. 国立国語院 Q&A / 標準発音法に出てくる規則例
3. `KoG2P` の `testset.txt`
4. `g2pK` / `Kss` と比較する差分テスト

評価戦略の推奨:

- Unit tests: 規則ごとの最小対立セット
- Regression tests: 例外辞書・慣用発音
- Dictionary evaluation: API から取得した gold 発音と照合
- Reference comparison: `g2pK` / `Kss` と差分確認

注意:
- AI Hub や Zeroth Korean は音声・転写コーパスとして有用だが、G2P の gold lexicon としてはそのまま使いにくい

## Agent 8: ライセンス整理

| ソース | ライセンス | 判断 |
|---|---|---|
| `g2pK` | Apache-2.0 | 参照可。コード移植時は notice 管理が必要 |
| `Kss` | BSD-3-Clause | 参照可 |
| `KoG2P` | GPL-3.0 | コード流用は避ける |
| `한국어기초사전` API | 要利用条件確認 | 開発・評価用途は有力だが、再配布方針は別途確認が必要 |

実務判断:

- 実装は国立国語院規範を元にゼロから起こす
- `g2pK` / `Kss` は挙動比較用
- `KoG2P` はテストケース発想や phone set 参考まで

## Agent 9: `dot-net-g2p` への組み込み案

### 新規パッケージ案

`src/DotNetG2P.Korean`

想定ファイル:

- `KoreanG2PEngine.cs`
- `KoreanG2POptions.cs`
- `Models/KoreanPhoneme.cs`
- `Models/KoreanPronunciation.cs`
- `Rules/GraphemeToPhonemeRules.cs`
- `Rules/KoreanOrthography.cs`
- `Rules/BatchimProcessor.cs`
- `Rules/AssimilationProcessor.cs`
- `Conversion/IpaConverter.cs`
- `Data/korean_exceptions.master.tsv`
- `Normalization/KoreanNormalizer.cs`

### multilingual 側の変更点

既存コードを見る限り、主に以下を触ることになる。

- `src/DotNetG2P.Multilingual/Language.cs`
- `src/DotNetG2P.Multilingual/LanguageDetector.cs`
- `src/DotNetG2P.Multilingual/TextSegmenter.cs`
- `src/DotNetG2P.Multilingual/MultilingualG2PEngine.cs`

特に重要なのは `LanguageDetector`。  
現状は Hangul を独立 script として扱っていないので、韓国語追加時は `ScriptKind.Korean` 相当を導入するのが素直。

推定:
- 韓国語セグメント判定は、日本語/中国語の漢字曖昧性より簡単
- 混在テキストのうち Hangul 部分はかなり高精度に Korean へ振り分けられる

## Agent 10: 実装ロードマップ提案

### Phase 1: Korean core

- Hangul 分解/再結合
- 終声処理
- 連音
- 濃音化
- 鼻音化
- 流音化
- ㅎ 系変化
- 小さな例外辞書
- ユニットテスト

### Phase 2: Normalization

- 数字読み
- 日付/時刻
- 英字/略語
- 外来語の例外

### Phase 3: Advanced

- optional 形態素解析
- descriptive mode
- group_vowels mode
- multilingual 統合
- dictionary API ベースの評価ハーネス

## 推奨仕様

最初の実装スコープとしては以下を推奨する。

- 入力: 現代 Hangul を主対象にする
- 出力: space-separated phoneme sequence + jamo
- 依存: 外部 Python / native binary なし
- 評価: 規則単位テスト + 辞書発音照合
- 例外: TSV ベース辞書で吸収

非推奨:

- 最初から `g2pK` 同等の英字/数字/Hanja 全対応を狙うこと
- `KoG2P` のコードや rulebook をそのまま持ち込むこと
- 形態素解析を必須依存にすること

## この調査から見た実装優先度

1. `DotNetG2P.Korean` の pure C# core を作る
2. `Multilingual` に Hangul 判定を追加する
3. 例外辞書と回帰テストを整える
4. 数字正規化を追加する
5. optional morph analyzer を検討する

## 参考ソース

- 国立国語院 한국어기초사전 Open API: <https://krdict.korean.go.kr/kor/openApi/openApiInfo>
- 国立国語院 한국어기초사전 ヘルプ: <https://krdict.korean.go.kr/kor/help/helpList>
- 国立国語院 Q&A 第9項/第23項関連: <https://korean.go.kr/front/onlineQna/onlineQnaView.do?mn_id=216&qna_seq=312379>
- 国立国語院 Q&A 第9項関連: <https://www.korean.go.kr/front/onlineQna/onlineQnaView.do?mn_id=216&pageIndex=1&qna_seq=318280&searchCondition=&searchKeyword=>
- 国立国語院 Q&A 第29項関連: <https://www.korean.go.kr/front/onlineQna/onlineQnaView.do?mn_id=216&qna_seq=295984>
- `g2pK`: <https://github.com/Kyubyong/g2pK>
- `g2pK` PyPI: <https://pypi.org/project/g2pK/>
- `Kss`: <https://github.com/hyunwoongko/kss>
- `Kss` PyPI: <https://pypi.org/project/kss/>
- `KoG2P`: <https://github.com/scarletcho/KoG2P>
- `g2pkk`: <https://github.com/harmlessman/g2pkk>
- `g2pkk` PyPI: <https://pypi.org/project/g2pkk/>
- `g2pk2` PyPI: <https://pypi.org/project/g2pk2/>
- `g2pkiwi` PyPI: <https://pypi.org/project/g2pkiwi/>
- `kokorog2p` docs: <https://kokorog2p.readthedocs.io/en/latest/>
- `kokorog2p` PyPI: <https://pypi.org/project/kokorog2p/>
- `misaki` GitHub: <https://github.com/hexgrad/misaki>
- `misaki` PyPI: <https://pypi.org/project/misaki/>
- `python-mecab-ko`: <https://github.com/python-mecab-ko/python-mecab-ko>
- `Kiwi`: <https://github.com/bab2min/Kiwi>
- `kiwipiepy`: <https://github.com/bab2min/kiwipiepy>
- `Naramal` NuGet: <https://www.nuget.org/packages/Naramal>
- GitHub repository search (`"korean" "g2p" language:C#`): <https://github.com/search?q=%22korean%22+%22g2p%22+language%3AC%23&type=repositories>
- GitHub repository search (`"hangul" "phoneme" language:C#`): <https://github.com/search?q=%22hangul%22+%22phoneme%22+language%3AC%23&type=repositories>
- GitHub repository search (`"hangul" "g2p" language:C#`): <https://github.com/search?q=%22hangul%22+%22g2p%22+language%3AC%23&type=repositories>
- 문성민 외 2022, 한국어 발음 변환기(G2P)의 현황과 성능 향상에 대한 언어학적 제안: <https://aaks.or.kr/webzine/202212/ss20>
- Korean grapheme-to-phoneme conversion using sound-pattern pairs corpus (2009): <https://www.sciencedirect.com/science/article/pii/S0885230809000280>
- Grapheme-to-phoneme conversion for Korean unrestricted text synthesis (1998): <https://www.isca-archive.org/icslp_1998/kim98o_icslp.html>

## 補足

このメモは「今すぐ韓国語 G2P をどう着手するか」の意思決定用サマリ。  
実装時には、各規則ごとに国立国語院の条文・例示へ戻って unit test を切る前提で進めるのがよい。

実装順序と担当分割は `docs/korean-g2p-implementation-plan.md` を参照。
