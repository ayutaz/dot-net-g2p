# SW1-003: 正書法ユーティリティ + 音節分割

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-001（プロジェクト骨格）, SW1-002（SwedishIpaPhoneme enum, SwedishPhoneme struct, SwedishSyllable struct）
> **後続チケット**: SW1-004（G2P規則エンジンが SwedishOrthography の母音/子音判定と SwedishSyllabifier を使用）

## 1. タスク目的とゴール

スウェーデン語の正書法ユーティリティ（`SwedishOrthography`）と音節分割器（`SwedishSyllabifier`）を実装する。正書法ユーティリティはスウェーデン語29文字のアルファベットに基づく書記素レベルの判定（軟母音/硬母音/母音/子音）を提供し、G2P規則エンジン（SW1-004）の前提となる。音節分割器はOnset最大化原則に基づいて音素列を音節に分割し、ストレス付与の基盤となる。

**完了状態**:
- `SwedishOrthography.IsSoftVowel('e')` → `true`, `IsHardVowel('a')` → `true` が動作
- `SwedishSyllabifier.Syllabify(phonemes)` が音素列を正しく音節分割
- テスト 30+ pass（正書法10+、音節分割20+）

## 2. 実装内容の詳細

### 新規作成ファイル

#### `src/DotNetG2P.Swedish/Rules/SwedishOrthography.cs`

`internal static class` として以下のメソッドを提供:

**軟母音/硬母音判定**（子音軟化規則 Phase2 の前提）:

| メソッド | 説明 | 対象文字 |
|---------|------|---------|
| `IsSoftVowel(char c)` | 軟母音（前舌母音）判定 | e, i, y, ä, ö |
| `IsHardVowel(char c)` | 硬母音（後舌母音）判定 | a, o, u, å |
| `IsVowelChar(char c)` | 母音文字判定（軟+硬） | a, e, i, o, u, y, å, ä, ö |
| `IsConsonantChar(char c)` | 子音文字判定 | a-ö のうち母音以外 |
| `IsSwedishLetter(char c)` | スウェーデン語アルファベット判定 | a-z, å, ä, ö |

**相補的数量法則のヘルパー**（Phase3 の前提）:

| メソッド | 説明 |
|---------|------|
| `IsFollowedByDoubleConsonant(ReadOnlySpan<char> word, int vowelIndex)` | 母音の後に二重子音（または子音連結）が続くか判定 |
| `IsOpenSyllableContext(ReadOnlySpan<char> word, int vowelIndex)` | 開音節コンテキスト（母音+単子音+母音、または語末母音）判定 |
| `CountFollowingConsonants(ReadOnlySpan<char> word, int index)` | 指定位置以降の連続子音数を返す |

**実装上の注意**:
- すべて `char` ベースの判定（小文字前提。大文字対応は呼び出し側の `ToLowerInvariant()` で処理）
- å (U+00E5), ä (U+00E4), ö (U+00F6) の Unicode コードポイントを明示的にチェック
- `ReadOnlySpan<char>` を使用して不要な文字列アロケーションを回避

#### `src/DotNetG2P.Swedish/Rules/SwedishSyllabifier.cs`

`internal static class` として以下のメソッドを提供:

```csharp
internal static class SwedishSyllabifier
{
    /// <summary>音素列を音節に分割する（Onset最大化原則）。</summary>
    public static IReadOnlyList<SwedishSyllable> Syllabify(IReadOnlyList<SwedishPhoneme> phonemes);
}
```

**Onset最大化アルゴリズム**:

1. 音素列から母音（nucleus）の位置を特定
2. 隣接する nucleus 間の子音連続（inter-vocalic cluster）を分割
3. 右側の音節に可能な限り多くの子音を Onset として割り当てる
4. 残りを左側の音節の Coda とする

**有効な Onset パターン**:

| 子音数 | 有効パターン |
|--------|-------------|
| 1子音 | 全子音（/h/ と /ɕ/ を除いた全子音が Onset 可能。/h/ は語頭のみ Onset） |
| 2子音 | pl, bl, pr, br, tr, dr, kl, gl, kr, gr, fr, fl, sl, sm, sn, sp, st, sk, sv, kv, tv, gn |
| 3子音 | spr, str, skr, spl, stl, skl |

**Coda 制約**:
- /h/ と /ɕ/ は Coda 不可（これらは Onset 専用）
- 他の全子音は Coda 可能

**エッジケース処理**:
- 母音なし（子音のみ）→ 全体を1音節として返す
- 単母音（子音+母音+子音）→ 1音節
- 語頭子音連結は常に同一音節の Onset

### .meta ファイル

以下の新規 .cs ファイルに対して .meta ファイルを生成:
- SwedishOrthography.cs.meta
- SwedishSyllabifier.cs.meta

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishOrthography + SwedishSyllabifier の実装 |
| テストエージェント | 1 | SwedishOrthographyTests + SwedishSyllabifierTests の作成 |

**推奨合計: 2名**（実装とテストは並行可能: API 仕様を先に合意しテストを先行作成する TDD スタイル推奨）

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含むもの**:
- SwedishOrthography: 軟母音/硬母音/母音/子音判定、相補的数量法則ヘルパー
- SwedishSyllabifier: Onset最大化による音節分割

**含まないもの**:
- G2P 規則（SW1-004）
- ストレス付与（SW1-004 の StressAssigner）
- テキスト正規化（Sw2）
- 書記素から音素への変換ロジック（SW1-004）

### ユニットテスト

#### テストファイル1: `tests/DotNetG2P.Tests/SwedishG2P/SwedishOrthographyTests.cs`

| テストメソッド | 内容 |
|---------------|------|
| `IsSoftVowel_e_ReturnsTrue` | 'e' → true |
| `IsSoftVowel_i_ReturnsTrue` | 'i' → true |
| `IsSoftVowel_y_ReturnsTrue` | 'y' → true |
| `IsSoftVowel_ä_ReturnsTrue` | 'ä' → true |
| `IsSoftVowel_ö_ReturnsTrue` | 'ö' → true |
| `IsSoftVowel_a_ReturnsFalse` | 'a'(硬母音) → false |
| `IsHardVowel_a_ReturnsTrue` | 'a' → true |
| `IsHardVowel_o_ReturnsTrue` | 'o' → true |
| `IsHardVowel_u_ReturnsTrue` | 'u' → true |
| `IsHardVowel_å_ReturnsTrue` | 'å' → true |
| `IsHardVowel_e_ReturnsFalse` | 'e'(軟母音) → false |
| `IsVowelChar_AllNineVowels_ReturnTrue` | a,e,i,o,u,y,å,ä,ö → 全て true |
| `IsVowelChar_Consonant_ReturnsFalse` | b,c,d... → false |
| `IsConsonantChar_TypicalConsonants_ReturnsTrue` | b,c,d,f,g,h,j,k,l,m,n,p,q,r,s,t,v,w,x,z → true |
| `IsConsonantChar_Vowel_ReturnsFalse` | a,e,i,o,u → false |
| `IsSwedishLetter_å_ReturnsTrue` | å → true |
| `IsSwedishLetter_Digit_ReturnsFalse` | '5' → false |
| `IsFollowedByDoubleConsonant_matt_ReturnsTrue` | "matt", vowelIndex=1 → true |
| `IsOpenSyllableContext_mata_ReturnsTrue` | "mata", vowelIndex=1 → true |
| `CountFollowingConsonants_dricka_ReturnsCorrectCount` | 子音クラスタのカウント |

**テスト数: 20**

#### テストファイル2: `tests/DotNetG2P.Tests/SwedishG2P/SwedishSyllabifierTests.cs`

| テストメソッド | 内容 |
|---------------|------|
| `Syllabify_SingleSyllable_CVC_OneSyllable` | /kat/ → [kat] |
| `Syllabify_SingleSyllable_CV_OneSyllable` | /ja/ → [ja] |
| `Syllabify_SingleSyllable_VC_OneSyllable` | /ɛn/ → [ɛn] |
| `Syllabify_TwoSyllables_CVCV_CorrectSplit` | /mata/ → [ma][ta] |
| `Syllabify_TwoSyllables_CVCCV_OnsetMaximized` | /vɪnter/ → [vɪn][ter] |
| `Syllabify_TwoSyllables_CVCCCV_ClusterSplit` | 子音3連続の分割 |
| `Syllabify_ThreeSyllables_CorrectSplit` | /ɧøːtɛbɔrj/ 等 |
| `Syllabify_OnsetCluster_pr_KeptTogether` | /pr/ は同一 Onset |
| `Syllabify_OnsetCluster_str_KeptTogether` | /str/ は同一 Onset |
| `Syllabify_OnsetCluster_spr_KeptTogether` | /spr/ は同一 Onset |
| `Syllabify_OnsetCluster_skr_KeptTogether` | /skr/ は同一 Onset |
| `Syllabify_InvalidOnset_SplitCorrectly` | 無効な Onset 組み合わせは分割される |
| `Syllabify_CodaConstraint_H_NotInCoda` | /h/ は Coda に入らない |
| `Syllabify_WordInitialCluster_AllInOnset` | 語頭子音連結は全て Onset |
| `Syllabify_SingleVowel_OneSyllable` | /aː/ → [aː] |
| `Syllabify_NoVowel_ConsonantsOnly_OneSyllable` | 子音のみ → 1音節 |
| `Syllabify_RetroflexAsOne_NotSplit` | そり舌音は1音素として扱う |
| `Syllabify_NucleusIndex_CorrectPosition` | 各音節の NucleusIndex が正しい |
| `Syllabify_OnsetLength_CorrectCount` | 各音節の OnsetLength が正しい |
| `Syllabify_CodaLength_CorrectCount` | 各音節の CodaLength が正しい |
| `Syllabify_EmptyInput_EmptyResult` | 空配列 → 空結果 |

**テスト数: 21**

### E2Eテスト

- SwedishOrthography の判定結果を使って、文字列を母音/子音に分類し、期待通りの分類結果が得られること
- SwedishSyllabifier に既知の単語の音素列を渡し、期待通りの音節数・音節構造が返ること

## 5. 懸念事項とレビュー項目

### 懸念事項

| 懸念 | 影響 | 対策 |
|------|------|------|
| `y` の分類 | スウェーデン語では `y` は**母音**（/yː/ or /ʏ/）。英語のように子音/母音の両方で使われることはない | IsSoftVowel, IsVowelChar ともに true を返す。IsConsonantChar は false |
| Onset テーブルの網羅性 | 有効な2子音 Onset の抜け漏れがあると音節分割が不正確になる | espeak-ng の sv_rules（823行）と Riad (2014) の音節構造記述を照合。テストで主要パターンをカバー |
| `h` の特殊扱い | /h/ は語頭でのみ Onset 可能、Coda 不可。語中の /h/ は前音節の Coda にも入らず次音節の Onset にもなりにくい | IsValidOnset で /h/ を語頭限定として扱う。語中 /h/ の出現は外来語に限定されるため例外辞書（Sw2）で対応 |
| そり舌音のOnset判定 | /ʈ/, /ɖ/ 等のそり舌音は Phase4 で生成されるが、音節分割時点で既に存在している前提 | SwedishSyllabifier は音素列（Phase4 適用後）を入力として受け取る設計。Phase4 前の音節分割は非サポート |
| `ReadOnlySpan<char>` の .NET Standard 2.1 互換 | `System.Memory` パッケージが必要な場合がある | .NET Standard 2.1 は `Span<T>` / `ReadOnlySpan<T>` を標準サポート。追加パッケージ不要 |

### レビューチェックリスト

- [ ] IsSoftVowel: e, i, y, ä, ö のみ true（大文字対応は不要、呼び出し側で小文字化）
- [ ] IsHardVowel: a, o, u, å のみ true
- [ ] IsVowelChar: 9文字（a,e,i,o,u,y,å,ä,ö）全てが true
- [ ] IsConsonantChar: 母音以外のスウェーデン語文字で true、母音で false
- [ ] å/ä/ö の Unicode コードポイント（U+00E5, U+00E4, U+00F6）が正しくチェックされていること
- [ ] IsFollowedByDoubleConsonant: 境界チェック（vowelIndex が配列末尾の場合）
- [ ] SwedishSyllabifier: 有効な2子音 Onset リストの網羅性（最低20パターン）
- [ ] SwedishSyllabifier: 有効な3子音 Onset リストの網羅性（最低6パターン: spr, str, skr, spl, stl, skl）
- [ ] SwedishSyllabifier: 母音なし入力に対して例外を throw しないこと
- [ ] SwedishSyllabifier: NucleusIndex が音節内で正しい位置を指すこと
- [ ] 全メソッドが `internal` または `internal static`（public API ではない）
- [ ] .meta ファイルが新規 .cs に対して存在

## 6. ゼロから作り直すとしたら

1. **正書法ルールのテーブル駆動化**: 現状は `IsSoftVowel` 等のメソッド内に文字集合をハードコードしているが、`ReadOnlySpan<char>` ベースのルックアップテーブル（`stackalloc` による初期化）にすれば、新言語追加時にデータだけの変更で済む

2. **音節分割の宣言的定義**: 有効な Onset パターンを文字列配列ではなく、`FrozenSet<(SwedishIpaPhoneme, SwedishIpaPhoneme)>` のようなタプルセットで定義すれば、型安全な Onset 判定が可能。ただし .NET Standard 2.1 では FrozenSet が使えないため `HashSet` で代替

3. **Syllabifier の戦略パターン化**: スペイン語/フランス語/ポルトガル語/スウェーデン語の音節分割は全て Onset 最大化原則に基づくが、有効 Onset テーブルが異なるだけ。共通の `OnsetMaximizingSyllabifier<TPhoneme>` ジェネリッククラスを定義し、Onset テーブルをパラメータ化すれば4言語で共有できる

現時点では既存パッケージとの一貫性を優先し、言語固有の static class として実装する。

## 7. 後続タスクへの連絡事項

1. **SW1-004 担当者へ**: `GraphemeToPhonemeRules` は `SwedishOrthography.IsSoftVowel()` を Phase2（子音軟化）で頻繁に呼び出す。パフォーマンス上の懸念はない（単純な char 比較）が、呼び出し前に必ず `char.ToLowerInvariant()` で小文字化すること
2. **SW1-004 担当者へ**: `SwedishSyllabifier.Syllabify()` は Phase4（そり舌化）**適用後**の音素列を入力として期待する。パイプラインの順序は `G2PRules(Phase1-5) → Syllabifier → StressAssigner` であること
3. **SW1-004 担当者へ**: `IsFollowedByDoubleConsonant()` と `IsOpenSyllableContext()` は Phase3（母音変換・相補的数量法則）で使用される。これらは**書記素レベル**の判定であり、音素レベルの判定ではないことに注意。Phase3 は書記素（入力文字列）を走査しながら音素を決定するため、書記素レベルのヘルパーが必要
4. **Sw2 担当者へ**: テキスト正規化（SwedishNormalizer）は SwedishOrthography の `IsSwedishLetter()` を使ってトークン境界判定を行う可能性がある。必要であれば `IsSwedishLetter` の対象にハイフン・アポストロフィ等を追加するかの判断が必要
5. **音節分割のデバッグ**: 音節分割の結果が不正確な場合、まず Onset テーブルの網羅性を疑うこと。espeak-ng の `sv_rules` ファイルに追加のパターンがある可能性がある
