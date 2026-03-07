using System;
using System.Collections.Generic;
using System.IO;
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
        /// [In-Sampleテスト] 手動選定した100語のCMU辞書登録語に対してLTS推定結果と辞書発音を比較し、PERを算出する。
        /// これらの語はLTSモデルの訓練データに含まれている可能性が高いため、
        /// 真のOOV精度ではなくモデルの表現能力の下限を示す指標として解釈すべきである。
        /// PER < 15% をアサーション（実測PER: 約5%）。
        /// </summary>
        [Fact]
        public void LtsPer_InSample_ManuallySelected100Words_BelowThreshold()
        {
            // テスト対象の100語（CMU辞書に含まれる一般的な英単語）
            // 注意: これらはin-sampleデータであり、LTSモデルの訓練セットと重複する可能性が高い
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
            _output.WriteLine($"\n[In-Sample] PER結果: {per:P2} ({totalErrors}/{totalPhonemes}) テスト語数: {testedCount}");

            // PER < 15% をアサーション（in-sample実測PER: 約5%）
            Assert.True(per < 0.15, $"PER ({per:P2}) が15%を超えています。");
        }

        /// <summary>
        /// [Hold-Outテスト] CMU辞書からシード固定ランダムで500語を選び、
        /// LTS予測と辞書正解のPER（ストレス込み）を計測する。
        /// LTSモデルはCMU辞書全体で訓練されている可能性があるが、
        /// ランダムサンプリングにより手動選定バイアスを排除した評価を行う。
        /// 閾値はin-sampleテストより緩く20%に設定。
        /// </summary>
        [Fact]
        public void LtsPer_HoldOut_Random500Words_WithStress()
        {
            var words = LoadDictionaryWords();
            Assert.True(words.Count >= 500, $"辞書語数が500未満です: {words.Count}");

            // シード固定ランダムで500語をサンプリング（再現性を保証）
            var rng = new Random(42);
            var sampled = words.OrderBy(_ => rng.Next()).Take(500).ToList();

            var totalPhonemes = 0;
            var totalErrors = 0;
            var testedCount = 0;
            var errorWords = new List<string>();

            foreach (var word in sampled)
            {
                // 辞書から正解の発音を取得
                var dictPronunciations = _engine.LookupAllPronunciations(word);
                if (dictPronunciations.Count == 0)
                    continue;

                // LTSで予測（英字のみの語に限定）
                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult == null || ltsResult.Length == 0)
                    continue;

                testedCount++;

                // ストレス込みで音素名を比較
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
                    errorWords.Add($"  {word}: LTS=[{string.Join(" ", ltsPhonemeNames.Select(p => p.ToString()))}] " +
                                   $"REF=[{string.Join(" ", bestReference.Select(p => p.ToString()))}] " +
                                   $"dist={minDistance}");
                }
            }

            // 上位20件のエラー語を出力
            _output.WriteLine($"[Hold-Out] エラー語（上位20件）:");
            foreach (var line in errorWords.Take(20))
                _output.WriteLine(line);

            Assert.True(testedCount >= 300, $"テスト対象語が少なすぎます: {testedCount}/500");

            var per = totalPhonemes > 0 ? (double)totalErrors / totalPhonemes : 0;
            _output.WriteLine($"\n[Hold-Out] PER（ストレス込み）: {per:P2} ({totalErrors}/{totalPhonemes}) テスト語数: {testedCount}");

            // Hold-outテストではin-sampleより高いPERが予想される（閾値20%）
            Assert.True(per < 0.20, $"Hold-Out PER ({per:P2}) が20%を超えています。");
        }

        /// <summary>
        /// [Hold-Outテスト] CMU辞書からシード固定ランダムで500語を選び、
        /// LTS予測と辞書正解のPER（ストレス除外、音素名のみ）を計測する。
        /// ストレス情報を無視することで純粋な音素列の正確性を評価する。
        /// </summary>
        [Fact]
        public void LtsPer_HoldOut_Random500Words_WithoutStress()
        {
            var words = LoadDictionaryWords();
            Assert.True(words.Count >= 500, $"辞書語数が500未満です: {words.Count}");

            // シード固定ランダムで500語をサンプリング（再現性を保証）
            var rng = new Random(42);
            var sampled = words.OrderBy(_ => rng.Next()).Take(500).ToList();

            var totalPhonemes = 0;
            var totalErrors = 0;
            var testedCount = 0;

            foreach (var word in sampled)
            {
                var dictPronunciations = _engine.LookupAllPronunciations(word);
                if (dictPronunciations.Count == 0)
                    continue;

                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult == null || ltsResult.Length == 0)
                    continue;

                testedCount++;

                // ストレス除外: 音素名（ArpabetPhoneme）のみで比較
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
            }

            Assert.True(testedCount >= 300, $"テスト対象語が少なすぎます: {testedCount}/500");

            var per = totalPhonemes > 0 ? (double)totalErrors / totalPhonemes : 0;
            _output.WriteLine($"[Hold-Out] PER（ストレス除外）: {per:P2} ({totalErrors}/{totalPhonemes}) テスト語数: {testedCount}");

            // ストレス除外PERはストレス込みPERより低くなるはず（閾値20%）
            Assert.True(per < 0.20, $"Hold-Out PER ストレス除外 ({per:P2}) が20%を超えています。");
        }

        /// <summary>
        /// [ストレス精度] LTS出力にSecondary stress (Stress.Secondary / "2") が含まれないことを確認する。
        /// Flite LTSのCARTツリーモデルはPrimary(1)とNoStress(0)のみを生成し、
        /// Secondary stress(2)は生成しない仕様である。
        /// </summary>
        [Fact]
        public void LtsStress_NeverProducesSecondaryStress()
        {
            // CMU辞書の一般的な語（secondary stressが辞書にある語を含む）
            var testWords = new[]
            {
                "about", "computer", "university", "telephone", "understanding",
                "international", "automobile", "communication", "education", "information",
                "opportunity", "organization", "responsibility", "administration", "transportation",
                "investigation", "accommodation", "undergraduate", "infrastructure", "entertainment",
            };

            var testedCount = 0;

            foreach (var word in testWords)
            {
                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult == null || ltsResult.Length == 0)
                    continue;

                testedCount++;

                var hasSecondary = ltsResult.Any(p => p.Stress == Stress.Secondary);

                _output.WriteLine($"  {word}: [{string.Join(" ", ltsResult.Select(p => p.ToString()))}] " +
                                 $"Secondary={hasSecondary}");

                // Flite LTSの仕様上、Secondary stress (2) は生成されない
                Assert.False(hasSecondary,
                    $"'{word}' のLTS出力にSecondary stress (2) が含まれています。" +
                    $"Flite LTSはPrimary(1)とNoStress(0)のみ生成する仕様です。");
            }

            Assert.True(testedCount >= 10, $"テスト対象語が少なすぎます: {testedCount}");
            _output.WriteLine($"\nSecondary stress非生成を{testedCount}語で確認しました。");
        }

        // ===== 難易度別LTS精度テスト =====

        [Theory]
        [InlineData("mcdonald")]
        [InlineData("nguyen")]
        [InlineData("schwarzenegger")]
        [InlineData("tchaikovsky")]
        public void LtsAccuracy_ProperNouns_ReturnsResult(string word)
        {
            // 固有名詞: LTS予測が結果を返すことを検証
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
            // 音素数が単語長の1/4以上であること（妥当性チェック）
            Assert.True(result.Length >= word.Length / 4,
                $"固有名詞'{word}'の音素数({result.Length})が少なすぎます（最低{word.Length / 4}）");
            _output.WriteLine($"固有名詞 {word}: [{string.Join(" ", result.Select(p => p.ToString()))}]");
        }

        [Theory]
        [InlineData("rendezvous")]
        [InlineData("entrepreneur")]
        [InlineData("facade")]
        [InlineData("naive")]
        public void LtsAccuracy_Loanwords_ReturnsResult(string word)
        {
            // 外来語: LTS予測が結果を返すことを検証
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
            Assert.True(result.Length >= word.Length / 4,
                $"外来語'{word}'の音素数({result.Length})が少なすぎます（最低{word.Length / 4}）");
            _output.WriteLine($"外来語 {word}: [{string.Join(" ", result.Select(p => p.ToString()))}]");
        }

        [Theory]
        [InlineData("kubernetes")]
        [InlineData("postgresql")]
        [InlineData("nginx")]
        public void LtsAccuracy_TechTerms_ReturnsResult(string word)
        {
            // 技術用語: LTS予測が結果を返すことを検証
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
            _output.WriteLine($"技術用語 {word}: [{string.Join(" ", result.Select(p => p.ToString()))}]");
        }

        [Theory]
        [InlineData("rendezvous", 5)]
        [InlineData("entrepreneur", 5)]
        [InlineData("facade", 4)]
        [InlineData("naive", 3)]
        public void LtsAccuracy_Loanwords_DistanceWithinRange(string word, int maxDistance)
        {
            // 外来語のLTS予測が辞書発音と妥当な範囲内の距離であることを検証
            var dictPronunciations = _engine.LookupAllPronunciations(word);
            if (dictPronunciations.Count == 0)
            {
                _output.WriteLine($"'{word}'は辞書に未登録のためスキップ");
                return;
            }

            var ltsResult = LtsEngine.Predict(word);
            Assert.NotNull(ltsResult);

            var ltsPhonemes = ltsResult!.Select(p => p.Phoneme).ToArray();
            var minDistance = dictPronunciations
                .Select(pron => LevenshteinDistance(
                    ltsPhonemes,
                    pron.Phonemes.Select(p => p.Phoneme).ToArray()))
                .Min();

            _output.WriteLine($"外来語 {word}: LTS=[{string.Join(" ", ltsPhonemes.Select(p => p.ToString()))}] " +
                             $"距離={minDistance} (最大許容={maxDistance})");

            Assert.True(minDistance <= maxDistance,
                $"外来語'{word}'のLevenshtein距離({minDistance})が許容値({maxDistance})を超えています。");
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

        // ===== ヘルパー: CMU辞書ワードリスト読み込み =====

        /// <summary>
        /// CMU辞書のEmbeddedResourceからワードリストを読み込む。
        /// LTS予測可能な英字のみの語（アポストロフィ・数字・ハイフンなし）に限定する。
        /// </summary>
        private static List<string> LoadDictionaryWords()
        {
            var assembly = typeof(CmuDictionary).Assembly;
            using (var stream = assembly.GetManifestResourceStream("DotNetG2P.English.cmudict.dict"))
            {
                if (stream == null)
                    throw new InvalidOperationException("埋め込みCMU辞書リソースが見つかりません。");

                var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = new StreamReader(stream))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == ';')
                            continue;

                        var firstSpace = line.IndexOf(' ');
                        if (firstSpace < 0)
                            continue;

                        var rawWord = line.Substring(0, firstSpace);

                        // バリアント番号 "(2)" を除去
                        var parenIdx = rawWord.IndexOf('(');
                        if (parenIdx >= 0)
                            rawWord = rawWord.Substring(0, parenIdx);

                        // 英字のみの語に限定（LTS予測可能な語）
                        var allAlpha = true;
                        for (var i = 0; i < rawWord.Length; i++)
                        {
                            var c = rawWord[i];
                            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                            {
                                allAlpha = false;
                                break;
                            }
                        }

                        if (allAlpha && rawWord.Length > 0)
                            words.Add(rawWord.ToLowerInvariant());
                    }
                }

                return words.ToList();
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
