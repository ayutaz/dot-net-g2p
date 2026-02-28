# OpenJTalk 内部アーキテクチャと処理パイプライン

## 1. 概要

OpenJTalkは名古屋工業大学で開発された日本語音声合成システムであり、テキストから音声波形を生成するまでの完全なパイプラインを提供する。本ドキュメントでは、G2P（書記素→音素変換）に関連するテキスト処理フロントエンド部分を中心に、C#での再実装に必要な知見をまとめる。

## 2. 全体アーキテクチャ

OpenJTalkは以下の4つの主要コンポーネントで構成される:

```
日本語テキスト
    │
    ▼
┌──────────────────┐
│ 1. text2mecab     │  テキスト → MeCab入力形式
└──────────────────┘
    │
    ▼
┌──────────────────┐
│ 2. MeCab          │  形態素解析（辞書: naist-jdic）
└──────────────────┘
    │
    ▼
┌──────────────────┐
│ 3. mecab2njd      │  MeCab出力 → NJDノードリスト
└──────────────────┘
    │
    ▼
┌──────────────────────────────────────┐
│ 4. NJD処理（6段階の言語ルール処理）     │
│   4-1. njd_set_pronunciation         │
│   4-2. njd_set_digit                 │
│   4-3. njd_set_accent_phrase         │
│   4-4. njd_set_accent_type           │
│   4-5. njd_set_unvoiced_vowel        │
│   4-6. njd_set_long_vowel            │
└──────────────────────────────────────┘
    │
    ▼
┌──────────────────┐
│ 5. njd2jpcommon   │  NJD → JPCommon中間表現
└──────────────────┘
    │
    ▼
┌──────────────────┐
│ 6. JPCommon       │  full-context label生成
│  make_label       │
└──────────────────┘
    │
    ▼
HTS-style full-context labels
    │
    ▼
┌──────────────────┐
│ 7. HTS Engine     │  音声波形合成（本プロジェクトのスコープ外）
└──────────────────┘
```

### pyopenjtalkでの処理フロー（Pythonラッパー）

```python
# run_frontend の処理順序
text2mecab(buff, text)                    # テキスト前処理
Mecab_analysis(self.mecab, buff)          # 形態素解析
mecab2njd(self.njd, Mecab_get_feature(self.mecab), ...)  # NJD変換

# NJD 6段階処理
njd_set_pronunciation(self.njd)
njd_set_digit(self.njd)
njd_set_accent_phrase(self.njd)
njd_set_accent_type(self.njd)
njd_set_unvoiced_vowel(self.njd)
njd_set_long_vowel(self.njd)

# full-context label生成
njd2jpcommon(self.jpcommon, self.njd)
JPCommon_make_label(self.jpcommon)
```

## 3. ソースコード構造

OpenJTalkのソースコードはモジュール単位でディレクトリに分割されている:

```
open_jtalk/src/
├── text2mecab/           # テキスト→MeCab入力変換
├── mecab/                # MeCab形態素解析エンジン（改変版）
├── mecab2njd/            # MeCab出力→NJDノード変換
├── njd/                  # NJDデータ構造・基本操作
├── njd_set_pronunciation/  # 発音生成
├── njd_set_digit/          # 数字読み変換
├── njd_set_accent_phrase/  # アクセント句結合
├── njd_set_accent_type/    # アクセント型結合
├── njd_set_unvoiced_vowel/ # 無声音化
├── njd_set_long_vowel/     # 長音化
├── njd2jpcommon/           # NJD→JPCommon変換
├── jpcommon/               # JPCommon・full-context label
└── hts_engine_API/         # HTS音声合成エンジン
```

各NJD処理モジュールは以下のファイルで構成される:
- `njd_set_*.h` : ヘッダファイル（関数宣言）
- `njd_set_*.c` : 実装ファイル
- `njd_set_*_rule_utf_8.h` : UTF-8エンコードのルールテーブル定義

## 4. NJDNodeデータ構造

NJDNode は OpenJTalk のテキスト処理パイプライン全体で使用される核心的なデータ構造である。

### 4.1 C言語での構造体定義

```c
typedef struct _NJDNode {
    char *string;       // 表層形（表記文字列）
    char *pos;          // 品詞
    char *pos_group1;   // 品詞細分類1
    char *pos_group2;   // 品詞細分類2
    char *pos_group3;   // 品詞細分類3
    char *ctype;        // 活用型
    char *cform;        // 活用形
    char *orig;         // 原形（辞書の基本形）
    char *read;         // 読み（カタカナ）
    char *pron;         // 発音（カタカナ）
    int   acc;          // アクセント核位置
    int   mora_size;    // モーラ数
    char *chain_rule;   // アクセント結合規則タイプ（C1〜C5等）
    int   chain_flag;   // アクセント句結合フラグ（-1: 未設定, 0: 非結合, 1: 結合）
    struct _NJDNode *prev;  // 前のノードへのポインタ
    struct _NJDNode *next;  // 次のノードへのポインタ
} NJDNode;
```

### 4.2 NJD構造体

```c
typedef struct _NJD {
    NJDNode *head;  // 先頭ノード
    NJDNode *tail;  // 末尾ノード
} NJD;
```

NJDは双方向連結リストとして実装されている。

### 4.3 C#での再実装案

```csharp
public class NjdNode
{
    public string Surface { get; set; }      // string（表層形）
    public string Pos { get; set; }          // pos（品詞）
    public string PosGroup1 { get; set; }    // pos_group1
    public string PosGroup2 { get; set; }    // pos_group2
    public string PosGroup3 { get; set; }    // pos_group3
    public string CType { get; set; }        // ctype（活用型）
    public string CForm { get; set; }        // cform（活用形）
    public string Orig { get; set; }         // orig（原形）
    public string Read { get; set; }         // read（読み）
    public string Pron { get; set; }         // pron（発音）
    public int AccentPosition { get; set; }  // acc
    public int MoraSize { get; set; }        // mora_size
    public string ChainRule { get; set; }    // chain_rule
    public int ChainFlag { get; set; }       // chain_flag (-1, 0, 1)
}

// C#では LinkedList<NjdNode> または List<NjdNode> で管理
```

### 4.4 MeCab出力からNJDNodeへのフィールドマッピング

`NJDNode_load()` 関数がMeCabのfeature文字列をパースしてNJDNodeの各フィールドに格納する。

naist-jdic辞書のCSVフォーマット:
```
表層形,左文脈ID,右文脈ID,コスト,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音,アクセント核位置/モーラ数,アクセント結合タイプ
```

MeCab出力のfeature文字列（カンマ区切り、インデックス0始まり）:

| インデックス | NJDNodeフィールド | 説明 |
|:---:|---|---|
| 0 | string | 表層形 |
| 1 | pos | 品詞 |
| 2 | pos_group1 | 品詞細分類1 |
| 3 | pos_group2 | 品詞細分類2 |
| 4 | pos_group3 | 品詞細分類3 |
| 5 | ctype | 活用型 |
| 6 | cform | 活用形 |
| 7 | orig | 原形 |
| 8 | read | 読み |
| 9 | pron | 発音 |
| 10 | acc / mora_size | `アクセント核位置/モーラ数` 形式（例: `3/4`） |
| 11 | chain_rule | アクセント結合タイプ（例: `C2`） |

注意: フィールド10は `"アクセント核位置/モーラ数"` の形式でスラッシュ区切りになっている。パース時に分割が必要。

## 5. NJD処理モジュールの詳細

### 5.1 njd_set_pronunciation（発音生成）

**目的**: 辞書に発音が登録されていない形態素や、アルファベット文字列に対して発音を生成する。

**処理内容**:
- 辞書の `read`（読み）や `pron`（発音）フィールドが `*`（未設定）の場合に発音を推定
- アルファベット文字列の読み生成（例: `TIF` → `ティーアイエフ`）
- 記号・句読点の処理
- 未知語（辞書にない単語）へのデフォルト発音設定

**ルールテーブル**: `njd_set_pronunciation_rule_utf_8.h` にエンコーディング別のルールが定義されている。

**C#実装ポイント**:
- カタカナ→発音変換テーブル（Dictionary<string, string>）
- アルファベット→カタカナ読みテーブル
- 記号の読み処理テーブル

### 5.2 njd_set_digit（数字読み変換）

**目的**: 連続する数字トークンを適切な日本語の位取り読みに変換する。

**処理例**:
```
入力: "3" "9" "3" "9"（MeCabが数字を1桁ずつ分割した場合）
→ さんきゅうさんきゅう（単純読み）
→ さんぜんきゅうひゃくさんじゅうきゅう（位取り読みへ変換）
```

**処理内容**:
1. 連続する数字ノードを検出
2. 位取り（一、十、百、千、万、億、兆…）に基づく読み割り当て
3. 助数詞との連結処理（例: 「3個」→「さんこ」）
4. 数字の音変化処理（例: 「三百」→「さんびゃく」、「八百」→「はっぴゃく」）

**数字の音変化規則（主要なもの）**:

| 数字 | 百 | 千 | 万 |
|------|-----|-----|-----|
| 1 | いっぴゃく | いっせん | いちまん |
| 3 | さんびゃく | さんぜん | さんまん |
| 6 | ろっぴゃく | ろくせん | ろくまん |
| 8 | はっぴゃく | はっせん | はちまん |

**C#実装ポイント**:
- 数字ノードの連続検出ロジック
- 位取りテーブル（Dictionary）
- 音変化ルールテーブル
- 助数詞との音便変化テーブル

### 5.3 njd_set_accent_phrase（アクセント句結合）

**目的**: 個々の形態素を韻律的なまとまり（アクセント句）にグループ化する。

**処理内容**: `chain_flag` を設定して、隣接するノードがアクセント句として結合するかどうかを判定する。

**18のルール**:

| ルール番号 | 条件 | 結果 |
|:---:|---|---|
| 01 | デフォルト | 独立アクセント句 |
| 02 | 名詞の連続 | 結合（同一アクセント句） |
| 03 | 形容詞 + 名詞 | 分離（別アクセント句） |
| 04 | 名詞・形容動詞語幹 + 名詞 | 分離 |
| 05 | 動詞 + 形容詞/名詞 | 分離 |
| 06 | 副詞/接続詞/連体詞 | 独立アクセント句 |
| 07 | 名詞 + 副詞可能修飾語 | 独立 |
| 08 | 助動詞/助詞 | 前の要素に結合 |
| 09 | 助動詞後の助詞 | 分離 |
| 10 | 接尾辞後の名詞 | 分離 |
| 11 | 非自立形容詞（動詞形/形容詞形/助詞への接続） | 前の要素に結合 |
| 12 | 非自立動詞（動詞形/名詞サ変への接続） | 前の要素に結合 |
| 13 | 名詞 + 動詞/形容詞/名詞動詞複合語 | 分離 |
| 14 | 句読点・記号 | 独立アクセント句 |
| 15 | 接頭詞 | 独立アクセント句 |
| 16 | 姓マーカー後の名詞 | 分離 |
| 17 | 名マーカー後の名詞 | 分離 |
| 18 | 接尾語 | 前の要素に結合 |

**C#実装ポイント**:
- 品詞情報に基づくルール判定（if-else/switch文の連鎖）
- `chain_flag` の設定: `1` = 結合, `0` = 非結合
- 品詞文字列の定数定義

### 5.4 njd_set_accent_type（アクセント型結合）

**目的**: アクセント句結合時のアクセント核位置（アクセント型）を決定する。

**理論的基盤**: 甲坂・佐藤（1983）の「日本語単語連鎖のアクセント規則」に基づく。

**アクセント結合タイプ（辞書フィールド `chain_rule`）**:

| タイプ | 説明 |
|---|---|
| C1 | 後続語が結合時、先行語のアクセント核を保持 |
| C2 | 後続語が2モーラ以上かつアクセント核なし/最終音節核の場合、先行語のアクセント核が消え後続要素先頭にアクセント核が移動（※実装上は無条件に後続要素先頭に核を配置） |
| C3 | 先行語のアクセント型を保持し、後続語のアクセント核を無視 |
| C4 | 結合後、全体のアクセント核を最後から2番目のモーラに配置 |
| C5 | 結合後、アクセント核なし（平板型）となる |

**C#実装ポイント**:
- `chain_flag == 1`（結合フラグ有効）の場合のみ処理
- `chain_rule` 文字列のパース（`C1`、`C2`等）
- アクセント核位置の再計算ロジック
- モーラ数の合算

### 5.5 njd_set_unvoiced_vowel（無声音化）

**目的**: 日本語における母音の無声化を推定し、発音表記に反映する。

**無声化の基本条件**: 高母音 `i`、`u` が無声子音（k, ky, s, sh, t, ty, ch, ts, h, f, hy, p, py）に挟まれている場合に無声化する。

**6つのルール**:

| ルール | 条件 | 処理 |
|:---:|---|---|
| Rule 0 | フィラー（「えーと」等） | 無声化対象外（除外） |
| Rule 1 | 「です」「ます」の末尾 | 「ス」の母音 `u` を無声化 |
| Rule 2 | 動詞・助動詞・助詞 | 「シ」が無声化しやすい |
| Rule 3 | 連続する無声化可能モーラ | 連続無声化を防止（1つおきに無声化） |
| Rule 4 | アクセント核位置 | アクセント核上のモーラは無声化しない |
| Rule 5 | 無声子音環境 | `i`、`u` が無声子音間で無声化（特定の子音遷移を除外） |

**Rule 5の無声子音リスト**: k, ky, s, sh, t, ty, ch, ts, h, f, hy, p, py

**Rule 5の除外遷移パターン（前→後）**: s→s, s→sh, f→f, f→h, f→hy, h→f, h→h, h→hy

**無声化の表記**: カタカナ読みの無声化対象モーラに特殊マーカーを付与（OpenJTalkでは大文字の母音で表記する場合がある）。

**C#実装ポイント**:
- モーラ単位での前後コンテキスト判定
- 無声子音のHashSet
- 除外パターンのチェック
- 連続無声化防止のための状態管理

### 5.6 njd_set_long_vowel（長音化）

**目的**: 特定の母音連続パターンを長音に変換する。

**主な変換規則**:

| 入力パターン | 出力 | 例 |
|---|---|---|
| エイ | エー | 先生（せんせい→せんせー） |
| オウ | オー | 東京（とうきょう→とーきょー） |

**処理の詳細**:
- 辞書上の読み「セイ」→ 発音「セー」のように `pron` フィールドを更新
- 語種（和語/漢語/外来語）による適用判定
- 全ての「エイ」「オウ」が長音化するわけではない（語彙的に判断）

**C#実装ポイント**:
- カタカナ発音文字列の走査
- 母音連続パターンの検出・変換
- 変換除外語の辞書管理

## 6. JPCommonとfull-context label

### 6.1 JPCommonの役割

JPCommonは、NJD処理済みのノードリストをHTS音声合成エンジン用のfull-context labelに変換する中間層である。NJDからJPCommonへの変換（`njd2jpcommon`）と、JPCommonからラベル文字列への変換（`JPCommon_make_label`）の2段階で処理される。

### 6.2 full-context labelのフォーマット

HTS-style full-context labelは、各音素について周辺の言語的コンテキスト情報を豊富に付与した形式である。

**ラベル文字列の全体構造**:
```
p1^p2-p3+p4=p5/A:a1+a2+a3/B:b1-b2_b3/C:c1_c2+c3/D:d1+d2_d3/E:e1_e2!e3_e4-e5/F:f1_f2#f3_f4@f5_f6|f7_f8/G:g1_g2%g3_g4_g5/H:h1_h2/I:i1-i2@i3+i4&i5-i6|i7+i8/J:j1_j2/K:k1+k2-k3
```

### 6.3 各フィールドの詳細

#### Phoneme（音素コンテキスト）

| フィールド | 説明 |
|---|---|
| p1 | 2つ前の音素 |
| p2 | 1つ前の音素 |
| p3 | 現在の音素 |
| p4 | 1つ後の音素 |
| p5 | 2つ後の音素 |

#### A: Mora（モーラ情報）

| フィールド | 説明 |
|---|---|
| a1 | アクセント型と現在のモーラ位置の差（相対アクセント位置） |
| a2 | 現在のアクセント句内でのモーラ位置（前方から） |
| a3 | 現在のアクセント句内でのモーラ位置（後方から） |

#### B: Previous Word（前の単語）

| フィールド | 説明 |
|---|---|
| b1 | 品詞（数値コード、2桁） |
| b2 | 活用型（数値コード、1桁） |
| b3 | 活用形（数値コード、1桁） |

#### C: Current Word（現在の単語）

| フィールド | 説明 |
|---|---|
| c1 | 品詞（数値コード、2桁） |
| c2 | 活用型（数値コード、1桁） |
| c3 | 活用形（数値コード、1桁） |

#### D: Next Word（次の単語）

| フィールド | 説明 |
|---|---|
| d1 | 品詞（数値コード、2桁） |
| d2 | 活用型（数値コード、1桁） |
| d3 | 活用形（数値コード、1桁） |

#### E: Previous Accent Phrase（前のアクセント句）

| フィールド | 説明 |
|---|---|
| e1 | モーラ数 |
| e2 | アクセント核位置 |
| e3 | 疑問型かどうか（0/1） |
| e4 | 未定義（xx） |
| e5 | ポーズ挿入の有無（注: 論理が反転、1=なし、0=あり） |

#### F: Current Accent Phrase（現在のアクセント句）

| フィールド | 説明 |
|---|---|
| f1 | モーラ数 |
| f2 | アクセント核位置 |
| f3 | 疑問型かどうか（0/1） |
| f4 | 未定義（xx） |
| f5 | 現在の呼気段落内でのアクセント句位置（前方から） |
| f6 | 現在の呼気段落内でのアクセント句位置（後方から） |
| f7 | 現在の呼気段落内でのモーラ位置（前方から） |
| f8 | 現在の呼気段落内でのモーラ位置（後方から） |

#### G: Next Accent Phrase（次のアクセント句）

| フィールド | 説明 |
|---|---|
| g1 | モーラ数 |
| g2 | アクセント核位置 |
| g3 | 疑問型かどうか（0/1） |
| g4 | 未定義（xx） |
| g5 | ポーズ挿入の有無 |

#### H: Previous Breath Group（前の呼気段落）

| フィールド | 説明 |
|---|---|
| h1 | アクセント句数 |
| h2 | モーラ数 |

#### I: Current Breath Group（現在の呼気段落）

| フィールド | 説明 |
|---|---|
| i1 | アクセント句数 |
| i2 | モーラ数 |
| i3 | 呼気段落の位置（前方から） |
| i4 | 呼気段落の位置（後方から） |
| i5 | アクセント句の位置（前方から） |
| i6 | アクセント句の位置（後方から） |
| i7 | モーラの位置（前方から） |
| i8 | モーラの位置（後方から） |

#### J: Next Breath Group（次の呼気段落）

| フィールド | 説明 |
|---|---|
| j1 | アクセント句数 |
| j2 | モーラ数 |

#### K: Utterance（発話全体）

| フィールド | 説明 |
|---|---|
| k1 | 呼気段落数 |
| k2 | アクセント句数 |
| k3 | モーラ数 |

### 6.4 full-context labelの例

```
xx^xx-sil+k=o/A:xx+xx+xx/B:xx-xx_xx/C:xx_xx+xx/D:09+xx_xx/E:xx_xx!xx_xx-xx/F:5_5#0_xx@1_1|5_5/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|5+5/J:xx_xx/K:1+1-5
```

この例では:
- `sil`: 無音（発話開始/終了）
- `k`: 現在の音素
- `o`: 次の音素
- `/F:5_5#0_xx@1_1|5_5`: 現在のアクセント句は5モーラ、アクセント型5、非疑問型
- `/K:1+1-5`: 発話全体は呼気段落1個、アクセント句1個、モーラ5個

## 7. 階層的データモデル

OpenJTalkのテキスト処理で使用される言語的階層構造:

```
発話 (Utterance)
  └── 呼気段落 (Breath Group) ← ポーズで区切られるまとまり
        └── アクセント句 (Accent Phrase) ← 韻律の基本単位
              └── 単語 (Word) ← 形態素
                    └── モーラ (Mora) ← 拍の単位
                          └── 音素 (Phoneme) ← 最小音韻単位
```

## 8. C#再実装に向けた設計指針

### 8.1 G2Pに必要なスコープ

本プロジェクトのG2P機能では、full-context label生成（JPCommon）は必ずしも必要ではない。最小構成は以下の通り:

1. **必須**: MeCab互換形態素解析 → NJDノード生成 → NJD 6段階処理 → 音素列出力
2. **オプション**: full-context label生成（アクセント情報の詳細が必要な場合）

### 8.2 データ構造の選択

- NJDNode: `List<NjdNode>` で管理（C言語の連結リストの代わり）
- ルールテーブル: `Dictionary<string, string>` や定数配列で定義
- 品詞情報: enum や文字列定数で型安全に管理

### 8.3 参考実装

| プロジェクト | 言語 | 特徴 |
|---|---|---|
| OpenJTalk | C | オリジナル実装 |
| pyopenjtalk | Python/Cython | Pythonラッパー |
| jpreprocess | Rust | OpenJTalkのRust再実装。可読性重視の設計。辞書フォーマットの最適化あり |

jpreprocessはOpenJTalkの構造を直接移植せず、可読性と保守性を重視した再設計を行っている。C#実装でも同様のアプローチが有効と考えられる。

## 参考資料

- [OpenJTalk 公式サイト](https://open-jtalk.sourceforge.net/)
- [OpenJTalk の解析資料](https://www.negi.moe/negitalk/openjtalk.html)
- [pyopenjtalk (Python wrapper)](https://github.com/r9y9/pyopenjtalk)
- [r9y9/open_jtalk fork](https://github.com/r9y9/open_jtalk)
- [jpreprocess (Rust再実装)](https://github.com/jpreprocess/jpreprocess)
- [jlabel (HTS full-context label toolkit)](https://github.com/jpreprocess/jlabel)
- [njd_set_accent_phrase ルール定義 (Debian Sources)](https://sources.debian.org/src/open-jtalk/1.11-1.1/njd_set_accent_phrase/njd_set_accent_phrase_rule_ascii_for_utf_8.h/)
- 甲坂勝・佐藤大和 (1983): 「日本語単語連鎖のアクセント規則」
