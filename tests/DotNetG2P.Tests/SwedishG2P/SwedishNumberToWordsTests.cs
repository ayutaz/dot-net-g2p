using DotNetG2P.Swedish.Normalization;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語数詞変換テスト。
    /// NumberToWords.ToCardinal()、ToOrdinal()、ToDecimal() を検証する。
    /// </summary>
    public class SwedishNumberToWordsTests
    {
        // =================================================================
        // 基数詞 0-20 テスト
        // =================================================================

        [Theory]
        [InlineData(0, "noll")]
        [InlineData(1, "ett")]
        [InlineData(2, "tv\u00E5")]       // två
        [InlineData(3, "tre")]
        [InlineData(4, "fyra")]
        [InlineData(5, "fem")]
        [InlineData(6, "sex")]
        [InlineData(7, "sju")]
        [InlineData(8, "\u00E5tta")]       // åtta
        [InlineData(9, "nio")]
        [InlineData(10, "tio")]
        [InlineData(11, "elva")]
        [InlineData(12, "tolv")]
        [InlineData(13, "tretton")]
        [InlineData(14, "fjorton")]
        [InlineData(15, "femton")]
        [InlineData(16, "sexton")]
        [InlineData(17, "sjutton")]
        [InlineData(18, "arton")]
        [InlineData(19, "nitton")]
        [InlineData(20, "tjugo")]
        public void ToCardinal_ZeroToTwenty_ReturnsExpected(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.ToCardinal(number));
        }

        // =================================================================
        // en/ett 区別テスト
        // =================================================================

        [Fact]
        public void ToCardinal_One_UseEn_ReturnsEn()
        {
            // 通性形: en
            Assert.Equal("en", NumberToWords.ToCardinal(1, useEn: true));
        }

        [Fact]
        public void ToCardinal_One_UseEtt_ReturnsEtt()
        {
            // 中性形（デフォルト）: ett
            Assert.Equal("ett", NumberToWords.ToCardinal(1, useEn: false));
        }

        // =================================================================
        // 合成数詞テスト（21-99）
        // =================================================================

        [Theory]
        [InlineData(21, "tjugoett")]
        [InlineData(42, "fyrtiotv\u00E5")]       // fyrtiotvå
        [InlineData(99, "nittionio")]
        [InlineData(55, "femtiofem")]
        [InlineData(73, "sjuttiotre")]
        public void ToCardinal_CompoundNumbers_ReturnsExpected(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.ToCardinal(number));
        }

        // =================================================================
        // 十の位テスト
        // =================================================================

        [Theory]
        [InlineData(30, "trettio")]
        [InlineData(40, "fyrtio")]
        [InlineData(50, "femtio")]
        [InlineData(60, "sextio")]
        [InlineData(70, "sjuttio")]
        [InlineData(80, "\u00E5ttio")]             // åttio
        [InlineData(90, "nittio")]
        public void ToCardinal_Tens_ReturnsExpected(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.ToCardinal(number));
        }

        // =================================================================
        // 百の位テスト
        // =================================================================

        [Fact]
        public void ToCardinal_OneHundred_ReturnsEtthundra()
        {
            Assert.Equal("etthundra", NumberToWords.ToCardinal(100));
        }

        [Fact]
        public void ToCardinal_TwoHundred_ContainsHundra()
        {
            var result = NumberToWords.ToCardinal(200);
            Assert.Contains("hundra", result);
        }

        // =================================================================
        // 千の位テスト
        // =================================================================

        [Fact]
        public void ToCardinal_OneThousand_ContainsTusen()
        {
            var result = NumberToWords.ToCardinal(1000);
            Assert.Contains("tusen", result);
        }

        // =================================================================
        // 大数テスト（長い目盛り）
        // =================================================================

        [Fact]
        public void ToCardinal_OneMillion_ContainsMilljon()
        {
            var result = NumberToWords.ToCardinal(1_000_000);
            Assert.Contains("miljon", result);
        }

        [Fact]
        public void ToCardinal_OneBillion_ContainsMilljard()
        {
            var result = NumberToWords.ToCardinal(1_000_000_000);
            Assert.Contains("miljard", result);
        }

        // =================================================================
        // 大きな合成数詞テスト
        // =================================================================

        [Fact]
        public void ToCardinal_LargeCompound_ContainsExpectedParts()
        {
            // 1,234,567 → "en miljon tvåhundratrettiofyratusen femhundrasextiosju" の各部分
            var result = NumberToWords.ToCardinal(1_234_567);
            Assert.Contains("miljon", result);
            Assert.Contains("tusen", result);
            Assert.Contains("hundra", result);
        }

        // =================================================================
        // 負の数テスト
        // =================================================================

        [Fact]
        public void ToCardinal_Negative_StartsWithMinus()
        {
            Assert.Equal("minus fem", NumberToWords.ToCardinal(-5));
        }

        // =================================================================
        // 序数詞テスト
        // =================================================================

        [Theory]
        [InlineData(1, "f\u00F6rsta")]     // första
        [InlineData(2, "andra")]
        [InlineData(3, "tredje")]
        [InlineData(4, "fj\u00E4rde")]     // fjärde
        [InlineData(5, "femte")]
        [InlineData(10, "tionde")]
        [InlineData(11, "elfte")]
        [InlineData(12, "tolfte")]
        public void ToOrdinal_BasicOrdinals_ReturnsExpected(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.ToOrdinal(number));
        }

        [Fact]
        public void ToOrdinal_Twenty_ReturnsTjugonde()
        {
            Assert.Equal("tjugonde", NumberToWords.ToOrdinal(20));
        }

        [Fact]
        public void ToOrdinal_ThirtyFirst_ContainsTrettio()
        {
            var result = NumberToWords.ToOrdinal(31);
            Assert.Contains("trettio", result);
            Assert.Contains("f\u00F6rsta", result); // första
        }

        // =================================================================
        // 小数テスト
        // =================================================================

        [Fact]
        public void ToDecimal_CommaSeparated_ReturnsExpected()
        {
            // "3,14" → "tre komma ett fyra"（小数部は桁ごとに読み上げ）
            var result = NumberToWords.ToDecimal("3,14");
            Assert.Equal("tre komma ett fyra", result);
        }

        [Fact]
        public void ToDecimal_PeriodSeparated_ReturnsExpected()
        {
            // "3.14" → "tre komma ett fyra" (ピリオドもサポート、小数部は桁ごとに読み上げ)
            var result = NumberToWords.ToDecimal("3.14");
            Assert.Equal("tre komma ett fyra", result);
        }

        [Fact]
        public void ToDecimal_NoDecimalPoint_ReturnsCardinal()
        {
            // 小数点なし → 通常の基数詞
            var result = NumberToWords.ToDecimal("42");
            Assert.Equal("fyrtiotv\u00E5", result); // fyrtiotvå
        }

        [Fact]
        public void ToDecimal_Empty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, NumberToWords.ToDecimal(""));
        }

        // =================================================================
        // ConvertDigits テスト
        // =================================================================

        [Fact]
        public void ConvertDigits_PhoneNumber_DigitsReadIndividually()
        {
            // 各桁を個別に読む
            var result = NumberToWords.ConvertDigits("123");
            Assert.Equal("ett tv\u00E5 tre", result); // "ett två tre"
        }

        [Fact]
        public void ConvertDigits_Empty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, NumberToWords.ConvertDigits(""));
        }
    }
}
