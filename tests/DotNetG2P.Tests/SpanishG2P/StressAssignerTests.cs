using DotNetG2P.Spanish.Rules;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class StressAssignerTests
    {
        [Theory]
        [InlineData("casa", 0)]
        [InlineData("camino", 1)]
        [InlineData("acción", 1)]
        [InlineData("guitarra", 1)]
        [InlineData("vergüenza", 1)]
        [InlineData("reloj", 1)]
        [InlineData("árbol", 0)]
        [InlineData("caer", 1)]
        [InlineData("ciudad", 1)]
        public void GetStressedSyllableIndex_ReturnsExpectedIndex(string word, int expected)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }
    }
}
