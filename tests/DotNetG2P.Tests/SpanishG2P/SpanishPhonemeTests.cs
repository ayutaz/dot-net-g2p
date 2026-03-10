using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishPhonemeTests
    {
        [Fact]
        public void Vowel_HasSyllabicVowelFlag()
        {
            var phoneme = new SpanishPhoneme(SpanishIpaPhoneme.A, isStressed: true);

            Assert.True(phoneme.IsSyllabicVowel);
            Assert.False(phoneme.IsSemivowel);
            Assert.True(phoneme.IsStressed);
            Assert.Equal("a", phoneme.ToString());
        }

        [Fact]
        public void Semivowel_HasSemivowelFlag()
        {
            var phoneme = new SpanishPhoneme(SpanishIpaPhoneme.W);

            Assert.False(phoneme.IsSyllabicVowel);
            Assert.True(phoneme.IsSemivowel);
            Assert.Equal("w", phoneme.ToString());
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            var left = new SpanishPhoneme(SpanishIpaPhoneme.R);
            var right = new SpanishPhoneme(SpanishIpaPhoneme.R);

            Assert.Equal(left, right);
            Assert.True(left == right);
        }

        [Fact]
        public void Equality_DifferentStress_IsNotEqual()
        {
            var unstressed = new SpanishPhoneme(SpanishIpaPhoneme.E, isStressed: false);
            var stressed = new SpanishPhoneme(SpanishIpaPhoneme.E, isStressed: true);

            Assert.NotEqual(unstressed, stressed);
            Assert.True(unstressed != stressed);
        }
    }
}
