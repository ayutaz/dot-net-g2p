# SW2-005: Sw2データセット評価テスト + 統合検証

> **マイルストーン**: Sw2 — 例外辞書 + テキスト正規化 + X-SAMPA
> **前提チケット**: SW2-001（例外辞書）、SW2-002（正規化）、SW2-003（X-SAMPA + FunctionWordList）、SW2-004（評価データ）
> **後続チケット**: Sw3 チケット群（ピッチアクセント + 方言 + PUA + Prosody のテスト基盤として利用）

## 1. タスク目的とゴール

Sw2 マイルストーンで実装された全コンポーネント（例外辞書、テキスト正規化、NumberToWords、X-SAMPA変換、FunctionWordList）のユニットテスト・データセット評価テストを作成し、Sw2 の完了条件を検証する。

**ゴール**:
- テスト計100+件を追加し、累計250+件（Sw1の150+に加算）を達成
- ipa-dict サンプル（256件）で PER < 8%（base プロファイル）を確認
- ipa-dict サンプルで PER < 15%（no_exceptions プロファイル）を確認
- `dotnet test --filter "ClassName~Swedish"` で全テスト pass

## 2. 実装内容の詳細

### 2.1 追加テストファイル

```
tests/DotNetG2P.Tests/SwedishG2P/
├── SwedishExceptionDictionaryTests.cs      — 例外辞書テスト（14テスト）
├── SwedishNormalizerTests.cs               — 正規化テスト（40テスト）
├── NumberToWordsTests.cs                   — 数値変換テスト（20テスト）
├── SwedishXSampaTests.cs                   — X-SAMPA変換テスト（15テスト）
└── SwedishDatasetEvaluationTests.cs        — データセット評価テスト（10テスト）
```

### 2.2 SwedishExceptionDictionaryTests.cs（14テスト）

```
├── TryLookup_機能語_och_正しい音素列を返す
├── TryLookup_機能語_det_t黙字の音素列を返す
├── TryLookup_機能語_de_不規則音素列dom
├── TryLookup_機能語_dem_deと同音
├── TryLookup_機能語_mig_ig→ej
├── TryLookup_機能語_jag_g弱化
├── TryLookup_フランス語外来語_chef_sj音含む
├── TryLookup_フランス語外来語_garage_sj音含む
├── TryLookup_英語外来語_show_正しい音素列
├── TryLookup_sj例外_station_ɧ含む
├── TryLookup_軟化例外_kille_k硬い
├── TryLookup_地名_Göteborg_不規則発音
├── TryLookup_存在しない語_false返却
└── TryLookup_方言フィルタ_dialect_star_全方言マッチ
```

**重点テスト項目:**
- dialect=`*` のエントリが Central/FinlandSwedish 両方でマッチすること
- dialect 固有エントリが該当方言でのみマッチすること
- stress_index=-1 のエントリで StressAssigner のデフォルトルールにフォールバックすること
- TSVの `|` 音節区切りが正しくパースされること
- コメント行（`#` 始まり）がスキップされること

### 2.3 SwedishNormalizerTests.cs（40テスト）

```
├── NormalizeUnicode_NFD入力_NFC正規化される
├── NormalizeUnicode_大文字_小文字化される
├── ExpandAbbreviations_tex_tillExempel
├── ExpandAbbreviations_dvs_detVillSäga
├── ExpandAbbreviations_bla_blandAnnat
├── ExpandAbbreviations_kl_klockan
├── ExpandAbbreviations_ca_cirka
├── ExpandAbbreviations_osv_ochSåVidare
├── ExpandAbbreviations_mm_medMera
├── ExpandAbbreviations_nr_nummer
├── ExpandOrdinals_1a_första
├── ExpandOrdinals_2a_andra
├── ExpandOrdinals_3e_tredje
├── ExpandOrdinals_10e_tionde
├── ExpandOrdinals_21a_tjugoförsta
├── ExpandDates_ISO形式_正しいスウェーデン語
├── ExpandDates_部分日付_月のみ展開
├── ExpandTimes_1530_femtonTrettio
├── ExpandTimes_kl3_klockanTre
├── ExpandCurrencies_5kr_femKronor
├── ExpandCurrencies_2999kr_クローナとオーレ
├── ExpandCurrencies_100コロン_hundraKronor
├── ExpandPercentages_50percent_femtioProcent
├── ExpandDecimals_3komma14_treKommaFjorton
├── ExpandNumbers_42_fyrtiotvå
├── ExpandNumbers_1000000_enMiljon
├── ExpandNumbers_千区切りスペース_正しく処理
├── ExpandSymbols_at_snabelA
├── ExpandSymbols_ampersand_och
├── ExpandSymbols_percent_procent
├── NormalizeWhitespace_連続スペース_単一化
├── NormalizeWhitespace_先頭末尾空白_trim
├── Normalize_全段階統合_正しい出力
├── Tokenize_正規化済みテキスト_正しいトークン分割
├── Tokenize_空文字入力_空配列
├── Tokenize_null入力_空配列またはArgumentNull
├── EnableTextNormalization_false_正規化スキップ
├── パイプライン順序_通貨が数字より先に処理
├── パイプライン順序_略語が序数より先に処理
└── 二重正規化防止_Tokenize内でNormalize一回のみ
```

### 2.4 NumberToWordsTests.cs（20テスト）

```
├── ToCardinal_0_noll
├── ToCardinal_1_ettデフォルト
├── ToCardinal_1_useEn_en
├── ToCardinal_2から20_正しいスウェーデン語
├── ToCardinal_21_tjugoett
├── ToCardinal_99_nittionio
├── ToCardinal_100_hundra
├── ToCardinal_1000_tusen
├── ToCardinal_1000000_enMiljon
├── ToCardinal_1000000000_enMiljard
├── ToCardinal_複合数_1語出力
├── ToCardinal_1234567_正しい出力
├── ToCardinal_負数_minusFem
├── ToOrdinal_1_första
├── ToOrdinal_2_andra
├── ToOrdinal_3_tredje
├── ToOrdinal_10_tionde
├── ToOrdinal_21_tjugoförsta
├── ToDecimal_3komma14_treKommaFjorton
└── ToDecimal_0komma5_nollKommaFem
```

### 2.5 SwedishXSampaTests.cs（15テスト）

```
├── ToSymbol_長母音_全9音素_正しいXSampa
├── ToSymbol_短母音_全9音素_正しいXSampa
├── ToSymbol_破裂音_全6音素_正しいXSampa
├── ToSymbol_摩擦音_Sj_xBackslash
├── ToSymbol_摩擦音_Tj_sBackslash
├── ToSymbol_鼻音_Ng_大文字N
├── ToSymbol_そり舌音_全5音素_バッククォート付き
├── ToSymbol_Schwa_atSign
├── Convert_発音情報_ストレス付きXSampa文字列
├── Convert_発音情報_ストレスなしXSampa文字列
├── ToXSampa_hej_正しい出力
├── ToXSampa_sjuk_sj音のXSampa
├── ToXSampaWithoutStress_ストレスマークなし
├── ToXSampaBatch_複数テキスト_正しいリスト
└── 全41音素_ラウンドトリップ_IPA変換と整合
```

### 2.6 SwedishDatasetEvaluationTests.cs（10テスト）

```
├── IpaDictSample_FileExists_256件以上
├── IpaDictSample_FormatValid_タブ区切り2列
├── IpaDictSample_NoDuplicateSurface
├── IpaDictSample_BasePER_Under8Percent
├── IpaDictSample_NoExceptionsPER_Under15Percent
├── WikiPronSample_FileExists_256件以上
├── WikiPronSample_FormatValid_タブ区切り2列
├── WikiPronSample_NoDuplicateSurface
├── WikiPronSample_BasePER_Under8Percent
└── MinimumSampleSize_両データセット_256件以上
```

**PER評価の実装詳細:**

```csharp
// base プロファイル: 例外辞書あり、異音なし
// 声調マーク ² を除外して比較
[Theory]
[InlineData("ipa_dict_sv_se_sample.tsv", 0.08)]  // PER < 8%
public void IpaDictSample_BasePER_Under8Percent(string tsvFile, double threshold)
{
    var engine = new SwedishG2PEngine(new SwedishG2POptions(
        enableExceptionDictionary: true,
        enableTextNormalization: false  // TSVは正規化済み
    ));
    
    var entries = LoadTsv(tsvFile);
    double per = CalculatePER(engine, entries, stripToneMarks: true);
    Assert.True(per < threshold, $"PER {per:P2} exceeds threshold {threshold:P2}");
}

// no_exceptions プロファイル: 例外辞書なし
[Theory]
[InlineData("ipa_dict_sv_se_sample.tsv", 0.15)]  // PER < 15%
public void IpaDictSample_NoExceptionsPER_Under15Percent(string tsvFile, double threshold)
{
    var engine = new SwedishG2PEngine(new SwedishG2POptions(
        enableExceptionDictionary: false,
        enableTextNormalization: false
    ));
    
    var entries = LoadTsv(tsvFile);
    double per = CalculatePER(engine, entries, stripToneMarks: true);
    Assert.True(per < threshold, $"PER {per:P2} exceeds threshold {threshold:P2}");
}
```

**PER計算:**
```
PER = Σ(Levenshtein距離(predicted, reference)) / Σ(length(reference))
```
- predicted: SwedishG2PEngine.ToIPA(surface) から声調マーク除去
- reference: TSVの ipa フィールドから声調マーク除去
- Levenshtein距離は音素レベル（文字レベルではない）で計算

**声調マーク除去:**
- ipa-dict は accent 2 を `²` (U+00B2) で記載
- 比較時に `²` と `¹` を除去してPERを計算
- ストレスマーク `ˈ`/`ˌ` は除去しない（ストレス位置もPER対象）

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| テスト実装エージェント | 1 | 5つのテストファイル作成、PER計算ユーティリティ、TSV読み込みヘルパー |

**計1名**。既存パッケージ（ポルトガル語 PortugueseDatasetEvaluationTests 等）のテストパターンを直接参考にできる。PER計算ロジックは共通ユーティリティとして既に存在する可能性があるため、まず既存コードを確認する。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**IN:**
- `SwedishExceptionDictionaryTests.cs`（15テスト）
- `SwedishNormalizerTests.cs`（40テスト）
- `NumberToWordsTests.cs`（20テスト）
- `SwedishXSampaTests.cs`（15テスト）
- `SwedishDatasetEvaluationTests.cs`（10テスト）
- PER計算ユーティリティ（既存のものを利用または新規作成）
- TSV読み込みヘルパー（既存のものを利用または新規作成）

**OUT:**
- SW2-001〜SW2-004 の実装コード自体
- Sw3/Sw4 のテスト（方言テスト、Prosodyテスト、Multilingualテスト等）
- フルデータセット評価（Sw4）

### ユニットテスト

上記 2.2〜2.6 の全100+テストが本チケットのスコープ。

### E2Eテスト

- `dotnet test --filter "ClassName~SwedishExceptionDictionary"` → 全pass
- `dotnet test --filter "ClassName~SwedishNormalizer"` → 全pass
- `dotnet test --filter "ClassName~NumberToWords" & ClassName~Swedish` → 全pass
- `dotnet test --filter "ClassName~SwedishXSampa"` → 全pass
- `dotnet test --filter "ClassName~SwedishDatasetEvaluation"` → 全pass
- `dotnet test --filter "ClassName~Swedish"` → 累計250+ pass

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **PER閾値の妥当性**: Sw2 時点の base PER < 8% は、Sw1 の < 15% からの改善を例外辞書の寄与で達成する想定。300+語の例外辞書で7ポイント以上の改善が得られない場合、閾値の見直しまたは例外辞書のエントリ追加が必要。既存パッケージでは辞書500+語で PER 1-3% を達成しているため、300語でも8%未満は現実的
2. **声調マーク除去の影響**: ipa-dict の声調マーク `²` を除去して比較するため、声調の正確性はPERに反映されない。Sw3 でピッチアクセントを実装した後、声調込みPERを別プロファイルとして追加する
3. **テストデータのTSVファイル名一致**: SW2-004 で生成されるTSVファイル名と、本チケットのテストコード内で参照するファイル名が完全一致している必要がある（MEMORY.md参照）。以下を厳密に使用する:
   - `ipa_dict_sv_se_sample.tsv`
   - `wikipron_swe_latn_broad_filtered_sample.tsv`
4. **NumberToWords のエッジケース**: `long.MaxValue` や `long.MinValue` の変換、0 のordinal（「nollte」は通常使わない）等のエッジケースを検討
5. **正規化パイプラインの順序依存テスト**: 段階間の順序が重要（例: 通貨展開→数字展開の順序）。順序を入れ替えた場合に失敗するテストを含める

### レビューチェックリスト

- [ ] 全100+テストが `dotnet test` で pass するか
- [ ] PER計算が音素レベル（文字レベルではない）のLevenshtein距離で行われているか
- [ ] 声調マーク `²`/`¹` の除去が正しく行われているか
- [ ] TSVファイル名が SW2-004 の出力と完全一致しているか
- [ ] テストの [Theory]/[InlineData] が適切に使用されているか（同種の多数テストは Theory で集約）
- [ ] テストメソッド名が日本語で意図を明確に表しているか
- [ ] null/空文字 の例外テストが含まれているか
- [ ] PER閾値テストが `[Fact]` ではなく `[Theory]` + `[InlineData]` で閾値をパラメータ化されているか
- [ ] テストプロジェクトの参照設定（DotNetG2P.Swedish への ProjectReference）が正しいか
- [ ] 既存の PER計算ユーティリティ・TSV読み込みヘルパーを再利用しているか（重複実装の回避）

## 6. ゼロから作り直すとしたら

1. **テストファイル構成**: 現在の5ファイル構成は機能単位で分割されており、保守性が高い。代替案として、全テストを `SwedishG2PSw2Tests.cs` に集約する方法もあるが、ファイルが巨大化して可読性が低下する。既存パッケージのファイル単位分割パターンを踏襲する
2. **PER評価のプロファイル設計**: `base`/`no_exceptions` の2プロファイルは Sw2 時点で最低限必要。Sw3 で `allophones` プロファイル、Sw4 で声調込みプロファイルを追加する段階的拡張が妥当
3. **テストデータの管理方法**: サンプルTSVをGitにコミットする方式（現行）vs テスト実行時にダウンロードする方式。再現性・オフライン実行を優先してGitコミット方式を採用。ただしフルデータセット（21k件）はサイズが大きいため、Sw4 では Git LFS または .gitignore + CI ダウンロードを検討

## 7. 後続タスクへの連絡事項

- **Sw3（テスト拡張）**: Sw3 では以下のテストファイルを追加・拡張する。Sw2 のテスト基盤（PER計算ユーティリティ、TSVヘルパー等）をそのまま利用する
  - `StressAssignerTests.cs` 拡張: ピッチアクセント予測テスト +20件
  - `AllophoneProcessorTests.cs` 新規: 方言別異音テスト 20件
  - `SwedishAllophoneEvaluationTests.cs` 新規: 異音参照テスト 5件
  - `SwedishProsodyTests.cs` 新規: 韻律テスト 15件
  - `SwedishPuaMappingTests.cs` 新規: PUA変換テスト 10件
  - `SwedishDialectTests.cs` 新規: 方言テスト 15件
- **Sw3（PER閾値更新）**: SwedishDatasetEvaluationTests の PER閾値を base < 4% に引き下げ。`allophones` プロファイルを追加
- **Sw4（フル評価テスト）**: SwedishDatasetEvaluationTests にフルデータセット評価テストを追加（ipa-dict 21k件、WikiPron 4.6k件）。サンプル評価テストはそのまま残す
- **CI/CD**: `dotnet test --filter "ClassName~Swedish"` がCIパイプラインに含まれていることを確認。テスト数250+が pass することを完了条件に含める
