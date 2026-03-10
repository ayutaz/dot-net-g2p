using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Spanish;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishDatasetEvaluationTests : IDisposable
    {
        private readonly SpanishG2PEngine _latinAmerican = new SpanishG2PEngine(new SpanishG2POptions(includeStress: false));
        private readonly SpanishG2PEngine _castilian = new SpanishG2PEngine(new SpanishG2POptions(dialect: SpanishDialect.Castilian, includeStress: false));
        private readonly ITestOutputHelper _output;

        public SpanishDatasetEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void WikiPron_LatinAmericanBroadSample_PerBelowThreshold()
        {
            var result = EvaluateCorpus(
                "wikipron_spa_latn_la_broad_filtered_sample.tsv",
                _latinAmerican,
                SourceKind.WikiPron,
                SpanishDialect.LatinAmerican);

            Assert.True(result.Cases >= 200, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08, $"WikiPron LA PER ({result.PhonemeErrorRate:P2}) が閾値を超えています。");
        }

        [Fact]
        public void WikiPron_CastilianBroadSample_PerBelowThreshold()
        {
            var result = EvaluateCorpus(
                "wikipron_spa_latn_ca_broad_filtered_sample.tsv",
                _castilian,
                SourceKind.WikiPron,
                SpanishDialect.Castilian);

            Assert.True(result.Cases >= 200, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08, $"WikiPron Castilian PER ({result.PhonemeErrorRate:P2}) が閾値を超えています。");
        }

        [Fact]
        public void IpaDict_MexicanSample_PerBelowThreshold()
        {
            var result = EvaluateCorpus(
                "ipa_dict_es_mx_sample.tsv",
                _latinAmerican,
                SourceKind.IpaDict,
                SpanishDialect.LatinAmerican);

            Assert.True(result.Cases >= 200, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12, $"ipa-dict es_MX PER ({result.PhonemeErrorRate:P2}) が閾値を超えています。");
        }

        [Fact]
        public void IpaDict_CastilianSample_PerBelowThreshold()
        {
            var result = EvaluateCorpus(
                "ipa_dict_es_es_sample.tsv",
                _castilian,
                SourceKind.IpaDict,
                SpanishDialect.Castilian);

            Assert.True(result.Cases >= 200, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12, $"ipa-dict es_ES PER ({result.PhonemeErrorRate:P2}) が閾値を超えています。");
        }

        public void Dispose()
        {
            _latinAmerican.Dispose();
            _castilian.Dispose();
        }

        private CorpusResult EvaluateCorpus(string fileName, SpanishG2PEngine engine, SourceKind sourceKind, SpanishDialect dialect)
        {
            var path = ResolveTestDataPath(fileName);
            var rows = File.ReadAllLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseRow)
                .ToArray();

            var totalErrors = 0;
            var totalReferencePhonemes = 0;
            var mismatches = new List<(string Word, int Distance, string Predicted, string Reference)>();

            foreach (var row in rows)
            {
                var predicted = NormalizePredicted(engine.ToPhonemeList(row.Word));
                var reference = NormalizeReference(row.Reference, sourceKind, dialect);
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
            _output.WriteLine($"{fileName}: PER={per:P2} ({totalErrors}/{totalReferencePhonemes}), cases={rows.Length}, mismatches={mismatches.Count}");
            foreach (var mismatch in mismatches.OrderByDescending(x => x.Distance).ThenBy(x => x.Word).Take(15))
            {
                _output.WriteLine($"  {mismatch.Word}: dist={mismatch.Distance} pred=[{mismatch.Predicted}] ref=[{mismatch.Reference}]");
            }

            return new CorpusResult(rows.Length, per);
        }

        private static (string Word, string Reference) ParseRow(string line)
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new InvalidDataException($"TSV row is malformed: {line}");

            return (parts[0].Trim(), parts[1].Trim());
        }

        private static string ResolveTestDataPath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "SpanishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "SpanishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "SpanishG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "SpanishG2P", fileName)),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            throw new FileNotFoundException($"Spanish evaluation sample not found: {fileName}");
        }

        private static string[] NormalizePredicted(IReadOnlyList<SpanishPhoneme> phonemes)
        {
            var result = new string[phonemes.Count];
            for (var i = 0; i < phonemes.Count; i++)
                result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
            return result;
        }

        private static string NormalizePredictedPhoneme(SpanishIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case SpanishIpaPhoneme.A: return "a";
                case SpanishIpaPhoneme.E: return "e";
                case SpanishIpaPhoneme.I: return "i";
                case SpanishIpaPhoneme.O: return "o";
                case SpanishIpaPhoneme.U: return "u";
                case SpanishIpaPhoneme.J: return "j";
                case SpanishIpaPhoneme.W: return "w";
                case SpanishIpaPhoneme.P: return "p";
                case SpanishIpaPhoneme.B:
                case SpanishIpaPhoneme.Beta: return "b";
                case SpanishIpaPhoneme.T: return "t";
                case SpanishIpaPhoneme.D:
                case SpanishIpaPhoneme.Dh: return "d";
                case SpanishIpaPhoneme.K: return "k";
                case SpanishIpaPhoneme.G:
                case SpanishIpaPhoneme.Gh: return "ɡ";
                case SpanishIpaPhoneme.F: return "f";
                case SpanishIpaPhoneme.S:
                case SpanishIpaPhoneme.Z: return "s";
                case SpanishIpaPhoneme.X:
                case SpanishIpaPhoneme.Sh: return "x";
                case SpanishIpaPhoneme.Ch: return "tʃ";
                case SpanishIpaPhoneme.Y:
                case SpanishIpaPhoneme.Ll:
                case SpanishIpaPhoneme.YAffricate: return "ʝ";
                case SpanishIpaPhoneme.M:
                case SpanishIpaPhoneme.NLabiodental: return "m";
                case SpanishIpaPhoneme.N:
                case SpanishIpaPhoneme.Eng:
                case SpanishIpaPhoneme.NDental: return "n";
                case SpanishIpaPhoneme.Ny: return "ɲ";
                case SpanishIpaPhoneme.L: return "l";
                case SpanishIpaPhoneme.Rr: return "r";
                case SpanishIpaPhoneme.R: return "ɾ";
                case SpanishIpaPhoneme.Th: return "θ";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }

        private static string[] NormalizeReference(string transcription, SourceKind sourceKind, SpanishDialect dialect)
        {
            var raw = sourceKind == SourceKind.WikiPron
                ? transcription.Replace("|", " ").Trim()
                : transcription.Replace("/", string.Empty)
                    .Replace("ˈ", string.Empty)
                    .Replace("ˌ", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace(" ", string.Empty)
                    .Trim();

            var tokens = sourceKind == SourceKind.WikiPron
                ? raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : TokenizeIpa(raw);

            for (var i = 0; i < tokens.Length; i++)
                tokens[i] = NormalizeReferenceToken(tokens[i], dialect);

            return tokens.Where(token => token.Length > 0).ToArray();
        }

        private static string NormalizeReferenceToken(string token, SpanishDialect dialect)
        {
            switch (token)
            {
                case "g":
                case "ɡ":
                case "ɣ":
                    return "ɡ";
                case "β":
                    return "b";
                case "ð":
                    return "d";
                case "z":
                    return "s";
                case "ɱ":
                    return "m";
                case "ŋ":
                case "n̪":
                    return "n";
                case "ʎ":
                case "ɟʝ":
                    return "ʝ";
                case "ʃ":
                    return dialect == SpanishDialect.Castilian ? "x" : "x";
                default:
                    return token;
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

                if (TryMatch(text, i, "ɟʝ", out var consumed)
                    || TryMatch(text, i, "tʃ", out consumed)
                    || TryMatch(text, i, "n̪", out consumed))
                {
                    tokens.Add(text.Substring(i, consumed));
                    i += consumed;
                    continue;
                }

                tokens.Add(text[i].ToString());
                i++;
            }

            return tokens.ToArray();
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

        private enum SourceKind : byte
        {
            WikiPron = 0,
            IpaDict = 1,
        }
    }
}
