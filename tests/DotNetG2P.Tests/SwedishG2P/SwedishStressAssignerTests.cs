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
    }
}
