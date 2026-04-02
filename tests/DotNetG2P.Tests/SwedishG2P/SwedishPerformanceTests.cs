using System;
using System.Collections.Generic;
using System.Diagnostics;
using DotNetG2P.Swedish;
using DotNetG2P.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語G2Pのパフォーマンステスト。
    /// </summary>
    [Trait("Category", "Performance")]
    public class SwedishPerformanceTests : IDisposable
    {
        private readonly SwedishG2PEngine _engine = new SwedishG2PEngine();
        private readonly ITestOutputHelper _output;

        public SwedishPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose() => _engine.Dispose();

        [Fact]
        public void Initialization_CompletesWithinThreshold()
        {
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 200, relaxedThreshold: 500);

            var sw = Stopwatch.StartNew();
            using var engine = new SwedishG2PEngine();
            sw.Stop();

            _output.WriteLine($"初期化時間: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"初期化が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ShortText_ConversionWithinThreshold()
        {
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 20, relaxedThreshold: 50);

            // ウォームアップ
            _engine.ToIPA("hej");

            var sw = Stopwatch.StartNew();
            _engine.ToIPA("det här är en kort text med tio ord ungefär");
            sw.Stop();

            _output.WriteLine($"単文変換時間: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"単文変換が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void BatchConversion_100Texts_WithinThreshold()
        {
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 200, relaxedThreshold: 500);

            var texts = new List<string>();
            for (var i = 0; i < 100; i++)
                texts.Add("det här är en kort text med tio ord ungefär");

            // ウォームアップ
            _engine.ToIPA("hej");

            var sw = Stopwatch.StartNew();
            _engine.ToIPABatch(texts);
            sw.Stop();

            _output.WriteLine($"バッチ100件変換時間: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"バッチ100件が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void DictionaryLookup_1000Times_Fast()
        {
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 500, relaxedThreshold: 1000);

            // ウォームアップ
            _engine.ToIPA("hej");

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
                _engine.ToIPA("station");
            sw.Stop();

            _output.WriteLine($"1000回辞書ルックアップ時間: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs,
                $"1000回ルックアップが閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void AfterDispose_ThrowsObjectDisposedException()
        {
            var engine = new SwedishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("hej"));
        }

        [Fact]
        public void FinlandSwedish_ConversionWorks()
        {
            using var engine = new SwedishG2PEngine(new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish));
            var result = engine.ToIPA("bord");
            Assert.NotEmpty(result);
            _output.WriteLine($"FinlandSwedish 'bord': {result}");
        }
    }
}
