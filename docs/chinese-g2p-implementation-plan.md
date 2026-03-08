# DotNetG2P.Chinese 実装計画

> 作成日: 2026-03-07
> ベース: [chinese-g2p-research.md](./chinese-g2p-research.md) の調査結果

---

## 目次

1. [全体方針](#1-全体方針)
2. [パッケージ構造](#2-パッケージ構造)
3. [音韻モデル設計](#3-音韻モデル設計)
4. [辞書データ設計](#4-辞書データ設計)
5. [変換パイプライン](#5-変換パイプライン)
6. [フェーズ別実装計画](#6-フェーズ別実装計画)
7. [変更が必要な既存ファイル](#7-変更が必要な既存ファイル)
8. [ライセンス対応](#8-ライセンス対応)

---

## 1. 全体方針

### アーキテクチャ
- **pypinyin方式**: 辞書ベース + フレーズ最長一致 + ルールベース声調変調
- **DotNetG2P.Englishと同一パターン**: Core参照なし（完全独立パッケージ）
- **ターゲット**: .NET Standard 2.1（Unity 2021.2+互換）
- **辞書データ**: pinyin-data + phrase-pinyin-data（共にMIT）のみ使用。CC-CEDICTは使用しない

### 精度目標
- **Phase 1 (C1-C3)**: pypinyin相当 ~87-90%（With-Tone、CPPベンチマーク）
- **Phase 2 (C4-C6)**: csharp-pinyinアルゴリズム参考で最適化、90%+

---

## 2. パッケージ構造

```
src/DotNetG2P.Chinese/
├── DotNetG2P.Chinese.csproj             # .NET Standard 2.1、依存なし（独立）
├── ChineseG2PEngine.cs                  # メインAPI（sealed, IDisposable）
├── ChineseG2POptions.cs                 # オプション（sealed, immutable）
├── Models/
│   ├── Initial.cs                       # 声母 enum : byte（22種 + None）
│   ├── Final.cs                         # 韻母 enum : byte（36種 + None + Er）
│   ├── Tone.cs                          # 声調 enum : byte（5種）
│   ├── PinyinSyllable.cs               # readonly struct（Initial + Final + Tone）
│   ├── PinyinStyle.cs                   # 出力スタイル enum
│   └── PinyinResult.cs                  # 変換結果クラス
├── Dictionary/
│   ├── PinyinCharDictionary.cs          # 単字辞書（コードポイント→ピンイン候補）
│   ├── PinyinPhraseDictionary.cs        # フレーズ辞書（最長一致検索）
│   └── Data/
│       ├── pinyin_char.txt              # 単字辞書（EmbeddedResource, ~300KB）
│       └── pinyin_phrase.txt            # フレーズ辞書（EmbeddedResource, ~5MB）
├── Conversion/
│   ├── PinyinParser.cs                  # ピンイン文字列→PinyinSyllableパーサ
│   ├── ToneConverter.cs                 # 声調記号⇔数字変換
│   ├── IpaConverter.cs                  # ピンイン→IPA変換（声母/韻母別テーブル）
│   └── ZhuyinConverter.cs              # ピンイン→注音変換
├── ToneSandhi/
│   └── ToneSandhiProcessor.cs           # 声調変調（三声連読、一/不変調）
├── LICENSE.md                           # Apache-2.0
├── THIRD-PARTY-NOTICES.md               # pinyin-data, phrase-pinyin-data (MIT)
├── package.json                         # UPM (com.dotnetg2p.chinese)
└── DotNetG2P.Chinese.asmdef             # Unity Assembly Definition
```

### csproj構成（DotNetG2P.English準拠）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
    <PackageId>DotNetG2P.Chinese</PackageId>
    <AssemblyName>DotNetG2P.Chinese</AssemblyName>
    <RootNamespace>DotNetG2P.Chinese</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Description>中国語（普通話）ピンイン変換ライブラリ。辞書ベースの多音字解決、声調変調、IPA/注音出力対応。</Description>
    <PackageTags>chinese;pinyin;g2p;mandarin;tts;phoneme</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="Dictionary\Data\pinyin_char.txt" LogicalName="pinyin_char.txt" />
    <EmbeddedResource Include="Dictionary\Data\pinyin_phrase.txt" LogicalName="pinyin_phrase.txt" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="DotNetG2P.Tests" />
  </ItemGroup>
</Project>
```

---

## 3. 音韻モデル設計

### 3.1 Initial enum（声母）: 22種 + None

```csharp
public enum Initial : byte
{
    None = 0,  // ゼロ声母
    B, P, M, F,           // 両唇音・唇歯音
    D, T, N, L,           // 歯茎音
    G, K, H,              // 軟口蓋音
    J, Q, X,              // 歯茎硬口蓋音
    Zh, Ch, Sh, R,        // そり舌音
    Z, C, S,              // 歯茎破擦音・摩擦音
    Y, W,                 // 半母音（ピンイン表記用）
}
```

### 3.2 Final enum（韻母）: 36種 + None

```csharp
public enum Final : byte
{
    None = 0,
    // 開口呼: A, O, E, Ai, Ei, Ao, Ou, An, En, Ang, Eng, Ong
    // 齊齒呼: I, Ia, Ie, Iao, Iu, Ian, In, Iang, Ing, Iong
    // 合口呼: U, Ua, Uo, Uai, Ui, Uan, Un, Uang, Ueng
    // 撮口呼: V(ü), Ve(üe), Van(üan), Vn(ün)
    // 特殊:   Er
}
```

- ü系韻母は `V`, `Ve`, `Van`, `Vn` として表現（pypinyin慣例）
- j/q/x後の ü→u 表記変換は `PinyinSyllable.ToString()` で処理

### 3.3 Tone enum（声調）

```csharp
public enum Tone : byte
{
    Neutral = 0,  // 軽声
    First = 1,    // 陰平 ˥ (55)
    Second = 2,   // 陽平 ˧˥ (35)
    Third = 3,    // 上声 ˨˩˦ (214)
    Fourth = 4,   // 去声 ˥˩ (51)
}
```

### 3.4 PinyinSyllable readonly struct

```csharp
public readonly struct PinyinSyllable : IEquatable<PinyinSyllable>
{
    public Initial Initial { get; }   // byte
    public Final Final { get; }       // byte
    public Tone Tone { get; }         // byte
    // 合計3バイト、パディング込み4バイト
}
```

- `ToString()`: ピンイン数字表記（例: "zhong1"）
- `ToToneMarked()`: 声調記号付き（例: "zhōng"）
- IEquatable, GetHashCode（ビットシフト結合）
- j/q/x後のü→u表記変換を内蔵

### 3.5 PinyinStyle enum

```csharp
public enum PinyinStyle : byte
{
    ToneMarked = 0,     // zhōng（声調記号付き）
    ToneNumber = 1,     // zhong1（声調数字末尾）
    ToneNumber2 = 2,    // zho1ng（声調数字母音後）
    Normal = 3,         // zhong（声調なし）
    Initials = 4,       // zh（声母のみ）
    Finals = 5,         // ong（韻母のみ）
    FinalsTone = 6,     // ōng（韻母+声調記号）
    FirstLetter = 7,    // z（頭文字のみ）
    Bopomofo = 8,       // ㄓㄨㄥ（注音符号）
    IPA = 9,            // ʈ͡ʂʊŋ˥˥（国際音声記号）
}
```

---

## 4. 辞書データ設計

### 4.1 データソース

| 辞書 | ソース | ライセンス | エントリ数 |
|------|--------|-----------|-----------|
| 単字辞書 | [pinyin-data](https://github.com/mozillazg/pinyin-data) `pinyin.txt` | MIT | ~42,000字 |
| フレーズ辞書 | [phrase-pinyin-data](https://github.com/mozillazg/phrase-pinyin-data) `large_pinyin.txt` | MIT | ~270,000フレーズ |

### 4.2 埋め込みデータ形式（テキスト、CmuDictionary準拠）

**pinyin_char.txt**（単字辞書、~300KB）:
```
4E00 yī
4E2D zhōng,zhòng
```
- 行形式: `{16進コードポイント} {ピンイン1},{ピンイン2},...`
- 最初のピンインが最優先発音

**pinyin_phrase.txt**（フレーズ辞書、~5MB）:
```
上海	shàng hǎi
三国演义	sān guó yǎn yì
```
- 行形式: `{フレーズ}\t{ピンイン列}`（タブ区切り）

### 4.3 辞書クラス設計

#### PinyinCharDictionary
```csharp
public sealed class PinyinCharDictionary
{
    private readonly Dictionary<int, string[]> _entries; // コードポイント→ピンイン候補

    public static PinyinCharDictionary LoadEmbedded();
    public static PinyinCharDictionary LoadFromFile(string path);
    public bool TryLookup(int codePoint, out string pinyin);       // 最優先
    public bool TryLookupAll(int codePoint, out string[] pinyins); // 全候補
    public int Count { get; }
    internal void Clear();
}
```
- 初期容量50,000で`Dictionary`事前確保
- `ReadOnlySpan<char>`でパース（CmuDictionary同様ゼロアロケーション志向）

#### PinyinPhraseDictionary
```csharp
public sealed class PinyinPhraseDictionary
{
    private readonly Dictionary<string, string[]> _entries; // フレーズ→各文字ピンイン
    private int _maxPhraseLength;  // 最長フレーズの文字数

    public static PinyinPhraseDictionary LoadEmbedded();
    public static PinyinPhraseDictionary LoadFromFile(string path);
    public bool TryLookup(string phrase, out string[] pinyins);
    public int FindLongestMatch(string text, int startIndex, out string[] pinyins);
    public int Count { get; }
    internal void Clear();
}
```

### 4.4 フレーズ最長一致検索アルゴリズム（pypinyin方式）

```
FindLongestMatch(text, startIndex):
  for len = min(maxPhraseLength, text.Length - startIndex) down to 2:
    phrase = text.Substring(startIndex, len)
    if _entries.TryGetValue(phrase, out pinyins):
      return len, pinyins
  return 0, null  // マッチなし→単字辞書にフォールバック
```

- Dictionary<string, string[]> でO(1)検索
- 最大フレーズ長（~8文字）から1文字ずつ縮めて検索
- ~270,000エントリでもDictionary初期化は1秒未満（CmuDictionary 135Kエントリで実証済み）
- メモリ: ~30-40MB（PrefixSet方式と同等だがHashSet操作不要）

### 4.5 辞書変換ツール

```
tools/
  convert_pinyin_data.js   # pinyin.txt → pinyin_char.txt, large_pinyin.txt → pinyin_phrase.txt
```

処理:
1. pinyin.txt: `U+{HEX}: {pinyins}  # {char}` → `{HEX} {pinyins}`
2. large_pinyin.txt: `{phrase}: {pinyins}` → `{phrase}\t{pinyins}`

---

## 5. 変換パイプライン

```
入力テキスト（例: "你好世界"）
  │
  ├─ フレーズ辞書検索（FindLongestMatch）
  │   → "你好" → "nǐ hǎo"
  │   → "世界" → "shì jiè"
  │
  ├─ 単字辞書フォールバック（マッチしない文字）
  │   → コードポイント→最優先ピンイン
  │
  ├─ 非漢字処理
  │   → ASCII英数字: そのまま通過
  │   → 句読点: 区切りとして処理
  │
  ├─ 声調変調（ToneSandhi）
  │   → 三声連読: nǐ hǎo → ní hǎo
  │   → "一"/"不"変調
  │
  └─ スタイル変換（PinyinStyle）
      → ToneMarked: "ní hǎo shì jiè"
      → Normal: "ni hao shi jie"
      → IPA: "ni˧˥ xaʊ˨˩˦ ʂʅ˥˩ t͡ɕiɛ˥˩"
```

### ChineseG2PEngine メインAPI

```csharp
public sealed class ChineseG2PEngine : IDisposable
{
    // コンストラクタ（EnglishG2PEngine準拠）
    public ChineseG2PEngine();
    public ChineseG2PEngine(ChineseG2POptions options);
    public ChineseG2PEngine(string charDictPath, string phraseDictPath);
    public ChineseG2PEngine(string charDictPath, string phraseDictPath, ChineseG2POptions options);

    // 基本API
    public string ToPinyin(string text);                          // 声調記号付き（デフォルト）
    public string ToPinyin(string text, PinyinStyle style);       // スタイル指定
    public string[] ToPinyinList(string text);                    // 文字ごとのピンイン配列
    public string[] ToPinyinList(string text, PinyinStyle style);
    public PinyinSyllable[] ToPinyinSyllables(string text);       // 構造化出力

    // 単語ルックアップ
    public bool ContainsChar(char c);
    public string[] LookupChar(char c);                           // 全候補

    // 変換API
    public string ToIPA(string text);
    public string ToBopomofo(string text);

    // バッチAPI
    public string[] ToPinyinBatch(string[] texts);
    public string[] ToIPABatch(string[] texts);
    public string[] ToBopomofoBatch(string[] texts);

    // IDisposable
    public void Dispose();
}
```

### ChineseG2POptions

```csharp
public sealed class ChineseG2POptions
{
    public static readonly ChineseG2POptions Default = new ChineseG2POptions();

    public PinyinStyle DefaultStyle { get; }       // デフォルト出力スタイル（ToneMarked）
    public bool EnableToneSandhi { get; }           // 声調変調（デフォルト: true）
    public string Separator { get; }                // 音節区切り文字（デフォルト: " "）
    public bool HandleHeteronyms { get; }           // 多音字処理（デフォルト: true）
}
```

---

## 6. フェーズ別実装計画

### C1: 基本ピンイン変換MVP

**目標**: `ToPinyin("你好世界")` → `"nǐ hǎo shì jiè"`

| # | タスク | 詳細 |
|---|--------|------|
| 1 | プロジェクト作成 | csproj, package.json, asmdef, LICENSE.md, slnx登録 |
| 2 | 音韻モデル | Initial, Final, Tone enum + PinyinSyllable struct |
| 3 | PinyinStyle enum | ToneMarked, ToneNumber, Normal の3形式 |
| 4 | 辞書変換ツール | tools/convert_pinyin_data.js（pinyin.txt→pinyin_char.txt） |
| 5 | PinyinCharDictionary | EmbeddedResource読み込み + LoadFromFile + TryLookup |
| 6 | PinyinParser | 声調記号付きピンイン文字列→PinyinSyllable変換 |
| 7 | ToneConverter | 声調記号⇔数字変換、声調除去 |
| 8 | ChineseG2PEngine | 基本API（ToPinyin, ToPinyinList）、単字辞書のみ |
| 9 | ChineseG2POptions | Default設定 |
| 10 | テスト | 基本変換100件 + モデル単体テスト |

**成果物**: 単字辞書ベースのピンイン変換（多音字は最優先読み）

### C2: フレーズ辞書と多音字解決

**目標**: `ToPinyin("重要")` → `"zhòng yào"` (×`chóng yào`)

| # | タスク | 詳細 |
|---|--------|------|
| 1 | 辞書変換ツール拡張 | large_pinyin.txt→pinyin_phrase.txt |
| 2 | PinyinPhraseDictionary | EmbeddedResource読み込み + FindLongestMatch |
| 3 | ChineseG2PEngine改修 | フレーズ辞書→単字辞書のフォールバックパイプライン |
| 4 | 非漢字処理 | ASCII英数字通過、句読点区切り |
| 5 | テスト | 多音字テスト50件 + フレーズ辞書単体テスト |

**成果物**: フレーズ辞書ベースの多音字解決（pypinyin相当精度）

### C3: 声調変調（Tone Sandhi）

**目標**: `ToPinyin("你好")` → `"ní hǎo"`（三声連読）

| # | タスク | 詳細 |
|---|--------|------|
| 1 | ToneSandhiProcessor | 三声連読変調（3声+3声→2声+3声） |
| 2 | "一"変調 | 4声前→2声、1/2/3声前→4声、序数例外、動詞間→軽声 |
| 3 | "不"変調 | 4声前→2声、動詞間→軽声 |
| 4 | ChineseG2PEngine統合 | パイプラインにToneSandhi追加 |
| 5 | テスト | 声調変調30件 |

**成果物**: 自然な声調処理

### C4: 出力形式の充実

**目標**: 10種の出力スタイル + IPA/注音変換

| # | タスク | 詳細 |
|---|--------|------|
| 1 | PinyinStyle拡張 | 残り7スタイル（Initials, Finals, FinalsTone, FirstLetter, ToneNumber2, Bopomofo, IPA） |
| 2 | IpaConverter | 声母/韻母別IPAテーブル + 異音処理（zh/ch/sh/r+i→ʅ, z/c/s+i→ɿ） |
| 3 | ZhuyinConverter | 声母/韻母/声調→注音変換テーブル |
| 4 | ToPinyinSyllables API | 構造化出力 |
| 5 | テスト | 全スタイル検証 |

### C5: テスト・品質保証

**目標**: 高精度の検証

| # | タスク | 詳細 |
|---|--------|------|
| 1 | pypinyin比較テスト | pypinyin出力との比較100文+ |
| 2 | CPPベンチマーク | CPPデータセットでの精度測定 |
| 3 | エッジケース | 句読点、英数字混在、空文字列、繁体字、サロゲートペア |
| 4 | パフォーマンス | 処理速度・メモリ使用量測定 |
| 5 | バッチAPI | ToPinyinBatch, ToIPABatch, ToBopomofoBatch |

### C6: パッケージング・Multilingual統合

**目標**: NuGet + UPM + 日中英混在テキスト対応

| # | タスク | 詳細 |
|---|--------|------|
| 1 | NuGetパッケージ | DotNetG2P.Chinese |
| 2 | UPMパッケージ | com.dotnetg2p.chinese |
| 3 | Language enum | `Chinese = 2` 追加 |
| 4 | ScriptKind | `CJKIdeograph` 新設（漢字を独立分類） |
| 5 | LanguageDetector改修 | CJK漢字→CJKIdeograph分類、文脈ベース日中判定 |
| 6 | TextSegmenter改修 | LangChinese定数追加、FromLangByte拡張 |
| 7 | MultilingualG2PEngine | ChineseG2PEngine統合、ConvertSegmentにChinese分岐 |
| 8 | MultilingualG2POptions | ChineseOptions追加、DefaultCjkLanguage設定 |
| 9 | Multilingual csproj/UPM | Chinese ProjectReference/依存追加 |
| 10 | テスト | 日中英混在テキスト162件+ |

---

## 7. 変更が必要な既存ファイル

### 新規作成

| ファイル | フェーズ |
|---------|---------|
| `src/DotNetG2P.Chinese/` 以下全ファイル | C1-C4 |
| `tests/DotNetG2P.Tests/ChineseG2P/` テストファイル群 | C1-C5 |
| `tools/convert_pinyin_data.js` | C1 |

### 既存ファイル変更

| ファイル | 変更内容 | フェーズ |
|---------|---------|---------|
| `DotNetG2P.slnx` | Chinese csproj追加 | C1 |
| `tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj` | Chinese ProjectReference追加 | C1 |
| `src/DotNetG2P.Multilingual/Language.cs` | `Chinese = 2` 追加 | C6 |
| `src/DotNetG2P.Multilingual/ScriptKind.cs` | `CJKIdeograph` 追加 | C6 |
| `src/DotNetG2P.Multilingual/LanguageDetector.cs` | CJK→CJKIdeograph分類 | C6 |
| `src/DotNetG2P.Multilingual/TextSegmenter.cs` | LangChinese追加 | C6 |
| `src/DotNetG2P.Multilingual/MultilingualG2PEngine.cs` | Chinese統合 | C6 |
| `src/DotNetG2P.Multilingual/MultilingualG2POptions.cs` | ChineseOptions追加 | C6 |
| `src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.csproj` | Chinese参照追加 | C6 |
| `src/DotNetG2P.Multilingual/package.json` | chinese依存追加 | C6 |
| `src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.asmdef` | chinese参照追加 | C6 |
| `src/DotNetG2P.Multilingual/THIRD-PARTY-NOTICES.md` | 中国語辞書帰属表示 | C6 |
| `CLAUDE.md` | プロジェクト構成にChinese追加 | C6 |

---

## 8. ライセンス対応

### THIRD-PARTY-NOTICES.md（DotNetG2P.Chinese用）

```markdown
# Third-Party Notices

## pinyin-data
- Source: https://github.com/mozillazg/pinyin-data
- License: MIT
- Copyright (c) 2016 mozillazg

## phrase-pinyin-data
- Source: https://github.com/mozillazg/phrase-pinyin-data
- License: MIT
- Copyright (c) 2016 mozillazg

## pypinyin (参考実装)
- Source: https://github.com/mozillazg/python-pinyin
- License: MIT
- Copyright (c) 2016 mozillazg
- Note: アーキテクチャとアルゴリズムを参考。コードの直接移植はなし。

## csharp-pinyin (参考実装)
- Source: https://github.com/wolfgitpr/csharp-pinyin
- License: Apache-2.0
- Note: アルゴリズムを参考。辞書データは使用せず独自構築。
```

### ライセンス制約

| データ/コード | ライセンス | 利用方法 |
|-------------|-----------|---------|
| pinyin-data | MIT | 辞書データとして埋め込み（帰属表示必要） |
| phrase-pinyin-data | MIT | 辞書データとして埋め込み（帰属表示必要） |
| pypinyin | MIT | アーキテクチャ参考のみ（コード移植時は帰属表示） |
| csharp-pinyin | Apache-2.0 | アルゴリズム参考のみ（辞書データは使用しない） |
| CC-CEDICT | CC BY-SA 4.0 | **使用しない**（ShareAlike条項によりApache-2.0非互換） |
| Unicode Unihan | Unicode License | 将来のレア漢字補完用（Category A互換） |
