# C#/.NET 日本語G2Pライブラリ 実装計画

## Context

OpenJTalk/pyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難。
14エージェントによる調査の結果、**jpreprocess（Rust）の設計をベースにOpenJTalk互換のルールベースG2Pパイプラインをc#で再実装**するのが最適と判明。
NuGet/UPMパッケージとして商用配布する。

## 推奨アプローチ

- **コア**: ルールベース（OpenJTalk互換）
- **辞書**: naist-jdic（BSD、15フィールド拡張）
- **設計参考**: jpreprocess（Rust）の型安全設計パターン
- **形態素解析**: ITokenizer抽象化 → 初期はNMeCab依存、将来BSD自前実装に差し替え
- **NJD処理**: jpreprocessのRustコードをベースにC#移植
- **出力形式**: 音素列、カタカナ、韻律記号付き、AccentPhrase構造体、フルコンテキストラベル

## 進捗状況

| フェーズ | 状態 | 備考 |
|---------|------|------|
| Phase 1: 基盤構築 | **完了** | 22ファイル、約2,758行。naist-jdic辞書での動作確認済み |
| Phase 2: NJD処理パイプライン | 未着手 | - |
| Phase 3: 出力形式・JPCommon | 未着手 | - |
| Phase 4: テスト・品質保証 | 未着手 | - |
| Phase 5: パッケージング | 未着手 | - |
| Phase 6: 独自MeCabエンジン | 未着手 | - |

## パッケージ構成

```
DotNetG2P.slnxx
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
│   │   │   ├── Utterance.cs
│   │   │   ├── BreathGroup.cs
│   │   │   └── FullContextLabel.cs
│   │   ├── TextNormalization/    # テキスト正規化
│   │   │   ├── TextNormalizer.cs
│   │   │   └── DigitRules.cs
│   │   ├── PhonemeConverter/     # 音素変換
│   │   │   ├── MoraMapping.cs    # カタカナ⇔音素（247種）
│   │   │   └── ProsodyExtractor.cs
│   │   └── G2PEngine.cs          # メインAPI
│   │
│   └── DotNetG2P.NMeCab/         # NMeCabアダプター（LGPL依存、将来差し替え）
│       └── NMeCabTokenizer.cs    # ITokenizer実装
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
4. ~~NMeCabアダプター実装（LibNMeCab 0.10.2、naist-jdicの15フィールドパース）~~ **完了**
5. ~~MoraMapping（162種カタカナ⇔音素マッピング）~~ **完了**
6. ~~基本G2P: テキスト→形態素→カタカナ→音素列（ToPhonemes + ToKana）~~ **完了**
7. ~~SetPronunciation（最小版: 発音フォールバック処理）~~ **完了**
8. ~~コンソールサンプル（辞書あり/なし両モード対応）~~ **完了**

### Phase 2: NJD処理パイプライン

7. TextNormalizer（全角/半角変換、濁点結合）
8. SetPronunciation（発音生成）
9. DigitSequence + SetDigit（数字読み変換、助数詞処理）
10. SetAccentPhrase（アクセント句結合18ルール）
11. SetAccentType（C1-C5, F1-F5, P系列アクセント結合）
12. SetUnvoicedVowel（無声音化6ルール）

### Phase 3: 出力形式・JPCommon

13. ToPhonemes() - 音素列出力
14. ToKana() - カタカナ出力
15. ToProsody() - 韻律記号付き出力（ESPnet prosody方式）
16. AccentPhrase/Mora構造体出力（VOICEVOX互換）
17. JPCommon実装（Utterance→BreathGroup→AccentPhrase階層）
18. ToFullContextLabels() - HTSフルコンテキストラベル出力

### Phase 4: テスト・品質保証

19. jpreprocessのテストケース移植
20. pyopenjtalk出力との比較テスト
21. エッジケース対応（記号、英字、長文、空文字列）

### Phase 5: パッケージング

22. NuGetパッケージ設定
23. UPM（Unity Package Manager）パッケージ構成
24. naist-jdic辞書のバンドル戦略（StreamingAssets対応）
25. README・APIドキュメント

### (将来) Phase 6: 独自MeCabエンジン

26. ダブル配列Trie実装
27. ラティス構築 + ビタビデコーディング
28. MeCabバイナリ辞書読み込み
29. 未知語処理
30. NMeCab依存の完全排除 → 完全BSDライセンス化

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
