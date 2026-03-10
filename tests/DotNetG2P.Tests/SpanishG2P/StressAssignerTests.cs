using System;
using System.Linq;
using DotNetG2P.Spanish;
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

        [Theory]
        [InlineData("teléfono", 1)]   // esdrújula: te|lé|fo|no → 音節1
        [InlineData("médico", 0)]     // esdrújula: mé|di|co → 音節0
        [InlineData("música", 0)]     // esdrújula: mú|si|ca → 音節0
        public void GetStressedSyllableIndex_Esdrujula_StressOnAntepenultimate(string word, int expected)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Fact]
        public void GetStressedSyllableIndex_Sobreesdrujula_Digamelo()
        {
            // sobreesdrújula: dí|ga|me|lo → 音節0
            var syllables = SpanishSyllabifier.Syllabify("dígamelo");

            Assert.Equal(0, StressAssigner.GetStressedSyllableIndex("dígamelo", syllables));
        }

        [Theory]
        [InlineData("sol", 0)]   // 単音節: sol → 音節0
        [InlineData("más", 0)]   // 単音節（アクセント記号付き）: más → 音節0
        public void GetStressedSyllableIndex_Monosyllable_ReturnsZero(string word, int expected)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Theory]
        [InlineData("convoy")]
        [InlineData("Paraguay")]
        public void GetStressedSyllableIndex_YEnding_StressOnLastSyllable(string word)
        {
            // y末尾語は子音終わりとして最終音節にストレス
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(syllables.Count - 1, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Theory]
        [InlineData("café")]     // アクセント記号付き最終音節
        [InlineData("corazón")]  // アクセント記号付き最終音節
        public void GetStressedSyllableIndex_AccentOnLastSyllable(string word)
        {
            var syllables = SpanishSyllabifier.Syllabify(word);

            Assert.Equal(syllables.Count - 1, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Fact]
        public void GetStressedSyllableIndex_EmptySyllables_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StressAssigner.GetStressedSyllableIndex("", Array.Empty<SpanishSyllable>()));
        }

        [Fact]
        public void GetStressedSyllableIndex_NullWord_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StressAssigner.GetStressedSyllableIndex(null!, Array.Empty<SpanishSyllable>()));
        }

        [Fact]
        public void MarkStress_EmptySyllables_ReturnsEmpty()
        {
            var result = StressAssigner.MarkStress("", Array.Empty<SpanishSyllable>());

            Assert.Empty(result);
        }

        [Fact]
        public void MarkStress_SetsCorrectSyllableAsStressed()
        {
            // casa → ca|sa → 音節0にストレス
            var word = "casa";
            var syllables = SpanishSyllabifier.Syllabify(word);

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(2, result.Count);
            Assert.True(result[0].IsStressed);
            Assert.False(result[1].IsStressed);
        }

        [Fact]
        public void MarkStress_PreservesTextAndPositionInfo()
        {
            var word = "camino";
            var syllables = SpanishSyllabifier.Syllabify(word);

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(syllables.Count, result.Count);
            for (var i = 0; i < syllables.Count; i++)
            {
                Assert.Equal(syllables[i].Text, result[i].Text);
                Assert.Equal(syllables[i].StartIndex, result[i].StartIndex);
                Assert.Equal(syllables[i].Length, result[i].Length);
            }
        }

        [Fact]
        public void MarkStress_Esdrujula_StressOnCorrectSyllable()
        {
            // teléfono → te|lé|fo|no → 音節1にストレス
            var word = "teléfono";
            var syllables = SpanishSyllabifier.Syllabify(word);

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.True(result[1].IsStressed);
            Assert.False(result[0].IsStressed);
            Assert.False(result[2].IsStressed);
            Assert.False(result[3].IsStressed);
        }

        [Fact]
        public void MarkStress_Monosyllable_SingleSyllableIsStressed()
        {
            var word = "sol";
            var syllables = SpanishSyllabifier.Syllabify(word);

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Single(result);
            Assert.True(result[0].IsStressed);
        }
    }
}
