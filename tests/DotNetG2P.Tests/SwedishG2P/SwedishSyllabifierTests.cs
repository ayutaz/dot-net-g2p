using DotNetG2P.Swedish.Rules;
using Xunit;
using System.Linq;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishSyllabifierTests
    {
        [Theory]
        [InlineData("hus", "hus")]
        [InlineData("huset", "hu|set")]
        [InlineData("flicka", "flic|ka")]
        [InlineData("arbete", "ar|be|te")]
        [InlineData("", "")]
        public void Syllabify_ReturnsExpectedSyllables(string word, string expectedSplit)
        {
            var syllables = SwedishSyllabifier.Syllabify(word.ToLowerInvariant());
            var actual = string.Join("|", syllables.Select(s => s.Text));
            Assert.Equal(expectedSplit, actual);
        }

        [Fact]
        public void Syllabify_Null_ReturnsEmpty()
        {
            var result = SwedishSyllabifier.Syllabify(null);
            Assert.Empty(result);
        }

        [Fact]
        public void Syllabify_SingleVowel_ReturnsSingleSyllable()
        {
            var result = SwedishSyllabifier.Syllabify("a");
            Assert.Single(result);
        }

        [Fact]
        public void Syllabify_ConsonantsOnly_HandlesGracefully()
        {
            var result = SwedishSyllabifier.Syllabify("str");
            // 母音なしの子音列 → 適切に処理されること（例外を投げない）
            Assert.NotNull(result);
        }

        [Fact]
        public void Syllabify_ThreeConsonantOnset_SplitsCorrectly()
        {
            // "springa" → 少なくとも2音節
            var result = SwedishSyllabifier.Syllabify("springa");
            Assert.True(result.Count >= 2);
        }

        [Fact]
        public void Syllabify_SyllablesCoverEntireWord()
        {
            var word = "stockholm";
            var syllables = SwedishSyllabifier.Syllabify(word);
            var joined = string.Concat(syllables.Select(s => s.Text));
            Assert.Equal(word, joined);
        }
    }
}
