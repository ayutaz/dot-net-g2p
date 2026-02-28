# VOICEVOX エンジンのG2P処理調査

## 1. 概要

VOICEVOX は無料の中品質テキスト音声合成ソフトウェアである。エディタ（GUI）とエンジン（バックエンド）が分離されたアーキテクチャを採用し、エンジンはHTTPサーバーとしてローカルPCで動作する。

- リポジトリ: https://github.com/VOICEVOX/voicevox_engine
- 言語: Python
- ライセンス: LGPL-3.0（エンジン）、MIT（Core）

VOICEVOXのG2P処理は **pyopenjtalk をベースとしつつ、独自の拡張**（英語カタカナ変換、AquesTalk風記法、ユーザー辞書）を加えたものである。

## 2. ソースコード構造

### 2.1 主要ディレクトリ

```
voicevox_engine/
├── app/                    # FastAPI アプリケーション・ルーター
│   └── routers/
│       └── tts_pipeline.py # TTS関連APIエンドポイント
├── core/                   # VOICEVOX Core アダプタ
│   └── core_adapter.py     # Core推論API呼び出し
├── tts_pipeline/           # テキスト→音声パイプライン（G2P中核）
│   ├── text_analyzer.py    # フルコンテキストラベル→アクセント句変換
│   ├── phoneme.py          # 音素定義・Phonemeクラス
│   ├── mora_mapping.py     # モーラ⇔音素マッピング（OpenJTalk由来）
│   ├── kana_converter.py   # AquesTalk風記法パーサ
│   ├── katakana_english.py # 英単語→カタカナ変換
│   ├── njd_feature_processor.py  # NJD特徴処理・pyopenjtalk連携
│   ├── model.py            # データモデル（Mora, AccentPhrase等）
│   ├── tts_engine.py       # TTSエンジン本体
│   ├── audio_postprocessing.py   # 音声後処理
│   ├── connect_base64_waves.py   # 波形結合
│   └── song_engine.py      # 歌唱合成エンジン
├── user_dict/              # ユーザー辞書機能
│   ├── model.py            # 辞書データモデル
│   ├── user_dict_word.py   # 単語バリデーション
│   └── user_dict_manager.py # 辞書CRUD・OpenJTalk辞書連携
└── ...
```

### 2.2 G2P関連モジュールの役割

| モジュール | 役割 |
|-----------|------|
| `njd_feature_processor.py` | pyopenjtalkを呼び出してテキスト→フルコンテキストラベル変換 |
| `text_analyzer.py` | フルコンテキストラベル→アクセント句・モーラ構造に変換 |
| `phoneme.py` | 音素体系の定義、音素ID・one-hotベクトル生成 |
| `mora_mapping.py` | カタカナ⇔音素の双方向マッピング |
| `kana_converter.py` | AquesTalk風記法のパース・生成 |
| `katakana_english.py` | 英単語のカタカナ読み変換 |
| `tts_engine.py` | 全体統合、Core推論呼び出し |

## 3. テキスト前処理パイプライン

### 3.1 全体フロー

```
テキスト入力
    │
    ▼
[1] pyopenjtalk.run_frontend(text)  ← MeCab形態素解析 + NJD処理
    │ NjdFeature列を取得
    ▼
[2] 英単語カタカナ変換（オプション）
    │ 未知語の英字をkanalizerでカタカナ化
    ▼
[3] 英単語間スペース除去
    │ アルファベット間のpauを削除
    ▼
[4] pyopenjtalk.make_label(features)  ← フルコンテキストラベル生成
    │ HTSフォーマットのラベル列
    ▼
[5] フルコンテキストラベル解析（text_analyzer.py）
    │ 正規表現でラベルをパース
    ▼
[6] アクセント句生成
    │ モーラ構造・アクセント位置・ポーズ情報を構築
    ▼
[7] Core推論（音素長・音高・波形）
    │
    ▼
音声波形出力
```

### 3.2 ステップ詳細

#### ステップ1: pyopenjtalkによるフロントエンド処理

`njd_feature_processor.py` の `text_to_full_context_labels()` 関数が起点。

```python
# pyopenjtalkを呼び出してNJD特徴を取得
njd_features = pyopenjtalk.run_frontend(text)
```

`pyopenjtalk.run_frontend()` は内部で以下を実行:
1. MeCab辞書による形態素解析
2. NJD（日本語処理）: 読み生成、数字読み、アクセント句結合等
3. NjdFeatureオブジェクト列を返却

NjdFeatureクラスは14フィールドを持つ:

| フィールド | 説明 |
|-----------|------|
| `string` | 表層形 |
| `pos` | 品詞 |
| `pos_group1` | 品詞細分類1 |
| `pos_group2` | 品詞細分類2 |
| `pos_group3` | 品詞細分類3 |
| `ctype` | 活用型 |
| `cform` | 活用形 |
| `orig` | 原形 |
| `read` | 読み |
| `pron` | 発音 |
| `acc` | アクセント位置 |
| `mora_size` | モーラ数 |
| `chain_rule` | チェーンルール |
| `chain_flag` | チェーンフラグ |

#### ステップ2: 英単語カタカナ変換

`katakana_english.py` で実装。`enable_katakana_english` オプションが有効時に動作。

処理フロー:
1. NjdFeature列を走査し、未知語（品詞が「フィラー」）かつ半角英字の単語を検出
2. キャメルケース分割: `VoiceVox` → `Voice`, `Vox`
3. 変換判定:
   - 1文字 → 変換しない（文字マッピング表を使用）
   - 全て大文字 → 変換しない（文字マッピング表を使用: `VOICE` → `ブイオーアイシーイー`）
   - それ以外 → `kanalizer` ライブラリで自然なカタカナに変換: `voice` → `ボイス`
4. 変換結果を `NjdFeature.from_english_kana()` で新しいNjdFeatureに変換

アルファベット文字マッピング表（OpenJTalk由来）:
```
A→エー, B→ビー, C→シー, D→ディー, E→イー, F→エフ, ...
```

#### ステップ3: 英単語間スペース除去

`_remove_pau_space_between_alphabet()` 関数で、英字に挟まれた全角スペース（pauseとして認識される）を削除し、英語フレーズの読みの自然性を向上させる。

#### ステップ4: フルコンテキストラベル生成

```python
labels = pyopenjtalk.make_label(njd_features)
```

HTSフォーマットのフルコンテキストラベルを生成。各ラベルは音素・モーラ・アクセント句・ブレスグループなどの情報を含む。

#### ステップ5-6: ラベル解析とアクセント句生成

`text_analyzer.py` の `full_context_labels_to_accent_phrases()` 関数が実行:

1. **ラベルパース**: `_Label.from_feature()` メソッドが正規表現でHTSラベルを解析
   - 音素（子音・母音）
   - ポーズ判定
   - モーラインデックス
   - アクセント位置
   - 疑問形フラグ
2. **ポーズ除外**: sil/pauラベルをフィルタ
3. **グループ化**: アクセント句単位でラベルをグループ化
4. **モーラ抽出**: 音素をモーラに変換（`mora_to_text()` でカタカナに）
5. **AccentPhrase生成**: モーラ列、アクセント位置、ポーズ有無、疑問形フラグを含むオブジェクト

## 4. OpenJTalkの利用方法と独自拡張

### 4.1 pyopenjtalkの利用

VOICEVOXは `pyopenjtalk` ライブラリを通じてOpenJTalkの機能を利用する:

| pyopenjtalk API | 用途 |
|----------------|------|
| `run_frontend(text)` | テキスト→NJD特徴（形態素解析+日本語処理） |
| `make_label(features)` | NJD特徴→フルコンテキストラベル |
| `mecab_dict_index()` | ユーザー辞書のコンパイル |

VOICEVOXはpyopenjtalkの **フロントエンド部分のみ** を使用し、音声合成（バックエンド）は独自のニューラルネットワークモデル（VOICEVOX Core）で行う。

### 4.2 独自拡張

VOICEVOXがOpenJTalkに対して独自に追加した機能:

1. **英単語カタカナ変換**: `kanalizer` ライブラリによる自然な英語→カタカナ変換
2. **AquesTalk風記法**: カタカナベースの直接入力記法
3. **ユーザー辞書**: JSON形式の辞書管理、OpenJTalk辞書へのコンパイル統合
4. **疑問文ピッチ上昇**: 疑問符付き文末に自動的にピッチ上昇を付与
5. **歌唱合成対応**: 楽譜情報からの音声合成

### 4.3 pyopenjtalk-plus の検討

VOICEVOXチームは `pyopenjtalk-plus`（改良版pyopenjtalk）への移行を検討中:

- **辞書の手動修正**: 発音フィールドの事前編集で長音化未対応ケースに対応
- **SudachiPy統合**: 形態素解析をSudachiPyで補正し、文脈依存読みの精度向上
- **精度向上**: ROHAN 4600データセットでBLEUスコア約0.19〜0.45%改善
- **速度トレードオフ**: 推論速度が約179倍遅い（72 it/s vs 12,895 it/s）

## 5. ユーザー辞書機能の実装

### 5.1 データモデル

```
UserDictWord:
  - surface: 表層形（半角英字は全角に自動変換）
  - pronunciation: 発音（カタカナのみ）
  - accent_type: アクセント型（モーラ数以下の値）
  - mora_count: モーラ数（自動計算可能）
  - priority: 優先度（0〜10、デフォルト5）
  - context_id: 文脈ID（デフォルト1348）
  - part_of_speech: 品詞（固有名詞/普通名詞/動詞/形容詞/接尾辞）
  - part_of_speech_detail_1〜3: 品詞細分類
  - inflectional_type: 活用型
  - inflectional_form: 活用形
```

### 5.2 辞書管理フロー

```
[JSON辞書ファイル] ←→ [user_dict_manager.py]
                          │
                          ▼
                    [デフォルト辞書CSV + ユーザー辞書] 統合
                          │
                          ▼
                    [pyopenjtalk.mecab_dict_index()] コンパイル
                          │
                          ▼
                    [OpenJTalkグローバル辞書を更新]
```

### 5.3 バリデーション

- 発音はカタカナのみ許可
- 捨て仮名の連続は検証
- 改行、null文字、カンマ、ダブルクォートは禁止
- アクセント型はモーラ数以下であること
- 優先度は0〜10の整数範囲

### 5.4 優先度とコストの変換

MeCabのコスト値とVOICEVOXの優先度（0〜10）は相互変換される。v0.12以前の辞書との後方互換性も維持。

### 5.5 スレッドセーフティ

2つのmutexロックにより並行アクセスを制御:
- `mutex_user_dict`: JSON読み書き用
- `mutex_openjtalk_dict`: OpenJTalk辞書更新用

## 6. アクセント修正機能

### 6.1 APIエンドポイント

VOICEVOXは以下のアクセント関連APIを提供:

| エンドポイント | 機能 |
|--------------|------|
| `POST /accent_phrases` | テキストからアクセント句を抽出 |
| `POST /mora_data` | アクセント句から音素長・音高を更新 |
| `POST /mora_length` | 音素長のみ更新 |
| `POST /mora_pitch` | 音高のみ更新 |

### 6.2 アクセント句データモデル

```
AccentPhrase:
  - moras: [Mora]           # モーラのリスト
  - accent: int              # アクセント核位置（1-indexed）
  - pause_mora: Mora | None  # 末尾ポーズモーラ
  - is_interrogative: bool   # 疑問文フラグ

Mora:
  - text: str                # カタカナ表記
  - consonant: str | None    # 子音音素
  - consonant_length: float  # 子音長
  - vowel: str               # 母音音素
  - vowel_length: float      # 母音長
  - pitch: float             # 音高（F0）
```

### 6.3 アクセント修正の流れ

1. `audio_query` でテキストからアクセント句を自動生成
2. ユーザーがエディタUIでアクセント位置を手動修正
3. 修正後の `accent_phrases` を `mora_data` に再送信して音素長・音高を再計算
4. `synthesis` で最終音声を生成

### 6.4 疑問文処理

疑問符付きの文末に自動的にピッチ上昇を付与する。`is_interrogative` フラグで制御。

## 7. 音素体系

### 7.1 VOICEVOXの音素リスト（48音素）

VOICEVOX（`phoneme.py`）で定義される音素リスト:

```
pau, A, E, I, N, O, U, a, b, by, ch, cl, d, dy, e, f, g, gw, gy,
h, hy, i, j, k, kw, ky, m, my, n, ny, o, p, py, r, ry, s, sh, t,
ts, ty, u, v, w, y, z
```

### 7.2 音素分類

#### 基本母音（BaseVowel）
```
pau（ポーズ）, N（撥音「ん」）, a, cl（促音「っ」）, e, i, o, u
```

#### 母音（Vowel）= BaseVowel + 無声母音
```
pau, N, a, cl, e, i, o, u, A, E, I, O, U
```

無声母音（大文字）は無声化した母音を表す。例: `hI` = 「ヒ」の無声化形。

#### 子音（Consonant）
```
b, by, ch, d, dy, f, g, gw, gy, h, hy, j, k, kw, ky,
m, my, n, ny, p, py, r, ry, s, sh, t, ts, ty, v, w, y, z
```

### 7.3 OpenJTalkとの差異

| 項目 | OpenJTalk | VOICEVOX |
|------|-----------|----------|
| 無声母音表記 | 小文字のまま（コンテキスト情報で区別） | 大文字（A,E,I,O,U）で明示 |
| 無音記号 | `sil`（文頭末）, `pau`（ポーズ） | 全て `pau` に統一（silをpauに変換） |
| 促音表記 | `q` | `cl`（close） |
| 音素数 | コンテキスト依存 | 48個の固定リスト |
| `gw`, `kw` | 標準OpenJTalkでは稀 | 明示的に定義 |
| `dy`, `ty` | 一部辞書のみ | 明示的に定義 |
| `v` | 標準では未定義が多い | 外来語用に定義 |

### 7.4 特殊な音素処理

- `sil` → `pau` への自動変換（Phonemeクラス初期化時）
- 無声母音の判定: `is_unvoiced_mora_tail()` メソッドで大文字母音を検出
- モーラ末尾判定: `is_mora_tail()` で母音・撥音・促音を判定
- one-hotベクトル: 48次元のベクトルで各音素をエンコード

## 8. テキスト正規化処理

### 8.1 OpenJTalk側の正規化

pyopenjtalkの `run_frontend()` 内で実行される正規化:
- 漢字→読み変換（MeCab辞書ベース）
- 数字読み変換
- 記号処理
- 助詞の読み補正（「は」→「わ」等）

### 8.2 VOICEVOX独自の正規化

1. **英単語カタカナ変換**: 辞書にない英字をkanalizerで変換
2. **キャメルケース分割**: `VoiceVox` → `Voice` + `Vox`
3. **全大文字判定**: `API` は文字単位で「エーピーアイ」
4. **英字間スペース除去**: 英単語間の不要なポーズを削除
5. **半角→全角変換**: ユーザー辞書の表層形で自動変換

### 8.3 AquesTalk風記法

テキストの代わりにカタカナベースの記法を直接入力可能:

| 記号 | 意味 |
|------|------|
| `_` | 無声化（直後の母音を無声化） |
| `'` | アクセント核位置 |
| `/` | アクセント句境界（ポーズなし） |
| `、` | アクセント句境界（ポーズあり） |
| `？` | 疑問文（文末ピッチ上昇） |

例: `ボ'イスボ'ックス` → 「ボイスボックス」（各句のアクセント位置を明示）

処理フロー:
1. 区切り文字（`/`、`、`）でアクセント句に分割
2. 各句内でlongest matchアルゴリズムでモーラに分解
3. `'` の位置からアクセント核を特定
4. `_` がある場合、直後の母音を無声化（大文字に）

## 9. 音素列→音声合成の流れ

### 9.1 3段階ニューラルネットワーク

VOICEVOX Coreは3つのモデルで音声を合成する:

```
[音素列 + アクセント情報]
        │
        ▼
[Yukarin-S] ──→ 音素ごとの長さ（duration）
        │
        ▼
[Yukarin-SA] ──→ モーラごとの音高（F0）
        │
        ▼
[Decoder] ──→ 音声波形
```

#### Yukarin-S（音素長予測）
- 入力: 音素列（one-hotベクトル）、話者ID
- 出力: 各音素の時間長
- Core API: `safe_yukarin_s_forward()`

#### Yukarin-SA（音高予測）
- 入力: 母音音素列、子音音素列、アクセント開始位置、アクセント終了位置、ピッチ情報、話者ID
- 出力: モーラごとのF0値
- Core API: `safe_yukarin_sa_forward()`

#### Decoder（波形生成）
- 入力: フレーム単位の音素one-hot、F0値
- 出力: 音声波形（float32配列）
- Core API: `safe_decode_forward()`

### 9.2 パラメータ適用順序

`tts_engine.py` の `_query_to_decoder_feature()` 内で以下の順序でパラメータを適用:

1. 前後無音（padding silence）追加
2. 無音時間調整（`prePhonemeLengthScale`, `postPhonemeLengthScale`）
3. 速度スケール（`speedScale`）→ 音素長に反映
4. ピッチスケール（`pitchScale`）→ F0に反映
5. 抑揚スケール（`intonationScale`）→ F0の変動幅に反映

### 9.3 後処理

`audio_postprocessing.py` で以下を実行:
1. 音量スケール適用（`volumeScale`）
2. サンプリングレート変換（`soxr`ライブラリ使用）
3. ステレオ変換（オプション）

## 10. VOICEVOX Coreとの連携

### 10.1 Core概要

- リポジトリ: https://github.com/VOICEVOX/voicevox_core
- 実装言語: Rust（79.8%）
- 提供形態: C API（動的ライブラリ）、Python wheels
- ライセンス: MIT
- 他言語バインディング: Go, C#, Ruby, Swift, Scala（コミュニティ提供）

### 10.2 CoreAdapterクラス

`core_adapter.py` がCoreとの通信を管理:

```python
class CoreAdapter:
    # スレッドセーフなmutexロックで保護

    def safe_yukarin_s_forward(phoneme_list, style_id):
        """音素列から音素長を予測"""
        # 前後にpau（無音）を付加して推論、結果からpauを除去

    def safe_yukarin_sa_forward(vowel_phoneme_list, consonant_phoneme_list,
                                  start_accent_list, end_accent_list,
                                  start_accent_phrase_list, end_accent_phrase_list,
                                  style_id):
        """モーラごとの音高(F0)を予測"""
        # 6つのアクセント関連リストを入力

    def safe_decode_forward(phoneme, f0, style_id):
        """フレーム単位の音素+F0から波形を生成"""
        # サンプリングレートとセットで返却
```

### 10.3 歌唱合成API

Coreは歌唱合成用の追加メソッドも提供:

| メソッド | 機能 |
|---------|------|
| `safe_predict_sing_consonant_length_forward` | 子音長予測 |
| `safe_predict_sing_f0_forward` | 歌唱音高予測 |
| `safe_predict_sing_volume_forward` | 音量予測 |
| `safe_sf_decode_forward` | 歌唱波形生成 |

### 10.4 デバイスサポート

CoreAdapterは以下のデバイスをサポート:
- CPU
- CUDA（NVIDIA GPU）
- DirectML（Windows GPU）

### 10.5 C#バインディング

VOICEVOX Coreにはコミュニティ提供のC#バインディングが存在し、C#からCoreの推論APIを直接呼び出すことが可能。ただし、G2P（テキスト→音素変換）はPython側（pyopenjtalk）で処理されるため、C#で完結するにはG2Pの再実装が必要。

## 11. dot-net-g2pプロジェクトへの示唆

### 11.1 参考にすべき設計

1. **音素体系**: VOICEVOXの48音素リスト（無声母音の大文字表記、sil→pau統一）は合理的
2. **モーラマッピング**: `mora_mapping.py` の247種カタカナ⇔音素マッピングは直接移植可能
3. **AccentPhrase/Moraモデル**: データ構造設計が明確で参考になる
4. **AquesTalk風記法**: テスト用入力やデバッグに有用な中間表現

### 11.2 C#実装における差異点

1. **pyopenjtalkの代替**: MeCab + NJD処理をC#で再実装する必要がある
2. **kanalizerの代替**: 英語→カタカナ変換は別途C#実装が必要
3. **Core推論は不要**: dot-net-g2pはG2P（テキスト→音素変換）のみが対象
4. **辞書管理**: JSON形式のユーザー辞書管理は参考にできるが、MeCab辞書コンパイルはC#で対応が必要

### 11.3 VOICEVOXの制約・課題

1. **pyopenjtalk依存**: OpenJTalkのC/C++バイナリに依存するため、他環境への移植が困難
2. **辞書精度**: OpenJTalkの辞書では文脈依存読みの精度が限定的（pyopenjtalk-plusで改善検討中）
3. **速度 vs 精度**: SudachiPy統合による精度改善は約179倍の速度低下を伴う
4. **英語対応**: kanalizerによる英語カタカナ変換は限定的な品質

### 11.4 モーラマッピング表の活用

`mora_mapping.py` の247種マッピングはOpenJTalkのソースコード由来（BSDライセンス）であり、dot-net-g2pプロジェクトで直接活用可能。主要なマッピング例:

```
ア → (None, "a")
カ → ("k", "a")
キャ → ("ky", "a")
シ → ("sh", "i")
チ → ("ch", "i")
ツ → ("ts", "u")
フ → ("f", "u")
ン → (None, "N")
ッ → (None, "cl")
```

## 12. 参考リンク

- VOICEVOX Engine: https://github.com/VOICEVOX/voicevox_engine
- VOICEVOX Core: https://github.com/VOICEVOX/voicevox_core
- pyopenjtalk: https://github.com/r9y9/pyopenjtalk
- pyopenjtalk-plus: https://github.com/tsukumijima/pyopenjtalk-plus
- VOICEVOX音声合成エンジン紹介（開発者ブログ）: https://blog.hiroshiba.jp/voicevox-engine-introduction/
- kanalizer: 英語→カタカナ変換ライブラリ
- OpenJTalk label getter: https://github.com/Hiroshiba/openjtalk-label-getter
