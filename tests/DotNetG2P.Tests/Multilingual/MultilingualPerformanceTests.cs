using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotNetG2P.Multilingual;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// 多言語G2Pパイプラインのパフォーマンステスト。
    /// CI環境の変動を考慮し、閾値には十分な余裕を持たせている。
    /// </summary>
    [Trait("Category", "Performance")]
    public class MultilingualPerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MultilingualG2PEngine? _engine;
        private readonly string? _dictPath;

        public MultilingualPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _dictPath = FindDictPath();
            if (_dictPath != null)
            {
                _engine = new MultilingualG2PEngine(_dictPath);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        private static string? FindDictPath()
        {
            var envPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;
            var candidates = new[]
            {
                @"C:\naist-jdic",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "naist-jdic"),
                "/usr/local/share/naist-jdic",
                "/usr/share/naist-jdic",
            };
            foreach (var path in candidates)
                if (Directory.Exists(path))
                    return path;
            return null;
        }

        // ===== 辞書依存テスト（SkippableFact）=====

        [SkippableFact]
        public void 短文変換_1秒以内()
        {
            Skip.If(_dictPath == null, "naist-jdic辞書が見つかりません");

            var text = "こんにちは world";

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) _engine!.ToPhonemes(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                _engine!.ToPhonemes(text);
            }
            sw.Stop();

            _output.WriteLine($"短文100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"短文100回が1秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void 長文変換_タイムアウトなし()
        {
            Skip.If(_dictPath == null, "naist-jdic辞書が見つかりません");

            // 500文字の日英混在テキストを生成
            var parts = new[]
            {
                "東京タワーに行きたいです。",
                "The weather is nice today. ",
                "新幹線で大阪まで行きます。",
                "I like sushi very much. ",
                "日本語と英語が混在するテキスト。",
                "Machine learning is interesting. ",
                "人工知能の研究が進んでいます。",
                "Natural language processing rocks. ",
            };
            var longText = string.Concat(Enumerable.Repeat(
                string.Concat(parts), 5)).Substring(0, 500);

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) _engine!.ToPhonemes(longText);

            var sw = Stopwatch.StartNew();
            _engine!.ToPhonemes(longText);
            sw.Stop();

            _output.WriteLine($"長文({longText.Length}文字): {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 2500,
                $"500文字の混在テキスト変換が2.5秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void バッチ変換_個別変換と同等速度()
        {
            Skip.If(_dictPath == null, "naist-jdic辞書が見つかりません");

            var texts = Enumerable.Range(0, 100)
                .Select(i => i % 2 == 0 ? "こんにちは world" : "Hello 世界")
                .ToList();

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) _engine!.ToPhonemes(texts[0]);

            // 個別変換の計測
            var swLoop = Stopwatch.StartNew();
            foreach (var text in texts)
            {
                _engine.ToPhonemes(text);
            }
            swLoop.Stop();

            // バッチ変換の計測
            var swBatch = Stopwatch.StartNew();
            _engine.ToPhonemesBatch(texts);
            swBatch.Stop();

            _output.WriteLine($"ループ100件: {swLoop.ElapsedMilliseconds}ms");
            _output.WriteLine($"バッチ100件: {swBatch.ElapsedMilliseconds}ms");

            // バッチがループの3倍以上遅くないことを確認
            Assert.True(swBatch.ElapsedMilliseconds <= swLoop.ElapsedMilliseconds * 3 + 100,
                $"バッチ変換がループ変換の3倍以上遅い: バッチ={swBatch.ElapsedMilliseconds}ms, ループ={swLoop.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void 大量セグメント_パフォーマンス()
        {
            Skip.If(_dictPath == null, "naist-jdic辞書が見つかりません");

            // 言語が頻繁に切り替わるテキスト（50回切り替え）
            var parts = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                parts.Add(i % 2 == 0 ? "日本語" : "English");
            }
            var mixedText = string.Join("", parts);

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) _engine!.ToPhonemes(mixedText);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                _engine!.ToPhonemes(mixedText);
            }
            sw.Stop();

            _output.WriteLine($"頻繁切替テキスト10回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 5000,
                $"大量セグメントテキスト10回が5秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [SkippableFact]
        public void メモリ圧迫なし()
        {
            Skip.If(_dictPath == null, "naist-jdic辞書が見つかりません");

            // GCを強制して初期メモリを計測
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memBefore = GC.GetTotalMemory(true);

            // 大量の変換を実行
            for (int i = 0; i < 500; i++)
            {
                _engine!.ToPhonemes("東京タワーに行きたいです。The weather is nice.");
            }

            // GCを強制して最終メモリを計測
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memAfter = GC.GetTotalMemory(true);

            var memDiffMB = (memAfter - memBefore) / (1024.0 * 1024.0);
            _output.WriteLine($"メモリ差分: {memDiffMB:F2} MB (前={memBefore / 1024.0 / 1024.0:F2}MB, 後={memAfter / 1024.0 / 1024.0:F2}MB)");

            Assert.True(memDiffMB < 100,
                $"500回変換後のメモリ増加が100MBを超過: {memDiffMB:F2}MB");
        }

        // ===== 辞書不要テスト（Fact）=====

        [Fact]
        public void TextSegmenter_大量テキスト_パフォーマンス()
        {
            // 10000文字の日英混在テキスト
            var parts = new[] { "あいうえお", "Hello", "かきくけこ", "World" };
            var repeated = string.Concat(Enumerable.Repeat(string.Concat(parts), 500));
            var text = repeated.Substring(0, 10000);

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) TextSegmenter.Segment(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                TextSegmenter.Segment(text);
            }
            sw.Stop();

            _output.WriteLine($"10000文字テキスト100回セグメント化: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 5000,
                $"10000文字テキスト100回のセグメント化が5秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void TextSegmenter_頻繁言語切替_パフォーマンス()
        {
            // "aあ" を繰り返して5000文字
            var text = string.Concat(Enumerable.Repeat("aあ", 2500));

            // ウォームアップ (JIT Tiered Compilation安定化)
            for (int w = 0; w < 10; w++) TextSegmenter.Segment(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                TextSegmenter.Segment(text);
            }
            sw.Stop();

            _output.WriteLine($"頻繁言語切替5000文字100回: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 5000,
                $"頻繁言語切替5000文字100回のセグメント化が5秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void LanguageDetector_大量文字分類_パフォーマンス()
        {
            // 100万文字の分類
            var chars = new char[1_000_000];
            var sampleChars = new[] { 'あ', 'A', '1', '!', ' ', '漢', 'ア', 'z', '。', 'Ａ' };
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = sampleChars[i % sampleChars.Length];
            }

            // ウォームアップ
            for (int i = 0; i < 1000; i++)
            {
                LanguageDetector.Classify(chars[i]);
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < chars.Length; i++)
            {
                LanguageDetector.Classify(chars[i]);
            }
            sw.Stop();

            _output.WriteLine($"100万文字分類: {sw.ElapsedMilliseconds}ms");
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"100万文字の分類が1秒を超過: {sw.ElapsedMilliseconds}ms");
        }
    }
}
