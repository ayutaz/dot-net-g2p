using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class AllophoneProcessorTests : IDisposable
    {
        private readonly SpanishG2PEngine _engine = new SpanishG2PEngine(new SpanishG2POptions(enableAllophones: true));

        [Theory]
        [InlineData("uva", "ˈuβa")]
        [InlineData("dedo", "ˈdeðo")]
        [InlineData("lago", "ˈlaɣo")]
        [InlineData("mismo", "ˈmizmo")]
        [InlineData("enfasis", "eɱˈfasis")]
        [InlineData("tengo", "ˈteŋɡo")]
        [InlineData("ancho", "ˈaɲtʃo")]
        public void ToIPA_EnableAllophones_AppliesExpectedVariants(string word, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(word));
        }

        public void Dispose() => _engine.Dispose();
    }
}
