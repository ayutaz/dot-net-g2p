using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotNetG2P.MeCab;
using DotNetG2P.NMeCab;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// MeCabTokenizerのパフォーマンステスト。
    /// CI環境の変動を考慮し、余裕のある閾値を設定する。
    /// </summary>
    [Trait("Category", "Performance")]
    public class PerformanceTests : IDisposable
    {
        private readonly MeCabTokenizer? _mecabTokenizer;
        private readonly NMeCabTokenizer? _nmecabTokenizer;
        private readonly ITestOutputHelper _output;
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            if (DictionaryExists)
            {
                _mecabTokenizer = new MeCabTokenizer(DicPath!);
                _nmecabTokenizer = new NMeCabTokenizer(DicPath!);
            }
        }

        public void Dispose()
        {
            _mecabTokenizer?.Dispose();
            _nmecabTokenizer?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません");
        }

        [SkippableFact]
        public void Tokenize_ShortText_CompletesQuickly()
        {
            SkipIfNoDictionary();
            // ウォームアップ
            _mecabTokenizer!.Tokenize("テスト");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                _mecabTokenizer.Tokenize("こんにちは");
            sw.Stop();

            _output.WriteLine($"短文100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
            Assert.True(sw.ElapsedMilliseconds < 5000, $"短文100回が5秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void Tokenize_StandardText_CompletesQuickly()
        {
            SkipIfNoDictionary();
            var text = "東京から大阪まで新幹線で行きます";
            _mecabTokenizer!.Tokenize(text); // ウォームアップ

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                _mecabTokenizer.Tokenize(text);
            sw.Stop();

            _output.WriteLine($"標準文100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"標準文100回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void Tokenize_LongText_CompletesReasonably()
        {
            SkipIfNoDictionary();
            var text = string.Concat(Enumerable.Repeat("東京タワーに行きたいです。", 10));
            _mecabTokenizer!.Tokenize(text); // ウォームアップ

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
                _mecabTokenizer.Tokenize(text);
            sw.Stop();

            _output.WriteLine($"長文({text.Length}文字)10回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"長文10回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void Tokenize_RepeatedCalls_ConsistentResults()
        {
            SkipIfNoDictionary();
            var text = "音声合成の研究";
            var firstResult = _mecabTokenizer!.Tokenize(text);

            for (int i = 0; i < 100; i++)
            {
                var result = _mecabTokenizer.Tokenize(text);
                Assert.Equal(firstResult.Count, result.Count);
                for (int j = 0; j < firstResult.Count; j++)
                    Assert.Equal(firstResult[j].Surface, result[j].Surface);
            }
        }

        [SkippableFact]
        public void Tokenize_MeCabNotSlowerThan5xNMeCab()
        {
            SkipIfNoDictionary();
            var texts = new[] { "こんにちは", "東京タワーに行きたい", "今日は天気がいいですね", "音声合成の研究" };

            // ウォームアップ
            foreach (var t in texts)
            {
                _mecabTokenizer!.Tokenize(t);
                _nmecabTokenizer!.Tokenize(t);
            }

            var swMeCab = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                foreach (var t in texts)
                    _mecabTokenizer!.Tokenize(t);
            swMeCab.Stop();

            var swNMeCab = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                foreach (var t in texts)
                    _nmecabTokenizer!.Tokenize(t);
            swNMeCab.Stop();

            var ratio = (double)swMeCab.ElapsedMilliseconds / Math.Max(1, swNMeCab.ElapsedMilliseconds);
            _output.WriteLine($"MeCab: {swMeCab.ElapsedMilliseconds}ms, NMeCab: {swNMeCab.ElapsedMilliseconds}ms, Ratio: {ratio:F2}x");

            Assert.True(ratio < 5.0, $"MeCabがNMeCabの5倍以上遅い: {ratio:F2}x");
        }
    }
}
