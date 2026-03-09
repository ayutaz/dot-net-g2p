# DotNetG2P

[日本語](README.md) | [English](README_EN.md) | **中文**

[![CI](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml/badge.svg)](https://github.com/ayutaz/dot-net-g2p/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DotNetG2P.svg)](https://www.nuget.org/packages/DotNetG2P)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)

面向 C#/.NET 的日英中多语言 + 西班牙语 G2P（Grapheme-to-Phoneme：字素到音素转换）库。
以纯 C# 原生实现了兼容 OpenJTalk 的日语 G2P 处理管线、基于 CMU 词典的英语 G2P、基于 pinyin-data 词典的中文拼音转换，以及基于规则的西班牙语 G2P，无需依赖 Python 或原生二进制文件即可转换为音素序列。

```csharp
using var engine = new G2PEngine(new MeCabTokenizer());

engine.ToPhonemes("こんにちは");  // => "k o N n i ch i w a"
engine.ToKana("音声合成");        // => "オンセーゴーセー"

// 英语 G2P
using var enEngine = new EnglishG2PEngine();
enEngine.ToPhonemes("hello world");  // => "HH AH0 L OW1 W ER1 L D"

// 中文 G2P（拼音转换）
using var zhEngine = new ChineseG2PEngine();
zhEngine.ToPinyin("你好世界");  // => "ní hǎo shì jiè"

// 西班牙语 G2P
using var esEngine = new SpanishG2PEngine();
esEngine.ToIPA("vergüenza");  // => "beɾˈɡwensa"

// 日英混合文本
using var multiEngine = new MultilingualG2PEngine();
multiEngine.ToPhonemes("私はhelloと言った");  // 日语部分 => 日语音素，英语部分 => ARPAbet
```

## 目录

- [特性](#特性)
- [安装](#安装)
- [快速开始](#快速开始)
- [API 参考](#api-参考)
- [处理管线](#处理管线)
- [词典准备](#词典准备)
- [西班牙语评估](#西班牙语评估)
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
- **支持英语 G2P** — CMU 词典（135,000 词）+ Flite LTS 规则进行 OOV 推测、IPA/X-SAMPA 输出、文本规范化、同形异音词解析
- **支持中文 G2P** — pinyin-data 单字词典（44,000 条）+ phrase-pinyin-data 短语词典（411,000 条），自动多音字解析、声调变调（三声连读、一/不变调）、3 种输出风格、IPA（国际音标）与注音符号（ㄅㄆㄇㄈ）输出
- **支持西班牙语 G2P** — 提供基于规则的 IPA 转写、音节划分、重音判定、Castilian/Latin American 切换、异音处理选项、文本规范化、例外词典以及全量语料评估工具链
- **支持日英中西混合文本** — 基于 Unicode 字符类别的自动语言检测与分段，并通过 `DefaultLatinLanguage` 控制英语/西班牙语拉丁文本路由

## 安装

### NuGet

```bash
# 核心库 + 自研 MeCab 引擎（日语 G2P）
dotnet add package DotNetG2P
dotnet add package DotNetG2P.MeCab

# 英语 G2P
dotnet add package DotNetG2P.English

# 中文 G2P（拼音转换）
dotnet add package DotNetG2P.Chinese

# 西班牙语 G2P
dotnet add package DotNetG2P.Spanish

# 日英中西混合文本支持
dotnet add package DotNetG2P.Multilingual
```

### 包组成

| 包 | 许可证 | 说明 |
|---|--------|------|
| `DotNetG2P` | Apache-2.0 | 核心库（G2P 引擎、NJD 处理、音素转换） |
| `DotNetG2P.MeCab` | Apache-2.0 | 自研 MeCab 引擎（无外部依赖） |
| `DotNetG2P.English` | Apache-2.0 | 英语 G2P 引擎（CMU 词典 + LTS 规则） |
| `DotNetG2P.Chinese` | Apache-2.0 | 中文 G2P 引擎（pinyin-data 词典 + 声调变调） |
| `DotNetG2P.Spanish` | Apache-2.0 | 西班牙语 G2P 引擎（规则驱动 + 可选异音处理） |
| `DotNetG2P.Multilingual` | Apache-2.0 | 多语言 G2P 引擎（日英中西混合文本支持） |

### Unity (UPM)

通过 Unity Package Manager 的 **Add package from git URL** 添加以下地址：

```
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Core
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.MeCab
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.English
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Chinese
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Spanish
https://github.com/ayutaz/dot-net-g2p.git?path=src/DotNetG2P.Multilingual
```

> **注意：** 日语或多语言引擎需要另行准备 naist-jdic 词典。详情请参阅[词典准备](#词典准备)。

## 快速开始

```csharp
using DotNetG2P;
using DotNetG2P.MeCab;

// 1. 从默认安装位置或环境变量自动解析词典
using var tokenizer = new MeCabTokenizer();
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

// === 中文 G2P（拼音转换）===
using DotNetG2P.Chinese;

using var zhEngine = new ChineseG2PEngine();

// 基本转换（带声调符号）
string pinyin = zhEngine.ToPinyin("你好世界");
// => "ní hǎo shì jiè"

// 声调数字格式
string toneNum = zhEngine.ToPinyin("你好世界", PinyinStyle.ToneNumber);
// => "ni2 hao3 shi4 jie4"

// 逐字拼音数组
string[] list = zhEngine.ToPinyinList("中国");
// => ["zhōng", "guó"]

// 自动多音字解析
string bank = zhEngine.ToPinyin("银行");  // => "yín háng"（háng = 银行）
string act = zhEngine.ToPinyin("行为");   // => "xíng wéi"（xíng = 行为）

// 声调变调
string hello = zhEngine.ToPinyin("你好");  // => "ní hǎo"（三声连读：nǐ → ní）
string yige = zhEngine.ToPinyin("一个");   // => "yí gè"（一变调：yī → yí）
string buyao = zhEngine.ToPinyin("不要");  // => "bú yào"（不变调：bù → bú）

// IPA（国际音标）输出
string ipa = zhEngine.ToIPA("你好");
// => IPA 表记

// 注音符号（ㄅㄆㄇㄈ）输出
string zhuyin = zhEngine.ToZhuyin("你好");
// => 注音符号表记

// === 英语 G2P ===
using DotNetG2P.English;

using var enEngine = new EnglishG2PEngine();
string enPhonemes = enEngine.ToPhonemes("hello world");
// => "HH AH0 L OW1 W ER1 L D"

// === 西班牙语 G2P ===
using DotNetG2P.Spanish;

using var esEngine = new SpanishG2PEngine();
string esIpa = esEngine.ToIPA("guion");
// => "ɡiˈon"

// === 日英中西混合文本 ===
using DotNetG2P.Multilingual;

using var multiEngine = new MultilingualG2PEngine();
string mixed = multiEngine.ToPhonemes("今日はgood dayです");
// 日语部分 => 日语音素，英语部分 => ARPAbet 音素

var segments = multiEngine.ToSegments("今日はgood dayです");
// 带语言标签的分段列表

// 包含中文文本的情况
var zhOptions = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
using var multiZhEngine = new MultilingualG2PEngine(zhOptions);
multiZhEngine.ToPhonemes("你好hello");
// 中文部分 => 拼音，英语部分 => ARPAbet 音素

// 包含西班牙语文本的情况
var esOptions = new MultilingualG2POptions(defaultLatinLanguage: Language.Spanish);
using var multiEsEngine = new MultilingualG2PEngine(esOptions);
multiEsEngine.ToPhonemes("hola世界");
// 西班牙语部分 => IPA 音素，日语部分 => 日语音素
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

### EnglishG2PEngine

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `ToPhonemes(text)` | `string` | ARPAbet 音素序列 (`"HH AH0 L OW1"`) |
| `ToIPA(text)` | `string` | IPA 表记 |
| `ToIPAWithoutStress(text)` | `string` | 无重音标记的 IPA 表记 |
| `ToXSampa(text)` | `string` | X-SAMPA 表记 |
| `ToXSampaWithoutStress(text)` | `string` | 无重音标记的 X-SAMPA 表记 |
| `ToPhonemeList(text)` | `IReadOnlyList<EnglishPhoneme>` | 结构化音素列表 |
| `LookupWord(word)` | `IReadOnlyList<EnglishPhoneme>` | 单词查询 |
| `LookupAllPronunciations(word)` | `IReadOnlyList<EnglishPronunciation>` | 获取全部发音变体 |
| `ContainsWord(word)` | `bool` | 词典存在性确认 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 批量 ARPAbet 转换 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | 批量 IPA 转换 |
| `ToXSampaBatch(texts)` | `IReadOnlyList<string>` | 批量 X-SAMPA 转换 |
| `ToPhonemeListBatch(texts)` | `IReadOnlyList<IReadOnlyList<EnglishPhoneme>>` | 批量结构化音素列表转换 |

### ChineseG2PEngine

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `ToPinyin(text)` | `string` | 带声调符号的拼音字符串 (`"nǐ hǎo"`) |
| `ToPinyin(text, style)` | `string` | 指定风格的拼音字符串 |
| `ToPinyinList(text)` | `string[]` | 逐字拼音数组 |
| `ToPinyinList(text, style)` | `string[]` | 指定风格的逐字拼音数组 |
| `ContainsChar(c)` | `bool` | 词典存在性确认 |
| `LookupChar(c)` | `string[]` | 获取全部拼音候选 |
| `ToIPA(text)` | `string` | IPA（国际音标）表记 |
| `ToIPA(text, includeTones)` | `string` | 声调控制的 IPA 表记 |
| `ToZhuyin(text)` | `string` | 注音符号（ㄅㄆㄇㄈ）表记 |
| `ToZhuyin(text, includeTones)` | `string` | 声调控制的注音表记 |
| `ToPinyinBatch(texts)` | `string[]` | 批量拼音转换 |
| `ToPinyinBatch(texts, style)` | `string[]` | 批量拼音转换（指定风格） |
| `ToPinyinListBatch(texts)` | `string[][]` | 批量逐字拼音转换 |
| `ToPinyinListBatch(texts, style)` | `string[][]` | 批量逐字拼音转换（指定风格） |
| `ToIPABatch(texts)` | `string[]` | 批量 IPA 转换 |
| `ToIPABatch(texts, includeTones)` | `string[]` | 批量 IPA 转换（声调控制） |
| `ToZhuyinBatch(texts)` | `string[]` | 批量注音转换 |
| `ToZhuyinBatch(texts, includeTones)` | `string[]` | 批量注音转换（声调控制） |

### SpanishG2PEngine

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `ToPhonemes(text)` | `string` | 空格分隔的 IPA 音素序列 |
| `ToIPA(text)` | `string` | IPA 表记 |
| `ToPhonemeList(text)` | `IReadOnlyList<SpanishPhoneme>` | 结构化音素列表 |
| `ToSyllables(word)` | `IReadOnlyList<SpanishSyllable>` | 音节划分结果 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 批量音素转换 |
| `ToIPABatch(texts)` | `IReadOnlyList<string>` | 批量 IPA 转换 |

### MultilingualG2PEngine

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `ToPhonemes(text)` | `string` | 日英中西混合音素序列 |
| `ToSegments(text)` | `IReadOnlyList<G2PSegment>` | 带语言标签的分段 |
| `ToPhonemesBatch(texts)` | `IReadOnlyList<string>` | 批量音素转换 |
| `ToSegmentsBatch(texts)` | `IReadOnlyList<IReadOnlyList<G2PSegment>>` | 批量分段转换 |

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

### 推荐方式

```powershell
pwsh -File tools/install_naist_jdic.ps1
```

该脚本会从 OpenJTalk 发布包下载词典，并默认解压到 `%USERPROFILE%\naist-jdic`。
`MeCabTokenizer()` 与 `MultilingualG2PEngine()` 会按以下顺序自动查找词典：

1. 环境变量 `DOTNETG2P_NAIST_JDIC_PATH`
2. 环境变量 `NAIST_JDIC_PATH`
3. `%USERPROFILE%\naist-jdic`
4. 当前目录下的 `naist-jdic` 或 `open_jtalk_dic_utf_8-1.11`

### 手动准备

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
using var multiEngine = new MultilingualG2PEngine(dicPath);
```

## 西班牙语评估

西班牙语 G2P 包含基于 `ipa-dict` 与 `WikiPron` 的全量语料评估管线。

```powershell
pwsh -File tools/refresh_spanish_eval_data.ps1 -Mode Full
pwsh -File tools/run_spanish_full_evaluation.ps1 -EnforceThresholds
```

- 语料输出目录: `artifacts/spanish-eval/corpora`
- 报告输出目录: `artifacts/spanish-eval/reports/latest`
- 主要输出:
  - `summary.tsv`
  - `category_summary.tsv`
  - `mismatches/*.tsv`

截至 2026-03-09 的实测值:

- `ipa_dict_es_es_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_es_full/allophones`: PER `1.37%`, WER `13.69%`
- `ipa_dict_es_mx_full/base`: PER `1.69%`, WER `16.49%`
- `ipa_dict_es_mx_full/allophones`: PER `1.37%`, WER `13.69%`
- `wikipron_spa_latn_ca_broad_filtered_full/base`: PER `1.38%`, WER `11.14%`
- `wikipron_spa_latn_la_broad_filtered_full/base`: PER `1.43%`, WER `11.46%`

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

# 将词典安装到默认位置
pwsh -File tools/install_naist_jdic.ps1

# 控制台示例（自动解析词典：完整 G2P）
dotnet run --project samples/DotNetG2P.Console

# 控制台示例（显式指定词典路径）
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
| **DotNetG2P.English** | [Apache-2.0](LICENSE) | 英语 G2P 引擎 |
| **DotNetG2P.Chinese** | [Apache-2.0](LICENSE) | 中文 G2P 引擎 |
| **DotNetG2P.Spanish** | [Apache-2.0](LICENSE) | 西班牙语 G2P 引擎 |
| **DotNetG2P.Multilingual** | [Apache-2.0](LICENSE) | 多语言 G2P 引擎（日英中西对应） |

所有组件均以 **Apache-2.0 许可证** 提供。
有关第三方组件的许可证信息，请参阅 [NOTICE](NOTICE) 文件。
