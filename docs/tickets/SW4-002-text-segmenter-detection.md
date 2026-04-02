# SW4-002: TextSegmenter スウェーデン語言語判定

> **マイルストーン**: Sw4 — Multilingual統合 + 評価ツール + リリース準備
> **前提チケット**: SW4-001（Language.Swedish 定義が必要）
> **後続チケット**: SW4-005

## 1. タスク目的とゴール

TextSegmenter と LanguageDetector にスウェーデン語の言語判定ロジックを追加し、スウェーデン語テキストが正しく `Language.Swedish` として分類されるようにする。既存7言語の判定精度を維持しつつ、スウェーデン語の確定信号（å 文字）、信号語、接尾辞信号を用いた判定を実装する。

**完了の定義:**
- `å` (U+00E5) を含むテキストがスウェーデン語として判定される
- 信号語（och, att, hej, tack 等）によるヒューリスティクス判定が機能する
- 接尾辞信号（-tion, -ighet, -ning 等）による判定が機能する
- 既存7言語の判定結果にリグレッションがない
- スウェーデン語と他のラテン文字言語（英/西/仏/葡）の混在テキストが正しくセグメント分割される

## 2. 実装内容の詳細

### 2.1 TextSegmenter.cs 変更

TextSegmenter.cs に以下の全変更を適用する（`ContainsExplicitSwedishCharacter` を含む全メソッドは TextSegmenter.cs 内に配置する。LanguageDetector.cs には配置しない）。

#### (a) LangSwedish byte 定数追加

```csharp
// 既存: LangKorean = 7
private const byte LangSwedish = 8;  // Language.Swedish
```

**注意**: 既存の LangNone=0, LangJapanese=1, ..., LangKorean=7 の連番パターンに従う。Language enum の値（Swedish=7）とは異なるオフセット値であることに注意（TextSegmenter 内部の byte エンコーディングは Language enum +1）。

#### (b) FromLangByte() に case LangSwedish 追加

```csharp
private static Language FromLangByte(byte lang)
{
    // 既存の switch に追加:
    case LangSwedish: return Language.Swedish;
}
```

#### (c) IsLatinLanguage() に `language == LangSwedish` 追加

```csharp
private static bool IsLatinLanguage(byte language)
{
    return language == LangEnglish || language == LangSpanish || language == LangFrench
        || language == LangPortuguese || language == LangSwedish;
}
```

#### (d) defaultLatinByte マッピング（166行目付近）にSwedish分岐追加

```csharp
byte defaultLatinByte = defaultLatinLanguage == Language.Spanish ? LangSpanish
                     : defaultLatinLanguage == Language.French ? LangFrench
                     : defaultLatinLanguage == Language.Portuguese ? LangPortuguese
                     : defaultLatinLanguage == Language.Swedish ? LangSwedish
                     : LangEnglish;
```

#### (e) バリデーション（132行目付近）にSwedish追加

`MultilingualG2POptions.cs` のバリデーション修正は SW4-001 のスコープで実施する。

#### (f) ResolveLatinLanguage() にスウェーデン語判定ロジック追加

挿入位置: **ポルトガル語確定文字判定（`ContainsExplicitPortugueseCharacter` / `ContainsPortugueseCedillaPattern`）の直後、フランス語確定文字判定（`ContainsExplicitFrenchCharacter`）の前**に挿入する。

```csharp
// ポルトガル語固有文字判定の直後に挿入:

// スウェーデン語特有文字 å (U+00E5) の検出
// å はスウェーデン語/ノルウェー語/デンマーク語の明確マーカー
// 現在ノルウェー語・デンマーク語は非サポートのためスウェーデン語に分類
if (ContainsExplicitSwedishCharacter(token))
    return LangSwedish;

// ↓ 既存のフランス語判定がここに続く
```

ASCII ヒューリスティクス（`hasLatinExtended=false` の場合のみ）:
```csharp
// 既存の LooksLikePortugueseAsciiToken の直後に挿入:
if (!hasLatinExtended && LooksLikeSwedishAsciiToken(token))
    return LangSwedish;
```

#### (g) ContainsExplicitSwedishCharacter() メソッド追加（TextSegmenter.cs 内）

```csharp
/// <summary>
/// スウェーデン語固有文字 å (U+00E5) の検出。
/// ä (U+00E4) と ö (U+00F6) はドイツ語等と共有するため除外。
/// </summary>
private static bool ContainsExplicitSwedishCharacter(ReadOnlySpan<char> text)
{
    for (int i = 0; i < text.Length; i++)
    {
        if (text[i] == '\u00E5') // å
            return true;
    }
    return false;
}
```

**設計判断**: `ä` (U+00E4) と `ö` (U+00F6) はドイツ語・フィンランド語等と共有されるため、確定信号としては使用しない。`å` のみがスカンジナビア言語の明確なマーカーとなる。

#### 信号語配列追加

```csharp
private static readonly string[] s_swedishWordSignals =
{
    "och", "att", "hej", "tack", "hur", "dag", "inte",
    "den", "ett", "har", "ska", "kan", "vill"
};

private static readonly string[] s_swedishSuffixSignals =
{
    "tion", "ighet", "ning", "skap", "lig", "ande", "else"
};
```

**信号語 "det" と "som" について**: これらは他言語との衝突リスクがある（"det" は英語の略語として使用される可能性、"som" はポルトガル語「音」の意味で存在）。**単一語マッチではスウェーデン語確定としない**。`LooksLikeSwedishAsciiToken()` ではスコアリング方式を採用し、信号語マッチのスコア合計が `score >= 3` に達した場合のみスウェーデン語と判定する（"det" と "som" は各1点、"och" や "att" 等の高信頼語は各2点）。ただし、"det" と "som" は `s_swedishWordSignals` からは除外し、低信頼度信号として別途 `s_swedishWeakSignals` 配列で管理する。

#### LooksLikeSwedishAsciiToken() メソッド追加

- 信号語の完全一致チェック（Array.BinarySearch または線形探索）
- 接尾辞信号のマッチング（EndsWith チェック）
- 既存の LooksLikeFrenchAsciiToken / LooksLikeSpanishAsciiToken / LooksLikePortugueseAsciiToken と同一パターン

### 2.2 LanguageDetector.cs 変更

#### ToLanguage() に Swedish 分岐追加

`LanguageDetector.ToLanguage(ScriptKind kind, Language defaultLatinLanguage)` メソッドの Latin 系分岐で、`defaultLatinLanguage == Language.Swedish` の場合に `Language.Swedish` を返すように対応を追加する。

**注意**: `ContainsExplicitSwedishCharacter()` は **TextSegmenter.cs** 内に `private static` メソッドとして配置する（LanguageDetector.cs には配置しない）。これは既存の `ContainsExplicitFrenchCharacter()` / `ContainsExplicitPortugueseCharacter()` 等と同じ配置パターンに従う。

### 2.3 信号語の選定根拠

| 信号語 | 意味 | 選定理由 |
|--------|------|---------|
| och | そして | 最頻接続詞、英語 "and" 相当。他言語と非衝突 |
| att | ～すること | 不定詞マーカー。高頻度 |
| hej | こんにちは | 挨拶語。英語/仏語/西語/葡語と非衝突 |
| tack | ありがとう | 日常語。他言語と非衝突 |
| inte | ～ない | 否定語。高頻度 |
| den/ett | 定冠詞/不定冠詞 | 超高頻度機能語 |
| har/ska/kan/vill | 助動詞 | 高頻度。英語 "has/shall/can/will" と非同形 |
| (det/som) | (弱信号) | 他言語と衝突リスクあり。s_swedishWeakSignals で管理し、score >= 3 のスコアリングで判定 |

### 2.4 接尾辞信号の選定根拠

| 接尾辞 | 用途 | 選定理由 |
|--------|------|---------|
| -tion | 名詞化 | スウェーデン語で非常に生産的（仏語/西語/葡語とは信号語との組合せで区別） |
| -ighet | 抽象名詞 | スウェーデン語固有。英語 "-ity" 相当だが形態が独自 |
| -ning | 名詞化 | スウェーデン語で高生産的。英語 "-ning" とは分布が異なる |
| -skap | 抽象名詞 | スウェーデン語固有。英語 "-ship" 相当 |
| -lig | 形容詞化 | スウェーデン語固有。英語 "-ly" 相当だが形態が異なる |
| -ande/-else | 名詞/形容詞化 | スウェーデン語で高生産的 |

### 2.5 他言語との衝突回避

| 潜在的衝突 | 対策 |
|-----------|------|
| "det" がスウェーデン語 / 英語 "detect" 等の部分一致 | 完全一致チェックのため衝突なし。ただし s_swedishWeakSignals に分類し、単独マッチでは確定しない（score >= 3 方式） |
| "-tion" が仏語/西語/葡語にも存在 | 信号語+接尾辞の組合せで判定。å があれば確定信号で即確定 |
| "dag" がスウェーデン語（日）/ 英語（短剣） | 英語では低頻度のため実質衝突なし |
| "som" がスウェーデン語（～として）/ ポルトガル語（音）| s_swedishWeakSignals に分類。単独マッチではスウェーデン語確定としない（score >= 3 方式で他の信号語との組合せで判定） |

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装担当 | 1名 | TextSegmenter.cs の変更（ContainsExplicitSwedishCharacter含む）、LanguageDetector.cs の ToLanguage() 変更 |
| テスト担当 | 1名 | 言語判定テスト、リグレッションテスト |

**合計: 2名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- TextSegmenter.cs: LangSwedish 定数、FromLangByte() case、IsLatinLanguage() 追加、defaultLatinByte マッピング追加、ResolveLatinLanguage() 判定ロジック、ContainsExplicitSwedishCharacter()、信号語/接尾辞配列、LooksLikeSwedishAsciiToken()
- LanguageDetector.cs: ToLanguage() への Swedish 分岐追加

**スコープ外:**
- MultilingualG2PEngine 本体の変更（SW4-001）
- Multilingual テスト（SW4-005）
- 評価ツール（SW4-003）

### ユニットテスト

| テスト | 検証内容 |
|--------|---------|
| ContainsExplicitSwedishCharacter_å含む_true | `"går"` → true |
| ContainsExplicitSwedishCharacter_ä含む_false | `"häst"` → false（ä はスウェーデン語確定信号ではない） |
| ContainsExplicitSwedishCharacter_ASCII_false | `"hej"` → false |
| Segment_å含むテキスト_Swedishに分類 | `"det går bra"` → Language.Swedish |
| Segment_ochキーワード_Swedishに分類 | `"jag och du"` → Language.Swedish（信号語で判定） |
| Segment_tackキーワード_Swedishに分類 | `"tack så mycket"` → Language.Swedish |
| Segment_ighet接尾辞_Swedishに分類 | `"möjlighet"` → Language.Swedish |
| Segment_英語テキスト_Englishのまま | `"hello world"` → Language.English（リグレッション確認） |
| Segment_仏語テキスト_Frenchのまま | `"bonjour le monde"` → Language.French（リグレッション確認） |
| Segment_西語テキスト_Spanishのまま | `"hola mundo"` → Language.Spanish（リグレッション確認） |
| Segment_葡語テキスト_Portugueseのまま | `"obrigado amigo"` → Language.Portuguese（リグレッション確認） |
| LooksLikeSwedishAsciiToken_信号語_true | `"och"`, `"att"`, `"hej"` 等 → true |
| LooksLikeSwedishAsciiToken_非信号語_false | `"hello"`, `"world"` 等 → false |

### E2Eテスト

| テスト | 検証内容 |
|--------|---------|
| 日瑞混在テキスト | `"こんにちは hej"` → 日本語 + スウェーデン語セグメント |
| 英瑞混在テキスト | `"hello hej världen"` → 英語 + スウェーデン語セグメント |
| 8言語混在テキスト | 全言語が正しくセグメント分割される |

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **LangSwedish byte 値のオフセット確認**: TextSegmenter 内部の byte 定数は Language enum の値と 1 ずれている（LangJapanese=1 だが Language.Japanese=0）。LangKorean=7 の次が LangSwedish=8 で正しいか、既存パターンを厳密に確認すること
2. **å とノルウェー語/デンマーク語の区別**: 現在これらの言語は未サポートのため å をスウェーデン語確定信号として使用できるが、将来のノルウェー語/デンマーク語追加時に変更が必要になる。コメントで明記すること
3. **信号語 "det" の衝突リスク**: 英語の "determine", "detect" 等の略語として "det" が使われる可能性は低いが、完全一致チェックであることを確認
4. **ポルトガル語 "som" との衝突**: ポルトガル語にも "som" が存在する。既存の portugueseWordSignals に "som" が含まれていないことを確認すること。含まれている場合は swedishWordSignals から除外を検討
5. **LooksLikeSwedishAsciiToken は hasLatinExtended=false の場合のみ呼ばれる**: ラテン拡張文字（å, ä, ö 等）を含むトークンは先に ContainsExplicitSwedishCharacter() で処理されるため、LooksLikeSwedishAsciiToken 内でこれらの文字を含むパターンを記述しても到達不能（MEMORYに記載のポルトガル語知見と同様）

### レビューチェックリスト

- [ ] LangSwedish の byte 値が既存パターンに従った正しい値である
- [ ] FromLangByte() に Swedish case が追加されている
- [ ] ContainsExplicitSwedishCharacter() が å (U+00E5) のみを検出する（ä, ö は除外）
- [ ] s_swedishWordSignals の語がソート済みである（BinarySearch 使用の場合）
- [ ] s_swedishWordSignals に既存の他言語信号語と衝突する語が含まれていない
- [ ] s_swedishSuffixSignals が既存パターン（EndsWith チェック）と整合している
- [ ] ResolveLatinLanguage() 内の判定優先度が正しい（確定信号 → ヒューリスティクス）
- [ ] 既存7言語の TextSegmenterTests が全てパスする
- [ ] å がノルウェー語/デンマーク語とも共有される旨のコメントが記載されている
- [ ] TextSegmenterTests に ContainsExplicitSwedishCharacter のテストが追加されている
- [ ] LanguageDetector.ToLanguage() に Swedish 分岐が追加されている

## 6. ゼロから作り直すとしたら

既存のポルトガル語言語判定追加（TextSegmenter への LangPortuguese 追加）の差分を直接参照して同一パターンで実装する。以下の手順で進める:

1. git log で ポルトガル語 TextSegmenter 追加の commit を特定
2. その diff を基に、Portuguese → Swedish の置換で骨格を作成
3. ContainsExplicitSwedishCharacter は ContainsExplicitPortugueseCharacter のパターンをコピー（ç → å に変更）
4. 信号語/接尾辞は技術調査レポートの 14.2 節を参照して選定
5. LooksLikeSwedishAsciiToken は LooksLikePortugueseAsciiToken をテンプレートにする

信号語の選定では「スウェーデン語固有性 > 頻度」を優先する。他のラテン文字言語と共有される語（例: "som"）は除外する。

## 7. 後続タスクへの連絡事項

- **SW4-005 へ**: TextSegmenter テストでは、å を含むテキスト、信号語のみのテキスト、接尾辞のみのテキストの3パターンを必ず検証すること。特に8言語混在テストでは、スウェーデン語セグメントが既存言語のセグメントと正しく分離されることを確認
- **将来のノルウェー語/デンマーク語対応時の注意**: å の確定信号としての使用は、スカンジナビア言語が1言語のみサポートされている現状に依存している。複数スカンジナビア言語対応時は、å を確定信号から除外し、信号語ベースのヒューリスティクスに変更する必要がある
- **ポルトガル語 "som" 確認結果**: 既存の s_portugueseWordSignals を確認し、"som" が含まれていない場合のみ s_swedishWordSignals に含めること。含まれている場合は除外する
