# pyopenjtalk 調査レポート

## 1. プロジェクト概要

**pyopenjtalk**はOpenJTalkのPythonラッパーで、日本語テキスト音声合成（TTS）と書記素音素変換（G2P）機能を提供する。

- リポジトリ: https://github.com/r9y9/pyopenjtalk
- ライセンス: MIT（pyopenjtalk）、Modified BSD（OpenJTalk）、Apache 2.0（marine）
- 言語構成: Cython 52%、Python 48%
- 最新バージョン: v0.4.1
- 対応Python: 3.8以上

### 主な特徴

- OpenJTalkのテキスト処理フロントエンドをCython経由でラッピング
- HTS full-context label生成
- G2P（書記素→音素）変換（音素列/カタカナ読み出力対応）
- HTSEngine音声合成バックエンド
- ユーザー辞書機能
- marine（DNNベースアクセント推定）のオプションサポート
- スレッドセーフ設計

---

## 2. ソースコード構造

```
pyopenjtalk/
├── lib/                          # Git submodules
│   ├── open_jtalk/               # r9y9/open_jtalk (OpenJTalk C/C++ソース)
│   └── hts_engine_API/           # r9y9/hts_engine_API (HTSEngine C ソース)
├── pyopenjtalk/
│   ├── __init__.py               # Python側メインモジュール（公開API）
│   ├── openjtalk.pyx             # Cython: OpenJTalkフロントエンドラッパー
│   ├── htsengine.pyx             # Cython: HTSEngineバックエンドラッパー
│   ├── utils.py                  # marine結果マージユーティリティ
│   ├── openjtalk/                # Cython .pxd宣言ファイル群
│   │   ├── __init__.pxd
│   │   ├── mecab.pxd             # MeCab C API宣言
│   │   ├── njd.pxd               # NJD（日本語ルール処理）C API宣言
│   │   ├── jpcommon.pxd          # JPCommon（共通音韻処理）C API宣言
│   │   ├── text2mecab.pxd        # テキスト前処理 C API宣言
│   │   ├── mecab2njd.pxd         # MeCab→NJD変換 C API宣言
│   │   └── njd2jpcommon.pxd      # NJD→JPCommon変換 C API宣言
│   └── htsvoice/                 # デフォルトHTSボイスファイル
│       └── mei_normal.htsvoice
├── tests/
│   ├── test_openjtalk.py         # テストスイート
│   └── test_data/                # テストデータ
├── setup.py                      # ビルド設定（C/C++コンパイル設定含む）
└── pyproject.toml                # パッケージメタデータ
```

### ビルド依存関係

pyopenjtalkは以下のOpenJTalk C/C++ソースモジュールを直接コンパイルしてリンクする:

| モジュール | 機能 |
|-----------|------|
| `jpcommon` | 共通音韻処理・full-context label生成 |
| `mecab/src` | 形態素解析エンジン |
| `mecab2njd` | MeCab出力→NJDノード変換 |
| `njd` | NJDデータ構造 |
| `njd2jpcommon` | NJD→JPCommon変換 |
| `njd_set_accent_phrase` | アクセント句設定 |
| `njd_set_accent_type` | アクセント型設定 |
| `njd_set_digit` | 数字読み変換 |
| `njd_set_long_vowel` | 長音化処理 |
| `njd_set_pronunciation` | 読み・発音設定 |
| `njd_set_unvoiced_vowel` | 無声母音化処理 |
| `text2mecab` | テキスト→MeCab入力変換 |

---

## 3. g2p()関数の内部処理フロー

### 3.1 公開API

```python
def g2p(text, kana=False, join=True):
    """
    Args:
        text (str): Unicode日本語テキスト
        kana (bool): True=カタカナ出力、False=音素出力（デフォルト）
        join (bool): True=文字列連結、False=リスト出力（デフォルト: True）
    Returns:
        str or list: G2P結果
    """
```

### 3.2 内部処理フロー詳細

```
入力テキスト（str）
    │
    ▼
[1] text2mecab()        ← テキスト正規化（全角/半角変換等）
    │                     C関数: text2mecab(buff, _text)
    │                     8192バイトバッファに変換結果を格納
    ▼
[2] Mecab_analysis()    ← MeCab形態素解析
    │                     辞書: naist-jdic (open_jtalk_dic_utf_8-1.11)
    │                     出力: 形態素feature配列とサイズ
    ▼
[3] mecab2njd()         ← MeCab出力→NJDノード変換
    │                     MeCabのfeature文字列をNJDNodeの
    │                     リンクリストに変換
    ▼
[4] NJD処理パイプライン（6段階、順序が重要）
    │
    ├─[4a] njd_set_pronunciation()   ← 読み・発音の設定
    │      辞書の「読み」フィールドから発音を設定
    │
    ├─[4b] njd_set_digit()           ← 数字読み変換
    │      数字を日本語読みに変換（例: 100→ヒャク）
    │
    ├─[4c] njd_set_accent_phrase()   ← アクセント句構成
    │      隣接語のアクセント句結合判定
    │
    ├─[4d] njd_set_accent_type()     ← アクセント型設定
    │      結合規則に基づくアクセント核位置の決定
    │
    ├─[4e] njd_set_unvoiced_vowel()  ← 無声母音化
    │      無声子音に挟まれた狭母音(i,u)の無声化
    │
    └─[4f] njd_set_long_vowel()      ← 長音化処理
           母音連続の長音化（例: オー）
    │
    ▼
[5] njd2feature()       ← NJDノード→Python辞書リスト変換
    │                     Cython側でNJDリンクリストを走査し
    │                     各ノードをPython dictに変換
    ▼
[6] 分岐: kana=True / kana=False
    │
    ├─ kana=True の場合:
    │   各NJDノードの"pron"フィールドを連結
    │   記号ノードは"string"フィールドを使用
    │   特殊文字("'")を除去
    │   → カタカナ文字列を返す
    │
    └─ kana=False の場合:
        [6a] make_label() → full-context label生成
        │    feature2njd(): Python dict → NJDノード復元
        │    njd2jpcommon(): NJD → JPCommon変換
        │    JPCommon_make_label(): label生成
        │
        [6b] ラベルから音素抽出
             各ラベルの先頭・末尾（sil）を除き
             "-"と"+"で囲まれた部分を音素として抽出
             label.split("-")[1].split("+")[0]
             → 音素列（スペース区切り文字列 or リスト）を返す
```

### 3.3 処理例

```python
# 入力: "こんにちは"

# run_frontend() の出力（NJD features）:
[{
    "string": "こんにちは",
    "pos": "感動詞",
    "pos_group1": "*",
    "pos_group2": "*",
    "pos_group3": "*",
    "ctype": "*",
    "cform": "*",
    "orig": "こんにちは",
    "read": "コンニチハ",
    "pron": "コンニチワ",
    "acc": 0,
    "mora_size": 5,
    "chain_rule": "-1",
    "chain_flag": -1,
}]

# g2p(kana=True) → "コンニチワ"
# g2p(kana=False) → "k o N n i ch i w a"
```

---

## 4. extract_fullcontext()の出力フォーマット（HTS full-context label）

### 4.1 処理フロー

```python
def extract_fullcontext(text, run_marine=False):
    njd_features = run_frontend(text)      # NJD特徴量取得
    if run_marine:
        njd_features = estimate_accent(njd_features)  # アクセント推定（オプション）
    return make_label(njd_features)        # full-context label生成
```

### 4.2 HTS full-context labelフォーマット

日本語HTS full-context labelは以下の階層構造を持つ:

```
音素(phoneme) → モーラ(mora) → 単語(word) → アクセント句(accent phrase) → フレーズ(phrase) → 発話(utterance)
```

#### ラベル構造

```
p1^p2-p3+p4=p5/A:a1+a2+a3/B:b1-b2_b3/C:c1_c2+c3/D:d1+d2_d3/E:e1_e2!e3_e4-e5/F:f1_f2#f3_f4@f5_f6|f7_f8/G:g1_g2%g3_g4-g5/H:h1_h2/I:i1-i2@i3+i4&i5-i6|i7+i8/J:j1_j2/K:k1+k2-k3
```

#### 各セクションの意味

| セクション | 意味 | フィールド |
|-----------|------|-----------|
| `p1^p2-p3+p4=p5` | 音素コンテキスト | p1:2つ前の音素, p2:1つ前の音素, p3:現在の音素, p4:1つ後の音素, p5:2つ後の音素 |
| `/A:` | 現在モーラ情報 | a1:モーラ内の音素位置, a2:アクセント句先頭からのモーラ位置, a3:アクセント核からの距離 |
| `/B:` | 前の単語情報 | b1:品詞, b2:活用型, b3:活用形 |
| `/C:` | 現在の単語情報 | c1:品詞, c2:活用型, c3:活用形 |
| `/D:` | 次の単語情報 | d1:品詞, d2:活用型, d3:活用形 |
| `/E:` | 前のアクセント句情報 | e1:モーラ数, e2:アクセント型, e3:疑問フラグ, e4:前からのアクセント句位置, e5:後ろからのアクセント句位置 |
| `/F:` | 現在のアクセント句情報 | f1:モーラ数, f2:アクセント型, f3:疑問フラグ, f4〜f8:位置情報 |
| `/G:` | 次のアクセント句情報 | g1:モーラ数, g2:アクセント型, g3〜g5:位置情報 |
| `/H:` | 前の呼気段落情報 | h1:アクセント句数, h2:モーラ数 |
| `/I:` | 現在の呼気段落情報 | i1〜i8:アクセント句数、モーラ数、位置情報 |
| `/J:` | 次の呼気段落情報 | j1:アクセント句数, j2:モーラ数 |
| `/K:` | 発話全体情報 | k1:呼気段落数, k2:アクセント句数, k3:モーラ数 |

#### 出力例（"こんにちは"の場合）

```
xx^xx-sil+k=o/A:xx+xx+xx/B:xx-xx_xx/C:xx_xx+xx/D:09+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
xx^sil-k+o=N/A:0+1+5/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
sil^k-o+N=n/A:0+2+4/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
k^o-N+n=i/A:1+2+4/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
o^N-n+i=ch/A:1+3+3/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
N^n-i+ch=i/A:1+3+3/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
n^i-ch+i=w/A:2+4+2/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
i^ch-i+w=a/A:2+4+2/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
ch^i-w+a=sil/A:3+5+1/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
i^w-a+sil=xx/A:3+5+1/B:xx-xx_xx/C:09_xx+xx/D:xx+xx_xx/E:xx_xx!xx_xx-xx/F:5_0#0_xx@1_1|1_1/G:xx_xx%xx_xx_xx/H:xx_xx/I:1-5@1+1&1-1|1+5/J:xx_xx/K:1+1-5
w^a-sil+xx=xx/A:xx+xx+xx/B:09-xx_xx/C:xx_xx+xx/D:xx+xx_xx/E:5_0!0_xx-1/F:xx_xx#xx_xx@xx_xx|xx_xx/G:xx_xx%xx_xx_xx/H:1_5/I:xx-xx@xx+xx&xx-xx|xx+xx/J:xx_xx/K:1+1-5
```

#### g2pでの音素抽出ロジック

```python
# labels[1:-1] で先頭・末尾のsilラベルを除外
# 各ラベルから: label.split("-")[1].split("+")[0] で音素を抽出
# 例: "sil^k-o+N=n/A:..." → "o"
prons = list(map(lambda s: s.split("-")[1].split("+")[0], labels[1:-1]))
# → ["k", "o", "N", "n", "i", "ch", "i", "w", "a"]
# join → "k o N n i ch i w a"
```

---

## 5. run_frontend()の処理フロー

### 5.1 Cython側の実装（openjtalk.pyx）

```python
@_lock_manager()
def run_frontend(self, text):
    cdef char buff[8192]

    if isinstance(text, str):
        text = text.encode("utf-8")

    with nogil:
        # Step 1: テキスト正規化→MeCab入力形式
        text2mecab(buff, _text)

        # Step 2: MeCab形態素解析
        Mecab_analysis(self.mecab, buff)

        # Step 3: MeCab結果→NJDノード変換
        mecab2njd(self.njd, Mecab_get_feature(self.mecab), Mecab_get_size(self.mecab))

        # Step 4: NJD処理パイプライン（6段階）
        njd_set_pronunciation(self.njd)     # 読み設定
        njd_set_digit(self.njd)             # 数字読み
        njd_set_accent_phrase(self.njd)     # アクセント句構成
        njd_set_accent_type(self.njd)       # アクセント型決定
        njd_set_unvoiced_vowel(self.njd)    # 無声母音化
        njd_set_long_vowel(self.njd)        # 長音化

    # Step 5: NJDノード→Python辞書リスト
    features = njd2feature(self.njd)

    # メモリ解放
    NJD_refresh(self.njd)
    Mecab_refresh(self.mecab)

    return features
```

### 5.2 NJDNode feature辞書の構造

各NJDNodeは以下の14フィールドを持つ辞書として返される:

| フィールド | 型 | 説明 | 例 |
|-----------|-----|------|-----|
| `string` | str | 表層形 | "今日" |
| `pos` | str | 品詞 | "名詞" |
| `pos_group1` | str | 品詞細分類1 | "副詞可能" |
| `pos_group2` | str | 品詞細分類2 | "*" |
| `pos_group3` | str | 品詞細分類3 | "*" |
| `ctype` | str | 活用型 | "*" |
| `cform` | str | 活用形 | "*" |
| `orig` | str | 原形 | "今日" |
| `read` | str | 読み（カタカナ） | "キョウ" |
| `pron` | str | 発音（カタカナ） | "キョー" |
| `acc` | int | アクセント核位置 | 1 |
| `mora_size` | int | モーラ数 | 2 |
| `chain_rule` | str | アクセント結合規則 | "C4" |
| `chain_flag` | int | 結合フラグ | 1 |

### 5.3 NJD処理の各段階の詳細

#### njd_set_pronunciation（読み・発音設定）
- 辞書の読みフィールドから発音を設定
- 「は」→「ワ」（助詞の場合）等の特殊読み変換
- 未知語への読み推定

#### njd_set_digit（数字読み変換）
- 漢数字・アラビア数字を日本語読みに変換
- 助数詞との結合処理（例: 3本→サンボン）
- 大きな数値の位取り（万、億など）

#### njd_set_accent_phrase（アクセント句構成）
- 隣接する語をアクセント句にまとめる判定
- chain_ruleに基づく結合判定
- 品詞情報による結合パターン

#### njd_set_accent_type（アクセント型設定）
- アクセント句内のアクセント核位置を決定
- chain_rule（C1〜C5）に基づく結合型判定
- 辞書のアクセント情報を考慮

#### njd_set_unvoiced_vowel（無声母音化）
- 無声子音に挟まれた狭母音（イ、ウ）の無声化
- 日本語の音韻規則に基づく自動判定

#### njd_set_long_vowel（長音化）
- 母音連続の長音化処理
- 「オウ」→「オー」等

---

## 6. ユーザー辞書機能の仕組み

### 6.1 ユーザー辞書CSV形式

```csv
表層形,左文脈ID,右文脈ID,コスト,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音,アクセント核/モーラ数,アクセント結合タイプ
```

実際の例:
```csv
ｎｎｍｎ,,,1,名詞,一般,*,*,*,*,ｎｎｍｎ,ナナミン,ナナミン,1/4,*
ＧＮＵ,,,1,名詞,一般,*,*,*,*,ＧＮＵ,グヌー,グヌー,2/3,*
```

### 6.2 辞書作成API

```python
# Step 1: CSVからバイナリ辞書を作成
pyopenjtalk.mecab_dict_index("user.csv", "user.dic")

# Step 2: グローバルインスタンスを更新
pyopenjtalk.update_global_jtalk_with_user_dict("user.dic")
```

### 6.3 内部実装

#### mecab_dict_index()
```python
def mecab_dict_index(path, out_path, dn_mecab=None):
    if dn_mecab is None:
        dn_mecab = OPEN_JTALK_DICT_DIR
    # MeCabの辞書インデクサを呼び出し
    r = _mecab_dict_index(dn_mecab, path.encode("utf-8"), out_path.encode("utf-8"))
```

Cython側では`mecab-dict-index`コマンド相当の処理を実行:
```python
def mecab_dict_index(bytes dn_mecab, bytes path, bytes out_path):
    cdef (char*)[10] argv = [
        "mecab-dict-index",
        "-d", dn_mecab,    # システム辞書
        "-u", out_path,     # 出力ユーザー辞書
        "-f", "utf-8",      # 入力エンコーディング
        "-t", "utf-8",      # 出力エンコーディング
        path                # 入力CSV
    ]
    ret = _mecab_dict_index(10, argv)
```

#### update_global_jtalk_with_user_dict()
```python
def update_global_jtalk_with_user_dict(path):
    global _global_jtalk
    # ユーザー辞書付きで新しいOpenJTalkインスタンスを作成
    _global_jtalk = _global_instance_manager(
        instance=OpenJTalk(
            dn_mecab=OPEN_JTALK_DICT_DIR,
            userdic=path.encode("utf-8")
        )
    )
```

#### MeCabユーザー辞書ロード（Cython側）
```cython
cdef inline int Mecab_load_with_userdic(Mecab *m, char* dicdir, char* userdic):
    if userdic == NULL or strlen(userdic) == 0:
        return Mecab_load(m, dicdir)  # ユーザー辞書なし

    # MeCabモデルを "-d dicdir -u userdic" オプション付きで作成
    cdef (char*)[5] argv = ["mecab", "-d", dicdir, "-u", userdic]
    cdef Model *model = createModel(5, argv)
    # Tagger, Lattice を初期化
```

---

## 7. CythonによるOpenJTalk Cライブラリのラッピング方法

### 7.1 アーキテクチャ

pyopenjtalkは3層のラッピング構造を持つ:

```
[Python API層] __init__.py
    │  グローバルインスタンス管理、スレッドセーフ
    ▼
[Cython層] openjtalk.pyx / htsengine.pyx
    │  C構造体操作、メモリ管理、GILリリース
    ▼
[C/C++層] lib/open_jtalk/src/* / lib/hts_engine_API/src/*
    │  実際のアルゴリズム実装
    ▼
[辞書] open_jtalk_dic_utf_8-1.11
```

### 7.2 .pxd宣言ファイル（型定義）

Cythonの`.pxd`ファイルでC構造体と関数を宣言:

```cython
# njd.pxd - NJDNode構造体の宣言
cdef extern from "njd.h":
    cdef cppclass NJDNode:
        char *string
        char *pos
        char *pos_group1
        char *pos_group2
        char *pos_group3
        char *ctype
        char *cform
        char *orig
        char *read
        char *pron
        int acc
        int mora_size
        char *chain_rule
        int chain_flag
        NJDNode *prev
        NJDNode *next
```

```cython
# mecab.pxd - MeCab構造体の宣言
cdef extern from "mecab.h":
    cdef cppclass Mecab:
        char **feature
        int size
        void *model
        void *tagger
        void *lattice
```

### 7.3 GILリリースとスレッドセーフ

- C関数呼び出し時に`nogil`でGILを解放し、マルチスレッド対応
- `_lock_manager`デコレータ（`threading.Lock`ベース）でメソッド全体をロック
- OpenJTalkクラスとHTSEngineクラスでそれぞれ独立したロックを使用

```cython
_lock_manager = _generate_lock_manager()

@_lock_manager()
def run_frontend(self, text):
    # ... Lock取得中にC関数を実行 ...
    with nogil:
        text2mecab(buff, _text)       # GILなしでC関数実行
        Mecab_analysis(self.mecab, buff)
        # ...
```

### 7.4 Python ↔ C データ変換

#### NJDノード → Python辞書（njd2feature）
```cython
cdef njd2feature(_njd.NJD* njd):
    cdef _njd.NJDNode* node = njd.head
    features = []
    while node is not NULL:
        features.append(node2feature(node))  # 各ノードをdictに変換
        node = node.next
    return features
```

#### Python辞書 → NJDノード（feature2njd）
```cython
cdef void feature2njd(_njd.NJD* njd, features):
    for feature_node in features:
        node = <_njd.NJDNode *> calloc(1, sizeof(_njd.NJDNode))
        _njd.NJDNode_initialize(node)
        _njd.NJDNode_set_string(node, feature_node["string"].encode("utf-8"))
        # ... 全フィールドを設定 ...
        _njd.NJD_push_node(njd, node)
```

### 7.5 ビルド設定（setup.py）

```python
# OpenJTalkフロントエンド拡張
Extension(
    name="pyopenjtalk.openjtalk",
    sources=["pyopenjtalk/openjtalk.pyx"] + all_src,  # Cython + C/C++
    include_dirs=include_dirs,
    language="c++",
    define_macros=[
        ("HAVE_CONFIG_H", None),
        ("DIC_VERSION", "102"),
        ("CHARSET_UTF_8", None),
        # ...
    ],
)
```

---

## 8. pyopenjtalkが使用する辞書とモデルファイル

### 8.1 MeCab辞書（必須）

| 項目 | 内容 |
|------|------|
| 辞書名 | open_jtalk_dic_utf_8-1.11 |
| ベース | naist-jdic（IPADIC互換 + アクセント情報拡張） |
| ダウンロードURL | https://github.com/r9y9/open_jtalk/releases/download/v1.11.1/open_jtalk_dic_utf_8-1.11.tar.gz |
| エンコーディング | UTF-8 |
| ライセンス | BSD |
| 自動管理 | `_lazy_init()`で辞書不在時に自動ダウンロード |
| 環境変数 | `OPEN_JTALK_DICT_DIR`でパスを上書き可能 |

#### 辞書フォーマット（naist-jdic拡張）

通常のIPADIC（13フィールド）に2フィールドを追加:

```
表層形,左文脈ID,右文脈ID,コスト,品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用型,活用形,原形,読み,発音,アクセント核位置/モーラ数,アクセント結合タイプ
```

| フィールド | 番号 | 例 | 説明 |
|-----------|------|-----|------|
| アクセント核位置/モーラ数 | 14 | `3/4` | アクセント核がモーラ3にあり、全4モーラ |
| アクセント結合タイプ | 15 | `C1` | アクセント句結合時の結合規則（C1〜C5） |

### 8.2 HTSボイスファイル（音声合成用）

| 項目 | 内容 |
|------|------|
| ファイル | `mei_normal.htsvoice` |
| 場所 | `pyopenjtalk/htsvoice/` |
| 用途 | HMMベースTTS音声合成 |
| サンプリング周波数 | 48000 Hz |

### 8.3 marine（オプション・DNN アクセント推定）

| 項目 | 内容 |
|------|------|
| パッケージ | `marine` (https://github.com/6gsn/marine) |
| インストール | `pip install pyopenjtalk[marine]` |
| 用途 | ニューラルネットワークベースのアクセント推定 |
| 入力 | NJD features |
| 出力 | accent_status, accent_phrase_boundary |

---

## 9. pyopenjtalkの出力例

### 9.1 基本的な挨拶

```python
# 入力: "こんにちは"
g2p("こんにちは")           # → "k o N n i ch i w a"
g2p("こんにちは", kana=True) # → "コンニチワ"
```

### 9.2 丁寧文

```python
# 入力: "今日も良い天気ですね"
g2p("今日も良い天気ですね")  # → "ky o o m o y o i t e N k i d e s U n e"
```

注: 大文字`U`は無声母音化を表す。

### 9.3 無声母音化の例

```python
# 入力: "ななみんです"
g2p("ななみんです")          # → "n a n a m i N d e s U"
# 末尾の「す」が無声化: "su" → "s U"
```

### 9.4 長音の例

```python
# 入力: "ハローユーチューブ"
g2p("ハローユーチューブ")     # → "h a r o o y u u ch u u b u"
```

### 9.5 カタカナ読みの例

```python
g2p("今日もこんにちは", kana=True)                    # → "キョーモコンニチワ"
g2p("いやあん", kana=True)                            # → "イヤーン"
g2p("パソコンのとりあえず知っておきたい使い方", kana=True)  # → "パソコンノトリアエズシッテオキタイツカイカタ"
```

### 9.6 run_frontend()の出力例（複数語の場合）

```python
# 入力: "今日も良い天気ですね"
run_frontend("今日も良い天気ですね")
# → [
#     {"string": "今日", "pos": "名詞", "read": "キョウ", "pron": "キョー", "acc": 1, "mora_size": 2, ...},
#     {"string": "も", "pos": "助詞", "read": "モ", "pron": "モ", "acc": 0, "mora_size": 1, ...},
#     {"string": "良い", "pos": "形容詞", "read": "ヨイ", "pron": "ヨイ", "acc": 1, "mora_size": 2, ...},
#     {"string": "天気", "pos": "名詞", "read": "テンキ", "pron": "テンキ", "acc": 1, "mora_size": 3, ...},
#     {"string": "です", "pos": "助動詞", "read": "デス", "pron": "デス", "acc": 0, "mora_size": 2, ...},
#     {"string": "ね", "pos": "助詞", "read": "ネ", "pron": "ネ", "acc": 0, "mora_size": 1, ...},
# ]
```

---

## 10. C#実装への参考ポイント

### 10.1 API設計の参考

pyopenjtalkのAPI階層はC#実装でも参考にできる:

```csharp
// 高レベルAPI（pyopenjtalkの__init__.pyに対応）
public static class JapaneseG2P
{
    // g2p()相当
    public static string ToPhonemes(string text);
    public static string ToKana(string text);
    public static IList<string> ToPhonemeList(string text);

    // run_frontend()相当
    public static IList<NjdFeature> RunFrontend(string text);

    // make_label()相当
    public static IList<string> MakeLabel(IList<NjdFeature> features);

    // extract_fullcontext()相当
    public static IList<string> ExtractFullContext(string text);
}

// NJDNode feature（14フィールド）
public class NjdFeature
{
    public string Surface { get; set; }      // string
    public string Pos { get; set; }          // pos
    public string PosGroup1 { get; set; }    // pos_group1
    public string PosGroup2 { get; set; }    // pos_group2
    public string PosGroup3 { get; set; }    // pos_group3
    public string ConjugationType { get; set; }  // ctype
    public string ConjugationForm { get; set; }  // cform
    public string Original { get; set; }     // orig
    public string Reading { get; set; }      // read
    public string Pronunciation { get; set; } // pron
    public int AccentPosition { get; set; }  // acc
    public int MoraSize { get; set; }        // mora_size
    public string ChainRule { get; set; }    // chain_rule
    public int ChainFlag { get; set; }       // chain_flag
}
```

### 10.2 処理パイプラインの対応

| pyopenjtalk (C) | C#実装での対応方針 |
|-----------------|-------------------|
| `text2mecab()` | テキスト正規化クラス（全角/半角変換等） |
| `Mecab_analysis()` | NMeCab / MeCab.DotNet の形態素解析 |
| `mecab2njd()` | MeCab出力パーサー（NjdFeatureリスト生成） |
| `njd_set_pronunciation()` | 読み設定ルールエンジン |
| `njd_set_digit()` | 数字読み変換モジュール |
| `njd_set_accent_phrase()` | アクセント句構成ルール |
| `njd_set_accent_type()` | アクセント型決定ロジック |
| `njd_set_unvoiced_vowel()` | 無声母音化ルール |
| `njd_set_long_vowel()` | 長音化ルール |
| `njd2jpcommon()` + `JPCommon_make_label()` | Full-context label生成器 |

### 10.3 重要な実装上の注意点

1. **処理順序の厳守**: NJD処理の6段階は厳密な順序で実行する必要がある
2. **naist-jdic辞書との互換性**: MeCab辞書はnaist-jdicのOpenJTalk拡張フォーマット（14+2フィールド）に対応する必要がある
3. **text2mecabの役割**: MeCab解析前のテキスト正規化（全角→半角変換等）は不可欠
4. **スレッドセーフ設計**: pyopenjtalkはLockベースのスレッドセーフを実現しており、C#でも同様の配慮が必要
5. **メモリ管理**: pyopenjtalkはC構造体のメモリをrefresh/clearで管理。C#ではGCで自動管理可能
6. **カタカナ→音素変換**: full-context label生成を介して音素を抽出する方法は、C#でも同様のアプローチが可能。ただし、カタカナ→音素の直接変換テーブルを実装する方がシンプル
7. **辞書の自動ダウンロード**: pyopenjtalkは辞書不在時に自動ダウンロードを行うが、C#/Unity向けではパッケージ同梱が望ましい

### 10.4 pyopenjtalkのG2P処理の2つのパス

pyopenjtalkのg2p()には2つの処理パスがある:

1. **音素パス（kana=False）**: `run_frontend()` → `make_label()` → ラベルパース
   - Full-context label経由で音素を取得
   - 無声母音化等の情報も反映される
   - 計算コストがやや高い

2. **カタカナパス（kana=True）**: `run_frontend()` → pronフィールド連結
   - NJDのpronフィールドをそのまま連結
   - Full-context label生成をスキップ
   - 計算コストが低い

C#実装では、カタカナ→音素変換テーブルを持つことで、Full-context label生成なしに音素を得る「第3のパス」を検討できる。これは処理速度の面で有利。

---

## 11. グローバルインスタンス管理パターン

### 11.1 シングルトン + 遅延初期化

```python
def _global_instance_manager(instance_factory=None, instance=None):
    """スレッドセーフなシングルトンインスタンス管理"""
    _instance = instance
    mutex = Lock()

    @contextmanager
    def manager():
        nonlocal _instance
        with mutex:
            if _instance is None:
                _instance = instance_factory()
            yield _instance
    return manager
```

### 11.2 C#での対応パターン

```csharp
// Lazy<T>を使ったスレッドセーフなシングルトン
public static class G2PEngine
{
    private static readonly Lazy<OpenJTalkFrontend> _instance =
        new Lazy<OpenJTalkFrontend>(() => new OpenJTalkFrontend(dictPath));

    public static OpenJTalkFrontend Instance => _instance.Value;
}
```

---

## 12. まとめ

pyopenjtalkはOpenJTalkのC/C++実装をCython経由でPythonから利用可能にしたラッパーライブラリである。C#/.NET実装においては:

1. **形態素解析**: NMeCab等の既存C#ライブラリで代替可能
2. **NJD処理**: OpenJTalkのC実装をC#に移植する必要がある（最も工数がかかる部分）
3. **辞書**: naist-jdicの拡張フォーマットをそのまま利用可能（MeCabバイナリ辞書対応が必要）
4. **音素変換**: カタカナ→音素変換テーブルで効率的に実装可能
5. **full-context label**: 音声合成で必要な場合はJPCommon相当の実装が必要

pyopenjtalkのコードベースは比較的コンパクト（Cython部分は約500行程度）であり、C側のOpenJTalkソースコード（特にNJD処理モジュール群）を直接参照してC#に移植することが最も確実な方法である。
