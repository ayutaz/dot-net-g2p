using System;
using System.Diagnostics;
using DotNetG2P.Tests.TestHelpers;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.Multilingual
{
    [Collection(MultilingualSharedCollection.Name)]
    [Trait("Category", "Performance")]
    public class MultilingualKoreanPerformanceTests
    {
        private readonly MultilingualSharedFixture _fixture;
        private readonly ITestOutputHelper _output;

        public MultilingualKoreanPerformanceTests(MultilingualSharedFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [SkippableFact]
        public void MixedSentence_KoreanEnglishChineseFrench_1000Times_CompletesWithinThreshold()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");

            const string text = "안녕하세요 hello 你好 café";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 1200, relaxedThreshold: 6000);

            for (var i = 0; i < 20; i++)
                _fixture.ChineseDefaultEngine!.ToPhonemes(text);

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
                _fixture.ChineseDefaultEngine!.ToPhonemes(text);
            stopwatch.Stop();

            _output.WriteLine($"mixed multilingual 1000x: {stopwatch.ElapsedMilliseconds}ms");
            Assert.True(
                stopwatch.ElapsedMilliseconds < thresholdMs,
                $"Mixed multilingual 1000x exceeded threshold {thresholdMs}ms: {stopwatch.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void MixedSentence_KoreanSpanishPortuguese_MemoryGrowth_IsReasonable()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");

            const string text = "안녕하세요 señor obrigado";
            var thresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 24, relaxedThreshold: 96);

            ForceFullCollection();
            var before = GC.GetTotalMemory(true);

            for (var i = 0; i < 5000; i++)
                _fixture.PortugueseDefaultEngine!.ToPhonemes(text);

            ForceFullCollection();
            var after = GC.GetTotalMemory(true);
            var diffMb = (after - before) / (1024.0 * 1024.0);

            _output.WriteLine($"mixed multilingual memory diff={diffMb:F2}MB");
            Assert.True(diffMb < thresholdMb, $"Mixed multilingual memory growth exceeded threshold {thresholdMb:F0}MB: {diffMb:F2}MB");
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
