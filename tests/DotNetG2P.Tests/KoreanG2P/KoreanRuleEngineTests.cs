using System.Linq;
using DotNetG2P.Korean;
using DotNetG2P.Korean.Rules;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanRuleEngineTests
    {
        [Theory]
        [InlineData('ㅅ', 'ㄷ')]
        [InlineData('ㅊ', 'ㄷ')]
        [InlineData('ㄲ', 'ㄱ')]
        [InlineData('ㅍ', 'ㅂ')]
        [InlineData('ㄻ', 'ㅁ')]
        public void RepresentativeCoda_IsNormalized(char input, char expected)
        {
            Assert.Equal(expected, BatchimProcessor.ToRepresentativeCoda(input));
        }

        [Theory]
        [InlineData('ㄳ', 'ㄱ', 'ㅅ')]
        [InlineData('ㄺ', 'ㄹ', 'ㄱ')]
        [InlineData('ㅄ', 'ㅂ', 'ㅅ')]
        public void LiaisonSplit_ReturnsExpectedRetainedAndMoved(char coda, char retained, char moved)
        {
            Assert.True(BatchimProcessor.TrySplitForLiaison(coda, out var actualRetained, out var actualMoved));
            Assert.Equal(retained, actualRetained);
            Assert.Equal(moved, actualMoved);
        }

        [Theory]
        [InlineData("꽃", "꼳")]
        [InlineData("먹는", "멍는")]
        [InlineData("국밥", "국빱")]
        [InlineData("신라", "실라")]
        [InlineData("밭이", "바치")]
        [InlineData("깻잎", "깬닙")]
        [InlineData("놓는", "논는")]
        [InlineData("않네", "안네")]
        public void Analyze_AppliesRepresentativeM2Rules(string input, string expected)
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.Analyze(input);

            Assert.Equal(expected, result.ToHangulString());
        }

        [Theory]
        [InlineData("좋아", "조아")]
        [InlineData("좋다", "조타")]
        [InlineData("좋지", "조치")]
        [InlineData("놓고", "노코")]
        [InlineData("않다", "안타")]
        [InlineData("싫어", "시러")]
        public void Analyze_AppliesHFamilyTransformations(string input, string expected)
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal(expected, engine.Analyze(input).ToHangulString());
        }

        [Theory]
        [InlineData("담요", "담뇨")]
        [InlineData("검열", "검녈")]
        [InlineData("색연필", "생년필")]
        [InlineData("막일", "망닐")]
        [InlineData("한여름", "한녀름")]
        [InlineData("솜이불", "솜니불")]
        public void Analyze_AppliesGeneralizedNInsertion(string input, string expected)
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal(expected, engine.Analyze(input).ToHangulString());
        }

        [Theory]
        [InlineData("밟다", "밥따")]
        [InlineData("밟고", "밥꼬")]
        [InlineData("밟는", "밤는")]
        public void Analyze_ResolvesBieupDominantDoubleBatchimBeforeConsonants(string input, string expected)
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal(expected, engine.Analyze(input).ToHangulString());
        }

        [Fact]
        public void DecomposeText_PreservesWhitespaceAsBoundaryMarker()
        {
            var syllables = KoreanOrthography.DecomposeText("국밥 신라", preserveNonHangul: false);

            Assert.Contains(syllables, syllable => syllable.IsBoundary);
            Assert.Equal(" ", syllables.Single(syllable => syllable.IsBoundary).ToHangulString());
        }

        [Fact]
        public void Analyze_DoesNotApplyIntraWordRulesAcrossWhitespaceBoundary()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal("검 열", engine.Analyze("검 열").ToHangulString());
        }

        [Theory]
        [InlineData("밭이", "ㅂ ㅏ ㅊ ㅣ")]
        [InlineData("먹는", "ㅁ ㅓ ㅇ ㄴ ㅡ ㄴ")]
        [InlineData("국밥", "ㄱ ㅜ ㄱ ㅃ ㅏ ㅂ")]
        [InlineData("좋다", "ㅈ ㅗ ㅌ ㅏ")]
        [InlineData("담요", "ㄷ ㅏ ㅁ ㄴ ㅛ")]
        [InlineData("밟다", "ㅂ ㅏ ㅂ ㄸ ㅏ")]
        public void ToPhonemes_ReflectsAppliedRules(string input, string expected)
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal(expected, engine.ToPhonemes(input));
        }

        [Fact]
        public void GraphemeToPhonemeRules_PreservesStandaloneNonHangul()
        {
            var syllables = new[]
            {
                new KoreanSyllable('ㄱ', 'ㅜ', 'ㄱ'),
                KoreanSyllable.FromStandaloneJamo('A'),
                new KoreanSyllable('ㅂ', 'ㅏ', 'ㅂ'),
            };

            var result = GraphemeToPhonemeRules.Convert(syllables);

            Assert.Equal("국A밥", string.Concat(
                result[0].ToHangulString(),
                result[1].ToHangulString(),
                result[2].ToHangulString()));
        }
    }
}
