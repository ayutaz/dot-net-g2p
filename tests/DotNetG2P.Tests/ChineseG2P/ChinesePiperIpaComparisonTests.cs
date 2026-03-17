using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 標準IPA (ToIPA) と piper-plus互換IPA (ToPiperIPA) の差異を体系的に検証するテスト。
    /// 両APIの出力を比較し、差異がある箇所と一致する箇所を明示的に確認する。
    /// </summary>
    public class ChinesePiperIpaComparisonTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine = new ChineseG2PEngine();

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 差異がある声母の比較: zh/ch のそり舌破擦音
        //    standard: ʈʂ (U+0288 U+0282) / ʈʂʰ (U+0288 U+0282 U+02B0)
        //    piper:    tʂ (t U+0282) / tʂʰ (t U+0282 U+02B0)
        // =====================================================================

        [Theory]
        [InlineData("知", "zh")] // zh: standard \u0288\u0282 vs piper t\u0282
        [InlineData("吃", "ch")] // ch: standard \u0288\u0282\u02B0 vs piper t\u0282\u02B0
        public void RetroflexInitials_StandardContainsRetroflexT_PiperDoesNot(string text, string initial)
        {
            var standardIpa = _engine.ToIPA(text);
            var piperIpa = _engine.ToPiperIPA(text);

            // 標準IPAには ʈ (U+0288) が含まれる
            Assert.Contains("\u0288", standardIpa);
            // piper IPAには ʈ (U+0288) が含まれない
            Assert.DoesNotContain("\u0288", piperIpa);

            // 両方とも ʂ (U+0282) は含まれる（そり舌摩擦音は共通）
            Assert.Contains("\u0282", standardIpa);
            Assert.Contains("\u0282", piperIpa);
        }

        // =====================================================================
        // 2. 差異がある韻母の比較
        // =====================================================================

        [Fact]
        public void Ong_StandardUsesNearCloseBackRounded_PiperUsesCloseBackRounded()
        {
            // ong 含む語: standard に ʊ (U+028A) + ŋ, piper に u + ŋ
            var standardIpa = _engine.ToIPA("东", false); // dōng, 声調なし
            var piperIpa = _engine.ToPiperIPA("东");

            // standard: ʊŋ (ʊ=U+028A, ŋ=U+014B)
            Assert.Contains("\u028A", standardIpa); // ʊ in standard
            Assert.Contains("\u014B", standardIpa); // ŋ in standard

            // piper: uŋ (u=u, ŋ=U+014B)
            Assert.DoesNotContain("\u028A", piperIpa); // ʊ NOT in piper
            Assert.Contains("u", piperIpa); // u in piper
            Assert.Contains("\u014B", piperIpa); // ŋ in piper
        }

        [Fact]
        public void Iu_StandardUsesOpenMidBackRoundedDiphthong_PiperUsesTriphthong()
        {
            // iu 含む語: standard に oʊ (o + U+028A), piper に iou
            var standardIpa = _engine.ToIPA("六", false); // liù, 声調なし
            var piperIpa = _engine.ToPiperIPA("六");

            // standard: ioʊ (o + U+028A)
            Assert.Contains("o\u028A", standardIpa); // oʊ in standard

            // piper: iou
            Assert.Contains("iou", piperIpa); // iou in piper
        }

        [Fact]
        public void Er_StandardUsesSchwaRetroflex_PiperUsesRhoticSchwa()
        {
            // er: standard に əɻ (U+0259 + U+027B), piper に ɚ (U+025A)
            var standardIpa = _engine.ToIPA("二", false); // èr, 声調なし
            var piperIpa = _engine.ToPiperIPA("二");

            // standard: əɻ
            Assert.Contains("\u0259", standardIpa); // ə in standard
            Assert.Contains("\u027B", standardIpa); // ɻ in standard

            // piper: ɚ (単一文字)
            Assert.Contains("\u025A", piperIpa); // ɚ in piper
            Assert.DoesNotContain("\u0259", piperIpa); // ə NOT in piper (ɚは独立文字)
        }

        [Fact]
        public void Iong_StandardUsesNearCloseBack_PiperUsesCloseBack()
        {
            // iong: standard に iʊŋ (i + U+028A + U+014B), piper に iuŋ (i + u + U+014B)
            var standardIpa = _engine.ToIPA("穷", false); // qióng, 声調なし
            var piperIpa = _engine.ToPiperIPA("穷");

            // standard: iʊŋ
            Assert.Contains("i\u028A\u014B", standardIpa); // iʊŋ in standard

            // piper: iuŋ
            Assert.Contains("iu\u014B", piperIpa); // iuŋ in piper
        }

        [Fact]
        public void Van_StandardUsesYan_PiperUsesYEpsilonN()
        {
            // van (üan): standard に yan, piper に yɛn (y + U+025B + n)
            var standardIpa = _engine.ToIPA("元", false); // yuán, 声調なし
            var piperIpa = _engine.ToPiperIPA("元");

            // standard: yan
            Assert.Contains("yan", standardIpa);

            // piper: yɛn (y + ɛ + n)
            Assert.Contains("y\u025Bn", piperIpa); // yɛn in piper
        }

        // =====================================================================
        // 3. 差異がない声母の確認
        //    b/p/m/f/d/t/n/l/g/k/h/j/q/x/sh/r/z/c/s は両方同じIPA
        // =====================================================================

        [Theory]
        // 唇音: b→p, p→pʰ, m→m, f→f
        [InlineData("八", "p")]     // b→p
        [InlineData("怕", "p\u02B0")] // p→pʰ
        [InlineData("妈", "m")]     // m→m
        [InlineData("法", "f")]     // f→f
        // 歯茎音: d→t, t→tʰ, n→n, l→l
        [InlineData("大", "t")]     // d→t
        [InlineData("他", "t\u02B0")] // t→tʰ
        [InlineData("那", "n")]     // n→n
        [InlineData("拉", "l")]     // l→l
        // 軟口蓋音: g→k, k→kʰ, h→x
        [InlineData("高", "k")]     // g→k
        [InlineData("看", "k\u02B0")] // k→kʰ
        [InlineData("好", "x")]     // h→x
        // 硬口蓋音: j→tɕ, q→tɕʰ, x→ɕ
        [InlineData("几", "t\u0255")] // j→tɕ
        [InlineData("七", "t\u0255\u02B0")] // q→tɕʰ
        [InlineData("西", "\u0255")] // x→ɕ
        // そり舌摩擦音: sh→ʂ, r→ɻ（shとrは両方同じ）
        [InlineData("沙", "\u0282")] // sh→ʂ
        [InlineData("热", "\u027B")] // r→ɻ
        // 歯茎破擦音/摩擦音: z→ts, c→tsʰ, s→s
        [InlineData("在", "ts")]     // z→ts
        [InlineData("才", "ts\u02B0")] // c→tsʰ
        [InlineData("三", "s")]     // s→s
        public void CommonInitials_BothOutputsContainSameIpa(string text, string expectedInitialIpa)
        {
            var standardIpa = _engine.ToIPA(text, false); // 声調なし
            var piperIpa = _engine.ToPiperIPA(text);

            // 両方とも同じ声母IPAを含む
            Assert.Contains(expectedInitialIpa, standardIpa);
            Assert.Contains(expectedInitialIpa, piperIpa);
        }

        // =====================================================================
        // 4. 差異がない韻母の確認
        //    a/o/e/i/u/ai/ei/ao/ou/an/en/ang/eng 等は両方同じIPA
        // =====================================================================

        [Theory]
        // 単母音
        [InlineData("啊", "a")]     // a→a
        [InlineData("哦", "o")]     // o→o
        [InlineData("鹅", "\u0264")] // e→ɤ
        // 二重母音
        [InlineData("爱", "a\u026A")] // ai→aɪ
        [InlineData("北", "e\u026A")] // ei→eɪ
        [InlineData("好", "a\u028A")] // ao→aʊ（haoのao部分）
        [InlineData("走", "o\u028A")] // ou→oʊ（zouのou部分）
        // 鼻母音
        [InlineData("安", "an")]    // an→an
        [InlineData("恩", "\u0259n")] // en→ən
        [InlineData("昂", "a\u014B")] // ang→aŋ
        [InlineData("风", "\u0259\u014B")] // eng→əŋ（fengのeng部分）
        public void CommonFinals_BothOutputsContainSameIpa(string text, string expectedFinalIpa)
        {
            var standardIpa = _engine.ToIPA(text, false); // 声調なし
            var piperIpa = _engine.ToPiperIPA(text);

            // 両方とも同じ韻母IPAを含む
            Assert.Contains(expectedFinalIpa, standardIpa);
            Assert.Contains(expectedFinalIpa, piperIpa);
        }

        // =====================================================================
        // 5. z/c/s + i の歯茎母音の差異
        //    standard: ɹ̩ (U+0279 + U+0329)
        //    piper:    ɨ (U+0268)
        // =====================================================================

        [Theory]
        [InlineData("子")] // zǐ: z + apical vowel
        [InlineData("次")] // cì: c + apical vowel
        [InlineData("四")] // sì: s + apical vowel
        public void AlveolarApicalVowel_StandardUsesAlveolarApproximant_PiperUsesBarredI(string text)
        {
            var standardIpa = _engine.ToIPA(text, false); // 声調なし
            var piperIpa = _engine.ToPiperIPA(text);

            // standard: ɹ̩ (U+0279 + U+0329)
            Assert.Contains("\u0279\u0329", standardIpa); // ɹ̩ in standard

            // piper: ɨ (U+0268)
            Assert.Contains("\u0268", piperIpa); // ɨ in piper
            Assert.DoesNotContain("\u0279", piperIpa); // ɹ NOT in piper
        }

        [Theory]
        [InlineData("知")] // zhī: zh + retroflex apical vowel
        [InlineData("吃")] // chī: ch + retroflex apical vowel
        [InlineData("十")] // shí: sh + retroflex apical vowel
        [InlineData("日")] // rì: r + retroflex apical vowel
        public void RetroflexApicalVowel_BothUseSameIpa(string text)
        {
            var standardIpa = _engine.ToIPA(text, false); // 声調なし
            var piperIpa = _engine.ToPiperIPA(text);

            // 両方とも ɻ̩ (U+027B + U+0329) を使う（そり舌母音は共通）
            Assert.Contains("\u027B\u0329", standardIpa); // ɻ̩ in standard
            Assert.Contains("\u027B\u0329", piperIpa);    // ɻ̩ in piper
        }

        // =====================================================================
        // 6. 声調マーカーの差異
        //    standard ToIPA: 声調 tone letter 含む
        //    piper ToPiperIPA: 声調 tone letter 含まない
        // =====================================================================

        [Fact]
        public void ToneMarkers_StandardContainsToneLetters_PiperDoesNot()
        {
            // 1声（˥˥ = U+02E5 U+02E5）
            var standardIpa1 = _engine.ToIPA("妈"); // mā (1声)
            var piperIpa1 = _engine.ToPiperIPA("妈");

            Assert.Contains("\u02E5", standardIpa1); // standard にはtone letter含む
            Assert.DoesNotContain("\u02E5", piperIpa1); // piper にはtone letter含まない

            // 2声（˧˥ = U+02E7 U+02E5）
            var standardIpa2 = _engine.ToIPA("麻"); // má (2声)
            var piperIpa2 = _engine.ToPiperIPA("麻");

            Assert.Contains("\u02E7", standardIpa2); // standard にはtone letter含む
            Assert.DoesNotContain("\u02E7", piperIpa2); // piper にはtone letter含まない

            // 3声（˨˩˦ = U+02E8 U+02E9 U+02E6）
            var standardIpa3 = _engine.ToIPA("马"); // mǎ (3声)
            var piperIpa3 = _engine.ToPiperIPA("马");

            Assert.Contains("\u02E8", standardIpa3); // standard にはtone letter含む
            Assert.DoesNotContain("\u02E8", piperIpa3); // piper にはtone letter含まない

            // 4声（˥˩ = U+02E5 U+02E9）
            var standardIpa4 = _engine.ToIPA("骂"); // mà (4声)
            var piperIpa4 = _engine.ToPiperIPA("骂");

            Assert.Contains("\u02E9", standardIpa4); // standard にはtone letter含む
            Assert.DoesNotContain("\u02E9", piperIpa4); // piper にはtone letter含まない
        }

        [Fact]
        public void ToneMarkers_StandardWithoutTones_MatchesPiperBasicStructure()
        {
            // 声調なしの標準IPAとpiper IPAは、差異がある箇所以外は同じ構造
            var standardNoTone = _engine.ToIPA("八", false); // bā → pa (声調なし)
            var piperIpa = _engine.ToPiperIPA("八");

            // 声調なし標準IPAとpiper IPAは一致する（差異がない声母+韻母の場合）
            Assert.Equal(standardNoTone, piperIpa);
        }

        // =====================================================================
        // 追加: 複数文字テキストでの比較
        // =====================================================================

        [Fact]
        public void MultiCharText_DifferencesAreConsistent()
        {
            // "中国" (zhōng guó) を両方で変換
            var standardIpa = _engine.ToIPA("中国", false);
            var piperIpa = _engine.ToPiperIPA("中国");

            // standard: zhōng → ʈʂʊŋ (ʈ含む, ʊ含む)
            Assert.Contains("\u0288", standardIpa); // ʈ in standard (zh声母)
            Assert.Contains("\u028A", standardIpa); // ʊ in standard (ong韻母)

            // piper: zhōng → tʂuŋ (ʈ含まない, u使用)
            Assert.DoesNotContain("\u0288", piperIpa); // ʈ NOT in piper
            // ong韻母: piperはuŋ（ʊではなくu）
            Assert.DoesNotContain("\u028A", piperIpa); // ʊ NOT in piper
        }

        [Fact]
        public void MultiCharText_BothProduceNonEmptyOutput()
        {
            var text = "你好世界";
            var standardIpa = _engine.ToIPA(text);
            var piperIpa = _engine.ToPiperIPA(text);

            Assert.NotEmpty(standardIpa);
            Assert.NotEmpty(piperIpa);
            // 両方ともスペース区切りで複数音節を含む
            Assert.Contains(" ", standardIpa);
            Assert.Contains(" ", piperIpa);
        }
    }
}
