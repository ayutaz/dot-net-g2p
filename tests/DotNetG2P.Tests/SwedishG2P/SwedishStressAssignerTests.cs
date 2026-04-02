using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Rules;
using Xunit;
using System.Linq;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishStressAssignerTests
    {
        [Fact]
        public void MarkStress_SingleSyllable_StressOnFirst()
        {
            var syllables = SwedishSyllabifier.Syllabify("hus");
            var result = StressAssigner.MarkStress("hus", syllables);
            Assert.Single(result);
            Assert.True(result[0].IsStressed);
        }

        [Fact]
        public void MarkStress_TwoSyllable_DefaultFirstSyllable()
        {
            var syllables = SwedishSyllabifier.Syllabify("flicka");
            var result = StressAssigner.MarkStress("flicka", syllables);
            Assert.True(result[0].IsStressed);
            Assert.False(result[result.Count - 1].IsStressed);
        }

        [Fact]
        public void MarkStress_ForeignSuffix_tion_LastSyllable()
        {
            var syllables = SwedishSyllabifier.Syllabify("station");
            var result = StressAssigner.MarkStress("station", syllables);
            Assert.True(result[result.Count - 1].IsStressed);
        }

        [Fact]
        public void MarkStress_EmptyInput_ReturnsEmpty()
        {
            var syllables = SwedishSyllabifier.Syllabify("");
            var result = StressAssigner.MarkStress("", syllables);
            Assert.Empty(result);
        }

        [Fact]
        public void MarkStress_Era_PenultimateSyllable()
        {
            var syllables = SwedishSyllabifier.Syllabify("operera");
            var result = StressAssigner.MarkStress("operera", syllables);
            // -era → ストレスは最終音節の1つ前
            if (result.Count >= 3)
            {
                Assert.True(result[result.Count - 2].IsStressed);
            }
        }

        // =================================================================
        // ピッチアクセント予測テスト (+20件)
        // =================================================================

        [Theory]
        [InlineData("hej", 1)]          // 単音節→Accent 1
        [InlineData("bok", 1)]          // 単音節→Accent 1
        [InlineData("hundar", 2)]       // -ar複数形→Accent 2
        [InlineData("bilar", 2)]        // -ar複数形→Accent 2
        [InlineData("flickor", 2)]      // -or複数形→Accent 2
        [InlineData("k\u00f6pte", 2)]   // -te過去形→Accent 2
        [InlineData("ringde", 2)]       // -de過去形→Accent 2
        [InlineData("frihet", 2)]       // -het派生名詞→Accent 2
        [InlineData("l\u00e4rare", 2)]  // -are行為者→Accent 2
        [InlineData("arbetare", 2)]     // -are行為者→Accent 2
        [InlineData("springande", 2)]   // -ande現在分詞→Accent 2
        [InlineData("kommende", 2)]     // -ende現在分詞→Accent 2
        [InlineData("hunden", 1)]       // -en定冠詞→Accent 1
        [InlineData("springer", 1)]     // -er現在形→Accent 1
        [InlineData("station", 1)]      // 外来語→Accent 1
        [InlineData("telefon", 1)]      // 外来語→Accent 1
        [InlineData("pojke", 2)]        // 語幹末尾e→Accent 2
        public void AssignAccent_ReturnsExpected(string word, byte expected)
        {
            var lower = word.ToLowerInvariant();
            var syllables = StressAssigner.MarkStress(lower, SwedishSyllabifier.Syllabify(lower));
            var accent = StressAssigner.AssignAccent(lower, syllables, 0);
            Assert.Equal(expected, accent);
        }

        [Theory]
        [InlineData("test", (byte)1)]
        [InlineData("test", (byte)2)]
        public void AssignAccent_DictionaryOverride_ReturnsGivenAccent(string word, byte dictAccent)
        {
            var lower = word.ToLowerInvariant();
            var syllables = StressAssigner.MarkStress(lower, SwedishSyllabifier.Syllabify(lower));
            var accent = StressAssigner.AssignAccent(lower, syllables, dictAccent);
            Assert.Equal(dictAccent, accent);
        }

        [Fact]
        public void AssignAccent_EmptySyllables_ReturnsAccent1()
        {
            var syllables = SwedishSyllabifier.Syllabify("");
            var result = StressAssigner.MarkStress("", syllables);
            var accent = StressAssigner.AssignAccent("", result, 0);
            Assert.Equal((byte)1, accent);
        }
    }
}
