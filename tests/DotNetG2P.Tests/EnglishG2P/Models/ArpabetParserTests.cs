using System;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Models
{
    /// <summary>
    /// ArpabetParser の単体テスト。
    /// 正常系・異常系・子音ストレス強制Noneの検証を行う。
    /// </summary>
    public class ArpabetParserTests
    {
        // ===== Parse: 正常系 =====

        [Theory]
        [InlineData("AH0", ArpabetPhoneme.AH, Stress.NoStress)]
        [InlineData("AH1", ArpabetPhoneme.AH, Stress.Primary)]
        [InlineData("AH2", ArpabetPhoneme.AH, Stress.Secondary)]
        [InlineData("K", ArpabetPhoneme.K, Stress.None)]
        [InlineData("HH", ArpabetPhoneme.HH, Stress.None)]
        [InlineData("CH", ArpabetPhoneme.CH, Stress.None)]
        [InlineData("OW1", ArpabetPhoneme.OW, Stress.Primary)]
        [InlineData("IY0", ArpabetPhoneme.IY, Stress.NoStress)]
        public void Parse_ValidTokens_ReturnsCorrectPhoneme(string token, ArpabetPhoneme expectedPhoneme, Stress expectedStress)
        {
            var result = ArpabetParser.Parse(token);
            Assert.Equal(expectedPhoneme, result.Phoneme);
            Assert.Equal(expectedStress, result.Stress);
        }

        // ===== Parse: 異常系 =====

        [Fact]
        public void Parse_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse(""));
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse(null!));
        }

        [Fact]
        public void Parse_UnknownToken_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse("XX"));
        }

        [Fact]
        public void Parse_UnknownTokenWithStress_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse("XX0"));
        }

        // ===== Parse: 子音にストレスが付いた場合はNoneに強制（W5修正検証） =====

        [Theory]
        [InlineData("K1")]
        [InlineData("B0")]
        [InlineData("S2")]
        [InlineData("T1")]
        [InlineData("N0")]
        public void Parse_ConsonantWithStress_ForcesStressNone(string token)
        {
            var result = ArpabetParser.Parse(token);
            Assert.Equal(Stress.None, result.Stress);
        }

        // ===== TryParse: 正常系 =====

        [Theory]
        [InlineData("AH0", true)]
        [InlineData("K", true)]
        [InlineData("OW1", true)]
        public void TryParse_ValidTokens_ReturnsTrue(string token, bool expected)
        {
            Assert.Equal(expected, ArpabetParser.TryParse(token, out _));
        }

        [Fact]
        public void TryParse_AH0_ReturnsCorrectResult()
        {
            Assert.True(ArpabetParser.TryParse("AH0", out var result));
            Assert.Equal(ArpabetPhoneme.AH, result.Phoneme);
            Assert.Equal(Stress.NoStress, result.Stress);
        }

        // ===== TryParse: 異常系 =====

        [Fact]
        public void TryParse_EmptyString_ReturnsFalse()
        {
            Assert.False(ArpabetParser.TryParse("", out _));
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            Assert.False(ArpabetParser.TryParse(null!, out _));
        }

        [Fact]
        public void TryParse_UnknownToken_ReturnsFalse()
        {
            Assert.False(ArpabetParser.TryParse("XX", out _));
        }

        // ===== TryParse: 子音にストレスが付いた場合もNoneに強制 =====

        [Fact]
        public void TryParse_ConsonantWithStress_ForcesStressNone()
        {
            Assert.True(ArpabetParser.TryParse("K1", out var result));
            Assert.Equal(ArpabetPhoneme.K, result.Phoneme);
            Assert.Equal(Stress.None, result.Stress);
        }

        // ===== PhonemeToString: 正常系 =====

        [Theory]
        [InlineData(ArpabetPhoneme.AA, "AA")]
        [InlineData(ArpabetPhoneme.AH, "AH")]
        [InlineData(ArpabetPhoneme.K, "K")]
        [InlineData(ArpabetPhoneme.HH, "HH")]
        [InlineData(ArpabetPhoneme.CH, "CH")]
        [InlineData(ArpabetPhoneme.SH, "SH")]
        [InlineData(ArpabetPhoneme.ZH, "ZH")]
        [InlineData(ArpabetPhoneme.NG, "NG")]
        public void PhonemeToString_ValidPhoneme_ReturnsCorrectName(ArpabetPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, ArpabetParser.PhonemeToString(phoneme));
        }

        // ===== PhonemeToString: 異常系 =====

        [Fact]
        public void PhonemeToString_InvalidValue_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ArpabetParser.PhonemeToString((ArpabetPhoneme)255));
        }

        // ===== Parse: 境界値テスト =====

        [Fact]
        public void Parse_StressThreeOrAbove_ThrowsArgumentException()
        {
            // ストレス番号3は0-2の範囲外 → ストレスとして認識されない
            // "AH3" → nameLen=3 → 1でも2でもないためパース失敗
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse("AH3"));
        }

        [Fact]
        public void TryParse_StressThreeOrAbove_ReturnsFalse()
        {
            // "AH3" → TryParseでもfalseになる
            Assert.False(ArpabetParser.TryParse("AH3", out _));
        }

        [Fact]
        public void Parse_LowercaseToken_ThrowsArgumentException()
        {
            // 小文字トークン "ah0" → パーサーは大文字のみ対応
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse("ah0"));
        }

        [Fact]
        public void TryParse_LowercaseToken_ReturnsFalse()
        {
            // 小文字トークン "ah0" → false
            Assert.False(ArpabetParser.TryParse("ah0", out _));
        }

        [Fact]
        public void Parse_SingleCharVowel_ThrowsArgumentException()
        {
            // "A" は母音の1文字名だが TryParseSingle には 'A' のcase が無い → 失敗
            Assert.Throws<ArgumentException>(() => ArpabetParser.Parse("A"));
        }

        [Fact]
        public void TryParse_ThreeCharConsonant_ReturnsFalse()
        {
            // "CHH" → nameLen=3（ストレスなし） → 範囲外
            Assert.False(ArpabetParser.TryParse("CHH", out _));
        }
    }
}
