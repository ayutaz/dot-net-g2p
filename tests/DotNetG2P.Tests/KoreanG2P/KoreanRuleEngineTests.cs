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
        [InlineData("밭이", "ㅂ ㅏ ㅊ ㅣ")]
        [InlineData("먹는", "ㅁ ㅓ ㅇ ㄴ ㅡ ㄴ")]
        [InlineData("국밥", "ㄱ ㅜ ㄱ ㅃ ㅏ ㅂ")]
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
