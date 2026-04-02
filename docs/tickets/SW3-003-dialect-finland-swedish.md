# SW3-003: 方言対応（FinlandSwedish）

> **マイルストーン**: Sw3 — ピッチアクセント + 方言 + PUA + Prosody
> **前提チケット**: SW3-001（ピッチアクセント予測）, SW3-002（異音処理 + AllophoneFeatures）
> **後続チケット**: SW3-004（PUA変換でFinlandSwedishのtj音→破擦音PUA対応）, SW3-005（SwedishDialectTests で検証）

## 1. タスク目的とゴール

`SwedishDialect` enum（Central / FinlandSwedish）を活用し、方言別のG2P処理差異を実装する。FinlandSwedish方言ではそり舌化スキップ、ピッチアクセント無効化（A1=0固定）、tj音の破擦音化を行う。`SwedishG2POptions` に `EnableAllophones` と `AllophoneFeatures` プロパティを追加し、方言設定に基づくデフォルト値を自動適用する。

**ゴール**: `new SwedishG2PEngine(new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish))` で方言切替が動作し、出力が方言の音韻特徴を正しく反映する。

## 2. 実装内容の詳細

### 2.1 方言別処理差異（技術調査レポートより）

| 処理 | Central（デフォルト） | FinlandSwedish |
|------|---------------------|----------------|
| そり舌化 (rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ) | 適用 | **スキップ** → r+t, r+d, r+n, r+l, r+s のまま |
| ピッチアクセント | Accent 1/2 出力 | **A1=0 固定**（アクセント区別なし） |
| tj音 | 摩擦音 [ɕ] | **破擦音 [t͡ɕ]** |
| 帯気 | あり（将来拡張） | なし（将来拡張） |

**sj音の方言差異（Sw3スコープ外）**: sj音の方言差（Central: [ɧ], Finland: [ʃ]寄り）はSw3スコープ外とし将来拡張とする。

### 2.2 SwedishDialect enum の活用

Sw1で定義済みの `SwedishDialect` enum:

```csharp
public enum SwedishDialect : byte
{
    Central = 0,        // デフォルト
    FinlandSwedish = 1,
}
```

### 2.3 SwedishG2POptions 拡張

```csharp
public sealed class SwedishG2POptions
{
    // 既存（Sw1-Sw2）
    public SwedishDialect Dialect { get; }              // Central(default)
    public bool IncludeStress { get; }                  // default: true
    public bool EnableTextNormalization { get; }         // default: true
    public bool EnableExceptionDictionary { get; }       // default: true
    public string Separator { get; }                     // default: " "

    // Sw3追加
    public bool EnableAllophones { get; }                // default: true
    public SwedishAllophoneFeatures AllophoneFeatures { get; }
    // ↑ default: Dialect に基づいて自動決定
    //   Central        → CentralDefault (Retroflexion | VowelLengthMarking)
    //   FinlandSwedish → FinlandDefault (VowelLengthMarking のみ)
}
```

**AllophoneFeatures のデフォルト値自動決定ロジック**:

```csharp
// コンストラクタでの処理
AllophoneFeatures = allophoneFeatures ?? Dialect switch
{
    SwedishDialect.Central => SwedishAllophoneFeatures.CentralDefault,
    SwedishDialect.FinlandSwedish => SwedishAllophoneFeatures.FinlandDefault,
    _ => SwedishAllophoneFeatures.CentralDefault,
};
```

`allophoneFeatures` が明示的に指定された場合はそれを優先し、null の場合は方言に基づくデフォルト値を使用する（ポルトガル語レビュー知見の nullable パターン踏襲）。

### 2.4 FinlandSwedish: そり舌化スキップ

SW3-002 で実装した `AllophoneProcessor` の `Retroflexion` フラグにより制御する:

- Central: `Retroflexion` フラグ有効 → そり舌化適用
- FinlandSwedish: `Retroflexion` フラグ無効 → そり舌化スキップ

**具体的な処理**:
- Phase 4 で正書法レベルのそり舌化は Central 前提で実行される
- FinlandSwedish 方言では `AllophoneProcessor` がそり舌音を歯茎音に戻す（ʈ→r+t, ɖ→r+d, ɳ→r+n, ɭ→r+l, ʂ→r+s）

### 2.5 FinlandSwedish: ピッチアクセント無効化

SW3-001 で実装した `StressAssigner.AssignAccent()` の結果を方言に基づいて上書きする:

```csharp
// SwedishG2PEngine のパイプライン内
if (options.Dialect == SwedishDialect.FinlandSwedish)
{
    pronunciation.Accent = 0; // A1=0固定（アクセント区別なし）
}
```

FinlandSwedish方言ではピッチアクセントの区別がないため、`AssignAccent()` は呼び出すがその後に A1=0 で上書きする。（`AssignAccent()` 自体に方言分岐を入れず、呼び出し元で上書きする設計。他の方言追加時の拡張性を考慮。）

### 2.6 FinlandSwedish: tj音の破擦音化

Central方言では tj/kj/k+軟母音 → 摩擦音 [ɕ] だが、FinlandSwedish方言では破擦音 [t͡ɕ] となる:

- `AllophoneProcessor` 内で、FinlandSwedish方言の場合に `ɕ` → `t͡ɕ` に変換する
- `t͡ɕ` は `SwedishIpaPhoneme` enum に追加するか、PUA変換時のみの処理とするか検討が必要
- PUA変換では `0xE023`（韓国語/中国語と共有の破擦音コードポイント）を使用

### 2.7 パイプライン統合

```
SwedishG2PEngine パイプライン（Sw3完了時）:

1. Normalize()
2. Tokenize()
3. ExceptionDictionary.TryLookup()
4. GraphemeToPhonemeRules.ConvertWord() — Phase 1-5（Central前提）
5. Syllabifier.Syllabify()
6. StressAssigner.MarkStress()
7. StressAssigner.AssignAccent()        ← SW3-001
8. if (FinlandSwedish) Accent = 0       ← SW3-003（本チケット）
9. AllophoneProcessor.Apply(features)    ← SW3-002 + SW3-003
10. Format (IPA / X-SAMPA / PUA)
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishG2POptions拡張、方言分岐ロジック、パイプライン統合 |
| テストエージェント | 1 | SwedishDialectTests.cs 作成（15件） |
| レビューエージェント | 1 | 他言語パッケージの方言実装パターンとの一貫性確認 |

**推奨**: 実装とテストを1人が兼任し、計2人（実装1＋レビュー1）で進行。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `SwedishG2POptions` に `EnableAllophones`, `AllophoneFeatures` プロパティ追加
- `AllophoneFeatures` のデフォルト値を方言に基づいて自動決定するコンストラクタロジック
- FinlandSwedish方言のピッチアクセント無効化（A1=0上書き）
- FinlandSwedish方言のtj音破擦音化
- `SwedishG2PEngine` パイプラインへの方言分岐統合

**スコープ外**:
- Scanian（南部）方言の対応（将来拡張）
- 帯気の有無（将来拡張）
- 語境界を越えたsandhi（将来拡張）
- sj音の方言差（Central: [ɧ], Finland: [ʃ]寄り）（将来拡張）

### ユニットテスト

`SwedishDialectTests.cs` に以下のテストを作成（15件）:

| テスト名 | 内容 |
|---------|------|
| Central_デフォルト設定_Dialectは0 | デフォルトオプションで Dialect == Central |
| Central_そり舌化あり_bord→buːɖ | Central方言で bord → buːɖ（そり舌化適用） |
| Central_ピッチアクセントあり_Accent1or2 | Central方言でアクセント情報が付与される |
| Central_tj音→ɕ | Central方言で kyrka → ɕʏrka（k+軟母音→摩擦音） |
| Finland_そり舌化なし_bord→buːrd | Finland方言で bord → buːrd（そり舌化スキップ） |
| Finland_rt維持_hjort→juːrt | Finland方言で hjort のrt維持 |
| Finland_rd維持 | rn, rl, rs も同様に維持 |
| Finland_ピッチアクセントなし_A1は0 | Finland方言で Accent == 0 |
| Finland_全語_A1は0固定 | 複数語でAccent=0を確認 |
| Finland_tj音→破擦音 | Finland方言で tj音が [t͡ɕ] に変化 |
| Options_Central_AllophoneFeatures自動設定 | Central → CentralDefault |
| Options_Finland_AllophoneFeatures自動設定 | Finland → FinlandDefault |
| Options_明示指定_AllophoneFeatures優先 | 明示指定が方言デフォルトを上書き |
| Options_EnableAllophones_false_異音処理スキップ | EnableAllophones=false で全異音処理スキップ |
| Dialect切り替え_同一テキスト_異なる出力 | Central と Finland で同一テキストの出力が異なることを確認 |

### E2Eテスト

- `SwedishG2PEngine` をCentral/FinlandSwedish両方言でインスタンス化し、同一テキストで異なるIPA出力を確認
- Central: `bord` → `buːɖ`, Finland: `bord` → `buːrd` の対比テスト

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **Phase 4 とAllophoneProcessorの二重処理**: Phase 4 で正書法レベルのそり舌化を行った後、FinlandSwedish方言ではAllophoneProcessorで「戻す」処理が必要。この「適用→戻し」のパターンは非効率だが、Central方言（デフォルト）のパフォーマンスを優先する設計として許容する
2. **tj音の破擦音 [t͡ɕ] の表現**: `SwedishIpaPhoneme` enum に破擦音 `t͡ɕ` を追加する必要があるか、AllophoneProcessor 内の変換のみで対応するかの設計判断。PUA変換（SW3-004）との整合性を考慮すると enum 追加が望ましい
3. **AllophoneFeatures のnullable設計**: `SwedishAllophoneFeatures?` として null=「方言デフォルト使用」を表現する場合、コンストラクタでの null チェックが必要。ポルトガル語の知見を参考にすること
4. **方言追加時の拡張性**: Scanian方言を将来追加する場合、そり舌化なし + 口蓋垂r + ピッチアクセントあり（1-peaked）という組み合わせが必要。現在の `SwedishAllophoneFeatures` フラグで対応可能か事前確認

### レビューチェックリスト

- [ ] `SwedishG2POptions` のコンストラクタで `AllophoneFeatures` のデフォルト値が方言に基づいて正しく設定されるか
- [ ] FinlandSwedish方言のピッチアクセント無効化がパイプラインの正しい位置で行われるか（`AssignAccent()` の後）
- [ ] そり舌化の「戻し」処理（FinlandSwedish時）が正しく動作するか（ʈ→r+t で2音素に展開される）
- [ ] tj音の破擦音化が `AllophoneProcessor` 内の正しい位置で行われるか
- [ ] 他言語パッケージ（ポルトガル語Brazilian/European、フランス語Metropolitan/Conservative等）の方言実装パターンとの一貫性
- [ ] `EnableAllophones = false` の場合に全異音処理がスキップされるか

## 6. ゼロから作り直すとしたら

1. **方言分岐をパイプラインの最初に配置する**: Phase 4 のそり舌化を方言フラグで制御し、FinlandSwedish方言ではPhase 4自体をスキップする。「適用→戻し」のパターンを避ける
2. **方言プロファイルクラスを導入する**: `SwedishDialect` enum の代わりに `ISwedishDialectProfile` インタフェースを定義し、各方言のルール差異を完全にカプセル化する。Central/FinlandSwedish/Scanian の3方言を最初からサポートする
3. **tj音の破擦音化を Phase 2（子音軟化）で処理する**: 方言によるtj音の発音差異は本質的にはG2P規則の差異であり、異音処理ではない。Phase 2 で方言フラグを参照し、k+軟母音→ɕ（Central）/ t͡ɕ（FinlandSwedish）を直接出力する
4. **AllophoneFeatures を方言プロファイルから自動導出する**: ユーザーが個別のフラグを操作するのではなく、方言選択だけで全ての音韻差異が自動設定される設計にする

## 7. 後続タスクへの連絡事項

- **SW3-004（PUA変換）**: FinlandSwedish方言の tj音 [t͡ɕ] は PUA `0xE023`（韓国語/中国語と共有）にマッピングする。`SwedishPuaMapper` でこの破擦音のPUAマッピングを実装すること
- **SW3-005（テスト）**: `SwedishDialectTests.cs` で Central/FinlandSwedish の出力差異を網羅的に検証する。ipa-dict評価はCentral方言で実施し、FinlandSwedish方言の評価は別途参照データ（手動キュレーション）で行う
- **Sw4（Multilingual統合）**: `MultilingualG2POptions` に `SwedishG2POptions?` を追加する際、方言設定が正しく伝達されることを確認するテストが必要。`TextSegmenter` の言語判定はCentral/FinlandSwedish両方とも「Swedish」として扱い、方言の区別は行わない
- **パイプライン統合順序**: FinlandSwedish方言のアクセント無効化は `StressAssigner.AssignAccent()` の直後に行うこと。AllophoneProcessor の前であること
