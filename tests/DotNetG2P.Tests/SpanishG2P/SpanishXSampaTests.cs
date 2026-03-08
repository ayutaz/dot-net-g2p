using System;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishXSampaTests : IDisposable
    {
        private readonly SpanishG2PEngine _latinAmerican = new SpanishG2PEngine();
        private readonly SpanishG2PEngine _castilian = new SpanishG2PEngine(new SpanishG2POptions(dialect: SpanishDialect.Castilian));
        private readonly SpanishG2PEngine _allophonic = new SpanishG2PEngine(new SpanishG2POptions(enableAllophones: true));

        [Theory]
        [InlineData("casa", "\"kasa")]
        [InlineData("camino", "ka\"mino")]
        [InlineData("cielo", "\"sjelo")]
        [InlineData("queso", "\"keso")]
        [InlineData("pingüino", "pin\"gwino")]
        [InlineData("show", "\"Sow")]
        public void ToXSampa_LatinAmerican_ReturnsExpectedOutput(string word, string expected)
        {
            Assert.Equal(expected, _latinAmerican.ToXSampa(word));
        }

        [Theory]
        [InlineData("zapato", "Ta\"pato")]
        [InlineData("cielo", "\"Tjelo")]
        [InlineData("acción", "ak\"Tjon")]
        public void ToXSampa_Castilian_ReturnsExpectedOutput(string word, string expected)
        {
            Assert.Equal(expected, _castilian.ToXSampa(word));
        }

        [Theory]
        [InlineData("uva", "\"uBa")]
        [InlineData("mismo", "\"mizmo")]
        [InlineData("yema", "\"J\\j\\ema")]
        [InlineData("adonde", "a\"Don_dde")]
        public void ToXSampa_EnableAllophones_UsesExpectedSymbols(string word, string expected)
        {
            Assert.Equal(expected, _allophonic.ToXSampa(word));
        }

        [Fact]
        public void ToXSampaWithoutStress_OmitsStressMark()
        {
            Assert.Equal("kamino", _latinAmerican.ToXSampaWithoutStress("camino"));
            Assert.DoesNotContain("\"", _latinAmerican.ToXSampaWithoutStress("camino"));
        }

        [Fact]
        public void ToXSampaBatch_ReturnsOutputsInOrder()
        {
            var results = _latinAmerican.ToXSampaBatch(new[] { "casa", "cielo", "show" });

            Assert.Equal(new[] { "\"kasa", "\"sjelo", "\"Sow" }, results);
        }

        [Fact]
        public void ToXSampa_OutputIsAsciiOnly()
        {
            var result = _allophonic.ToXSampa("inyección show adonde");

            Assert.All(result.ToCharArray(), c =>
                Assert.True(c < 128, $"Non-ASCII character found: U+{(int)c:X4} '{c}'"));
        }

        [Fact]
        public void ToXSampa_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new SpanishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaWithoutStress("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(new[] { "casa" }));
        }

        public void Dispose()
        {
            _latinAmerican.Dispose();
            _castilian.Dispose();
            _allophonic.Dispose();
        }
    }
}
