using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishG2PEngineTests : IDisposable
    {
        private readonly SwedishG2PEngine _engine = new SwedishG2PEngine();

        public void Dispose() => _engine.Dispose();

        [Fact]
        public void Constructor_Default_DoesNotThrow()
        {
            using var engine = new SwedishG2PEngine();
            Assert.NotNull(engine);
        }

        [Fact]
        public void Constructor_WithOptions_DoesNotThrow()
        {
            using var engine = new SwedishG2PEngine(new SwedishG2POptions());
            Assert.NotNull(engine);
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SwedishG2PEngine(null!));
        }

        [Fact]
        public void ToPhonemes_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes(""));
        }

        [Fact]
        public void ToPhonemes_Null_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes(null!));
        }

        [Theory]
        [InlineData("hej", "\u02C8he\u02D0j")]
        [InlineData("ja", "\u02C8j\u0251\u02D0")]
        public void ToIPA_BasicWords_ReturnsExpectedIPA(string input, string expected)
        {
            var result = _engine.ToIPA(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToIPA_ReturnsNonEmptyString()
        {
            var result = _engine.ToIPA("hej");
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void ToIPAWithoutStress_DoesNotContainStressMarker()
        {
            var result = _engine.ToIPAWithoutStress("hej");
            Assert.DoesNotContain("\u02C8", result);
        }

        [Fact]
        public void ToPhonemeList_ReturnsNonEmptyList()
        {
            var result = _engine.ToPhonemeList("hej");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToSyllables_ReturnsNonEmptyList()
        {
            var result = _engine.ToSyllables("huset");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemesBatch_MultipleWords_ReturnsCorrectCount()
        {
            var result = _engine.ToPhonemesBatch(new[] { "hej", "ja" });
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ToIPABatch_MultipleWords_ReturnsCorrectCount()
        {
            var result = _engine.ToIPABatch(new[] { "hej", "ja" });
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Dispose_ThenToPhonemes_ThrowsObjectDisposedException()
        {
            _engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _engine.ToPhonemes("hej"));
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _engine.Dispose();
            _engine.Dispose(); // 二重Disposeは例外なし
        }
    }
}
