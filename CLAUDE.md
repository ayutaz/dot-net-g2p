# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

C#/.NET（Unity対応）向けの日英中西仏多言語G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk互換の日本語G2Pパイプライン、CMU辞書ベースの英語G2P、pinyin-data辞書ベースの中国語ピンイン変換、ルールベースのスペイン語G2P、ルールベース+例外辞書のフランス語G2PをC#でネイティブに再実装し、Pythonやネイティブバイナリへの依存を排除する。

## 進捗状況

- **M1（最小動作プロトタイプ）**: 完了
  - `g2p("こんにちは")` → `"k o N n i ch i w a"` が動作確認済み
  - naist-jdic辞書によるフルパイプライン（形態素解析→NJD→音素変換）が動作
- **M2（NJD処理パイプライン完成）**: 完了
  - NJDパイプライン6段階すべて実装（TextNormalizer→SetPronunciation→DigitSequence/SetDigit→SetAccentPhrase→SetAccentType→SetUnvoicedVowel）
  - 無声音化（`s U k i`）、アクセント句結合、数字読み変換が動作
  - G2POptionsによる各処理段階のON/OFF制御、Analyze APIを追加
- **M3（出力形式の充実）**: 完了
  - ToProsody()（ESPnet韻律記号付き出力）、ToAccentPhrases()（VOICEVOX互換）、ToFullContextLabels()（HTSフルコンテキストラベル）を追加
  - JPCommon階層モデル（JPUtterance→JPBreathGroup→JPAccentPhrase→JPWord→JPMora→JPPhoneme）を実装
  - WordAttr（POS/CType/CForm→ID変換テーブル、jpreprocess word_attr.rs準拠）を実装
- **M4（テスト・品質保証）**: 完了
  - 502件の新規テストを追加（合計950超テスト）
  - NJD各処理の単体テスト（SetPronunciation/SetAccentPhrase/SetAccentType/DigitSequence/SetDigit）
  - MoraMapping全165パターン検証、piper-plusテスト移植（87件）、pyopenjtalk比較テスト（20件）
  - エッジケーステスト（記号/英字/空文字列/長文/混在スクリプト）
- **M5（パッケージング）**: 完了
  - NuGetパッケージ設定（Directory.Build.props、Core csproj更新、`dotnet pack`で.nupkg生成確認済み）
  - GitHub Actions CI/CD（ci.yml: push/PR時ビルド・テスト・パック、release.yml: NuGet push + GitHub Release）
  - UPMパッケージ構造（package.json、DotNetG2P.asmdef）
  - LICENSE（Apache-2.0）、README.md、.editorconfig、.gitattributes
- **M6（独自MeCabエンジン）**: 完了
  - 純C#でMeCab互換形態素解析エンジンを実装（`DotNetG2P.MeCab`パッケージ）
  - 外部依存を排除しApache-2.0ライセンスで統一
  - DoubleArrayTrie + Viterbiデコーダ + 未知語処理の完全実装
  - 100+文で全15フィールド出力一致を検証済み
  - NuGet (`DotNetG2P.MeCab`) + UPM (`com.dotnetg2p.mecab`) パッケージ対応
- **M7（パフォーマンス最適化）**: 完了
  - ValueStringBuilder/ThrowHelper基盤整備、AllowUnsafeBlocks有効化
  - MeCab辞書一括読み込み（Buffer.BlockCopy/MemoryMarshal.Read）、AggressiveInlining
  - DoubleArrayTrie unsafeポインタ高速化、LatticeBuilderバッファ再利用
  - MeCabToken遅延パーサ（Split廃止）、string.Intern()頻出文字列共有
  - StringBuilder→ValueStringBuilder（FullContextLabel/G2PEngine/ProsodyExtractor/MoraMapping/Pronunciation）
  - enum基底型最適化（Consonant:byte, Vowel:byte, MoraKind:ushort）
  - Regex→手動パーサ（SetAccentType）、Dictionary→配列インデックス（TextNormalizer）
  - DictionaryBundle WeakReferenceキャッシュ + スレッドセーフDispose
  - バッチ処理API追加（ToPhonemesBatch等5メソッド）
  - 10エージェントレビュー + ポストレビュー修正完了
- **中国語G2P (DotNetG2P.Chinese)**: C6完了（feature/chinese-g2p ブランチ）
  - **C1（基本ピンイン変換MVP）**: 完了
    - pinyin-data 44,435エントリの単字辞書 + phrase-pinyin-data 411,958エントリのフレーズ辞書を埋め込み
    - ChineseG2PEngine メインAPI、3種の出力スタイル（ToneMarked/ToneNumber/Normal）
    - PinyinCharDictionary/PinyinPhraseDictionary、PinyinParser、ToneConverter
    - テスト261件
  - **C2（フレーズ辞書と多音字解決）**: 完了
    - PinyinPhraseDictionary（411,958エントリ、最長一致検索）
    - 多音字の文脈依存読み分け（pypinyin方式フレーズルックアップ）
    - 非漢字処理（CJK/ASCII句読点は区切り、英数字はパススルー）
    - テスト155件追加
  - **C3（声調変調）**: 完了
    - ToneSandhiProcessor（三声連読変調、"一"変調、"不"変調）
    - ChineseG2PEngine 3段階パイプライン（収集→声調変調→スタイル変換）
    - テスト72件追加（合計494件）
  - **C4（出力形式拡張）**: 完了
    - IPA（国際音声記号）変換 PinyinToIpa（声母22種+韻母36種の完全マッピング、声調マーカー対応）
    - 注音符号（ボポモフォ）変換 PinyinToZhuyin（声母21種+全韻母マッピング、声調記号対応）
    - ChineseG2PEngine API拡張: ToIPA(), ToZhuyin(), バッチAPI 9メソッド追加
    - テスト288件追加（IPA 125件 + Zhuyin 112件 + C4統合 51件）
  - **C5（テスト・品質保証）**: 完了
    - エッジケーステスト（61件）: 空/null/特殊文字/句読点/長文/混在テキスト/辞書境界/声調変調/オプション/Dispose
    - パフォーマンステスト（15件）: スループット/バッチ比較/辞書初期化/メモリ/フレーズ辞書/声調変調/スタイル変換
    - 精度・回帰テスト（78件）: 高頻度多音字20語/声調変調正確性/一般フレーズ/スタイル一貫性/回帰
  - **C6（多言語統合・パッケージング）**: 完了
    - DotNetG2P.Multilingual に中国語G2P統合（Language.Chinese、ScriptKind.CJKIdeograph、DefaultCjkLanguage オプション）
    - LanguageDetector/TextSegmenter のCJK漢字分離判定（ひらがな/カタカナ近接→日本語、それ以外→DefaultCjkLanguage）
    - Multilingualテスト43件追加
- **英語G2P (DotNetG2P.English)**: E6完了（feature/english-g2p ブランチ）
  - **E1（CMU辞書ルックアップMVP）**: 完了
    - 135,166エントリのCMU辞書埋め込み、`EnglishG2PEngine` メインAPI、ARPAbet音素体系（39音素）
    - テスト約214件
  - **E2（Flite LTS CARTツリー）**: 完了
    - 25,505ノードのCARTツリーによるOOV音素推定、PER 5.26%（espeak-ng 6.92%を上回る）
    - `LtsEngine` スレッドセーフ遅延初期化、`tools/extract_lts.js` 抽出ツール
  - **E3（テキスト正規化）**: 完了
    - `EnglishNormalizer` + 6サブモジュール（NumberToWords, CurrencyExpander, TimeExpander, AbbreviationExpander, AcronymDetector, SymbolExpander）
    - 数字・通貨・時刻・略語・頭字語・記号の英語読み展開、テスト143件
  - **E4（同綴異音語解決）**: 完了
    - `HomographResolver`（PosGuesser + HomographDatabase）による品詞ルールベース判別
    - 30+語の同綴異音語データベース（母音変化型・ストレス移動型・-ate語尾型）、テスト154件
  - **E5（IPA/X-SAMPA出力・テスト充実）**: 完了
    - IPA/X-SAMPA変換、バッチAPI（8メソッド追加）、エッジケース・パフォーマンス・精度テスト
    - テスト197件追加
  - **E6（日英混在テキスト対応）**: 完了
    - `DotNetG2P.Multilingual` パッケージ新規作成（Core + MeCab + English + Chinese依存）
    - LanguageDetector（Unicode文字種ベース言語判定）、TextSegmenter（2パスセグメント分割）
    - MultilingualG2PEngine（日英中G2Pファサード、IDisposable、lock保護）
    - テスト162件追加
- **スペイン語G2P (DotNetG2P.Spanish)**: S4実装済み（feature/spanish-g2p ブランチ）
  - **S1（コアルールエンジン + 基本G2P MVP）**: 完了
    - プロジェクト構成（csproj, package.json, asmdef, slnx更新）
    - モデル定義（SpanishIpaPhoneme enum, SpanishPhoneme struct, Dialect enum）
    - SpanishG2PEngine（sealed class, IDisposable）、SpanishG2POptions
    - GraphemeToPhonemeRules（ダイグラフ→文脈依存→単純対応の3フェーズ）
    - SpanishSyllabifier（音節分割、onset maximization）
    - StressAssigner（ストレス位置決定、アクセント記号 or デフォルトルール）
    - IPA出力: ToIPA(), ToPhonemes(), ToPhonemeList(), ToSyllables(), バッチAPI
    - テスト実装済み
  - **S2（精度向上・異音規則・テキスト正規化）**: 完了
    - `SpanishNormalizer` をカテゴリ別展開へ整理し、日付・時刻・単位・略語・記号を拡張
    - 桁区切りと小数点の解釈分離、不正な日付/時刻の安全なフォールバックを追加
    - `NumberToWords` に文脈依存数詞（`un/uno`, `una`, `veintiún/veintiuna`）を追加
    - `SpanishAllophoneFeatures` により必須規則と可変規則を切替可能
    - `spanish_exceptions.master.tsv` + `tools/generate_spanish_exceptions.ps1` による例外辞書運用へ移行
    - SpanishG2P テスト 227件通過
  - **S3（X-SAMPA・大規模精度評価・拡張テスト）**: 完了
    - XSampaConverter、`ToXSampa()`, `ToXSampaWithoutStress()`, `ToXSampaBatch()` を追加
    - SpanishXSampaTests / SpanishEdgeCaseTests / SpanishPerformanceTests / SpanishAccuracyTests を追加
    - `ipa-dict / WikiPron` サンプルコーパスと PER 回帰テストを追加
    - `tools/refresh_spanish_eval_data.ps1` + `tools/DotNetG2P.SpanishEval` + `tools/run_spanish_full_evaluation.ps1` により全量 PER / WER / カテゴリ別集計を追加
    - 2026-03-09 実測:
      - `ipa_dict_es_es_full/base` PER `1.69%`, `allophones` PER `1.37%`
      - `ipa_dict_es_mx_full/base` PER `1.69%`, `allophones` PER `1.37%`
      - `wikipron_ca_full/base` PER `1.38%`
      - `wikipron_la_full/base` PER `1.43%`
    - SpanishG2P テスト 227件通過
  - **S4（Multilingual統合・パッケージング拡張）**: 完了
    - `DotNetG2P.Multilingual` に `Language.Spanish` と `DefaultLatinLanguage` を追加
    - `TextSegmenter` を英語/スペイン語のラテン文字振り分けに対応
    - `TextSegmenter` を補強し、ASCII Spanish 高頻度語・接尾辞・`güe/güi`、standalone neutral token、CJK marker ベース判定、埋め込み中国語 phrase/char 辞書と日本語語彙ヒントによる純漢字run判定を追加
    - `TextSegmenter` と `ChineseG2PEngine` の埋め込み中国語辞書共有キャッシュを追加し、純CJK判定時の辞書二重ロードを解消
    - `MultilingualG2PEngine` に `SpanishG2PEngine` を統合
    - `MultilingualSpanishTests` / `MultilingualMixedLanguageTests` を追加し、重い Multilingual 統合テストは shared fixture 化
    - Multilingual テスト 341件通過、代表 Multilingual 回帰 110件通過、Multilingual performance テスト 8件通過
- **フランス語G2P (DotNetG2P.French)**: F4完了（feature/french-g2p ブランチ）
  - **F1（コアG2Pルールエンジン + 基本MVP）**: 完了
    - プロジェクト構成（csproj, package.json, asmdef, slnx更新）
    - モデル定義（FrenchIpaPhoneme enum 40種, FrenchPhoneme struct, FrenchDialect enum）
    - FrenchG2PEngine（sealed class, IDisposable）、FrenchG2POptions
    - GraphemeToPhonemeRules（6フェーズ: ダイグラフ→文脈依存→鼻母音化→半母音化→位置の法則→黙字）
    - FrenchSyllabifier（音素ベース音節分割、onset maximization）
    - FrenchOrthography + NasalVowelizer（独立ヘルパー）
    - IPA出力: ToIPA(), ToPhonemes(), ToPhonemeList(), ToSyllables(), バッチAPI
    - テスト218件通過
  - **F2（精度向上・異音規則・テキスト正規化）**: 完了
    - `FrenchNormalizer` 11段階正規化パイプライン（NFC→略語→日付→時刻→通貨→%→単位→小数→数値→記号→空白）
    - `NumberToWords` フランス語20進法数詞変換（vigesimal: 70=soixante-dix, 80=quatre-vingts等）
    - `AllophoneProcessor`（R無声化、阻害音有声性同化）+ `FrenchAllophoneFeatures` flags enum
    - `FrenchExceptionDictionary` 例外辞書（500+エントリ、外来語/不規則語/動詞3複/学術語/同綴異音語）
    - テスト148件追加（累計366件通過）
  - **F3（X-SAMPA・大規模精度評価・拡張テスト）**: 完了
    - XSampaConverter（40音素マッピング）、`ToXSampa()`, `ToXSampaWithoutStress()`, `ToXSampaBatch()` を追加
    - FrenchXSampaTests / FrenchEdgeCaseTests / FrenchPerformanceTests / FrenchAccuracyTests を追加
    - FrenchDatasetEvaluationTests / FrenchAllophoneEvaluationTests（外部TSVコーパスPER閾値テスト）を追加
    - `tools/DotNetG2P.FrenchEval` + `tools/refresh_french_eval_data.ps1` + `tools/run_french_full_evaluation.ps1` により全量PER/WER/カテゴリ別集計を追加
    - テスト150件追加（累計719件: 707 pass + 12 skip）
  - **F4（Multilingual統合・パッケージング）**: 完了
    - `DotNetG2P.Multilingual` に `Language.French` と `FrenchOptions` を追加
    - `TextSegmenter` にフランス語言語判定（高頻度語46語+接尾辞23種+特有文字27種+é曖昧フォールバック）を実装
    - `MultilingualG2PEngine` に `FrenchG2PEngine` を統合
    - `MultilingualFrenchTests` / `MultilingualMixedLanguageTests` に5言語混在テストを追加
    - Multilingual テスト 372件通過、テスト31件追加

## ビルド・実行

```bash
# ビルド
dotnet build DotNetG2P.slnx

# テスト
dotnet test DotNetG2P.slnx

# コンソールサンプル実行（辞書なし: MoraMappingのみ確認）
dotnet run --project samples/DotNetG2P.Console/DotNetG2P.Console.csproj

# コンソールサンプル実行（辞書あり: フルG2P）
dotnet run --project samples/DotNetG2P.Console/DotNetG2P.Console.csproj -- <naist-jdic辞書パス>
```

## プロジェクト構成

```
DotNetG2P.slnx                          # ソリューションファイル（.NET 10 .slnx形式）
├── Directory.Build.props                # NuGet共通メタデータ
├── LICENSE                              # Apache-2.0 License
├── README.md                            # プロジェクトREADME（358行）
├── .editorconfig                        # コーディング規約
├── .gitattributes                       # Git属性設定
├── .github/workflows/                   # GitHub Actions
│   ├── ci.yml                           # CI（push/PR: ビルド・テスト・パック）
│   └── release.yml                      # リリース（NuGet push + GitHub Release）
├── src/
│   ├── DotNetG2P.Core/                  # コアライブラリ（.NET Standard 2.1）
│   │   ├── Models/                      # データ構造
│   │   │   ├── Phoneme.cs               # Consonant enum (35種) + Vowel enum (10種)
│   │   │   ├── MoraKind.cs              # MoraKind enum (~165種) + カタカナ変換
│   │   │   ├── POS.cs                   # POSType enum (14種) + POS class (品詞4フィールド)
│   │   │   ├── Mora.cs                  # Mora readonly struct (子音+母音+種類)
│   │   │   ├── Pronunciation.cs         # Pronunciation class (モーラ列+アクセント位置)
│   │   │   ├── WordDetails.cs           # WordDetails class (品詞・活用・読み)
│   │   │   ├── WordEntry.cs             # WordEntry class (表層形+詳細+アクセント情報)
│   │   │   ├── NjdNode.cs              # NjdNode class (NJD処理中間表現)
│   │   │   ├── AccentPhrase.cs          # AccentPhrase class (VOICEVOX互換)
│   │   │   └── ProsodyFeatures.cs       # ProsodyFeatures class (韻律特徴量A1/A2/A3)
│   │   ├── Tokenizer/                   # 形態素解析抽象化
│   │   │   ├── ITokenizer.cs            # ITokenizer interface
│   │   │   └── IToken.cs               # IToken interface (naist-jdic 15フィールド)
│   │   ├── NJD/                         # NJD処理（6段階パイプライン）
│   │   │   ├── SetPronunciation.cs      # 1. 発音設定（完全版5段階処理）
│   │   │   ├── DigitSequence.cs         # 2a. 数字列検出・グループ化
│   │   │   ├── DigitLut.cs              # 2b. 数字読みLUTテーブル
│   │   │   ├── SetDigit.cs              # 2c. 数字読み変換メインロジック
│   │   │   ├── SetAccentPhrase.cs       # 3. アクセント句結合（18ルール）
│   │   │   ├── SetAccentType.cs         # 4. アクセント結合型（C1-C5, F1-F5, P系列）
│   │   │   └── SetUnvoicedVowel.cs      # 5. 無声音化（6ルール）
│   │   ├── Internal/                   # 内部ユーティリティ
│   │   │   ├── ValueStringBuilder.cs   # ゼロアロケーション文字列構築（ref struct）
│   │   │   └── ThrowHelper.cs          # 例外スローヘルパー（JITインライン化促進）
│   │   ├── TextNormalization/           # テキスト正規化
│   │   │   └── TextNormalizer.cs        # 全角/半角変換、濁点結合
│   │   ├── PhonemeConverter/            # 音素変換
│   │   │   ├── MoraMapping.cs           # カタカナ⇔音素マッピング (162種)
│   │   │   ├── AccentPhraseConverter.cs # VOICEVOX互換アクセント句変換
│   │   │   └── ProsodyExtractor.cs      # ESPnet韻律記号付き出力
│   │   ├── JPCommon/                    # HTSフルコンテキストラベル生成
│   │   │   ├── Models.cs               # 階層モデル (JPUtterance/JPBreathGroup/JPAccentPhrase/JPWord/JPMora/JPPhoneme)
│   │   │   ├── JPCommonBuilder.cs       # NjdNode列→JPCommon階層構築
│   │   │   ├── FullContextLabel.cs      # HTSフルコンテキストラベル生成 + ExtractProsodyFeatures
│   │   │   └── WordAttr.cs             # POS/CType/CForm→ID変換テーブル (jpreprocess準拠)
│   │   ├── G2PEngine.cs                # メインAPI (ToPhonemes, ToKana, ToProsody, ToAccentPhrases, ToFullContextLabels, ToProsodyFeatures, Analyze, +Batch API)
│   │   ├── G2POptions.cs               # 処理オプション（各段階ON/OFF）
│   │   ├── package.json                # UPM パッケージ定義 (com.dotnetg2p.core)
│   │   └── DotNetG2P.asmdef            # Unity Assembly Definition
│   │
│   └── DotNetG2P.MeCab/                # 独自MeCabエンジン（Apache-2.0、外部依存なし）
│       ├── DotNetG2P.MeCab.csproj       # .NET Standard 2.1、DotNetG2P.Core参照のみ
│       ├── MeCabTokenizer.cs            # ITokenizer実装（公開API）
│       ├── Dictionary/                  # 辞書読み込み層
│       │   ├── DictionaryHeader.cs      # 72バイトヘッダパーサ
│       │   ├── DicToken.cs              # トークン構造体（16バイト）
│       │   ├── SystemDictionary.cs      # sys.dic読み込み
│       │   ├── ConnectionMatrix.cs      # matrix.bin読み込み（連接コスト行列）
│       │   ├── CharProperty.cs          # char.bin読み込み（文字カテゴリ）
│       │   ├── UnknownDictionary.cs     # unk.dic読み込み（未知語テンプレート）
│       │   └── DictionaryBundle.cs      # 全辞書ファイル集約管理
│       ├── Trie/                        # DoubleArray Trie
│       │   ├── DoubleArrayTrie.cs       # 共通接頭辞検索
│       │   └── Utf8CharMap.cs           # UTF-8バイト⇔char オフセット変換
│       ├── Lattice/                     # ラティス＋Viterbi
│       │   ├── LatticeNode.cs           # ラティスノード
│       │   ├── LatticeBuilder.cs        # Trie検索+未知語生成→ラティス構築
│       │   └── ViterbiDecoder.cs        # 前向きパス+後ろ向きトレース
│       ├── DotNetG2P.MeCab.asmdef       # Unity Assembly Definition
│       └── package.json                 # UPM パッケージ定義 (com.dotnetg2p.mecab)
│   │
│   ├── DotNetG2P.Chinese/              # 中国語G2Pパッケージ（独立、Core参照なし）
│   │   ├── DotNetG2P.Chinese.csproj     # .NET Standard 2.1
│   │   ├── ChineseG2PEngine.cs          # メインAPI (ToPinyin, ToPinyinList, LookupChar等)
│   │   ├── ChineseG2POptions.cs         # オプション (EnableToneSandhi, HandleHeteronyms, DefaultStyle)
│   │   ├── Models/
│   │   │   ├── Initial.cs               # 声母enum (22種, byte基底)
│   │   │   ├── Final.cs                 # 韻母enum (38種, byte基底)
│   │   │   ├── Tone.cs                  # 声調enum (5種: Neutral/First/Second/Third/Fourth)
│   │   │   ├── PinyinSyllable.cs        # 音節 readonly struct
│   │   │   ├── PinyinStyle.cs           # 出力スタイルenum (ToneMarked/ToneNumber/Normal)
│   │   │   └── PinyinResult.cs          # 変換結果クラス
│   │   ├── Dictionary/
│   │   │   ├── PinyinCharDictionary.cs  # 単字辞書 (44,435エントリ)
│   │   │   ├── PinyinPhraseDictionary.cs # フレーズ辞書 (411,958エントリ)
│   │   │   └── Data/
│   │   │       ├── pinyin_char.txt      # 単字辞書 (EmbeddedResource)
│   │   │       └── pinyin_phrase.txt    # フレーズ辞書 (EmbeddedResource)
│   │   ├── Conversion/
│   │   │   ├── PinyinParser.cs          # ピンイン文字列パーサ
│   │   │   ├── ToneConverter.cs         # 声調変換ユーティリティ
│   │   │   ├── PinyinToIpa.cs           # ピンイン→IPA変換 (C4)
│   │   │   └── PinyinToZhuyin.cs        # ピンイン→注音符号変換 (C4)
│   │   ├── ToneSandhi/
│   │   │   └── ToneSandhiProcessor.cs   # 声調変調（三声連読、一/不変調）
│   │   ├── package.json                 # UPM (com.dotnetg2p.chinese)
│   │   └── DotNetG2P.Chinese.asmdef     # Unity Assembly Definition
│   │
│   ├── DotNetG2P.English/              # 英語G2Pパッケージ（独立、Core参照なし）
│       ├── DotNetG2P.English.csproj     # .NET Standard 2.1
│       ├── EnglishG2PEngine.cs          # メインAPI (ToPhonemes, ToPhonemeList, LookupWord等)
│       ├── EnglishG2POptions.cs         # オプション (IncludeStress, EnableLts, EnableNormalization, EnableHomographResolution)
│       ├── Models/
│       │   ├── ArpabetPhoneme.cs        # ARPAbet音素enum (39音素, byte基底)
│       │   ├── Stress.cs                # ストレスenum (None/NoStress/Primary/Secondary)
│       │   ├── EnglishPhoneme.cs        # ストレス付き音素 readonly struct
│       │   ├── EnglishPronunciation.cs  # 発音クラス (音素配列)
│       │   └── ArpabetParser.cs         # ARPAbetパーサー
│       ├── Dictionary/
│       │   ├── CmuDictionary.cs         # CMU辞書ルックアップ (135,166エントリ)
│       │   └── Data/cmudict.dict        # CMU辞書 (EmbeddedResource)
│       ├── LTS/
│       │   ├── LtsEngine.cs             # Flite CARTツリーLTSエンジン
│       │   ├── LtsData.cs               # CARTツリーデータ定義
│       │   ├── LtsPhoneMapping.cs       # LTS→ARPAbetマッピング
│       │   └── cmu_lts_model.bin        # CARTツリーバイナリ (EmbeddedResource)
│       ├── Normalization/               # テキスト正規化 (E3)
│       │   ├── EnglishNormalizer.cs     # ファサード
│       │   ├── NumberToWords.cs         # 数字→英語読み
│       │   ├── CurrencyExpander.cs      # 通貨展開
│       │   ├── TimeExpander.cs          # 時刻展開
│       │   ├── AbbreviationExpander.cs  # 略語展開
│       │   ├── AcronymDetector.cs       # 頭字語判別
│       │   └── SymbolExpander.cs        # 記号→名前変換
│       ├── Homograph/                   # 同綴異音語解決 (E4)
│       │   ├── HomographResolver.cs     # 解決ファサード
│       │   ├── HomographDatabase.cs     # 30+語データベース
│       │   ├── HomographEntry.cs        # エントリ・ルールモデル
│       │   ├── PosGuesser.cs            # 軽量品詞推定
│       │   └── PosTag.cs               # 品詞タグenum
│       ├── package.json                 # UPM (com.dotnetg2p.english)
│       └── DotNetG2P.English.asmdef     # Unity Assembly Definition
│
│   ├── DotNetG2P.Spanish/              # スペイン語G2Pパッケージ（独立、Core参照なし）
│   │   ├── DotNetG2P.Spanish.csproj     # .NET Standard 2.1
│   │   ├── SpanishG2PEngine.cs          # メインAPI (ToIPA, ToPhonemes, ToPhonemeList等)
│   │   ├── SpanishG2POptions.cs         # オプション (Dialect, IncludeStress, EnableAllophones, Separator)
│   │   ├── Models/
│   │   │   ├── SpanishIpaPhoneme.cs     # IPA音素enum : byte (28種)
│   │   │   ├── SpanishPhoneme.cs        # ストレス付き音素 readonly struct
│   │   │   ├── SpanishPronunciation.cs  # 発音クラス (音素配列ラッパー)
│   │   │   └── Dialect.cs               # 方言enum : byte (LatinAmerican, Castilian)
│   │   ├── Rules/
│   │   │   ├── GraphemeToPhonemeRules.cs # コアG2Pルール（switch文ベース3フェーズ）
│   │   │   ├── SyllableParser.cs        # 音節分割 (onset maximization)
│   │   │   ├── StressAssigner.cs        # ストレス位置決定
│   │   │   └── AllophoneProcessor.cs    # 異音規則 (β,ð,ɣ弱化, 鼻音同化) [S2]
│   │   ├── Normalization/
│   │   │   └── SpanishNormalizer.cs     # テキスト正規化 (数値/日付/時刻/単位/略語/記号)
│   │   ├── Conversion/
│   │   │   ├── IpaConverter.cs          # IPA変換
│   │   │   └── XSampaConverter.cs       # X-SAMPA変換 [S3]
│   │   ├── package.json                 # UPM (com.dotnetg2p.spanish)
│   │   └── DotNetG2P.Spanish.asmdef     # Unity Assembly Definition
│   │
│   ├── DotNetG2P.French/               # フランス語G2Pパッケージ（独立、Core参照なし）
│   │   ├── DotNetG2P.French.csproj      # .NET Standard 2.1
│   │   ├── FrenchG2PEngine.cs           # メインAPI (ToIPA, ToPhonemes, ToXSampa, ToPhonemeList等)
│   │   ├── FrenchG2POptions.cs          # オプション (Dialect, EnableAllophones, EnableExceptionDictionary等)
│   │   ├── FrenchAllophoneFeatures.cs   # [Flags] enum : byte (5規則)
│   │   ├── Models/
│   │   │   ├── FrenchIpaPhoneme.cs      # IPA音素enum : byte (40種)
│   │   │   ├── FrenchPhoneme.cs         # 音素 readonly struct (Phoneme + IsSyllableNucleus)
│   │   │   ├── FrenchPronunciation.cs   # 発音クラス (音素配列 + 音節オフセット)
│   │   │   └── FrenchDialect.cs         # 方言enum : byte (Metropolitan, Conservative)
│   │   ├── Rules/
│   │   │   ├── GraphemeToPhonemeRules.cs # コアG2Pルール (6フェーズ) [F1]
│   │   │   ├── FrenchOrthography.cs     # 正書法ヘルパー [F1]
│   │   │   ├── NasalVowelizer.cs        # 鼻母音化ロジック [F1]
│   │   │   ├── FrenchSyllabifier.cs     # 音素ベース音節分割 [F1]
│   │   │   └── AllophoneProcessor.cs    # 異音規則 (R無声化、阻害音有声性同化) [F2]
│   │   ├── Normalization/
│   │   │   ├── FrenchNormalizer.cs      # テキスト正規化 (11段階パイプライン) [F2]
│   │   │   └── NumberToWords.cs         # フランス語数詞変換 (vigesimal 20進法) [F2]
│   │   ├── Data/
│   │   │   ├── FrenchExceptionDictionary.cs # 例外辞書ルックアップ [F2]
│   │   │   └── french_exceptions.master.tsv # 例外辞書TSV (500+エントリ) [F2]
│   │   ├── Conversion/
│   │   │   ├── IpaConverter.cs          # IPA変換 [F1]
│   │   │   └── XSampaConverter.cs       # X-SAMPA変換 (40音素マッピング) [F3]
│   │   ├── package.json                 # UPM (com.dotnetg2p.french)
│   │   └── DotNetG2P.French.asmdef      # Unity Assembly Definition
│   │
│   └── DotNetG2P.Multilingual/         # 多言語G2Pパッケージ（Core + MeCab + English + Chinese + Spanish + French依存）
│       ├── DotNetG2P.Multilingual.csproj # .NET Standard 2.1
│       ├── Language.cs                  # Language enum (Japanese/English/Chinese/Spanish/French)
│       ├── ScriptKind.cs               # ScriptKind enum (8種分類、internal)
│       ├── TextSegment.cs              # 言語タグ付きテキストセグメント
│       ├── G2PSegment.cs               # G2P結果セグメント
│       ├── MultilingualG2POptions.cs   # 多言語G2Pオプション（DefaultCjkLanguage / DefaultLatinLanguage）
│       ├── LanguageDetector.cs         # Unicode文字種ベース言語判定
│       ├── TextSegmenter.cs            # テキストセグメント分割（日英西ラテン文字対応）
│       ├── MultilingualG2PEngine.cs    # 多言語G2Pエンジン（日英中西仏ファサード）
│       ├── package.json                # UPM (com.dotnetg2p.multilingual)
│       └── DotNetG2P.Multilingual.asmdef # Unity Assembly Definition
│
├── tests/
│   ├── TestData/                        # テストデータ
│   │   ├── expected_phonemes.json       # pyopenjtalk期待値データ（18件）
│   │   └── generate_expected.py         # テストデータ生成スクリプト
│   └── DotNetG2P.Tests/                 # xUnit テストプロジェクト (net8.0)
│       ├── DotNetG2P.Tests.csproj
│       ├── G2PEngineApiTests.cs         # G2PEngine API統合テスト
│       ├── Models/                      # モデルテスト
│       │   ├── NjdNodeTests.cs
│       │   └── PronunciationTests.cs
│       ├── NJD/                         # NJD処理テスト
│       │   ├── SetPronunciationTests.cs # 発音設定テスト（25件）
│       │   ├── SetAccentPhraseTests.cs  # アクセント句結合テスト（37件）
│       │   ├── SetAccentTypeTests.cs    # アクセント結合型テスト（39件）
│       │   ├── DigitSequenceTests.cs    # 数字列検出テスト（14件）
│       │   ├── SetDigitTests.cs         # 数字読み変換テスト（32件）
│       │   ├── DigitReadingTests.cs     # 数字読み網羅テスト（25件、辞書依存）
│       │   └── SetUnvoicedVowelTests.cs
│       ├── TextNormalization/           # テキスト正規化テスト
│       │   └── TextNormalizerTests.cs
│       ├── PhonemeConverter/            # 音素変換テスト
│       │   ├── MoraMappingTests.cs
│       │   ├── MoraMappingFullTests.cs  # 全165パターン検証（166件）
│       │   ├── AccentPhraseConverterTests.cs
│       │   └── ProsodyExtractorTests.cs
│       ├── JPCommon/                    # JPCommonテスト
│       │   ├── JPCommonBuilderTests.cs
│       │   ├── WordAttrTests.cs
│       │   ├── FullContextLabelTests.cs
│       │   └── ProsodyFeaturesTests.cs  # 韻律特徴量テスト（7件）
│       ├── MeCab/                       # MeCabエンジンテスト
│       │   ├── MeCabTokenizerTests.cs   # 基本動作テスト（~30件）
│       │   ├── TokenizerComparisonTests.cs # 出力一致テスト（100+文×3）
│       │   ├── G2PComparisonTests.cs    # G2Pパイプライン比較テスト（20件×6）
│       │   ├── Utf8CharMapTests.cs      # UTF-8オフセット変換テスト
│       │   ├── DictionaryErrorTests.cs  # エラーハンドリングテスト
│       │   ├── MeCabIndependentTests.cs # 独立仕様検証テスト（21件）
│       │   └── PerformanceTests.cs      # パフォーマンステスト（5件）
│       ├── ChineseG2P/                  # 中国語G2Pテスト (936件)
│       │   ├── ChineseG2PEngineTests.cs  # C1エンジン統合テスト
│       │   ├── ChineseG2PEngineC2Tests.cs # C2エンジン統合テスト (78件)
│       │   ├── ChineseG2PEngineC3Tests.cs # C3エンジン統合テスト (42件)
│       │   ├── ChineseG2PEngineC4Tests.cs # C4統合テスト (51件)
│       │   ├── ToneSandhiProcessorTests.cs # 声調変調テスト (30件)
│       │   ├── PolyphoneResolutionTests.cs # 多音字解決テスト (54件)
│       │   ├── PinyinPhraseDictionaryTests.cs # フレーズ辞書テスト (23件)
│       │   ├── PinyinCharDictionaryTests.cs # 単字辞書テスト
│       │   ├── ToneConverterTests.cs     # 声調変換テスト
│       │   ├── PinyinParserTests.cs      # ピンインパーサテスト
│       │   ├── ToneTests.cs             # 声調テスト
│       │   ├── InitialTests.cs          # 声母テスト
│       │   ├── FinalTests.cs            # 韻母テスト
│       │   ├── PinyinSyllableTests.cs   # 音節テスト
│       │   ├── IpaConversionTests.cs     # IPA変換テスト (125件)
│       │   ├── ZhuyinConversionTests.cs  # 注音変換テスト (112件)
│       │   ├── ChineseEdgeCaseTests.cs   # エッジケーステスト (61件)
│       │   ├── ChinesePerformanceTests.cs # パフォーマンステスト (15件)
│       │   └── ChineseAccuracyTests.cs   # 精度・回帰テスト (78件)
│       ├── EnglishG2P/                  # 英語G2Pテスト (511件)
│       │   ├── Dictionary/              # 辞書テスト (~29件)
│       │   ├── Models/                  # モデルテスト (~31件)
│       │   ├── Lts/                     # LTSテスト (~95件)
│       │   ├── Normalization/           # 正規化テスト (143件)
│       │   ├── Homograph/              # 同綴異音語テスト (154件)
│       │   └── Integration/            # 統合テスト (~42件)
│       ├── SpanishG2P/                 # スペイン語G2Pテスト
│       │   ├── SpanishG2PEngineTests.cs    # エンジン統合テスト [S1]
│       │   ├── GraphemeToPhonemeRulesTests.cs # G2Pルールテスト [S1]
│       │   ├── SpanishSyllabifierTests.cs  # 音節分割テスト [S1]
│       │   ├── StressAssignerTests.cs      # ストレステスト [S1]
│       │   ├── SpanishIpaTests.cs          # IPA変換テスト [S1]
│       │   ├── SpanishPhonemeTests.cs      # 音素モデルテスト [S1]
│       │   ├── SpanishNormalizerTests.cs   # 正規化テスト [S2]
│       │   ├── AllophoneProcessorTests.cs  # 異音テスト [S2]
│       │   ├── SpanishXSampaTests.cs       # X-SAMPA変換テスト [S3]
│       │   ├── SpanishEdgeCaseTests.cs     # エッジケーステスト [S3]
│       │   ├── SpanishPerformanceTests.cs  # パフォーマンステスト [S3]
│       │   └── SpanishAccuracyTests.cs     # 精度・回帰テスト [S3]
│       ├── FrenchG2P/                  # フランス語G2Pテスト (719件: 707 pass + 12 skip)
│       │   ├── FrenchG2PEngineTests.cs     # エンジン統合テスト (32件) [F1]
│       │   ├── GraphemeToPhonemeRulesTests.cs # G2Pルール単体テスト (109件) [F1]
│       │   ├── FrenchSyllabifierTests.cs   # 音節分割テスト (38件) [F1]
│       │   ├── FrenchIpaTests.cs           # IPA変換テスト (43件) [F1]
│       │   ├── FrenchPhonemeTests.cs       # 音素モデルテスト (31件) [F1]
│       │   ├── FrenchNumberToWordsTests.cs # 数値→文字列変換テスト (55件) [F2]
│       │   ├── FrenchNormalizerTests.cs    # 正規化テスト (51件) [F2]
│       │   ├── AllophoneProcessorTests.cs  # 異音テスト (22件) [F2]
│       │   ├── FrenchExceptionDictionaryTests.cs # 例外辞書テスト (24件) [F2]
│       │   ├── FrenchXSampaTests.cs        # X-SAMPA変換テスト (63件) [F3]
│       │   ├── FrenchEdgeCaseTests.cs      # エッジケーステスト (36件) [F3]
│       │   ├── FrenchPerformanceTests.cs   # パフォーマンステスト (10件) [F3]
│       │   ├── FrenchAccuracyTests.cs      # 精度・回帰テスト (29件) [F3]
│       │   ├── FrenchDatasetEvaluationTests.cs # 外部TSVコーパスPER閾値テスト (6件) [F3]
│       │   ├── FrenchAllophoneEvaluationTests.cs # 異音プロファイル別PER評価 (6件) [F3]
│       │   ├── NasalVowelizerTests.cs         # 鼻母音化テスト (35件) [F1]
│       │   └── FrenchOrthographyTests.cs      # 正書法ヘルパーテスト (129件) [F1]
│       ├── Multilingual/               # 多言語G2Pテスト（372件通過）
│       │   ├── LanguageDetectorTests.cs  # 言語判定テスト
│       │   ├── TextSegmenterTests.cs     # セグメント分割テスト
│       │   ├── MultilingualEngineTests.cs # エンジン統合テスト
│       │   ├── MultilingualEdgeCaseTests.cs # エッジケーステスト
│       │   ├── MultilingualDisposeTests.cs # Disposeテスト
│       │   ├── MultilingualPerformanceTests.cs # パフォーマンステスト
│       │   ├── LanguageConsistencyTests.cs # 言語検出一貫性テスト
│       │   ├── MultilingualChineseTests.cs # 中国語統合テスト
│       │   ├── MultilingualSpanishTests.cs # スペイン語統合テスト
│       │   ├── MultilingualFrenchTests.cs # フランス語統合テスト
│       │   ├── MultilingualSharedFixture.cs # 重い統合テスト用 shared fixture
│       │   ├── EmbeddedChineseDictionaryCacheTests.cs # 中国語辞書共有キャッシュ検証
│       │   ├── MixedTextBasicTests.cs    # 混在テキスト基本テスト
│       │   ├── MixedTextAdvancedTests.cs # 混在テキスト応用テスト
│       │   └── MultilingualMixedLanguageTests.cs # 5言語混在回帰テスト
│       └── Integration/                # 統合テスト
│           ├── G2PPipelineTests.cs
│           ├── EdgeCaseTests.cs         # エッジケーステスト（~57件）
│           ├── PiperPlusTests.cs        # piper-plus移植テスト（87件）
│           └── PyOpenJTalkComparisonTests.cs  # pyopenjtalk比較テスト（20件）
│
└── samples/
    └── DotNetG2P.Console/               # コンソールサンプル (net8.0)
        ├── DotNetG2P.Console.csproj
        └── Program.cs
```

## 背景・動機

- OpenJTalkやpyopenjtalkはC/C++/Python実装であり、C#/.NETやUnityから直接利用するのが困難
- 既存のC#向け日本語G2Pライブラリは存在しない
- Unity（ゲーム・VTuber・音声合成等）での日本語TTS前処理として需要がある

## アーキテクチャ方針

OpenJTalkの処理パイプラインに準拠した4段階処理:

1. **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、Apache-2.0）を使用（ITokenizer抽象化により差し替え可能）
2. **NJD処理（日本語ルール処理）**: 読み生成、数字読み変換、アクセント句結合、アクセント結合、無声音化、長音化
3. **音素変換**: カタカナ読み → 音素列（例: `コンニチワ` → `k o N n i ch i w a`）
4. **アクセント情報付与**（オプション）: モーラ数・アクセント核位置の出力

### 日本語音素体系

| 種別 | 音素 |
|------|------|
| 母音 | a, i, u, e, o (+ 無声母音 A, I, U, E, O) |
| 半母音 | y, w |
| 子音 | k, g, s, z, t, d, n, h, b, p, m, r, ch, sh, j, f, ts, ky, gy, ny, hy, by, py, my, ry, v, dy, ty, gw, kw |
| 特殊 | N（撥音）, cl（促音）, -（長音） |

### 辞書

OpenJTalk用のnaist-jdic辞書フォーマット（IPADIC + アクセント情報2フィールド拡張）を使用:
- フィールド13: `アクセント核位置/モーラ数`（例: `3/4`）
- フィールド14: アクセント結合タイプ（C1〜C5）

## 技術スタック

- **言語**: C#
- **ターゲット**: .NET Standard 2.1（Unity 2021.2+互換）
- **形態素解析**: 独自MeCabエンジン（`DotNetG2P.MeCab`、Apache-2.0、外部依存なし）
- **辞書**: naist-jdic（BSD License）
- **テスト**: xUnit 2.5.3 (net8.0)
- **パッケージング**: NuGet (`DotNetG2P`, `DotNetG2P.MeCab`, `DotNetG2P.Chinese`, `DotNetG2P.English`, `DotNetG2P.Spanish`, `DotNetG2P.French`, `DotNetG2P.Multilingual`) + UPM (`com.dotnetg2p.core`, `com.dotnetg2p.mecab`, `com.dotnetg2p.chinese`, `com.dotnetg2p.english`, `com.dotnetg2p.spanish`, `com.dotnetg2p.french`, `com.dotnetg2p.multilingual`)
- **CI/CD**: GitHub Actions (ci.yml, release.yml)
- **ソリューション形式**: .slnx（.NET 10）

## 開発言語

コード内コメント・ドキュメント・コミットメッセージ・PR・Issueはすべて**日本語**で記述する。
