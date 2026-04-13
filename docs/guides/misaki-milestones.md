# Misaki互換 中国語G2P — マイルストーン計画

> 対応Issue: [#56](https://github.com/ayutaz/dot-net-g2p/issues/56) | 設計ドキュメント: [misaki-compatible-chinese.md](misaki-compatible-chinese.md)

## 概要

DotNetG2P.Chinese に Misaki 互換出力モードを追加する。全3マイルストーン。

## チケット一覧

詳細は [../tickets/README.md](../tickets/README.md) を参照。

| マイルストーン | チケット |
|-------------|---------|
| Mi1 | [T01 マッピングテーブル](../tickets/T01-misaki-mapping-tables.md), [T02 Convert統合](../tickets/T02-misaki-convert-method.md) |
| Mi2 | [T03 API追加](../tickets/T03-engine-api-integration.md), [T04 テスト実装](../tickets/T04-misaki-tests.md) |
| Mi3 | [T05 ドキュメント・QA](../tickets/T05-documentation-qa.md), [T06 リリース準備](../tickets/T06-release-followup.md) |

---

## Mi1: PinyinToMisaki 変換クラス

**目標**: Misaki互換のマッピングテーブルを持つ変換クラスを新規作成する。

### 成果物

| ファイル | 内容 |
|---------|------|
| `src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs` | 新規作成 |

### 実装内容

- [x] `PinyinToMisaki` 内部静的クラスの作成（`PinyinToPiperIpa` と同構造）
- [x] 声母マッピングテーブル `s_initialIpa` (22エントリ)
  - `j/q` → `ʨ/ʨʰ` (DotNetG2Pの `tɕ/tɕʰ` から変更)
  - `z/c` → `ʦ/ʦʰ` (DotNetG2Pの `ts/tsʰ` から変更)
  - 他は標準IPA と同一
- [x] 韻母マッピングテーブル `s_finalIpa` (32エントリ)
  - 二重母音: `ɪ` → `i`、`ʊ` → `u` (非音節化符号 U+032F は出力に含まれない)
  - 例: `aɪ` → `ai`、`aʊ` → `au`、`eɪ` → `ei`、`oʊ` → `ou`
- [x] 声調マッピング `s_toneArrows` (5エントリ)
  - 1声 → `→`、2声 → `↗`、3声 → `↓`、4声 → `↘`、軽声 → 空
- [x] `Convert(string pinyin)` / `Convert(string pinyin, bool includeTones)` メソッド
- [x] `ConvertSyllable(PinyinSyllable syllable, bool includeTones)` 内部メソッド
- [x] そり舌母音 (zh/ch/sh/r + i) の処理
- [x] 歯茎母音 (z/c/s + i) の処理
- [x] `ShouldOmitSemivowel` (y/w 半母音省略判定)

### 完了条件

- `PinyinToMisaki.Convert("mā")` → `"ma→"` が返ること
- `PinyinToMisaki.Convert("hǎo")` → `"xau↓"` が返ること
- `PinyinToMisaki.Convert("jī")` → `"ʨi→"` が返ること (声母差異)
- `PinyinToMisaki.Convert("māi")` → `"mai→"` が返ること (韻母差異)
- ビルドが通ること (`dotnet build`)

---

## Mi2: ChineseG2PEngine API統合 + テスト

**目標**: `ChineseG2PEngine` にMisaki互換の公開APIを追加し、テストで検証する。

### 成果物

| ファイル | 内容 |
|---------|------|
| `src/DotNetG2P.Chinese/ChineseG2PEngine.cs` | メソッド追加 |
| `tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiIpaTests.cs` | 新規作成 |

### 実装内容 — API

- [x] `ToMisakiIPA(string text)` — Misaki互換IPA文字列を返す
- [x] `ToMisakiIPA(string text, bool includeTones)` — 声調有無指定オーバーロード
- [x] `ToMisakiIPABatch(string[] texts)` — バッチ変換
- [x] `ToMisakiIPABatch(string[] texts, bool includeTones)` — バッチ変換 (声調有無指定)
- [x] 内部実装: `RunPipeline(text, p => PinyinToMisaki.Convert(p, includeTones))` パターン

### 実装内容 — テスト

- [x] **声調テスト**: 各声調 (1-4 + 軽声) の矢印記号変換
  - 1声: `妈` → `ma→`、2声: `麻` → `ma↗`、3声: `马` → `ma↓`、4声: `骂` → `ma↘`
- [x] **声母テスト**: Misaki固有の声母マッピング
  - `j/q` → `ʨ/ʨʰ`、`z/c` → `ʦ/ʦʰ`
- [x] **韻母テスト**: 二重母音の非音節化符号
  - `ai/ei/ao/ou` → `ai/ei/au/ou`
- [x] **声調変調テスト**: ToneSandhi結果がMisaki出力にも反映
  - 三声連読: `你好` → 3+3 → 2+3 → `ni↗xau↓`
  - 「一」変調: `一个` → `i↘kɤ↘` (一 + 4声 → 2声に変調)
- [x] **エッジケーステスト**
  - 空文字列 → 空文字列
  - 句読点のみ → 句読点そのまま
  - 非漢字テキスト → パススルー
  - 軽声 (声調なし) → 矢印なし
  - er化音
- [x] **Issue #56 再現テスト**: `"你好"` の出力がMisaki互換であること

### 完了条件

- `dotnet test` で ChineseMisakiIpaTests 全件パス
- 既存テスト (936件) に回帰なし
- `engine.ToMisakiIPA("你好")` が Misaki と同等の出力を返すこと

---

## Mi3: ドキュメント・品質保証・リリース準備

**目標**: ドキュメント整備、エッジケース追加テスト、リリース準備を行う。

### 成果物

| ファイル | 内容 |
|---------|------|
| `README.md` | Misaki互換出力の使用例を追加 |
| `CLAUDE.md` | 進捗状況テーブル更新 |
| `docs/guides/misaki-compatible-chinese.md` | 設計ドキュメント最終更新 |
| `tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiEdgeCaseTests.cs` | 追加エッジケーステスト (任意) |

### 実装内容

- [x] **README.md 更新**
  - 中国語セクションに Misaki 互換出力の使用例を追加
  - `ToMisakiIPA()` の API 説明
  - Kokoro TTS との連携例
- [x] **CLAUDE.md 更新**
  - 中国語パッケージの備考に「Misaki互換出力対応」を追記
- [x] **品質保証**
  - Misaki の Python 実装との出力比較テスト (可能な範囲で)
  - パフォーマンステスト: `ToMisakiIPA` が `ToIPA` と同等の速度であること
- [x] **Issue #56 へのフォローアップコメント**
  - 実装完了の報告
  - 使用例コード提示
- [x] **設計ドキュメント最終更新**
  - マイルストーン完了状況を反映
  - 備考・知見を追記

### 完了条件

- README に Misaki 互換出力の使用例があること
- `dotnet test` 全テストパス
- Issue #56 にフォローアップコメント投稿済み

---

## マイルストーン進捗サマリ

| マイルストーン | 内容 | 状態 |
|--------------|------|------|
| **Mi1** | PinyinToMisaki 変換クラス | 完了 |
| **Mi2** | API統合 + テスト | 完了 |
| **Mi3** | ドキュメント・品質保証・リリース準備 | 完了 |
