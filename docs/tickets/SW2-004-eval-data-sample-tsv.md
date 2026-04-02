# SW2-004: 評価データ取得 + サンプルTSV生成

> **マイルストーン**: Sw2 — 例外辞書 + テキスト正規化 + X-SAMPA
> **前提チケット**: なし（他チケットと並行作業可能。評価データはG2Pエンジン実装に依存しない）
> **後続チケット**: SW2-005（SwedishDatasetEvaluationTests がサンプルTSVを参照する）

## 1. タスク目的とゴール

スウェーデン語G2Pの精度評価に使用する外部データセット（ipa-dict、WikiPron）をダウンロード・フィルタリングし、テストから参照可能なサンプルTSVファイルを生成する。PowerShellスクリプト `refresh_swedish_eval_data.ps1` でデータ取得・加工を自動化し、再現性を確保する。

**ゴール**: `tests/TestData/SwedishG2P/` ディレクトリに `ipa_dict_sv_se_sample.tsv`（256件）と `wikipron_swe_latn_broad_filtered_sample.tsv`（256件）が配置され、SW2-005 のデータセット評価テストから参照できる状態。

## 2. 実装内容の詳細

### 2.1 追加ファイル

```
tools/
└── refresh_swedish_eval_data.ps1   — データ取得・フィルタ・TSV生成スクリプト

tests/TestData/SwedishG2P/
├── README.md                                        — データソース・ライセンス・再生成手順
├── ipa_dict_sv_se_sample.tsv                        — ipa-dictサンプル（256件）
└── wikipron_swe_latn_broad_filtered_sample.tsv      — WikiPronサンプル（256件）
```

### 2.2 refresh_swedish_eval_data.ps1

**処理フロー:**

```
1. ipa-dict sv.txt ダウンロード
   URL: https://raw.githubusercontent.com/open-dict-data/ipa-dict/master/data/sv.txt
   
2. ipa-dict フィルタリング
   - タブ区切り: surface \t IPA
   - 複数発音エントリ（カンマ区切り）→ 最初の発音のみ採用
   - ASCII以外の制御文字を含むエントリを除外
   - surface が空のエントリを除外

3. ipa-dict サンプリング（256件）
   - 等間隔サンプリング（既存スクリプトと同一方式）
   - 出力: ipa_dict_sv_se_sample.tsv

4. WikiPron swe_latn_broad.tsv ダウンロード
   URL: https://raw.githubusercontent.com/CUNY-CL/wikipron/master/data/scrape/tsv/swe_latn_broad.tsv
   
5. WikiPron フィルタリング
   - タブ区切り: surface \t IPA
   - 複数単語エントリ（スペース含む surface）を除外（単語レベル評価のため）
   - 数字を含む surface を除外
   - surface が1文字以下のエントリを除外
   - 重複 surface の除去（最初の出現を採用）

6. WikiPron サンプリング（256件）
   - 等間隔サンプリング（既存スクリプトと同一方式）
   - 出力: wikipron_swe_latn_broad_filtered_sample.tsv
   - 注: `_filtered` はスクリプト側でフィルタリング済みであることを示すサフィックス
```

**スクリプトのパラメータ:**

```powershell
param(
    [int]$SampleSize = 256,
    [switch]$Full,          # フルデータセットも出力（Sw4用）
    [string]$OutputDir = "tests/TestData/SwedishG2P"
)
```

**出力TSV形式:**

```tsv
# ipa-dict sv_SE sample (256 entries, evenly spaced)
# Source: https://github.com/open-dict-data/ipa-dict (CC BY-SA 2.5)
# Generated: 2026-04-02
surface	ipa
hej	hɛj
sjuk	ɧʉːk
köpa	ɕøːpa
...
```

### 2.3 データソースの特徴と注意点

**ipa-dict sv.txt:**
- エントリ数: 21,107語
- ソース: Folkets lexikon（KTH）
- ライセンス: CC BY-SA 2.5
- 声調アクセントマーク `²`（accent 2）を含む
- ストレスマーク `ˈ` 使用
- 長音記号 `ː`、sj音 `ɧ`、そり舌音 `ɳ, ɖ, ʂ` 等含有
- 複数発音がカンマ区切りで記載されるエントリあり

**WikiPron swe_latn_broad.tsv:**
- エントリ数: 約4,631語
- ソース: Wiktionary
- ライセンス: Apache-2.0
- broad transcription（音韻転写）
- 一部エントリは複数単語フレーズを含む
- ipa-dict とは独立したソースで、クロス検証に有用

### 2.4 サンプリング戦略

- **サンプルサイズ 256件**: 既存パッケージ（ポルトガル語256件、スペイン語256件）と統一。PER計算の統計的安定性に十分な規模
- **等間隔サンプリング**: フィルタ済みリストの総数を256で割ったステップ幅で等間隔に抽出。既存の `refresh_portuguese_eval_data.ps1` 等と同一方式で、スクリプト再実行で同一サンプルが再現される
- **フィルタリングの目的**: 単語レベルG2P評価に不適切なエントリ（フレーズ、数字のみ、空エントリ）を除外し、クリーンな評価セットを作成

### 2.5 tests/TestData/SwedishG2P/README.md

```markdown
# Swedish G2P Test Data

## データソース

| ファイル | ソース | ライセンス | エントリ数 |
|---------|--------|-----------|----------|
| ipa_dict_sv_se_sample.tsv | ipa-dict (Folkets lexikon) | CC BY-SA 2.5 | 256 |
| wikipron_swe_latn_broad_filtered_sample.tsv | WikiPron (Wiktionary) | Apache-2.0 | 256 |

## 再生成手順

\`\`\`powershell
# サンプルTSV再生成
pwsh tools/refresh_swedish_eval_data.ps1

# フルデータセット生成（Sw4用）
pwsh tools/refresh_swedish_eval_data.ps1 -Full
\`\`\`

## 注意事項

- サンプルTSVは等間隔サンプリングで決定的に生成されています
- TSVはGitにコミットされます（評価再現性のため）
- フルデータセットはサイズが大きいため .gitignore 推奨
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| スクリプト実装エージェント | 1 | refresh_swedish_eval_data.ps1 作成、データDL・フィルタ・サンプリングロジック、README.md作成 |

**計1名**。既存の `tools/refresh_portuguese_eval_data.ps1` を直接参考にし、ダウンロードURL・フィルタ条件・出力パスをスウェーデン語用に変更する。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**IN:**
- `tools/refresh_swedish_eval_data.ps1`（データ取得・フィルタ・サンプリング）
- `tests/TestData/SwedishG2P/ipa_dict_sv_se_sample.tsv`（256件）
- `tests/TestData/SwedishG2P/wikipron_swe_latn_broad_filtered_sample.tsv`（256件）
- `tests/TestData/SwedishG2P/README.md`

**OUT:**
- フルデータセット（Sw4で生成: `ipa_dict_sv_se_full.tsv`, `wikipron_swe_latn_broad_filtered_full.tsv`）
- G2Pエンジン実装（SW2-001, SW2-002, SW2-003）
- 評価テスト実装（SW2-005）

### ユニットテスト

本チケット自体のテストは SW2-005 で以下をカバー:

- `ipa_dict_sv_se_sample.tsv` が存在し、256件以上のエントリを含むこと
- `wikipron_swe_latn_broad_filtered_sample.tsv` が存在し、256件以上のエントリを含むこと
- 各TSVの surface フィールドが非空であること
- 各TSVの ipa フィールドが非空であること
- 重複 surface が存在しないこと

### E2Eテスト

- `refresh_swedish_eval_data.ps1` を実行して、期待されるファイルが生成されること（CI環境でのスモークテスト）
- 生成されたTSVファイルが正しいタブ区切りフォーマットであること

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **ipa-dict の声調マーク `²`**: ipa-dict sv.txt には accent 2 を示す `²` マークが含まれる。Sw2 時点ではピッチアクセント予測は未実装（Sw3で実装）のため、PER評価時にアクセントマークを除外するオプションが必要。サンプルTSV自体にはマークを残し、評価テスト側で除去する
2. **ipa-dict の複数発音**: 一部エントリがカンマ区切りで複数発音を持つ（例: `ord /uːrd/, /uːɖ/`）。フィルタリングで最初の発音のみ採用するが、評価時にはどちらかに一致すればOKとする寛容マッチも検討
3. **WikiPron のデータ品質**: Wiktionary由来のため、一部エントリに誤りがある可能性。フィルタリングで明らかに不正なエントリ（IPA文字以外を含む等）を除外する
4. **ネットワーク依存**: スクリプトは外部URLからダウンロードするため、CI環境でのネットワークアクセスが必要。サンプルTSVはGitにコミットしておき、スクリプト実行はオプショナルとする
5. **TSVファイル名の一致**: ポルトガル語G2Pレビュー知見（MEMORY.md参照）で、DatasetEvaluationテストのTSVファイル名はrefreshスクリプトの出力名と正確に合わせる必要がある。ファイル名の不一致でテストが失敗するバグを防ぐ

### レビューチェックリスト

- [ ] ipa-dict/WikiPron のダウンロードURLが正しいか
- [ ] フィルタリング条件が適切か（フレーズ除外、空エントリ除外、重複除去）
- [ ] サンプリングが等間隔方式で決定的（再現性がある）か
- [ ] 出力TSVのフォーマットが `surface\tipa` のタブ区切りであるか
- [ ] TSVファイル名が SW2-005 の SwedishDatasetEvaluationTests.cs で参照する名前と一致するか
- [ ] README.md にデータソース・ライセンス・再生成手順が記載されているか
- [ ] サンプルTSVがGitにコミット対象となっているか（.gitignore に含まれていないか）
- [ ] ipa-dict の声調マーク `²` がサンプルTSVに保持されているか（評価テスト側で除去する方針）
- [ ] スクリプトがエラー時に適切なメッセージを出力するか

## 6. ゼロから作り直すとしたら

1. **サンプリング方法**: 等間隔サンプリングではなく、頻度順・カテゴリ別の層化サンプリングも検討できる（例: 高頻度語50%、低頻度語50%）。ただし既存パッケージとの一貫性と簡潔さを優先して等間隔方式を採用
2. **TSVの列構成**: 現在は `surface\tipa` の2列だが、`frequency`（頻度）や `category`（品詞）列を追加すれば、精度分析の粒度が上がる。ただしipa-dict/WikiPronにはこの情報がないため、追加は困難
3. **フルデータとサンプルの分離**: Sw2 ではサンプル（256件）のみ、Sw4 でフル（21k/4.6k件）を追加する段階的アプローチ。最初からフルデータを生成してサンプルを部分集合とする設計もあるが、テスト実行時間とGitリポジトリサイズの観点からサンプル優先が妥当

## 7. 後続タスクへの連絡事項

- **SW2-005（テスト）**: SwedishDatasetEvaluationTests.cs で以下のファイル名を正確に参照すること
  - `tests/TestData/SwedishG2P/ipa_dict_sv_se_sample.tsv`
  - `tests/TestData/SwedishG2P/wikipron_swe_latn_broad_filtered_sample.tsv`
  - ファイル名の不一致は即テスト失敗につながる（ポルトガル語での既知問題、MEMORY.md参照）
- **SW2-005（PER閾値）**: Sw2 時点のPER閾値は ipa-dict base < 8%、no_exceptions < 15%。声調マーク `²` を除外した比較を行う評価プロファイルを用意すること
- **Sw4（フルデータセット）**: `refresh_swedish_eval_data.ps1 -Full` でフルデータセットを生成する機能を予め組み込んでおくこと。Sw4 では `ipa_dict_sv_se_full.tsv`（21,107件）と `wikipron_swe_latn_broad_filtered_full.tsv`（4,631件）を追加する
- **Sw4（評価ツール）**: `tools/DotNetG2P.SwedishEval/` はフルデータセットを使用するCLIツール。refresh スクリプトで生成されたTSVをそのまま入力として使えるよう、フォーマットを統一する
