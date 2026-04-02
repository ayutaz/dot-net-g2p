# SW1-005: ストレス付与 + IPA変換

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-002（音素enum）, SW1-003（音節分割・正書法）
> **後続チケット**: SW1-006（SwedishG2PEngine メインAPI）

## 1. タスク目的とゴール

音節分割された音素列に対して、スウェーデン語のストレス規則（第1音節デフォルト + 外来語接尾辞シフト）を適用する `StressAssigner` と、41音素の `SwedishIpaPhoneme` enum をIPA記号文字列に変換する `IpaConverter` を実装する。

### 完了状態

- `StressAssigner.MarkStress()` が音節リストに対して正しいストレス位置を付与できる
- 固有語は第1音節にprimary stress、外来語接尾辞（-tion, -sion, -ell, -ent, -ör 等）で最終/準最終音節へシフト
- 単音節語にはストレスマーク付与（primary）
- `IpaConverter` が41音素すべてに対して正しいIPA記号を返す
- ストレスマーク（ˈ）が音節先頭の正しい位置に配置される
- `ToIPA()` / `ToIPAWithoutStress()` に相当する変換が単体で動作する

## 2. 実装内容の詳細

### 作成ファイル

#### `src/DotNetG2P.Swedish/Rules/StressAssigner.cs`

ストレス付与処理。Sw1時点では基本ストレスのみ（ピッチアクセント予測はSw3で拡張）。

**主要メソッド:**

- `MarkStress(string word, IReadOnlyList<SwedishSyllable> syllables) -> void`
  - Phase 1: デフォルト第1音節ストレス（ゲルマン語の基本規則）
  - Phase 2: 外来語接尾辞によるストレスシフト
- `IsLoanwordSuffix(string word) -> (bool isLoanword, int stressedSyllableIndex)`
  - 検出対象接尾辞: `-tion`, `-sion`, `-ell`, `-ent`, `-ör`, `-ös`, `-al`, `-ik`, `-ism`, `-ist`
  - 該当する場合、ストレスを最終音節または準最終音節にシフト

**ストレス規則の詳細:**

| 条件 | ストレス位置 | 例 |
|------|------------|-----|
| デフォルト（固有語） | 第1音節 | `ˈhɛj`, `ˈtala` |
| 単音節語 | その音節 | `ˈhuːs` |
| -tion / -sion 語尾 | 最終音節 | `staˈɧuːn` |
| -ell / -ent 語尾 | 最終音節 | `hoˈtɛl` |
| -ör 語尾 | 最終音節 | `akˈtøːr` |
| -ik / -al 語尾 | 最終音節 | `muˈsiːk` |
| 複合語（基本対応） | 第1要素の第1音節にprimary、第2要素にsecondary | `ˈfuːtˌbɔl` |

**注意事項:**
- Sw1では複合語分解は簡易実装（明らかなハイフン複合のみ）。本格対応はSw2以降
- ピッチアクセント（accent 1/2）の予測はSw3で `AssignAccent()` として拡張追加する前提で、拡張ポイントを設計に残すこと

#### `src/DotNetG2P.Swedish/Conversion/IpaConverter.cs`

41音素のIPA記号マッピングとストレスマーク配置。

**主要メソッド:**

- `ToSymbol(SwedishIpaPhoneme phoneme) -> string`
  - 41音素それぞれに対応するIPA記号文字列を返す
- `Convert(SwedishPronunciation pronunciation, bool includeStress = true) -> string`
  - `SwedishPronunciation`（音素配列 + 音節 + ストレス情報）からIPA文字列を生成
  - `includeStress=true` の場合、ˈ（primary）/ ˌ（secondary）を音節先頭に挿入
  - `includeStress=false` の場合、ストレスマーク省略
- `ConvertPhonemeList(SwedishPronunciation pronunciation) -> string`
  - スペース区切りの音素列文字列を返す（`ToPhonemes` API用）

**41音素 → IPA記号マッピング:**

| enum値 | IPA記号 | enum値 | IPA記号 |
|--------|--------|--------|--------|
| LongI | iː | ShortI | ɪ |
| LongY | yː | ShortY | ʏ |
| LongU_Central | ʉː | ShortU_Central | ɵ |
| LongU | uː | ShortU | ʊ |
| LongE | eː | ShortE | ɛ |
| LongOe | øː | ShortOe | œ |
| LongEh | ɛː | ShortO | ɔ |
| LongO | oː | ShortA | a |
| LongA | ɑː | Schwa | ə |
| P | p | B | b |
| T | t | D | d |
| K | k | G | ɡ |
| F | f | V | v |
| S | s | H | h |
| Sj | ɧ | Tj | ɕ |
| M | m | N | n |
| Ng | ŋ | L | l |
| R | r | J | j |
| RetroT | ʈ | RetroD | ɖ |
| RetroN | ɳ | RetroL | ɭ |
| RetroS | ʂ | | |

**ストレスマーク配置規則:**
- ˈ（U+02C8）: primary stress。音節の先頭（最初の音素の直前）に配置
- ˌ（U+02CC）: secondary stress。同上
- 長音記号 ː は母音enumに含まれる（LongI = iː 等）ため、IpaConverter 側での追加は不要

**実装パターン参考:** `src/DotNetG2P.Spanish/Conversion/IpaConverter.cs`、`src/DotNetG2P.French/Conversion/IpaConverter.cs` と同一設計。`static readonly string[]` による O(1) ルックアップテーブルを使用。

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | StressAssigner + IpaConverter の実装 |
| テストエージェント | 1 | StressAssignerTests + SwedishIpaTests の実装 |
| レビューエージェント | 1 | コードレビュー + 既存言語パッケージとの整合性確認 |

**推奨**: 実装とテストは並行作業可能。レビューは両方完了後に実施。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含む:**
- `StressAssigner.cs` — 基本ストレス付与（第1音節デフォルト + 外来語接尾辞シフト）
- `IpaConverter.cs` — 41音素IPA記号マッピング + ストレスマーク配置
- 対応するユニットテスト（30テスト）

**含まない:**
- ピッチアクセント（accent 1/2）予測 → Sw3
- 方言別IPA出力差異 → Sw3
- X-SAMPA変換 → Sw2
- PUA変換 → Sw3
- 複合語の本格的なストレス分配 → Sw2以降

### ユニットテスト

#### `tests/DotNetG2P.Tests/SwedishG2P/StressAssignerTests.cs`（15テスト）

| テストメソッド | 内容 |
|--------------|------|
| `MarkStress_固有語_第1音節にPrimaryStress` | tala → 第1音節にˈ |
| `MarkStress_単音節語_ストレスあり` | hus → ˈhuːs |
| `MarkStress_Tion語尾_最終音節にシフト` | station → 最終音節にˈ |
| `MarkStress_Sion語尾_最終音節にシフト` | mission → 最終音節にˈ |
| `MarkStress_Ell語尾_最終音節にシフト` | hotell → 最終音節にˈ |
| `MarkStress_Ent語尾_最終音節にシフト` | student → 最終音節にˈ |
| `MarkStress_Or語尾_最終音節にシフト` | aktör → 最終音節にˈ |
| `MarkStress_Ik語尾_最終音節にシフト` | musik → 最終音節にˈ |
| `MarkStress_Al語尾_最終音節にシフト` | normal → 最終音節にˈ |
| `MarkStress_Ism語尾_最終音節にシフト` | socialism → 最終音節にˈ |
| `MarkStress_3音節固有語_第1音節にストレス` | flickorna → 第1音節にˈ |
| `IsLoanwordSuffix_該当あり_trueとインデックス` | -tion検出 |
| `IsLoanwordSuffix_該当なし_false` | 固有語はfalse |
| `MarkStress_ハイフン複合語_primary_secondary` | テスト-ケース |
| `MarkStress_空リスト_例外なし` | 空入力に安全 |

#### `tests/DotNetG2P.Tests/SwedishG2P/SwedishIpaTests.cs`（15テスト）

| テストメソッド | 内容 |
|--------------|------|
| `ToSymbol_長母音9種_正しいIPA` | LongI→"iː" 等 |
| `ToSymbol_短母音9種_正しいIPA` | ShortI→"ɪ" 等 |
| `ToSymbol_破裂音6種_正しいIPA` | P→"p" 等 |
| `ToSymbol_摩擦音6種_正しいIPA` | Sj→"ɧ", Tj→"ɕ" 含む |
| `ToSymbol_鼻音3種_正しいIPA` | Ng→"ŋ" 等 |
| `ToSymbol_接近音3種_正しいIPA` | L, R, J |
| `ToSymbol_そり舌音5種_正しいIPA` | RetroT→"ʈ" 等 |
| `Convert_ストレス付き_音節先頭にˈ` | ˈが正しい位置に |
| `Convert_ストレスなし_マーク省略` | includeStress=false |
| `Convert_SecondaryStress_ˌ配置` | ˌが正しい位置に |
| `Convert_単音節語_正しいIPA` | hej→"hɛj" |
| `Convert_長母音語_長音記号含む` | hus→"hʉːs" |
| `ConvertPhonemeList_スペース区切り` | "h ɛ j" 形式 |
| `Convert_そり舌音含む語_正しい出力` | bord→"buːɖ" |
| `Convert_空入力_空文字列` | 空の発音情報に安全 |

### E2Eテスト

- `StressAssigner` + `IpaConverter` を組み合わせて、音節分割済み音素列からストレス付きIPA文字列が生成されることを確認
- 例: `"hej"` の音素列 → 音節分割 → ストレス付与 → IPA変換 → `"hɛj"`
- 例: `"station"` の音素列 → 音節分割 → ストレスシフト → IPA変換 → `"staˈɧuːn"`

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **外来語接尾辞の網羅性**: Sw1時点では主要な接尾辞（-tion, -sion, -ell, -ent, -ör, -ik, -al, -ism, -ist）のみ対応。不足する接尾辞パターンはSw2の例外辞書で補完する想定だが、リストの妥当性を言語学的に検証する必要がある
2. **複合語ストレス**: Sw1ではハイフン区切りの複合語のみ簡易対応。スウェーデン語は非ハイフン複合語が多い（fotboll, barnbok等）が、形態素境界の検出には辞書が必要。Sw2以降への先送りは妥当だが、APIシグネチャが変わらないよう設計すること
3. **ストレスマーク位置**: IPA規約では ˈ は音節先頭（最初の子音の前）に配置する。日本語音声学文献では母音の前に置く慣例もあるが、ipa-dict の表記に合わせること
4. **`o` の長母音マッピング**: スウェーデン語の `o` は長母音時に `/uː/` になる非直感的な対応。IpaConverter のテストで明示的に検証すること
5. **Sw3拡張への設計考慮**: `StressAssigner` はSw3で `AssignAccent()` メソッドが追加される。MarkStress の内部状態が拡張を阻害しない設計にすること（音節リストのストレス情報をmutableにする等）

### レビューチェックリスト

- [ ] 41音素すべてのIPA記号マッピングが正しいか（特に ɧ, ɕ, ʉː, ɵ 等のスウェーデン語固有音素）
- [ ] ストレスマーク ˈ(U+02C8) / ˌ(U+02CC) がハードコードされた似た記号（'(U+0027) 等）と混同されていないか
- [ ] 長母音の ː(U+02D0) が正しいUnicodeコードポイントか
- [ ] `ɡ`(U+0261) が ASCII `g`(U+0067) と混同されていないか（ipa-dict は U+0261 を使用）
- [ ] 外来語接尾辞リストが他言語パッケージ（スペイン語/フランス語）と同程度の網羅性か
- [ ] `StressAssigner` のAPIが Sw3 の `AssignAccent()` 拡張と互換性のある設計か
- [ ] `IpaConverter.ToSymbol()` が `static readonly string[]` によるO(1)ルックアップになっているか
- [ ] 既存言語パッケージ（SpanishIpaConverter, FrenchIpaConverter等）と同一の設計パターンか
- [ ] null/空入力に対する防御的処理があるか
- [ ] `[Preserve]` 属性がpublic型に付与されているか（Unity IL2CPP対応）

## 6. ゼロから作り直すとしたら

ストレス付与とIPA変換は比較的単純な処理であり、現設計で問題ない。改善の余地があるとすれば:

1. **ストレス規則の外部データ化**: 外来語接尾辞リストをコード内ハードコードではなく、TSV等の外部データとして管理する方法。これにより接尾辞の追加・修正がリコンパイル不要になる。ただし、Sw1の規模（10接尾辞程度）では過剰設計。Sw2の例外辞書にストレス情報を含めることで同等の効果が得られるため、現行設計を推奨
2. **IPA変換のSpan<char>最適化**: 高頻度呼び出し時のstring allocation削減のため、`Span<char>` ベースの変換を検討できる。ただし .NET Standard 2.1 では `Span` サポートが限定的であり、Unity互換性との兼ね合いで見送りが妥当
3. **ストレスとピッチアクセントの統合設計**: Sw1（ストレス）とSw3（ピッチアクセント）を最初から統合設計する方法。StressAssigner 内に AccentInfo を含むデータ構造を初期から定義しておけば、Sw3での拡張がよりスムーズになる。ただし、YAGNI原則に反するため、拡張ポイントを残す現設計で十分

## 7. 後続タスクへの連絡事項

### SW1-006（SwedishG2PEngine メインAPI）担当者へ

- `StressAssigner.MarkStress()` は元の単語文字列と音節リスト（`string word, IReadOnlyList<SwedishSyllable> syllables`）を受け取る。エンジン内で `SwedishSyllabifier.Syllabify()` の結果をそのまま渡す設計
- `IpaConverter.Convert()` は `SwedishPronunciation` を受け取る。エンジン内で音素配列 + 音節 + ストレス情報をまとめた `SwedishPronunciation` を構築してから呼び出す
- `IpaConverter` はstaticメソッド群で実装。インスタンス生成不要
- `includeStress` パラメータにより `ToIPA` / `ToIPAWithoutStress` の両API を1つのConverterで対応可能

### SW1-007（テスト + 精度検証）担当者へ

- `StressAssigner` の単体テストは本チケットで15テスト作成済み。SW1-007では統合テスト（エンジン経由の精度検証）に集中すること
- IPA出力の期待値は ipa-dict (Folkets lexikon由来) の表記に合わせている。ipa-dictの声調マーク `²` はSw1時点では無視してよい
- ストレスマーク位置が ipa-dict と異なる場合、本チケットの `IpaConverter` 側を修正する必要がある可能性があるため、精度検証で不一致が見つかった場合は報告すること

### Sw3（ピッチアクセント）担当者へ

- `StressAssigner` にはSw3で `AssignAccent()` メソッドを追加する想定。MarkStress の後に呼ばれるフローを想定
- `SwedishSyllable` のストレス情報フィールドはmutableにしてあるため、AccentInfo の追加は後方互換で可能
- 外来語接尾辞の判定ロジック `IsLoanwordSuffix()` はSw3のアクセント予測でも再利用可能（外来語は常にAccent 1）
