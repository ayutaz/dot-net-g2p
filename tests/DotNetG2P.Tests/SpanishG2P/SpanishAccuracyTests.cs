using System;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishAccuracyTests : IDisposable
    {
        private readonly SpanishG2PEngine _latinAmerican = new SpanishG2PEngine();
        private readonly SpanishG2PEngine _castilian = new SpanishG2PEngine(new SpanishG2POptions(dialect: SpanishDialect.Castilian));
        private readonly SpanishG2PEngine _allophonic = new SpanishG2PEngine(new SpanishG2POptions(enableAllophones: true));

        [Theory]
        [InlineData("casa", "ˈkasa")]
        [InlineData("camino", "kaˈmino")]
        [InlineData("ciudad", "sjuˈdad")]
        [InlineData("guerra", "ˈɡera")]
        [InlineData("queso", "ˈkeso")]
        [InlineData("pingüino", "pinˈɡwino")]
        [InlineData("vergüenza", "beɾˈɡwensa")]
        [InlineData("xilófono", "siˈlofono")]
        [InlineData("alrededor", "alredeˈdoɾ")]
        [InlineData("México", "ˈmexiko")]
        [InlineData("Xochimilco", "ʃotʃiˈmilko")]
        [InlineData("Wagner", "ˈbaɡner")]
        public void ToIPA_CuratedLatinAmericanCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _latinAmerican.ToIPA(word));
        }

        [Theory]
        [InlineData("zapato", "θaˈpato")]
        [InlineData("cielo", "ˈθjelo")]
        [InlineData("acción", "akˈθjon")]
        [InlineData("vergüenza", "beɾˈɡwenθa")]
        [InlineData("cinco", "ˈθinko")]
        public void ToIPA_CuratedCastilianCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _castilian.ToIPA(word));
        }

        [Theory]
        [InlineData("uva", "ˈuβa")]
        [InlineData("dedo", "ˈdeðo")]
        [InlineData("lago", "ˈlaɣo")]
        [InlineData("mismo", "ˈmizmo")]
        [InlineData("enfasis", "eɱˈfasis")]
        [InlineData("tengo", "ˈteŋɡo")]
        [InlineData("inyección", "iɲɟʝekˈsjon")]
        public void ToIPA_AllophonicCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _allophonic.ToIPA(word));
        }

        [Theory]
        [InlineData("casa", "\"kasa")]
        [InlineData("camino", "ka\"mino")]
        [InlineData("guion", "gi\"on")]
        [InlineData("truhan", "t4u\"an")]
        [InlineData("show", "\"Sow")]
        [InlineData("México", "\"mexiko")]
        [InlineData("Xochimilco", "SotSi\"milko")]
        [InlineData("Wagner", "\"bagner")]
        public void ToXSampa_CuratedRegressionCorpus_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _latinAmerican.ToXSampa(word));
        }

        [Fact]
        public void ExceptionAndRuleCorpus_AllReturnNonEmptyAcrossFormats()
        {
            var words = new[]
            {
                "guion", "truhan", "whisky", "wifi", "show",
                "México", "Xochimilco", "Wagner", "software", "shampoo"
            };

            foreach (var word in words)
            {
                Assert.NotEmpty(_latinAmerican.ToIPA(word));
                Assert.NotEmpty(_latinAmerican.ToXSampa(word));
                Assert.NotEmpty(_latinAmerican.ToPhonemes(word));
            }
        }

        public void Dispose()
        {
            _latinAmerican.Dispose();
            _castilian.Dispose();
            _allophonic.Dispose();
        }
    }
}
