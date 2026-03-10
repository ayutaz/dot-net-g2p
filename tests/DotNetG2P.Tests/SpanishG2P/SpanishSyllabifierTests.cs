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

        [Theory]
        [InlineData("caos", 2)]     // 連続強母音(hiatus): ca|os
        [InlineData("poeta", 3)]    // 連続強母音(hiatus): po|e|ta
        public void Syllabify_Hiatus_SplitsStrongVowels(string word, int expectedCount)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expectedCount, syllables.Count);
        }

        [Theory]
        [InlineData("precio", 2)]   // 語頭子音クラスタ: pre|cio
        [InlineData("blanco", 2)]   // 語頭子音クラスタ: blan|co
        public void Syllabify_InitialCluster_CorrectSyllableCount(string word, int expectedCount)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expectedCount, syllables.Count);
        }

        [Theory]
        [InlineData("construir")]
        [InlineData("instrumento")]
        [InlineData("estructura")]
        public void Syllabify_ThreeConsonantCluster_HasMultipleSyllables(string word)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.True(syllables.Count >= 2, $"Expected at least 2 syllables, got {syllables.Count}: {string.Join("|", syllables.Select(s => s.Text))}");
            // 音節を結合すると元の単語と一致すること
            Assert.Equal(word, string.Concat(syllables.Select(s => s.Text)));
        }

        [Fact]
        public void Syllabify_Triphthong_Buey_SingleSyllable()
        {
            // buey は三重母音で1音節
            var syllables = SpanishSyllabifier.Syllabify("buey");

            Assert.Single(syllables);
        }

        [Fact]
        public void Syllabify_Paraguay_HasMultipleSyllables()
        {
            var syllables = SpanishSyllabifier.Syllabify("Paraguay");

            Assert.True(syllables.Count >= 3, $"Expected at least 3 syllables, got {syllables.Count}: {string.Join("|", syllables.Select(s => s.Text))}");
            Assert.Equal("Paraguay", string.Concat(syllables.Select(s => s.Text)));
        }

        [Fact]
        public void Syllabify_SingleVowel_ReturnsSingleSyllable()
        {
            var syllables = SpanishSyllabifier.Syllabify("a");

            Assert.Single(syllables);
            Assert.Equal("a", syllables[0].Text);
        }

        [Fact]
        public void Syllabify_Null_ReturnsEmpty()
        {
            var syllables = SpanishSyllabifier.Syllabify(null!);

            Assert.Empty(syllables);
        }

        [Fact]
        public void Syllabify_EmptyString_ReturnsEmpty()
        {
            var syllables = SpanishSyllabifier.Syllabify("");

            Assert.Empty(syllables);
        }

        [Fact]
        public void Syllabify_StartIndexAndLengthAreCorrect()
        {
            // 各音節の StartIndex + Length が元の単語と一致すること
            var word = "camino";
            var syllables = SpanishSyllabifier.Syllabify(word);

            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                Assert.Equal(syllable.Text, word.Substring(syllable.StartIndex, syllable.Length));
            }
        }

        [Fact]
        public void Syllabify_SyllablesCoverEntireWord()
        {
            var word = "alrededor";
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(word, string.Concat(syllables.Select(s => s.Text)));
        }
    }
}
