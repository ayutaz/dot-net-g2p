using DotNetG2P.French;
using DotNetG2P.French.Conversion;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class FrenchIpaTests
    {
        // ========== ToSymbol テスト ==========

        [Theory]
        [InlineData(FrenchIpaPhoneme.A, "a")]
        [InlineData(FrenchIpaPhoneme.Ah, "\u0251")]           // ɑ
        [InlineData(FrenchIpaPhoneme.E, "e")]
        [InlineData(FrenchIpaPhoneme.Eh, "\u025B")]           // ɛ
        [InlineData(FrenchIpaPhoneme.I, "i")]
        [InlineData(FrenchIpaPhoneme.O, "o")]
        [InlineData(FrenchIpaPhoneme.Oh, "\u0254")]           // ɔ
        [InlineData(FrenchIpaPhoneme.U, "u")]
        [InlineData(FrenchIpaPhoneme.Y, "y")]
        [InlineData(FrenchIpaPhoneme.Oe, "\u00F8")]           // ø
        [InlineData(FrenchIpaPhoneme.Oeh, "\u0153")]          // œ
        [InlineData(FrenchIpaPhoneme.Schwa, "\u0259")]        // ə
        [InlineData(FrenchIpaPhoneme.ANasal, "\u0251\u0303")] // ɑ̃
        [InlineData(FrenchIpaPhoneme.ONasal, "\u0254\u0303")] // ɔ̃
        [InlineData(FrenchIpaPhoneme.ENasal, "\u025B\u0303")] // ɛ̃
        [InlineData(FrenchIpaPhoneme.OeNasal, "\u0153\u0303")]// œ̃
        [InlineData(FrenchIpaPhoneme.Sh, "\u0283")]           // ʃ
        [InlineData(FrenchIpaPhoneme.Zh, "\u0292")]           // ʒ
        [InlineData(FrenchIpaPhoneme.R, "\u0281")]            // ʁ
        [InlineData(FrenchIpaPhoneme.Ny, "\u0272")]           // ɲ
        // 半母音
        [InlineData(FrenchIpaPhoneme.J, "j")]
        [InlineData(FrenchIpaPhoneme.W, "w")]
        [InlineData(FrenchIpaPhoneme.Uj, "\u0265")]            // ɥ
        // 閉鎖音
        [InlineData(FrenchIpaPhoneme.P, "p")]
        [InlineData(FrenchIpaPhoneme.B, "b")]
        [InlineData(FrenchIpaPhoneme.T, "t")]
        [InlineData(FrenchIpaPhoneme.D, "d")]
        [InlineData(FrenchIpaPhoneme.K, "k")]
        [InlineData(FrenchIpaPhoneme.G, "\u0261")]             // ɡ (U+0261)
        // 摩擦音
        [InlineData(FrenchIpaPhoneme.F, "f")]
        [InlineData(FrenchIpaPhoneme.V, "v")]
        [InlineData(FrenchIpaPhoneme.S, "s")]
        [InlineData(FrenchIpaPhoneme.Z, "z")]
        // 鼻音
        [InlineData(FrenchIpaPhoneme.M, "m")]
        [InlineData(FrenchIpaPhoneme.N, "n")]
        // 側面音
        [InlineData(FrenchIpaPhoneme.L, "l")]
        // 異音
        [InlineData(FrenchIpaPhoneme.Rh, "\u03C7")]            // χ
        [InlineData(FrenchIpaPhoneme.Ng, "\u014B")]            // ŋ
        [InlineData(FrenchIpaPhoneme.Ts, "ts")]
        [InlineData(FrenchIpaPhoneme.Dz, "dz")]
        public void ToSymbol_Phoneme_ReturnsCorrectIPA(FrenchIpaPhoneme phoneme, string expected)
        {
            var result = IpaConverter.ToSymbol(phoneme);
            Assert.Equal(expected, result);
        }

        // ========== Convert テスト ==========

        [Fact]
        public void Convert_EmptyPronunciation_ReturnsEmpty()
        {
            var pron = new FrenchPronunciation(
                Array.Empty<FrenchPhoneme>(),
                Array.Empty<int>(),
                stressedSyllableIndex: -1);

            var result = IpaConverter.Convert(pron, includeStress: false);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_SimplePronunciation_ReturnsIPA()
        {
            // "bonjour" → /b ɔ̃ ʒ u ʁ/ (2音節: bɔ̃.ʒuʁ)
            var phonemes = new[]
            {
                new FrenchPhoneme(FrenchIpaPhoneme.B),
                new FrenchPhoneme(FrenchIpaPhoneme.ONasal, isSyllableNucleus: true),
                new FrenchPhoneme(FrenchIpaPhoneme.Zh),
                new FrenchPhoneme(FrenchIpaPhoneme.U, isSyllableNucleus: true),
                new FrenchPhoneme(FrenchIpaPhoneme.R),
            };
            var syllableOffsets = new[] { 0, 2 };
            var pron = new FrenchPronunciation(phonemes, syllableOffsets, stressedSyllableIndex: -1);

            var result = IpaConverter.Convert(pron, includeStress: false);
            Assert.Equal("b\u0254\u0303\u0292u\u0281", result); // bɔ̃ʒuʁ
        }

        // ========== ConvertPhonemeSequence テスト ==========

        [Fact]
        public void ConvertPhonemeSequence_SimplePronunciation_ReturnsSpaceSeparated()
        {
            // "si" → /s i/ (1音節)
            var phonemes = new[]
            {
                new FrenchPhoneme(FrenchIpaPhoneme.S),
                new FrenchPhoneme(FrenchIpaPhoneme.I, isSyllableNucleus: true),
            };
            var syllableOffsets = new[] { 0 };
            var pron = new FrenchPronunciation(phonemes, syllableOffsets, stressedSyllableIndex: -1);

            var result = IpaConverter.ConvertPhonemeSequence(pron, includeStress: false, separator: " ");
            Assert.Equal("s i", result);
        }
    }
}
