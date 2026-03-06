using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Conversion
{
    /// <summary>
    /// EnglishG2PEngine のバッチAPI テスト。
    /// ToPhonemesBatch / ToIPABatch / ToXSampaBatch / ToPhonemeListBatch を検証する。
    /// </summary>
    public class BatchApiTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public BatchApiTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // =====================================================================
        // ToPhonemesBatch
        // =====================================================================

        [Fact]
        public void ToPhonemesBatch_MultipleTexts_ReturnsCorrectCount()
        {
            var texts = new[] { "hello", "world", "computer" };
            var results = _engine.ToPhonemesBatch(texts);
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.False(string.IsNullOrEmpty(r)));
        }

        [Fact]
        public void ToPhonemesBatch_EmptyList_ReturnsEmptyResult()
        {
            var results = _engine.ToPhonemesBatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void ToPhonemesBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
        }

        [Fact]
        public void ToPhonemesBatch_ResultsMatchIndividualCalls()
        {
            var texts = new[] { "hello world", "the quick brown fox" };
            var batchResults = _engine.ToPhonemesBatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToPhonemes(texts[i]), batchResults[i]);
            }
        }

        // =====================================================================
        // ToIPABatch
        // =====================================================================

        [Fact]
        public void ToIPABatch_MultipleTexts_ReturnsIpaStrings()
        {
            var texts = new[] { "hello", "world", "computer" };
            var results = _engine.ToIPABatch(texts);
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.False(string.IsNullOrEmpty(r)));
        }

        [Fact]
        public void ToIPABatch_EmptyList_ReturnsEmptyResult()
        {
            var results = _engine.ToIPABatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void ToIPABatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
        }

        [Fact]
        public void ToIPABatch_ResultsMatchIndividualCalls()
        {
            var texts = new[] { "hello world", "the quick brown fox" };
            var batchResults = _engine.ToIPABatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToIPA(texts[i]), batchResults[i]);
            }
        }

        // =====================================================================
        // ToXSampaBatch
        // =====================================================================

        [Fact]
        public void ToXSampaBatch_MultipleTexts_ReturnsXSampaStrings()
        {
            var texts = new[] { "hello", "world", "computer" };
            var results = _engine.ToXSampaBatch(texts);
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.False(string.IsNullOrEmpty(r)));
        }

        [Fact]
        public void ToXSampaBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToXSampaBatch(null!));
        }

        [Fact]
        public void ToXSampaBatch_ResultsMatchIndividualCalls()
        {
            var texts = new[] { "hello world", "the quick brown fox" };
            var batchResults = _engine.ToXSampaBatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToXSampa(texts[i]), batchResults[i]);
            }
        }

        // =====================================================================
        // ToPhonemeListBatch
        // =====================================================================

        [Fact]
        public void ToPhonemeListBatch_MultipleTexts_ReturnsPhonemeListsOfLists()
        {
            var texts = new[] { "hello", "world", "computer" };
            var results = _engine.ToPhonemeListBatch(texts);
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.True(r.Count > 0));
        }

        [Fact]
        public void ToPhonemeListBatch_ResultsMatchIndividualCalls()
        {
            var texts = new[] { "hello world", "the quick brown fox" };
            var batchResults = _engine.ToPhonemeListBatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToPhonemeList(texts[i]);
                Assert.Equal(individual.Count, batchResults[i].Count);
                for (int j = 0; j < individual.Count; j++)
                {
                    Assert.Equal(individual[j], batchResults[i][j]);
                }
            }
        }

        // =====================================================================
        // バッチ共通
        // =====================================================================

        [Fact]
        public void BatchApis_AfterDispose_ThrowObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            var texts = new[] { "hello" };
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(texts));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(texts));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(texts));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeListBatch(texts));
        }

        [Fact]
        public void BatchApis_WithEmptyStringElement_ReturnsEmptyForThatElement()
        {
            var texts = new[] { "hello", "", "world" };

            var phonemesResults = _engine.ToPhonemesBatch(texts);
            Assert.Equal(3, phonemesResults.Count);
            Assert.False(string.IsNullOrEmpty(phonemesResults[0]));
            Assert.Equal("", phonemesResults[1]);
            Assert.False(string.IsNullOrEmpty(phonemesResults[2]));

            var ipaResults = _engine.ToIPABatch(texts);
            Assert.Equal(3, ipaResults.Count);
            Assert.Equal("", ipaResults[1]);

            var xsampaResults = _engine.ToXSampaBatch(texts);
            Assert.Equal(3, xsampaResults.Count);
            Assert.Equal("", xsampaResults[1]);

            var listResults = _engine.ToPhonemeListBatch(texts);
            Assert.Equal(3, listResults.Count);
            Assert.Empty(listResults[1]);
        }
    }
}
