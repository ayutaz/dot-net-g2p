using DotNetG2P.French.Normalization;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class FrenchNumberToWordsTests
    {
        // 基本数詞 (0-19)
        [Theory]
        [InlineData(0, "zéro")]
        [InlineData(1, "un")]
        [InlineData(5, "cinq")]
        [InlineData(11, "onze")]
        [InlineData(16, "seize")]
        [InlineData(19, "dix-neuf")]
        public void Convert_BasicNumbers_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // 20台
        [Fact]
        public void Convert_Twenty_ReturnsVingt()
        {
            Assert.Equal("vingt", NumberToWords.Convert(20));
        }

        // et挿入 (21, 31, 41, 51, 61, 71)
        [Theory]
        [InlineData(21, "vingt et un")]
        [InlineData(31, "trente et un")]
        [InlineData(41, "quarante et un")]
        [InlineData(51, "cinquante et un")]
        [InlineData(61, "soixante et un")]
        public void Convert_EtInsertion_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // 通常の十の位
        [Theory]
        [InlineData(22, "vingt-deux")]
        [InlineData(35, "trente-cinq")]
        [InlineData(48, "quarante-huit")]
        [InlineData(59, "cinquante-neuf")]
        [InlineData(63, "soixante-trois")]
        public void Convert_RegularTens_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // vigesimal 70系列
        [Theory]
        [InlineData(70, "soixante-dix")]
        [InlineData(71, "soixante et onze")]
        [InlineData(72, "soixante-douze")]
        [InlineData(75, "soixante-quinze")]
        [InlineData(79, "soixante-dix-neuf")]
        public void Convert_Seventies_ReturnsVigesimal(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // vigesimal 80系列
        [Theory]
        [InlineData(80, "quatre-vingts")]
        [InlineData(81, "quatre-vingt-un")]
        [InlineData(85, "quatre-vingt-cinq")]
        [InlineData(89, "quatre-vingt-neuf")]
        public void Convert_Eighties_ReturnsVigesimal(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // vigesimal 90系列
        [Theory]
        [InlineData(90, "quatre-vingt-dix")]
        [InlineData(91, "quatre-vingt-onze")]
        [InlineData(95, "quatre-vingt-quinze")]
        [InlineData(99, "quatre-vingt-dix-neuf")]
        public void Convert_Nineties_ReturnsVigesimal(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // 100-999
        [Theory]
        [InlineData(100, "cent")]
        [InlineData(101, "cent un")]
        [InlineData(200, "deux cents")]
        [InlineData(201, "deux cent un")]
        [InlineData(300, "trois cents")]
        [InlineData(999, "neuf cent quatre-vingt-dix-neuf")]
        public void Convert_Hundreds_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // 1000+
        [Theory]
        [InlineData(1000, "mille")]
        [InlineData(1001, "mille un")]
        [InlineData(2000, "deux mille")]
        [InlineData(2025, "deux mille vingt-cinq")]
        public void Convert_Thousands_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // million, milliard
        [Theory]
        [InlineData(1000000, "un million")]
        [InlineData(2000000, "deux millions")]
        [InlineData(1000000000, "un milliard")]
        [InlineData(2000000000, "deux milliards")]
        public void Convert_LargeNumbers_ReturnsCorrect(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // 負の数
        [Fact]
        public void Convert_Negative_ReturnsMoins()
        {
            Assert.Equal("moins cinq", NumberToWords.Convert(-5));
        }

        // 序数詞
        [Theory]
        [InlineData("1er", "premier")]
        [InlineData("1ère", "première")]
        [InlineData("2e", "deuxième")]
        [InlineData("3ème", "troisième")]
        [InlineData("5e", "cinquième")]
        [InlineData("9e", "neuvième")]
        public void ConvertOrdinal_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, NumberToWords.ConvertOrdinal(input));
        }

        // ConvertDigits
        [Fact]
        public void ConvertDigits_ReturnsIndividualDigits()
        {
            Assert.Equal("un deux trois", NumberToWords.ConvertDigits("123"));
        }

        // 文字列版Convert
        [Theory]
        [InlineData("42", "quarante-deux")]
        [InlineData("abc", "abc")]
        [InlineData("", "")]
        public void Convert_String_ReturnsCorrect(string input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(input));
        }
    }
}
