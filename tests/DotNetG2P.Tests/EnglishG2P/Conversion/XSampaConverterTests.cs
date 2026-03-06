using DotNetG2P.English;
using DotNetG2P.English.Conversion;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Conversion
{
    /// <summary>
    /// XSampaConverter の単体テスト。
    /// PhonemeToXSampa / Convert / ConvertWithoutStress の各メソッドを検証する。
    /// </summary>
    public class XSampaConverterTests
    {
        private static EnglishPhoneme P(ArpabetPhoneme p, Stress s = Stress.None)
            => new EnglishPhoneme(p, s);

        // ===== PhonemeToXSampa: 母音マッピング =====

        [Fact]
        public void PhonemeToXSampa_AH_Unstressed_ReturnsSchwa()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AH, Stress.NoStress));
            Assert.Equal("@", result);
        }

        [Fact]
        public void PhonemeToXSampa_AH_Stressed_ReturnsOpenMidBack()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AH, Stress.Primary));
            Assert.Equal("V", result);
        }

        [Fact]
        public void PhonemeToXSampa_ER_Unstressed_ReturnsRColoredSchwa()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.ER, Stress.NoStress));
            Assert.Equal("@`", result);
        }

        [Fact]
        public void PhonemeToXSampa_ER_Stressed_ReturnsRColoredOpenMid()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.ER, Stress.Primary));
            Assert.Equal("3`", result);
        }

        [Fact]
        public void PhonemeToXSampa_AA_ReturnsA()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AA, Stress.NoStress));
            Assert.Equal("A", result);
        }

        [Fact]
        public void PhonemeToXSampa_AE_ReturnsCurlyBrace()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AE, Stress.Primary));
            Assert.Equal("{", result);
        }

        [Fact]
        public void PhonemeToXSampa_AO_ReturnsO()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AO, Stress.NoStress));
            Assert.Equal("O", result);
        }

        [Fact]
        public void PhonemeToXSampa_AW_ReturnsDiphthong()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AW, Stress.Primary));
            Assert.Equal("aU", result);
        }

        [Fact]
        public void PhonemeToXSampa_AY_ReturnsDiphthong()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.AY, Stress.Primary));
            Assert.Equal("aI", result);
        }

        [Fact]
        public void PhonemeToXSampa_EY_ReturnsDiphthong()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.EY, Stress.Primary));
            Assert.Equal("eI", result);
        }

        [Fact]
        public void PhonemeToXSampa_IY_ReturnsSmallI()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.IY, Stress.Primary));
            Assert.Equal("i", result);
        }

        [Fact]
        public void PhonemeToXSampa_OW_ReturnsDiphthong()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.OW, Stress.Primary));
            Assert.Equal("oU", result);
        }

        [Fact]
        public void PhonemeToXSampa_UW_ReturnsSmallU()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.UW, Stress.Primary));
            Assert.Equal("u", result);
        }

        // ===== PhonemeToXSampa: 子音マッピング =====

        [Fact]
        public void PhonemeToXSampa_CH_ReturnsTSh()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.CH));
            Assert.Equal("tS", result);
        }

        [Fact]
        public void PhonemeToXSampa_DH_ReturnsCapitalD()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.DH));
            Assert.Equal("D", result);
        }

        [Fact]
        public void PhonemeToXSampa_JH_ReturnsDZh()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.JH));
            Assert.Equal("dZ", result);
        }

        [Fact]
        public void PhonemeToXSampa_NG_ReturnsCapitalN()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.NG));
            Assert.Equal("N", result);
        }

        [Fact]
        public void PhonemeToXSampa_R_ReturnsBackslashR()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.R));
            Assert.Equal("r\\", result);
        }

        [Fact]
        public void PhonemeToXSampa_SH_ReturnsCapitalS()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.SH));
            Assert.Equal("S", result);
        }

        [Fact]
        public void PhonemeToXSampa_TH_ReturnsCapitalT()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.TH));
            Assert.Equal("T", result);
        }

        [Fact]
        public void PhonemeToXSampa_ZH_ReturnsCapitalZ()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.ZH));
            Assert.Equal("Z", result);
        }

        [Fact]
        public void PhonemeToXSampa_Y_ReturnsJ()
        {
            var result = XSampaConverter.PhonemeToXSampa(P(ArpabetPhoneme.Y));
            Assert.Equal("j", result);
        }

        // ===== Convert: ストレスマーク付き =====

        [Fact]
        public void Convert_Hello_IncludesPrimaryStressMark()
        {
            // HH AH0 L OW1 → h @ l "oU
            var phonemes = new[]
            {
                P(ArpabetPhoneme.HH),
                P(ArpabetPhoneme.AH, Stress.NoStress),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.OW, Stress.Primary),
            };
            var result = XSampaConverter.Convert(phonemes);
            Assert.Equal("h @ l \"oU", result);
        }

        [Fact]
        public void Convert_World_StressOnER()
        {
            // W ER1 L D → w "3` l d
            var phonemes = new[]
            {
                P(ArpabetPhoneme.W),
                P(ArpabetPhoneme.ER, Stress.Primary),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.D),
            };
            var result = XSampaConverter.Convert(phonemes);
            Assert.Equal("w \"3` l d", result);
        }

        [Fact]
        public void Convert_Compute_SecondaryAndPrimaryStress()
        {
            // K AH0 M P Y UW1 T ER0 → k @ m p j "u t @`
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
            var result = XSampaConverter.Convert(phonemes);
            Assert.Equal("k @ m p j \"u t @`", result);
        }

        [Fact]
        public void Convert_SecondaryStress_UsesPercentMark()
        {
            // AH2 → %@  (Secondary stress uses % mark, but AH stressed → V)
            var phonemes = new[]
            {
                P(ArpabetPhoneme.AH, Stress.Secondary),
            };
            var result = XSampaConverter.Convert(phonemes);
            Assert.Equal("%V", result);
        }

        [Fact]
        public void Convert_EmptyArray_ReturnsEmpty()
        {
            var result = XSampaConverter.Convert(new EnglishPhoneme[0]);
            Assert.Equal("", result);
        }

        [Fact]
        public void Convert_Null_ReturnsEmpty()
        {
            var result = XSampaConverter.Convert(null!);
            Assert.Equal("", result);
        }

        // ===== ConvertWithoutStress =====

        [Fact]
        public void ConvertWithoutStress_Hello_NoStressMarks()
        {
            // HH AH0 L OW1 → h @ l oU (ストレスマークなし、AH→@固定)
            var phonemes = new[]
            {
                P(ArpabetPhoneme.HH),
                P(ArpabetPhoneme.AH, Stress.NoStress),
                P(ArpabetPhoneme.L),
                P(ArpabetPhoneme.OW, Stress.Primary),
            };
            var result = XSampaConverter.ConvertWithoutStress(phonemes);
            Assert.Equal("h @ l oU", result);
        }

        [Fact]
        public void ConvertWithoutStress_AH_AlwaysSchwa()
        {
            // AH1 → @ (ストレスなし版は常にschwa)
            var phonemes = new[]
            {
                P(ArpabetPhoneme.AH, Stress.Primary),
            };
            var result = XSampaConverter.ConvertWithoutStress(phonemes);
            Assert.Equal("@", result);
        }

        [Fact]
        public void ConvertWithoutStress_ER_AlwaysRColoredSchwa()
        {
            // ER1 → @` (ストレスなし版は常にr-colored schwa)
            var phonemes = new[]
            {
                P(ArpabetPhoneme.ER, Stress.Primary),
            };
            var result = XSampaConverter.ConvertWithoutStress(phonemes);
            Assert.Equal("@`", result);
        }

        [Fact]
        public void ConvertWithoutStress_EmptyArray_ReturnsEmpty()
        {
            var result = XSampaConverter.ConvertWithoutStress(new EnglishPhoneme[0]);
            Assert.Equal("", result);
        }

        [Fact]
        public void ConvertWithoutStress_Null_ReturnsEmpty()
        {
            var result = XSampaConverter.ConvertWithoutStress(null!);
            Assert.Equal("", result);
        }

        [Fact]
        public void ConvertWithoutStress_ConsonantsOnly_NoStressMarks()
        {
            // K S T → k s t
            var phonemes = new[]
            {
                P(ArpabetPhoneme.K),
                P(ArpabetPhoneme.S),
                P(ArpabetPhoneme.T),
            };
            var result = XSampaConverter.ConvertWithoutStress(phonemes);
            Assert.Equal("k s t", result);
        }
    }
}
