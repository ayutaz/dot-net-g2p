using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 複合語・形態素（活用形）の処理精度を計測するテスト。
    /// 各カテゴリの成功率をコンソール出力で確認する。
    /// </summary>
    public class CompoundMorphologyErrorRateTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public CompoundMorphologyErrorRateTests(ITestOutputHelper output)
        {
            _engine = new EnglishG2PEngine();
            _output = output;
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // テスト1: 複合語（ハイフン付き）
        // =====================================================================

        [Fact]
        public void HyphenatedCompounds_ShouldProduceNonEmptyPhonemes()
        {
            var compounds = new[]
            {
                "well-known", "self-esteem", "long-term", "high-quality", "real-time",
                "up-to-date", "state-of-the-art", "mother-in-law", "over-the-counter",
                "one-on-one", "face-to-face", "day-to-day", "step-by-step", "word-of-mouth",
                "top-notch", "cold-blooded", "open-minded", "short-lived", "hard-working",
                "good-looking"
            };

            _output.WriteLine("=== テスト1: ハイフン付き複合語 ===");
            _output.WriteLine($"{"単語",-25} {"結果",-10} {"音素出力"}");
            _output.WriteLine(new string('-', 80));

            var successCount = 0;
            var failedWords = new List<string>();

            foreach (var word in compounds)
            {
                var result = _engine.ToPhonemes(word);
                var hasOutput = !string.IsNullOrWhiteSpace(result);

                if (hasOutput)
                {
                    successCount++;
                    _output.WriteLine($"{word,-25} {"OK",-10} {result}");
                }
                else
                {
                    failedWords.Add(word);
                    _output.WriteLine($"{word,-25} {"EMPTY",-10} (音素生成なし)");
                }
            }

            var rate = (double)successCount / compounds.Length * 100;
            _output.WriteLine(new string('-', 80));
            _output.WriteLine($"成功率: {successCount}/{compounds.Length} ({rate:F1}%)");

            if (failedWords.Count > 0)
            {
                _output.WriteLine($"失敗単語: {string.Join(", ", failedWords)}");
            }

            _output.WriteLine("");

            // 少なくとも1つ以上の複合語が音素生成に成功することを確認
            Assert.True(successCount > 0, "ハイフン付き複合語の音素生成が全て失敗");
        }

        // =====================================================================
        // テスト2: 接頭辞付き複合語（ハイフンなし）
        // =====================================================================

        [Fact]
        public void PrefixedCompounds_DictionaryAndLtsCoverage()
        {
            var words = new[]
            {
                "undo", "redo", "preorder", "misunderstand", "overwork", "underestimate",
                "disconnect", "rewrite", "unhappy", "nonprofit", "coworker", "multitask",
                "semicircle", "anticlockwise", "subway", "outperform", "upload", "download",
                "overload", "underpay"
            };

            _output.WriteLine("=== テスト2: 接頭辞付き複合語（ハイフンなし） ===");
            _output.WriteLine($"{"単語",-20} {"辞書",-8} {"音素生成",-10} {"音素出力"}");
            _output.WriteLine(new string('-', 90));

            var totalCount = words.Length;
            var dictHitCount = 0;
            var ltsSuccessCount = 0;
            var emptyCount = 0;
            var ltsWords = new List<string>();
            var emptyWords = new List<string>();

            foreach (var word in words)
            {
                var phonemes = _engine.ToPhonemes(word);
                var inDict = _engine.ContainsWord(word);
                var hasOutput = !string.IsNullOrWhiteSpace(phonemes);

                string source;
                if (inDict)
                {
                    dictHitCount++;
                    source = "辞書";
                }
                else if (hasOutput)
                {
                    ltsSuccessCount++;
                    ltsWords.Add(word);
                    source = "LTS";
                }
                else
                {
                    emptyCount++;
                    emptyWords.Add(word);
                    source = "なし";
                }

                _output.WriteLine($"{word,-20} {(inDict ? "YES" : "NO"),-8} {source,-10} {(hasOutput ? phonemes : "(空)")}");
            }

            _output.WriteLine(new string('-', 90));
            _output.WriteLine($"辞書ヒット: {dictHitCount}/{totalCount} ({(double)dictHitCount / totalCount * 100:F1}%)");
            _output.WriteLine($"LTSフォールバック成功: {ltsSuccessCount}/{totalCount} ({(double)ltsSuccessCount / totalCount * 100:F1}%)");
            _output.WriteLine($"音素生成なし: {emptyCount}/{totalCount} ({(double)emptyCount / totalCount * 100:F1}%)");
            _output.WriteLine($"音素生成成功率(合計): {dictHitCount + ltsSuccessCount}/{totalCount} ({(double)(dictHitCount + ltsSuccessCount) / totalCount * 100:F1}%)");

            if (ltsWords.Count > 0)
            {
                _output.WriteLine($"LTSフォールバック単語: {string.Join(", ", ltsWords)}");
            }

            if (emptyWords.Count > 0)
            {
                _output.WriteLine($"音素生成失敗単語: {string.Join(", ", emptyWords)}");
            }

            _output.WriteLine("");

            // 少なくとも1つ以上の接頭辞付き複合語が音素生成に成功することを確認
            Assert.True(dictHitCount + ltsSuccessCount > 0, "接頭辞付き複合語の音素生成が全て失敗");
        }

        // =====================================================================
        // テスト3: 活用形（形態素解析が必要なケース）
        // =====================================================================

        [Fact]
        public void InflectedForms_DictionaryAndLtsCoverage()
        {
            var words = new[]
            {
                "walked", "walking", "talked", "talking", "played", "playing",
                "worked", "working", "looked", "looking", "jumped", "jumping",
                "opened", "opening", "started", "starting", "wanted", "wanting",
                "needed", "needing", "happened", "happening", "considered", "considering",
                "established", "establishing", "demonstrated", "demonstrating",
                "communicated", "communicating", "unfortunately", "beautifully",
                "internationally", "approximately", "significantly"
            };

            _output.WriteLine("=== テスト3: 活用形・派生語 ===");
            _output.WriteLine($"{"単語",-20} {"辞書",-8} {"音素生成",-10} {"音素出力"}");
            _output.WriteLine(new string('-', 100));

            var totalCount = words.Length;
            var dictHitCount = 0;
            var ltsSuccessCount = 0;
            var emptyCount = 0;
            var dictWords = new List<string>();
            var ltsWords = new List<string>();
            var emptyWords = new List<string>();

            foreach (var word in words)
            {
                var phonemes = _engine.ToPhonemes(word);
                var inDict = _engine.ContainsWord(word);
                var hasOutput = !string.IsNullOrWhiteSpace(phonemes);

                string source;
                if (inDict)
                {
                    dictHitCount++;
                    dictWords.Add(word);
                    source = "辞書";
                }
                else if (hasOutput)
                {
                    ltsSuccessCount++;
                    ltsWords.Add(word);
                    source = "LTS";
                }
                else
                {
                    emptyCount++;
                    emptyWords.Add(word);
                    source = "なし";
                }

                _output.WriteLine($"{word,-20} {(inDict ? "YES" : "NO"),-8} {source,-10} {(hasOutput ? phonemes : "(空)")}");
            }

            _output.WriteLine(new string('-', 100));
            _output.WriteLine($"辞書ヒット: {dictHitCount}/{totalCount} ({(double)dictHitCount / totalCount * 100:F1}%)");
            _output.WriteLine($"LTSフォールバック: {ltsSuccessCount}/{totalCount} ({(double)ltsSuccessCount / totalCount * 100:F1}%)");
            _output.WriteLine($"音素生成なし: {emptyCount}/{totalCount} ({(double)emptyCount / totalCount * 100:F1}%)");
            _output.WriteLine($"音素生成成功率(合計): {dictHitCount + ltsSuccessCount}/{totalCount} ({(double)(dictHitCount + ltsSuccessCount) / totalCount * 100:F1}%)");

            if (dictWords.Count > 0)
            {
                _output.WriteLine($"辞書ヒット単語: {string.Join(", ", dictWords)}");
            }

            if (ltsWords.Count > 0)
            {
                _output.WriteLine($"LTSフォールバック単語: {string.Join(", ", ltsWords)}");
            }

            if (emptyWords.Count > 0)
            {
                _output.WriteLine($"音素生成失敗単語: {string.Join(", ", emptyWords)}");
            }

            _output.WriteLine("");

            // 少なくとも1つ以上の活用形が音素生成に成功することを確認
            Assert.True(dictHitCount + ltsSuccessCount > 0, "活用形・派生語の音素生成が全て失敗");
        }
    }
}
