using DotNetG2P.Korean.Normalization;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanNormalizerTests
    {
        [Theory]
        [InlineData("  안녕,  한글!  ", "안녕 한글")]
        [InlineData("깻잎·검열/나의", "깻잎 검열 나의")]
        [InlineData("（한글）　ＡＢＣ　１２３", "한글 ABC 123")]
        [InlineData("밟다-검열", "밟다 검열")]
        [InlineData("한글\t\r\n검열", "한글 검열")]
        public void Normalize_ReturnsExpectedText(string input, string expected)
        {
            Assert.Equal(expected, KoreanNormalizer.Normalize(input));
        }

        [Fact]
        public void Normalize_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, KoreanNormalizer.Normalize(null!));
            Assert.Equal(string.Empty, KoreanNormalizer.Normalize(string.Empty));
            Assert.Equal(string.Empty, KoreanNormalizer.Normalize("  \t "));
        }

        [Fact]
        public void Tokenize_SplitsNormalizedWhitespace()
        {
            var tokens = KoreanNormalizer.Tokenize("국밥 신라 나의");

            Assert.Equal(new[] { "국밥", "신라", "나의" }, tokens);
        }
    }
}
