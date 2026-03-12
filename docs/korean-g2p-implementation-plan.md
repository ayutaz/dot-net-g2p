# 韓国語 G2P 実装計画マイルストーン

更新日: 2026-03-12

## 目的

`docs/korean-g2p-research.md` の調査結果を、実装順序と担当レーンに落とした計画書。  
ここでは「何を先に作るか」「どこで品質を判定するか」「何を v1 の範囲外に置くか」を明確にする。

## 前提

- 主 benchmark は `g2pK`
- 副 benchmark は `KoG2P`
- gold oracle は国立国語院辞書発音と規則例
- v1 は `Hangul-first` を原則とし、英字/Hanja/高度な数詞処理は後続マイルストーンに分離する
- 実装は pure C# を維持し、外部 Python や native binary を必須にしない

## 10-agent 体制

| Agent | 役割 | 主担当 |
|---|---|---|
| 1 | Tech Lead | 全体設計、API 境界、レビュー基準 |
| 2 | Benchmark Lead | `g2pK` parity、official gold、weak rules の評価設計 |
| 3 | Core Model Lead | Hangul 分解、Jamo/phoneme model、syllable model |
| 4 | Rule Engine Lead | 終声中和、連音、濃音化、鼻音化、流音化、ㅎ系 |
| 5 | Exception Lead | 例外辞書、慣用発音、TSV 管理 |
| 6 | Normalization Lead | 숫자、記号、英字、将来の Hanja レイヤ設計 |
| 7 | Conversion/API Lead | `ToPhonemes`, `ToJamo`, `ToIPA`, options 設計 |
| 8 | Multilingual Lead | `LanguageDetector`, `TextSegmenter`, `MultilingualG2PEngine` 統合 |
| 9 | Packaging Lead | `.csproj`, `package.json`, `asmdef`, ライセンス/notice |
| 10 | Release Lead | ドキュメント、テストゲート、 milestone 統合管理 |

## マイルストーン概要

| Milestone | 状態 | 目的 | 主担当 agent | 完了条件 |
|---|---|---|---|---|
| M0 | Done | 評価土台を先に固定する | 1,2,10 | benchmark ファイル雛形と採点ルールがある |
| M1 | Done | Korean core の骨組みを作る | 1,3,7,9 | `DotNetG2P.Korean` が build できる |
| M2 | Done | MVP 規則群を実装する | 3,4,5,7 | 必須規則が unit test で通る |
| M3 | Done | benchmark で品質を見える化する | 2,4,5,10 | parity/gold/weak rules のレポートが出る |
| M4 | Done | 例外辞書と基本正規化を厚くする | 5,6,7 | Hangul-first v1 の精度が安定する |
| M5 | Next | multilingual へ統合する | 1,8,9 | Hangul segment が自動で Korean に流れる |
| M6 | Pending | パッケージ化と release readiness | 7,9,10 | README/API/tests/package metadata が揃う |

## 現在地

2026-03-12 時点の進捗は `M0 -> M4 完了`, `M5 次着手`。

直近の M4 完了内容:

- `src/DotNetG2P.Korean/Data/korean_exceptions.master.tsv` と `KoreanExceptionDictionary.cs` を追加
- `src/DotNetG2P.Korean/Normalization/KoreanNormalizer.cs` を追加
- `EnableTextNormalization`, `EnableExceptionDictionary`, `UiVariationMode` を `KoreanG2POptions` に追加
- engine に `Normalize -> ExceptionDictionary -> Rules` の流れを追加
- `나의` の `ui-variation` を `Standard` / `Colloquial` option で切り替えられるようにした
- punctuation / repeated whitespace / fullwidth ASCII の最小正規化を追加
- `tests/DotNetG2P.Tests/KoreanG2P/` に normalizer / exception dictionary / engine override tests を追加
- review 指摘を受けて `담임` のような単純語への `ㄴ` 添音過剰適用を止め、compound 系は exact lexical exception に寄せた
- `넓다`, `넓고`, `앉다`, `핥다` を通す hidden obstruent tensification を追加した
- `KoreanPronunciation` の collection を read-only view 化し、外部から内部配列を書き換えられないようにした
- 例外辞書 loader を strict validation に変え、invalid mode / duplicate entry を fail-fast にした
- benchmark harness に custom options と custom output directory を追加し、test 実行時の worktree 汚染を抑えた

直近のテストゲート:

- `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj --filter KoreanG2P --no-restore`
  - `140 passed`
- `dotnet build DotNetG2P.slnx -m:1 --no-restore`
  - success

## M0: Evaluation First

### 目的

実装前に評価基準を固定し、`g2pK` 追従だけで終わらないようにする。

### 担当

- Agent 1
- Agent 2
- Agent 10

### 成果物

- `tests/TestData/KoreanG2P/g2pk_parity.tsv`
- `tests/TestData/KoreanG2P/official_gold.tsv`
- `tests/TestData/KoreanG2P/weak_rules.tsv`
- 比較ルールのメモ

### 完了条件

- 各 TSV に `input`, `expected`, `source`, `rule_tag`, `notes` 列がある
- `rule_tag` の最小セットが定義される
- `official_gold` 優先の判定方針が文章化される
- `expected` に複数許容発音がある場合は `|` 区切りで保持できる

### 注意点

- `g2pK` 一致を正解扱いしない
- `weak_rules` を別セットにして弱点規則を埋もれさせない

## M1: Package Scaffold

### 目的

`DotNetG2P.Korean` の最小パッケージを作り、以降のルール実装の受け皿を固定する。

### 担当

- Agent 1
- Agent 3
- Agent 7
- Agent 9

### 成果物

- `src/DotNetG2P.Korean/DotNetG2P.Korean.csproj`
- `src/DotNetG2P.Korean/DotNetG2P.Korean.asmdef`
- `src/DotNetG2P.Korean/package.json`
- `src/DotNetG2P.Korean/KoreanG2PEngine.cs`
- `src/DotNetG2P.Korean/KoreanG2POptions.cs`
- `src/DotNetG2P.Korean/Models/*`

### 完了条件

- solution に追加される
- build が通る
- 空文字、null guard、batch API の基本挙動が揃う
- `ToPhonemes`, `ToJamo` の public API 形が固まる
- Korean core package が独立テスト対象として固定される

## M2: MVP Rule Engine

### 目的

標準発音法のうち、Hangul-first MVP に必要な規則を優先実装する。

### 担当

- Agent 3
- Agent 4
- Agent 5
- Agent 7

### 対象規則

- 終声中和
- 連音
- 濃音化
- 鼻音化
- 流音化
- ㅎ 系変化
- ㄴ 添音
- 二重終声の基本処理

### 成果物

- `Rules/KoreanOrthography.cs`
- `Rules/BatchimProcessor.cs`
- `Rules/AssimilationProcessor.cs`
- `Rules/GraphemeToPhonemeRules.cs`
- 規則単位 unit tests

### 完了条件

- `g2pk_parity`, `official_gold`, `weak_rules` の current seed cases が pass する
- 音節境界を跨ぐ規則適用順が固定される
- 代表規則ごとのテストが読みやすい形で残る
- boundary-aware な pipeline で whitespace を保持できる

### リスク

- 規則順序の違いで意図しない回帰が起きやすい
- 例外辞書で吸収すべきものと rule で吸収すべきものの境界がぶれやすい
- morphology なしでは `ㄴ` 添音と lexical variation にヒューリスティックが残る

## M3: Benchmark Harness

### 目的

実装を主観で進めず、`g2pK` parity と official gold の差を常に見えるようにする。

### 担当

- Agent 2
- Agent 4
- Agent 5
- Agent 10

### 成果物

- benchmark 実行テスト
- rule-wise summary
- mismatch レポート出力
- parity/gold/weak をまとめた読みやすい実行結果
- `tests/DotNetG2P.Tests/KoreanG2P/Benchmarking/*`
- `tests/DotNetG2P.Tests/TestResults/KoreanG2P/korean-benchmark-summary.json`
- `tests/DotNetG2P.Tests/TestResults/KoreanG2P/korean-benchmark-dataset-summary.tsv`
- `tests/DotNetG2P.Tests/TestResults/KoreanG2P/korean-benchmark-rule-summary.tsv`
- `tests/DotNetG2P.Tests/TestResults/KoreanG2P/korean-benchmark-mismatches.tsv`

### 完了条件

- `g2pk_parity`
- `official_gold`
- `weak_rules`

の 3 セットで pass/fail と差分が確認できる
- 許容形が複数あるケースで accepted alternatives を区別できる
- current implementation の弱点規則が rule tag 単位で集計できる

### 判定基準

- `g2pk_parity`: 互換性の監視
- `official_gold`: 規範準拠の監視
- `weak_rules`: 改善余地の監視

## M4: Exceptions And Basic Normalization

### 目的

rule engine だけでは取り切れない慣用発音と、v1 に必要な最小の正規化を追加する。

### 担当

- Agent 5
- Agent 6
- Agent 7

### 範囲

- 例外辞書 TSV
- 記号/空白の基本処理
- Hangul-only 入力に対する安定化
- 数字レイヤの interface 設計

### 成果物

- `Data/korean_exceptions.master.tsv`
- `Data/KoreanExceptionDictionary.cs`
- `Normalization/KoreanNormalizer.cs`

### 完了条件

- 既知の慣用発音を例外辞書で回収できる
- 正規化の有無で API の期待値が一貫する
- benchmark / dictionary / public model が fail-open にならない
- 숫자/英字/Hanja は `v1 optional or later` として境界が明文化される

## M5: Multilingual Integration

### 目的

Hangul を multilingual パイプラインに組み込み、混在文で自動に Korean セグメントへ流す。

### 担当

- Agent 1
- Agent 8
- Agent 9

### 成果物

- `src/DotNetG2P.Multilingual/Language.cs` 更新
- `src/DotNetG2P.Multilingual/ScriptKind.cs` 更新
- `src/DotNetG2P.Multilingual/LanguageDetector.cs` 更新
- `src/DotNetG2P.Multilingual/TextSegmenter.cs` 更新
- `src/DotNetG2P.Multilingual/MultilingualG2PEngine.cs` 更新

### 完了条件

- Hangul block が Korean として検出される
- 日本語/中国語/Latin 系の既存判定を壊さない
- 混在例で Korean engine に正しくルーティングされる

### リスク

- 全角記号や数字を跨ぐ segment merge の挙動
- 韓国語と英字混在語の扱い

## M6: Release Readiness

### 目的

利用者がパッケージとして使える状態まで整える。

### 担当

- Agent 7
- Agent 9
- Agent 10

### 成果物

- README 追記
- API 使用例
- package metadata
- third-party notices
- thread safety と制約の説明

### 完了条件

- package description が他言語パッケージと同じ粒度で揃う
- README に quick start が載る
- tests, benchmark, known limitations が明記される

## v1 の定義

v1 で満たすもの:

- Hangul-first Korean G2P
- `ToPhonemes`
- `ToJamo`
- 主要音韻規則
- 例外辞書
- `g2pK` parity / official gold / weak rules の 3 系統評価
- multilingual Hangul routing

v1 で見送るもの:

- 英字の本格的 Hangul 化
- Hanja 全面対応
- 文脈依存の高度な数詞読み
- mandatory morph analyzer
- descriptive mode
- group_vowels mode

## 着手順

1. M0 を先に作る
2. M1 で package scaffold を固定する
3. M2 で core rule engine を入れる
4. M3 で benchmark を回しながら穴を埋める
5. M4 で例外辞書と基本正規化を厚くする
6. M5 で multilingual に接続する
7. M6 で release readiness に進む

## Go / No-Go Gate

各マイルストーンのゲートは以下。

- M0 gate: benchmark datasets の意味と優先順位が確定している
- M1 gate: API と内部 model が大きく揺れない
- M2 gate: 必須規則が unit tests で安定している
- M3 gate: `weak_rules` の未対応箇所が可視化されている
- M4 gate: 例外辞書で吸収する範囲が明文化されている
- M5 gate: multilingual 既存挙動に回帰がない
- M6 gate: package と docs が他言語パッケージに並ぶ

## 次の具体タスク

M5 着手の最初の 1 週間分としては以下を推奨する。

1. `LanguageDetector` に Hangul block 判定を追加する
2. `TextSegmenter` で Hangul segment を独立させる
3. `MultilingualG2PEngine` に Korean routing を追加する
4. 日本語 / 中国語 / Latin 系の既存 segment merge に回帰がないことを確認する
5. mixed-script sample を Korean benchmark と別に固定する
