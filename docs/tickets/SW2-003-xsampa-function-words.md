# SW2-003: X-SAMPA変換 + FunctionWordList

> **マイルストーン**: Sw2 — 例外辞書 + テキスト正規化 + X-SAMPA
> **前提チケット**: Sw1完了（SwedishIpaPhoneme enum確定）
> **後続チケット**: SW2-005（SwedishXSampaTests）

## 1. タスク目的とゴール

SwedishIpaPhoneme enum（41音素）のX-SAMPA表記マッピングを実装し、`ToXSampa()`/`ToXSampaWithoutStress()`/`ToXSampaBatch()` APIを追加する。また、ストレス除去対象の機能語リスト `FunctionWordList.cs` を実装し、自然なプロソディを持つ音素出力を実現する。

**ゴール**: `SwedishG2PEngine.ToXSampa("hej")` が正しいX-SAMPA文字列を返し、機能語（och, det, en, att 等）に対してストレスマークが付与されないこと。

## 2. 実装内容の詳細

### 2.1 追加ファイル

```
src/DotNetG2P.Swedish/
├── Conversion/
│   ├── XSampaConverter.cs       — X-SAMPA変換（41音素マッピング）
│   └── FunctionWordList.cs      — 機能語リスト（ストレス除去用）
```

### 2.2 XSampaConverter.cs — 41音素のX-SAMPAマッピング

| # | SwedishIpaPhoneme | IPA | X-SAMPA | 備考 |
|---|-------------------|-----|---------|------|
| 0 | LongI | iː | i: | |
| 1 | LongY | yː | y: | |
| 2 | LongU_Central | ʉː | u\`: | またはバー付きu |
| 3 | LongU | uː | u: | |
| 4 | LongE | eː | e: | |
| 5 | LongOe | øː | 2: | |
| 6 | LongEh | ɛː | E: | |
| 7 | LongO | oː | o: | |
| 8 | LongA | ɑː | A: | |
| 9 | ShortI | ɪ | I | |
| 10 | ShortY | ʏ | Y | |
| 11 | ShortU_Central | ɵ | 8 | |
| 12 | ShortU | ʊ | U | |
| 13 | ShortE | ɛ | E | |
| 14 | ShortOe | œ | 9 | |
| 15 | ShortO | ɔ | O | |
| 16 | ShortA | a | a | |
| 17 | Schwa | ə | @ | |
| 18 | P | p | p | |
| 19 | B | b | b | |
| 20 | T | t | t | |
| 21 | D | d | d | |
| 22 | K | k | k | |
| 23 | G | ɡ | g | |
| 24 | F | f | f | |
| 25 | V | v | v | |
| 26 | S | s | s | |
| 27 | H | h | h | |
| 28 | Sj | ɧ | x\\ | SAMPA for Swedish 準拠 |
| 29 | Tj | ɕ | s\\ | |
| 30 | M | m | m | |
| 31 | N | n | n | |
| 32 | Ng | ŋ | N | |
| 33 | L | l | l | |
| 34 | R | r | r | |
| 35 | J | j | j | |
| 36 | RetroT | ʈ | t` | |
| 37 | RetroD | ɖ | d` | |
| 38 | RetroN | ɳ | n` | |
| 39 | RetroL | ɭ | l` | |
| 40 | RetroS | ʂ | s` | |

**超分節素のX-SAMPA:**

| IPA | X-SAMPA | 用途 |
|-----|---------|------|
| ˈ | " | 一次ストレス |
| ˌ | % | 二次ストレス |
| ː | : | 長音 |
| . | . | 音節区切り |

**参照**: SAMPA for Swedish (https://www.phon.ucl.ac.uk/home/sampa/swedish.htm)

**実装:**

```csharp
internal static class XSampaConverter
{
    /// <summary>音素enum → X-SAMPA文字列</summary>
    public static string ToSymbol(SwedishIpaPhoneme phoneme);
    
    /// <summary>発音情報 → X-SAMPA文字列（ストレス付き）</summary>
    public static string Convert(SwedishPronunciation pronunciation, bool includeStress = true);
}
```

### 2.3 FunctionWordList.cs — 機能語ストレス除去用

自然発話ではストレスが置かれない（または弱化される）機能語のリスト。SwedishG2PEngine の出力時に、これらの語に対してストレスマーク（ˈ/ˌ）を除去する。

**機能語カテゴリと語彙:**

| カテゴリ | 語彙 |
|---------|------|
| 人称代名詞 | jag, du, han, hon, den, det, vi, ni, de |
| 目的格代名詞 | mig, dig, sig, honom, henne, oss, er, dem |
| 所有代名詞 | min, din, sin, hans, hennes, vår, er, deras |
| 冠詞 | en, ett, den, det, de |
| 前置詞 | i, på, av, till, med, för, om, ur, vid, hos, mot, från, under, över, efter, utan, mellan, genom |
| 接続詞 | och, att, eller, men, om, som, när, medan, fast, ty |
| 助動詞 | är, var, har, hade, ska, skulle, kan, kunde, vill, ville, måste, bör, får, fick |
| 副詞（弱形） | inte, så, då, ju, nog, väl, här, där |

**実装:**

```csharp
internal static class FunctionWordList
{
    /// <summary>指定語が機能語であるかを判定</summary>
    public static bool IsFunctionWord(ReadOnlySpan<char> word);
    
    // 内部: HashSet<string> による高速ルックアップ
    // SW2-001 の例外辞書 function_word カテゴリと整合させること
}
```

**SwedishG2PEngine への統合:**

```csharp
// ストレス付与後の後処理
if (FunctionWordList.IsFunctionWord(word))
{
    // ストレスマークを除去（文中の機能語は弱化）
    pronunciation = pronunciation.WithoutStress();
}
```

### 2.4 SwedishG2PEngine への追加API

```csharp
// 追加 Public API
public string ToXSampa(string text);
public string ToXSampaWithoutStress(string text);
public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts);
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | XSampaConverter.cs（41音素マッピング）、FunctionWordList.cs、SwedishG2PEngine API追加 |

**計1名**。既存パッケージ（ポルトガル語49音素マッピング、フランス語40音素マッピング）の XSampaConverter と FunctionWordList の実装パターンを直接参考にできるため、1名で十分。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**IN:**
- `XSampaConverter.cs`（41音素のX-SAMPAマッピング + 超分節素変換）
- `FunctionWordList.cs`（機能語70-80語のハードコードリスト + IsFunctionWord判定）
- `SwedishG2PEngine` への `ToXSampa()`/`ToXSampaWithoutStress()`/`ToXSampaBatch()` 追加

**OUT:**
- 例外辞書（SW2-001）
- テキスト正規化（SW2-002）
- テスト作成（SW2-005）

### ユニットテスト

SW2-005 で以下をカバー（本チケットでは実装対象外だがAPI設計の参考として記載）:

**SwedishXSampaTests.cs（15テスト）:**
- `ToSymbol` 各音素→正しいX-SAMPA記号（全41音素）
- 長母音の `:` 付与: LongI → `i:`、LongA → `A:` 等
- そり舌音のバッククォート: RetroT → `` t` ``、RetroD → `` d` `` 等
- sj音/tj音: Sj → `x\\`、Tj → `s\\`
- ストレスマーク: ˈ → `"`、ˌ → `%`
- `Convert` 発音情報→X-SAMPA文字列（ストレス付き/なし）
- `ToXSampa("hej")` → 期待されるX-SAMPA出力
- `ToXSampaWithoutStress` → ストレスマークなし出力
- `ToXSampaBatch` → 複数テキストのバッチ処理

**FunctionWordList テスト（SwedishG2PEngineTests.cs 内）:**
- 機能語 `och`/`det`/`en`/`att` にストレスマークがないこと
- 非機能語 `hej`/`bord`/`sjuk` にストレスマークがあること
- `IsFunctionWord` の境界テスト

### E2Eテスト

- `SwedishG2PEngine.ToXSampa("hej världen")` → 正しいX-SAMPA出力
- `SwedishG2PEngine.ToIPA("och hej")` → `och` にストレスなし、`hej` にストレスあり

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **sj音 `/ɧ/` のX-SAMPA表記**: SAMPA for Swedish では `x\\` を使用するが、一般的なX-SAMPAでは定義されていない音素。Amazon Polly Swedish phoneme table も参照して決定する
2. **中央円唇母音 `/ʉː/` `/ɵ/` のX-SAMPA**: これらはスウェーデン語固有の母音で、標準X-SAMPAでは `` u\` `` と `8` が最も近い。SAMPA for Swedish 公式定義を優先する
3. **FunctionWordList と例外辞書の整合性**: SW2-001 の例外辞書 `function_word` カテゴリと FunctionWordList の語彙が矛盾しないよう注意。例外辞書は「発音の不規則性」を担当し、FunctionWordList は「ストレス除去」を担当する。同じ語（och, det 等）が両方に存在するが、役割が異なる
4. **ストレス除去の適用範囲**: 機能語であっても、文の焦点（フォーカス）位置にある場合はストレスが付く。Sw2 時点ではこの文脈依存処理は行わず、一律にストレス除去する。文脈依存処理は将来拡張

### レビューチェックリスト

- [ ] 41音素すべてのX-SAMPAマッピングが SAMPA for Swedish 仕様に準拠しているか
- [ ] 超分節素（ストレス、長音、音節区切り）のX-SAMPA変換が正しいか
- [ ] FunctionWordList の語彙がスウェーデン語文法の主要機能語をカバーしているか
- [ ] FunctionWordList と SW2-001 例外辞書 `function_word` カテゴリの語彙に矛盾がないか
- [ ] `ToXSampa()`/`ToXSampaWithoutStress()`/`ToXSampaBatch()` のAPI署名が既存パッケージ（ポルトガル語等）と一貫しているか
- [ ] 機能語のストレス除去が SwedishG2PEngine のパイプライン内で正しいタイミング（ストレス付与後）で適用されているか
- [ ] X-SAMPAのエスケープ文字（バックスラッシュ `\`、バッククォート `` ` `` 等）が文字列内で正しくエスケープされているか

## 6. ゼロから作り直すとしたら

1. **X-SAMPAマッピングの実装方法**: `switch` 式（C# 8.0+）で41音素をマッピングするのが最もシンプル。配列ベースのルックアップ（`byte` → `string[]` インデックスアクセス）も高速だが、41音素程度では switch 式の可読性を優先する
2. **FunctionWordList の実装方法**: `HashSet<string>` を `static readonly` で保持する方式が最もシンプルかつ高速。フランス語・ポルトガル語の既存実装と同一パターン。将来的に機能語リストを外部化（TSV等）する拡張も検討できるが、70-80語程度ではハードコードで十分
3. **SAMPA vs X-SAMPA**: SAMPA for Swedish の公式定義と X-SAMPA 標準には微妙な差異がある（特に ɧ, ʉ 等のスウェーデン語固有音素）。本実装では X-SAMPA 標準をベースとし、標準で定義されない音素のみ SAMPA for Swedish の記号を採用する

## 7. 後続タスクへの連絡事項

- **SW2-005（テスト）**: SwedishXSampaTests.cs では 41音素すべての個別マッピングテスト + 発音情報→X-SAMPA文字列変換のE2Eテストを作成すること。SAMPA for Swedish の公式資料 (https://www.phon.ucl.ac.uk/home/sampa/swedish.htm) をリファレンスとして使用
- **Sw3（PUA変換）**: XSampaConverter と SwedishPuaMapper は並行する出力形式変換。Sw3 で PuaMapper を実装する際、XSampaConverter のマッピングテーブル構造を参考にする
- **Sw4（Multilingual統合）**: FunctionWordList の語彙は TextSegmenter の言語判定シグナル（`s_swedishWordSignals`）と一部重複する（och, att, det 等）。両者の役割は異なる（言語判定 vs ストレス除去）が、語彙更新時に整合性を保つこと
