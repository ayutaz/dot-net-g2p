using DotNetG2P.Spanish;
using DotNetG2P.Spanish.Data;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishExceptionDictionaryTests
    {
        [Theory]
        [InlineData("méxico")]
        [InlineData("mexico")]
        [InlineData("whisky")]
        [InlineData("wifi")]
        [InlineData("google")]
        public void TryLookup_KnownExceptionWord_ReturnsTrue(string word)
        {
            Assert.True(SpanishExceptionDictionary.TryLookup(word, SpanishDialect.LatinAmerican, out var pronunciation));
            Assert.NotNull(pronunciation);
            Assert.True(pronunciation.Phonemes.Count > 0);
        }

        [Theory]
        [InlineData("casa")]
        [InlineData("perro")]
        [InlineData("hola")]
        [InlineData("zzzzz")]
        public void TryLookup_UnknownWord_ReturnsFalse(string word)
        {
            Assert.False(SpanishExceptionDictionary.TryLookup(word, SpanishDialect.LatinAmerican, out _));
        }

        [Fact]
        public void TryLookup_NullWord_ReturnsFalse()
        {
            Assert.False(SpanishExceptionDictionary.TryLookup(null!, SpanishDialect.LatinAmerican, out _));
        }

        [Theory]
        [InlineData("méxico")]
        [InlineData("wifi")]
        [InlineData("show")]
        public void TryLookup_BothDialects_ReturnSameResult(string word)
        {
            // All entries in master.tsv use '*' dialect, so both should return the same pronunciation
            Assert.True(SpanishExceptionDictionary.TryLookup(word, SpanishDialect.LatinAmerican, out var laPron));
            Assert.True(SpanishExceptionDictionary.TryLookup(word, SpanishDialect.Castilian, out var caPron));

            Assert.Equal(laPron.Phonemes.Count, caPron.Phonemes.Count);
            Assert.Equal(laPron.StressedSyllableIndex, caPron.StressedSyllableIndex);
        }

        [Fact]
        public void TryLookup_México_HasCorrectStress()
        {
            Assert.True(SpanishExceptionDictionary.TryLookup("méxico", SpanishDialect.LatinAmerican, out var pronunciation));
            // "méxico" has stress on syllable 0 (mé)
            Assert.Equal(0, pronunciation.StressedSyllableIndex);
        }
    }
}
