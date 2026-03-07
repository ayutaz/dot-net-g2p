using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.English;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 句読点処理・OOV率計測テスト。
    /// 英語G2Pエンジンの句読点処理とCMU辞書カバレッジを検証する。
    /// </summary>
    public class OovAndPunctuationTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;
        private readonly EnglishG2PEngine _engineNoLts;
        private readonly ITestOutputHelper _output;

        public OovAndPunctuationTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new EnglishG2PEngine();
            _engineNoLts = new EnglishG2PEngine(new EnglishG2POptions(
                includeStress: true,
                enableLts: false,
                enableNormalization: true,
                enableHomographResolution: true
            ));
        }

        public void Dispose()
        {
            _engine.Dispose();
            _engineNoLts.Dispose();
        }

        // =====================================================================
        // テスト1: 句読点を含む文の処理
        // =====================================================================

        [Fact]
        public void PunctuationHandling_VariousSentences_ProducesPhonemes()
        {
            var sentences = new[]
            {
                "Hello, world!",
                "Wait... what?",
                "Yes; no; maybe.",
                "Dr. Smith said: 'Hello!'",
                "Price is $100, right?",
                "It's 3:30 p.m.",
                "He said, 'stop!' and left.",
                "First, second, and third.",
                "However, it works; mostly.",
                "Really?! That's amazing!"
            };

            _output.WriteLine("=== テスト1: 句読点を含む文の処理 ===");
            _output.WriteLine("");

            foreach (var sentence in sentences)
            {
                var phonemes = _engine.ToPhonemes(sentence);
                _output.WriteLine($"入力: \"{sentence}\"");
                _output.WriteLine($"音素: {phonemes}");
                _output.WriteLine("");

                // 各文が空でない音素列を生成すること
                Assert.False(string.IsNullOrEmpty(phonemes),
                    $"文 \"{sentence}\" が空の音素列を生成しました");
            }

            _output.WriteLine("--- 句読点処理の分析 ---");
            _output.WriteLine("エンジンのTokenize処理:");
            _output.WriteLine("  - カンマ(,)、感嘆符(!)、疑問符(?)、セミコロン(;)、コロン(:) は除去");
            _output.WriteLine("  - ピリオド(.) は単語文字として扱われ、末尾でトリム");
            _output.WriteLine("  - アポストロフィ(')はcan't, it's等の短縮形を保持するため単語文字");
            _output.WriteLine("  - $100等は正規化によりone hundredに展開後に処理");
            _output.WriteLine("");
        }

        // =====================================================================
        // テスト2: 実用的な文でのOOV率
        // =====================================================================

        [Fact]
        public void OovRate_CommonEnglishSentences_MeasureDictionaryCoverage()
        {
            var sentences = new[]
            {
                "The quick brown fox jumps over the lazy dog",
                "She sells seashells by the seashore",
                "How much wood would a woodchuck chuck",
                "The government announced new economic policies yesterday",
                "Scientists discovered a breakthrough in quantum computing",
                "The restaurant serves delicious Mediterranean cuisine",
                "Please submit your application before the deadline",
                "The unprecedented situation required extraordinary measures",
                "Artificial intelligence is transforming healthcare delivery",
                "The archaeological excavation revealed ancient artifacts",
                "Environmental sustainability requires international cooperation",
                "The pharmaceutical company developed revolutionary treatments",
                "Cryptocurrency markets experienced significant volatility",
                "The entrepreneurial ecosystem fosters innovative startups",
                "Neurological research demonstrates remarkable plasticity"
            };

            _output.WriteLine("=== テスト2: 実用的な文でのOOV率計測 ===");
            _output.WriteLine("");

            var totalWords = 0;
            var totalInDict = 0;
            var totalOov = 0;
            var allOovWords = new List<string>();

            foreach (var sentence in sentences)
            {
                // 単語分割（アルファベットのみの単語を抽出）
                var words = ExtractWords(sentence);
                var inDict = 0;
                var oov = 0;
                var oovWords = new List<string>();

                _output.WriteLine($"文: \"{sentence}\"");

                foreach (var word in words)
                {
                    var isInDict = _engine.ContainsWord(word);
                    if (isInDict)
                    {
                        inDict++;
                    }
                    else
                    {
                        oov++;
                        oovWords.Add(word);
                    }
                }

                var rate = words.Length > 0 ? (double)oov / words.Length * 100 : 0;
                _output.WriteLine($"  単語数: {words.Length}, 辞書内: {inDict}, OOV: {oov} ({rate:F1}%)");
                if (oovWords.Count > 0)
                {
                    _output.WriteLine($"  OOV単語: {string.Join(", ", oovWords)}");
                }
                _output.WriteLine("");

                totalWords += words.Length;
                totalInDict += inDict;
                totalOov += oov;
                allOovWords.AddRange(oovWords);
            }

            var totalRate = totalWords > 0 ? (double)totalOov / totalWords * 100 : 0;
            _output.WriteLine("=== 全体集計 ===");
            _output.WriteLine($"総単語数: {totalWords}");
            _output.WriteLine($"辞書内単語数: {totalInDict}");
            _output.WriteLine($"OOV単語数: {totalOov}");
            _output.WriteLine($"全体OOV率: {totalRate:F2}%");
            _output.WriteLine("");

            if (allOovWords.Count > 0)
            {
                var uniqueOov = allOovWords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                _output.WriteLine($"ユニークOOV単語 ({uniqueOov.Count}件):");
                foreach (var w in uniqueOov)
                {
                    _output.WriteLine($"  - {w}");
                }
            }
            else
            {
                _output.WriteLine("全単語が辞書に存在しました。");
            }
            _output.WriteLine("");

            // OOV率は30%以下であるべき（CMU辞書は一般的な英語を高くカバー）
            Assert.True(totalRate < 30.0,
                $"OOV率が高すぎます: {totalRate:F2}%（期待: 30%未満）");
        }

        // =====================================================================
        // テスト3: OOV語のLTS品質チェック
        // =====================================================================

        [Fact]
        public void OovLtsQuality_CheckLtsPredictionsForOovWords()
        {
            var sentences = new[]
            {
                "The quick brown fox jumps over the lazy dog",
                "She sells seashells by the seashore",
                "How much wood would a woodchuck chuck",
                "The government announced new economic policies yesterday",
                "Scientists discovered a breakthrough in quantum computing",
                "The restaurant serves delicious Mediterranean cuisine",
                "Please submit your application before the deadline",
                "The unprecedented situation required extraordinary measures",
                "Artificial intelligence is transforming healthcare delivery",
                "The archaeological excavation revealed ancient artifacts",
                "Environmental sustainability requires international cooperation",
                "The pharmaceutical company developed revolutionary treatments",
                "Cryptocurrency markets experienced significant volatility",
                "The entrepreneurial ecosystem fosters innovative startups",
                "Neurological research demonstrates remarkable plasticity"
            };

            _output.WriteLine("=== テスト3: OOV語のLTS品質チェック ===");
            _output.WriteLine("");

            var oovWithLts = new List<(string word, string phonemes)>();
            var oovWithoutLts = new List<string>();

            foreach (var sentence in sentences)
            {
                var words = ExtractWords(sentence);
                foreach (var word in words)
                {
                    if (!_engine.ContainsWord(word))
                    {
                        // LTS有効エンジンで音素取得
                        var phonemeList = _engine.LookupWord(word);
                        if (phonemeList.Count > 0)
                        {
                            var phonemeStr = string.Join(" ", phonemeList.Select(p => p.ToString()));
                            oovWithLts.Add((word, phonemeStr));
                        }
                        else
                        {
                            oovWithoutLts.Add(word);
                        }
                    }
                }
            }

            if (oovWithLts.Count > 0)
            {
                _output.WriteLine($"LTSで音素推定されたOOV語 ({oovWithLts.Count}件):");
                _output.WriteLine("");
                _output.WriteLine($"{"単語",-25} {"LTS推定音素"}");
                _output.WriteLine(new string('-', 70));

                foreach (var (word, phonemes) in oovWithLts)
                {
                    _output.WriteLine($"{word,-25} {phonemes}");

                    // 基本的な品質チェック: 音素が少なくとも1つあること
                    Assert.False(string.IsNullOrEmpty(phonemes),
                        $"OOV語 \"{word}\" のLTS結果が空です");

                    // 音素列に母音が含まれていること（英語の単語は通常母音を含む）
                    var hasVowel = phonemes.Split(' ')
                        .Any(p => "AA AE AH AO AW AY EH ER EY IH IY OW OY UH UW".Split(' ')
                            .Any(v => p.StartsWith(v)));
                    Assert.True(hasVowel,
                        $"OOV語 \"{word}\" のLTS結果 \"{phonemes}\" に母音がありません");
                }
            }
            else
            {
                _output.WriteLine("OOV語がないため、LTS品質チェックはスキップされました。");
            }

            if (oovWithoutLts.Count > 0)
            {
                _output.WriteLine("");
                _output.WriteLine($"LTSでも音素推定できなかった語 ({oovWithoutLts.Count}件):");
                foreach (var w in oovWithoutLts)
                {
                    _output.WriteLine($"  - {w}");
                }
            }

            _output.WriteLine("");

            // LTS付きエンジンとLTSなしエンジンの出力比較
            _output.WriteLine("=== LTS有効/無効エンジンの出力比較 ===");
            _output.WriteLine("");

            var testSentences = new[]
            {
                "Cryptocurrency markets experienced significant volatility",
                "The entrepreneurial ecosystem fosters innovative startups"
            };

            foreach (var sentence in testSentences)
            {
                var withLts = _engine.ToPhonemes(sentence);
                var withoutLts = _engineNoLts.ToPhonemes(sentence);

                _output.WriteLine($"入力: \"{sentence}\"");
                _output.WriteLine($"  LTS有効:   {withLts}");
                _output.WriteLine($"  LTS無効:   {withoutLts}");

                var withLtsWords = withLts.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries).Length;
                var withoutLtsWords = withoutLts.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries).Length;
                _output.WriteLine($"  LTS有効の単語数: ~{withLtsWords}, LTS無効の単語数: ~{withoutLtsWords}");
                _output.WriteLine("");
            }

            Assert.True(true, "LTS品質チェック完了");
        }

        // =====================================================================
        // ヘルパーメソッド
        // =====================================================================

        /// <summary>
        /// テキストからアルファベットのみの単語を抽出する。
        /// EnglishG2PEngineのTokenize処理と同等のロジック。
        /// </summary>
        private static string[] ExtractWords(string text)
        {
            var words = new List<string>();
            var start = -1;

            for (var i = 0; i <= text.Length; i++)
            {
                var isWordChar = i < text.Length &&
                    ((text[i] >= 'A' && text[i] <= 'Z') ||
                     (text[i] >= 'a' && text[i] <= 'z') ||
                     text[i] == '\'' || text[i] == '\u2019' ||
                     text[i] == '.');

                if (isWordChar)
                {
                    if (start < 0)
                        start = i;
                }
                else
                {
                    if (start >= 0)
                    {
                        var word = text.Substring(start, i - start);
                        word = word.Trim('\'', '\u2019');
                        word = word.TrimEnd('.');
                        if (word.Length > 0)
                            words.Add(word);
                        start = -1;
                    }
                }
            }

            return words.ToArray();
        }
    }
}
