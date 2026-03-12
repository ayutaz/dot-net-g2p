using System.Collections.Generic;
using System.IO;
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
        [InlineData("담요", KoreanUiVariationMode.Standard, "담뇨")]
        [InlineData("넓죽하다", KoreanUiVariationMode.Standard, "넙쭉하다")]
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

        [Fact]
        public void ParseEntries_RejectsUnknownMode()
        {
            var lines = new List<string>
            {
                "surface\tui_mode\tpronunciation\tcategory\tsource\tnotes",
                "나의\tunexpected\t나에\tui-variation\tTest\tinvalid mode",
            };

            Assert.Throws<InvalidDataException>(() => KoreanExceptionDictionary.ParseEntries(lines));
        }

        [Fact]
        public void ParseEntries_RejectsDuplicateSurfaceAndMode()
        {
            var lines = new List<string>
            {
                "surface\tui_mode\tpronunciation\tcategory\tsource\tnotes",
                "나의\tstandard\t나의\tui-variation\tTest\tfirst",
                "나의\tstandard\t나에\tui-variation\tTest\tduplicate",
            };

            Assert.Throws<InvalidDataException>(() => KoreanExceptionDictionary.ParseEntries(lines));
        }
    }
}
