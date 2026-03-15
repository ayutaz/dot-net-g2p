# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- ルートドキュメントとして `CONTRIBUTING.md` と `MIGRATION.md` を追加
- `DotNetG2P` / `DotNetG2P.MeCab` / `DotNetG2P.English` / `DotNetG2P.Chinese` / `DotNetG2P.Spanish` / `DotNetG2P.French` / `DotNetG2P.Portuguese` 向けのパッケージ別 README を追加
- Dependabot 設定 (`.github/dependabot.yml`) を追加し、NuGet / .NET SDK / GitHub Actions の更新監視を有効化
- `ARCHITECTURE.md`、DocFX 設定、SBOM 生成用のローカル tool manifest を追加
- Japanese / Multilingual / Romance 言語群の BenchmarkDotNet シナリオを追加
- `samples/DotNetG2P.Console` を多言語デモへ拡張

### Changed
- バッチAPI実装を共通 helper ベースへ整理しつつ、公開 `IReadOnlyList<T>` API のランタイム互換性を維持
- Core と Multilingual の batch contract テストを追加し、`null` / 空入力 / mixed input / Dispose 後動作の回帰を補強
- CI を `ubuntu-latest` / `windows-latest` / `macos-latest` と `.NET 8` / `.NET 9` の matrix に拡張
- PR でテスト結果公開、coverage summary/comment、Cobertura/HTML artifact、NuGet pack 検証を自動化
- `Directory.Build.props` に deterministic build / CI build 設定を追加し、pack 時の package README 解決を共通化
- ルート README と翻訳 README を関連ドキュメント、CI/.NET 8/9 ビルド要件、最新のパッケージ導線に合わせて更新
- `setup-dictionary` action の cache を `actions/cache@v5` に更新
- CI / Release workflow に DocFX build と CycloneDX SBOM 生成を追加
- `Multilingual` の言語ルーティングを capability-based internal adapter 経由へ整理し、共通 fixture から検証できるようにした

## [1.4.0] - 2026-03-12

### Added
- **フランス語G2Pパッケージ `DotNetG2P.French` 新規追加**
  - DotNetG2P.Coreに依存しない独立パッケージ（.NET Standard 2.1）
  - `FrenchG2PEngine`、`FrenchG2POptions`、例外辞書、Metropolitan/Conservative方言、IPA/X-SAMPA出力を追加
  - 数値/日付/時刻/通貨/単位/略語/記号の正規化と全量コーパス評価ツールを追加
- **ポルトガル語G2Pパッケージ `DotNetG2P.Portuguese` 新規追加**
  - DotNetG2P.Coreに依存しない独立パッケージ（.NET Standard 2.1）
  - `PortugueseG2PEngine`、`PortugueseG2POptions`、例外辞書、Brazilian/European方言、IPA/X-SAMPA出力を追加
  - 7種の異音規則、13段階の正規化、全量コーパス評価ツールを追加
- **韓国語G2Pパッケージ `DotNetG2P.Korean` 新規追加**
  - Hangul-first の規則ベース変換、Jamo分解、例外辞書、軽量正規化、`KoreanUiVariationMode` を追加
  - `g2pk_parity` / `official_gold` / `weak_rules` benchmark harness、external corpus gate、performance test を追加
- **多言語統合パッケージ `DotNetG2P.Multilingual` 拡張**
  - `Language` / `DefaultLatinLanguage` / `KoreanOptions` を拡張し、韓国語・フランス語・ポルトガル語を統合
  - Hangul直接ルーティング、ポルトガル語固有文字・接尾辞パターンによるラテン文字判定を追加
- Unity UPM パッケージの install matrix / metadata / workflow coverage を検証する packaging test を追加

### Changed
- UPMパッケージバージョンを1.4.0に更新
- GitHub Actions の `ci.yml` / `release.yml` の pack 対象に `DotNetG2P.Korean` と `DotNetG2P.Portuguese` を追加
- Unity UPM package installability 修正とメタファイル整合性検証を強化

## [1.3.0] - 2026-03-10

### Added
- **中国語G2Pパッケージ `DotNetG2P.Chinese` 新規追加**
  - DotNetG2P.Coreに依存しない独立パッケージ（.NET Standard 2.1）
- 基本ピンイン変換MVP
  - pinyin-data 44,435エントリの単字辞書をEmbeddedResourceとして埋め込み
  - phrase-pinyin-data 411,958エントリのフレーズ辞書をEmbeddedResourceとして埋め込み
  - `ChineseG2PEngine` クラス: `ToPinyin()`, `ToPinyinList()`, `LookupChar()` 等
  - 3種の出力スタイル（ToneMarked/ToneNumber/Normal）、`PinyinStyle` enum
  - UPMパッケージ設定（com.dotnetg2p.chinese）
- フレーズ辞書と多音字解決
  - `PinyinPhraseDictionary`（411,958エントリ、最長一致検索）
  - 多音字の文脈依存読み分け（pypinyin方式フレーズルックアップ）
  - 非漢字処理（CJK/ASCII句読点は区切り、英数字はパススルー）
- 声調変調
  - `ToneSandhiProcessor`（三声連読変調、"一"変調、"不"変調）
  - `ChineseG2PEngine` 3段階パイプライン（収集→声調変調→スタイル変換）
- IPA/注音変換・バッチAPI
  - IPA変換（`PinyinToIpa`）、注音変換（`PinyinToZhuyin`）
  - バッチAPI 11メソッド追加
- テスト充実
  - エッジケーステスト61件、パフォーマンステスト15件、精度テスト78件追加
- Multilingual中国語統合
  - `Language.Chinese` 追加、`ScriptKind.CJKIdeograph` 追加
  - `MultilingualG2POptions.DefaultCjkLanguage` 追加
  - 日中英混在テキスト対応（MultilingualG2PEngine中国語統合）
  - Multilingualテスト43件追加
- 中国語G2Pテスト合計約936件追加
- **スペイン語G2Pパッケージ `DotNetG2P.Spanish` 新規追加**
  - DotNetG2P.Coreに依存しない独立パッケージ（.NET Standard 2.1）
- コアルールエンジン + 基本G2P MVP
  - `SpanishG2PEngine` メインAPI、`SpanishG2POptions`（Dialect, IncludeStress, EnableAllophones, Separator）
  - `GraphemeToPhonemeRules`（ダイグラフ→文脈依存→単純対応の3フェーズ）
  - `SpanishSyllabifier`（音節分割、onset maximization）
  - `StressAssigner`（ストレス位置決定、アクセント記号 or デフォルトルール）
  - IPA出力: `ToIPA()`, `ToPhonemes()`, `ToPhonemeList()`, `ToSyllables()`、バッチAPI
  - UPMパッケージ設定（com.dotnetg2p.spanish）
- 精度向上・異音規則・テキスト正規化
  - `SpanishNormalizer`（数値・日付・時刻・単位・略語・記号展開）
  - `AllophoneProcessor`（β,ð,ɣ弱化、鼻音同化）
  - 方言対応: Castilian (distincion) / LatinAmerican (seseo)
  - 例外辞書運用（`spanish_exceptions.master.tsv`）
- X-SAMPA・大規模精度評価
  - `XSampaConverter`、`ToXSampa()`, `ToXSampaWithoutStress()`, `ToXSampaBatch()` 追加
  - ipa-dict PER 1.69% (base) / 1.37% (allophones)
  - WikiPron PER 1.38-1.43%
  - 精度評価ツール（`tools/run_spanish_full_evaluation.ps1`）
- Multilingual統合
  - `Language.Spanish` 追加、`MultilingualG2POptions.DefaultLatinLanguage` 追加
  - `TextSegmenter` 英語/スペイン語ラテン文字振り分け対応
  - ASCII Spanish高頻度語・接尾辞・`gue/gui` 判定
  - 埋め込み中国語辞書共有キャッシュによる辞書二重ロード解消
  - `MultilingualG2PEngine` にスペイン語G2P統合
  - `MultilingualSpanishTests` / `MultilingualMixedLanguageTests` 追加
- スペイン語G2Pテスト合計約355件追加
- **多言語統合パッケージ `DotNetG2P.Multilingual` 拡張**
  - `Language` enum拡張（Japanese/English/Chinese/Spanish の4言語対応）
  - `TextSegmenter` 拡張（CJK marker判定、埋め込み中国語辞書・日本語語彙ヒントによる純漢字run判定）
  - 重い統合テストのshared fixture化
  - Multilingualテスト合計341件通過、代表回帰110件通過、パフォーマンス8件通過
- CI/CD更新: DotNetG2P.Chinese・DotNetG2P.Spanishのpackステップ追加

### Changed
- プロジェクト全体テスト数: 約3,469件

## [1.2.0] - 2026-03-07

### Added
- **英語G2Pパッケージ `DotNetG2P.English` 新規追加**
  - DotNetG2P.Coreに依存しない独立パッケージ（.NET Standard 2.1）
- CMU辞書ルックアップMVP
  - CMU Pronouncing Dictionary（135,166エントリ）をEmbeddedResourceとして埋め込み
  - `EnglishG2PEngine` クラス: `ToPhonemes()`, `ToPhonemeList()`, `LookupWord()`, `LookupAllPronunciations()`, `ContainsWord()`
  - ARPAbet音素体系（39音素 + ストレス4段階）、`ArpabetPhoneme` enum (byte基底)
  - `EnglishG2POptions`: `IncludeStress`, `UnknownWordHandling`, `EnableLts`
  - UPMパッケージ設定（com.dotnetg2p.english）
  - NOTICEファイルにCMU辞書ライセンス表記追加
- Flite LTS CARTツリーによるOOV音素推定
  - Fliteプロジェクトから25,505ノードのCARTツリーを移植
  - OOV（辞書未登録語）に対するLTSフォールバック（PER 5.26%）
  - `tools/extract_lts.js` 抽出スクリプト（Fliteソース→バイナリ+C#自動生成）
  - NOTICEファイルにFliteライセンス表記追加
- テキスト正規化
  - `EnglishNormalizer` ファサード + 6サブモジュール
  - 数字→英語読み（基数・序数・小数・負数）、通貨展開（$, £, €, ¥）
  - 時刻展開、略語展開（Dr./Mr./Mrs.等）、頭字語判別（NASA vs API）
  - 記号→名前変換（@→at, #→number等）
  - `EnglishG2POptions.EnableNormalization` 追加
- 同綴異音語解決
  - `HomographResolver`（PosGuesser + HomographDatabase）による品詞ルールベース判別
  - 30+語の同綴異音語データベース（母音変化型・ストレス移動型・-ate語尾型）
  - 軽量品詞推定: 接尾辞ルール（-ing→動詞, -tion→名詞等）+ 文脈ルール（冠詞後→名詞等）
  - `EnglishG2POptions.EnableHomographResolution` 追加
- 英語G2Pテスト511件追加
- **多言語G2Pパッケージ `DotNetG2P.Multilingual` 新規追加**
  - DotNetG2P.Core + DotNetG2P.MeCab + DotNetG2P.English依存（.NET Standard 2.1）
  - `LanguageDetector`（Unicode文字種ベース言語判定）
  - `TextSegmenter`（2パスセグメント分割）
  - `MultilingualG2PEngine`（日英G2Pファサード、IDisposable、lock保護）
  - UPMパッケージ設定（com.dotnetg2p.multilingual）
  - テスト162件追加
- CI/CD更新: DotNetG2P.English・DotNetG2P.Multilingualのpackステップ追加

### Changed
- `Directory.Build.props` に `PackageLicenseExpression` を集約
- プロジェクト全体テスト数: 2,318件

## [1.1.0] - 2026-03-05

### Added
- ToProsodyFeatures() / ToProsodyFeaturesBatch() API追加
  - 韻律特徴量（A1/A2/A3）を音素単位で直接取得可能に
  - ProsodyFeaturesクラス新規追加（Phonemes, A1, A2, A3プロパティ）
- スレッドセーフティのドキュメント整備
  - G2PEngine・MeCabTokenizer・DictionaryBundleにXMLドキュメントコメント追加
  - README 3言語版にスレッドセーフティセクション追記
- README多言語化（英語版・中国語版を追加）

### Changed
- FullContextLabel内部メソッドをinternalに変更（ExtractProsodyFeaturesで再利用）
- DotNetG2P.Core.csprojにInternalsVisibleTo追加（テストプロジェクト用）
- UPMパッケージバージョンを1.1.0に更新

## [1.0.0] - 2026-03-03

### Added
- NJD処理パイプライン6段階すべて実装
  - TextNormalizer, SetPronunciation, DigitSequence/SetDigit, SetAccentPhrase, SetAccentType, SetUnvoicedVowel
- 5種類の出力形式
  - ToProsody()（ESPnet韻律記号付き出力）
  - ToAccentPhrases()（VOICEVOX互換アクセント句）
  - ToFullContextLabels()（HTSフルコンテキストラベル）
  - JPCommon階層モデル（JPUtterance→JPBreathGroup→JPAccentPhrase→JPWord→JPMora→JPPhoneme）
- 1,600超テストで品質保証
  - pyopenjtalk比較テスト、piper-plus移植テスト、NJD単体テスト
- NuGet/UPMパッケージング
  - GitHub Actions CI/CD（ci.yml, release.yml）
  - UPMパッケージ構造（package.json, asmdef）
- 独自MeCabエンジン DotNetG2P.MeCab
  - 純C#でMeCab互換形態素解析エンジンを実装
  - DoubleArrayTrie + Viterbiデコーダ + 未知語処理
  - 外部依存を排除しApache-2.0ライセンスで統一
- パフォーマンス最適化
  - ValueStringBuilderによるゼロアロケーション文字列構築
  - MeCab辞書一括読み込み（Buffer.BlockCopy/MemoryMarshal.Read）
  - DoubleArrayTrie unsafeポインタ高速化
  - LatticeBuilderバッファ再利用 + CharInfoプリキャッシュ
  - MeCabToken遅延パーサ（Split廃止）
  - enum基底型最適化（Consonant:byte, Vowel:byte, MoraKind:ushort）
  - SetAccentType Regex→手動パーサ
  - DictionaryBundle WeakReferenceキャッシュ
  - バッチ処理API（ToPhonemesBatch, ToKanaBatch, ToProsodyBatch, ToFullContextLabelsBatch）
- G2POptionsによる各処理段階のON/OFF制御
- Analyze APIでNJDノード列を取得可能
- ITokenizer抽象化で形態素解析エンジン差し替え可能

### Changed
- ライセンスをApache-2.0に統一
- 形態素解析を独自MeCabエンジンに移行

### Removed
- LibNMeCab依存を削除

[Unreleased]: https://github.com/ayutaz/dot-net-g2p/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/ayutaz/dot-net-g2p/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/ayutaz/dot-net-g2p/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/ayutaz/dot-net-g2p/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/ayutaz/dot-net-g2p/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/ayutaz/dot-net-g2p/releases/tag/v1.0.0
