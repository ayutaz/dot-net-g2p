# VOICEVOX派生・関連TTSソフトウェアのG2P処理調査

## 1. VOICEVOXのG2P処理アーキテクチャ（基盤）

VOICEVOX派生ソフトウェアを理解するためには、まずVOICEVOX本体のG2P処理フローを把握する必要がある。

### 1.1 全体アーキテクチャ

VOICEVOXは3つのモジュールで構成される:

- **VOICEVOX Editor**: ユーザーインターフェース
- **VOICEVOX Engine**: 音声合成エンジン（HTTPサーバー）
- **VOICEVOX Core**: 音声合成コアライブラリ

### 1.2 TTS処理フロー

```
日本語テキスト
    |
    v
[OpenJtalk.analyze] --- テキスト解析（形態素解析 + NJD処理）
    |
    v
アクセント句の列（音高・音素長なし）
    |
    v
[Synthesizer.replace_phoneme_length] --- 音素長の推定
    |
    v
[Synthesizer.replace_mora_pitch] --- モーラ音高の推定
    |
    v
AudioQuery（アクセント句 + 音高 + 音素長）
    |
    v
[Synthesizer.synthesis] --- 波形合成
    |
    v
WAV音声ファイル
```

### 1.3 G2P処理の詳細

VOICEVOXのG2P処理はOpenJTalkに依存しており、以下の流れで処理される:

1. **テキスト入力**: `/audio_query` エンドポイントでテキストを受け付け
2. **形態素解析**: OpenJTalk内蔵のMeCab + naist-jdic辞書で解析
3. **NJD処理**: 数字読み、アクセント結合等のルール処理
4. **アクセント句生成**: 音素列とアクセント位置を含むアクセント句の列を生成
5. **モーラ分割**: 各アクセント句をモーラ（子音+母音の単位）に分割

### 1.4 VOICEVOXの音素セット

VOICEVOXは独自の音素セットを定義しており、エディタとエンジンの互換性を保証している。

### 1.5 主要APIエンドポイント

| エンドポイント | 機能 |
|---|---|
| `/audio_query` | テキストから音素・アクセント情報を生成 |
| `/synthesis` | AudioQueryから音声波形を合成 |
| `/accent_phrases` | テキストからアクセント句を生成 |
| `/mora_data` | アクセント句にモーラデータ（音高・音素長）を付与 |

### 1.6 Rust移行の取り組み

VOICEVOXはコアの実装言語をC++からRustへ移行する取り組みを進めている:

- **open_jtalk-rs**: OpenJTalkのRustバインディング（VOICEVOX開発用）
- Rustへの移行により、開発体験の改善とクロスプラットフォーム対応の強化を目指している

## 2. Coeiroink

### 2.1 概要

- **開発者**: シロワニさん
- **URL**: https://coeiroink.com/
- **初版**: 2021年
- **ライセンス**: 無料（利用規約あり）

### 2.2 バージョンの変遷とG2P処理

#### Coeiroink v1

- VOICEVOXのエディタとエンジンをベースにしたフォーク
- G2P処理はVOICEVOXと同一（OpenJTalk/pyopenjtalkベース）
- 形態素解析 → NJD処理 → 音素変換 → アクセント情報付与

#### Coeiroink v2（現行版）

- **独自のUIに移行**: VOICEVOXエディタから完全に独立
- **独自のエンジン開発**: VOICEVOX互換APIから離れ、独自路線へ
- **API互換性の喪失**: VOICEVOX APIとは異なるAPIを採用
  - VOICEVOXはf0（基本周波数）も含めてAPIで推論
  - Coeiroinkはテキスト情報のみで推論（処理の最適化が困難）
- **ブリッジの必要性**: VOICEVOXのマルチエンジン機能で使用するには `coeiroink-v2-bridge` が必要

### 2.3 G2P処理の特徴

- v1時代はOpenJTalk依存でVOICEVOXと同等
- v2ではテキストから直接音声を推論するアプローチを採用
- 内部的なG2P処理の詳細は非公開だが、音声モデルの精度が高く、イントネーション調整機能がなくても自然な音声を生成できるとされる

### 2.4 技術的特徴

- 「つくよみちゃん」等の公式キャラクターを提供
- ユーザーが公開している音声モデルを追加可能
- 高精度な音声生成により、イントネーション手動調整の必要性が低い

## 3. SHAREVOX

### 3.1 概要

- **GitHub**: https://github.com/SHAREVOX
- **説明**: 「無料で使える、声を作れるテキスト読み上げソフトウェア」
- **構成**: Editor + Engine + Core の3層構造（VOICEVOXと同様）

### 3.2 アーキテクチャ

SHAREVOX EngineはVOICEVOX Engineのフォークであり、HTTPサーバーとして動作する。

主要コンポーネント:
- **sharevox_engine**: 音声合成エンジン（HTTPサーバー）
- **sharevox**: エディタ（UI）
- **sharevox_core**: 合成コア

### 3.3 G2P処理

- VOICEVOXと同様のG2P処理パイプライン（OpenJTalk/pyopenjtalkベース）
- AquesTalk風の表記を使用（全てのカナはカタカナで記述）
- アクセント位置は特定の記号で表現

### 3.4 APIエンドポイント

VOICEVOXと互換性のあるAPI:
- `/audio_query`: 合成パラメータ生成
- `/synthesis`: 音声出力
- `/accent_phrases`: 読み修正
- `/speaker_info`: 話者情報取得

### 3.5 VOICEVOXとの差異

- 基本的なG2P処理はVOICEVOXと同一
- 独自の音声モデルと話者を提供
- 声の作成機能（ユーザーが独自音声モデルを作成可能）

## 4. LMROID

### 4.1 概要

- **開発者**: nohoshio
- **公開日**: 2022年3月3日
- **特徴**: VOICEVOXのシステムをベースにした無料AIスピーチ合成ソフト
- **制約**: 商用利用不可（VOICEVOX派生の中でユニーク）

### 4.2 G2P処理

- VOICEVOXのUIとエンジンを使用
- G2P処理はVOICEVOXと完全に同一（OpenJTalk依存）
- 独自のG2P改善は報告されていない

### 4.3 機能的特徴

- VOICEVOXと比較して長さ調整・イントネーション調整等の一部機能が制限されている
- 独自の音声キャラクターを提供

## 5. ITVOICE

### 5.1 概要

- **開発者**: iTahobi
- **公開日**: 2022年12月10日
- **特徴**: VOICEVOX互換エンジンを使用

### 5.2 G2P処理

- VOICEVOXのエンジンを使用しており、G2P処理は同一
- OpenJTalk/pyopenjtalkベースの処理パイプライン
- 独自のG2P改善は報告されていない

## 6. AivisSpeech（注目すべき新興派生）

### 6.1 概要

- **プロジェクト**: Aivis Project
- **GitHub**: https://github.com/Aivis-Project/AivisSpeech-Engine
- **説明**: 「AI Voice Imitation System - Text to Speech Engine」
- **基盤**: VOICEVOX ENGINEベース

### 6.2 G2P処理の大幅改善

AivisSpeechはVOICEVOX派生の中で最も注目すべきG2P改善を実現している:

#### 高度なテキスト正規化
- 英単語・ローマ字・記号混じりのテキストを自然に読み上げ
- 英語の固有名詞、人名、CamelCase複合語を「カタカナ英語」として自然に読み上げ
- 日付、数値等のフォーマットにも対応

#### 内蔵辞書の大幅拡張
- 新語、ネットスラング
- アニメ・漫画の作品名
- インターネット用語
- 医療用語
- 人名
- 四字熟語
- 会社名・団体名・固有名詞

#### Style-Bert-VITS2より高性能なG2P
- AivisSpeechは独自のより高性能なG2P処理を実装
- VOICEVOXで問題だった英語単語の発音問題を改善

### 6.3 VOICEVOX互換性

- VOICEVOX Engine互換APIを提供
- VOICEVOXの最新バージョンに追従しつつ、最小限の変更に留める方針
- モーラデータの処理方法に独自の改良（記号をモーラとして記録）

## 7. ttslearn（Pythonで学ぶ音声合成）

### 7.1 概要

- **開発者**: r9y9（山本龍一）
- **URL**: https://r9y9.github.io/ttslearn/
- **GitHub**: https://github.com/r9y9/ttslearn
- **説明**: 「Pythonで学ぶ音声合成」の教材ライブラリ

### 7.2 G2P処理アーキテクチャ

ttslearnはOpenJTalkフロントエンドを用いた教育的なG2P実装を提供する。

#### 基本的な音素変換

```python
import pyopenjtalk
phones = pyopenjtalk.g2p("箸が")
# => "h a sh i g a"
```

#### 韻律記号付き音素列（pp_symbols）

フルコンテキストラベルから韻律情報を含む音素列を生成:

```python
labels = pyopenjtalk.extract_fullcontext("箸が")
symbols = pp_symbols(labels)
# => "^ h a ] sh i g a $"
```

### 7.3 韻律記号体系

| 記号 | 意味 |
|------|------|
| `^` | 発話開始 |
| `$` | 発話終了 |
| `?` | 疑問文終了 |
| `_` | ポーズ |
| `#` | アクセント句境界 |
| `[` | ピッチ上昇位置 |
| `]` | ピッチ下降位置 |

### 7.4 ボキャブラリ体系

52個のシンボルで構成:
- パディング記号（`~`）: 1個
- 韻律記号: 7個（`^ $ ? _ # [ ]`）
- 音素: 43個（母音、子音、`pau`、`sil`等）
- 特殊音素: `N`（撥音）、`cl`（促音）等

### 7.5 text_to_sequence関数

音素+韻律記号のリストを数値の系列に変換:

```python
text_to_sequence(["^", "m", "i", "[", "z", "o", "$"])
# => [1, 31, 27, 6, 49, 35, 2]
```

### 7.6 C#実装への示唆

ttslearnの韻律記号付き音素列は、日本語TTSの前処理として標準的なアプローチであり、C#実装でも同様のpp_symbols関数を実装することが有用。

## 8. pyopenjtalk派生ライブラリのG2P改善

### 8.1 pyopenjtalk-plus

- **GitHub**: https://github.com/tsukumijima/pyopenjtalk-plus
- **PyPI**: https://pypi.org/project/pyopenjtalk-plus/

#### 主な改善点

1. **辞書の改善**
   - wheel同梱の独自カスタム辞書（初期化時のダウンロード不要）
   - n5-suzuki版mecab-naist-jdicベース
   - jpreprocess/naist-jdicの改良点を統合
   - 「百合」の読み修正等の具体的な辞書データ修正
   - IPAdicパッチによる読み推定精度の回帰修正

2. **SudachiPy統合による読み補正**
   - OpenJTalkの辞書マージによる形態素解析能力低下を補償
   - 複数読みの漢字（例: 「何」→「なん」/「なに」）をSudachiPyで再解析
   - 読み判定の機械学習モデルをONNX形式に変換しONNXRuntimeで推論

3. **アクセント推定の拡張**
   - `extract_fullcontext()`に加え、`run_frontend()`と`g2p()`にも`run_marine=True`オプションを追加
   - marine（DNN-basedアクセント推定）をより広範なAPIで利用可能
   - ただしデフォルトの学習済みモデルはOpenJTalkのアクセント推定より精度が低い場合がある
   - より高性能なmarine-plusの使用を推奨

4. **プラットフォーム対応**
   - Python 3.11/3.12/3.13/3.14に明示的対応
   - Windows, macOS (x64/arm64), Linux向けのプリビルドwheel提供

### 8.2 pyopenjtalk-mod

VOICEVOXコミュニティで議論されたpyopenjtalk-plusの派生版:

#### 特徴
- 辞書データベースを約2倍に拡張
- AIモデルによるアクセント推定
- 方言対応

#### 課題と議論の結果
- **処理性能**: pyopenjtalk-plusは約14ms/文で既に遅く、onnxruntime依存が増加
- **著作権リスク**: IME辞書等の二次著作物から生成された辞書ファイルの法的問題
- **辞書品質**: 機械的に生成されたエントリは継続的な手動メンテナンスが必要
- **結論**: 提案はクローズされ、より限定的な機能追加の方針へシフト

### 8.3 VOICEVOXにおけるpyopenjtalk-plus採用の検討（Issue #1486）

#### ベンチマーク結果
- ROHAN 4600データセットで音素レベルの精度をBLEUスコアで比較
- pyopenjtalk-plusの性能向上は約0.1〜0.45%程度
- 推論速度が大幅に低下: 72.06 it/s vs 12,895.64 it/s（約180倍遅い）

#### 長音処理の差異
- 「セイ」vs「セー」等、長音表記の機械的な変換方法が異なる

#### 文脈対応の改善
- SudachiPyで形態素解析を再実施し、文脈に応じた読み方の補正を実施
- OpenJTalkの辞書マージがもたらした形態素解析能力の低下を補償

#### 現状
- 2025年時点で非アクティブとラベル付けされ、実装判断は保留中

## 9. OpenJTalk G2Pの既知の問題点

### 9.1 辞書関連の問題

| 問題 | 詳細 |
|------|------|
| 辞書未登録語 | 辞書にない漢字は読みが生成できない（例: 「雯」→無音） |
| 英語単語 | 辞書未登録の英単語は1文字ずつアルファベットで読み上げ（例: "Python" → "ピーワイティーエイチオーエヌ"） |
| 同音異義語 | 文脈判断が不十分（例: 「十分」→「ジュップン」/「ジューブン」） |
| 形態素境界 | 辞書の形態素結合パターンにより読みが変わる（例: 「本日は」→「本日+は」/「本+日+は」） |
| ユーザー辞書 | OpenJTalk単体ではユーザー辞書の追加が困難（pyopenjtalk-plusで改善） |

### 9.2 読み推定精度の問題

- OpenJTalkの出力する音素は意図とずれることが「かなり多い」（VOICEVOXの開発者であるHihoの指摘）
- 手動検証が推奨される、または学習データを1000サンプル以上に増やすことで軽減
- MeCabの形態素解析は「辞書登録形が全て」であり、口語やくだけた表現に弱い

### 9.3 アクセント推定の問題

- デフォルトのアクセント推定は辞書ベースで、未知語に対しては不正確
- marine（DNN-basedアクセント推定）は改善の可能性があるが、デフォルトモデルの精度は必ずしもOpenJTalkを上回らない

## 10. G2P精度向上のコミュニティ知見

### 10.1 VOICEVOXコミュニティの取り組み

#### ユーザー辞書による対処
- 設定 → 「読み・アクセント辞書」で単語登録可能
- カタカナで読みを入力し、アクセント調整で自然なアクセントを設定
- 単語の優先度を最大に設定することで強制的に登録した読みとアクセントを使用可能
- 複合語（例: 「NTT東日本」）はアクセント辞書で正しく登録することが困難

#### 英語対応の議論（Issue #1524）
- OpenJTalkの出力に後処理を適用し、英語音素を日本語モーラに変換する案
- g2p-en（英語用G2P）で英語単語を音素に変換後、日本語モーラにマッピング
- 辞書ベースのアプローチは容量増大、大文字小文字の区別等の課題がある

### 10.2 BERTベースのG2P改善（Style-Bert-VITS2）

#### 革新的アプローチ
- 従来のTTSは音素列のみを入力としていた
- Bert-VITS2はBERTを介して「音素列に変換される前の日本語文章の情報」も入力に含める
- これにより同音異義語の文脈判断が大幅に改善

#### Style-Bert-VITS2 JP-Extraの改善
- 日本語の発音とアクセントの改善
- 日本語データでの再学習
- 不要な英語・中国語コンポーネントの削除

#### pyopenjtalk_workerの活用
- TCP socketサーバーとしてpyopenjtalkを初期化
- 辞書データを適用してテキスト処理を実行

### 10.3 テキスト前処理による改善（AivisSpeech）

- 英単語・ローマ字・記号を含むテキストの高度な正規化
- CamelCase複合語のカタカナ英語変換
- 大規模な内蔵辞書による語彙カバレッジの向上

## 11. 各ソフトウェアのG2P処理比較

| ソフトウェア | G2P基盤 | 独自改善 | 辞書 | 英語対応 |
|---|---|---|---|---|
| VOICEVOX | OpenJTalk/pyopenjtalk | ユーザー辞書、open_jtalk-rs | naist-jdic | 限定的（1文字ずつ） |
| Coeiroink v1 | OpenJTalk（VOICEVOXと同一） | なし | naist-jdic | 限定的 |
| Coeiroink v2 | 独自エンジン | テキスト直接推論 | 非公開 | 不明 |
| SHAREVOX | OpenJTalk（VOICEVOXと同一） | なし | naist-jdic | 限定的 |
| LMROID | OpenJTalk（VOICEVOXと同一） | なし | naist-jdic | 限定的 |
| ITVOICE | OpenJTalk（VOICEVOXと同一） | なし | naist-jdic | 限定的 |
| AivisSpeech | VOICEVOX Engine + 独自改善 | テキスト正規化、辞書拡張 | naist-jdic + 独自辞書 | 大幅改善 |
| ttslearn | pyopenjtalk | 韻律記号付き音素列 | naist-jdic | なし |
| Style-Bert-VITS2 | pyopenjtalk + BERT | BERT文脈情報統合 | naist-jdic + カスタム | 部分的 |

## 12. C#実装への示唆

### 12.1 基本方針

VOICEVOX派生ソフトウェアの調査から、以下の知見が得られた:

1. **OpenJTalk依存からの脱却が課題**: 大半の派生がOpenJTalkのG2P精度問題を共有
2. **辞書品質が最重要**: 辞書の拡張・改善が精度向上の最も直接的な手段
3. **テキスト正規化の重要性**: AivisSpeechの成功は高度なテキスト正規化に起因
4. **BERT等のニューラルアプローチ**: 文脈依存のG2P判断に有効だが、計算コストが大きい

### 12.2 推奨実装戦略

1. **jpreprocessの参考**: OpenJTalkのRust再実装であるjpreprocessがNJD処理の参考になる
2. **辞書改善の統合**: pyopenjtalk-plus/jpreprocessで行われた辞書修正をC#実装にも反映
3. **テキスト正規化層の実装**: AivisSpeechが実証したように、英語・数字・記号の前処理が品質に直結
4. **ユーザー辞書機能**: VOICEVOXコミュニティの需要から、ユーザーが読みを修正できる仕組みは必須
5. **段階的なニューラルG2P統合**: 将来的にONNX Runtimeを通じたBERTベースの読み推定を検討

### 12.3 避けるべきアプローチ

- 機械的な大規模辞書生成（著作権リスク、品質維持困難）
- SudachiPy等のPython専用ライブラリへの依存（C#実装では使用不可）
- 処理速度を大幅に犠牲にするアプローチ（pyopenjtalk-plusの180倍速度低下の教訓）

## 13. 参考リンク

### VOICEVOX関連
- [VOICEVOX Engine GitHub](https://github.com/VOICEVOX/voicevox_engine)
- [VOICEVOX Core GitHub](https://github.com/VOICEVOX/voicevox_core)
- [VOICEVOX Core TTS Process](https://github.com/VOICEVOX/voicevox_core/blob/main/docs/guide/user/tts-process.md)
- [VOICEVOXの音声合成エンジンの紹介（Hiho's Blog）](https://blog.hiroshiba.jp/voicevox-engine-introduction/)
- [open_jtalk-rs](https://github.com/VOICEVOX/open_jtalk-rs)
- [openjtalk-label-getter](https://github.com/Hiroshiba/openjtalk-label-getter)

### 派生ソフトウェア
- [Coeiroink公式](https://coeiroink.com/)
- [SHAREVOX Engine GitHub](https://github.com/SHAREVOX/sharevox_engine)
- [coeiroink-v2-bridge](https://github.com/sevenc-nanashi/coeiroink-v2-bridge)
- [AivisSpeech Engine GitHub](https://github.com/Aivis-Project/AivisSpeech-Engine)
- [AivisSpeech公式](https://aivis-project.com/)

### pyopenjtalk派生
- [pyopenjtalk-plus GitHub](https://github.com/tsukumijima/pyopenjtalk-plus)
- [pyopenjtalk-plus PyPI](https://pypi.org/project/pyopenjtalk-plus/)
- [pyopenjtalk-plus切り替え議論（Issue #1486）](https://github.com/VOICEVOX/voicevox_engine/issues/1486)
- [pyopenjtalk-mod議論（Issue #1763）](https://github.com/VOICEVOX/voicevox_engine/issues/1763)
- [英語読み上げ改善議論（Issue #1524）](https://github.com/VOICEVOX/voicevox_engine/issues/1524)

### ttslearn・Style-Bert-VITS2
- [ttslearn GitHub](https://github.com/r9y9/ttslearn)
- [ttslearn OpenJTalkフロントエンド](https://r9y9.github.io/ttslearn/latest/_modules/ttslearn/tacotron/frontend/openjtalk.html)
- [Style-Bert-VITS2 GitHub](https://github.com/litagin02/Style-Bert-VITS2)
- [Style-Bert-VITS2 JP-Extraについて](https://zenn.dev/litagin/articles/034819a5256ff4)

### その他
- [jpreprocess GitHub](https://github.com/jpreprocess/jpreprocess)
- [pyopenjtalk GitHub](https://github.com/r9y9/pyopenjtalk)
