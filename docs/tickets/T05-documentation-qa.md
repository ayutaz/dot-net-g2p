---
ticket: T05
title: ドキュメント更新・品質保証
milestone: Mi3
status: 未着手
depends_on: [T04]
blocks: [T06]
---

# T05: ドキュメント更新・品質保証

## 1. タスク目的とゴール

T01-T04 で実装・テスト完了した Misaki 互換中国語 G2P 出力（`ToMisakiIpa()` / `ToMisakiIpaBatch()`）について、ドキュメント整備と品質保証を行い、利用者が Misaki 互換出力を正しく使えるようにする。

**ゴール:**

- 利用者が README.md を読むだけで `ToMisakiIpa()` の使い方と出力形式を理解できる
- Kokoro TTS (KokoroSharp) との連携に必要な情報がドキュメントに揃っている
- パフォーマンス面で `ToMisakiIpa` が `ToIPA` と同等の速度であることが確認されている
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
zhEngine.ToMisakiIpa("你好世界");  // => "ni↗xau̯↓ʂʐ̩↘ʨiɛ↘"
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
string misakiIpa = zhEngine.ToMisakiIpa("你好");
// => "ni↗xau̯↓"
// 声調記号: → (1声), ↗ (2声), ↓ (3声), ↘ (4声)
// 声母: j→ʨ, q→ʨʰ, z→ʦ, c→ʦʰ
// 韻母: ai→ai̯, ao→au̯, ei→ei̯, ou→ou̯ (非音節化符号)

// 声調なし Misaki 互換 IPA
string misakiNoTone = zhEngine.ToMisakiIpa("你好", includeTones: false);
// => "nixau̯"

// バッチ変換
string[] misakiBatch = zhEngine.ToMisakiIpaBatch(new[] { "你好", "世界" });
```

#### 2.1.4 API リファレンステーブル（L481-512付近、ChineseG2PEngine セクション）への追記

既存テーブルの `ToIpaWithProsodyBatch(texts, includeTones)` 行の後に以下の行を追加:

```
| `ToMisakiIpa(text)` | `string` | Misaki互換IPA文字列（矢印声調記号付き） |
| `ToMisakiIpa(text, includeTones)` | `string` | 声調制御付きMisaki互換IPA |
| `ToMisakiIpaBatch(texts)` | `string[]` | バッチMisaki互換IPA変換 |
| `ToMisakiIpaBatch(texts, includeTones)` | `string[]` | バッチMisaki互換IPA変換（声調制御） |
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
  - `ToMisakiIpa("你好")` のコメントに書かれた出力例が実際の出力と一致すること
  - `ToMisakiIpa("你好世界")` のコメントに書かれた出力例が実際の出力と一致すること
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

- [ ] `ToMisakiIpa` が `ToIPA` と同等の処理速度であることを確認
  - 測定方法: 同一テキストセット（100文以上）を各メソッドで1000回変換し、平均処理時間を比較
  - 許容範囲: `ToMisakiIpa` の処理時間が `ToIPA` の 1.2 倍以内
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

- [ ] README.md のコードサンプル内のコメント出力例が、実際の `ToMisakiIpa()` 出力と完全一致すること
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

---

## 7. 後続タスクへの連絡事項

T06（Issue #56 フォローアップ）に伝えるべき情報:

### 7.1 Issue #56 へのフォローアップコメント

- T05 完了後、Issue #56 に実装完了の報告コメントを投稿すること
- コメントには以下を含める:
  - `ToMisakiIpa()` の使用例コード
  - NuGet パッケージバージョン（Mi3 リリース後のバージョン番号）
  - 既知の制限事項（Legacy パスのみ対応、Multilingual 層未統合等）

### 7.2 README_EN.md / README_ZH.md への反映

- T05 では日本語 README.md のみを更新する
- T06 で英語版・中国語版への反映を行うこと
- 中国語版は Misaki の主要ユーザー層と重なるため、特に丁寧な記述が望ましい

### 7.3 Multilingual 層への統合検討

- T01-T04 の実装では `DotNetG2P.Multilingual` への統合は見送っている
- `MultilingualG2PEngine` に `ToMisakiIpa()` を追加するかは T06 で検討すること
- 追加する場合、`MultilingualG2POptions` に Misaki 出力モードの設定が必要になる可能性がある

### 7.4 パフォーマンステスト結果の引き継ぎ

- T05 で実施したパフォーマンステスト結果（ToMisakiIpa vs ToIPA の処理時間比較）を T06 に引き継ぐ
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
