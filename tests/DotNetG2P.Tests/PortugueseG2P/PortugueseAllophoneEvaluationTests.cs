using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Portuguese;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// ポルトガル語G2Pの異音プロファイル別PER評価テスト。
    /// 外部TSVリファレンスを使い、各プロファイルごとの正確性を検証する。
    /// </summary>
    [Trait("Category", "DatasetEvaluation")]
    public class PortugueseAllophoneEvaluationTests : IDisposable
    {
        private readonly PortugueseG2PEngine _baseBp = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false));
        private readonly PortugueseG2PEngine _obligatory = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableAllophones: true,
            allophoneFeatures: PortugueseAllophoneFeatures.Obligatory));
        private readonly PortugueseG2PEngine _brazilianDefault = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableAllophones: true,
            allophoneFeatures: PortugueseAllophoneFeatures.BrazilianDefault));
        private readonly PortugueseG2PEngine _europeanDefault = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.European, includeStress: false, enableAllophones: true,
            allophoneFeatures: PortugueseAllophoneFeatures.EuropeanDefault));
        private readonly PortugueseG2PEngine _all = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableAllophones: true,
            allophoneFeatures: PortugueseAllophoneFeatures.All));
        private readonly PortugueseG2PEngine _noExceptions = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableExceptionDictionary: false));
        private readonly ITestOutputHelper _output;

        public PortugueseAllophoneEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ========== ipa-dict サンプル: プロファイル別PER ==========

        [SkippableFact]
        public void IpaDictSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _baseBp);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_Obligatory_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _obligatory);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"obligatory PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_BrazilianDefault_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _brazilianDefault);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.04,
                $"BrazilianDefault PER ({result.PhonemeErrorRate:P2}) が閾値 4% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_EuropeanDefault_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _europeanDefault);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            // EP方言でBPリファレンスを評価するため、閾値は緩めに設定
            Assert.True(result.PhonemeErrorRate < 0.10,
                $"EuropeanDefault PER ({result.PhonemeErrorRate:P2}) が閾値 10% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_All_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _all);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"all PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_NoExceptions_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _noExceptions);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"no_exceptions PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        // ========== プロファイル間比較 ==========

        [SkippableFact]
        public void IpaDictSample_BasePerIsBetterThanNoExceptions()
        {
            var basePer = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _baseBp);
            var noExcPer = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _noExceptions);

            Skip.If(basePer.Cases == 0, "評価用TSVが見つかりません");

            _output.WriteLine($"base PER={basePer.PhonemeErrorRate:P2}, no_exceptions PER={noExcPer.PhonemeErrorRate:P2}");
            Assert.True(basePer.PhonemeErrorRate <= noExcPer.PhonemeErrorRate,
                $"base PER ({basePer.PhonemeErrorRate:P2}) が no_exceptions PER ({noExcPer.PhonemeErrorRate:P2}) より悪い");
        }

        [SkippableFact]
        public void IpaDictSample_BrazilianDefaultSimilarToBase()
        {
            var basePer = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _baseBp);
            var bpPer = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _brazilianDefault);

            Skip.If(basePer.Cases == 0, "評価用TSVが見つかりません");

            _output.WriteLine($"base PER={basePer.PhonemeErrorRate:P2}, BrazilianDefault PER={bpPer.PhonemeErrorRate:P2}");
            // 異音プロファイルはbase以下であるべき（異音正規化によりPERが下がるまたは同等）
            // ただし異音が参照と一致しない場合もあるため、5%の許容範囲を設ける
            Assert.True(bpPer.PhonemeErrorRate < basePer.PhonemeErrorRate + 0.05,
                $"BrazilianDefault PER ({bpPer.PhonemeErrorRate:P2}) が base PER ({basePer.PhonemeErrorRate:P2}) より大幅に悪い");
        }

        public void Dispose()
        {
            _baseBp.Dispose();
            _obligatory.Dispose();
            _brazilianDefault.Dispose();
            _europeanDefault.Dispose();
            _all.Dispose();
            _noExceptions.Dispose();
        }

        // ========== 共通ヘルパー ==========

        private string GetProfileName(PortugueseG2PEngine engine)
        {
            if (ReferenceEquals(engine, _baseBp)) return "base_bp";
            if (ReferenceEquals(engine, _obligatory)) return "obligatory";
            if (ReferenceEquals(engine, _brazilianDefault)) return "brazilian_default";
            if (ReferenceEquals(engine, _europeanDefault)) return "european_default";
            if (ReferenceEquals(engine, _all)) return "all";
            if (ReferenceEquals(engine, _noExceptions)) return "no_exceptions";
            return "unknown";
        }

        private CorpusResult EvaluateCorpus(string fileName, PortugueseG2PEngine engine)
        {
            var path = TryResolveTestDataPath(fileName);
            if (path == null)
            {
                _output.WriteLine($"評価用TSVが見つかりません: {fileName}");
                Skip.If(true, $"評価用TSVが見つかりません: {fileName}");
                return new CorpusResult(0, 0d);
            }

            var rows = File.ReadAllLines(path!)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .Select(ParseRow)
                .ToArray();

            var totalErrors = 0;
            var totalReferencePhonemes = 0;
            var mismatches = new List<(string Word, int Distance, string Predicted, string Reference)>();

            foreach (var row in rows)
            {
                var predicted = NormalizePredicted(engine.ToPhonemeList(row.Word));
                var reference = NormalizeReference(row.Reference);
                if (reference.Length == 0 || predicted.Length == 0)
                    continue;

                var distance = LevenshteinDistance(predicted, reference);
                totalErrors += distance;
                totalReferencePhonemes += reference.Length;

                if (distance > 0)
                {
                    mismatches.Add((
                        row.Word,
                        distance,
                        string.Join(" ", predicted),
                        string.Join(" ", reference)));
                }
            }

            var per = totalReferencePhonemes > 0 ? (double)totalErrors / totalReferencePhonemes : 0d;
            _output.WriteLine($"{fileName} [{GetProfileName(engine)}]: PER={per:P2} ({totalErrors}/{totalReferencePhonemes}), cases={rows.Length}, mismatches={mismatches.Count}");
            foreach (var mismatch in mismatches.OrderByDescending(x => x.Distance).ThenBy(x => x.Word).Take(10))
            {
                _output.WriteLine($"  {mismatch.Word}: dist={mismatch.Distance} pred=[{mismatch.Predicted}] ref=[{mismatch.Reference}]");
            }

            return new CorpusResult(rows.Length, per);
        }

        private static (string Word, string Reference) ParseRow(string line)
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new InvalidDataException($"TSV行が不正です: {line}");
            return (parts[0].Trim(), parts[1].Trim());
        }

        private static string? TryResolveTestDataPath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "PortugueseG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "PortugueseG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "PortugueseG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "PortugueseG2P", fileName)),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "eval_data", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tools", "eval_data", fileName),
                Path.GetFullPath(Path.Combine("tools", "eval_data", fileName)),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }

        private static string[] NormalizePredicted(IReadOnlyList<PortuguesePhoneme> phonemes)
        {
            var result = new string[phonemes.Count];
            for (var i = 0; i < phonemes.Count; i++)
                result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
            return result;
        }

        private static string NormalizePredictedPhoneme(PortugueseIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case PortugueseIpaPhoneme.A: return "a";
                case PortugueseIpaPhoneme.E: return "e";
                case PortugueseIpaPhoneme.Eh: return "\u025B"; // ɛ
                case PortugueseIpaPhoneme.I: return "i";
                case PortugueseIpaPhoneme.O: return "o";
                case PortugueseIpaPhoneme.Oh: return "\u0254"; // ɔ
                case PortugueseIpaPhoneme.U: return "u";
                case PortugueseIpaPhoneme.Schwa: return "\u0250"; // ɐ
                case PortugueseIpaPhoneme.HighCentral: return "\u0268"; // ɨ
                case PortugueseIpaPhoneme.ANasal: return "\u0250\u0303"; // ɐ̃
                case PortugueseIpaPhoneme.ENasal: return "e\u0303"; // ẽ
                case PortugueseIpaPhoneme.INasal: return "i\u0303"; // ĩ
                case PortugueseIpaPhoneme.ONasal: return "\u00F5"; // õ
                case PortugueseIpaPhoneme.UNasal: return "u\u0303"; // ũ
                case PortugueseIpaPhoneme.J: return "j";
                case PortugueseIpaPhoneme.W: return "w";
                case PortugueseIpaPhoneme.P: return "p";
                case PortugueseIpaPhoneme.B: return "b";
                case PortugueseIpaPhoneme.T: return "t";
                case PortugueseIpaPhoneme.D: return "d";
                case PortugueseIpaPhoneme.K: return "k";
                case PortugueseIpaPhoneme.G: return "\u0261"; // ɡ
                case PortugueseIpaPhoneme.F: return "f";
                case PortugueseIpaPhoneme.V: return "v";
                case PortugueseIpaPhoneme.S: return "s";
                case PortugueseIpaPhoneme.Z: return "z";
                case PortugueseIpaPhoneme.Sh: return "\u0283"; // ʃ
                case PortugueseIpaPhoneme.Zh: return "\u0292"; // ʒ
                case PortugueseIpaPhoneme.M: return "m";
                case PortugueseIpaPhoneme.N: return "n";
                case PortugueseIpaPhoneme.Ny: return "\u0272"; // ɲ
                case PortugueseIpaPhoneme.L:
                case PortugueseIpaPhoneme.DarkL: return "l";
                case PortugueseIpaPhoneme.Lh: return "\u028E"; // ʎ
                case PortugueseIpaPhoneme.R: return "\u027E"; // ɾ
                case PortugueseIpaPhoneme.Rr: return "\u0281"; // ʁ
                case PortugueseIpaPhoneme.Ch: return "t\u0283"; // tʃ
                case PortugueseIpaPhoneme.Jh: return "d\u0292"; // dʒ
                case PortugueseIpaPhoneme.X: return "\u0283"; // →ʃ
                case PortugueseIpaPhoneme.H: return "h";
                case PortugueseIpaPhoneme.Xh: return "\u0283"; // →ʃ
                case PortugueseIpaPhoneme.Ng: return "n"; // ŋ→n
                case PortugueseIpaPhoneme.NLabiodental: return "m"; // ɱ→m
                case PortugueseIpaPhoneme.NDental: return "n";
                case PortugueseIpaPhoneme.Beta: return "b";
                case PortugueseIpaPhoneme.Dh: return "d";
                case PortugueseIpaPhoneme.Gh: return "\u0261"; // ɣ→ɡ
                case PortugueseIpaPhoneme.WNasal: return "w\u0303"; // w̃
                case PortugueseIpaPhoneme.JNasal: return "j\u0303"; // j̃
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }

        private static string[] NormalizeReference(string transcription)
        {
            var raw = transcription.Replace("/", string.Empty)
                .Replace("\u02C8", string.Empty)  // ˈ
                .Replace("\u02CC", string.Empty)  // ˌ
                .Replace(".", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

            var tokens = TokenizeIpa(raw);
            for (var i = 0; i < tokens.Length; i++)
                tokens[i] = NormalizeReferenceToken(tokens[i]);

            return tokens.Where(token => token.Length > 0).ToArray();
        }

        private static string NormalizeReferenceToken(string token)
        {
            switch (token)
            {
                case "g": return "\u0261";
                case "\u026B": return "l"; // ɫ→l
                case "\u03B2": return "b"; // β→b
                case "\u00F0": return "d"; // ð→d
                case "\u0263": return "\u0261"; // ɣ→ɡ
                case "\u0271": return "m"; // ɱ→m
                case "\u014B": return "n"; // ŋ→n
                case "n\u032A": return "n"; // n̪→n
                case "t\u0361\u0283": return "t\u0283"; // t͡ʃ→tʃ
                case "d\u0361\u0292": return "d\u0292"; // d͡ʒ→dʒ
                default: return token;
            }
        }

        private static string[] TokenizeIpa(string text)
        {
            var tokens = new List<string>();
            for (var i = 0; i < text.Length;)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    i++;
                    continue;
                }

                if (TryMatch(text, i, "t\u0361\u0283", out var consumed)    // t͡ʃ
                    || TryMatch(text, i, "d\u0361\u0292", out consumed)     // d͡ʒ
                    || TryMatch(text, i, "t\u0283", out consumed)           // tʃ
                    || TryMatch(text, i, "d\u0292", out consumed))          // dʒ
                {
                    tokens.Add(text.Substring(i, consumed));
                    i += consumed;
                    continue;
                }

                var start = i;
                i++;
                while (i < text.Length && IsCombiningMark(text[i]))
                    i++;

                tokens.Add(text.Substring(start, i - start));
            }

            return tokens.ToArray();
        }

        private static bool IsCombiningMark(char c)
        {
            var category = char.GetUnicodeCategory(c);
            return category == System.Globalization.UnicodeCategory.NonSpacingMark
                || category == System.Globalization.UnicodeCategory.SpacingCombiningMark
                || category == System.Globalization.UnicodeCategory.EnclosingMark;
        }

        private static bool TryMatch(string text, int start, string match, out int consumed)
        {
            if (start + match.Length <= text.Length && string.CompareOrdinal(text, start, match, 0, match.Length) == 0)
            {
                consumed = match.Length;
                return true;
            }

            consumed = 0;
            return false;
        }

        private static int LevenshteinDistance<T>(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            if (source.Count == 0)
                return target.Count;
            if (target.Count == 0)
                return source.Count;

            var previous = new int[target.Count + 1];
            var current = new int[target.Count + 1];

            for (var j = 0; j <= target.Count; j++)
                previous[j] = j;

            for (var i = 1; i <= source.Count; i++)
            {
                current[0] = i;
                for (var j = 1; j <= target.Count; j++)
                {
                    var cost = EqualityComparer<T>.Default.Equals(source[i - 1], target[j - 1]) ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[target.Count];
        }

        private readonly struct CorpusResult
        {
            public int Cases { get; }
            public double PhonemeErrorRate { get; }

            public CorpusResult(int cases, double phonemeErrorRate)
            {
                Cases = cases;
                PhonemeErrorRate = phonemeErrorRate;
            }
        }
    }
}
