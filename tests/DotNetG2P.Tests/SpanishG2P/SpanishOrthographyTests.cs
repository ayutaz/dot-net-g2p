using DotNetG2P.Spanish.Rules;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishOrthographyTests
    {
        [Theory]
        [InlineData('a', true)]
        [InlineData('e', true)]
        [InlineData('i', true)]
        [InlineData('o', true)]
        [InlineData('u', true)]
        [InlineData('á', true)]
        [InlineData('é', true)]
        [InlineData('í', true)]
        [InlineData('ó', true)]
        [InlineData('ú', true)]
        [InlineData('ü', true)]
        [InlineData('b', false)]
        [InlineData('c', false)]
        [InlineData('d', false)]
        [InlineData('f', false)]
        public void IsVowelChar_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.IsVowelChar(c));
        }

        [Theory]
        [InlineData("a", 0, true)]
        [InlineData("e", 0, true)]
        [InlineData("i", 0, true)]
        [InlineData("o", 0, true)]
        [InlineData("u", 0, true)]
        [InlineData("á", 0, true)]
        [InlineData("ü", 0, true)]
        [InlineData("b", 0, false)]
        [InlineData("y", 0, true)]     // standalone y is a vowel
        [InlineData("ya", 0, false)]   // y at start of multi-char word is consonant
        [InlineData("ay", 1, true)]    // y at end of word is vowel
        public void IsPronouncedVowel_ReturnsExpected(string word, int index, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.IsPronouncedVowel(word, index));
        }

        [Theory]
        [InlineData("que", 1, false)]   // u after q before e is silent
        [InlineData("gui", 1, false)]   // u after g before i is silent
        [InlineData("gue", 1, false)]   // u after g before e is silent
        public void IsPronouncedVowel_SilentU_ReturnsFalse(string word, int index, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.IsPronouncedVowel(word, index));
        }

        [Theory]
        [InlineData('a', true)]
        [InlineData('e', true)]
        [InlineData('o', true)]
        [InlineData('á', true)]
        [InlineData('é', true)]
        [InlineData('ó', true)]
        [InlineData('i', false)]
        [InlineData('u', false)]
        [InlineData('í', false)]
        [InlineData('ú', false)]
        [InlineData('b', false)]
        public void IsStrongVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.IsStrongVowel(c));
        }

        [Theory]
        [InlineData('i', true)]
        [InlineData('u', true)]
        [InlineData('ü', true)]
        [InlineData('y', true)]
        [InlineData('a', false)]
        [InlineData('í', false)]
        [InlineData('ú', false)]
        public void IsWeakUnaccentedVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.IsWeakUnaccentedVowel(c));
        }

        [Theory]
        [InlineData('i', 'a', true)]    // weak + strong
        [InlineData('a', 'i', true)]    // strong + weak
        [InlineData('i', 'u', true)]    // weak + weak
        [InlineData('a', 'e', false)]   // strong + strong = hiatus
        [InlineData('o', 'a', false)]   // strong + strong = hiatus
        [InlineData('í', 'a', false)]   // accented weak + strong = hiatus
        [InlineData('a', 'ú', false)]   // strong + accented weak = hiatus
        public void CanFormDiphthong_ReturnsExpected(char left, char right, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.CanFormDiphthong(left, right));
        }

        [Theory]
        [InlineData('á', true)]
        [InlineData('é', true)]
        [InlineData('í', true)]
        [InlineData('ó', true)]
        [InlineData('ú', true)]
        [InlineData('a', false)]
        [InlineData('e', false)]
        [InlineData('i', false)]
        [InlineData('o', false)]
        [InlineData('u', false)]
        [InlineData('ü', false)]
        public void HasWrittenAccent_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.HasWrittenAccent(c));
        }

        [Theory]
        [InlineData('i', 'a', 'i', true)]   // weak + strong + weak
        [InlineData('u', 'e', 'i', true)]   // weak + strong + weak
        [InlineData('a', 'e', 'i', false)]  // strong + strong + weak
        [InlineData('i', 'i', 'i', false)]  // weak + weak + weak (middle not strong)
        public void CanFormTriphthong_ReturnsExpected(char first, char second, char third, bool expected)
        {
            Assert.Equal(expected, SpanishOrthography.CanFormTriphthong(first, second, third));
        }
    }
}
