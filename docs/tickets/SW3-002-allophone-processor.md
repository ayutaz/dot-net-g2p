# SW3-002: 異音処理 + AllophoneFeatures

> **マイルストーン**: Sw3 — ピッチアクセント + 方言 + PUA + Prosody
> **前提チケット**: なし（Sw2完了が前提）
> **後続チケット**: SW3-003（方言対応で AllophoneFeatures のデフォルト値を方言別に設定）, SW3-005（AllophoneProcessorTests, AllophoneEvaluationTests で検証）

## 1. タスク目的とゴール

スウェーデン語の方言別異音処理を行う `AllophoneProcessor.cs` と、異音規則の有効/無効を制御する `SwedishAllophoneFeatures.cs`（`[Flags]` enum）を新規作成する。Central方言とFinlandSwedish方言で異音処理の適用パターンが異なるため、フラグで柔軟に制御できる設計とする。

**ゴール**: `AllophoneProcessor.Apply()` メソッドにより、方言に応じた異音変換（そり舌化、母音長マーク、/r/前母音低下）を音素列に適用する。

## 2. 実装内容の詳細

### 2.1 SwedishAllophoneFeatures.cs（新規）

```csharp
[Flags]
public enum SwedishAllophoneFeatures : byte
{
    /// <summary>異音処理なし。</summary>
    None = 0,

    /// <summary>r + 歯茎子音 → そり舌音（rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ）。</summary>
    Retroflexion = 1 << 0,

    /// <summary>ストレス母音の長母音にːマークを付与。</summary>
    VowelLengthMarking = 1 << 1,

    /// <summary>/r/ 前の母音低下（ɛ→æ, œ→œ̞）。Sw3スコープ外。将来拡張として予約。</summary>
    RBeforeVowelLowering = 1 << 2,

    /// <summary>Central方言のデフォルト。</summary>
    CentralDefault = Retroflexion | VowelLengthMarking,

    /// <summary>FinlandSwedish方言のデフォルト。</summary>
    FinlandDefault = VowelLengthMarking,

    /// <summary>Sw3スコープ内の全異音処理を有効化。RBeforeVowelLoweringは将来拡張のため含まない。</summary>
    All = Retroflexion | VowelLengthMarking,
}
```

### 2.2 AllophoneProcessor.cs（新規）

```
AllophoneProcessor.Apply() フロー:

入力: SwedishPronunciation（音素配列 + 音節情報 + ストレス情報）
      SwedishAllophoneFeatures（有効な異音規則フラグ）

1. Retroflexion フラグ有効時:
   - 音素列を走査し、r + {t, d, n, l, s} の連続を検出
   - rt → ʈ, rd → ɖ, rn → ɳ, rl → ɭ, rs → ʂ に置換
   - 語境界を越えたsandhi（語末r + 次語頭の歯茎子音）は将来拡張

2. VowelLengthMarking フラグ有効時:
   - ストレス音節の母音を長短判定（相補的数量法則に基づく）
   - 長母音にːマーカーを付与（SwedishIpaPhoneme の LongI～LongA 範囲）
   - 短母音にはマーカーなし

3. RBeforeVowelLowering フラグ有効時:
   - /r/ の直前の母音を低下: ɛ → æ, œ → œ̞
   - Central方言でオプション的に観察される現象（デフォルト無効）
```

### 2.3 そり舌化の詳細

技術調査レポートに基づくそり舌化規則:

| 入力（正書法） | 入力（音素） | 出力（音素） | 例 |
|--------------|------------|------------|-----|
| rt | r + t | ʈ | hjort → juːʈ |
| rd | r + d | ɖ | bord → buːɖ |
| rn | r + n | ɳ | barn → bɑːɳ |
| rl | r + l | ɭ | Karl → kɑːɭ |
| rs | r + s | ʂ | fors → fɔʂː |

**Phase 4 との二重処理の方針**: 推奨設計を採用する。Phase 4 で正書法レベルのそり舌化を実行し、AllophoneProcessor は FinlandSwedish 方言での戻し処理のみを担当する。1→2音素展開（そり舌→r+歯茎: ʈ→r+t, ɖ→r+d, ɳ→r+n, ɭ→r+l, ʂ→r+s）は `List<SwedishPhoneme>` を新規構築する方式で実装する（in-place書き換え不可）。

### 2.4 SwedishG2POptions への統合

`SwedishG2POptions` に以下のプロパティを追加する（実際のオプション拡張はSW3-003で行うが、`AllophoneFeatures` の型定義はこのチケットで完了する）:

```csharp
public bool EnableAllophones { get; }                    // default: true
public SwedishAllophoneFeatures AllophoneFeatures { get; } // default: CentralDefault
```

### 2.5 既存パッケージのパターン踏襲

ポルトガル語（`PortugueseAllophoneFeatures`, `AllophoneProcessor`）やフランス語の異音処理と同一の設計パターンを踏襲する:

- `[Flags]` enum による異音規則のフラグ管理
- `AllophoneProcessor` クラスは static メソッド or インスタンスメソッドで `Apply()` を提供
- `SwedishG2PEngine` のパイプライン内で音節分割・ストレス付与の後に呼び出す

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishAllophoneFeatures.cs 新規作成、AllophoneProcessor.cs 新規作成 |
| テストエージェント | 1 | AllophoneProcessorTests.cs 作成（20件）、swedish_allophone_reference.tsv 作成 |
| レビューエージェント | 1 | ポルトガル語/フランス語の AllophoneProcessor との設計一貫性確認 |

**推奨**: 実装とテストを1人が兼任し、計2人（実装1＋レビュー1）で進行。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `SwedishAllophoneFeatures.cs` 新規作成（`[Flags]` enum）
- `AllophoneProcessor.cs` 新規作成（`Apply()` メソッド）
- G2Pパイプラインへの統合（`StressAssigner` の後に呼び出し）
- `tests/TestData/SwedishG2P/swedish_allophone_reference.tsv` 作成（15-20件の参照データ）

**スコープ外**:
- `SwedishG2POptions` への `EnableAllophones`/`AllophoneFeatures` プロパティ追加（SW3-003で実施）
- 方言別デフォルト値の切り替えロジック（SW3-003で実施）
- 語境界を越えたsandhi処理（将来拡張）

### ユニットテスト

`AllophoneProcessorTests.cs` に以下のテストを作成（20件）:

| テスト名 | 内容 |
|---------|------|
| Apply_Retroflexion有効_rt→ʈ | rt の音素列がそり舌音 ʈ に変換される |
| Apply_Retroflexion有効_rd→ɖ | rd → ɖ |
| Apply_Retroflexion有効_rn→ɳ | rn → ɳ |
| Apply_Retroflexion有効_rl→ɭ | rl → ɭ |
| Apply_Retroflexion有効_rs→ʂ | rs → ʂ |
| Apply_Retroflexion無効_rt維持 | FinlandSwedish設定でrt→rtのまま |
| Apply_Retroflexion無効_rd維持 | rd→rdのまま |
| Apply_Retroflexion無効_全そり舌音維持 | rn, rl, rs もすべて維持 |
| Apply_VowelLengthMarking有効_長母音にː | ストレス音節の長母音にːマーカー |
| Apply_VowelLengthMarking有効_短母音はマークなし | 短母音にはːなし |
| Apply_VowelLengthMarking無効_ːなし | フラグ無効時はー切のːマークなし |
| Apply_RBeforeVowelLowering有効_ɛ→æ | /r/前のɛがæに低下 |
| Apply_RBeforeVowelLowering有効_œ→œ̞ | /r/前のœがœ̞に低下 |
| Apply_RBeforeVowelLowering無効_ɛ維持 | フラグ無効時は低下なし |
| Apply_CentralDefault_そり舌化あり長母音マークあり | CentralDefault フラグで期待される変換 |
| Apply_FinlandDefault_そり舌化なし長母音マークあり | FinlandDefault フラグで期待される変換 |
| Apply_None_全処理スキップ | None フラグで音素列が変更されない |
| Apply_All_全処理適用 | All フラグで全異音処理が適用される |
| Apply_bord_Central_buːɖ | 「bord」Central方言 → buːɖ |
| Apply_bord_Finland_buːrd | 「bord」Finland方言 → buːrd |

### E2Eテスト

- `SwedishG2PEngine.ToIPA()` 経由で異音処理が反映されたIPA出力を確認
- 異音参照TSV（`swedish_allophone_reference.tsv`）に基づく完全一致検証

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **Phase 4 との二重処理（方針確定済み）**: Phase 4 で正書法レベルのそり舌化を実行し、AllophoneProcessor は FinlandSwedish 方言での戻し処理のみを担当する。1→2音素展開は `List<SwedishPhoneme>` を新規構築する方式で実装する（in-place書き換え不可）
2. **AllophoneFeatures の nullable 問題**: ポルトガル語レビュー知見として「AllophoneFeatures パラメータは nullable にすること（`default`==`None`==0 の曖昧さ回避）」がある。`SwedishAllophoneFeatures?` として null=「方言デフォルトを使用」、`None`=「全異音無効」を区別する
3. **長母音判定の正確性**: 相補的数量法則に基づく長短判定が Phase 3 で行われているが、AllophoneProcessor が受け取る音素列に長短情報が保持されているか確認が必要
4. **RBeforeVowelLowering の音素追加**: æ と œ̞ は `SwedishIpaPhoneme` enum（41音素）に含まれていない。RBeforeVowelLowering は Sw3 スコープ外とするため、enum 拡張は将来拡張時に実施する

### レビューチェックリスト

- [ ] `AllophoneProcessor.Apply()` の処理順序が正しいか（Retroflexion → VowelLengthMarking → RBeforeVowelLowering）
- [ ] Phase 4 との二重適用がないか
- [ ] `SwedishAllophoneFeatures` の `[Flags]` 値が正しいか（ビット演算の衝突なし）
- [ ] CentralDefault と FinlandDefault の組み合わせが技術調査レポートの方言差異テーブルと一致するか
- [ ] ポルトガル語の `AllophoneProcessor` との設計パターン一貫性
- [ ] `swedish_allophone_reference.tsv` の参照データが正確か
- [ ] AllophoneFeatures の nullable 設計（null vs None の区別）

## 6. ゼロから作り直すとしたら

1. **Phase 4 のそり舌化を AllophoneProcessor に統合する**: 正書法レベルの Phase 4 を廃止し、全てのそり舌化を AllophoneProcessor に移動する。これにより「方言フラグによるそり舌化スキップ」が自然に実装でき、Phase 4 との二重適用の懸念がなくなる
2. **異音処理をチェーンパターンで実装する**: 各異音規則を独立した `IAllophoneRule` インタフェースとして実装し、パイプラインにチェーンで追加するパターンにする。方言追加時の拡張性が向上する
3. **VowelLengthMarking を Phase 3 の一部とする**: 母音の長短判定と長音マーク付与は密結合であり、Phase 3 内で完結させる方が自然。AllophoneProcessor からは除外する
4. **RBeforeVowelLowering 用の異音音素を最初から enum に含める**: æ, œ̞ を enum 定義段階から含め、41→43音素にする

## 7. 後続タスクへの連絡事項

- **SW3-003（方言対応）**: `SwedishG2POptions` に `EnableAllophones` と `AllophoneFeatures` を追加する際、Central方言のデフォルト値を `CentralDefault = Retroflexion | VowelLengthMarking`、FinlandSwedish方言のデフォルト値を `FinlandDefault = VowelLengthMarking` とすること
- **SW3-005（テスト）**: `SwedishAllophoneEvaluationTests.cs` で `swedish_allophone_reference.tsv` を使用した異音プロファイル別の完全一致検証を行うこと。Central/Finland の両プロファイルで検証する
- **Phase 4 との整合性**: AllophoneProcessor 実装時に Phase 4 との役割分担を確定したら、本チケットの設計メモを更新すること。SW3-005 のテスト設計に影響する
- **enum 拡張（将来）**: RBeforeVowelLowering は Sw3 スコープ外。将来実装する際に異音音素（æ, œ̞）を enum に追加し、IpaConverter と XSampaConverter にもマッピングを追加すること
