# SW4-001: Multilingual統合（Language.Swedish）

> **マイルストーン**: Sw4 — Multilingual統合 + 評価ツール + リリース準備
> **前提チケット**: なし（Sw3完了が前提）
> **後続チケット**: SW4-002, SW4-005

## 1. タスク目的とゴール

DotNetG2P.Multilingual パッケージにスウェーデン語（Language.Swedish = 7）を追加し、8言語対応を完成させる。MultilingualG2PEngine 経由でスウェーデン語G2Pが動作し、既存7言語との共存を維持する。

**完了の定義:**
- `Language.Swedish == 7` が定義される
- `MultilingualG2PEngine` 経由でスウェーデン語G2Pが動作する
- `MultilingualG2POptions` に `SwedishG2POptions?` プロパティが追加される
- `CapabilityAdapters.cs` にスウェーデン語エンジンが登録される
- csproj/asmdef/package.json の依存が正しく追加される
- 既存7言語のテストが全てパスする（リグレッションなし）

## 2. 実装内容の詳細

### 2.1 Language.cs（Swedish = 7 追加）

```csharp
// src/DotNetG2P.Multilingual/Language.cs
/// <summary>スウェーデン語</summary>
Swedish = 7,
```

**注意**: 既存の Korean = 6 の次の値として 7 を割り当てる。

### 2.2 MultilingualG2PEngine.cs

- `Lazy<SwedishG2PEngine>` フィールドを追加
- コンストラクタで `SwedishG2POptions` を受け取り、Lazy初期化ファクトリを設定
- `ToIPA()` / `ToPhonemes()` 等のメソッドで `Language.Swedish` case を追加
- `Dispose()` で SwedishG2PEngine も適切に解放

```csharp
private readonly Lazy<SwedishG2PEngine> _swedishEngine;

// コンストラクタ内
_swedishEngine = new Lazy<SwedishG2PEngine>(() =>
    new SwedishG2PEngine(options.SwedishOptions ?? new SwedishG2POptions()));
```

### 2.3 MultilingualG2POptions.cs

- `SwedishG2POptions?` プロパティ追加
- コンストラクタに `swedishOptions` パラメータ追加
- `DefaultLatinLanguage` のバリデーションに `Language.Swedish` を許容に追加（**本チケットのスコープ内**: 現在のバリデーションは `English/Spanish/French/Portuguese` のみ許容しており、Swedish 追加が必要）

```csharp
/// <summary>スウェーデン語G2Pオプション（null時はデフォルト）。</summary>
public SwedishG2POptions? SwedishOptions { get; }
```

### 2.4 CapabilityAdapters.cs（Internal）

- `LanguageCapabilityRouter.Create()` **および `CreateLazy()`** の両メソッドに `SwedishG2PEngine`（`Create()`）/ `Lazy<SwedishG2PEngine>`（`CreateLazy()`）パラメータを追加
- SwedishG2PEngineはIPA対応のため、既存の英語/中国語/スペイン語/フランス語/ポルトガル語と同様に **`DelegateIpaTextBatchProcessor`** を使用してルーティングテーブルに登録する（韓国語の `DelegateTextBatchProcessor` とは異なる）
- `Language.Swedish` をルーティングテーブルに登録

### 2.4a DotNetG2P.Multilingual.csproj メタデータ更新

- `<Description>` に "Swedish" を追加: `"Multilingual G2P engine combining Japanese, English, Chinese, Korean, Spanish, French, Portuguese, and Swedish grapheme-to-phoneme support with automatic language detection."`
- `<PackageTags>` に "swedish" を追加: `g2p;multilingual;japanese;english;chinese;korean;hangul;spanish;french;portuguese;swedish;ipa;pinyin;mandarin;tts;phoneme;text-to-speech;unity`

### 2.5 csproj依存追加

```xml
<!-- src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.csproj -->
<ProjectReference Include="..\DotNetG2P.Swedish\DotNetG2P.Swedish.csproj" />
```

### 2.6 asmdef依存追加

```json
// src/DotNetG2P.Multilingual/DotNetG2P.Multilingual.asmdef
// references配列に追加:
"com.dotnetg2p.swedish"
```

### 2.7 package.json依存追加

```json
// src/DotNetG2P.Multilingual/package.json
// dependencies に追加:
"com.dotnetg2p.swedish": "1.x.x"
```

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装担当 | 1名 | Language.cs, MultilingualG2PEngine.cs, MultilingualG2POptions.cs, CapabilityAdapters.cs の変更 |
| レビュー担当 | 1名 | 既存7言語との整合性確認、Disposeパターン確認、Lazy初期化パターン確認 |

**合計: 2名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- Language.cs への Swedish = 7 追加
- MultilingualG2PEngine.cs への Lazy<SwedishG2PEngine> 統合
- MultilingualG2POptions.cs への SwedishG2POptions プロパティ追加 + DefaultLatinLanguage バリデーションへの Swedish 追加
- CapabilityAdapters.cs への スウェーデン語エンジン登録（`Create()` と `CreateLazy()` の両方、`DelegateIpaTextBatchProcessor` 使用）
- DotNetG2P.Multilingual.csproj の `<Description>` と `<PackageTags>` への "swedish" 追加
- csproj / asmdef / package.json の依存追加

**スコープ外:**
- TextSegmenter 言語判定ロジック（SW4-002）
- Multilingual テスト（SW4-005）
- 評価ツール（SW4-003）

### ユニットテスト

| テスト | 検証内容 |
|--------|---------|
| Language_Swedish_値は7 | `(byte)Language.Swedish == 7` |
| MultilingualG2POptions_SwedishOptions_正しく保持 | コンストラクタ経由で SwedishG2POptions が保持される |
| MultilingualG2POptions_DefaultLatinLanguage_Swedish許容 | DefaultLatinLanguage に Swedish を設定可能 |
| MultilingualG2PEngine_Swedish_Lazy初期化_使用まで未初期化 | SwedishG2PEngine が Lazy 初期化される |
| MultilingualG2PEngine_Swedish_ToIPA_正しい出力 | エンジン経由でスウェーデン語IPA出力が得られる |
| MultilingualG2PEngine_Dispose_SwedishEngine解放 | Dispose 後に SwedishG2PEngine も解放される |
| 既存7言語テスト_リグレッションなし | 既存テストが全てパスする |

### E2Eテスト

| テスト | 検証内容 |
|--------|---------|
| MultilingualG2PEngine_Swedish単独テキスト | `"hej"` → 正しい IPA 出力 |
| MultilingualG2PEngine_Swedish_バッチ変換 | 複数スウェーデン語テキストのバッチ処理 |

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **Dispose パターンの一貫性**: 既存7言語と同じ Dispose パターン（int + Interlocked.CompareExchange + Volatile.Read）を使用しているか。Lazy 未初期化の場合に Dispose で IsValueCreated チェックが必要
2. **DefaultLatinLanguage バリデーション**: 現在 English/Spanish/French/Portuguese のみ許容している箇所に Swedish を追加する必要がある。漏れるとランタイム例外が発生
3. **CapabilityAdapters の Create メソッドシグネチャ変更**: パラメータ追加により既存のテストコード（CapabilityAdapterTests.cs）も更新が必要

### レビューチェックリスト

- [ ] `Language.Swedish = 7` が定義され、byte基底の値が重複していない
- [ ] `MultilingualG2PEngine` の Lazy<SwedishG2PEngine> が他言語と同じパターンで初期化される
- [ ] `MultilingualG2POptions` の `SwedishOptions` プロパティが null 許容で宣言されている
- [ ] `DefaultLatinLanguage` バリデーションに Swedish が追加されている
- [ ] `CapabilityAdapters.Create()` および `CreateLazy()` の両方にスウェーデン語エンジンが追加されている
- [ ] スウェーデン語エンジンは `DelegateIpaTextBatchProcessor` で登録されている（IPA対応のため）
- [ ] DotNetG2P.Multilingual.csproj の `<Description>` に "Swedish" が追加されている
- [ ] DotNetG2P.Multilingual.csproj の `<PackageTags>` に "swedish" が追加されている
- [ ] csproj の ProjectReference が正しいパスで追加されている
- [ ] asmdef の references にスウェーデン語アセンブリが追加されている
- [ ] package.json の dependencies に com.dotnetg2p.swedish が追加されている
- [ ] Dispose で Lazy.IsValueCreated チェック後に SwedishG2PEngine.Dispose() が呼ばれる
- [ ] 既存7言語のテストが全てパスする

## 6. ゼロから作り直すとしたら

既存の Multilingual 統合パターン（ポルトガル語統合の PR#50 が直近のリファレンス）に完全に従う。Language enum 値の割り当て、Lazy 初期化パターン、CapabilityAdapters のアダプタクラス構造、Dispose パターンは全て既存コードからコピーして修正する形が最も安全。独自の判断でパターンを変更しない。

`MultilingualG2POptions` のコンストラクタパラメータ追加は破壊的変更になり得るが、全パラメータがデフォルト値付きの名前付きパラメータのため互換性は維持される。

## 7. 後続タスクへの連絡事項

- **SW4-002 へ**: `Language.Swedish` の byte 値は 7。TextSegmenter.cs の `LangSwedish` 定数は `8` とする（LangNone=0 からの連番で、Language enum の値 +1 のオフセットパターンに従う）。既存コードの LangKorean = 7 の次の値を確認すること
- **SW4-005 へ**: MultilingualG2PEngine 経由のテストは、本チケットの統合完了後に実施すること。`MultilingualG2POptions` に `swedishOptions` パラメータが追加されているため、テスト時はデフォルトオプションでの動作確認から始めること
- **全チケット共通**: `using DotNetG2P.Swedish;` の名前空間インポートが必要。SwedishG2PEngine は IDisposable を実装している前提
