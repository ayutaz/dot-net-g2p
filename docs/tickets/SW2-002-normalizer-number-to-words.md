# SW2-002: テキスト正規化 + NumberToWords

> **マイルストーン**: Sw2 — 例外辞書 + テキスト正規化 + X-SAMPA
> **前提チケット**: SW2-001（例外辞書が統合された SwedishG2PEngine が動作する状態）
> **後続チケット**: SW2-005（SwedishNormalizerTests, NumberToWordsTests）

## 1. タスク目的とゴール

数字・略語・記号・日付・通貨などを含む自然なスウェーデン語テキストをG2Pエンジンで処理可能にする。11段階の正規化パイプライン `SwedishNormalizer.cs` と、スウェーデン語固有の数値→単語変換 `NumberToWords.cs`（en/ett性区別、長大数制）を実装する。

**ゴール**: `SwedishNormalizer.Normalize("3:e april 2026")` → `"tredje april tvåtusentjugosex"` が正しく動作し、`SwedishG2PEngine` のパイプラインに統合されること。

## 2. 実装内容の詳細

### 2.1 追加ファイル

```
src/DotNetG2P.Swedish/
├── Normalization/
│   ├── SwedishNormalizer.cs     — 11段階テキスト正規化 + Tokenize
│   └── NumberToWords.cs          — 数値→スウェーデン語単語変換
└── SwedishG2POptions.cs          — EnableTextNormalization 追加
```

### 2.2 SwedishNormalizer.cs — 11段階パイプライン

| 段階 | メソッド | 入力例 | 出力例 | 備考 |
|------|---------|--------|--------|------|
| 1 | NormalizeUnicode | NFD `a\u030A` | NFC `å` | NFC正規化 + 小文字化 |
| 2 | ExpandAbbreviations | `t.ex. dvs. bl.a. kl. ca.` | `till exempel det vill säga bland annat klockan cirka` | 主要略語20-30種 |
| 3 | ExpandOrdinals | `1:a 2:a 3:e 10:e` | `första andra tredje tionde` | `:a`/`:e` 形式の序数略記 |
| 4 | ExpandDates | `2026-04-02` | `andra april tvåtusentjugosex` | ISO形式 YYYY-MM-DD。年号展開ルール: 2000年代は `tvåtusen` + 残り（例: 2026→tvåtusentjugosex）、1900年代以前は百の位分割（例: 1985→nittonhundraåttiofem） |
| 5 | ExpandTimes | `15:30` / `kl. 3` | `femton trettio` / `klockan tre` | 24時間制デジタル形式のみ。halv形式（3:30=halv fyra）の出力はSw3以降で検討 |
| 6 | ExpandCurrencies | `5 kr` / `29:99 kr` | `fem kronor` / `tjugonio kronor och nittionio öre` | kr/SEK/:-（クローナ/オーレ） |
| 7 | ExpandPercentages | `50%` | `femtio procent` | |
| 8 | ExpandDecimals | `3,14` | `tre komma fjorton` | カンマ区切り（スウェーデン語） |
| 9 | ExpandNumbers | `42` / `1 000 000` | `fyrtiotvå` / `en miljon` | en/ett性区別、長大数制 |
| 10 | ExpandSymbols | `@ & %` | `snabel-a och procent` | 記号→単語 |
| 11 | NormalizeWhitespace | `hej   världen ` | `hej världen` | 連続スペース統一、trim |

**主要略語一覧:**

| 略語 | 展開形 |
|------|--------|
| t.ex. | till exempel |
| dvs. | det vill säga |
| bl.a. | bland annat |
| kl. | klockan |
| ca. | cirka |
| osv. | och så vidare |
| m.m. | med mera |
| s.k. | så kallad |
| d.v.s. | det vill säga |
| f.n. | för närvarande |
| t.o.m. | till och med |
| f.ö. | för övrigt |
| o.d. | och dylikt |
| m.fl. | med flera |
| nr. / nr | nummer |
| st. / st | stycken |

**記号→単語マッピング:**

| 記号 | スウェーデン語 |
|------|--------------|
| @ | snabel-a |
| & | och |
| % | procent |
| + | plus |
| - | minus |
| = | lika med |
| € | euro |
| $ | dollar |
| £ | pund |

**Tokenize メソッド:**

`SwedishNormalizer.Tokenize(string text)` は内部で `Normalize()` を呼んでからトークン分割する。呼び出し側で二重正規化しないこと（フランス語G2Pレビュー知見、MEMORY.md参照）。内部用 `TokenizeNormalized()` メソッドを用意し、既に正規化済みのテキストに対してトークン分割のみ行う。

### 2.3 NumberToWords.cs — スウェーデン語数値変換

**スウェーデン語数値の特殊性:**

1. **en/ett性区別**: 共性名詞の前は `en`、中性名詞の前は `ett`。単独の1は通常 `ett`
   - `en bil`（1台の車、共性）vs `ett hus`（1軒の家、中性）
   - NumberToWords のデフォルトは `ett`（中性）。API引数で `en` 指定可能

2. **長大数制（Long Scale）**: 北欧諸国で使用
   - miljon = 10^6、miljard = 10^9、biljon = 10^12、biljard = 10^15

3. **複合数は1語**: tjugoett(21)、trettiotre(33)、fyrtiotvå(42)、nittionio(99)

4. **小数点はカンマ**: `3,14` = `tre komma fjorton`

5. **千の区切りはスペース**: `1 000 000` = `en miljon`

**基数一覧（0-20）:**

| 数 | スウェーデン語 |
|----|--------------|
| 0 | noll |
| 1 | ett / en |
| 2 | två |
| 3 | tre |
| 4 | fyra |
| 5 | fem |
| 6 | sex |
| 7 | sju |
| 8 | åtta |
| 9 | nio |
| 10 | tio |
| 11 | elva |
| 12 | tolv |
| 13 | tretton |
| 14 | fjorton |
| 15 | femton |
| 16 | sexton |
| 17 | sjutton |
| 18 | arton |
| 19 | nitton |
| 20 | tjugo |

**十の位（30-90）:**

| 数 | スウェーデン語 |
|----|--------------|
| 30 | trettio |
| 40 | fyrtio |
| 50 | femtio |
| 60 | sextio |
| 70 | sjuttio |
| 80 | åttio |
| 90 | nittio |

**大きな数:**

| 数 | スウェーデン語 |
|----|--------------|
| 100 | (ett) hundra |
| 1,000 | (ett) tusen |
| 1,000,000 | en miljon |
| 1,000,000,000 | en miljard |

**序数:**

| 基数 | 序数 |
|------|------|
| 1 | första |
| 2 | andra |
| 3 | tredje |
| 4 | fjärde |
| 5 | femte |
| 6 | sjätte |
| 7 | sjunde |
| 8 | åttonde |
| 9 | nionde |
| 10 | tionde |
| 11 | elfte |
| 12 | tolfte |
| 20 | tjugonde |
| 100 | hundrade |
| 規則 | 末尾 -nde / -de / -te |

**Public API:**

```csharp
internal static class NumberToWords
{
    /// <summary>基数を単語に変換。useEn=true で "en"（共性）、false で "ett"（中性、デフォルト）</summary>
    public static string ToCardinal(long number, bool useEn = false);
    
    /// <summary>序数を単語に変換</summary>
    public static string ToOrdinal(long number);
    
    /// <summary>小数を単語に変換（カンマ区切り入力）</summary>
    public static string ToDecimal(string decimalString);
}
```

### 2.4 SwedishG2POptions への追加

```csharp
public bool EnableTextNormalization { get; }  // default: true
```

### 2.5 SwedishG2PEngine への統合

```csharp
// SwedishG2PEngine.ToIPA(text) の処理フロー
string ToIPA(string text)
{
    // 1. テキスト正規化（EnableTextNormalization=true の場合）
    var tokens = _options.EnableTextNormalization
        ? _normalizer.Tokenize(text)
        : SimpleTokenize(text);
    
    // 2. 各トークンを変換（辞書ルックアップ → G2P規則）
    // ...
}
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishNormalizer.cs（11段階パイプライン）、NumberToWords.cs、SwedishG2PEngine統合 |

**計1名**。既存パッケージ（フランス語・ポルトガル語）の Normalizer/NumberToWords 実装パターンを参考に、スウェーデン語固有の変換ルール（en/ett性区別、長大数制、カンマ小数点、`:a`/`:e`序数略記、kr通貨）を実装する。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**IN:**
- `SwedishNormalizer.cs`（11段階パイプライン + Tokenize）
- `NumberToWords.cs`（基数・序数・小数、en/ett性区別、長大数制）
- `SwedishG2POptions.EnableTextNormalization` プロパティ追加
- `SwedishG2PEngine` への正規化パイプライン統合

**OUT:**
- 例外辞書（SW2-001）
- X-SAMPA/FunctionWordList（SW2-003）
- テスト作成（SW2-005）

### ユニットテスト

SW2-005 で以下をカバー（本チケットでは実装対象外だがAPI設計の参考として記載）:

**SwedishNormalizerTests.cs（40テスト）:**
- NFC正規化: NFD入力 `a\u030A` → NFC `å`
- 小文字化: `HEJ` → `hej`
- 略語展開: `t.ex.` → `till exempel`、`dvs.` → `det vill säga`、`bl.a.` → `bland annat`、`kl.` → `klockan`
- 序数展開: `1:a` → `första`、`3:e` → `tredje`、`10:e` → `tionde`
- 日付展開: `2026-04-02` → `andra april tvåtusentjugosex`
- 時刻展開: `15:30` → `femton trettio`、`kl. 3` → `klockan tre`
- 通貨展開: `5 kr` → `fem kronor`、`29:99 kr` → `tjugonio kronor och nittionio öre`
- パーセント展開: `50%` → `femtio procent`
- 小数展開: `3,14` → `tre komma fjorton`
- 数字展開: `42` → `fyrtiotvå`、`1 000 000` → `en miljon`
- 記号展開: `@` → `snabel-a`、`&` → `och`
- 空白正規化: 連続スペース → 単一スペース

**NumberToWordsTests.cs（20テスト）:**
- 基数 0-20: `noll`-`tjugo`
- 十の位 30-90: `trettio`-`nittio`
- 複合数: `tjugoett`(21)、`nittionio`(99)
- 百・千: `hundra`、`tusen`
- 長大数制: `miljon`(10^6)、`miljard`(10^9)
- en/ett性区別: `ToCardinal(1, useEn:true)` → `en`、`ToCardinal(1)` → `ett`
- 序数: `första`(1st)、`andra`(2nd)、`tredje`(3rd)、`tionde`(10th)
- 小数: `tre komma fjorton`(3.14)
- 負数: `minus fem`(-5)
- ゼロ: `noll`(0)
- 大きな数: `en miljon tvåhundratrettiofyra tusen femhundrasextiosju`(1,234,567)

### E2Eテスト

- `SwedishG2PEngine.ToIPA("3:e april 2026")` → 正規化後のIPA出力
- `SwedishG2PEngine.ToIPA("5 kr")` → `fem kronor` のIPA出力
- `EnableTextNormalization=false` → 数字がそのまま（変換されない）

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **二重正規化の防止**: フランス語G2Pレビューで `Tokenize()` 内部で `Normalize()` を呼ぶ設計が確立済み（MEMORY.md参照）。SwedishNormalizer も同一パターンで、`Tokenize()` = `Normalize()` + `Split()`、内部用 `TokenizeNormalized()` を分離する
2. **en/ett性の曖昧さ**: NumberToWords のデフォルトは `ett`（中性）とするが、文脈依存で `en` が必要な場合がある（例: `1 bil` = `en bil`）。Sw2 時点では文脈解析は行わず、デフォルト `ett` で統一。文脈依存 en/ett は将来拡張として残す
3. **日付フォーマットの多様性**: スウェーデン語では ISO形式 `2026-04-02` が標準だが、`2 april 2026` や `2/4-2026` 等の変種もある。Sw2 では ISO形式のみを処理し、他形式は将来拡張とする
4. **千の区切りスペース**: `1 000 000` のように薄いスペース（U+2009 THIN SPACE）やノーブレークスペース（U+00A0）が使われる場合がある。NormalizeWhitespace でASCIIスペースに統一した上で数字パターンを検出する
5. **通貨パターンの複雑さ**: `29:99 kr` のコロン区切りはスウェーデン語特有。`:-`（例: `100:-`）は「ちょうど100クローナ」を意味する。これらのパターンを正規表現で適切に処理する

### レビューチェックリスト

- [ ] 11段階パイプラインが正しい順序で適用されているか（NFC→略語→序数→日付→時刻→通貨→%→小数→数字→記号→空白）
- [ ] NumberToWords の en/ett 性区別が正しく動作するか
- [ ] 長大数制（miljard=10^9, biljon=10^12）が正しく実装されているか
- [ ] 複合数が1語として出力されるか（tjugoett, fyrtiotvå 等）
- [ ] 小数のカンマ区切りが正しく処理されるか（3,14 → tre komma fjorton）
- [ ] 序数略記 `:a`/`:e` の展開が正しいか
- [ ] 通貨パターン（kr, SEK, :-）が正しく展開されるか
- [ ] Tokenize 内で Normalize を呼ぶ設計（二重正規化防止）が実装されているか
- [ ] EnableTextNormalization=false で正規化がスキップされるか
- [ ] 空文字/null入力に対する適切なハンドリング

## 6. ゼロから作り直すとしたら

1. **パイプライン順序**: 現在の11段階は依存関係を考慮した順序（略語展開→数字展開の順で、略語内の数字が先に展開されない）だが、段階間の相互依存をより明示的にドキュメント化すべき。特に通貨展開（段階6）と数字展開（段階9）の間で「コロン区切り金額」が数字パターンに誤マッチしないよう、処理順序の根拠を明記する
2. **NumberToWords の構造**: 再帰的分割方式（1000単位で分割→各桁を再帰処理）は既存パッケージと共通の設計。スウェーデン語の「複合数1語」制約（tjugoett等）を考慮して、21-99の範囲は特別処理（十の位+一の位を連結）する
3. **正規表現の管理**: 各段階で個別の正規表現を使用するが、コンパイル済み正規表現を static readonly フィールドで保持し、パフォーマンスを確保する

## 7. 後続タスクへの連絡事項

- **SW2-003（FunctionWordList）**: 正規化後のトークンに対して FunctionWordList によるストレス除去が適用される。正規化で略語が展開された結果、機能語が出現する場合がある（例: `bl.a.` → `bland annat` の `bland` と `annat`）
- **SW2-005（テスト）**: SwedishNormalizerTests.cs で11段階の各段階を個別テストすること。特に段階間の順序依存性（通貨→数字の順序）を検証するテストを含める。NumberToWordsTests.cs では en/ett 性区別、長大数制、複合数を重点的にテストする
- **Sw3（ピッチアクセント）**: 正規化で展開された複合数（tjugoett等）のピッチアクセントは Accent 2 となる。NumberToWords の出力をそのまま G2P に通す場合、StressAssigner が複合語パターンを認識してAccent 2を付与する必要がある
