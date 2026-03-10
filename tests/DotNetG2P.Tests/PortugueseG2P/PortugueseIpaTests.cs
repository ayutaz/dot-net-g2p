using System;
using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Conversion;

namespace DotNetG2P.Tests.PortugueseG2P
{
    public class PortugueseIpaTests
    {
        // ========== ToSymbol: 口母音 (9件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.A, "a")]
        [InlineData(PortugueseIpaPhoneme.E, "e")]
        [InlineData(PortugueseIpaPhoneme.Eh, "\u025B")]           // ɛ
        [InlineData(PortugueseIpaPhoneme.I, "i")]
        [InlineData(PortugueseIpaPhoneme.O, "o")]
        [InlineData(PortugueseIpaPhoneme.Oh, "\u0254")]           // ɔ
        [InlineData(PortugueseIpaPhoneme.U, "u")]
        [InlineData(PortugueseIpaPhoneme.Schwa, "\u0250")]        // ɐ
        [InlineData(PortugueseIpaPhoneme.HighCentral, "\u0268")]  // ɨ
        public void ToSymbol_OralVowel_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 鼻母音 (5件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.ANasal, "\u0250\u0303")] // ɐ̃
        [InlineData(PortugueseIpaPhoneme.ENasal, "e\u0303")]      // ẽ
        [InlineData(PortugueseIpaPhoneme.INasal, "i\u0303")]      // ĩ
        [InlineData(PortugueseIpaPhoneme.ONasal, "\u00F5")]       // õ
        [InlineData(PortugueseIpaPhoneme.UNasal, "u\u0303")]      // ũ
        public void ToSymbol_NasalVowel_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 半母音 (2件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.J, "j")]
        [InlineData(PortugueseIpaPhoneme.W, "w")]
        public void ToSymbol_Semivowel_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 破裂音 (6件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.P, "p")]
        [InlineData(PortugueseIpaPhoneme.B, "b")]
        [InlineData(PortugueseIpaPhoneme.T, "t")]
        [InlineData(PortugueseIpaPhoneme.D, "d")]
        [InlineData(PortugueseIpaPhoneme.K, "k")]
        [InlineData(PortugueseIpaPhoneme.G, "\u0261")]            // ɡ (U+0261)
        public void ToSymbol_Plosive_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 摩擦音 (6件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.F, "f")]
        [InlineData(PortugueseIpaPhoneme.V, "v")]
        [InlineData(PortugueseIpaPhoneme.S, "s")]
        [InlineData(PortugueseIpaPhoneme.Z, "z")]
        [InlineData(PortugueseIpaPhoneme.Sh, "\u0283")]           // ʃ
        [InlineData(PortugueseIpaPhoneme.Zh, "\u0292")]           // ʒ
        public void ToSymbol_Fricative_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 鼻音 (3件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.M, "m")]
        [InlineData(PortugueseIpaPhoneme.N, "n")]
        [InlineData(PortugueseIpaPhoneme.Ny, "\u0272")]           // ɲ
        public void ToSymbol_Nasal_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 側面音 (2件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.L, "l")]
        [InlineData(PortugueseIpaPhoneme.Lh, "\u028E")]           // ʎ
        public void ToSymbol_Lateral_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: ロティック (2件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.R, "\u027E")]            // ɾ
        [InlineData(PortugueseIpaPhoneme.Rr, "\u0281")]           // ʁ
        public void ToSymbol_Rhotic_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: BP固有異音 (4件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.Ch, "t\u0361\u0283")]    // t͡ʃ
        [InlineData(PortugueseIpaPhoneme.Jh, "d\u0361\u0292")]    // d͡ʒ
        [InlineData(PortugueseIpaPhoneme.X, "x")]
        [InlineData(PortugueseIpaPhoneme.H, "h")]
        public void ToSymbol_BrazilianAllophone_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: EP固有異音 (2件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.DarkL, "\u026B")]        // ɫ
        [InlineData(PortugueseIpaPhoneme.Xh, "\u03C7")]           // χ
        public void ToSymbol_EuropeanAllophone_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 共通異音 (3件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.Ng, "\u014B")]           // ŋ
        [InlineData(PortugueseIpaPhoneme.NLabiodental, "\u0271")] // ɱ
        [InlineData(PortugueseIpaPhoneme.NDental, "n\u032A")]     // n̪
        public void ToSymbol_CommonAllophone_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 弱化異音 (3件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.Beta, "\u03B2")]         // β
        [InlineData(PortugueseIpaPhoneme.Dh, "\u00F0")]           // ð
        [InlineData(PortugueseIpaPhoneme.Gh, "\u0263")]           // ɣ
        public void ToSymbol_LenitionAllophone_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 鼻わたり音 (2件) ==========

        [Theory]
        [InlineData(PortugueseIpaPhoneme.WNasal, "w\u0303")]     // w̃
        [InlineData(PortugueseIpaPhoneme.JNasal, "j\u0303")]     // j̃
        public void ToSymbol_NasalGlide_ReturnsCorrectIPA(PortugueseIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, IpaConverter.ToSymbol(phoneme));
        }

        // ========== ToSymbol: 不正値 ==========

        [Fact]
        public void ToSymbol_InvalidPhoneme_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => IpaConverter.ToSymbol((PortugueseIpaPhoneme)255));
        }

        // ========== Convert テスト ==========

        [Fact]
        public void Convert_NullPronunciation_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => IpaConverter.Convert(null!, includeStress: false));
        }

        [Fact]
        public void Convert_EmptyPronunciation_ReturnsEmpty()
        {
            var pron = new PortuguesePronunciation(
                Array.Empty<PortuguesePhoneme>(),
                Array.Empty<int>(),
                stressedSyllableIndex: -1);

            Assert.Equal(string.Empty, IpaConverter.Convert(pron, includeStress: false));
        }

        [Fact]
        public void Convert_SingleSyllable_NoStress_ReturnsIPA()
        {
            // "sol" → /s ɔ l/ (1音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.S),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Oh, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.L),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0 }, stressedSyllableIndex: 0);

            var result = IpaConverter.Convert(pron, includeStress: false);
            Assert.Equal("s\u0254l", result); // sɔl
        }

        [Fact]
        public void Convert_SingleSyllable_WithStress_InsertsStressMark()
        {
            // "sol" → /ˈsɔl/ (1音節、ストレスあり)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.S),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Oh, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.L),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0 }, stressedSyllableIndex: 0);

            var result = IpaConverter.Convert(pron, includeStress: true);
            Assert.Equal("\u02C8s\u0254l", result); // ˈsɔl
        }

        [Fact]
        public void Convert_TwoSyllables_StressOnSecond()
        {
            // "café" → /ka.ˈfɛ/ (2音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A),
                new PortuguesePhoneme(PortugueseIpaPhoneme.F),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Eh, isStressed: true),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2 }, stressedSyllableIndex: 1);

            var result = IpaConverter.Convert(pron, includeStress: true);
            Assert.Equal("ka\u02C8f\u025B", result); // kaˈfɛ
        }

        [Fact]
        public void Convert_ThreeSyllables_StressOnSecond()
        {
            // "bonito" → /bo.ˈni.tu/ (3音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.B),
                new PortuguesePhoneme(PortugueseIpaPhoneme.O),
                new PortuguesePhoneme(PortugueseIpaPhoneme.N),
                new PortuguesePhoneme(PortugueseIpaPhoneme.I, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.T),
                new PortuguesePhoneme(PortugueseIpaPhoneme.U),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2, 4 }, stressedSyllableIndex: 1);

            var result = IpaConverter.Convert(pron, includeStress: true);
            Assert.Equal("bo\u02C8nitu", result); // boˈnitu
        }

        [Fact]
        public void Convert_WithNasalVowel_ReturnsCorrectIPA()
        {
            // "mão" → /m ɐ̃ w̃/ (1音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.M),
                new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.WNasal),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0 }, stressedSyllableIndex: 0);

            var result = IpaConverter.Convert(pron, includeStress: true);
            Assert.Equal("\u02C8m\u0250\u0303w\u0303", result); // ˈmɐ̃w̃
        }

        [Fact]
        public void Convert_WithAffricate_ReturnsCorrectIPA()
        {
            // BP "dia" → /d͡ʒi.a/ (2音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.Jh),
                new PortuguesePhoneme(PortugueseIpaPhoneme.I, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2 }, stressedSyllableIndex: 0);

            var result = IpaConverter.Convert(pron, includeStress: true);
            Assert.Equal("\u02C8d\u0361\u0292ia", result); // ˈd͡ʒia
        }

        // ========== ConvertPhonemeSequence テスト ==========

        [Fact]
        public void ConvertPhonemeSequence_NullPronunciation_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                IpaConverter.ConvertPhonemeSequence(null!, includeStress: false, separator: " "));
        }

        [Fact]
        public void ConvertPhonemeSequence_EmptyPronunciation_ReturnsEmpty()
        {
            var pron = new PortuguesePronunciation(
                Array.Empty<PortuguesePhoneme>(),
                Array.Empty<int>(),
                stressedSyllableIndex: -1);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: false, separator: " ");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertPhonemeSequence_SpaceSeparator_ReturnsSpaceSeparated()
        {
            // "si" → /s i/ (1音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.S),
                new PortuguesePhoneme(PortugueseIpaPhoneme.I, isStressed: true),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0 }, stressedSyllableIndex: 0);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: false, separator: " ");
            Assert.Equal("s i", result);
        }

        [Fact]
        public void ConvertPhonemeSequence_HyphenSeparator_ReturnsHyphenSeparated()
        {
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Z),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2 }, stressedSyllableIndex: 0);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: false, separator: "-");
            Assert.Equal("k-a-z-a", result);
        }

        [Fact]
        public void ConvertPhonemeSequence_WithStress_InsertsStressOnSyllableStart()
        {
            // "café" → /k a ˈf ɛ/ (ストレスは第2音節先頭に)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A),
                new PortuguesePhoneme(PortugueseIpaPhoneme.F),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Eh, isStressed: true),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2 }, stressedSyllableIndex: 1);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: true, separator: " ");
            Assert.Equal("k a \u02C8f \u025B", result); // k a ˈf ɛ
        }

        [Fact]
        public void ConvertPhonemeSequence_MultiSyllable_WithNasals()
        {
            // "canção" → /k ɐ̃ s ɐ̃ w̃/ (2音節)
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K),
                new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal),
                new PortuguesePhoneme(PortugueseIpaPhoneme.S),
                new PortuguesePhoneme(PortugueseIpaPhoneme.ANasal, isStressed: true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.WNasal),
            };
            var pron = new PortuguesePronunciation(phonemes, new[] { 0, 2 }, stressedSyllableIndex: 1);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: true, separator: " ");
            Assert.Equal("k \u0250\u0303 \u02C8s \u0250\u0303 w\u0303", result);
        }

        // ========== 全音素カバレッジ確認 ==========

        [Fact]
        public void ToSymbol_AllPhonemes_NoThrow()
        {
            // 全49音素 (0..48) が例外なくマッピングされていることを確認
            for (byte i = 0; i <= 48; i++)
            {
                var phoneme = (PortugueseIpaPhoneme)i;
                var symbol = IpaConverter.ToSymbol(phoneme);
                Assert.False(string.IsNullOrEmpty(symbol), $"Phoneme {phoneme} returned null or empty symbol.");
            }
        }
    }
}
