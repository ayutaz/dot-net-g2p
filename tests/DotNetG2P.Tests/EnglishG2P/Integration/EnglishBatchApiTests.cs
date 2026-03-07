using System;
using System.Collections.Generic;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// EnglishG2PEngine バッチAPIテスト。
    /// null引数・空リスト・混合入力の動作を検証する。
    /// </summary>
    public class EnglishBatchApiTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EnglishBatchApiTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // ===== ToPhonemesBatch =====

        [Fact]
        public void ToPhonemesBatch_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
        }

        [Fact]
        public void ToPhonemesBatch_EmptyList_ReturnsEmptyList()
        {
            var result = _engine.ToPhonemesBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToPhonemesBatch_MixedInput_HandlesAllElements()
        {
            var texts = new string[] { "hello", "", null!, "world" };
            var result = _engine.ToPhonemesBatch(texts);

            Assert.Equal(4, result.Count);
            Assert.Equal("HH AH0 L OW1", result[0]);       // 通常単語
            Assert.Equal("", result[1]);                      // 空文字列
            Assert.Equal("", result[2]);                      // null
            Assert.Equal("W ER1 L D", result[3]);             // 通常単語
        }

        [Fact]
        public void ToPhonemesBatch_MultipleTexts_ReturnsCorrectCount()
        {
            var texts = new string[] { "hello", "world", "test" };
            var result = _engine.ToPhonemesBatch(texts);

            Assert.Equal(3, result.Count);
            foreach (var r in result)
            {
                Assert.NotEmpty(r);
            }
        }

        // ===== ToIPABatch =====

        [Fact]
        public void ToIPABatch_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
        }

        [Fact]
        public void ToIPABatch_EmptyList_ReturnsEmptyList()
        {
            var result = _engine.ToIPABatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToIPABatch_MixedInput_HandlesAllElements()
        {
            var texts = new string[] { "hello", "", null! };
            var result = _engine.ToIPABatch(texts);

            Assert.Equal(3, result.Count);
            Assert.NotEmpty(result[0]);  // IPA出力
            Assert.Equal("", result[1]); // 空文字列
            Assert.Equal("", result[2]); // null
        }

        // ===== ToXSampaBatch =====

        [Fact]
        public void ToXSampaBatch_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToXSampaBatch(null!));
        }

        [Fact]
        public void ToXSampaBatch_EmptyList_ReturnsEmptyList()
        {
            var result = _engine.ToXSampaBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToXSampaBatch_MixedInput_HandlesAllElements()
        {
            var texts = new string[] { "hello", "", null! };
            var result = _engine.ToXSampaBatch(texts);

            Assert.Equal(3, result.Count);
            Assert.NotEmpty(result[0]);
            Assert.Equal("", result[1]);
            Assert.Equal("", result[2]);
        }

        // ===== ToPhonemeListBatch =====

        [Fact]
        public void ToPhonemeListBatch_NullArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemeListBatch(null!));
        }

        [Fact]
        public void ToPhonemeListBatch_EmptyList_ReturnsEmptyList()
        {
            var result = _engine.ToPhonemeListBatch(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ToPhonemeListBatch_MixedInput_HandlesAllElements()
        {
            var texts = new string[] { "hello", "", null! };
            var result = _engine.ToPhonemeListBatch(texts);

            Assert.Equal(3, result.Count);
            Assert.NotEmpty(result[0]);  // EnglishPhoneme列
            Assert.Empty(result[1]);     // 空
            Assert.Empty(result[2]);     // null
        }

        // ===== バッチ結果と個別結果の一貫性 =====

        [Fact]
        public void ToPhonemesBatch_ConsistentWithIndividualCalls()
        {
            var texts = new string[] { "hello", "world", "computer" };
            var batchResult = _engine.ToPhonemesBatch(texts);

            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToPhonemes(texts[i]);
                Assert.Equal(individual, batchResult[i]);
            }
        }

        [Fact]
        public void ToIPABatch_ConsistentWithIndividualCalls()
        {
            var texts = new string[] { "hello", "world" };
            var batchResult = _engine.ToIPABatch(texts);

            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToIPA(texts[i]);
                Assert.Equal(individual, batchResult[i]);
            }
        }

        [Fact]
        public void ToXSampaBatch_ConsistentWithIndividualCalls()
        {
            var texts = new string[] { "hello", "world" };
            var batchResult = _engine.ToXSampaBatch(texts);

            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToXSampa(texts[i]);
                Assert.Equal(individual, batchResult[i]);
            }
        }

        // ===== Dispose後のバッチAPI呼び出し =====

        [Fact]
        public void ToPhonemesBatch_AfterDispose_ThrowsObjectDisposedException()
        {
            // Dispose済みエンジンでバッチAPIを呼び出すとObjectDisposedException
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToPhonemesBatch(new[] { "hello" }));
        }

        [Fact]
        public void ToIPABatch_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToIPABatch(new[] { "hello" }));
        }

        [Fact]
        public void ToXSampaBatch_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToXSampaBatch(new[] { "hello" }));
        }

        [Fact]
        public void ToPhonemeListBatch_AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => engine.ToPhonemeListBatch(new[] { "hello" }));
        }
    }
}
