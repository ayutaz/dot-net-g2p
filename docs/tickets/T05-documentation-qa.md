---
ticket: T05
title: ドキュメント更新・品質保証
milestone: Mi3
status: 完了
depends_on: [T04]
blocks: [T06]
---

# T05: ドキュメント更新・品質保証

## 1. タスク目的とゴール

T01-T04 で実装・テスト完了した Misaki 互換中国語 G2P 出力（`ToMisakiIPA()` / `ToMisakiIPABatch()`）について、ドキュメント整備と品質保証を行い、利用者が Misaki 互換出力を正しく使えるようにする。

**ゴール:**

- 利用者が README.md を読むだけで `ToMisakiIPA()` の使い方と出力形式を理解できる
- Kokoro TTS (KokoroSharp) との連携に必要な情報がドキュメントに揃っている
- パフォーマンス面で `ToMisakiIPA` が `ToIPA` と同等の速度であることが確認されている
- CLAUDE.md の進捗テーブルが最新状態に更新されている
- 設計ドキュメント・マイルストーンドキュメントの完了状態が反映されている

---

## 2. 実装する内容の詳細

### 2.1 README.md の更新

#### 2.1.1 冒頭コードサンプル（L22-24付近）への追記

現状の中国語サンプル:

```csharp
// 中国語G2P（ピンイン変換）
using var zhEngine = new ChineseG2PEngine();
zhEngine.ToPinyin("你好世界");  // => "ní hǎo shì jiè"
```

以下を直後に追加:

```csharp
// 中国語G2P（Misaki互換IPA — Kokoro TTS向け）
zhEngine.ToMisakiIPA("你好世界");  // Misaki互換IPA文字列（矢印声調記号付き、スペース区切り）
```

#### 2.1.2 特徴セクション（L78付近）の中国語G2P説明への追記

現状:

> **中国語G2P対応** — pinyin-data単字辞書（44,000語）+ phrase-pinyin-dataフレーズ辞書（411,000語）による多音字自動解決、声調変調（三声連読・一/不変調）、3種の出力スタイル、IPA（国際音声記号）・注音符号（ボポモフォ）出力、piper-plus 互換 IPA/PUA/Prosody API

変更後:

> **中国語G2P対応** — pinyin-data単字辞書（44,000語）+ phrase-pinyin-dataフレーズ辞書（411,000語）による多音字自動解決、声調変調（三声連読・一/不変調）、4種の出力スタイル、IPA（国際音声記号）・注音符号（ボポモフォ）出力、Misaki互換IPA出力（Kokoro TTS向け矢印声調・非音節化符号方式）、piper-plus 互換 IPA/PUA/Prosody API

（「3種」→「4種」、「Misaki互換IPA出力（Kokoro TTS向け矢印声調・非音節化符号方式）」を追加）

#### 2.1.3 詳細コードサンプル（L204-241付近、中国語G2Pセクション）への追記

`// Prosody 情報` の後に以下を追加:

```csharp
// Misaki 互換 IPA（Kokoro TTS向け）
string misakiIpa = zhEngine.ToMisakiIPA("你好");
// => "ni↗xau̯↓"
// 声調記号: → (1声), ↗ (2声), ↓ (3声), ↘ (4声)
// 声母: j→ʨ, q→ʨʰ, z→ʦ, c→ʦʰ
// 韻母: ai→ai̯, ao→au̯, ei→ei̯, ou→ou̯ (非音節化符号)

// 声調なし Misaki 互換 IPA
string misakiNoTone = zhEngine.ToMisakiIPA("你好", includeTones: false);
// => "nixau̯"

// バッチ変換
IReadOnlyList<string> misakiBatch = zhEngine.ToMisakiIPABatch(new[] { "你好", "世界" });
```

#### 2.1.4 API リファレンステーブル（L481-512付近、ChineseG2PEngine セクション）への追記

既存テーブルの `ToIpaWithProsodyBatch(texts, includeTones)` 行の後に以下の行を追加:

```
| `ToMisakiIPA(text)` | `string` | Misaki互換IPA文字列（矢印声調記号付き） |
| `ToMisakiIPA(text, includeTones)` | `string` | 声調制御付きMisaki互換IPA |
| `ToMisakiIPABatch(texts)` | `IReadOnlyList<string>` | バッチMisaki互換IPA変換 |
| `ToMisakiIPABatch(texts, includeTones)` | `IReadOnlyList<string>` | バッチMisaki互換IPA変換（声調制御） |
```

### 2.2 CLAUDE.md の更新

#### 2.2.1 進捗状況テーブル（L18付近）の中国語行を更新

現状:

```
| 中国語 | DotNetG2P.Chinese | C1-C6完了 | 936 | pinyin-data 44k + phrase-pinyin-data 412kエントリ、声調変調、IPA/注音/piper-plus互換 |
```

更新後:

```
| 中国語 | DotNetG2P.Chinese | C1-C6完了 | 936+ | pinyin-data 44k + phrase-pinyin-data 412kエントリ、声調変調、IPA/注音/piper-plus互換、Misaki互換IPA出力(Kokoro TTS向け) |
```

（テスト数を「936+」に更新（Misakiテスト追加分）、備考に「Misaki互換IPA出力(Kokoro TTS向け)」を追加）

#### 2.2.2 プロジェクト概要（L7-8付近）

変更不要。「pinyin-data辞書ベースの中国語ピンイン変換」の記述は変換方式の説明であり、出力フォーマットの追加は特徴セクションでカバーされるため。

### 2.3 docs/guides/misaki-compatible-chinese.md の最終更新

- 「備考」セクションに Mi1-Mi3 完了後の知見を追記
- 実装で判明した Misaki との差異や注意点があれば記録
- 「実装方式」セクションの「採用: 方式B」が実際の実装と一致していることを確認

### 2.4 docs/guides/misaki-milestones.md の進捗更新

マイルストーン進捗サマリテーブル（L136-140）を更新:

現状:

```
| **Mi1** | PinyinToMisaki 変換クラス | 未着手 |
| **Mi2** | API統合 + テスト | 未着手 |
| **Mi3** | ドキュメント・品質保証・リリース準備 | 未着手 |
```

更新後:

```
| **Mi1** | PinyinToMisaki 変換クラス | 完了 |
| **Mi2** | API統合 + テスト | 完了 |
| **Mi3** | ドキュメント・品質保証・リリース準備 | 完了 |
```

---

## 3. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当内容 |
|------|------|---------|
| ドキュメントライター | 1名 | README.md、CLAUDE.md、設計ドキュメントの更新。コードサンプルの正確性確認 |
| QAエンジニア | 1名 | パフォーマンステスト作成・実行、Misaki Python実装との出力比較、コードサンプルの動作確認 |

**合計: 2名**

ドキュメントライターとQAエンジニアは並行作業が可能。ドキュメントライターがコードサンプルを書き、QAエンジニアがそのサンプルの動作確認を行う。

---

## 4. 提供範囲とテスト項目

### 4.1 ドキュメント正確性チェック

- [ ] README.md のコードサンプルが実際にコンパイル・実行できること
  - `ToMisakiIPA("你好")` のコメントに書かれた出力例が実際の出力と一致すること
  - `ToMisakiIPA("你好世界")` のコメントに書かれた出力例が実際の出力と一致すること
  - バッチ API のサンプルが正しいこと
- [ ] API リファレンステーブルのメソッドシグネチャが実装と一致すること
  - 戻り値型（`string` / `string[]`）が正しいこと
  - オーバーロードの引数名・型が正しいこと
- [ ] CLAUDE.md のテスト数が `dotnet test` の実行結果と一致すること
- [ ] misaki-milestones.md の各マイルストーンのチェックリストが全て完了状態であること

### 4.2 コードサンプル動作確認

- [ ] README.md に記載した全コードスニペットを DotNetG2P.Console サンプルプロジェクト等で実行し、コメントの出力例と一致することを確認
- [ ] 声調記号（→ ↗ ↓ ↘）が正しく表示されること（Unicode の矢印文字が化けないこと）

### 4.3 パフォーマンステスト

- [ ] `ToMisakiIPA` が `ToIPA` と同等の処理速度であることを確認
  - 測定方法: 同一テキストセット（100文以上）を各メソッドで1000回変換し、平均処理時間を比較
  - 許容範囲: `ToMisakiIPA` の処理時間が `ToIPA` の 1.2 倍以内
  - 根拠: `PinyinToMisaki` は `PinyinToIpa` と同じく静的辞書参照のみでアルゴリズム計算量は同等。声調マッピングが配列インデックスアクセスのため差が出にくい
- [ ] テストクラス `ChineseMisakiPerformanceTests.cs` の作成（任意）
  - BenchmarkDotNet または Stopwatch による簡易ベンチマーク
  - CI に組み込む場合は `[Trait("Category", "Performance")]` で分離

### 4.4 Misaki Python実装との出力比較（可能な範囲）

- [ ] Misaki Python パッケージ (`pip install misaki`) をローカル環境にインストールし、以下のテストケースで出力を比較:
  - 基本: `"你好"` → 期待: `ni↗xau̯↓`（三声連読: 3+3 → 2+3）
  - 四声: `"妈麻马骂"` → 各声調が正しい矢印で出力されること
  - 声母差異: `"鸡七"` → `j/q` が `ʨ/ʨʰ` で出力されること
  - 韻母差異: `"来回"` → `ai/ui` が非音節化符号付きで出力されること
  - 声調変調: `"一个"` → 「一」の変調が反映されること
- [ ] 差異がある場合は misaki-compatible-chinese.md の備考セクションに記録

---

## 5. 実装に関する懸念事項とレビュー項目

### 5.1 ドキュメントの言語対応

- **現状**: プロジェクトは日本語で記述（CLAUDE.md「開発言語」セクションに明記）。README.md は日本語版が主、英語版 (`README_EN.md`) と中国語版 (`README_ZH.md`) が別途存在
- **懸念**: Misaki互換機能の主要ユーザーは Kokoro TTS / KokoroSharp のユーザーであり、英語圏・中国語圏のユーザーも多い
- **対応方針**:
  - 本チケットでは日本語 README.md のみを更新対象とする
  - README_EN.md / README_ZH.md への反映は T06（Issue#56 フォローアップ）で対応する
  - コードサンプルはプログラミング言語（C#）で書かれるため、日本語コメント以外の部分は言語非依存

### 5.2 Misaki 仕様変更への追従方針

- **懸念**: Misaki には Legacy パス（IPA+矢印）と v1.1 パス（注音符号）の2つが存在する。現在の実装は Legacy パスのみ対象。将来 Misaki が仕様変更した場合にドキュメントが陳腐化するリスクがある
- **対応方針**:
  - ドキュメントに「本機能は Misaki Legacy パス（Kokoro-82M で使用される IPA+矢印方式）を対象とする」と明記する
  - Misaki のバージョンや対象コミットハッシュをドキュメントに記録しておく
  - 仕様変更時は `PinyinToMisaki.cs` のマッピングテーブル差分のみの修正で対応可能な設計であることをドキュメントに記載する

### 5.3 レビュー項目

- [ ] README.md のコードサンプル内のコメント出力例が、実際の `ToMisakiIPA()` 出力と完全一致すること
- [ ] Unicode 文字（矢印声調記号 → ↗ ↓ ↘、非音節化符号 U+032F）がドキュメント内で正しくレンダリングされること（GitHub Markdown ビューアで確認）
- [ ] CLAUDE.md の更新が他のセクション（プロジェクト概要、技術スタック等）と整合していること
- [ ] API リファレンステーブルの追加行が既存行のフォーマット（パイプ区切り、等幅フォント等）と一致していること
- [ ] misaki-milestones.md のチェックボックスが全て `[x]` になっていること

---

## 6. 一から作り直すとしたら

### 6.1 ドキュメント生成の自動化

現状の README.md は手動メンテナンスで約600行以上に達しており、APIリファレンステーブルの更新が手作業である。一から設計し直すなら:

- **docfx によるAPI ドキュメント自動生成**: XML ドキュメントコメントから API リファレンスを自動生成し、README.md からは API テーブルを除去。README にはクイックスタートと概要のみを記載し、詳細は docfx サイトへリンクする
- **テスト結果からの自動ドキュメント生成**: `[Fact]` テストの `DisplayName` や `InlineData` からサンプル入出力ペアを抽出し、ドキュメントのコードサンプルを自動更新するスクリプトを用意する。これにより「ドキュメントの出力例と実際の出力が異なる」問題を根本解決できる
- **Verify ライブラリの活用**: テストの期待値をスナップショットファイルで管理し、ドキュメントと共有する仕組み

### 6.2 CHANGELOG 管理方式の再考

現状はリリースタグとGitHub Releasesで変更履歴を管理しているが:

- **conventional commits + 自動 CHANGELOG 生成**: コミットメッセージ規約（`feat:`, `fix:`, `docs:` 等）を導入し、`release-please` や `standard-version` で CHANGELOG.md を自動生成する。Misaki 互換出力のような機能追加が自動的に CHANGELOG に反映される
- **Keep a Changelog 形式の手動 CHANGELOG.md**: 各リリースごとに Added / Changed / Fixed を手動記載。自動化ほど効率的でないが、日本語プロジェクトとの相性は良い

### 6.3 言語別 README の統合管理

現状の README.md / README_EN.md / README_ZH.md の3ファイル手動管理は更新漏れが起きやすい。テンプレートエンジン（Scriban等）で共通テンプレートから多言語READMEを生成する方式が理想的。

### ドキュメント戦略の追加レビュー

本セクションは T05 のスコープ（Misaki 互換中国語 G2P のドキュメント整備）を一段階引き上げ、DotNetG2P プロジェクト全体のドキュメント戦略を「一から作り直すとしたら」どう構築するかを整理した追加レビューである。既存の 6.1〜6.3 の方針を補強しつつ、プロジェクト固有の制約（日本語ファースト、Unity 対応、8 言語 G2P、Kokoro TTS コミュニティとの接点）を踏まえた実装可能な改善案を提示する。

#### A. 現状記載（6.1〜6.3）の評価

| 観点 | 現状記載の評価 | 不足している点 |
|------|--------------|--------------|
| 6.1 DocFX | docfx によるAPI自動生成の方向性は正しい。現在 `docs/docfx.json` にて 10 パッケージの DLL を metadata ソースとして登録済みで、基盤は既に整っている | (1) README から API テーブルを"除去する"と書いているが、README はクイックスタートの目的もあるため「除去」ではなく「主要 API 5〜10 個に絞り残りは DocFX に誘導」とするのが妥当。(2) `docs/index.md` が英語で書かれているため、日本語ドキュメントとの整合性が取れていない。(3) `CI` で DocFX をビルドしているが、生成物を GitHub Pages に公開する `release.yml` フック、PR プレビューの仕組みが記述されていない |
| 6.2 CHANGELOG | conventional commits + release-please の候補提示は妥当 | (1) 現状 `CHANGELOG.md` がリポジトリルートに存在することが前提だが、実際のファイル有無と現行 `release.yml` との統合方針が不明。(2) 日本語コミットメッセージ運用と conventional commits 英語プレフィックスの混在方針が未整理。(3) NuGet/UPM の両パッケージ系統へのバージョン番号同期について言及なし |
| 6.3 多言語 README | Scriban による生成は理論上可能だが現実的な運用負荷が考慮不足 | (1) README_EN.md / README_ZH.md の"存在は知られているが誰がメンテするか"の責務が未定義。(2) 翻訳自動化（DeepL / LLM）の現実的な選択肢に触れられていない。(3) 8 言語の G2P ライブラリであるのに README が 3 言語のみな理由（需要の実測値）の記載がない |

全体として方向性は正しいが、**"何を"作るかの提示に留まり"どう運用するか"の実務面が浅い**。以下に具体案を示す。

#### B. Single Source of Truth（SSoT）化の具体案

現状の情報源泉は 5 系統に分散している:

1. README.md / README_EN.md / README_ZH.md（ユーザー向けクイックスタート + API 概要）
2. CLAUDE.md（開発者向け進捗・アーキ方針・音素体系）
3. docs/guides/\*.md（設計ドキュメント・マイルストーン計画）
4. docs/index.md + DocFX 生成 API ドキュメント
5. コード内 XML ドキュメントコメント（公開 API の summary / param / returns）

これらを **XMLDoc を SSoT とする** 形に再編する案:

**B-1. XMLDoc 拡充と抽出パイプライン**

- 各 `{Lang}G2PEngine.cs` の public メソッドに以下を完全記載する:
  - `<summary>` — 1 行要約（README 抜粋用）
  - `<remarks>` — 詳細説明（DocFX 詳細ページ用）
  - `<example>` — 実行可能な C# コードブロック（README/DocFX 共用）
  - `<returns>` / `<param>` — API テーブル生成用
- `tools/GenerateReadmeSnippets` というビルドタスクを新設し、XMLDoc の `<example>` タグから C# コードブロックを抽出して `docs/_generated/snippets/` へ出力
- README.md / README_EN.md 内では `<!-- @snippet:ChineseG2PEngine.ToMisakiIPA -->` のようなプレースホルダを使い、ビルド時に `_generated/snippets/` の内容へ置換する（Markdown インクルード方式）
- これにより「コード例の変更は XMLDoc の修正だけで全ドキュメントに波及」する構造になり、T05 第 4 節で懸念している「コメント出力例と実装の乖離」を根本解決できる

**B-2. API テーブルの自動生成**

- `tools/GenerateApiTable` ビルドタスクを新設し、DocFX の `metadata` ステージで生成される中間 YAML (`api/*.yml`) をパースして、言語パッケージごとに README 用の Markdown テーブル (`docs/_generated/api-tables/{lang}.md`) を出力
- README.md の「API リファレンス」節は手書きをやめ、`<!-- @include:api-tables/chinese.md -->` 形式の include プレースホルダにする
- 現在 T05 第 2.1.4 節で手作業追加している `ToMisakiIPA(text)` 等の行が自動追記されるようになる

**B-3. 進捗テーブルの CLAUDE.md ↔ README.md 同期**

- `tools/GenerateProgressTable` により、テスト実行結果（`dotnet test --logger trx`）と csproj のバージョン情報から進捗テーブルを機械生成
- CLAUDE.md と README.md 両方の進捗テーブルを `<!-- @include:progress-table.md -->` に置換し、手動更新を撤廃
- T05 第 2.2.1 節のテスト数手動更新（`936` → `936+`）のような曖昧運用を排除する

**B-4. ビルドタスクとしての統合**

- `Directory.Build.targets` に `BeforeTargets="Build"` で上記 3 タスクを順序実行
- `dotnet build` すれば README が自動更新される構造にし、CI の `validation` ジョブで `git diff --exit-code README.md` を実行して生成差分コミット漏れを検出

#### C. 英語版ドキュメントの提供戦略

現状 README_EN.md / README_ZH.md は存在するが、T05 第 5.1 節が示すように Misaki 機能更新では「T06 で対応する」と後送りされ、更新遅延のリスクが放置されている。DotNetG2P のユーザー層は Kokoro TTS コミュニティ（英語話者比率が高い）を含むため、英語版を「翻訳物」ではなく「一級市民」として扱う戦略が必要。

**C-1. 英語を SSoT にする逆転発想**

- CLAUDE.md 冒頭の「開発言語: 日本語」は維持しつつ、**コード内 XMLDoc だけは英語 SSoT** とする例外ルールを設ける
- 理由: (1) DocFX が生成する API リファレンスは国際標準として英語が適切、(2) NuGet.org / nuget-packages 検索の SEO が英語前提、(3) Kokoro TTS コミュニティへの導線となる
- 日本語コメント・日本語 README のコンテンツは英語 XMLDoc から LLM または DeepL API で生成する逆方向パイプラインにする
- これにより「英語版 README は翻訳待ち」という現状の課題が解消される

**C-2. 翻訳自動化パイプライン**

- `tools/TranslateDocs` というユーティリティを作成し、以下の翻訳戦略を段階的に検討:
  - **段階 1: DeepL API (Pro)** — 技術用語の精度が高い。月 50 万文字まで有料プラン。README 規模（3 言語 × 600 行）なら月 \$10 程度でカバー
  - **段階 2: OpenAI API / Anthropic API** — プロンプトで「音声学専門用語は保持、`ToMisakiIPA` 等のコードは翻訳しない」と制御可能。コスト \$5〜20/月
  - **段階 3: ローカル LLM (Qwen2.5-7B 等)** — コスト 0 だが品質は要評価
- 翻訳対象は `<!-- @translate:start -->` / `<!-- @translate:end -->` マーカーで区切り、コードブロック・表・リンクはスキップ
- CI で翻訳差分を PR として自動作成（`github-actions[bot]` が `docs: auto-translate README to EN/ZH` PR を作成）
- 日本語プロジェクト維持方針を守りつつ、英語・中国語版の更新遅延をゼロにできる

**C-3. 言語カバレッジの段階的拡大**

- 第 1 フェーズ: 既存の日/英/中 3 言語を継続（ただし自動翻訳化）
- 第 2 フェーズ: Kokoro TTS コミュニティで需要が高い言語（韓国語・スペイン語）を追加。ライブラリが G2P 対応する言語と README 対応言語の一致は親和性が高い
- 第 3 フェーズ: G2P 対応 8 言語全てに README を展開（自動翻訳のため実装コストは一定）

#### D. DocFX API doc からのサンプルコード自動抽出

T05 第 6.1 節の `[Fact]` の DisplayName / InlineData からサンプル抽出という方向性を具体化:

**D-1. xUnit テストからのサンプル抽出**

- `tests/DotNetG2P.Tests/` 配下のテストに `[Trait("DocSample", "chinese.misaki-ipa")]` のようなタグを付与
- `tools/ExtractTestSamples` ツールが MSTest の test assembly を Reflection で読み、`DocSample` トレイト付きテストから:
  - `[InlineData("你好", "ni↗xau̯↓")]` などの引数 → `input = "你好", expected = "ni↗xau̯↓"` の形で抽出
  - テストメソッド本体の 1 行分を「使用例」として抽出（`engine.ToMisakiIPA("你好")` の行）
- 抽出結果を `docs/_generated/examples/chinese.misaki-ipa.md` に出力し、README と DocFX に include する

**D-2. DocFX `<example>` タグとの統合**

- `<example>` タグ内に `<code>` で C# コードを記述すると、DocFX はその内容を構文ハイライト付きで API ページに自動埋め込みする
- `tools/ExtractTestSamples` の出力を XMLDoc 形式に整形するモードを設け、csproj ビルド前に `obj/GeneratedDocComments/` へ追加 XMLDoc を出力、`DocumentationFile` プロパティで統合する
- これにより「テストがパスしている = ドキュメントのサンプル出力値が正確」という保証が得られる

**D-3. Verify/Snapshot との連携（オプション）**

- `Verify.Xunit` パッケージ導入でスナップショット駆動の期待値管理が可能
- `.verified.txt` ファイルをドキュメントの「期待出力」として DocFX に include すれば、Misaki Python との出力比較（T05 第 4.4 節）も自動化できる

#### E. Conventional Commits + release-please による CHANGELOG 自動生成

**E-1. 運用ポリシー（日本語プロジェクトでの折衷案）**

- **コミットメッセージの構造**: `<type>: <日本語の要約>` という折衷形式を採用
  - 例: `feat: 中国語 G2P に Misaki 互換 IPA 出力を追加`
  - 例: `fix: ポルトガル語 EP 方言の非ストレス /e/ 弱化を修正`
  - 例: `docs: スウェーデン語 README のクイックスタートを更新`
- `type` は英語（conventional commits 標準 `feat`/`fix`/`docs`/`chore`/`refactor`/`perf`/`test`/`build`/`ci`）を使用し、release-please のパーサと互換を取る
- 要約本文は日本語で記述し、CLAUDE.md の「開発言語: 日本語」方針を維持
- 既存コミット履歴を見ると `feat: スウェーデン語G2P...` `fix: [Preserve]使用パッケージ...` `chore: Bump the all-nuget-dependencies group` の形で既に conventional commits 相当になっているため、運用の追加負荷は低い

**E-2. release-please の導入手順**

- `.github/workflows/release-please.yml` を新設し、`googleapis/release-please-action@v4` を利用
- release type を `simple`（CHANGELOG + tag 管理のみ）または `dotnet` にする
  - `dotnet` タイプは `Directory.Build.props` の `<Version>` 要素を自動更新するが、本プロジェクトのように 10 csproj が共通 props を参照する構造と相性が良い
- `release-please-config.json` で以下を設定:
  - `packages`: モノレポ構造として 10 パッケージを列挙
  - `include-component-in-tag`: `false`（単一バージョン運用）
  - `changelog-sections`: 日本語の見出しにマッピング（`feat` → 新機能、`fix` → バグ修正、`docs` → ドキュメント、`perf` → パフォーマンス改善、等）
- 生成される `CHANGELOG.md` は「## v1.10.0 - 2026-04-XX」形式 + 各コミットの日本語本文が列挙される形になる

**E-3. 現行 release.yml との統合**

- 現在の `release.yml` は tag push トリガーで NuGet publish を行っている想定
- release-please は `main` ブランチへの commit を監視し、次期バージョンの PR を自動作成する（`chore(main): release v1.10.0` という PR）
- この PR をマージすると tag と GitHub Release が自動作成 → 既存の `release.yml` が発火 → NuGet publish という流れになり、既存ワークフローを破壊しない
- UPM パッケージのバージョン（`com.dotnetg2p.*`）も release-please の `extra-files` 設定で同期可能

**E-4. CI でのドキュメントビルド・リンクチェック**

- `.github/workflows/ci.yml` に `docs` ジョブを追加:
  - `dotnet build -c Release` で DocFX metadata ソースの DLL をビルド
  - `docfx docs/docfx.json --warningsAsErrors` で DocFX サイトを警告ゼロでビルド
  - `lychee --no-progress --exclude-mail README.md README_EN.md README_ZH.md docs/**/*.md` でリンク切れチェック（`lychee-action@v1` を使用）
  - `markdownlint-cli2` で Markdown フォーマット検証
  - `tools/GenerateReadmeSnippets` 実行後の `git diff --exit-code README.md` で SSoT 生成差分のコミット漏れを検出
- PR プレビュー: `actions/upload-pages-artifact` + `actions/deploy-pages` で PR プレビュー用 GitHub Pages にデプロイし、PR コメントに URL を自動投稿

#### F. 実装優先度と段階的ロードマップ

| フェーズ | 実装項目 | 推定工数 | 期待効果 |
|---------|---------|---------|---------|
| Phase 1（即時） | CI ドキュメントビルド + リンクチェック（E-4 の後半）、release-please 導入（E-2） | 1〜2 日 | リンク切れゼロ、CHANGELOG 自動化 |
| Phase 2（短期） | XMLDoc 拡充 + API テーブル自動生成（B-2）、進捗テーブル自動生成（B-3） | 3〜5 日 | API リファレンス手動更新の撤廃 |
| Phase 3（中期） | テストからのサンプル抽出（D-1 + D-2）、snippet include 方式（B-1） | 5〜7 日 | 出力例と実装の乖離を根本解決 |
| Phase 4（長期） | 翻訳自動化パイプライン（C-2）、多言語 README 統合管理（6.3 の具体化） | 7〜10 日 | 英語・中国語版の即時同期 |

T05 の本来スコープは Misaki 互換機能のドキュメント整備に限定されているため、本追加レビューで提示した改善案は別途独立したドキュメント戦略チケット（仮称 T07）として切り出すことを推奨する。特に Phase 1 と Phase 2 は既存の CI 資産と整合性が高く、他の機能開発ブランチへの影響が少ないため、先行着手の価値が大きい。

### プロジェクト全体振り返りの追加レビュー

本節は T05 の最終段階で、Mi1〜Mi3 を通じた全体計画を「もし白紙から引き直すなら」の視点で振り返るものである。ドキュメント/QA 観点からの総括であり、T06 §プロジェクト全体振り返りの追加レビュー（リリース観点）と対になっている。Phase 1 (T01/T02)・Phase 2 (T03/T04) で既に浮上した改善案（`record` + `switch` 式、TSV データ駆動テスト、`ChineseOutputFormat` enum による戦略パターン、`IMisakiCapableProcessor` による Multilingual 統合、命名揺れ `ToPiperIPA` vs `ToPiperIpa` の整理、Unity IL2CPP `[Preserve]` 静的検証テスト等）を統合し、ドキュメント・QA の観点で次回に持ち越すべき教訓を明示する。

#### A. フェーズ分割の代替案（ドキュメント観点）

現行は **Mi1（T01/T02）→ Mi2（T03/T04）→ Mi3（T05/T06）** の 3 フェーズ / 6 チケット構成だが、実装規模（ChineseG2PEngine への 4 メソッド追加＋変換クラス 1 個＋マッピング 18 エントリ差分＋テスト 1 ファイル）に比してフェーズ境界が多すぎ、**ドキュメントを「最後にまとめて書く」フェーズに追いやった結果、T03 のシグネチャ確定と README 文面のドリフトが発生しやすい構造**になっている。

- **代替案 A-1: 2 フェーズ統合（Mi1+Mi2 = 実装＋ドキュメント、Mi3 = リリース判断のみ）**
  - Mi3 を「リリース判断」だけに切り詰め、ドキュメント更新は Mi1+Mi2 内部で「コードと同一 PR」の運用にする。これにより T05 §1.1 で挙げた「README の出力例と実装出力の一致保証」が自動化される（実装と同じコミットで書くため）。
- **代替案 A-2: ドキュメント先行フェーズの追加**
  - Mi0（スパイク＋ドキュメント骨子） / Mi1（実装） / Mi2（テスト＋リリース） の 3 フェーズ。Misaki 本家仕様のドキュメント化と `docs/guides/misaki-compatible-chinese.md` の確定を実装前に完了する。T06 §6.1 の「Misaki 仕様書との差分検証を T04 まで後ろ倒しにした反省」と整合する。
- **推奨**: Mi3 規模の小さい拡張には **A-1（2 フェーズ）** を推奨。他言語モジュールが採用した 4 フェーズ構成（S1〜S4, F1〜F4, P1〜P4, Sw1〜Sw4）はゼロからの大規模実装用フォーマットであり、既存エンジンへの差分追加には過剰。

#### B. チケット粒度の再設計

T01〜T06 のドキュメント行数を合計すると約 5,000 行（T01: 1,023 / T02: 968 / T03: 802 / T04: 1,337 / T05: 445 / T06: 約 560）に達しており、**実装 1 行あたりのチケット行数が他言語マイルストーン比で 5〜10 倍**に膨張した。要因の大部分は各チケット末尾の「一から作り直すとしたら」＋「追加レビュー」サブセクションで横断論点（record+switch 式、戦略パターン、TSV 駆動、Multilingual 統合）を**フェーズごとに繰り返し議論**したためである。

- **T01+T02 統合（= PinyinToMisaki の完成）**: T01 の record+switch 式推奨と T02 の同方式推奨はほぼ同一内容。統合して 1 枚で管理した方が重複が消える。
- **T03+T04 統合（= エンジン API + テスト同時確定）**: T04 §B の TSV データ駆動テスト、T04 §A の Multilingual プレースホルダ、T04 §B-1 の `[Preserve]` 検証テスト、T04 §C の KokoroSharp 契約テストは、いずれも T03 の API 設計と同時に決めるべき項目。直列化した結果、T04 のレビューが 1,337 行に肥大化した。
- **T05+T06 統合（= ドキュメント＋リリース＋Issue 対応）**: 実作業工数は合算で 0.5〜1 人日。T05 で README/CLAUDE.md を更新し T06 でバージョンだけ上げる工程分離は過剰。統合版では「バージョン更新＋ドキュメント＋リリース実行＋Issue コメント投稿」を 1 PR で完結させる。
- **再設計後のチケット構成案（3 枚）**:
  1. **TA: マッピング＋変換クラス＋エンジン API 実装**（旧 T01+T02+T03） — ADR 決定事項を前提にリファレンス実装のみ
  2. **TB: テスト＋ドキュメント**（旧 T04+T05） — TSV データ駆動テスト＋README/CLAUDE.md 同時更新
  3. **TC: リリース＋Issue フォロー**（旧 T06） — バージョンバンプ＋CI 実行＋Issue コメント
- 6 → 3 枚でチケット枚数が半減し、各チケットごとに 3 エージェントレビューを実施する現行運用の工数も半減する。

#### C. 依存関係の並列化機会（T05 視点）

現行は `T01→T02→T03→T04→T05→T06` の完全直列だが、並列化可能な作業は以下の通り:

- **T04 テストは T03 API 完了前に書き始められる**: T04 §B-1 の `[Preserve]` 属性存在確認テスト、T04 §B 記載の TSV ローダ実装、TSV の入力カラム（漢字＋ピンイン）は T03 のシグネチャ確定を待たずに準備できる。期待値カラムは空のまま PR を先に用意し、T03 確定後に埋める。
- **T05 ドキュメントは T04 完了を待たなくて良い**: README への「Misaki 互換出力」説明文・CLAUDE.md 進捗表更新・API リファレンス行追加は、T03 のシグネチャ決定後すぐ書ける。T04 完了待ちの理由は「出力例が実装出力と一致する」確認のみで、これは最後に実行すれば良い。
- **T05 パフォーマンステストは T04 と並行**: テストハーネス（Stopwatch ラッパー、測定対象リスト）は T04 と独立に書ける。
- **T06 バージョン更新は T05 と並行**: T06 §2.2 の「11 ファイルのバージョン更新」は T05 のドキュメント更新と完全独立。同一 PR にまとめるメリットの方が大きい。
- **再構成後の依存グラフ案**:
  - `TA(実装) ← TB(テスト+ドキュメント 入力設計先行) → TC(リリース)`
  - `TA → TB` は実装確定後に合流、TB 内部で TSV 入力設計とドキュメント骨子を並行
- 理論上の最短日数は現行（直列）より 30〜40% 短縮可能。

#### D. Phase 1/2 レビューで共通して浮上した課題の統合（ドキュメント観点）

Phase 1/Phase 2 の「一から作り直すとしたら」＋「追加レビュー」サブセクションを横断して同じ論点が繰り返されている。本来は Mi1 冒頭でプロジェクト全体の設計決定事項（ADR: Architecture Decision Record）として一度記録すべきだった。

| 横断論点 | T01/T02 での提示 | T03/T04 での再提示 | ドキュメント上の統合方針 |
|---------|----------------|-------------------|---------|
| `record + switch` 式への移行 | T01 §6.5.2 方式 3 / T02 §6.7.3 方式 3 で**最有力**判定 | T03 では触れず | `docs/adr/0001-pinyin-mapping-strategy.md` に「短期は独立テーブル維持、中期で `PinyinMappingTable` record 化」と記録 |
| 戦略パターン / `ChineseOutputFormat` enum | なし | T03 §B で Phase 1〜4 の段階計画 | `docs/adr/0002-chinese-output-format-enum.md` で v1.10.0 → v2.0.0 の移行ロードマップを確定 |
| TSV データ駆動テスト | なし | T04 §B で Korean/Portuguese/Spanish のパターン流用推奨 | `docs/adr/0003-tsv-driven-test.md` で全言語の `tests/TestData/{Lang}G2P/` 配置規則を標準化 |
| Multilingual への Misaki 能力追加 | なし | T03 §A / T04 §A で `IMisakiCapableProcessor` を提案、T04 はプレースホルダ Skip のみ | Mi 完了条件の必須項目に**「Multilingual 経由のテストが通る」**を昇格 |
| 命名揺れ `ToPiperIPA` vs `ToPiperIpa` | なし | T03 §E で既存バグ発見、`ToPiperIpa` (PascalCase) に寄せる方針 | `docs/adr/0004-api-naming-convention.md` に「3 文字以上の頭字語は PascalCase」を明記 |
| Unity IL2CPP `[Preserve]` 検証テスト | なし | T04 §B-1 で静的解析テスト追加を推奨 | 全言語共通テストクラス `{Lang}G2PEnginePreserveAttributeTests` を共通テンプレ化 |
| Unicode 定数クラス | T01 §推奨 2 で具体コード提示 | なし | 全言語パッケージの `Internal/{Lang}Unicode.cs` を規約化 |
| エントリ順序統一＋差分メタテスト | T01 §推奨 1/3 で提示 | T04 で個別言及なし | TB（テスト統合）で `MisakiDiffersFromIpa_OnlyAtKnownPositions` 類を必須化 |

**ADR 化の提案**: 次回マイルストーン開始時、実装着手前に `docs/adr/{番号}-{タイトル}.md` を 1 枚作成し、上記の「横断論点の決定事項」を全て先取りしておく。これにより各チケットの「一から作り直すとしたら」セクションは「本チケット固有の振り返り」のみに集中でき、ドキュメント総量が 5,000 行から 2,000 行程度に削減できる見込み。なお ADR 形式は既存の `docs/guides/misaki-compatible-chinese.md`（設計ドキュメント）と補完関係にあり、前者が「決定事項のみ簡潔に」、後者が「背景を含む詳細設計」を担う。

#### E. 次回マイルストーンプロジェクト（英語/日本語 Misaki 等）への教訓 — ドキュメント/QA 観点

T06 §7.3 で提案されている Mi4〜Mi6（英語/日本語/韓国語 の Misaki 互換）に対し、ドキュメント/QA 観点で持ち越すべき教訓:

1. **ADR 先行**: 上記 D の横断論点を予め ADR として確定させ、個別チケットで再議論しない。
2. **ドキュメント先行書式の統一**: README.md / CLAUDE.md / `docs/guides/*-milestones.md` へのパッチを**コードと同じ PR 内**で書く（Mi3 で後追いドキュメント更新になった反省点）。`*.md` パッチ用のテンプレート（「### 特徴 の箇所に 1 行追加」「API リファレンス表に N 行追加」等）を `tools/templates/misaki-docs-patch.md` として用意する。
3. **TSV ゴールデンデータセットの既定化**: 「Misaki Python 実装との差分比較」が本プロジェクトの品質指標の要。TA 段階で TSV (`misaki_golden.tsv`) を 100 ケース以上準備し、Phase 1 終了時点で PER を測定可能にする。スペイン語（PER 1.69%）、ポルトガル語（異音 7 規則）の評価ツール `tools/DotNetG2P.{Lang}Eval` をテンプレ流用。
4. **Multilingual ファサード統合の必須化**: Mi3 の最大の反省点は「Multilingual 統合を T04 で見送ったまま Mi3 が完了しそう」だった点。次回は「Multilingual 統合テスト通過」を Mi の完了条件（非ゴールではなく MUST ゴール）に昇格させる。
5. **Kokoro 連携サンプルの同梱**: Mi3 で「動作確認手順の提示のみ」にとどまった反省を踏まえ、`samples/DotNetG2P.Kokoro/` を Mi4 で新設し、Misaki 出力 → KokoroSharp 入力の実動作を手動検証可能にする。
6. **パフォーマンステストの CI 統合**: T05 §4.3 でローカル実行にとどまった反省を踏まえ、Mi4 では `[Trait("Category", "Performance")]` で CI 分離しつつ、日次スケジュール実行で性能退行を検知する。
7. **XML ドキュメンテーションコメントの網羅**: Misaki 互換 API には `<remarks>` に「KokoroSharp 等の Kokoro TTS C# 実装にそのまま入力可能」の 1 行を必須化（T03 §C 推奨事項）。IntelliSense でのディスカバリが主要な利用経路。

#### F. T05 / ドキュメント/QA 観点の結論

- **フェーズ分割**: 次回は 2 フェーズ（実装＋ドキュメント一体 / リリース判断）に圧縮。
- **チケット粒度**: 6 枚 → 3 枚（TA/TB/TC）に再設計。
- **並列化**: T04 テスト TSV 入力先行、T05 ドキュメント先行で 30〜40% リードタイム短縮。
- **ADR 導入**: 横断論点を Mi 開始前に確定し、チケット末尾レビューの重複を排除。
- **ドキュメント/コード同時更新**: README/CLAUDE.md パッチを実装 PR 内に必須化し、事後ドリフトを防止。
- **次回持越し**: ADR・ドキュメント・TSV・Multilingual・Kokoro サンプル・性能 CI の 6 項目をテンプレ化。

本節の判断は T06 §プロジェクト全体振り返りの追加レビュー（リリース観点）と合わせて一つの全体総括を構成する。双方を参照してから次期マイルストーンを起票すること。

---

## 7. 後続タスクへの連絡事項

T06（Issue #56 フォローアップ）に伝えるべき情報:

### 7.1 Issue #56 へのフォローアップコメント

- T05 完了後、Issue #56 に実装完了の報告コメントを投稿すること
- コメントには以下を含める:
  - `ToMisakiIPA()` の使用例コード
  - NuGet パッケージバージョン（Mi3 リリース後のバージョン番号）
  - 既知の制限事項（Legacy パスのみ対応、Multilingual 層未統合等）

### 7.2 README_EN.md / README_ZH.md への反映

- T05 では日本語 README.md のみを更新する
- T06 で英語版・中国語版への反映を行うこと
- 中国語版は Misaki の主要ユーザー層と重なるため、特に丁寧な記述が望ましい

### 7.3 Multilingual 層への統合検討

- T01-T04 の実装では `DotNetG2P.Multilingual` への統合は見送っている
- `MultilingualG2PEngine` に `ToMisakiIPA()` を追加するかは T06 で検討すること
- 追加する場合、`MultilingualG2POptions` に Misaki 出力モードの設定が必要になる可能性がある

### 7.4 パフォーマンステスト結果の引き継ぎ

- T05 で実施したパフォーマンステスト結果（ToMisakiIPA vs ToIPA の処理時間比較）を T06 に引き継ぐ
- 性能劣化が見られた場合はその原因と改善案を記録しておくこと

### 7.5 Misaki 仕様追従の監視

- Misaki リポジトリ (https://github.com/hexgrad/misaki) の更新を定期的に確認する体制を T06 で検討すること
- 特に v1.1 パス（注音符号方式）への対応要否は KokoroSharp コミュニティの需要次第

---

## 8. 紐づけ

- **マイルストーン**: Mi3（ドキュメント・品質保証・リリース準備）
- **依存**: T04（Mi2: API統合 + テスト — 全実装・テストが完了していること）
- **後続**: T06（Issue #56 フォローアップ — 英語/中国語版README反映、Issue コメント投稿、Multilingual統合検討）
- **関連Issue**: [#56 - How can i make result similar like misaki does?](https://github.com/ayutaz/dot-net-g2p/issues/56)
- **関連ドキュメント**:
  - [docs/guides/misaki-compatible-chinese.md](../guides/misaki-compatible-chinese.md) — 設計ドキュメント
  - [docs/guides/misaki-milestones.md](../guides/misaki-milestones.md) — マイルストーン計画
