# SW1-002: 音素・モデル定義

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-001（プロジェクト骨格が存在すること）
> **後続チケット**: SW1-003（SwedishIpaPhoneme を母音/子音判定で使用）, SW1-004（G2P規則が音素 enum を出力先として使用）

## 1. タスク目的とゴール

スウェーデン語G2Pの音素体系と関連データモデルを定義する。41音素（長母音9 + 短母音9 + 破裂音6 + 摩擦音6 + 鼻音3 + 接近音/ふるえ音3 + そり舌音5）を byte 基底の enum として定義し、音素・音節・発音・方言の struct/enum を実装する。後続チケット（SW1-003, SW1-004）がこれらの型を使って規則やユーティリティを構築するため、API 設計の正確性が極めて重要。

**完了状態**:
- SwedishIpaPhoneme enum が 41 音素を byte 値 0-40 で定義
- SwedishPhoneme struct が音素＋ストレス情報を保持し、IsVowel/IsConsonant 判定メソッドを提供
- SwedishPronunciation が音素配列＋音節配列＋ストレス情報をイミュータブルに保持
- SwedishSyllable が音節内の音素配列＋nucleus 位置を保持
- SwedishDialect enum が Central(0), FinlandSwedish(1) を定義
- SwedishG2POptions が Dialect プロパティで方言選択可能
- `dotnet build` 成功、テスト 10+ pass

## 2. 実装内容の詳細

### 新規作成ファイル

#### `src/DotNetG2P.Swedish/Models/SwedishIpaPhoneme.cs`

```csharp
public enum SwedishIpaPhoneme : byte
{
    // 長母音 (0-8)
    LongI = 0,           // iː
    LongY = 1,           // yː
    LongU_Central = 2,   // ʉː
    LongU = 3,           // uː
    LongE = 4,           // eː
    LongOe = 5,          // øː
    LongEh = 6,          // ɛː
    LongO = 7,           // oː
    LongA = 8,           // ɑː

    // 短母音 (9-17)
    ShortI = 9,          // ɪ
    ShortY = 10,         // ʏ
    ShortU_Central = 11, // ɵ
    ShortU = 12,         // ʊ
    ShortE = 13,         // ɛ
    ShortOe = 14,        // œ
    ShortO = 15,         // ɔ
    ShortA = 16,         // a
    Schwa = 17,          // ə

    // 破裂音 (18-23)
    P = 18, B = 19, T = 20, D = 21, K = 22, G = 23,

    // 摩擦音 (24-29)
    F = 24, V = 25, S = 26, H = 27, Sj = 28, Tj = 29,

    // 鼻音 (30-32)
    M = 30, N = 31, Ng = 32,

    // 接近音・ふるえ音 (33-35)
    L = 33, R = 34, J = 35,

    // そり舌音 (36-40)
    RetroT = 36, RetroD = 37, RetroN = 38, RetroL = 39, RetroS = 40,
}
```

- XML doc コメントで各メンバーに IPA 記号と例語を記載（例: `/// <summary>長母音 iː (例: sil 'ふるい')</summary>`）
- byte 基底により配列インデックスとしても使用可能

#### `src/DotNetG2P.Swedish/Models/SwedishPhoneme.cs`

`readonly struct` で以下を保持:
- `SwedishIpaPhoneme Value` — 音素 enum 値
- `bool IsStressed` — ストレス付きかどうか
- `bool IsPrimaryStress` — 一次ストレスか
- `bool IsSecondaryStress` — 二次ストレスか

ヘルパーメソッド:
- `bool IsVowel` — Value が 0-17（長母音・短母音範囲）かを判定
- `bool IsConsonant` — Value が 18-40 かを判定
- `bool IsLongVowel` — Value が 0-8 かを判定
- `bool IsShortVowel` — Value が 9-17 かを判定
- `bool IsRetroflex` — Value が 36-40 かを判定
- `bool IsSyllableNucleus` — IsVowel と同義（スウェーデン語では母音のみが核）
- `Equals`, `GetHashCode`, `==`, `!=` 演算子オーバーライド
- `ToString()` — IPA 記号文字列を返す

#### `src/DotNetG2P.Swedish/Models/SwedishPronunciation.cs`

`readonly struct` で以下を保持:
- `IReadOnlyList<SwedishPhoneme> Phonemes` — 音素配列
- `IReadOnlyList<SwedishSyllable> Syllables` — 音節配列
- `int StressedSyllableIndex` — ストレス音節のインデックス
- `int SyllableCount` — 音節数

#### `src/DotNetG2P.Swedish/Models/SwedishSyllable.cs`

`readonly struct` で以下を保持:
- `IReadOnlyList<SwedishPhoneme> Phonemes` — 音節内音素
- `int NucleusIndex` — 核（母音）のインデックス
- `int OnsetLength` — Onset 子音数
- `int CodaLength` — Coda 子音数
- `bool HasCoda` — Coda があるか
- `bool IsStressed` — ストレス音節か

#### `src/DotNetG2P.Swedish/Models/SwedishDialect.cs`

```csharp
public enum SwedishDialect : byte
{
    /// <summary>中央標準スウェーデン語（rikssvenska）。デフォルト。</summary>
    Central = 0,

    /// <summary>フィンランド・スウェーデン語（finlandssvenska）。
    /// そり舌音なし、ピッチアクセントなし、帯気なし。</summary>
    FinlandSwedish = 1,
}
```

### 変更ファイル

| ファイルパス | 変更内容 |
|-------------|---------|
| `src/DotNetG2P.Swedish/SwedishG2POptions.cs` | Dialect プロパティの型を SwedishDialect に変更（SW1-001のスタブから実型へ） |

### .meta ファイル

以下の新規 .cs ファイルに対して .meta ファイルを生成:
- SwedishIpaPhoneme.cs.meta
- SwedishPhoneme.cs.meta
- SwedishPronunciation.cs.meta
- SwedishSyllable.cs.meta
- SwedishDialect.cs.meta

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | enum/struct の定義、XML doc コメント、SwedishG2POptions 更新 |
| テストエージェント | 1 | SwedishPhonemeTests.cs の作成（10+ テスト） |

**推奨合計: 2名**（実装とテストは並行可能: 先に enum の API 仕様を合意してからテストを先行作成）

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含むもの**:
- SwedishIpaPhoneme enum（41音素、byte 基底）
- SwedishPhoneme struct（ストレス付き音素、IsVowel/IsConsonant 判定）
- SwedishPronunciation struct（発音情報のコンテナ）
- SwedishSyllable struct（音節情報のコンテナ）
- SwedishDialect enum（Central, FinlandSwedish）
- SwedishG2POptions の Dialect プロパティ実型化

**含まないもの**:
- IPA 文字列への変換ロジック（IpaConverter は SW1-004 で実装）
- 音素 enum 値から IPA 記号文字列へのマッピングテーブル（IpaConverter の責務）
- ProsodyInfo / ProsodyResult（Sw3）
- AllophoneFeatures（Sw3）

### ユニットテスト

**テストファイル**: `tests/DotNetG2P.Tests/SwedishG2P/SwedishPhonemeTests.cs`

| テストメソッド | 内容 |
|---------------|------|
| `SwedishIpaPhoneme_ByteValues_CorrectRange` | LongI=0, RetroS=40 の確認 |
| `SwedishIpaPhoneme_LongVowels_ValuesInRange0To8` | 長母音9個が 0-8 に収まることを確認 |
| `SwedishIpaPhoneme_ShortVowels_ValuesInRange9To17` | 短母音9個が 9-17 に収まることを確認 |
| `SwedishIpaPhoneme_Consonants_ValuesInRange18To40` | 子音23個が 18-40 に収まることを確認 |
| `SwedishPhoneme_IsVowel_TrueForVowels` | 長母音・短母音に対して true |
| `SwedishPhoneme_IsConsonant_TrueForConsonants` | 破裂音〜そり舌音に対して true |
| `SwedishPhoneme_IsLongVowel_TrueForLongOnly` | LongI〜LongA に対して true、ShortI 等は false |
| `SwedishPhoneme_IsRetroflex_TrueForRetroflexOnly` | RetroT〜RetroS に対して true |
| `SwedishPhoneme_IsSyllableNucleus_SameAsIsVowel` | IsVowel と同値であることを確認 |
| `SwedishPhoneme_Equals_SameValueAndStress_True` | 同一音素・同一ストレスで等値 |
| `SwedishPhoneme_Equals_DifferentValue_False` | 異なる音素で非等値 |
| `SwedishPhoneme_GetHashCode_ConsistentWithEquals` | 等値オブジェクトのハッシュ一致 |
| `SwedishDialect_Central_IsDefault` | (byte)Central == 0 |
| `SwedishDialect_FinlandSwedish_Value` | (byte)FinlandSwedish == 1 |

**テスト数: 14**

### E2Eテスト

- SwedishPhoneme を List に格納し、IsVowel でフィルタした結果が期待通りの母音集合であること
- SwedishPronunciation に音素配列と音節配列を設定し、SyllableCount が正しいこと

## 5. 懸念事項とレビュー項目

### 懸念事項

| 懸念 | 影響 | 対策 |
|------|------|------|
| `o` の長母音が /uː/ | 直感に反するため、LongU と LongO の使い分けで混乱しやすい | LongU は `/uː/`（書記素 o の長母音）、LongO は `/oː/`（書記素 å の長母音）と XML doc に明記 |
| ShortE と LongEh が同じ /ɛ/ | 長短の区別が IPA 記号だけでは曖昧 | ShortE = `/ɛ/`（短）、LongEh = `/ɛː/`（長）。enum名で区別、IPA変換時に長音記号で区別 |
| LongU_Central (`ʉː`) の命名 | `ʉ` は Central Swedish 特有の「中舌母音」で、命名に方言名を含めるか | 技術調査レポートでは `LongU_Central` を推奨。この名称を採用する |
| Schwa (`ə`) の必要性 | 弱化母音としての schwa はスウェーデン語で議論がある | enum に含めておき、使用しない場合は G2P 規則で出力しないだけ。将来の拡張余地を残す |
| readonly struct の配列フィールド | IReadOnlyList は参照型のため struct 内に保持すると boxing の可能性 | 既存パッケージ（SpanishPronunciation 等）と同一パターンを踏襲。パフォーマンスクリティカルなパスでは Span を別途検討（Sw3以降） |

### レビューチェックリスト

- [ ] SwedishIpaPhoneme: byte 基底、値 0-40 が連続で欠番なし
- [ ] SwedishIpaPhoneme: 各メンバーの XML doc に IPA 記号と例語を記載
- [ ] SwedishIpaPhoneme: 技術調査レポートの音素インベントリ（研究 2.4 節）と完全一致
- [ ] SwedishPhoneme: readonly struct、Equals/GetHashCode/演算子オーバーライド
- [ ] SwedishPhoneme: IsVowel 判定の範囲が 0-17（長母音+短母音）
- [ ] SwedishPhoneme: IsRetroflex 判定の範囲が 36-40
- [ ] SwedishPronunciation: IReadOnlyList で外部からの変更不可
- [ ] SwedishSyllable: NucleusIndex が音節内の正しい位置を指すこと
- [ ] SwedishDialect: Central=0 がデフォルト、FinlandSwedish=1
- [ ] SwedishG2POptions: Dialect プロパティが SwedishDialect 型
- [ ] 全ファイルに .meta が存在
- [ ] 既存パッケージ（SpanishIpaPhoneme, SpanishPhoneme 等）との API パターン整合性

## 6. ゼロから作り直すとしたら

音素 enum の設計は全言語パッケージで最も慎重な判断が必要な部分であり、一度確定すると変更コストが高い。改善案として:

1. **共通基底インターフェース `IIpaPhoneme`**: 全言語パッケージで `IsVowel`, `IsConsonant`, `ToIpaString()` を共通化できる。ただし struct にインターフェースを実装すると boxing が発生するため、ジェネリック制約(`where T : struct, IIpaPhoneme`) で回避する設計が必要
2. **ソースジェネレーター**: enum 定義から IsVowel/IsConsonant 等のヘルパーメソッドを自動生成する Roslyn Source Generator。7言語分のボイラープレート削減効果が大きい
3. **音素カテゴリを別途定義**: `[PhonemeCategory(Category.LongVowel)]` のようなカスタム属性で範囲チェックをハードコードせずに宣言的に記述

現時点では既存パッケージとの一貫性を優先し、手動の範囲チェックパターンを踏襲する。

## 7. 後続タスクへの連絡事項

1. **SW1-003 担当者へ**: `SwedishOrthography` で母音/子音判定に `SwedishIpaPhoneme` の範囲を使う場合は、書記素レベル（char）と音素レベル（enum）を明確に分離すること。SwedishOrthography は書記素レベルの判定（`IsVowelChar(char c)`）を提供し、`SwedishPhoneme.IsVowel` は音素レベルの判定。混同に注意
2. **SW1-004 担当者へ**: `GraphemeToPhonemeRules` は `SwedishIpaPhoneme` の値を直接使って音素リストを構築する。`LongU` (= `/uː/`, 書記素 `o` の長母音) と `LongO` (= `/oː/`, 書記素 `å` の長母音) の使い分けに注意。書記素 `o` → `LongU` / `ShortU` (`ʊ`) or `ShortO` (`ɔ`) は文脈依存
3. **IpaConverter 担当者へ**: enum 値から IPA 文字列へのマッピングテーブルは本チケットでは作成しない。SW1-004 で `IpaConverter.cs` に `static readonly string[]` としてマッピングを定義すること。`Sj` → `"ɧ"`, `Tj` → `"ɕ"`, `Ng` → `"ŋ"` 等の多文字 IPA 記号に注意
4. **長母音マーク**: IPA 出力時に長母音（0-8）には `ː` を付加する。enum 名の `Long*` プレフィックスが目印だが、実際の `ː` 付加は `IpaConverter` の責務
