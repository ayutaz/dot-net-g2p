using DotNetG2P.English.Homograph;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Homograph
{
    /// <summary>
    /// PosGuesser の単体テスト。
    /// 接尾辞ルールと文脈ルールによる品詞推定を検証する。
    /// </summary>
    public class PosGuesserTests
    {
        // ===== 接尾辞ルールテスト =====

        [Theory]
        [InlineData("running")]
        [InlineData("walking")]
        [InlineData("playing")]
        public void Suffix_Ing_ReturnsVerb(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Verb, result);
        }

        [Theory]
        [InlineData("thing")]
        [InlineData("king")]
        [InlineData("ring")]
        [InlineData("building")]
        [InlineData("morning")]
        [InlineData("nothing")]
        public void Suffix_IngExceptions_ReturnNoun(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Noun, result);
        }

        [Theory]
        [InlineData("education")]
        [InlineData("discussion")]
        [InlineData("development")]
        [InlineData("happiness")]
        [InlineData("reality")]
        [InlineData("distance")]
        [InlineData("patience")]
        [InlineData("socialism")]
        [InlineData("artist")]
        [InlineData("teacher")]
        [InlineData("actor")]
        public void Suffix_NounSuffixes_ReturnNoun(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Noun, result);
        }

        [Theory]
        [InlineData("quickly")]
        [InlineData("slowly")]
        public void Suffix_Ly_ReturnsAdverb(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Adverb, result);
        }

        [Theory]
        [InlineData("beautiful")]
        [InlineData("dangerous")]
        [InlineData("active")]
        [InlineData("readable")]
        [InlineData("possible")]
        [InlineData("musical")]
        [InlineData("careless")]
        [InlineData("tallest")]
        [InlineData("official")]
        public void Suffix_AdjectiveSuffixes_ReturnAdjective(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Adjective, result);
        }

        [Theory]
        [InlineData("walked")]
        [InlineData("played")]
        [InlineData("started")]
        public void Suffix_Ed_ReturnsVerb(string word)
        {
            var result = PosGuesser.Guess(new[] { word }, 0);
            Assert.Equal(PosTag.Verb, result);
        }

        [Fact]
        public void Suffix_Live_ExcludedFromIve_ReturnsUnknown()
        {
            // "live" は -ive ルールから除外される
            var result = PosGuesser.Guess(new[] { "live" }, 0);
            Assert.Equal(PosTag.Unknown, result);
        }

        [Fact]
        public void Suffix_ShortEr_NotNoun()
        {
            // 3文字以下の -er は Noun にならない（"her" は3文字）
            var result = PosGuesser.Guess(new[] { "her" }, 0);
            Assert.NotEqual(PosTag.Noun, result);
        }

        // ===== 文脈ルールテスト =====

        [Theory]
        [InlineData("the")]
        [InlineData("a")]
        [InlineData("an")]
        public void Context_AfterArticle_ReturnsNoun(string article)
        {
            var result = PosGuesser.Guess(new[] { article, "record" }, 1);
            Assert.Equal(PosTag.Noun, result);
        }

        [Theory]
        [InlineData("will")]
        [InlineData("can")]
        [InlineData("should")]
        [InlineData("must")]
        public void Context_AfterModal_ReturnsVerb(string modal)
        {
            var result = PosGuesser.Guess(new[] { modal, "record" }, 1);
            Assert.Equal(PosTag.Verb, result);
        }

        [Fact]
        public void Context_AfterTo_ReturnsVerb()
        {
            var result = PosGuesser.Guess(new[] { "to", "record" }, 1);
            Assert.Equal(PosTag.Verb, result);
        }

        [Theory]
        [InlineData("my")]
        [InlineData("your")]
        [InlineData("his")]
        [InlineData("their")]
        public void Context_AfterPossessive_ReturnsNoun(string possessive)
        {
            var result = PosGuesser.Guess(new[] { possessive, "record" }, 1);
            Assert.Equal(PosTag.Noun, result);
        }

        [Fact]
        public void Context_AfterPronoun_ReturnsVerb()
        {
            // "I record" → 動詞
            var result = PosGuesser.Guess(new[] { "I", "record" }, 1);
            Assert.Equal(PosTag.Verb, result);
        }

        [Fact]
        public void Context_AfterPlease_ReturnsVerb()
        {
            var result = PosGuesser.Guess(new[] { "please", "close" }, 1);
            Assert.Equal(PosTag.Verb, result);
        }

        [Theory]
        [InlineData("very")]
        [InlineData("quite")]
        [InlineData("extremely")]
        public void Context_AfterDegreeAdverb_ReturnsAdjective(string adverb)
        {
            var result = PosGuesser.Guess(new[] { adverb, "close" }, 1);
            Assert.Equal(PosTag.Adjective, result);
        }

        // ===== 文脈優先テスト =====

        [Fact]
        public void Context_OverridesSuffix_ArticleBeforeIngWord()
        {
            // "the running" → 文脈ルール(Noun)が接尾辞ルール(Verb)より優先
            var result = PosGuesser.Guess(new[] { "the", "running" }, 1);
            Assert.Equal(PosTag.Noun, result);
        }

        [Fact]
        public void Context_OverridesSuffix_ModalBeforeNounSuffix()
        {
            // "will development" → 文脈ルール(Verb)が接尾辞ルール(Noun)より優先
            var result = PosGuesser.Guess(new[] { "will", "development" }, 1);
            Assert.Equal(PosTag.Verb, result);
        }

        // ===== エッジケーステスト =====

        [Fact]
        public void SingleWord_NoSuffixMatch_ReturnsUnknown()
        {
            var result = PosGuesser.Guess(new[] { "record" }, 0);
            Assert.Equal(PosTag.Unknown, result);
        }

        [Fact]
        public void NullWords_ReturnsUnknown()
        {
            var result = PosGuesser.Guess(null!, 0);
            Assert.Equal(PosTag.Unknown, result);
        }

        [Fact]
        public void OutOfBoundsIndex_ReturnsUnknown()
        {
            var result = PosGuesser.Guess(new[] { "hello" }, 5);
            Assert.Equal(PosTag.Unknown, result);
        }

        [Fact]
        public void NegativeIndex_ReturnsUnknown()
        {
            var result = PosGuesser.Guess(new[] { "hello" }, -1);
            Assert.Equal(PosTag.Unknown, result);
        }

        [Fact]
        public void EmptyWord_ReturnsUnknown()
        {
            var result = PosGuesser.Guess(new[] { "" }, 0);
            Assert.Equal(PosTag.Unknown, result);
        }
    }
}
