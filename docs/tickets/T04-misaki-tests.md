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
