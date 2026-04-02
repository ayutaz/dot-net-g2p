# SW3-004: PUA変換 + Prosody API

> **マイルストーン**: Sw3 — ピッチアクセント + 方言 + PUA + Prosody
> **前提チケット**: SW3-001（ピッチアクセント予測 — Prosody A1フィールドに必要）, SW3-003（方言対応 — FinlandSwedishのtj破擦音PUA対応）
> **後続チケット**: SW3-005（SwedishProsodyTests, SwedishPuaMappingTests で検証）

## 1. タスク目的とゴール

piper-plus互換のPUA（Private Use Area）変換と、ピッチアクセント情報を含むProsody APIを実装する。`SwedishPuaMapper.cs`、`SwedishProsodyInfo` struct、`SwedishProsodyResult` struct を新規作成し、`ToIpaWithProsody()`、`ToPuaPhonemes()`、`ToPuaString()` の各APIを `SwedishG2PEngine` に追加する。

**ゴール**: スウェーデン語音素のPUA変換が動作し、`ToIpaWithProsody()` がIPA音素列とともにピッチアクセント・ストレス・音節数の韻律情報を返す。

## 2. 実装内容の詳細

### 2.1 SwedishProsodyInfo struct（新規）

技術調査レポートの「4.4 Prosody API設計」に基づく:

```csharp
public readonly struct SwedishProsodyInfo : IEquatable<SwedishProsodyInfo>
{
    /// <summary>ピッチアクセント番号。0=不明, 1=accent 1, 2=accent 2。</summary>
    public int A1 { get; }

    /// <summary>ストレスレベル。0=なし, 1=primary, 2=secondary。</summary>
    public int A2 { get; }

    /// <summary>語の音節数。</summary>
    public int A3 { get; }

    public SwedishProsodyInfo(int a1, int a2, int a3)
    {
        A1 = a1;
        A2 = a2;
        A3 = a3;
    }

    // IEquatable<SwedishProsodyInfo> 実装
    public bool Equals(SwedishProsodyInfo other)
        => A1 == other.A1 && A2 == other.A2 && A3 == other.A3;

    public override bool Equals(object? obj)
        => obj is SwedishProsodyInfo other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(A1, A2, A3);

    public static bool operator ==(SwedishProsodyInfo left, SwedishProsodyInfo right)
        => left.Equals(right);

    public static bool operator !=(SwedishProsodyInfo left, SwedishProsodyInfo right)
        => !left.Equals(right);

    public override string ToString()
        => $"A1={A1}, A2={A2}, A3={A3}";
}
```

**フィールド設計根拠**:
- A1（ピッチアクセント）: SW3-001 の `StressAssigner.AssignAccent()` の出力をマッピング。FinlandSwedish方言では常に0
- A2（ストレス）: `StressAssigner.MarkStress()` の出力をマッピング。0=非ストレス, 1=primary, 2=secondary（複合語の第二要素）
- A3（音節数）: `Syllabifier.Syllabify()` の結果からカウント

**型の選択根拠**: A1/A2/A3 の型は `int` とする。既存の `KoreanProsodyInfo` との一貫性を維持するため。`byte` ではなく `int` を採用することで、将来的な拡張（セカンダリストレス等の追加レベル）にも対応しやすい。

### 2.2 SwedishProsodyResult struct（新規）

```csharp
public readonly struct SwedishProsodyResult
{
    /// <summary>IPA音素文字列。</summary>
    public string Ipa { get; }

    /// <summary>音素配列（構造化）。</summary>
    public IReadOnlyList<SwedishPhoneme> Phonemes { get; }

    /// <summary>語ごとの韻律情報配列。</summary>
    public IReadOnlyList<SwedishProsodyInfo> ProsodyInfos { get; }

    public SwedishProsodyResult(
        string ipa,
        IReadOnlyList<SwedishPhoneme> phonemes,
        IReadOnlyList<SwedishProsodyInfo> prosodyInfos)
    {
        Ipa = ipa;
        Phonemes = phonemes;
        ProsodyInfos = prosodyInfos;
    }
}
```

### 2.3 SwedishPuaMapper.cs（新規）

スウェーデン語は多文字IPA音素が少なく、PUA追加は最小限:

| SwedishIpaPhoneme | IPA | PUA文字 | コードポイント | 備考 |
|-------------------|-----|---------|-------------|------|
| Sj | ɧ | (1文字) | 0x0267 | IPA標準文字のためPUA不要 |
| Tj | ɕ | (1文字) | 0x0255 | IPA標準文字のためPUA不要 |
| TjAffricate | t͡ɕ | PUA | 0xE023 | FinlandSwedish方言。韓国語/中国語と共有 |
| RetroT | ʈ | (1文字) | 0x0288 | IPA標準文字のためPUA不要 |
| RetroD | ɖ | (1文字) | 0x0256 | IPA標準文字のためPUA不要 |
| RetroN | ɳ | (1文字) | 0x0273 | IPA標準文字のためPUA不要 |
| RetroL | ɭ | (1文字) | 0x026D | IPA標準文字のためPUA不要 |
| RetroS | ʂ | (1文字) | 0x0282 | IPA標準文字のためPUA不要 |
| LongVowels | (各)ː付き | PUA不要 | — | 長音記号はストレスマーク同様に後付け |

スウェーデン語は単一IPA文字で表現可能な音素が大半であり、PUA変換が必要な音素は `t͡ɕ`（FinlandSwedish方言のtj音破擦音）のみ。

**PuaMapper の主要メソッド**:

```csharp
public static class SwedishPuaMapper
{
    /// <summary>IPA音素文字列をPUA文字列に変換する。</summary>
    public static string MapToPuaString(IReadOnlyList<SwedishPhoneme> phonemes);

    /// <summary>IPA音素配列をPUA音素文字列配列に変換する。</summary>
    public static string[] MapToPuaPhonemes(IReadOnlyList<SwedishPhoneme> phonemes);
}
```

### 2.4 SwedishG2PEngine への API 追加

| メソッド | 戻り値 | 説明 |
|---------|--------|------|
| `ToPuaPhonemes(text)` | `string[]` | PUA音素配列 |
| `ToPuaString(text)` | `string` | PUA音素文字列（スペース区切り） |
| `ToPuaStringBatch(texts)` | `IReadOnlyList<string>` | バッチPUA変換 |
| `ToIpaWithProsody(text)` | `SwedishProsodyResult` | IPA + 韻律情報 |
| `ToIpaWithProsodyBatch(texts)` | `IReadOnlyList<SwedishProsodyResult>` | バッチ韻律変換 |

### 2.5 Prosody 情報の生成フロー

```
SwedishG2PEngine.ToIpaWithProsody(text) フロー:

1. 通常のG2Pパイプライン実行（Normalize→...→Format）
2. 各語ごとの SwedishPronunciation から ProsodyInfo を生成:
   - A1 = pronunciation.Accent (0/1/2)
   - A2 = pronunciation.StressLevel (0/1/2)
   - A3 = pronunciation.Syllables.Count
3. SwedishProsodyResult を構築して返却
```

### 2.6 既存パッケージとの一貫性

韓国語（`KoreanPuaMapper`, `KoreanProsodyInfo`）、ポルトガル語（`PortuguesePuaMapper`, `PortugueseProsodyInfo`）のパターンを踏襲する:

- ProsodyInfo は `readonly struct`（値型）
- ProsodyResult は IPA文字列 + 音素リスト + ProsodyInfo リストの3フィールド
- PuaMapper は static クラス
- バッチAPIは `BatchConversionHelper` を使用

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | SwedishProsodyInfo, SwedishProsodyResult, SwedishPuaMapper 新規作成、SwedishG2PEngine へのAPI追加 |
| テストエージェント | 1 | SwedishProsodyTests.cs（15件）, SwedishPuaMappingTests.cs（10件）作成 |
| レビューエージェント | 1 | 韓国語/ポルトガル語のPUA/Prosody APIとの一貫性確認、piper-plus互換性確認 |

**推奨**: 実装とテストを1人が兼任し、計2人（実装1＋レビュー1）で進行。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `Models/SwedishProsodyInfo.cs` 新規作成
- `Models/SwedishProsodyResult.cs` 新規作成
- `Conversion/SwedishPuaMapper.cs` 新規作成
- `SwedishG2PEngine.cs` に `ToPuaPhonemes()`, `ToPuaString()`, `ToPuaStringBatch()`, `ToIpaWithProsody()`, `ToIpaWithProsodyBatch()` 追加

**スコープ外**:
- Multilingual統合（Sw4で対応）
- piper-plus との実際のTTS連携テスト（将来拡張）
- NST辞書のSAMPA→PUA変換（将来拡張）

### ユニットテスト

**SwedishProsodyTests.cs**（15件）:

| テスト名 | 内容 |
|---------|------|
| A1_単音節語_Accent1_値は1 | hej → A1=1 |
| A1_Accent2接尾辞語_値は2 | hundar → A1=2 |
| A1_FinlandSwedish_値は0 | Finland方言で A1=0 固定 |
| A2_ストレス音節_Primary_値は1 | ストレス音節 → A2=1 |
| A2_非ストレス音節_値は0 | 非ストレス音節 → A2=0 |
| A2_複合語_Secondary_値は2 | 複合語第二要素 → A2=2 |
| A3_単音節語_値は1 | hej → A3=1 |
| A3_2音節語_値は2 | huset → A3=2 |
| A3_3音節語_値は3 | station → A3=3 |
| ToIpaWithProsody_IPA文字列が正しい | IPA出力が通常のToIPAと一致 |
| ToIpaWithProsody_ProsodyInfo配列長_語数と一致 | 語ごとに1つのProsodyInfo |
| ToIpaWithProsody_複数語_各語のProsodyInfoが正しい | 複数語テキストの各語検証 |
| ToIpaWithProsodyBatch_複数テキスト_正しい結果配列 | バッチ処理で結果配列の長さとContent確認 |
| ToIpaWithProsody_空文字_空結果 | 空文字入力で空結果 |
| ToIpaWithProsody_Dispose後_ObjectDisposedException | Dispose後にアクセスで例外 |

**SwedishPuaMappingTests.cs**（10件）:

| テスト名 | 内容 |
|---------|------|
| MapToPua_基本音素_正しいPUA文字 | 各音素 → 正しいPUA/IPA文字 |
| MapToPua_FinlandSwedish_tj破擦音_0xE023 | t͡ɕ → PUA 0xE023 |
| MapToPua_そり舌音_IPA標準文字 | ʈ,ɖ,ɳ,ɭ,ʂ → IPA標準文字（PUA不要） |
| MapToPua_sj音_IPA標準文字 | ɧ → IPA標準文字 |
| ToPuaPhonemes_配列長_音素数と一致 | 出力配列の長さが正しい |
| ToPuaString_スペース区切り | PUA文字列がスペース区切り |
| ToPuaStringBatch_複数テキスト | バッチ処理で正しい結果 |
| ApplyPuaMapping_空配列_空配列返却 | 空入力で空出力 |
| ToPuaPhonemes_長母音_ːマーク含む | 長母音のPUA表現にːが含まれる |
| ToPuaString_Central_FinlandSwedish_差異 | 方言によりPUA出力が異なるケースの確認 |

### E2Eテスト

- `SwedishG2PEngine.ToIpaWithProsody("hej världen")` が正しい IPA + ProsodyInfo を返すことを確認
- `SwedishG2PEngine.ToPuaString("köpa")` が正しい PUA 文字列を返すことを確認

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **PUAコードポイントの衝突**: `0xE023` を韓国語/中国語の破擦音 `t͡ɕ` と共有する。同一コードポイントが異なる言語で同じ音素を表すため問題はないが、Multilingual統合（Sw4）で言語切り替え時にPUAマッピングの整合性を確認する必要がある
2. **PUA変換の必要性が低い**: スウェーデン語は多文字IPA音素が少なく、PUA変換が必要な音素は `t͡ɕ`（FinlandSwedish方言のみ）だけ。Central方言ではPUA変換がほぼ恒等変換になる。それでもAPIの一貫性のために実装する
3. **ProsodyInfo のメモリ効率**: 各語ごとに `SwedishProsodyInfo` を生成するため、長文テキストではアロケーションが増加する。`readonly struct`（値型）のため、ヒープアロケーションは `IReadOnlyList` のバッキング配列のみ
4. **A2（ストレス）の粒度**: 語レベルではなく音節レベルでストレス情報を返すべきか。日本語G2Pの韻律情報（モーラレベル）との整合性を考慮し、語レベルで統一する

### レビューチェックリスト

- [ ] `SwedishProsodyInfo` が `readonly struct` で宣言され、`IEquatable<SwedishProsodyInfo>` を実装しているか（Equals, GetHashCode, operator ==, operator !=, ToString()）
- [ ] `SwedishProsodyInfo` の A1/A2/A3 が `int` 型であるか（`KoreanProsodyInfo` との一貫性）
- [ ] `SwedishProsodyResult` の3フィールド（Ipa, Phonemes, ProsodyInfos）が正しく設定されるか
- [ ] `SwedishPuaMapper` が static クラスか
- [ ] PUA `0xE023` が韓国語/中国語の同音素と同一コードポイントであることの確認
- [ ] `ToPuaPhonemes` と `ToPuaString` の出力が一貫しているか（配列 → join したものが文字列版と一致）
- [ ] バッチAPIが `BatchConversionHelper`（sync-shared-internals管理）を使用しているか
- [ ] `Dispose()` 後のAPI呼び出しで `ObjectDisposedException` がスローされるか
- [ ] 韓国語/ポルトガル語の ProsodyInfo/PuaMapper との API 命名・構造の一貫性

## 6. ゼロから作り直すとしたら

1. **ProsodyInfo を音節レベルにする**: 語レベルではなく音節レベルの韻律情報を返す設計にする。各音節にストレスレベル（0/1/2）とアクセント情報を付与し、TTS側でより細かい制御が可能にする
2. **PUA変換テーブルを外部リソース化する**: PUAマッピングをハードコードではなく TSV/JSON の埋め込みリソースとして管理する。piper-plus のバージョンアップ時にコード変更なしでマッピングを更新できる
3. **汎用 ProsodyResult<T> を導入する**: 各言語パッケージで独自の ProsodyInfo/ProsodyResult を定義するのではなく、`ProsodyResult<TProsodyInfo>` のようなジェネリック型を共通ライブラリで定義する
4. **IPA + Prosody を統合した出力形式を定義する**: 例えば IPA-Prosody 形式 `²ˈhʉn.dar`（アクセントマーク + IPA）のような統合表記を定義し、パース可能な文字列として返す

## 7. 後続タスクへの連絡事項

- **SW3-005（テスト）**: `SwedishProsodyTests.cs` と `SwedishPuaMappingTests.cs` のテストデータは、SW3-001（アクセント予測）とSW3-003（方言対応）の実装結果に依存する。これらが先に完了していることを前提としたテストデータを用意すること
- **Sw4（Multilingual統合）**: `MultilingualG2PEngine` に `ToIpaWithProsody` を追加する際、戻り値型が言語ごとに異なる（`SwedishProsodyResult` vs `JapaneseProsodyResult` 等）ため、`object` 型か言語別メソッドかの設計判断が必要。他言語パッケージの統合パターンに合わせること
- **piper-plus互換性**: piper-plusのスウェーデン語モデル（sv_SE-nst-medium, sv_SE-lisa-medium）が使用するPUA音素セットとの互換性を確認すること。Central方言ではPUA変換がほぼ恒等変換のため問題は少ないが、FinlandSwedish方言の `t͡ɕ` マッピングは要確認
- **バッチAPI**: `BatchConversionHelper` は `sync-shared-internals.ps1` で管理される共有コードであり、SW3-004の実装後に `sync-shared-internals.ps1 -Check` を実行して同期状態を確認すること
