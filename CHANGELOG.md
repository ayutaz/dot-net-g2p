# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.1.0]: https://github.com/ayutaz/dot-net-g2p/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/ayutaz/dot-net-g2p/releases/tag/v1.0.0
