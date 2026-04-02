# SW4-004: CI/CD + ソリューション + ドキュメント更新

> **マイルストーン**: Sw4 — Multilingual統合 + 評価ツール + リリース準備
> **前提チケット**: SW4-001, SW4-003（ソリューション追加対象プロジェクトが存在する必要がある）
> **後続チケット**: SW4-005

## 1. タスク目的とゴール

DotNetG2P.Swedish および DotNetG2P.SwedishEval をソリューションに追加し、CI/CD パイプラインのビルド・テスト・パッケージ対象に含める。sync-shared-internals.ps1 のコピー先にスウェーデン語を追加する。CLAUDE.md と CHANGELOG.md を更新してプロジェクト全体のドキュメント整合性を確保する。

**完了の定義:**
- `DotNetG2P.slnx` に Swedish / SwedishEval プロジェクトが追加される
- `dotnet build DotNetG2P.slnx` がスウェーデン語を含めて成功する
- `dotnet test DotNetG2P.slnx` がスウェーデン語テストを含めて成功する
- CI（ci.yml）でスウェーデン語のビルド・テスト・パッケージ生成が自動実行される
- `sync-shared-internals.ps1 -Check` がスウェーデン語の Internal ファイルを検証する
- CLAUDE.md の進捗テーブルにスウェーデン語行が追加される
- CHANGELOG.md にスウェーデン語 G2P 追加エントリが記載される

## 2. 実装内容の詳細

### 2.1 DotNetG2P.slnx 更新

```xml
<!-- /src/ フォルダ -->
<Project Path="src\DotNetG2P.Swedish\DotNetG2P.Swedish.csproj" />

<!-- /tools/ フォルダ -->
<Project Path="tools\DotNetG2P.SwedishEval\DotNetG2P.SwedishEval.csproj" />
```

**注意**: .slnx 形式（.NET 10 対応）であるため、XML 要素の追加位置は既存プロジェクト（Portuguese, PortugueseEval）の直後に揃える。

### 2.2 ci.yml 更新

`.github/workflows/ci.yml` は**個別プロジェクト列挙方式**でRestore/Build/Packの各ステップにプロジェクトパスを明示的に列挙している。以下の全ステップに `DotNetG2P.Swedish.csproj` と `DotNetG2P.SwedishEval.csproj` を追加する。

#### Restore ステップ（PowerShell $projects 配列）

```yaml
# 既存の $projects 配列に追加:
'tools/DotNetG2P.SwedishEval/DotNetG2P.SwedishEval.csproj'
```

**注意**: `DotNetG2P.Swedish.csproj` 自体は `DotNetG2P.Tests.csproj` や `DotNetG2P.SwedishEval.csproj` の ProjectReference 経由で間接的に restore されるが、明示追加も可。

#### Build ステップ（PowerShell $projects 配列）

```yaml
# 既存の $projects 配列に追加（Restore と同一リスト構造）:
'tools/DotNetG2P.SwedishEval/DotNetG2P.SwedishEval.csproj'
```

#### テスト対象

```yaml
# テストは dotnet test tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj で実行されるため、
# テストプロジェクトが DotNetG2P.Swedish を参照していれば自動的にスウェーデン語テストが実行される
```

#### Pack ステップ（個別 dotnet pack 行の列挙）

ci.yml の Pack ステップは各パッケージの `dotnet pack` を個別行で列挙している。以下の行を追加する:

```yaml
dotnet pack src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj --configuration Release --no-build -p:EnablePackageValidation=true --output ./artifacts
```

**挿入位置**: 既存の `DotNetG2P.Portuguese.csproj` の行の直後、`DotNetG2P.Multilingual.csproj` の行の直前。

### 2.3 sync-shared-internals.ps1 更新

`tools/sync-shared-internals.ps1` に DotNetG2P.Swedish/Internal/ をコピー先リストに追加する。

```powershell
# 既存のコピー先リストに追加:
$targets = @(
    # ... 既存の言語パッケージ ...
    "src/DotNetG2P.Swedish/Internal"
)
```

**同期対象ファイル:**
- `BatchConversionHelper.cs` — バッチ変換ヘルパー
- `PreserveAttribute.cs` — Unity IL2CPP strip 防止

**検証**: `sync-shared-internals.ps1 -Check` モードでスウェーデン語の Internal ファイルが最新であることを検証する。

### 2.4 CLAUDE.md 更新

#### 進捗状況テーブルにスウェーデン語行を追加

```markdown
| スウェーデン語 | DotNetG2P.Swedish | Sw1-Sw4完了 | 400+ | ルールベース+例外辞書500+語、Central/FinlandSwedish方言 |
```

#### プロジェクト構成セクション更新

```markdown
│   ├── DotNetG2P.Swedish/               # スウェーデン語G2P（独立、Core参照なし）
```

#### 技術スタック・パッケージングセクション更新

```markdown
- **パッケージング**: NuGet (`DotNetG2P`, ..., `DotNetG2P.Swedish`, `DotNetG2P.Multilingual`) + UPM (`com.dotnetg2p.core`, ..., `com.dotnetg2p.swedish`, `com.dotnetg2p.multilingual`)
```

#### 概要文のスウェーデン語追加

CLAUDE.md の「プロジェクト概要」セクションの「日英中韓西仏葡多言語G2P」を「日英中韓西仏葡瑞多言語G2P」に更新する。同様に該当する箇所の言語リストにスウェーデン語を追加する。

#### 多言語統合テーブルの Multilingual 行を更新

```markdown
| 多言語 | DotNetG2P.Multilingual | 完了 | 450+ | 8言語ファサード、Lazy初期化、言語自動判定+セグメント分割 |
```

### 2.5 CHANGELOG.md 更新

```markdown
## [1.9.0] - 2026-xx-xx

### Added
- スウェーデン語G2P（DotNetG2P.Swedish）: ルールベース+例外辞書500+語、Central/FinlandSwedish方言対応
  - 5フェーズG2P規則（トリグラフ/ダイグラフ → 子音軟化 → 母音変換 → そり舌化 → 黙字処理）
  - ピッチアクセント予測（accent 1/2）
  - テキスト正規化11段階（略語、序数、日付、時刻、通貨、数字等）
  - IPA / X-SAMPA / PUA / Prosody 出力
  - 例外辞書500+語（外来語、機能語、sj音例外、地名等）
- Multilingual 8言語対応（Language.Swedish = 7）
  - TextSegmenter スウェーデン語言語判定（å検出、信号語、接尾辞信号）
  - SwedishEval 評価ツール

### Changed
- Multilingual: 7言語 → 8言語対応
```

### 2.6 release.yml 更新

`release.yml` の Pack ステップも ci.yml と同様に**個別プロジェクトを列挙する方式**である。以下の行を Pack ステップに明示的に追加する:

```yaml
dotnet pack src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj --configuration Release --no-build -p:PackageVersion="$VERSION" --output ./artifacts
```

**挿入位置**: 既存の `DotNetG2P.Portuguese.csproj` の行の直後、`DotNetG2P.Multilingual.csproj` の行の直前。

## 3. エージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| 実装担当 | 1名 | slnx, ci.yml, sync-shared-internals.ps1, CLAUDE.md, CHANGELOG.md の更新 |
| 検証担当 | 1名 | CI パイプライン動作確認、sync-shared-internals -Check 実行 |

**合計: 2名**

## 4. 提供範囲とテスト項目

### 提供範囲（スコープ）

- `DotNetG2P.slnx` への Swedish / SwedishEval 追加
- `.github/workflows/ci.yml` のビルド・テスト・パッケージ対象更新
- `tools/sync-shared-internals.ps1` のコピー先リスト更新
- `CLAUDE.md` の進捗テーブル・プロジェクト構成・技術スタック更新
- `CHANGELOG.md` のリリースエントリ追加

**スコープ外:**
- DotNetG2P.Swedish パッケージ本体（Sw1-Sw3）
- Multilingual 統合コード（SW4-001）
- TextSegmenter 言語判定（SW4-002）
- 評価ツール本体（SW4-003）
- テスト実行（SW4-005）

### ユニットテスト

本チケットでは新規テストは作成しない。以下の既存テスト/検証コマンドによる確認を行う:

| 確認項目 | コマンド |
|---------|---------|
| ソリューションビルド | `dotnet build DotNetG2P.slnx` |
| テスト実行 | `dotnet test DotNetG2P.slnx` |
| sync-shared-internals チェック | `pwsh tools/sync-shared-internals.ps1 -Check` |
| パッケージ生成 | `dotnet pack src/DotNetG2P.Swedish/DotNetG2P.Swedish.csproj -c Release` |

### E2Eテスト

| テスト | 検証内容 |
|--------|---------|
| CI パイプライン | GitHub Actions の ci.yml がスウェーデン語を含めて全ステップ pass |
| Release パイプライン | release.yml でスウェーデン語 NuGet パッケージが生成される |

## 5. 懸念事項とレビュー項目

### 懸念事項

1. **slnx フォーマット互換性**: `.slnx` は .NET 10 対応の新しいソリューション形式。XML 要素の追加位置やフォーマットが既存エントリと一致しているか確認。手動編集による XML 破損に注意
2. **ci.yml / release.yml の個別プロジェクト列挙**: 両ファイルとも個別プロジェクト列挙方式であるため、Restore/Build/Pack の各ステップに `DotNetG2P.Swedish.csproj` と `DotNetG2P.SwedishEval.csproj` を漏れなく追加すること
3. **sync-shared-internals のパスセパレータ**: Windows (\\) と Linux (/) の両方で動作するパス指定が必要。既存のエントリのパターンに従う
4. **CHANGELOG.md のバージョン番号**: 現在 v1.8.2 であるため、スウェーデン語追加は v1.9.0 が妥当。ただし最終的なバージョンはリリース時に決定
5. **CLAUDE.md のテスト数合計**: Multilingual テスト数が既存の 412 から増加する（SW4-005 で +35 テスト）。最終値は SW4-005 完了後に確定する

### レビューチェックリスト

- [ ] `DotNetG2P.slnx` に Swedish と SwedishEval が正しいパスで追加されている
- [ ] `dotnet build DotNetG2P.slnx` が成功する
- [ ] `dotnet test DotNetG2P.slnx` が成功する（スウェーデン語テスト含む）
- [ ] ci.yml の Restore ステップに DotNetG2P.SwedishEval.csproj が追加されている
- [ ] ci.yml の Build ステップに DotNetG2P.SwedishEval.csproj が追加されている
- [ ] ci.yml の Pack ステップに DotNetG2P.Swedish.csproj が追加されている
- [ ] release.yml の Pack ステップに DotNetG2P.Swedish.csproj が追加されている
- [ ] sync-shared-internals.ps1 のコピー先に DotNetG2P.Swedish/Internal が含まれている
- [ ] `sync-shared-internals.ps1 -Check` が pass する
- [ ] CLAUDE.md の進捗テーブルにスウェーデン語行が追加されている
- [ ] CLAUDE.md のプロジェクト構成に Swedish が追加されている
- [ ] CLAUDE.md のパッケージ一覧に Swedish NuGet/UPM が追加されている
- [ ] CLAUDE.md の Multilingual 行がテスト数・言語数とも更新されている
- [ ] CHANGELOG.md に v1.9.0 のスウェーデン語追加エントリがある
- [ ] CLAUDE.md の概要文「日英中韓西仏葡多言語G2P」にスウェーデン語が追加されている

## 6. ゼロから作り直すとしたら

ポルトガル語追加時の PR（PR#50 付近）の diff を参照し、以下のファイルについて Portuguese → Swedish の diff を適用する:

1. `DotNetG2P.slnx`: PortugueseEval 追加行の直後に SwedishEval を追加
2. `ci.yml`: Portuguese の Restore/Build/Pack 各ステップの行をコピーして Swedish に変更（個別プロジェクト列挙方式のため）
2a. `release.yml`: Portuguese の Pack ステップの行をコピーして Swedish に変更
3. `sync-shared-internals.ps1`: Portuguese の Internal エントリをコピーして Swedish に変更
4. `CLAUDE.md`: ポルトガル語行の下にスウェーデン語行を追加
5. `CHANGELOG.md`: 新バージョンセクションを追加

各ファイルの変更は最小限（1-5行程度）であるため、手動編集で十分。自動化ツールは不要。

## 7. 後続タスクへの連絡事項

- **SW4-005 へ**: slnx へのプロジェクト追加が完了した時点で、`dotnet test DotNetG2P.slnx` でスウェーデン語テストが CI で自動実行される。テストファイルの追加は slnx 更新後に行うこと
- **リリース時の注意**: CHANGELOG.md のバージョン番号（v1.9.0）と日付はリリース直前に確定する。リリース PR で最終更新する
- **CLAUDE.md のテスト数**: Multilingual テスト数は SW4-005 完了後に最終値を反映する。本チケットでは暫定値を記入し、SW4-005 完了時に更新する
- **Directory.Build.props**: NuGet パッケージメタデータ（Author, Description 等）は `Directory.Build.props` で共通定義されているため、DotNetG2P.Swedish 固有のメタデータが必要な場合は csproj 内で上書きする
