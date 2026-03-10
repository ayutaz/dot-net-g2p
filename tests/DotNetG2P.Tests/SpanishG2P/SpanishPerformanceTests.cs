using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.Spanish;
using DotNetG2P.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.SpanishG2P
{
    [Trait("Category", "Performance")]
    public class SpanishPerformanceTests : IDisposable
    {
        private readonly SpanishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public SpanishPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new SpanishG2PEngine(new SpanishG2POptions(enableAllophones: true));
        }

        [Fact]
        public void Constructor_RepeatedLoads_StayWithinThreshold()
        {
            var times = new long[5];
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 50, relaxedThreshold: 250);

            for (var i = 0; i < times.Length; i++)
            {
                var sw = Stopwatch.StartNew();
                using var engine = new SpanishG2PEngine();
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;

                Assert.Equal("ˈkasa", engine.ToIPA("casa"));
            }

            var average = times.Average();
            _output.WriteLine($"初期化時間(5回): {string.Join(", ", times.Select(t => $"{t}ms"))} 平均: {average:F1}ms");
            Assert.True(average < thresholdMs, $"平均初期化時間が閾値({thresholdMs}ms)を超過: {average:F1}ms");
        }

        [Fact]
        public void ToIPA_ShortSentence_10000Times_CompletesQuickly()
        {
            const string text = "hola mundo desde méxico";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            for (var w = 0; w < 10; w++)
                _engine.ToIPA(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                _engine.ToIPA(text);
            sw.Stop();

            _output.WriteLine($"短文10000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"短文10000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToXSampa_LongSentence_2000Times_CompletesQuickly()
        {
            const string text = "el pingüino camina alrededor del aeropuerto internacional mientras escucha whisky show y wifi";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2500, relaxedThreshold: 10000);

            for (var w = 0; w < 10; w++)
                _engine.ToXSampa(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 2000; i++)
                _engine.ToXSampa(text);
            sw.Stop();

            _output.WriteLine($"長文XSampa 2000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"長文XSampa 2000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToXSampaBatch_5000Items_IsComparableToLoop()
        {
            var texts = Enumerable.Range(0, 5000).Select(_ => "camino show wifi").ToArray();

            _engine.ToXSampaBatch(texts.Take(10).ToArray());

            var swBatch = Stopwatch.StartNew();
            var batchResults = _engine.ToXSampaBatch(texts);
            swBatch.Stop();

            var swLoop = Stopwatch.StartNew();
            var loopResults = new string[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                loopResults[i] = _engine.ToXSampa(texts[i]);
            swLoop.Stop();

            _output.WriteLine($"バッチ: {swBatch.ElapsedMilliseconds}ms, ループ: {swLoop.ElapsedMilliseconds}ms");
            Assert.Equal(loopResults, batchResults);
            Assert.True(swBatch.ElapsedMilliseconds <= swLoop.ElapsedMilliseconds * 3 + 100,
                $"バッチ({swBatch.ElapsedMilliseconds}ms)がループ({swLoop.ElapsedMilliseconds}ms)より過度に遅い");
        }

        [Fact]
        public void RepeatedProcessing_MemoryGrowth_IsReasonable()
        {
            var thresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 16, relaxedThreshold: 64);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(true);
            for (var i = 0; i < 20000; i++)
                _engine.ToIPA("el pingüino y el whisky llegaron al aeropuerto internacional");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var after = GC.GetTotalMemory(true);
            var diffMb = (after - before) / (1024.0 * 1024.0);

            _output.WriteLine($"処理前: {before / (1024.0 * 1024.0):F2}MB, 処理後: {after / (1024.0 * 1024.0):F2}MB, 差分: {diffMb:F2}MB");
            Assert.True(diffMb < thresholdMb, $"メモリ増加が閾値({thresholdMb:F0}MB)を超過: {diffMb:F2}MB");
        }

        public void Dispose() => _engine.Dispose();
    }
}
