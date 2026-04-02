# SW3-001: ピッチアクセント予測

> **マイルストーン**: Sw3 — ピッチアクセント + 方言 + PUA + Prosody
> **前提チケット**: なし（Sw2完了が前提）
> **後続チケット**: SW3-003（方言対応でFinlandSwedishのアクセント無効化に依存）, SW3-004（Prosody APIのA1フィールドに依存）, SW3-005（テストで検証）

## 1. タスク目的とゴール

スウェーデン語固有のピッチアクセント（accent 1 / accent 2）を規則ベースで予測するロジックを `StressAssigner.cs` に追加する。スウェーデン語のピッチアクセントは「語アクセントはほぼ完全に冗長であり、ストレスパターンと接尾辞情報から導出可能」（Roll et al. 2022）とされており、接尾辞規則＋例外辞書の組み合わせで高精度な予測が可能。

**ゴール**: `StressAssigner.AssignAccent()` メソッドを追加し、各単語に対してAccent 1（acute）またはAccent 2（grave）を正しく付与する。例外辞書の `accent` フィールドが存在する場合はそれを優先する。

## 2. 実装内容の詳細

### 2.1 StressAssigner.cs への AssignAccent() メソッド追加

既存の `StressAssigner.MarkStress()` の後段に `AssignAccent()` を追加する。

```
AssignAccent() フロー:

1. 例外辞書の accent 情報を優先（TSVの accent 列が 1 or 2）
2. 単音節語チェック → Accent 1（常に。単音節語にAccent 2は不可能）
3. 複合語検出 → Accent 2（常に。複合語は例外なくAccent 2）
4. Accent 2 誘発接尾辞チェック:
   - -ar（複数形: hundar, bilar）
   - -or（複数形: flickor, bilder→bildor）
   - -te / -de（過去形: ringde, köpte）
   - -het（派生名詞: frihet, storhet）
   - -are（行為者接尾辞: lärare, arbetare）
   - -ande / -ende（現在分詞: springande, gående）
   - 語幹末尾 -e → Accent 2（2音節のネイティブ語のみ。外来語（cafe, garage等）は除外）
5. デフォルト → Accent 1
```

### 2.2 接尾辞パターンの実装

接尾辞チェックは `ReadOnlySpan<char>` を用いた文字列末尾比較で実装する。正規表現は使用しない（パフォーマンス要件）。

```csharp
// 例: 接尾辞チェックの疑似コード
private static bool HasAccent2Suffix(ReadOnlySpan<char> word)
{
    if (word.EndsWith("ande") || word.EndsWith("ende")) return true;
    if (word.EndsWith("are")) return true;
    if (word.EndsWith("het")) return true;
    if (word.EndsWith("ar") || word.EndsWith("or")) return true;
    if (word.EndsWith("te") || word.EndsWith("de")) return true;
    return false;
}
```

### 2.3 複合語検出

複合語は以下のヒューリスティクスで検出する:
- 例外辞書に `compound` カテゴリとして登録されている語
- 8文字以上かつ分割点が推定可能な語（将来的な拡張ポイント）
- 現時点では例外辞書の `category` フィールドによる検出を主とする

### 2.4 例外辞書 accent フィールドの活用

Sw2で作成済みの `swedish_exceptions.master.tsv` には `accent` 列（値: 1 or 2）が含まれている。`SwedishExceptionDictionary.TryLookup()` の戻り値に既にアクセント情報が含まれているため、`AssignAccent()` ではまずこの情報をチェックし、存在すればルール適用をスキップする。

### 2.5 accent 値の表現

`SwedishPronunciation` 構造体に `Accent` プロパティ（`byte` 型、0=不明/1=accent1/2=accent2）を追加する。

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | StressAssigner.AssignAccent() の実装、SwedishPronunciation への Accent プロパティ追加 |
| テストエージェント | 1 | StressAssignerTests.cs へのアクセント予測テスト20件追加 |
| レビューエージェント | 1 | 接尾辞規則の網羅性確認、最小対語テストの妥当性検証 |

**推奨**: 実装とテストを1人が兼任し、計2人（実装1＋レビュー1）で進行。

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `StressAssigner.cs` に `AssignAccent()` メソッド追加
- `SwedishPronunciation` に `Accent` プロパティ（byte: 0/1/2）追加
- G2Pパイプラインの `MarkStress()` 後に `AssignAccent()` を呼び出す統合
- IPA出力へのアクセントマーク反映はオプション（`SwedishG2POptions.IncludeAccentMark`）

**スコープ外**:
- 方言によるアクセント無効化（SW3-003で対応）
- Prosody API の A1 フィールドへのマッピング（SW3-004で対応）
- ipa-dict の `²` マークとの評価比較（SW3-005で対応）

### ユニットテスト

`StressAssignerTests.cs` に以下のテストを追加（+20件）:

| テスト名 | 内容 |
|---------|------|
| AssignAccent_単音節語_常にAccent1 | hej, bok, hund, bil → Accent 1 |
| AssignAccent_複合語_常にAccent2 | fotboll, järnväg → Accent 2 |
| AssignAccent_ar複数形_Accent2 | hundar, bilar, dagar → Accent 2 |
| AssignAccent_or複数形_Accent2 | flickor, faktorer → Accent 2 |
| AssignAccent_te過去形_Accent2 | köpte, ringde → Accent 2 |
| AssignAccent_de過去形_Accent2 | ringde, handlade → Accent 2 |
| AssignAccent_het派生名詞_Accent2 | frihet, storhet → Accent 2 |
| AssignAccent_are行為者_Accent2 | lärare, arbetare → Accent 2 |
| AssignAccent_ande現在分詞_Accent2 | springande, gående → Accent 2 |
| AssignAccent_ende現在分詞_Accent2 | kommende → Accent 2 |
| AssignAccent_en定冠詞_Accent1 | hunden, boken → Accent 1 |
| AssignAccent_er現在形_Accent1 | springer, kommer → Accent 1 |
| AssignAccent_外来語_Accent1 | station, telefon → Accent 1（例外辞書経由） |
| AssignAccent_例外辞書優先_辞書値を返す | 例外辞書に accent=2 登録語 → Accent 2 |
| AssignAccent_最小対語_anden_Accent1 | anden（アヒル）→ Accent 1 |
| AssignAccent_最小対語_anden_Accent2 | anden（精霊）→ Accent 2（例外辞書） |
| AssignAccent_最小対語_tomten_Accent1 | tomten（敷地）→ Accent 1 |
| AssignAccent_最小対語_tomten_Accent2 | tomten（サンタ）→ Accent 2（例外辞書） |
| AssignAccent_デフォルト_Accent1 | 規則にマッチしない2音節以上語 → Accent 1 |
| AssignAccent_語幹末尾e_Accent2 | pojke, ande のような2音節ネイティブ語 → Accent 2 |

### E2Eテスト

- `SwedishG2PEngine.ToIPA()` 経由でアクセント情報が `SwedishPronunciation.Accent` に正しく設定されることを確認
- IPA出力文字列にアクセントマーク（オプション有効時）が含まれることを確認

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **接尾辞の曖昧性**: `-ar` は複数形だけでなく動詞現在形（springer→spring**ar** は不正例）にも現れる。過剰にAccent 2を付与するリスクがある。対策: 例外辞書で主要な偽陽性語を登録する
2. **`-ar` 接尾辞の偽陽性リスク**: 動詞現在形 -ar（springer, talar等）はAccent 1であるべき。-ar接尾辞によるAccent 2判定は偽陽性リスクが高い。例外辞書での補正を前提とし、テストに動詞現在形のAccent 1検証を追加すること
3. **複合語検出の限界**: 正書法レベルでの複合語検出は完璧ではない。初期実装では例外辞書依存とし、将来的にヒューリスティクス拡張を検討
4. **最小対語の処理**: 同綴異音語（anden, tomten等）はコンテキストなしでは区別不可能。例外辞書にデフォルトアクセントを登録し、曖昧な場合はAccent 1をデフォルトとする
5. **パフォーマンス**: 接尾辞チェックは `ReadOnlySpan<char>.EndsWith()` で実装し、文字列アロケーションを避ける

### レビューチェックリスト

- [ ] `AssignAccent()` のフローが仕様通りの優先順序（例外辞書→単音節→複合語→接尾辞→デフォルト）になっているか
- [ ] 接尾辞パターンの網羅性（Roll et al. 2022の規則と照合）
- [ ] 例外辞書の accent 値が正しく読み込まれているか（TSVパース時のフィールドインデックス）
- [ ] `SwedishPronunciation.Accent` プロパティの追加が既存テストを破壊しないか
- [ ] Accent 2 誘発接尾辞の順序（長い接尾辞を先にマッチすること: -ande を -de より先に）
- [ ] 単音節語判定が音節分割結果に基づいているか（正書法ベースの文字数判定ではないか）

## 6. ゼロから作り直すとしたら

ピッチアクセント予測を最初から実装し直す場合、以下のアプローチを取る:

1. **NST辞書（822k語, CC0）のアクセント情報を統計分析する**: 接尾辞パターンとアクセントの相関を定量的に評価し、最も高い予測力を持つ接尾辞パターンのランキングを作成する
2. **決定木（手動）を構築する**: 接尾辞の長さ順にチェックする線形フローではなく、最初に音節数で分岐し、次に接尾辞パターンで分岐する決定木構造にする
3. **複合語検出にデコンパウンダを導入する**: 形態素分割を試み、分割可能な語を複合語と判定する。ただし .NET Standard 2.1 の制約内で実装可能な軽量版とする
4. **例外辞書のアクセント情報を自動生成する**: NST辞書のSAMPA表記からアクセント情報を抽出し、G2P規則で誤予測される語を自動的に例外辞書に登録するパイプラインを構築する

## 7. 後続タスクへの連絡事項

- **SW3-003（方言対応）**: `AssignAccent()` の戻り値が `SwedishPronunciation.Accent` に格納される。FinlandSwedish方言では `AssignAccent()` 呼び出し後に `Accent = 0` に上書きする処理が必要。`AssignAccent()` 自体に方言分岐を入れるか、呼び出し元で上書きするかはSW3-003で決定すること
- **SW3-004（Prosody API）**: `SwedishProsodyInfo.A1` は `SwedishPronunciation.Accent` の値をそのままマッピングする。A1=0（不明）, A1=1（accent 1）, A1=2（accent 2）
- **SW3-005（テスト）**: ipa-dict の `²` マーク（accent 2を示す）との比較評価が必要。評価時にアクセント込み/アクセント除外の両方のPERを計測すること
- **例外辞書拡充**: 最小対語（anden, tomten, buren等の約300対）のうち主要なものを例外辞書に登録する作業がSW3-005で発生する
