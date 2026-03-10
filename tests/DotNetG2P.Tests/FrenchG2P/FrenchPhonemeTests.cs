using DotNetG2P.French;
using DotNetG2P.French.Conversion;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class FrenchPhonemeTests
    {
        // ========== FrenchIpaPhoneme enum値の確認 ==========

        [Fact]
        public void FrenchIpaPhoneme_A_Is0()
        {
            Assert.Equal(0, (byte)FrenchIpaPhoneme.A);
        }

        [Fact]
        public void FrenchIpaPhoneme_OeNasal_Is15()
        {
            Assert.Equal(15, (byte)FrenchIpaPhoneme.OeNasal);
        }

        [Fact]
        public void FrenchIpaPhoneme_R_Is35()
        {
            Assert.Equal(35, (byte)FrenchIpaPhoneme.R);
        }

        [Fact]
        public void FrenchIpaPhoneme_Count_Is40()
        {
            Assert.Equal(39, (byte)FrenchIpaPhoneme.Dz);
        }

        // ========== FrenchPhoneme.IsVowel ==========

        [Theory]
        [InlineData(FrenchIpaPhoneme.A)]
        [InlineData(FrenchIpaPhoneme.E)]
        [InlineData(FrenchIpaPhoneme.I)]
        [InlineData(FrenchIpaPhoneme.O)]
        [InlineData(FrenchIpaPhoneme.Schwa)]
        public void IsVowel_OralVowels_ReturnsTrue(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.True(p.IsVowel);
        }

        [Theory]
        [InlineData(FrenchIpaPhoneme.ANasal)]
        [InlineData(FrenchIpaPhoneme.ONasal)]
        [InlineData(FrenchIpaPhoneme.ENasal)]
        [InlineData(FrenchIpaPhoneme.OeNasal)]
        public void IsVowel_NasalVowels_ReturnsTrue(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.True(p.IsVowel);
        }

        [Theory]
        [InlineData(FrenchIpaPhoneme.P)]
        [InlineData(FrenchIpaPhoneme.S)]
        [InlineData(FrenchIpaPhoneme.R)]
        public void IsVowel_Consonants_ReturnsFalse(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.False(p.IsVowel);
        }

        // ========== FrenchPhoneme.IsNasalVowel ==========

        [Theory]
        [InlineData(FrenchIpaPhoneme.ANasal)]
        [InlineData(FrenchIpaPhoneme.ONasal)]
        [InlineData(FrenchIpaPhoneme.ENasal)]
        [InlineData(FrenchIpaPhoneme.OeNasal)]
        public void IsNasalVowel_NasalVowels_ReturnsTrue(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.True(p.IsNasalVowel);
        }

        [Theory]
        [InlineData(FrenchIpaPhoneme.A)]
        [InlineData(FrenchIpaPhoneme.E)]
        public void IsNasalVowel_OralVowels_ReturnsFalse(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.False(p.IsNasalVowel);
        }

        // ========== FrenchPhoneme.IsSemivowel ==========

        [Theory]
        [InlineData(FrenchIpaPhoneme.J)]
        [InlineData(FrenchIpaPhoneme.W)]
        [InlineData(FrenchIpaPhoneme.Uj)]
        public void IsSemivowel_JWUj_ReturnsTrue(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.True(p.IsSemivowel);
        }

        [Theory]
        [InlineData(FrenchIpaPhoneme.A)]
        [InlineData(FrenchIpaPhoneme.I)]
        public void IsSemivowel_Vowels_ReturnsFalse(FrenchIpaPhoneme phoneme)
        {
            var p = new FrenchPhoneme(phoneme);
            Assert.False(p.IsSemivowel);
        }

        // ========== FrenchPhoneme equality ==========

        [Fact]
        public void Equals_SamePhoneme_ReturnsTrue()
        {
            var a = new FrenchPhoneme(FrenchIpaPhoneme.B, isSyllableNucleus: false);
            var b = new FrenchPhoneme(FrenchIpaPhoneme.B, isSyllableNucleus: false);
            Assert.Equal(a, b);
            Assert.True(a == b);
        }

        [Fact]
        public void Equals_DifferentPhoneme_ReturnsFalse()
        {
            var a = new FrenchPhoneme(FrenchIpaPhoneme.B);
            var b = new FrenchPhoneme(FrenchIpaPhoneme.D);
            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }

        // ========== FrenchDialect enum ==========

        [Fact]
        public void FrenchDialect_Metropolitan_Is0()
        {
            Assert.Equal(0, (byte)FrenchDialect.Metropolitan);
        }

        [Fact]
        public void FrenchDialect_Conservative_Is1()
        {
            Assert.Equal(1, (byte)FrenchDialect.Conservative);
        }
    }
}
