# 09. 既存コードベースのパターン分析

スペイン語 (DotNetG2P.Spanish) とフランス語 (DotNetG2P.French) の実装構造を分析し、ポルトガル語G2P実装のテンプレートとして抽出した。

---

## 1. プロジェクト構成パターン

### 1.1 csproj テンプレート

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
    <PackageId>DotNetG2P.Portuguese</PackageId>
    <AssemblyName>DotNetG2P.Portuguese</AssemblyName>
    <RootNamespace>DotNetG2P.Portuguese</RootNamespace>
    <Description>Portuguese Grapheme-to-Phoneme (G2P) library for .NET and Unity. Rule-based IPA conversion with syllabification, stress assignment, nasal vowels, and Brazilian/European dialect options.</Description>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageTags>g2p;portuguese;tts;phoneme;ipa;text-to-speech;unity</PackageTags>
  </PropertyGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="DotNetG2P.Tests" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="Data\portuguese_exceptions.master.tsv" LogicalName="DotNetG2P.Portuguese.Data.portuguese_exceptions.master.tsv" />
  </ItemGroup>
</Project>
```

**共通パターン**:
- .NET Standard 2.1 (Unity 2021.2+ 互換)
- `Nullable>enable` で null 安全
- `IsPackable>true` で NuGet パッケージ対象
- 例外辞書 TSV を `EmbeddedResource` として埋め込み
- テストプロジェクトに `InternalsVisibleTo` を許可
- Core パッケージへの参照は**持たない**（独立パッケージ）
- `ItemGroup` は `InternalsVisibleTo` と `EmbeddedResource` を別々の `<ItemGroup>` に分離する（French csproj パターンに統一。Spanish csproj では1つにまとめているが、French 方式を推奨）

### 1.2 package.json (UPM)

```json
{
  "name": "com.dotnetg2p.portuguese",
  "displayName": "DotNetG2P.Portuguese",
  "version": "1.3.0",
  "unity": "2021.2",
  "description": "Portuguese G2P library...",
  "keywords": ["g2p", "portuguese", "tts", "phoneme", "ipa"],
  "license": "Apache-2.0",
  "dependencies": {},
  "author": { "name": "ayutaz" },
  "documentationUrl": "https://github.com/ayutaz/dot-net-g2p#readme",
  "repository": {
    "type": "git",
    "url": "https://github.com/ayutaz/dot-net-g2p.git",
    "directory": "src/DotNetG2P.Portuguese"
  }
}
```

### 1.3 asmdef (Unity Assembly Definition)

```json
{
  "name": "DotNetG2P.Portuguese",
  "rootNamespace": "DotNetG2P.Portuguese",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

### 1.4 ソリューション変更 (DotNetG2P.slnx)

新言語追加時に `/src/` フォルダにプロジェクトを追加し、`/tools/` フォルダに評価ツールプロジェクトも追加する:
```xml
<Folder Name="/src/">
  ...
  <Project Path="src/DotNetG2P.Portuguese/DotNetG2P.Portuguese.csproj" />
</Folder>
<Folder Name="/tools/">
  ...
  <Project Path="tools/DotNetG2P.PortugueseEval/DotNetG2P.PortugueseEval.csproj" />
</Folder>
```

---

## 2. Engine クラス設計パターン

### 2.1 クラス構造

```
public sealed class {Lang}G2PEngine : IDisposable
├── private readonly {Lang}G2POptions _options;
├── private int _disposed;  // Dispose状態 (Interlocked)
│
├── ctor() → this({Lang}G2POptions.Default)
├── ctor({Lang}G2POptions options)
│
├── ToPhonemes(string) → string          // スペース区切り音素列
├── ToIPA(string) → string               // IPA表記
├── ToIPAWithoutStress(string) → string  // ストレスマークなしIPA
├── ToXSampa(string) → string            // X-SAMPA表記
├── ToXSampaWithoutStress(string) → string
├── ToPhonemeList(string) → IReadOnlyList<{Lang}Phoneme>
├── ToSyllables(string) → IReadOnlyList<...>
│
├── ToPhonemesBatch(IReadOnlyList<string>) → IReadOnlyList<string>
├── ToIPABatch(IReadOnlyList<string>) → IReadOnlyList<string>
├── ToPhonemeListBatch(IReadOnlyList<string>) → IReadOnlyList<IReadOnlyList<{Lang}Phoneme>>
├── ToXSampaBatch(IReadOnlyList<string>) → IReadOnlyList<string>
│
├── Dispose()  // Interlocked.CompareExchange(ref _disposed, 1, 0)
│
├── [private] ProcessText(string, Func<{Lang}Pronunciation, string>) → string
├── [private] GetWords(string) → IReadOnlyList<string>
├── [private] Normalize(string) → string
├── [private] ApplyAllophonesIfNeeded(...) → {Lang}Pronunciation
└── [private] ThrowIfDisposed()  // Volatile.Read(ref _disposed) != 0
```

### 2.2 Dispose パターン (統一)

```csharp
private int _disposed;

public void Dispose()
{
    Interlocked.CompareExchange(ref _disposed, 1, 0);
}

private void ThrowIfDisposed()
{
    if (Volatile.Read(ref _disposed) != 0)
        throw new ObjectDisposedException(nameof(PortugueseG2PEngine));
}
```

**注意**: 上記は単言語エンジン（Spanish/French/Portuguese）用のパターンで、リソースを持たないため `CompareExchange` の戻り値チェックは不要。`MultilingualG2PEngine` は子エンジンの Dispose が必要なため異なるパターンを使用する（`MultilingualG2PEngine.cs:212-220`）:

```csharp
// MultilingualG2PEngine の Dispose パターン（子リソース解放あり）
public void Dispose()
{
    if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        return;  // 戻り値チェックで二重 Dispose を防止
    _japaneseEngine.Dispose();
    _englishEngine.Dispose();
    // ... 各子エンジンの Dispose
}
```

ポルトガル語エンジンは単言語エンジンなので、最初のパターン（戻り値チェックなし）を使用する。

### 2.3 ProcessText パターン

Spanish と French で ProcessText 内での異音（allophone）適用パターンが異なる。

**Spanish 方式**: ProcessText 内では素の `pronunciation` を `formatter` に渡し、各 public メソッドのラムダ式内で `ApplyAllophonesIfNeeded` を呼ぶ。

```csharp
// SpanishG2PEngine.cs:146-165
private string ProcessText(string text, Func<SpanishPronunciation, string> formatter)
{
    ThrowIfDisposed();
    var words = GetWords(text);
    if (words.Count == 0) return string.Empty;

    var builder = new StringBuilder(text.Length + 8);
    for (var i = 0; i < words.Count; i++)
    {
        if (i > 0) builder.Append(' ');
        var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
        builder.Append(formatter(pronunciation));  // formatter 内で ApplyAllophonesIfNeeded を呼ぶ
    }
    return builder.ToString();
}

// 使用例: pronunciation => IpaConverter.Convert(ApplyAllophonesIfNeeded(pronunciation), ...)
```

**French 方式**: ProcessText 内で直接 `AllophoneProcessor.Apply` を呼び、変換済みの `pronunciation` を `formatter` に渡す。

```csharp
// FrenchG2PEngine.cs:168-189
private string ProcessText(string text, Func<FrenchPronunciation, string> formatter)
{
    ThrowIfDisposed();
    var words = GetWords(text);
    if (words.Count == 0) return string.Empty;

    var builder = new StringBuilder(text.Length + 8);
    for (var i = 0; i < words.Count; i++)
    {
        if (i > 0) builder.Append(' ');
        var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
        if (_options.EnableAllophones)
            pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);
        builder.Append(formatter(pronunciation));  // 変換済み pronunciation を渡す
    }
    return builder.ToString();
}

// 使用例: pronunciation => IpaConverter.Convert(pronunciation, ...)
```

**ポルトガル語での推奨**: French 方式を推奨する。理由: (1) ProcessText 内で一箇所にまとめた方が allophone 適用の漏れを防げる、(2) ラムダ式が簡潔になる、(3) ToPhonemeList 等の非 ProcessText メソッドでも同じパターンで適用できる。ただし Spanish 方式でも機能的には等価であり、どちらを選択しても問題はない。

### 2.4 GetWords/Normalize パターン

```csharp
private IReadOnlyList<string> GetWords(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
    return {Lang}Normalizer.Tokenize(Normalize(text));
}

private string Normalize(string text)
{
    if (_options.EnableTextNormalization)
        return {Lang}Normalizer.Normalize(text);
    return text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
}
```

**Tokenize / TokenizeNormalized の選択**:
- **Spanish 方式**: `SpanishNormalizer.Tokenize(Normalize(text))` — `Tokenize` は公開メソッドで、内部で `Normalize` を呼ばない。`GetWords` 内で先に `Normalize` を呼んでから `Tokenize` に渡す。
- **French 方式**: `FrenchNormalizer.TokenizeNormalized(Normalize(text))` — `Tokenize` は public メソッドだが内部で `Normalize` を呼ぶため、`GetWords` 内では `TokenizeNormalized`（internal メソッド、正規化済みテキストを受け取る）を使用して二重正規化を避ける。
- **ポルトガル語での推奨**: Spanish 方式（`Tokenize` が正規化済みテキストを受け取る設計）が単純でよい。もし `Tokenize` 内で `Normalize` を呼ぶ設計にする場合は、French のように `TokenizeNormalized` を internal メソッドとして分離し、`GetWords` から呼ぶこと。

---

## 3. Options クラス設計パターン

```csharp
public sealed class {Lang}G2POptions
{
    public static readonly {Lang}G2POptions Default = new {Lang}G2POptions();

    public {Lang}Dialect Dialect { get; }             // 方言enum
    public bool IncludeStress { get; }                // ストレスマーク出力 (Spanish=true, French=false)
    public bool EnableAllophones { get; }             // 異音処理ON/OFF
    public bool EnableTextNormalization { get; }      // テキスト正規化ON/OFF
    public bool EnableExceptionDictionary { get; }    // 例外辞書ON/OFF
    public string Separator { get; }                  // 音素区切り (default=" ")
    public {Lang}AllophoneFeatures AllophoneFeatures { get; }  // [Flags] enum

    public {Lang}G2POptions(
        {Lang}Dialect dialect = {default},
        bool includeStress = {true/false},
        bool enableAllophones = false,
        bool enableTextNormalization = true,
        bool enableExceptionDictionary = true,
        string separator = " ",
        {Lang}AllophoneFeatures allophoneFeatures = {Lang}AllophoneFeatures.Default)
    { ... }
}
```

**ポルトガル語向け注意点**:
- `PortugueseDialect`: `Brazilian = 0`, `European = 1`
- `IncludeStress`: デフォルト `true`（ポルトガル語は語強勢が重要）
- Dialect の値 `0` がコンストラクタのデフォルト引数 `dialect = default` で使用されるため、最も一般的な方言を `= 0` に設定する（Spanish は `LatinAmerican = 0`、French は `Metropolitan = 0`、ポルトガル語は `Brazilian = 0`）

---

## 4. Models ディレクトリ設計パターン

### 4.1 IPA音素 enum

```csharp
public enum {Lang}IpaPhoneme : byte
{
    // 母音 (値0から開始、IsSyllabicVowel判定に使用)
    A = 0, E = 1, ...
    // 半母音
    J = ..., W = ...,
    // 子音 (閉鎖→摩擦→破擦→鼻音→側面→ふるえ/はじき)
    P = ..., B = ..., ...
    // 異音 (最後尾)
    Beta = ..., Dh = ..., ...
}
```

**パターン**: `byte` 基底型、母音を先頭に配置して `Phoneme <= LastVowelValue` で母音判定可能にする。

- Spanish: 35種 (母音5+半母音2+子音14+方言音1+異音13)
- French: 40種 (口母音12+鼻母音4+半母音3+子音17+異音4)
- **Portuguese**: ポルトガル語は口母音(7-12)+鼻母音(5)+半母音(2-3)+子音(19-21)+異音(5-10) = 推定40-50種

### 4.2 Phoneme readonly struct

```csharp
public readonly struct {Lang}Phoneme : IEquatable<{Lang}Phoneme>
{
    public {Lang}IpaPhoneme Phoneme { get; }
    public bool IsStressed { get; }  // Spanish
    // or
    public bool IsSyllableNucleus { get; }  // French

    // IsSyllabicVowel, IsSemivowel (AggressiveInlining)
    // ToString → IpaConverter.ToSymbol
    // Equals, GetHashCode, ==, !=
}
```

**ポルトガル語向け**: `IsStressed` (Spanish方式) が適切。鼻母音判定も追加推奨 (`IsNasalVowel` like French)。

### 4.3 Pronunciation class

```csharp
public sealed class {Lang}Pronunciation
{
    internal {Lang}Phoneme[] PhonemesInternal { get; }
    internal int[] SyllableOffsetsInternal { get; }
    public IReadOnlyList<{Lang}Phoneme> Phonemes => PhonemesInternal;
    public int StressedSyllableIndex { get; }

    internal {Lang}Pronunciation({Lang}Phoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex)
    { ... }
}
```

### 4.4 Dialect enum

```csharp
public enum {Lang}Dialect : byte
{
    // Spanish: LatinAmerican=0, Castilian=1
    // French: Metropolitan=0, Conservative=1
    // Portuguese: Brazilian=0, European=1
}
```

### 4.5 Syllable struct (Spanish のみ)

```csharp
public readonly struct {Lang}Syllable : IEquatable<{Lang}Syllable>
{
    public int StartIndex { get; }
    public int Length { get; }
    public string Text { get; }
    public bool IsStressed { get; }
}
```

French は音素ベース音節分割のため `FrenchPhoneme[]` を返す。ポルトガル語はSpanish方式の正書法ベース音節が適切。

---

## 5. Rules ディレクトリ設計パターン

### 5.1 ファイル構成

| ファイル | 役割 | Spanish | French | Portuguese |
|---------|------|---------|--------|------------|
| GraphemeToPhonemeRules.cs | コアG2Pルール | 3フェーズ (ダイグラフ→文脈→単純) | 6フェーズ | 4-5フェーズ (ダイグラフ→文脈→鼻母音→位置→黙字) |
| {Lang}Syllabifier.cs | 音節分割 | 正書法ベース (onset maximization) | 音素ベース | 正書法ベース (Spanish方式) |
| StressAssigner.cs | ストレス位置 | アクセント記号 or デフォルトルール | 最終音節固定 | アクセント記号 or デフォルトルール (Spanish類似) |
| {Lang}Orthography.cs | 正書法ヘルパー | 母音判定/二重母音/三重母音 | 母音・子音分類 | 母音判定/二重母音/三重母音/ティルデ |
| AllophoneProcessor.cs | 異音規則 | 弱化/鼻音同化/s有声化等 | R無声化/有声性同化 | 弱化/鼻音同化/母音弱化等 |
| NasalVowelizer.cs | 鼻母音化 | N/A | あり | **あり** (ポルトガル語は鼻母音が重要) |

### 5.2 GraphemeToPhonemeRules パターン

```csharp
internal static class GraphemeToPhonemeRules
{
    public static {Lang}Pronunciation ConvertWord(string word, {Lang}Dialect dialect, bool enableExceptionDictionary = true)
    {
        // 1. 空チェック
        // 2. 例外辞書ルックアップ
        // 3. 音節分割 + ストレス割当
        // 4. 各音節の音素変換
        // 5. Pronunciation 構築
    }

    private static void AppendSyllable(...)  // 音節→音素変換
    private static void AppendConsonants(...)  // 子音変換 (switch文ベース)
    private static void AppendVowelGroup(...)  // 母音群変換 (二重母音/三重母音判定)
}
```

### 5.3 Syllabifier パターン (Spanish方式)

```csharp
internal static class {Lang}Syllabifier
{
    public static IReadOnlyList<{Lang}Syllable> Syllabify(string word)
    {
        // 1. 次の母音を探索
        // 2. 母音核の終端を決定（二重母音/三重母音考慮）
        // 3. 子音クラスタの分割位置を onset maximization で決定
        // 4. 音節リスト構築
    }

    private static int GetCodaLength(...)  // コーダ長計算
    private static int GetOnsetLength(...)  // 有効オンセット長計算
    private static bool IsValidConsonantClusterOnset(...)  // 有効子音クラスタ判定
}
```

### 5.4 StressAssigner パターン

```csharp
internal static class StressAssigner
{
    public static int GetStressedSyllableIndex(string word, IReadOnlyList<{Lang}Syllable> syllables)
    {
        // 1. アクセント記号のある音節を探索
        // 2. なければデフォルトルール（末尾文字で判定）
    }

    public static IReadOnlyList<{Lang}Syllable> MarkStress(string word, IReadOnlyList<{Lang}Syllable> syllables)
    {
        // stressed フラグ付き音節リストを返す
    }
}
```

**ポルトガル語のストレスルール**: スペイン語とほぼ同じ（母音/n/s/m末尾→後ろから2番目、それ以外→最終音節）

### 5.5 AllophoneProcessor パターン

```csharp
internal static class AllophoneProcessor
{
    public static {Lang}Pronunciation Apply({Lang}Pronunciation pronunciation, {Lang}AllophoneFeatures features)
    {
        // 各音素を順に走査し、features フラグに応じた変換を適用
        // 前後の音素コンテキスト参照
    }
}
```

### 5.6 AllophoneFeatures [Flags] enum パターン

```csharp
[Flags]
public enum {Lang}AllophoneFeatures : byte
{
    None = 0,
    Rule1 = 1 << 0,
    Rule2 = 1 << 1,
    ...
    Obligatory = Rule1 | Rule2,
    Default = Obligatory | ...,
    All = Default | ...,
}
```

---

## 6. Normalization 設計パターン

### 6.1 ファイル構成

| ファイル | 役割 |
|---------|------|
| {Lang}Normalizer.cs | メイン正規化パイプライン + Tokenize |
| NumberToWords.cs | 数値→文字列変換 |

### 6.2 Normalizer パターン

```csharp
internal static class {Lang}Normalizer
{
    public static string Normalize(string text)
    {
        // NormalizationForm.FormKC → ToLowerInvariant
        // → 略語展開 → 日付展開 → 時刻展開
        // → パーセント展開 → 通貨展開 → 単位展開
        // → 数値範囲展開 → 小数展開 → 数値展開
        // → 記号展開 → 文字フィルタリング
    }

    public static IReadOnlyList<string> Tokenize(string text)
    {
        // スペース分割
    }
}
```

**共通特徴**:
- `static class` (stateless)
- Regex ベースのパターンマッチ（略語・日付・時刻等）
- 言語固有の月名・単位・略語辞書
- 通貨: `$`/`€` の展開
- 数値: 桁区切り (`.`/`,`) の解釈

**ポルトガル語の追加要件**:
- 序数接尾辞 (1o/1a → primeiro/primeira)
- ポルトガル語固有の略語 (Sr./Sra./Dr./Dra./etc.)
- ブラジル vs ヨーロッパの桁区切り規則

---

## 7. Conversion 設計パターン

### 7.1 IpaConverter

```csharp
internal static class IpaConverter
{
    public static string Convert({Lang}Pronunciation pronunciation, bool includeStress)
    {
        // 音節ごとにイテレート
        // ストレスマーク 'ˈ' を挿入（includeStress && stressed syllable）
        // ToSymbol で音素→IPA文字列変換
    }

    public static string ConvertPhonemeSequence({Lang}Pronunciation pronunciation, bool includeStress, string separator)
    {
        // 区切り文字付きの音素列出力
    }

    public static string ToSymbol({Lang}IpaPhoneme phoneme)
    {
        // switch文で enum → IPA Unicode文字列マッピング
    }
}
```

### 7.2 XSampaConverter

```csharp
internal static class XSampaConverter
{
    public static string Convert({Lang}Pronunciation pronunciation, bool includeStress)
    {
        // IpaConverter と同構造、ストレスマーク '"' (double quote)
    }

    public static string ToSymbol({Lang}IpaPhoneme phoneme)
    {
        // switch文で enum → X-SAMPA ASCII文字列マッピング
    }
}
```

---

## 8. Data (例外辞書) 設計パターン

### 8.1 ExceptionDictionary

```csharp
internal static class {Lang}ExceptionDictionary
{
    private static readonly Dictionary<string, Dictionary<byte, {Lang}Pronunciation>> s_entries = LoadEntries();

    public static bool TryLookup(string word, {Lang}Dialect dialect, out {Lang}Pronunciation pronunciation)
    {
        // 方言固有エントリ → any-dialect エントリの順で検索
    }

    private static ... LoadEntries()
    {
        // Assembly.GetManifestResourceStream でTSV読み込み
        // TSVフォーマット: surface\tdialect\tcategory\tstress_index\tphonemes\tnotes
    }
}
```

### 8.2 TSV フォーマット

```
surface	dialect	category	stress_index	phonemes	notes
word	*	foreign	0	p a|l a|b ɾ a	コメント
word	la	irregular	1	...	LatinAmerican向け
```

---

## 9. Multilingual 統合パターン

新言語追加時に変更が必要なファイル一覧:

### 9.1 Language.cs

```csharp
public enum Language : byte
{
    Japanese = 0,
    English = 1,
    Chinese = 2,
    Spanish = 3,
    French = 4,
    Portuguese = 5,  // 追加
}
```

### 9.2 MultilingualG2POptions.cs

```csharp
public sealed class MultilingualG2POptions
{
    public PortugueseG2POptions? PortugueseOptions { get; }  // 追加

    public MultilingualG2POptions(
        ...,
        PortugueseG2POptions? portugueseOptions = null)  // 追加
    {
        PortugueseOptions = portugueseOptions;  // 追加

        // DefaultLatinLanguage のバリデーションに Language.Portuguese を追加する。
        // 現行コード (MultilingualG2POptions.cs:62-63):
        //   if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish && defaultLatinLanguage != Language.French)
        //       throw new ArgumentOutOfRangeException(...);
        // → Language.Portuguese を条件に追加:
        //   if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish
        //       && defaultLatinLanguage != Language.French && defaultLatinLanguage != Language.Portuguese)
    }
}
```

**重要**: `DefaultLatinLanguage` のバリデーションは `MultilingualG2POptions.cs` と `TextSegmenter.cs` の**2箇所**に存在する（後者については 9.4 を参照）。両方にポルトガル語を追加しないと、`DefaultLatinLanguage = Language.Portuguese` 指定時に `ArgumentOutOfRangeException` がスローされる。

### 9.3 MultilingualG2PEngine.cs

```csharp
public sealed class MultilingualG2PEngine : IDisposable
{
    private readonly PortugueseG2PEngine _portugueseEngine;  // 追加

    // コンストラクタ: new PortugueseG2PEngine(options.PortugueseOptions ?? PortugueseG2POptions.Default)
    // Dispose: _portugueseEngine.Dispose()
    // ConvertSegment: case Language.Portuguese: return _portugueseEngine.ToPhonemes(segment.Text)
}
```

### 9.4 TextSegmenter.cs

ポルトガル語追加時に TextSegmenter.cs に必要な変更は以下の通り:

#### 9.4.1 定数・シグナル配列の追加

```csharp
// byte エンコーディング定数に追加 (TextSegmenter.cs:17-22 付近)
private const byte LangPortuguese = 6;  // Language.Portuguese

// 言語判定シグナル配列を追加
private static readonly string[] s_portugueseWordSignals = { ... };  // 高頻度語
private static readonly string[] s_portugueseSuffixSignals = { ... };  // 特有接尾辞
```

#### 9.4.2 DefaultLatinLanguage バリデーションの更新

```csharp
// TextSegmenter.cs:111-112 のバリデーションに Language.Portuguese を追加:
if (defaultLatinLanguage != Language.English && defaultLatinLanguage != Language.Spanish
    && defaultLatinLanguage != Language.French && defaultLatinLanguage != Language.Portuguese)
    throw new ArgumentOutOfRangeException(...);
```

**重要**: このバリデーションは `MultilingualG2POptions.cs:62-63` にも同一の条件がある（9.2 参照）。両方を更新すること。

#### 9.4.3 defaultLatinByte 変換の更新

```csharp
// TextSegmenter.cs:146-148 の defaultLatinByte 変換にポルトガル語を追加:
byte defaultLatinByte = defaultLatinLanguage == Language.Spanish ? LangSpanish
                       : defaultLatinLanguage == Language.French ? LangFrench
                       : defaultLatinLanguage == Language.Portuguese ? LangPortuguese
                       : LangEnglish;
```

#### 9.4.4 FromLangByte メソッドの更新

```csharp
// TextSegmenter.cs:438-449 の FromLangByte メソッドにポルトガル語の case を追加:
private static Language FromLangByte(byte b)
{
    switch (b)
    {
        case LangJapanese: return Language.Japanese;
        case LangChinese: return Language.Chinese;
        case LangSpanish: return Language.Spanish;
        case LangFrench: return Language.French;
        case LangPortuguese: return Language.Portuguese;  // 追加
        default: return Language.English;
    }
}
```

#### 9.4.5 IsLatinLanguage メソッドの更新

```csharp
// TextSegmenter.cs:456-459 の IsLatinLanguage メソッドにポルトガル語を追加:
private static bool IsLatinLanguage(byte language)
{
    return language == LangEnglish || language == LangSpanish || language == LangFrench || language == LangPortuguese;
}
```

このメソッドはアポストロフィ・ハイフンのセグメント結合判定（TextSegmenter.cs:267-278）で使用される。ポルトガル語を追加しないと、"l'enfant" のようなポルトガル語内のアポストロフィがセグメント分割されてしまう。

#### 9.4.6 ResolveLatinLanguage メソッドの更新

現行の `ResolveLatinLanguage`（TextSegmenter.cs:461-489）は以下の構造:

```csharp
private static byte ResolveLatinLanguage(string text, int start, int length, byte defaultLatinByte, bool hasLatinExtended)
{
    // 1. defaultLatinByte が Spanish/French なら即リターン
    if (defaultLatinByte == LangSpanish) return LangSpanish;
    if (defaultLatinByte == LangFrench) return LangFrench;

    // 2. 文字ベース判定（フランス語特有文字 → スペイン語特有文字）
    // 3. é のみ → フランス語フォールバック
    // 4. ASCII パターンベース判定
    // 5. defaultLatinByte を返す
}
```

ポルトガル語追加時の変更:

1. **早期リターン分岐の追加**: `if (defaultLatinByte == LangPortuguese) return LangPortuguese;`
2. **ポルトガル語特有文字判定の追加**: `ContainsExplicitPortugueseCharacter` メソッドを追加し、ã (U+00E3)、õ (U+00F5) などポルトガル語固有のティルデ付き母音を判定する。ただし ç はフランス語でも使用されるため、ç 単独ではポルトガル語と断定できない（`ContainsExplicitFrenchCharacter` で既に ç をフランス語マーカーとして使用している。ã/õ はフランス語では使わないため、これらが存在すればポルトガル語と判定可能）
3. **ASCII パターン判定の追加**: `LooksLikePortugueseAsciiToken` メソッドを追加
4. **判定優先順序**: フランス語特有文字 → ポルトガル語特有文字（ã/õ）→ スペイン語特有文字 → é のみ → ASCII パターン（仏→葡→西の順）の順が推奨

### 9.5 DotNetG2P.Multilingual.csproj

```xml
<ProjectReference Include="..\DotNetG2P.Portuguese\DotNetG2P.Portuguese.csproj" />
```

### 9.6 ScriptKind / LanguageDetector

変更不要（ポルトガル語はラテン文字+ラテン拡張で既にカバー）。ただしポルトガル語固有のダイアクリティカル (ã, õ, ç 等) はラテン拡張範囲 (U+00C0-U+024F) に含まれるため `ScriptKind.Latin` として判定される。

**ç の曖昧性に関する注意**: `TextSegmenter.cs:607-609` の `ContainsExplicitFrenchCharacter` では ç (U+00E7) / Ç (U+00C7) をフランス語マーカーとして使用している。ポルトガル語も ç を使うため、ç 単独ではフランス語・ポルトガル語を区別できない。言語判定ロジックでは ã (U+00E3) / õ (U+00F5) をポルトガル語固有のマーカーとして使用し、ç は共有文字として扱う設計が推奨される。

---

## 10. テスト構造パターン

### 10.1 テストファイル一覧

スペイン語テスト (18ファイル):
```
tests/DotNetG2P.Tests/SpanishG2P/
├── SpanishG2PEngineTests.cs            # エンジン統合テスト
├── GraphemeToPhonemeRulesTests.cs       # G2Pルール単体テスト
├── SpanishSyllabifierTests.cs          # 音節分割テスト
├── StressAssignerTests.cs              # ストレステスト
├── SpanishIpaTests.cs                  # IPA変換テスト
├── SpanishPhonemeTests.cs              # 音素モデルテスト
├── SpanishOrthographyTests.cs          # 正書法テスト
├── SpanishNormalizerTests.cs           # 正規化テスト
├── NumberToWordsTests.cs               # 数値変換テスト
├── AllophoneProcessorTests.cs          # 異音テスト
├── SpanishExceptionDictionaryTests.cs  # 例外辞書テスト
├── SpanishExceptionDictionaryMetadataTests.cs
├── SpanishXSampaTests.cs              # X-SAMPA変換テスト
├── SpanishEdgeCaseTests.cs            # エッジケーステスト
├── SpanishPerformanceTests.cs         # パフォーマンステスト
├── SpanishAccuracyTests.cs            # 精度・回帰テスト
├── SpanishDatasetEvaluationTests.cs   # 外部データセットPER閾値テスト
└── SpanishAllophoneEvaluationTests.cs # 異音プロファイル別PER評価
```

フランス語テスト (17ファイル):
```
tests/DotNetG2P.Tests/FrenchG2P/
├── FrenchG2PEngineTests.cs
├── GraphemeToPhonemeRulesTests.cs
├── FrenchSyllabifierTests.cs
├── FrenchIpaTests.cs
├── FrenchPhonemeTests.cs
├── FrenchOrthographyTests.cs
├── FrenchNormalizerTests.cs
├── FrenchNumberToWordsTests.cs
├── AllophoneProcessorTests.cs
├── FrenchExceptionDictionaryTests.cs
├── NasalVowelizerTests.cs             # French固有
├── FrenchXSampaTests.cs
├── FrenchEdgeCaseTests.cs
├── FrenchPerformanceTests.cs
├── FrenchAccuracyTests.cs
├── FrenchDatasetEvaluationTests.cs
└── FrenchAllophoneEvaluationTests.cs
```

Multilingual テスト (15ファイル):
```
tests/DotNetG2P.Tests/Multilingual/
├── MultilingualPortugueseTests.cs     # ポルトガル語統合テスト (新規)
├── MultilingualMixedLanguageTests.cs  # 6言語混在テストに拡張
└── ...
```

### 10.2 ポルトガル語テスト計画

```
tests/DotNetG2P.Tests/PortugueseG2P/
├── PortugueseG2PEngineTests.cs         # エンジン統合テスト
├── GraphemeToPhonemeRulesTests.cs       # G2Pルール単体テスト
├── PortugueseSyllabifierTests.cs       # 音節分割テスト
├── StressAssignerTests.cs              # ストレステスト
├── PortugueseIpaTests.cs              # IPA変換テスト
├── PortuguesePhonemeTests.cs          # 音素モデルテスト
├── PortugueseOrthographyTests.cs      # 正書法テスト
├── PortugueseNormalizerTests.cs       # 正規化テスト
├── NumberToWordsTests.cs              # 数値変換テスト
├── AllophoneProcessorTests.cs         # 異音テスト
├── NasalVowelizerTests.cs            # 鼻母音化テスト (Portuguese固有)
├── PortugueseExceptionDictionaryTests.cs # 例外辞書テスト
├── PortugueseXSampaTests.cs          # X-SAMPA変換テスト
├── PortugueseEdgeCaseTests.cs        # エッジケーステスト
├── PortuguesePerformanceTests.cs     # パフォーマンステスト
├── PortugueseAccuracyTests.cs        # 精度・回帰テスト
├── PortugueseDatasetEvaluationTests.cs # 外部データセットPER閾値テスト
└── PortugueseAllophoneEvaluationTests.cs # 異音プロファイル別PER評価
```

---

## 11. 評価ツール構成パターン

```
tools/
├── DotNetG2P.PortugueseEval/             # 全量精度評価コンソール
│   └── DotNetG2P.PortugueseEval.csproj
├── refresh_portuguese_eval_data.ps1       # 評価データ取得スクリプト
├── run_portuguese_full_evaluation.ps1     # 全量PER/WER評価スクリプト
└── generate_portuguese_exceptions.ps1     # 例外辞書生成スクリプト
```

---

## 12. ポルトガル語G2P 作成ファイル一覧

### 12.1 src/DotNetG2P.Portuguese/ (メインパッケージ)

```
src/DotNetG2P.Portuguese/
├── DotNetG2P.Portuguese.csproj
├── DotNetG2P.Portuguese.asmdef
├── package.json
├── PortugueseG2PEngine.cs
├── PortugueseG2POptions.cs
├── PortugueseAllophoneFeatures.cs
├── Models/
│   ├── PortugueseIpaPhoneme.cs
│   ├── PortuguesePhoneme.cs
│   ├── PortuguesePronunciation.cs
│   ├── PortugueseDialect.cs
│   └── PortugueseSyllable.cs
├── Rules/
│   ├── GraphemeToPhonemeRules.cs
│   ├── PortugueseSyllabifier.cs
│   ├── StressAssigner.cs
│   ├── PortugueseOrthography.cs
│   ├── AllophoneProcessor.cs
│   └── NasalVowelizer.cs
├── Normalization/
│   ├── PortugueseNormalizer.cs
│   └── NumberToWords.cs
├── Conversion/
│   ├── IpaConverter.cs
│   └── XSampaConverter.cs
└── Data/
    ├── PortugueseExceptionDictionary.cs
    └── portuguese_exceptions.master.tsv
```

計: **22 ファイル**

### 12.2 テストファイル

```
tests/DotNetG2P.Tests/PortugueseG2P/
├── PortugueseG2PEngineTests.cs
├── GraphemeToPhonemeRulesTests.cs
├── PortugueseSyllabifierTests.cs
├── StressAssignerTests.cs
├── PortugueseIpaTests.cs
├── PortuguesePhonemeTests.cs
├── PortugueseOrthographyTests.cs
├── PortugueseNormalizerTests.cs
├── NumberToWordsTests.cs
├── AllophoneProcessorTests.cs
├── NasalVowelizerTests.cs
├── PortugueseExceptionDictionaryTests.cs
├── PortugueseXSampaTests.cs
├── PortugueseEdgeCaseTests.cs
├── PortuguesePerformanceTests.cs
├── PortugueseAccuracyTests.cs
├── PortugueseDatasetEvaluationTests.cs
└── PortugueseAllophoneEvaluationTests.cs
```

計: **18 テストファイル**

### 12.3 Multilingual 変更ファイル

```
src/DotNetG2P.Multilingual/
├── Language.cs                    # Portuguese 追加
├── MultilingualG2POptions.cs      # PortugueseOptions 追加
├── MultilingualG2PEngine.cs       # _portugueseEngine 追加
├── TextSegmenter.cs               # ポルトガル語判定シグナル追加
└── DotNetG2P.Multilingual.csproj  # ProjectReference 追加
```

計: **5 ファイル変更**

### 12.4 ソリューション / インフラ変更

```
DotNetG2P.slnx                    # Portuguese プロジェクト追加
tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj  # Portuguese ProjectReference 追加 (不要: InternalsVisibleTo のみ)
tests/DotNetG2P.Tests/Multilingual/
├── MultilingualPortugueseTests.cs  # 新規
└── MultilingualMixedLanguageTests.cs  # 6言語混在テストに拡張
```

### 12.5 評価ツール

```
tools/
├── DotNetG2P.PortugueseEval/
│   └── DotNetG2P.PortugueseEval.csproj
├── refresh_portuguese_eval_data.ps1
├── run_portuguese_full_evaluation.ps1
└── generate_portuguese_exceptions.ps1
```

計: **4 ファイル**

### 12.6 合計ファイル数

| カテゴリ | ファイル数 |
|---------|----------|
| メインパッケージ (12.1) | 22 |
| テスト (12.2) | 18 |
| Multilingual 変更 (12.3) | 5 |
| ソリューション・インフラ変更 (12.4) | 4 (slnx + テストcsproj + MultilingualPortugueseTests + MultilingualMixedLanguageTests変更) |
| 評価ツール (12.5) | 4 |
| **合計** | **53** |

---

## 13. スペイン語/フランス語との差異ポイント

| 項目 | Spanish | French | Portuguese (予定) |
|------|---------|--------|------------------|
| 鼻母音 | なし | 4種 (NasalVowelizer) | 5種 (NasalVowelizer, より複雑) |
| 母音体系 | 5母音 | 12口母音+4鼻母音 | 7-12口母音+5鼻母音 |
| ストレス | 明確 (アクセント規則) | 最終音節固定 | 明確 (アクセント規則, Spanish類似) |
| 音節分割 | 正書法ベース | 音素ベース | 正書法ベース (Spanish方式) |
| 母音弱化 | なし | なし | あり (ブラジル/ヨーロッパ差異大) |
| R変異 | ふるえ/はじき | 口蓋垂摩擦音 | 多様 (はじき/ふるえ/摩擦/後部歯茎/口蓋垂) |
| 語末子音削除 | 限定的 | リエゾン系 | ヨーロッパ方言で顕著 |
| lh/nh | なし | なし (il/gn) | あり (ダイグラフ) |
| ç | なし | あり (/s/) | あり (/s/)。ただし TextSegmenter では ç をフランス語マーカーとして使用しているため、ポルトガル語の言語判定には ã/õ を使うこと |
| ã/õ | なし | なし | あり (鼻母音)。TextSegmenter でポルトガル語固有マーカーとして使用可能 |

---

## 14. 実装推奨順序

1. **P1: Models + csproj + 基本ルール** (GraphemeToPhonemeRules, Syllabifier, StressAssigner)
2. **P2: NasalVowelizer + AllophoneProcessor** (ポルトガル語固有の複雑性)
3. **P3: Normalizer + NumberToWords + ExceptionDictionary**
4. **P4: IPA/X-SAMPA Converter + 精度評価**
5. **P5: Multilingual 統合 + パッケージング**

---

## 15. コーディング規約メモ

- namespace: `DotNetG2P.Portuguese`
- `internal static class` でルールクラスを定義（Engine のみ `public sealed class`）
- `readonly struct` + `IEquatable<T>` で値型モデル
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` で高頻度判定メソッド
- コメント・doc comment は日本語
- `Array.Empty<T>()` を空配列の代わりに使用
- `HashCode.Combine()` で GetHashCode 実装
- フィールド命名: `_camelCase` (private), `s_camelCase` (static)
