using DotNetG2P.Spanish.Normalization;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class NumberToWordsTests
    {
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
