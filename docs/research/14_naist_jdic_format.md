# naist-jdic 辞書フォーマットと MeCab バイナリ辞書構造

## 1. naist-jdic の概要

### 1.1 辞書の背景

NAIST-jdic（奈良先端科学技術大学院大学日本語辞書）は、IPAdic の後継として開発された MeCab 用日本語辞書である。IPAdic の固有名詞以外の全エントリをチェックし、品詞の整理・表記ゆれ情報の付与・複合語構造の付与を行っている。

OpenJTalk では、この naist-jdic を拡張して音声合成用のアクセント情報を付加した `mecab-naist-jdic` を使用する。

### 1.2 ライセンス

- **ライセンス**: BSD-3-Clause（広告条項なし）
- IPAdic のライセンスで問題となっていた ICOT 条項を削除
- 著作権者:
  - Nara Institute of Science and Technology (2009)
  - The UniDic Consortium (2011-2017)
  - Nagoya Institute of Technology Department of Computer Science (2008-2016)

### 1.3 辞書サイズ

| ファイル | サイズ |
|---------|--------|
| sys.dic | 約78,490 KB (約76.7 MB) |
| matrix.bin | 約3,704 KB (約3.6 MB) |
| char.bin | 約257 KB |
| unk.dic | 約6 KB |
| **合計** | **約82,457 KB (約80.5 MB)** |

- naist-jdic.csv のエントリ数: 約39万語（推定）
- バージョン: 0.6.3b-20111013（最新リリース版）

---

## 2. naist-jdic の CSV フォーマット

### 2.1 MeCab 標準 IPADIC フォーマット（13フィールド）

MeCab の標準辞書 CSV フォーマットは以下の13フィールドで構成される:

| # | フィールド名 | 説明 | 例 |
|---|------------|------|-----|
| 0 | 表層形 (surface) | 単語の表記形 | `歌う` |
| 1 | 左文脈ID (lcAttr) | 左から見た内部状態ID | `817` |
| 2 | 右文脈ID (rcAttr) | 右から見た内部状態ID | `817` |
| 3 | コスト (cost) | 生起コスト（小さいほど出現しやすい） | `7077` |
| 4 | 品詞 (pos) | 品詞大分類 | `動詞` |
| 5 | 品詞細分類1 (pos_group1) | 品詞中分類 | `自立` |
| 6 | 品詞細分類2 (pos_group2) | 品詞小分類 | `*` |
| 7 | 品詞細分類3 (pos_group3) | 品詞細分類 | `*` |
| 8 | 活用型 (ctype) | 活用の種類 | `五段・ワ行促音便` |
| 9 | 活用形 (cform) | 活用の形 | `基本形` |
| 10 | 原形 (orig) | 辞書見出し形 | `歌う` |
| 11 | 読み (read) | カタカナ読み | `ウタウ` |
| 12 | 発音 (pron) | 実際の発音 | `ウタウ` |

- フィールド0〜3はMeCabシステム用（辞書コンパイル時に処理）
- フィールド4〜12は「素性（feature）」としてMeCabから返される

### 2.2 OpenJTalk 拡張フィールド（+2フィールド = 全15フィールド）

OpenJTalk 用の naist-jdic は、標準の13フィールドに以下の2フィールドを追加する:

| # | フィールド名 | 説明 | 例 |
|---|------------|------|-----|
| 13 | アクセント型/モーラ数 | `アクセント核位置/モーラ数` 形式 | `0/3` |
| 14 | アクセント結合型 | 後続語との結合タイプ | `C2` |

### 2.3 サンプルエントリ

```csv
歌う,817,817,7077,動詞,自立,*,*,五段・ワ行促音便,基本形,歌う,ウタウ,ウタウ,0/3,C2
鄧艾,1349,1349,516,名詞,固有名詞,人名,一般,*,*,鄧艾,トウガイ,トーガイ,1/4,*
鍾会,1349,1349,516,名詞,固有名詞,人名,一般,*,*,鍾会,ショウカイ,ショーカイ,1/5,*
```

### 2.4 アクセント型フィールド（フィールド13）の詳細

形式: `アクセント核位置/モーラ数`

- **アクセント核位置**: 0 = 平板型（アクセント核なし）、1〜N = 第Nモーラにアクセント核
- **モーラ数**: 単語のモーラ数

例:
- `0/3` → 3モーラの平板型（例: ウタウ）
- `1/4` → 4モーラで第1モーラにアクセント核（例: トウガイ → ト↓ウガイ）
- `1/5` → 5モーラで第1モーラにアクセント核

### 2.5 アクセント結合型フィールド（フィールド14）の詳細

後続語とのアクセント結合パターンを定義する:

| 値 | 名称 | 説明 | エントリ数(概算) |
|---|------|------|----------------|
| C1 | 自立語結合保存型 | 後続語が2モーラ以上で最終拍以外にアクセント核がある場合、後続語のアクセント型を保存 | 約154,130 |
| C2 | 自立語結合生起型 | 後続語が2モーラ以上でアクセント核がない場合、前部要素のアクセント核が消え、後部要素の第1モーラにアクセント核が生起 | 約100,359 |
| C3 | 接辞結合標準型 | 接辞や2モーラ以下の名詞が接続する場合、前部要素の末尾モーラにアクセント核が生起 | 約11,846 |
| C4 | 接辞結合平板化型 | 接辞や2モーラ以下の名詞が接続する場合、複合語全体が平板化 | 約1,063 |
| C5 | 従属型 | 後続語のモーラ数やアクセント型に関わらず、前部要素のアクセント型を保存 | 少数（敬称等） |
| P1 | 接頭型1 | 接頭辞の結合パターン | 約35 |
| P2 | 接頭型2 | 接頭辞の結合パターン | 約219 |
| F | 動詞用 | 動詞のアクセント結合 | 動詞エントリ |
| * | 該当なし | アクセント結合規則なし | - |
| -1 | 未定義 | アクセント結合タイプが未定義 | - |

---

## 3. MeCab バイナリ辞書フォーマット

### 3.1 辞書ファイル一覧

| ファイル名 | 定数名 | 説明 |
|-----------|--------|------|
| sys.dic | SYS_DIC_FILE | システム辞書（ダブル配列Trie + トークン + 素性） |
| unk.dic | UNK_DIC_FILE | 未知語辞書 |
| matrix.bin | MATRIX_FILE | 連接コスト表 |
| char.bin | CHAR_PROPERTY_FILE | 文字カテゴリ定義 |
| dicrc | DICRC | 辞書設定ファイル（テキスト） |

### 3.2 辞書タイプ定数

```
MECAB_SYS_DIC = 0  // システム辞書
MECAB_USR_DIC = 1  // ユーザー辞書
MECAB_UNK_DIC = 2  // 未知語辞書
```

### 3.3 sys.dic バイナリフォーマット

#### ヘッダ構造（72バイト）

```
オフセット  サイズ  型              フィールド名        説明
────────────────────────────────────────────────────────────────
0x00        4      unsigned int    magic              マジックナンバー (XOR検証)
0x04        4      unsigned int    version            辞書バージョン (= 0x66 = 102)
0x08        4      unsigned int    type               辞書タイプ (0=sys, 1=usr, 2=unk)
0x0C        4      unsigned int    lexsize            語彙サイズ（単語数）
0x10        4      unsigned int    lsize              左文脈サイズ
0x14        4      unsigned int    rsize              右文脈サイズ
0x18        4      unsigned int    dsize              ダブル配列サイズ（バイト数）
0x1C        4      unsigned int    tsize              トークンバッファサイズ（バイト数）
0x20        4      unsigned int    fsize              フィーチャバッファサイズ（バイト数）
0x24        4      unsigned int    dummy              予約（未使用）
0x28       32      char[32]        charset            文字エンコーディング ("UTF-8"等、NULL終端)
────────────────────────────────────────────────────────────────
合計: 72バイト (= 10 × sizeof(unsigned int) + 32)
```

#### 重要な定数

```
DictionaryMagicID = 0xef718f77
DIC_VERSION       = 0x66 (= 102)
```

#### マジックナンバーの検証

```
stored_magic = magic ^ DictionaryMagicID
// stored_magic はファイルサイズと等しいはず
// 検証: (magic ^ DictionaryMagicID) == file_size
```

#### データセクション配置

```
オフセット                     内容
─────────────────────────────────────────────────
0x00 - 0x47                   ヘッダ (72バイト)
0x48                          ダブル配列データ開始
0x48 + dsize                  トークン配列開始
0x48 + dsize + tsize          フィーチャ文字列バッファ開始
0x48 + dsize + tsize + fsize  ファイル末尾
```

### 3.4 Token 構造体

各トークンは固定サイズ16バイトの構造体:

```
オフセット  サイズ  型               フィールド名    説明
─────────────────────────────────────────────────────────────
0x00        2      unsigned short  lcAttr         左文脈ID (品詞左ID)
0x02        2      unsigned short  rcAttr         右文脈ID (品詞右ID)
0x04        2      unsigned short  posid          品詞ID
0x06        2      short           wcost          単語生起コスト
0x08        4      unsigned int    feature        フィーチャ文字列へのオフセット
0x0C        4      unsigned int    compound       複合語情報
─────────────────────────────────────────────────────────────
合計: 16バイト
```

- `feature` フィールドはフィーチャバッファ先頭からの文字列オフセット
- フィーチャ文字列にはCSVのフィールド4〜14（品詞以降）がカンマ区切りで格納される

### 3.5 mecab_dictionary_info_t 構造体

```c
struct mecab_dictionary_info_t {
    const char *filename;   // 辞書ファイル名
    const char *charset;    // 文字セット
    unsigned int size;      // 登録単語数
    int type;               // 辞書タイプ (0=sys, 1=usr, 2=unk)
    unsigned int lsize;     // 左属性サイズ
    unsigned int rsize;     // 右属性サイズ
    unsigned short version; // 辞書バージョン
    struct mecab_dictionary_info_t *next; // 次の辞書へのポインタ
};
```

---

## 4. matrix.bin（連接コスト表）

### 4.1 フォーマット

```
オフセット  サイズ             型                内容
──────────────────────────────────────────────────────────────
0x00        2                 unsigned short    lsize (左文脈サイズ)
0x02        2                 unsigned short    rsize (右文脈サイズ)
0x04        lsize*rsize*2     short[]           連接コスト配列
```

### 4.2 連接コストの格納方法

- 2次元配列 `matrix[lsize][rsize]` を1次元化して格納
- インデックス計算: `index = l + lsize * r`（l=左文脈ID, r=右文脈ID）
- 各要素は `short`（2バイト、符号付き16ビット整数）

### 4.3 ファイルサイズ検証

```
expected_size = sizeof(unsigned short) * (lsize * rsize + 2)
// +2 は lsize, rsize ヘッダの分
```

### 4.4 コスト検索

```csharp
// C# での連接コスト取得
short GetTransitionCost(short[] matrix, int lsize, int leftId, int rightId) {
    return matrix[leftId + lsize * rightId];
}
```

---

## 5. char.bin（文字カテゴリ定義）

### 5.1 フォーマット

```
オフセット              サイズ              型                 内容
───────────────────────────────────────────────────────────────────────
0x00                    4                  unsigned int       カテゴリ数 (csize)
0x04                    32 * csize         char[32][]         カテゴリ名配列（各32バイト固定）
0x04 + 32*csize         4 * 0xFFFF         CharInfo[]         文字→カテゴリマッピング表
```

### 5.2 CharInfo 構造体

```
ビットフィールド構造:
- type: カテゴリのビットマスク（複数カテゴリの組み合わせ）
- default_type: デフォルトカテゴリインデックス
- invoke: 起動フラグ（1=常にこのカテゴリの未知語処理を起動）
- group: グループフラグ（1=同じカテゴリの連続文字をまとめる）
- length: 未知語の最大長
```

### 5.3 文字カテゴリの定義（char.def）

char.def で定義される主要な文字カテゴリ:

| カテゴリ | 説明 |
|---------|------|
| DEFAULT | デフォルト |
| SPACE | 空白文字 |
| KANJI | 漢字 |
| HIRAGANA | ひらがな |
| KATAKANA | カタカナ |
| SYMBOL | 記号 |
| NUMERIC | 数字 |
| ALPHA | アルファベット |
| ASCII | ASCII文字 |
| KANJINUMERIC | 漢数字 |

### 5.4 ファイルサイズ検証

```
expected_size = sizeof(unsigned int) + (32 * csize) + sizeof(CharInfo) * 0xFFFF
```

---

## 6. unk.dic（未知語辞書）

unk.dic は sys.dic と同じバイナリフォーマット（ヘッダ + ダブル配列 + トークン + フィーチャ）を使用する。ただし以下の違いがある:

- `type` フィールドが `MECAB_UNK_DIC (= 2)`
- キーは表層形ではなく、文字カテゴリ名（"KANJI", "HIRAGANA" 等）
- char.bin の文字カテゴリと連動して未知語の処理を行う

### unk.def の定義例

```
KANJI,1285,1285,10000,名詞,一般,*,*,*,*,*
HIRAGANA,1285,1285,10000,名詞,一般,*,*,*,*,*
KATAKANA,1285,1285,10000,名詞,一般,*,*,*,*,*
```

---

## 7. ダブル配列 Trie 構造

### 7.1 基本概念

ダブル配列（Double Array）は、Trie 木を2つの配列 `BASE` と `CHECK` で表現するデータ構造である。MeCab では Darts（Double ARray Trie System）/Darts-clone ライブラリを使用する。

### 7.2 遷移規則

ノード `s` から文字 `c` で遷移する場合:

```
t = BASE[s] + c           // 遷移先ノード番号を計算
CHECK[t] == s であれば有効  // 親ノードの一致を検証
```

### 7.3 Darts / Darts-clone の違い

| 項目 | Darts (オリジナル) | Darts-clone |
|------|-------------------|-------------|
| unit_size | 8バイト (int + int) | 4バイト |
| 配列要素 | BASE[i], CHECK[i] を別々に格納 | 1つの32ビット値に圧縮 |
| MeCab での使用 | 旧バージョン | 新バージョン (0.996) |

### 7.4 MeCab での使用

- MeCab 0.996 では Darts-clone を使用（unit_size = 4バイト）
- ダブル配列サイズ: `dsize = da.unit_size() * da.size()`
- 検索方法: `commonPrefixSearch`（共通接頭辞検索）で入力テキストの全接頭辞を一度に検索

### 7.5 バイナリレイアウト（Darts-clone, unit_size=4）

```
ダブル配列セクション (sys.dic 内):
┌─────────────┬─────────────┬─────────────┬─────────────┬───
│  unit[0]    │  unit[1]    │  unit[2]    │  unit[3]    │ ...
│  4 bytes    │  4 bytes    │  4 bytes    │  4 bytes    │
└─────────────┴─────────────┴─────────────┴─────────────┴───
各 unit は 32 ビットの整数で、base値とcheck値をビットフィールドで圧縮格納
```

### 7.6 共通接頭辞検索（Common Prefix Search）

形態素解析で最も重要な操作。入力文字列の全接頭辞に対してマッチする辞書エントリを検索する:

```
入力: "東京都に住む"
検索結果:
  "東" → マッチ
  "東京" → マッチ
  "東京都" → マッチ
```

### 7.7 ダブル配列のトークンへの対応

ダブル配列の検索結果は `value` として整数値を返す。この値からトークン配列内の位置を計算する:

```
value = (token_count << 8) | token_offset_in_group
```

---

## 8. 辞書のビルドプロセス（mecab-dict-index）

### 8.1 入力ファイル

| ファイル | 説明 |
|---------|------|
| naist-jdic.csv | 辞書エントリ（CSV形式） |
| matrix.def | 連接コスト定義 |
| char.def | 文字カテゴリ定義 |
| unk.def | 未知語定義 |
| left-id.def | 左文脈ID定義 |
| right-id.def | 右文脈ID定義 |
| pos-id.def | 品詞ID定義 |
| rewrite.def | 素性書き換え規則 |
| feature.def | 素性テンプレート |
| dicrc | 辞書設定 |

### 8.2 ビルドコマンド

```bash
# UTF-8 辞書のビルド
mecab-dict-index -d . -o output_dir -f utf-8 -t utf-8

# Shift_JIS への変換
mecab-dict-index -d . -o output_dir -f utf-8 -t sjis
```

### 8.3 コンパイル処理の流れ

```
1. CSVファイル読み込み
   ├── 表層形のソート
   ├── 左/右文脈IDの割り当て
   └── コスト値の検証

2. ダブル配列構築
   ├── ソート済み表層形からTrie構築
   ├── Darts::DoubleArray::build() 呼び出し
   └── バイナリデータ生成

3. トークン配列構築
   ├── 各単語のToken構造体生成
   ├── lcAttr, rcAttr, posid, wcost 設定
   └── feature文字列オフセット計算

4. フィーチャバッファ構築
   ├── 品詞以降のCSVフィールドを連結
   └── NULL終端文字列として格納

5. バイナリファイル出力
   ├── ヘッダ (72バイト)
   ├── ダブル配列データ
   ├── トークン配列
   └── フィーチャバッファ

6. matrix.def → matrix.bin コンパイル
   ├── lsize, rsize 読み込み
   ├── short配列構築
   └── バイナリ出力

7. char.def → char.bin コンパイル
   ├── カテゴリ定義解析
   ├── Unicode範囲マッピング構築
   └── バイナリ出力

8. unk.def → unk.dic コンパイル
   （sys.dic と同様の処理）
```

---

## 9. jpreprocess の naist-jdic 辞書フォーマット

### 9.1 概要

[jpreprocess](https://github.com/jpreprocess/jpreprocess) は OpenJTalk の Rust 再実装で、独自の辞書フォーマットを持つ。

### 9.2 辞書の種類

1. **独自フォーマット**: 辞書ファイルのサイズを削減した形式
2. **従来形式**: 全ての情報を文字列で持つ形式（MeCab 互換）

### 9.3 辞書ビルド

- `lindera-ipadic-builder` をベースにした `jpreprocess-dictionary-builder` でビルド
- OpenJTalk に同梱されていた naist-jdic CSV から辞書を生成
- MeCab バイナリ辞書とは互換性なし（独自バイナリ形式）

### 9.4 jpreprocess のリポジトリ構成

- `jpreprocess-naist-jdic`: 辞書ビルド用クレート
- naist-jdic.csv と unidic-csj.csv を含む
- char.def, feature.def, matrix.def, unk.def を含む

---

## 10. OpenJTalk の NJDNode 構造体

辞書からの情報は最終的に NJDNode 構造体に格納される:

| フィールド | 型 | CSV対応 | 説明 |
|-----------|------|---------|------|
| string | char* | フィールド0 | 表層形 |
| pos | char* | フィールド4 | 品詞 |
| pos_group1 | char* | フィールド5 | 品詞細分類1 |
| pos_group2 | char* | フィールド6 | 品詞細分類2 |
| pos_group3 | char* | フィールド7 | 品詞細分類3 |
| ctype | char* | フィールド8 | 活用型 |
| cform | char* | フィールド9 | 活用形 |
| orig | char* | フィールド10 | 原形 |
| read | char* | フィールド11 | 読み |
| pron | char* | フィールド12 | 発音 |
| acc | int | フィールド13 | アクセント核位置 |
| mora_size | int | フィールド13 | モーラ数 |
| chain_rule | char* | フィールド14 | アクセント結合規則 |
| chain_flag | int | (処理結果) | 結合フラグ（NJD処理で設定） |
| prev | NJDNode* | - | 前ノードへのポインタ |
| next | NJDNode* | - | 次ノードへのポインタ |

---

## 11. C# でバイナリ辞書を読み込むために必要な知識

### 11.1 既存実装の参考

- **NMeCab**: C# による MeCab 再実装。バイナリ辞書の読み込みを実装済み
- **MeCab.DotNet**: MeCab の .NET ラッパー

### 11.2 実装上の注意点

1. **エンディアン**: MeCab のバイナリはリトルエンディアン（x86/x64）で作成される
2. **メモリマッピング**: 元の C++ 実装は mmap を使用。C# では `MemoryMappedFile` が利用可能
3. **構造体アライメント**: Token 構造体は8バイト境界にアライン
4. **文字エンコーディング**: UTF-8 辞書を使用する場合、`Encoding.UTF8` で文字列をデコード
5. **ダブル配列ライブラリ**: Darts-clone 互換のダブル配列検索を C# で実装する必要がある

### 11.3 C# での読み込み手順

```csharp
// 1. ファイルをバイト配列として読み込み
byte[] data = File.ReadAllBytes("sys.dic");

// 2. ヘッダの解析（リトルエンディアン）
uint magic   = BitConverter.ToUInt32(data, 0);
uint version = BitConverter.ToUInt32(data, 4);
uint type    = BitConverter.ToUInt32(data, 8);
uint lexsize = BitConverter.ToUInt32(data, 12);
uint lsize   = BitConverter.ToUInt32(data, 16);
uint rsize   = BitConverter.ToUInt32(data, 20);
uint dsize   = BitConverter.ToUInt32(data, 24);
uint tsize   = BitConverter.ToUInt32(data, 28);
uint fsize   = BitConverter.ToUInt32(data, 32);
string charset = Encoding.ASCII.GetString(data, 40, 32).TrimEnd('\0');

// 3. マジックナンバー検証
uint expectedMagic = (uint)data.Length ^ 0xef718f77u;
Debug.Assert(magic == expectedMagic);

// 4. バージョン検証
Debug.Assert(version == 0x66);

// 5. 各セクションのオフセット計算
int daOffset      = 72;                        // ダブル配列開始
int tokenOffset   = 72 + (int)dsize;           // トークン配列開始
int featureOffset = 72 + (int)dsize + (int)tsize; // フィーチャ開始

// 6. ダブル配列検索の実装
// unit_size = 4 (Darts-clone) の場合
int unitCount = (int)(dsize / 4);
uint[] daUnits = new uint[unitCount];
Buffer.BlockCopy(data, daOffset, daUnits, 0, (int)dsize);

// 7. トークンの解析
int tokenCount = (int)(tsize / 16); // Token は 16 バイト
for (int i = 0; i < tokenCount; i++) {
    int offset = tokenOffset + i * 16;
    ushort lcAttr  = BitConverter.ToUInt16(data, offset);
    ushort rcAttr  = BitConverter.ToUInt16(data, offset + 2);
    ushort posid   = BitConverter.ToUInt16(data, offset + 4);
    short  wcost   = BitConverter.ToInt16(data, offset + 6);
    uint   feature = BitConverter.ToUInt32(data, offset + 8);
    uint   compound = BitConverter.ToUInt32(data, offset + 12);
}

// 8. フィーチャ文字列の取得
string GetFeature(byte[] data, int featureOffset, uint featureIndex) {
    int start = featureOffset + (int)featureIndex;
    int end = start;
    while (data[end] != 0) end++;
    return Encoding.UTF8.GetString(data, start, end - start);
}
```

### 11.4 matrix.bin の読み込み

```csharp
byte[] matrixData = File.ReadAllBytes("matrix.bin");
ushort lsize = BitConverter.ToUInt16(matrixData, 0);
ushort rsize = BitConverter.ToUInt16(matrixData, 2);

short[] matrix = new short[lsize * rsize];
Buffer.BlockCopy(matrixData, 4, matrix, 0, lsize * rsize * 2);

// 連接コスト取得
short cost = matrix[leftId + lsize * rightId];
```

---

## 12. 参考資料

- [MeCab 公式ドキュメント - 辞書の詳細](https://taku910.github.io/mecab/dic-detail.html)
- [MeCab 公式ドキュメント - 単語の追加方法](https://taku910.github.io/mecab/dic.html)
- [MeCab ソースコード (taku910/mecab)](https://github.com/taku910/mecab)
- [Darts-clone ドキュメント](https://github.com/s-yata/darts-clone)
- [jpreprocess プロジェクト](https://github.com/jpreprocess/jpreprocess)
- [jpreprocess/naist-jdic 辞書リポジトリ](https://github.com/jpreprocess/naist-jdic)
- [Open JTalk 公式サイト](https://open-jtalk.sourceforge.net/)
- [NMeCab (C# 実装)](https://github.com/komutan/NMeCab)
- [MeCab.DotNet](https://github.com/kekyo/MeCab.DotNet)
- [Darts: Double ARray Trie System](http://chasen.org/~taku/software/darts/)
- [情報系修士にもわかるダブル配列](https://takeda25.hatenablog.jp/entry/20120219/1329634865)
- [MeCab はどのように形態素解析しているか - クックパッド開発者ブログ](https://techlife.cookpad.com/entry/2016/05/11/170000)
- [OpenJTalk の解析資料](https://www.negi.moe/negitalk/openjtalk.html)
- [OpenJTalkの辞書へ単語を追加する - LANCARD.LAB](https://www.lancard.com/blog/2016/02/09/open-jtalk%E3%81%AE%E8%BE%9E%E6%9B%B8%E3%81%B8%E3%80%81%E5%8D%98%E8%AA%9E%E3%82%92%E8%BF%BD%E5%8A%A0%E3%81%99%E3%82%8B/)
- [Open JTalk の辞書サイズ調査](https://kunsen.net/2021/03/21/post-3844/)
- [notmecab (Rust)](https://docs.rs/notmecab/latest/notmecab/struct.Dict.html)
