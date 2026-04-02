# SW1-001: プロジェクト骨格構築

> **マイルストーン**: Sw1 — コアルールエンジン + 基本MVP
> **前提チケット**: なし（Sw1の最初のチケット）
> **後続チケット**: SW1-002, SW1-003, SW1-004（すべて本チケットの成果物に依存）

## 1. タスク目的とゴール

DotNetG2P.Swedish パッケージのプロジェクト骨格を構築する。`dotnet build` が成功し、空の SwedishG2PEngine クラスがインスタンス化できる状態を達成する。既存の他言語パッケージ（スペイン語/フランス語/ポルトガル語）と同一の構成パターンに従い、NuGet パッケージング、Unity UPM 対応、sync-shared-internals 整合性をすべて満たす。

**完了状態**:
- `dotnet build DotNetG2P.slnx` がエラーなく成功
- `sync-shared-internals.ps1 -Check` が pass
- `new SwedishG2PEngine()` がインスタンス化可能（メソッドは NotImplementedException でOK）
- Unity の .meta ファイルが全 .cs / ディレクトリ / asmdef / package.json に対して存在

## 2. 実装内容の詳細

### 新規作成ファイル

| ファイルパス | 内容 |
|-------------|------|
| `src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj` | netstandard2.1, IsPackable=true, PackageId=DotNetG2P.Swedish, InternalsVisibleTo=DotNetG2P.Tests。スペイン語 csproj をテンプレートとする |
| `src/DotNetG2P.Swedish/DotNetG2P.Swedish.asmdef` | Unity 用アセンブリ定義。name=DotNetG2P.Swedish, rootNamespace=DotNetG2P.Swedish, noEngineReferences=false |
| `src/DotNetG2P.Swedish/package.json` | UPM パッケージ定義。name=com.dotnetg2p.swedish, version=1.8.2, unity=2021.2, license=Apache-2.0 |
| `src/DotNetG2P.Swedish/README.md` | パッケージ概要（スウェーデン語G2Pライブラリ） |
| `src/DotNetG2P.Swedish/LICENSE.md` | Apache-2.0 ライセンス全文 |
| `src/DotNetG2P.Swedish/THIRD-PARTY-NOTICES.md` | サードパーティ帰属（ipa-dict CC BY-SA 2.5 等） |
| `src/DotNetG2P.Swedish/SwedishG2PEngine.cs` | メインエンジンクラスのスタブ。IDisposable 実装、Public API メソッドシグネチャ（ToPhonemes, ToIPA, ToIPAWithoutStress, ToPhonemeList, ToSyllables, ToPhonemesBatch, ToIPABatch, Dispose）。Sw1時点では NotImplementedException を throw |
| `src/DotNetG2P.Swedish/SwedishG2POptions.cs` | イミュータブルオプション。Dialect (Central default), IncludeStress (true), Separator (" ") |
| `src/DotNetG2P.Swedish/Internal/PreserveAttribute.cs` | sync-shared-internals 管理。既存パッケージと同一内容（`#if !UNITY_5_3_OR_NEWER` ガード付き） |
| `src/DotNetG2P.Swedish/Internal/BatchConversionHelper.cs` | sync-shared-internals 管理。既存パッケージと同一内容 |

### ディレクトリ構造（空ディレクトリ含む）

```
src/DotNetG2P.Swedish/
├── Conversion/          （空、後続チケットで IpaConverter.cs を追加）
├── Data/                （空、Sw2で例外辞書を追加）
├── Internal/
│   ├── BatchConversionHelper.cs
│   └── PreserveAttribute.cs
├── Models/              （空、SW1-002で音素・モデル定義を追加）
├── Normalization/       （空、Sw2で正規化を追加）
├── Rules/               （空、SW1-003/004でルールを追加）
├── DotNetG2P.Swedish.csproj
├── DotNetG2P.Swedish.asmdef
├── package.json
├── README.md
├── LICENSE.md
├── THIRD-PARTY-NOTICES.md
├── SwedishG2PEngine.cs
└── SwedishG2POptions.cs
```

### Unity .meta ファイル

以下のすべてに対して .meta ファイルを生成する（GUID は新規生成）:
- 各 .cs ファイル
- 各ディレクトリ（Conversion, Data, Internal, Models, Normalization, Rules）
- DotNetG2P.Swedish.asmdef
- package.json
- README.md, LICENSE.md, THIRD-PARTY-NOTICES.md
- DotNetG2P.Swedish.csproj（.meta で除外指定）

### 変更ファイル

| ファイルパス | 変更内容 |
|-------------|---------|
| `DotNetG2P.slnx` | `/src/` フォルダに `src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj` を追加 |

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装エージェント | 1 | csproj, asmdef, package.json, Engine/Options スタブ、Internal ファイルコピー、.meta 生成、slnx 編集 |
| レビューエージェント | 1 | 既存パッケージとの構成一致確認、sync-shared-internals 整合性、.meta の GUID 一意性確認 |

**推奨合計: 2名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

**含むもの**:
- プロジェクトファイル一式（csproj, asmdef, package.json）
- SwedishG2PEngine スタブ（メソッドシグネチャのみ、NotImplementedException）
- SwedishG2POptions クラス
- Internal/ 共有ファイル（PreserveAttribute, BatchConversionHelper）
- Unity .meta ファイル
- DotNetG2P.slnx への追加
- README.md, LICENSE.md, THIRD-PARTY-NOTICES.md

**含まないもの**:
- 音素 enum やモデル定義（SW1-002）
- G2P ルール実装（SW1-003, SW1-004）
- IPA 変換実装（SW1-004と並行）
- テキスト正規化（Sw2）
- 例外辞書（Sw2）

### ユニットテスト

本チケットではテストファイルの作成は不要（ビルド確認のみ）。ただし以下のスモークテストを手動確認する:

| 確認項目 | 期待結果 |
|---------|---------|
| `dotnet build DotNetG2P.slnx` | 成功（warning 0） |
| `dotnet build src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj` | 成功 |
| `sync-shared-internals.ps1 -Check` | pass |

### E2Eテスト

- `dotnet build` 後にアセンブリ `DotNetG2P.Swedish.dll` が `netstandard2.1` ターゲットで生成されること
- slnx にプロジェクトが含まれ、ソリューション全体ビルドが通ること

## 5. 懸念事項とレビュー項目

### 懸念事項

| 懸念 | 影響 | 対策 |
|------|------|------|
| sync-shared-internals の名前空間 | Internal/ ファイルの名前空間が `DotNetG2P.Internal` であること（言語固有名前空間ではない） | 既存パッケージと同一内容をコピー。スクリプトで検証 |
| .meta GUID 重複 | Unity でアセンブリ参照エラー | 全 .meta の GUID を新規生成し、既存パッケージの GUID と重複しないことを確認 |
| csproj の EmbeddedResource | Sw1 時点では Data/ が空のため不要だが、Sw2 で追加時に忘れやすい | csproj に TODO コメントで `EmbeddedResource` の追加箇所を明記 |
| noEngineReferences | v1.8.2 で false に変更された経緯（[Preserve] 属性使用のため） | asmdef で `noEngineReferences: false` を明示的に設定 |

### レビューチェックリスト

- [ ] csproj: TargetFramework=netstandard2.1, IsPackable=true, Nullable=enable
- [ ] csproj: PackageId, AssemblyName, RootNamespace が "DotNetG2P.Swedish" で統一
- [ ] csproj: InternalsVisibleTo=DotNetG2P.Tests
- [ ] csproj: Description, PackageTags が適切（swedish, g2p, tts, phoneme, ipa, text-to-speech, unity）
- [ ] asmdef: noEngineReferences=false（[Preserve] 属性互換）
- [ ] package.json: name=com.dotnetg2p.swedish, version=1.8.2, unity=2021.2
- [ ] Internal/PreserveAttribute.cs: 既存パッケージと完全一致
- [ ] Internal/BatchConversionHelper.cs: 既存パッケージと完全一致
- [ ] SwedishG2PEngine: IDisposable 実装、Dispose パターン（int + Interlocked.CompareExchange + Volatile.Read）
- [ ] SwedishG2POptions: イミュータブル（コンストラクタ引数 + get-only プロパティ）
- [ ] .meta ファイル: 全 .cs / ディレクトリ / asmdef / package.json に対して存在
- [ ] .meta GUID: 既存パッケージと重複なし
- [ ] DotNetG2P.slnx: `/src/` フォルダ内に正しいパスで追加
- [ ] `dotnet build DotNetG2P.slnx` 成功
- [ ] `sync-shared-internals.ps1 -Check` pass

## 6. ゼロから作り直すとしたら

既存7言語パッケージの骨格はほぼ同一のテンプレートパターンに収束しているため、言語名をパラメータとしたプロジェクト生成スクリプト（`dotnet new` カスタムテンプレート、または PowerShell/bash スクリプト）を用意すれば、骨格構築を完全自動化できる。具体的には:

1. `tools/new-language-package.ps1 -Language Swedish -LangCode sv -PhonemeCount 41` のようなスクリプトで csproj / asmdef / package.json / Engine スタブ / Options / Internal ファイル / .meta / slnx 編集を一括生成
2. テンプレート変数: `{Lang}`, `{lang}`, `{LANG}`, `{langcode}`, `{phoneme_count}`, `{version}`
3. .meta GUID は `[System.Guid]::NewGuid()` で自動生成

現状は手動コピー＆修正で対応するが、9言語目以降の追加効率を考えるとテンプレート化の投資対効果が高い。

## 7. 後続タスクへの連絡事項

1. **SW1-002 担当者へ**: `Models/` ディレクトリは空で作成済み。SwedishIpaPhoneme.cs 等のファイル追加時に .meta ファイルも忘れずに生成すること
2. **SW1-003 担当者へ**: `Rules/` ディレクトリは空で作成済み。SwedishOrthography.cs, SwedishSyllabifier.cs の追加時に .meta ファイルも忘れずに生成すること
3. **SW1-004 担当者へ**: `Conversion/` ディレクトリは空で作成済み。IpaConverter.cs の追加時に .meta ファイルも忘れずに生成すること。SwedishG2PEngine.cs の NotImplementedException を実装に置き換える際、Dispose パターンは変更しないこと
4. **全後続チケット担当者へ**: SwedishG2POptions は Sw1 時点では Dialect, IncludeStress, Separator のみ。Sw2 で EnableExceptionDictionary, EnableTextNormalization が追加される予定。プロパティ追加時はイミュータブルパターン（コンストラクタ引数追加）を維持すること
5. **Dispose パターン**: `int _disposed` + `Interlocked.CompareExchange` + `Volatile.Read` で統一。既存パッケージのパターンを踏襲すること（MEMORY.md 参照）
