using DotNetG2P.Portuguese;

namespace DotNetG2P.Tests.PortugueseG2P
{
    public class PortuguesePhonemeTests
    {
        // ========== PortugueseIpaPhoneme enum値の確認 ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.A, 0)]
        [InlineData(PortugueseIpaPhoneme.E, 1)]
        [InlineData(PortugueseIpaPhoneme.Eh, 2)]
        [InlineData(PortugueseIpaPhoneme.I, 3)]
        [InlineData(PortugueseIpaPhoneme.O, 4)]
        [InlineData(PortugueseIpaPhoneme.Oh, 5)]
        [InlineData(PortugueseIpaPhoneme.U, 6)]
        [InlineData(PortugueseIpaPhoneme.Schwa, 7)]
        [InlineData(PortugueseIpaPhoneme.HighCentral, 8)]
        [InlineData(PortugueseIpaPhoneme.ANasal, 9)]
        [InlineData(PortugueseIpaPhoneme.ENasal, 10)]
        [InlineData(PortugueseIpaPhoneme.INasal, 11)]
        [InlineData(PortugueseIpaPhoneme.ONasal, 12)]
        [InlineData(PortugueseIpaPhoneme.UNasal, 13)]
        [InlineData(PortugueseIpaPhoneme.J, 14)]
        [InlineData(PortugueseIpaPhoneme.W, 15)]
        [InlineData(PortugueseIpaPhoneme.P, 16)]
        [InlineData(PortugueseIpaPhoneme.B, 17)]
        [InlineData(PortugueseIpaPhoneme.T, 18)]
        [InlineData(PortugueseIpaPhoneme.D, 19)]
        [InlineData(PortugueseIpaPhoneme.K, 20)]
        [InlineData(PortugueseIpaPhoneme.G, 21)]
        [InlineData(PortugueseIpaPhoneme.F, 22)]
        [InlineData(PortugueseIpaPhoneme.V, 23)]
        [InlineData(PortugueseIpaPhoneme.S, 24)]
        [InlineData(PortugueseIpaPhoneme.Z, 25)]
        [InlineData(PortugueseIpaPhoneme.Sh, 26)]
        [InlineData(PortugueseIpaPhoneme.Zh, 27)]
        [InlineData(PortugueseIpaPhoneme.M, 28)]
        [InlineData(PortugueseIpaPhoneme.N, 29)]
        [InlineData(PortugueseIpaPhoneme.Ny, 30)]
        [InlineData(PortugueseIpaPhoneme.L, 31)]
        [InlineData(PortugueseIpaPhoneme.Lh, 32)]
        [InlineData(PortugueseIpaPhoneme.R, 33)]
        [InlineData(PortugueseIpaPhoneme.Rr, 34)]
        [InlineData(PortugueseIpaPhoneme.Ch, 35)]
        [InlineData(PortugueseIpaPhoneme.Jh, 36)]
        [InlineData(PortugueseIpaPhoneme.X, 37)]
        [InlineData(PortugueseIpaPhoneme.H, 38)]
        [InlineData(PortugueseIpaPhoneme.DarkL, 39)]
        [InlineData(PortugueseIpaPhoneme.Xh, 40)]
        [InlineData(PortugueseIpaPhoneme.Ng, 41)]
        [InlineData(PortugueseIpaPhoneme.NLabiodental, 42)]
        [InlineData(PortugueseIpaPhoneme.NDental, 43)]
        [InlineData(PortugueseIpaPhoneme.Beta, 44)]
        [InlineData(PortugueseIpaPhoneme.Dh, 45)]
        [InlineData(PortugueseIpaPhoneme.Gh, 46)]
        [InlineData(PortugueseIpaPhoneme.WNasal, 47)]
        [InlineData(PortugueseIpaPhoneme.JNasal, 48)]
        public void PortugueseIpaPhoneme_HasExpectedValue(PortugueseIpaPhoneme phoneme, byte expected)
        {
            Assert.Equal(expected, (byte)phoneme);
        }

        [Fact]
        public void PortugueseIpaPhoneme_Has49Values()
        {
            var values = (PortugueseIpaPhoneme[])Enum.GetValues(typeof(PortugueseIpaPhoneme));
            Assert.Equal(49, values.Length);
        }

        [Fact]
        public void PortugueseIpaPhoneme_AllValuesAreUnique()
        {
            var values = (PortugueseIpaPhoneme[])Enum.GetValues(typeof(PortugueseIpaPhoneme));
            var byteValues = values.Select(v => (byte)v).ToHashSet();
            Assert.Equal(values.Length, byteValues.Count);
        }

        // ========== PortugueseDialect enum ==========

        [Fact]
        public void PortugueseDialect_Brazilian_Is0()
        {
            Assert.Equal(0, (byte)PortugueseDialect.Brazilian);
        }

        [Fact]
        public void PortugueseDialect_European_Is1()
        {
            Assert.Equal(1, (byte)PortugueseDialect.European);
        }

        [Fact]
        public void PortugueseDialect_Default_IsBrazilian()
        {
            Assert.Equal(PortugueseDialect.Brazilian, default(PortugueseDialect));
        }

        // ========== PortuguesePhoneme コンストラクタ ==========

        [Fact]
        public void Constructor_DefaultIsStressed_IsFalse()
        {
            var p = new PortuguesePhoneme(PortugueseIpaPhoneme.A);
            Assert.Equal(PortugueseIpaPhoneme.A, p.Phoneme);
            Assert.False(p.IsStressed);
        }

        [Fact]
        public void Constructor_WithStress_SetsIsStressed()
        {
            var p = new PortuguesePhoneme(PortugueseIpaPhoneme.E, isStressed: true);
            Assert.Equal(PortugueseIpaPhoneme.E, p.Phoneme);
            Assert.True(p.IsStressed);
        }

        // ========== PortuguesePhoneme.IsSyllabicVowel ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.A)]
        [InlineData(PortugueseIpaPhoneme.E)]
        [InlineData(PortugueseIpaPhoneme.Eh)]
        [InlineData(PortugueseIpaPhoneme.I)]
        [InlineData(PortugueseIpaPhoneme.O)]
        [InlineData(PortugueseIpaPhoneme.Oh)]
        [InlineData(PortugueseIpaPhoneme.U)]
        [InlineData(PortugueseIpaPhoneme.Schwa)]
        [InlineData(PortugueseIpaPhoneme.HighCentral)]
        public void IsSyllabicVowel_OralVowels_ReturnsTrue(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.True(p.IsSyllabicVowel);
        }

        [Theory]
        [InlineData(PortugueseIpaPhoneme.ANasal)]
        [InlineData(PortugueseIpaPhoneme.ENasal)]
        [InlineData(PortugueseIpaPhoneme.INasal)]
        [InlineData(PortugueseIpaPhoneme.ONasal)]
        [InlineData(PortugueseIpaPhoneme.UNasal)]
        public void IsSyllabicVowel_NasalVowels_ReturnsTrue(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.True(p.IsSyllabicVowel);
        }

        [Theory]
        [InlineData(PortugueseIpaPhoneme.P)]
        [InlineData(PortugueseIpaPhoneme.S)]
        [InlineData(PortugueseIpaPhoneme.R)]
        [InlineData(PortugueseIpaPhoneme.J)]
        [InlineData(PortugueseIpaPhoneme.W)]
        [InlineData(PortugueseIpaPhoneme.Ch)]
        [InlineData(PortugueseIpaPhoneme.Beta)]
        [InlineData(PortugueseIpaPhoneme.WNasal)]
        public void IsSyllabicVowel_ConsonantsAndSemivowels_ReturnsFalse(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.False(p.IsSyllabicVowel);
        }

        // ========== PortuguesePhoneme.IsNasalVowel ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.ANasal)]
        [InlineData(PortugueseIpaPhoneme.ENasal)]
        [InlineData(PortugueseIpaPhoneme.INasal)]
        [InlineData(PortugueseIpaPhoneme.ONasal)]
        [InlineData(PortugueseIpaPhoneme.UNasal)]
        public void IsNasalVowel_NasalVowels_ReturnsTrue(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.True(p.IsNasalVowel);
        }

        [Theory]
        [InlineData(PortugueseIpaPhoneme.A)]
        [InlineData(PortugueseIpaPhoneme.E)]
        [InlineData(PortugueseIpaPhoneme.I)]
        [InlineData(PortugueseIpaPhoneme.Schwa)]
        [InlineData(PortugueseIpaPhoneme.HighCentral)]
        public void IsNasalVowel_OralVowels_ReturnsFalse(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.False(p.IsNasalVowel);
        }

        [Theory]
        [InlineData(PortugueseIpaPhoneme.N)]
        [InlineData(PortugueseIpaPhoneme.M)]
        [InlineData(PortugueseIpaPhoneme.Ng)]
        public void IsNasalVowel_NasalConsonants_ReturnsFalse(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.False(p.IsNasalVowel);
        }

        // ========== PortuguesePhoneme.IsSemivowel ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.J)]
        [InlineData(PortugueseIpaPhoneme.W)]
        public void IsSemivowel_JW_ReturnsTrue(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.True(p.IsSemivowel);
        }

        [Theory]
        [InlineData(PortugueseIpaPhoneme.A)]
        [InlineData(PortugueseIpaPhoneme.I)]
        [InlineData(PortugueseIpaPhoneme.WNasal)]
        [InlineData(PortugueseIpaPhoneme.JNasal)]
        [InlineData(PortugueseIpaPhoneme.P)]
        public void IsSemivowel_NonSemivowels_ReturnsFalse(PortugueseIpaPhoneme phoneme)
        {
            var p = new PortuguesePhoneme(phoneme);
            Assert.False(p.IsSemivowel);
        }

        // ========== PortuguesePhoneme Equals / GetHashCode / ==, != ==========

        [Fact]
        public void Equals_SamePhonemeAndStress_ReturnsTrue()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.B, isStressed: false);
            var b = new PortuguesePhoneme(PortugueseIpaPhoneme.B, isStressed: false);
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void Equals_DifferentPhoneme_ReturnsFalse()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.B);
            var b = new PortuguesePhoneme(PortugueseIpaPhoneme.D);
            Assert.NotEqual(a, b);
            Assert.True(a != b);
            Assert.False(a == b);
        }

        [Fact]
        public void Equals_SamePhoneme_DifferentStress_ReturnsFalse()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.A, isStressed: true);
            var b = new PortuguesePhoneme(PortugueseIpaPhoneme.A, isStressed: false);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Equals_ObjectOverload_ReturnsTrue()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.K);
            object b = new PortuguesePhoneme(PortugueseIpaPhoneme.K);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_ObjectOverload_Null_ReturnsFalse()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.K);
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void GetHashCode_SamePhoneme_SameHash()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.S, isStressed: true);
            var b = new PortuguesePhoneme(PortugueseIpaPhoneme.S, isStressed: true);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentPhoneme_DifferentHash()
        {
            var a = new PortuguesePhoneme(PortugueseIpaPhoneme.S);
            var b = new PortuguesePhoneme(PortugueseIpaPhoneme.Z);
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        // ========== PortuguesePhoneme.ToString ==========

        [Fact]
        public void ToString_ReturnsPhonemeEnumName()
        {
            var p = new PortuguesePhoneme(PortugueseIpaPhoneme.Rr);
            Assert.Equal("Rr", p.ToString());
        }

        // ========== PortuguesePronunciation ==========

        [Fact]
        public void PortuguesePronunciation_Phonemes_ReturnsCorrectList()
        {
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Z),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Schwa),
            };
            var offsets = new[] { 0, 2 };
            var pron = new PortuguesePronunciation(phonemes, offsets, stressedSyllableIndex: 0);

            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.K, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.Schwa, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void PortuguesePronunciation_StressedSyllableIndex_ReturnsCorrectValue()
        {
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A, isStressed: true),
            };
            var offsets = new[] { 0 };
            var pron = new PortuguesePronunciation(phonemes, offsets, stressedSyllableIndex: 0);

            Assert.Equal(0, pron.StressedSyllableIndex);
        }

        [Fact]
        public void PortuguesePronunciation_ToString_ReturnsSpaceSeparated()
        {
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.B),
                new PortuguesePhoneme(PortugueseIpaPhoneme.O, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.M),
            };
            var offsets = new[] { 0 };
            var pron = new PortuguesePronunciation(phonemes, offsets, stressedSyllableIndex: 0);

            Assert.Equal("B O M", pron.ToString());
        }

        [Fact]
        public void PortuguesePronunciation_EmptyPhonemes_ToString_ReturnsEmpty()
        {
            var pron = new PortuguesePronunciation(
                Array.Empty<PortuguesePhoneme>(),
                Array.Empty<int>(),
                stressedSyllableIndex: -1);

            Assert.Equal(string.Empty, pron.ToString());
        }

        [Fact]
        public void PortuguesePronunciation_NullPhonemes_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PortuguesePronunciation(null!, new[] { 0 }, stressedSyllableIndex: 0));
        }

        [Fact]
        public void PortuguesePronunciation_NullSyllableOffsets_Throws()
        {
            var phonemes = new[] { new PortuguesePhoneme(PortugueseIpaPhoneme.A) };
            Assert.Throws<ArgumentNullException>(() =>
                new PortuguesePronunciation(phonemes, null!, stressedSyllableIndex: 0));
        }

        // ========== PortugueseSyllable ==========

        [Fact]
        public void PortugueseSyllable_Constructor_SetsProperties()
        {
            var syl = new PortugueseSyllable(startIndex: 0, length: 2, text: "ca", isStressed: true);
            Assert.Equal(0, syl.StartIndex);
            Assert.Equal(2, syl.Length);
            Assert.Equal("ca", syl.Text);
            Assert.True(syl.IsStressed);
        }

        [Fact]
        public void PortugueseSyllable_DefaultIsStressed_IsFalse()
        {
            var syl = new PortugueseSyllable(startIndex: 2, length: 2, text: "sa");
            Assert.False(syl.IsStressed);
        }

        [Fact]
        public void PortugueseSyllable_NullText_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PortugueseSyllable(0, 1, null!));
        }

        [Fact]
        public void PortugueseSyllable_ToString_ReturnsText()
        {
            var syl = new PortugueseSyllable(0, 3, "por");
            Assert.Equal("por", syl.ToString());
        }

        [Fact]
        public void PortugueseSyllable_Equals_SameValues_ReturnsTrue()
        {
            var a = new PortugueseSyllable(0, 2, "ca", true);
            var b = new PortugueseSyllable(0, 2, "ca", true);
            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void PortugueseSyllable_Equals_DifferentText_ReturnsFalse()
        {
            var a = new PortugueseSyllable(0, 2, "ca");
            var b = new PortugueseSyllable(0, 2, "sa");
            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }

        [Fact]
        public void PortugueseSyllable_Equals_DifferentStress_ReturnsFalse()
        {
            var a = new PortugueseSyllable(0, 2, "ca", true);
            var b = new PortugueseSyllable(0, 2, "ca", false);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void PortugueseSyllable_Equals_ObjectOverload_ReturnsTrue()
        {
            var a = new PortugueseSyllable(0, 2, "ca");
            object b = new PortugueseSyllable(0, 2, "ca");
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void PortugueseSyllable_Equals_ObjectOverload_Null_ReturnsFalse()
        {
            var a = new PortugueseSyllable(0, 2, "ca");
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void PortugueseSyllable_GetHashCode_SameValues_SameHash()
        {
            var a = new PortugueseSyllable(0, 2, "ca", true);
            var b = new PortugueseSyllable(0, 2, "ca", true);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void PortugueseSyllable_GetHashCode_DifferentValues_DifferentHash()
        {
            var a = new PortugueseSyllable(0, 2, "ca");
            var b = new PortugueseSyllable(0, 2, "sa");
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
