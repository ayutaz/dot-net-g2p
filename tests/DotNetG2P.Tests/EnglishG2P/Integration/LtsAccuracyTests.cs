using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using DotNetG2P.English.LTS;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// LTS精度評価テスト。
    /// CMU辞書の既知語に対してLTS推定結果と辞書発音をLevenshtein距離で比較し、
    /// PER（Phoneme Error Rate）を計測する。
    /// </summary>
    public class LtsAccuracyTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public LtsAccuracyTests(ITestOutputHelper output)
        {
            _engine = new EnglishG2PEngine();
            _output = output;
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        /// <summary>
        /// CMU辞書から100語をサンプルし、LTS推定結果と辞書発音を比較してPERを算出する。
        /// PER < 15% をアサーション（実測PER: 5.26%）。
        /// </summary>
        [Fact]
        public void LtsPer_SampledWords_BelowThreshold()
        {
            // テスト対象の100語（CMU辞書に含まれる一般的な英単語）
            var testWords = new[]
            {
                "about", "after", "again", "air", "also", "always", "animal", "answer", "around", "away",
                "back", "ball", "because", "before", "began", "begin", "below", "between", "big", "body",
                "book", "both", "boy", "bring", "build", "call", "came", "carry", "change", "children",
                "city", "close", "cold", "come", "could", "country", "cut", "day", "different", "does",
                "door", "down", "draw", "earth", "eat", "end", "enough", "even", "every", "example",
                "face", "family", "far", "father", "feet", "find", "first", "food", "form", "found",
                "give", "good", "got", "great", "green", "group", "grow", "hand", "hard", "head",
                "help", "here", "high", "home", "house", "idea", "important", "inside", "just", "keep",
                "kind", "king", "land", "large", "last", "later", "learn", "letter", "life", "light",
                "line", "long", "look", "made", "make", "man", "many", "money", "morning", "mother",
            };

            var totalPhonemes = 0;
            var totalErrors = 0;
            var testedCount = 0;

            foreach (var word in testWords)
            {
                // 辞書から正解の発音を取得
                var dictPronunciations = _engine.LookupAllPronunciations(word);
                if (dictPronunciations.Count == 0)
                    continue;

                // LTSで予測
                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult == null || ltsResult.Length == 0)
                    continue;

                testedCount++;

                // 辞書の全バリアントに対して最小のLevenshtein距離を取る
                var ltsPhonemeNames = ltsResult.Select(p => p.Phoneme).ToArray();
                var minDistance = int.MaxValue;
                ArpabetPhoneme[]? bestReference = null;

                foreach (var pron in dictPronunciations)
                {
                    var refPhonemes = pron.Phonemes.Select(p => p.Phoneme).ToArray();
                    var dist = LevenshteinDistance(ltsPhonemeNames, refPhonemes);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestReference = refPhonemes;
                    }
                }

                totalErrors += minDistance;
                totalPhonemes += bestReference!.Length;

                if (minDistance > 0)
                {
                    _output.WriteLine($"  {word}: LTS=[{string.Join(" ", ltsPhonemeNames.Select(p => p.ToString()))}] " +
                                     $"REF=[{string.Join(" ", bestReference.Select(p => p.ToString()))}] " +
                                     $"dist={minDistance}");
                }
            }

            Assert.True(testedCount >= 80, $"テスト対象語が少なすぎます: {testedCount}/100");

            var per = totalPhonemes > 0 ? (double)totalErrors / totalPhonemes : 0;
            _output.WriteLine($"\nPER結果: {per:P2} ({totalErrors}/{totalPhonemes}) テスト語数: {testedCount}");

            // PER < 15% をアサーション（実測PER: 5.26% (20/380), 100語サンプル）
            Assert.True(per < 0.15, $"PER ({per:P2}) が15%を超えています。");
        }

        // ===== 特定単語の精度チェック =====

        [Theory]
        [InlineData("hello", 2)]
        [InlineData("world", 2)]
        [InlineData("computer", 3)]
        [InlineData("water", 2)]
        [InlineData("paper", 2)]
        public void LtsAccuracy_SpecificWords_CloseToDict(string word, int maxDistance)
        {
            // 辞書から正解を取得
            var dictPronunciations = _engine.LookupAllPronunciations(word);
            Assert.NotEmpty(dictPronunciations);

            // LTSで予測
            var ltsResult = LtsEngine.Predict(word);
            Assert.NotNull(ltsResult);

            var ltsPhonemes = ltsResult!.Select(p => p.Phoneme).ToArray();

            // 全バリアントに対する最小距離を計算
            var minDistance = dictPronunciations
                .Select(pron => LevenshteinDistance(
                    ltsPhonemes,
                    pron.Phonemes.Select(p => p.Phoneme).ToArray()))
                .Min();

            _output.WriteLine($"{word}: LTS=[{string.Join(" ", ltsPhonemes.Select(p => p.ToString()))}] " +
                             $"距離={minDistance} (最大許容={maxDistance})");

            Assert.True(minDistance <= maxDistance,
                $"'{word}' のLevenshtein距離({minDistance})が許容値({maxDistance})を超えています。");
        }

        // ===== 音素数の妥当性テスト =====

        [Theory]
        [InlineData("cat", 2, 5)]
        [InlineData("hello", 3, 7)]
        [InlineData("computer", 5, 12)]
        [InlineData("beautiful", 5, 12)]
        [InlineData("university", 7, 16)]
        public void LtsAccuracy_PhonemeCount_InReasonableRange(string word, int minPhonemes, int maxPhonemes)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.InRange(result!.Length, minPhonemes, maxPhonemes);
        }

        // ===== 基本的なパターンの一貫性テスト =====

        [Fact]
        public void LtsAccuracy_SameWordDifferentCalls_SameResult()
        {
            // 同じ単語を複数回呼び出しても同じ結果を返すことを検証
            var result1 = LtsEngine.Predict("testing");
            var result2 = LtsEngine.Predict("testing");

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1!.Length, result2!.Length);
            for (var i = 0; i < result1.Length; i++)
            {
                Assert.Equal(result1[i], result2[i]);
            }
        }

        [Fact]
        public void LtsAccuracy_SimilarWords_SimilarResults()
        {
            // "cat" と "bat" は最初の子音だけ異なるはず
            var catResult = LtsEngine.Predict("cat");
            var batResult = LtsEngine.Predict("bat");

            Assert.NotNull(catResult);
            Assert.NotNull(batResult);

            // 音素数が同じであること
            Assert.Equal(catResult!.Length, batResult!.Length);

            // 最初の子音が異なることを検証
            Assert.NotEqual(catResult[0].Phoneme, batResult[0].Phoneme);

            // 残りの音素が同じであることを検証
            for (var i = 1; i < catResult.Length; i++)
            {
                Assert.Equal(catResult[i].Phoneme, batResult[i].Phoneme);
            }
        }

        // ===== ヘルパー: Levenshtein距離 =====

        private static int LevenshteinDistance<T>(T[] source, T[] target) where T : struct
        {
            var n = source.Length;
            var m = target.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            var dp = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) dp[i, 0] = i;
            for (var j = 0; j <= m; j++) dp[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = source[i - 1].Equals(target[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[n, m];
        }
    }
}
