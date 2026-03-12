# DotNetG2P

**日本語** | [English](README_EN.md) | [中文](README_ZH.md)

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

C#/.NET向け日英中韓西仏葡多言語対応 G2P（Grapheme-to-Phoneme: 書記素→音素変換）ライブラリ。
OpenJTalk互換の日本語G2Pパイプライン、CMU辞書ベースの英語G2P、pinyin-data辞書ベースの中国語ピンイン変換、Hangul-first の韓国語G2P、ルールベースのスペイン語G2P、ルールベース+例外辞書のフランス語G2P、ルールベース+例外辞書のポルトガル語G2PをC#でネイティブに再実装し、Pythonやネイティブバイナリへの依存なしに音素列へ変換します。

```csharp
using var engine = new G2PEngine(new MeCabTokenizer());

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"

// 英語G2P
using var enEngine = new EnglishG2PEngine();
enEngine.ToPhonemes("hello world");  // => "HH AH0 L OW1 W ER1 L D"

// 中国語G2P（ピンイン変換）
using var zhEngine = new ChineseG2PEngine();
zhEngine.ToPinyin("你好世界");  // => "ní hǎo shì jiè"

// 韓国語G2P
using var koEngine = new KoreanG2PEngine();
koEngine.ToPhonemes("좋다");  // => "ㅈ ㅗ ㅌ ㅏ"

// スペイン語G2P
using var esEngine = new SpanishG2PEngine();
esEngine.ToIPA("vergüenza");  // => "beɾˈɡwensa"

// フランス語G2P
using var frEngine = new FrenchG2PEngine();
frEngine.ToIPA("bonjour");  // => "bɔ̃ʒuʁ"

// ポルトガル語G2P
using var ptEngine = new PortugueseG2PEngine();
ptEngine.ToIPA("obrigado");  // => "obɾiˈɡadu"

// 日韓英混在テキスト
using var multiEngine = new MultilingualG2PEngine();
multiEngine.ToPhonemes("今日は안녕하세요 hello");  // 日本語部分は日本語音素、韓国語部分はHangul phoneme、英語部分はARPAbet
```

## 目次

- [特徴](#特徴)
- [インストール](#インストール)
- [クイックスタート](#クイックスタート)
- [API リファレンス](#api-リファレンス)
- [処理パイプライン](#処理パイプライン)
- [辞書の準備](#辞書の準備)
- [スペイン語評価](#スペイン語評価)
- [フランス語評価](#フランス語評価)
- [ポルトガル語評価](#ポルトガル語評価)
- [オプション設定](#オプション設定)
- [ビルド](#ビルド)
- [スレッドセーフティ](#スレッドセーフティ)
- [ライセンス](#ライセンス)

## 特徴

- **純C#実装** — ネイティブバイナリ不要、独自MeCabエンジン（`DotNetG2P.MeCab`）によりNuGetパッケージ依存なし（実行時に[naist-jdic辞書](#辞書の準備)が必要）
- **OpenJTalk互換パイプライン** — 発音生成・数字読み・アクセント句結合・アクセント結合型・無声音化の6段階NJD処理
- **複数の出力形式** — 音素列 / カタカナ / ESPnet韻律記号 / VOICEVOX互換AccentPhrase / HTSフルコンテキストラベル / 韻律特徴量（A1/A2/A3）
- **Unity対応** — .NET Standard 2.1（Unity 2021.2+）ターゲット、UPMパッケージ提供
- **拡張可能な設計** — `ITokenizer`インターフェースにより形態素解析エンジンを差し替え可能
- **英語G2P対応** — CMU辞書（135,000語）+ Flite LTSルールによるOOV推定、IPA/X-SAMPA出力、テキスト正規化、同綴異音語解決
- **中国語G2P対応** — pinyin-data単字辞書（44,000語）+ phrase-pinyin-dataフレーズ辞書（411,000語）による多音字自動解決、声調変調（三声連読・一/不変調）、3種の出力スタイル、IPA（国際音声記号）・注音符号（ボポモフォ）出力
- **韓国語G2P対応** — Hangul-first の規則ベース変換、Jamo 分解、例外辞書、軽量正規化、`ㅎ` 系変化・終声中和・連音・濃音化・鼻音化・流音化を含む標準発音寄り rule engine、benchmark harness、external corpus gate、performance test を実装
- **スペイン語G2P対応** — ルールベースIPA変換、音節分割、ストレス付与、Castilian/Latin American 切り替え、異音処理オプション、略語/数値/通貨/割合の正規化、例外辞書、全量コーパス評価ツールを実装。桁区切り/小数点の解釈分離と不正な日付/時刻の安全なフォールバックにも対応
- **フランス語G2P対応** — ルールベース6フェーズG2P変換（ダイグラフ→文脈依存→鼻母音化→半母音化→位置の法則→黙字）、音素ベース音節分割、Metropolitan/Conservative方言切り替え、異音処理（R無声化・阻害音有声性同化）、例外辞書500+エントリ（外来語/不規則語/動詞3複/学術語/同綴異音語）、テキスト正規化（数値/日付/時刻/通貨/単位/略語/記号）、IPA/X-SAMPA出力、全量コーパス評価ツールを実装
- **ポルトガル語G2P対応** — ルールベースG2P変換 + 例外辞書（560+エントリ）、音節分割、ストレス付与、Brazilian/European方言切り替え、7種の異音規則（母音弱化・鼻音同化・歯擦音有声性同化・閉鎖音弱化・歯擦音後部歯茎化・t/d破擦音化・コーダl異音）、テキスト正規化（13段階パイプライン: 略語/日付/時刻/通貨/%/単位/数値範囲/小数/数値/記号）、IPA/X-SAMPA出力、全量コーパス評価ツールを実装
- **日英中韓西仏葡混在テキスト対応** — Unicode文字種ベースの自動言語判定・セグメント分割に加え、Hangul block は Korean segment として自動ルーティング。`DefaultLatinLanguage` により英語/スペイン語/フランス語/ポルトガル語のラテン文字系セグメントを切り替え可能。ポルトガル語は特有文字(ã/õ)・ç接尾辞パターン・高頻度語彙で自動判定。純漢字runは marker・日本語語彙ヒント・埋め込み中国語辞書を使って JP/ZH を補強判定し、中国語埋め込み辞書は `ChineseG2PEngine` と共有して二重ロードを避けます

## インストール

### NuGet

```bash
# コアライブラリ + 独自MeCabエンジン（日本語G2P）
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab

# 英語G2P
dotnet add package DotNetG2P.English

# 中国語G2P（ピンイン変換）
dotnet add package DotNetG2P.Chinese

# 韓国語G2P
dotnet add package DotNetG2P.Korean

# スペイン語G2P
dotnet add package DotNetG2P.Spanish

# フランス語G2P
dotnet add package DotNetG2P.French

# ポルトガル語G2P
dotnet add package DotNetG2P.Portuguese

# 日英中韓西仏葡混在テキスト対応
dotnet add package DotNetG2P.Multilingual
```

### パッケージ構成

| パッケージ | ライセンス | 説明 |
|-----------|-----------|------|
| `DotNetG2P` | Apache-2.0 | コアライブラリ（G2Pエンジン、NJD処理、音素変換） |
| `DotNetG2P.MeCab` | Apache-2.0 | 独自MeCabエンジン（外部依存なし） |
| `DotNetG2P.English` | Apache-2.0 | 英語G2Pエンジン（CMU辞書 + LTSルール） |
| `DotNetG2P.Chinese` | Apache-2.0 | 中国語G2Pエンジン（pinyin-data辞書 + 声調変調） |
| `DotNetG2P.Korean` | Apache-2.0 | 韓国語G2Pエンジン（Hangul-first rule engine + 例外辞書 + 正規化） |
| `DotNetG2P.Spanish` | Apache-2.0 | スペイン語G2Pエンジン（ルールベース + 異音処理オプション） |
| `DotNetG2P.French` | Apache-2.0 | フランス語G2Pエンジン（ルールベース + 例外辞書 + 異音処理オプション） |
| `DotNetG2P.Portuguese` | Apache-2.0 | ポルトガル語G2Pエンジン（ルールベース + 例外辞書 + 異音処理オプション） |
| `DotNetG2P.Multilingual` | Apache-2.0 | 多言語G2Pエンジン（日英中韓西仏葡混在テキスト対応） |

### Unity (UPM)

Unity Package Managerの **Add package from git URL** で以下を追加:

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.English
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Chinese
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Korean
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Spanish
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.French
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Portuguese
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Multilingual
```

> **Dependency note:** `DotNetG2P.MeCab` を使う場合は `DotNetG2P.Core` も追加してください。`DotNetG2P.Multilingual` を使う場合は、上記の依存パッケージ群も一緒に `manifest.json` へ追加する必要があります。
> **Note:** 日本語または多言語エンジンでは別途 naist-jdic 辞書が必要です。詳細は[辞書の準備](#辞書の準備)を参照してください。

## クイックスタート

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

// 1. 既定インストール先または環境変数から辞書を自動解決
using var tokenizer = new MeCabTokenizer();
using var engine = new G2PEngine(tokenizer);

// 2. テキストから音素列を取得
string phonemes = engine.ToPhonemes("今日は良い天気です");
// => "ky o o w a i i t e N k i d e s U"

// 3. カタカナ読みを取得
string kana = engine.ToKana("今日は良い天気です");
// => "キョーワイーテンキデス"

// 4. 韻律記号付き出力（ESPnet方式）
string prosody = engine.ToProsody("こんにちは");
// => "^ k o [ N n i ch i w a $"

// 5. VOICEVOX互換アクセント句
var phrases = engine.ToAccentPhrases("こんにちは");

// 6. HTSフルコンテキストラベル（HMM/DNN音声合成用）
var labels = engine.ToFullContextLabels("こんにちは");

// 7. 韻律特徴量（音素単位のA1/A2/A3、uPiper等の音声合成エンジン向け）
var features = engine.ToProsodyFeatures("こんにちは");
// features.Phonemes: ["sil","k","o","N","n","i","ch","i","w","a","sil"]
// features.A1, A2, A3: 各音素のアクセント位置情報

// === 中国語G2P（ピンイン変換）===
using DotNetG2P.Chinese;

using var zhEngine = new ChineseG2PEngine();

// 基本変換（声調記号付き）
string pinyin = zhEngine.ToPinyin("你好世界");
// => "ní hǎo shì jiè"

// 声調番号形式
string toneNum = zhEngine.ToPinyin("你好世界", PinyinStyle.ToneNumber);
// => "ni2 hao3 shi4 jie4"

// 各文字ごとのピンイン配列
string[] list = zhEngine.ToPinyinList("中国");
// => ["zhōng", "guó"]

// 多音字の自動解決
string bank = zhEngine.ToPinyin("银行");  // => "yín háng"（háng = 銀行）
string act = zhEngine.ToPinyin("行为");   // => "xíng wéi"（xíng = 行為）

// IPA（国際音声記号）出力
string ipa = zhEngine.ToIPA("你好");
// => IPA表記

// 注音符号（ボポモフォ）出力
string zhuyin = zhEngine.ToZhuyin("你好");
// => 注音符号表記

// === 英語G2P ===
using DotNetG2P.English;

using var enEngine = new EnglishG2PEngine();
string enPhonemes = enEngine.ToPhonemes("hello world");
// => "HH AH0 L OW1 W ER1 L D"

// === 韓国語G2P ===
using DotNetG2P.Korean;

using var koEngine = new KoreanG2PEngine();
string koPhonemes = koEngine.ToPhonemes("좋다");
// => "ㅈ ㅗ ㅌ ㅏ"

string koJamo = koEngine.ToJamo("한글");
// => "ㅎㅏㄴ ㄱㅡㄹ"

using var koColloquial = new KoreanG2PEngine(
    new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial));
string koColloquialHangul = koColloquial.Analyze("나의").ToHangulString();
// => "나에"

// === スペイン語G2P ===
using DotNetG2P.Spanish;

using var esEngine = new SpanishG2PEngine();
string esIpa = esEngine.ToIPA("guion");
// => "ɡiˈon"

using var esAlloEngine = new SpanishG2PEngine(new SpanishG2POptions(enableAllophones: true));
string esAllo = esAlloEngine.ToIPA("uva");
// => "ˈuβa"

// === フランス語G2P ===
using DotNetG2P.French;

using var frEngine = new FrenchG2PEngine();
string frIpa = frEngine.ToIPA("bonjour");
// => "bɔ̃ʒuʁ"

string frIpa2 = frEngine.ToIPA("merci");
// => "mɛʁsi"

// 例外辞書 + 異音処理
using var frAlloEngine = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: true));
string frAllo = frAlloEngine.ToIPA("autre");
// => 異音規則適用済みIPA

// X-SAMPA出力
string frXsampa = frEngine.ToXSampa("bonjour");

// === ポルトガル語G2P ===
using DotNetG2P.Portuguese;

using var ptEngine = new PortugueseG2PEngine();
string ptIpa = ptEngine.ToIPA("obrigado");
// => "obɾiˈɡadu"

string ptIpa2 = ptEngine.ToIPA("coração");
// => "koɾaˈsɐ̃w̃"

// ヨーロッパポルトガル語方言
using var ptEpEngine = new PortugueseG2PEngine(new PortugueseG2POptions(dialect: PortugueseDialect.European));
string ptEpIpa = ptEpEngine.ToIPA("obrigado");

// 異音処理有効化
using var ptAlloEngine = new PortugueseG2PEngine(new PortugueseG2POptions(enableAllophones: true));
string ptAllo = ptAlloEngine.ToIPA("cidade");
// => 異音規則適用済みIPA（BP: t/d破擦音化など）

// X-SAMPA出力
string ptXsampa = ptEngine.ToXSampa("obrigado");
string ptXsampaNoStress = ptEngine.ToXSampaWithoutStress("obrigado");

// バッチ処理
var ptBatch = ptEngine.ToIPABatch(new[] { "bom dia", "boa noite" });

// === 日英中韓西仏葡混在テキスト ===
using DotNetG2P.Multilingual;

using var multiEngine = new MultilingualG2PEngine();
string mixed = multiEngine.ToPhonemes("今日は안녕하세요 good dayです");
// 日本語部分→日本語音素、韓国語部分→Hangul phoneme、英語部分→ARPAbet音素

var segments = multiEngine.ToSegments("今日はgood dayです");
// 言語タグ付きセグメントリスト

// 中国語テキストを含む場合
var zhOptions = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
using var multiZhEngine = new MultilingualG2PEngine(zhOptions);
multiZhEngine.ToPhonemes("你好hello");
// 中国語部分→ピンイン、英語部分→ARPAbet音素

// 韓国語オプションを含む場合
var koOptions = new MultilingualG2POptions(
    koreanOptions: new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial));
using var multiKoEngine = new MultilingualG2PEngine(koOptions);
multiKoEngine.ToPhonemes("나의 hello");
// 韓国語部分→KoreanG2PEngine、英語部分→ARPAbet音素

// スペイン語テキストを含む場合
var esOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Spanish);
using var multiEsEngine = new MultilingualG2PEngine(esOptions);
multiEsEngine.ToPhonemes("hola世界");
// スペイン語部分→IPA音素、日本語部分→日本語音素

// フランス語テキストを含む場合
var frOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.French);
using var multiFrEngine = new MultilingualG2PEngine(frOptions);
multiFrEngine.ToPhonemes("bonjour世界");
// フランス語部分→IPA音素、日本語部分→日本語音素

// ポルトガル語テキストを含む場合
var ptOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Portuguese);
using var multiPtEngine = new MultilingualG2PEngine(ptOptions);
multiPtEngine.ToPhonemes("obrigado世界");
// ポルトガル語部分→IPA音素、日本語部分→日本語音素
```

## API リファレンス

### G2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | 空白区切り音素列 (`"k o N n i ch i w a"`) |
| `ToKana(text)` | `string` | カタカナ読み (`"コンニチワ"`) |
| `ToProsody(text)` | `string` | ESPnet韻律記号付き (`"^ k o [ N n i ch i w a $"`) |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX互換アクセント句構造体 |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTSフルコンテキストラベル |
| `ToProsodyFeatures(text)` | `ProsodyFeatures` | 韻律特徴量（音素単位のA1/A2/A3） |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | NJD処理後のノード列 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括で音素列に変換 |
| `ToKanaBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括でカタカナ読みに変換 |
| `ToProsodyBatch(texts)` | `IReadOnlyList<string>` | 複数テキストを一括で韻律記号付きに変換 |
| `ToFullContextLabelsBatch(texts)` | `IReadOnlyList<IReadOnlyList<string>>` | 複数テキストを一括でHTSラベルに変換 |
| `ToProsodyFeaturesBatch(texts)` | `IReadOnlyList<ProsodyFeatures>` | 複数テキストを一括で韻律特徴量に変換 |

### EnglishG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | ARPAbet音素列 (`"HH AH0 L OW1"`) |
| `ToIPA(text)` | `string` | IPA表記 |
| `ToIPAWithoutStress(text)` | `string` | ストレスマークなしIPA表記 |
| `ToXSampa(text)` | `string` | X-SAMPA表記 |
| `ToXSampaWithoutStress(text)` | `string` | ストレスマークなしX-SAMPA表記 |
| `ToPhonemeList(text)` | `IReadOnlyList<EnglishPhoneme>` | 構造化音素リスト |
| `LookupWord(word)` | `IReadOnlyList<EnglishPhoneme>` | 単語ルックアップ |
| `LookupAllPronunciations(word)` | `IReadOnlyList<EnglishPronunciation>` | 全発音バリアント取得 |
| `ContainsWord(word)` | `bool` | 辞書存在確認 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチARPAbet変換 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | バッチIPA変換 |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | バッチX-SAMPA変換 |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<EnglishPhoneme>>` | バッチ構造化音素リスト変換 |

### ChineseG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPinyin(text)` | `string` | 声調記号付きピンイン文字列 (`"nǐ hǎo"`) |
| `ToPinyin(text, style)` | `string` | 指定スタイルのピンイン文字列 |
| `ToPinyinList(text)` | `string[]` | 各文字ごとのピンイン配列 |
| `ToPinyinList(text, style)` | `string[]` | 指定スタイルの各文字ごとピンイン配列 |
| `ContainsChar(c)` | `bool` | 辞書存在確認 |
| `LookupChar(c)` | `string[]` | 全ピンイン候補取得 |
| `ToIPA(text)` | `string` | IPA（国際音声記号）表記 |
| `ToIPA(text, includeTones)` | `string` | 声調制御付きIPA表記 |
| `ToZhuyin(text)` | `string` | 注音符号（ボポモフォ）表記 |
| `ToZhuyin(text, includeTones)` | `string` | 声調制御付き注音表記 |
| `ToPinyinBatch(texts)` | `string[]` | バッチピンイン変換 |
| `ToPinyinBatch(texts, style)` | `string[]` | バッチピンイン変換（スタイル指定） |
| `ToPinyinListBatch(texts)` | `string[][]` | バッチピンインリスト変換 |
| `ToPinyinListBatch(texts, style)` | `string[][]` | バッチピンインリスト変換（スタイル指定） |
| `ToIPABatch(texts)` | `string[]` | バッチIPA変換 |
| `ToIPABatch(texts, includeTones)` | `string[]` | バッチIPA変換（声調制御） |
| `ToZhuyinBatch(texts)` | `string[]` | バッチ注音変換 |
| `ToZhuyinBatch(texts, includeTones)` | `string[]` | バッチ注音変換（声調制御） |

### KoreanG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | スペース区切りの compatibility Jamo 音素列 (`"ㅈ ㅗ ㅌ ㅏ"`) |
| `ToJamo(text)` | `string` | 音節区切り付き Jamo 列 (`"ㅎㅏㄴ ㄱㅡㄹ"`) |
| `Analyze(text)` | `KoreanPronunciation` | 正規化後テキストと音節/音素モデルを返す |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチ音素変換 |
| `ToJamoBatch(texts)` | `IReadOnlyList<string>` | バッチ Jamo 変換 |

### SpanishG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | スペース区切りIPA音素列 (`"ˈb e ɾ ˈɡ w e n s a"` のような形式) |
| `ToIPA(text)` | `string` | IPA表記 |
| `ToXSampa(text)` | `string` | X-SAMPA表記 |
| `ToXSampaWithoutStress(text)` | `string` | ストレスマークなしX-SAMPA表記 |
| `ToPhonemeList(text)` | `IReadOnlyList<SpanishPhoneme>` | 構造化音素リスト |
| `ToSyllables(word)` | `IReadOnlyList<SpanishSyllable>` | 音節分割結果 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチ音素変換 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | バッチIPA変換 |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | バッチX-SAMPA変換 |

### FrenchG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | スペース区切りIPA音素列 |
| `ToIPA(text)` | `string` | IPA表記（`"bɔ̃ʒuʁ"` のような形式） |
| `ToIPAWithoutStress(text)` | `string` | ストレスマークなしIPA表記 |
| `ToXSampa(text)` | `string` | X-SAMPA表記 |
| `ToXSampaWithoutStress(text)` | `string` | ストレスマークなしX-SAMPA表記 |
| `ToPhonemeList(text)` | `IReadOnlyList<FrenchPhoneme>` | 構造化音素リスト |
| `ToSyllables(word)` | `IReadOnlyList<FrenchPhoneme[]>` | 音節分割結果 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチ音素変換 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | バッチIPA変換 |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | バッチX-SAMPA変換 |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<FrenchPhoneme>>` | バッチ構造化音素リスト変換 |

### PortugueseG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | スペース区切りIPA音素列 |
| `ToIPA(text)` | `string` | IPA表記（`"obɾiˈɡadu"` のような形式） |
| `ToIPAWithoutStress(text)` | `string` | ストレスマークなしIPA表記 |
| `ToXSampa(text)` | `string` | X-SAMPA表記 |
| `ToXSampaWithoutStress(text)` | `string` | ストレスマークなしX-SAMPA表記 |
| `ToPhonemeList(text)` | `IReadOnlyList<PortuguesePhoneme>` | 構造化音素リスト |
| `ToSyllables(text)` | `IReadOnlyList<PortugueseSyllable>` | 音節分割結果 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチ音素変換 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | バッチIPA変換 |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | バッチX-SAMPA変換 |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<PortuguesePhoneme>>` | バッチ構造化音素リスト変換 |

### MultilingualG2PEngine

| メソッド | 戻り値型 | 説明 |
|---------|---------|------|
| `ToPhonemes(text)` | `string` | 日英中韓西仏葡混在音素列 |
| `ToSegments(text)` | `IReadOnlyList<G2PSegment>` | 言語タグ付きセグメント |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | バッチ音素変換 |
| `ToSegmentsBatch(texts)` | `IReadOnlyList<IReadOnlyList<G2PSegment>>` | バッチセグメント変換 |

Multilingual の補足:

- Hangul syllables / Jamo / compatibility jamo / halfwidth Hangul は Korean として分類され、`DotNetG2P.Korean` にルーティングされます
- ラテン文字列は `DefaultLatinLanguage` を既定にしつつ、アクセント付きスペイン語文字、`güe/güi`、高頻度 ASCII Spanish 語彙、代表的な接尾辞で English / Spanish / French / Portuguese を切り替えます。ポルトガル語は特有文字(ã/õ)、ç接尾辞パターン(-ço/-ça)、高頻度語彙で判定します
- 純漢字 run は `Chinese strong/weak markers`、`Japanese markers`、日本語語彙ヒント、埋め込み中国語 phrase/char 辞書を使って JP / ZH を補強判定します
- 埋め込み中国語辞書は `ChineseG2PEngine` と共有され、`TextSegmenter` 単独の追加辞書常駐は実測で約 `0.02MB` です
- それでも根拠が弱い曖昧な純漢字 run だけ `DefaultCjkLanguage` にフォールバックします
- `MultilingualG2POptions.KoreanOptions` で `UiVariationMode` や正規化設定を multilingual 側から渡せます
- 2026-03-12 時点の Multilingual 回帰: `443 passed`（7言語対応）
- `MultilingualPerformanceTests`: `8 passed`
- `MultilingualKoreanPerformanceTests`: `2 passed`

### 日本語音素体系

| 種別 | 音素 |
|------|------|
| 母音 | `a` `i` `u` `e` `o` （無声: `A` `I` `U` `E` `O`） |
| 子音 | `k` `g` `s` `z` `t` `d` `n` `h` `b` `p` `m` `r` `f` `v` |
| 拗音子音 | `ky` `gy` `sh` `j` `ch` `ts` `ny` `hy` `by` `py` `my` `ry` `dy` `ty` `kw` `gw` |
| 半母音 | `y` `w` |
| 特殊 | `N`（撥音） `cl`（促音） `-`（長音） `pau`（ポーズ） |

## 処理パイプライン

DotNetG2Pは[OpenJTalk](https://open-jtalk.sourceforge.net/)と同等の6段階NJD処理パイプラインを実装しています。

```
テキスト入力
  │
  ├─ TextNormalizer        全角/半角正規化、濁点結合
  ├─ ITokenizer.Tokenize   形態素解析（MeCabTokenizer + naist-jdic）
  ├─ SetPronunciation      辞書読み・フォールバック発音生成
  ├─ SetDigit              数字列検出・助数詞読み変換
  ├─ SetAccentPhrase       品詞パターンによるアクセント句結合（18ルール）
  ├─ SetAccentType         チェインルールによるアクセント結合型決定
  └─ SetUnvoicedVowel      無声母音化（6ルール）
  │
  ▼
  出力（音素列 / カタカナ / 韻律記号 / AccentPhrase / HTSラベル / 韻律特徴量）
```

## 辞書の準備

DotNetG2Pは形態素解析にnaist-jdic辞書（OpenJTalk用MeCab辞書）を使用します。

### 推奨手順

```powershell
pwsh -File tools/install_naist_jdic.ps1
```

上記スクリプトは OpenJTalk 配布物から辞書をダウンロードし、既定で `%USERPROFILE%\naist-jdic` に展開します。
`MeCabTokenizer()` と `MultilingualG2PEngine()` は次の順で辞書を自動探索します。

1. 環境変数 `DOTNETG2P_NAIST_JDIC_PATH`
2. 環境変数 `NAIST_JDIC_PATH`
3. `%USERPROFILE%\naist-jdic`
4. カレントディレクトリ配下の `naist-jdic` または `open_jtalk_dic_utf_8-1.11`

### 手動で用意する場合

1. [Open JTalk公式サイト](https://open-jtalk.sourceforge.net/)からダウンロード
2. pyopenjtalkやOpenJTalkに同梱の辞書ディレクトリをそのまま使用

### 必要なファイル

辞書ディレクトリに以下の4ファイルが含まれている必要があります:

| ファイル | 内容 |
|---------|------|
| `sys.dic` | システム辞書 |
| `matrix.bin` | 遷移コスト行列 |
| `char.bin` | 文字カテゴリ定義 |
| `unk.dic` | 未知語テンプレート |

### Unity での配置

Unityでは `StreamingAssets` フォルダに辞書ファイルを配置し、`Application.streamingAssetsPath` を使用してパスを指定します。

```csharp
var dicPath = Path.Combine(Application.streamingAssetsPath, "naist-jdic");
using var tokenizer = new MeCabTokenizer(dicPath);
using var multiEngine = new MultilingualG2PEngine(dicPath);
```

## スペイン語評価

スペイン語G2Pには、`ipa-dict` と `WikiPron` を使った全量評価パイプラインが含まれます。

```powershell
pwsh -File tools/refresh_spanish_eval_data.ps1 -Mode Full
pwsh -File tools/run_spanish_full_evaluation.ps1 -EnforceThresholds
```

- コーパス出力先: `artifacts/spanish-eval/corpora`
- レポート出力先: `artifacts/spanish-eval/reports/latest`
- 主な出力:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

2026-03-09 時点の実測値:

- `ipa_dict_es_es_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_es_full/allophones`: PER `1.37%`, WER `13.69%`
- `ipa_dict_es_mx_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_mx_full/allophones`: PER `1.37%`, WER `13.69%`
- `wikipron_spa_latn_ca_broad_filtered_full/base`: PER `1.38%`, WER `11.14%`
- `wikipron_spa_latn_la_broad_filtered_full/base`: PER `1.43%`, WER `11.46%`

追加の回帰確認（2026-03-10）:

- `SpanishG2P`: `227 passed`
- `SpanishNormalizer` は `1.234` を千区切りとして扱い、不正な日付/時刻は意味展開せず安全にフォールバック

## フランス語評価

フランス語G2Pには、`ipa-dict` と `WikiPron` を使った全量評価パイプラインが含まれます。

```powershell
pwsh -File tools/refresh_french_eval_data.ps1 -Mode Full
pwsh -File tools/run_french_full_evaluation.ps1 -EnforceThresholds
```

- コーパス出力先: `artifacts/french-eval/corpora`
- レポート出力先: `artifacts/french-eval/reports/latest`
- 主な出力:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

PER閾値設定（`tools/french_eval_thresholds.json`）:

- `ipa_dict_fr_fr_sample`: base/allophones PER `< 8%`、no_exceptions PER `< 12%`
- `ipa_dict_fr_fr_full`: base/allophones PER `< 12%`、no_exceptions PER `< 18%`
- `wikipron_fra_latn_broad_filtered_sample`: base PER `< 8%`
- `wikipron_fra_latn_broad_filtered_full`: base PER `< 12%`

テスト実績:

- `FrenchG2P`: `707 passed, 12 skipped`（合計719件）
- `FrenchDatasetEvaluationTests`: 外部TSVコーパスを使ったPER閾値テスト（6件）
- `FrenchAllophoneEvaluationTests`: 異音プロファイル別PER評価（6件）

## ポルトガル語評価

ポルトガル語G2Pはルールベース変換 + 例外辞書（560+エントリ）で構成されています。全量評価パイプラインも含まれます。

```powershell
pwsh -File tools/refresh_portuguese_eval_data.ps1 -Mode Full
pwsh -File tools/run_portuguese_full_evaluation.ps1 -EnforceThresholds
```

- コーパス出力先: `artifacts/portuguese-eval/corpora`
- レポート出力先: `artifacts/portuguese-eval/reports/latest`
- 主な出力:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

PER閾値設定（`tools/portuguese_eval_thresholds.json`）

テスト実績:

- `PortugueseG2P`: `1294 passed, 16 skipped`（合計1310件）
- `PortugueseDatasetEvaluationTests`: 外部TSVコーパスを使ったPER閾値テスト（9件）
- `PortugueseAllophoneEvaluationTests`: 異音プロファイル別PER評価（7件）

### 音素体系

49種のIPA音素を定義（`PortugueseIpaPhoneme` enum）:

| カテゴリ | 数 | 内容 |
|---------|-----|------|
| 口母音 | 9 | /a/, /e/, /ɛ/, /i/, /o/, /ɔ/, /u/, /ɐ/, /ɨ/（EP固有） |
| 鼻母音 | 5 | /ɐ̃/, /ẽ/, /ĩ/, /õ/, /ũ/ |
| 半母音 | 2 | /j/, /w/ |
| 鼻わたり音 | 2 | /w̃/, /j̃/ |
| 子音 | 20 | 破裂6 + 摩擦6 + 鼻音3 + 側面音2 + ロティック2 + 破擦音1（ChはBP異音兼用） |
| 異音 | 11 | BP固有4 + EP固有2 + 共通3 + 弱化2 |

### 方言サポート

| 方言 | enum値 | 説明 |
|------|--------|------|
| Brazilian | `PortugueseDialect.Brazilian`（デフォルト） | サンパウロ/リオ標準。t/d破擦音化、コーダl半母音化 |
| European | `PortugueseDialect.European` | リスボン標準。閉鎖音弱化、歯擦音後部歯茎化、コーダl軟口蓋化 |

### 異音規則

`PortugueseAllophoneFeatures` で個別にON/OFF可能な7種の異音規則:

| 規則 | 説明 | BP既定 | EP既定 |
|------|------|--------|--------|
| `VowelReduction` | 無ストレス母音の弱化 | ON | ON |
| `NasalAssimilation` | 鼻音の調音位置同化 | ON | ON |
| `SibilantVoicingAssimilation` | 歯擦音の有声性同化 | ON | ON |
| `Lenition` | 閉鎖音弱化 [b→β, d→ð, g→ɣ] | OFF | ON |
| `SibilantPalatalization` | 歯擦音の後部歯茎化 [s→ʃ, z→ʒ] | OFF | ON |
| `TDPalatalization` | t/d の破擦音化 [t→tʃ, d→dʒ] + /i/ | ON | OFF |
| `LAllophony` | コーダ /l/ の異音: BP=半母音化[w] / EP=軟口蓋化[ɫ] | ON | ON |

### テキスト正規化

`PortugueseNormalizer` は13段階のパイプラインでテキストを読み上げ形式に展開します:

1. NFKC正規化 + 小文字化
2. 略語展開
3. ISO日付展開
4. 日付展開
5. 時刻展開
6. 通貨展開
7. パーセント展開
8. 単位展開
9. 数値範囲展開
10. 小数展開
11. 独立数値展開
12. 記号展開
13. 空白正規化

## オプション設定

`G2POptions` で各処理段階を個別にON/OFFできます（イミュータブル設計）。

```csharp
// 無声音化のみ無効にする例
var options = new G2POptions(enableUnvoicedVowel: false);
using var engine = new G2PEngine(tokenizer, options);
```

| パラメータ | デフォルト | 説明 |
|-----------|-----------|------|
| `enableTextNormalization` | `true` | テキスト正規化（全角/半角変換） |
| `enableDigitProcessing` | `true` | 数字読み変換・助数詞処理 |
| `enableAccentPhrase` | `true` | アクセント句結合（18ルール） |
| `enableAccentType` | `true` | アクセント結合型決定 |
| `enableUnvoicedVowel` | `true` | 無声母音化（6ルール） |
| `expandLongVowels` | `true` | 長音を母音繰り返しで出力（`false`=`"-"`記号を使用） |

## ビルド

### 要件

- .NET SDK 9.0 以上

### コマンド

```bash
# ビルド
dotnet build DotNetG2P.slnx

# テスト実行
dotnet test DotNetG2P.slnx

# コンソールサンプル（辞書なし: MoraMappingのみ確認）
dotnet run --project samples/DotNetG2P.Console

# 辞書を既定場所にインストール
pwsh -File tools/install_naist_jdic.ps1

# コンソールサンプル（辞書自動解決: フルG2P）
dotnet run --project samples/DotNetG2P.Console

# コンソールサンプル（辞書パスを明示）
dotnet run --project samples/DotNetG2P.Console -- /path/to/naist-jdic
```

## スレッドセーフティ

`G2PEngine` および `MeCabTokenizer` はスレッドセーフではありません。
マルチスレッド環境では、スレッドごとにインスタンスを作成してください。

辞書データ（`DictionaryBundle`）は内部でWeakReferenceキャッシュにより自動的に共有されるため、
複数インスタンスを作成してもメモリ使用量は最小限に抑えられます。

`EnglishG2PEngine`、`ChineseG2PEngine`、`KoreanG2PEngine`、`SpanishG2PEngine`、`FrenchG2PEngine`、`PortugueseG2PEngine` はステートレスな変換を行うため、
単一インスタンスを複数スレッドから呼び出しても安全です。

`MultilingualG2PEngine` は内部の日本語エンジンを `lock` で保護しているため、
複数スレッドから安全に呼び出せます。ただし日本語テキストの変換は直列化されます。

## ライセンス

| パッケージ | ライセンス | 備考 |
|-----------|-----------|------|
| **DotNetG2P** | [Apache-2.0](LICENSE) | コアライブラリ |
| **DotNetG2P.MeCab** | [Apache-2.0](LICENSE) | 独自MeCabエンジン |
| **DotNetG2P.English** | [Apache-2.0](LICENSE) | 英語G2Pエンジン |
| **DotNetG2P.Chinese** | [Apache-2.0](LICENSE) | 中国語G2Pエンジン |
| **DotNetG2P.Korean** | [Apache-2.0](LICENSE) | 韓国語G2Pエンジン |
| **DotNetG2P.Spanish** | [Apache-2.0](LICENSE) | スペイン語G2Pエンジン |
| **DotNetG2P.French** | [Apache-2.0](LICENSE) | フランス語G2Pエンジン |
| **DotNetG2P.Portuguese** | [Apache-2.0](LICENSE) | ポルトガル語G2Pエンジン |
| **DotNetG2P.Multilingual** | [Apache-2.0](LICENSE) | 多言語G2Pエンジン（日英中韓西仏葡対応） |

全コンポーネントが**Apache-2.0ライセンス**で利用可能です。
サードパーティコンポーネントのライセンスについては [NOTICE](NOTICE) ファイルを参照してください。
