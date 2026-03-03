# DotNetG2P 設計ドキュメント

UniTask（Cysharp/UniTask）の設計パターンを参考に、DotNetG2Pの設計・フォルダ構成・パッケージング戦略を定義する。

---

## 1. 設計方針

### UniTaskから採用するパターン

| パターン | UniTaskでの使い方 | DotNetG2Pでの適用 |
|----------|------------------|-------------------|
| **Single Source of Truth** | Unity側Runtime/にソース実体、NuGet用csprojはCompile Includeで参照 | 同一戦略を採用。Unity側Runtime/が正 |
| **Directory.Build.props** | NuGetメタデータ・署名設定を共通化 | 同一戦略を採用 |
| **asmdefモジュール分離** | コア/Editor/外部連携を独立asmdef化 | Core/MeCab/Editorをasmdef分離 |
| **条件付きコンパイル** | `UNITASK_NETCORE` defineでプラットフォーム分岐 | `DOTNETG2P_NETCORE` defineを使用 |
| **バージョン外部注入** | コードにバージョン埋め込まず、CIの`-p:Version`で注入 | 同一戦略を採用 |
| **デュアルテスト** | .NET Core (xUnit) + Unity Test Runner (NUnit) 並行 | 同一戦略を採用 |
| **partial classによるファイル分割** | `UniTask.Factory.cs`, `UniTask.Delay.cs`等 | `G2PEngine.Convert.cs`, `G2PEngine.Config.cs`等 |

### DotNetG2P固有の設計判断

UniTaskは非同期ライブラリ（コード中心、データなし）であるのに対し、G2Pは**辞書データ（~80MB）に強く依存する自然言語処理ライブラリ**。以下はG2P固有の設計判断:

| 観点 | UniTask | DotNetG2P |
|------|---------|-----------|
| 外部データ | なし | naist-jdic辞書 (~80MB) |
| 外部ライブラリ依存 | なし | 独自MeCabエンジン（Apache-2.0、外部依存なし） |
| API特性 | 非同期・軽量struct | 同期中心・テキスト処理 |
| ライセンス制約 | MIT一本 | Apache-2.0（全コンポーネント） |
| プラットフォーム差異 | async/awaitの差異 | ファイルI/O・辞書ロードの差異 |

---

## 2. 現在の実装状況（M7完了時点）

M1（最小動作プロトタイプ）、M2（NJD処理パイプライン完成）、M3（出力形式の充実）、M4（テスト・品質保証）、M5（パッケージング）、M6（独自MeCabエンジン）、M7（パフォーマンス最適化）が完了し、以下の構成で動作している:

```
dot-net-g2p/
├── DotNetG2P.slnx                    # ソリューション（.NET 10 .slnx形式）
├── Directory.Build.props             # NuGet共通メタデータ
├── LICENSE                           # Apache-2.0 License
├── README.md                         # プロジェクトREADME（126行）
├── .editorconfig                     # コーディング規約
├── .gitattributes                    # Git属性設定
├── .github/workflows/
│   ├── ci.yml                        # CI（push/PR: ビルド・テスト・パック）
│   └── release.yml                   # リリース（NuGet push + GitHub Release）
├── CLAUDE.md                          # プロジェクトガイダンス
├── docs/                              # ドキュメント
│   ├── design.md                      # 本ドキュメント
│   ├── implementation-plan.md         # 実装計画
│   ├── roadmap.md                     # ロードマップ
│   └── research/                      # 調査資料（01-17）
├── src/
│   ├── DotNetG2P.Core/               # コアライブラリ（netstandard2.1）約10,100行
│   │   ├── Models/
│   │   │   ├── Phoneme.cs            # Consonant enum (35種) + Vowel enum (10種)
│   │   │   ├── MoraKind.cs           # MoraKind enum (~165種)
│   │   │   ├── Mora.cs               # readonly struct Mora
│   │   │   ├── Pronunciation.cs      # List<Mora> + AccentPosition + ParseMoraSegments
│   │   │   ├── POS.cs                # POSType enum (14種) + POS sealed class
│   │   │   ├── WordDetails.cs        # 形態素詳細情報
│   │   │   ├── WordEntry.cs          # 辞書エントリ
│   │   │   ├── NjdNode.cs            # NJDノード（MergeFrom/Reset/ChainFlag 3値）
│   │   │   └── AccentPhrase.cs       # VOICEVOX互換アクセント句
│   │   ├── Tokenizer/
│   │   │   ├── ITokenizer.cs         # 形態素解析エンジン抽象化
│   │   │   └── IToken.cs             # トークンインターフェース（15フィールド）
│   │   ├── NJD/                      # NJD処理パイプライン（6段階）
│   │   │   ├── SetPronunciation.cs   # 1. 発音設定（完全版5段階処理）
│   │   │   ├── DigitSequence.cs      # 2a. 数字列検出・グループ化
│   │   │   ├── DigitLut.cs           # 2b. 数字読みLUTテーブル
│   │   │   ├── SetDigit.cs           # 2c. 数字読み変換メインロジック
│   │   │   ├── SetAccentPhrase.cs    # 3. アクセント句結合（18ルール）
│   │   │   ├── SetAccentType.cs      # 4. アクセント結合型（C1-C5, F1-F5, P系列）
│   │   │   └── SetUnvoicedVowel.cs   # 5. 無声音化（6ルール）
│   │   ├── TextNormalization/
│   │   │   └── TextNormalizer.cs     # テキスト正規化（全角/半角変換、濁点結合）
│   │   ├── Internal/                # 内部ユーティリティ
│   │   │   ├── ValueStringBuilder.cs # ゼロアロケーション文字列構築（ref struct, ArrayPool）
│   │   │   └── ThrowHelper.cs       # 例外スローヘルパー（[NoInlining]）
│   │   ├── PhonemeConverter/
│   │   │   ├── MoraMapping.cs        # 162種カタカナ⇔音素マッピング
│   │   │   ├── AccentPhraseConverter.cs  # VOICEVOX互換AccentPhrase出力
│   │   │   └── ProsodyExtractor.cs   # ESPnet韻律記号付き出力
│   │   ├── JPCommon/                 # フルコンテキストラベル生成
│   │   │   ├── Models.cs             # 6階層モデル（JPUtterance→...→JPPhoneme）
│   │   │   ├── JPCommonBuilder.cs    # NjdNode→JPUtterance階層構築
│   │   │   ├── FullContextLabel.cs   # HTSフルコンテキストラベル生成
│   │   │   └── WordAttr.cs           # POS/CType/CForm→ID変換テーブル
│   │   ├── G2PEngine.cs              # メインAPI (ToPhonemes, ToKana, ToProsody, ToAccentPhrases, ToFullContextLabels, Analyze, +Batch API)
│   │   ├── G2POptions.cs             # 処理オプション（各段階ON/OFF）
│   │   ├── package.json              # UPMパッケージ定義 (com.dotnetg2p.core)
│   │   └── DotNetG2P.asmdef          # Unity Assembly Definition
│   └── DotNetG2P.MeCab/              # 独自MeCabエンジン（Apache-2.0、外部依存なし）
│       ├── DotNetG2P.MeCab.csproj    # .NET Standard 2.1、DotNetG2P.Core参照のみ
│       ├── MeCabTokenizer.cs         # ITokenizer実装（公開API）
│       ├── Dictionary/              # 辞書読み込み層
│       │   ├── DictionaryHeader.cs  # 72バイトヘッダパーサ
│       │   ├── DicToken.cs          # トークン構造体（16バイト）
│       │   ├── SystemDictionary.cs  # sys.dic読み込み
│       │   ├── ConnectionMatrix.cs  # matrix.bin読み込み（連接コスト行列）
│       │   ├── CharProperty.cs      # char.bin読み込み（文字カテゴリ）
│       │   ├── UnknownDictionary.cs # unk.dic読み込み（未知語テンプレート）
│       │   └── DictionaryBundle.cs  # 全辞書ファイル集約管理
│       ├── Trie/                    # DoubleArray Trie
│       │   ├── DoubleArrayTrie.cs   # 共通接頭辞検索
│       │   └── Utf8CharMap.cs       # UTF-8バイト⇔char オフセット変換
│       └── Lattice/                 # ラティス＋Viterbi
│           ├── LatticeNode.cs       # ラティスノード
│           ├── LatticeBuilder.cs    # Trie検索+未知語生成→ラティス構築
│           └── ViterbiDecoder.cs    # 前向きパス+後ろ向きトレース
├── tests/
│   ├── TestData/                      # テストデータ
│   │   ├── expected_phonemes.json     # pyopenjtalk期待値（18件）
│   │   └── generate_expected.py       # テストデータ生成スクリプト
│   └── DotNetG2P.Tests/              # xUnitテスト（1,600超）
│       ├── Models/
│       │   ├── NjdNodeTests.cs
│       │   └── PronunciationTests.cs
│       ├── NJD/
│       │   ├── SetPronunciationTests.cs    # 発音設定（25件）
│       │   ├── SetAccentPhraseTests.cs     # アクセント句結合（37件）
│       │   ├── SetAccentTypeTests.cs       # アクセント結合型（39件）
│       │   ├── DigitSequenceTests.cs       # 数字列検出（14件）
│       │   ├── SetDigitTests.cs            # 数字読み変換（32件）
│       │   ├── DigitReadingTests.cs        # 数字読み網羅（25件、辞書依存）
│       │   └── SetUnvoicedVowelTests.cs
│       ├── TextNormalization/
│       │   └── TextNormalizerTests.cs
│       ├── PhonemeConverter/
│       │   ├── MoraMappingTests.cs
│       │   ├── MoraMappingFullTests.cs     # 全165パターン検証（166件）
│       │   ├── AccentPhraseConverterTests.cs
│       │   └── ProsodyExtractorTests.cs
│       ├── JPCommon/
│       │   ├── JPCommonBuilderTests.cs
│       │   ├── FullContextLabelTests.cs
│       │   └── WordAttrTests.cs
│       ├── MeCab/                          # MeCabエンジンテスト
│       │   ├── MeCabTokenizerTests.cs      # 基本動作テスト（~30件）
│       │   ├── TokenizerComparisonTests.cs # 出力一致テスト（100+文×3）
│       │   ├── G2PComparisonTests.cs       # G2Pパイプライン比較テスト（20件×6）
│       │   ├── MeCabIndependentTests.cs    # 辞書非依存テスト
│       │   ├── PerformanceTests.cs         # パフォーマンステスト
│       │   ├── Utf8CharMapTests.cs         # UTF-8オフセット変換テスト
│       │   └── DictionaryErrorTests.cs     # エラーハンドリングテスト
│       ├── Integration/
│       │   ├── G2PPipelineTests.cs
│       │   ├── EdgeCaseTests.cs            # エッジケース（~57件）
│       │   ├── PiperPlusTests.cs           # piper-plus移植（87件）
│       │   └── PyOpenJTalkComparisonTests.cs  # pyopenjtalk比較（20件）
│       └── G2PEngineApiTests.cs
└── samples/
    └── DotNetG2P.Console/            # コンソールサンプル（M3対応）
```

### ビルド・実行

```bash
# ビルド
dotnet build DotNetG2P.slnx

# 辞書なしモード（MoraMapping動作確認のみ）
dotnet run --project samples/DotNetG2P.Console

# 辞書ありモード（完全G2P変換、NJDパイプライン付き）
dotnet run --project samples/DotNetG2P.Console -- <naist-jdic辞書パス>
```

## 3. 将来のフォルダ構成（M5パッケージング時）

```
DotNetG2P/
├── .github/
│   └── workflows/
│       ├── build-debug.yaml          # PR/push時のCI（ビルド+テスト）
│       └── build-release.yaml        # リリースCI（NuGet push + .unitypackage + GitHub Release）
│
├── Directory.Build.props             # NuGet共通設定（メタデータ、署名等）
├── DotNetG2P.slnx                     # .NETソリューション
├── LICENSE                           # Apache-2.0ライセンス
├── README.md
│
├── docs/
│   ├── design.md                     # 本ドキュメント
│   ├── implementation-plan.md        # 実装計画
│   └── research/                     # 調査資料（01-15）
│
├── src/
│   ├── DotNetG2P.Unity/              # Unityプロジェクト（ソースの正本）
│   │   ├── Assets/
│   │   │   ├── Editor/
│   │   │   │   └── PackageExporter.cs    # .unitypackageエクスポーター
│   │   │   │
│   │   │   └── Plugins/DotNetG2P/        # UPMパッケージルート
│   │   │       ├── package.json           # UPM定義
│   │   │       ├── LICENSE
│   │   │       ├── README.md
│   │   │       │
│   │   │       ├── Runtime/               # ランタイムコード（=ソースの正本）
│   │   │       │   ├── DotNetG2P.asmdef
│   │   │       │   │
│   │   │       │   ├── Models/
│   │   │       │   │   ├── POS.cs                # 品詞enum（ネスト構造）
│   │   │       │   │   ├── MoraKind.cs           # モーラenum（約150種）
│   │   │       │   │   ├── Mora.cs               # モーラ構造体
│   │   │       │   │   ├── Pronunciation.cs      # 発音（List<Mora> + AccentPosition）
│   │   │       │   │   ├── WordDetails.cs        # 単語詳細情報
│   │   │       │   │   ├── WordEntry.cs          # 辞書エントリ
│   │   │       │   │   ├── NjdNode.cs            # NJDノード
│   │   │       │   │   ├── AccentPhrase.cs       # アクセント句
│   │   │       │   │   └── Phoneme.cs            # 音素定義
│   │   │       │   │
│   │   │       │   ├── Tokenizer/
│   │   │       │   │   ├── ITokenizer.cs         # 形態素解析エンジン抽象化
│   │   │       │   │   └── IToken.cs             # トークンインターフェース（15フィールド）
│   │   │       │   │
│   │   │       │   ├── TextNormalization/
│   │   │       │   │   ├── TextNormalizer.cs     # テキスト正規化
│   │   │       │   │   └── DigitRules.cs         # 数字読み規則
│   │   │       │   │
│   │   │       │   ├── NJD/                      # NJD処理（6段階、順序厳守）
│   │   │       │   │   ├── SetPronunciation.cs   # 1. 発音生成
│   │   │       │   │   ├── DigitSequence.cs      # 2a. 数字連続検出
│   │   │       │   │   ├── SetDigit.cs           # 2b. 数字読み変換
│   │   │       │   │   ├── SetAccentPhrase.cs    # 3. アクセント句結合（18ルール）
│   │   │       │   │   ├── SetAccentType.cs      # 4. アクセント結合型（C1-C5, F1-F5, P系列）
│   │   │       │   │   └── SetUnvoicedVowel.cs   # 5. 無声音化（6ルール）
│   │   │       │   │
│   │   │       │   ├── JPCommon/                 # フルコンテキストラベル生成
│   │   │       │   │   ├── Utterance.cs
│   │   │       │   │   ├── BreathGroup.cs
│   │   │       │   │   └── FullContextLabel.cs
│   │   │       │   │
│   │   │       │   ├── PhonemeConverter/
│   │   │       │   │   ├── MoraMapping.cs        # カタカナ⇔音素（247種）
│   │   │       │   │   └── ProsodyExtractor.cs   # 韻律記号抽出
│   │   │       │   │
│   │   │       │   ├── Internal/                 # 内部ユーティリティ
│   │   │       │   │   └── StringHelper.cs       # 文字列処理ヘルパー
│   │   │       │   │
│   │   │       │   └── G2PEngine.cs              # メインAPI
│   │   │       │
│   │   │       ├── Editor/                        # Editor専用コード
│   │   │       │   ├── DotNetG2P.Editor.asmdef
│   │   │       │   └── DotNetG2PSettingsProvider.cs  # Project Settings UI（辞書パス設定等）
│   │   │
│   │   ├── Packages/
│   │   │   └── manifest.json
│   │   └── ProjectSettings/
│   │
│   ├── DotNetG2P.NetCore/            # NuGet用プロジェクト（ソース実体なし）
│   │   ├── DotNetG2P.NetCore.csproj  # Compile IncludeでRuntime/**/*.csを参照
│   │   └── NetCore/                  # .NET Core専用の差し替えファイル（辞書ロード等）
│   │       └── DictionaryLoader.cs   # System.IO.File直接利用版
│
├── tests/
│   └── DotNetG2P.Tests/              # .NET Core用xUnitテスト
│       ├── DotNetG2P.Tests.csproj
│       ├── NJD/
│       │   ├── SetPronunciationTests.cs
│       │   ├── SetDigitTests.cs
│       │   ├── SetAccentPhraseTests.cs
│       │   ├── SetAccentTypeTests.cs
│       │   └── SetUnvoicedVowelTests.cs
│       ├── PhonemeConverter/
│       │   ├── MoraMappingTests.cs
│       │   └── ProsodyExtractorTests.cs
│       ├── TextNormalization/
│       │   └── TextNormalizerTests.cs
│       └── Integration/
│           └── G2PEngineTests.cs     # pyopenjtalk出力との比較テスト
│
└── samples/
    └── DotNetG2P.Console/            # コンソールサンプル
        ├── DotNetG2P.Console.csproj
        └── Program.cs
```

### UniTaskとの構成比較

| 要素 | UniTask | DotNetG2P |
|------|---------|-----------|
| ソース正本 | `src/UniTask/Assets/Plugins/UniTask/Runtime/` | `src/DotNetG2P.Unity/Assets/Plugins/DotNetG2P/Runtime/` |
| NuGet用csproj | `src/UniTask.NetCore/` | `src/DotNetG2P.NetCore/` |
| テスト | `src/UniTask.NetCoreTests/` | `tests/DotNetG2P.Tests/` |
| UPM package.json | `Assets/Plugins/UniTask/package.json` | `Assets/Plugins/DotNetG2P/package.json` |
| サンプル | `Assets/Scenes/` + NetCoreSandbox | `samples/DotNetG2P.Console/` |
| 辞書データ | なし | StreamingAssets~/DotNetG2P/naist-jdic/ |

---

## 4. プロジェクト構成

### DotNetG2P.slnx

```
DotNetG2P.slnx
├── DotNetG2P.NetCore          # NuGetパッケージ用メインライブラリ
├── DotNetG2P.MeCab            # 独自MeCabエンジン（Apache-2.0）
├── DotNetG2P.Tests            # xUnitテスト
└── DotNetG2P.Console          # コンソールサンプル
```

### DotNetG2P.NetCore.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.1;net6.0;net8.0</TargetFrameworks>
    <AssemblyName>DotNetG2P</AssemblyName>
    <RootNamespace>DotNetG2P</RootNamespace>
    <LangVersion>9.0</LangVersion>
    <DefineConstants>DOTNETG2P_NETCORE</DefineConstants>
    <IsPackable>true</IsPackable>
    <Id>DotNetG2P</Id>
    <Description>Japanese Grapheme-to-Phoneme library for .NET and Unity. OpenJTalk-compatible rule-based G2P pipeline.</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- Unity側Runtime/のソースを直接参照 -->
    <Compile Include="..\DotNetG2P.Unity\Assets\Plugins\DotNetG2P\Runtime\**\*.cs" />
    <!-- .NET Core専用の差し替えファイル -->
    <Compile Include="NetCore\**\*.cs" />
  </ItemGroup>
</Project>
```

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <PackageVersion>$(Version)</PackageVersion>
    <Company><!-- 組織名 --></Company>
    <Authors><!-- 著者名 --></Authors>
    <PackageTags>g2p;japanese;tts;phoneme;openjtalk</PackageTags>
    <PackageProjectUrl>https://github.com/xxx/DotNetG2P</PackageProjectUrl>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>$(PackageProjectUrl)</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
  </PropertyGroup>
  <ItemGroup>
    <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="\" />
    <EmbeddedResource Include="$(MSBuildThisFileDirectory)LICENSE" />
  </ItemGroup>
</Project>
```

---

## 5. 名前空間設計

UniTaskの `Cysharp.Threading.Tasks` パターンに倣い、ドメイン形式の名前空間を採用:

```
DotNetG2P                            # メイン名前空間（G2PEngine, ITokenizer, IToken）
DotNetG2P.Models                     # データ構造（POS, MoraKind, Mora, NjdNode, AccentPhrase等）
DotNetG2P.TextNormalization          # テキスト正規化（TextNormalizer, DigitRules）
DotNetG2P.NJD                        # NJD処理6段階（SetPronunciation, SetDigit等）
DotNetG2P.JPCommon                   # フルコンテキストラベル（Utterance, BreathGroup等）
DotNetG2P.PhonemeConverter           # 音素変換（MoraMapping, ProsodyExtractor）
DotNetG2P.Internal                   # 内部ユーティリティ（InternalsVisibleToで限定公開）

DotNetG2P.MeCab                      # 独自MeCabエンジン（Apache-2.0）
```

### 設計ポイント

- ルート名前空間 `DotNetG2P` にメインAPI（G2PEngine）と抽象化インターフェース（ITokenizer/IToken）を配置
- ユーザーが通常触るのは `DotNetG2P` と `DotNetG2P.Models` のみ
- `DotNetG2P.Internal` は `[assembly: InternalsVisibleTo("DotNetG2P.Tests")]` で限定公開

---

## 6. パブリックAPI設計

### メインAPI: G2PEngine

```csharp
namespace DotNetG2P
{
    /// <summary>
    /// 日本語G2P（書記素→音素変換）エンジン。
    /// OpenJTalk互換のルールベースパイプラインを提供する。
    /// </summary>
    public sealed class G2PEngine : IDisposable
    {
        // --- コンストラクタ ---
        public G2PEngine(ITokenizer tokenizer);
        public G2PEngine(ITokenizer tokenizer, G2POptions options);

        // --- 基本出力 ---
        /// <summary>音素列を返す（例: "k o N n i ch i w a"）</summary>
        public string ToPhonemes(string text);

        /// <summary>カタカナ読みを返す（例: "コンニチワ"）</summary>
        public string ToKana(string text);

        /// <summary>韻律記号付き音素列を返す（ESPnet prosody方式）</summary>
        public string ToProsody(string text);

        // --- 構造化出力 ---
        /// <summary>アクセント句のリストを返す（VOICEVOX互換）</summary>
        public IReadOnlyList<AccentPhrase> ToAccentPhrases(string text);

        /// <summary>HTSフルコンテキストラベルを返す</summary>
        public IReadOnlyList<string> ToFullContextLabels(string text);

        // --- バッチ処理 ---
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts);
        public IReadOnlyList<string> ToKanaBatch(IReadOnlyList<string> texts);
        public IReadOnlyList<string> ToProsodyBatch(IReadOnlyList<string> texts);
        public IReadOnlyList<IReadOnlyList<string>> ToFullContextLabelsBatch(IReadOnlyList<string> texts);

        // --- 中間表現 ---
        /// <summary>NJDノード列を返す（デバッグ・拡張用）</summary>
        public IReadOnlyList<NjdNode> ToNjdNodes(string text);

        public void Dispose();
    }

    public sealed class G2POptions
    {
        /// <summary>テキスト正規化を有効にするか（デフォルト: true）</summary>
        public bool EnableTextNormalization { get; set; } = true;

        /// <summary>無声音化処理を有効にするか（デフォルト: true）</summary>
        public bool EnableUnvoicedVowel { get; set; } = true;
    }
}
```

### 抽象化インターフェース

```csharp
namespace DotNetG2P
{
    public interface ITokenizer : IDisposable
    {
        IReadOnlyList<IToken> Tokenize(string text);
    }

    public interface IToken
    {
        string Surface { get; }
        string[] Features { get; }

        // naist-jdic 15フィールドへの便利アクセサ
        string POS { get; }              // 品詞
        string POSGroup1 { get; }        // 品詞細分類1
        string POSGroup2 { get; }        // 品詞細分類2
        string POSGroup3 { get; }        // 品詞細分類3
        string ConjugationType { get; }  // 活用型
        string ConjugationForm { get; }  // 活用形
        string OriginalForm { get; }     // 原形
        string Reading { get; }          // 読み
        string Pronunciation { get; }    // 発音
        string AccentInfo { get; }       // アクセント核位置/モーラ数
        string ChainRule { get; }        // アクセント結合タイプ
    }
}
```

### 使用例

```csharp
// 独自MeCabエンジンを使用
using var tokenizer = new MeCabTokenizer("/path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);

// 基本的な使い方
string phonemes = engine.ToPhonemes("こんにちは");
// => "k o N n i ch i w a"

string kana = engine.ToKana("今日は天気がいいですね");
// => "キョーワテンキガイーデスネ"

// VOICEVOX互換出力
var phrases = engine.ToAccentPhrases("今日は天気がいいですね");
foreach (var phrase in phrases)
{
    Console.WriteLine($"アクセント句: {phrase.Moras.Count}モーラ, アクセント核: {phrase.Accent}");
}

// フルコンテキストラベル（HTS形式）
var labels = engine.ToFullContextLabels("今日は天気がいいですね");
```

---

## 7. NuGet/UPM デュアルパッケージ戦略

UniTaskのパターンに従い、**ソースの正本をUnity側に置き、NuGet用csprojはCompile Includeで参照**する。

### NuGetパッケージ

| パッケージ | ライセンス | 依存関係 | 説明 |
|-----------|-----------|---------|------|
| `DotNetG2P` | Apache-2.0 | なし | コアG2Pエンジン |
| `DotNetG2P.MeCab` | Apache-2.0 | DotNetG2P | 独自MeCabエンジン |
| `DotNetG2P.Dictionary.NaistJdic` | BSD | なし | naist-jdic辞書データ（将来検討） |

### UPMパッケージ

#### package.json

```json
{
  "name": "com.dotnetg2p.core",
  "displayName": "DotNetG2P",
  "version": "0.1.0",
  "unity": "2021.2",
  "description": "Japanese Grapheme-to-Phoneme library. OpenJTalk-compatible rule-based G2P pipeline for Unity.",
  "keywords": ["g2p", "japanese", "tts", "phoneme"],
  "license": "Apache-2.0",
  "dependencies": {}
}
```

**注意**: UPMパッケージでは独自MeCabエンジン（`com.dotnetg2p.mecab`）を使用する。

### Assembly Definition構成

| asmdef | パス | 依存 | 用途 |
|--------|------|------|------|
| `DotNetG2P` | Runtime/ | なし | コアライブラリ |
| `DotNetG2P.MeCab` | MeCab/ | DotNetG2P | 独自MeCabエンジン（Apache-2.0） |
| `DotNetG2P.Editor` | Editor/ | DotNetG2P | Editor専用ツール |
| `DotNetG2P.Tests` | Tests/ | DotNetG2P | Unity Test Runner用 |

#### DotNetG2P.asmdef

```json
{
  "name": "DotNetG2P",
  "rootNamespace": "DotNetG2P",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "autoReferenced": true,
  "noEngineReferences": true,
  "versionDefines": []
}
```

**ポイント**: `noEngineReferences: true` により UnityEngine への依存を排除し、純粋C#ライブラリとして動作させる。

#### DotNetG2P.Editor.asmdef

```json
{
  "name": "DotNetG2P.Editor",
  "rootNamespace": "DotNetG2P.Editor",
  "references": ["DotNetG2P"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "autoReferenced": false
}
```

### 条件付きコンパイル

```csharp
// .NET Core用のファイルI/O（辞書読み込み）
#if DOTNETG2P_NETCORE
    // System.IO.File を直接使用
#else
    // Unity用: IDictionaryLoader抽象化経由
#endif
```

---

## 8. 辞書ファイルの配置・ロード戦略

### 辞書サイズ

| ファイル | サイズ | 内容 |
|---------|-------|------|
| sys.dic | ~72.8 MB | システム辞書（ダブル配列+トークン+フィーチャー） |
| matrix.bin | ~7.2 MB | 遷移コスト行列 |
| char.bin | ~0.3 MB | 文字カテゴリ定義 |
| unk.dic | ~0.2 MB | 未知語テンプレート |
| **合計** | **~80.5 MB** | |

### プラットフォーム別ロード戦略

#### NuGet (.NET Core / .NET)

```
DotNetG2P.Dictionary.NaistJdic (NuGetパッケージ)
└── contentFiles/
    └── any/netstandard2.1/
        └── naist-jdic/
            ├── sys.dic
            ├── matrix.bin
            ├── char.bin
            └── unk.dic
```

**または**: NuGetパッケージには辞書を同梱せず、初回起動時にダウンロードするヘルパーを提供:
```csharp
// 辞書パスを明示的に指定（推奨）
var tokenizer = new MeCabTokenizer("/path/to/naist-jdic");

// ヘルパーで辞書ダウンロード（オプション）
await DictionaryManager.EnsureDownloadedAsync("~/.dotnetg2p/naist-jdic");
```

#### Unity

```
Assets/
└── StreamingAssets/
    └── DotNetG2P/
        └── naist-jdic/
            ├── sys.dic
            ├── matrix.bin
            ├── char.bin
            └── unk.dic
```

- `StreamingAssets`はビルドに含まれ、ランタイムでファイルパスアクセス可能
- WebGLの場合は `UnityWebRequest` でのロードパスが必要（将来対応）
- **辞書パスの自動検出**: Editorで `Application.streamingAssetsPath + "/DotNetG2P/naist-jdic"` をデフォルトに

### 辞書ローダー抽象化

```csharp
namespace DotNetG2P.Internal
{
    /// <summary>辞書ファイルの読み込み抽象化（プラットフォーム差異を吸収）</summary>
    internal interface IDictionaryLoader
    {
        Stream OpenDictionary(string relativePath);
    }
}
```

---

## 9. テスト戦略

UniTaskと同様に、**.NET CoreテストとUnityテストの2系統**を並行運用:

### (A) .NET Coreテスト（CI向け）

```
tests/DotNetG2P.Tests/
├── DotNetG2P.Tests.csproj     # xUnit, FluentAssertions
├── NJD/                       # NJD処理の単体テスト
├── PhonemeConverter/           # 音素変換テスト
├── TextNormalization/          # テキスト正規化テスト
├── Models/                    # データモデルテスト
├── JPCommon/                  # JPCommonテスト
├── MeCab/                     # MeCabエンジンテスト
│   ├── MeCabTokenizerTests.cs      # 基本動作テスト
│   ├── TokenizerComparisonTests.cs # 出力一致テスト
│   ├── G2PComparisonTests.cs       # G2Pパイプライン比較テスト
│   ├── MeCabIndependentTests.cs    # 辞書非依存テスト
│   ├── PerformanceTests.cs         # パフォーマンステスト
│   ├── Utf8CharMapTests.cs         # UTF-8オフセット変換テスト
│   └── DictionaryErrorTests.cs     # エラーハンドリングテスト
├── Integration/               # 統合テスト
└── TestData/                  # テストデータ（pyopenjtalk出力との比較データ）
    ├── expected_phonemes.json
    └── expected_labels.json
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DotNetG2P.NetCore\DotNetG2P.NetCore.csproj" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
  </ItemGroup>
</Project>
```

### (B) Unityテスト（手動 + IL2CPP検証）

```
Assets/Tests/
├── DotNetG2P.Tests.asmdef     # defineConstraints: ["UNITY_INCLUDE_TESTS"]
├── NJDTests.cs
├── MoraMappingTests.cs
└── G2PEngineTests.cs
```

### テストデータの事前生成

pyopenjtalkの出力をテスト期待値として使用:

```python
# テストデータ生成スクリプト（Python側）
import pyopenjtalk
import json

test_cases = [
    "こんにちは",
    "今日は天気がいいですね",
    "東京都港区",
    "100円",
    "2024年3月15日",
    # ... 網羅的なテストケース
]

results = []
for text in test_cases:
    phonemes = pyopenjtalk.g2p(text)
    labels = pyopenjtalk.extract_fullcontext(text)
    results.append({"input": text, "phonemes": phonemes, "labels": labels})

with open("expected_output.json", "w") as f:
    json.dump(results, f, ensure_ascii=False, indent=2)
```

---

## 10. ビルド・CI/CD方針

UniTaskのCIパターンを参考にしたワークフロー:

### build-debug.yaml（PR/push時）

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-dotnet:
    runs-on: ubuntu-latest
    steps:
      - dotnet build
      - dotnet test
      - dotnet pack（検証のみ）

  build-unity:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        unity-version: ["2021.3", "2022.3", "6000.0"]
    steps:
      - Unity batch mode テスト
      - IL2CPPビルド検証（2022.3のみ）
      - metaファイル整合性チェック
```

### build-release.yaml（手動実行）

```yaml
on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version (e.g., 1.0.0)'
        required: true

jobs:
  update-version:
    - package.jsonのバージョン更新
    - コミット＆タグ

  build-dotnet:
    - dotnet build -c Release -p:Version=${{ inputs.version }}
    - dotnet test
    - dotnet pack -p:Version=${{ inputs.version }}

  build-unity:
    - .unitypackageエクスポート

  create-release:
    - GitHubリリース作成
    - NuGet push（DotNetG2P + DotNetG2P.MeCab）
    - .unitypackageをリリースアセットに添付
```

### バージョニング

- **SemVer**: `MAJOR.MINOR.PATCH`
- コードにバージョンをハードコーディングしない
- CIの `-p:Version` パラメータで注入
- package.jsonはCIが自動更新

---

## 11. AOT安全性チェックリスト

Unity IL2CPP/AOT環境での動作を保証するため、以下を遵守:

| 禁止事項 | 理由 | 代替手段 |
|---------|------|---------|
| `Reflection.Emit` | AOT非対応 | 静的コード生成 |
| `dynamic` キーワード | DLR非対応 | 明示的な型キャスト |
| `Expression.Compile()` | AOT非対応 | 直接メソッド呼び出し |
| `Activator.CreateInstance`（ジェネリック） | ストリップ対象 | ファクトリメソッド |
| `Type.MakeGenericType` | AOTジェネリック制限 | 具体型を事前定義 |

**注記**: DoubleArrayTrieの初期化でunsafeポインタ操作を使用（AllowUnsafeBlocks=true）。パフォーマンスクリティカルパスのみに限定し、IL2CPP環境でも動作確認済み。

### 推奨プラクティス

- データモデルは `struct` を活用（GCフレンドリー）
- `Span<T>` / `ReadOnlySpan<T>` で辞書パース時のアロケーション削減
- `ArrayPool<T>` でバッファ再利用
- 同期APIを基本とし、async版はオプション提供（WebGL対応）
- `CultureInfo.InvariantCulture` で文字列比較（環境依存回避）
