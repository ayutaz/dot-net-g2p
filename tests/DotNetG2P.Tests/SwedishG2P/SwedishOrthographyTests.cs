using DotNetG2P.Swedish.Rules;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishOrthographyTests
    {
        [Theory]
        [InlineData('e', true)] [InlineData('i', true)] [InlineData('y', true)]
        [InlineData('\u00e4', true)] [InlineData('\u00f6', true)]
        [InlineData('a', false)] [InlineData('o', false)] [InlineData('b', false)]
        public void IsSoftVowel_ReturnsExpected(char c, bool expected) =>
            Assert.Equal(expected, SwedishOrthography.IsSoftVowel(c));

        [Theory]
        [InlineData('a', true)] [InlineData('o', true)] [InlineData('u', true)] [InlineData('\u00e5', true)]
        [InlineData('e', false)] [InlineData('i', false)] [InlineData('b', false)]
        public void IsHardVowel_ReturnsExpected(char c, bool expected) =>
            Assert.Equal(expected, SwedishOrthography.IsHardVowel(c));

        [Theory]
        [InlineData('a', true)] [InlineData('e', true)] [InlineData('\u00f6', true)]
        [InlineData('b', false)] [InlineData('1', false)]
        public void IsVowelChar_ReturnsExpected(char c, bool expected) =>
            Assert.Equal(expected, SwedishOrthography.IsVowelChar(c));

        [Theory]
        [InlineData('b', true)] [InlineData('k', true)] [InlineData('s', true)]
        [InlineData('a', false)] [InlineData('1', false)] [InlineData(' ', false)]
        public void IsConsonantChar_ReturnsExpected(char c, bool expected) =>
            Assert.Equal(expected, SwedishOrthography.IsConsonantChar(c));

        [Theory]
        [InlineData("matt", 1, true)]    // a(idx=1) + tt → 二重子音
        [InlineData("mat", 1, false)]     // a(idx=1) + t (1子音のみ)
        [InlineData("dricka", 2, true)]   // i(idx=2) + ck → ck は二重子音
        [InlineData("taxi", 1, true)]     // a(idx=1) + x → x は2子音相当
        [InlineData("ja", 1, false)]      // a(idx=1) + 語末
        public void IsFollowedByDoubleConsonant_ReturnsExpected(string word, int idx, bool expected) =>
            Assert.Equal(expected, SwedishOrthography.IsFollowedByDoubleConsonant(word, idx));
    }
}
