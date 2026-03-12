using System;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    public class MultilingualOptionValidationTests
    {
        [Fact]
        public void Options_DefaultCjkLanguageEnglish指定_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MultilingualG2POptions(defaultCjkLanguage: Language.English));
        }

        [Fact]
        public void Options_DefaultCjkLanguageKorean指定_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MultilingualG2POptions(defaultCjkLanguage: Language.Korean));
        }

        [Fact]
        public void TextSegmenter_DefaultCjkLanguageEnglish指定_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TextSegmenter.Segment("東京", Language.English, Language.English));
        }

        [Fact]
        public void TextSegmenter_DefaultCjkLanguageKorean指定_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TextSegmenter.Segment("東京", Language.Korean, Language.English));
        }
    }
}
