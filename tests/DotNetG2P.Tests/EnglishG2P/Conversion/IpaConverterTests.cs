using DotNetG2P.English;
using DotNetG2P.English.Conversion;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Conversion
{
    /// <summary>
    /// IpaConverter の単体テスト。
    /// PhonemeToIpa / Convert / ConvertWithoutStress の各メソッドを検証する。
    /// </summary>
    public class IpaConverterTests
    {
        private static EnglishPhoneme P(ArpabetPhoneme p, Stress s = Stress.None)
            => new EnglishPhoneme(p, s);

        // ===== PhonemeToIpa: 母音マッピング（unstressed） =====

        [Theory]
        [InlineData(ArpabetPhoneme.AA, "ɑ")]
        [InlineData(ArpabetPhoneme.AE, "æ")]
        [InlineData(ArpabetPhoneme.AH, "ə")]
        [InlineData(ArpabetPhoneme.AO, "ɔ")]
        [InlineData(ArpabetPhoneme.AW, "aʊ")]
        [InlineData(ArpabetPhoneme.AY, "aɪ")]
        [InlineData(ArpabetPhoneme.EH, "ɛ")]
        [InlineData(ArpabetPhoneme.ER, "ɚ")]
        [InlineData(ArpabetPhoneme.EY, "eɪ")]
        [InlineData(ArpabetPhoneme.IH, "ɪ")]
        [InlineData(ArpabetPhoneme.IY, "i")]
        [InlineData(ArpabetPhoneme.OW, "oʊ")]
        [InlineData(ArpabetPhoneme.OY, "ɔɪ")]
        [InlineData(ArpabetPhoneme.UH, "ʊ")]
        [InlineData(ArpabetPhoneme.UW, "u")]
        public void PhonemeToIpa_VowelUnstressed_ReturnsCorrectIpa(ArpabetPhoneme phoneme, string expectedIpa)
        {
            var result = IpaConverter.PhonemeToIpa(P(phoneme, Stress.NoStress));
            Assert.Equal(expectedIpa, result);
        }

        // ===== PhonemeToIpa: 母音マッピング（Primary stressed） =====

        [Theory]
        [InlineData(ArpabetPhoneme.AA, "ɑ")]
        [InlineData(ArpabetPhoneme.AE, "æ")]
        [InlineData(ArpabetPhoneme.AH, "ʌ")]   // stressed = ʌ
        [InlineData(ArpabetPhoneme.AO, "ɔ")]
        [InlineData(ArpabetPhoneme.AW, "aʊ")]
        [InlineData(ArpabetPhoneme.AY, "aɪ")]
        [InlineData(ArpabetPhoneme.EH, "ɛ")]
        [InlineData(ArpabetPhoneme.ER, "ɝ")]   // stressed = ɝ
        [InlineData(ArpabetPhoneme.EY, "eɪ")]
        [InlineData(ArpabetPhoneme.IH, "ɪ")]
        [InlineData(ArpabetPhoneme.IY, "i")]
        [InlineData(ArpabetPhoneme.OW, "oʊ")]
        [InlineData(ArpabetPhoneme.OY, "ɔɪ")]
        [InlineData(ArpabetPhoneme.UH, "ʊ")]
        [InlineData(ArpabetPhoneme.UW, "u")]
        public void PhonemeToIpa_VowelPrimaryStress_ReturnsCorrectIpa(ArpabetPhoneme phoneme, string expectedIpa)
        {
            var result = IpaConverter.PhonemeToIpa(P(phoneme, Stress.Primary));
            Assert.Equal(expectedIpa, result);
        }

        // ===== PhonemeToIpa: AH / ER ストレス切り替え検証 =====

        [Fact]
        public void PhonemeToIpa_AH_SecondaryStress_ReturnsOpenMidBack()
        {
            var result = IpaConverter.PhonemeToIpa(P(ArpabetPhoneme.AH, Stress.Secondary));
            Assert.Equal("ʌ", result);
        }

        [Fact]
        public void PhonemeToIpa_ER_SecondaryStress_ReturnsRColoredOpenMid()
        {
            var result = IpaConverter.PhonemeToIpa(P(ArpabetPhoneme.ER, Stress.Secondary));
            Assert.Equal("ɝ", result);
        }

        // ===== PhonemeToIpa: 子音マッピング =====

        [Theory]
        [InlineData(ArpabetPhoneme.B, "b")]
        [InlineData(ArpabetPhoneme.CH, "tʃ")]
        [InlineData(ArpabetPhoneme.D, "d")]
        [InlineData(ArpabetPhoneme.DH, "ð")]
        [InlineData(ArpabetPhoneme.F, "f")]
        [InlineData(ArpabetPhoneme.G, "ɡ")]   // U+0261
        [InlineData(ArpabetPhoneme.HH, "h")]
        [InlineData(ArpabetPhoneme.JH, "dʒ")]
        [InlineData(ArpabetPhoneme.K, "k")]
        [InlineData(ArpabetPhoneme.L, "l")]
        [InlineData(ArpabetPhoneme.M, "m")]
        [InlineData(ArpabetPhoneme.N, "n")]
        [InlineData(ArpabetPhoneme.NG, "ŋ")]
        [InlineData(ArpabetPhoneme.P, "p")]
        [InlineData(ArpabetPhoneme.R, "ɹ")]
        [InlineData(ArpabetPhoneme.S, "s")]
        [InlineData(ArpabetPhoneme.SH, "ʃ")]
        [InlineData(ArpabetPhoneme.T, "t")]
        [InlineData(ArpabetPhoneme.TH, "θ")]
        [InlineData(ArpabetPhoneme.V, "v")]
        [InlineData(ArpabetPhoneme.W, "w")]
        [InlineData(ArpabetPhoneme.Y, "j")]
        [InlineData(ArpabetPhoneme.Z, "z")]
        [InlineData(ArpabetPhoneme.ZH, "ʒ")]
        public void PhonemeToIpa_Consonant_ReturnsCorrectIpa(ArpabetPhoneme phoneme, string expectedIpa)
        {
            var result = IpaConverter.PhonemeToIpa(P(phoneme));
            Assert.Equal(expectedIpa, result);
        }

        // ===== Convert: 単語レベル（ストレスマーク付き） =====

        [Fact]
        public void Convert_Hello_ReturnsIpaWithStressMark()
        {
            // HELLO: HH AH0 L OW1
            var phonemes = new[]
            {
                P(ArpabetPhoneme.HH),
                P(ArpabetPhoneme.AH, Stress.NoStress),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.OW, Stress.Primary),
            };
            var result = IpaConverter.Convert(phonemes);
            // IPA標準: ストレスマークは音節オンセット（先行子音群の前）に配置
            // h + ə + ˈl + oʊ
            Assert.Equal("həˈloʊ", result);
        }

        [Fact]
        public void Convert_World_ReturnsIpaWithStressMark()
        {
            // WORLD: W ER1 L D
            var phonemes = new[]
            {
                P(ArpabetPhoneme.W),
                P(ArpabetPhoneme.ER, Stress.Primary),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.D),
            };
            var result = IpaConverter.Convert(phonemes);
            // IPA標準: ストレスマークは音節オンセット（先行子音群の前）に配置
            // ˈw + ɝ + l + d（語頭の子音Wの前にストレスマーク）
            Assert.Equal("ˈwɝld", result);
        }

        [Fact]
        public void Convert_Computer_ReturnsIpaWithStressMark()
        {
            // COMPUTER: K AH0 M P Y UW1 T ER0
            var phonemes = new[]
            {
                P(ArpabetPhoneme.K),
                P(ArpabetPhoneme.AH, Stress.NoStress),
                P(ArpabetPhoneme.M),
                P(ArpabetPhoneme.P),
                P(ArpabetPhoneme.Y),
                P(ArpabetPhoneme.UW, Stress.Primary),
                P(ArpabetPhoneme.T),
                P(ArpabetPhoneme.ER, Stress.NoStress),
            };
            var result = IpaConverter.Convert(phonemes);
            // IPA標準: ストレスマークは音節オンセット（先行子音群の前）に配置
            // k + ə + ˈm + p + j + u + t + ɚ（ストレス母音UWの前の子音群MPYの前にマーク）
            Assert.Equal("kəˈmpjutɚ", result);
        }

        [Fact]
        public void Convert_WithSecondaryStress_ReturnsSecondaryMark()
        {
            // テスト用: AO2 の場合 ˌɔ が出力される
            var phonemes = new[]
            {
                P(ArpabetPhoneme.AO, Stress.Secondary),
            };
            var result = IpaConverter.Convert(phonemes);
            Assert.Equal("ˌɔ", result);
        }

        [Fact]
        public void Convert_ConsonantsOnly_NoStressMarks()
        {
            // 子音のみの場合、ストレスマークなし
            var phonemes = new[]
            {
                P(ArpabetPhoneme.S),
                P(ArpabetPhoneme.T),
                P(ArpabetPhoneme.R),
            };
            var result = IpaConverter.Convert(phonemes);
            Assert.Equal("stɹ", result);
        }

        [Fact]
        public void Convert_EmptyArray_ReturnsEmptyString()
        {
            var result = IpaConverter.Convert(new EnglishPhoneme[0]);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_Null_ReturnsEmptyString()
        {
            var result = IpaConverter.Convert(null!);
            Assert.Equal(string.Empty, result);
        }

        // ===== ConvertWithoutStress =====

        [Fact]
        public void ConvertWithoutStress_Hello_ReturnsIpaWithoutStressMarks()
        {
            // HELLO: HH AH0 L OW1 → ストレスマークなし、母音は常にunstressed IPA
            var phonemes = new[]
            {
                P(ArpabetPhoneme.HH),
                P(ArpabetPhoneme.AH, Stress.NoStress),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.OW, Stress.Primary),
            };
            var result = IpaConverter.ConvertWithoutStress(phonemes);
            // h + ə + l + oʊ (マーカーなし、unstressed IPA)
            Assert.Equal("həloʊ", result);
        }

        [Fact]
        public void ConvertWithoutStress_World_UsesUnstressedER()
        {
            // WORLD: W ER1 L D → ER は常に ɚ（unstressed版）
            var phonemes = new[]
            {
                P(ArpabetPhoneme.W),
                P(ArpabetPhoneme.ER, Stress.Primary),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.D),
            };
            var result = IpaConverter.ConvertWithoutStress(phonemes);
            // w + ɚ + l + d (stressed ERでもɚを使用)
            Assert.Equal("wɚld", result);
        }

        [Fact]
        public void ConvertWithoutStress_AH_AlwaysSchwa()
        {
            // AH1 → ConvertWithoutStressでは常に ə
            var phonemes = new[]
            {
                P(ArpabetPhoneme.AH, Stress.Primary),
            };
            var result = IpaConverter.ConvertWithoutStress(phonemes);
            Assert.Equal("ə", result);
        }

        [Fact]
        public void ConvertWithoutStress_EmptyArray_ReturnsEmptyString()
        {
            var result = IpaConverter.ConvertWithoutStress(new EnglishPhoneme[0]);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertWithoutStress_Null_ReturnsEmptyString()
        {
            var result = IpaConverter.ConvertWithoutStress(null!);
            Assert.Equal(string.Empty, result);
        }
    }
}
