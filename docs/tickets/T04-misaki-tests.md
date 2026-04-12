---
ticket: T04
title: Misaki互換テスト実装
milestone: Mi2
status: 未着手
depends_on: [T03]
blocks: [T05]
---

# T04 — Misaki互換テスト実装

## 1. タスク目的とゴール

### 目的
Mi2マイルストーンの2枚目として、T01-T03で実装された `PinyinToMisaki` 変換クラスおよび `ChineseG2PEngine.ToMisakiIpa()` / `ToMisakiIpaBatch()` API の品質を、網羅的な単体テストとエンドツーエンドテストで保証する。

### ゴール
1. **Misaki互換出力の正確性保証**: 声調マッピング（矢印記号）、声母マッピング（`j/q→ʨ/ʨʰ`, `z/c→ʦ/ʦʰ`）、韻母マッピング（二重母音の非音節化符号 `i̯/u̯`）がすべて仕様通りに出力されること
2. **既存936件テストへの回帰なし**: Misaki対応の変更が既存の `ToIPA` / `ToPiperIPA` / `ToZhuyin` 出力に影響を与えていないことを確認
3. **Issue #56 再現**: `"你好"` の出力が Misaki と同等の `ni↗xau̯↓` 形式で得られることを確認
4. **エッジケース網羅**: 空文字列・null・句読点のみ・英数字混在・サロゲートペア・er化音・軽声などで例外を投げず、期待通りの挙動を示すこと
5. **パイプライン連携確認**: 声調変調（三声連読、一/不の変調）の結果が Misaki 出力にも正しく反映されること

### 成功指標
- `ChineseMisakiIpaTests` が 100% パスする（最低35件以上のテストを想定）
- 既存 `dotnet test DotNetG2P.slnx` が全件パスする（936件 + 新規テスト）
- `ToMisakiIpa("你好")` が Misaki と同等の出力を返す
- 全エッジケースで例外が発生しない

---

## 2. 実装する内容の詳細

### 2-1. テストファイルの構成判断

| 方針 | 判断 |
|------|------|
| **単一ファイル集約**: `ChineseMisakiIpaTests.cs` にすべてのテストを配置 | **採用** |
| **分離**: `ChineseMisakiIpaTests.cs`（単体）+ `ChineseMisakiIntegrationTests.cs`（統合） | **不採用**（初期対応ではオーバーエンジニアリング） |
| **さらに分離**: `ChineseMisakiEdgeCaseTests.cs` の追加 | **Mi3 で検討**（マイルストーン Mi3 に記載済み） |

**採用理由**: `ChinesePiperIpaTests.cs`（512行）が単一ファイルで全カテゴリのテストを含んでいる先例に従う。保守性・レビュー効率・ファイル検索の観点で単一ファイル集約が最適。将来エッジケースが肥大化した場合のみ Mi3 で `ChineseMisakiEdgeCaseTests.cs` に分離する。

### 2-2. テストクラス構成

```csharp
namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Misaki (Kokoro TTS) 互換 IPA 変換の正確性を検証するテスト。
    /// ChineseG2PEngine の ToMisakiIpa() メソッド経由で、
    /// Misaki方式の声母・韻母IPAマッピング、特殊母音、声調矢印記号を検証する。
    /// </summary>
    public class ChineseMisakiIpaTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseMisakiIpaTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // セクション 1 - 13（後述）
    }
}
```

### 2-3. 各テストメソッド一覧（カテゴリ別）

#### セクション 1: 声調矢印マッピング（1-4声 + 軽声）

| # | メソッド名 | 入力 | 期待出力/Assert | 備考 |
|---|----------|------|---------------|------|
| 1.1 | `ToMisakiIpa_第1声_矢印右向き` | `"妈"` (mā) | `"ma\u2192"` (`ma→`) | `Assert.Equal` 完全一致 |
| 1.2 | `ToMisakiIpa_第2声_矢印右上向き` | `"麻"` (má) | `"ma\u2197"` (`ma↗`) | `Assert.Equal` 完全一致 |
| 1.3 | `ToMisakiIpa_第3声_矢印下向き` | `"马"` (mǎ, 単字で変調なし) | `"ma\u2193"` (`ma↓`) | `Assert.Equal` 完全一致 |
| 1.4 | `ToMisakiIpa_第4声_矢印右下向き` | `"骂"` (mà) | `"ma\u2198"` (`ma↘`) | `Assert.Equal` 完全一致 |
| 1.5 | `ToMisakiIpa_軽声_矢印なし` | `"吗"` (ma, 軽声) | `"ma"`（矢印なし） | `Assert.DoesNotContain` で各矢印を検証 |
| 1.6 | `ToMisakiIpa_IncludeTonesFalse_矢印なし` | `"妈"`, `includeTones=false` | `"ma"`（矢印なし） | `Assert.Equal` |
| 1.7 | `ToMisakiIpa_IPA声調letterを含まない` (Theory) | `"妈麻马骂"` 各1字 | `˥ ˦ ˧ ˨ ˩` を含まない | `Assert.DoesNotContain` 5回 |

**Theory 例:**
```csharp
[Theory]
[InlineData("\u5988", "ma\u2192")]  // 妈 → ma→
[InlineData("\u9EBB", "ma\u2197")]  // 麻 → ma↗
[InlineData("\u9A6C", "ma\u2193")]  // 马 → ma↓
[InlineData("\u9A82", "ma\u2198")]  // 骂 → ma↘
public void ToMisakiIpa_声調マッピング網羅(string hanzi, string expected)
{
    var result = _engine.ToMisakiIpa(hanzi);
    Assert.Equal(expected, result);
}
```

#### セクション 2: 声母マッピング（Misaki固有差異）

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 2.1 | `ToMisakiIpa_j声母_ʨを返す` | `"几"` (jǐ) | `Assert.Contains("\uA7B3", result)` ではなく `"\u02A8"` (ʨ, U+02A8) | Misaki固有: `tɕ` → `ʨ` |
| 2.2 | `ToMisakiIpa_q声母_ʨʰを返す` | `"七"` (qī) | `Assert.Contains("\u02A8\u02B0", result)` (ʨʰ) | Misaki固有: `tɕʰ` → `ʨʰ` |
| 2.3 | `ToMisakiIpa_x声母_ɕを返す` | `"西"` (xī) | `Assert.Contains("\u0255", result)` (ɕ) | 標準IPAと共通 |
| 2.4 | `ToMisakiIpa_z声母_ʦを返す` | `"在"` (zài) | `Assert.Contains("\u02A6", result)` (ʦ, U+02A6) | Misaki固有: `ts` → `ʦ` |
| 2.5 | `ToMisakiIpa_c声母_ʦʰを返す` | `"才"` (cái) | `Assert.Contains("\u02A6\u02B0", result)` (ʦʰ) | Misaki固有: `tsʰ` → `ʦʰ` |
| 2.6 | `ToMisakiIpa_s声母_sを返す` | `"三"` (sān) | `Assert.Contains("s", result)` | 標準IPAと共通 |
| 2.7 | `ToMisakiIpa_zh声母_ʈʂを返す` | `"知"` (zhī) | `Assert.Contains("\u0288\u0282", result)` (ʈʂ) | 標準IPAと同じ |
| 2.8 | `ToMisakiIpa_ch声母_ʈʂʰを返す` | `"吃"` (chī) | `Assert.Contains("\u0288\u0282\u02B0", result)` (ʈʂʰ) | 標準IPAと同じ |

**Theory による声母網羅:**
```csharp
[Theory]
[InlineData("\u51E0", "\u02A8")]         // 几 (jǐ): j → ʨ (Misaki固有)
[InlineData("\u4E03", "\u02A8\u02B0")]   // 七 (qī): q → ʨʰ (Misaki固有)
[InlineData("\u897F", "\u0255")]         // 西 (xī): x → ɕ (共通)
[InlineData("\u5728", "\u02A6")]         // 在 (zài): z → ʦ (Misaki固有)
[InlineData("\u624D", "\u02A6\u02B0")]   // 才 (cái): c → ʦʰ (Misaki固有)
[InlineData("\u4E09", "s")]              // 三 (sān): s → s (共通)
[InlineData("\u5988", "m")]              // 妈 (mā): m → m (共通)
[InlineData("\u7238", "p")]              // 爸 (bà): b → p (共通)
[InlineData("\u6015", "p\u02B0")]        // 怕 (pà): p → pʰ (共通)
[InlineData("\u98DE", "f")]              // 飞 (fēi): f → f (共通)
[InlineData("\u5927", "t")]              // 大 (dà): d → t (共通)
[InlineData("\u5929", "t\u02B0")]        // 天 (tiān): t → tʰ (共通)
[InlineData("\u5973", "n")]              // 女 (nǚ): n → n (共通)
[InlineData("\u6765", "l")]              // 来 (lái): l → l (共通)
[InlineData("\u5E72", "k")]              // 干 (gān): g → k (共通)
[InlineData("\u770B", "k\u02B0")]        // 看 (kàn): k → kʰ (共通)
[InlineData("\u597D", "x")]              // 好 (hǎo): h → x (共通)
public void ToMisakiIpa_声母マッピング網羅(string hanzi, string expectedInitialIpa)
{
    var result = _engine.ToMisakiIpa(hanzi);
    Assert.Contains(expectedInitialIpa, result);
}
```

#### セクション 3: 韻母マッピング（二重母音の非音節化符号）

非音節化符号は `U+032F` (combining inverted breve below)。

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 3.1 | `ToMisakiIpa_ai韻母_i非音節化を返す` | `"爱"` (ài) | `Assert.Contains("ai\u032F", result)` | DotNetG2P `aɪ` → Misaki `ai̯` |
| 3.2 | `ToMisakiIpa_ei韻母_i非音節化を返す` | `"北"` (běi) | `Assert.Contains("ei\u032F", result)` | DotNetG2P `eɪ` → Misaki `ei̯` |
| 3.3 | `ToMisakiIpa_ao韻母_u非音節化を返す` | `"好"` (hǎo, 単字) | `Assert.Contains("au\u032F", result)` | DotNetG2P `aʊ` → Misaki `au̯` |
| 3.4 | `ToMisakiIpa_ou韻母_u非音節化を返す` | `"走"` (zǒu) | `Assert.Contains("ou\u032F", result)` | DotNetG2P `oʊ` → Misaki `ou̯` |
| 3.5 | `ToMisakiIpa_iao韻母_u非音節化を返す` | `"小"` (xiǎo) | `Assert.Contains("iau\u032F", result)` | 3母音韻母 |
| 3.6 | `ToMisakiIpa_iu韻母_iou非音節化を返す` | `"六"` (liù) | `Assert.Contains("iou\u032F", result)` | DotNetG2P `ioʊ` → Misaki `iou̯` |
| 3.7 | `ToMisakiIpa_uai韻母_非音節化を返す` | `"怀"` (huái) | `Assert.Contains("uai\u032F", result)` | 3母音韻母 |
| 3.8 | `ToMisakiIpa_ui韻母_uei非音節化を返す` | `"对"` (duì) | `Assert.Contains("uei\u032F", result)` | DotNetG2P `ueɪ` → Misaki `uei̯` |
| 3.9 | `ToMisakiIpa_単母音a_非音節化符号なし` | `"啊"` (ā) | `Assert.DoesNotContain("\u032F", result)` | 単母音には付かないこと |

**Theory での網羅:**
```csharp
[Theory]
[InlineData("\u7231", "ai\u032F")]   // 爱 (ài)
[InlineData("\u5317", "ei\u032F")]   // 北 (běi)
[InlineData("\u597D", "au\u032F")]   // 好 (hǎo) ※ 単字の場合
[InlineData("\u8D70", "ou\u032F")]   // 走 (zǒu)
[InlineData("\u5C0F", "iau\u032F")]  // 小 (xiǎo)
[InlineData("\u516D", "iou\u032F")]  // 六 (liù)
[InlineData("\u6000", "uai\u032F")]  // 怀 (huái)
[InlineData("\u5BF9", "uei\u032F")]  // 对 (duì)
public void ToMisakiIpa_二重母音非音節化符号マッピング(string hanzi, string expectedFinal)
{
    var result = _engine.ToMisakiIpa(hanzi);
    Assert.Contains(expectedFinal, result);
}
```

#### セクション 4: そり舌/歯茎母音

Misaki 設計ドキュメントより: `zh/ch/sh/r+i → ɻ̩` / `ʐ̩`, `z/c/s+i → ɹ̩` / `z̩`

初期実装では両方とも単一表現（`ɻ̩` / `ɹ̩`）を採用する可能性が高いため、それに準拠。

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 4.1 | `ToMisakiIpa_zi_歯茎母音を含む` | `"子"` (zǐ) | `Assert.Contains("\u0279\u0329", result)` (ɹ̩) | z+i |
| 4.2 | `ToMisakiIpa_ci_歯茎母音を含む` | `"次"` (cì) | `Assert.Contains("\u0279\u0329", result)` (ɹ̩) | c+i |
| 4.3 | `ToMisakiIpa_si_歯茎母音を含む` | `"四"` (sì) | `Assert.Contains("\u0279\u0329", result)` (ɹ̩) | s+i |
| 4.4 | `ToMisakiIpa_zhi_そり舌母音を含む` | `"知"` (zhī) | `Assert.Contains("\u027B\u0329", result)` (ɻ̩) | zh+i |
| 4.5 | `ToMisakiIpa_chi_そり舌母音を含む` | `"吃"` (chī) | `Assert.Contains("\u027B\u0329", result)` (ɻ̩) | ch+i |
| 4.6 | `ToMisakiIpa_shi_そり舌母音を含む` | `"十"` (shí) | `Assert.Contains("\u027B\u0329", result)` (ɻ̩) | sh+i |
| 4.7 | `ToMisakiIpa_ri_そり舌母音を含む` | `"日"` (rì) | `Assert.Contains("\u027B\u0329", result)` (ɻ̩) | r+i |

#### セクション 5: 声調変調（三声連読、一/不変調）

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 5.1 | `ToMisakiIpa_三声連読_你好_前字が二声矢印` | `"你好"` | `Assert.Contains("ni\u2197", result)` (ni↗) | 3+3 → 2+3 変調 |
| 5.2 | `ToMisakiIpa_三声連読_你好_後字が三声矢印保持` | `"你好"` | `Assert.Contains("\u2193", result)` (↓) | 後字の三声矢印は残る |
| 5.3 | `ToMisakiIpa_三声連読_你也好` | `"你也好"` | `Assert.Contains("ni\u2197", result)` + 全体が3音節 | 3+3+3 連読 |
| 5.4 | `ToMisakiIpa_一変調_一个_二声矢印` | `"一个"` | `Assert.Contains("i\u2197", result)` (i↗) | 一+4声 → 2声変調 |
| 5.5 | `ToMisakiIpa_一変調_一天_四声矢印` | `"一天"` | `Assert.Contains("i\u2198", result)` (i↘) | 一+1声 → 4声変調 |
| 5.6 | `ToMisakiIpa_不変調_不要_二声矢印` | `"不要"` | `Assert.Contains("pu\u2197", result)` (pu↗) | 不+4声 → 2声変調 |
| 5.7 | `ToMisakiIpa_EnableToneSandhiFalse_你好_三声保持` | `"你好"`, sandhi=false | `Assert.Contains("ni\u2193", result)` (ni↓) | 変調無効で3声のまま |

#### セクション 6: エッジケース

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 6.1 | `ToMisakiIpa_null入力_空文字列` | `null` | `Assert.Equal("", result)` | null安全 |
| 6.2 | `ToMisakiIpa_空文字列_空文字列` | `""` | `Assert.Equal("", result)` | |
| 6.3 | `ToMisakiIpa_空白のみ_空文字列` | `"   "` | `Assert.Equal("", result)` | |
| 6.4 | `ToMisakiIpa_タブ改行_空文字列` | `"\t\n"` | `Assert.Equal("", result)` | |
| 6.5 | `ToMisakiIpa_CJK句読点のみ_空文字列` | `"，。！"` | `Assert.Equal("", result)` | |
| 6.6 | `ToMisakiIpa_数字のみ_数字パススルー` | `"123"` | `Assert.Contains("123", result)` | |
| 6.7 | `ToMisakiIpa_英数字混在_英数字パススルー` | `"OK了"` | `Assert.Contains("OK", result)` | |
| 6.8 | `ToMisakiIpa_非漢字混在_漢字部分のみ変換` | `"Hello你好"` | `Assert.Contains("Hello", result)` + `Assert.Contains("ni\u2197", result)` | |
| 6.9 | `ToMisakiIpa_er化音_儿_独立erが変換される` | `"儿"` (ér) | `Assert.NotEmpty(result)` + `Assert.Contains("\u2197", result)` (2声) | er韻母単独。Misakiでの表現は T01-T03 実装に準拠 |
| 6.10 | `ToMisakiIpa_ü母音_鱼_yが出力される` | `"鱼"` (yú) | `Assert.Contains("y", result)` | |
| 6.11 | `ToMisakiIpa_サロゲートペア_エラーなし` | `"\U00020000你好"` | `Assert.NotNull(result)` + `Assert.Contains("ni", result)` | |
| 6.12 | `ToMisakiIpa_絵文字混在_エラーなし` | `"好\U0001F600好"` | `Assert.NotEmpty(result)` | |
| 6.13 | `ToMisakiIpa_長文_全音節変換` | `"中华人民共和国"` (7字) | `Split(' ').Length == 7` | |

#### セクション 7: Issue #56 再現テスト

Issue #56 の要望通り `"你好"` が Misaki 互換形式で出力されることを確認。

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 7.1 | `ToMisakiIpa_Issue56_你好_完全一致` | `"你好"` | `Assert.Equal("ni\u2197 xau\u032F\u2193", result)` | 三声連読後: ni↗ xau̯↓ |
| 7.2 | `ToMisakiIpa_Issue56_你好_声調矢印を含む` | `"你好"` | `Assert.Contains("\u2197", result)` + `Assert.Contains("\u2193", result)` | 2声矢印と3声矢印両方 |
| 7.3 | `ToMisakiIpa_Issue56_你好_IPA声調letterを含まない` | `"你好"` | `Assert.DoesNotContain("\u02E5", result)` ... 5種類 | Misakiは矢印のみ使用 |
| 7.4 | `ToMisakiIpa_Issue56_你好_非音節化符号を含む` | `"你好"` | `Assert.Contains("\u032F", result)` | au̯ の非音節化符号 |

**注**: 7.1 の完全一致テストは T01-T03 実装完了後に期待値を実装と合わせて調整すること。Misaki Python 実装との照合が望ましい（後述の「懸念事項」参照）。

#### セクション 8: バッチ API テスト

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 8.1 | `ToMisakiIpaBatch_複数テキスト_正しい件数` | `["你好", "世界", "中国"]` | `Assert.Equal(3, results.Count)` | |
| 8.2 | `ToMisakiIpaBatch_各結果が非空` | `["妈", "爸"]` | `Assert.NotEmpty(r)` 各要素 | |
| 8.3 | `ToMisakiIpaBatch_個別呼び出しと同一結果` | `["东", "元", "六"]` | `Assert.Equal(individual, batchResults[i])` | 個別呼び出しと一致 |
| 8.4 | `ToMisakiIpaBatch_IncludeTonesFalse_矢印なし` | `["妈", "麻"]`, `includeTones=false` | `Assert.DoesNotContain("\u2192", r)` ... 全矢印 | |
| 8.5 | `ToMisakiIpaBatch_空配列_空リスト` | `Array.Empty<string>()` | `Assert.Empty(results)` | |
| 8.6 | `ToMisakiIpaBatch_混在入力_全要素が返る` | `["你好", "", null!, "世界"]` | 4要素、空/nullは空文字列 | |
| 8.7 | `ToMisakiIpaBatch_Null引数_ArgumentNullException` | `null` | `Assert.Throws<ArgumentNullException>` | |

#### セクション 9: Dispose後の動作

| # | メソッド名 | 期待Assert | 備考 |
|---|----------|-----------|------|
| 9.1 | `Dispose後_ToMisakiIpa_ObjectDisposedException` | `Assert.Throws<ObjectDisposedException>` | |
| 9.2 | `Dispose後_ToMisakiIpa_WithTones_ObjectDisposedException` | 同上 | `includeTones` オーバーロード |
| 9.3 | `Dispose後_ToMisakiIpaBatch_ObjectDisposedException` | 同上 | |
| 9.4 | `Dispose後_ToMisakiIpaBatch_WithTones_ObjectDisposedException` | 同上 | |

#### セクション 10: 複数文字テキスト（音節区切り確認）

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 10.1 | `ToMisakiIpa_複数漢字_スペース区切り` | `"中国"` | `Assert.Contains(" ", result)` | 音節間はスペース |
| 10.2 | `ToMisakiIpa_4文字_3スペース区切り` | `"你好世界"` | スペース数 == 3 | |
| 10.3 | `ToMisakiIpa_長文_文字数と音節数が一致` | `"我爱北京天安门"` | `Split(' ').Length == 7` | |

#### セクション 11: 標準IPA・piper-plus との比較

| # | メソッド名 | 入力 | 期待Assert | 備考 |
|---|----------|------|-----------|------|
| 11.1 | `ToMisakiIpa_ToIPA出力と異なる` | `"妈"` | `Assert.NotEqual(standardIpa, misakiIpa)` | 声調記号体系が異なる |
| 11.2 | `ToMisakiIpa_ToPiperIPA出力と異なる` | `"几"` | `Assert.NotEqual(piperIpa, misakiIpa)` | j声母が異なる |
| 11.3 | `ToMisakiIpa_IncludeTonesFalse時_z声母だけがpiper-plusと異なる` | `"在"`, sandhi無関係 | piper: `ts`, misaki: `ʦ` | |

#### セクション 12: 回帰確認（他APIへの影響なし）

| # | メソッド名 | 期待Assert | 備考 |
|---|----------|-----------|------|
| 12.1 | `ToIPA_回帰_Misaki実装後も変わらない` | `Assert.Equal("ma\u02E5\u02E5", engine.ToIPA("妈"))` | `ma˥˥` 既存出力 |
| 12.2 | `ToPiperIPA_回帰_Misaki実装後も変わらない` | `Assert.Equal("ma", engine.ToPiperIPA("妈"))` | 既存出力 |
| 12.3 | `ToZhuyin_回帰_Misaki実装後も変わらない` | `Assert.Equal("\u3107\u311A", engine.ToZhuyin("妈"))` | ㄇㄚ 既存出力 |

---

## 3. 実装するために必要なエージェントチームの役割と人数

| 役割 | 人数 | 担当範囲 | 所要工数目安 |
|------|------|---------|-------------|
| **テスト設計エージェント** | 1 | テストメソッドの網羅的な洗い出し、Theory入力データの設計、Unicode コードポイント検証 | 0.5日 |
| **テスト実装エージェント** | 1 | `ChineseMisakiIpaTests.cs` の実装、xUnit セマンティクス確認 | 1日 |
| **Misaki 照合エージェント** | 1 | Misaki Python 実装または論文での期待出力の調査・照合、テストデータの妥当性検証 | 0.5日 |
| **回帰テストレビュアー** | 1 | 既存936件テストへの影響確認、`dotnet test` 全件実行、差分レポート | 0.5日 |
| **レビュー担当** | 1 | コードレビュー、テストカバレッジ確認、T05 への引き継ぎ事項整理 | 0.25日 |

**合計**: 5役割 / 5人（または1人が複数役を兼務可能）。単一エンジニアなら約2.5人日。

**最小構成**: テスト実装1名 + 照合1名 の2名体制。実装は「テスト設計→実装→照合→回帰確認→レビュー」のシーケンシャルな流れで可。

---

## 4. 提供範囲とテスト項目

### 4-1. 単体テスト（Unit Tests）

`PinyinToMisaki.Convert` 静的メソッド単体のテスト。ただし `PinyinToMisaki` が `internal static` のため、以下のいずれかで対応する:

| オプション | 方針 |
|----------|------|
| **A. `InternalsVisibleTo` 属性で `DotNetG2P.Tests` に公開** | 既存の `PinyinParser` などは同様のパターンを使用している前提で確認 |
| **B. E2Eテストのみで間接検証** | `ChineseG2PEngine.ToMisakiIpa()` を経由して検証する（現実的） |

**採用**: **B 方針**（E2E 経由で検証）。理由:
- 既存 `ChinesePiperIpaTests.cs` も `PinyinToPiperIpa` を直接テストせず `ToPiperIPA()` 経由で検証している
- エンジン経由の方がリアルワールドの使用パターンに近い
- ただし、T03 実装完了後に T01-T02 で作成した `PinyinToMisaki` を直接テストする必要が生じた場合のみ A 方針に切り替え

### 4-2. エンドツーエンドテスト（E2E Tests）

`ChineseG2PEngine.ToMisakiIpa()` の全パイプラインを通すテスト。以下の処理を統合的に検証:

1. 漢字入力 → フレーズ辞書/単字辞書によるピンイン解決
2. ピンイン → 声調変調（三声連読、一/不変調）
3. ピンイン → Misaki互換IPA文字列
4. 複数漢字のスペース区切り統合

### 4-3. テスト件数の目安

| カテゴリ | 単発テスト | Theory テスト | 合計 |
|---------|----------|--------------|------|
| 1. 声調矢印マッピング | 7 | 1 (4ケース) | 11 |
| 2. 声母マッピング | 8 | 1 (17ケース) | 25 |
| 3. 韻母マッピング | 9 | 1 (8ケース) | 17 |
| 4. そり舌/歯茎母音 | 7 | - | 7 |
| 5. 声調変調 | 7 | - | 7 |
| 6. エッジケース | 13 | 0-1 (4ケース) | 13-17 |
| 7. Issue #56 再現 | 4 | - | 4 |
| 8. バッチAPI | 7 | - | 7 |
| 9. Dispose後 | 4 | - | 4 |
| 10. 複数文字テキスト | 3 | - | 3 |
| 11. 他API比較 | 3 | - | 3 |
| 12. 回帰確認 | 3 | - | 3 |
| **合計（単発）** | **75** | **30ケース** | **約105件** |

**最低目標**: 40件以上（上記のうち主要カテゴリ 1-8 のテストを最低限実装）
**標準目標**: 70件以上（全カテゴリをカバー）
**満点目標**: 100件以上（Theory の全ケースを含む）

### 4-4. 回帰テスト範囲

既存の以下テストが100%パスすること:
- `ChineseG2PEngineC4Tests.cs`（IPA/注音/バッチ）
- `ChinesePiperIpaTests.cs`（piper-plus IPA）
- `ChinesePiperIpaComparisonTests.cs`（比較）
- `ChinesePiperEdgeCaseTests.cs`（エッジケース）
- `ChinesePiperIntegrationTests.cs`（統合）
- `ChineseAccuracyTests.cs`（精度）
- その他 `tests/DotNetG2P.Tests/ChineseG2P/*.cs` の全テスト

**確認コマンド**: `dotnet test DotNetG2P.slnx --filter "FullyQualifiedName~ChineseG2P"`

---

## 5. 実装に関する懸念事項とレビュー項目

### 5-1. Misaki の正確な出力との照合方法

**懸念**: DotNetG2P のテストで「期待出力」として何を採用するかが不明確。Misaki Python 実装を動かして出力を取得する手段が確立していない。

**対策**:

| 方法 | 実現性 | 備考 |
|------|-------|------|
| A. Misaki Python 実装をローカル実行して出力を取得 | 中 | Python 環境構築が必要。CI に組み込めないので参考値として使用 |
| B. Misaki の README や論文記載の例を参照 | 高 | 量が限られるが信頼性は高い（例: `你好` → `ni↓xau̯↓`※sandhi無効時） |
| C. Kokoro TTS のテストデータから逆引き | 低 | KokoroSharp の vocab を確認できれば可能 |
| D. 設計ドキュメント (`docs/guides/misaki-compatible-chinese.md`) の仕様表に従う | 高 | プロジェクト内の単一真実源 (SSOT) として採用 |

**採用**: **D 優先 + B で補完**。設計ドキュメントの仕様表を SSOT として採用し、Misaki の README や論文記載の例（`ni↓xau̯↓` 等）で妥当性確認。

### 5-2. Unicode 正規化の影響

**懸念**: Misaki出力の文字列比較で NFC/NFD 正規化の差異により `Assert.Equal` が失敗する可能性。特に以下:
- 非音節化符号 `U+032F`（結合記号、NFD/NFC で扱いが異なる可能性）
- そり舌母音 `U+027B + U+0329`（結合記号シーケンス）

**対策**:
1. すべてのテスト期待値に Unicode エスケープ（`\uXXXX`）を使用し、ソースコードエディタの正規化を回避
2. 長いテスト期待値は複数の `Assert.Contains` で検証し、完全一致 (`Assert.Equal`) は避ける方向で設計
3. 実装側で `StringComparison.Ordinal` を明示的に使用することを T03 レビュー時にも確認

**レビュー項目**: PinyinToMisaki.cs の出力が NFC 正規化済みか確認。必要なら `string.Normalize(NormalizationForm.FormC)` を挟む。

### 5-3. テストデータの信頼性

**懸念**: 漢字→ピンイン→声調変調→Misaki IPA の各段階で使用する辞書やルールが正しく設定されていないと、期待値が実装と合っていても「実は誤った共通認識」で合格するリスク。

**対策**:
- 声調変調テストはまず `ToPinyin` / `ToIPA` の出力を確認し、既存テストでの正しい声調番号を把握してから期待値を設計
- Issue #56 のテスト (7.1) は `Assert.Equal` 完全一致ではなく、複数の `Assert.Contains` で段階的に検証（矢印記号の存在、非音節化符号の存在、子音/母音の構成要素の存在）
- レビュー時に `ITestOutputHelper` で実際の出力をログ出力し、人間が目視確認できるようにする

**レビュー項目**:
- 声調変調が含まれるテストは `enableToneSandhi` のデフォルト値を明示的に確認
- 多音字（`好`, `行`, `一`, `不` 等）のテストはフレーズ辞書の影響を考慮
- `ITestOutputHelper` を使ったデバッグ出力の追加を検討

### 5-4. T01-T03 の実装詳細に依存するテスト

**懸念**: テスト実装時点で `PinyinToMisaki` / `ToMisakiIpa()` の正確なシグネチャが確定していないと、テストコードがコンパイルエラーになる。

**対策**:
- **T03 完了後に着手** (`depends_on: [T03]` として明示)
- T03 で `PinyinToMisaki.cs` と `ChineseG2PEngine.cs` が確定したことを確認してから実装開始
- シグネチャの変更が発生した場合は T04 のテストも同期修正

### 5-5. 既存テストへの影響

**懸念**: 新しいテストファイル追加に伴い xUnit のコンストラクタ/Dispose でリソース競合が起きる可能性（低い）。

**対策**: `ChinesePiperIpaTests.cs` と同じ `IDisposable` パターンで `ChineseG2PEngine` を使い捨てする。並列実行による辞書ロード競合は既存テストでも起きていないため問題なし。

### 5-6. レビュー項目チェックリスト

- [ ] すべてのテストメソッド名が日本語で、内容を明確に表現している
- [ ] `[Theory]` の `InlineData` で Unicode エスケープを統一している
- [ ] コンストラクタとDispose でリソース管理が正しく行われている
- [ ] 各セクションコメントが `// ===...===` 形式で統一されている
- [ ] `Assert.Equal` vs `Assert.Contains` の使い分けが妥当（完全一致はリスクが高いので Contains を優先）
- [ ] テストの独立性が保たれている（順序依存なし）
- [ ] 回帰テスト（セクション12）が存在している
- [ ] Issue #56 の再現テスト（セクション7）が存在している
- [ ] バッチAPIテスト（セクション8）が存在している
- [ ] Dispose後テスト（セクション9）が存在している
- [ ] xUnit の `ITestOutputHelper` を活用してデバッグログを残している（任意、推奨）

---

## 6. 一から作り直すとしたら

現状の「既存 `ChinesePiperIpaTests.cs` のパターンを踏襲する」アプローチは保守性・レビュー容易性が高く妥当。しかし、もし白紙から設計するなら以下のアプローチも検討できる。

### 6-1. パラメタライズドテスト中心の設計

現行案は `[Fact]` と `[Theory]` が混在しているが、初期設計から `[Theory]` + 大規模 `InlineData` に寄せることで:
- テスト本体のコード量を削減
- 新ケース追加時のコスト削減
- テストカバレッジの可視化が容易

**例**:
```csharp
[Theory]
[InlineData("妈", "ma\u2192", "声調1声")]
[InlineData("麻", "ma\u2197", "声調2声")]
[InlineData("马", "ma\u2193", "声調3声")]
[InlineData("骂", "ma\u2198", "声調4声")]
[InlineData("吗", "ma",       "軽声")]
public void ToMisakiIpa_声調網羅(string hanzi, string expected, string description)
{
    var result = _engine.ToMisakiIpa(hanzi);
    Assert.Equal(expected, result);
}
```

### 6-2. TSV データ駆動テスト

Portuguese/Spanish パッケージで採用されている方式:
- `tests/DotNetG2P.Tests/ChineseG2P/data/misaki_expected.tsv` に `漢字\tピンイン\tMisakiIPA` 形式で 100 ケース以上
- `MemberData` で TSV を読み込んで `[Theory]` に流し込む
- Misaki Python 実装の出力を元にした実データを蓄積できる

**メリット**:
- 大量のリアルデータでカバレッジ向上
- 実装者が手でコードを書かずにデータのみで拡張可能
- 他言語パッケージとの設計統一

**デメリット**:
- 初期セットアップ工数が増える
- TSV の生成元（Misaki実装）に依存

**判断**: 本 T04 では採用見送り。Mi3 (`ChineseMisakiEdgeCaseTests.cs` または `ChineseMisakiDatasetTests.cs`) で検討する。

### 6-3. Misaki との差分レポート自動生成ツール

`tools/DotNetG2P.MisakiEval/` を新規作成し、以下を自動化:
1. 頻出漢字 1000 語リストを `ToMisakiIpa()` で変換
2. Misaki Python 実装と比較
3. 差分率（Phone Error Rate 相当）をレポート

**利点**:
- 品質を定量的に保証
- Misaki の仕様変更時の影響を定量把握
- PER 0% を目標値として追跡可能

**判断**: Mi3 で検討。本 T04 のスコープ外（テスト実装のみ）。

### 6-4. Snapshot Testing の導入

`Verify.Xunit` などのスナップショットテストライブラリを使用:
- 初回実行時に実際の出力を `.verified.txt` として保存
- 2回目以降は差分チェック
- 意図的な変更時のみ `.received.txt` を承認

**利点**:
- テストコードが最小化
- 差分の可視化
- Issue #56 のような「期待値が動的」な状況に強い

**デメリット**:
- 新しい依存パッケージの追加が必要
- 既存パターンから逸脱

**判断**: 採用見送り。既存パターンとの整合性を優先。

### 6-5. カテゴリ分割の再設計

現状は単一ファイルだが、テスト件数が 100 件を超えた場合:
- `ChineseMisakiInitialsTests.cs` — 声母マッピング
- `ChineseMisakiFinalsTests.cs` — 韻母マッピング
- `ChineseMisakiTonesTests.cs` — 声調
- `ChineseMisakiSandhiTests.cs` — 声調変調
- `ChineseMisakiIpaTests.cs` — 統合・Issue#56再現
- `ChineseMisakiEdgeCaseTests.cs` — エッジケース

**判断**: Mi3 で検討。本 T04 では単一ファイル集約で開始し、保守性に問題が出たら分割する。

### テスト戦略の追加レビュー

本節は T04 の「一から作り直すとしたら」セクション (6-1 〜 6-5) をテスト戦略エンジニア視点で再レビューし、現在の記載内容に対する評価と具体的な改善案を追記したものである。

#### A. 現在の記載内容の評価

| 項目 | 現状記載の評価 | 課題 |
|------|---------------|------|
| 6-1 パラメタライズドテスト中心 | 方向性は正しいが `InlineData` へのベタ書き前提で、データとロジックが混在する設計になっている | テスト追加の度にソース編集が必要、非エンジニアが期待値を更新できない |
| 6-2 TSV データ駆動テスト | Portuguese/Spanish の例に言及されているが「Mi3 で検討」として先送り | T04 時点で `ChinesePiperIpaTests.cs` (512行) 相当のベタ書きが量産されるリスク。後から移行コストが増す |
| 6-3 Misaki 差分レポート自動生成ツール | `tools/DotNetG2P.MisakiEval/` 新規作成の構想があるだけでスコープ外扱い | Python 依存をどう切り離すか、CI で回せる範囲はどこかの議論が欠落 |
| 6-4 Snapshot Testing | 「既存パターンから逸脱」との理由だけで棄却 | Unicode 結合記号 (`U+032F`, `U+0329`) を含む長い期待値には Snapshot が相性抜群であり、機械的棄却は勿体ない |
| 6-5 カテゴリ分割 | 将来検討で妥当 | 判断時期の基準が「保守性に問題が出たら」と曖昧で、誰が判断するか不明 |

**総合評価**: 「現状維持 + Mi3 送り」の判断が多く、T04 段階での改善余地を捨ててしまっている。既存の `KoreanBenchmarkSeedEvaluationTests.cs` + `KoreanBenchmarkDataLoader.cs` がまさに本ライブラリ内の TSV + MemberData パターンの確立例であり、これを参照しない理由はない。以下で T04 スコープ内で追加すべき具体策を示す。

#### B. TSV データ駆動テストの具体的な実装コード

既存の `KoreanBenchmarkDataLoader` / `PortugueseDatasetEvaluationTests.cs` を手本に、以下の構成を T04 に追加することを提案する。

**B-1. TSV ファイル配置**

```
tests/TestData/ChineseG2P/
├── misaki_tones.tsv         # 声調矢印マッピング（セクション1相当）
├── misaki_initials.tsv      # 声母マッピング（セクション2相当）
├── misaki_finals.tsv        # 韻母マッピング（セクション3相当）
├── misaki_apical_vowels.tsv # そり舌/歯茎母音（セクション4相当）
├── misaki_sandhi.tsv        # 声調変調（セクション5相当）
├── misaki_issue56.tsv       # Issue #56 再現（セクション7相当）
└── README.md                # データ生成元・ライセンス・更新手順
```

各 TSV のヘッダ例（6カラム、欠損は空文字）:

```tsv
input	expected_equal	expected_contains	options	category	notes
妈	ma→		default	tone-1	第1声矢印
麻	ma↗		default	tone-2	第2声矢印
爱		ai̯	default	final-ai	二重母音非音節化符号
你好	ni↗ xau̯↓		default	sandhi-3+3	三声連読
一个		i↗	default	yi-sandhi	一+4声→2声変調
妈	ma		include_tones=false	tone-off	声調記号抑制
你好	ni↓ xau̯↓		sandhi=false	sandhi-disabled	変調無効化
```

設計ポイント:
- `expected_equal` と `expected_contains` は**排他**（片方だけ使う）。`Assert.Equal` で落ちやすい Unicode 正規化問題を回避するため、基本は `expected_contains` を推奨
- `options` カラムで `includeTones` や `enableToneSandhi` を切り替え可能にし、セクション5-7, 1-6 を統合
- `category` は xUnit の Trait 相当で、後でフィルタ実行可能
- ファイルは UTF-8 (BOM なし) で保存し、CI で BOM 検出を行う

**B-2. データローダの実装**

```csharp
// tests/DotNetG2P.Tests/ChineseG2P/MisakiData/MisakiTestCase.cs
namespace DotNetG2P.Tests.ChineseG2P.MisakiData
{
    internal sealed record MisakiTestCase(
        string DatasetFileName,
        string Input,
        string? ExpectedEqual,
        string? ExpectedContains,
        MisakiTestOptions Options,
        string Category,
        string Notes);

    internal sealed record MisakiTestOptions(
        bool IncludeTones = true,
        bool EnableToneSandhi = true)
    {
        public static MisakiTestOptions Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "default")
                return new MisakiTestOptions();

            var includeTones = true;
            var sandhi = true;
            foreach (var kv in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = kv.Split('=', 2);
                if (pair.Length != 2) continue;
                switch (pair[0].Trim())
                {
                    case "include_tones":
                        includeTones = bool.Parse(pair[1]);
                        break;
                    case "sandhi":
                        sandhi = bool.Parse(pair[1]);
                        break;
                }
            }
            return new MisakiTestOptions(includeTones, sandhi);
        }
    }
}

// tests/DotNetG2P.Tests/ChineseG2P/MisakiData/MisakiTestCaseLoader.cs
namespace DotNetG2P.Tests.ChineseG2P.MisakiData
{
    internal static class MisakiTestCaseLoader
    {
        private const string ExpectedHeader =
            "input\texpected_equal\texpected_contains\toptions\tcategory\tnotes";

        private static readonly string[] s_datasetFiles =
        {
            "misaki_tones.tsv",
            "misaki_initials.tsv",
            "misaki_finals.tsv",
            "misaki_apical_vowels.tsv",
            "misaki_sandhi.tsv",
            "misaki_issue56.tsv",
        };

        public static IReadOnlyList<MisakiTestCase> LoadAllCases()
        {
            var cases = new List<MisakiTestCase>();
            foreach (var fileName in s_datasetFiles)
                cases.AddRange(LoadCases(fileName));
            return cases;
        }

        public static IReadOnlyList<MisakiTestCase> LoadCases(string fileName)
        {
            var path = ResolveDataPath(fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Misaki test data not found: {path}", path);

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0 || lines[0] != ExpectedHeader)
                throw new InvalidDataException($"Unexpected header in {fileName}: {(lines.Length > 0 ? lines[0] : "(empty)")}");

            var cases = new List<MisakiTestCase>(lines.Length);
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length != 6)
                    throw new InvalidDataException($"Expected 6 columns in {fileName} line {i + 1}, got {parts.Length}");

                var expectedEqual = string.IsNullOrEmpty(parts[1]) ? null : parts[1];
                var expectedContains = string.IsNullOrEmpty(parts[2]) ? null : parts[2];
                if (expectedEqual == null && expectedContains == null)
                    throw new InvalidDataException($"Both expected_equal and expected_contains empty at {fileName}:{i + 1}");

                cases.Add(new MisakiTestCase(
                    DatasetFileName: fileName,
                    Input: parts[0],
                    ExpectedEqual: expectedEqual,
                    ExpectedContains: expectedContains,
                    Options: MisakiTestOptions.Parse(parts[3]),
                    Category: parts[4],
                    Notes: parts[5]));
            }
            return cases;
        }

        private static string ResolveDataPath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                    "tests", "TestData", "ChineseG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                    "TestData", "ChineseG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "ChineseG2P", fileName)),
            };
            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full)) return full;
            }
            return candidates[0];
        }
    }
}
```

**B-3. MemberData 方式の Theory テスト**

```csharp
// tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiIpaTests.cs の一部
public class ChineseMisakiIpaTests : IDisposable
{
    private readonly ChineseG2PEngine _engine = new();
    private readonly ITestOutputHelper _output;

    public ChineseMisakiIpaTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _engine.Dispose();

    // MemberData はカテゴリ別に分割するとログで追跡しやすい
    public static IEnumerable<object[]> TonesCases()
        => MisakiTestCaseLoader.LoadCases("misaki_tones.tsv")
            .Select(c => new object[] { c });

    public static IEnumerable<object[]> InitialsCases()
        => MisakiTestCaseLoader.LoadCases("misaki_initials.tsv")
            .Select(c => new object[] { c });

    // ... Finals / ApicalVowels / Sandhi / Issue56 も同様

    [Theory]
    [MemberData(nameof(TonesCases))]
    [MemberData(nameof(InitialsCases))]
    [MemberData(nameof(FinalsCases))]
    [MemberData(nameof(ApicalVowelsCases))]
    [MemberData(nameof(SandhiCases))]
    [MemberData(nameof(Issue56Cases))]
    public void ToMisakiIpa_DataDriven(MisakiTestCase c)
    {
        var result = _engine.ToMisakiIpa(
            c.Input,
            includeTones: c.Options.IncludeTones,
            enableToneSandhi: c.Options.EnableToneSandhi);

        _output.WriteLine($"[{c.DatasetFileName}/{c.Category}] {c.Input} => {Escape(result)} (expected {Describe(c)})");

        if (c.ExpectedEqual is not null)
            Assert.Equal(c.ExpectedEqual, result);
        if (c.ExpectedContains is not null)
            Assert.Contains(c.ExpectedContains, result, StringComparison.Ordinal);
    }

    private static string Escape(string s)
        => string.Concat(s.Select(ch => ch < 0x20 || ch > 0x7E
            ? $"\\u{(int)ch:X4}" : ch.ToString()));

    private static string Describe(MisakiTestCase c)
        => c.ExpectedEqual is not null
            ? $"equal={Escape(c.ExpectedEqual)}"
            : $"contains={Escape(c.ExpectedContains!)}";
}
```

注記:
- xUnit v2.9.3 は単一テストメソッドに複数の `[MemberData]` を積めるため、上記の 1 メソッドで 6 カテゴリを網羅できる
- `MisakiTestCase` は `record` (C# 9+) で `IXunitSerializable` 相当の動作を得られる。必要なら `record` 継承クラスに `ToString` を実装し、xUnit のテスト名に期待値が表示されるようにする
- `MemberData` を使うと **xUnit のテストエクスプローラ上で個別のサブテストとして並列実行される** ため、CI 並列化の恩恵を自動で受ける

**B-4. TSV 追加・レビュー運用**

- `tests/TestData/ChineseG2P/README.md` に「新規ケース追加手順」「Misaki Python 実装との照合方法」「文字コード要件 (UTF-8, no BOM, LF)」を明記
- Git の `.gitattributes` で `tests/TestData/ChineseG2P/*.tsv text eol=lf working-tree-encoding=UTF-8` を設定し、Windows での CRLF 混入を防止
- PR レビュー時は TSV 差分を `git diff --color-words` でレビュアーが見やすい形で確認

#### C. Verify.Xunit による Snapshot Testing の適用例

セクション 6-4 では「既存パターンから逸脱」として棄却されているが、以下の**限定的な用途**では Snapshot Testing が著しく有効である。

**C-1. 適用すべきシナリオ**

1. **複合シナリオの長文出力**: `"你好世界，我爱北京天安门。"` のような複数音節+句読点混在テキスト。完全一致を `Assert.Equal` で書くと Unicode エスケープで可読性が壊滅する
2. **Issue #56 の完全一致検証**: `"你好"` の 4 パターン (`ToMisakiIpa` / `includeTones=false` / `sandhi=false` / バッチAPI) をまとめて記録
3. **既存 API への回帰**: `ToIPA` / `ToPiperIPA` / `ToZhuyin` を含む 4 API × 頻出 100 漢字のマトリクスを 1 ファイルにまとめる

**C-2. 実装例 (Verify.Xunit 28.x 系)**

```csharp
// tests/DotNetG2P.Tests/DotNetG2P.Tests.csproj に追加:
// <PackageReference Include="Verify.Xunit" Version="28.*" />

// tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiSnapshotTests.cs
[UsesVerify]
public class ChineseMisakiSnapshotTests : IDisposable
{
    private readonly ChineseG2PEngine _engine = new();
    public void Dispose() => _engine.Dispose();

    [Fact]
    public Task Issue56_你好_全パターン()
    {
        var result = new
        {
            Default      = _engine.ToMisakiIpa("你好"),
            NoTones      = _engine.ToMisakiIpa("你好", includeTones: false),
            NoSandhi     = _engine.ToMisakiIpa("你好", enableToneSandhi: false),
            BatchDefault = _engine.ToMisakiIpaBatch(new[] { "你好" }).ToArray(),
        };
        return Verify(result)
            .UseDirectory("Snapshots")
            .UseFileName("Issue56_你好");
    }

    [Fact]
    public Task APIMatrix_頻出漢字100()
    {
        var hanzi = new[] { "的", "一", "是", "不", "了", /* ... 100字 */ };
        var matrix = hanzi.Select(h => new
        {
            Hanzi    = h,
            Standard = _engine.ToIPA(h),
            Piper    = _engine.ToPiperIPA(h),
            Misaki   = _engine.ToMisakiIpa(h),
            Zhuyin   = _engine.ToZhuyin(h),
        }).ToArray();
        return Verify(matrix).UseDirectory("Snapshots");
    }
}
```

**C-3. 運用ルール**

- `Snapshots/Issue56_你好.verified.txt` を Git 管理、`.received.txt` を `.gitignore` に追加
- 意図的な変更時は `dotnet test --environment Verify.AutoVerify=true` で一括承認
- CI では `--environment DiffEngine_Disabled=true` を設定して diff ツールの起動を抑制
- Snapshot の差分レビューは人間必須（自動マージ禁止）

**C-4. 既存パターンとの共存**

- 既存の `[Fact]` / `[Theory]` スタイルは維持し、Snapshot は**補完的に**使用
- 1 ファイル内にのみ Verify 依存を閉じ込めることで、他のテストへの波及を最小化
- `[UsesVerify]` 属性を付けたクラスだけが Verify を使うため、ライブラリ追加のリスクは限定的

**判断案の修正**: 6-4 の「採用見送り」は上記 (C-1) のシナリオに限っては**見直しを推奨**。最低でも Issue #56 の 4 パターン記録は Snapshot の方が明らかに保守性が高い。

#### D. Misaki 差分レポート自動生成ツールの設計

セクション 6-3 の `tools/DotNetG2P.MisakiEval/` 構想を具体化する。既存の `tools/DotNetG2P.PortugueseEval/` が手本になる。

**D-1. ツール全体構成**

```
tools/DotNetG2P.MisakiEval/
├── DotNetG2P.MisakiEval.csproj     # net8.0, OutputType=Exe, Core/Chinese 参照
├── Program.cs                       # CLI エントリ
├── MisakiCorpusLoader.cs            # TSV コーパスのロード
├── MisakiEvaluator.cs               # 予測と参照の比較、距離計算
├── DiffReportWriter.cs              # Markdown/TSV/JSON レポート出力
├── EvalThresholds.cs                # misaki_eval_thresholds.json の型
└── ReferenceProviders/
    ├── IReferenceProvider.cs        # 参照音素列の提供抽象
    ├── StaticTsvReferenceProvider.cs # 事前生成 TSV
    └── PythonBridgeProvider.cs      # Python Misaki 実装を subprocess 呼び出し (開発環境限定)

tools/
├── misaki_eval_thresholds.json       # データセット毎の PER 閾値
├── refresh_misaki_eval_data.ps1      # artifacts/misaki-eval/corpora 再生成
└── run_misaki_full_evaluation.ps1    # フル評価 + レポート生成
```

**D-2. Python 依存の切り離し方針**

| 段階 | 参照源 | 環境 | 備考 |
|------|-------|------|------|
| 1. 静的 TSV (推奨) | `artifacts/misaki-eval/corpora/misaki_reference_*.tsv` | 任意 | Misaki Python 実装で**事前に**生成した参照を Git 管理外の artifacts に保存。CI でダウンロード |
| 2. Python bridge (開発者向け) | `python -m misaki_cli ...` | Python 環境あり | 開発者のローカル検証用。CI 禁止 |
| 3. テスト統合 (`SkippableFact`) | 静的 TSV が存在する場合のみ動作 | CI 含む全環境 | `PortugueseDatasetEvaluationTests.cs` と同じ `SkippableFact` パターン |

**D-3. CLI 仕様**

```bash
# フル評価
dotnet run --project tools/DotNetG2P.MisakiEval -- \
  --corpus-dir artifacts/misaki-eval/corpora \
  --output-dir artifacts/misaki-eval/reports/$(date -u +%Y%m%d-%H%M%S) \
  --thresholds tools/misaki_eval_thresholds.json \
  --enforce-thresholds

# 差分比較のみ(閾値無視)
dotnet run --project tools/DotNetG2P.MisakiEval -- \
  --mismatch-limit 100 \
  --categories tone-sandhi,apical-vowel
```

**D-4. レポート構造 (出力例)**

```
artifacts/misaki-eval/reports/20260412-120000/
├── summary.tsv        # dataset, profile, cases, PER, WER, exact_match_rate
├── summary.json       # 上記の JSON 版
├── mismatches/
│   ├── frequency_1000__default.tsv   # 頻出 1000 字で一致しなかったケース
│   └── sandhi_patterns__default.tsv
├── categories.tsv     # カテゴリ別平均距離 (initial / final / tone / sandhi)
└── report.md          # 人間可読の日本語レポート (PR コメント投入用)
```

`summary.tsv` の列定義:

```tsv
dataset	profile	cases	exact_match	per	wer	threshold	passed
frequency_1000	default	1000	945	0.0082	0.0550	0.01	true
sandhi_patterns	default	150	148	0.0031	0.0133	0.01	true
issue56_variants	default	4	4	0.0000	0.0000	0.00	true
```

**D-5. CI での扱い**

- **PR CI (必須)**: `tests/DotNetG2P.Tests` 内の `MisakiDatasetEvaluationTests` (`SkippableFact`) を実行。TSV が存在しないときは Skip
- **Nightly (任意)**: `tools/DotNetG2P.MisakiEval` をフル実行し、Markdown レポートを artifact として公開
- **リリース時**: PER 閾値超過で Red。`misaki_eval_thresholds.json` の変更は別 PR で明示レビュー

**D-6. Mi3 送りではなく T04 段階で着手すべきか**

現状 6-3 は「Mi3 で検討」だが、**最小構成 (`IReferenceProvider` + 静的 TSV 1 本 + `summary.tsv` 出力のみ)** なら T04 スコープ内に追加可能。Issue #56 の `"你好"` 4 パターン計測だけでも PR への根拠提示になり、T05 ドキュメント更新の材料になる。

#### E. 既存 Piper/IPA テストへの波及 (統一戦略として適用可能か)

現状 `ChinesePiperIpaTests.cs` (512行) も InlineData ベタ書きで、`ChinesePiperIpaComparisonTests.cs` と合わせて同種の保守性問題を抱えている。本レビューで提案する TSV + MemberData 方式は**統一戦略として横展開可能**である。

**E-1. 移行ロードマップ (推奨順)**

| 順序 | 対象 | 内容 | 対象マイルストーン |
|------|------|------|-------------------|
| 1 | `ChineseMisakiIpaTests` (新規) | TSV 駆動で**最初から**実装 | T04 (本チケット) |
| 2 | `ChinesePiperIpaComparisonTests` | `misaki_standard_piper_diff.tsv` に統合し、3-API 差分を 1 ファイルで表現 | Mi3 |
| 3 | `ChinesePiperIpaTests` | カテゴリ別 TSV (`piper_initials.tsv`, `piper_finals.tsv`, `piper_apical_vowels.tsv`, `piper_edge_cases.tsv`) に移行 | Mi3 |
| 4 | `ChineseG2PEngineC4Tests` (IPA/注音) | `standard_ipa_{initials,finals}.tsv` / `zhuyin_map.tsv` に移行 | Mi4 |
| 5 | `MisakiTestCase` を `ChineseG2PTestCase` に一般化 | `target` カラム (standard/piper/misaki/zhuyin) を追加し、1 つのローダで 4 API すべて扱う | Mi4 以降 |

**E-2. 共通化する際のポイント**

- TSV ヘッダを**全 API 共通**に揃える (`input\ttarget\texpected_equal\texpected_contains\toptions\tcategory\tnotes`)
- `target` カラムで `ToIPA` / `ToPiperIPA` / `ToMisakiIpa` / `ToZhuyin` を切り替え
- ローダは `Dictionary<string, Func<ChineseG2PEngine, string, string, string>>` で API 毎のアダプタを持つ
- テストクラスは API 毎に分離 (`ChineseIpaTests` / `ChinesePiperIpaTests` / `ChineseMisakiIpaTests` / `ChineseZhuyinTests`) するが、**ローダとケース型は共通** にする

**E-3. 波及による効果 (定量)**

| 指標 | 現状 (InlineData) | TSV 駆動後 (推定) |
|------|------------------|-----------------|
| `tests/DotNetG2P.Tests/ChineseG2P/*.cs` 総行数 | 約 2,500 行 | 約 800 行 (68% 削減) |
| 新規テストケース追加コスト | C# 編集 + リビルド必須 | TSV 編集のみ |
| 非エンジニアによるレビュー | 困難 | TSV は表形式で容易 |
| CI 並列実行での粒度 | クラス単位 | 個別ケース単位 (MemberData) |
| Unicode エスケープの可読性 | `\u032F` がソース散在 | TSV は実文字で記述可能 |

**E-4. 横展開のリスクと対策**

| リスク | 対策 |
|-------|------|
| Mi3/Mi4 で既存テストを書き換えると PR が巨大化 | カテゴリ毎に小 PR に分割。1 PR あたり 1 TSV を原則とする |
| TSV 編集ミスでテストが静かに Skip される | ローダで **件数の下限アサート**を入れる (`Assert.True(cases.Count >= 10)`) |
| xUnit の `MemberData` は `static` メソッドが必要で、`DotNetG2P.Tests` の既存構造を変更しない | `ChineseG2PTestDataLoaders` 静的クラスを `tests/DotNetG2P.Tests/ChineseG2P/TestData/` に新設して吸収 |
| レビュアーが TSV 差分を見落とす | PR テンプレートに「TSV 追加・編集時は diff を本文に貼る」を明記 |

#### F. 結論とアクションアイテム

**現状 T04 記載に対する判断の更新提案**:

| セクション | 現状判断 | 提案 |
|-----------|---------|------|
| 6-1 パラメタライズド | 既に Theory 使用 | 維持 + TSV 併用 |
| 6-2 TSV データ駆動 | Mi3 送り | **T04 で採用** (上記 B 案) |
| 6-3 差分レポートツール | Mi3 送り | **T04 で最小構成を着手** (上記 D-6) |
| 6-4 Snapshot Testing | 棄却 | **Issue #56 限定で採用**再検討 (上記 C-1) |
| 6-5 カテゴリ分割 | Mi3 送り | 維持 (TSV 採用で単一ファイルでも十分) |

**T04 完了条件に追加すべきアイテム**:

- [ ] `tests/TestData/ChineseG2P/` に最低 6 本の TSV を作成し、合計 100 ケース以上を収録
- [ ] `MisakiTestCaseLoader` と `ChineseMisakiIpaTests` を TSV + MemberData 方式で実装
- [ ] `tests/TestData/ChineseG2P/README.md` にデータ生成元と更新手順を記載
- [ ] `.gitattributes` に TSV 用のエンコーディング/改行コード設定を追加
- [ ] (任意) `tools/DotNetG2P.MisakiEval` の最小構成 (静的 TSV ベース) を追加し、`summary.tsv` 出力を確認
- [ ] (任意) `Verify.Xunit` を参照に追加し、Issue #56 用の Snapshot テスト 1 ファイルのみ作成

### システム統合観点の追加レビュー

本節は、T04 のテスト設計を「DotNetG2P.Multilingual（多言語ファサード）／Unity UPM／NuGet／KokoroSharp」との連携前提でレビューした結果と、テスト統合観点での改善案を示す。上記 §テスト戦略の追加レビュー が「`ChineseG2PEngine` 単体のテスト品質」に主眼を置いていたのに対し、本節は **テスト対象の外** — 上位層・配布チャネル・下流ランタイム — からテストをどう検証するかに焦点を当てる。T04 の成果物は `ChineseG2PEngine.ToMisakiIpa` の正確性だけでなく、それが Multilingual 経由・Unity ランタイム上・KokoroSharp 統合で期待通り動くことを保証する必要がある。

#### A. Multilingual 層を経由したテスト（将来の布石）

現状の T04 スコープ（§2-1〜2-3）は `ChineseG2PEngine` 単体のみを対象にしており、`MultilingualG2PEngine` 経由の Misaki 出力テストは一切含まれていない。これは T03 が Multilingual 層を触らないためで妥当だが、Mi3 で Multilingual 統合が実装された際にテストが後追いになるリスクがある。

T04 の段階で以下の「**将来に備えた空テストクラス**」を用意しておくと、Mi3 実装時のテストカバレッジ漏れを防げる:

```csharp
namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual 層経由の Misaki 互換出力テスト。
    /// T04 時点では Chinese 単体のみが Misaki 対応しているため、
    /// 多言語混在テキストのテストは Mi3 で有効化する。
    /// </summary>
    public class MultilingualMisakiIpaTests : IDisposable
    {
        private readonly MultilingualG2PEngine _engine;

        public MultilingualMisakiIpaTests()
        {
            _engine = new MultilingualG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        [Fact(Skip = "Mi3: Multilingual 層への Misaki 統合実装後に有効化")]
        public void ToMisakiIpa_Chinese単独セグメント_Misaki出力()
        {
            // var result = _engine.ToMisakiIpa("你好");
            // Assert.Contains("\u2197", result);
        }

        [Fact(Skip = "Mi3: 中英混在 + フォールバック処理実装後")]
        public void ToMisakiIpa_中英混在_Chinese部分のみMisaki_English部分はIPA()
        {
            // var result = _engine.ToMisakiIpa("你好 Hello 世界");
            // Assert.Contains("ni\u2197", result);  // Chinese 部分は Misaki
            // Assert.Contains("həloʊ", result);     // English 部分は標準 IPA
        }

        [Fact(Skip = "Mi3: TryGetMisakiIpa 実装後")]
        public void TryGetMisakiIpa_Korean単独_false返却()
        {
            // Assert.False(_engine.TryGetMisakiIpa("안녕", out _));
        }
    }
}
```

**判断**: T04 では **`Skip` 属性付きのプレースホルダクラスのみ配置**し、実装は Mi3 に委ねる。これにより:

1. テストファイルのディレクトリ構造が Mi3 時点で確定し、ファイル作成の PR が不要になる
2. `Skip` 理由に「Mi3」と明記することで、後続担当者が実装順序を把握できる
3. 既存 `MultilingualG2PEngineTests` のパターン（`src/DotNetG2P.Multilingual/MultilingualG2PEngine.cs` を参照するテスト）に合流できる

#### B. Unity ランタイムでの動作検証テスト

T04 のテストは `dotnet test DotNetG2P.slnx`（net8.0 環境）でのみ実行される。Unity IL2CPP ビルドで実際にランタイムエラーが起きないかは、通常の xUnit テストでは検出できない。この観点で以下のテストを追加することを推奨する:

**B-1. `[Preserve]` 属性の存在確認テスト（静的解析）**

```csharp
[Fact]
public void ChineseG2PEngine_Preserve属性が付与されている()
{
    var type = typeof(ChineseG2PEngine);
    var attrs = type.GetCustomAttributes(inherit: false);
    var hasPreserve = attrs.Any(a => a.GetType().FullName == "UnityEngine.Scripting.PreserveAttribute");
    Assert.True(hasPreserve,
        $"{type.FullName} に [Preserve] が付与されていません。Unity IL2CPP 環境で strip される可能性があります。");
}

[Fact]
public void ChineseG2PEngine_ToMisakiIpa_メソッドが公開されている()
{
    var type = typeof(ChineseG2PEngine);
    var method = type.GetMethod(nameof(ChineseG2PEngine.ToMisakiIpa),
        new[] { typeof(string), typeof(bool) });
    Assert.NotNull(method);
    Assert.True(method!.IsPublic);
    // public メソッドはクラスレベル [Preserve] により自動保護される
}
```

このテストは実装コードの変更ではなくリフレクションによる構造検証のみのため、`DotNetG2P.Tests` (net8.0) でそのまま動く。Unity プロジェクトへの依存は一切ない。

**B-2. Unity ビルドスモークテスト（CI 統合）**

`tests/DotNetG2P.Tests/ChineseG2P/` とは別に、`.github/workflows/ci.yml` にジョブを追加:

```yaml
unity-il2cpp-smoke:
  runs-on: ubuntu-latest
  needs: [test]
  steps:
    - name: Unity IL2CPP ビルド検証
      uses: game-ci/unity-builder@v4
      with:
        unityVersion: 2022.3.20f1
        targetPlatform: StandaloneLinux64
        buildMethod: DotNetG2P.Tests.Unity.SmokeBuilder.Build
        # Packages/com.dotnetg2p.chinese を含む最小 Unity プロジェクトで
        # engine.ToMisakiIpa("你好") を実行し、ランタイム例外が出ないことを確認
```

**判断**: **B-1 は T04 で実装必須**、B-2 は Mi3/Mi4 で検討。B-1 だけでも「`[Preserve]` 属性の外し忘れ」という最頻出の IL2CPP バグを検出できる。

#### C. KokoroSharp 統合テストの位置付け

T03 で追加される `ToMisakiIpa` API は KokoroSharp（または類似の Kokoro TTS C# 実装）から呼び出されることを前提としている。しかし KokoroSharp 自体への依存をテストプロジェクトに追加すると:

- NuGet 依存関係の複雑化
- KokoroSharp のバージョン互換性への配慮
- ONNX モデルファイル（>100MB）のダウンロードと CI での実行

等の問題がある。このため T04 では **KokoroSharp を参照しない「契約テスト」** を実装する:

```csharp
public class KokoroSharpContractTests : IDisposable
{
    private readonly ChineseG2PEngine _engine;

    public KokoroSharpContractTests()
    {
        _engine = new ChineseG2PEngine();
    }

    public void Dispose() => _engine.Dispose();

    /// <summary>
    /// KokoroSharp が期待する入力仕様を満たしていることを確認する契約テスト。
    /// Kokoro TTS のトークナイザ仕様に基づき、以下を検証:
    /// 1. 出力は string 型
    /// 2. Unicode IPA + 矢印記号のみで構成される（制御文字なし）
    /// 3. セグメント区切りは半角スペース固定
    /// 4. 空入力時は空文字列を返す（例外を投げない）
    /// </summary>
    [Theory]
    [InlineData("你好")]
    [InlineData("你好世界")]
    [InlineData("我爱北京天安门")]
    public void ToMisakiIpa_KokoroSharp契約_制御文字を含まない(string input)
    {
        var result = _engine.ToMisakiIpa(input);

        Assert.NotNull(result);
        foreach (var ch in result)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            // Cc (Control), Cf (Format), Cs (Surrogate) を許可しない
            Assert.NotEqual(UnicodeCategory.Control, category);
            Assert.NotEqual(UnicodeCategory.Format, category);
        }
    }

    [Fact]
    public void ToMisakiIpa_KokoroSharp契約_セグメント区切りはスペース()
    {
        var result = _engine.ToMisakiIpa("中国");
        // KokoroSharp の tokenizer は " " (0x20) でセグメント分割する
        Assert.Contains(" ", result);
    }

    [Fact]
    public void ToMisakiIpa_KokoroSharp契約_空入力で例外なし()
    {
        // KokoroSharp は空文字列を「無音」として扱う前提
        var result = _engine.ToMisakiIpa("");
        Assert.Equal("", result);
    }
}
```

**利点**:

- KokoroSharp 本体に依存しないため、CI 時間・依存管理が簡素化される
- KokoroSharp 側の仕様変更があった場合、契約テストを更新するだけで追従できる
- 将来 Kokoro Python 実装と互換性比較をする際の SSOT としても機能する

#### D. NuGet / UPM 両配布のテスト整合性

`DotNetG2P.Chinese` は NuGet（`DotNetG2P.Chinese`）と UPM（`com.dotnetg2p.chinese`）の両方で配布されるが、T04 のテストはすべて NuGet / .csproj プロジェクト参照経由で実行される。以下の観点で UPM 配布版の品質を担保する必要がある:

1. **埋め込みリソースのロード経路**: `EmbeddedChineseDictionaryCache` は `Assembly.GetManifestResourceStream` で辞書をロードするが、Unity UPM 環境では Assembly の扱いが異なる場合がある。T04 で以下のテストを追加:

```csharp
[Fact]
public void ChineseG2PEngine_デフォルトコンストラクタ_埋め込み辞書をロードできる()
{
    // このテストは NuGet 環境で動くが、UPM 環境でも同じロジックが使われる
    using var engine = new ChineseG2PEngine();
    var result = engine.ToMisakiIpa("你好");
    Assert.NotEmpty(result);
    // 埋め込み辞書が正しくロードされた証左として、
    // 三声連読変調が適用されていることを確認
    Assert.Contains("\u2197", result);
}
```

2. **.meta ファイル整合性**: T04 で `tests/TestData/ChineseG2P/` 配下に TSV を新規作成する場合、Unity UPM パッケージのルートには **含めない**（`tests/` ディレクトリは UPM パッケージに含まれないため問題なし）。ただし `tools/sync-shared-internals.ps1` の同期対象に `.meta` 整合性チェックが含まれる場合は、TSV ファイルが誤って同期されないことを確認
3. **パッケージ独立性テスト**: `DotNetG2P.Chinese` は `DotNetG2P.Core` を参照しない独立パッケージ。T04 のテストが誤って Core の型（`G2PEngine` 等）に依存していないことを確認:

```csharp
[Fact]
public void ChineseG2PEngine_Core参照なし_単独で動作する()
{
    // このテストの存在自体が、ChineseG2PEngine が独立パッケージであることの保証
    var assembly = typeof(ChineseG2PEngine).Assembly;
    var referencedAssemblies = assembly.GetReferencedAssemblies();
    Assert.DoesNotContain(referencedAssemblies,
        a => a.Name == "DotNetG2P" || a.Name == "DotNetG2P.Core");
}
```

#### E. 将来の他言語 Kokoro 互換追加に備えたテスト命名規則

T03 §E で他言語の `ToMisakiIpa` 命名規則を統一することを推奨した。T04 のテストファイル命名もこれに揃えることで、Mi3/Mi4 で他言語の Misaki テストを追加する際のレビューコストを下げる:

**推奨テストクラス命名**:

| 言語 | テストクラス | 実装タイミング |
|------|------------|----------------|
| 中国語 | `ChineseMisakiIpaTests` | **T04 で実装** |
| 英語 | `EnglishMisakiIpaTests` | Mi3 以降 |
| 日本語 | `JapaneseMisakiIpaTests` | Mi3 以降 |
| 韓国語 | `KoreanMisakiIpaTests` | Mi3 以降 |
| スペイン語 | `SpanishMisakiIpaTests` | Mi3 以降 |
| フランス語 | `FrenchMisakiIpaTests` | Mi3 以降 |
| ポルトガル語 | `PortugueseMisakiIpaTests` | Mi3 以降 |
| Multilingual | `MultilingualMisakiIpaTests` | Mi3 以降（上記 A 節で先行プレースホルダ） |

**統一規則**:

- テストクラス名: `{言語名}MisakiIpaTests` で固定
- 配置: `tests/DotNetG2P.Tests/{言語名}G2P/` 配下（例: `tests/DotNetG2P.Tests/ChineseG2P/ChineseMisakiIpaTests.cs`）
- TSV データ配置: `tests/TestData/{言語名}G2P/misaki_*.tsv`
- コンストラクタ/Dispose パターン: 全言語で統一（`IDisposable` + `_engine` フィールド）
- テストメソッド命名: 既存の日本語命名ルール（例: `ToMisakiIpa_第1声_矢印右向き`）を他言語にも適用

**契約テストの共通基底クラス**（Mi3 以降）:

```csharp
// 将来の構想: 全言語で共有される KokoroSharp 契約テスト基底
public abstract class KokoroSharpContractTestsBase<TEngine> : IDisposable
    where TEngine : IDisposable
{
    protected abstract TEngine CreateEngine();
    protected abstract string ConvertToMisakiIpa(TEngine engine, string text);

    [Theory]
    [MemberData(nameof(InputSamples))]
    public void 契約_制御文字なし(string input) { /* ... */ }

    public static IEnumerable<object[]> InputSamples => /* 各言語共通のサンプル */;
    // Dispose パターン省略
}
```

**T04 での判断**: **基底クラスは T04 では作らず、Mi3 で各言語テストクラスが 2 つ以上できた段階で抽出する**。T04 で先行して作ると YAGNI（You Aren't Gonna Need It）に該当するリスクがある。ただし、T04 で実装する `ChineseMisakiIpaTests` と `KokoroSharpContractTests` のメソッド命名は、後に基底クラスへ抽出しやすい形（`契約_XXX_YYY` プレフィックス）にすること。

#### F. T04 完了条件への追加アイテム（本節）

§F（結論とアクションアイテム）の末尾に、以下を追加で検討する:

- [ ] `MultilingualMisakiIpaTests.cs` を `[Fact(Skip="Mi3")]` 付きプレースホルダとして作成（上記 A 節）
- [ ] `[Preserve]` 属性存在確認テストを `ChineseG2PEngineTests` に追加（上記 B-1 節）
- [ ] `KokoroSharpContractTests.cs` を新規作成し、制御文字・スペース区切り・空入力契約を検証（上記 C 節）
- [ ] `ChineseG2PEngine_Core参照なし_単独で動作する` テストを追加し、パッケージ独立性を保証（上記 D-3 節）
- [ ] テストクラス命名が `{言語名}MisakiIpaTests` パターンに準拠していることを確認（上記 E 節）

**§テスト戦略の追加レビュー との整合性**: §B の TSV データ駆動テスト採用案と、本節の Multilingual / Unity / KokoroSharp 契約テスト案は相互補完関係にある。TSV は「言語固有の変換ロジックの正確性」を保証し、本節の統合テストは「クラス間の契約・配布環境・外部ランタイム互換性」を保証する。両者を併用することで T04 の完了条件が堅牢になる。

---

## 7. 後続タスクへの連絡事項

### 7-1. T05（ドキュメント更新）に伝えるべき情報

T04 完了時点で以下を T05 の作業者に引き継ぐこと:

1. **テスト結果サマリ**
   - 実装したテスト件数（合計、カテゴリ別）
   - 全件パス/失敗した件数
   - 既存936件テストの回帰状況
   - `dotnet test DotNetG2P.slnx` の実行時間

2. **発見した差異・仕様確定事項**
   - Misaki と DotNetG2P の実装差異で、設計ドキュメントに追記が必要な項目
   - 例: そり舌母音の区別 (`ʐ̩` vs `ɻ̩`) を初期実装で採用したか
   - 例: Misaki の `ꭧ` (U+AB67) 対応を行ったか
   - 声調変調の挙動で設計ドキュメント記載と異なる点があれば明記

3. **既知の制限事項**
   - `嗯` などの非標準ピンインで Misaki 互換出力が空文字列になる
   - 辞書に無い漢字のフォールバック挙動（パススルー）
   - サロゲートペア・絵文字混在時の挙動

4. **パフォーマンス指標**
   - `ToMisakiIpa` の実行時間が `ToIPA` / `ToPiperIPA` と同等であることの確認結果
   - Mi3 のパフォーマンステスト計画への引き継ぎ

5. **Issue #56 再現テストの結果**
   - `"你好"` の実際の出力（複数パターン: `ToMisakiIpa` / `ToMisakiIpa(text, includeTones:false)` / `enableToneSandhi:false`）
   - Issue 本文の期待値 `ni↓xau̯↓` との一致状況
   - Issue へのコメント下書きを T05 のドキュメント更新タスクに含めること

6. **README.md に記載すべき使用例**
   ```csharp
   using var engine = new ChineseG2PEngine();
   var result = engine.ToMisakiIpa("你好");
   // 出力例: "ni↗ xau̯↓"
   ```

7. **CLAUDE.md 進捗テーブル更新**
   - 中国語パッケージの備考に「Misaki互換出力対応（v1.10.0）」を追記
   - テスト件数を 936 から新しい合計値に更新

8. **Mi3 に持ち越す項目**
   - パフォーマンステストの本実装
   - Misaki Python 実装との定量比較
   - 追加エッジケーステスト
   - Multilingual 層への統合検討

### 7-2. T05 の前提条件

T05 の作業者は以下を確認してから作業開始すること:
- [x] T04 の全テストがパスしている
- [x] `dotnet test DotNetG2P.slnx` 全件パスしている
- [x] 本チケット (T04) の成果物が `main` または `feature/misaki-chinese` ブランチに反映されている
- [x] 設計ドキュメント `docs/guides/misaki-compatible-chinese.md` の「備考」セクションに T04 の発見事項が反映されている

---

## 8. 紐づけ

| 項目 | 値 |
|------|-----|
| **マイルストーン** | **Mi2** — ChineseG2PEngine API統合 + テスト |
| **依存** | **T03** — `PinyinToMisaki.cs` + `ChineseG2PEngine.ToMisakiIpa()` 実装完了 |
| **後続** | **T05** — ドキュメント更新（README.md, CLAUDE.md, 設計ドキュメント、Issue#56 コメント） |
| **関連 Issue** | [#56 — How can i make result similar like misaki does?](https://github.com/ayutaz/dot-net-g2p/issues/56) |
| **関連ドキュメント** | [docs/guides/misaki-compatible-chinese.md](../guides/misaki-compatible-chinese.md) |
| **関連ドキュメント** | [docs/guides/misaki-milestones.md](../guides/misaki-milestones.md) |
| **参考テスト** | `tests/DotNetG2P.Tests/ChineseG2P/ChinesePiperIpaTests.cs`（パターン参考） |
| **参考テスト** | `tests/DotNetG2P.Tests/ChineseG2P/ChinesePiperIpaComparisonTests.cs`（比較パターン） |
| **参考テスト** | `tests/DotNetG2P.Tests/ChineseG2P/ChineseG2PEngineC4Tests.cs`（IPA出力パターン） |
| **参考テスト** | `tests/DotNetG2P.Tests/ChineseG2P/ChinesePiperEdgeCaseTests.cs`（エッジケースパターン） |
| **参考テスト** | `tests/DotNetG2P.Tests/ChineseG2P/ChinesePiperIntegrationTests.cs`（統合テストパターン） |
| **作業ブランチ（想定）** | `feature/misaki-chinese-tests` |
| **PR タイトル（想定）** | `test: Misaki互換中国語G2P出力のテスト追加 (T04/Mi2)` |
