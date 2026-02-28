# ESPnet/ESPnet2 日本語G2Pモジュール調査

## 1. 概要

ESPnet（End-to-End Speech Processing Toolkit）は、PyTorchベースのエンドツーエンド音声処理ツールキットであり、音声認識（ASR）、テキスト音声合成（TTS）、音声翻訳、歌声合成（SVS）等の多様なタスクをサポートする。日本語TTSにおいては、pyopenjtalkを基盤としたG2Pモジュールを提供し、韻律情報を含む高度な音素変換を実現している。

- リポジトリ: https://github.com/espnet/espnet
- 論文: "ESPnet2-TTS: Extending the Edge of TTS Research" (2021)

## 2. テキスト前処理パイプライン

### 2.1 全体アーキテクチャ

ESPnet2のTTSパイプラインでは、テキスト前処理が以下の3段階で構成される:

```
テキスト入力 → TextCleaner（正規化） → PhonemeTokenizer（G2P変換） → トークンID列出力
```

- **TextCleaner**: テキストの正規化処理
- **PhonemeTokenizer**: G2P変換（書記素→音素変換）
- **TokenIDConverter**: トークンからIDへのマッピング

### 2.2 TextCleaner（テキスト正規化）

`espnet2/text/cleaner.py`で実装。日本語向けには以下のクリーナーが用意されている:

| クリーナー名 | 説明 |
|-------------|------|
| `jaconv` | 全角⇔半角変換、特殊文字の正規化 |
| `tacotron` | 英語向け正規化（略語展開等） |
| `korean_cleaner` | 韓国語向け |
| `vietnamese` | ベトナム語向け |

日本語TTSでは通常 `--cleaner jaconv` を指定する。

### 2.3 トークナイザーの種別

`espnet2/text/build_tokenizer.py`で以下のトークナイザーを選択可能:

| token_type | トークナイザー | 説明 |
|-----------|--------------|------|
| `char` | CharTokenizer | 文字単位 |
| `word` | WordTokenizer | 単語単位 |
| `phn` | PhonemeTokenizer | 音素単位（G2P使用） |
| `bpe` | SentencepiecesTokenizer | BPEサブワード |
| `whisper` | OpenAIWhisperTokenizer | Whisperモデル用 |

日本語TTSでは `token_type=phn` を使用し、G2Pバックエンドを指定する。

### 2.4 TTSタスクでの統合

`espnet2/tasks/tts.py`の`TTSTask`クラスが`CommonPreprocessor`を構築し、cleaner・tokenizer・G2Pを統合する:

```python
# 実行例
./run.sh --g2p pyopenjtalk_prosody --cleaner jaconv --token_type phn
```

ESPnet2の特徴として、前処理は「on-the-fly」（学習時に動的に実行）であり、事前にダンプファイルを生成する必要がない。

## 3. 日本語G2Pモジュールの詳細

### 3.1 G2Pバックエンドの種類

`espnet2/text/phoneme_tokenizer.py`で実装。日本語向けに5種類のG2Pバックエンドを提供:

| g2p_type | 関数 | 説明 |
|----------|------|------|
| `pyopenjtalk` | `pyopenjtalk_g2p()` | 基本的な音素列出力 |
| `pyopenjtalk_kana` | `pyopenjtalk_g2p_kana()` | カタカナ表記で出力 |
| `pyopenjtalk_accent` | `pyopenjtalk_g2p_accent()` | アクセント情報付き音素列 |
| `pyopenjtalk_accent_with_pause` | `pyopenjtalk_g2p_accent_with_pause()` | アクセント情報＋ポーズ記号 |
| `pyopenjtalk_prosody` | `pyopenjtalk_g2p_prosody()` | 韻律記号付き音素列（推奨） |

### 3.2 pyopenjtalk_g2p（基本）

pyopenjtalkの`g2p()`関数を呼び出し、空白区切りの音素文字列をリストに変換する。

```python
# 入力: "こんにちは"
# 出力: ["k", "o", "N", "n", "i", "ch", "i", "w", "a"]
```

### 3.3 pyopenjtalk_g2p_kana（カナ出力）

テキストをカタカナ読みに変換する。

```python
# 入力: "こんにちは"
# 出力: ["コ", "ン", "ニ", "チ", "ワ"]
```

### 3.4 pyopenjtalk_g2p_accent（アクセント情報付き）

フルコンテキストラベルから音素とアクセント情報（数値）を抽出する。正規表現でラベルからアクセント型・モーラ位置等の情報を取得し、音素と合わせて出力する。

```python
# 入力: "お疲れ様です"
# 出力: ["o", "8", "-7", "ts", "8", "-6", "U", "8", "-6", ...]
```

### 3.5 pyopenjtalk_g2p_accent_with_pause（ポーズ付き）

`pyopenjtalk_g2p_accent`の出力にポーズ記号（`pau`）を追加したバリエーション。

### 3.6 pyopenjtalk_g2p_prosody（韻律記号付き・推奨）

最も高度なG2Pバックエンド。フルコンテキストラベルから音素と韻律制御記号を抽出する。

**論文ベース**: "Prosodic features control by symbols as input of sequence-to-sequence acoustic modeling for neural TTS"（r9y9氏による改良あり）

#### 韻律記号の定義

| 記号 | 意味 | 挿入条件 |
|------|------|---------|
| `^` | 文頭（発話開始） | 先頭のsilenceラベル |
| `$` | 文末（発話終了・平叙文） | 末尾のsilenceラベル（疑問形でない場合） |
| `?` | 文末（疑問文） | 末尾のsilenceラベル（疑問形の場合） |
| `_` | ポーズ（休止） | `pau`ラベル |
| `#` | アクセント句境界 | アクセント句の切れ目 |
| `[` | ピッチ上昇 | アクセント核の上昇位置 |
| `]` | ピッチ下降 | アクセント核の下降位置 |

#### 実装アルゴリズム

```python
def pyopenjtalk_g2p_prosody(text: str, drop_unvoiced_vowels: bool = True) -> List[str]:
    # 1. フルコンテキストラベルを取得
    labels = pyopenjtalk.run_frontend(text)[1]

    for n, lab_curr in enumerate(labels):
        # 2. 現在の音素を正規表現で抽出
        p3 = re.search(r"\-(.*?)\+", lab_curr).group(1)

        # 3. 無声母音の処理（大文字→小文字変換）
        if drop_unvoiced_vowels and p3 in "AEIOU":
            p3 = p3.lower()

        # 4. 特殊ラベルの処理
        if p3 == "sil":
            # 文頭: "^", 文末: "$" or "?"
            ...
        elif p3 == "pau":
            phones.append("_")  # ポーズ
        else:
            phones.append(p3)   # 通常音素

            # 5. アクセント特徴の抽出
            a1 = _numeric_feature_by_regex(r"/A:([0-9\-]+)\+", lab_curr)  # アクセント型
            a2 = _numeric_feature_by_regex(r"\+(\d+)\+", lab_curr)        # モーラ位置
            a3 = _numeric_feature_by_regex(r"\+(\d+)/", lab_curr)         # 句内位置
            f1 = _numeric_feature_by_regex(r"/F:(\d+)_", lab_curr)        # モーラ数

            # 6. 韻律記号の挿入判定
            # アクセント句境界
            if a3 == 1 and a2_next == 1 and p3 in "aeiouAEIOUNcl":
                phones.append("#")
            # ピッチ下降
            elif a1 == 0 and a2_next == a2 + 1 and a2 != f1:
                phones.append("]")
            # ピッチ上昇
            elif a2 == 1 and a2_next == 2:
                phones.append("[")
```

#### 出力例

```python
>>> pyopenjtalk_g2p_prosody("こんにちは。")
['^', 'k', 'o', '[', 'N', 'n', 'i', 'ch', 'i', 'w', 'a', '$']
```

#### バグ修正の経緯（Issue #3716）

アクセント句境界の判定条件に不備があり、子音と母音の区別なく `#` が挿入される問題があった。修正後は母音・撥音(N)・促音(cl)のみで境界判定を行うようになった（PR #3849で修正）。

## 4. 音素体系

### 4.1 ESPnetが使用する日本語音素セット

ESPnetの日本語G2Pは、pyopenjtalk/OpenJTalkの音素体系をそのまま使用する:

| 種別 | 音素 |
|------|------|
| 母音 | a, i, u, e, o |
| 無声母音 | A, I, U, E, O（drop_unvoiced_vowels=Trueで小文字化） |
| 半母音 | y, w |
| 子音 | k, g, s, z, t, d, n, h, b, p, m, r |
| 拗音子音 | ky, gy, ny, hy, by, py, my, ry |
| 特殊子音 | ch, sh, j, f, ts |
| 撥音 | N |
| 促音 | cl（OpenJTalk内部表現） / Q |
| 無音 | sil（文頭・文末）, pau（ポーズ） |

### 4.2 韻律記号セット（pyopenjtalk_prosody使用時）

上記の音素に加えて以下の韻律記号が追加される:

```
^  $  ?  _  #  [  ]
```

合計トークン数: 音素約40種 + 韻律記号7種 = 約47種

## 5. pyopenjtalkとの連携方法

### 5.1 依存関係

ESPnetの日本語G2Pは完全にpyopenjtalkに依存している。pyopenjtalkはOpenJTalkのPythonラッパーであり、以下の機能を提供する:

- `pyopenjtalk.g2p(text)`: 基本的な音素変換
- `pyopenjtalk.run_frontend(text)`: フルコンテキストラベル生成
- `pyopenjtalk.extract_fullcontext(text)`: フルコンテキストラベル抽出

### 5.2 フルコンテキストラベルの活用

ESPnetのG2P関数群は、pyopenjtalkの`run_frontend()`が返すHTS形式のフルコンテキストラベルから音素・韻律情報を正規表現で抽出する。これはOpenJTalkの内部処理パイプライン（形態素解析→NJD→JPCommon→フルコンテキストラベル生成）の出力をそのまま利用している。

### 5.3 ヘルパー関数

```python
def _numeric_feature_by_regex(regex, s):
    """フルコンテキストラベルから数値特徴を正規表現で抽出"""
    match = re.search(regex, s)
    if match:
        return int(match.group(1))
    return -50  # マッチしない場合のデフォルト値

def _extract_fullcontext_label(text):
    """pyopenjtalkのバージョン互換性を考慮したラベル抽出"""
    # pyopenjtalk >= 0.3.0 と旧バージョンの両方に対応
```

## 6. ESPnet独自のG2P改善点

### 6.1 韻律記号の統合

ESPnetの最大の独自改善点は、`pyopenjtalk_g2p_prosody`による韻律記号の統合である。OpenJTalk/pyopenjtalkの基本G2Pは音素列のみを出力するが、ESPnetではフルコンテキストラベルからアクセント・イントネーション情報を抽出し、音素列に韻律記号として埋め込む。

これにより、TTSモデルが音素列だけから自然な韻律を生成できるようになる。

### 6.2 無声母音の統一処理

`drop_unvoiced_vowels`パラメータにより、OpenJTalkが出力する無声母音（大文字: A, I, U, E, O）を通常の母音（小文字）に統一する機能を提供。TTSモデルの学習時に無声/有声の区別が不要な場合に使用する。

### 6.3 複数G2Pバリエーション

用途に応じて5種類のG2Pバックエンドを選択可能にした点もESPnet独自の工夫である:

- 基本研究: `pyopenjtalk`
- カナベース: `pyopenjtalk_kana`
- アクセント研究: `pyopenjtalk_accent` / `pyopenjtalk_accent_with_pause`
- 高品質TTS: `pyopenjtalk_prosody`（推奨）

### 6.4 on-the-fly前処理

ESPnet2では、G2P変換を含むテキスト前処理を学習時に動的に実行する「on-the-fly」方式を採用。これにより事前ダンプファイルの生成が不要になり、異なるG2P設定の実験が容易になった。

### 6.5 歌声合成（SVS）対応

`text2tokens_svs()`メソッドで、歌声合成用の特殊なトークン変換を提供。歌詞のひらがな入力から音素変換を行い、SVSモデルへの入力を生成する。

### 6.6 多言語G2Pの統一インターフェース

日本語以外にも英語（g2p_en, espeak_ng）、中国語（pypinyin）、韓国語（g2pk）、アイスランド語等、35種類以上のG2Pバックエンドを統一的な`PhonemeTokenizer`インターフェースで提供する。

## 7. ニューラルG2Pモデルについて

### 7.1 現状

ESPnetは**ニューラルG2Pモデルを使用していない**。日本語G2Pは完全にルールベース/辞書ベースのpyopenjtalk（OpenJTalk）に依存しており、ニューラルネットワークによるG2P変換は実装されていない。

ESPnet2-TTS論文（2021）でも、G2Pモジュールとして従来のルールベース/辞書ベースのツール（g2p_en、pyopenjtalk、espeak_ng等）のみが言及されている。

### 7.2 エンドツーエンドTTSでのG2P

ESPnetが採用するVITS等のエンドツーエンドTTSモデルは、テキスト→波形の変換を一気に行うが、入力はG2P変換済みの音素列であり、モデル内部にG2P機能は含まれない。

### 7.3 ASRでの音素出力（Issue #3456）

ESPnet ASRモデルで日本語音声から直接音素・アクセント記号を出力する試みが議論されたが（Issue #3456）、専用モデルは実装されず、「ASR出力テキストにG2Pツールを適用する」というパイプラインアプローチが推奨された。

## 8. C#/.NETへの移植可能性

### 8.1 移植対象の分析

ESPnetのG2Pモジュール自体はPython実装であるが、そのコア処理は以下の2層に分離できる:

**Layer 1: pyopenjtalk依存部分（移植困難）**
- フルコンテキストラベルの生成（`run_frontend()`）
- 形態素解析（MeCab + OpenJTalk辞書）
- NJD処理、JPCommon処理

**Layer 2: ESPnet独自処理（移植容易）**
- フルコンテキストラベルからの音素・韻律情報抽出（正規表現ベース）
- 韻律記号の挿入ルール
- 無声母音の統一処理

### 8.2 移植戦略

1. **Layer 2の移植（推奨・高優先度）**:
   - `pyopenjtalk_g2p_prosody`のアルゴリズムはC#で容易に再実装可能
   - フルコンテキストラベルのパース処理は正規表現のみで構成
   - 韻律記号挿入ルールは数十行のロジックで完結

2. **Layer 1の代替**:
   - OpenJTalkの処理パイプラインをC#で再実装する必要がある（本プロジェクトの主目標）
   - jpreprocess（Rust実装）のC#バインディングも選択肢

### 8.3 C#実装時の設計提案

```csharp
// ESPnetのG2Pバリエーションに対応するインターフェース
public interface IG2PConverter
{
    List<string> ConvertToPhonemes(string text);
}

// 基本G2P
public class BasicG2PConverter : IG2PConverter { ... }

// 韻律記号付きG2P（ESPnet pyopenjtalk_prosody相当）
public class ProsodyG2PConverter : IG2PConverter
{
    // フルコンテキストラベルからの韻律情報抽出
    private List<string> ExtractProsodySymbols(FullContextLabel label) { ... }
}

// アクセント情報付きG2P
public class AccentG2PConverter : IG2PConverter { ... }
```

### 8.4 移植の工数見積もり

| 要素 | 難易度 | 説明 |
|------|--------|------|
| 韻律記号抽出ロジック | 低 | 正規表現ベースで約100行 |
| フルコンテキストラベルパーサー | 低〜中 | HTS形式の解析 |
| 無声母音処理 | 低 | 単純な文字変換 |
| フルコンテキストラベル生成 | 高 | OpenJTalkパイプライン全体の再実装が必要 |

### 8.5 価値のある移植ポイント

ESPnetの調査から得られる、C#実装に活用すべき知見:

1. **韻律記号体系**: `^ $ ? _ # [ ]` の7記号によるシンプルな韻律表現は、C#実装でもそのまま採用可能
2. **G2Pバリエーション設計**: 用途に応じた複数のG2P出力形式を提供するアーキテクチャ
3. **フルコンテキストラベルの活用**: OpenJTalkパイプラインの出力を最大限活用する設計思想
4. **on-the-fly処理**: 辞書データの事前変換を最小化する設計

## 9. 参考情報

### 9.1 関連ファイル（ESPnetリポジトリ内）

| ファイル | 役割 |
|---------|------|
| `espnet2/text/phoneme_tokenizer.py` | G2P変換の中核実装 |
| `espnet2/text/cleaner.py` | テキスト正規化 |
| `espnet2/text/build_tokenizer.py` | トークナイザーファクトリー |
| `espnet2/text/abs_tokenizer.py` | トークナイザー抽象基底クラス |
| `espnet2/tasks/tts.py` | TTSタスク定義（前処理統合） |
| `egs2/jsut/tts1/` | 日本語TTSレシピ（JSUT） |

### 9.2 対応TTSモデル

ESPnetの日本語TTSレシピで使用可能なモデル:
- Tacotron2
- Transformer-TTS
- FastSpeech / FastSpeech2
- Conformer FastSpeech2
- VITS（推奨）
- JETS

### 9.3 参考論文・リソース

- ESPnet2-TTS論文: https://arxiv.org/abs/2110.07840
- "Prosodic features control by symbols as input of sequence-to-sequence acoustic modeling for neural TTS"（韻律記号アルゴリズムの元論文）
- pyopenjtalk: https://github.com/r9y9/pyopenjtalk
- ESPnet TTS公式ドキュメント: https://espnet.github.io/espnet/recipe/tts1.html
- ESPnet2テキスト処理: https://espnet.github.io/espnet/guide/espnet2/text/
