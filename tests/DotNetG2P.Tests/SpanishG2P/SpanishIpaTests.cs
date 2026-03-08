using System;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishIpaTests : IDisposable
    {
        private readonly SpanishG2PEngine _latinAmerican = new SpanishG2PEngine();
        private readonly SpanishG2PEngine _castilian = new SpanishG2PEngine(new SpanishG2POptions(dialect: SpanishDialect.Castilian));

        [Theory]
        [InlineData("casa", "ˈkasa")]
        [InlineData("camino", "kaˈmino")]
        [InlineData("cielo", "ˈsjelo")]
        [InlineData("guerra", "ˈɡera")]
        [InlineData("queso", "ˈkeso")]
        [InlineData("guitarra", "ɡiˈtara")]
        [InlineData("llama", "ˈʝama")]
        [InlineData("chico", "ˈtʃiko")]
        [InlineData("acción", "akˈsjon")]
        [InlineData("ciudad", "sjuˈdad")]
        [InlineData("vergüenza", "beɾˈɡwensa")]
        [InlineData("pingüino", "pinˈɡwino")]
        [InlineData("xilófono", "siˈlofono")]
        [InlineData("alrededor", "alredeˈdoɾ")]
        public void ToIPA_LatinAmerican_ReturnsExpectedIpa(string word, string expected)
        {
            Assert.Equal(expected, _latinAmerican.ToIPA(word));
        }

        [Theory]
        [InlineData("zapato", "θaˈpato")]
        [InlineData("cielo", "ˈθjelo")]
        [InlineData("acción", "akˈθjon")]
        [InlineData("vergüenza", "beɾˈɡwenθa")]
        public void ToIPA_Castilian_UsesTheta(string word, string expected)
        {
            Assert.Equal(expected, _castilian.ToIPA(word));
        }

        [Fact]
        public void Dispose_ReleasesEngines()
        {
            _latinAmerican.Dispose();
            _castilian.Dispose();
        }

        public void Dispose()
        {
            _latinAmerican.Dispose();
            _castilian.Dispose();
        }
    }
}
