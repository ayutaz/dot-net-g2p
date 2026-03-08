using System;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishEdgeCaseTests : IDisposable
    {
        private readonly SpanishG2PEngine _engine = new SpanishG2PEngine();

        [Fact]
        public void PunctuationOnly_ReturnsEmptyOutputs()
        {
            Assert.Equal(string.Empty, _engine.ToIPA("...!!!"));
            Assert.Equal(string.Empty, _engine.ToXSampa("...!!!"));
            Assert.Empty(_engine.ToPhonemeList("...!!!"));
        }

        [Fact]
        public void BatchApis_EmptyInput_ReturnEmptyCollection()
        {
            Assert.Empty(_engine.ToPhonemesBatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToIPABatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToXSampaBatch(Array.Empty<string>()));
        }

        [Fact]
        public void BatchApis_Null_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToXSampaBatch(null!));
        }

        [Fact]
        public void BatchAndSingleApis_ReturnSameResults()
        {
            var texts = new[] { "México", "guion", "show", "cielo" };
            var batchIpa = _engine.ToIPABatch(texts);
            var batchXsampa = _engine.ToXSampaBatch(texts);

            for (var i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToIPA(texts[i]), batchIpa[i]);
                Assert.Equal(_engine.ToXSampa(texts[i]), batchXsampa[i]);
            }
        }

        [Fact]
        public void IncludeStressFalse_AffectsXSampaOutput()
        {
            using var engine = new SpanishG2PEngine(new SpanishG2POptions(includeStress: false));

            Assert.Equal("kamino", engine.ToXSampa("camino"));
            Assert.Equal("kamino", engine.ToXSampaWithoutStress("camino"));
        }

        [Fact]
        public void MixedCaseAndFullWidthDigits_NormalizeConsistently()
        {
            var ipa = _engine.ToIPA("  MÉXICO ５G WIFI ");
            var xsampa = _engine.ToXSampa("  MÉXICO ５G WIFI ");

            Assert.Equal("ˈmexiko ˈɡ ˈwifi", ipa);
            Assert.Equal("\"mexiko \"g \"wifi", xsampa);
        }

        [Fact]
        public void ToSyllables_Whitespace_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToSyllables("   "));
        }

        public void Dispose() => _engine.Dispose();
    }
}
