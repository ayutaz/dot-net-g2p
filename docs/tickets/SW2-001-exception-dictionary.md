# SW2-001: 例外辞書（TSV + ローダー）

> **マイルストーン**: Sw2 — 例外辞書 + テキスト正規化 + X-SAMPA
> **前提チケット**: Sw1完了（SwedishG2PEngine、SwedishIpaPhoneme enum、GraphemeToPhonemeRules、SwedishSyllabifier、StressAssigner、IpaConverter が動作する状態）
> **後続チケット**: SW2-002（正規化パイプラインが辞書ルックアップ結果を利用）、SW2-003（FunctionWordListが辞書カテゴリ `function_word` と連携）、SW2-005（SwedishExceptionDictionaryTests）

## 1. タスク目的とゴール

Sw1で構築したルールベースG2Pでは対処困難な不規則語・外来語・機能語の発音を、例外辞書で補完する。300語以上のエントリを持つ `swedish_exceptions.master.tsv` をアセンブリ埋め込みリソースとして提供し、`SwedishExceptionDictionary.cs` でロード・ルックアップAPIを実装する。

**ゴール**: `SwedishG2PEngine.ToIPA("och")` → `"ɔ"`（chが黙字の不規則語）が例外辞書経由で正しく返ること。これにより ipa-dict サンプルのPERを Sw1の15%未満からSw2の8%未満に引き下げる基盤を作る。

## 2. 実装内容の詳細

### 2.1 追加ファイル

```
src/DotNetG2P.Swedish/
├── Data/
│   ├── SwedishExceptionDictionary.cs  — 例外辞書ローダー + ルックアップAPI
│   └── swedish_exceptions.master.tsv  — 埋め込みリソース（300+語）
```

### 2.2 TSV形式

```tsv
# surface	dialect	category	accent	stress_index	phonemes	source	note
och	*	function_word	1	-1	ɔ	manual	ch黙字
det	*	function_word	1	-1	d eː	manual	t黙字
de	*	function_word	1	-1	d ɔ m	manual	完全不規則
dem	*	function_word	1	-1	d ɔ m	manual	deと同音
mig	*	function_word	1	-1	m ɛ j	manual	ig→ej
dig	*	function_word	1	-1	d ɛ j	manual	ig→ej
sig	*	function_word	1	-1	s ɛ j	manual	ig→ej
jag	*	function_word	1	-1	j ɑː	manual	g弱化
chef	*	loanword_fr	1	0	ɧ eː f	manual	フランス語由来sj音
garage	*	loanword_fr	1	1	ɡ a|r ɑː ɧ	manual	フランス語由来
station	*	sj_exception	2	1	s t a|ɧ uː n	manual	-tion語尾
mission	*	sj_exception	2	1	m ɪ|ɧ uː n	manual	-sion語尾
kille	*	softening_exception	1	0	k ɪ|l ɛ	manual	軟母音前だがk硬い
Göteborg	*	place_name	2	1	j øː t ɛ|b ɔ r j	manual	不規則
Stockholm	*	place_name	1	0	s t ɔ k|h ɔ l m	manual
```

**フィールド定義:**

| フィールド | 型 | 説明 |
|-----------|-----|------|
| surface | string | 表層形（小文字正規化済み。地名のみ先頭大文字可） |
| dialect | string | `*`=全方言、`central`=Central、`finland`=FinlandSwedish |
| category | string | `function_word`, `loanword_fr`, `loanword_en`, `loanword_other`, `sj_exception`, `softening_exception`, `place_name`, `silent_letter`, `irregular` |
| accent | int | ピッチアクセント番号: 1=accent 1, 2=accent 2, `*`=未指定 |
| stress_index | int | 主ストレス音節インデックス（0-based）。-1=辞書で指定しない |
| phonemes | string | スペース区切り音素列。`\|` で音節区切り |
| source | string | `manual`, `nst`, `folkets` 等 |
| note | string | 備考（任意） |

**カテゴリ別規模（計300+語）:**

| カテゴリ | 推定数 | 内容 |
|---------|-------|------|
| function_word | 30-40 | 代名詞(jag,mig,dig,sig,de,dem)、前置詞、接続詞(och)、助動詞の弱形 |
| loanword_fr | 40-50 | フランス語由来（chef, garage, restaurant, paté, parfym 等） |
| loanword_en | 40-50 | 英語由来（show, team, design, jeans, mail 等） |
| loanword_other | 10-15 | ドイツ語・ラテン語由来 |
| sj_exception | 30-40 | -tion/-sion語尾(station,nation,mission)、ch→ɧ パターン |
| softening_exception | 15-20 | 子音軟化の例外（kille, gem, kex, öken 等）。k+軟母音でも/k/のまま |
| place_name | 40-50 | 主要都市・県名（Göteborg, Stockholm, Uppsala, Malmö, Linköping 等） |
| silent_letter | 10-15 | gn-, ps- 等のギリシャ語由来黙字 |
| irregular | 15-20 | その他不規則語（世界, musik, öga 等） |

カテゴリをフランス語（3カテゴリ）・ポルトガル語（4カテゴリ）より多い9カテゴリに拡張しているのは、スウェーデン語の外来語借用源が多様（仏・英・独・ラテン）であり、sj音例外・子音軟化例外・地名など言語固有の不規則パターンが多岐にわたるためである。

### 2.3 SwedishExceptionDictionary.cs

```csharp
internal static class SwedishExceptionDictionary
{
    // static readonly Dictionary（フランス語/ポルトガル語と同一パターン）
    // 初回アクセス時に埋め込みリソースからTSVをパースして構築
    private static readonly Dictionary<string, ExceptionEntry[]> s_entries = LoadEntries();
    
    public static bool TryLookup(string surface, SwedishDialect dialect, out ExceptionEntry entry);
    
    // ExceptionEntry: category, accent, stressIndex, phonemes(SwedishIpaPhoneme[]), syllableBoundaries
    
    internal readonly struct ExceptionEntry
    {
        public string Category { get; }
        public byte Accent { get; }          // 1, 2, or 0(未指定)
        public int StressIndex { get; }      // -1 = 未指定
        public SwedishIpaPhoneme[] Phonemes { get; }
        public int[] SyllableBoundaries { get; }  // |位置をパースして保持
    }
}
```

**TSVフィールドインデックスマッピング:**

| インデックス | フィールド | 例 |
|-------------|-----------|-----|
| parts[0] | surface | och |
| parts[1] | dialect | * |
| parts[2] | category | function_word |
| parts[3] | accent | 1 |
| parts[4] | stress_index | -1 |
| parts[5] | phonemes | ɔ |
| parts[6] | source | manual |
| parts[7] | note | ch黙字 |

**実装ポイント:**

- ロード: `Assembly.GetManifestResourceStream()` で埋め込みリソースからTSVをパース。Unity環境で埋め込みリソースが利用できない場合は `SwedishG2POptions.ExceptionDictionaryPath` によるファイルパスフォールバック
- static readonly Dictionary: `LoadEntries()` で初回アクセス時に一括ロード。IDisposable は不要（フランス語 `FrenchExceptionDictionary`、ポルトガル語 `PortugueseExceptionDictionary` と同一パターン）
- ルックアップ: surface を小文字化してDictionary検索。dialect が `*` のエントリは全方言でマッチ。dialect 指定ありのエントリは該当方言でのみマッチ（指定ありが `*` より優先）
- phonemes フィールドのパース: スペース区切りの各トークンを `SwedishIpaPhoneme` enum にマッピング。`|` は音節区切りとして `SyllableBoundaries` に記録
- `#` で始まる行はコメントとしてスキップ
- SwedishG2PEngine.ConvertWord() 内で、G2P規則適用前に TryLookup を呼び、ヒットすれば辞書のエントリを優先使用

### 2.4 SwedishG2PEngine への統合

```csharp
// SwedishG2PEngine.ConvertWord() の疑似コード
SwedishPronunciation ConvertWord(string word)
{
    // 1. 例外辞書ルックアップ（EnableExceptionDictionary=true の場合）
    if (_options.EnableExceptionDictionary && _exceptionDictionary.TryLookup(word, _options.Dialect, out var entry))
    {
        return BuildPronunciationFromEntry(entry);
    }
    
    // 2. G2P規則による変換（Sw1で実装済み）
    return _g2pRules.ConvertWord(word);
}
```

### 2.5 SwedishG2POptions への追加

```csharp
public bool EnableExceptionDictionary { get; }  // default: true
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishExceptionDictionary.cs、TSVパーサー、SwedishG2PEngine統合 |
| データキュレーション担当 | 1 | swedish_exceptions.master.tsv の300+エントリ作成・IPA検証 |

**計2名**。データキュレーションは既存パッケージ（フランス語500+語、ポルトガル語560+語）のTSV作成経験を参照し、espeak-ng sv_list（約1,040エントリ）やFolkets lexikonを発音リファレンスとして利用する。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**IN:**
- `swedish_exceptions.master.tsv` 300+エントリ（8カテゴリ）
- `SwedishExceptionDictionary.cs`（埋め込みリソースロード、方言フィルタ付きルックアップ）
- `SwedishG2PEngine` への辞書統合（EnableExceptionDictionary オプション）
- `DotNetG2P.Swedish.csproj` への EmbeddedResource 設定

**OUT:**
- テキスト正規化（SW2-002）
- X-SAMPA/FunctionWordList（SW2-003）
- 評価データ取得（SW2-004）
- テスト作成（SW2-005）

### ユニットテスト

SW2-005 で以下をカバー（本チケットでは実装対象外だがAPI設計の参考として記載）:

- `TryLookup_機能語_och/det/de/dem` → 正しい音素列
- `TryLookup_フランス語外来語_chef/garage` → sj音含む正しい音素列
- `TryLookup_英語外来語_show/team` → 正しい音素列
- `TryLookup_sj例外_station/mission` → ɧ含む正しい音素列
- `TryLookup_軟化例外_kille/gem` → /k/のまま
- `TryLookup_地名_Göteborg/Stockholm` → 不規則発音
- `TryLookup_存在しない語_false返却`
- `TryLookup_方言フィルタ_dialect=*で全方言マッチ`
- `TryLookup_方言固有エントリ_Central/Finlandで異なる結果`
- `EnableExceptionDictionary=false_辞書スキップ`
- `TSVパース_コメント行スキップ`
- `TSVパース_音節区切り正しくパース`

### E2Eテスト

- `SwedishG2PEngine.ToIPA("och")` → `"ɔ"` （辞書経由の不規則変換）
- `SwedishG2PEngine.ToIPA("station")` → ɧ音を含むIPA出力
- `SwedishG2PEngine.ToIPA("kille")` → /k/ で始まるIPA出力（軟化例外）
- EnableExceptionDictionary=true/false でPERが有意に異なること

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **stress_index=-1 の外来語問題**: ポルトガル語で `stress_index=-1` のTSVエントリが AllophoneProcessor の VowelReduction で全母音弱化される問題が発生した（MEMORY.md参照）。スウェーデン語でも同様の問題が起こり得るため、stress_index は可能な限り正しい値を設定する。-1の場合は StressAssigner のデフォルトルール（第1音節）にフォールバックする設計とする
2. **IPA音素パース時のUnicode問題**: ポルトガル語で IPA g (U+0261) と ASCII g (U+0067) のフォールバックが必要だった。TSV内の音素表記でUnicodeバリアントが混在しないよう、パーサーで正規化するか両方を受け付ける
3. **大文字・小文字の正規化**: 地名（Göteborg, Stockholm）は先頭大文字で入力される可能性がある。ルックアップ時は小文字正規化して検索するが、TSVでは元の表記と小文字化後の表記の両方を考慮する
4. **方言オーバーライドの優先順位**: dialect指定ありエントリ > dialect=`*`エントリの優先順位を明確に実装する

### レビューチェックリスト

- [ ] TSVフォーマットが8フィールド（surface/dialect/category/accent/stress_index/phonemes/source/note）で統一されているか
- [ ] 300+エントリが8カテゴリに適切に分類されているか
- [ ] phonemesフィールドの各音素が `SwedishIpaPhoneme` enum の値に正しくマッピングされるか
- [ ] `|` 音節区切りが音韻的に妥当な位置にあるか
- [ ] stress_index が -1 の場合のフォールバック処理が実装されているか
- [ ] 埋め込みリソースのロードとUnity代替パスフォールバックが動作するか
- [ ] 方言フィルタ（dialect=`*`/`central`/`finland`）の優先順位が正しいか
- [ ] コメント行（`#`始まり）・空行がスキップされるか
- [ ] espeak-ng sv_list や Folkets lexikon と照合して発音の正確性が検証されているか

## 6. ゼロから作り直すとしたら

1. **TSV設計**: phonemes フィールドの音節区切り記号を `|` ではなく `.`（IPA音節区切り）にする選択肢もあるが、既存パッケージ（ポルトガル語/フランス語）との一貫性のために `|` を踏襲する
2. **ローダー実装**: 初回ロードでTSV全体をパースしてDictionaryに格納する一括ロード方式は、300語程度なら十分高速。1000語を超える場合はバイナリフォーマット（ポルトガル語で検討）も視野に入るが、スウェーデン語の規模感では不要
3. **カテゴリの粒度**: `loanword_fr`/`loanword_en`/`loanword_other` を統合して `loanword` + `origin` フィールドにする設計もあるが、既存パッケージとの一貫性とフィルタ操作の簡便さを優先してカテゴリ内に言語情報を含める

## 7. 後続タスクへの連絡事項

- **SW2-002（正規化）**: `SwedishNormalizer.Tokenize()` が返す各トークンに対して辞書ルックアップが適用される。Tokenize の出力形式（小文字化済みか等）を揃えること
- **SW2-003（FunctionWordList）**: 辞書の `function_word` カテゴリのエントリと FunctionWordList の内容を一致させること。FunctionWordList は辞書とは独立したハードコードリストだが、カバー範囲が矛盾しないよう注意
- **SW2-005（テスト）**: SwedishExceptionDictionaryTests.cs で全カテゴリのルックアップを検証すること。特に dialect フィルタと stress_index=-1 のフォールバックは重点テスト対象
- **Sw3（ピッチアクセント）**: TSVの `accent` フィールド（1/2）は Sw3 の StressAssigner.AssignAccent() で利用される。Sw2 時点ではパースして ExceptionEntry に保持するが、アクセント付与ロジックへの統合は Sw3 で行う
