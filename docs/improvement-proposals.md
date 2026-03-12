# DotNetG2P 改善提案書

> 調査日: 2026-03-13
> 対象: main ブランチ (ab98163)
> 調査方法: 9チームによるコードベース並列調査
> レビュー反映: 2026-03-13（15視点レビューで現行リポジトリとの整合性を補正）

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

DotNetG2P は7言語対応・6,700+テスト・高度なパフォーマンス最適化済みの成熟したプロジェクトであり、.NET/Unity 向けでは**稀少な純C#多言語G2P実装**です。

以下の8領域で改善の機会を特定しました。

| 領域 | 主要課題 | 推定効果 |
|------|---------|---------|
| **コード品質** | 共通抽象の粒度未整理、バッチAPI重複 | 保守性向上、テスト共通化 |
| **テスト・CI/CD** | バッチAPIテスト不足、マトリックスビルド未実装 | 品質保証・互換性確保 |
| **パフォーマンス** | FrozenDictionary/SearchValues未活用、BenchmarkDotNet未導入 | 定量的な最適化判断が可能 |
| **機能拡張** | SSML/ストリーミング/WebAssembly | 市場競争力大幅向上 |
| **新言語** | 11言語候補を調査、Tier 1-4に分類 | 最大14言語対応 |
| **パッケージ** | AOT/trim適合性未検証、Dependabot未導入 | 配布互換性・運用強化 |
| **ドキュメント・DX** | パッケージ別README不足、DocFX/CONTRIBUTING.md欠如 | 新規ユーザー獲得・貢献促進 |
| **競合対策** | 比較表の根拠整備、ユースケース訴求 | 市場認知度向上 |

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

**現状の問題**: 各言語エンジンは独立実装だが、API表現が言語ごとに異なる。特に中国語は `ToPinyin()`/`ToZhuyin()` が主APIであり、単純な `ToPhonemes()` への統一は不自然。

**提案**: 公開APIを無理に一本化せず、内部利用・テスト共通化のために capability 単位の抽象を導入する。

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

**効果**: テストヘルパー共通化、内部アダプタ導入、Multilingual 側の整理が可能。中国語の `ToPinyin()` のような言語固有APIも維持できる。

### 4.2 バッチAPI実装の共通化（優先度: 高）

**現状の問題**: 8エンジンで複数のバッチメソッドがほぼ同じループ実装になっている。

**提案**: 静的ヘルパーメソッドに集約。

```csharp
public static class G2PEngineBatchExtensions
{
    public static IReadOnlyList<T> BatchProcess<T>(
        IReadOnlyList<string> texts, Func<string, T> processor)
    {
        if (texts == null) throw new ArgumentNullException(nameof(texts));
        var results = new T[texts.Count];
        for (int i = 0; i < texts.Count; i++)
            results[i] = processor(texts[i]);
        return results;
    }
}
```

**効果**: コード重複 ~85% 削減。

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

| 言語 | 全テスト数 | バッチテスト | 状況 |
|------|-----------|-----------|------|
| 英語 | 511+ | 充実 | 共通化の基準として適切 |
| 中国語 | 936 | 一定数あり | 例外伝播・大規模入力を補強 |
| スペイン語 | 227 | 基本ケースあり | 境界値を補強 |
| フランス語 | 719 | 基本ケースあり | 共通ケース横展開が有効 |
| ポルトガル語 | 1,310 | 基本ケースあり | 共通ケース横展開が有効 |
| Multilingual | 443 | 基本ケースあり | 混在境界・並列観点を補強 |

**提案**: 「未実装」前提ではなく、既存テストを棚卸ししたうえで `null` / 空配列 / 大規模配列 / 例外伝播 / Dispose後動作 を `BatchApiCommonTests` に共通化する。

### 5.2 CI/CDマトリックスビルド（優先度: 高）

**現状の問題**: ubuntu-latest + .NET 9.0.x の単一環境のみ。

**提案**:
```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
    dotnet: ['8.0.x', '9.0.x']
```

### 5.3 コードカバレッジ統合（優先度: 高）

**現状の問題**: `coverlet.collector` パッケージ参照はあるが、CI/CDでの出力設定なし。

**提案**: `dotnet test --collect:"XPlat Code Coverage"` + `reportgenerator` + CodeCov アップロード。

### 5.4 テスト構造の共通化（優先度: 中）

- `DictionaryPathResolver` ユーティリティクラスで辞書パス検出ロジック統一
- `BaseLanguageEngineFixture<TEngine>` 基底クラスでFixtureパターン統一

### 5.5 CIキャッシング見直し（優先度: 低）

**現状**: NuGet パッケージと naist-jdic 辞書は既に `actions/cache@v4` でキャッシュされている。

**提案**: `.build/` キャッシュは効果測定後に判断する。キャッシュサイズ増加や古い生成物混入のリスクがあるため、先に CI 実行時間の内訳を可視化する。

### 5.6 テスト結果レポート（優先度: 中）

`EnricoMi/publish-unit-test-result-action@v2` で PR コメントにテスト結果を自動表示。

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

**現状の問題**: Stopwatch ベースの性能テストは存在するが、統計的な比較やランタイム差分の可視化には弱い。

**提案**: `tests/DotNetG2P.Benchmarks/` 新設。CI/CD統合でPRごとのリグレッション検出。

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

### 9.1 AOT / Trim 互換性検証（優先度: 中、推定工数: 3-5日）

**現状**: ソース配下で 8 箇所の `Assembly.GetManifestResourceStream()` による埋め込みリソース読み込みがある。これだけで NativeAOT 非対応とは断定できないが、AOT / trimming の実検証は未実施。

**対策**:
- `PublishAot=true` / `PublishTrimmed=true` の smoke test を追加
- 問題再現時のみ属性追加やリソース読込方式の変更を行う
- 実測結果を README / CI に反映し、「対応済み」ではなく「検証済み」状態を作る

### 9.2 Source Generator活用（優先度: 低、推定工数: 1-2日）

- enum→文字列変換マッピング自動生成
- Dictionary初期化コード自動生成
- 公開APIに対するXML Docコメント未記述の警告Analyzer

### 9.3 パッケージエコシステム強化

**既に優れている点**:
- ✅ SourceLink完全実装
- ✅ Symbol Package (snupkg) 実装済み

**改善項目**:

| 項目 | 工数 | 効果 |
|------|------|------|
| MinVer / Nerdbank.GitVersioning | 1-2日 | gitタグからバージョン自動検出 |
| Deterministic Build | 0.5日 | バイナリ再現可能性 |
| NuGet Package Validation (.NET 8+) | 0.5日 | 互換性レポート自動生成 |
| SBOM生成 (CycloneDX) | 1日 | セキュリティ監査対応 |

### 9.4 セキュリティ（優先度: 中）

**現状**: Dependabot/Renovate未導入、Strong Naming未実装、SBOM未生成。

| 施策 | 工数 | 効果 |
|------|------|------|
| `.github/dependabot.yml` 追加 | 0.5日 | NuGet依存関係の自動監視・PR自動作成 |
| NuGet Package Signing | 1日 | パッケージ改ざん防止 |
| SBOM生成（CycloneDX） | 0.5日 | サプライチェーン安全性 |

---

## 10. ドキュメント・DX

### 10.1 パッケージ別README拡充（優先度: 高）

**現状**: `PackageReadmeFile` 自体は `Directory.Build.props` で既に設定済み。Korean / Multilingual は専用 README を持ち、それ以外はリポジトリ直下 README をパッケージREADMEとして流用している。

**提案**: 「未設定」対応ではなく、外部向け価値が高いパッケージから専用 README を追加する。優先候補は `DotNetG2P`, `DotNetG2P.MeCab`, `DotNetG2P.English`, `DotNetG2P.Chinese`。

### 10.2 APIドキュメント生成（優先度: 高）

**現状**: XMLドキュメント生成は全csprojで有効だが、`///` コメント密度は低い（146ファイル中19ファイルのみ充実）。DocFX未導入。

**提案**:
- DocFX統合（`docfx.json` 作成、GitHub Pages公開）
- 全公開APIにXML Docコメント充実化（例示付き）
- サンプルコードの自動テスト

### 10.3 CONTRIBUTING.md 新規作成（優先度: 高）

Prerequisites / Setup / Code Standards / Testing / 新言語追加手順 を含む 150-200行のガイド。

### 10.4 言語別サンプルコード拡充（優先度: 高）

**現状**: ルート README には各言語の利用例があるが、実行可能な `samples/DotNetG2P.Console/` は日本語中心。

**提案**: 各言語の基本使用例 + Multilingual 混在テキスト例をサンプルプロジェクトとして追加し、README の断片コードだけで終わらせない。

### 10.5 ARCHITECTURE.md / MIGRATION.md 新規作成（優先度: 中）

- ARCHITECTURE.md: 設計判断の背景（なぜ独立パッケージか / なぜ純C#か / なぜ EmbeddedResource か）
- MIGRATION.md: v1.0→1.4 の段階的マイグレーションガイド

### 10.6 CHANGELOG の Unreleased セクション（優先度: 低）

ロードマップ・計画中機能を記載し、ユーザーが今後の方向性を把握可能に。

---

## 11. 統合ロードマップ

### Phase 1: 即時実施（1-2ヶ月）— 品質基盤整備

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 1 | パッケージ別README拡充 | DX | 2日 | NuGet発見性向上 |
| 2 | CONTRIBUTING.md / MIGRATION.md | DX | 2日 | 貢献促進 |
| 3 | CI マトリックスビルド | CI/CD | 1日 | 互換性確保 |
| 4 | コードカバレッジ統合 | CI/CD | 1日 | 品質可視化 |
| 5 | Dependabot + Deterministic Build | セキュリティ | 1日 | サプライチェーン安全性 |
| 6 | BenchmarkDotNet 導入 | パフォーマンス | 1-2週 | ボトルネック可視化 |
| 7 | Capability-based internal adapter 導入 | アーキテクチャ | 1-2週 | テスト/内部整理 |
| 8 | バッチAPI共通化 + テスト棚卸し | アーキテクチャ | 1-2週 | コード重複削減 |

### Phase 2: 短期（2-4ヶ月）— 機能拡張 + 新言語Tier 1

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 9 | インドネシア語 G2P | 新言語 | 2-3週 | 最高ROI新言語 |
| 10 | トルコ語 G2P | 新言語 | 3-4週 | 規則性高い新言語 |
| 11 | SSML ライトMVP | 機能拡張 | 2-3週 | TTS統合基盤 |
| 12 | マルチターゲット + FrozenDictionary | パフォーマンス | 2-3週 | 10-20%高速化 |
| 13 | DocFX + XMLDoc充実化 | DX | 1-2週 | API使いやすさ |

### Phase 3: 中期（4-8ヶ月）— 新言語Tier 1-2 + 機能拡張

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 14 | ドイツ語 G2P | 新言語 | 3-4週 | 欧州主要言語 |
| 15 | ストリーミングAPI | 機能拡張 | 2-3週 | リアルタイム対応 |
| 16 | ベトナム語 G2P | 新言語 | 4-5週 | アジア市場拡大 |
| 17 | AOT / Trim 互換性検証 | 品質 | 3-5日 | 配布互換性可視化 |
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
| Phase 1 | 7言語（現状維持） | 品質基盤・CI/CD強化 |
| Phase 2 | 9言語 | SSML、マルチターゲット |
| Phase 3 | 11言語 | ストリーミング、AOT/trim検証 |
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
