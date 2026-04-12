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

### API設計の追加レビュー

本節は API 設計エンジニア視点での踏み込んだレビューである。§6.1〜§6.4 の方針は現実解として妥当だが、以下の点で不足がある。

#### A. 現行記載内容の評価

| 項目 | §6 の評価 | 本レビューでの補足 |
|------|----------|--------------------|
| メソッド数の把握 | 「30 メソッド以上」と概数 | 実測: 単一17 + バッチ11 + 辞書参照3 = **31 public メソッド**(`ChineseG2PEngine.cs` L104-L603)。`EnglishG2PEngine` (11 public) と比較して **約3倍**。今回の ToMisakiIpa 追加で 35 メソッドに増える |
| メソッド爆発の原因分析 | 「出力形式ごとに増殖」のみ | 実際には **3軸の直積** — (出力形式 × 引数バリエーション × 単一/バッチ)。例: IPA は (style × includeTones × 単一/List/Batch) = 8通り。この構造に言及がない |
| 命名の揺れ | `ToMisakiIpa` vs `ToMisakiIPA` のみ議論 | 既に **Chinese=`ToPiperIPA` (全大文字) / English=`ToPiperIpa` (PascalCase)** の破綻が存在 (`ChineseG2PEngine.cs:256` vs `EnglishG2PEngine.cs:345`)。パッケージ間の揺れこそが本質問題。§6 はこの既存矛盾に触れていない |
| 戦略パターン案の具体性 | インターフェース定義のみ | `PinyinSyllable` を渡すシグネチャになっているが、現行 `RunPipeline` は `Func<string,string>` で**文字列のピンイン**を渡している。型が一致せず机上論にとどまっている |
| フルエント API 案 | `ToFormat(OutputFormat.MisakiIpa)` | `enum` ベースだと `ToPinyinList(style)` 型の**配列戻り値**や `ToIpaWithProsody` 型の**構造体戻り値**を統一できない。ジェネリクスや型パラメータの議論が欠落 |
| 段階的導入計画 | なし | 破壊的変更を避けるロードマップ、SemVer 上の位置付け、`[Obsolete]` の扱い方などが未記載 |

#### B. メソッド爆発を解く具体的な型設計

ポイントは「**出力形式** と **出力形状** (string / string[] / ProsodyResult) を分離」することである。現行の Chinese エンジンは両軸を 1 メソッドに畳み込んでいるため爆発している。

```csharp
// ===== 出力形式（何に変換するか）=====
// 拡張性: 新形式は enum 追加 + Strategy 実装のみ。エンジン本体の変更不要
public enum ChineseOutputFormat
{
    PinyinToneMarked,   // nǐ hǎo
    PinyinNumbered,     // ni3 hao3
    PinyinPlain,        // ni hao
    Ipa,                // ni˨˩˦ xɑʊ˨˩˦
    Zhuyin,             // ㄋㄧˇ ㄏㄠˇ
    PiperIpa,           // piper-plus 互換
    MisakiIpa,          // ← T03 で追加される形式
    Pua,                // piper-plus PUA
}

// ===== 変換オプション（副次パラメータ）=====
// readonly struct にすることで Unity IL2CPP でのアロケーション/boxing を回避
public readonly struct ChineseConvertOptions
{
    public bool IncludeTones { get; init; }
    public bool RemoveFunctionWordStress { get; init; }  // 将来の拡張用
    public string? Separator { get; init; }              // null = Engine の既定値
    public static ChineseConvertOptions Default
        => new() { IncludeTones = true };
}

// ===== ストラテジ（内部 interface、publish 不要）=====
// internal にすれば NuGet の public API サーフェスを汚さない
internal interface IPinyinStringConverter
{
    string Convert(string pinyin, in ChineseConvertOptions options);
}

internal static class PinyinConverterRegistry
{
    // static readonly 辞書で O(1) ディスパッチ。ジェネリクス未使用 = IL2CPP 安全
    private static readonly Dictionary<ChineseOutputFormat, IPinyinStringConverter> _map = new()
    {
        [ChineseOutputFormat.Ipa]       = new IpaStrategy(),
        [ChineseOutputFormat.Zhuyin]    = new ZhuyinStrategy(),
        [ChineseOutputFormat.PiperIpa]  = new PiperIpaStrategy(),
        [ChineseOutputFormat.MisakiIpa] = new MisakiIpaStrategy(),
        // ... 他の形式
    };

    public static IPinyinStringConverter Get(ChineseOutputFormat format)
        => _map.TryGetValue(format, out var c) ? c
            : throw new NotSupportedException($"Output format {format} is not supported.");
}

// ===== 具体実装（1 形式 1 クラス = OCP）=====
internal sealed class MisakiIpaStrategy : IPinyinStringConverter
{
    public string Convert(string pinyin, in ChineseConvertOptions options)
        => PinyinToMisaki.Convert(pinyin, options.IncludeTones);
}
```

エンジン側の公開 API は **1 メソッドで集約**できる:

```csharp
// 新しい集約 API（1 メソッドで全形式をカバー）
public string Convert(string text, ChineseOutputFormat format)
    => Convert(text, format, ChineseConvertOptions.Default);

public string Convert(string text, ChineseOutputFormat format, in ChineseConvertOptions options)
{
    var strategy = PinyinConverterRegistry.Get(format);
    return RunPipeline(text, p => strategy.Convert(p, options));
}

// バッチも1メソッド
public IReadOnlyList<string> ConvertBatch(string[] texts, ChineseOutputFormat format)
    => ConvertBatch(texts, format, ChineseConvertOptions.Default);

public IReadOnlyList<string> ConvertBatch(string[] texts, ChineseOutputFormat format, in ChineseConvertOptions options)
{
    ThrowIfDisposed();
    var strategy = PinyinConverterRegistry.Get(format);
    var opt = options; // struct キャプチャのため local copy
    return BatchConversionHelper.ConvertToList(
        texts,
        (engine: this, strategy, opt),
        (text, ctx) => ctx.engine.RunPipeline(text, p => ctx.strategy.Convert(p, ctx.opt)));
}
```

**結果**: 単一/バッチ + 8形式 × (tones付/無) = 旧 32 メソッド → **新 4 メソッド**。Prosody や `string[]` 返しのような **出力形状が異なる API** は無理に統合せず別メソッドとして残す（型システムの限界を素直に受け入れる）。

#### C. Fluent / ビルダー API 設計

§6.3 の `.ToFormat(...).WithTones(...)` は良いアイデアだが、**「変換を遅延する」ビルダー** ではなく **「設定をチェーンで組み立てる」即時ビルダー** とすべき。中間オブジェクトを `readonly struct` にすれば GC プレッシャを回避できる。

```csharp
// Engine の拡張メソッドとして実装（本体を汚さない）
public static class ChineseG2PFluent
{
    public static ChineseConvertRequest For(this ChineseG2PEngine engine, string text)
        => new(engine, text, ChineseOutputFormat.PinyinToneMarked, ChineseConvertOptions.Default);
}

public readonly struct ChineseConvertRequest
{
    private readonly ChineseG2PEngine _engine;
    private readonly string _text;
    private readonly ChineseOutputFormat _format;
    private readonly ChineseConvertOptions _options;

    internal ChineseConvertRequest(ChineseG2PEngine engine, string text,
                                   ChineseOutputFormat format, ChineseConvertOptions options)
    {
        _engine = engine;
        _text = text;
        _format = format;
        _options = options;
    }

    // 形式を切り替える With メソッド群（immutable copy-on-write）
    public ChineseConvertRequest AsIpa()      => With(ChineseOutputFormat.Ipa);
    public ChineseConvertRequest AsZhuyin()   => With(ChineseOutputFormat.Zhuyin);
    public ChineseConvertRequest AsPiperIpa() => With(ChineseOutputFormat.PiperIpa);
    public ChineseConvertRequest AsMisakiIpa()=> With(ChineseOutputFormat.MisakiIpa);

    // オプションの切り替え
    public ChineseConvertRequest WithTones(bool include = true)
        => With(_options with { IncludeTones = include });
    public ChineseConvertRequest WithoutTones()
        => With(_options with { IncludeTones = false });
    public ChineseConvertRequest WithSeparator(string sep)
        => With(_options with { Separator = sep });

    private ChineseConvertRequest With(ChineseOutputFormat f)
        => new(_engine, _text, f, _options);
    private ChineseConvertRequest With(ChineseConvertOptions o)
        => new(_engine, _text, _format, o);

    // 終端（Execute）
    public string ToStringResult()  => _engine.Convert(_text, _format, _options);
    public static implicit operator string(ChineseConvertRequest r) => r.ToStringResult();
}

// 使用例:
//   string ipa = engine.For("你好").AsMisakiIpa().WithoutTones();   // 暗黙変換
//   string s   = engine.For("你好").AsPiperIpa().ToStringResult();
```

**利点**:
- すべて `readonly struct` + immutable chain → GC アロケーションは `string` の boxing 1 回のみ
- IntelliSense で「次に何ができるか」が自然に誘導される
- **既存メソッドと共存可能**（エンジン本体に触れていないため）

**注意点**:
- `For(text)` が予約語との衝突リスク → `Text(text)` や `Request(text)` も候補
- バッチ用は `engine.ForBatch(texts).AsMisakiIpa()` のように別エントリポイントを用意する（`IReadOnlyList<string>` 戻りと型が違うため混在不可）

#### D. 段階的導入計画（SemVer 準拠）

破壊的変更を避けつつ、**3つのリリースサイクル**で移行する:

| 段階 | バージョン | 作業内容 | API 破壊 |
|------|-----------|---------|----------|
| **Phase 1** | v1.10.0 (Minor) | `ChineseOutputFormat` enum / `IPinyinStringConverter` / `Convert(text, format, options)` 追加。既存 `ToIPA` / `ToMisakiIpa` 等はそのまま残し、**内部で新 API に委譲**する。T03 の `ToMisakiIpa` もこの形で追加するだけでよい | なし |
| **Phase 2** | v1.11.0 (Minor) | Fluent API (`engine.For(...).AsMisakiIpa()` 等) を追加。XML ドキュメントで Fluent 版を推奨 | なし |
| **Phase 3** | v1.12.0 (Minor) | 既存個別メソッドに `[Obsolete("v2.0 で削除予定。Convert(text, Format.X) を使用してください", error: false)]` を付与。CHANGELOG で周知 | なし（警告のみ） |
| **Phase 4** | v2.0.0 (Major) | Obsolete 済みメソッドを削除。Fluent + `Convert(...)` の2系統のみに集約 | **あり** |

**ポイント**:
- Phase 1-3 はすべて **Minor** バージョンで完結 → ユーザーコードに影響なし
- Phase 3 で `error: false` にすることで、ビルド警告としてのみ表示される（CI を壊さない）
- `ToMisakiIpa` は Phase 1 の一部として追加される「最後の個別メソッド」と位置付ける

#### E. 他言語モジュールとの整合性

現状の命名不一致を一覧化する:

| 言語 | IPA | X-SAMPA | PiperIpa | Prosody |
|------|-----|---------|----------|---------|
| Chinese | `ToIPA` | なし | **`ToPiperIPA`** | `ToIpaWithProsody` |
| English | `ToIPA` | `ToXSampa` | **`ToPiperIpa`** | `ToIpaWithProsody` |
| Portuguese | `ToIPA` | `ToXSampa` | — | — |
| Spanish | `ToIPA` | `ToXSampa` | — | — |

`ToPiperIPA` (Chinese) と `ToPiperIpa` (English) の **大小文字揺れ** が既に存在する。これはコードレビューで見落とされた既存バグであり、T03 で `ToMisakiIpa` を追加する際に **英語側のスタイル (`ToMisakiIpa`, PascalCase)** に合わせることで、新規追加分は正しい方向に揃う。

**推奨**:
- 今後の命名規則を **「3文字以上の頭字語は PascalCase」** と明文化（.NET 設計ガイドライン準拠: `Xml`, `Html`, `Ipa`）
- Chinese の `ToPiperIPA` は Phase 3 で `[Obsolete]` 化し、`ToPiperIpa` を正規名として併設 → 他言語と完全一致
- `IMultilingualG2PEngine` のような **共通インターフェース** を切って `Convert(text, format)` を言語横断で統一することも検討価値あり（ただし Language ごとに format enum が異なるため、ジェネリクス境界が複雑化する点は注意）

#### F. T03 への即時推奨

本チケット (T03) のスコープはあくまで `ToMisakiIpa` 追加のため、上記リファクタはこのチケットでは**行わない**。ただし以下の点は T03 実装時点で配慮すべき:

1. **命名**: `ToMisakiIpa` (PascalCase) を採用 → 既に §5.4 で決定済みで正解
2. **型シグネチャ**: 将来 Strategy 実装 (`MisakiIpaStrategy`) に置き換えやすいよう、`PinyinToMisaki.Convert` は引き続き **静的メソッド + ステートレス** を維持する
3. **XML ドキュメント**: `<seealso cref="ToPiperIPA"/>` / `<seealso cref="ToIPA"/>` を追加し、相互参照により IntelliSense でのディスカバリを改善
4. **新規 enum 追加の先取り**: T03 では不要だが、次のチケット (T05 以降) で `ChineseOutputFormat` 追加を計画に入れておくと、Phase 1 への移行コストが下がる

以上により、メソッド爆発 / 命名揺れ / 拡張性 の3点を、破壊的変更なしに段階的に解消する道筋が描ける。

### システム統合観点の追加レビュー

本節は、T03 の API 統合設計を「DotNetG2P.Multilingual（多言語ファサード）／Unity UPM／NuGet／KokoroSharp」との連携前提でレビューした結果と、一から作り直す場合の改善案を示す。上記 §API設計の追加レビュー が「クラス内部の型設計」に主眼を置いていたのに対し、本節は **クラスの外** — 上位層・下流ランタイム・配布チャネル — との接続性に焦点を当てる。T03 の成果物は `ChineseG2PEngine` 単体で完結せず、Multilingual / Unity / Kokoro から参照される公開 API となるため、下記観点を追加で検討すべきである。

#### A. Multilingual 層への透過的統合（ICapabilityProvider 拡張案）

現状の `src/DotNetG2P.Multilingual/Internal/CapabilityAdapters.cs` では、`LanguageCapabilityRouter` が `ITextBatchProcessor<string>` と `IIpaTextBatchProcessor` の 2 段階の能力インターフェースで各言語エンジンを薄くラップしている。中国語は `DelegateIpaTextBatchProcessor` に `ToPinyin/ToIPA` の 4 デリゲートを渡して `_primaryProcessors` 辞書に登録されており、他言語と同一パイプラインで動作している。

問題点:

- `IIpaTextBatchProcessor` は「標準 IPA」しか表現できない。`ToMisakiIpa` を ChineseG2PEngine に追加しても、Multilingual 経由で呼び出す経路が存在しない
- Issue #56 の本質は「Kokoro TTS 用の Misaki 互換出力を多言語混在テキストから取得したい」であり、Multilingual 層に Misaki 能力が伝搬しない限り、ユーザーは中英混在テキストを手動でセグメント分割して ChineseG2PEngine を個別にインスタンス化する必要がある
- 将来 Kokoro 対応を英語・日本語に拡張する際、能力インターフェースが爆発する（`IMisakiEnglishTextBatchProcessor` / `IMisakiJapaneseTextBatchProcessor` 等）

改善案（Mi3 以降で実施、T03 時点では API 命名のみ先行確保）:

```csharp
// 既存: ITextBatchProcessor<out TResult> / IIpaTextBatchProcessor
// 追加: Misaki プロファイル（標準 IPA とは別軸の能力）
internal interface IMisakiCapableProcessor
{
    string ConvertToMisakiIpa(string text, bool includeTones = true);
    IReadOnlyList<string> ConvertToMisakiIpaBatch(
        IReadOnlyList<string> texts, bool includeTones = true);
}

// DelegateIpaTextBatchProcessor と同様に DelegateMisakiTextBatchProcessor を追加
// LanguageCapabilityRouter.CreateLazy に Chinese 用の Misaki デリゲートを注入
//   text => lazyChineseEngine.Value.ToMisakiIpa(text),
//   texts => lazyChineseEngine.Value.ToMisakiIpaBatch(texts.ToArray())
```

`LanguageCapabilityRouter.TryGetMisaki(Language, out IMisakiCapableProcessor?)` を追加し、`MultilingualG2PEngine.ToMisakiIpa(string text)` が Chinese セグメントだけを Misaki で処理し、他言語セグメントは現状の IPA でフォールバックする（または将来的に各言語の Misaki 変換で置き換える）。`TryGetMisakiIpa` が `false` を返せばフォールバック経路を明示できる設計にする。

**T03 への具体影響**: 本チケットで追加する `ToMisakiIpa` / `ToMisakiIpaBatch` のシグネチャは、後述の `IMisakiCapableProcessor` の契約と 1:1 で対応させること。具体的には:

- メソッド名は `ToMisakiIpa`（`ToMisakiIPA` ではない）で統一 → 将来の `IMisakiCapableProcessor.ConvertToMisakiIpa` との整合性を担保（これは §API設計の追加レビュー §E の「3文字以上の頭字語は PascalCase」方針とも一致）
- `includeTones` パラメータの既定値は `true` → Kokoro Python の `misaki` デフォルトと一致、`IMisakiCapableProcessor` と揃える
- バッチ版の戻り値は `IReadOnlyList<string>` を維持 → Multilingual の既存契約（`ITextBatchProcessor<string>.ConvertBatch`）と同型なので、将来のアダプタ実装でシグネチャ変換が不要

#### B. Unity IL2CPP strip 対策と AOT 互換性

`src/DotNetG2P.Chinese/ChineseG2PEngine.cs` は既に `[Preserve]` 属性（`UnityEngine.Scripting.PreserveAttribute` 互換）をクラスレベルで付与済みで（L21）、非 Unity ビルドでは `src/DotNetG2P.Chinese/Internal/PreserveAttribute.cs` のシム（`#if !UNITY_5_3_OR_NEWER` ガード付き）が利用される。T03 で追加する新メソッドは以下を遵守すること:

1. **メソッド単位の `[Preserve]` は不要**: クラスレベルで `[Preserve]` を付けた場合、Unity IL2CPP リンカーはそのクラスの `public` メソッドをすべて保持対象とする。T03 で追加する 4 メソッドは `public` なのでクラス属性で保護される
2. **`ConvertMisakiIpaBatchItem` 静的ヘルパーには `[Preserve]` 不要**: `private static` かつ `BatchConversionHelper.ConvertToList<TContext, TState, TResult>` から `Func` デリゲート経由で呼び出される。同じアセンブリ内での静的メソッド参照はリフレクションではなくメタデータ参照のため strip されない（ただし、万一 Unity エディタで `link.xml` ベースのアグレッシブな strip 設定がされている環境では、`[Preserve]` を個別付与する保険策も検討に値する）
3. **AOT 警告に注意**: `Func<ChineseG2PEngine, string, bool, string>` のようなジェネリックデリゲートは .NET Native AOT でコード生成が必要だが、既存 `ConvertIpaBatchItem` と同型なので新たな警告は発生しない。ただし `<IsAotCompatible>true</IsAotCompatible>` を csproj に宣言している場合は、T04 のテストで `dotnet publish -r win-x64 --self-contained true /p:PublishAot=true` を 1 回実行して警告有無を確認することを推奨
4. **トリム警告（IL2026 / IL3050）**: `PinyinToMisaki` が reflection や dynamic code generation を使わない限り、新規警告は発生しない。T02 で実装する `PinyinToMisaki.Convert` がリフレクションベースの辞書ロード（例: `Assembly.GetManifestResourceStream` 経由で辞書を遅延ロード）を含む場合は、`[RequiresUnreferencedCode]` 属性の付与と `ILLink.xml` の更新が必要になる可能性がある
5. **`ILLink.xml` の更新は原則不要**: `[Preserve]` 属性で十分。ただし将来 `IMisakiCapableProcessor` をリフレクションで解決する設計に移行する場合は、`ILLink.xml` に `<type fullname="DotNetG2P.Chinese.ChineseG2PEngine" preserve="all"/>` を明示することを検討

**レビュー項目追加**: T03 実装後、Unity 2022.3 LTS + IL2CPP + iOS/Android Build Target の組み合わせで `engine.ToMisakiIpa("你好")` がランタイムで動作することを簡易確認すること。具体的には UPM パッケージ `com.dotnetg2p.chinese` を含む空の Unity プロジェクトで IL2CPP ビルドし、起動時に例外が出ないことを Log で確認する。Unity Editor の Managed Stripping Level は `Low` / `Medium` / `High` の 3 段階を順に試すことが理想だが、最低でも `Medium`（デフォルト）で通ることを保証する。

#### C. KokoroSharp 統合のサンプルコード（ユーザー視点の期待）

KokoroSharp（https://github.com/Lyrcaxis/KokoroSharp など）は Kokoro TTS モデルの C# 実装で、入力として Misaki 互換 IPA 文字列を想定する。T03 の API がそのまま使える形になっていることが重要である。想定される統合コードは以下:

```csharp
using DotNetG2P.Chinese;
using KokoroSharp;

// DotNetG2P 側: Misaki 互換 IPA を取得
using var g2p = new ChineseG2PEngine();
string misakiIpa = g2p.ToMisakiIpa("你好世界");
// 出力例: "ni↗ xau̯↓ ʂɨ↘ ʨie↘"

// KokoroSharp 側: Misaki IPA を直接フィード
using var tts = new KokoroTTS("kokoro-v1.onnx");
var audioSamples = tts.Synthesize(misakiIpa, voice: "zf_xiaobei");
File.WriteAllBytes("output.wav", audioSamples.ToWav());
```

バッチ処理（字幕合成などで複数行を一括変換する場合）:

```csharp
var lines = new[] { "你好", "世界", "再见" };
IReadOnlyList<string> misakiLines = g2p.ToMisakiIpaBatch(lines);
// 各要素が Misaki 互換 IPA。順序は入力配列と 1:1 対応
foreach (var ipa in misakiLines)
{
    var clip = tts.Synthesize(ipa, voice: "zf_xiaobei");
    // ...
}
```

Multilingual 経由（字幕が中英混在の場合、Mi3 で実装予定の構想）:

```csharp
// 将来の拡張。現時点では Chinese のみ Misaki 対応
using var multi = new MultilingualG2PEngine();
if (multi.TryGetMisakiIpa("你好 Hello 世界", out string? misaki))
{
    // Chinese セグメントは Misaki、English セグメントは標準 IPA or 英語 Misaki
    tts.Synthesize(misaki, voice: "zf_xiaobei");
}
```

**T03 への反映**: 上記サンプルが動作するために本チケットで必要な公開 API は `ToMisakiIpa(string)` / `ToMisakiIpaBatch(string[])` の 2 つのみ（既存スコープに含まれる）。ただし XML ドキュメンテーションコメントに「KokoroSharp 等の Kokoro TTS C# 実装にそのまま入力可能」の 1 行を `<remarks>` に追加すると、IntelliSense で用途が明確になりユーザー体験が向上する。具体的には:

```csharp
/// <summary>
/// テキストを Misaki 互換 IPA 文字列に変換する（声調マーカー付き）。
/// </summary>
/// <remarks>
/// 出力形式は Python の misaki ライブラリと互換性があり、
/// KokoroSharp などの Kokoro TTS C# 実装にそのまま入力可能です。
/// </remarks>
public string ToMisakiIpa(string text) { ... }
```

#### D. NuGet / UPM 両配布における制約

`DotNetG2P.Chinese` は NuGet（`DotNetG2P.Chinese`）と UPM（`com.dotnetg2p.chinese`）の両方で配布される。T03 の変更が両配布チャネルで動作するために以下を確認すること:

1. **NuGet ターゲット**: `.csproj` で `netstandard2.1` ターゲットを維持。T03 の追加メソッドは BCL 標準 API のみ使用するため変更不要
2. **UPM パッケージ同期**: `Packages/com.dotnetg2p.chinese/Runtime/` 配下に `ChineseG2PEngine.cs` のミラーが存在する場合、`tools/sync-shared-internals.ps1` で同期する必要がある（既存プロジェクトの運用パターン）。新規メソッド追加時は sync スクリプトの対象であることを PR 本文で明記すること
3. **.meta 整合性**: Unity `.meta` ファイルは既存クラスに新メソッドを追加する場合は再生成不要。ただし新規ファイル（例: T02 で `PinyinToMisaki.cs` を別ファイルとして配置）を追加する場合は、`.github/workflows/ci.yml` の .meta 整合性チェック（v1.7.0 で導入）でエラーにならないか確認
4. **埋め込みリソースの増加**: 本チケットは新規辞書を追加しないため影響なし。ただし将来 Misaki 固有の変換テーブル（例: `misaki_mapping.tsv`）が必要になった場合、UPM の `Resources/` フォルダに配置するのか StreamingAssets 経由にするのかの方針を事前決定すべき（現状の中国語辞書は `Resources.*` 埋め込みリソース方式）
5. **内部型の可視性**: `PinyinToMisaki` クラスは `internal static` のため、NuGet 公開時にも UPM 配布時にも外部 API サーフェスには現れない。`InternalsVisibleTo("DotNetG2P.Tests")` で T04 から直接参照可能にする場合は、`.csproj` に `<InternalsVisibleTo>` を追加すること。ただし T04 の方針（E2E テストのみで間接検証）ではこの追加は不要

#### E. 将来の他言語 Kokoro 互換追加に備えた命名・構造

Kokoro TTS は中国語以外に日本語・英語・韓国語・スペイン語・フランス語・ポルトガル語・イタリア語・ヒンディー語等に対応する予定がある（2026 年 4 月時点の roadmap）。本 T03 の命名規則が他言語にそのまま展開できる構造になっていることが、将来の拡張コストを大幅に削減する。

**推奨命名規則**（Mi3 以降の全言語で統一）:

| 言語 | API 例 | 備考 |
|------|--------|------|
| 中国語 | `ChineseG2PEngine.ToMisakiIpa(string)` | **T03 で実装** |
| 英語 | `EnglishG2PEngine.ToMisakiIpa(string)` | misaki-en の置換規則を適用 |
| 日本語 | `G2PEngine.ToMisakiIpa(string)` | misaki-ja (OpenJTalk ベース) の置換規則 |
| 韓国語 | `KoreanG2PEngine.ToMisakiIpa(string)` | Hangul-first で Misaki 互換 |
| スペイン語 | `SpanishG2PEngine.ToMisakiIpa(string)` | ipa-dict ベースに Kokoro 変換 |
| フランス語 | `FrenchG2PEngine.ToMisakiIpa(string)` | 例外辞書 + Kokoro 変換 |
| ポルトガル語 | `PortugueseG2PEngine.ToMisakiIpa(string)` | 同上 |
| スウェーデン語 | `SwedishG2PEngine.ToMisakiIpa(string)` | Kokoro 未対応だが将来に備え予約 |

**統一規則**:

- メソッド名: `ToMisakiIpa` で固定（「Ipa」は PascalCase）。`ToMisakiIPA` は採用しない → T03 の §5.4 の決定を全言語に適用（これは §API設計の追加レビュー §E で指摘された既存の `ToPiperIPA` vs `ToPiperIpa` 揺れ問題の修正方向とも一致）
- オーバーロード: `ToMisakiIpa(string text)` と `ToMisakiIpa(string text, bool includeTones)` の 2 種類を全言語で提供
- バッチ版: `ToMisakiIpaBatch(string[] texts)` と `ToMisakiIpaBatch(string[] texts, bool includeTones)`
- 名前空間: 各言語パッケージの `Conversion` 名前空間内に `PinyinToMisaki` / `CmuToMisaki` / `KanaToMisaki` / `HangulToMisaki` のような対応クラスを配置（言語ごとに中間表現は異なるが、クラス名の末尾 `ToMisaki` で命名を揃える）

**T03 の構造的寄与**: 本チケットの実装パターン（`RunPipeline` + 静的変換クラス + `BatchConversionHelper` 利用）は、他言語の Misaki 対応にそのまま再利用可能なテンプレートとなる。コードレビュー時に「この実装パターンが他言語でも再現可能か？」を確認し、困難な箇所があれば T03 の段階で `Conversion/PinyinToMisaki.cs` の命名・責務分離を修正すること。特に `PinyinToMisaki.Convert(pinyin, includeTones)` の静的メソッドシグネチャは、他言語のシングルエントリとして引数型を除いて**完全に同じ形**になる設計を目指す。

**避けるべき命名**:

- `ToKokoroIpa` → Kokoro は TTS 実装名、Misaki は G2P プロセス名なので混同を招く
- `ToIpaForKokoro` → 冗長、読みにくい
- `ToMisakiPhonemes` → 出力は「音素」ではなく IPA 文字列なので不正確（既存の `ToPhonemes` が「音素列」を返すメソッドと衝突する）
- `ToIPA(IpaFormat.Misaki)` のようなオプション経由 → 他言語展開時に `IpaFormat` 列挙型が肥大化し、§API設計の追加レビュー §B の戦略パターン案（`ChineseOutputFormat` enum）と衝突する。Kokoro 互換は独立した "音素系の変換軸" であり、`Format` enum には混ぜないこと

**§API設計の追加レビュー との整合性**: §B の `ChineseOutputFormat.MisakiIpa` と本節の `ToMisakiIpa` メソッドは、**Phase 1 (v1.10.0)** 時点では共存する設計となる — すなわち `ToMisakiIpa(text)` の内部実装が `Convert(text, ChineseOutputFormat.MisakiIpa)` を呼び出す、という委譲関係になる。この 2 つの視点（クラス内部の型設計 / 外部配布・統合）は矛盾せず、同じ Phase 1 計画のもとで同期的に実装可能である。

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
