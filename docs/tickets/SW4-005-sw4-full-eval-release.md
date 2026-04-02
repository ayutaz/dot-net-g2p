# SW4-005: Sw4フル評価 + Multilingualテスト + リリース検証

> **マイルストーン**: Sw4 — Multilingual統合 + 評価ツール + リリース準備
> **前提チケット**: SW4-001, SW4-002, SW4-003, SW4-004（全チケット完了が前提）
> **後続チケット**: なし（Sw4最終チケット）

## 1. タスク目的とゴール

Sw4 マイルストーンの最終チケットとして、フルコーパス評価による PER 目標達成の確認、Multilingual 統合テストの完成、リリース前の総合検証を行う。全テストが pass し、PER が閾値内であることを確認してリリース可能状態とする。

**完了の定義:**
- SwedishDatasetEvaluationTests にフル評価テストが追加され全 pass
- ipa-dict フル PER < 4%（base）, < 3%（allophones）
- WikiPron フル PER < 5%（base）
- MultilingualSwedishTests（20テスト）が全 pass
- MultilingualSwedishMixedLanguageTests（10テスト）が全 pass
- MultilingualSwedishPerformanceTests（5テスト）が全 pass
- `dotnet test DotNetG2P.slnx` で全テスト（既存 + スウェーデン語 + Multilingual）が pass
- リリースブロッカーが存在しない

## 2. 実装内容の詳細

### 2.1 SwedishDatasetEvaluationTests 拡張（フル評価）

```
tests/DotNetG2P.Tests/SwedishG2P/SwedishDatasetEvaluationTests.cs
```

既存の Sw2 サンプル評価テストを拡張し、フルデータセット評価を追加する。

**追加テスト（+10テスト）:**

| テスト名 | データセット | プロファイル | 閾値 |
|---------|------------|-----------|------|
| IpaDict_Full_Base_PER_WithinThreshold | ipa_dict_sv_se_full.tsv (21,107件) | base | < 4% |
| IpaDict_Full_Allophones_PER_WithinThreshold | ipa_dict_sv_se_full.tsv | allophones | < 3% |
| IpaDict_Full_NoExceptions_PER_WithinThreshold | ipa_dict_sv_se_full.tsv | no_exceptions | < 12% |
| WikiPron_Full_Base_PER_WithinThreshold | wikipron_swe_latn_broad_filtered_full.tsv (4,631件) | base | < 5% |
| WikiPron_Full_Allophones_PER_WithinThreshold | wikipron_swe_latn_broad_filtered_full.tsv | allophones | < 4% |
| IpaDict_Full_MinSampleCount | ipa_dict_sv_se_full.tsv | - | >= 21,000件 |
| WikiPron_Full_MinSampleCount | wikipron_swe_latn_broad_filtered_full.tsv | - | >= 4,500件 |
| IpaDict_Full_Central_Vs_Finland_Different | ipa_dict_sv_se_full.tsv | base | Central と Finland で PER が異なる |
| IpaDict_Full_TopErrors_NoSystematicBug | ipa_dict_sv_se_full.tsv | base | 上位エラー語に系統的バグがない |
| FullEval_Performance_WithinTimeout | 全データセット | - | 60秒以内 |

**テストデータファイル（SW4-003 で生成）:**
```
tests/TestData/SwedishG2P/
├── ipa_dict_sv_se_full.tsv              (21,107件)
└── wikipron_swe_latn_broad_filtered_full.tsv  (4,631件)
```

**注意**: フル TSV ファイルはサイズが大きいため、Git LFS または `.gitignore` + CI 生成のパターンを検討する。既存言語（スペイン語/フランス語/ポルトガル語）のパターンに従う。

### 2.2 MultilingualSwedishTests（20テスト）

```
tests/DotNetG2P.Tests/Multilingual/MultilingualSwedishTests.cs
```

| # | テスト名 | 検証内容 |
|---|---------|---------|
| 1 | Language_Swedish_値は7 | `(byte)Language.Swedish == 7` |
| 2 | Language_Swedish_ToString | `Language.Swedish.ToString() == "Swedish"` |
| 3 | Segment_å含むテキスト_Swedishに分類 | `TextSegmenter.Segment("det går bra")` → Swedish |
| 4 | Segment_ochキーワード_Swedishに分類 | `TextSegmenter.Segment("jag och du")` → Swedish |
| 5 | Segment_tackキーワード_Swedishに分類 | `TextSegmenter.Segment("tack så mycket")` → Swedish |
| 6 | Segment_ighet接尾辞_Swedishに分類 | `TextSegmenter.Segment("möjlighet")` → Swedish |
| 7 | Engine_スウェーデン語のみ_正しいIPA出力 | `MultilingualG2PEngine.ToIPA("hej")` → 正しい IPA |
| 8 | Engine_スウェーデン語_ToPhonemes | `MultilingualG2PEngine.ToPhonemes("hej")` → 正しい音素列 |
| 9 | Engine_スウェーデン語_複数語 | `"hej världen"` → 正しい IPA |
| 10 | Engine_日瑞混在_分割される | `"こんにちは hej"` → 日本語 + スウェーデン語 |
| 11 | Engine_英瑞混在_分割される | `"hello hej"` → 英語 + スウェーデン語 |
| 12 | Engine_中瑞混在_分割される | `"你好 hej"` → 中国語 + スウェーデン語 |
| 13 | Engine_韓瑞混在_分割される | `"안녕 hej"` → 韓国語 + スウェーデン語 |
| 14 | Engine_バッチ変換_複数テキスト | バッチ API が正しく動作 |
| 15 | Options_SwedishOptions_正しく保持 | MultilingualG2POptions に SwedishG2POptions が保持される |
| 16 | Options_SwedishOptions_null時デフォルト | null 指定時にデフォルトオプションが使用される |
| 17 | Engine_Dispose_Swedish解放 | Dispose 後に ObjectDisposedException |
| 18 | Engine_Swedish_sj音_正しいIPA | `"sjuk"` → sj 音が正しく出力 |
| 19 | Engine_Swedish_そり舌音_正しいIPA | `"bord"` → そり舌音が正しく出力 |
| 20 | Engine_Swedish_黙字_正しいIPA | `"ljus"` → 黙字が正しく処理 |

### 2.3 MultilingualSwedishMixedLanguageTests（10テスト）

```
tests/DotNetG2P.Tests/Multilingual/MultilingualSwedishMixedLanguageTests.cs
```

| # | テスト名 | 検証内容 |
|---|---------|---------|
| 1 | 日英中韓西仏葡瑞8言語混在 | 全8言語が正しくセグメント分割される |
| 2 | 瑞西混在_ラテン文字共有 | スウェーデン語とスペイン語が分離される |
| 3 | 瑞仏混在_ラテン文字共有 | スウェーデン語とフランス語が分離される |
| 4 | 瑞葡混在_ラテン文字共有 | スウェーデン語とポルトガル語が分離される |
| 5 | 瑞英混在_ASCII共有 | スウェーデン語と英語が分離される |
| 6 | å含む混在テキスト_確定信号 | å による確定信号が機能する |
| 7 | 方言設定_Multilingual経由伝達 | Central/FinlandSwedish がエンジンに正しく伝達 |
| 8 | 複数セグメント_IPA結合 | 混在テキストの IPA 出力が正しく結合される |
| 9 | 空セグメント_スウェーデン語 | 空文字列入力でエラーなし |
| 10 | 長文混在テスト | 50語以上の8言語混在テキストが正しく処理される |

### 2.4 MultilingualSwedishPerformanceTests（5テスト）

```
tests/DotNetG2P.Tests/Multilingual/MultilingualSwedishPerformanceTests.cs
```

| # | テスト名 | 検証内容 |
|---|---------|---------|
| 1 | Lazy初期化_使用まで未初期化 | `IsValueCreated == false` まで SwedishG2PEngine が初期化されない |
| 2 | Lazy初期化_初回アクセスで初期化 | スウェーデン語テキストの最初の処理で初期化される |
| 3 | 初期化時間_閾値内 | SwedishG2PEngine の初期化が 500ms 以内 |
| 4 | バッチ処理速度_閾値内 | 100テキストのバッチ処理が 5秒以内 |
| 5 | メモリ成長_閾値内 | 1000回変換後のメモリ増加量が 10MB 以内 |

### 2.5 リリース検証チェックリスト

全チケット完了後に実施する最終検証:

| 検証項目 | コマンド/手順 |
|---------|-------------|
| ソリューションビルド | `dotnet build DotNetG2P.slnx -c Release` |
| 全テスト実行 | `dotnet test DotNetG2P.slnx` |
| sync-shared-internals チェック | `pwsh tools/sync-shared-internals.ps1 -Check` |
| NuGet パッケージ生成 | `dotnet pack src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj -c Release` |
| NuGet パッケージ検証 | `.nupkg` の中身確認（DLL、埋め込みリソース、依存関係） |
| UPM パッケージ構造確認 | `package.json`, `asmdef`, `*.meta` の整合性 |
| フル評価実行 | `pwsh tools/run_swedish_full_evaluation.ps1` |
| CI パイプライン確認 | GitHub Actions の ci.yml が全ステップ pass |

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| テスト実装担当 | 1名 | SwedishDatasetEvaluationTests 拡張、MultilingualSwedishTests、MixedLanguageTests、PerformanceTests |
| PER 分析担当 | 1名 | フル評価結果の分析、エラー語の調査、閾値の最終調整 |
| リリース検証担当 | 1名 | NuGet/UPM パッケージ検証、CI 確認、リリースブロッカー調査 |

**合計: 3名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `SwedishDatasetEvaluationTests.cs` のフル評価テスト拡張（+10テスト）
- `MultilingualSwedishTests.cs` 新規作成（20テスト）
- `MultilingualSwedishMixedLanguageTests.cs` 新規作成（10テスト）
- `MultilingualSwedishPerformanceTests.cs` 新規作成（5テスト）
- フルデータセット TSV の配置確認
- リリース前最終検証の実施

**スコープ外:**
- SwedishG2PEngine 本体の機能修正（Sw1-Sw3 スコープ）
- PER 未達時の規則/辞書チューニング（PER 未達の場合は別チケットで対応）

### ユニットテスト

本チケットで作成する全テスト（**45テスト** = 10 + 20 + 10 + 5）:

**注意**: milestones.md の Sw4 セクション（643行目付近）に「+50」と記載されているが、正確には **+45** である。milestones.md を後で修正すること。

**SwedishDatasetEvaluationTests 拡張: 10テスト**
- フルデータセット × 3プロファイル（base, allophones, no_exceptions）の PER チェック
- データセット件数チェック、方言比較、系統的バグチェック、パフォーマンスチェック

**MultilingualSwedishTests: 20テスト**
- Language enum 値、TextSegmenter 判定、Engine IPA/Phonemes 出力、混在テキスト分割、オプション伝達、Dispose

**MultilingualSwedishMixedLanguageTests: 10テスト**
- 8言語混在、ラテン文字言語間の分離、確定信号、方言伝達、長文処理

**MultilingualSwedishPerformanceTests: 5テスト**
- Lazy 初期化、初期化時間、バッチ速度、メモリ成長

### E2Eテスト

| テスト | 検証内容 |
|--------|---------|
| CI 全テスト pass | `dotnet test DotNetG2P.slnx` で全言語テスト pass |
| フル評価 pass | `run_swedish_full_evaluation.ps1` が終了コード 0 |
| NuGet パッケージビルド | `dotnet pack` が正常完了 |
| 8言語混在テスト | MultilingualG2PEngine で全8言語が正しく処理される |

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **PER 目標未達のリスク**: ipa-dict base < 4% が達成できない場合、例外辞書の拡充または G2P 規則のチューニングが必要になる。その場合は Sw3 スコープに戻って修正し、再評価する
2. **フル TSV ファイルのリポジトリサイズ**: ipa_dict_sv_se_full.tsv（21,107件）はファイルサイズが大きい可能性がある。既存言語のフル TSV の管理方法（Git 直接 / LFS / CI 生成）に従う
3. **声調アクセントマーク ² の比較方式**: ipa-dict のリファレンスに含まれる `²` マークと、SwedishG2PEngine のアクセント出力形式の整合性。base プロファイルでは両方から除去して比較するが、allophones プロファイルでの扱いを明確にする
4. **MultilingualSharedFixture との統合**: 既存の `MultilingualSharedFixture.cs` で MultilingualG2PEngine の共有インスタンスを管理している場合、スウェーデン語テストもこの Fixture を使用すること
5. **パフォーマンステスト閾値の CI 安定性**: GitHub Actions の runner は性能にばらつきがあるため、パフォーマンステストの閾値は十分なマージンを持たせる（ローカルの2-3倍）
6. **既存 Multilingual テスト（412テスト）のリグレッション**: スウェーデン語追加により既存のテストが影響を受けないことを確認。特に LanguageConsistencyTests、TextSegmenterTests のリグレッションに注意

### レビューチェックリスト

- [ ] SwedishDatasetEvaluationTests のフル評価テストが全 pass
- [ ] ipa-dict フル PER < 4%（base）を達成
- [ ] ipa-dict フル PER < 3%（allophones）を達成
- [ ] WikiPron フル PER < 5%（base）を達成
- [ ] MultilingualSwedishTests の 20 テストが全 pass
- [ ] MultilingualSwedishMixedLanguageTests の 10 テストが全 pass
- [ ] MultilingualSwedishPerformanceTests の 5 テストが全 pass
- [ ] 既存の Multilingual テスト（412テスト）が全 pass（リグレッションなし）
- [ ] 既存の全言語テスト（950+ 日本語, 511 英語, 936 中国語, 375 韓国語, 355 スペイン語, 719 フランス語, 1310 ポルトガル語）が全 pass
- [ ] `dotnet test DotNetG2P.slnx` が全テスト pass
- [ ] `dotnet build DotNetG2P.slnx -c Release` が成功
- [ ] `dotnet pack` で DotNetG2P.Swedish の .nupkg が正常生成
- [ ] sync-shared-internals.ps1 -Check が pass
- [ ] フル評価テストのデータファイル名が SW4-003 の出力名と一致
- [ ] MultilingualSharedFixture を使用している（該当する場合）
- [ ] パフォーマンステストの閾値に CI マージンが含まれている

## 6. ゼロから作り直すとしたら

テスト作成は以下の順序で進める:

1. **MultilingualSwedishTests を最初に作成**: SW4-001/002 の統合が正しく動作するかの基本検証。既存の `MultilingualPortugueseTests.cs` をテンプレートとしてコピーし、Portuguese → Swedish に置換、テストデータをスウェーデン語に変更する
2. **MultilingualSwedishMixedLanguageTests**: 既存の `MultilingualKoreanMixedLanguageTests.cs` をテンプレート。8言語混在テストは既存の `MultilingualMixedLanguageTests.cs` の拡張版として作成
3. **MultilingualSwedishPerformanceTests**: 既存の `MultilingualKoreanPerformanceTests.cs` をテンプレート
4. **SwedishDatasetEvaluationTests フル拡張**: 既存のサンプル評価テストに `[Theory]` 属性でフルデータパスを追加。PortugueseDatasetEvaluationTests のフル評価パターンを参照

PER 未達の場合のフォールバック計画:
- エラー上位語を分析し、系統的パターンを特定
- 例外辞書に不足語を追加（50-100語程度で PER 1-2% 改善見込み）
- G2P 規則のエッジケース修正（sj 音パターンの漏れ、複合語境界の誤判定）
- 閾値の緩和は最終手段とし、その場合は根拠をドキュメントに記載

## 7. 後続タスクへの連絡事項

- **Sw4 完了後**: 本チケットが完了し全検証が pass した時点で、Sw4 マイルストーンは完了。リリース PR を作成し、CHANGELOG.md の日付を確定する
- **バージョン番号**: v1.9.0 としてリリース。NuGet / UPM の全パッケージバージョンを統一する
- **CLAUDE.md 最終更新**: 本チケット完了後に以下を反映:
  - スウェーデン語行: `Sw1-Sw4完了`、テスト数（最終値）
  - Multilingual行: テスト数（412 + 35 = 447+）
  - 全体テスト数の更新
- **MEMORY.md 更新候補**: 本チケットで得られた知見（PER 達成値、声調アクセント比較の注意点、信号語衝突の結果等）を MEMORY.md に追記
- **将来の改善候補**: フル評価で PER が高い語のカテゴリ（sj 音パターン、複合語、外来語等）を記録し、将来のマイナーバージョンでの改善候補とする
