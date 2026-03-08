using System.Linq;
using DotNetG2P.Spanish.Rules;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishSyllabifierTests
    {
        [Theory]
        [InlineData("casa", "ca|sa")]
        [InlineData("camino", "ca|mi|no")]
        [InlineData("acción", "ac|ción")]
        [InlineData("guitarra", "gui|ta|rra")]
        [InlineData("vergüenza", "ver|güen|za")]
        [InlineData("alrededor", "al|re|de|dor")]
        [InlineData("israel", "is|ra|el")]
        [InlineData("ciudad", "ciu|dad")]
        [InlineData("caer", "ca|er")]
        [InlineData("xilófono", "xi|ló|fo|no")]
        [InlineData("país", "pa|ís")]
        [InlineData("baúl", "ba|úl")]
        public void Syllabify_ReturnsExpectedSplit(string word, string expected)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expected, string.Join("|", syllables.Select(s => s.Text)));
        }
    }
}
