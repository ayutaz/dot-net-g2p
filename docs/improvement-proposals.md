# DotNetG2P 改善提案書

> 調査日: 2026-03-13
> 対象: main ブランチ (ab98163)
> 調査方法: 9チームによるコードベース並列調査

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

DotNetG2P は6言語対応・6,700+テスト・高度なパフォーマンス最適化済みの成熟したプロジェクトであり、C#/.NETエコシステムにおいて**唯一の純C#多言語G2P実装**です。

以下の8領域で改善の機会を特定しました。

| 領域 | 主要課題 | 推定効果 |
|------|---------|---------|
| **コード品質** | 共通インターフェース欠如、バッチAPI重複 | 保守性30%向上、新言語追加コスト削減 |
| **テスト・CI/CD** | バッチAPIテスト不足、マトリックスビルド未実装 | 品質保証・互換性確保 |
| **パフォーマンス** | FrozenDictionary/SearchValues未活用、BenchmarkDotNet未導入 | スループット30-45%向上 |
| **機能拡張** | SSML/ストリーミング/WebAssembly | 市場競争力大幅向上 |
| **新言語** | 12言語候補を調査、Tier 1-4に分類 | 最大14言語対応 |
| **パッケージ** | NativeAOT未対応、Dependabot未導入 | プロダクション対応強化 |
| **ドキュメント・DX** | NuGet README不足、DocFX/CONTRIBUTING.md欠如 | 新規ユーザー獲得・貢献促進 |
| **競合対策** | espeak-ng(GPL)との差別化、ユースケース拡大 | 市場認知度向上 |

---

## 2. 競合分析・市場ポジション

### 2.1 競合G2Pライブラリ比較

| 項目 | espeak-ng | Phonemizer | Gruut | DeepPhonemizer | **DotNetG2P** |
|------|-----------|-----------|-------|-----------------|-----------|
| 言語数 | 100+ | 100+ | 複数 | 多言語 | **6言語（深い実装）** |
| ライセンス | GPL 3.0 | GPL 3.0 | MIT | 複合 | **Apache-2.0** |
| 実装言語 | C | Python | Python | Python | **C# (.NET Standard 2.1)** |
| 外部依存 | あり | あり | あり | PyTorch/ONNX | **なし** |
| Unity対応 | ✗ | ✗ | ✗ | ✗ | **✅ (UPM)** |
| 日本語形態素解析 | ✗ | ✗ | ✗ | ✗ | **✅ (独自MeCab)** |
| 出力形式 | IPAのみ | IPAのみ | IPAのみ | IPAのみ | **IPA/X-SAMPA/ピンイン/注音/VOICEVOX等** |
| 低遅延・オフライン | ✅ | △ | △ | ✗(GPU推奨) | **✅** |
| 商用フレンドリー | ✗(GPL) | ✗(GPL) | ✅(MIT) | △ | **✅(Apache-2.0)** |

### 2.2 DotNetG2Pの独自価値

1. **C#/.NETエコシステム唯一の純C#多言語G2P** — Python/ネイティブバイナリ不要
2. **Unity UPMネイティブ対応** — ゲーム開発者の決定的な差別化ポイント
3. **Apache-2.0で完全に商用自由** — GPLの制約から解放
4. **多様な出力形式** — IPA/X-SAMPA/ピンイン/注音/VOICEVOX/HTSラベル等
5. **深い言語学的実装** — 日本語NJDパイプライン（OpenJTalk互換）、独自MeCabエンジン

### 2.3 「広さ」vs「深さ」の差別化

espeak-ngは100+言語をカバーするが各言語の実装深度は浅い。DotNetG2Pは6言語に絞り込みつつ、各言語で深い言語学的処理（形態素解析、声調変調、異音規則、方言対応等）を実現。

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

### 4.1 共通インターフェースの導入（優先度: 高）

**現状の問題**: 各言語エンジンが独立した `IDisposable` 実装で、ポリモーフィズムが活用できない。

**提案**: `IG2PEngine` 基本インターフェースと `IIpaG2PEngine`/`IXSampaG2PEngine` 拡張インターフェースの導入。

```csharp
public interface IG2PEngine : IDisposable
{
    string ToPhonemes(string text);
    IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
    ThreadSafetyLevel ThreadSafety { get; }
}

public interface IIpaG2PEngine : IG2PEngine
{
    string ToIPA(string text);
    IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts);
}

public enum ThreadSafetyLevel { None = 0, ReadOnly = 1, Synchronized = 2 }
```

**効果**: MultilingualG2PEngine簡潔化、言語追加時の修正不要、テスト時のモック化容易。

### 4.2 バッチAPI実装の共通化（優先度: 高）

**現状の問題**: 9言語 x 平均4.5バッチメソッド = 40+個の同一パターンコードが重複。

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

**現状の問題**: 共通プロパティ（`IncludeStress`, `Separator`, `EnableTextNormalization`）の扱いが言語ごとに異なる。

**提案**: `BaseG2POptions` 抽象基底クラスで共通プロパティを統一。

### 4.4 ThreadSafetyLevel の明示化（優先度: 中）

**現状の問題**: スペイン語・フランス語・ポルトガル語エンジンのスレッドセーフティがドキュメント化されていない。

**提案**: 各エンジンに `ThreadSafetyLevel` プロパティを追加し、MultilingualG2PEngine で自動ロック判定に活用。

### 4.5 Multilingual パッケージの依存最適化（優先度: 低）

**現状の問題**: Multilingual をインストールすると全言語パッケージが強制インストール。

**提案**: `DotNetG2P.Multilingual.Core`（LanguageDetector/TextSegmenter のみ）を分離し、言語パッケージはオプション依存に。

---

## 5. テスト・CI/CD・品質保証

### 5.1 バッチAPIテストの整備（優先度: 高）

| 言語 | 全テスト数 | バッチテスト | 状況 |
|------|-----------|-----------|------|
| 英語 | 511+ | 40 | 完備 |
| 中国語 | 936 | ~23 | 不足 |
| スペイン語 | 227 | <5 | 不足 |
| フランス語 | 719 | <5 | 不足 |
| ポルトガル語 | 1,310 | <5 | 不足 |
| Multilingual | 412 | 0 | 未実装 |

**提案**: 共通バッチテストスイート `BatchApiCommonTests<TEngine>` でnull/空配列/大規模配列/例外伝播を統一テスト。

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

### 5.5 CIキャッシング強化（優先度: 中）

`.build/` と辞書ファイルを `actions/cache@v4` でキャッシュ。推定 1-2 分/実行の短縮。

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

**現状の問題**: パフォーマンステストは Stopwatch ベースで統計的な分析ができない。

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

### 6.3 バッチAPI の ArrayPool 活用（優先度: 高）

`ArrayPool<string>.Shared.Rent()` で GC Gen0 圧力 **20-30% 削減**。

### 6.4 Parallel.For バッチ処理（優先度: 中）

100件以上のバッチ時に `Parallel.For` 使用。スレッドセーフなエンジンで **2-4倍高速化**。

### 6.5 推定総合効果

| フェーズ | 期間 | スループット向上 | メモリ削減 |
|---------|------|----------------|----------|
| Phase 1 | 1-2ヶ月 | +15-20% | -20-30% |
| Phase 2 | 3-6ヶ月 | +10-15%（累積 25-35%） | -10-15% |
| Phase 3 | 6-12ヶ月 | +5-10%（累積 30-45%） | -5-10% |

---

## 7. 機能拡張

### 7.1 SSMLサポート（優先度: 最高、推定工数: 60h）

**新パッケージ**: `DotNetG2P.Ssml`

W3C SSML 1.1 準拠のXMLパーサを実装し、既存G2Pパイプラインに統合。

**対応レベル**:
- Level 1（基本）: `<phoneme>`, `<say-as>`, `<sub>` — 発音上書き、読み方指定、置換
- Level 2（拡張）: `<break>`, `<prosody>`, `<emphasis>` — 間、韻律、強調
- Level 3（Phase 2）: `<voice>`, `<audio>`, `<mark>` — 話者切替、音声挿入

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

12言語を実装難易度・工数・市場需要・既存アーキテクチャとの互換性で4段階に分類。

| Tier | 言語 | 難易度 | 工数（週） | ML必須 | 既存互換 | TTS需要 |
|------|------|--------|-----------|--------|---------|---------|
| **1** | インドネシア語 | 低-中 | 2-3 | ✗ | ◎ | 中-高 |
| **1** | トルコ語 | 中 | 3-4 | ✗ | ◎ | 中 |
| **1** | ドイツ語 | 中 | 3-4 | ✗ | ◎ | 高 |
| **2** | ベトナム語 | 中-高 | 4-5 | ✗ | ◎ | 中-高 |
| **2** | イタリア語 | 中 | 3-4 | ✗ | ◎ | 中 |
| **2** | ポーランド語 | 中-高 | 4-5 | ✗ | ◎ | 中 |
| **2** | 韓国語 | 高 | 5-6 | ✗ | ◎ | 中-高 |
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

#### 韓国語 (Korean)
- **音素体系**: 子音19種（激音・帯気・双音対立）、母音10種
- **課題**: ハングル分解、連音化・鼻音化・流音化規則
- **参照**: g2pK（GitHub Stars 3.4k+）

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

### 9.1 NativeAOT対応（優先度: 中、推定工数: 3-4日）

**現状**: 20ファイルで `Assembly.GetManifestResourceStream()` によるリフレクション使用。NativeAOT非対応。

**対策**:
- `DynamicallyAccessedMembers` 属性の適用
- 埋め込みリソース読み込みのAoT互換化（`ModuleInitializer` or 事前バイナリ化）
- Trimming用の `.rd.xml` 設定
- `PublishAot=true` / `PublishTrimmed=true`

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

### 10.1 NuGet PackageReadmeFile 設定（優先度: 最高）

**現状**: 11パッケージ中9パッケージで PackageReadmeFile 未設定。

**提案**: 各パッケージに 50-80 行の README.md + csproj に `<PackageReadmeFile>` 追加。

### 10.2 APIドキュメント生成（優先度: 高）

**現状**: XMLドキュメント生成は全csprojで有効だが、`///` コメント密度は低い（146ファイル中19ファイルのみ充実）。DocFX未導入。

**提案**:
- DocFX統合（`docfx.json` 作成、GitHub Pages公開）
- 全公開APIにXML Docコメント充実化（例示付き）
- サンプルコードの自動テスト

### 10.3 CONTRIBUTING.md 新規作成（優先度: 高）

Prerequisites / Setup / Code Standards / Testing / 新言語追加手順 を含む 150-200行のガイド。

### 10.4 言語別サンプルコード拡充（優先度: 高）

**現状**: `samples/DotNetG2P.Console/` は日本語のみ。各言語の基本使用例 + Multilingual 混在テキスト例を追加。

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
| 1 | NuGet PackageReadmeFile + 言語別README | DX | 2日 | NuGet発見性向上 |
| 2 | CONTRIBUTING.md / MIGRATION.md | DX | 2日 | 貢献促進 |
| 3 | CI マトリックスビルド | CI/CD | 1日 | 互換性確保 |
| 4 | コードカバレッジ統合 | CI/CD | 1日 | 品質可視化 |
| 5 | Dependabot + Deterministic Build | セキュリティ | 1日 | サプライチェーン安全性 |
| 6 | BenchmarkDotNet 導入 | パフォーマンス | 1-2週 | ボトルネック可視化 |
| 7 | `IG2PEngine` インターフェース導入 | アーキテクチャ | 2週 | 保守性向上 |
| 8 | バッチAPI共通化 + テスト整備 | アーキテクチャ | 1-2週 | コード85%削減 |

### Phase 2: 短期（2-4ヶ月）— 機能拡張 + 新言語Tier 1

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 9 | インドネシア語 G2P | 新言語 | 2-3週 | 最高ROI新言語 |
| 10 | トルコ語 G2P | 新言語 | 3-4週 | 規則性高い新言語 |
| 11 | SSML サポート MVP | 機能拡張 | 2-3週 | TTS統合基盤 |
| 12 | マルチターゲット + FrozenDictionary | パフォーマンス | 2-3週 | 10-20%高速化 |
| 13 | DocFX + XMLDoc充実化 | DX | 1-2週 | API使いやすさ |

### Phase 3: 中期（4-8ヶ月）— 新言語Tier 1-2 + 機能拡張

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 14 | ドイツ語 G2P | 新言語 | 3-4週 | 欧州主要言語 |
| 15 | ストリーミングAPI | 機能拡張 | 2-3週 | リアルタイム対応 |
| 16 | ベトナム語 G2P | 新言語 | 4-5週 | アジア市場拡大 |
| 17 | NativeAOT対応 | 品質 | 3-4日 | Unity/モバイル |
| 18 | Unity Editor拡張 | Unity | 4-5週 | ゲーム市場 |

### Phase 4: 長期（8-12ヶ月）— 大規模展開

| # | 項目 | 領域 | 工数 | 効果 |
|---|------|------|------|------|
| 19 | イタリア語 + ポーランド語 G2P | 新言語 | 7-9週 | 12言語対応 |
| 20 | 韓国語 G2P（強化/新規） | 新言語 | 5-6週 | アジア主要言語 |
| 21 | WebAssembly対応 | 機能拡張 | 3-4週 | Web市場 |
| 22 | 音韻規則エンジン汎用化 | アーキテクチャ | 4-5週 | 新言語追加容易化 |
| 23 | ロシア語 G2P（ML統合） | 新言語 | 5-7週 | 東欧市場 |
| 24 | gRPCサービステンプレート | エコシステム | 2-3週 | 企業向け |

### 目標到達点

| フェーズ完了 | 対応言語数 | 主な新機能 |
|------------|-----------|-----------|
| Phase 1 | 6言語（現状維持） | 品質基盤・CI/CD強化 |
| Phase 2 | 8言語 | SSML、マルチターゲット |
| Phase 3 | 10言語 | ストリーミング、NativeAOT |
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
| Phonemizer | Python | GPL 3.0 | espeak-ngラッパー |
| Gruut | Python | MIT | lexicon + CRFベース |
| DeepPhonemizer | Python | Apache-2.0 | TransformerニューラルG2P |
| piper-phonemize | C++ | MIT | Piper TTS用 |
| Epitran | Python | MIT | 多言語IPAマッピング |
| NVIDIA NeMo G2P | Python | Apache-2.0 | 多言語G2Pフレームワーク |
