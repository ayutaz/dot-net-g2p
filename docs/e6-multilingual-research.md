# E6 日英混在テキスト対応 — 詳細調査レポート

Issue: [#1 espeak-ngと同等の精度の英語のg2p for C#を実装する](https://github.com/ayutaz/dot-net-g2p/issues/1)

## 調査概要

10エージェントチームによる並列調査を実施し、E6（日英混在テキスト対応）の実装に必要な全方面を調査した。

---

## 1. 日英両エンジンのAPI比較

### 1.1 G2PEngine（日本語）

| 項目 | 内容 |
|------|------|
| クラス | `sealed class G2PEngine : IDisposable` |
| コンストラクタ | `G2PEngine(ITokenizer)`, `G2PEngine(ITokenizer, G2POptions)` |
| 辞書 | **外部必須**（naist-jdicディレクトリパス → MeCabTokenizer経由） |
| スレッドセーフ | **No**（LatticeBuilderの内部バッファが再利用されるため） |

**主要API:**
| メソッド | 戻り値 | 説明 |
|----------|--------|------|
| `ToPhonemes(string)` | `string` | `"k o N n i ch i w a"` |
| `ToKana(string)` | `string` | `"コンニチワ"` |
| `ToProsody(string)` | `string` | ESPnet韻律記号付き |
| `ToAccentPhrases(string)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX互換 |
| `ToFullContextLabels(string)` | `IReadOnlyList<string>` | HTSラベル |
| `ToProsodyFeatures(string)` | `ProsodyFeatures` | A1/A2/A3韻律特徴量 |
| `Analyze(string)` | `IReadOnlyList<NjdNode>` | NJD中間結果 |
| `ToPhonemesBatch(...)` 等5種 | バッチ版 | ループ呼び出し |

**G2POptions:**
| プロパティ | デフォルト |
|-----------|----------|
| EnableTextNormalization | true |
| EnableUnvoicedVowel | true |
| EnableDigitProcessing | true |
| EnableAccentPhrase | true |
| EnableAccentType | true |
| ExpandLongVowels | true |

### 1.2 EnglishG2PEngine（英語）

| 項目 | 内容 |
|------|------|
| クラス | `sealed class EnglishG2PEngine : IDisposable` |
| コンストラクタ | `EnglishG2PEngine()`, `(options)`, `(dictPath)`, `(dictPath, options)` |
| 辞書 | **埋め込みリソース**（引数なしコンストラクタ可） |
| スレッドセーフ | **Yes**（全データが初期化後読み取り専用） |

**主要API:**
| メソッド | 戻り値 | 説明 |
|----------|--------|------|
| `ToPhonemes(string)` | `string` | `"HH AH0 L OW1"` (ARPAbet) |
| `ToPhonemeList(string)` | `IReadOnlyList<EnglishPhoneme>` | 構造化 |
| `ToIPA(string)` | `string` | `"həˈloʊ"` |
| `ToIPAWithoutStress(string)` | `string` | ストレスなしIPA |
| `ToXSampa(string)` | `string` | X-SAMPA |
| `ToXSampaWithoutStress(string)` | `string` | ストレスなしX-SAMPA |
| `LookupWord(string)` | `IReadOnlyList<EnglishPhoneme>` | 単語検索 |
| `ContainsWord(string)` | `bool` | 辞書存在確認 |
| バッチAPI 4種 | バッチ版 | ループ呼び出し |

**EnglishG2POptions:**
| プロパティ | デフォルト |
|-----------|----------|
| IncludeStress | true |
| UnknownWordHandling | Skip |
| EnableLts | true |
| EnableNormalization | true |
| EnableHomographResolution | true |

### 1.3 設計上の非対称性

| 項目 | 日本語 | 英語 |
|------|--------|------|
| 辞書 | 外部必須（naist-jdic ~60MB） | 埋め込み（CMU ~4MB） |
| 初期化コスト | 高（辞書I/O 200-500ms） | 中（テキストパース 500ms-2s） |
| メモリ | ~55-65MB | ~30-50MB |
| スレッドセーフ | No | Yes |
| Dispose | DictionaryBundle参照カウント | 参照null化のみ |
| 辞書キャッシュ | WeakReference + 参照カウント | なし |

---

## 2. 言語判定アルゴリズム設計

### 2.1 文字種分類

```csharp
public enum ScriptKind { Japanese, English, Digit, Punctuation, Whitespace, Other }
```

| Unicode範囲 | 分類 |
|------------|------|
| U+3040-309F（ひらがな） | Japanese |
| U+30A0-30FF（カタカナ） | Japanese |
| U+4E00-9FFF（CJK統合漢字） | Japanese |
| U+3400-4DBF（CJK拡張A） | Japanese |
| U+FF65-FF9F（半角カナ） | Japanese |
| U+3000-303F（CJK記号・句読点） | Japanese |
| A-Z, a-z（ASCII英字） | English |
| 0-9（ASCII数字） | Digit |
| その他ASCII記号 | Punctuation |
| 空白・タブ・改行 | Whitespace |
| 上記以外 | Other |

### 2.2 セグメント分割アルゴリズム

```
入力: "今日はgood dayですね"
Step 1: 文字分類 → 今(J)日(J)は(J)g(E)o(E)o(E)d(E)( )(SP)d(E)a(E)y(E)で(J)す(J)ね(J)
Step 2: 連続同一スクリプトをグループ化
Step 3: 空白を英語セグメントにマージ（英語は空白区切りが自然）
Step 4: 隣接同一言語セグメントをマージ
結果: ("今日は", JA), ("good day", EN), ("ですね", JA)
```

### 2.3 数字・記号の帰属ルール

- **数字**: 隣接セグメントの言語に吸収（`"100人"` → JA、`"page 123"` → EN）
- **句読点**: 直前のセグメントに付属
- **全角英字**: 正規化前に言語判定を行うため、English扱い
- **アポストロフィ**: 英語コンテキストで英語の一部（`don't`）

### 2.4 重要な設計判断: TextNormalizerとの順序

**現在のパイプライン:**
```
入力 → TextNormalizer（ASCII→全角化） → MeCab → NJD → 音素
```

**問題**: TextNormalizerがASCII英字を全角化（`hello` → `ｈｅｌｌｏ`）するため、正規化後に言語判定すると英語テキストが破壊される。

**解決策: 言語判定はTextNormalization前に行う**
```
入力 → LanguageSegmenter（言語判定・分割）
  → JAセグメント → TextNormalizer → MeCab → NJD → 日本語音素
  → ENセグメント → EnglishNormalizer → CMU/LTS → 英語音素
→ 結合
```

---

## 3. 出力形式統一戦略

### 3.1 検討した4案

| 案 | 概要 | 評価 |
|----|------|------|
| 案1: IPA統一 | 日英ともIPA出力 | 補助APIとしては有用。メイン出力には不向き（TTS互換性損失） |
| 案2: 各言語体系維持 | JA=日本語音素、EN=ARPAbet | **推奨**。TTS互換性最優先 |
| 案3: セグメント分離 | `List<G2PSegment>` | 案2と組み合わせ推奨。高度なユースケース対応 |
| 案4: カスタムマーカー | `{ja}...{en}...` | 非推奨。独自規格 |

### 3.2 推奨: 案2+案3のハイブリッド

```csharp
MultilingualG2PEngine
├── ToPhonemes(text)    → "k o N n i ch i w a HH AH0 L OW1"  // 単純結合
├── ToSegments(text)    → List<G2PSegment>                    // 構造化
├── ToIPA(text)         → "koɴnitɕiwa həˈloʊ"                // IPA統一（補助）
└── 言語別メソッド
    ├── ToProsody(text)       → ESPnet韻律記号（日本語部分のみ）
    ├── ToAccentPhrases(text) → VOICEVOX互換（日本語部分のみ）
    └── ...
```

### 3.3 G2PSegmentデータモデル

```csharp
public readonly struct TextSegment
{
    public string Text { get; }        // 原文テキスト
    public Language Language { get; }   // Japanese / English
}

public class G2PSegment
{
    public string Language { get; }     // "ja" / "en"
    public string SourceText { get; }   // 原文
    public string Phonemes { get; }     // 言語固有の音素表記
}

public enum Language { Japanese, English }
```

---

## 4. パッケージ構成

### 4.1 推奨: 新パッケージ `DotNetG2P.Multilingual`

```
DotNetG2P.Multilingual → DotNetG2P (Core) + DotNetG2P.English
```

**3案の比較:**

| 案 | 構成 | 評価 |
|----|------|------|
| **案1: 新パッケージ** | `DotNetG2P.Multilingual` 新規作成 | **推奨**。既存パッケージの独立性維持 |
| 案2: Core統合 | CoreにMultilingualを追加 | 非推奨。CoreがEnglishに依存してしまう |
| 案3: English統合 | EnglishにMultilingualを追加 | 非推奨。EnglishがCoreに依存してしまう |

### 4.2 パッケージ依存グラフ（案1適用後）

```
ユーザーの選択:
  日本語のみ    → DotNetG2P + DotNetG2P.MeCab
  英語のみ      → DotNetG2P.English
  日英混在      → DotNetG2P.Multilingual (→ Core + English を自動解決)
```

### 4.3 csproj設計

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <PackageId>DotNetG2P.Multilingual</PackageId>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\DotNetG2P.Core\DotNetG2P.Core.csproj" />
    <ProjectReference Include="..\DotNetG2P.English\DotNetG2P.English.csproj" />
  </ItemGroup>
</Project>
```

### 4.4 CI/CD変更（最小限）

- `ci.yml` / `release.yml` に `dotnet pack src/DotNetG2P.Multilingual/...` を1行追加
- UPMパッケージ: `com.dotnetg2p.multilingual` を追加

---

## 5. スレッドセーフティ・Disposeパターン設計

### 5.1 スレッドセーフティ戦略

**英語エンジンは共有可能、日本語エンジンは排他制御が必要。**

| 選択肢 | 方式 | 評価 |
|--------|------|------|
| A: lock保護 | 日本語アクセスを`lock`で直列化 | シンプル。並列性は低い |
| **B: ThreadLocal** | スレッドごとにG2PEngineインスタンス | **推奨**。DictionaryBundleの参照カウント共有と相性良好 |

**推奨パターン（ThreadLocal）:**
```csharp
public sealed class MultilingualG2PEngine : IDisposable
{
    private readonly EnglishG2PEngine _englishEngine;           // 共有OK
    private readonly ThreadLocal<G2PEngine> _japaneseEngines;   // スレッドごと
    // DictionaryBundleは参照カウントキャッシュで自動共有（メモリ増加は最小限）
}
```

### 5.2 Disposeパターン

```csharp
public void Dispose()
{
    if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

    // ThreadLocal内の全G2PEngineをDispose（trackAllValues: true必須）
    foreach (var engine in _japaneseEngines.Values)
        engine.Dispose();
    _japaneseEngines.Dispose();
    _englishEngine.Dispose();
    GC.SuppressFinalize(this);
}
```

### 5.3 遅延初期化

```csharp
public MultilingualG2PEngine(string japaneseDictPath)
{
    // パス存在チェックは即座に実行（早期エラー検出）
    if (!Directory.Exists(japaneseDictPath))
        throw new DirectoryNotFoundException(japaneseDictPath);

    _japaneseEngines = new ThreadLocal<G2PEngine>(() =>
        new G2PEngine(new MeCabTokenizer(japaneseDictPath)), trackAllValues: true);
    _englishEngine = new Lazy<EnglishG2PEngine>(() => new EnglishG2PEngine());
}
```

---

## 6. エッジケース・境界条件

### 6.1 優先度P0（クラッシュ防止）

| ケース | 期待動作 |
|--------|---------|
| 空文字列 / null | 空結果 |
| 空白のみ | 空結果 |
| 記号のみ（`♪★◆`） | 無音 |
| 絵文字（サロゲートペア） | 無音 |
| 1000+文字の長文 | 正常処理 |

### 6.2 優先度P1（正しい言語分離）

| ケース | 入力例 | 期待分割 |
|--------|--------|---------|
| 典型的な混在 | `"私はhelloと言った"` | JA / EN / JA |
| 英語スペース保持 | `"今日はgood dayですね"` | JA / EN / JA |
| ブランド名 | `"iPhone12を買った"` | EN / JA |
| 頭字語 | `"HTMLを書く"` | EN / JA |
| 数字帰属 | `"100人"` vs `"page 100"` | JA / EN |

### 6.3 優先度P2（出力品質）

| ケース | 注意点 |
|--------|--------|
| ハイフン英語 | `"state-of-the-art"` → 1チャンク |
| アポストロフィ | `"don't"` → 英語の一部 |
| 全角英字 | `"Ｈｅｌｌｏ"` → 言語判定前に検出 |
| 半角カナ | `"ｶﾀｶﾅ"` → Japanese |
| カタカナ外来語 | `"コンピュータ"` → Japanese（問題なし） |
| ローマ字 | `"Tokyo"` → English扱い（CMU辞書/LTSで処理） |

### 6.4 優先度P3（将来）

| ケース | 注意点 |
|--------|--------|
| URL | 検出してスキップ or 特殊処理 |
| メールアドレス | 検出してスキップ |
| 3言語以上 | 現バージョンでは日英のみ |
| 中黒区切り英語 | `"man・in・the・middle"` 要設計判断 |

### 6.5 TextNormalizerとの相互作用（最大の設計課題）

- TextNormalizerはASCIIを全角化（`hello` → `ｈｅｌｌｏ`）するため、**言語判定の前に実行不可**
- 言語判定 → 分割 → 各セグメントごとにNormalization適用が正しい順序
- 英語セグメントには`EnglishNormalizer`、日本語セグメントには`TextNormalizer`を適用

---

## 7. 先行事例からの知見

### 7.1 業界アプローチの比較

| ライブラリ | 混在テキスト対応 |
|-----------|----------------|
| **espeak-ng** | SSMLの`<lang>`タグで明示切り替え。自動検出なし |
| **Phonemizer** | バックエンド全体に1言語指定。自動分割なし |
| **piper-phonemize** | 1言語指定（文単位）。プラグインアーキテクチャ議論中 |
| **Style-BERT-VITS2** | pyopenjtalkに全テキスト渡し。読めない文字はスキップ |
| **GPT-SoVITS** | 日本語→pyopenjtalk、英語→g2p_en で言語別G2P |
| **OpenJTalk/pyopenjtalk** | 辞書にない英字は1文字ずつスペルアウト |
| **VOICEVOX** | `enable_katakana_english`でカタカナ変換 or スペルアウト |
| **OLaPh** | Linguaフレームワークで単語レベル言語検出 + 言語別辞書 |

### 7.2 DotNetG2Pへの推奨

**GPT-SoVITSパターン（文字種ベース分割 + 言語別G2Pディスパッチ）が最適。**

理由:
1. 実績のあるアプローチ（複数のTTSシステムが採用）
2. 既存の日英G2Pエンジンをそのまま活用可能
3. 文字種ベースの判定は日英の区別が明確（CJK vs Latin）
4. 統計モデルや外部依存不要

---

## 8. テスト戦略

### 8.1 テストファイル構成

```
tests/DotNetG2P.Tests/
└── Multilingual/
    ├── LanguageDetectorTests.cs        # ~25件（辞書不要）
    ├── TextSegmenterTests.cs           # ~30件（辞書不要）
    ├── MultilingualEngineTests.cs      # ~35件（辞書必要: SkippableFact）
    ├── LanguageConsistencyTests.cs     # ~20件（辞書必要）
    ├── MultilingualEdgeCaseTests.cs    # ~30件（一部辞書必要）
    ├── MultilingualPerformanceTests.cs # ~8件（辞書必要）
    └── MultilingualDisposeTests.cs     # ~15件（一部辞書必要）
```

### 8.2 テスト件数見積もり

| カテゴリ | 件数 | 辞書依存 |
|---------|------|---------|
| 言語判定 単体テスト | ~25 | なし |
| セグメント分割 単体テスト | ~30 | なし |
| エンジン統合テスト | ~35 | あり |
| 単独エンジン一致テスト | ~20 | あり |
| エッジケーステスト | ~30 | 一部 |
| パフォーマンステスト | ~8 | あり |
| Dispose/スレッドセーフテスト | ~15 | 一部 |
| **合計** | **~163件** | |

### 8.3 テスト優先度

1. **最優先**: LanguageDetector + TextSegmenter（辞書不要、CIで常時実行）
2. **高優先**: MultilingualEngineTests（基本統合テスト）
3. **中優先**: LanguageConsistency + EdgeCase（品質保証）
4. **低優先**: Performance + Dispose（非機能要件）

---

## 9. メモリ・パフォーマンス見積もり

| 指標 | 日本語のみ | 英語のみ | 両方同時 |
|------|----------|---------|---------|
| メモリ | ~55-65MB | ~30-50MB | **~90-120MB** |
| 初期化時間 | 200-500ms | 500ms-2s | ~1-3s |
| 辞書キャッシュ | WeakRef共有 | なし | 日本語は共有可能 |

---

## 10. 実装ロードマップ

### Phase 1: コアロジック（言語判定 + セグメント分割）
- `LanguageDetector` static class
- `TextSegmenter` static class
- 単体テスト ~55件

### Phase 2: MultilingualG2PEngine + 基本API
- `MultilingualG2PEngine` class（ToPhonemes, ToSegments）
- コンストラクタ、IDisposable、ThreadLocal/lock
- 統合テスト ~35件

### Phase 3: 拡張API + エッジケース
- ToIPA、バッチAPI
- エッジケース処理
- エッジケーステスト + Disposeテスト ~45件

### Phase 4: パフォーマンス + パッケージング
- パフォーマンス最適化
- NuGet/UPM設定
- CI/CD更新
- パフォーマンステスト + 一致テスト ~28件

---

## 参考資料

- [実装計画書](./english-g2p-implementation-plan.md)
- [調査レポート](./english-g2p-research.md)
- [espeak-ng出力検証](./espeak-ng-output-verification.md)
