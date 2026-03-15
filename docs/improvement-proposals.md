# DotNetG2P 改善提案書

> 調査日: 2026-03-16
> 対象: release/v1.5.0 ブランチ（ea468fb）
> 調査方法: 10視点レビューによるコードベース・文書整合調査
> レビュー反映: 2026-03-16（10視点レビューで現行リポジトリとの差分を再確認）
> 実装反映: 2026-03-16（main ca6c0d9 + release/v1.5.0 の v1.5.0 release prep ea468fb まで反映）

---

## 目次

1. [エグゼクティブサマリー](#1-エグゼクティブサマリー)
2. [競合分析・市場ポジション](#2-競合分析市場ポジション)
3. [ユースケース・ターゲット市場](#3-ユースケースターゲット市場)
4. [コード品質・アーキテクチャ](#4-コード品質アーキテクチャ)
5. [テスト・CI/CD・品質保証](#5-テストcicd品質保証)
6. [パフォーマンス最適化](#6-パフォーマンス最適化)
7. [機能拡張](#7-機能拡張)
8. [新言語追加候補](#8-新言語追加候補)
9. [パッケージ・エコシステム・セキュリティ](#9-パッケージエコシステムセキュリティ)
10. [ドキュメント・DX](#10-ドキュメントdx)
11. [統合ロードマップ](#11-統合ロードマップ)
12. [付録: 参考リソース](#12-付録-参考リソース)

---

## 1. エグゼクティブサマリー

DotNetG2P は 7 言語対応・総計 6,350 テスト定義（CI 既定レーン `Category!=Performance` では 6,287 件）を持つ、.NET/Unity 向けでは**稀少な純 C# 多言語 G2P 実装**です。2026-03-16 時点では、CI は 3 OS x .NET 8/9 matrix、PR テスト結果公開、coverage summary/comment、DocFX build、AOT/trim smoke test、NuGet Package Validation、CycloneDX SBOM 生成まで導入済みで、BenchmarkDotNet 基盤と Japanese / Multilingual / Romance への拡張、batch API 共通化、capability-based internal adapter、パッケージ別 README、`CONTRIBUTING.md`、`MIGRATION.md`、`ARCHITECTURE.md`、多言語 sample、Dependabot、deterministic build も `main` に反映済みです。release/v1.5.0 ではこれに加えて UPM `package.json` と `CHANGELOG.md` の 1.5.0 release prep まで反映済みです。次の主戦場は Signing / Strong Naming、XML Doc / compiler warning 整理、scheduled benchmark / baseline 比較、新言語拡張です。

以下の8領域で改善の機会を特定しました。

| 領域 | 主要課題 | 推定効果 |
|------|---------|---------|
| **コード品質** | Capability-based internal adapter とバッチAPI共通化は main 反映済み。残りは contract test 横展開と設計文書同期 | 保守性向上、テスト共通化 |
| **テスト・CI/CD** | 3OS x .NET 8/9 CI / coverage / PR結果公開 / DocFX / trim-AOT / package validation / SBOM は導入済み | 品質保証・互換性確保 |
| **パフォーマンス** | BenchmarkDotNet基盤と Japanese / Multilingual / Romance 拡張は完了。残りは定期実行と baseline 比較 | 定量的な最適化判断が可能 |
| **機能拡張** | SSML/ストリーミング/WebAssembly | 市場競争力大幅向上 |
| **新言語** | 11言語候補を調査、Tier 1-4に分類 | 最大14言語対応 |
| **パッケージ** | Dependabot / deterministic build / Package Validation / SBOM / trim-AOT smoke は導入済み。残りは Signing / Strong Naming | 配布互換性・運用強化 |
| **ドキュメント・DX** | パッケージ別README、CONTRIBUTING、MIGRATION、DocFX、サンプル、ARCHITECTURE は整備済み。残りは XML Doc / warning 整理と公開運用 | 新規ユーザー獲得・貢献促進 |
| **競合対策** | 比較表の根拠整備、ユースケース訴求 | 市場認知度向上 |

### 1.1 直近の進捗スナップショット

- 完了: 3 OS x .NET 8/9 CI、PR テスト結果公開、coverage summary/comment、DocFX build、trim/AOT smoke test、Package Validation、SBOM 生成
- 完了: BenchmarkDotNet 基盤と Japanese / Multilingual / Romance 拡張、batch API 共通化、Multilingual の capability-based internal adapter、Core / Multilingual の batch contract テスト補強
- 完了: パッケージ別 README、`CONTRIBUTING.md`、`MIGRATION.md`、`ARCHITECTURE.md`、多言語 sample、Dependabot、deterministic build
- 運用開始: Dependabot による GitHub Actions / NuGet 更新 PR を継続取り込み（2026-03-16 時点で #31 / #32 / #34 / #35 / #36 / #37 / #38、および機能追加 PR #39 / #40 を main へ反映し、release/v1.5.0 では v1.5.0 release prep `ea468fb` まで積まれている）
- 確認済み: 2026-03-16 のローカル検証で `Category!=Performance` レーンは `0 failure / 5,830 pass / 457 skip / 6,287 total`（skip は辞書・外部データ依存ケースを含む）で完了し、`--list-tests` では総計 `6,350` テストを列挙
- 次の優先: Signing / Strong Naming、XML Doc / compiler warning 整理、scheduled benchmark / baseline 比較、新言語拡張または SSML ライトMVP

---

## 2. 競合分析・市場ポジション

> 注: 外部ツール比較は 2026-03-13 時点の公開 README / 公式ドキュメントベースの概況。細部は継続確認が必要。

### 2.1 競合G2Pライブラリ比較

| 項目 | espeak-ng | Phonemizer | Gruut | DeepPhonemizer | **DotNetG2P** |
|------|-----------|-----------|-------|-----------------|-----------|
| 言語数 | 100+ | バックエンド依存 | 言語パッケージ依存 | モデル依存 | **7言語（深い実装）** |
| ライセンス | GPL 3.0 | GPL 3.0 | MIT | Apache-2.0 | **Apache-2.0** |
| 実装言語 | C | Python | Python | Python | **C# (.NET Standard 2.1)** |
| 実行依存 | ネイティブ実装 | バックエンド依存 | Python依存 | PyTorch/ONNX | **.NETのみ（日本語は辞書必要）** |
| Unity対応 | ✗ | ✗ | ✗ | ✗ | **✅ (UPM)** |
| 出力形式 | IPA/phoneme symbols | backend依存 | phoneme/SSML | phoneme sequence | **IPA/X-SAMPA/ピンイン/注音/VOICEVOX等** |
| 低遅延・オフライン | ✅ | △ | △ | ✗(GPU推奨) | **✅** |
| 商用フレンドリー | ✗(GPL) | ✗(GPL) | ✅(MIT) | ✅ | **✅(Apache-2.0)** |

### 2.2 DotNetG2Pの独自価値

1. **C#/.NETエコシステムでは稀少な純C#多言語G2P** — Python/ネイティブバイナリ不要
2. **Unity UPMネイティブ対応** — ゲーム開発者の決定的な差別化ポイント
3. **Apache-2.0で商用利用しやすい** — GPLの制約を回避しやすい
4. **多様な出力形式** — IPA/X-SAMPA/ピンイン/注音/VOICEVOX/HTSラベル等
5. **深い言語学的実装** — 日本語NJDパイプライン（OpenJTalk互換）、独自MeCabエンジン

### 2.3 「広さ」vs「深さ」の差別化

競合を一律に「浅い」と断定するより、DotNetG2P は 7 言語に絞り込みつつ各言語で深い言語学的処理（形態素解析、声調変調、異音規則、方言対応等）を提供する、という打ち出しが適切です。

---

## 3. ユースケース・ターゲット市場

### 3.1 主要ユースケース

| ユースケース | 対象市場 | DotNetG2P対応状況 | 追加必要機能 |
|-----------|---------|------------------|-------------|
| **Unity TTS統合** | ゲーム・VTuber | ✅ UPM対応済み | WebGL対応 |
| **VTuberリアルタイム音声** | 配信者 | ✅ 低遅延バッチAPI | ストリーミングAPI、キャッシング |
| **語学学習アプリ** | 教育 | ✅ IPA/X-SAMPA/ピンイン | 発音スコアリングAPI |
| **アクセシビリティ** | スクリーンリーダー | ✅ 多言語自動判定 | SSML対応 |
| **歌声合成** | UTAU/Synthesizer V | △ 音素出力あり | `ToUtauPhonemes()`, `ToSynthVPhonemes()` |
| **コールセンターIVR** | 企業システム | ✅ ASP.NET Core対応 | gRPCサービス、高スループット |

### 3.2 エコシステム統合

| 統合先 | 実装方法 | 難易度 |
|--------|---------|--------|
| **ASP.NET Core Web API** | Controller + DI | 低 |
| **gRPCサービス** | Protobuf定義 + streaming | 中 |
| **Docker/Kubernetes** | コンテナ化 + Pod autoscaling | 低 |
| **MAUI/Avalonia** | NuGetパッケージ直接参照 | 低 |
| **Blazor WebAssembly** | DotNetG2P.Wasm パッケージ | 中-高 |
| **Azure/GCP Speech API** | フォールバック補完 | 低 |

### 3.3 ターゲット市場（優先順）

1. **Unityゲーム開発者** — VTuber・RPGキャラクターボイス
2. **VTuber・配信者** — リアルタイム音声合成
3. **教育アプリ開発者** — 語学学習、発音矯正
4. **アクセシビリティ企業** — スクリーンリーダー統合
5. **歌声合成開発者** — UTAU/Synthesizer V連携
6. **企業システム開発** — ASP.NET Core/gRPCマイクロサービス

---

## 4. コード品質・アーキテクチャ

### 4.1 Capability-Based 抽象化（優先度: 中）

**現状**: 2026-03-15 に `DotNetG2P.Multilingual` へ capability-based internal adapter を実装済み。`src/DotNetG2P.Multilingual/Internal/CapabilityAdapters.cs` に `ITextBatchProcessor<TResult>` / `IIpaTextBatchProcessor` / `LanguageCapabilityRouter` が導入され、日本語の lock 保護を含めて言語別処理を内部 capability として束ねている。公開APIは中国語の `ToPinyin()` / `ToZhuyin()` など言語固有の形を維持している。

```csharp
public interface ITextBatchProcessor<TResult> : IDisposable
{
    TResult Convert(string text);
    IReadOnlyList<TResult> ConvertBatch(IReadOnlyList<string> texts);
}

public interface IIpaConvertible
{
    string ToIPA(string text);
    IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts);
}
```

**評価**: 公開APIを無理に共通化しないという当初方針を守りつつ、`CapabilityAdapterTests` による内部 contract 検証、Multilingual ルーティング整理、batch helper との接続点明確化まで進んだ。中国語の `ToPinyin()` のような言語固有APIもそのまま維持できている。

**残課題**:
- large input / exception propagation / parallel 実行の共通 fixture をさらに横展開する
- README / XML Doc / `ARCHITECTURE.md` に internal adapter 導入後の設計意図を同期する

### 4.2 バッチAPI実装の共通化（優先度: 高）

**現状**: `src/Shared/BatchConversionHelper.cs` を追加し、Core / English / Chinese / Spanish / French / Portuguese / Korean のバッチ実装は共通 helper に集約済み。`IReadOnlyList<T>` の public シグネチャは維持しつつ、従来の戻り値実体（`List<T>` / 配列）も保っている。Chinese の `style` / `includeTones` 付き batch API は state 付き helper で capture を避ける形まで反映済み。Multilingual はアセンブリ境界での型競合を避けるためクラス内 helper を維持しつつ、言語別変換そのものは `LanguageCapabilityRouter` 経由へ整理された。

**現行実装の形**:

```csharp
internal static class BatchConversionHelper
{
    public static List<TResult> ConvertToList<TResult>(...);
    public static TResult[] ConvertToArray<TResult>(...);
}
```

**残課題**:
- `null` / 空配列 / Dispose後 / 例外伝播 / 大規模入力 / 並列入力 の contract test を全言語へ横展開
- Multilingual の局所 `ConvertBatch` を shared helper へ寄せるかは、実利が出るまで保留でよい
- capability adapter 前提での benchmark fixture / sample code の再利用度をもう一段上げる

**効果**: コード重複削減と runtime contract の明確化はすでに進んでおり、残りはテスト観点と文書の同期が中心になった。

### 4.3 オプションクラスの基底クラス導入（優先度: 中）

**現状の問題**: `EnableTextNormalization` / `EnableNormalization` のような命名揺れはあるが、各言語で必要なオプション集合はかなり異なる。

**提案**: `BaseG2POptions` の継承導入よりも、命名規約・XML Doc・README 上の説明を揃える方が低コストで効果的。共通化する場合も継承ではなくガイドライン中心で進める。

### 4.4 スレッドセーフティ記述の一元化（優先度: 低）

**現状の問題**: スレッドセーフティは README で説明済みだが、README / XML Doc / テスト観点の対応表が分散している。

**提案**: まず README と XML Doc を同期し、必要なら内部メタデータで補う。現時点では公開APIに `ThreadSafetyLevel` を追加する優先度は低い。

### 4.5 Multilingual パッケージの依存最適化（優先度: 低）

**現状の問題**: Multilingual をインストールすると全言語パッケージが強制インストールされる。

**提案**: `DotNetG2P.Multilingual.Core` 分離は将来的な選択肢だが、現状は `TextSegmenter` が中国語辞書共有などに依存しており設計変更コストが大きい。PoC で依存境界を確認してから着手する。

---

## 5. テスト・CI/CD・品質保証

### 5.1 バッチAPIテストの棚卸しと共通化（優先度: 高）

| 対象 | 2026-03-13時点の状態 | 直近の補強 | 残課題 |
|------|----------------------|-----------|--------|
| 日本語(Core API) | API統合テストあり | `null` / 空 / mixed input / Dispose後 の batch 契約テストを追加 | 大規模入力・例外伝播の共通ケース |
| 英語 / 中国語 / スペイン語 / フランス語 / ポルトガル語 / 韓国語 | 各言語で基本ケースあり | 棚卸し基準として利用可能 | 共通 fixture への寄せ直し |
| Multilingual | 混在テキスト系のAPIテストあり | `null` / 空 / mixed input の batch ケースを追加 | 並列実行・高負荷・言語境界ケース |

**提案**: `BatchApiContractTests` のような共通 fixture を導入し、`null` / 空配列 / 大規模配列 / 例外伝播 / Dispose後動作 を全言語で同じ観点から検証する。現時点では Core と Multilingual の基準ケースが先行している。

### 5.2 CI/CDマトリックスビルド（優先度: 高）

**現状**: `CI` workflow は `ubuntu-latest` / `windows-latest` / `macos-latest` と `.NET 8` / `.NET 9` の matrix で動作している。`.NET 8` は project file 直接ビルドで互換性確認、Ubuntu `.NET 9` ジョブではカバレッジ収集、ReportGenerator、PR 向け coverage comment、DocFX build（`--warningsAsErrors`）、trim / AOT publish smoke test、NuGet Package Validation、CycloneDX SBOM 生成まで実施している。2026-03-16 のローカル確認では `dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj -c Release --filter "Category!=Performance"` が `0 failure / 5,830 pass / 457 skip / 6,287 total`（skip は辞書・外部データ依存ケースを含む）で完了し、`--list-tests` では `6,350` 件のテスト定義を列挙できた。

**残課題**: scheduled benchmark、coverage threshold、repo 全体の compiler warning 整理は未完了。DocFX warning gate は導入済みだが、C# compile warning までは fail 条件にしていない。

**次の一手**:
- benchmark は別 workflow に切り出し、baseline 差分比較付きで保管する
- coverage はベースライン安定後に threshold 導入を検討する
- compiler warning gate は XML Doc / nullability 整理後に段階導入する

### 5.3 コードカバレッジ統合（優先度: 高）

**現状**: Ubuntu `.NET 9` ジョブで `dotnet test --collect:"XPlat Code Coverage"` を実行し、`reportgenerator` で HTML / Cobertura / TextSummary を生成、artifact・GitHub Step Summary・PR コメントに公開している。

**残課題**: coverage しきい値ゲート、履歴トレンド、外部サービス連携は未実装。

**提案**: 現行の `XPlat Code Coverage` + `reportgenerator` を維持しつつ、ベースラインが安定した段階で threshold を導入する。外部サービス連携は必須ではなく、まずは PR 内での可視性を保ち続ける方針が妥当。

### 5.4 テスト構造の共通化（優先度: 中）

- `DictionaryPathResolver` ユーティリティクラスで辞書パス検出ロジック統一
- `BaseLanguageEngineFixture<TEngine>` 基底クラスでFixtureパターン統一

### 5.5 CIキャッシング見直し（優先度: 低）

**現状**: `CI` workflow の NuGet パッケージキャッシュと `.github/actions/setup-dictionary/action.yml` の naist-jdic 辞書キャッシュは、いずれも `actions/cache@v5` に揃っている。

**提案**: `.build/` キャッシュは効果測定後に判断する。キャッシュサイズ増加や古い生成物混入のリスクがあるため、先に CI 実行時間の内訳を可視化する。

### 5.6 テスト結果レポート（優先度: 中）

**現状**: `EnricoMi/publish-unit-test-result-action@v2` で matrix ごとのテスト結果を PR に自動表示し、TRX も artifact として保持している。

**残課題**: flaky test ラベル付け、coverage とテスト結果の統合ビュー、失敗時のトリアージ導線整備。

### 5.7 高度なテスト手法の導入（優先度: 中-低）

| 手法 | ツール | 効果 |
|------|--------|------|
| Property-Based Testing | FsCheck for C# | ランダム入力での安定性検証 |
| Mutation Testing | Stryker.NET | テストの有効性（バグ検出力）検証 |
| Fuzz Testing | SharpFuzz | 制御文字・長文・未知文字への耐性 |
| Snapshot Testing | Verify | フルコンテキストラベル等の回帰検出 |

---

## 6. パフォーマンス最適化

### 実施済みの最適化（M7段階）

- ValueStringBuilder / ThrowHelper 基盤
- DoubleArrayTrie unsafe ポインタ高速化
- MeCabToken 遅延パーサ / string.Intern()
- enum 基底型最適化 / Regex→手動パーサ
- DictionaryBundle WeakReference キャッシュ

### 6.1 BenchmarkDotNet 導入（優先度: 高）

**現状**: `tests/DotNetG2P.Benchmarks/` は導入済みで、English / Chinese / Korean に加えて Japanese / Multilingual / Romance（Spanish / French / Portuguese）の代表シナリオを `BenchmarkSwitcher` から実行できる。2026-03-16 の `--list flat` では 27 ベンチマークケースを列挙できた。README と入力データの整理も含めて、基盤導入フェーズは完了している。

**残課題**:
- CI での定期実行や baseline 比較未導入
- cold start と steady-state の計測粒度が言語ごとにまだ揃っていない
- 結果の artifact 保管と回帰比較の導線が未整備

**提案**: 基盤新設は完了として、次は manual / scheduled workflow でベンチ結果を保管し、主要言語の cold start / steady-state 指標を比較しやすくする。

### 6.2 マルチターゲット化 + .NET 8+ API活用（優先度: 高）

**現状の問題**: netstandard2.1 のみで最新APIが使えない。

**背景**: Unity 6 (6000.0.x) を含む現行のUnityランタイムは .NET Standard 2.1 が上限であり、CoreCLR移行（net8.0+ネイティブ対応）は計画中だが未完了。そのため、Unity最低バージョンを上げても .NET Standard 2.1 の制約は変わらない。本提案の目的は **非Unityの .NET 8+ ユーザー向けパフォーマンス最適化** であり、Unity互換性は `netstandard2.1` ターゲットで維持する。

**提案**:
```xml
<TargetFrameworks>netstandard2.1;net8.0;net9.0</TargetFrameworks>
```

`#if` ディレクティブで .NET 8+ API を条件分岐:
```csharp
#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<string, string> _dict = ...;
#else
    private static readonly Dictionary<string, string> _dict = ...;
#endif
```

| ターゲット | 対象ユーザー | 備考 |
|-----------|-------------|------|
| **netstandard2.1** | Unity 2021.2+ / Unity 6+ | 従来通りの互換性を維持 |
| **net8.0** | .NET 8 サーバー/デスクトップ | FrozenDictionary等の高速API活用 |
| **net9.0** | .NET 9 最新環境 | 最新ランタイム最適化の恩恵 |

| API | 適用箇所 | 効果 | 対象ターゲット |
|-----|---------|------|--------------|
| **FrozenDictionary** | PinyinCharDictionary (44,435件)、CmuDictionary (135,166件) | 読み取り専用辞書 10-20% 高速化 | net8.0+ |
| **SearchValues\<T\>** | テキスト正規化の文字検索 | 文字検索高速化 | net8.0+ |
| **Dictionary容量プリアロケート** | 全辞書初期化 | 初期化時のリハッシュ削減 | 全ターゲット |
| **SIMD (Vector128/256)** | UTF-8バイト列スキャン、フォネームマッピング | 処理高速化 | net8.0+ |

### 6.3 公開APIに影響しない内部バッファ再利用（優先度: 中）

`ArrayPool<string>` をそのままバッチAPIの戻り値に使うのは所有権管理が難しい。代わりに、`List<T>(capacity)`、内部一時バッファ、`ArrayPool<char>` / `ValueStringBuilder` のような hot path に限定して再利用する。

### 6.4 Parallel.For バッチ処理（優先度: 中）

100件以上のバッチ時に opt-in で `Parallel.For` を検討する。順序維持・例外伝播・日本語セグメントの直列化コストを考慮し、既定動作は逐次処理のままが安全。

### 6.5 推定総合効果

現時点で「累積 30-45% 向上」のような数値を置くのは推測が強い。まず BenchmarkDotNet を導入し、辞書初期化・正規化・バッチ処理・Multilingual 分岐の4領域でボトルネックを定量化したうえで、効果予測を更新する。

---

## 7. 機能拡張

### 7.1 SSMLライトMVP（優先度: 高、推定工数: 40-60h）

**新パッケージ**: `DotNetG2P.Ssml`

W3C SSML 1.1 全面対応を最初から目指すのではなく、G2Pレイヤーで意味のある要素に絞ったライトMVPを実装する。

**対応レベル**:
- Level 1（MVP）: `<phoneme>`, `<sub>`, `<lang>`, `<break>` — 発音上書き、置換、言語切替、区切り
- Level 2（拡張）: `<say-as>`, `<prosody>`, `<emphasis>` — 読み方指定、韻律、強調
- Out of scope（初期）: `<voice>`, `<audio>`, `<mark>` — G2P単体では責務が広すぎる

**XMLパーサ選択**: System.Xml.Linq（netstandard 2.1対応、軽量、依存なし）

```csharp
public class SsmlPhonemeResult
{
    public string OriginalText { get; set; }
    public string Phonemes { get; set; }
    public BreakInfo Break { get; set; }
    public ProsodyInfo Prosody { get; set; }
    public EmphasisLevel Emphasis { get; set; }
}
```

### 7.2 ストリーミングAPI（優先度: 高、推定工数: 50h）

**新パッケージ**: `DotNetG2P.Streaming`

| パターン | 技術 | 用途 |
|---------|------|------|
| IAsyncEnumerable\<T\> | LINQ統合 | 一般用途（推奨） |
| System.IO.Pipelines | PipeReader/Writer | 高スループット |
| System.Threading.Channels | Channel\<T\> | スケーラブル並列処理 |

### 7.3 WebAssembly対応（優先度: 中、推定工数: 70h）

**新パッケージ**: `DotNetG2P.Wasm`

**戦略1（推奨）**: 軽量化版—英語/中国語/スペイン語/フランス語/ポルトガル語のみ

**推定バンドルサイズ**: ~650-750 KB（gzip: ~200-250 KB）

**戦略2**: Web Worker統合、**戦略3**: 日本語フルサポート（サーバーからMeCab辞書ダウンロード）

### 7.4 Unity統合強化（優先度: 中）

- **Unity Editor拡張**: G2Pプレビューウィンドウ、Timeline統合（4-5週間）
- **WebGL対応**: IL2CPP対応 + IndexedDB辞書キャッシュ（4-6週間）

### 7.5 Unicode正規化の強化（優先度: 中、推定工数: 30h）

- 異体字セレクタ（U+E0100〜U+E01EF）対応
- CJK互換文字（U+F900〜U+FAFF）→標準文字統一
- 言語別正規化の `ILanguageSpecificNormalizer` インターフェース共通化

### 7.6 音韻規則エンジンの汎用化（優先度: 低、推定工数: 100h）

DSL（Domain Specific Language）で音韻規則を記述する汎用フレームワーク。新言語追加を容易化。

```
rule "digraph-ll":
    pattern: "ll" -> ["ʎ"]     # Castilian
    | [ˈʝ]                     # Latin American
    condition: dialect == Castilian ? ʎ : ʝ

pipeline:
    1. digraph → 2. contextual → 3. nasalization → 4. voicing
```

### 7.7 音素埋め込み（優先度: 低、推定工数: 45h）

TTS/ASRシステムとの連携用に音素のベクトル表現を出力。言語あたり ~2-5 MBの埋め込みモデル。

---

## 8. 新言語追加候補

### 8.1 言語候補一覧（Tier分類）

11言語を実装難易度・工数・市場需要・既存アーキテクチャとの互換性で4段階に分類。

| Tier | 言語 | 難易度 | 工数（週） | ML必須 | 既存互換 | TTS需要 |
|------|------|--------|-----------|--------|---------|---------|
| **1** | インドネシア語 | 低-中 | 2-3 | ✗ | ◎ | 中-高 |
| **1** | トルコ語 | 中 | 3-4 | ✗ | ◎ | 中 |
| **1** | ドイツ語 | 中 | 3-4 | ✗ | ◎ | 高 |
| **2** | ベトナム語 | 中-高 | 4-5 | ✗ | ◎ | 中-高 |
| **2** | イタリア語 | 中 | 3-4 | ✗ | ◎ | 中 |
| **2** | ポーランド語 | 中-高 | 4-5 | ✗ | ◎ | 中 |
| **3** | ヒンディー語 | 高 | 5-6 | ▲ | ◎ | 高 |
| **3** | ロシア語 | 高 | 5-7 | ◎ | ▲ | 高 |
| **4** | タイ語 | 高 | 6-8 | ◎ | ✗ | 中 |
| **4** | スウェーデン語 | 中 | 3-4 | △ | ◎ | 中 |
| **4** | アラビア語 | 非常に高 | 8-12 | ◎◎ | ✗ | 高 |

**Tier分類基準**: Tier 1-2はルールベースで既存アーキテクチャ（`GraphemeToPhonemeRules` + `Syllabifier` + 例外辞書）と互換。Tier 3-4はML統合が必要。

### 8.2 Tier 1: 優先推奨（低リスク・高効率）

#### インドネシア語 (Indonesian/Malay)
- **音素体系**: 子音19-21種、母音6種
- **利点**: 規則性が非常に高く、ルールベースG2Pとの相性が最も良い
- **課題**: 同音異字語のPOS処理（小規模）
- **参照**: g2p_id（PER 0.78%の高精度実装あり）
- **推奨理由**: 実装コスト最小（2-3週）で話者数2億人以上の市場をカバー

#### トルコ語 (Turkish)
- **音素体系**: 子音24種、母音8種（前舌/後舌対立）
- **課題**: 母音調和（Vowel Harmony）— 語内で前舌/後舌母音に統一
- **利点**: 規則性高い、ストレス位置予測可能（語末-1音節）
- **参照**: Rule-based Turkish G2P論文（Duygu Altınok）

#### ドイツ語 (German)
- **音素体系**: 子音23種、母音14種（短3+長7+中央2）
- **課題**: 複合語分割（Zusammensetzung）、ウムラウト処理
- **参照**: BAS G2P Webサービス、espeak-ng
- **アプローチ**: 辞書ベース最長一致検索で複合語分割 + 3フェーズG2Pルール

### 8.3 Tier 2: 実装可能（中程度リスク）

#### ベトナム語 (Vietnamese)
- **音素体系**: 子音21種、母音11種、声調6種
- **利点**: ラテン文字ベースで声調は正書法で常に明記
- **課題**: 複雑な二重母音（ai, au, oi, ơi, ưa等）
- **参照**: ViG2P (Viphoneme)

#### イタリア語 (Italian)
- **音素体系**: 子音21種、母音7種（開閉対立 e/ɛ, o/ɔ）
- **課題**: 開閉母音の区別が不規則 — 例外辞書必須
- **アプローチ**: スペイン語/フランス語と同等のハイブリッド方式

#### ポーランド語 (Polish)
- **音素体系**: 子音29種、母音6種
- **課題**: 複雑な子音クラスタ（語頭で最大4子音）
- **利点**: 音韻規則は複雑だが明確、正書法の信頼性高い
- **参照**: TransFon論文（MDPI）

**補足**: 韓国語は新規追加候補ではなく既存実装の強化対象として扱うのが適切。追加投資先はベンチマーク拡充・外部コーパス評価・例外辞書拡張。

### 8.4 Tier 3-4: ML依存または困難

| 言語 | 最大困難 | ML依存理由 |
|------|---------|-----------|
| **ロシア語** | ストレス位置が語彙的（正書法に標記なし） | RuAccent（BERT-based）統合必須 |
| **ヒンディー語** | デーヴァナーガリー文字 + シュワー削除 | Festvox Indic Frontendで対応可能 |
| **タイ語** | 語境界検出（スペース区切りなし）+ 5声調 | CRF/LSTM語分割 + 声調推定必須 |
| **アラビア語** | 短母音記号省略（通常テキストに母音なし） | DNN母音復元必須、データ取得困難 |
| **スウェーデン語** | 2種の語彙的声調アクセント | 辞書ルックアップ必須、学術リソース限定 |

---

## 9. パッケージ・エコシステム・セキュリティ

### 9.1 AOT / Trim 互換性検証（実装済み、継続検証）

**現状**: `tests/DotNetG2P.PublishSmoke/` を追加し、Ubuntu `.NET 9` の CI レーンで `PublishTrimmed=true` と `PublishAot=true` の publish smoke test を実行している。smoke app は英語・中国語・韓国語・スペイン語・フランス語・ポルトガル語を常時検証し、辞書が見つかる環境では日本語と Multilingual も検証する。埋め込みリソース読み込みは引き続き 8 箇所あるが、「未検証」状態は解消された。

**残課題**:
- 現状の smoke test は代表変換の publish 成功確認が中心で、全 RID や WebAssembly までは網羅していない
- 今後 reflection / dynamic loading を追加する変更では、AOT/trim レーンを先に回してから merge する運用を徹底する
- 実測結果の README / release note 反映はまだ薄い

### 9.2 Source Generator活用（優先度: 低、推定工数: 1-2日）

- enum→文字列変換マッピング自動生成
- Dictionary初期化コード自動生成
- 公開APIに対するXML Docコメント未記述の警告Analyzer

### 9.3 パッケージエコシステム強化

**既に優れている点**:
- ✅ SourceLink完全実装
- ✅ Symbol Package (snupkg) 実装済み
- ✅ Deterministic Build / `ContinuousIntegrationBuild` 設定済み

**改善項目**:

| 項目 | 工数 | 効果 |
|------|------|------|
| MinVer / Nerdbank.GitVersioning | 1-2日 | gitタグからバージョン自動検出 |
| NuGet Package Signing / Strong Naming | 1-2日 | 配布物の信頼性向上 |
| release provenance / attestations | 1日 | サプライチェーン透明性 |

### 9.4 セキュリティ（優先度: 中）

**現状**: Dependabot は NuGet / .NET SDK / GitHub Actions 向けに導入済みで、2026-03-16 時点で `actions/download-artifact@v8`、`actions/upload-artifact@v7`、`actions/checkout@v6`、`actions/setup-dotnet@v5`、`actions/cache@v5`、`actions/github-script@v8`、`all-nuget-dependencies` グループ更新（PR #31 / #32 / #34 / #35 / #36 / #37 / #38）が `main` に反映済み。`.github/actions/setup-dictionary/action.yml` も `actions/cache@v5` に更新済みで、SBOM 生成と AOT/trim の safety smoke test も CI / release に入った。残っている中心課題は Strong Naming、NuGet Package Signing、署名用 secret / 証明書運用方針の確立である。

| 施策 | 工数 | 効果 |
|------|------|------|
| Dependabotルールの調整（grouping / cadence） | 0.5日 | ノイズ抑制と更新追従の両立 |
| Strong Naming | 1日 | 参照整合性と企業利用時の安心感向上 |
| NuGet Package Signing | 1日 | パッケージ改ざん防止 |

---

## 10. ドキュメント・DX

### 10.1 パッケージ別README拡充（優先度: 高）

**現状**: `PackageReadmeFile` は `Directory.Build.props` で共通化済み。現在は 9 公開パッケージすべてで pack 時に README が付与され、`DotNetG2P` / `MeCab` / `English` / `Chinese` / `Spanish` / `French` / `Portuguese` に専用 README を追加済み。Korean / Multilingual の既存 README も維持している。

**残課題**: root README・パッケージ README・NuGet 表示の drift を防ぐ運用、実行可能サンプルへの導線追加、主要 README の翻訳方針整理。

### 10.2 APIドキュメント生成（優先度: 高）

**現状**: XML ドキュメント生成は全 9 公開パッケージの csproj で有効で、DocFX も `docs/docfx.json` とローカル tool manifest を介して導入済みである。metadata は Release build 済み DLL + XML doc を参照し、CI / release workflow の両方で `dotnet tool run docfx docs/docfx.json --logLevel Warning --warningsAsErrors` が quality gate として動作する。残課題は公開 API の XML Doc 充実化、`CS1591` を含む compiler warning 整理、DocFX site の公開運用である。

**提案**:
- 全公開APIに XML Doc コメントを充実化し、`CS1591` を優先的に減らす
- compiler warning を repo-wide gate に引き上げられる状態まで nullability / doc warning を整理する
- DocFX site の GitHub Pages 公開や versioning 戦略を整える
- サンプルコードの自動テスト

### 10.3 CONTRIBUTING.md 維持（優先度: 中）

**現状**: `CONTRIBUTING.md` は追加済みで、`.slnx` contributor workflow、辞書セットアップ、targeted test / benchmark コマンド、PR 方針、`MIGRATION.md` への導線まで記載している。

**残課題**: release checklist、アーキテクチャ判断への導線、doc lint ルール整備。

### 10.4 言語別サンプルコード拡充（優先度: 高）

**現状**: ルート README には各言語の利用例があり、実行可能な `samples/DotNetG2P.Console/` も standalone language engines、日本語、Multilingual を横断する多言語デモへ拡張済みである。

**残課題**:
- 各言語専用の最小 sample project を分けるかどうかの判断
- sample の smoke test 自動化と README 断片コードとの同期

### 10.5 ARCHITECTURE.md 整備と MIGRATION.md 維持（優先度: 中）

- MIGRATION.md: 追加済み。辞書要件、batch API の collection contract、`.slnx` / .NET 8 project file 併用方針を記録
- ARCHITECTURE.md: 追加済み。パッケージ境界、日本語パイプライン、Multilingual routing、CI quality gate を整理した。今後は internal adapter や benchmark / signing 方針変更時に追随更新を続ける

### 10.6 CHANGELOG の Unreleased セクション（優先度: 低）

**現状**: `CHANGELOG.md` の `Unreleased` に batch API、CI/DX、package README などの未リリース変更を記載する運用へ更新済み。

**残課題**: tag / release note 生成との連携、change category の粒度統一。

---

## 11. 統合ロードマップ

### Phase 1: 品質基盤整備（実質完了）

| # | 項目 | 状態 | 次アクション |
|---|------|------|-------------|
| 1 | パッケージ別README拡充 | 完了 | drift 防止とサンプル導線の整備を継続 |
| 2 | CONTRIBUTING.md / MIGRATION.md | 完了 | release checklist と architecture 導線を追加検討 |
| 3 | CI マトリックスビルド | 完了 | scheduled benchmark と coverage threshold を次段で検討 |
| 4 | コードカバレッジ統合 | 完了 | threshold 運用はベースライン安定後に導入 |
| 5 | Dependabot + Deterministic Build | 完了 | signing / strong naming 方針を次段へ |
| 6 | BenchmarkDotNet 導入 | 完了 | scheduled 実行と baseline 比較を次段へ |
| 7 | Capability-based internal adapter 導入 | 完了 | Multilingual contract test と設計文書同期を継続 |
| 8 | バッチAPI共通化 + テスト棚卸し | 完了 | 共通 contract test を全言語へ横展開 |

### Phase 1 の残タスク

1. Signing / Strong Naming / NuGet Package Signing の方針と release 組み込みを決める
2. XML Doc / `CS1591` / compiler warning を整理し、repo-wide warning gate に近づける
3. scheduled benchmark と baseline 比較を導入し、性能回帰の可視化を進める
4. DocFX site の公開運用と README / sample / `ARCHITECTURE.md` の drift 防止を整える

### Phase 2: 短期（2-4ヶ月）— 機能拡張 + 新言語Tier 1

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 9 | インドネシア語 G2P | 新言語 | 2-3週 | 最高ROI新言語 |
| 10 | トルコ語 G2P | 新言語 | 3-4週 | 規則性高い新言語 |
| 11 | SSML ライトMVP | 機能拡張 | 2-3週 | TTS統合基盤 |
| 12 | マルチターゲット + FrozenDictionary | パフォーマンス | 2-3週 | 10-20%高速化 |
| 13 | XMLDoc充実化 + DocFX公開運用 | DX | 1-2週 | API使いやすさ |

### Phase 3: 中期（4-8ヶ月）— 新言語Tier 1-2 + 機能拡張

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 14 | ドイツ語 G2P | 新言語 | 3-4週 | 欧州主要言語 |
| 15 | ストリーミングAPI | 機能拡張 | 2-3週 | リアルタイム対応 |
| 16 | ベトナム語 G2P | 新言語 | 4-5週 | アジア市場拡大 |
| 17 | Signing / Strong Naming | 品質 | 3-5日 | 配布信頼性向上 |
| 18 | Unity Editor拡張 | Unity | 4-5週 | ゲーム市場 |

### Phase 4: 長期（8-12ヶ月）— 大規模展開

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 19 | イタリア語 + ポーランド語 G2P | 新言語 | 7-9週 | 13言語対応 |
| 20 | 韓国語評価基盤 / 例外辞書強化 | 既存言語強化 | 3-4週 | 既存品質向上 |
| 21 | WebAssembly対応 | 機能拡張 | 3-4週 | Web市場 |
| 22 | 音韻規則エンジン汎用化 | アーキテクチャ | 4-5週 | 新言語追加容易化 |
| 23 | ロシア語 G2P（ML統合） | 新言語 | 5-7週 | 東欧市場 |
| 24 | gRPCサービステンプレート | エコシステム | 2-3週 | 企業向け |

### 目標到達点

| フェーズ完了 | 対応言語数 | 主な新機能 |
|------------|-----------|-----------|
| Phase 1 | 7言語（現状維持） | 品質基盤・CI/CD強化 + internal adapter / DocFX / benchmark 整備 |
| Phase 2 | 9言語 | SSML、マルチターゲット |
| Phase 3 | 11言語 | ストリーミング、Signing / Strong Naming |
| Phase 4 | 14言語 | WebAssembly、gRPC |

---

## 12. 付録: 参考リソース

### 新言語実装参照

| 言語 | 参照プロジェクト | URL |
|------|-----------------|-----|
| インドネシア語 | g2p_id | github.com/bookbot-kids/g2p_id |
| 韓国語 | g2pK | github.com/Kyubyong/g2pK |
| トルコ語 | Rule-based Turkish G2P | arxiv.org/pdf/1601.03783 |
| ベトナム語 | ViG2P (Viphoneme) | github.com/v-nhandt21/Viphoneme |
| ヒンディー語 | Festvox Indic Frontend | festvox.org |
| ポーランド語 | TransFon | mdpi.com/2076-3417/12/5/2758 |
| ロシア語 | RuAccent | github.com/IlyaGusev/ruaccent |

### 競合G2Pツール

| ツール | 言語 | ライセンス | 用途 |
|--------|------|-----------|------|
| espeak-ng | C | GPL 3.0 | 100+言語対応の定番 |
| Phonemizer | Python | GPL 3.0 | 複数バックエンド phonemizer |
| Gruut | Python | MIT | tokenizer + phonemizer + SSML |
| DeepPhonemizer | Python | Apache-2.0 | TransformerニューラルG2P |
| piper-phonemize | C++ | MIT | Piper TTS用 |
| Epitran | Python | MIT | 多言語IPAマッピング |
| NVIDIA NeMo G2P | Python | Apache-2.0 | 多言語G2Pフレームワーク |
