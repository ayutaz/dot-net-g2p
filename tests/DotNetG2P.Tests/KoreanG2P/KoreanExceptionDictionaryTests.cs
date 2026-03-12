using DotNetG2P.Korean;
using DotNetG2P.Korean.Data;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanExceptionDictionaryTests
    {
        [Theory]
        [InlineData("나의", KoreanUiVariationMode.Standard, "나의")]
        [InlineData("나의", KoreanUiVariationMode.Colloquial, "나에")]
        [InlineData("밟다", KoreanUiVariationMode.Standard, "밥따")]
        [InlineData("검열", KoreanUiVariationMode.Standard, "검녈")]
        public void TryLookup_KnownEntry_ReturnsPronunciation(string input, KoreanUiVariationMode mode, string expected)
        {
            Assert.True(KoreanExceptionDictionary.TryLookup(input, mode, out var pronunciation));
            Assert.Equal(expected, pronunciation);
        }

        [Theory]
        [InlineData("국밥")]
        [InlineData("한글")]
        [InlineData("xyz")]
        public void TryLookup_UnknownEntry_ReturnsFalse(string input)
        {
            Assert.False(KoreanExceptionDictionary.TryLookup(input, KoreanUiVariationMode.Standard, out _));
        }

        [Fact]
        public void TryLookup_NullEntry_ReturnsFalse()
        {
            Assert.False(KoreanExceptionDictionary.TryLookup(null!, KoreanUiVariationMode.Standard, out _));
        }
    }
}
