using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.Korean;
using DotNetG2P.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.KoreanG2P
{
    [Trait("Category", "Performance")]
    public class KoreanPerformanceTests : IDisposable
    {
        private readonly KoreanG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public KoreanPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new KoreanG2PEngine();
        }

        [Fact]
        public void ToPhonemes_ShortSentence_10000Times_CompletesWithinThreshold()
        {
            const string text = "안녕하세요 저는 한국어 g2p 엔진을 테스트합니다";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 1500, relaxedThreshold: 7000);

            for (var i = 0; i < 20; i++)
                _engine.ToPhonemes(text);

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                _engine.ToPhonemes(text);
            stopwatch.Stop();

            _output.WriteLine($"ToPhonemes 10000x: {stopwatch.ElapsedMilliseconds}ms");
            Assert.True(
                stopwatch.ElapsedMilliseconds < thresholdMs,
                $"ToPhonemes 10000x exceeded threshold {thresholdMs}ms: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToPhonemesBatch_1000Items_IsComparableToLoop()
        {
            var texts = Enumerable.Range(0, 1000)
                .Select(index => index % 2 == 0 ? "좋다 놓고 밟다 담임" : "안녕하세요 저는 한국어 g2p 엔진을 테스트합니다")
                .ToArray();

            _engine.ToPhonemesBatch(texts.Take(10).ToArray());

            var loopStopwatch = Stopwatch.StartNew();
            foreach (var text in texts)
                _engine.ToPhonemes(text);
            loopStopwatch.Stop();

            var batchStopwatch = Stopwatch.StartNew();
            var results = _engine.ToPhonemesBatch(texts);
            batchStopwatch.Stop();

            _output.WriteLine($"loop={loopStopwatch.ElapsedMilliseconds}ms, batch={batchStopwatch.ElapsedMilliseconds}ms");
            Assert.Equal(texts.Length, results.Count);
            Assert.True(
                batchStopwatch.ElapsedMilliseconds <= loopStopwatch.ElapsedMilliseconds * 2 + 200,
                $"Batch conversion is disproportionately slower than loop: batch={batchStopwatch.ElapsedMilliseconds}ms, loop={loopStopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void RepeatedProcessing_MemoryGrowth_IsReasonable()
        {
            const string text = "안녕하세요 저는 한국어 g2p 엔진을 테스트합니다 좋다 놓고 밟다 담임";
            var thresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 16, relaxedThreshold: 64);

            ForceFullCollection();
            var before = GC.GetTotalMemory(true);
            for (var i = 0; i < 10000; i++)
                _engine.ToPhonemes(text);
            ForceFullCollection();
            var after = GC.GetTotalMemory(true);

            var diffMb = (after - before) / (1024.0 * 1024.0);
            _output.WriteLine($"memory before={before / (1024.0 * 1024.0):F2}MB after={after / (1024.0 * 1024.0):F2}MB diff={diffMb:F2}MB");
            Assert.True(diffMb < thresholdMb, $"Memory growth exceeded threshold {thresholdMb:F0}MB: {diffMb:F2}MB");
        }

        [Fact]
        public void RepeatedProcessing_AllocationAndGcProfile_StayWithinBudget()
        {
            const string text = "좋다 놓고 밟다 담임 안녕하세요 저는 한국어 g2p 엔진을 테스트합니다";
            var allocationThresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 64, relaxedThreshold: 256);
            var gen0Threshold = PerformanceThresholds.Milliseconds(strictThreshold: 12, relaxedThreshold: 40);
            var gen1Threshold = PerformanceThresholds.Milliseconds(strictThreshold: 4, relaxedThreshold: 16);
            var gen2Threshold = PerformanceThresholds.Milliseconds(strictThreshold: 1, relaxedThreshold: 6);

            for (var i = 0; i < 200; i++)
                _engine.ToPhonemes(text);

            ForceFullCollection();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            for (var i = 0; i < 5000; i++)
            {
                _engine.ToPhonemes(text);
                _engine.ToJamo(text);
            }

            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            var gen0Delta = GC.CollectionCount(0) - gen0Before;
            var gen1Delta = GC.CollectionCount(1) - gen1Before;
            var gen2Delta = GC.CollectionCount(2) - gen2Before;
            var allocatedMb = (allocatedAfter - allocatedBefore) / (1024.0 * 1024.0);

            _output.WriteLine($"allocated={allocatedMb:F2}MB, gen0={gen0Delta}, gen1={gen1Delta}, gen2={gen2Delta}");
            Assert.True(allocatedMb < allocationThresholdMb, $"Allocated bytes exceeded threshold {allocationThresholdMb:F0}MB: {allocatedMb:F2}MB");
            Assert.True(gen0Delta <= gen0Threshold, $"Gen0 collections exceeded threshold {gen0Threshold}: {gen0Delta}");
            Assert.True(gen1Delta <= gen1Threshold, $"Gen1 collections exceeded threshold {gen1Threshold}: {gen1Delta}");
            Assert.True(gen2Delta <= gen2Threshold, $"Gen2 collections exceeded threshold {gen2Threshold}: {gen2Delta}");
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
