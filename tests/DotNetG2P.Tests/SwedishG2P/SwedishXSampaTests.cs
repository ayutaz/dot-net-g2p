using System;
using System.Linq;
using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Conversion;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語X-SAMPA変換テスト。
    /// XSampaConverter.ToSymbol() およびエンジンAPI (ToXSampa等) を検証する。
    /// </summary>
    public class SwedishXSampaTests : IDisposable
    {
        private readonly SwedishG2PEngine _engine;

        public SwedishXSampaTests()
        {
            _engine = new SwedishG2PEngine(new SwedishG2POptions(
                includeStress: true,
                enableTextNormalization: false));
        }

        public void Dispose() => _engine.Dispose();

        // =================================================================
        // 個別音素マッピングテスト
        // =================================================================

        [Theory]
        // 長母音（9音素）
        [InlineData(SwedishIpaPhoneme.LongI, "i:")]
        [InlineData(SwedishIpaPhoneme.LongY, "y:")]
        [InlineData(SwedishIpaPhoneme.LongUCentral, "u\\`:")]  // ʉː → u\`:
        [InlineData(SwedishIpaPhoneme.LongU, "u:")]
        [InlineData(SwedishIpaPhoneme.LongE, "e:")]
        [InlineData(SwedishIpaPhoneme.LongOe, "2:")]
        [InlineData(SwedishIpaPhoneme.LongEh, "E:")]
        [InlineData(SwedishIpaPhoneme.LongO, "o:")]
        [InlineData(SwedishIpaPhoneme.LongA, "A:")]
        // 短母音（9音素）
        [InlineData(SwedishIpaPhoneme.ShortI, "I")]
        [InlineData(SwedishIpaPhoneme.ShortY, "Y")]
        [InlineData(SwedishIpaPhoneme.ShortUCentral, "8")]      // ɵ → 8
        [InlineData(SwedishIpaPhoneme.ShortU, "U")]
        [InlineData(SwedishIpaPhoneme.ShortE, "E")]
        [InlineData(SwedishIpaPhoneme.ShortOe, "9")]            // œ → 9
        [InlineData(SwedishIpaPhoneme.ShortO, "O")]
        [InlineData(SwedishIpaPhoneme.ShortA, "a")]
        [InlineData(SwedishIpaPhoneme.Schwa, "@")]
        // 破裂音（6音素）
        [InlineData(SwedishIpaPhoneme.P, "p")]
        [InlineData(SwedishIpaPhoneme.B, "b")]
        [InlineData(SwedishIpaPhoneme.T, "t")]
        [InlineData(SwedishIpaPhoneme.D, "d")]
        [InlineData(SwedishIpaPhoneme.K, "k")]
        [InlineData(SwedishIpaPhoneme.G, "g")]
        // 摩擦音（6音素）
        [InlineData(SwedishIpaPhoneme.F, "f")]
        [InlineData(SwedishIpaPhoneme.V, "v")]
        [InlineData(SwedishIpaPhoneme.S, "s")]
        [InlineData(SwedishIpaPhoneme.H, "h")]
        [InlineData(SwedishIpaPhoneme.Sj, "x\\")]
        [InlineData(SwedishIpaPhoneme.Tj, "s\\")]
        // 鼻音（3音素）
        [InlineData(SwedishIpaPhoneme.M, "m")]
        [InlineData(SwedishIpaPhoneme.N, "n")]
        [InlineData(SwedishIpaPhoneme.Ng, "N")]
        // 接近音・ふるえ音（3音素）
        [InlineData(SwedishIpaPhoneme.L, "l")]
        [InlineData(SwedishIpaPhoneme.R, "r")]
        [InlineData(SwedishIpaPhoneme.J, "j")]
        // そり舌音（5音素）
        [InlineData(SwedishIpaPhoneme.RetroT, "t`")]
        [InlineData(SwedishIpaPhoneme.RetroD, "d`")]
        [InlineData(SwedishIpaPhoneme.RetroN, "n`")]
        [InlineData(SwedishIpaPhoneme.RetroL, "l`")]
        [InlineData(SwedishIpaPhoneme.RetroS, "s`")]
        public void ToSymbol_IndividualPhonemes_ReturnsExpectedXSampa(SwedishIpaPhoneme phoneme, string expected)
        {
            Assert.Equal(expected, XSampaConverter.ToSymbol(phoneme));
        }

        // =================================================================
        // エンジンAPI テスト
        // =================================================================

        [Fact]
        public void ToXSampa_Hej_ProducesNonEmptyOutput()
        {
            var result = _engine.ToXSampa("hej");
            Assert.False(string.IsNullOrEmpty(result), "ToXSampaの結果が空です");
        }

        [Fact]
        public void ToXSampaWithoutStress_NoStressMarker()
        {
            var result = _engine.ToXSampaWithoutStress("hej");
            Assert.DoesNotContain("\"", result); // X-SAMPAのストレスマーカー
        }

        [Fact]
        public void ToXSampa_WithStress_ContainsStressMarker()
        {
            var result = _engine.ToXSampa("hej");
            Assert.Contains("\"", result); // X-SAMPAのストレスマーカー
        }

        [Fact]
        public void ToXSampaBatch_MultipleTexts_ReturnsCorrectCount()
        {
            var result = _engine.ToXSampaBatch(new[] { "hej", "ja", "nej" });
            Assert.Equal(3, result.Count);
            foreach (var item in result)
            {
                Assert.False(string.IsNullOrEmpty(item), "バッチ変換の各結果が空であってはならない");
            }
        }

        // =================================================================
        // 全42音素カバレッジテスト
        // =================================================================

        [Fact]
        public void AllPhonemes_HaveNonEmptyXSampa()
        {
            // SwedishIpaPhoneme enum の全値 (0-41) がX-SAMPAマッピングを持つことを確認
            var allPhonemes = Enum.GetValues(typeof(SwedishIpaPhoneme)).Cast<SwedishIpaPhoneme>();
            foreach (var phoneme in allPhonemes)
            {
                var xsampa = XSampaConverter.ToSymbol(phoneme);
                Assert.False(string.IsNullOrEmpty(xsampa),
                    $"音素 {phoneme} のX-SAMPAマッピングが空です");
            }
        }

        [Fact]
        public void AllPhonemes_Count_Is42()
        {
            // SwedishIpaPhoneme enumは42個の音素を持つ (0-41)
            var count = Enum.GetValues(typeof(SwedishIpaPhoneme)).Length;
            Assert.Equal(42, count);
        }
    }
}
