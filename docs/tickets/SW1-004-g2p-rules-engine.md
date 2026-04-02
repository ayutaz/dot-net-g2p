# SW1-004: G2P規則エンジン（GraphemeToPhonemeRules 5フェーズ）

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: SW1-001（プロジェクト骨格）, SW1-002（SwedishIpaPhoneme, SwedishPhoneme, SwedishPronunciation, SwedishSyllable）, SW1-003（SwedishOrthography, SwedishSyllabifier）
> **後続チケット**: SW1-006（SwedishG2PEngine メインAPI — 本チケットの出力を統合）

## 1. タスク目的とゴール

スウェーデン語G2P変換の中核である5フェーズ規則エンジン（`GraphemeToPhonemeRules`）を実装する。書記素文字列を受け取り、5フェーズの変換規則を適用して音素列（`List<SwedishIpaPhoneme>`）を返す。

**注意**: 本チケットのスコープは `GraphemeToPhonemeRules.cs` のみ。StressAssigner、IpaConverter、SwedishG2PEngineの実装はそれぞれSW1-005、SW1-006が担当する。

**完了状態**:
- `GraphemeToPhonemeRules.ConvertWord("hej")` が正しい音素列を返す
- 5フェーズ（トリグラフ/ダイグラフ認識、子音軟化、母音変換、そり舌化、黙字処理）が正しく動作する
- テスト 40 pass（GraphemeToPhonemeRulesTests のみ）

## 2. 実装内容の詳細

### 新規作成ファイル

#### `src/DotNetG2P.Swedish/Rules/GraphemeToPhonemeRules.cs`

`internal static class` として、5フェーズのG2P変換を統合する:

```csharp
internal static class GraphemeToPhonemeRules
{
    /// <summary>書記素文字列を音素列に変換する。</summary>
    public static List<SwedishIpaPhoneme> ConvertWord(ReadOnlySpan<char> word);
}
```

**Phase 1: トリグラフ/ダイグラフ認識**

最長一致（greedy matching）で書記素列を消費する。優先順位はトリグラフ > ダイグラフ > 単一文字。

| 綴り | 音素 | consumed | 条件 |
|------|------|----------|------|
| stj | Sj (ɧ) | 3 | 常に |
| skj | Sj (ɧ) | 3 | 常に |
| sj | Sj (ɧ) | 2 | 常に |
| sk + 軟母音 | Sj (ɧ) | 2 | 次文字が e/i/y/ä/ö |
| tj | Tj (ɕ) | 2 | 常に |
| kj | Tj (ɕ) | 2 | 常に |
| ng | Ng (ŋ) | 2 | 常に |
| nk | Ng (ŋ) + K | 2 | 常に（2音素出力） |
| ck | K + K (kː) | 2 | 重子音として2音素出力 |
| dj | J (j) | 2 | 語頭のみ（黙字処理との統合） |
| gj | J (j) | 2 | 語頭のみ |
| hj | J (j) | 2 | 語頭のみ |
| lj | J (j) | 2 | 語頭のみ |

**Phase 2: 子音軟化**

Phase1 で消費されなかった k, g, sk を処理:

| 条件 | 入力 | 出力 | 例 |
|------|------|------|-----|
| k + 軟母音 | k | Tj (ɕ) | köpa → ɕøːpa |
| g + 軟母音 | g | J (j) | göra → jøːra |
| sk + 軟母音 | sk | Sj (ɧ) | sked → ɧeːd |
| k + 硬母音/子音 | k | K | katt → kat: |
| g + 硬母音/子音 | g | G | gata → ɡɑːta |
| sk + 硬母音/子音 | sk | S + K | skola → skuːla |

**注意**: Phase1 の sk+軟母音 とPhase2 の sk+軟母音 は同一規則だが、Phase1 でトリグラフ/ダイグラフとして先に処理される設計とする。Phase2 ではPhase1 で未処理の k/g のみを対象とする。

**Phase 3: 母音変換（相補的数量法則）**

書記素→音素マッピングに加え、長短母音の決定を行う:

| 書記素 | 長母音（開音節） | 短母音（閉音節） | 備考 |
|--------|---------------|---------------|------|
| a | LongA (ɑː) | ShortA (a) | |
| e | LongE (eː) | ShortE (ɛ) | |
| i | LongI (iː) | ShortI (ɪ) | |
| o | LongU (uː) | ShortU (ʊ) or ShortO (ɔ) | `o` の短母音は文脈依存。基本は ShortU、一部は ShortO |
| u | LongU_Central (ʉː) | ShortU_Central (ɵ) | |
| y | LongY (yː) | ShortY (ʏ) | |
| å | LongO (oː) | ShortO (ɔ) | |
| ä | LongEh (ɛː) | ShortE (ɛ) | 長短で音質が同じだが長さが異なる |
| ö | LongOe (øː) | ShortOe (œ) | |

**長短決定の規則**（`SwedishOrthography` のヘルパーを使用）:
1. ストレス音節 + 開音節コンテキスト（V+単子音+V、または語末V）→ **長母音**
2. ストレス音節 + 閉音節コンテキスト（V+CC 以上）→ **短母音**
3. 非ストレス音節 → 基本**短母音**（一部例外あり）
4. 語末母音 → **長母音**

**`o` の短母音の判断**:
- 基本: ShortU (ʊ)
- ort, ort- 等の語幹: ShortO (ɔ)
- Sw1 ではデフォルト ShortU とし、例外は Sw2 の例外辞書で対応

**Phase 4: そり舌化**

音素列を走査し、R + 歯茎子音のペアをそり舌音に置換:

| 入力ペア | 出力 | 例 |
|---------|------|-----|
| R + T | RetroT (ʈ) | hjort → juːʈ |
| R + D | RetroD (ɖ) | bord → buːɖ |
| R + N | RetroN (ɳ) | barn → bɑːɳ |
| R + L | RetroL (ɭ) | Karl → kɑːɭ |
| R + S | RetroS (ʂ) | fors → fɔʂː |

**注意**: そり舌化は音素レベルで処理する（書記素レベルではない）。Phase1-3 で生成された音素列に対して適用する。

**Phase 5: 黙字処理**

Phase1 で語頭の dj/gj/hj/lj は既に処理されるが、追加の黙字パターンを処理:

| パターン | 処理 | 例 |
|---------|------|-----|
| 語末 -ig | g 黙字、/ɪ/ → /ɪ/ のまま | rolig → ruːlɪ |
| 語末 -lig | g 黙字 | trevlig → treːvlɪ |
| 語末 -igt | g 黙字、t 保持 | roligt → ruːlɪt |

### .meta ファイル

以下の新規 .cs ファイルに対して .meta ファイルを生成:
- GraphemeToPhonemeRules.cs.meta

### テストデータファイル

| ファイルパス | 内容 |
|-------------|------|
| `tests/DotNetG2P.Tests/SwedishG2P/` ディレクトリ | テストクラス群（下記参照） |

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | GraphemeToPhonemeRules.cs の5フェーズ実装 |
| テストエージェント | 1 | GraphemeToPhonemeRulesTests の作成（40テスト） |

**推奨合計: 2名**（G2P規則実装が最も工数が大きく、専任1名が必要。テストは実装と並行して進められる）

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含むもの**:
- GraphemeToPhonemeRules: 5フェーズG2P変換（トリグラフ/ダイグラフ認識、子音軟化、母音変換、そり舌化、黙字処理）
- 対応するユニットテスト（40テスト）

**含まないもの**:
- StressAssigner（基本ストレス付与） → SW1-005
- IpaConverter（IPA文字列変換） → SW1-005
- SwedishG2PEngine（メインAPI統合） → SW1-006
- 例外辞書（Sw2）
- テキスト正規化（Sw2）
- X-SAMPA 変換（Sw2）
- ピッチアクセント予測（Sw3）
- 方言別処理（Sw3）
- 異音処理（Sw3）
- PUA 変換（Sw3）
- Prosody API（Sw3）

### ユニットテスト

#### テストファイル1: `tests/DotNetG2P.Tests/SwedishG2P/GraphemeToPhonemeRulesTests.cs` (40テスト)

| テストグループ | テスト数 | 内容 |
|--------------|---------|------|
| Phase1_トリグラフ | 4 | stj→ɧ (stjärna), skj→ɧ (skjorta), sj→ɧ (sjuk, sjö) |
| Phase1_ダイグラフ | 6 | tj→ɕ (tjugo), kj→ɕ (kjol), ng→ŋ (lång), nk→ŋk (bank), ck→kː (bock), dj→j (djur) |
| Phase1_語頭黙字ダイグラフ | 3 | gj→j (gjord), hj→j (hjärta), lj→j (ljus) |
| Phase2_子音軟化_k | 4 | k+e→ɕ (kemi), k+i→ɕ (kina), k+ö→ɕ (köpa), k+a→k (katt) |
| Phase2_子音軟化_g | 4 | g+e→j (geni), g+i→j (gift), g+ö→j (göra), g+a→ɡ (gata) |
| Phase2_子音軟化_sk | 3 | sk+e→ɧ (sked), sk+i→ɧ (skina), sk+o→sk (skola) |
| Phase3_長母音 | 5 | mat→ɑː, hel→eː, sil→iː, hus→ʉː, öl→øː |
| Phase3_短母音 | 5 | matt→a, hell→ɛ, sill→ɪ, hund→ɵ, höst→œ |
| Phase3_oの特殊対応 | 2 | sol→uː (長), bott→ʊ (短) |
| Phase4_そり舌化 | 5 | rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ |
| Phase5_黙字_ig | 2 | rolig→rlɪ(g黙字), roligt→rlɪt(g黙字,t保持) |
| 統合テスト | 2 | 複数フェーズが連鎖する単語（例: skjorta で Phase1+3+4 が連鎖） |

**テスト合計: 40**（GraphemeToPhonemeRulesTests のみ。StressAssignerTests/SwedishIpaTests は SW1-005、SwedishG2PEngineTests は SW1-006、SwedishAccuracyTests/SwedishEdgeCaseTests は SW1-007 が担当）

### E2Eテスト

本チケットではE2Eテストは実施しない（エンジン統合はSW1-006が担当）。GraphemeToPhonemeRulesの単体テスト（ConvertWord の入出力検証）のみを実施する。

## 5. 懸念事項とレビュー項目

### 懸念事項

| 懸念 | 影響 | 対策 |
|------|------|------|
| Phase1とPhase2の重複 | sk+軟母音が Phase1（ダイグラフ）と Phase2（子音軟化）の両方で該当する | Phase1 で sk+軟母音を処理し consumed=2 で消費。Phase2 では Phase1 で未消費の k/g のみを対象とする。設計上は Phase1 が優先 |
| `o` の短母音の曖昧性 | `o` の短母音は /ʊ/ と /ɔ/ の2通りがあり、規則だけでは判別困難 | Sw1 ではデフォルト ShortU (ʊ) とし、ShortO (ɔ) が正解のケースは Sw2 で例外辞書に追加。PER への影響は限定的 |
| 相補的数量法則の例外 | 一部の語で長短母音が規則通りでない（例: `vara` は /ɑː/ だが閉音節的にも見える） | 規則ベースで最大限カバーし、例外は Sw2 の例外辞書で対応。Sw1 の PER 目標は < 15% で、規則のみで達成可能 |
| 語末 -ig/-lig の範囲 | `-ig` で終わる全ての語で g が黙字というわけではない（例: `fig` は /fiːɡ/） | `-ig` 黙字を機能語・形容詞接尾辞に限定するか、全語に適用するかの判断が必要。Sw1 では形態素境界なしで語末2文字が `-ig` かつ2音節以上の語に限定 |
| G (U+0261) vs g (U+0067) | IPA の `ɡ` は U+0261（Latin Small Letter Script G）だが、表示上は通常の g と区別困難 | 音素enum出力時に正しいenum値を使用すること。IpaConverter側での対応はSW1-005が担当 |

### レビューチェックリスト

- [ ] GraphemeToPhonemeRules: Phase1 のトリグラフ > ダイグラフ > 単一文字の優先順位が正しいこと
- [ ] GraphemeToPhonemeRules: Phase1 の consumed 値が正しいこと（skj=3, sj=2 等）
- [ ] GraphemeToPhonemeRules: Phase2 で Phase1 未消費の文字のみ対象としていること
- [ ] GraphemeToPhonemeRules: Phase3 で `SwedishOrthography.IsFollowedByDoubleConsonant` / `IsOpenSyllableContext` を正しく使用していること
- [ ] GraphemeToPhonemeRules: Phase4 のそり舌化が音素レベル（R+T→RetroT 等）で処理されていること
- [ ] GraphemeToPhonemeRules: Phase5 の -ig 黙字が2音節以上の語に限定されていること
- [ ] GraphemeToPhonemeRules: 境界チェック（`i + 1 < word.Length` 等）が全フェーズで正しいこと
- [ ] テスト: 40テストが全て pass すること

## 6. ゼロから作り直すとしたら

1. **フェーズの分離と合成の設計**: 現状は5フェーズを1つの `ConvertWord` メソッド内で逐次適用しているが、各フェーズを独立した `IPhase` インターフェースとして定義し、パイプラインとして合成する設計にすれば:
   - フェーズ単位のユニットテストが容易になる
   - フェーズの順序入れ替えや追加が柔軟になる
   - 方言別に適用フェーズを変更できる（FinlandSwedish では Phase4 をスキップ等）

2. **書記素→音素の変換表の外部化**: Phase1-3 の変換規則を C# コード内にハードコードするのではなく、TSV/JSON ファイルとして外部化し、埋め込みリソースとしてロードする設計にすれば、規則の追加・修正が再コンパイル不要になる。ただし、パフォーマンスとの トレードオフがある

3. **Phase3 の状態マシン化**: 相補的数量法則の判定は前後の文脈に依存するため、現状の `IsFollowedByDoubleConsonant` のような個別ヘルパーでは漏れが生じやすい。書記素列を走査する有限状態マシン（FSM）として形式化すれば、規則の網羅性を保証しやすい

4. **テスト駆動の規則追加**: ipa-dict の256語サンプルからテストケースを自動生成し、「レッドテスト→規則追加→グリーンテスト」のサイクルで規則を積み上げる TDD アプローチ。PER 目標に向けた進捗が定量的に追跡可能

## 7. 後続タスクへの連絡事項

### SW1-005（ストレス付与 + IPA変換）担当者へ

- `GraphemeToPhonemeRules.ConvertWord()` は `List<SwedishIpaPhoneme>` を返す。SW1-005の `StressAssigner` は、この音素列を `SwedishSyllabifier.Syllabify()` で音節分割した結果に対して適用する
- Phase4（そり舌化）は音素レベルで処理済み。IpaConverter は個々の enum 値をIPA記号に変換するだけで、追加のそり舌化処理は不要

### SW1-006（SwedishG2PEngine メインAPI）担当者へ

- パイプライン内の例外辞書挿入ポイントは `ConvertWord` 呼び出しの前。例外辞書にヒットした場合は `ConvertWord` をスキップする設計。Sw1時点ではフォールスルーで常に `ConvertWord` を通す

### Sw3 方言担当者へ

- Phase4（そり舌化）は FinlandSwedish 方言ではスキップする。`GraphemeToPhonemeRules.ConvertWord` に dialect パラメータを追加するか、呼び出し側（SwedishG2PEngine）で方言に応じて Phase4 を条件分岐するかの設計判断が必要。後者の方が既存コードへの影響が小さい
