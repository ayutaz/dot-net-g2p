using System;
using System.Linq;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishG2PEngineTests : IDisposable
    {
        private readonly SpanishG2PEngine _engine = new SpanishG2PEngine();

        [Fact]
        public void ToPhonemes_MultiWord_ReturnsWordsSeparatedBySpaces()
        {
            var result = _engine.ToPhonemes("hola mundo");

            Assert.Equal("ˈo l a ˈm u n d o", result);
        }

        [Fact]
        public void ToIPA_Normalization_StripsPunctuationAndLowercases()
        {
            var result = _engine.ToIPA("  ¡Hola, MUNDO!  ");

            Assert.Equal("ˈola ˈmundo", result);
        }

        [Fact]
        public void ToPhonemeList_ReturnsFlattenedPhonemes()
        {
            var result = _engine.ToPhonemeList("casa");

            Assert.Equal(4, result.Count);
            Assert.Equal(new[] { "k", "a", "s", "a" }, result.Select(p => p.ToString()).ToArray());
        }

        [Fact]
        public void ToSyllables_ReturnsStressFlags()
        {
            var syllables = _engine.ToSyllables("camino");

            Assert.Equal(new[] { "ca", "mi", "no" }, syllables.Select(s => s.Text).ToArray());
            Assert.Equal(new[] { false, true, false }, syllables.Select(s => s.IsStressed).ToArray());
        }

        [Fact]
        public void BatchApis_ReturnExpectedOutputs()
        {
            var phonemes = _engine.ToPhonemesBatch(new[] { "casa", "cielo" });
            var ipa = _engine.ToIPABatch(new[] { "zapato", "queso" });

            Assert.Equal(new[] { "ˈk a s a", "ˈs j e l o" }, phonemes);
            Assert.Equal(new[] { "saˈpato", "ˈkeso" }, ipa);
        }

        [Theory]
        [InlineData("y", "i")]
        [InlineData("guion", "ɡiˈon")]
        [InlineData("truhan", "tɾuˈan")]
        [InlineData("whisky", "ˈwiski")]
        [InlineData("wifi", "ˈwifi")]
        [InlineData("show", "ˈʃow")]
        [InlineData("México", "ˈmexiko")]
        [InlineData("software", "sofˈweɾ")]
        [InlineData("shampoo", "ʃamˈpu")]
        [InlineData("hockey", "ˈxokej")]
        [InlineData("whatsapp", "waˈsap")]
        [InlineData("sándwich", "ˈsanwitʃ")]
        [InlineData("Oaxaca", "waˈxaka")]
        [InlineData("Ximena", "xiˈmena")]
        [InlineData("ketchup", "ˈketʃup")]
        [InlineData("croissant", "kɾwaˈsan")]
        [InlineData("jetlag", "xetˈlaɡ")]
        [InlineData("podcast", "podˈkast")]
        [InlineData("yonqui", "ˈʝonki")]
        [InlineData("google", "ɡuˈɣel")]
        public void ToIPA_ExceptionCases_ReturnExpectedOutput(string text, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(text));
        }

        [Fact]
        public void IncludeStressFalse_OmitsStressMarks()
        {
            using var engine = new SpanishG2PEngine(new SpanishG2POptions(includeStress: false));

            Assert.Equal("kamino", engine.ToIPA("camino"));
            Assert.Equal("k a m i n o", engine.ToPhonemes("camino"));
        }

        [Fact]
        public void EmptyInput_ReturnsEmptyOutputs()
        {
            Assert.Equal("", _engine.ToIPA(""));
            Assert.Equal("", _engine.ToPhonemes(" "));
            Assert.Empty(_engine.ToPhonemeList(null!));
            Assert.Empty(_engine.ToSyllables(""));
        }

        [Fact]
        public void AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new SpanishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("casa"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToSyllables("casa"));
        }

        public void Dispose() => _engine.Dispose();
    }
}
