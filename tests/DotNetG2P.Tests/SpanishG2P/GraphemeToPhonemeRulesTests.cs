using DotNetG2P.Spanish;
using DotNetG2P.Spanish.Rules;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class GraphemeToPhonemeRulesTests
    {
        [Theory]
        [InlineData("casa", "k a s a")]
        [InlineData("camino", "k a m i n o")]
        [InlineData("cielo", "s j e l o")]
        [InlineData("guerra", "ɡ e r a")]
        [InlineData("queso", "k e s o")]
        [InlineData("guitarra", "ɡ i t a r a")]
        [InlineData("chico", "tʃ i k o")]
        [InlineData("llama", "ʝ a m a")]
        [InlineData("hoy", "o j")]
        [InlineData("acción", "a k s j o n")]
        [InlineData("ciudad", "s j u d a d")]
        [InlineData("pingüino", "p i n ɡ w i n o")]
        [InlineData("vergüenza", "b e ɾ ɡ w e n s a")]
        [InlineData("zapato", "s a p a t o")]
        [InlineData("naranja", "n a ɾ a n x a")]
        [InlineData("xilófono", "s i l o f o n o")]
        public void ConvertWord_LatinAmerican_ReturnsExpectedSequence(string word, string expected)
        {
            var phonemes = GraphemeToPhonemeRules.ConvertWordToPhonemes(word, SpanishDialect.LatinAmerican);

            Assert.Equal(expected, string.Join(" ", phonemes.Select(p => p.ToString())));
        }

        [Theory]
        [InlineData("zapato", "θ a p a t o")]
        [InlineData("cielo", "θ j e l o")]
        [InlineData("acción", "a k θ j o n")]
        [InlineData("vergüenza", "b e ɾ ɡ w e n θ a")]
        public void ConvertWord_Castilian_UsesDistincion(string word, string expected)
        {
            var phonemes = GraphemeToPhonemeRules.ConvertWordToPhonemes(word, SpanishDialect.Castilian);

            Assert.Equal(expected, string.Join(" ", phonemes.Select(p => p.ToString())));
        }

        [Theory]
        [InlineData("guion", "ɡ i o n")]
        [InlineData("truhan", "t ɾ u a n")]
        [InlineData("show", "ʃ o w")]
        [InlineData("méxico", "m e x i k o")]
        public void ConvertWord_ExceptionEntries_ReturnExpectedSequence(string word, string expected)
        {
            var phonemes = GraphemeToPhonemeRules.ConvertWordToPhonemes(word, SpanishDialect.LatinAmerican);

            Assert.Equal(expected, string.Join(" ", phonemes.Select(p => p.ToString())));
        }
    }
}
