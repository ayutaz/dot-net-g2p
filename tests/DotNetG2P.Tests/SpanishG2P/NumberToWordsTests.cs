using DotNetG2P.Spanish.Normalization;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class NumberToWordsTests
    {
        [Theory]
        [InlineData(0, "cero")]
        [InlineData(1, "uno")]
        [InlineData(15, "quince")]
        [InlineData(29, "veintinueve")]
        [InlineData(30, "treinta")]
        [InlineData(99, "noventa y nueve")]
        [InlineData(100, "cien")]
        [InlineData(101, "ciento uno")]
        [InlineData(999, "novecientos noventa y nueve")]
        [InlineData(1000, "mil")]
        [InlineData(1001, "mil uno")]
        [InlineData(999999, "novecientos noventa y nueve mil novecientos noventa y nueve")]
        [InlineData(1000000, "un millón")]
        public void Convert_Cardinal_ReturnsExpectedWord(long value, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(value));
        }

        [Fact]
        public void Convert_Negative_PrependsMenos()
        {
            Assert.Equal("menos cinco", NumberToWords.Convert(-5));
        }

        [Fact]
        public void ConvertDigits_ReturnsDigitByDigit()
        {
            Assert.Equal("uno dos tres cuatro cinco", NumberToWords.ConvertDigits("12345"));
        }

        [Theory]
        [InlineData(1, 0, true, "un")]
        [InlineData(21, 0, true, "veintiún")]
        [InlineData(31, 0, true, "treinta y un")]
        [InlineData(101, 0, true, "ciento un")]
        [InlineData(1, 1, false, "una")]
        [InlineData(21, 1, false, "veintiuna")]
        [InlineData(31, 1, false, "treinta y una")]
        [InlineData(201, 1, false, "doscientas una")]
        public void ConvertAttributed_ReturnsExpectedAgreement(long value, byte genderValue, bool apocopate, string expected)
        {
            var gender = (SpanishNumberGender)genderValue;
            Assert.Equal(expected, NumberToWords.ConvertAttributed(value, gender, apocopate));
        }
    }
}
