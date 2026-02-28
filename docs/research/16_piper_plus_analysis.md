# piper-plus リポジトリ調査結果

## 調査日: 2026-02-28

piper-plus（C:\Users\yuta\Desktop\Private\piper-plus）および関連リポジトリ（uPiper）を調査し、
dot-net-g2pプロジェクトで再利用・参考にできるコードとできない部分を整理した。

---

## 1. プロジェクト概要

**piper-plus** (v1.5.4) は、rhasspy/piper をフォークした高品質ニューラルTTS。
VITSアーキテクチャを採用し、**日本語対応の強化**を中心に多数の機能を追加している。

- **言語**: Python / C++ / JavaScript(WASM)
- **C#コードは存在しない**（.csproj, .asmdef なし）
- **ライセンス**: MIT
- **日本語処理**: OpenJTalk統合（外部バイナリ呼び出し or pyopenjtalk経由）

### 関連リポジトリ: uPiper

- **配置**: C:\Users\yuta\Desktop\Private\uPiper
- **言語**: C#（Unityプロジェクト）
- **OpenJTalkとの連携**: P/Invoke（ネイティブライブラリ呼び出し）
- NJD処理のC#再実装は**含まれない**（ネイティブに委譲）

---

## 2. 重要な発見: NJD処理のC#移植は存在しない

piper-plusおよびuPiperを徹底調査した結果、**NJD処理（SetPronunciation, SetDigit, SetAccentPhrase, SetAccentType, SetUnvoicedVowel）をC#でネイティブに再実装したコードは存在しない**。

| リポジトリ | NJD処理の実装方式 |
|-----------|------------------|
| piper-plus (Python) | pyopenjtalkに委譲（`extract_fullcontext()`呼び出し） |
| piper-plus (C++) | OpenJTalkバイナリを外部プロセスとして実行 |
| piper-plus (WASM) | OpenJTalkをEmscriptenでコンパイルしたWASMモジュール呼び出し |
| uPiper (C#/Unity) | P/InvokeでネイティブOpenJTalkを呼び出し |

→ **dot-net-g2pでは、NJD処理を新規にC#で実装する必要がある**（jpreprocess/OpenJTalkを参考に）

---

## 3. dot-net-g2pで再利用・参考にできるコード

### 3.1 フルコンテキストラベルからの音素・韻律抽出ロジック

**Python版**: `src/python/piper_train/phonemize/japanese.py` (382行)
**JS版**: `src/wasm/openjtalk-web/src/japanese_phoneme_extract.js` (155行)
**C#版**: `uPiper/Assets/uPiper/Runtime/Core/Phonemizers/Implementations/OpenJTalkPhonemizer.cs` (935行)

3言語で同一ロジックが実装されている。C#への変換は容易。

#### 韻律記号の挿入ルール（栗原メソッド）

```
A1: アクセント核からの相対位置（0で下降点）
A2: アクセント句内のモーラ位置（1-based）
A3: アクセント句内の総モーラ数

^  : 文頭（最初のsil）
$  : 文末（最後のsil、平叙文）
?  : 文末（疑問文）
_  : ポーズ（pau）
#  : アクセント句境界（a2 == a3 かつ a2_next == 1）
[  : 上昇マーク（a2 == 1 かつ a2_next == 2）
]  : アクセント核マーク（a1 == 0 かつ a2_next == a2 + 1）
```

### 3.2 音素マッピングテーブル（uPiperから流用可能）

**ファイル**: `uPiper/Assets/uPiper/Runtime/Core/Phonemizers/OpenJTalkToPiperMapping.cs` (338行)

- OpenJTalk音素→Piper音素のマッピング定義
- PUA（Private Use Area）マッピング（多文字音素→単一Unicode）
- 音素ID辞書

### 3.3 N音素バリアント処理（uPiperから流用可能）

**ファイル**: `uPiper/Assets/uPiper/Runtime/Core/Phonemizers/Implementations/OpenJTalkPhonemizer.cs`

コンテキスト依存の「ん」バリアント分類:
- `N_m` : 両唇音（m, b, p）の前
- `N_n` : 歯茎音（n, t, d, s, z）の前
- `N_ng` : 軟口蓋音（k, g）の前
- `N_uvular` : 語末 / 母音の前

### 3.4 カスタム辞書システム

**ファイル**: `src/python/piper_train/phonemize/custom_dict.py`

JSON形式の辞書（英単語→カタカナ読み）:
```json
{
  "version": "2.0",
  "entries": {
    "Docker": {"pronunciation": "ドッカー", "priority": 9}
  }
}
```

dot-net-g2pでもユーザー辞書機能の参考になる。

### 3.5 疑問文タイプ判定

```
?  → generic（一般疑問）
?! → emphatic（強調疑問）→ PUA U+E016
?. → neutral（平叙疑問）→ PUA U+E017
?~ → tag（確認疑問）→ PUA U+E018
```

---

## 4. 音素体系

### piper-plusの日本語音素（60シンボル）

| 種別 | 音素 |
|------|------|
| 特殊トークン (10) | `_`, `^`, `$`, `?`, `?!`, `?.`, `?~`, `#`, `[`, `]` |
| 有声母音 (5) | a, i, u, e, o |
| 無声母音 (5) | A, I, U, E, O |
| 長母音 (5) | a:, i:, u:, e:, o: |
| 撥音 (5) | N, N_m, N_n, N_ng, N_uvular |
| 促音 (2) | cl, q |
| 子音 (28) | k, ky, kw, g, gy, gw, t, ty, d, dy, p, py, b, by, ch, ts, s, sh, z, j, zy, f, h, hy, v, n, ny, m, my, r, ry, w, y |

**dot-net-g2pとの差異**:
- piper-plusはTTS向けに拡張（N音素バリアント4種、長母音5種、疑問タイプ3種）
- dot-net-g2pのPhoneme定義はOpenJTalk標準（48音素）をベースにするが、piper-plusの拡張も出力オプションとして検討可能

### PUAマッピング（多文字音素→Unicode Private Use Area）

```
a:→U+E000  i:→U+E001  u:→U+E002  e:→U+E003  o:→U+E004
cl→U+E005  ky→U+E006  kw→U+E007  gy→U+E008  gw→U+E009
ty→U+E00A  dy→U+E00B  py→U+E00C  by→U+E00D  ch→U+E00E
ts→U+E00F  sh→U+E010  zy→U+E011  hy→U+E012  ny→U+E013
my→U+E014  ry→U+E015  ?!→U+E016  ?.→U+E017  ?~→U+E018
N_m→U+E019 N_n→U+E01A N_ng→U+E01B N_uvular→U+E01C
```

---

## 5. 辞書関連

### OpenJTalk MeCab辞書
- `open_jtalk_dic_utf_8-1.11`（naist-jdic UTF-8版）
- SHA256: `fe6ba0e43542cef98339abdffd903e062008ea170b04e7e2a35da805902f382a`
- 自動ダウンロード機構あり（`openjtalk_dictionary_manager.c`）
- 辞書パス探索順: 環境変数 → システムパス → データディレクトリ

### カスタム辞書
- `data/dictionaries/default_common_dict.json` - 一般用語（約440エントリ）
- `data/dictionaries/default_tech_dict.json` - IT用語
- `data/dictionaries/additional_tech_dict.json` - 最新トレンド用語

---

## 6. テスト

### テストケースの網羅性（`src/python/tests/test_phonemize.py`）

| カテゴリ | サブケース数 | 内容 |
|---------|------------|------|
| 基本音素化 | 2 | ひらがな・カタカナ基本変換 |
| カタカナ→音素 | 16 | ア～ワ・ンの各音素 |
| 長音処理 | 6 | カー/キー/クー/ケー/コー/ソフトウェアー |
| 促音処理 | 3 | がっこう/ハッピー/ロック |
| 拗音処理 | 12 | きゃ/きゅ/きょ/しゃ/ちゃ/にゃ等 |
| 疑問文タイプ | 5 | ?/?!/?./日本語形式 |
| N音素バリアント | 7 | N_m/N_n/N_ng/N_uvular |
| エラー処理 | 1 | None/空文字/長大テキスト |

→ dot-net-g2pの統合テストのテストケースとして流用可能

---

## 7. uPiperの処理パイプライン（C#）

```
テキスト入力
  → カスタム辞書適用
  → P/Invoke: openjtalk_phonemize(ネイティブC/C++)
    → [ネイティブ側でMeCab解析 + NJD処理 + JPCommon処理]
  → NativePhonemeResult取得
  → OpenJTalkToPiperMapping.ConvertToPiperPhonemes（音素マッピング）
  → ApplyNPhonemeRules（N音素バリアント分類）
  → GetQuestionType（疑問マーカー分類）
  → PhonemeResult
```

**dot-net-g2pとの違い**:
- uPiperはP/InvokeでネイティブOpenJTalkに全て委譲
- dot-net-g2pはMeCab解析→NJD処理→JPCommonを**全てC#で実装**する

---

## 8. まとめ: dot-net-g2pプロジェクトへの影響

### 流用可能なもの

| 対象 | ソース | 用途 |
|------|--------|------|
| 音素マッピングテーブル | uPiper `OpenJTalkToPiperMapping.cs` | Phoneme定義、PUAマッピング |
| N音素バリアント処理 | uPiper `OpenJTalkPhonemizer.cs` | SetUnvoicedVowel後の後処理 |
| 韻律記号挿入ロジック | piper-plus `japanese.py` | ProsodyExtractor実装 |
| テストケース | piper-plus `test_phonemize.py` | 統合テストの期待値 |
| カスタム辞書フォーマット | piper-plus `custom_dict.py` | ユーザー辞書機能の設計参考 |
| 辞書ダウンロード・管理 | piper-plus `openjtalk_dictionary_manager.c` | 辞書管理戦略の参考 |

### 新規C#実装が必要なもの（変更なし）

| 対象 | 参考実装 |
|------|---------|
| NJD処理6段階 | jpreprocess (Rust) / OpenJTalk (C) |
| JPCommon（フルコンテキストラベル生成） | jpreprocess / OpenJTalk |
| 形態素解析（MeCab互換） | NMeCab (C#) → 将来自前実装 |
| naist-jdic辞書ローダー | NMeCab / MeCab辞書仕様 |
