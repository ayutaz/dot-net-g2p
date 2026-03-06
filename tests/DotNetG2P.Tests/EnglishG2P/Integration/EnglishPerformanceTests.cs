using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetG2P.English;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// EnglishG2PEngineのパフォーマンステスト。
    /// CI環境の変動を考慮し、余裕のある閾値を設定する。
    /// </summary>
    [Trait("Category", "Performance")]
    public class EnglishPerformanceTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public EnglishPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new EnglishG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // ===== 1. 辞書ロード時間 =====

        [Fact]
        public void DictionaryLoad_CompletesWithinTimeLimit()
        {
            var sw = Stopwatch.StartNew();
            using var engine = new EnglishG2PEngine();
            sw.Stop();

            _output.WriteLine($"辞書ロード時間: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 5000,
                $"辞書ロードが{sw.ElapsedMilliseconds}msかかりました（閾値: 5000ms）");
        }

        [Fact]
        public void DictionaryLoad_RepeatedLoads_StableTime()
        {
            var times = new long[3];

            for (var i = 0; i < 3; i++)
            {
                var sw = Stopwatch.StartNew();
                using var engine = new EnglishG2PEngine();
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;
            }

            var avg = times.Average();
            var max = times.Max();
            _output.WriteLine($"3回ロード時間: {times[0]}ms, {times[1]}ms, {times[2]}ms (平均: {avg:F1}ms)");

            Assert.True(avg < 10000,
                $"辞書ロード平均が{avg:F1}msかかりました（閾値: 10000ms）");
            // 最大値が平均の3倍を超えないことで安定性を確認
            Assert.True(max < avg * 3 + 500,
                $"辞書ロード最大値({max}ms)が平均({avg:F1}ms)に対して不安定です");
        }

        // ===== 2. 単語変換スループット =====

        [Fact]
        public void ToPhonemes_CommonWord_1000Times_CompletesQuickly()
        {
            // ウォームアップ
            _engine.ToPhonemes("hello");

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
            {
                _engine.ToPhonemes("hello");
            }
            sw.Stop();

            _output.WriteLine($"一般単語1000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 1000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"一般単語1000回が{sw.ElapsedMilliseconds}msかかりました（閾値: 1000ms）");
        }

        [Fact]
        public void ToPhonemes_ShortSentence_1000Times_CompletesQuickly()
        {
            var sentence = "I love you so much";
            // ウォームアップ
            _engine.ToPhonemes(sentence);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
            {
                _engine.ToPhonemes(sentence);
            }
            sw.Stop();

            _output.WriteLine($"短文(5単語)1000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 1000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"短文1000回が{sw.ElapsedMilliseconds}msかかりました（閾値: 3000ms）");
        }

        [Fact]
        public void ToPhonemes_LongSentence_100Times_CompletesReasonably()
        {
            var sentence = "The quick brown fox jumps over the lazy dog and then runs around the big red barn looking for food";
            // ウォームアップ
            _engine.ToPhonemes(sentence);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++)
            {
                _engine.ToPhonemes(sentence);
            }
            sw.Stop();

            _output.WriteLine($"長文(20+単語)100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"長文100回が{sw.ElapsedMilliseconds}msかかりました（閾値: 3000ms）");
        }

        // ===== 3. LTS変換速度 =====

        [Fact]
        public void LtsConversion_MadeUpWords_CompletesReasonably()
        {
            var madeUpWords = new[]
            {
                "blorft", "snazzle", "grumpkin", "flibbert", "quozzle",
                "plonkify", "zibbledy", "wunkster", "crizmond", "drapple",
                "frobnitz", "glarbage", "hunkster", "jibblify", "klonkle",
                "mornblat", "niffledy", "pronkify", "qwibster", "razzledy",
                "slonkify", "trinzoid", "unblorft", "vronkish", "wazzledy",
                "xylphoid", "yibbledy", "zorkster", "abcdefgh", "bcderfgh",
                "florpest", "grimbald", "huckster", "jazzlefy", "kerfloop",
                "limbster", "mumblist", "norfledy", "opsilant", "pretzeld",
                "quilbent", "ristbane", "slothbed", "trombist", "uglified",
                "vexingly", "waftsmen", "xerstoid", "yelpford", "zinbrake",
                "ackplord", "blunfish", "crompied", "draxlift", "elfwhist",
                "frugbelt", "glimbark", "humpwald", "ickblest", "junkflip",
                "krampeld", "loftbend", "muskbird", "nerdflop", "oxbridge",
                "plumwist", "quadfish", "rushbelt", "salkweed", "trunkbed",
                "umpfield", "voltmesh", "waspbird", "xerdfish", "yawnbell",
                "zedfield", "almblest", "brickpod", "crestfud", "driftbel",
                "elmsgate", "frostbid", "grumpfed", "heltbird", "inskleft",
                "jumpfild", "kestbird", "lumpfeld", "monkbird", "nestfish",
                "oatfield", "punkbird", "questfed", "riskbelt", "silkbird",
                "tuskfeld", "ulmfield", "vestbird", "wiskfeld", "xerfbird",
            };

            // ウォームアップ（LTSモデル初期化を含む）
            _engine.ToPhonemes("xyzzyplugh");

            var sw = Stopwatch.StartNew();
            foreach (var word in madeUpWords)
            {
                _engine.ToPhonemes(word);
            }
            sw.Stop();

            _output.WriteLine($"造語{madeUpWords.Length}語のLTS変換: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / (double)madeUpWords.Length:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"造語{madeUpWords.Length}語のLTS変換が{sw.ElapsedMilliseconds}msかかりました（閾値: 3000ms）");
        }

        [Fact]
        public void LtsConversion_SlowerThanDictionaryLookup()
        {
            var dictWords = new[] { "hello", "world", "computer", "beautiful", "understanding" };
            var oovWords = new[] { "blorft", "snazzle", "grumpkin", "flibbert", "quozzle" };
            const int iterations = 500;

            // ウォームアップ
            foreach (var w in dictWords) _engine.ToPhonemes(w);
            foreach (var w in oovWords) _engine.ToPhonemes(w);

            // 辞書単語の測定
            var swDict = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                foreach (var word in dictWords)
                    _engine.ToPhonemes(word);
            }
            swDict.Stop();

            // LTS単語の測定
            var swLts = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                foreach (var word in oovWords)
                    _engine.ToPhonemes(word);
            }
            swLts.Stop();

            _output.WriteLine($"辞書ルックアップ {dictWords.Length * iterations}回: {swDict.ElapsedMilliseconds}ms");
            _output.WriteLine($"LTS変換 {oovWords.Length * iterations}回: {swLts.ElapsedMilliseconds}ms");
            _output.WriteLine($"LTS/辞書 比率: {(double)swLts.ElapsedMilliseconds / Math.Max(swDict.ElapsedMilliseconds, 1):F2}x");

            // LTSは辞書ルックアップより遅いことを確認（期待される動作）
            // CI環境やフルスイート実行時はGC/他テストの影響でタイミングが不安定になるため、
            // 十分な差が出る場合のみ比較する
            if (swDict.ElapsedMilliseconds > 50)
            {
                Assert.True(swLts.ElapsedMilliseconds >= swDict.ElapsedMilliseconds,
                    $"LTS変換({swLts.ElapsedMilliseconds}ms)が辞書ルックアップ({swDict.ElapsedMilliseconds}ms)より速いのは想定外です");
            }
            else
            {
                _output.WriteLine($"辞書ルックアップが{swDict.ElapsedMilliseconds}ms（閾値50ms以下）のため比較スキップ（どちらも十分高速）");
            }
        }

        // ===== 4. メモリ使用量 =====

        [Fact]
        public void MemoryUsage_EngineCreation_WithinReasonableRange()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memBefore = GC.GetTotalMemory(true);

            using var engine = new EnglishG2PEngine();
            // 軽い操作でデータを確実にロードさせる
            engine.ToPhonemes("hello");

            // forceFullCollection=true でGCノイズを低減し、より正確な測定にする
            var memAfter = GC.GetTotalMemory(true);
            var memUsedMb = (memAfter - memBefore) / (1024.0 * 1024.0);

            _output.WriteLine($"エンジン作成後のメモリ増加: {memUsedMb:F2} MB");
            // フルスイート実行時は他テストのGC遅延によりメモリが膨らむことがあるため余裕を持たせる
            Assert.True(memUsedMb < 150,
                $"メモリ使用量が{memUsedMb:F2}MBで閾値(150MB)を超えています");
        }

        [Fact]
        public void MemoryUsage_AfterDispose_MemoryReleased()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memBaseline = GC.GetTotalMemory(true);

            // エンジン作成→使用→Dispose
            var engine = new EnglishG2PEngine();
            engine.ToPhonemes("hello world");
            var memDuring = GC.GetTotalMemory(false);
            engine.Dispose();

            // GCを強制してメモリ解放を促す
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memAfterDispose = GC.GetTotalMemory(true);

            var memDuringMb = (memDuring - memBaseline) / (1024.0 * 1024.0);
            var memAfterMb = (memAfterDispose - memBaseline) / (1024.0 * 1024.0);

            _output.WriteLine($"使用中のメモリ増加: {memDuringMb:F2} MB");
            _output.WriteLine($"Dispose後のメモリ増加: {memAfterMb:F2} MB");

            // Dispose後のメモリが使用中より小さいか、もしくはベースラインに近いことを確認
            // GCの挙動は非決定的なので、大まかな確認のみ
            Assert.True(memAfterMb < memDuringMb + 5,
                $"Dispose後のメモリ({memAfterMb:F2}MB)が使用中({memDuringMb:F2}MB)より大幅に増えています");
        }

        // ===== 5. 並行アクセス =====

        [Fact]
        public async Task ConcurrentAccess_10Threads_1000CallsEach_AllComplete()
        {
            const int threadCount = 10;
            const int callsPerThread = 1000;
            var testWords = new[] { "hello", "world", "computer", "beautiful", "the" };
            var errors = new int[threadCount];
            var barrier = new Barrier(threadCount);

            // ウォームアップ
            foreach (var w in testWords) _engine.ToPhonemes(w);

            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, threadCount).Select(threadIdx => Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (var i = 0; i < callsPerThread; i++)
                {
                    try
                    {
                        var word = testWords[i % testWords.Length];
                        var result = _engine.ToPhonemes(word);
                        if (string.IsNullOrEmpty(result))
                            Interlocked.Increment(ref errors[threadIdx]);
                    }
                    catch
                    {
                        Interlocked.Increment(ref errors[threadIdx]);
                    }
                }
            })).ToArray();

            await Task.WhenAll(tasks);
            sw.Stop();

            var totalErrors = errors.Sum();
            var totalCalls = threadCount * callsPerThread;
            _output.WriteLine($"{threadCount}スレッド x {callsPerThread}回 = 合計{totalCalls}回: {sw.ElapsedMilliseconds}ms");
            _output.WriteLine($"エラー数: {totalErrors}/{totalCalls}");

            Assert.Equal(0, totalErrors);
        }
    }
}
