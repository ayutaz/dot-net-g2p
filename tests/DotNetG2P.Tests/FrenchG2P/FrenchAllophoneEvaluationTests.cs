using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.French;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// フランス語G2Pの異音プロファイル別PER評価テスト。
    /// 外部TSVリファレンスを使い、base/allophones/no_exceptionsプロファイルごとの正確性を検証する。
    /// </summary>
    [Trait("Category", "Accuracy")]
    public class FrenchAllophoneEvaluationTests : IDisposable
    {
        private readonly FrenchG2PEngine _base = new FrenchG2PEngine(new FrenchG2POptions(includeStress: false));
        private readonly FrenchG2PEngine _allophones = new FrenchG2PEngine(new FrenchG2POptions(
            includeStress: false, enableAllophones: true));
        private readonly FrenchG2PEngine _noExceptions = new FrenchG2PEngine(new FrenchG2POptions(
            includeStress: false, enableExceptionDictionary: false));
        private readonly ITestOutputHelper _output;

        public FrenchAllophoneEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ========== ipa-dict サンプル: プロファイル別PER ==========

        [SkippableFact]
        public void IpaDictSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _base);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"base PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_Allophones_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _allophones);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"allophones PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_NoExceptions_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _noExceptions);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12,
                $"no_exceptions PER ({result.PhonemeErrorRate:P2}) が閾値 12% を超えています。");
        }

        // ========== プロファイル間比較 ==========

        [SkippableFact]
        public void IpaDictSample_BasePerIsBetterThanNoExceptions()
        {
            var basePer = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _base);
            var noExcPer = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _noExceptions);

            Skip.If(basePer.Cases == 0, "評価用TSVが見つかりません");

            _output.WriteLine($"base PER={basePer.PhonemeErrorRate:P2}, no_exceptions PER={noExcPer.PhonemeErrorRate:P2}");
            Assert.True(basePer.PhonemeErrorRate <= noExcPer.PhonemeErrorRate,
                $"base PER ({basePer.PhonemeErrorRate:P2}) が no_exceptions PER ({noExcPer.PhonemeErrorRate:P2}) より悪い");
        }

        [SkippableFact]
        public void IpaDictSample_AllophonesSimilarToBase()
        {
            var basePer = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _base);
            var alloPer = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _allophones);

            Skip.If(basePer.Cases == 0, "評価用TSVが見つかりません");

            _output.WriteLine($"base PER={basePer.PhonemeErrorRate:P2}, allophones PER={alloPer.PhonemeErrorRate:P2}");
            // 異音プロファイルはbase以下であるべき（異音正規化によりPERが下がるまたは同等）
            // ただし異音が参照と一致しない場合もあるため、5%の許容範囲を設ける
            Assert.True(alloPer.PhonemeErrorRate < basePer.PhonemeErrorRate + 0.05,
                $"allophones PER ({alloPer.PhonemeErrorRate:P2}) が base PER ({basePer.PhonemeErrorRate:P2}) より大幅に悪い");
        }

        // ========== キュレーション済み異音リファレンス ==========

        [SkippableFact]
        public void CuratedAllophoneReference_MatchesExpectedProfiles()
        {
            var path = TryResolveTestDataPath("french_allophone_reference.tsv");
            Skip.If(path == null, "異音リファレンスTSVが見つかりません");

            var rows = File.ReadAllLines(path!)
                .Skip(1) // ヘッダ行
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .Select(line => line.Split('\t'))
                .Where(parts => parts.Length >= 3)
                .Select(parts => new Row(parts[0].Trim(), parts[1].Trim(), parts[2].Trim()))
                .ToArray();

            Skip.If(rows.Length == 0, "異音リファレンスTSVが空です");

            var grouped = rows.GroupBy(row => row.Profile, StringComparer.Ordinal);
            foreach (var group in grouped)
            {
                var engine = GetEngine(group.Key);
                var failures = new List<string>();

                foreach (var row in group)
                {
                    var actual = engine.ToIPA(row.Word);
                    if (!string.Equals(actual, row.Expected, StringComparison.Ordinal))
                        failures.Add($"{row.Word}: expected={row.Expected}, actual={actual}");
                }

                _output.WriteLine($"{group.Key}: {group.Count()} cases, failures={failures.Count}");
                foreach (var failure in failures)
                    _output.WriteLine("  " + failure);

                Assert.Empty(failures);
            }
        }

        public void Dispose()
        {
            _base.Dispose();
            _allophones.Dispose();
            _noExceptions.Dispose();
        }

        // ========== 共通ヘルパー ==========

        private FrenchG2PEngine GetEngine(string profile)
        {
            switch (profile)
            {
                case "base": return _base;
                case "allophones": return _allophones;
                case "no_exceptions": return _noExceptions;
                default: throw new InvalidOperationException("Unknown allophone profile: " + profile);
            }
        }

        private CorpusResult EvaluateCorpus(string fileName, FrenchG2PEngine engine)
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

        private string GetProfileName(FrenchG2PEngine engine)
        {
            if (ReferenceEquals(engine, _base)) return "base";
            if (ReferenceEquals(engine, _allophones)) return "allophones";
            if (ReferenceEquals(engine, _noExceptions)) return "no_exceptions";
            return "unknown";
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
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "FrenchG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "FrenchG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "FrenchG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "FrenchG2P", fileName)),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }

        private static string[] NormalizePredicted(IReadOnlyList<FrenchPhoneme> phonemes)
        {
            var result = new string[phonemes.Count];
            for (var i = 0; i < phonemes.Count; i++)
                result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
            return result;
        }

        private static string NormalizePredictedPhoneme(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case FrenchIpaPhoneme.A:
                case FrenchIpaPhoneme.Ah: return "a";
                case FrenchIpaPhoneme.E: return "e";
                case FrenchIpaPhoneme.Eh: return "\u025B";
                case FrenchIpaPhoneme.I: return "i";
                case FrenchIpaPhoneme.O: return "o";
                case FrenchIpaPhoneme.Oh: return "\u0254";
                case FrenchIpaPhoneme.U: return "u";
                case FrenchIpaPhoneme.Y: return "y";
                case FrenchIpaPhoneme.Oe: return "\u00F8";
                case FrenchIpaPhoneme.Oeh: return "\u0153";
                case FrenchIpaPhoneme.Schwa: return "\u0259";
                case FrenchIpaPhoneme.ANasal: return "\u0251\u0303";
                case FrenchIpaPhoneme.ONasal: return "\u0254\u0303";
                case FrenchIpaPhoneme.ENasal: return "\u025B\u0303";
                case FrenchIpaPhoneme.OeNasal: return "\u025B\u0303"; // œ̃→ɛ̃ Metropolitan中和
                case FrenchIpaPhoneme.J: return "j";
                case FrenchIpaPhoneme.W: return "w";
                case FrenchIpaPhoneme.Uj: return "\u0265";
                case FrenchIpaPhoneme.P: return "p";
                case FrenchIpaPhoneme.B: return "b";
                case FrenchIpaPhoneme.T: return "t";
                case FrenchIpaPhoneme.D: return "d";
                case FrenchIpaPhoneme.K: return "k";
                case FrenchIpaPhoneme.G: return "\u0261";
                case FrenchIpaPhoneme.F: return "f";
                case FrenchIpaPhoneme.V: return "v";
                case FrenchIpaPhoneme.S: return "s";
                case FrenchIpaPhoneme.Z: return "z";
                case FrenchIpaPhoneme.Sh: return "\u0283";
                case FrenchIpaPhoneme.Zh: return "\u0292";
                case FrenchIpaPhoneme.M: return "m";
                case FrenchIpaPhoneme.N: return "n";
                case FrenchIpaPhoneme.Ny: return "\u0272";
                case FrenchIpaPhoneme.L: return "l";
                case FrenchIpaPhoneme.R:
                case FrenchIpaPhoneme.Rh: return "\u0281"; // 異音→基底形
                case FrenchIpaPhoneme.Ng: return "\u014B";
                case FrenchIpaPhoneme.Ts: return "ts";
                case FrenchIpaPhoneme.Dz: return "dz";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }

        private static string[] NormalizeReference(string transcription)
        {
            var raw = transcription.Replace("/", string.Empty)
                .Replace("\u02C8", string.Empty)
                .Replace("\u02CC", string.Empty)
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
                case "\u0251": return "a";
                case "g": return "\u0261";
                case "\u0153\u0303": return "\u025B\u0303";
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

                if (TryMatch(text, i, "ts", out var consumed)
                    || TryMatch(text, i, "dz", out consumed)
                    || TryMatch(text, i, "t\u0283", out consumed)
                    || TryMatch(text, i, "d\u0292", out consumed))
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

        private readonly struct Row
        {
            public string Word { get; }
            public string Profile { get; }
            public string Expected { get; }

            public Row(string word, string profile, string expected)
            {
                Word = word;
                Profile = profile;
                Expected = expected;
            }
        }
    }
}
