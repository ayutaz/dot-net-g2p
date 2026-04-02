# SW3-005: Sw3テスト + 例外辞書拡充500語 + パフォーマンス

> **マイルストーン**: Sw3 — ピッチアクセント + 方言 + PUA + Prosody
> **前提チケット**: SW3-001（ピッチアクセント予測）, SW3-002（異音処理）, SW3-003（方言対応）, SW3-004（PUA + Prosody API）
> **後続チケット**: Sw4（Multilingual統合 + 評価ツール + リリース準備）

## 1. タスク目的とゴール

Sw3マイルストーンの全機能（ピッチアクセント、異音処理、方言対応、PUA変換、Prosody API）を網羅的にテストし、例外辞書を300語から500語以上に拡充してPER < 4%の目標を達成する。パフォーマンステストにより実用的な処理速度を確認する。

**ゴール**: Sw3の追加テスト100件以上（累計350件以上）が全て通過し、ipa-dictサンプルでPER < 4%（base, Central方言）を達成する。例外辞書を500エントリ以上に拡充する。

## 2. 実装内容の詳細

### 2.1 テストファイル構成

マイルストーン計画に基づくSw3テスト（+100件 = 累計350+）。内訳: StressAssigner拡張+20, AllophoneProcessor+20, AllophoneEvaluation+5, Prosody+15, PuaMapping+10, Dialect+15, Performance+6, DatasetEvaluation+9 = 計100件:

```
tests/DotNetG2P.Tests/SwedishG2P/
├── StressAssignerTests.cs                  — ★拡張: アクセント予測テスト (+20件) [SW3-001]
├── AllophoneProcessorTests.cs              — ★新規: 異音処理テスト (20件) [SW3-002]
├── SwedishAllophoneEvaluationTests.cs      — ★新規: 異音参照テスト (5件) [SW3-002]
├── SwedishProsodyTests.cs                  — ★新規: 韻律テスト (15件) [SW3-004]
├── SwedishPuaMappingTests.cs               — ★新規: PUA変換テスト (10件) [SW3-004]
├── SwedishDialectTests.cs                  — ★新規: 方言テスト (15件) [SW3-003]
├── SwedishPerformanceTests.cs              — ★新規: パフォーマンステスト (6件)
└── SwedishDatasetEvaluationTests.cs        — ★拡張: PER閾値テスト追加 (+9件)

tests/TestData/SwedishG2P/
├── swedish_allophone_reference.tsv         — 異音参照データ (15-20件)
├── ipa_dict_sv_se_sample.tsv              — 既存 (256件)
└── wikipron_swe_latn_broad_filtered_sample.tsv — 既存 (256件)
```

### 2.2 例外辞書拡充（300→500+語）

Sw2で作成済みの `swedish_exceptions.master.tsv` を以下のカテゴリで拡充する:

| カテゴリ | Sw2時点 | 拡充後 | 追加内容 |
|---------|--------|--------|---------|
| function_word | 30-40 | 40-50 | 弱形/強形の追加バリアント |
| loanword_fr | 40-50 | 50-60 | フランス語由来追加（restaurant, bagage, parfym等） |
| loanword_en | 40-50 | 60-70 | 英語由来追加（design, e-mail, website, management等） |
| loanword_other | 10-15 | 15-20 | ドイツ語/ラテン語/アラビア語由来 |
| sj_exception | 30-40 | 40-50 | -tion/-sion/-eur 語尾追加 |
| softening_exception | 15-20 | 20-25 | 子音軟化例外追加 |
| place_name | 40-50 | 60-70 | 主要都市・県名・ラップランド地名追加 |
| person_name | 0 | 20-30 | 主要な不規則人名（Kjell, Björk等） |
| silent_letter | 10-15 | 15-20 | 黙字パターン追加 |
| irregular | 15-20 | 30-40 | その他不規則語追加 |
| compound | 0 | 40-50 | 主要複合語（accent情報付き） |
| minimal_pair | 0 | 20-30 | 最小対語（accent 1/2の区別、ipa-dict評価用） |

**拡充の優先順位**:
1. ipa-dict 評価でPER改善に直結するエントリ（PER分析→誤予測語を辞書追加）
2. 最小対語のデフォルトアクセント登録
3. 頻度の高い外来語
4. 複合語（accent 2 情報付き）

### 2.3 パフォーマンステスト

`SwedishPerformanceTests.cs` に以下のテストを作成（6件）:

| テスト名 | 閾値 | 内容 |
|---------|------|------|
| 初期化時間 | < 500ms | `new SwedishG2PEngine()` の初期化時間 |
| 単文変換速度 | < 10ms | 短文（10語程度）のIPA変換時間 |
| バッチ変換速度 | < 100ms/100文 | 100文のバッチ変換時間 |
| メモリ成長 | < 50MB | 1000回変換後のメモリ増加量 |
| Dispose後メモリ解放 | GC後にメモリ減少 | Dispose + GC.Collect 後のメモリ確認 |
| 例外辞書ルックアップ速度 | < 1ms/語 | 500語辞書での単語検索速度 |

### 2.4 異音評価テスト

`SwedishAllophoneEvaluationTests.cs` で `swedish_allophone_reference.tsv` を使用した検証:

**TSV形式**:
```tsv
# word	dialect	expected_ipa	features
bord	Central	buːɖ	Retroflexion|VowelLengthMarking
bord	FinlandSwedish	buːrd	VowelLengthMarking
barn	Central	bɑːɳ	Retroflexion|VowelLengthMarking
barn	FinlandSwedish	bɑːrn	VowelLengthMarking
Karl	Central	kɑːɭ	Retroflexion|VowelLengthMarking
Karl	FinlandSwedish	kɑːrl	VowelLengthMarking
```

Central/Finland の両プロファイルで15-20語の完全一致検証を行う。

### 2.5 PER評価テスト拡張

`SwedishDatasetEvaluationTests.cs` に以下のテストを追加（+9件）:

| テスト名 | データセット | 条件 | 閾値 |
|---------|------------|------|------|
| IpaDict_サンプル_baseプロファイル_PER | ipa_dict (256件) | base (Central + 異音) | < 4% |
| IpaDict_サンプル_allophoneプロファイル_PER | ipa_dict (256件) | allophones (全異音有効) | < 3% |
| IpaDict_サンプル_accentExcluded_PER | ipa_dict (256件) | アクセントマーク除外 | < 4% |
| WikiPron_サンプル_baseプロファイル_PER | wikipron (256件) | base | < 5% |
| IpaDict_サンプル_accentIncluded_PER | ipa_dict (256件) | アクセントマーク含む | < 5% |
| IpaDict_サンプル_Finland_baseプロファイル_PER | ipa_dict (256件) | base (Finland + 異音) | 計測のみ（閾値なし） |
| WikiPron_サンプル_accentExcluded_PER | wikipron (256件) | アクセントマーク除外 | < 5% |
| IpaDict_辞書拡充前後_PER改善確認 | ipa_dict (256件) | base (Central) | 拡充後PER < 拡充前PER |
| DatasetEvaluation_TSVエントリ数_256以上 | ipa_dict + wikipron | 各TSVファイルの行数 | >= 256 |

### 2.6 PER改善サイクル

例外辞書拡充は以下のサイクルで行う:

```
1. 現状PER計測（ipa-dict 256件サンプル）
2. 誤予測語リスト抽出（expected vs actual の差分）
3. 誤予測パターン分析:
   a. ルール改善で対応可能 → GraphemeToPhonemeRules 修正
   b. 不規則語 → 例外辞書追加
   c. 外来語 → 例外辞書追加（カテゴリ: loanword_*）
   d. アクセント誤り → 例外辞書のaccent列で補正
4. 辞書追加後にPER再計測
5. PER < 4% 達成まで繰り返し
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| テストエージェント | 1 | 全テストファイル作成（StressAssigner拡張、Allophone、Prosody、PUA、Dialect、Performance、Evaluation） |
| 辞書拡充エージェント | 1 | swedish_exceptions.master.tsv の拡充（300→500+語）、PER分析→辞書追加サイクル |
| レビューエージェント | 1 | テストの網羅性確認、例外辞書エントリの正確性確認、PER閾値の妥当性確認 |

**推奨**: テストと辞書拡充は並行して進行可能。PER改善サイクルは辞書拡充エージェントが主導し、テストエージェントが評価テストで検証する。計3人で進行。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- テストファイル6種類の新規作成/拡張（合計+100件）
- `swedish_allophone_reference.tsv` の作成（15-20件）
- `swedish_exceptions.master.tsv` の拡充（300→500+語）
- PER分析と辞書改善サイクルの実行
- パフォーマンステストの作成と閾値設定

**スコープ外**:
- ipa-dict フルデータセット（21,107件）でのPER評価（Sw4で対応）
- WikiPron フルデータセット（4,631件）での評価（Sw4で対応）
- 評価ツール（`DotNetG2P.SwedishEval`）の作成（Sw4で対応）

### ユニットテスト

各テストの詳細は以下の通り:

**StressAssignerTests.cs 拡張（+20件）**: SW3-001チケットのテスト項目を参照

**AllophoneProcessorTests.cs（20件）**: SW3-002チケットのテスト項目を参照

**SwedishAllophoneEvaluationTests.cs（5件）**:

| テスト名 | 内容 |
|---------|------|
| Central_allophone_reference_全一致 | Central方言で参照TSVの全エントリが一致 |
| Finland_allophone_reference_全一致 | Finland方言で参照TSVの全エントリが一致 |
| Central_そり舌音_含む | Central出力にそり舌音が含まれる |
| Finland_そり舌音_含まない | Finland出力にそり舌音が含まれない |
| TSV_最小エントリ数_15以上 | 参照TSVに最低15エントリあること |

**SwedishProsodyTests.cs（15件）**: SW3-004チケットのテスト項目を参照

**SwedishPuaMappingTests.cs（10件）**: SW3-004チケットのテスト項目を参照

**SwedishDialectTests.cs（15件）**: SW3-003チケットのテスト項目を参照

**SwedishPerformanceTests.cs（6件）**: 上記2.3節の一覧を参照

### E2Eテスト

- Sw3全機能の統合テスト: テキスト入力 → 正規化 → G2P → アクセント予測 → 異音処理 → IPA/PUA/Prosody 出力の全パイプラインを通した検証
- ipa-dict 256件サンプルで PER < 4% を確認する回帰テスト

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **PER < 4% 達成の不確実性**: 例外辞書500語でPER < 4%を達成できるかは、誤予測パターンの分布に依存する。規則改善が必要なケースが多い場合、辞書追加だけでは不十分な可能性がある
2. **例外辞書のアクセント情報の正確性**: 最小対語のデフォルトアクセント選択は言語学的知識が必要。頻度ベース（より一般的な意味のアクセントをデフォルト）で決定する
3. **パフォーマンス閾値の妥当性**: 他言語パッケージのパフォーマンステストと閾値を比較し、スウェーデン語固有の処理（例外辞書500語ルックアップ、アクセント予測）による追加コストを考慮した閾値を設定する
4. **ipa-dict のアクセントマーク `²` との比較**: ipa-dict はaccent 2を `²` マークで表記する。評価時にアクセント含む/除くの両方でPERを計測し、アクセント予測精度を別途計測する
5. **テストデータの再現性**: `ipa_dict_sv_se_sample.tsv`（256件）のサンプリングがSw2時点のものと同一であること（ランダムシード固定）を確認する

### レビューチェックリスト

- [ ] テスト総数が Sw3 目標の 350+ 件に達しているか（Sw1: 150 + Sw2: +100 + Sw3: +100）
- [ ] 例外辞書が 500 エントリ以上あるか（`wc -l swedish_exceptions.master.tsv` で確認）
- [ ] PER < 4%（base, Central方言）が ipa-dict サンプルで達成されているか
- [ ] パフォーマンステストの閾値が他言語パッケージと整合しているか
- [ ] `swedish_allophone_reference.tsv` のフォーマットが他言語の参照TSVと一致しているか
- [ ] 例外辞書のTSVフォーマットが Sw2 で定義した形式と一致しているか（surface, dialect, category, accent, stress_index, phonemes, source, note）
- [ ] 全テストが `dotnet test --filter "ClassName~SwedishG2P"` で通過するか
- [ ] `sync-shared-internals.ps1 -Check` が pass するか
- [ ] テストファイル名がマイルストーン計画のファイル名と一致しているか

## 6. ゼロから作り直すとしたら

1. **テスト駆動開発（TDD）でSw3を進める**: SW3-001〜004の各チケットでテストを先に書き、テストが通るように実装を進める。現在の設計ではテストが最後のチケット（SW3-005）に集中しており、実装とテストのフィードバックループが遅い
2. **PER分析自動化パイプラインを最初に構築する**: 辞書追加 → PER計測 → 誤予測分析のサイクルを自動化するスクリプトをSw3開始前に準備し、辞書拡充の効率を最大化する
3. **例外辞書をNST辞書から自動生成する**: NST辞書（822k語, CC0）からG2P規則で誤予測される語を自動抽出し、例外辞書のベースラインを自動生成する。手動キュレーションはこのベースライン上で行う
4. **アクセント評価を独立した指標にする**: PERにアクセント情報を含めると、音素変換の精度とアクセント予測の精度が混在する。音素PER（アクセント除外）とアクセント精度（accent accuracy）を別指標として計測する

## 7. 後続タスクへの連絡事項

- **Sw4（Multilingual統合）**: Sw3完了時点で累計350+テストが全てpassしていること。Sw4の前提条件としてSw3の完了条件を全て満たすこと:
  - ipa-dict サンプル PER < 4%（base, Central方言）
  - 例外辞書 500+ エントリ
  - Sw3テスト 100+ 件追加（累計 350+）
- **Sw4（評価ツール）**: `tools/DotNetG2P.SwedishEval/` の作成時に、SW3-005 で確立したPER計測手法（アクセント含む/除くの2種類）を踏襲すること。`tools/refresh_swedish_eval_data.ps1` で ipa-dict/WikiPron のダウンロード・フィルタを行い、`tools/run_swedish_full_evaluation.ps1` でフル評価を実行するパイプラインを構築する
- **Sw4（フル評価）**: Sw3ではipa-dictサンプル（256件）での評価だが、Sw4ではフルデータセット（21,107件）での評価を行う。サンプルPERとフルPERの乖離が想定されるため、辞書拡充の余地を残しておくこと
- **例外辞書の品質**: 500+語の辞書エントリは全て手動確認済みであること。自動生成したエントリは `source` 列に `auto` と記載し、手動確認済みのものは `manual` と記載する
- **テストデータのファイル名**: refresh スクリプトの出力ファイル名と `SwedishDatasetEvaluationTests.cs` で参照するファイル名が正確に一致すること（ポルトガル語レビュー知見）
