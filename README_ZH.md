# DotNetG2P

[日本語](README.md) | [English](README_EN.md) | **中文**

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

面向 C#/.NET 的日语 G2P（Grapheme-to-Phoneme：字素到音素转换）库。
以纯 C# 原生实现了兼容 OpenJTalk 的基于规则的 G2P 处理管线，无需依赖 Python 或原生二进制文件即可将日语文本转换为音素序列。

```csharp
using var engine = new G2PEngine(new MeCabTokenizer("/path/to/naist-jdic"));

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"
```

## 目录

- [特性](#特性)
- [安装](#安装)
- [快速开始](#快速开始)
- [API 参考](#api-参考)
- [处理管线](#处理管线)
- [词典准备](#词典准备)
- [选项配置](#选项配置)
- [构建](#构建)
- [线程安全性](#线程安全性)
- [许可证](#许可证)

## 特性

- **纯 C# 实现** — 无需原生二进制文件，内置自研 MeCab 引擎（`DotNetG2P.MeCab`），无 NuGet 包依赖（运行时需要 [naist-jdic 词典](#词典准备)）
- **兼容 OpenJTalk 的处理管线** — 包含发音生成、数字读法、重音短语合并、重音结合类型、清音化的 6 阶段 NJD 处理
- **多种输出格式** — 音素序列 / 片假名 / ESPnet 韵律符号 / VOICEVOX 兼容 AccentPhrase / HTS 全上下文标签 / 韵律特征量（A1/A2/A3）
- **支持 Unity** — 目标框架为 .NET Standard 2.1（Unity 2021.2+），提供 UPM 包
- **可扩展设计** — 通过 `ITokenizer` 接口可替换形态素分析引擎

## 安装

### NuGet

```bash
# 核心库 + 自研 MeCab 引擎
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab
```

### 包组成

| 包 | 许可证 | 说明 |
|---|--------|------|
| `DotNetG2P` | Apache-2.0 | 核心库（G2P 引擎、NJD 处理、音素转换） |
| `DotNetG2P.MeCab` | Apache-2.0 | 自研 MeCab 引擎（无外部依赖） |

### Unity (UPM)

通过 Unity Package Manager 的 **Add package from git URL** 添加以下地址：

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
```

> **注意：** 需要另行准备 naist-jdic 词典。详情请参阅[词典准备](#词典准备)。

## 快速开始

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

// 1. 初始化引擎（指定词典路径）
using var tokenizer = new MeCabTokenizer("/path/to/naist-jdic");
using var engine = new G2PEngine(tokenizer);

// 2. 从文本获取音素序列
string phonemes = engine.ToPhonemes("今日は良い天気です");
// => "ky o o w a i i t e N k i d e s U"

// 3. 获取片假名读音
string kana = engine.ToKana("今日は良い天気です");
// => "キョーワイーテンキデス"

// 4. 带韵律符号的输出（ESPnet 格式）
string prosody = engine.ToProsody("こんにちは");
// => "^ k o [ N n i ch i w a $"

// 5. VOICEVOX 兼容重音短语
var phrases = engine.ToAccentPhrases("こんにちは");

// 6. HTS 全上下文标签（用于 HMM/DNN 语音合成）
var labels = engine.ToFullContextLabels("こんにちは");

// 7. 韵律特征量（逐音素的 A1/A2/A3，面向 uPiper 等语音合成引擎）
var features = engine.ToProsodyFeatures("こんにちは");
// features.Phonemes: ["sil","k","o","N","n","i","ch","i","w","a","sil"]
// features.A1, A2, A3: 各音素的重音位置信息
```

## API 参考

### G2PEngine

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `ToPhonemes(text)` | `string` | 空格分隔的音素序列 (`"k o N n i ch i w a"`) |
| `ToKana(text)` | `string` | 片假名读音 (`"コンニチワ"`) |
| `ToProsody(text)` | `string` | 带 ESPnet 韵律符号 (`"^ k o [ N n i ch i w a $"`) |
| `ToAccentPhrases(text)` | `IReadOnlyList<AccentPhrase>` | VOICEVOX 兼容重音短语结构体 |
| `ToFullContextLabels(text)` | `IReadOnlyList<string>` | HTS 全上下文标签 |
| `ToProsodyFeatures(text)` | `ProsodyFeatures` | 韵律特征量（逐音素的 A1/A2/A3） |
| `Analyze(text)` | `IReadOnlyList<NjdNode>` | NJD 处理后的节点序列 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 批量将多段文本转换为音素序列 |
| `ToKanaBatch(texts)` | `IReadOnlyList<string>` | 批量将多段文本转换为片假名读音 |
| `ToProsodyBatch(texts)` | `IReadOnlyList<string>` | 批量将多段文本转换为带韵律符号格式 |
| `ToFullContextLabelsBatch(texts)` | `IReadOnlyList<IReadOnlyList<string>>` | 批量将多段文本转换为 HTS 标签 |
| `ToProsodyFeaturesBatch(texts)` | `IReadOnlyList<ProsodyFeatures>` | 批量将多段文本转换为韵律特征量 |

### 日语音素体系

| 类别 | 音素 |
|------|------|
| 元音 | `a` `i` `u` `e` `o` （清化元音：`A` `I` `U` `E` `O`） |
| 辅音 | `k` `g` `s` `z` `t` `d` `n` `h` `b` `p` `m` `r` `f` `v` |
| 拗音辅音 | `ky` `gy` `sh` `j` `ch` `ts` `ny` `hy` `by` `py` `my` `ry` `dy` `ty` `kw` `gw` |
| 半元音 | `y` `w` |
| 特殊音素 | `N`（拨音） `cl`（促音） `-`（长音） `pau`（停顿） |

## 处理管线

DotNetG2P 实现了与 [OpenJTalk](https://open-jtalk.sourceforge.net/) 相同的 6 阶段 NJD 处理管线。

```
文本输入
  │
  ├─ TextNormalizer        全角/半角规范化、浊点合并
  ├─ ITokenizer.Tokenize   形态素分析（MeCabTokenizer + naist-jdic）
  ├─ SetPronunciation      词典读音与回退发音生成
  ├─ SetDigit              数字串检测与助数词读法转换
  ├─ SetAccentPhrase       基于词性模式的重音短语合并（18 条规则）
  ├─ SetAccentType         基于链接规则的重音结合类型判定
  └─ SetUnvoicedVowel      元音清化（6 条规则）
  │
  ▼
  输出（音素序列 / 片假名 / 韵律符号 / AccentPhrase / HTS 标签 / 韵律特征量）
```

## 词典准备

DotNetG2P 使用 naist-jdic 词典（OpenJTalk 专用 MeCab 词典）进行形态素分析。

### 获取方式

1. 从 [Open JTalk 官方网站](https://open-jtalk.sourceforge.net/)下载
2. 直接使用 pyopenjtalk 或 OpenJTalk 附带的词典目录

### 所需文件

词典目录中需要包含以下 4 个文件：

| 文件 | 内容 |
|------|------|
| `sys.dic` | 系统词典 |
| `matrix.bin` | 转移代价矩阵 |
| `char.bin` | 字符类别定义 |
| `unk.dic` | 未知词模板 |

### 在 Unity 中的部署

在 Unity 中，将词典文件放置于 `StreamingAssets` 文件夹中，并通过 `Application.streamingAssetsPath` 指定路径。

```csharp
var dicPath = Path.Combine(Application.streamingAssetsPath, "naist-jdic");
using var tokenizer = new MeCabTokenizer(dicPath);
```

## 选项配置

通过 `G2POptions` 可以单独开启或关闭各处理阶段（不可变设计）。

```csharp
// 仅禁用元音清化的示例
var options = new G2POptions(enableUnvoicedVowel: false);
using var engine = new G2PEngine(tokenizer, options);
```

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `enableTextNormalization` | `true` | 文本规范化（全角/半角转换） |
| `enableDigitProcessing` | `true` | 数字读法转换与助数词处理 |
| `enableAccentPhrase` | `true` | 重音短语合并（18 条规则） |
| `enableAccentType` | `true` | 重音结合类型判定 |
| `enableUnvoicedVowel` | `true` | 元音清化（6 条规则） |
| `expandLongVowels` | `true` | 以元音重复输出长音（`false` = 使用 `"-"` 符号） |

## 构建

### 环境要求

- .NET SDK 9.0 或更高版本

### 命令

```bash
# 构建
dotnet build DotNetG2P.slnx

# 运行测试
dotnet test DotNetG2P.slnx

# 控制台示例（无词典：仅验证 MoraMapping）
dotnet run --project samples/DotNetG2P.Console

# 控制台示例（有词典：完整 G2P）
dotnet run --project samples/DotNetG2P.Console -- /path/to/naist-jdic
```

## 线程安全性

`G2PEngine` 和 `MeCabTokenizer` 不是线程安全的。
在多线程环境中，请为每个线程创建单独的实例。

字典数据（`DictionaryBundle`）通过内部 WeakReference 缓存自动共享，
因此创建多个实例的内存开销极小。

## 许可证

| 包 | 许可证 | 备注 |
|---|--------|------|
| **DotNetG2P** | [Apache-2.0](LICENSE) | 核心库 |
| **DotNetG2P.MeCab** | [Apache-2.0](LICENSE) | 自研 MeCab 引擎 |

所有组件均以 **Apache-2.0 许可证** 提供。
