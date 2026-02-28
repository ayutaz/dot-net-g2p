# ONNX Runtimeを使ったC#でのニューラルG2Pの可能性調査

## 1. ONNX Runtime C#バインディング（Microsoft.ML.OnnxRuntime）

### 1.1 パッケージ概要

- **最新バージョン**: 1.24.2（2026年2月19日更新）
- **NuGetパッケージ**: `Microsoft.ML.OnnxRuntime`（CPU）、`Microsoft.ML.OnnxRuntime.Gpu`（CUDA）、`Microsoft.ML.OnnxRuntime.DirectML`（DirectML）
- **マネージドパッケージ**: `Microsoft.ML.OnnxRuntime.Managed`（ネイティブバイナリなしのC#マネージドコードのみ）
- **対応フレームワーク**: .NET Standard 1.1+, .NET Core, .NET Framework 4.6.1+, .NET 5/6/7/8+
- **ライセンス**: MIT

### 1.2 C# API概要

主要なクラスと使用パターン:

```csharp
// セッション作成
using var sessionOptions = new SessionOptions();
sessionOptions.SetIntraOpNumThreads(4); // スレッド数設定
using var session = new InferenceSession("model.onnx", sessionOptions);

// 入力テンソル作成（OrtValue API）
var inputData = new long[] { 1, 2, 3, 4, 5 }; // トークンID列
var shape = new long[] { 1, inputData.Length };
using var inputTensor = OrtValue.CreateTensorValueFromMemory(inputData, shape);

// 推論実行
var inputs = new Dictionary<string, OrtValue> { { "input_ids", inputTensor } };
using var runOptions = new RunOptions();
using var results = session.Run(runOptions, inputs, session.OutputNames);

// 結果取得
var output = results[0].GetTensorDataAsSpan<long>();
```

主要API:
- `InferenceSession`: モデルのロードと推論実行を管理
- `SessionOptions`: スレッド数、実行プロバイダ、最適化レベル等の設定
- `OrtValue`: テンソル・マップ・シーケンスを保持する汎用コンテナ。`ReadOnlySpan<T>` / `Span<T>` でデータアクセス
- `RunOptions`: 推論実行時のオプション（タイムアウト、ログレベル等）

### 1.3 Unity対応状況

#### ONNX Runtime Unity Plugin（asus4/onnxruntime-unity）

| プラットフォーム | CPU | GPU/アクセラレータ |
|---|---|---|
| macOS | 対応 | CoreML対応 |
| iOS | 対応 | CoreML対応, XNNPACK実験中 |
| Android | 対応 | NNAPI対応, XNNPACK実験中 |
| Windows | 対応 | DirectML対応, CUDA/TensorRT実験中 |
| Linux | 対応 | CUDA/TensorRT実験中 |
| **WebGL** | **未対応** | **未対応** |

- **プラグインバージョン**: 0.4.4（ONNX Runtime 1.23.2ベース）
- **インストール**: NPMレジストリ経由（`com.github.asus4.onnxruntime`）
- **注意**: IL2CPPビルドでの互換性問題が過去に報告されている（Issue #10427）

#### Unity Sentis（旧Barracuda → Sentis → Inference Engine）

- Unityが公式に提供するMLモデル推論ライブラリ
- ONNXモデル（opset 7-15）をインポート可能
- **WebGL対応**: Sentisは各種Unityランタイムプラットフォームで動作
- **最新バージョン**: Sentis 2.4.1（com.unity.ai.inference）
- **利点**: Unityエコシステムとのネイティブ統合、クロスプラットフォーム対応
- **制限**: 対応オペレータセットがONNX Runtime本体より限定的

### 1.4 パフォーマンス（CPU推論速度）

- ONNX Runtimeは一般に、同等のPyTorch/TensorFlowモデルと比較して20-99%の推論高速化を実現
- 小規模モデル（数MB）の場合、CPU推論で1-10ms程度のレイテンシ
- 量子化（INT8）により、FP32比で2-4倍の高速化・メモリ削減が可能
- スレッド並列化により、マルチコアCPUでの推論を最適化可能

## 2. 既存のニューラルG2Pモデル

### 2.1 Transformer-based G2P

| モデル | アーキテクチャ | パラメータ | 対応言語 | ONNXエクスポート |
|---|---|---|---|---|
| CMUSphinx g2p-seq2seq | 3層Transformer (256隠れ層) | 数MB程度 | 英語 | TensorFlow経由で可能 |
| Cisco g2p_seq2seq_pytorch | Transformer (FairSeq) | 256隠れ層 | 英語 | PyTorch経由で可能 |
| CharsiuG2P (ByT5) | ByT5 Transformer | tiny(8/12/16層), small | 100言語（日本語含む） | Hugging Face Optimumで変換可能 |
| NVIDIA NeMo G2P | ByT5 / Conformer CTC | 多様 | 多言語 | NeMo ONNX Exporterで変換可能 |

#### CharsiuG2P詳細

- バイトレベルのT5モデル（ByT5）を使用した100言語対応G2Pモデル
- 日本語対応あり（CJK言語は外部トークナイザーが必要）
- 入力形式: `<jpn>: こんにちは` のように言語コード接頭辞が必要
- モデルバリエーション:
  - `g2p_multilingual_byT5_tiny_8_layers_100`: 最軽量
  - `g2p_multilingual_byT5_tiny_16_layers_100`: 中間
  - `g2p_multilingual_byT5_small_100`: 高精度
- Hugging Face Hubで公開（`charsiu/` プレフィックス）

### 2.2 Seq2Seq / LSTM-based G2P

| モデル | アーキテクチャ | 特徴 |
|---|---|---|
| Kyubyong/g2p | Encoder-Decoder LSTM | CMU辞書ベース、英語特化 |
| Yolchuyeva et al. | Transformer G2P | Transformer初のG2P適用研究 |
| ICASSP 2015 LSTM G2P | Bidirectional LSTM | RNN-based G2Pの先駆的研究 |

### 2.3 日本語対応のG2Pモデル

| モデル/システム | 方式 | 日本語対応 | 特徴 |
|---|---|---|---|
| CharsiuG2P | ByT5 Transformer | 対応（100言語の1つ） | 多言語統一モデル |
| ESPnet pyopenjtalk_g2p | ルールベース + 形態素解析 | ネイティブ対応 | OpenJTalk依存 |
| PORORO G2P | Transformer | 韓国語/日本語/中国語 | カカオブレイン製 |
| MixedG2P-T5 | T5 + SSL | CJK混在テキスト対応 | 最新研究（2025） |

**日本語G2Pの特殊性**: 日本語は漢字・ひらがな・カタカナ・ローマ字が混在し、漢字は多読みを持つため、単純なG2Pモデルでは精度が不十分。形態素解析による単語分割とそれに基づく読み付与が必要不可欠。

## 3. PyTorch/TensorFlowモデルのONNX変換

### 3.1 PyTorch → ONNX変換

```python
# PyTorch 2.5+推奨方式
import torch

model = load_g2p_model()
dummy_input = torch.randint(0, vocab_size, (1, max_len))

torch.onnx.export(
    model,
    dummy_input,
    "g2p_model.onnx",
    dynamo=True,  # 推奨（PyTorch 2.5+）
    input_names=["input_ids"],
    output_names=["phoneme_ids"],
    dynamic_axes={
        "input_ids": {0: "batch", 1: "sequence"},
        "phoneme_ids": {0: "batch", 1: "sequence"}
    }
)
```

### 3.2 Hugging Face Optimumによるエクスポート

```python
# ByT5/T5系モデルのONNXエクスポート
from optimum.onnxruntime import ORTModelForSeq2SeqLM

model = ORTModelForSeq2SeqLM.from_pretrained(
    "charsiu/g2p_multilingual_byT5_tiny_16_layers_100",
    export=True
)
model.save_pretrained("g2p_onnx/")
# => encoder_model.onnx, decoder_model.onnx, decoder_with_past_model.onnx が生成
```

### 3.3 Seq2Seqモデルのエクスポート時の注意点

- Encoder-Decoderモデルは通常3つのONNXファイルに分割される:
  1. `encoder_model.onnx`: エンコーダ部分
  2. `decoder_model.onnx`: デコーダ（初回ステップ）
  3. `decoder_with_past_model.onnx`: デコーダ（KVキャッシュ利用、2回目以降）
- C#側で3つのInferenceSessionを管理し、自己回帰デコーディングループを実装する必要がある
- ビームサーチ等のデコーディング戦略もC#側で実装が必要

## 4. ルールベース + ニューラルのハイブリッドアプローチ

### 4.1 ハイブリッドG2Pの概念

```
入力テキスト
    │
    ▼
[形態素解析 / 単語分割]
    │
    ▼
[辞書ルックアップ]──── 辞書にあり ──→ 読み確定
    │
    辞書になし（OOV）
    │
    ▼
[ニューラルG2Pモデル]
    │
    ▼
音素列出力
```

### 4.2 代表的なハイブリッド実装

#### Misaki G2P（hexgrad）

- Kokoro TTS向けに開発されたハイブリッドG2Pエンジン
- **ルールベース部分**: ルックアップテーブル + 基本ルール
- **ニューラル部分**: OOV単語に対するフォールバック（eSpeak-ng または ニューラルseq2seqモデル）
- 日本語トークナイザーは pyopenjtalk + full UniDic を使用し、ピッチアクセントマークやフレーズ結合を実現
- Swift移植版（MisakiSwift）も存在

#### Fast, Not Fancy（2025年論文）

- arXiv: 2505.12973
- ルールベースG2P（eSpeak）をリッチなデータで強化するアプローチ
- 同音異義語（homograph）の曖昧性解消で約30%の精度向上
- リアルタイムアプリケーション（スクリーンリーダー等）向けの低レイテンシ設計

#### 一般的なハイブリッド戦略

| 戦略 | 説明 | 利点 | 欠点 |
|---|---|---|---|
| 辞書優先 + ニューラルフォールバック | 辞書にある単語はルックアップ、OOVはニューラル | 高速、辞書語の精度保証 | ニューラルモデルの読み込みが必要 |
| ルールベース + ニューラル同音異義語解消 | ルールでG2P、同音異義語はニューラルで判別 | 効率的 | 実装複雑 |
| ニューラル主体 + ルール後処理 | ニューラルで変換後、ルールで修正 | 高精度 | 推論コスト大 |

### 4.3 日本語G2Pへの適用

日本語G2Pにおけるハイブリッドアプローチの最適構成:

```
入力テキスト（漢字かな混じり）
    │
    ▼
[形態素解析（MeCab/NMeCab）] ← ルールベース
    │
    ▼
[辞書読み付与（naist-jdic）] ← 辞書ルックアップ
    │
    ├── 辞書にあり → 読み確定
    │
    └── 未知語（OOV）
         │
         ▼
    [ニューラルG2Pモデル] ← ニューラル
         │
         ▼
[NJD処理（アクセント結合等）] ← ルールベース
    │
    ▼
[音素変換] ← ルールベース（カタカナ→音素マッピング）
    │
    ▼
音素列 + アクセント情報
```

**重要**: 日本語G2Pでは形態素解析が根幹であり、これをニューラルモデルで完全に代替するのは現時点では非現実的。ニューラルの活用は主に「未知語の読み推定」に限定するのが実用的。

## 5. 日本語G2Pへのニューラルアプローチの適用事例

### 5.1 既存の適用事例

| プロジェクト | 方式 | 日本語G2Pでのニューラル活用 |
|---|---|---|
| ESPnet TTS | pyopenjtalk + ニューラルTTS | G2P自体はルールベース（OpenJTalk）、TTSがニューラル |
| Misaki (Kokoro) | ハイブリッド | 日本語はpyopenjtalk+UniDicベース、ニューラルは英語OOV向け |
| VOICEVOX | OpenJTalk | G2Pは完全にルールベース |
| MixedG2P-T5 | T5 + SSL | CJK混在テキストのEnd-to-End G2P（研究段階） |

### 5.2 日本語特有の課題

1. **漢字の多読み**: 「生」は「せい」「しょう」「い（きる）」「う（まれる）」「なま」等、多数の読みを持つ
2. **形態素境界の曖昧性**: 「東京都」→「とうきょう/と」vs「ひがしきょうと」
3. **固有名詞の読み**: 人名・地名は辞書に無いケースが多い
4. **外来語の表記揺れ**: 「コンピュータ」「コンピューター」
5. **アクセント情報**: 音素列だけでなく、アクセント核位置の推定も必要

### 5.3 ニューラルが有効な場面

- **未知語（OOV）の読み推定**: 特に固有名詞、新語、専門用語
- **漢字の読み分け（同音異義語解消）**: 文脈に基づく読み選択
- **アクセント予測**: 複合語のアクセント核位置推定

## 6. 推論速度・メモリ使用量の概算

### 6.1 モデルサイズの概算

| モデル | パラメータ数 | FP32サイズ | INT8量子化後 |
|---|---|---|---|
| CMUSphinx G2P (3層/256) | 約2-5M | 約10-20MB | 約3-5MB |
| ByT5-tiny (8層) | 約15-30M | 約60-120MB | 約15-30MB |
| ByT5-tiny (16層) | 約30-60M | 約120-240MB | 約30-60MB |
| ByT5-small | 約60-120M | 約240-480MB | 約60-120MB |
| 日本語専用小型G2P（仮） | 約1-3M | 約4-12MB | 約1-3MB |

### 6.2 推論速度の概算（CPU、1単語あたり）

| モデル規模 | FP32推論 | INT8推論 | 備考 |
|---|---|---|---|
| 小型（1-5M params） | 1-5ms | 0.5-2ms | リアルタイム向け |
| 中型（15-60M params） | 10-50ms | 5-20ms | バッチ処理向け |
| 大型（60M+ params） | 50-200ms | 20-80ms | 高精度向け |

**注**: Seq2Seqモデルはデコーダの自己回帰ループがあるため、出力長に比例して推論時間が増加する。日本語音素列は通常5-20音素程度なので、デコーディングステップは限定的。

### 6.3 メモリ使用量の概算

- **ONNX Runtime本体**: 約50-100MB
- **モデル読み込み**: モデルサイズ + 約20-50%のオーバーヘッド
- **推論時のワーキングメモリ**: モデルサイズの10-30%程度
- **合計目安（小型モデル）**: 約100-200MB

## 7. Unity WebGLでのONNX Runtime動作可能性

### 7.1 現状の結論

**ONNX Runtime自体のWebGL対応は困難**

| 要因 | 詳細 |
|---|---|
| ネイティブバイナリ依存 | ONNX RuntimeはC++ネイティブライブラリに依存。WebGLではネイティブDLLロード不可 |
| WebGL制約 | シングルスレッド、WebAssemblyメモリ制限、ファイルシステムアクセス不可 |
| 公式サポートなし | onnxruntime-unity pluginのプラットフォーム表にWebGLは含まれていない |

### 7.2 代替手段

| 手段 | 説明 | 実現可能性 |
|---|---|---|
| **Unity Sentis** | Unityの公式ML推論エンジン。WebGLを含む全ランタイムで動作 | 高 |
| **onnxruntime-web** | JavaScript/WebAssembly版ONNX Runtime。WebGPU/WebGLバックエンド対応 | 中（Unity WebGLとの統合が課題） |
| **サーバーサイド推論** | WebAPIでサーバーにG2P推論を委譲 | 高（ただしオフライン不可） |
| **ルールベースのみ** | WebGLではニューラルを使わず、完全ルールベースで処理 | 最も現実的 |

### 7.3 推奨アプローチ

WebGLプラットフォームでは以下の段階的アプローチを推奨:

1. **基本**: ルールベースG2P（形態素解析 + 辞書）のみをWebGLで使用
2. **拡張**: Unity Sentisで小型ニューラルモデルを追加（OOV対応）
3. **高品質**: サーバーサイドAPIで高精度ニューラルG2Pを提供

## 8. KokoroSharp等の既存C# ONNX音声合成の実装分析

### 8.1 KokoroSharp

| 項目 | 詳細 |
|---|---|
| **リポジトリ** | [Lyrcaxis/KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp) |
| **ライセンス** | MIT |
| **NuGetパッケージ** | `KokoroSharp` (CPU), `KokoroSharp.GPU` (CUDA), `KokoroSharp.GPU.Windows` |
| **最新バージョン** | 0.6.2 |
| **モデルサイズ** | 約320MB（FP32） |
| **対応言語** | 英語、中国語、日本語、ヒンディー語、スペイン語、フランス語、イタリア語、ポルトガル語 |

#### アーキテクチャ

```
テキスト入力
    │
    ▼
[テキスト → 音素変換（G2P）]
  ├── 組み込みトークナイザー（eSpeak NGベース）
  └── 外部音素化ソリューション（Android/iOS向け）
    │
    ▼
[音素 → トークンID変換]
    │
    ▼
[ONNX Runtime推論（Kokoro-82Mモデル）]
    │
    ▼
[音声波形出力]
```

#### 実装の特徴

- **Plug & Play**: NuGetインストールだけで全依存関係が解決される
- **ストリーミング**: テキストセグメント単位でのストリーミング推論に対応
- **ボイスミキシング**: 複数話者の声をミックス可能
- **音素リテラル**: `[tomato](/t&#601;me&#618;to&#650;/)` のようなIPA直接指定に対応
- **日本語**: Misaki G2Pの日本語トークナイザー（pyopenjtalk + full UniDic）をベースに処理

#### dot-net-g2pプロジェクトへの示唆

- ONNX Runtimeの統合パターン（NuGetパッケージ構成、マルチプラットフォーム対応）が参考になる
- 音素化部分を外部化可能な設計は、G2Pライブラリとの連携に有用
- 日本語G2P処理はpyopenjtalk依存であり、まさにdot-net-g2pが解決しようとしている問題

### 8.2 sherpa-onnx

| 項目 | 詳細 |
|---|---|
| **リポジトリ** | [k2-fsa/sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) |
| **対応言語** | 12言語のプログラミング言語バインディング（C#含む） |
| **機能** | ASR, TTS, VAD, Speaker Diarization, Speech Enhancement |
| **C#対応** | dotnet-examples/ にサンプルコード |

#### C#での使用パターン

```csharp
// sherpa-onnxのC# TTS例（Kokoro TTS）
var config = new OfflineTtsConfig();
config.Model.Kokoro.Model = "kokoro-v1.0.onnx";
config.Model.Kokoro.Voices = "voices.bin";
config.Model.Kokoro.DataDir = "./espeak-ng-data";

var tts = new OfflineTts(config);
var audio = tts.Generate("Hello world", sid: 0, speed: 1.0f);
```

#### dot-net-g2pへの示唆

- ネイティブC#ラッパーパターンの実装参考
- lexicon.txtファイルによるG2P辞書管理方式
- eSpeak-ngのIPA生成との連携パターン

### 8.3 espnet_onnx

| 項目 | 詳細 |
|---|---|
| **リポジトリ** | [espnet/espnet_onnx](https://github.com/espnet/espnet_onnx) |
| **対応機能** | ASR, TTS（ONNX形式でのエクスポート・推論） |
| **Python依存** | エクスポート時はPyTorch/ESPnet必要、推論時は不要 |
| **G2P機能** | 明示的なG2P ONNXエクスポートは未対応 |

## 9. 総合評価と推奨アプローチ

### 9.1 dot-net-g2pプロジェクトへの適用可能性

| アプローチ | 実現可能性 | 推奨度 | 理由 |
|---|---|---|---|
| 完全ニューラルG2P | 低 | 非推奨 | 日本語の複雑性（漢字多読み、形態素境界）に対応困難 |
| ルールベース + ニューラルOOV | 中～高 | 推奨 | OpenJTalkパイプラインをベースに、未知語処理をニューラルで補強 |
| ルールベースのみ | 高 | 基本方針として推奨 | まずルールベースで十分な精度を確保し、後からニューラルを追加 |

### 9.2 段階的導入ロードマップ

#### Phase 1: ルールベースG2P（コアライブラリ）

- MeCab互換形態素解析
- naist-jdic辞書による読み付与
- NJD処理（アクセント句結合、数字読み等）
- カタカナ→音素変換
- **ニューラル不要、ONNX依存なし**

#### Phase 2: ニューラルOOV読み推定（オプション拡張）

- 小型Seq2Seq/Transformerモデルの導入
- ONNX Runtimeによる推論
- 漢字→読みのニューラルモデル（形態素解析の補助）
- 推定モデルサイズ: 5-20MB（INT8量子化済み）

#### Phase 3: 高度なニューラル機能（将来構想）

- 文脈依存の漢字読み分け（同音異義語解消）
- ニューラルアクセント予測
- End-to-Endニューラルフォールバック

### 9.3 技術選定の推奨

| 項目 | 推奨 | 理由 |
|---|---|---|
| **推論エンジン** | Microsoft.ML.OnnxRuntime | C#ネイティブ対応、高パフォーマンス、広いプラットフォーム対応 |
| **Unity対応** | Sentis（WebGL）+ ONNX Runtime（それ以外） | WebGL対応はSentisが唯一の現実的選択肢 |
| **モデル形式** | ONNX（INT8量子化） | サイズ・速度のバランスが最適 |
| **モデルアーキテクチャ** | 小型Transformer（3-6層、256隠れ層） | G2Pには十分な精度、軽量 |
| **パッケージ構成** | コアライブラリ + ニューラル拡張パッケージ | ニューラル不要の環境では軽量に動作 |

### 9.4 推奨パッケージ構成

```
DotNetG2P/
├── DotNetG2P.Core/           # ルールベースG2P（依存なし）
│   ├── MorphAnalyzer/        # 形態素解析
│   ├── NJD/                  # NJD処理
│   └── PhonemeConverter/     # 音素変換
│
├── DotNetG2P.Neural/         # ニューラル拡張（オプション）
│   ├── OnnxG2PModel.cs       # ONNX Runtime推論ラッパー
│   ├── OovPredictor.cs       # 未知語読み推定
│   └── models/               # ONNXモデルファイル
│   └── (依存: Microsoft.ML.OnnxRuntime)
│
└── DotNetG2P.Unity/          # Unity統合（オプション）
    ├── SentisG2PModel.cs     # Sentis推論ラッパー
    └── (依存: com.unity.sentis)
```

## 10. 参考資料

### ONNX Runtime
- [ONNX Runtime C# API](https://onnxruntime.ai/docs/get-started/with-csharp.html)
- [ONNX Runtime C# チュートリアル](https://onnxruntime.ai/docs/tutorials/csharp/basic_csharp.html)
- [Microsoft.ML.OnnxRuntime NuGet](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime)
- [ONNX Runtime GitHub](https://github.com/microsoft/onnxruntime)

### Unity
- [onnxruntime-unity プラグイン](https://github.com/asus4/onnxruntime-unity)
- [Unity Sentis ドキュメント](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.4/manual/index.html)

### ニューラルG2Pモデル
- [CMUSphinx g2p-seq2seq](https://github.com/cmusphinx/g2p-seq2seq)
- [Cisco g2p_seq2seq_pytorch](https://github.com/CiscoDevNet/g2p_seq2seq_pytorch)
- [CharsiuG2P（100言語対応）](https://github.com/lingjzhu/CharsiuG2P)
- [NVIDIA NeMo G2Pドキュメント](https://docs.nvidia.com/nemo-framework/user-guide/latest/nemotoolkit/tts/g2p.html)

### ハイブリッドG2P
- [Misaki G2P（hexgrad）](https://github.com/hexgrad/misaki)
- [G2P Shrinks Speech Models（Hugging Face Blog）](https://huggingface.co/blog/hexgrad/g2p)
- [Fast, Not Fancy: Rethinking G2P（arXiv 2505.12973）](https://arxiv.org/abs/2505.12973)

### C# ONNX音声合成
- [KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp)
- [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)
- [espnet_onnx](https://github.com/espnet/espnet_onnx)

### 日本語G2P関連
- [ESPnet phoneme_tokenizer.py](https://github.com/espnet/espnet/blob/master/espnet2/text/phoneme_tokenizer.py)
- [lilasaba/jpn_g2p](https://github.com/lilasaba/jpn_g2p)
- [MixedG2P-T5（arXiv 2509.01391）](https://arxiv.org/html/2509.01391v1)
- [Multilingual G2P with Byte Representation（Amazon Science）](https://assets.amazon.science/f0/d2/1db4b7c146cf821e3a0752f636a7/scipub-1172.pdf)
