using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinParser の単体テスト。
    /// Parse / TryParse の各種パターンを検証する。
    /// </summary>
    public class PinyinParserTests
    {
        // ===== Parse: 基本的な声母+韻母+声調 =====

        [Fact]
        public void Parse_Zhong1_ReturnsZhOngFirst()
        {
            var result = PinyinParser.Parse("zhōng");
            Assert.Equal(Initial.Zh, result.Initial);
            Assert.Equal(Final.Ong, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Guo2_ReturnsGUoSecond()
        {
            var result = PinyinParser.Parse("guó");
            Assert.Equal(Initial.G, result.Initial);
            Assert.Equal(Final.Uo, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        [Fact]
        public void Parse_Hao3_ReturnsHAoThird()
        {
            var result = PinyinParser.Parse("hǎo");
            Assert.Equal(Initial.H, result.Initial);
            Assert.Equal(Final.Ao, result.Final);
            Assert.Equal(Tone.Third, result.Tone);
        }

        [Fact]
        public void Parse_Shi4_ReturnsShIFourth()
        {
            var result = PinyinParser.Parse("shì");
            Assert.Equal(Initial.Sh, result.Initial);
            Assert.Equal(Final.I, result.Final);
            Assert.Equal(Tone.Fourth, result.Tone);
        }

        [Fact]
        public void Parse_De_ReturnsDENeutral()
        {
            var result = PinyinParser.Parse("de");
            Assert.Equal(Initial.D, result.Initial);
            Assert.Equal(Final.E, result.Final);
            Assert.Equal(Tone.Neutral, result.Tone);
        }

        // ===== Parse: ゼロ声母 =====

        [Fact]
        public void Parse_A1_ReturnsNoneAFirst()
        {
            var result = PinyinParser.Parse("ā");
            Assert.Equal(Initial.None, result.Initial);
            Assert.Equal(Final.A, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Er2_ReturnsNoneErSecond()
        {
            var result = PinyinParser.Parse("ér");
            Assert.Equal(Initial.None, result.Initial);
            Assert.Equal(Final.Er, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        [Fact]
        public void Parse_Ou1_ReturnsNoneOuFirst()
        {
            var result = PinyinParser.Parse("ōu");
            Assert.Equal(Initial.None, result.Initial);
            Assert.Equal(Final.Ou, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Ai4_ReturnsNoneAiFourth()
        {
            var result = PinyinParser.Parse("ài");
            Assert.Equal(Initial.None, result.Initial);
            Assert.Equal(Final.Ai, result.Final);
            Assert.Equal(Tone.Fourth, result.Tone);
        }

        // ===== Parse: 2文字声母 (zh/ch/sh) =====

        [Fact]
        public void Parse_Zhi1_ReturnsZhIFirst()
        {
            var result = PinyinParser.Parse("zhī");
            Assert.Equal(Initial.Zh, result.Initial);
            Assert.Equal(Final.I, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Chi1_ReturnsChIFirst()
        {
            var result = PinyinParser.Parse("chī");
            Assert.Equal(Initial.Ch, result.Initial);
            Assert.Equal(Final.I, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Shi4_2char_ReturnsShIFourth()
        {
            var result = PinyinParser.Parse("shì");
            Assert.Equal(Initial.Sh, result.Initial);
            Assert.Equal(Final.I, result.Final);
            Assert.Equal(Tone.Fourth, result.Tone);
        }

        // ===== Parse: j/q/x + ü → V系韻母 =====

        [Fact]
        public void Parse_Ju3_ReturnsJVThird()
        {
            var result = PinyinParser.Parse("jǔ");
            Assert.Equal(Initial.J, result.Initial);
            Assert.Equal(Final.V, result.Final);
            Assert.Equal(Tone.Third, result.Tone);
        }

        [Fact]
        public void Parse_Qu4_ReturnsQVFourth()
        {
            var result = PinyinParser.Parse("qù");
            Assert.Equal(Initial.Q, result.Initial);
            Assert.Equal(Final.V, result.Final);
            Assert.Equal(Tone.Fourth, result.Tone);
        }

        [Fact]
        public void Parse_Xuan2_ReturnsXVanSecond()
        {
            var result = PinyinParser.Parse("xuán");
            Assert.Equal(Initial.X, result.Initial);
            Assert.Equal(Final.Van, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        [Fact]
        public void Parse_Xue2_ReturnsXVeSecond()
        {
            var result = PinyinParser.Parse("xué");
            Assert.Equal(Initial.X, result.Initial);
            Assert.Equal(Final.Ve, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        [Fact]
        public void Parse_Jun1_ReturnsJVnFirst()
        {
            var result = PinyinParser.Parse("jūn");
            Assert.Equal(Initial.J, result.Initial);
            Assert.Equal(Final.Vn, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        // ===== Parse: 半母音 y/w =====

        [Fact]
        public void Parse_Yi1_ReturnsYIFirst()
        {
            var result = PinyinParser.Parse("yī");
            Assert.Equal(Initial.Y, result.Initial);
            Assert.Equal(Final.I, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Wo3_ReturnsWOThird()
        {
            var result = PinyinParser.Parse("wǒ");
            Assert.Equal(Initial.W, result.Initial);
            Assert.Equal(Final.O, result.Final);
            Assert.Equal(Tone.Third, result.Tone);
        }

        [Fact]
        public void Parse_Yuan2_ReturnsYVanSecond()
        {
            // y + uan → y後のuはü扱い → Y + Van
            var result = PinyinParser.Parse("yuán");
            Assert.Equal(Initial.Y, result.Initial);
            Assert.Equal(Final.Van, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        // ===== Parse: 1文字声母 =====

        [Fact]
        public void Parse_Ma1_ReturnsMaFirst()
        {
            var result = PinyinParser.Parse("mā");
            Assert.Equal(Initial.M, result.Initial);
            Assert.Equal(Final.A, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Ren2_ReturnsREnSecond()
        {
            var result = PinyinParser.Parse("rén");
            Assert.Equal(Initial.R, result.Initial);
            Assert.Equal(Final.En, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        // ===== Parse: 複合韻母 =====

        [Fact]
        public void Parse_Zhuang1_ReturnsZhUangFirst()
        {
            var result = PinyinParser.Parse("zhuāng");
            Assert.Equal(Initial.Zh, result.Initial);
            Assert.Equal(Final.Uang, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void Parse_Liang2_ReturnsLIangSecond()
        {
            var result = PinyinParser.Parse("liáng");
            Assert.Equal(Initial.L, result.Initial);
            Assert.Equal(Final.Iang, result.Final);
            Assert.Equal(Tone.Second, result.Tone);
        }

        // ===== TryParse: 成功ケース =====

        [Fact]
        public void TryParse_ValidPinyin_ReturnsTrue()
        {
            bool success = PinyinParser.TryParse("zhōng", out var result);
            Assert.True(success);
            Assert.Equal(Initial.Zh, result.Initial);
            Assert.Equal(Final.Ong, result.Final);
            Assert.Equal(Tone.First, result.Tone);
        }

        [Fact]
        public void TryParse_NeutralTone_ReturnsTrue()
        {
            bool success = PinyinParser.TryParse("de", out var result);
            Assert.True(success);
            Assert.Equal(Tone.Neutral, result.Tone);
        }

        // ===== TryParse: 失敗ケース =====

        [Fact]
        public void TryParse_EmptyString_ReturnsFalse()
        {
            bool success = PinyinParser.TryParse("", out var result);
            Assert.False(success);
            Assert.Equal(default(PinyinSyllable), result);
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            bool success = PinyinParser.TryParse(null!, out var result);
            Assert.False(success);
            Assert.Equal(default(PinyinSyllable), result);
        }

        [Fact]
        public void TryParse_WhitespaceOnly_ReturnsFalse()
        {
            bool success = PinyinParser.TryParse("   ", out var result);
            Assert.False(success);
        }

        [Fact]
        public void TryParse_InvalidString_ReturnsFalse()
        {
            bool success = PinyinParser.TryParse("xyz", out _);
            Assert.False(success);
        }

        // ===== Parse: 異常系 =====

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => PinyinParser.Parse(null!));
        }

        [Fact]
        public void Parse_InvalidPinyin_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => PinyinParser.Parse("xyz"));
        }

        // ===== Parse: ü を直接含むピンイン =====

        [Fact]
        public void Parse_LvWithUmlaut4_ReturnsLVFourth()
        {
            // lǜ = l + ǜ (U+01DC)
            var result = PinyinParser.Parse("lǜ");
            Assert.Equal(Initial.L, result.Initial);
            Assert.Equal(Final.V, result.Final);
            Assert.Equal(Tone.Fourth, result.Tone);
        }

        [Fact]
        public void Parse_Nv3_ReturnsNVThird()
        {
            var result = PinyinParser.Parse("nǚ");
            Assert.Equal(Initial.N, result.Initial);
            Assert.Equal(Final.V, result.Final);
            Assert.Equal(Tone.Third, result.Tone);
        }
    }
}
