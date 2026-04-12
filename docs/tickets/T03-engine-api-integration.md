---
ticket: T03
title: ChineseG2PEngine ToMisakiIpa API 追加
milestone: Mi2
status: 未着手
depends_on: [T02]
blocks: [T04]
---

# T03: ChineseG2PEngine ToMisakiIpa API 追加

## 1. タスク目的とゴール

ChineseG2PEngine に Misaki 互換 IPA 出力 API を追加し、ユーザーが以下のように呼び出し可能にする。

```csharp
var engine = new ChineseG2PEngine();
string ipa = engine.ToMisakiIpa("你好");
// => Misaki互換のIPA文字列が返る

string ipaNoTones = engine.ToMisakiIpa("你好", includeTones: false);
// => 声調なしMisaki互換IPA

var batch = engine.ToMisakiIpaBatch(new[] { "你好", "世界" });
// => 複数テキストの一括変換
```

T01-T02 で実装済みの `PinyinToMisaki.cs`（`Conversion/` 配下の内部静的クラス）を利用し、既存の piper-plus 互換 IPA (`ToPiperIPA`) と同一のパイプラインパターンで公開 API を追加する。

## 2. 実装する内容の詳細

### 2.1 既存パターンの分析

ChineseG2PEngine は以下の共通パイプラインで全出力形式を統一している。

#### RunPipeline パターン（文字列出力）

```csharp
private string RunPipeline(string text, Func<string, string> converter)
{
    ThrowIfDisposed();

    if (string.IsNullOrWhiteSpace(text))
        return "";

    var entries = CollectPinyins(text);       // Step1: ピンイン収集

    if (_options.EnableToneSandhi)
        ApplyToneSandhiToEntries(entries);     // Step2: 声調変調

    return FormatOutput(entries, converter);   // Step3: コンバータ適用 + 文字列整形
}
```

`FormatOutput` は `_options.Separator` を使って音節間を区切る。各ピンインエントリに対して `converter(entry.Pinyin)` を呼び出す。

#### ToPiperIPA の実装例（参考実装）

```csharp
// 単一テキスト: RunPipelineにラムダでコンバータを渡す
public string ToPiperIPA(string text)
{
    return RunPipeline(text, p => PinyinToPiperIpa.Convert(p));
}

// バッチ: ThrowIfDisposed() + BatchConversionHelper.ConvertToList
public IReadOnlyList<string> ToPiperIPABatch(string[] texts)
{
    ThrowIfDisposed();
    return BatchConversionHelper.ConvertToList(texts, ToPiperIPA);
}
```

#### ToIPA の実装例（includeTonesパラメータ付き参考実装）

```csharp
// 声調あり（デフォルト）
public string ToIPA(string text)
{
    return ToIPA(text, true);
}

// 声調制御付き: RunPipelineにクロージャでincludeTonesをキャプチャ
public string ToIPA(string text, bool includeTones)
{
    return RunPipeline(text, p => PinyinToIpa.Convert(p, includeTones));
}

// バッチ（声調あり）: 単純委譲
public IReadOnlyList<string> ToIPABatch(string[] texts)
{
    ThrowIfDisposed();
    return BatchConversionHelper.ConvertToList(texts, ToIPA);
}

// バッチ（声調制御付き）: ConvertToListの3引数オーバーロードで静的メソッド委譲
public IReadOnlyList<string> ToIPABatch(string[] texts, bool includeTones)
{
    ThrowIfDisposed();
    return BatchConversionHelper.ConvertToList(
        texts,
        this,
        includeTones,
        ConvertIpaBatchItem);
}

// 静的バッチアイテム変換（ラムダのアロケーション回避）
private static string ConvertIpaBatchItem(ChineseG2PEngine engine, string text, bool includeTones)
{
    return engine.ToIPA(text, includeTones);
}
```

### 2.2 追加する API メソッド

以下の4メソッドを `ChineseG2PEngine.cs` に追加する。配置場所は piper-plus 互換 IPA セクションの直後（PUA出力セクションの前）に新しいセクションコメントとともに挿入する。

#### 2.2.1 ToMisakiIpa(string text) — 声調付きデフォルト

```csharp
// =====================================================================
// Misaki 互換 IPA 出力
// =====================================================================

/// <summary>
/// テキストを Misaki 互換 IPA 文字列に変換する（声調マーカー付き）。
/// </summary>
/// <param name="text">入力テキスト</param>
/// <returns>Misaki 互換 IPA 文字列</returns>
public string ToMisakiIpa(string text)
{
    return ToMisakiIpa(text, true);
}
```

#### 2.2.2 ToMisakiIpa(string text, bool includeTones) — 声調制御付き

```csharp
/// <summary>
/// テキストを Misaki 互換 IPA 文字列に変換する。
/// </summary>
/// <param name="text">入力テキスト</param>
/// <param name="includeTones">声調マーカーを含めるかどうか</param>
/// <returns>Misaki 互換 IPA 文字列</returns>
public string ToMisakiIpa(string text, bool includeTones)
{
    return RunPipeline(text, p => PinyinToMisaki.Convert(p, includeTones));
}
```

**ポイント**: `RunPipeline` にラムダを渡すパターンは `ToIPA(text, includeTones)` と完全に同一。`PinyinToMisaki.Convert` は T01-T02 で実装済みの `PinyinToMisaki` 静的クラスの変換メソッドで、シグネチャは `public static string Convert(string pinyin, bool includeTones)` を想定する。

#### 2.2.3 ToMisakiIpaBatch(string[] texts) — バッチ（声調付きデフォルト）

```csharp
/// <summary>
/// 複数テキストを一括で Misaki 互換 IPA に変換する（声調マーカー付き）。
/// </summary>
/// <param name="texts">入力テキストの配列</param>
/// <returns>各テキストに対応する Misaki 互換 IPA 文字列のリスト</returns>
public IReadOnlyList<string> ToMisakiIpaBatch(string[] texts)
{
    ThrowIfDisposed();
    return BatchConversionHelper.ConvertToList(texts, ToMisakiIpa);
}
```

**ポイント**: `ToPiperIPABatch` と同一パターン。`ThrowIfDisposed()` の後、`BatchConversionHelper.ConvertToList` にメソッドグループ `ToMisakiIpa`（引数1つのオーバーロード）を渡す。`ConvertToList<TResult>(IReadOnlyList<string>, Func<string, TResult>)` オーバーロードが使われる。

#### 2.2.4 ToMisakiIpaBatch(string[] texts, bool includeTones) — バッチ（声調制御付き）

```csharp
/// <summary>
/// 複数テキストを一括で Misaki 互換 IPA に変換する。
/// </summary>
/// <param name="texts">入力テキストの配列</param>
/// <param name="includeTones">声調マーカーを含めるかどうか</param>
/// <returns>各テキストに対応する Misaki 互換 IPA 文字列のリスト</returns>
public IReadOnlyList<string> ToMisakiIpaBatch(string[] texts, bool includeTones)
{
    ThrowIfDisposed();
    return BatchConversionHelper.ConvertToList(
        texts,
        this,
        includeTones,
        ConvertMisakiIpaBatchItem);
}
```

#### 2.2.5 静的バッチヘルパーメソッド

ファイル末尾のバッチヘルパー領域（`ConvertIpaBatchItem` 等が並んでいる箇所）に追加する。

```csharp
private static string ConvertMisakiIpaBatchItem(ChineseG2PEngine engine, string text, bool includeTones)
{
    return engine.ToMisakiIpa(text, includeTones);
}
```

**ポイント**: `ConvertIpaBatchItem`/`ConvertZhuyinBatchItem` と同一パターン。`BatchConversionHelper.ConvertToList<TContext, TState, TResult>` オーバーロードに渡す静的メソッドで、ラムダのデリゲートアロケーションを回避する設計。

### 2.3 PinyinToMisaki.Convert の想定シグネチャ

T01-T02 で `src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs` に実装済みのクラス。以下のシグネチャを前提とする。

```csharp
namespace DotNetG2P.Chinese.Conversion  // ← 既存の PinyinToPiperIpa と同じ名前空間
{
    internal static class PinyinToMisaki
    {
        /// <summary>
        /// ピンイン文字列をMisaki互換IPAに変換する。
        /// </summary>
        /// <param name="pinyin">声調記号付きまたは声調数字付きのピンイン文字列。</param>
        /// <param name="includeTones">声調マーカーを含めるかどうか。</param>
        /// <returns>Misaki互換IPA表記文字列。</returns>
        public static string Convert(string pinyin, bool includeTones);
    }
}
```

もし T02 の実装で `Convert` メソッドのシグネチャが `Convert(string pinyin)` のみ（声調制御なし）の場合は、`includeTones` パラメータ付きオーバーロードの追加を T02 の実装者に依頼すること。

### 2.4 using ディレクティブの追加

`ChineseG2PEngine.cs` の先頭で `DotNetG2P.Chinese.Conversion` は既に `using` されている（`PinyinToPiperIpa` 等を使用）。`PinyinToMisaki` は同じ名前空間に配置するため、追加の `using` は不要。

```csharp
using DotNetG2P.Chinese.Conversion;  // ← 既存。PinyinToMisaki もここに含まれる
```

### 2.5 挿入位置の詳細

`ChineseG2PEngine.cs` のセクション構成に従い、以下の位置に挿入する。

```
// =====================================================================
// piper-plus 互換 IPA 出力  (既存: L248-L291)
// =====================================================================
    ToPiperIPA(string text)
    ToPiperIpaPhonemes(string text)

// =====================================================================
// Misaki 互換 IPA 出力  ← ★ここに新セクションを挿入
// =====================================================================
    ToMisakiIpa(string text)
    ToMisakiIpa(string text, bool includeTones)

// =====================================================================
// PUA 出力  (既存: L295-L350)
// =====================================================================
    ToPuaPhonemes(string text)
    ToPuaString(string text)
```

バッチ API セクション内では、`ToPiperIPABatch` の直後に `ToMisakiIpaBatch` 2メソッドを追加する。

静的バッチヘルパーは `ConvertIpaWithProsodyBatchItem` の直後に `ConvertMisakiIpaBatchItem` を追加する。

## 3. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|----------|
| 実装エージェント | 1名 | ChineseG2PEngine.cs への4メソッド + 1静的ヘルパーの追加。コンパイル確認 |
| レビューエージェント | 1名 | API 一貫性（命名規則、XMLDoc、Dispose チェック）、既存パターンとの整合性確認 |

合計: 2名

T01-T02 が完了した `PinyinToMisaki.cs` を前提とするため、実装自体は既存パターンの踏襲であり、作業量は小規模。1名の実装エージェントで十分対応可能。

## 4. 提供範囲とテスト項目

### 4.1 API 統合テスト（パイプライン全体通し）

以下のテストを `tests/DotNetG2P.Tests/` 配下に追加する（具体的なテストクラス名・配置は T04 で決定）。

| テスト項目 | 内容 | 検証ポイント |
|------------|------|-------------|
| 基本変換 | `engine.ToMisakiIpa("你好")` が空でない Misaki 互換 IPA を返す | パイプライン全体が接続されている |
| 声調付き | `engine.ToMisakiIpa("你好", true)` に声調マーカーが含まれる | includeTones=true の動作 |
| 声調なし | `engine.ToMisakiIpa("你好", false)` に声調マーカーが含まれない | includeTones=false の動作 |
| デフォルト声調 | `ToMisakiIpa(text)` と `ToMisakiIpa(text, true)` が同一結果 | デフォルト引数の一貫性 |
| 声調変調 | 三声連続（"你好"）で声調変調が適用される | EnableToneSandhi 連携 |
| 句読点区切り | `"你好，世界"` で句読点前後が正しく区切られる | FormatOutput の区切り処理 |
| 空文字入力 | `ToMisakiIpa("")` → `""` | 空入力ガード |
| null入力 | `ToMisakiIpa(null)` → `""` | null ガード（RunPipeline の IsNullOrWhiteSpace） |
| ASCII混在 | `"Hello你好"` で ASCII 部分がそのまま、漢字部分が IPA | 非漢字スルー |
| Separator 設定 | `Separator = "-"` のオプションで区切り文字が変わる | Options.Separator の反映 |

### 4.2 バッチ API テスト

| テスト項目 | 内容 | 検証ポイント |
|------------|------|-------------|
| バッチ基本 | `ToMisakiIpaBatch(new[] {"你好", "世界"})` が2要素を返す | バッチ変換の動作 |
| バッチ声調制御 | `ToMisakiIpaBatch(texts, false)` で全要素が声調なし | includeTones バッチ転送 |
| バッチ空配列 | `ToMisakiIpaBatch(Array.Empty<string>())` が空リスト | 空配列ガード |
| バッチ null | `ToMisakiIpaBatch(null)` で `ArgumentNullException` | BatchConversionHelper の null チェック |

### 4.3 異常系テスト

| テスト項目 | 内容 | 検証ポイント |
|------------|------|-------------|
| Dispose 後呼び出し | `engine.Dispose(); engine.ToMisakiIpa("你好")` → `ObjectDisposedException` | ThrowIfDisposed |
| Dispose 後バッチ | `engine.Dispose(); engine.ToMisakiIpaBatch(texts)` → `ObjectDisposedException` | バッチ側の ThrowIfDisposed |

## 5. 実装に関する懸念事項とレビュー項目

### 5.1 Dispose 済みチェック

- `ToMisakiIpa(string)` は `RunPipeline` 内で `ThrowIfDisposed()` が呼ばれるため、明示的なチェックは不要
- `ToMisakiIpa(string, bool)` も同様に `RunPipeline` 経由で保護される
- `ToMisakiIpaBatch` は `RunPipeline` の前に `ThrowIfDisposed()` を明示的に呼ぶ（既存バッチ API と同一パターン）。これは `BatchConversionHelper.ConvertToList` が内部で個別に `ToMisakiIpa` を呼ぶ前にまずエンジンの状態を検証するため
- レビュー時に `ThrowIfDisposed()` の呼び出し漏れがないことを確認すること

### 5.2 スレッドセーフティ

- `ChineseG2PEngine` は「辞書はコンストラクタで読み込まれ、以後は読み取り専用」と XMLDoc に明記されている
- `RunPipeline` 内の `CollectPinyins` / `FormatOutput` はローカル変数のみ使用し、インスタンス状態を変更しない
- `PinyinToMisaki.Convert` は静的メソッドかつステートレスである前提（T02 の実装を確認すること）
- `_disposed` フィールドは `Volatile.Read` / `Interlocked.CompareExchange` で安全にアクセスされている
- **レビュー項目**: `PinyinToMisaki` 内に static mutable state（`static Dictionary` への遅延書き込み等）がないことを確認する

### 5.3 Separator 設定との関係

- `RunPipeline` → `FormatOutput` は `_options.Separator` を使って音節間を区切る
- Misaki 互換出力で特別な区切り規則が必要な場合（例: Misaki ではスペース区切りではなく連結する等）、`RunPipeline` ではなく独自の `FormatOutput` 相当を実装する必要がある
- **レビュー項目**: Misaki のフォーマット仕様を確認し、`_options.Separator` をそのまま使って問題ないかを T02 実装者に確認すること。問題がある場合は `RunPipeline` を使わず `ToPiperIpaPhonemes` のように `CollectPinyins` → `ApplyToneSandhiToEntries` → 独自ループの直接実装を検討する

### 5.4 命名規則の一貫性

- 既存: `ToPiperIPA`（"IPA" 全大文字）、`ToIPA`（全大文字）
- 新規: `ToMisakiIpa` — "Ipa" を PascalCase にする理由は、"Misaki" が固有名詞であり "MisakiIPA" だと "KIPA" のように読めてしまうため。ただし、既存の `ToPiperIPA` との整合性から `ToMisakiIPA` も検討すべき
- **レビュー項目**: チーム内で `ToMisakiIpa` vs `ToMisakiIPA` の命名を統一すること。本チケットでは Issue #56 の記載に従い `ToMisakiIpa` を採用する

### 5.5 PinyinToMisaki.Convert のシグネチャ互換性

- 本チケットは `Convert(string pinyin, bool includeTones)` を前提としている
- T02 の実装で `Convert(string pinyin)` のみの場合、以下の対応が必要:
  - (A) T02 に `includeTones` 付きオーバーロードの追加を依頼する（推奨）
  - (B) ChineseG2PEngine 側で `includeTones=false` 時に声調マーカーを除去する後処理を追加する（非推奨: 責務の分離に反する）

## 6. 一から作り直すとしたら

### 6.1 メソッド爆発問題

現在の ChineseG2PEngine は出力形式ごとに専用メソッドが増殖している:

```
ToPinyin / ToPinyinList / ToPinyinBatch / ToPinyinListBatch (各2オーバーロード)
ToIPA / ToIPABatch (各2オーバーロード)
ToZhuyin / ToZhuyinBatch (各2オーバーロード)
ToPiperIPA / ToPiperIpaPhonemes / ToPiperIPABatch
ToPuaPhonemes / ToPuaString / ToPuaStringBatch
ToIpaWithProsody / ToIpaWithProsodyBatch (各2オーバーロード)
ToMisakiIpa / ToMisakiIpaBatch (各2オーバーロード)  ← 今回追加
```

合計 30 メソッド以上のフラットな API サーフェスとなり、今後さらに出力形式が増えると管理が困難になる。

### 6.2 戦略パターンによる統一設計

出力形式を `IChineseOutputFormat` インターフェースで抽象化する:

```csharp
public interface IChineseOutputFormat<TResult>
{
    TResult Convert(string pinyin, PinyinSyllable syllable);
}

// 各形式を個別のクラスとして実装
public sealed class MisakiIpaFormat : IChineseOutputFormat<string> { ... }
public sealed class PiperIpaFormat : IChineseOutputFormat<string> { ... }
public sealed class ZhuyinFormat : IChineseOutputFormat<string> { ... }
```

エンジン側は汎用メソッド1つで対応:

```csharp
public string Convert<TFormat>(string text) where TFormat : IChineseOutputFormat<string>, new()
{
    return RunPipeline(text, new TFormat().Convert);
}
```

**利点**: 新形式追加時にエンジンクラスの変更が不要（Open-Closed Principle）。
**欠点**: ジェネリクスが .NET Standard 2.1 の型制約に縛られる。Unity IL2CPP との相性問題の可能性。

### 6.3 ビルダーパターンによるフルエント API

```csharp
var result = engine.Convert("你好")
    .ToFormat(OutputFormat.MisakiIpa)
    .WithTones(true)
    .WithSeparator(" ")
    .Execute();
```

**利点**: メソッド数が爆発しない。オプションの組み合わせを柔軟に表現可能。
**欠点**: 中間オブジェクトのアロケーションが発生する。既存 API との後方互換性を維持しながらの導入が複雑。

### 6.4 現実的な判断

現時点では既存パターンの踏襲（専用メソッド追加）が最も安全。理由:
- 既存ユーザーとの後方互換性を維持できる
- Unity IL2CPP 環境でのジェネリクス問題を回避できる
- 各メソッドの IntelliSense / XMLDoc が明確
- 出力形式の総数は有限（現実的に 10 種類程度が上限）

ただし、将来的に出力形式が 10 種類を超える場合は、戦略パターンへのリファクタリングを検討すべき。その際は既存メソッドを `[Obsolete]` にせず、内部で戦略パターンに委譲する形にすれば後方互換性を維持できる。

## 7. 後続タスクへの連絡事項

T04（テスト実装）担当者への伝達事項:

1. **テストクラスの配置**: 既存の `ChineseG2PEngineTests` に追加するか、`ChineseG2PMisakiTests` として分離するかはテストファイルの規模に応じて判断する。既存テストファイルのパターンを確認すること
2. **PinyinToMisaki 単体テスト**: 本チケット（T03）は API 統合のみ。`PinyinToMisaki.Convert` の単体テスト（個別ピンイン→IPA 変換の正確性）は T02 のテストスコープに含まれるが、T04 でも統合テストとして通しで検証すること
3. **期待値の取得方法**: Misaki 互換 IPA の期待値は、Python の misaki ライブラリの出力と比較して決定する。T02 の実装で参照した変換テーブルに基づいて期待値を作成すること
4. **声調変調の検証**: `"你好"` は三声連続（nǐ + hǎo）で声調変調が適用される（nǐ → ní）。声調変調後の Misaki IPA 出力が正しいことを検証するテストを必ず含めること
5. **Separator テスト**: `ChineseG2POptions` の `Separator` プロパティが Misaki 出力にも反映されることを確認するテストを含めること。デフォルト（スペース区切り）と カスタム区切り文字の両方をテストする
6. **Dispose テスト**: `ObjectDisposedException` のテストは `Assert.Throws<ObjectDisposedException>` で検証する。単一 API とバッチ API の両方で確認すること
7. **バッチ API の戻り値型**: `ToMisakiIpaBatch` の戻り値は `IReadOnlyList<string>` であることに注意。`List<string>` や `string[]` ではない

## 8. 紐づけ

- **マイルストーン**: Mi2（Misaki 互換中国語出力）
- **依存**: T02（PinyinToMisaki.cs の実装完了が前提）
- **後続**: T04（テスト実装 -- 本チケットの API に対する統合テスト・異常系テスト）
- **関連 Issue**: #56（Misaki 互換中国語出力の要望）
