# C#/.NET 日本語G2Pライブラリ 実装計画

## Context

OpenJTalk/pyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難。
14エージェントによる調査の結果、**jpreprocess（Rust）の設計をベースにOpenJTalk互換のルールベースG2Pパイプラインをc#で再実装**するのが最適と判明。
NuGet/UPMパッケージとして商用配布する。

## 推奨アプローチ

- **コア**: ルールベース（OpenJTalk互換）
- **辞書**: naist-jdic（BSD、15フィールド拡張）
- **設計参考**: jpreprocess（Rust）の型安全設計パターン
- **形態素解析**: ITokenizer抽象化、独自MeCabエンジン（Apache-2.0）を使用
- **NJD処理**: jpreprocessのRustコードをベースにC#移植
- **出力形式**: 音素列、カタカナ、韻律記号付き、AccentPhrase構造体、フルコンテキストラベル

## 進捗状況

| フェーズ | 状態 | 備考 |
|---------|------|------|
| Phase 1: 基盤構築 | **完了** | 22ファイル、約2,758行。naist-jdic辞書での動作確認済み |
| Phase 2: NJD処理パイプライン | **完了** | NJD 7モジュール+TextNormalizer+G2POptions。約3,900行追加、全体約6,620行 |
| Phase 3: 出力形式・JPCommon | **完了** | 6ファイル新規、約1,465行追加。全310テスト成功 |
| Phase 4: テスト・品質保証 | **完了** | 502件の新規テスト追加（合計812件）。12ファイル、+4,855行 |
| Phase 5: パッケージング | **完了** | 12ファイル新規、+371行。NuGetパック・CI/CD・UPM・README/LICENSE |
| Phase 6: 独自MeCabエンジン | **完了** | 純C# MeCab互換エンジン実装。外部依存排除、Apache-2.0ライセンスで統一 |
| Phase 7: パフォーマンス最適化 | **完了** | 27ファイル変更。ValueStringBuilder, 辞書一括読み込み, バッファ再利用, バッチAPI等 |

## パッケージ構成

```
DotNetG2P.slnx
├── src/
│   ├── DotNetG2P.Core/           # コアライブラリ（.NET Standard 2.1）
│   │   ├── Models/               # データ構造
│   │   │   ├── POS.cs            # 品詞enum（ネスト構造）
│   │   │   ├── MoraKind.cs       # モーラenum（約150種）
│   │   │   ├── Mora.cs           # モーラ構造体
│   │   │   ├── Pronunciation.cs  # 発音（List<Mora> + AccentPosition）
│   │   │   ├── WordDetails.cs    # 単語詳細情報
│   │   │   ├── WordEntry.cs      # 辞書エントリ
│   │   │   ├── NjdNode.cs        # NJDノード
│   │   │   ├── AccentPhrase.cs   # アクセント句
│   │   │   └── Phoneme.cs        # 音素定義
│   │   ├── Tokenizer/            # 形態素解析抽象化
│   │   │   ├── ITokenizer.cs     # インターフェース
│   │   │   └── IToken.cs         # トークンインターフェース
│   │   ├── NJD/                  # NJD処理（6段階、順序厳守）
│   │   │   ├── SetPronunciation.cs
│   │   │   ├── DigitSequence.cs
│   │   │   ├── SetDigit.cs
│   │   │   ├── SetAccentPhrase.cs   # 18ルール
│   │   │   ├── SetAccentType.cs     # C1-C5, F1-F5, P系列
│   │   │   └── SetUnvoicedVowel.cs  # 6ルール
│   │   ├── JPCommon/             # フルコンテキストラベル生成
│   │   │   ├── Models.cs             # 6階層モデル（JPUtterance→...→JPPhoneme）
│   │   │   ├── JPCommonBuilder.cs    # NjdNode→JPUtterance階層構築
│   │   │   ├── FullContextLabel.cs   # HTSフルコンテキストラベル生成
│   │   │   └── WordAttr.cs           # POS/CType/CForm→ID変換テーブル
│   │   ├── TextNormalization/    # テキスト正規化
│   │   │   ├── TextNormalizer.cs
│   │   │   └── DigitRules.cs
│   │   ├── PhonemeConverter/     # 音素変換
│   │   │   ├── MoraMapping.cs           # カタカナ⇔音素（162種）
│   │   │   ├── AccentPhraseConverter.cs  # VOICEVOX互換AccentPhrase出力
│   │   │   └── ProsodyExtractor.cs      # ESPnet韻律記号付き出力
│   │   └── G2PEngine.cs          # メインAPI
│
├── tests/
│   └── DotNetG2P.Tests/
│       ├── NJD/                  # 各NJD処理の単体テスト
│       ├── PhonemeConverter/     # 音素変換テスト
│       └── Integration/         # 統合テスト（pyopenjtalk出力との比較）
│
└── samples/
    └── DotNetG2P.Console/        # コンソールサンプル
```

## 実装フェーズ

### Phase 1: 基盤構築 **[完了]**

1. ~~ソリューション・プロジェクト作成（.NET Standard 2.1、.slnx形式）~~ **完了**
2. ~~コアデータ構造の実装（POS, MoraKind, Mora, Pronunciation, WordDetails, WordEntry, NjdNode, AccentPhrase, Phoneme）~~ **完了**
3. ~~ITokenizer/ITokenインターフェース定義（15フィールド対応）~~ **完了**
4. ~~ITokenizer実装（naist-jdicの15フィールドパース）~~ **完了**
5. ~~MoraMapping（162種カタカナ⇔音素マッピング）~~ **完了**
6. ~~基本G2P: テキスト→形態素→カタカナ→音素列（ToPhonemes + ToKana）~~ **完了**
7. ~~SetPronunciation（最小版: 発音フォールバック処理）~~ **完了**
8. ~~コンソールサンプル（辞書あり/なし両モード対応）~~ **完了**

### Phase 2: NJD処理パイプライン **[完了]**

7. ~~TextNormalizer（全角/半角変換、濁点結合、278行）~~ **完了**
8. ~~SetPronunciation完全版（5段階処理、311行）~~ **完了**
9. ~~DigitSequence + DigitLut + SetDigit（数字読み変換、助数詞処理、合計2,006行）~~ **完了**
10. ~~SetAccentPhrase（アクセント句結合18ルール、237行）~~ **完了**
11. ~~SetAccentType（C1-C5, F1-F5, P系列アクセント結合、475行）~~ **完了**
12. ~~SetUnvoicedVowel（無声音化6ルール、389行）~~ **完了**
13. ~~NjdNode拡張（MergeFrom/Reset/ChainFlag 3値化、183行）~~ **完了**
14. ~~G2PEngine パイプライン統合 + G2POptions（222行）~~ **完了**

### Phase 3: 出力形式・JPCommon **[完了]**

13. ~~ToPhonemes() - 音素列出力（M1実装済み）~~ **完了**
14. ~~ToKana() - カタカナ出力（M1実装済み）~~ **完了**
15. ~~ToProsody() - 韻律記号付き出力（ProsodyExtractor.cs ~132行）~~ **完了**
16. ~~AccentPhrase/Mora構造体出力（AccentPhraseConverter.cs ~160行）~~ **完了**
17. ~~JPCommon実装（Models.cs ~208行, JPCommonBuilder.cs ~413行, WordAttr.cs）~~ **完了**
18. ~~ToFullContextLabels() - HTSフルコンテキストラベル出力（FullContextLabel.cs ~552行）~~ **完了**

### Phase 4: テスト・品質保証 **[完了]**

19. ~~NJD各処理の単体テスト（SetPronunciation 25件、SetAccentPhrase 37件、SetAccentType 39件、DigitSequence 14件、SetDigit 32件、DigitReading 25件）~~ **完了**
20. ~~MoraMapping全165パターン検証（166件）、piper-plusテスト移植（87件）~~ **完了**
21. ~~pyopenjtalk出力との比較テスト（20件、テストデータ生成スクリプト付き）~~ **完了**
22. ~~エッジケース対応（記号、英字、長文、空文字列、混在スクリプト、~57件）~~ **完了**

### Phase 5: パッケージング **[完了]**

23. ~~Directory.Build.props + Core csproj NuGet設定（IsPackable, PackageId, Description, License）~~ **完了**
24. ~~UPMパッケージ構造（package.json, DotNetG2P.asmdef）~~ **完了**
25. ~~GitHub Actions CI/CD（ci.yml: ビルド・テスト・パック、release.yml: NuGet push + GitHub Release）~~ **完了**
26. ~~LICENSE（Apache-2.0）、README.md、.editorconfig、.gitattributes~~ **完了**

### Phase 6: 独自MeCabエンジン **[完了]**

26. ~~DotNetG2P.MeCab.csproj 作成（netstandard2.1、Apache-2.0）~~ **完了**
27. ~~辞書読み込み層（DictionaryHeader, DicToken, SystemDictionary, ConnectionMatrix, CharProperty, UnknownDictionary, DictionaryBundle）~~ **完了**
28. ~~DoubleArrayTrie（Darts-clone互換）+ Utf8CharMap~~ **完了**
29. ~~LatticeBuilder + ViterbiDecoder~~ **完了**
30. ~~MeCabTokenizer（ITokenizer実装）~~ **完了**
31. ~~テスト作成・検証（MeCabTokenizerTests, TokenizerComparisonTests, G2PComparisonTests, MeCabIndependentTests, PerformanceTests, Utf8CharMapTests, DictionaryErrorTests）~~ **完了**
32. ~~UPM/NuGetパッケージング、CI/CD更新~~ **完了**
33. ~~10専門家レビュー + 16件の修正（エンディアン、ViterbiDecoder自己参照バグ等）~~ **完了**

### Phase 7: パフォーマンス最適化 **[完了]**

34. ~~基盤整備: ValueStringBuilder (ref struct), ThrowHelper, AllowUnsafeBlocks~~ **完了**
35. ~~MeCab辞書高速化: Buffer.BlockCopy/MemoryMarshal.Read/AggressiveInlining~~ **完了**
36. ~~LatticeBuilder/Utf8CharMap最適化: バッファ再利用, ArrayPool, stackalloc~~ **完了**
37. ~~ViterbiDecoder/MeCabTokenizer最適化: foreach→for, Lazy<T>, 遅延パーサ~~ **完了**
38. ~~Core出力系: StringBuilder→ValueStringBuilder (5ファイル)~~ **完了**
39. ~~NJD/enum/TextNormalizer: enum:byte/ushort, Regex→手動パーサ, Dictionary→配列~~ **完了**
40. ~~追加最適化: LatticeNode lazy Surface, List初期容量, WeakReference, バッチAPI, string.Intern~~ **完了**
41. ~~10エージェントレビュー + ポストレビュー修正3件~~ **完了**

## 主要参考資料

| 資料 | 用途 |
|------|------|
| jpreprocess (Rust) `crates/jpreprocess-core/` | データ構造、MoraEnum定義 |
| jpreprocess `crates/jpreprocess-njd/` | NJD処理6段階の実装ロジック |
| jpreprocess `crates/jpreprocess-jpcommon/` | フルコンテキストラベル生成 |
| OpenJTalk (C) `njd/` | NJD処理のオリジナルルールテーブル |
| VOICEVOX `mora_mapping.py` | 247種モーラ⇔音素マッピング（BSD） |
| ESPnet `text/japanese.py` | 韻律記号抽出アルゴリズム |
| docs/research/14_naist_jdic_format.md | 辞書フォーマット仕様 |

## 検証方法

1. **単体テスト**: 各NJD処理モジュールにjpreprocessのテストケースを移植して検証
2. **統合テスト**: pyopenjtalkのg2p()出力と比較（Python側でテストデータを事前生成）
3. **コンソールサンプル**: 対話型G2P変換で手動検証
4. **Unity検証**: IL2CPPビルドでの動作確認
