using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.French;
using DotNetG2P.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.FrenchG2P
{
    [Trait("Category", "Performance")]
    public class FrenchPerformanceTests : IDisposable
    {
        private readonly FrenchG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public FrenchPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: true));
        }

        [Fact]
        public void Constructor_RepeatedLoads_StayWithinThreshold()
        {
            var times = new long[5];
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 100, relaxedThreshold: 500);

            for (var i = 0; i < times.Length; i++)
            {
                var sw = Stopwatch.StartNew();
                using var engine = new FrenchG2PEngine();
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;

                Assert.False(string.IsNullOrEmpty(engine.ToIPA("bonjour")));
            }

            var average = times.Average();
            _output.WriteLine($"初期化時間(5回): {string.Join(", ", times.Select(t => $"{t}ms"))} 平均: {average:F1}ms");
            Assert.True(average < thresholdMs, $"平均初期化時間が閾値({thresholdMs}ms)を超過: {average:F1}ms");
        }

        [Fact]
        public void ToIPA_ShortSentence_10000Times_CompletesQuickly()
        {
            const string text = "bonjour le monde";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
                _engine.ToIPA(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                _engine.ToIPA(text);
            sw.Stop();

            _output.WriteLine($"短文IPA 10000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"短文10000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToXSampa_LongSentence_2000Times_CompletesQuickly()
        {
            const string text = "le petit prince voyageait à travers les étoiles en cherchant des amis dans l'univers infini et magnifique";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2500, relaxedThreshold: 10000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
                _engine.ToXSampa(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 2000; i++)
                _engine.ToXSampa(text);
            sw.Stop();

            _output.WriteLine($"長文X-SAMPA 2000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"長文X-SAMPA 2000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToXSampaBatch_5000Items_IsComparableToLoop()
        {
            var texts = Enumerable.Range(0, 5000).Select(_ => "château français").ToArray();

            // ウォームアップ
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
        public void ToIPABatch_100Items_IsComparableToLoop()
        {
            var texts = Enumerable.Range(0, 100).Select(_ => "aujourd'hui nous allons au marché").ToArray();

            // ウォームアップ
            _engine.ToIPABatch(texts.Take(5).ToArray());

            var swBatch = Stopwatch.StartNew();
            var batchResults = _engine.ToIPABatch(texts);
            swBatch.Stop();

            var swLoop = Stopwatch.StartNew();
            var loopResults = new string[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                loopResults[i] = _engine.ToIPA(texts[i]);
            swLoop.Stop();

            _output.WriteLine($"IPAバッチ100件: バッチ={swBatch.ElapsedMilliseconds}ms, ループ={swLoop.ElapsedMilliseconds}ms");
            Assert.Equal(loopResults, batchResults);
        }

        [Fact]
        public void ExceptionDictionary_Lookup_10000Times_CompletesQuickly()
        {
            using var engine = new FrenchG2PEngine(new FrenchG2POptions(enableExceptionDictionary: true));
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
                engine.ToIPA("monsieur");

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                engine.ToIPA("monsieur");
            sw.Stop();

            _output.WriteLine($"例外辞書ルックアップ10000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"例外辞書10000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
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
                _engine.ToIPA("le petit prince voyageait à travers les étoiles en cherchant des amis");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var after = GC.GetTotalMemory(true);
            var diffMb = (after - before) / (1024.0 * 1024.0);

            _output.WriteLine($"処理前: {before / (1024.0 * 1024.0):F2}MB, 処理後: {after / (1024.0 * 1024.0):F2}MB, 差分: {diffMb:F2}MB");
            Assert.True(diffMb < thresholdMb, $"メモリ増加が閾値({thresholdMb:F0}MB)を超過: {diffMb:F2}MB");
        }

        [Fact]
        public void ToPhonemeList_ShortWord_10000Times_CompletesQuickly()
        {
            const string text = "merci beaucoup";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
                _engine.ToPhonemeList(text);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                _engine.ToPhonemeList(text);
            sw.Stop();

            _output.WriteLine($"音素リスト10000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"音素リスト10000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToSyllables_10000Times_CompletesQuickly()
        {
            const string word = "extraordinaire";
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
                _engine.ToSyllables(word);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 10000; i++)
                _engine.ToSyllables(word);
            sw.Stop();

            _output.WriteLine($"音節分割10000回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < thresholdMs, $"音節分割10000回が閾値({thresholdMs}ms)を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void WithAndWithoutAllophones_BothCompleteQuickly()
        {
            using var withAllophones = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: true));
            using var withoutAllophones = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: false));
            const string text = "les enfants jouent dans le jardin";
            const int iterations = 5000;
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 2000, relaxedThreshold: 8000);

            // ウォームアップ
            for (var w = 0; w < 10; w++)
            {
                withAllophones.ToIPA(text);
                withoutAllophones.ToIPA(text);
            }

            var swWith = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                withAllophones.ToIPA(text);
            swWith.Stop();

            var swWithout = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                withoutAllophones.ToIPA(text);
            swWithout.Stop();

            _output.WriteLine($"異音あり{iterations}回: {swWith.ElapsedMilliseconds}ms, 異音なし{iterations}回: {swWithout.ElapsedMilliseconds}ms");
            Assert.True(swWith.ElapsedMilliseconds < thresholdMs, $"異音あり{iterations}回が閾値({thresholdMs}ms)を超過: {swWith.ElapsedMilliseconds}ms");
            Assert.True(swWithout.ElapsedMilliseconds < thresholdMs, $"異音なし{iterations}回が閾値({thresholdMs}ms)を超過: {swWithout.ElapsedMilliseconds}ms");
        }

        public void Dispose() => _engine.Dispose();
    }
}
