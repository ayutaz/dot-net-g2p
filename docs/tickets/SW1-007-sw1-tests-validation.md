# SW1-007: Sw1基本テスト + 精度検証

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-006（SwedishG2PEngine メインAPI）
> **後続チケット**: Sw2チケット群（例外辞書 + 正規化 + X-SAMPA）

## 1. タスク目的とゴール

Sw1マイルストーンの最終チケットとして、SwedishG2PEngine の統合的な精度検証テスト（SwedishAccuracyTests: 25テスト）とエッジケーステスト（SwedishEdgeCaseTests: 5テスト）を作成し、既存の SwedishG2PEngineTests にも統合テストを追加する。Sw1完了条件（hej→hɛj, köpa→ɕøːpa, sjuk→ɧʉːk, bord→buːɖ, ljus→jʉːs）を検証し、マイルストーン完了を確定する。

### 完了状態

- `SwedishAccuracyTests` 25テストがすべてpass
- `SwedishEdgeCaseTests` 5テストがすべてpass
- `SwedishG2PEngineTests` に統合テスト（パイプライン全体の動作確認）が追加されている
- Sw1完了条件の5変換例がすべて正しい出力を返す
- `dotnet test --filter "ClassName~SwedishG2P"` でSw1の全テスト（150+）がpass
- `dotnet build` が警告なしで成功
- `sync-shared-internals.ps1 -Check` がpass

## 2. 実装内容の詳細

### 作成ファイル

#### `tests/DotNetG2P.Tests/SwedishG2P/SwedishAccuracyTests.cs`（25テスト）

エンジン経由の変換精度を検証するキュレーションテスト。`[Theory]` + `[InlineData]` で代表的な単語の変換を網羅的に確認する。

**テスト構成:**

```csharp
[Preserve]
public class SwedishAccuracyTests : IDisposable
{
    private readonly SwedishG2PEngine _engine = new();
    
    // --- 基本語彙 (12テスト) ---
    [Theory]
    [InlineData("hej", "hɛj")]           // 基本挨拶
    [InlineData("tack", "takː")]         // ck→kː
    [InlineData("hus", "hʉːs")]          // u→ʉː（開音節長母音）
    [InlineData("mat", "mɑːt")]          // a→ɑː（開音節長母音）
    [InlineData("hall", "halː")]         // a→a（二重子音前短母音）
    [InlineData("sol", "suːl")]          // o→uː（長母音の非直感的マッピング）
    [InlineData("bok", "buːk")]          // o→uː
    [InlineData("bil", "biːl")]          // i→iː
    [InlineData("hund", "hɵnd")]         // u→ɵ（短母音）
    [InlineData("dag", "dɑːɡ")]         // 基本語
    [InlineData("öl", "øːl")]           // ö→øː
    [InlineData("år", "oːr")]           // å→oː
    
    // --- sj音 (4テスト) ---
    [InlineData("sjuk", "ɧʉːk")]        // sj→ɧ
    [InlineData("sked", "ɧeːd")]        // sk+軟母音→ɧ
    [InlineData("skjorta", "ɧuːʈa")]    // skj→ɧ + rt→ʈ
    [InlineData("stjärna", "ɧɛːɳa")]    // stj→ɧ + rn→ɳ
    
    // --- tj音 (3テスト) ---
    [InlineData("köpa", "ɕøːpa")]        // k+軟母音→ɕ
    [InlineData("kör", "ɕøːr")]          // k+ö→ɕ
    [InlineData("tjugo", "ɕʉːɡu")]      // tj→ɕ
    
    // --- そり舌音 (3テスト) ---
    [InlineData("bord", "buːɖ")]         // rd→ɖ
    [InlineData("barn", "bɑːɳ")]         // rn→ɳ
    [InlineData("karl", "kɑːɭ")]         // rl→ɭ
    
    // --- 黙字 (3テスト) ---
    [InlineData("ljus", "jʉːs")]         // lj→j
    [InlineData("djur", "jʉːr")]         // dj→j
    [InlineData("hjärta", "jɛːʈa")]      // hj→j + rt→ʈ
    
    public void ToIPA_キュレーション語彙_正しい変換(string input, string expected)
}
```

**注意事項:**
- 上記の期待値は暫定的なもの。実装完了後に実際の出力と照合して微調整が必要な場合がある（特に長短母音の判定、ストレスマーク位置）
- ipa-dict (Folkets lexikon) の表記を基準とするが、ipa-dict にはストレスマーク ˈ が含まれる場合がある。Sw1ではストレスマークの有無両方でテストを作成すること
- InlineData の期待値文字列中のIPA記号は正確なUnicodeコードポイントを使用すること（ɡ=U+0261, ɧ=U+0267, ɕ=U+0255, ʈ=U+0288, ɖ=U+0256, ɳ=U+0273, ɭ=U+026D, ʂ=U+0282 等）

#### `tests/DotNetG2P.Tests/SwedishG2P/SwedishEdgeCaseTests.cs`（5テスト）

境界条件とエラーケースの検証。

| テストメソッド | 内容 |
|--------------|------|
| `ToIPA_null入力_空文字列を返す` | `engine.ToIPA(null)` → `""` |
| `ToIPA_空文字入力_空文字列を返す` | `engine.ToIPA("")` → `""` |
| `ToIPA_数字のみ_空文字列または無変換` | `engine.ToIPA("123")` → `""`（Sw1では数字変換なし） |
| `ToIPA_記号のみ_空文字列` | `engine.ToIPA("!@#")` → `""` |
| `ToIPA_非スウェーデン語文字_安全に処理` | `engine.ToIPA("日本語")` → `""`（例外をスローしない） |

#### `tests/DotNetG2P.Tests/SwedishG2P/SwedishG2PEngineTests.cs`（統合テスト追加分）

SW1-006で作成済みの15テストに加え、以下の統合テストを追加（またはSW1-006で未実装のテストを補完）。

| テストメソッド | 内容 |
|--------------|------|
| `ToIPA_Sw1完了条件_hej` | `ToIPA("hej")` → `"hɛj"` |
| `ToIPA_Sw1完了条件_kopa` | `ToIPA("köpa")` → `"ɕøːpa"` |
| `ToIPA_Sw1完了条件_sjuk` | `ToIPA("sjuk")` → `"ɧʉːk"` |
| `ToIPA_Sw1完了条件_bord` | `ToIPA("bord")` → `"buːɖ"` |
| `ToIPA_Sw1完了条件_ljus` | `ToIPA("ljus")` → `"jʉːs"` |
| `ToIPA_複数語文_正しい結合出力` | `ToIPA("hej alla")` → 語ごとのIPA結合 |
| `ToPhonemes_Separator変更_反映` | Separator="." オプション動作確認 |

### 検証対象: Sw1完了条件

マイルストーン計画書に記載された完了条件を網羅的に検証:

| 条件 | 検証方法 | テスト箇所 |
|------|---------|-----------|
| `dotnet build` が成功 | CIで自動検証 | ci.yml |
| `dotnet test --filter "ClassName~SwedishG2P"` で150+ pass | テスト数カウント | SW1-007で最終確認 |
| `ToIPA("hej")` → `"hɛj"` | SwedishAccuracyTests + SwedishG2PEngineTests | 両方で検証 |
| `ToIPA("köpa")` → `"ɕøːpa"` | SwedishAccuracyTests + SwedishG2PEngineTests | 子音軟化 |
| `ToIPA("sjuk")` → `"ɧʉːk"` | SwedishAccuracyTests + SwedishG2PEngineTests | sj音 |
| `ToIPA("bord")` → `"buːɖ"` | SwedishAccuracyTests + SwedishG2PEngineTests | そり舌化 |
| `ToIPA("ljus")` → `"jʉːs"` | SwedishAccuracyTests + SwedishG2PEngineTests | 黙字 |
| `sync-shared-internals.ps1 -Check` pass | CIで自動検証 | ci.yml |
| ipa-dictサンプル(256語) PER < 15% | Sw2で本格検証 | Sw1では手動確認のみ |

### テストデータの準備

Sw1時点ではipa-dictの本格的な評価パイプライン（SwedishDatasetEvaluationTests）はSw2で構築する。SW1-007では以下の手動確認を行う:

1. ipa-dict sv.txt から代表25語を選定し、`SwedishAccuracyTests` の `[InlineData]` として記述
2. 25語中20語以上が完全一致であればSw1のPER目標（< 15%）は達成見込みとみなす
3. 不一致の語はコメントで理由を記録し、Sw2の例外辞書候補としてリスト化する

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| テスト実装エージェント | 1 | SwedishAccuracyTests + SwedishEdgeCaseTests + SwedishG2PEngineTests追加分の実装 |
| 検証エージェント | 1 | 全テスト実行、テスト数カウント（150+確認）、Sw1完了条件の全項目チェック |
| レビューエージェント | 1 | テストの網羅性確認、期待値の言語学的妥当性確認、既存テストパターンとの整合性 |

**推奨**: SW1-006（エンジン）が完成してから着手。テスト実装と検証は順次実行（テスト作成→実行→失敗テストのデバッグ→再実行）。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含む:**
- `SwedishAccuracyTests.cs` — キュレーション精度テスト（25テスト）
- `SwedishEdgeCaseTests.cs` — エッジケーステスト（5テスト）
- `SwedishG2PEngineTests.cs` への統合テスト追加（5-7テスト）
- Sw1完了条件の全項目検証
- 不一致語のリスト化（Sw2例外辞書候補として）

**含まない:**
- ipa-dictフルデータセットによる評価 → Sw2（SwedishDatasetEvaluationTests）
- WikiPronデータセットによる独立検証 → Sw2
- 評価ツール（tools/DotNetG2P.SwedishEval） → Sw4
- パフォーマンステスト → Sw3
- 異音プロファイル別テスト → Sw3

### ユニットテスト

#### `SwedishAccuracyTests.cs`（25テスト）

- 基本語彙: 12テスト（母音の長短、基本子音、ck→kː等）
- sj音パターン: 4テスト（sj, sk+軟母音, skj, stj）
- tj音パターン: 3テスト（k+軟母音, tj）
- そり舌音: 3テスト（rd→ɖ, rn→ɳ, rl→ɭ）
- 黙字: 3テスト（lj→j, dj→j, hj→j）

#### `SwedishEdgeCaseTests.cs`（5テスト）

- null/空文字/数字のみ/記号のみ/非ラテン文字

#### `SwedishG2PEngineTests.cs` 追加分（5-7テスト）

- Sw1完了条件5語の検証 + 複数語文 + オプション動作確認

### E2Eテスト

- Sw1の全コンポーネントが統合された状態で、テキスト入力からIPA出力までの完全なパイプラインを25語で検証
- 各変換パターン（子音軟化、sj音、tj音、そり舌化、黙字、長短母音、相補的数量法則）が正しく動作することを確認
- エッジケース（null、空文字、非アルファベット入力）でクラッシュしないことを確認

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **期待値の確定**: AccuracyTests の `[InlineData]` 期待値はipa-dict (Folkets lexikon) を基準とするが、ipa-dictの表記とDotNetG2P.Swedishの出力形式（ストレスマーク位置、長音記号の有無等）に差異がある可能性が高い。テスト実装時に実際のエンジン出力と照合し、差異がある場合は以下を判断すること:
   - エンジン出力が言語学的に正しい場合 → 期待値をエンジン出力に合わせる
   - ipa-dictが正しい場合 → バグとしてSW1-004/SW1-005に報告
2. **テスト数の150+達成**: SW1-002〜SW1-007の各テストを合計して150+になる必要がある。内訳:
   - SW1-002（SwedishPhonemeTests）: 14テスト
   - SW1-003（SwedishOrthographyTests 20 + SwedishSyllabifierTests 21）: 41テスト
   - SW1-004（GraphemeToPhonemeRulesTests）: 40テスト
   - SW1-005（StressAssignerTests 15 + SwedishIpaTests 15）: 30テスト
   - SW1-006（SwedishG2PEngineTests）: 15テスト
   - SW1-007（SwedishAccuracyTests 25 + SwedishEdgeCaseTests 5）: 30テスト
   - **合計: 170テスト**（150+目標を達成）
3. **Sw1時点のPER**: 例外辞書なしのルール変換のみでipa-dict 256語サンプルのPER < 15%が目標。機能語（och, det, de, mig等）の不規則発音が大きなPER悪化要因になる。Sw1ではこれらは未対応（Sw2の例外辞書で対応）であることを記録し、PER評価からの除外または許容範囲として扱う
4. **Unicodeコードポイントの正確性**: テストコード内のIPA記号が正確なUnicodeコードポイントであることを検証する必要がある。特にIDEの自動補完で類似文字が混入するリスクがある（例: ɡ(U+0261) vs g(U+0067)、ː(U+02D0) vs :(U+003A)）

### レビューチェックリスト

- [ ] AccuracyTestsの25語がスウェーデン語の主要な音韻パターンを網羅しているか（母音長短、子音軟化、sj音、tj音、そり舌化、黙字の各カテゴリ）
- [ ] InlineData の期待値のIPA記号が正確なUnicodeコードポイントか（ipa-dictと同一形式）
- [ ] EdgeCaseTests が null / 空文字 / 数字 / 記号 / 非ラテン文字を網羅しているか
- [ ] テストクラスが `IDisposable` を実装し、`SwedishG2PEngine` を適切にDisposeしているか
- [ ] Sw1完了条件の5変換例がテストに含まれているか
- [ ] テスト総数が150+であることを `dotnet test --filter "ClassName~SwedishG2P" --list-tests` で確認したか
- [ ] 失敗テストがある場合、バグ（SW1-001〜SW1-006起因）か期待値の誤りかが明確に区分されているか
- [ ] Sw2の例外辞書候補リスト（不一致語）がコメントまたは別ファイルで記録されているか
- [ ] テストの命名規則が既存テスト（SwedishG2PEngineTests, GraphemeToPhonemeRulesTests等）と一貫しているか
- [ ] `[Theory]` + `[InlineData]` パターンが既存言語パッケージ（SpanishAccuracyTests等）と同一か

## 6. ゼロから作り直すとしたら

1. **データ駆動テストの外部化**: AccuracyTestsの25語を `[InlineData]` ではなく外部TSVファイルから読み込む方法。`tests/TestData/SwedishG2P/swedish_accuracy_curated.tsv` として管理すれば、テストデータの追加・修正がリコンパイル不要になる。ただし、既存言語パッケージの `AccuracyTests` は `[InlineData]` パターンであり一貫性を優先。Sw2の `DatasetEvaluationTests` でTSVベースの評価に移行するため、Sw1では `[InlineData]` で十分
2. **スナップショットテスト**: 各単語の変換結果をスナップショットファイルに保存し、回帰テストとして使用する方法。変換ルールの変更時にスナップショット更新が必要になるが、意図しない出力変更を検出できる。DotNetG2Pプロジェクトでは採用されていないため見送り
3. **プロパティベーステスト**: FsCheck等を使ったプロパティベーステスト。「全スウェーデン語アルファベット文字列に対してエンジンが例外をスローしない」「出力文字列がIPA文字のみで構成される」等の性質をテスト。テスト品質は向上するが、依存パッケージ追加のコストとプロジェクトの方針を考慮して見送り

## 7. 後続タスクへの連絡事項

### Sw2（例外辞書 + 正規化 + X-SAMPA）担当者へ

- AccuracyTestsで不一致となった語のリストをSw2の例外辞書候補として活用すること。特に以下のカテゴリの語は例外辞書での対応が必要:
  - 機能語の不規則発音: och(/ɔ/), det(/deː/), de/dem(/dɔm/), mig/dig/sig(/mɛj/,/dɛj/,/sɛj/), jag(/jɑː/)
  - フランス語由来外来語: chef, garage, restaurant等のsj音
  - -tion/-sion語尾: station, mission等
  - 子音軟化の例外: kille(k硬い), gem等
- PER評価の基準値: Sw1時点のPER（例外辞書なし）を記録しておくこと。Sw2で例外辞書追加後のPER改善量を測定するためのベースラインとなる
- `SwedishDatasetEvaluationTests` のテストデータ（ipa_dict_sv_se_sample.tsv, 256件）はSw2で作成。`tools/refresh_swedish_eval_data.ps1` スクリプトも同時に作成すること

### Sw3（ピッチアクセント + 方言）担当者へ

- AccuracyTestsの期待値にはピッチアクセント情報（accent 1/2マーク）を含まない。Sw3でAccent付きテストを追加する際は、AccuracyTestsを拡張するのではなく新規テストクラス（SwedishAccentAccuracyTests等）として追加することを推奨
- ipa-dictのスウェーデン語データには声調マーク `²`（accent 2）が含まれる。Sw1のAccuracyTestsではこのマークを無視して比較している。Sw3で声調マークを含むテストを追加する際はipa-dictの表記に合わせること

### 全後続マイルストーン担当者へ

- Sw1のテスト総数の内訳と最終カウントをコミットメッセージまたはPR descriptionに記録すること。Sw2以降のテスト追加で「+100 = 累計250+」等の計算に使用する
- `dotnet test --filter "ClassName~SwedishG2P"` の実行結果（passテスト数、skipテスト数、failテスト数）をSw1完了報告に含めること
