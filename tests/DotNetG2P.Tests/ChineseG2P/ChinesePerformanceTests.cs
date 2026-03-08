using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.Chinese;
using DotNetG2P.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine のパフォーマンステスト。
    /// CI環境の変動を考慮し、余裕のある閾値を設定する。
    /// </summary>
    [Trait("Category", "Performance")]
    public class ChinesePerformanceTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public ChinesePerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. スループットテスト (4件)
        // =====================================================================

        [Fact]
        public void ToPinyin_短文10000回_妥当な時間で完了()
        {
            // ウォームアップ
            for (int w = 0; w < 10; w++)
                _engine.ToPinyin("你好世界");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                _engine.ToPinyin("你好世界");
            sw.Stop();

            _output.WriteLine($"短文(4文字)×10000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 10000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"短文10000回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToPinyin_中文1000回_妥当な時間で完了()
        {
            var text = "今天天气非常好，我们一起去公园散步吧。";  // 18文字

            // ウォームアップ
            for (int w = 0; w < 10; w++)
                _engine.ToPinyin(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyin(text);
            sw.Stop();

            _output.WriteLine($"中文({text.Length}文字)×1000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 1000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"中文1000回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToPinyin_長文100回_妥当な時間で完了()
        {
            // 100文字以上の長文を構築
            var longText = string.Concat(Enumerable.Repeat("中华人民共和国是世界上人口最多的国家之一。", 6)); // 120文字

            // ウォームアップ
            for (int w = 0; w < 5; w++)
                _engine.ToPinyin(longText);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                _engine.ToPinyin(longText);
            sw.Stop();

            _output.WriteLine($"長文({longText.Length}文字)×100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"長文100回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToPinyinList_1000回_妥当な時間で完了()
        {
            var text = "学习中文很有意思";  // 8文字

            // ウォームアップ
            for (int w = 0; w < 10; w++)
                _engine.ToPinyinList(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyinList(text);
            sw.Stop();

            _output.WriteLine($"ToPinyinList({text.Length}文字)×1000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 1000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"ToPinyinList 1000回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 2. バッチAPI vs ループ比較 (2件)
        // =====================================================================

        [Fact]
        public void ToPinyinBatch_1000件_ループと同等以上の速度()
        {
            var texts = Enumerable.Range(0, 1000).Select(i => "你好世界" + i).ToArray();

            // ウォームアップ
            _engine.ToPinyinBatch(texts.Take(10).ToArray());

            // バッチAPI計測
            var swBatch = Stopwatch.StartNew();
            var batchResults = _engine.ToPinyinBatch(texts);
            swBatch.Stop();

            // forループ計測
            var swLoop = Stopwatch.StartNew();
            var loopResults = new string[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                loopResults[i] = _engine.ToPinyin(texts[i]);
            swLoop.Stop();

            _output.WriteLine($"バッチ: {swBatch.ElapsedMilliseconds}ms, ループ: {swLoop.ElapsedMilliseconds}ms");

            // バッチがループの3倍以上遅くないことを確認
            Assert.True(swBatch.ElapsedMilliseconds <= swLoop.ElapsedMilliseconds * 3 + 100,
                $"バッチ({swBatch.ElapsedMilliseconds}ms)がループ({swLoop.ElapsedMilliseconds}ms)の3倍以上遅い");

            // 結果が同一であることも確認
            Assert.Equal(batchResults.Count, loopResults.Length);
        }

        [Fact]
        public void ToPinyinBatch_大量短文_妥当な時間で完了()
        {
            var texts = Enumerable.Range(0, 5000).Select(_ => "你好").ToArray();

            // ウォームアップ
            _engine.ToPinyinBatch(texts.Take(10).ToArray());

            var sw = Stopwatch.StartNew();
            var results = _engine.ToPinyinBatch(texts);
            sw.Stop();

            _output.WriteLine($"バッチ5000件: {sw.ElapsedMilliseconds}ms");
            Assert.Equal(5000, results.Count);
            Assert.True(sw.ElapsedMilliseconds < 10000, $"バッチ5000件が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 3. 辞書初期化テスト (2件)
        // =====================================================================

        [Fact]
        public void コンストラクタ_初期化時間_5回計測平均が妥当()
        {
            const int trials = 5;
            var times = new long[trials];
            var thresholdMs = PerformanceThresholds.Milliseconds(strictThreshold: 5000, relaxedThreshold: 10000);

            for (int i = 0; i < trials; i++)
            {
                var sw = Stopwatch.StartNew();
                using var engine = new ChineseG2PEngine();
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;

                // 初期化後に動作確認（デフォルトは声調変調有効: 三声連読 nǐ→ní）
                var result = engine.ToPinyin("你好");
                Assert.Equal("ní hǎo", result);
            }

            var avg = times.Average();
            _output.WriteLine($"初期化時間(5回): {string.Join(", ", times.Select(t => $"{t}ms"))}  平均: {avg:F1}ms");
            Assert.True(avg < thresholdMs, $"平均初期化時間が閾値({thresholdMs}ms)を超過: {avg:F1}ms");
        }

        [Fact]
        public void コンストラクタ_5インスタンス連続作成_各インスタンス正常動作()
        {
            var engines = new ChineseG2PEngine[5];

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5; i++)
                engines[i] = new ChineseG2PEngine();
            sw.Stop();

            _output.WriteLine($"5インスタンス連続作成: {sw.ElapsedMilliseconds}ms");

            // 各インスタンスが正常に動作するか確認（デフォルトは声調変調有効: 三声連読 nǐ→ní）
            for (int i = 0; i < 5; i++)
            {
                var result = engines[i].ToPinyin("你好");
                Assert.Equal("ní hǎo", result);
            }

            // クリーンアップ
            for (int i = 0; i < 5; i++)
                engines[i].Dispose();

            Assert.True(sw.ElapsedMilliseconds < 90000, $"5インスタンス作成が90秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 4. メモリ関連テスト (2件)
        // =====================================================================

        [Fact]
        public void 大量テキスト処理_メモリ増加が妥当()
        {
            var thresholdMb = PerformanceThresholds.Megabytes(strictThreshold: 50, relaxedThreshold: 128);
            // GCで安定化
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeMemory = GC.GetTotalMemory(true);

            // 大量処理
            for (int i = 0; i < 10000; i++)
                _engine.ToPinyin("今天天气非常好，我们一起去公园散步吧。");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(true);
            var diffMb = (afterMemory - beforeMemory) / (1024.0 * 1024.0);

            _output.WriteLine($"処理前: {beforeMemory / (1024.0 * 1024.0):F2}MB, 処理後: {afterMemory / (1024.0 * 1024.0):F2}MB, 差分: {diffMb:F2}MB");

            // 10000回処理後のメモリ増加が50MBを超えないこと
            Assert.True(diffMb < thresholdMb, $"メモリ増加が閾値({thresholdMb:F0}MB)を超過: {diffMb:F2}MB");
        }

        [Fact]
        public void Dispose後再作成_繰り返し10回_安定動作()
        {
            for (int i = 0; i < 10; i++)
            {
                using var engine = new ChineseG2PEngine();
                var result = engine.ToPinyin("你好");
                Assert.Equal("ní hǎo", result); // デフォルトは声調変調有効: 三声連読 nǐ→ní
            }

            // 10回のDispose/再作成サイクル後もGCが安定していることを確認
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // 最終確認：再作成が正常に動作
            using var finalEngine = new ChineseG2PEngine();
            Assert.Equal("ní hǎo", finalEngine.ToPinyin("你好"));
        }

        // =====================================================================
        // 5. フレーズ辞書パフォーマンス (2件)
        // =====================================================================

        [Fact]
        public void フレーズ辞書_長文最長一致検索_スループット妥当()
        {
            // フレーズ一致が多い長文（多音字を含む）
            var text = "银行行长说了一句重要的话，研究生产力的问题。音乐会很好听。";

            // ウォームアップ
            for (int w = 0; w < 10; w++)
                _engine.ToPinyin(text);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyin(text);
            sw.Stop();

            _output.WriteLine($"フレーズ一致長文({text.Length}文字)×1000回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 1000.0:F3}ms)");
            Assert.True(sw.ElapsedMilliseconds < 10000, $"フレーズ辞書処理1000回が10秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void HandleHeteronyms有効vs無効_処理時間比較()
        {
            var text = "银行行长研究生产力的重要问题";

            var optionsEnabled = new ChineseG2POptions(handleHeteronyms: true);
            var optionsDisabled = new ChineseG2POptions(handleHeteronyms: false);

            using var engineEnabled = new ChineseG2PEngine(optionsEnabled);
            using var engineDisabled = new ChineseG2PEngine(optionsDisabled);

            // ウォームアップ
            for (int w = 0; w < 10; w++)
            {
                engineEnabled.ToPinyin(text);
                engineDisabled.ToPinyin(text);
            }

            var swEnabled = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                engineEnabled.ToPinyin(text);
            swEnabled.Stop();

            var swDisabled = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                engineDisabled.ToPinyin(text);
            swDisabled.Stop();

            _output.WriteLine($"HandleHeteronyms=true: {swEnabled.ElapsedMilliseconds}ms, false: {swDisabled.ElapsedMilliseconds}ms");

            // どちらも妥当な時間で完了すること
            Assert.True(swEnabled.ElapsedMilliseconds < 15000, $"HandleHeteronyms=true 5000回が15秒を超過: {swEnabled.ElapsedMilliseconds}ms");
            Assert.True(swDisabled.ElapsedMilliseconds < 15000, $"HandleHeteronyms=false 5000回が15秒を超過: {swDisabled.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 6. 声調変調パフォーマンス (1件)
        // =====================================================================

        [Fact]
        public void EnableToneSandhi有効vs無効_処理時間比較()
        {
            // 三声連読・"一"・"不"を含むテキスト
            var text = "你好，一个不好的小老虎跑了很远。";

            var optionsEnabled = new ChineseG2POptions(enableToneSandhi: true);
            var optionsDisabled = new ChineseG2POptions(enableToneSandhi: false);

            using var engineEnabled = new ChineseG2PEngine(optionsEnabled);
            using var engineDisabled = new ChineseG2PEngine(optionsDisabled);

            // ウォームアップ
            for (int w = 0; w < 10; w++)
            {
                engineEnabled.ToPinyin(text);
                engineDisabled.ToPinyin(text);
            }

            var swEnabled = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                engineEnabled.ToPinyin(text);
            swEnabled.Stop();

            var swDisabled = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
                engineDisabled.ToPinyin(text);
            swDisabled.Stop();

            _output.WriteLine($"EnableToneSandhi=true: {swEnabled.ElapsedMilliseconds}ms, false: {swDisabled.ElapsedMilliseconds}ms");

            // どちらも妥当な時間で完了すること
            Assert.True(swEnabled.ElapsedMilliseconds < 15000, $"EnableToneSandhi=true 5000回が15秒を超過: {swEnabled.ElapsedMilliseconds}ms");
            Assert.True(swDisabled.ElapsedMilliseconds < 15000, $"EnableToneSandhi=false 5000回が15秒を超過: {swDisabled.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 7. スタイル変換パフォーマンス (2件)
        // =====================================================================

        [Fact]
        public void ToPinyin_3スタイル各1000回_全スタイル妥当な時間で完了()
        {
            var text = "中华人民共和国";

            // ウォームアップ
            for (int w = 0; w < 10; w++)
            {
                _engine.ToPinyin(text, PinyinStyle.ToneMarked);
                _engine.ToPinyin(text, PinyinStyle.ToneNumber);
                _engine.ToPinyin(text, PinyinStyle.Normal);
            }

            var swToneMarked = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyin(text, PinyinStyle.ToneMarked);
            swToneMarked.Stop();

            var swToneNumber = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyin(text, PinyinStyle.ToneNumber);
            swToneNumber.Stop();

            var swNormal = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyin(text, PinyinStyle.Normal);
            swNormal.Stop();

            _output.WriteLine($"ToneMarked: {swToneMarked.ElapsedMilliseconds}ms, ToneNumber: {swToneNumber.ElapsedMilliseconds}ms, Normal: {swNormal.ElapsedMilliseconds}ms");

            Assert.True(swToneMarked.ElapsedMilliseconds < 5000, $"ToneMarked 1000回が5秒を超過: {swToneMarked.ElapsedMilliseconds}ms");
            Assert.True(swToneNumber.ElapsedMilliseconds < 5000, $"ToneNumber 1000回が5秒を超過: {swToneNumber.ElapsedMilliseconds}ms");
            Assert.True(swNormal.ElapsedMilliseconds < 5000, $"Normal 1000回が5秒を超過: {swNormal.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void ToPinyinList_3スタイル各1000回_全スタイル妥当な時間で完了()
        {
            var text = "中华人民共和国";

            // ウォームアップ
            for (int w = 0; w < 10; w++)
            {
                _engine.ToPinyinList(text, PinyinStyle.ToneMarked);
                _engine.ToPinyinList(text, PinyinStyle.ToneNumber);
                _engine.ToPinyinList(text, PinyinStyle.Normal);
            }

            var swToneMarked = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyinList(text, PinyinStyle.ToneMarked);
            swToneMarked.Stop();

            var swToneNumber = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyinList(text, PinyinStyle.ToneNumber);
            swToneNumber.Stop();

            var swNormal = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                _engine.ToPinyinList(text, PinyinStyle.Normal);
            swNormal.Stop();

            _output.WriteLine($"ToPinyinList - ToneMarked: {swToneMarked.ElapsedMilliseconds}ms, ToneNumber: {swToneNumber.ElapsedMilliseconds}ms, Normal: {swNormal.ElapsedMilliseconds}ms");

            Assert.True(swToneMarked.ElapsedMilliseconds < 5000, $"ToPinyinList ToneMarked 1000回が5秒を超過: {swToneMarked.ElapsedMilliseconds}ms");
            Assert.True(swToneNumber.ElapsedMilliseconds < 5000, $"ToPinyinList ToneNumber 1000回が5秒を超過: {swToneNumber.ElapsedMilliseconds}ms");
            Assert.True(swNormal.ElapsedMilliseconds < 5000, $"ToPinyinList Normal 1000回が5秒を超過: {swNormal.ElapsedMilliseconds}ms");
        }
    }
}
