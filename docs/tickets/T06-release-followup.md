---
ticket: T06
title: Issue#56 フォローアップ・リリース準備
milestone: Mi3
status: 未着手
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
