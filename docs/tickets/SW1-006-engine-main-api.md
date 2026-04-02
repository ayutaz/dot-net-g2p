# SW1-006: SwedishG2PEngine メインAPI

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-001〜SW1-005（プロジェクト骨格、Models、音節分割、G2P規則、ストレス+IPA変換）
> **後続チケット**: SW1-007（Sw1基本テスト + 精度検証）

## 1. タスク目的とゴール

SW1-001〜SW1-005で実装した各コンポーネントを統合し、`SwedishG2PEngine` メインAPIクラスを完成させる。これにより `SwedishG2PEngine.ToIPA("hej")` → `"hɛj"` が動作するSw1の最小限G2Pエンジンが利用可能になる。

### 完了状態

- `SwedishG2PEngine` が `IDisposable` を実装し、全Public APIメソッドが動作する
- 内部パイプライン: Tokenize → (ExceptionDict: Sw2で追加) → G2PRules → Syllabify → Stress → Format が一貫して動作する
- `SwedishG2POptions` によるオプション制御（ストレス有無、セパレータ等）が機能する
- `Dispose()` 後のメソッド呼び出しで `ObjectDisposedException` がスローされる
- null/空文字入力に対して安全に空文字列または空リストを返す
- Batch系メソッドが複数テキストを効率的に処理できる

## 2. 実装内容の詳細

### 作成ファイル

#### `src/DotNetG2P.Swedish/SwedishG2PEngine.cs`

Sw1時点のメインエンジン。IDisposable実装。内部で各コンポーネントを組み合わせてG2Pパイプラインを構成する。

**クラス設計:**

```
[Preserve]
public sealed class SwedishG2PEngine : IDisposable
```

**コンストラクタ:**

- `SwedishG2PEngine()` — デフォルトオプションで初期化
- `SwedishG2PEngine(SwedishG2POptions options)` — カスタムオプションで初期化

**内部パイプライン（Sw1時点）:**

```
入力テキスト
  ↓
1. Tokenize（空白区切りの単純分割。Sw2でSwedishNormalizer.Tokenize()に置換）
  ↓
2. [Sw2で追加] ExceptionDictionary.TryLookup()
  ↓
3. GraphemeToPhonemeRules.ConvertWord()
   Phase 1-5 の書記素→音素変換
  ↓
4. SwedishSyllabifier.Syllabify()
   Onset最大化音節分割
  ↓
5. StressAssigner.MarkStress()
   基本ストレス付与
  ↓
6. Format（IpaConverter.Convert() / ConvertPhonemeList()）
```

**Public API（Sw1で提供する全メソッド）:**

| メソッド | シグネチャ | 説明 |
|---------|-----------|------|
| `ToPhonemes` | `string ToPhonemes(string text)` | スペース区切り音素列。複数語はダブルスペースで区切り |
| `ToIPA` | `string ToIPA(string text)` | IPA文字列（ストレスマーク付き） |
| `ToIPAWithoutStress` | `string ToIPAWithoutStress(string text)` | IPA文字列（ストレスマークなし） |
| `ToPhonemeList` | `IReadOnlyList<SwedishPhoneme> ToPhonemeList(string text)` | 構造化された音素リスト |
| `ToSyllables` | `IReadOnlyList<SwedishSyllable> ToSyllables(string word)` | 単語の音節リスト |
| `ToPhonemesBatch` | `IReadOnlyList<string> ToPhonemesBatch(IEnumerable<string> texts)` | バッチ音素変換 |
| `ToIPABatch` | `IReadOnlyList<string> ToIPABatch(IEnumerable<string> texts)` | バッチIPA変換 |
| `Dispose` | `void Dispose()` | リソース解放。二重Disposeに安全 |

**Dispose パターン:**
- `int _disposed` フィールド + `Interlocked.CompareExchange` + `Volatile.Read` で統一（DotNetG2Pプロジェクト共通パターン）
- Dispose後の全publicメソッド呼び出しで `ObjectDisposedException` をスロー
- 二重Disposeは例外なく無視

**内部フィールド:**

```csharp
private readonly SwedishG2POptions _options;
private int _disposed;
```

**注意**: `GraphemeToPhonemeRules`、`SwedishSyllabifier`、`StressAssigner` はいずれも `internal static class` であるため、インスタンスフィールドとして保持しない。メソッド内でstatic呼び出しする。

**単語変換の内部フロー（`ConvertWord` privateメソッド）:**

1. 小文字化（Sw1の簡易正規化。Sw2でNormalizerに移行）
2. `GraphemeToPhonemeRules.ConvertWord(word)` で音素配列を取得
3. `SwedishSyllabifier.Syllabify(phonemes)` で音節分割
4. `StressAssigner.MarkStress(word, syllables)` でストレス付与
5. `SwedishPronunciation` を構築して返却

**複数語テキストの処理:**
- 空白で分割して各単語を `ConvertWord` で変換
- 句読点・数字等の非アルファベット文字はSw1では除去（Sw2でNormalizerが処理）
- 結果はオプションの `Separator`（デフォルト: スペース）で結合。語境界はダブルスペース

#### `src/DotNetG2P.Swedish/SwedishG2POptions.cs`（更新）

Sw1時点のイミュータブルオプションクラス。

```csharp
[Preserve]
public sealed class SwedishG2POptions
{
    public SwedishDialect Dialect { get; }       // Central(default)。Sw1ではCentralのみ使用
    public bool IncludeStress { get; }           // default: true
    public string Separator { get; }              // default: " "
    
    // Sw2で追加予定:
    // public bool EnableTextNormalization { get; }
    // public bool EnableExceptionDictionary { get; }
    
    public SwedishG2POptions(
        SwedishDialect dialect = SwedishDialect.Central,
        bool includeStress = true,
        string separator = " ");
}
```

**設計原則:**
- イミュータブル（全プロパティ get のみ）
- コンストラクタの引数はすべてオプショナル（名前付き引数で指定可能）
- 既存言語パッケージ（SpanishG2POptions, FrenchG2POptions等）と同一パターン

#### `src/DotNetG2P.Swedish/Internal/BatchConversionHelper.cs`

sync-shared-internals で管理される共有ファイル。`tools/sync-shared-internals.ps1` を実行して他言語パッケージから同期する。

**注意**: 手動編集禁止。sync-shared-internals.ps1 -Check がCIで検証される。

#### `src/DotNetG2P.Swedish/Internal/PreserveAttribute.cs`

同上。sync-shared-internals で管理。Unity IL2CPP のストリップ防止用。

**実装パターン参考:** 以下のファイルを直接参考にすること
- `src/DotNetG2P.Spanish/SpanishG2PEngine.cs`
- `src/DotNetG2P.French/FrenchG2PEngine.cs`
- `src/DotNetG2P.Portuguese/PortugueseG2PEngine.cs`

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishG2PEngine + SwedishG2POptions の実装、パイプライン統合 |
| 統合テストエージェント | 1 | SwedishG2PEngineTests の実装（15テスト） |
| レビューエージェント | 1 | コードレビュー、既存エンジンとのAPI整合性確認、Disposeパターン検証 |

**推奨**: SW1-001〜SW1-005の全コンポーネントが完成してから本チケットに着手。統合テストは実装と並行可能（API仕様が確定しているため）。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含む:**
- `SwedishG2PEngine.cs` — メインAPIクラス（IDisposable、全Public API）
- `SwedishG2POptions.cs` — Sw1時点のオプション（Dialect, IncludeStress, Separator）
- `Internal/BatchConversionHelper.cs` — sync-shared-internals同期
- `Internal/PreserveAttribute.cs` — sync-shared-internals同期
- パイプライン統合: Tokenize → G2PRules → Syllabify → Stress → Format
- 対応するユニットテスト（15テスト）

**含まない:**
- テキスト正規化（SwedishNormalizer） → Sw2
- 例外辞書（SwedishExceptionDictionary） → Sw2
- X-SAMPA出力（ToXSampa 等） → Sw2
- PUA出力（ToPuaPhonemes 等） → Sw3
- Prosody API（ToIpaWithProsody） → Sw3
- 方言切替（FinlandSwedish） → Sw3
- 異音処理（AllophoneProcessor） → Sw3
- Multilingual統合 → Sw4

### ユニットテスト

#### `tests/DotNetG2P.Tests/SwedishG2P/SwedishG2PEngineTests.cs`（15テスト）

| テストメソッド | 内容 |
|--------------|------|
| `ToPhonemes_基本単語_期待される音素列` | hej → "h ɛ j" |
| `ToPhonemes_複数語_語境界区切り` | "hej alla" → 語ごとに変換 |
| `ToIPA_基本単語_ストレス付きIPA` | hej → "hɛj" |
| `ToIPA_子音軟化_正しい出力` | köpa → "ɕøːpa" |
| `ToIPA_sj音_正しい出力` | sjuk → "ɧʉːk" |
| `ToIPA_そり舌化_正しい出力` | bord → "buːɖ" |
| `ToIPA_黙字_正しい出力` | ljus → "jʉːs" |
| `ToIPAWithoutStress_ストレスマークなし` | ストレスマーク省略確認 |
| `ToPhonemeList_構造化された音素リスト` | SwedishPhoneme配列の検証 |
| `ToSyllables_正しい音節分割` | 音節数と音素割り当ての確認 |
| `ToPhonemesBatch_複数テキスト_一括処理` | バッチ処理の結果確認 |
| `ToIPABatch_複数テキスト_一括処理` | バッチ処理の結果確認 |
| `Dispose後_ObjectDisposedException` | Dispose後のToIPA呼び出し |
| `null入力_空文字列を返す` | null → "" |
| `空文字入力_空文字列を返す` | "" → "" |

### E2Eテスト

- 完全なパイプラインの動作確認（テキスト入力からIPA出力まで）:
  - `SwedishG2PEngine.ToIPA("hej")` → `"hɛj"`
  - `SwedishG2PEngine.ToIPA("köpa")` → `"ɕøːpa"`（子音軟化）
  - `SwedishG2PEngine.ToIPA("sjuk")` → `"ɧʉːk"`（sj音）
  - `SwedishG2PEngine.ToIPA("bord")` → `"buːɖ"`（そり舌化）
  - `SwedishG2PEngine.ToIPA("ljus")` → `"jʉːs"`（黙字）
- Sw1完了条件の5つの変換例がすべて正しく出力されること
- Disposeパターンの正常動作（二重Dispose、Dispose後の呼び出し）

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **Sw2以降の拡張ポイント**: パイプライン内に例外辞書ルックアップ（Sw2）と異音処理（Sw3）を挿入する拡張ポイントが必要。ConvertWordの内部フローで「例外辞書ヒット時にG2P規則をスキップ」するブランチを設計に含めておくこと。Sw1時点ではフォールスルーで常にG2P規則を通す
2. **Tokenize の簡易実装**: Sw1では空白分割のみ。句読点付きの語（"hej,"等）が混入した場合に句読点が音素変換に渡されてしまう。最低限の句読点除去ロジックは入れるべきか判断が必要
3. **パフォーマンス考慮**: Batch系メソッドは `BatchConversionHelper` を使用して並列化する想定。ただしSw1では例外辞書がないため並列化の効果は限定的。単純なforeachループで十分か
4. **`o` → `/uː/` の非直感的マッピング**: スウェーデン語では `o` の長母音が `/uː/` になる。G2PRules（SW1-004）で処理されるが、エンジン統合テストで明示的に検証すること
5. **sync-shared-internals の同期タイミング**: BatchConversionHelper.cs と PreserveAttribute.cs は他言語パッケージから同期する。`tools/sync-shared-internals.ps1` を実行して最新版を取得し、手動編集は行わないこと

### レビューチェックリスト

- [ ] `IDisposable` パターンが `int _disposed` + `Interlocked.CompareExchange` + `Volatile.Read` で実装されているか（プロジェクト共通パターン）
- [ ] 全publicメソッドの冒頭で `ThrowIfDisposed()` が呼ばれているか
- [ ] 二重Disposeが安全に無視されるか
- [ ] null入力に対して `ArgumentNullException` ではなく空文字列/空リストを返すか（既存言語パッケージと同じ動作）
- [ ] `SwedishG2POptions` がイミュータブルか（setterがないこと）
- [ ] コンストラクタでの初期化処理が適切か（遅延初期化が必要なコンポーネントはないか）
- [ ] Batch系メソッドが `BatchConversionHelper` を使用しているか
- [ ] `[Preserve]` 属性が `SwedishG2PEngine` と `SwedishG2POptions` に付与されているか
- [ ] APIシグネチャが既存言語パッケージ（SpanishG2PEngine, FrenchG2PEngine等）と一貫しているか
- [ ] `sync-shared-internals.ps1 -Check` がpassするか
- [ ] `dotnet build src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj` が警告なしで成功するか
- [ ] Sw1完了条件の5変換例（hej, köpa, sjuk, bord, ljus）が正しい出力を返すか

## 6. ゼロから作り直すとしたら

1. **パイプラインの明示的なステージ抽象化**: 現設計ではConvertWord内で各コンポーネントを順次呼び出す手続き的な実装。代替案として `IPipelineStage<TInput, TOutput>` インターフェースでステージを抽象化し、パイプラインを動的に構成する方法がある。これによりSw2/Sw3での拡張（例外辞書・異音処理の挿入）がより宣言的になる。ただし、既存7言語パッケージがすべて手続き的実装であり、一貫性を重視して現行設計を推奨
2. **非同期API**: `ToIPAAsync` 等の非同期版APIを提供する方法。大量テキスト処理やUnityのメインスレッドブロック回避に有用。ただし .NET Standard 2.1 の制約と既存パッケージとの整合性から、Sw1では見送りが妥当。将来的にはBatchメソッドの内部並列化で対応可能
3. **Builder パターンによるオプション構築**: `SwedishG2POptions` のコンストラクタ引数が増えていく（Sw2でENableTextNormalization, EnableExceptionDictionary、Sw3でEnableAllophones, AllophoneFeatures等）ことを見越して、Builderパターンを採用する方法。既存パッケージとの一貫性を優先して現行のコンストラクタ引数方式を推奨

## 7. 後続タスクへの連絡事項

### SW1-007（Sw1基本テスト + 精度検証）担当者へ

- `SwedishG2PEngine` のコンストラクタはデフォルト引数なしで `new SwedishG2PEngine()` として呼び出し可能。テストコードでのセットアップはこれで十分
- Sw1完了条件の5変換例は `SwedishG2PEngineTests` で検証済みだが、SW1-007ではより広範な精度検証（25語のAccuracyTests）を行うこと
- `ToPhonemes` の出力形式はスペース区切り。語境界はダブルスペース。この形式でipa-dictの期待値と比較すること
- Batch系メソッドは単一テキスト版と同じ結果を返すことをSW1-007でも確認すること
- Dispose関連テストは `SwedishG2PEngineTests` でカバー済み。SW1-007で重複テストは不要

### Sw2（例外辞書 + 正規化 + X-SAMPA）担当者へ

- パイプライン内の例外辞書挿入ポイントは `ConvertWord` メソッド内、G2PRules呼び出しの前。`ExceptionDictionary.TryLookup()` がヒットした場合、G2PRules をスキップして辞書の音素列を使用するブランチを追加する
- Tokenize処理はSw2で `SwedishNormalizer.Tokenize()` に置き換える。現在のSw1実装（空白分割 + 簡易句読点除去）を完全に置換可能な設計
- `SwedishG2POptions` に `EnableTextNormalization` / `EnableExceptionDictionary` プロパティを追加する際、コンストラクタの引数順は既存のものの後に追加すること（後方互換性）
- X-SAMPA出力メソッド（ToXSampa等）の追加は、IpaConverter と同パターンで XSampaConverter を呼び出す形式

### Sw3（ピッチアクセント + 方言 + PUA + Prosody）担当者へ

- 方言対応（SwedishDialect.FinlandSwedish）では、ConvertWord内でG2P規則のPhase 4（そり舌化）をスキップするブランチが必要。_options.Dialect をチェックする分岐点を準備済み
- PUA出力メソッド（ToPuaPhonemes等）は IpaConverter と同パターンで SwedishPuaMapper を呼び出す形式で追加
- Prosody API（ToIpaWithProsody）は SwedishPronunciation にアクセント情報を追加してから SwedishProsodyResult を構築するフローを想定

### Sw4（Multilingual統合）担当者へ

- `SwedishG2PEngine` は `IDisposable` を実装済み。`MultilingualG2PEngine` 内で `Lazy<SwedishG2PEngine>` として保持し、Dispose時にまとめて解放する既存パターンに準拠
- 言語判定（TextSegmenter）でスウェーデン語を検出するシグナル: `å` 文字の検出（ä, ö はドイツ語等と共有のため単独では判定不可）+ `s_swedishWordSignals` セット
