---
ticket: T06
title: Issue#56 フォローアップ・リリース準備
milestone: Mi3
status: 完了
depends_on: [T05]
blocks: []
---

# T06: Issue#56 フォローアップ・リリース準備

## 1. タスク目的とゴール

Mi3 マイルストーン（Misaki 互換中国語出力）の最終チケット。T01〜T05 で Misaki 互換
API の実装・テスト・ドキュメントが完了済みの状態から、Issue #56 質問者への回答を確定し、
新機能を NuGet/UPM の両チャネルに同時リリースする。

### ゴール

- Issue #56 に実装完了の報告コメントを投稿し、使用例と Kokoro 連携での動作確認手順を提示する
- Misaki 互換機能を含む新バージョンを `DotNetG2P.Chinese`/`DotNetG2P.Multilingual` を中心に
  リリースし、NuGet と Unity Package Manager の両方で取得可能な状態にする
- ホームの README/CLAUDE.md を新バージョンに揃え、CI/リリースワークフローが緑であることを確認する
- Issue #56 を「完了」として正式にクローズする（質問者の動作確認後）

### 非ゴール

- 新しい機能追加（本チケットの範囲は「確定済み実装のリリース」のみ）
- 他言語（日本語/韓国語等）の Kokoro/Misaki 互換対応（将来課題）
- Kokoro 本体との統合テストやサンプルプロジェクト新規作成（動作確認手順の提示のみ）

---

## 2. 実装する内容の詳細

### 2.1 バージョン番号の決定

現行バージョン: **v1.9.0**（`Directory.Build.props` の `<Version>` 及び各 `package.json`）

**推奨: v1.10.0（マイナーバージョン）**

根拠:
- セマンティックバージョニング (SemVer 2.0.0) に従うと、
  - **MAJOR**: 後方互換性のない API 変更
  - **MINOR**: 後方互換を維持した機能追加
  - **PATCH**: 後方互換を維持したバグ修正
- Mi3 は「Misaki 互換の新しい出力 API（`ToMisaki()` 等）の追加」であり、
  既存の `ToPinyin()` / `ToIPA()` 等の挙動を変更しない → **機能追加**
- よって **MINOR** バージョン（1.9.0 → 1.10.0）が適切
- パッチ (1.9.1) は「バグ修正のみ」の意味になるため不適切
- メジャー (2.0.0) は破壊的変更がないため過剰

破壊的変更の有無チェック項目:
- `ChineseG2PEngine` の既存公開メソッドのシグネチャ変更なし
- `ChineseG2POptions` の既存プロパティ削除/型変更なし
- enum (`PinyinStyle`, `ChineseIpaPhoneme` 等) の既存値の序数変更なし
- `DotNetG2P.Multilingual` の `Language` enum の既存値の序数変更なし
- 既存公開クラスの `sealed`/`abstract`/`static` 修飾子追加なし
- → すべて満たせば v1.10.0 で確定

### 2.2 バージョン更新対象ファイル

以下のファイルをすべて `1.9.0` → `1.10.0` に更新する:

| ファイル | 箇所 |
|---------|------|
| `Directory.Build.props` | `<Version Condition="...">1.10.0</Version>` を 2 行 |
| `src/DotNetG2P.Core/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.MeCab/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Chinese/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.English/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Korean/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Spanish/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.French/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Portuguese/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Swedish/package.json` | `"version": "1.10.0"` |
| `src/DotNetG2P.Multilingual/package.json` | `"version": "1.10.0"` |
| `CLAUDE.md` | 「現在 v1.10.0」に更新、進捗表に Mi3 を追加 |
| `README.md` | バージョンバッジ、使用例のインストールコマンド |

全パッケージ同時バージョンアップとする理由: モノレポ方針により、個別昇格は運用コストが高く、
`Directory.Build.props` の共通 `<Version>` で一括管理している現状を維持する。

### 2.3 Issue #56 へのコメント文面案

````markdown
@<質問者> お待たせいたしました。Misaki 互換中国語出力の対応が完了しましたのでご報告します。

## 実装内容

v1.10.0 で `DotNetG2P.Chinese` に Misaki 互換出力 API を追加しました。
Kokoro の Misaki フロントエンドと同じ音素列/トークナイズ形式でピンインを取得できます。

- `ChineseG2PEngine.ToMisaki(string text)` — Misaki 互換音素列を文字列で返す
- `ChineseG2PEngine.ToMisakiTokens(string text)` — トークンごとの情報（表層形・声調番号・音素列）を配列で返す
- `ChineseG2POptions.MisakiCompatibility` — Misaki の挙動に合わせた補正フラグ

`DotNetG2P.Multilingual` 側からも `MultilingualG2PEngine.ToMisaki(text, Language.Chinese)` で
呼び出せます（言語自動判定にも対応）。

## 使用例（NuGet）

```csharp
using DotNetG2P.Chinese;

using var engine = new ChineseG2PEngine();

// Misaki 互換音素列
string misaki = engine.ToMisaki("你好世界");
// => "ni3 hao3 shi4 jie4"（例）

// トークン単位情報
var tokens = engine.ToMisakiTokens("你好世界");
foreach (var t in tokens)
{
    Console.WriteLine($"{t.Surface} / {t.Pinyin} / tone={t.Tone}");
}
```

インストール:

```bash
dotnet add package DotNetG2P.Chinese --version 1.10.0
# または多言語ファサード
dotnet add package DotNetG2P.Multilingual --version 1.10.0
```

Unity (UPM) の場合は `Packages/manifest.json` に以下を追加:

```json
{
  "dependencies": {
    "com.dotnetg2p.chinese": "https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Chinese#v1.10.0"
  }
}
```

## Kokoro 連携での動作確認手順

1. 本ライブラリ v1.10.0 をインストール
2. `ToMisaki()` で得た音素列を Kokoro の入力へそのまま渡す
3. Misaki 本体（Python）と同一入力で出力差分を比較（T04 で提供しているゴールデンデータセットを利用可能）

差分が発生するケースや想定外の挙動がありましたら、本 Issue に再コメントをお願いします。
動作確認が取れましたら Issue をクローズいたします。改めてご報告ありがとうございました。
````

注: `ToMisaki` / `ToMisakiTokens` / `MisakiCompatibility` の正確な名前・シグネチャは T01〜T05 の
実装に合わせて確定する（上記は想定例）。

### 2.4 リリースノート案（GitHub Release）

```markdown
## v1.10.0 — Misaki 互換中国語出力

### 新機能
- **DotNetG2P.Chinese**: Misaki（Kokoro フロントエンド）互換の音素列出力 API を追加
  - `ChineseG2PEngine.ToMisaki(text)` — Misaki 互換音素列を文字列で返す
  - `ChineseG2PEngine.ToMisakiTokens(text)` — トークン単位の情報配列を返す
  - `ChineseG2POptions.MisakiCompatibility` — Misaki 準拠モードフラグ
- **DotNetG2P.Multilingual**: `MultilingualG2PEngine.ToMisaki()` ファサード追加
- Misaki 相当のトークナイズ/声調処理/句読点ポリシーを実装

### 改善
- Chinese ピンイン辞書ルックアップの微最適化（T02 の副産物）
- ドキュメント `docs/chinese-misaki.md` 追加（使用法・差分仕様）

### 後方互換性
- 破壊的変更なし（既存 API のシグネチャ・挙動は v1.9.x と同一）
- 既存の `ToPinyin()` / `ToIPA()` 等は従来どおり動作

### 関連 Issue
- Fixes #56: Misaki 互換中国語出力の要望

### NuGet パッケージ
全 10 パッケージを v1.10.0 として同時リリース:
`DotNetG2P`, `DotNetG2P.MeCab`, `DotNetG2P.Chinese`, `DotNetG2P.English`,
`DotNetG2P.Korean`, `DotNetG2P.Spanish`, `DotNetG2P.French`, `DotNetG2P.Portuguese`,
`DotNetG2P.Swedish`, `DotNetG2P.Multilingual`

### SBOM
CycloneDX SBOM はリリース成果物 (`bom.xml`) に同梱。
```

備考: `release.yml` の `gh release create ... --generate-notes` が自動生成するコミットログと
併用する前提。上の手書き部分はリリース概要セクションとして追記する（リリース作成後に手動編集、
もしくは `--notes` オプションで差し替え）。

### 2.5 PR 作成内容

**ブランチ名案**: `release/v1.10.0-misaki` または `chore/v1.10.0-release`

**PR タイトル（日本語）**:
「v1.10.0 リリース準備 — Misaki 互換中国語出力対応 (#56)」

**PR 本文**:
```markdown
## 概要

Mi3 マイルストーン（Issue #56）の最終作業として、v1.9.0 → v1.10.0 のリリース準備を行う。

## 変更内容

- `Directory.Build.props` のバージョンを 1.10.0 に更新
- 全 UPM パッケージの `package.json` を 1.10.0 に更新
- `CLAUDE.md` の進捗表に「中国語(Misaki互換) Mi1-Mi3 完了」を追記
- `README.md` のバージョン表記を更新
- （必要に応じて）`docs/chinese-misaki.md` のクロスリンク調整

## リリース後の作業（本 PR マージ後）

1. `workflow_dispatch` で `release.yml` を実行（input: `version=1.10.0`）
2. Issue #56 に報告コメント投稿（本チケット 2.3 節参照）
3. 質問者の動作確認完了後に Issue #56 をクローズ

## 依存

- T01〜T05（Misaki 互換機能の実装・テスト・ドキュメント）が main にマージ済みであること

## チェック項目

- [ ] CI（ci.yml）全マトリクスグリーン
- [ ] `DotNetG2P.Tests` 全テスト通過（中国語 936 件以上 + Misaki 関連追加分）
- [ ] `Multilingual` テスト通過
- [ ] Unity .meta 整合性チェック通過
- [ ] `sync-shared-internals.ps1 -Check` 通過
- [ ] Publish smoke (trim + AOT) 通過
- [ ] DocFX ビルド警告ゼロ
- [ ] 破壊的変更なし（T06 2.1 節チェックリスト参照）
```

### 2.6 リリース実行手順

1. 本 PR を main にマージ
2. GitHub Actions の `Release` ワークフローを `workflow_dispatch` で起動
   - input `version`: `1.10.0`（先頭に `v` を付けない）
3. `release.yml` が以下を実行:
   - `validate` ジョブ: バージョン形式検証、`v1.10.0` タグ未存在確認
   - `build-and-test` ジョブ: ビルド、DocFX、辞書セットアップ、テスト、
     全 10 パッケージの `dotnet pack`、CycloneDX SBOM 生成、アーティファクト
     アップロード (`nuget-packages`)
   - `publish` ジョブ: NuGet.org への `dotnet nuget push`（`--skip-duplicate`）、
     GitHub Release の作成（`gh release create v1.10.0 ... --generate-notes`）
4. NuGet.org で 10 パッケージがインデックス化されたことを確認
5. GitHub Release のノート本文を 2.4 節の文面で手動編集（ハイライト追加）
6. UPM 側は Git URL + tag `v1.10.0` で即座に参照可能になることを確認
7. Issue #56 に 2.3 節のコメントを投稿

---

## 3. 実装するために必要なエージェントチームの役割と人数

本チケットは実装よりも調整・確認作業が主体のため少人数で完結する。

| 役割 | 人数 | 主な責務 |
|------|-----|---------|
| リリースマネージャ | 1 | バージョン確定、`Directory.Build.props` と全 `package.json` の一括更新、PR 作成、リリースワークフロー起動、NuGet/GitHub Release の最終確認 |
| テクニカルライター兼サポート | 1 | Issue #56 への回答コメント作成・投稿、リリースノート本文編集、README/CLAUDE.md の文言調整、質問者フォロー対応 |
| QA / リリース検証 | 1 | CI グリーン確認、NuGet から実際に `dotnet add package` して Misaki API が呼べること、UPM Git URL 経由で Unity 2021.2 に取り込めること、破壊的変更チェックリスト消化 |

合計 **3 名**（総作業時間目安: 0.5〜1 人日）。兼務可能。

---

## 4. 提供範囲とテスト項目

### 4.1 提供範囲

- `Directory.Build.props` / 各 `package.json` のバージョン更新（11 ファイル）
- `CLAUDE.md` 進捗表更新
- `README.md` バージョン表記更新
- PR 作成・マージ
- `Release` ワークフロー実行による成果物発行:
  - NuGet 10 パッケージ (`*.nupkg` + `*.snupkg`)
  - GitHub Release `v1.10.0`（SBOM 同梱）
  - Git tag `v1.10.0`
- Issue #56 報告コメント投稿

### 4.2 テスト項目

#### CI 全テスト通過確認（ci.yml）

- `unity-meta-check` ジョブ
  - 全 UPM パッケージの `.meta` ファイル整合性
  - `tools/sync-shared-internals.ps1 -Check` 通過
- `build-test-and-validate` ジョブ（6 マトリクス）
  - ubuntu-latest × .NET 8
  - ubuntu-latest × .NET 9（coverage + validate_pack）
  - windows-latest × .NET 8 / 9
  - macos-latest × .NET 8 / 9
  - 各マトリクスで `dotnet test --filter "Category!=Performance"` グリーン
- `validate_pack` マトリクスで:
  - DocFX ビルド警告ゼロ (`--warningsAsErrors`)
  - Publish smoke trim (`DotNetG2PPublishTrimmedSmoke=true`) 成功
  - Publish smoke AOT (`DotNetG2PPublishAotSmoke=true`) 成功
  - 全 10 パッケージの `dotnet pack -p:EnablePackageValidation=true` 成功
  - CycloneDX SBOM 生成成功

#### テスト件数の目安

| パッケージ | 既存テスト数 | Mi3 追加見込み | 備考 |
|-----------|-----|-----|------|
| 日本語 | 950+ | 0 | 影響なし |
| 英語 | 511 | 0 | 影響なし |
| 中国語 | 936 | +N | Misaki 互換テスト追加分 |
| 韓国語 | 375 | 0 | 影響なし |
| スペイン語 | 355 | 0 | 影響なし |
| フランス語 | 719 | 0 | 影響なし |
| ポルトガル語 | 1310 | 0 | 影響なし |
| スウェーデン語 | 400+ | 0 | 影響なし |
| 多言語 | 450+ | +M | Multilingual ファサード経由の Misaki 呼び出しテスト |

中国語・多言語以外のテスト件数が減っていたら退行扱い → 破壊的変更の疑いで調査。

#### リリースワークフロー確認（release.yml）

- `validate` ジョブ: バージョン `1.10.0` が正規表現 `^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?$` を通過
- `v1.10.0` タグが未存在であることを確認
- `build-and-test` ジョブ: ビルド/テスト/pack/SBOM 成功
- `publish` ジョブ:
  - 10 個の `.nupkg` + 10 個の `.snupkg` が `artifacts` にアップロード済み
  - `dotnet nuget push --skip-duplicate` が全パッケージ成功
  - `gh release create v1.10.0` が成功し、リリース本文が自動生成される

#### リリース後スモークテスト

- クリーン環境（ローカル/Docker）で:
  ```bash
  dotnet new console -o TestMisaki
  cd TestMisaki
  dotnet add package DotNetG2P.Chinese --version 1.10.0
  # Program.cs に ChineseG2PEngine.ToMisaki("你好") を追加して実行
  dotnet run
  ```
- Unity 2021.2 以降の空プロジェクトで:
  - `Packages/manifest.json` に Git URL + `#v1.10.0` を追加
  - コンパイルエラーなく `ChineseG2PEngine` が解決される
  - IL2CPP ビルドで Misaki API がストリップされない（`[Preserve]` 効果確認）

---

## 5. 実装に関する懸念事項とレビュー項目

### 5.1 セマンティックバージョニング

- **新 API 追加はマイナーバージョン**が原則 (SemVer §7)
- `ToMisaki()` 等の追加は純粋な機能追加 → v1.9.0 → **v1.10.0**
- もし T01〜T05 で以下のような変更が入っていたら再検討が必要:
  - 既存公開メソッドの戻り値型変更 → **v2.0.0**
  - 既存 enum 値の削除・序数シフト → **v2.0.0**
  - `ChineseG2POptions` の必須プロパティ追加 → **v2.0.0**
- **レビュー項目**: PR 作成前に `git diff v1.9.0..HEAD -- src/DotNetG2P.Chinese/*.cs src/DotNetG2P.Multilingual/*.cs` で公開 API を目視確認

### 5.2 NuGet/UPM の同時リリース

- NuGet: `release.yml` の `publish` ジョブで `dotnet nuget push` により自動公開
- UPM: Git tag `v1.10.0` を打った時点で即時公開（`package.json` のバージョンは Git tag とは独立だが、UPM は package.json の値を信頼する）
- **同時性の保証**:
  - `release.yml` は tag 作成 → NuGet push の順（`gh release create "v$VERSION"` で tag 作成）
  - NuGet インデックス化は 10〜30 分遅延することがあるため、Issue コメント投稿は NuGet.org で実際にパッケージが検索可能になったのを確認してから行う
- **懸念**: `package.json` のバージョンだけ更新し忘れると、UPM 側では旧バージョンとして扱われる
  - → 2.2 節のチェックリストで 11 ファイル全更新を担保
  - → CI の `unity-meta-check` ジョブでは package.json バージョンの整合性は検査していない
    ため、PR レビュー時に grep で全 `"version"` 値を目視確認
- **推奨自動化（将来課題）**: `tools/bump-version.ps1`（仮）のようなスクリプトで 11 ファイル一括更新

### 5.3 破壊的変更の有無確認

- 2.1 節のチェックリストを PR レビューの必須項目とする
- 具体的な確認コマンド:
  ```bash
  git diff v1.9.0..HEAD -- 'src/DotNetG2P.Chinese/**/*.cs' | grep -E '^-\s+public '
  git diff v1.9.0..HEAD -- 'src/DotNetG2P.Multilingual/**/*.cs' | grep -E '^-\s+public '
  ```
  削除された `public` 行がなければ API 削除なしと判断
- `dotnet pack -p:EnablePackageValidation=true` により、前バージョンとの
  API 差分で破壊的変更があれば自動検出される（CI で既に実行中）→ これがグリーンなら安全

### 5.4 辞書ファイルサイズの変化

- `pinyin_char.txt` / `pinyin_phrase.txt` が Mi2 で更新されている場合、
  NuGet パッケージサイズが増加する
- パッケージサイズ上限（NuGet.org: 250 MB）に近づいていないか確認
- Unity の embedded resource 読み込み時間にも影響するため、
  Mi2 で辞書差分を最小化しているかレビュー

### 5.5 `docs/chinese-misaki.md` / DocFX

- DocFX ビルドは `--warningsAsErrors` で走るため、T05 で追加されたドキュメントに
  デッドリンクや XML コメント不備があると CI 落ちする
- PR 前にローカルで `dotnet tool run docfx docs/docfx.json --warningsAsErrors` を実行

### 5.6 リリース後の Rollback 戦略

- NuGet.org は一度公開したパッケージを削除できない（unlist のみ可能）
- 重大な不具合が発覚した場合:
  1. 該当パッケージを NuGet.org UI で unlist
  2. 緊急パッチ v1.10.1 を即時リリース
  3. GitHub Release をドラフトに戻す（タグは残す）
- → リリース前のスモークテスト（§4.2 最終節）を必須化することで事前検知

### 5.7 CI 環境変数とシークレット

- `release.yml` が依存するシークレット:
  - `NUGET_API_KEY`（NuGet.org 発行のキー、90 日有効期限）
  - `GITHUB_TOKEN`（自動発行）
- リリース前に Organization/Repo の Settings > Secrets で `NUGET_API_KEY` の有効期限を確認

---

## 6. 一から作り直すとしたら

Mi1〜Mi3 全体、さらにプロジェクト全体のリリース管理方式を振り返る。

### 6.1 Mi1〜Mi3 全体の振り返り（Misaki 互換対応）

**良かった点:**
- チケット駆動（T01〜T06 の 6 分割）により並行作業がしやすかった
- 既存の Chinese G2P パイプラインの拡張として実装でき、破壊的変更を避けられた

**改善したい点:**
- Misaki の仕様書（Python 実装）との差分検証を T04 まで後ろ倒ししたため、T01 実装後に
  再設計が発生した可能性 → **スパイク（T0）を先に置くべき**
- Kokoro との連携テストを実機で回さなかったため、Issue 質問者の環境依存問題を
  本チケット 2.3 節の「動作確認手順」として提示するだけに留まっている → **Kokoro サンプル
  プロジェクトをリポジトリ内に同梱する選択肢もあった**
- 中国語単体チケットだったが、他言語でも Misaki 互換の需要はあるはず → **初期設計で
  `IMisakiCompatible` インタフェースを導入**しておけば、英語/日本語 Kokoro 対応の土台になった

### 6.2 プロジェクト全体のリリース管理方式の再考

現状の課題:
- 全 10 パッケージを常に同一バージョンでリリース（モノレポ一括昇格）→ 変更がない
  パッケージも毎回新バージョンが発行され、ユーザが「何が変わったのか」を追いにくい
- `Directory.Build.props` と各 `package.json` のバージョンが二重管理（11 箇所手動更新）
- NuGet リリースと Issue クローズの間に人手作業が多い（リリースノート編集、コメント投稿）

**代替案 A: Feature flag 方式**

- `ChineseG2POptions.MisakiCompatibility = false` をデフォルトとして v1.10.0 に同梱し、
  デフォルト挙動は v1.9.x と完全互換にする
- ユーザが明示的に opt-in した時のみ Misaki 互換動作
- メリット: 万一のバグでも既存ユーザに影響なし、安全にメインにマージできる
- デメリット: 1 年後にデフォルト切替のための v2.0 が必要
- 本プロジェクトの T01〜T05 実装がすでに「新 API 追加」方式（既存 API 不変）であれば、
  flag 方式と実質同等の互換性は得られている

**代替案 B: プレビュー版リリース（`-preview.N`）**

- `v1.10.0-preview.1` を先に NuGet に公開（`release.yml` のバージョン検証正規表現は
  既に `-[a-zA-Z0-9.]+` サフィックスに対応済み）
- Issue #56 質問者に先行提供してフィードバックを得る
- 問題なければ `v1.10.0` を正式リリース
- 本プロジェクトのように外部ユーザからの要望ベースで機能追加する場合、
  **プレビュー版 → 安定版の 2 段階リリース**が最も安全
- T06 を以下のように再構成する案:
  - T06a: v1.10.0-preview.1 リリース + Issue #56 に preview 案内
  - T06b: 質問者フィードバック反映 + v1.10.0 正式リリース

**代替案 C: Misaki 仕様追従の自動化**

- Misaki (https://github.com/hexgrad/kokoro 配下の misaki/ja/zh) は upstream が更新される
- GitHub Actions の `schedule` で週次実行: Misaki の特定ファイル（`zh.py` 等）の hash を監視
- 変更検知で issue 自動作成 + 回帰テストの golden データ再生成 PR 自動作成
- **本プロジェクトが長期メンテされるなら必須レベルの自動化**
- 手動追従では T01〜T06 相当の工数が毎回発生する

**代替案 D: 個別パッケージ独立バージョニング**

- `Directory.Build.props` の `<Version>` を廃止し、各 `.csproj` に `<Version>` を個別定義
- 変更のあったパッケージだけバージョンアップ（changesets 風）
- 例: Misaki 対応は `DotNetG2P.Chinese` と `DotNetG2P.Multilingual` のみ v1.10.0、
  他は v1.9.0 のまま
- メリット: リリースノートが明瞭、ユーザ側の更新追従コスト減
- デメリット: 依存関係マトリクスが複雑化、`dotnet pack` スクリプトとリリースノート生成の
  再設計が必要
- **10 パッケージ規模になった現時点で採用を検討する価値がある**

**代替案 E: Release Please / changesets 等のリリースボット導入**

- Conventional Commits を採用し、Release Please Action でリリース PR を自動生成
- コミットメッセージから SemVer 判定、CHANGELOG 生成、バージョン更新 PR 作成まで自動化
- 本プロジェクトのコミットメッセージは既に日本語 Conventional Commits 風
  (`feat:`, `fix:`, `chore:` 等) なので導入ハードル低
- T06 のバージョン決定・PR 作成作業を完全自動化できる

### 6.3 推奨する次世代リリースフロー

1. Misaki 上流監視を GitHub Actions の `schedule` で自動化（代替案 C）
2. Release Please 導入で PR 作成を自動化（代替案 E）
3. 破壊的変更のない新機能は `-preview.N` を経由（代替案 B）
4. 個別パッケージ独立バージョニングへ段階移行（代替案 D、長期計画）

### 6.4 リリース戦略の追加レビュー

§6.1〜§6.3 の議論はいずれも妥当だが、抽象度が高く「次にどのファイルを触れば動くか」まで
落ちていない。v1.10.0 でリリース管理の近代化に着手する前提で、6 つの観点について
具体的な実装レベルのレビューと推奨事項を記録する。

#### 6.4.1 現在の記載内容の評価

| 観点 | 既存記載 | 評価 | 残課題 |
|------|---------|------|-------|
| バージョン自動化 | §6.2 代替案 E で Release Please 言及のみ | 方向性は妥当だが設定例がゼロ | `release-please-config.json` / `.release-please-manifest.json` の具体例が必要 |
| NuGet/UPM 同期 | §5.2 で懸念指摘 + 手動 grep 推奨 | 検出のみで予防策なし | PR 時に CI で自動 fail させる仕組みが必要 |
| プレビュー版 | §6.2 代替案 B で記述 | workflow_dispatch で手動起動前提に留まる | Nightly 自動発行フィードと feed 分離の設計が欠落 |
| 破壊的変更検出 | §5.3 で `EnablePackageValidation=true` のみ | baseline 未設定のため実際は検査が緩い | PublicApiAnalyzer + `PublicAPI.Shipped.txt` 導入が必要 |
| 独立バージョニング | §6.2 代替案 D で概念のみ | 移行コスト試算なし | 「どのファイルを何行触るか」が未提示 |
| Misaki 上流追従 | §6.2 代替案 C で概念のみ | hash 監視の対象ファイル未指定 | 実装可能な YAML サンプルと判定ロジックが必要 |

結論: 方針は正しいが「Infra-1〜Infra-5 チケット（§7.4）が着手されても手が止まる」粒度。
本節でその隙間を埋める。

#### 6.4.2 release-please による自動バージョンバンプ + CHANGELOG

**採用根拠**: 本プロジェクトのコミットメッセージは既に日本語 Conventional Commits 風
(`feat:`, `fix:`, `docs:`, `chore:`)。release-please は日本語本文でも type プレフィクスさえ
あれば SemVer 判定が動くため、導入ハードルはワークフロー 1 本 + 設定 2 ファイルのみ。

`.github/workflows/release-please.yml`（新規作成）:

```yaml
name: Release Please
on:
  push:
    branches: [main]

permissions:
  contents: write
  pull-requests: write

jobs:
  release-please:
    runs-on: ubuntu-latest
    steps:
      - uses: googleapis/release-please-action@v4
        id: release
        with:
          config-file: release-please-config.json
          manifest-file: .release-please-manifest.json
          token: ${{ secrets.GITHUB_TOKEN }}
      # リリース PR がマージされてタグが打たれたら、既存 release.yml を呼び出す
      - name: Trigger release.yml
        if: ${{ steps.release.outputs.release_created == 'true' }}
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: >
          gh workflow run release.yml
          --ref main
          -f version=${{ steps.release.outputs.major }}.${{ steps.release.outputs.minor }}.${{ steps.release.outputs.patch }}
```

`release-please-config.json`（ルート、manifest モード・全パッケージ同一バージョン運用時）:

```json
{
  "release-type": "simple",
  "bump-minor-pre-major": true,
  "bump-patch-for-minor-pre-major": false,
  "include-component-in-tag": false,
  "packages": {
    ".": {
      "package-name": "dot-net-g2p",
      "changelog-path": "CHANGELOG.md",
      "extra-files": [
        {
          "type": "xml",
          "path": "Directory.Build.props",
          "xpath": "//Project/PropertyGroup/Version"
        },
        "src/DotNetG2P.Core/package.json",
        "src/DotNetG2P.MeCab/package.json",
        "src/DotNetG2P.Chinese/package.json",
        "src/DotNetG2P.English/package.json",
        "src/DotNetG2P.Korean/package.json",
        "src/DotNetG2P.Spanish/package.json",
        "src/DotNetG2P.French/package.json",
        "src/DotNetG2P.Portuguese/package.json",
        "src/DotNetG2P.Swedish/package.json",
        "src/DotNetG2P.Multilingual/package.json"
      ]
    }
  },
  "changelog-sections": [
    {"type": "feat", "section": "新機能"},
    {"type": "fix", "section": "バグ修正"},
    {"type": "perf", "section": "パフォーマンス"},
    {"type": "refactor", "section": "リファクタリング"},
    {"type": "docs", "section": "ドキュメント"},
    {"type": "chore", "section": "その他", "hidden": true}
  ]
}
```

`.release-please-manifest.json`（初期状態）:

```json
{
  ".": "1.9.0"
}
```

注意点:
- `Directory.Build.props` は `<Version Condition="...">` が 2 行ある（CI 用 + local 用）。
  xpath 単体では 1 行目しかマッチしないため、`type: "generic"` + 正規表現版の差し替えや、
  tools/bump-version.ps1 を release-please の extra-files hook から呼び出す方式も候補。
  実装時に hook ベースに倒すのが安全。
- release-please は Conventional Commits の英語 keyword (`feat`, `fix`, `BREAKING CHANGE:`)
  を見るため、本プロジェクトの「日本語本文 + 英語 type」運用は維持する必要がある。
  PR マージ時のスカッシュコミット整形ルールを `CLAUDE.md` に明記しておく。

#### 6.4.3 PublicApiAnalyzer による破壊的変更の自動検出

現状の `EnablePackageValidation=true` はベースラインパッケージを指定しない限り前バージョン
との diff を取らない（CI ログで「No baseline package was specified」警告が出ているはず）。
より堅牢な検出には Roslyn の `Microsoft.CodeAnalysis.PublicApiAnalyzers` が向いている。

`Directory.Build.props` への追記例:

```xml
<ItemGroup Condition="'$(IsPackable)' == 'true'">
  <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="4.14.0" PrivateAssets="all" />
  <AdditionalFiles Include="PublicAPI.Shipped.txt" Condition="Exists('PublicAPI.Shipped.txt')" />
  <AdditionalFiles Include="PublicAPI.Unshipped.txt" Condition="Exists('PublicAPI.Unshipped.txt')" />
</ItemGroup>
<PropertyGroup>
  <!-- RS0016: 未宣言の public API があれば警告 → CI では Error に昇格 -->
  <WarningsAsErrors>$(WarningsAsErrors);RS0016;RS0017;RS0022;RS0036;RS0037;RS0041</WarningsAsErrors>
</PropertyGroup>
```

運用:
1. `PublicAPI.Shipped.txt` を各 `src/DotNetG2P.*/` ディレクトリに作成（v1.9.0 時点の公開
   メンバー一覧を `dotnet format analyzers --diagnostics RS0016` で初回生成）
2. 新 API を追加するとアナライザが RS0016 を出す → 修正 PR で `PublicAPI.Unshipped.txt` に
   追記することでビルドを通す
3. リリース PR では `Unshipped → Shipped` にマージ（release-please の `extra-files` で自動化可能）
4. 既存メンバーを削除するには `PublicAPI.Shipped.txt` から削除 → `BREAKING CHANGE` 扱い

これにより「気付かずに `public` を `internal` に変更した」「enum 値を削除した」等が
コンパイルエラーとして出る。NuGet Package Validation と併用することで diff の粒度が
「バイナリ互換」と「ソース互換」の両面から担保される。

Chinese/Multilingual 以外のパッケージに先行導入して様子を見る案（Es/Fr/Pt は API が
安定している）も現実的。

#### 6.4.4 Nightly build + preview feed（GitHub Packages）の設計

release.yml は `workflow_dispatch` のみで手動起動前提。これを残しつつ、別ワークフロー
`nightly.yml` を追加して GitHub Packages（NuGet.org ではない）へ日次公開する。

`.github/workflows/nightly.yml`（新規作成、例）:

```yaml
name: Nightly Preview
on:
  schedule:
    - cron: '17 19 * * *'  # JST 04:17 毎日
  workflow_dispatch:

permissions:
  contents: read
  packages: write  # GitHub Packages へ push するために必須

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  nightly:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
        with:
          fetch-depth: 0  # git describe でコミット数を取るため

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
          source-url: https://nuget.pkg.github.com/ayutaz/index.json
        env:
          NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      - name: Compute preview version
        id: ver
        run: |
          BASE=$(grep -oP '(?<=<Version Condition="'"'"'\$\(CI\)'"'"' == '"'"'true'"'"' Or '"'"'\$\(GITHUB_ACTIONS\)'"'"' == '"'"'true'"'"'">)[0-9.]+' Directory.Build.props | head -1)
          SHA=$(git rev-parse --short HEAD)
          STAMP=$(date -u +%Y%m%d%H%M)
          VERSION="${BASE}-nightly.${STAMP}+${SHA}"
          echo "version=${VERSION}" >> "$GITHUB_OUTPUT"

      - run: dotnet restore DotNetG2P.slnx
      - run: dotnet build DotNetG2P.slnx -c Release -p:Version="${{ steps.ver.outputs.version }}"
      - uses: ./.github/actions/setup-dictionary
      - run: dotnet test DotNetG2P.slnx -c Release --no-build --filter "Category!=Performance"

      - name: Pack
        run: |
          for proj in src/DotNetG2P.*/DotNetG2P.*.csproj; do
            dotnet pack "$proj" -c Release --no-build \
              -p:PackageVersion="${{ steps.ver.outputs.version }}" \
              --output ./artifacts
          done

      - name: Push to GitHub Packages
        run: |
          for pkg in ./artifacts/*.nupkg; do
            dotnet nuget push "$pkg" \
              --api-key ${{ secrets.GITHUB_TOKEN }} \
              --source https://nuget.pkg.github.com/ayutaz/index.json \
              --skip-duplicate
          done
```

ユーザ側（`nuget.config`）:

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="dot-net-g2p-nightly" value="https://nuget.pkg.github.com/ayutaz/index.json" />
  </packageSources>
</configuration>
```

設計上のポイント:
- **バージョン形式** `1.10.0-nightly.202604150417+abc1234` は SemVer 2.0 準拠。`+` 以降は
  build metadata でソート対象外 → NuGet.org 側の通常リリース `1.10.0` と衝突しない
- **GitHub Packages を選ぶ理由**: (1) Pat 不要で `GITHUB_TOKEN` が使える、(2) NuGet.org の
  一度 push すると削除不可ポリシーを回避、(3) 自動 retention を効かせやすい
- **Preview は release-please の prerelease ブランチと併用**: `next` ブランチを作り、
  release-please の `release-as` オプションで `1.10.0-preview.N` を発行する運用にすれば、
  Issue #56 質問者向けに安定 preview を提供できる（nightly より品質が上のチャネル）
- **3 層チャネル構成**:
  1. Nightly (GitHub Packages, 自動, コミットごと) — 開発者向け
  2. Preview (NuGet.org, `-preview.N`, リリース PR 手動マージ) — 早期採用者向け
  3. Stable (NuGet.org, `1.10.0`) — 一般ユーザ向け

#### 6.4.5 Misaki 上流監視ワークフロー

§6.2 代替案 C を具体化する。Misaki 本体は現在 `hexgrad/misaki` リポジトリに分離されて
いる（旧 `hexgrad/kokoro` の `misaki/` サブパッケージから独立）ので、監視対象もそこに絞る。

`.github/workflows/misaki-upstream-watch.yml`（新規作成、例）:

```yaml
name: Misaki Upstream Watch
on:
  schedule:
    - cron: '0 21 * * 1'  # 毎週月曜 JST 06:00
  workflow_dispatch:

permissions:
  contents: write
  issues: write
  pull-requests: write

jobs:
  watch:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6

      - name: Fetch upstream files
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          mkdir -p .misaki-snapshot
          # 監視対象: 中国語変換ロジック + 日英韓も将来のため取得
          for file in misaki/zh.py misaki/en.py misaki/ja.py misaki/ko.py misaki/__init__.py pyproject.toml; do
            gh api "repos/hexgrad/misaki/contents/${file}" \
              --jq '.content' 2>/dev/null \
              | base64 -d \
              > ".misaki-snapshot/${file//\//_}" || echo "skipped: ${file}"
          done

      - name: Compute hash manifest
        id: hash
        run: |
          (cd .misaki-snapshot && sha256sum * | sort) > misaki-hashes.new
          if [ -f tools/misaki-watch/misaki-hashes.txt ]; then
            if diff -u tools/misaki-watch/misaki-hashes.txt misaki-hashes.new > hash.diff; then
              echo "changed=false" >> "$GITHUB_OUTPUT"
            else
              echo "changed=true" >> "$GITHUB_OUTPUT"
              echo "diff<<EOF" >> "$GITHUB_OUTPUT"
              cat hash.diff >> "$GITHUB_OUTPUT"
              echo "EOF" >> "$GITHUB_OUTPUT"
            fi
          else
            echo "changed=true" >> "$GITHUB_OUTPUT"
            echo "diff=INITIAL BASELINE" >> "$GITHUB_OUTPUT"
          fi
          mkdir -p tools/misaki-watch
          cp misaki-hashes.new tools/misaki-watch/misaki-hashes.txt

      - name: Get upstream commit
        id: upstream
        if: steps.hash.outputs.changed == 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          SHA=$(gh api repos/hexgrad/misaki/commits/main --jq '.sha')
          echo "sha=${SHA}" >> "$GITHUB_OUTPUT"

      - name: Open tracking issue
        if: steps.hash.outputs.changed == 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh issue create \
            --title "Misaki upstream 変更検出: ${{ steps.upstream.outputs.sha }}" \
            --label "misaki-upstream,needs-triage" \
            --body "$(cat <<EOF
          hexgrad/misaki の監視対象ファイルにハッシュ差分を検出しました。

          **upstream commit**: https://github.com/hexgrad/misaki/commit/${{ steps.upstream.outputs.sha }}

          **差分**:
          \`\`\`diff
          ${{ steps.hash.outputs.diff }}
          \`\`\`

          ## 対応チェックリスト
          - [ ] zh.py の変更が G2P 出力に影響するか確認
          - [ ] T04 ゴールデンデータセット再生成要否を判定
          - [ ] tests/DotNetG2P.Tests/ChineseMisakiCompatibilityTests.cs の期待値更新
          - [ ] 必要なら \`feat(chinese): misaki upstream XXXX 追従\` PR 作成
          - [ ] 影響なしなら本 issue を close + \`tools/misaki-watch/misaki-hashes.txt\` を
                merge してベースライン更新
          EOF
          )"

      - name: Commit baseline update
        if: steps.hash.outputs.changed == 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          BRANCH="chore/misaki-watch-${{ steps.upstream.outputs.sha }}"
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git checkout -b "$BRANCH"
          git add tools/misaki-watch/misaki-hashes.txt
          git commit -m "chore: Misaki upstream ハッシュベースライン更新 (${{ steps.upstream.outputs.sha }})"
          git push -u origin "$BRANCH"
          gh pr create --fill --label misaki-upstream --base main
```

運用メモ:
- hexgrad は `kokoro` と `misaki` の両方を保有しており、Misaki パッケージ単体は
  `hexgrad/misaki` リポジトリ。hash 監視のみなら raw 取得で十分だが、将来的に
  tarball を丸ごと持って差分テストを自動実行する拡張も視野に入れる
- `tools/misaki-watch/misaki-hashes.txt` を baseline として git 管理することで、
  「どの upstream commit まで追従済みか」を再現可能にする（調査の履歴が残る）
- `needs-triage` ラベル + 自動 PR は「見落とし防止」「軽微な変更の即時マージ」両立が目的
- 週次 + workflow_dispatch 併用で、臨時確認も容易

#### 6.4.6 パッケージ独立バージョニング移行コスト試算

§6.2 代替案 D を定量化する。現状は `Directory.Build.props` 1 箇所で `<Version>` を
共有しているが、以下の移行パスがある。

**移行先: Nx/Changesets スタイル（PR 単位で変更パッケージを記録、独立バージョン発行）**

影響を受けるファイル・コード規模の概算:

| カテゴリ | 変更内容 | ファイル数 | 工数 |
|---------|---------|-----------|------|
| ビルド設定 | `Directory.Build.props` の `<Version>` 削除、各 `.csproj` に `<VersionPrefix>` 追加 | 11 | 0.5d |
| UPM package.json | 各 UPM `package.json` の `version` を `.csproj` と同期するスクリプト | 10 + 1 script | 1d |
| release.yml | `workflow_dispatch` input を `package: all/chinese/multilingual/...` 方式に拡張、マトリクス化 | 1 | 1.5d |
| changeset 管理 | `.changeset/*.md` ディレクトリ運用、Conventional Commits + 変更パッケージ宣言 | 新規 + CLAUDE.md 更新 | 1d |
| 依存関係整合性 | `DotNetG2P.Multilingual` は 8 パッケージに `ProjectReference` 済 → NuGet 公開時は `PackageReference` に切替える Multi-Targeting パターン（現在未対応） | 1 `.csproj` + pack script | 2d |
| リリースノート | パッケージ別 `CHANGELOG.md` 分割 | 10 | 0.5d |
| ドキュメント | README、CLAUDE.md、DocFX の「バージョン表」記述変更 | 3-5 | 0.5d |
| テスト | `MultilingualTests` で参照する Chinese/English 等のバージョン整合性テスト追加 | 1-2 | 0.5d |
| **合計** |  |  | **約 7.5 人日** |

**最大の障壁**: `DotNetG2P.Multilingual` が 8 言語パッケージに `ProjectReference` で依存
している構造。独立バージョニング時は NuGet パッケージとして「固定バージョンの
DotNetG2P.Chinese ≥1.10.0」を参照する形に変わるため、以下のいずれかが必要:

1. **固定下限参照**: `<PackageReference Include="DotNetG2P.Chinese" Version="1.10.*" />`
   - CI では `ProjectReference`、pack 時に自動変換する MSBuild ターゲット記述が必要
2. **ソリューションローカル参照のまま Multilingual を常に最新バンプ**:
   既存運用に近い妥協案。Multilingual だけ「依存先が変わったら必ず上がる」ルール
3. **Multilingual をメタパッケージ化**: コードゼロ、依存関係宣言だけのパッケージに分解
   すれば依存バージョン更新のみで release-please が patch bump を自動発行できる

推奨: 段階移行として **フェーズ 1「Shipped.txt + release-please 導入（単一バージョン維持）」
→ フェーズ 2「Multilingual をメタパッケージ化」→ フェーズ 3「独立バージョニング」**
の 3 段階で進める。フェーズ 1 だけで「何を変えたか」が CHANGELOG で可視化されるため、
多くのユーザ課題は解消される。フェーズ 3 は需要が顕在化してから着手で十分。

#### 6.4.7 Infra チケットの更新提案

§7.4 の Infra-1〜Infra-5 を以下に置き換え推奨:

| ID | 内容 | 工数見積 | 優先度 |
|---|------|---------|--------|
| Infra-1 | release-please 導入（§6.4.2、manifest + workflow + hook スクリプト） | 2d | 高 |
| Infra-2 | PublicApiAnalyzer + `PublicAPI.Shipped.txt` baseline 生成（§6.4.3） | 1.5d | 高 |
| Infra-3 | Misaki upstream watcher（§6.4.5、hexgrad/misaki 対応） | 1d | 中 |
| Infra-4 | Nightly / preview feed on GitHub Packages（§6.4.4） | 2d | 中 |
| Infra-5 | Multilingual メタパッケージ化検証（§6.4.6 フェーズ 2） | 3d | 低 |
| Infra-6 | Kokoro 連携サンプルプロジェクト（旧 Infra-4） | 2d | 低 |
| Infra-7 | 独立バージョニング完全移行（§6.4.6 フェーズ 3） | 5d | 保留 |

**v1.10.0 リリース直後の着手推奨順**: Infra-1 → Infra-2 → Infra-3 → Infra-4 → Infra-5。
Infra-7 は少なくとも 2 件以上の外部要望が来るまで保留で問題ない。

### プロジェクト全体振り返りの追加レビュー

本節は T06（リリース観点）の最終段階で、Mi1〜Mi3 を通じた全体計画を「もし白紙から引き直すなら」の視点で振り返るものである。リリースマネジメント / 配布チャネル観点からの総括であり、T05 §プロジェクト全体振り返りの追加レビュー（ドキュメント/QA 観点）と対になっている。Phase 1 (T01/T02)・Phase 2 (T03/T04) で既に浮上した改善案（`record` + `switch` 式、TSV データ駆動テスト、`ChineseOutputFormat` enum による戦略パターン、`IMisakiCapableProcessor` による Multilingual 統合、命名揺れ `ToPiperIPA` vs `ToPiperIpa` の整理、Unity IL2CPP `[Preserve]` 検証等）を統合し、リリース観点で次回に持ち越すべき教訓を明示する。

#### A. フェーズ分割の代替案（リリース観点）

現行は **Mi1（T01/T02）→ Mi2（T03/T04）→ Mi3（T05/T06）** の 3 フェーズ / 6 チケット構成だが、リリース観点から評価すると**「実装 → テスト → リリース」の古典的ウォーターフォール**であり、NuGet/UPM の継続デリバリー戦略との相性が悪い。具体的には:

- 全 10 パッケージを同時バージョンアップする現行運用（§6.2 / §6.4）と、フェーズ境界で「リリースしない」現行計画が矛盾しない。Mi1 完了時点で `1.10.0-preview.1` を出せば、Mi2/Mi3 を「本番 feedback を受けながら並行実装」できた。
- **代替案 A-1: プレビュー版リリースを挟む 2 段階フェーズ**
  - **Phase α（Mi1a）**: 最小実装 + `v1.10.0-preview.1` リリース → Issue #56 質問者へ先行提供（§6.2 代替案 B と整合）
  - **Phase β（Mi1b〜Mi3）**: フィードバック反映 + 本番リリース
  - リリース 2 段階にすることで、「質問者の動作確認」を Mi3 完了後ではなく Mi1 完了直後に前倒しでき、Mi3 での手戻りリスクが解消される。
- **代替案 A-2: Mi3 をリリース判断のみに切り詰め**
  - Mi1/Mi2 内でドキュメント・テスト・バージョン更新まで全て完了させ、Mi3 は「CI 緑確認＋`workflow_dispatch` 起動＋Issue コメント投稿」のみ。**半日で終わる軽量フェーズ**に再定義する。
  - T05 + T06 の統合に相当。
- **推奨**: **A-1（プレビュー版挟み）+ A-2（Mi3 軽量化）のハイブリッド**。Mi3 規模の拡張には 4 フェーズも 3 フェーズも過剰で、実質は「実装 → preview → 本番」の 3 段階で完結する。

#### B. チケット粒度の再設計（リリース観点）

T01〜T06 は 6 枚構成だが、**リリース作業専用チケット（T06）が単独で存在**する運用は Mi3 規模には過剰。他言語マイルストーン（S1〜S4, F1〜F4, P1〜P4, Sw1〜Sw4）では各フェーズが「実装＋テスト＋ドキュメント＋リリース」を統合して扱っており、リリース専用チケットは存在しない。T06 の 560 行のドキュメント（§6.4 の追加レビュー 420 行を含む）は、**リリース運用の汎用的な改善案を全て T06 に詰め込んだ結果**であり、本来は別チケット（Infra-1〜Infra-7）として切り出すべきだった。

- **T05+T06 統合（= ドキュメント＋リリース＋Issue 対応）**: §6.4.5 で既に「T05+T06 統合の是非」が触れられているが、Mi3 規模では明確に統合が望ましい。実作業工数は合算で 0.5〜1 人日。1 PR に集約することで:
  - PR レビューが 1 回で済む
  - バージョン更新（`Directory.Build.props` + 10 package.json）とドキュメント更新（README/CLAUDE.md）のアトミック性が保証される
  - `depends_on: [T05]` の直列依存が消える
- **リリース汎用改善案の外出し**: T06 §6.2〜§6.4 の代替案 A〜E（Feature Flag / Preview / Upstream 監視 / 独立バージョニング / Release Please）は**すべて独立チケット（Infra-1〜Infra-7）**として既に §7.4 / §6.4.7 に起票されている。T06 本体はこれらの要約のみに留め、詳細は Infra チケット側に委譲すれば 560 行 → 150 行程度に削減可能。
- **再設計後のチケット構成案（3 枚）**:
  1. **TA: マッピング＋変換クラス＋エンジン API 実装**（旧 T01+T02+T03）
  2. **TB: テスト＋ドキュメント**（旧 T04+T05）
  3. **TC: リリース＋Issue フォロー**（旧 T06 軽量版 = バージョン更新＋ワークフロー起動＋Issue コメント投稿の 3 手順のみ）
- リリース観点で特筆すべきは、**TC が実質的に `release.yml` の `workflow_dispatch` を叩くだけのチケット**となる点。Release Please（§6.4.6 Infra-1）導入後はさらに PR 作成自体が自動化され、TC は「自動生成された PR のマージ＋動作確認」のみで完了する。

#### C. 依存関係の並列化機会（リリース観点）

現行は `T01→T02→T03→T04→T05→T06` の完全直列で、リリースが Mi3 末尾に押し付けられている。リリース観点での並列化機会:

- **T06 バージョン更新は T05 と独立**: §2.2 の「11 ファイルのバージョン更新」は T05 のドキュメント更新と完全独立。同一 PR にまとめるメリット大。
- **T06 リリースノート案は T03 完了時点で書き始められる**: §2.4 のリリースノート本文（新機能・改善・後方互換性）は API シグネチャ確定（T03）後すぐ執筆可能。T05 のパフォーマンステスト結果だけ最後に挿入すれば良い。
- **T06 Issue コメント文面は T03 完了時点で書き始められる**: §2.3 の Issue #56 コメント文面は、使用例コードが書ける T03 完了時点でドラフト可能。実際の投稿だけ T06 で行う。
- **T06 破壊的変更チェックは T04 完了時点で実行可能**: §5.3 の `git diff v1.9.0..HEAD -- 'src/DotNetG2P.Chinese/**/*.cs' | grep -E '^-\s+public '` チェックは、T04 完了直後に CI で自動実行できる。T06 まで待つ理由がない。
- **preview 版の先行リリース**: §6.4 代替案 B の `v1.10.0-preview.1` は T03 完了直後に出せる。Mi2/Mi3 の作業と**並行して外部 feedback を集められる**。
- 理論上の最短日数は現行（直列）より 30〜40% 短縮可能。特に「Issue 質問者への動作確認提供」リードタイムを Mi3 完了 → Mi1 完了直後に前倒しできる意義は大きい。

#### D. Phase 1/2 レビューで共通して浮上した課題の統合（リリース観点）

Phase 1/Phase 2 の横断論点とリリース時の対応関係を整理:

| 横断論点 | T01〜T04 での提示 | T05/T06 での統合先 | リリース時の対応 |
|---------|-----------------|-----------------|---------|
| `record + switch` 式への移行 | T01/T02 で**最有力**判定 | §6.4 で触れず | v1.11.0（中期）で別 Minor リリース |
| 戦略パターン / `ChineseOutputFormat` enum | T03 §B で Phase 1〜4 の段階計画 | §5.1 破壊的変更チェックリストに反映 | Phase 1 (v1.10.0): enum 追加 / Phase 3 (v1.12.0): `[Obsolete]` / Phase 4 (v2.0.0): 削除 |
| TSV データ駆動テスト | T04 §B で Korean/Portuguese/Spanish パターン流用 | §5.4 辞書ファイルサイズチェック | TSV 追加時は NuGet パッケージサイズ影響を確認（現状の 250MB 上限には余裕） |
| Multilingual への Misaki 能力追加 | T03 §A / T04 §A で `IMisakiCapableProcessor` 提案 | §4.2 Multilingual テスト確認項目 | Mi3 完了条件の MUST ゴールに昇格（現行は非ゴール扱い） |
| 命名揺れ `ToPiperIPA` vs `ToPiperIpa` | T03 §E で発見、PascalCase に寄せる方針 | §5.3 破壊的変更チェック | v2.0.0 で `ToPiperIPA` を削除、`ToPiperIpa` を正規名に確定 |
| Unity IL2CPP `[Preserve]` 検証 | T04 §B-1 で静的解析テスト推奨 | §4.2 `unity-meta-check` ジョブで補強 | `link.xml` 不要、クラスレベル `[Preserve]` で十分 |
| Unicode 定数クラス | T01 §推奨 2 で具体コード | §4.2 テストカウント変動で間接検証 | TA（統合後）の最初のコミットで `Internal/ChineseUnicode.cs` を追加 |
| Upstream 監視自動化 | なし | §6.4 代替案 C（Infra-3） | Mi4 開始前に `schedule` ワークフローで Misaki リポジトリ hash 監視 |

**リリース観点の ADR 提案**: 次回マイルストーン開始時、`docs/adr/0005-release-strategy-for-misaki-compatible.md` を作成し:

- 初回リリースは `-preview.N` を必ず挟む
- `Directory.Build.props` の共通 `<Version>` は維持（§6.4.6 で独立バージョニングへ移行予定だが当面維持）
- Release Please 導入後は PR 作成を自動化
- 破壊的変更チェックリストは CI で自動実行
- Multilingual 統合テスト通過を Mi 完了条件の MUST ゴールに昇格

を明記することで、Mi4〜Mi6 で再議論せずに済む。

#### E. 次回マイルストーンプロジェクト（英語/日本語 Misaki 等）への教訓 — リリース観点

T06 §7.3 で提案されている Mi4〜Mi6（英語/日本語/韓国語 Misaki 互換）に対し、リリース観点で持ち越すべき教訓:

1. **Release Please 先行導入**: Infra-1（§6.4.6 推奨）を Mi4 開始前に完了させる。Mi4 以降のバージョン更新 PR が自動化され、T06 相当のチケット工数がゼロになる。
2. **プレビュー版の常態化**: 全 Mi で `-preview.N → 安定版` の 2 段階リリースを標準化（§6.4 代替案 B）。外部要望ドリブン機能では **Phase 1 完了直後に preview を出す** ことで feedback を前倒し。
3. **独立バージョニングの部分導入**: §6.4.6 フェーズ 2「Multilingual メタパッケージ化」を Mi4 前に検証。Misaki 拡張は常に 2 パッケージ（`DotNetG2P.{Lang}` + `DotNetG2P.Multilingual`）に影響するため、変更のないパッケージはバージョン据え置きにしたい。
4. **Upstream 監視の自動化**: Infra-3（§6.4 代替案 C）を Mi4 開始前に稼働させ、Misaki Python 実装の仕様変更を週次で検知。手動追従では毎回 T01〜T06 相当の工数が発生する。
5. **Multilingual 統合の必須化**: Mi3 の最大の反省点は「`IMisakiCapableProcessor` 追加を T06 §7.1 送りにしてしまった」点。次回は**「Multilingual 経由のテストが通ること」を Mi 完了条件の MUST ゴール**に昇格する（§4.2 の Multilingual テスト項目を拡張）。
6. **Kokoro 連携サンプルの同梱**: §7 記載の Infra-6（旧 Infra-4）を Mi4 リリース直前に完了。Misaki 出力が実際に Kokoro で動くことをリリース前に保証する。
7. **SBOM / 破壊的変更チェックの自動化強化**: 現行の `dotnet pack -p:EnablePackageValidation=true` で破壊的変更検出は自動化済み（§5.3）。これに加えて CycloneDX SBOM の diff を GitHub Release 本文に自動添付することを Mi4 で検討。
8. **バージョン番号の意味論の明文化**: §5.1 のセマンティックバージョニング運用を全 Mi で徹底。`Feature flag` 方式（§6.2 代替案 A）を採用すれば Mi4〜Mi6 の全言語 Misaki 対応を**単一の Minor バージョン（v1.11.0）で済ませる**選択肢も取れる（既存ユーザーへの影響ゼロ）。
9. **リリース後 smoke の必須化**: §4.2「リリース後スモークテスト」で示されている「クリーン環境での `dotnet add package` + 実行」を Mi4 以降は必須フェーズ化。ロールバック戦略（§5.6）の発動を未然に防ぐ。

#### F. T06 / リリース観点の結論

- **フェーズ分割**: 次回は「実装 → preview → 本番」の 3 段階、Mi3 軽量化で実質 2 フェーズ運用。
- **チケット粒度**: 6 枚 → 3 枚（TA/TB/TC）に再設計。TC は Release Please 導入後は自動生成 PR のマージのみで完了。
- **並列化**: preview 版先行で Issue feedback を前倒し、破壊的変更チェックを T04 直後に CI 実行、リリースノート/Issue コメント文面を T03 完了時点から執筆開始。
- **ADR 導入**: リリース戦略を `docs/adr/0005-release-strategy-for-misaki-compatible.md` に確定させ、Mi4〜Mi6 で再議論しない。
- **Infra チケット先行**: Mi4 開始前に Infra-1（Release Please）/ Infra-3（Upstream 監視）/ Infra-4（破壊的変更自動化）を完了させる。
- **Multilingual 統合**: 次回 Mi の完了条件 MUST ゴールに昇格。
- **次回持越し**: Release Please、Preview 版運用、Upstream 監視、Kokoro サンプル、独立バージョニング（段階導入）、SBOM diff 自動添付の 6 項目をテンプレ化。

本節の判断は T05 §プロジェクト全体振り返りの追加レビュー（ドキュメント/QA 観点）と合わせて一つの全体総括を構成する。双方を参照してから次期マイルストーン（Mi4: 英語 Misaki 互換等）を起票すること。具体的には本セクション F と T05 §F の結論を統合した「Mi4 起票前チェックリスト」を `docs/adr/` に新設することを推奨する。

---

## 7. 後続タスクへの連絡事項

本チケットで Mi3 は完了となり、明示的な後続タスクはない。ただし将来チケット化すべき
課題を以下に記録する。

### 7.1 Multilingual ファサードの拡張

- `MultilingualG2PEngine.ToMisaki(text, Language.Chinese)` は本チケットで提供済み
- 将来の `ToMisaki(text)` 引数なし呼び出し（自動言語判定）では、Misaki 対応言語が
  1 言語（中国語）のみ → 英語/日本語 Kokoro 対応後に自動判定ロジックを整備

### 7.2 Misaki v1.1（注音パス）対応

- Misaki 上流で注音（Bopomofo）出力パスが追加された場合:
  - `DotNetG2P.Chinese` は既に `PinyinStyle.Zhuyin` をサポートしているため、
    `ToMisakiZhuyin()` 追加だけで対応可能なはず
  - Mi2 の `MisakiCompatibility` フラグに `ZhuyinMode` を追加する形も検討
- 代替案 C（6.2 節）の自動監視が動いていれば検知は自動化される

### 7.3 他言語の Kokoro 互換

Kokoro は複数言語をサポート:
- 英語 (`misaki/en.py`) — CMU 辞書ベース → `DotNetG2P.English` の既存実装と近い
- 日本語 (`misaki/ja.py`) — pyopenjtalk/fugashi ベース → `DotNetG2P.Core` の既存実装と近い
- 中国語 (`misaki/zh.py`) — **本チケットで対応済み**
- 韓国語 (`misaki/ko.py`) — `DotNetG2P.Korean` 対応可能

**次期マイルストーン案（Mi4〜Mi6）:**
- Mi4: 英語 Misaki 互換（CMU 辞書 + Flite LTS → Misaki 音素表への変換層）
- Mi5: 日本語 Misaki 互換（OpenJTalk パイプライン → Misaki 音素表）
- Mi6: 韓国語 Misaki 互換（Hangul-first → Misaki 音素表）

いずれも T01〜T06 と同様の 6 チケット構成で進める想定。

### 7.4 リリース管理の改善（6.2 節より抜粋）

以下を独立チケットとして追跡推奨:

- **Infra-1**: Release Please Action 導入調査 & PoC
- **Infra-2**: `tools/bump-version.ps1` スクリプト作成（11 ファイル一括更新）
- **Infra-3**: Misaki upstream 監視ワークフロー（週次 schedule + hash 差分検知）
- **Infra-4**: Kokoro 連携サンプルプロジェクト（`samples/DotNetG2P.Kokoro/`）
- **Infra-5**: 個別パッケージ独立バージョニングへの移行検討

### 7.5 Issue #56 クローズ条件

- NuGet.org で `DotNetG2P.Chinese 1.10.0` が検索可能になっている
- 質問者から「動作確認 OK」のコメントを受領
- 受領後に Issue を `Closed (completed)` にする
- 質問者からの返信が 2 週間ない場合はリリース済みを根拠に先行クローズ可（コメントで予告）

---

## 8. 紐づけ

- **マイルストーン**: Mi3（Misaki 互換中国語出力）
- **依存**: T05（Misaki 互換機能のドキュメント整備・ゴールデンデータ確定）
- **後続**: なし（将来課題は §7 に記録。Mi4 以降は独立マイルストーンとして起票）
- **関連 Issue**: [#56](https://github.com/ayutaz/dot-net-g2p/issues/56) — Misaki 互換中国語出力の要望
- **関連 PR**: 本チケットで作成する v1.10.0 リリース PR（マージ後に本ドキュメントへ追記）
- **関連ファイル**:
  - `.github/workflows/release.yml` — リリースワークフロー
  - `.github/workflows/ci.yml` — CI ワークフロー
  - `Directory.Build.props` — NuGet バージョン共通定義
  - `src/DotNetG2P.*/package.json` — UPM バージョン（10 ファイル）
  - `CLAUDE.md` — 進捗表の更新対象
  - `README.md` — バージョン表記の更新対象
