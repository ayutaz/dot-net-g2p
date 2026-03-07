using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 縮約形およびホモグラフ（同綴異音語）の処理精度を計測するテスト。
    /// </summary>
    public class ContractionHomographErrorRateTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public ContractionHomographErrorRateTests(ITestOutputHelper output)
        {
            _engine = new EnglishG2PEngine();
            _output = output;
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // テスト1: 縮約形の処理精度
        // =====================================================================

        [Fact]
        public void ContractionProcessingAccuracy()
        {
            // 対象縮約形とその展開形（参考用）
            var contractions = new (string contraction, string expandedForm)[]
            {
                ("can't", "cannot"),
                ("won't", "will not"),
                ("don't", "do not"),
                ("didn't", "did not"),
                ("wouldn't", "would not"),
                ("shouldn't", "should not"),
                ("couldn't", "could not"),
                ("isn't", "is not"),
                ("aren't", "are not"),
                ("wasn't", "was not"),
                ("weren't", "were not"),
                ("hasn't", "has not"),
                ("haven't", "have not"),
                ("hadn't", "had not"),
                ("I'm", "I am"),
                ("I've", "I have"),
                ("I'll", "I will"),
                ("I'd", "I would"),
                ("you're", "you are"),
                ("you've", "you have"),
                ("you'll", "you will"),
                ("you'd", "you would"),
                ("he's", "he is"),
                ("she's", "she is"),
                ("it's", "it is"),
                ("we're", "we are"),
                ("we've", "we have"),
                ("we'll", "we will"),
                ("we'd", "we would"),
                ("they're", "they are"),
                ("they've", "they have"),
                ("they'll", "they will"),
                ("they'd", "they would"),
                ("that's", "that is"),
                ("who's", "who is"),
                ("what's", "what is"),
                ("there's", "there is"),
                ("here's", "here is"),
                ("let's", "let us"),
                ("o'clock", "of the clock"),
            };

            var totalCount = contractions.Length;
            var nonEmptyCount = 0;
            var emptyCount = 0;
            var expandedExistsCount = 0;

            _output.WriteLine("=== 縮約形の処理精度テスト ===");
            _output.WriteLine($"対象語数: {totalCount}");
            _output.WriteLine("");
            _output.WriteLine($"{"縮約形",-15} {"音素出力",-40} {"空?",-5} {"展開形",-15} {"展開形辞書登録"}");
            _output.WriteLine(new string('-', 100));

            foreach (var (contraction, expandedForm) in contractions)
            {
                var result = _engine.ToPhonemes(contraction);
                var isEmpty = string.IsNullOrEmpty(result);

                if (!isEmpty)
                    nonEmptyCount++;
                else
                    emptyCount++;

                // 展開形がCMU辞書に存在するかチェック
                // 展開形は複数語の場合があるのでスペースで分割して全語チェック
                var expandedWords = expandedForm.Split(' ');
                var allExpandedExist = expandedWords.All(w => _engine.ContainsWord(w));
                if (allExpandedExist)
                    expandedExistsCount++;

                _output.WriteLine($"{contraction,-15} {(isEmpty ? "(empty)" : result),-40} {(isEmpty ? "Yes" : "No"),-5} {expandedForm,-15} {(allExpandedExist ? "Yes" : "No")}");
            }

            _output.WriteLine("");
            _output.WriteLine("=== 縮約形処理サマリー ===");
            _output.WriteLine($"総数: {totalCount}");
            _output.WriteLine($"音素出力あり: {nonEmptyCount} ({100.0 * nonEmptyCount / totalCount:F1}%)");
            _output.WriteLine($"音素出力なし(空): {emptyCount} ({100.0 * emptyCount / totalCount:F1}%)");
            _output.WriteLine($"展開形が辞書に存在: {expandedExistsCount} / {totalCount}");

            // 少なくとも1つ以上の縮約形が音素生成に成功することを確認
            Assert.True(nonEmptyCount > 0, "縮約形の音素生成が全て失敗");
        }

        // =====================================================================
        // テスト2: ホモグラフの文脈解決精度
        // =====================================================================

        [Fact]
        public void HomographContextResolutionAccuracy()
        {
            // (文, ターゲット単語, 期待される音素部分文字列, 説明)
            var testCases = new (string sentence, string targetWord, string expectedPhonemeSubstring, string description)[]
            {
                // "read" のケース（現在形 vs 過去形）
                // CMU辞書: read[0]=R EH1 D (過去形), read[1]=R IY1 D (現在形)
                // HomographDB: read デフォルト=1(現在形), Verb→1, Noun→1
                ("I read books every day", "read", "R IY1 D", "read: 現在形"),
                ("I read that book yesterday", "read", "R EH1 D", "read: 過去形"),
                ("I have read the book", "read", "R EH1 D", "read: 過去分詞(have+read)"),

                // "lead" のケース
                // CMU辞書: lead[0]=L EH1 D (鉛), lead[1]=L IY1 D (導く)
                // HomographDB: lead デフォルト=1, Noun→0, Verb→1
                ("She will lead the team", "lead", "L IY1 D", "lead: 動詞(導く)"),
                ("The lead pipe is heavy", "lead", "L EH1 D", "lead: 名詞(鉛)"),

                // "tear" のケース
                // CMU辞書: tear[0]=T EH1 R (裂く), tear[1]=T IH1 R (涙)
                // HomographDB: tear デフォルト=0, Verb→0, Noun→1
                ("A tear rolled down her cheek", "tear", "T IH1 R", "tear: 名詞(涙)"),
                ("Don't tear the paper", "tear", "T EH1 R", "tear: 動詞(裂く)"),

                // "wind" のケース
                // CMU辞書: wind[0]=W AY1 N D (巻く), wind[1]=W IH1 N D (風)
                // HomographDB: wind デフォルト=1, Noun→1, Verb→0
                ("The wind is strong today", "wind", "W IH1 N D", "wind: 名詞(風)"),
                ("Wind the clock please", "wind", "W AY1 N D", "wind: 動詞(巻く)"),

                // "close" のケース
                // CMU辞書: close[0]=K L OW1 S (形容詞:近い), close[1]=K L OW1 Z (動詞:閉じる)
                // HomographDB: close デフォルト=1, Adjective→0, Adverb→0, Verb→1, Noun→1
                ("Close the door", "close", "K L OW1 Z", "close: 動詞(閉じる)"),
                ("Stay close to me", "close", "K L OW1 S", "close: 形容詞(近い)"),

                // "live" のケース
                // CMU辞書: live[0]=L AY1 V (形容詞:生の), live[1]=L IH1 V (動詞:生きる)
                // HomographDB: live デフォルト=1, Adjective→0, Adverb→0, Verb→1
                ("I live in Tokyo", "live", "L IH1 V", "live: 動詞(生きる)"),
                ("This is a live broadcast", "live", "L AY1 V", "live: 形容詞(生の)"),

                // "bow" のケース
                // CMU辞書: bow[0]=B AW1 (お辞儀), bow[1]=B OW1 (弓/リボン)
                // HomographDB: bow デフォルト=0, Verb→0, Noun→1
                ("Take a bow", "bow", "B AW1", "bow: 名詞(お辞儀) ※DBではNoun→1(B OW1)だが文脈次第"),
                ("She tied a bow", "bow", "B OW1", "bow: 名詞(リボン)"),

                // "use" のケース
                // CMU辞書: use[0]=Y UW1 S (名詞), use[1]=Y UW1 Z (動詞)
                // HomographDB: use デフォルト=0, Noun→0, Verb→1
                ("I use this tool", "use", "Y UW1 Z", "use: 動詞(使う)"),
                ("What is the use of it", "use", "Y UW1 S", "use: 名詞(用途)"),

                // "present" のケース
                // CMU辞書: present[0]=P R EH1 Z AH0 N T (名詞/形容詞), present[1]=P R IY0 Z EH1 N T (動詞)
                // HomographDB: present デフォルト=0, Noun→0, Adjective→0, Verb→1
                ("I present the award", "present", "P R IY0 Z EH1 N T", "present: 動詞(ストレス後方)"),
                ("A birthday present", "present", "P R EH1 Z AH0 N T", "present: 名詞(ストレス前方)"),

                // "record" のケース
                // CMU辞書: record[0]=R AH0 K AO1 R D (動詞), record[1]=R EH1 K ER0 D (名詞)
                // HomographDB: record デフォルト=1, Noun→1, Verb→0
                ("Record the song", "record", "R AH0 K AO1 R D", "record: 動詞"),
                ("A new record", "record", "R EH1 K ER0 D", "record: 名詞"),
            };

            var totalCount = testCases.Length;
            var correctCount = 0;
            var incorrectCount = 0;
            var errorDetails = new List<string>();

            // カテゴリ別集計
            var categoryResults = new Dictionary<string, (int total, int correct)>();

            _output.WriteLine("=== ホモグラフ文脈解決精度テスト ===");
            _output.WriteLine($"対象ケース数: {totalCount}");
            _output.WriteLine("");
            _output.WriteLine($"{"結果",-5} {"説明",-40} {"期待音素",-25} {"実際の出力(該当部分)"}");
            _output.WriteLine(new string('-', 120));

            foreach (var (sentence, targetWord, expectedPhonemeSubstring, description) in testCases)
            {
                var fullResult = _engine.ToPhonemes(sentence);

                // 出力全体から期待音素部分文字列を含むか判定
                var contains = !string.IsNullOrEmpty(fullResult) &&
                               fullResult.Contains(expectedPhonemeSubstring);

                // カテゴリ名（ターゲット単語）
                var category = targetWord;
                if (!categoryResults.ContainsKey(category))
                    categoryResults[category] = (0, 0);
                var (catTotal, catCorrect) = categoryResults[category];
                catTotal++;
                if (contains) catCorrect++;
                categoryResults[category] = (catTotal, catCorrect);

                if (contains)
                {
                    correctCount++;
                    _output.WriteLine($"{"OK",-5} {description,-40} {expectedPhonemeSubstring,-25} {TruncateOutput(fullResult)}");
                }
                else
                {
                    incorrectCount++;
                    var detail = $"  NG: {description} | 期待: [{expectedPhonemeSubstring}] | 実際: [{fullResult}]";
                    errorDetails.Add(detail);
                    _output.WriteLine($"{"NG",-5} {description,-40} {expectedPhonemeSubstring,-25} {TruncateOutput(fullResult)}");
                }
            }

            _output.WriteLine("");
            _output.WriteLine("=== ホモグラフ解決サマリー ===");
            _output.WriteLine($"総ケース数: {totalCount}");
            _output.WriteLine($"正解数: {correctCount} ({100.0 * correctCount / totalCount:F1}%)");
            _output.WriteLine($"不正解数: {incorrectCount} ({100.0 * incorrectCount / totalCount:F1}%)");

            _output.WriteLine("");
            _output.WriteLine("=== カテゴリ別結果 ===");
            foreach (var kvp in categoryResults.OrderBy(x => x.Key))
            {
                var (catTotal, catCorrect) = kvp.Value;
                var catRate = 100.0 * catCorrect / catTotal;
                _output.WriteLine($"  {kvp.Key,-12}: {catCorrect}/{catTotal} ({catRate:F0}%)");
            }

            if (errorDetails.Count > 0)
            {
                _output.WriteLine("");
                _output.WriteLine("=== 不正解の詳細 ===");
                foreach (var detail in errorDetails)
                {
                    _output.WriteLine(detail);
                }
            }

            // 少なくとも1つ以上のホモグラフが正しく解決されることを確認
            Assert.True(correctCount > 0, "ホモグラフの文脈解決が全て失敗");
        }

        /// <summary>
        /// 出力文字列が長すぎる場合に切り詰める。
        /// </summary>
        private static string TruncateOutput(string? output)
        {
            if (string.IsNullOrEmpty(output))
                return "(empty)";
            return output.Length > 80 ? output.Substring(0, 80) + "..." : output;
        }
    }
}
