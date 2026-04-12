# Misaki互換 中国語G2P出力モード 設計ドキュメント

> 対応Issue: [#56 - How can i make result similar like misaki does?](https://github.com/ayutaz/dot-net-g2p/issues/56)

## 背景

[Kokoro TTS](https://github.com/hexgrad/kokoro) (82Mパラメータ) はG2Pフロントエンドとして [Misaki](https://github.com/hexgrad/misaki) を使用する。C#によるKokoro推論エンジン ([KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp)) が存在するが、MisakiのC#ポートが無いため中国語G2P品質が低い (eSpeak-ng依存、KokoroSharp Issue#5)。

DotNetG2P.ChineseにMisaki互換出力モードを追加することで、C#/UnityのKokoro TTSエコシステムで採用可能になる。

## 現状の差異

`"你好"` の変換結果:

| 項目 | Misaki (Legacy) | DotNetG2P 現行 |
|------|----------------|----------------|
| 出力例 | `ni↓xau̯↓` | `ni˧˥ xaʊ˨˩˦` |
| 声調記号 | 矢印 (`→` `↗` `↓` `↘`) | IPA tone letters (`˥˥` `˧˥` `˨˩˦` `˥˩`) |
| 音節区切り | スペース (語間) | スペース (音節間) |
| 声母 j/q | `ʨ` / `ʨʰ` | `tɕ` / `tɕʰ` |
| 二重母音 | `ai̯` `au̯` `ei̯` `ou̯` (非音節化符号) | `aɪ` `aʊ` `eɪ` `oʊ` (別字母) |
| zh/ch/sh+i | `ɻ̩` / `ʐ̩` | `ɻ̩` |
| z/c/s+i | `ɹ̩` / `z̩` | `ɹ̩` |

### 声調マッピング詳細

| 声調 | DotNetG2P (Chao式) | Misaki (矢印) |
|------|-------------------|--------------|
| 1声 (陰平) | `˥˥` | `→` |
| 2声 (陽平) | `˧˥` | `↗` |
| 3声 (上声) | `˨˩˦` | `↓` |
| 4声 (去声) | `˥˩` | `↘` |
| 軽声 | なし | なし |

### 声母マッピング差異

| ピンイン | DotNetG2P | Misaki |
|---------|-----------|--------|
| j | `tɕ` | `ʨ` |
| q | `tɕʰ` | `ʨʰ` |
| x | `ɕ` | `ɕ` (同一) |

### 韻母 (二重母音) マッピング差異

| 韻母 | DotNetG2P | Misaki |
|------|-----------|--------|
| ai | `aɪ` (U+026A) | `ai̯` (i + U+032F) |
| ei | `eɪ` | `ei̯` |
| ao | `aʊ` (U+028A) | `au̯` (u + U+032F) |
| ou | `oʊ` | `ou̯` |
| uai | `uaɪ` | `uai̯` |
| ui | `ueɪ` | `uei̯` |
| iu | `ioʊ` | `iou̯` |

## 実装方式

### 方式比較

| 方式 | 概要 | 判定 |
|------|------|------|
| A: PinyinStyle に追加 | PinyinStyle はピンイン表記用 enum。IPA 出力とはレイヤーが異なる | **不採用** (責務混在) |
| **B: PinyinToMisaki.cs 新規 + ToMisakiIPA()** | PiperIpa と同パターン。独立マッピングテーブル | **採用** |
| C: ToIPA() 出力のポストプロセス | 文字列置換で変換。脆弱で将来変更に弱い | **不採用** |

### 採用: 方式B — 独立変換クラス + 専用メソッド

既存の `PinyinToIpa` / `PinyinToPiperIpa` / `PinyinToZhuyin` と同じ「変換先ごとに独立クラス」パターンに従う。

```
src/DotNetG2P.Chinese/Conversion/
├── PinyinToIpa.cs         ← 標準IPA (既存)
├── PinyinToPiperIpa.cs    ← piper-plus互換 (既存)
├── PinyinToZhuyin.cs      ← 注音符号 (既存)
└── PinyinToMisaki.cs      ← Misaki互換 (新規)
```

**選定理由:**

1. **一貫性**: 全変換クラスが独立マッピングテーブルを持つ既存設計に完全合致
2. **拡張性**: `RunPipeline` の `Func<string, string> converter` 委譲パターンにそのまま乗る
3. **保守性**: Misaki の仕様変更時にテーブル差分のみの修正で対応可能
4. **独立性**: 既存の ToIPA / ToPiperIPA 出力に一切影響しない

## 変更ファイル一覧

### 新規作成

| ファイル | 内容 |
|---------|------|
| `src/DotNetG2P.Chinese/Conversion/PinyinToMisaki.cs` | Misaki互換マッピングテーブル (声母/韻母/声調) |
| `tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiIpaTests.cs` | Misaki互換出力テスト |

### 変更

| ファイル | 内容 |
|---------|------|
| `src/DotNetG2P.Chinese/ChineseG2PEngine.cs` | `ToMisakiIPA()` / `ToMisakiIPABatch()` 公開メソッド追加 |

### 変更不要 (共通基盤)

- `PinyinParser.cs` — ピンイン解析 (共通)
- `ToneConverter.cs` — 声調変換 (共通)
- `ToneSandhiProcessor.cs` — 声調変調 (共通、結果はそのまま反映)
- `ChineseG2POptions.cs` — オプション (Separator等は既存で対応可能)
- `DotNetG2P.Multilingual/` — 初期対応では変更不要

## PinyinToMisaki.cs 設計

### 声母マッピング

```csharp
private static readonly Dictionary<Initial, string> s_initialIpa = new()
{
    [Initial.B]  = "p",
    [Initial.P]  = "pʰ",
    [Initial.M]  = "m",
    [Initial.F]  = "f",
    [Initial.D]  = "t",
    [Initial.T]  = "tʰ",
    [Initial.N]  = "n",
    [Initial.L]  = "l",
    [Initial.G]  = "k",
    [Initial.K]  = "kʰ",
    [Initial.H]  = "x",
    [Initial.J]  = "ʨ",        // DotNetG2P: tɕ → Misaki: ʨ
    [Initial.Q]  = "ʨʰ",       // DotNetG2P: tɕʰ → Misaki: ʨʰ
    [Initial.X]  = "ɕ",
    [Initial.Zh] = "ʈʂ",
    [Initial.Ch] = "ʈʂʰ",
    [Initial.Sh] = "ʂ",
    [Initial.R]  = "ɻ",
    [Initial.Z]  = "ʦ",        // DotNetG2P: ts → Misaki: ʦ
    [Initial.C]  = "ʦʰ",       // DotNetG2P: tsʰ → Misaki: ʦʰ
    [Initial.S]  = "s",
    [Initial.Y]  = "j",
    [Initial.W]  = "w",
};
```

### 韻母マッピング (二重母音の差異)

```csharp
// 主な差異: ɪ→i̯, ʊ→u̯ (非音節化符号 U+032F 付き)
[Final.Ai]  = "ai\u032F",    // aɪ → ai̯
[Final.Ei]  = "ei\u032F",    // eɪ → ei̯
[Final.Ao]  = "au\u032F",    // aʊ → au̯
[Final.Ou]  = "ou\u032F",    // oʊ → ou̯
[Final.Iao] = "iau\u032F",   // iaʊ → iau̯
[Final.Iu]  = "iou\u032F",   // ioʊ → iou̯
[Final.Uai] = "uai\u032F",   // uaɪ → uai̯
[Final.Ui]  = "uei\u032F",   // ueɪ → uei̯
```

### 声調マッピング

```csharp
private static readonly string[] s_toneArrows = new[]
{
    "",    // Neutral (0) - なし
    "→",   // First (1)
    "↗",   // Second (2)
    "↓",   // Third (3)
    "↘",   // Fourth (4)
};
```

## ChineseG2PEngine 公開API

既存の `ToPiperIPA` パターンに準拠:

```csharp
// 文字列出力
public string ToMisakiIPA(string text)
public string ToMisakiIPA(string text, bool includeTones)

// バッチ出力
public string[] ToMisakiIPABatch(string[] texts)
public string[] ToMisakiIPABatch(string[] texts, bool includeTones)
```

## テスト方針

`ChineseMisakiIpaTests.cs` で以下をカバー:

1. **声調マッピング**: 各声調 (1-4 + 軽声) が正しい矢印記号に変換されること
2. **声母マッピング**: j/q → ʨ/ʨʰ、z/c → ʦ/ʦʰ 等の差異が反映されること
3. **韻母マッピング**: 二重母音の非音節化符号が正しいこと
4. **声調変調**: ToneSandhi の結果が Misaki 出力にも反映されること
5. **エッジケース**: 軽声、er化、句読点、空文字列等
6. **Misaki 出力例との比較**: issue #56 の `ni↓xau̯↓` 等

## 備考

- Misaki には Legacy パス (IPA+矢印) と v1.1 パス (注音符号) の2つが存在するが、Kokoro-82M で使用されるのは Legacy パスのみ。本対応は Legacy パスを対象とする
- Misaki が `ꭧ` (U+AB67) を zh/ch の子音IPAに使用する件は、Kokoro vocab に含まれない可能性があるため初期対応では見送り、必要に応じて追加する
- Multilingual 層への統合は将来の追加作業とする

### Phase 1-R 実装知見

- Misaki legacy は 3-3 tone sandhi (三声連読変調) を適用しない。DotNetG2P では `EnableToneSandhi` オプションで制御可能。Misaki legacy と完全一致させるには `EnableToneSandhi = false` でエンジンを初期化する
- U+032F (COMBINING INVERTED BREVE BELOW, 非音節化符号) は Misaki テンプレート側で事前除去されるため、実際の出力には含まれない。DotNetG2P の `ToMisakiIPA()` はマッピングテーブルに U+032F を含むが、Kokoro TTS に渡す前にテンプレート処理で除去される想定
