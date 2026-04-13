using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Misaki (Kokoro TTS) 互換 IPA 変換の正確性を検証するテスト。
    /// ChineseG2PEngine の ToMisakiIPA() メソッド経由で、
    /// Misaki 方式の声母・韻母IPAマッピング、声調矢印、特殊母音を検証する。
    /// Phase 1-R で Misaki 0.9.4 実出力を検証済みの期待値を使用。
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

        // =====================================================================
        // 1. 声調矢印マッピング (1-4声 + 軽声)
        // =====================================================================

        [Theory]
        [InlineData("\u5988", "ma\u2192")]   // 妈 (mā) 第1声 → ma→
        [InlineData("\u9EBB", "ma\u2197")]   // 麻 (má) 第2声 → ma↗
        [InlineData("\u9A6C", "ma\u2193")]   // 马 (mǎ) 第3声 → ma↓
        [InlineData("\u9A82", "ma\u2198")]   // 骂 (mà) 第4声 → ma↘
        public void ToMisakiIPA_声調矢印_1声から4声まで正しく付与される(string hanzi, string expected)
        {
            var result = _engine.ToMisakiIPA(hanzi);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToMisakiIPA_軽声_矢印なし()
        {
            // 吗 (ma, 軽声) → "ma" (矢印なし)
            var result = _engine.ToMisakiIPA("\u5417");
            Assert.Equal("ma", result);
        }

        [Fact]
        public void ToMisakiIPA_includeTones_false_矢印なし()
        {
            // 妈 (mā) を includeTones=false で変換 → "ma" (矢印なし)
            var result = _engine.ToMisakiIPA("\u5988", false);
            Assert.Equal("ma", result);
            // 矢印文字を含まないことを確認
            Assert.DoesNotContain("\u2192", result); // →
            Assert.DoesNotContain("\u2197", result); // ↗
            Assert.DoesNotContain("\u2193", result); // ↓
            Assert.DoesNotContain("\u2198", result); // ↘
        }

        [Theory]
        [InlineData("\u5988")]  // 妈 (mā) 第1声
        [InlineData("\u9EBB")]  // 麻 (má) 第2声
        [InlineData("\u9A6C")]  // 马 (mǎ) 第3声
        [InlineData("\u9A82")]  // 骂 (mà) 第4声
        [InlineData("\u5417")]  // 吗 (ma) 軽声
        public void ToMisakiIPA_IPA声調letter_含まない(string hanzi)
        {
            // Misaki 形式は矢印を使い、IPA tone letter (˥˦˧˨˩) は使わない
            var result = _engine.ToMisakiIPA(hanzi);
            Assert.DoesNotContain("\u02E5", result); // ˥
            Assert.DoesNotContain("\u02E6", result); // ˦
            Assert.DoesNotContain("\u02E7", result); // ˧
            Assert.DoesNotContain("\u02E8", result); // ˨
            Assert.DoesNotContain("\u02E9", result); // ˩
        }

        // =====================================================================
        // 2. 声母マッピング (Misaki 固有差異 — 全21声母網羅)
        // =====================================================================

        [Theory]
        // 口蓋音 (j/q/x) — 合字 ʨ (U+02A8) を使用
        [InlineData("\u51E0", "\u02A8")]             // 几 (jǐ): j → ʨ
        [InlineData("\u4E03", "\u02A8\u02B0")]       // 七 (qī): q → ʨʰ
        [InlineData("\u897F", "\u0255")]              // 西 (xī): x → ɕ
        // 歯茎破擦音 (z/c) — 合字 ʦ (U+02A6) を使用, s は共通
        [InlineData("\u5728", "\u02A6")]              // 在 (zài): z → ʦ
        [InlineData("\u624D", "\u02A6\u02B0")]        // 才 (cái): c → ʦʰ
        [InlineData("\u4E09", "s")]                   // 三 (sān): s → s
        // そり舌音 (zh/ch/sh/r) — ꭧ (U+AB67) を使用
        [InlineData("\u77E5", "\uAB67")]              // 知 (zhī): zh → ꭧ
        [InlineData("\u5403", "\uAB67\u02B0")]        // 吃 (chī): ch → ꭧʰ
        [InlineData("\u5341", "\u0282")]              // 十 (shí): sh → ʂ
        [InlineData("\u65E5", "\u027B")]              // 日 (rì): r → ɻ
        // 両唇音 (b/p/m) + 唇歯音 (f)
        [InlineData("\u7238", "p")]                   // 爸 (bà): b → p
        [InlineData("\u6015", "p\u02B0")]             // 怕 (pà): p → pʰ
        [InlineData("\u5988", "m")]                   // 妈 (mā): m → m
        [InlineData("\u98DE", "f")]                   // 飞 (fēi): f → f
        // 歯茎音 (d/t/n/l)
        [InlineData("\u5927", "t")]                   // 大 (dà): d → t
        [InlineData("\u5929", "t\u02B0")]             // 天 (tiān): t → tʰ
        [InlineData("\u5973", "n")]                   // 女 (nǚ): n → n
        [InlineData("\u6765", "l")]                   // 来 (lái): l → l
        // 軟口蓋音 (g/k/h)
        [InlineData("\u5E72", "k")]                   // 干 (gàn): g → k
        [InlineData("\u770B", "k\u02B0")]             // 看 (kàn): k → kʰ
        [InlineData("\u597D", "x")]                   // 好 (hǎo): h → x
        public void ToMisakiIPA_声母マッピング_21声母すべて正しいIPAを含む(string hanzi, string expectedInitial)
        {
            var result = _engine.ToMisakiIPA(hanzi);
            Assert.Contains(expectedInitial, result);
        }

        // --- 声母 Assert.Equal 完全一致テスト (代表的な数件) ---

        [Fact]
        public void ToMisakiIPA_声母j_几_完全一致()
        {
            // 几 (jǐ, 3声): j+i → ʨi↓
            var result = _engine.ToMisakiIPA("\u51E0"); // 几
            Assert.Equal("\u02A8i\u2193", result); // ʨi↓
        }

        [Fact]
        public void ToMisakiIPA_声母zh_知_完全一致()
        {
            // 知 (zhī, 1声): zh+i → ꭧɨ→
            var result = _engine.ToMisakiIPA("\u77E5"); // 知
            Assert.Equal("\uAB67\u0268\u2192", result); // ꭧɨ→
        }

        [Fact]
        public void ToMisakiIPA_声母ch_吃_完全一致()
        {
            // 吃 (chī, 1声): ch+i → ꭧʰɨ→
            var result = _engine.ToMisakiIPA("\u5403"); // 吃
            Assert.Equal("\uAB67\u02B0\u0268\u2192", result); // ꭧʰɨ→
        }

        [Fact]
        public void ToMisakiIPA_声母d_东_完全一致()
        {
            // 东 (dōng, 1声): d+ong → tʊ→ŋ
            var result = _engine.ToMisakiIPA("\u4E1C"); // 东
            Assert.Equal("t\u028A\u2192\u014B", result); // tʊ→ŋ
        }

        [Fact]
        public void ToMisakiIPA_声母er_儿_完全一致()
        {
            // 儿 (ér, 2声): er → ɚ↗
            var result = _engine.ToMisakiIPA("\u513F"); // 儿
            Assert.Equal("\u025A\u2197", result); // ɚ↗
        }

        [Fact]
        public void ToMisakiIPA_zh声母_従来IPAのʈʂを含まない()
        {
            // 知 (zhī): Misaki では ꭧ (U+AB67) を使い、従来の ʈʂ (U+0288 U+0282) は使わない
            var result = _engine.ToMisakiIPA("\u77E5");
            Assert.DoesNotContain("\u0288\u0282", result); // ʈʂ ではない
            Assert.DoesNotContain("\u0288", result);       // ʈ を含まない
        }

        [Fact]
        public void ToMisakiIPA_j声母_従来IPAのtɕを含まない()
        {
            // 几 (jǐ): Misaki では ʨ (U+02A8) を使い、従来の tɕ (t + U+0255) は使わない
            var result = _engine.ToMisakiIPA("\u51E0");
            Assert.DoesNotContain("t\u0255", result); // tɕ ではない
        }

        [Fact]
        public void ToMisakiIPA_z声母_従来IPAのtsを含まない()
        {
            // 在 (zài): Misaki では ʦ (U+02A6) を使い、従来の ts (2文字) は使わない
            var result = _engine.ToMisakiIPA("\u5728");
            // ʦ (U+02A6, 合字1文字) を含むことを確認
            Assert.Contains("\u02A6", result);
        }

        // =====================================================================
        // 3. 韻母マッピング (Phase 1-R 準拠、U+032F なし)
        // =====================================================================

        // --- 二重母音: ai, ei, ao(→au), ou ---

        [Fact]
        public void ToMisakiIPA_ai韻母_U032F非音節化符号を含まない()
        {
            // 买 (mǎi): ai 二重母音 → U+032F (非音節化符号) を含まない
            var result = _engine.ToMisakiIPA("\u4E70");
            Assert.Contains("ai", result);
            Assert.DoesNotContain("\u032F", result);
        }

        [Fact]
        public void ToMisakiIPA_ei韻母_U032F非音節化符号を含まない()
        {
            // 北 (běi): ei 二重母音 → U+032F を含まない
            var result = _engine.ToMisakiIPA("\u5317");
            Assert.Contains("ei", result);
            Assert.DoesNotContain("\u032F", result);
        }

        [Fact]
        public void ToMisakiIPA_ao韻母_auに変換されU032Fを含まない()
        {
            // 高 (gāo): ao → au (Misaki 方式)
            var result = _engine.ToMisakiIPA("\u9AD8");
            Assert.Contains("au", result);
            Assert.DoesNotContain("ao", result);
            Assert.DoesNotContain("\u032F", result);
        }

        [Fact]
        public void ToMisakiIPA_ou韻母_U032F非音節化符号を含まない()
        {
            // 口 (kǒu): ou 二重母音 → U+032F を含まない
            var result = _engine.ToMisakiIPA("\u53E3");
            Assert.Contains("ou", result);
            Assert.DoesNotContain("\u032F", result);
        }

        // --- CVC韻母: an, en, ang, eng, ong ---

        [Theory]
        [InlineData("\u5B89", "a", "n")]       // 安 (ān): an → prefix="a", suffix="n"
        [InlineData("\u6069", "\u0259", "n")]   // 恩 (ēn): en → prefix="ə", suffix="n"
        [InlineData("\u82B3", "a", "\u014B")]   // 芳 (fāng): ang → prefix="a", suffix="ŋ"
        [InlineData("\u98CE", "\u0259", "\u014B")] // 风 (fēng): eng → prefix="ə", suffix="ŋ"
        [InlineData("\u4E1C", "\u028A", "\u014B")] // 东 (dōng): ong → prefix="ʊ", suffix="ŋ"
        public void ToMisakiIPA_CVC韻母_正しいprefix_suffixペア(string hanzi, string vowelPart, string codaPart)
        {
            var result = _engine.ToMisakiIPA(hanzi);
            Assert.Contains(vowelPart, result);
            Assert.Contains(codaPart, result);
        }

        // --- i系韻母: ia, ie, ian, iang, iong ---

        [Fact]
        public void ToMisakiIPA_ia韻母_jaを含む()
        {
            // 家 (jiā): ia → ja
            var result = _engine.ToMisakiIPA("\u5BB6");
            Assert.Contains("ja", result);
        }

        [Fact]
        public void ToMisakiIPA_ie韻母_jeを含む()
        {
            // 写 (xiě): ie → je
            var result = _engine.ToMisakiIPA("\u5199");
            Assert.Contains("je", result);
        }

        [Fact]
        public void ToMisakiIPA_ian韻母_jɛnを含む()
        {
            // 先 (xiān): ian → jɛn (j半母音 + ɛ + n)
            var result = _engine.ToMisakiIPA("\u5148");
            Assert.Contains("j\u025B", result); // jɛ
            Assert.Contains("n", result);
        }

        [Fact]
        public void ToMisakiIPA_iang韻母_jaŋを含む()
        {
            // 江 (jiāng): iang → jaŋ
            var result = _engine.ToMisakiIPA("\u6C5F");
            Assert.Contains("ja", result);
            Assert.Contains("\u014B", result); // ŋ
        }

        [Fact]
        public void ToMisakiIPA_iong韻母_jʊŋを含む()
        {
            // 穷 (qióng): iong → jʊŋ
            var result = _engine.ToMisakiIPA("\u7A77");
            Assert.Contains("j\u028A", result); // jʊ
            Assert.Contains("\u014B", result);   // ŋ
        }

        // --- u系韻母: ua, uo, uan, uang ---

        [Fact]
        public void ToMisakiIPA_ua韻母_waを含む()
        {
            // 花 (huā): ua → wa
            var result = _engine.ToMisakiIPA("\u82B1");
            Assert.Contains("wa", result);
        }

        [Fact]
        public void ToMisakiIPA_uo韻母_woを含む()
        {
            // 多 (duō): uo → wo
            var result = _engine.ToMisakiIPA("\u591A");
            Assert.Contains("wo", result);
        }

        [Fact]
        public void ToMisakiIPA_uan韻母_wanを含む()
        {
            // 官 (guān): uan → wa...n (prefix="wa", suffix="n")
            var result = _engine.ToMisakiIPA("\u5B98");
            Assert.Contains("wa", result);
            Assert.Contains("n", result);
        }

        [Fact]
        public void ToMisakiIPA_uang韻母_waŋを含む()
        {
            // 光 (guāng): uang → wa...ŋ (prefix="wa", suffix="ŋ")
            var result = _engine.ToMisakiIPA("\u5149");
            Assert.Contains("wa", result);
            Assert.Contains("\u014B", result); // ŋ
        }

        // --- ü系韻母: v, ve, van ---

        [Fact]
        public void ToMisakiIPA_v韻母_yを含む()
        {
            // 女 (nǚ): ü → y (U+0079)
            var result = _engine.ToMisakiIPA("\u5973");
            Assert.Contains("y", result);
        }

        [Fact]
        public void ToMisakiIPA_ve韻母_ɥeを含む()
        {
            // 学 (xué): üe → ɥe (U+0265 + e)
            var result = _engine.ToMisakiIPA("\u5B66");
            Assert.Contains("\u0265e", result); // ɥe
        }

        [Fact]
        public void ToMisakiIPA_van韻母_ɥɛnを含む()
        {
            // 全 (quán): üan → ɥɛn (U+0265 + U+025B + n)
            var result = _engine.ToMisakiIPA("\u5168");
            Assert.Contains("\u0265\u025B", result); // ɥɛ
            Assert.Contains("n", result);
        }

        // --- er韻母 ---

        [Fact]
        public void ToMisakiIPA_er韻母_ɚを返す()
        {
            // 二 (èr): er → ɚ (U+025A)
            var result = _engine.ToMisakiIPA("\u4E8C");
            Assert.Contains("\u025A", result); // ɚ
            // 従来IPA の əɻ (U+0259 U+027B) ではないことを確認
            Assert.DoesNotContain("\u0259\u027B", result);
        }

        // --- Y/W OmitInitial=true E2E テスト ---

        // yi: Y+I → omitInitial=true → "i" + tone
        // 衣 (yī) → i→
        [Fact]
        public void ToMisakiIPA_yi_初声母省略でiのみ()
        {
            var result = _engine.ToMisakiIPA("\u8863"); // 衣
            Assert.Equal("i\u2192", result); // i→
        }

        // wu: W+U → omitInitial=true → "u" + tone
        // 五 (wǔ) → u↓
        [Fact]
        public void ToMisakiIPA_wu_初声母省略でuのみ()
        {
            var result = _engine.ToMisakiIPA("\u4E94"); // 五
            Assert.Equal("u\u2193", result); // u↓
        }

        // yu: Y+V → omitInitial=true → "y" + tone
        // 鱼 (yú) → y↗
        [Fact]
        public void ToMisakiIPA_yu_初声母省略でyのみ()
        {
            var result = _engine.ToMisakiIPA("\u9C7C"); // 鱼
            Assert.Equal("y\u2197", result); // y↗
        }

        // yin: Y+In → omitInitial=true → "i" + tone + "n"
        // 音 (yīn) → i→n
        [Fact]
        public void ToMisakiIPA_yin_初声母省略でinのみ()
        {
            var result = _engine.ToMisakiIPA("\u97F3"); // 音
            Assert.Equal("i\u2192n", result); // i→n
        }

        // ying: Y+Ing → omitInitial=true → "i" + tone + "ŋ"
        // 英 (yīng) → i→ŋ
        [Fact]
        public void ToMisakiIPA_ying_初声母省略でiŋのみ()
        {
            var result = _engine.ToMisakiIPA("\u82F1"); // 英
            Assert.Equal("i\u2192\u014B", result); // i→ŋ
        }

        // yun: Y+Vn → omitInitial=true → "y" + tone + "n"
        // 云 (yún) → y↗n
        [Fact]
        public void ToMisakiIPA_yun_初声母省略でynのみ()
        {
            var result = _engine.ToMisakiIPA("\u4E91"); // 云
            Assert.Equal("y\u2197n", result); // y↗n
        }

        // --- bpmf+o E2E テスト ---

        // 波 (bō) → pwo→ (B=p, O=("wo",""))
        [Fact]
        public void ToMisakiIPA_bpmf加o_pwoを返す()
        {
            var result = _engine.ToMisakiIPA("\u6CE2"); // 波 bō
            Assert.Equal("pwo\u2192", result); // pwo→
        }

        // =====================================================================
        // 4. そり舌/歯茎母音 (ɨ U+0268)
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_zh加i_ꭧɨ矢印を返す()
        {
            // 知 (zhī, 1声): zh+i → ꭧɨ→
            var result = _engine.ToMisakiIPA("\u77E5");
            Assert.Equal("\uAB67\u0268\u2192", result); // ꭧɨ→
        }

        [Fact]
        public void ToMisakiIPA_ch加i_ꭧʰɨ矢印を返す()
        {
            // 吃 (chī, 1声): ch+i → ꭧʰɨ→
            var result = _engine.ToMisakiIPA("\u5403");
            Assert.Equal("\uAB67\u02B0\u0268\u2192", result); // ꭧʰɨ→
        }

        [Fact]
        public void ToMisakiIPA_sh加i_ʂɨ矢印を返す()
        {
            // 十 (shí, 2声): sh+i → ʂɨ↗
            var result = _engine.ToMisakiIPA("\u5341");
            Assert.Equal("\u0282\u0268\u2197", result); // ʂɨ↗
        }

        [Fact]
        public void ToMisakiIPA_r加i_ɻɨ矢印を返す()
        {
            // 日 (rì, 4声): r+i → ɻɨ↘
            var result = _engine.ToMisakiIPA("\u65E5");
            Assert.Equal("\u027B\u0268\u2198", result); // ɻɨ↘
        }

        [Fact]
        public void ToMisakiIPA_z加i_ʦɨを返す()
        {
            // 子 (zi, 軽声): z+i → ʦɨ (軽声なので矢印なし)
            var result = _engine.ToMisakiIPA("\u5B50");
            Assert.Equal("\u02A6\u0268", result); // ʦɨ
        }

        [Fact]
        public void ToMisakiIPA_c加i_ʦʰɨ矢印を返す()
        {
            // 次 (cì, 4声): c+i → ʦʰɨ↘
            var result = _engine.ToMisakiIPA("\u6B21");
            Assert.Equal("\u02A6\u02B0\u0268\u2198", result); // ʦʰɨ↘
        }

        [Fact]
        public void ToMisakiIPA_s加i_sɨ矢印を返す()
        {
            // 四 (sì, 4声): s+i → sɨ↘
            var result = _engine.ToMisakiIPA("\u56DB");
            Assert.Equal("s\u0268\u2198", result); // sɨ↘
        }

        [Theory]
        [InlineData("\u77E5")]  // 知 (zhī)
        [InlineData("\u5403")]  // 吃 (chī)
        [InlineData("\u5341")]  // 十 (shí)
        [InlineData("\u65E5")]  // 日 (rì)
        [InlineData("\u5B50")]  // 子 (zi)
        [InlineData("\u6B21")]  // 次 (cì)
        [InlineData("\u56DB")]  // 四 (sì)
        public void ToMisakiIPA_そり舌歯茎母音_ɨを含みɻ̩ɹ̩を含まない(string hanzi)
        {
            var result = _engine.ToMisakiIPA(hanzi);
            // ɨ (U+0268) を含むこと
            Assert.Contains("\u0268", result);
            // 従来表記の ɻ̩ (U+027B U+0329) や ɹ̩ (U+0279 U+0329) を含まないこと
            Assert.DoesNotContain("\u0329", result); // 非音節化符号 U+0329
        }

        // =====================================================================
        // 5. 声調変調 (三声連読、一/不変調)
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_三声連読_你好_niが2声に変調()
        {
            // 你好: nǐ+hǎo (3+3) → ní+hǎo (2+3)
            var result = _engine.ToMisakiIPA("\u4F60\u597D");
            Assert.Contains("ni\u2197", result); // ni↗ (2声)
            Assert.Contains("xau\u2193", result); // xau↓ (3声)
        }

        [Fact]
        public void ToMisakiIPA_一変調_4声前で2声_一个()
        {
            // 一个: yī+gè → yí+gè (4声前→2声)
            var result = _engine.ToMisakiIPA("\u4E00\u4E2A");
            Assert.Contains("i\u2197", result); // i↗ (2声)
            Assert.Contains("k\u0264\u2198", result); // kɤ↘ (4声)
        }

        [Fact]
        public void ToMisakiIPA_一変調_1声前で4声_一天()
        {
            // 一天: yī+tiān → yì+tiān (1声前→4声)
            var result = _engine.ToMisakiIPA("\u4E00\u5929");
            Assert.Contains("i\u2198", result); // i↘ (4声)
            Assert.Contains("t\u02B0j\u025B\u2192n", result); // tʰjɛ→n (1声)
        }

        [Fact]
        public void ToMisakiIPA_不変調_4声前で2声_不要()
        {
            // 不要: bù+yào → bú+yào (4声前→2声)
            var result = _engine.ToMisakiIPA("\u4E0D\u8981");
            Assert.Contains("pu\u2197", result); // pu↗ (2声)
            Assert.Contains("jau\u2198", result); // jau↘ (4声)
        }

        [Fact]
        public void ToMisakiIPA_EnableToneSandhiがfalse_你好で3声のまま()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToMisakiIPA("\u4F60\u597D");
            Assert.Contains("ni\u2193", result); // ni↓ (3声のまま)
            Assert.Contains("xau\u2193", result); // xau↓ (3声)
        }

        [Fact]
        public void ToMisakiIPA_EnableToneSandhiがtrue_デフォルトで変調適用()
        {
            // デフォルト (EnableToneSandhi=true) では三声連読が適用される
            var result = _engine.ToMisakiIPA("\u4F60\u597D"); // 你好
            // ni↗ (2声に変調) + xau↓ (3声)
            Assert.Contains("ni\u2197", result);
        }

        [Fact]
        public void ToMisakiIPA_MisakiLegacy互換_EnableToneSandhiをfalseにすると変調なし()
        {
            // Misaki legacy 互換: EnableToneSandhi=false で変調なし
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToMisakiIPA("\u4F60\u597D"); // 你好
            // ni↓ (3声のまま) + xau↓ (3声)
            Assert.Equal("ni\u2193 xau\u2193", result);
        }

        // =====================================================================
        // 6. エッジケース
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_null入力_空文字列を返す()
        {
            var result = _engine.ToMisakiIPA(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void ToMisakiIPA_空文字列_空文字列を返す()
        {
            var result = _engine.ToMisakiIPA("");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToMisakiIPA_空白のみ_空文字列を返す()
        {
            var result = _engine.ToMisakiIPA("   ");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToMisakiIPA_CJK句読点のみ_空文字列を返す()
        {
            var result = _engine.ToMisakiIPA("\u3002\u3001\uFF01\uFF1F");
            Assert.Equal("", result);
        }

        [Fact]
        public void ToMisakiIPA_数字英数字混在_エラーなし()
        {
            var result = _engine.ToMisakiIPA("ABC123");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToMisakiIPA_er化音_儿_ɚを返す()
        {
            // 儿 (ér, tone 2): Final.Er → ɚ↗
            var result = _engine.ToMisakiIPA("\u513F");
            Assert.Contains("\u025A", result); // ɚ
            Assert.Contains("\u2197", result); // ↗ (2声)
        }

        [Fact]
        public void ToMisakiIPA_ü母音_鱼_yを返す()
        {
            // 鱼 (yú, tone 2): Y+V(omit) → "y↗"
            var result = _engine.ToMisakiIPA("\u9C7C");
            Assert.Contains("y\u2197", result); // y↗
        }

        [Fact]
        public void ToMisakiIPA_サロゲートペア混在_エラーなし()
        {
            var result = _engine.ToMisakiIPA("\u4F60\uD83D\uDE00\u597D");
            Assert.NotNull(result);
        }

        // =====================================================================
        // 7. Issue #56 再現
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_Issue56_你好_完全一致_三声連読後()
        {
            // 你好: 三声連読 → ni↗ xau↓ (U+032F なし)
            var result = _engine.ToMisakiIPA("\u4F60\u597D");
            Assert.Equal("ni\u2197 xau\u2193", result);
        }

        [Fact]
        public void ToMisakiIPA_Issue56_你好_U032Fを含まない()
        {
            var result = _engine.ToMisakiIPA("\u4F60\u597D");
            Assert.DoesNotContain("\u032F", result);
        }

        [Fact]
        public void ToMisakiIPA_Issue56_IPA_toneLetterを含まない()
        {
            var result = _engine.ToMisakiIPA("\u4F60\u597D");
            Assert.DoesNotContain("\u02E5", result); // ˥
            Assert.DoesNotContain("\u02E6", result); // ˦
            Assert.DoesNotContain("\u02E7", result); // ˧
            Assert.DoesNotContain("\u02E8", result); // ˨
            Assert.DoesNotContain("\u02E9", result); // ˩
        }

        [Theory]
        [InlineData("\u5988")]  // 妈 (1声)
        [InlineData("\u9EBB")]  // 麻 (2声)
        [InlineData("\u9A6C")]  // 马 (3声)
        [InlineData("\u9A82")]  // 骂 (4声)
        public void ToMisakiIPA_各声調_toneLetterを含まない(string hanzi)
        {
            var result = _engine.ToMisakiIPA(hanzi);
            Assert.DoesNotContain("\u02E5", result);
            Assert.DoesNotContain("\u02E6", result);
            Assert.DoesNotContain("\u02E7", result);
            Assert.DoesNotContain("\u02E8", result);
            Assert.DoesNotContain("\u02E9", result);
        }

        // =====================================================================
        // 8. バッチ API
        // =====================================================================

        [Fact]
        public void ToMisakiIPABatch_複数テキスト_正しい件数を返す()
        {
            var texts = new[] { "\u4F60\u597D", "\u4E16\u754C", "\u4E2D\u56FD" };
            var results = _engine.ToMisakiIPABatch(texts);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void ToMisakiIPABatch_includeTonesがfalse_声調矢印なし()
        {
            var texts = new[] { "\u5988", "\u9EBB" };
            var results = _engine.ToMisakiIPABatch(texts, false);
            foreach (var result in results)
            {
                Assert.DoesNotContain("\u2192", result);
                Assert.DoesNotContain("\u2197", result);
                Assert.DoesNotContain("\u2193", result);
                Assert.DoesNotContain("\u2198", result);
            }
        }

        [Fact]
        public void ToMisakiIPABatch_空配列_空リストを返す()
        {
            var results = _engine.ToMisakiIPABatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void ToMisakiIPABatch_null引数_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToMisakiIPABatch(null!));
        }

        [Fact]
        public void ToMisakiIPABatch_includeTones付きnull引数_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToMisakiIPABatch(null!, true));
        }

        [Fact]
        public void ToMisakiIPABatch_個別呼び出しと同一結果()
        {
            var texts = new[] { "\u4F60\u597D", "\u5988", "\u4E8C" };
            var batchResults = _engine.ToMisakiIPABatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToMisakiIPA(texts[i]);
                Assert.Equal(individual, batchResults[i]);
            }
        }

        [Fact]
        public void ToMisakiIPABatch_includeTones付き個別呼び出しと同一結果()
        {
            var texts = new[] { "\u4F60\u597D", "\u5988" };
            var batchResults = _engine.ToMisakiIPABatch(texts, false);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToMisakiIPA(texts[i], false);
                Assert.Equal(individual, batchResults[i]);
            }
        }

        // =====================================================================
        // 9. Dispose 後の動作
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_Dispose後_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToMisakiIPA("\u4F60\u597D"));
        }

        [Fact]
        public void ToMisakiIPA_includeTones付きDispose後_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToMisakiIPA("\u4F60\u597D", false));
        }

        [Fact]
        public void ToMisakiIPABatch_Dispose後_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToMisakiIPABatch(new[] { "\u4F60\u597D" }));
        }

        [Fact]
        public void ToMisakiIPABatch_includeTones付きDispose後_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToMisakiIPABatch(new[] { "\u4F60\u597D" }, true));
        }

        // =====================================================================
        // 10. 複数文字テキスト (音節区切り)
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_中国_スペース区切り2音節()
        {
            var result = _engine.ToMisakiIPA("\u4E2D\u56FD");
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
        }

        [Fact]
        public void ToMisakiIPA_你好世界_4音節3スペース()
        {
            var result = _engine.ToMisakiIPA("\u4F60\u597D\u4E16\u754C");
            var parts = result.Split(' ');
            Assert.Equal(4, parts.Length);
        }

        [Fact]
        public void ToMisakiIPA_我爱北京天安门_7音節()
        {
            var result = _engine.ToMisakiIPA("\u6211\u7231\u5317\u4EAC\u5929\u5B89\u95E8");
            var parts = result.Split(' ');
            Assert.Equal(7, parts.Length);
            Assert.All(parts, p => Assert.NotEmpty(p));
        }

        [Fact]
        public void ToMisakiIPA_中国_各音節が声調矢印を含む()
        {
            var result = _engine.ToMisakiIPA("\u4E2D\u56FD");
            var parts = result.Split(' ');
            Assert.Equal(2, parts.Length);
            foreach (var part in parts)
            {
                bool hasToneArrow = part.Contains("\u2192") || part.Contains("\u2197")
                    || part.Contains("\u2193") || part.Contains("\u2198");
                Assert.True(hasToneArrow, $"音節 '{part}' に声調矢印がありません");
            }
        }

        // =====================================================================
        // 10b. Separator オプション
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_Separator空文字_スペースなしで連結()
        {
            var options = new ChineseG2POptions(separator: "");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToMisakiIPA("\u4F60\u597D"); // 你好
            Assert.DoesNotContain(" ", result);
            // デフォルトのスペース区切り結果からスペースを除去した値と一致
            var defaultResult = _engine.ToMisakiIPA("\u4F60\u597D");
            Assert.Equal(defaultResult.Replace(" ", ""), result);
        }

        [Fact]
        public void ToMisakiIPA_Separatorハイフン_区切り文字が変更される()
        {
            var options = new ChineseG2POptions(separator: "-");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToMisakiIPA("\u4F60\u597D"); // 你好
            Assert.Contains("-", result);
            Assert.DoesNotContain(" ", result);
        }

        // =====================================================================
        // 11. 標準 IPA / piper-plus との比較
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_と_ToIPA_の出力が異なる()
        {
            var misakiResult = _engine.ToMisakiIPA("\u5988");
            var standardResult = _engine.ToIPA("\u5988");
            Assert.NotEqual(misakiResult, standardResult);
        }

        [Fact]
        public void ToMisakiIPA_と_ToPiperIPA_の出力が異なる()
        {
            var misakiResult = _engine.ToMisakiIPA("\u77E5");
            var piperResult = _engine.ToPiperIPA("\u77E5");
            Assert.NotEqual(misakiResult, piperResult);
        }

        [Fact]
        public void ToMisakiIPA_声母体系が異なる_zh声母()
        {
            var misakiResult = _engine.ToMisakiIPA("\u77E5");
            Assert.Contains("\uAB67", misakiResult); // ꭧ (Misaki 合字)
            var piperResult = _engine.ToPiperIPA("\u77E5");
            Assert.Contains("t\u0282", piperResult); // tʂ (piper-plus)
            Assert.DoesNotContain("\uAB67", piperResult);
        }

        [Fact]
        public void ToMisakiIPA_と_ToIPA_声調体系の違い()
        {
            var misakiResult = _engine.ToMisakiIPA("\u5988");
            var standardResult = _engine.ToIPA("\u5988");
            Assert.Contains("\u2192", misakiResult); // → (Misaki 矢印)
            Assert.Contains("\u02E5", standardResult); // ˥ (IPA tone letter)
            Assert.DoesNotContain("\u02E5", misakiResult);
            Assert.DoesNotContain("\u2192", standardResult);
        }

        [Fact]
        public void ToMisakiIPA_と_ToPiperIPA_声調有無の違い()
        {
            var misakiResult = _engine.ToMisakiIPA("\u9A6C");
            var piperResult = _engine.ToPiperIPA("\u9A6C");
            Assert.Contains("\u2193", misakiResult); // ↓ (3声)
            Assert.DoesNotContain("\u2192", piperResult);
            Assert.DoesNotContain("\u2197", piperResult);
            Assert.DoesNotContain("\u2193", piperResult);
            Assert.DoesNotContain("\u2198", piperResult);
        }

        // =====================================================================
        // 12. 回帰確認
        // =====================================================================

        [Fact]
        public void ToMisakiIPA_回帰_ToIPAの妈が既存出力のまま()
        {
            var result = _engine.ToIPA("\u5988");
            Assert.Equal("ma\u02E5\u02E5", result);
        }

        [Fact]
        public void ToMisakiIPA_回帰_ToPiperIPAの妈が既存出力のまま()
        {
            var result = _engine.ToPiperIPA("\u5988");
            Assert.Equal("ma", result);
        }

        [Fact]
        public void ToMisakiIPA_回帰_ToZhuyinの妈が既存出力のまま()
        {
            var result = _engine.ToZhuyin("\u5988");
            Assert.Equal("\u3107\u311A", result);
        }

        [Fact]
        public void ToMisakiIPA_回帰_ToIPA声調false版が不変()
        {
            var result = _engine.ToIPA("\u5988", false);
            Assert.Equal("ma", result);
        }

        [Fact]
        public void ToMisakiIPA_回帰_ToIPAの你好が既存出力のまま()
        {
            var result = _engine.ToIPA("\u4F60\u597D");
            Assert.DoesNotContain("\u2192", result);
            Assert.DoesNotContain("\u2197", result);
            Assert.DoesNotContain("\u2193", result);
            Assert.DoesNotContain("\u2198", result);
            bool hasToneLetter = result.Contains("\u02E5") || result.Contains("\u02E6")
                || result.Contains("\u02E7") || result.Contains("\u02E8") || result.Contains("\u02E9");
            Assert.True(hasToneLetter, "標準IPA出力にtone letterが含まれていません");
        }
    }
}
