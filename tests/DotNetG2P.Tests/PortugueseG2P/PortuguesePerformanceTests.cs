using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.Portuguese;
using DotNetG2P.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.PortugueseG2P
{
    [Trait("Category", "Performance")]
    public class PortuguesePerformanceTests : IDisposable
    {
        private readonly PortugueseG2PEngine _engine;
        private readonly PortugueseG2PEngine _alloEngine;
        private readonly ITestOutputHelper _output;

        public PortuguesePerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new PortugueseG2PEngine();
            _alloEngine = new PortugueseG2PEngine(new PortugueseG2POptions(enableAllophones: true));
        }

        [Fact]
        public void Constructor_RepeatedLoads_StayWithinThreshold()
        {
            var times = new long[5];
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 100, relaxedThreshold: 500);

            for (var i = 0; i < times.Length; i++)
            {
                var sw = Stopwatch.StartNew();
                using var engine = new PortugueseG2PEngine();
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;

                Assert.Equal("\u02C8kaza", engine.ToIPA("casa"));
            }

            var average = times.Average();
            _output.WriteLine($"\u521D\u671F\u5316\u6642\u9593(5\u56DE): {string.Join(", ", times.Select(t => $"{t}ms"))} \u5E73\u5747: {average:F1}ms");
            Assert.True(average < thresholdMs, $"\u5E73\u5747\u521D\u671F\u5316\u6642\u9593\u304C\u95BE\u5024({thresholdMs}ms)\u3092\u8D85\u904E: {average:F1}ms");
        }

        [Fact]
        public void ToIPA_1000Words_CompletesWithinThreshold()
        {
            const string text = "casa gato mundo bonito";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 1000, relaxedThreshold: 5000);

            // \u30A6\u30A9\u30FC\u30E0\u30A2\u30C3\u30D7
            for (var w = 0; w < 10; w++)
                _engine.ToIPA(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
                _engine.ToIPA(text);
            sw.Stop();

            _output.WriteLine($"1000\u56DE\u5909\u63DB: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"1000\u56DE\u304C\u95BE\u5024({thresholdMs}ms)\u3092\u8D85\u904E: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToIPABatch_100Items_CompletesQuickly()
        {
            var texts = Enumerable.Range(0, 100).Select(_ => "bonito feliz cidade").ToArray();
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 500, relaxedThreshold: 3000);

            // \u30A6\u30A9\u30FC\u30E0\u30A2\u30C3\u30D7
            _engine.ToIPABatch(texts.Take(5).ToArray());

            var sw = Stopwatch.StartNew();
            var results = _engine.ToIPABatch(texts);
            sw.Stop();

            _output.WriteLine($"\u30D0\u30C3\u30C1100\u4EF6: {sw.ElapsedMilliseconds}ms");
            Assert.Equal(100, results.Count);
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"\u30D0\u30C3\u30C1100\u4EF6\u304C\u95BE\u5024({thresholdMs}ms)\u3092\u8D85\u904E: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void AllophonesVsBase_PerformanceComparison()
        {
            const string text = "o menino grande trabalha na escola";
            const int iterations = 500;

            // \u30A6\u30A9\u30FC\u30E0\u30A2\u30C3\u30D7
            for (var w = 0; w < 10; w++)
            {
                _engine.ToIPA(text);
                _alloEngine.ToIPA(text);
            }

            var swBase = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                _engine.ToIPA(text);
            swBase.Stop();

            var swAllo = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                _alloEngine.ToIPA(text);
            swAllo.Stop();

            _output.WriteLine($"\u30D9\u30FC\u30B9{iterations}\u56DE: {swBase.ElapsedMilliseconds}ms, \u7570\u97F3{iterations}\u56DE: {swAllo.ElapsedMilliseconds}ms");

            // \u7570\u97F3\u51E6\u7406\u3042\u308A\u304C\u30D9\u30FC\u30B9\u306E5\u500D\u4EE5\u5185\u3067\u3042\u308B\u3053\u3068
            Assert.True(swAllo.ElapsedMilliseconds <= swBase.ElapsedMilliseconds * 5 + 200,
                $"\u7570\u97F3({swAllo.ElapsedMilliseconds}ms)\u304C\u30D9\u30FC\u30B9({swBase.ElapsedMilliseconds}ms)\u3088\u308A\u904E\u5EA6\u306B\u9045\u3044");
        }

        [Fact]
        public void RepeatedProcessing_MemoryGrowth_IsReasonable()
        {
            var thresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 16, relaxedThreshold: 64);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(true);
            for (var i = 0; i < 10000; i++)
                _engine.ToIPA("o menino grande trabalha na escola todos os dias");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var after = GC.GetTotalMemory(true);
            var diffMb = (after - before) / (1024.0 * 1024.0);

            _output.WriteLine($"\u51E6\u7406\u524D: {before / (1024.0 * 1024.0):F2}MB, \u51E6\u7406\u5F8C: {after / (1024.0 * 1024.0):F2}MB, \u5DEE\u5206: {diffMb:F2}MB");
            Assert.True(diffMb < thresholdMb, $"\u30E1\u30E2\u30EA\u5897\u52A0\u304C\u95BE\u5024({thresholdMb:F0}MB)\u3092\u8D85\u904E: {diffMb:F2}MB");
        }

        public void Dispose()
        {
            _engine.Dispose();
            _alloEngine.Dispose();
        }
    }
}
