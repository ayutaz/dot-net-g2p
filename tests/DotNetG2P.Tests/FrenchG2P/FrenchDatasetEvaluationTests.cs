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
    /// 外部TSVコーパスを使ったフランス語G2P PER閾値テスト。
    /// TSVが存在しない場合はSkipする。
    /// </summary>
    [Trait("Category", "Accuracy")]
    public class FrenchDatasetEvaluationTests : IDisposable
    {
        private readonly FrenchG2PEngine _base = new FrenchG2PEngine(new FrenchG2POptions(includeStress: false));
        private readonly FrenchG2PEngine _allophones = new FrenchG2PEngine(new FrenchG2POptions(
            includeStress: false, enableAllophones: true));
        private readonly FrenchG2PEngine _noExceptions = new FrenchG2PEngine(new FrenchG2POptions(
            includeStress: false, enableExceptionDictionary: false));
        private readonly ITestOutputHelper _output;

        public FrenchDatasetEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ========== ipa-dict fr_FR サンプル ==========

        [SkippableFact]
        public void IpaDictSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _base, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"ipa-dict fr_FR sample base PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_Allophones_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _allophones, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"ipa-dict fr_FR sample allophones PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_NoExceptions_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_sample.tsv", _noExceptions, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12,
                $"ipa-dict fr_FR sample no_exceptions PER ({result.PhonemeErrorRate:P2}) が閾値 12% を超えています。");
        }

        // ========== ipa-dict fr_FR フル ==========

        [SkippableFact]
        public void IpaDictFull_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_fr_fr_full.tsv", _base, SourceKind.IpaDict);
            Assert.True(result.Cases >= 1000, $"フル語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12,
                $"ipa-dict fr_FR full base PER ({result.PhonemeErrorRate:P2}) が閾値 12% を超えています。");
        }

        // ========== WikiPron fra サンプル ==========

        [SkippableFact]
        public void WikiPronSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_fra_latn_broad_filtered_sample.tsv", _base, SourceKind.WikiPron);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"WikiPron fra sample base PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        // ========== WikiPron fra フル ==========

        [SkippableFact]
        public void WikiPronFull_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_fra_latn_broad_filtered_full.tsv", _base, SourceKind.WikiPron);
            Assert.True(result.Cases >= 1000, $"フル語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.12,
                $"WikiPron fra full base PER ({result.PhonemeErrorRate:P2}) が閾値 12% を超えています。");
        }

        public void Dispose()
        {
            _base.Dispose();
            _allophones.Dispose();
            _noExceptions.Dispose();
        }

        // ========== 共通ヘルパー ==========

        private CorpusResult EvaluateCorpus(string fileName, FrenchG2PEngine engine, SourceKind sourceKind)
        {
            var path = TryResolveTestDataPath(fileName);
            Skip.If(path == null, $"評価用TSVが見つかりません: {fileName}");

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
                var reference = NormalizeReference(row.Reference, sourceKind);
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
            foreach (var mismatch in mismatches.OrderByDescending(x => x.Distance).ThenBy(x => x.Word).Take(20))
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

        /// <summary>
        /// 予測音素を正規化する。
        /// Metropolitan方言の中和を反映: /ɑ/→/a/, /œ̃/→/ɛ̃/
        /// 異音も基底形に正規化: Rh→R
        /// </summary>
        private static string NormalizePredictedPhoneme(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case FrenchIpaPhoneme.A:
                case FrenchIpaPhoneme.Ah: return "a";  // /ɑ/→/a/ Metropolitan中和
                case FrenchIpaPhoneme.E: return "e";
                case FrenchIpaPhoneme.Eh: return "\u025B";  // ɛ
                case FrenchIpaPhoneme.I: return "i";
                case FrenchIpaPhoneme.O: return "o";
                case FrenchIpaPhoneme.Oh: return "\u0254";  // ɔ
                case FrenchIpaPhoneme.U: return "u";
                case FrenchIpaPhoneme.Y: return "y";
                case FrenchIpaPhoneme.Oe: return "\u00F8";  // ø
                case FrenchIpaPhoneme.Oeh: return "\u0153"; // œ
                case FrenchIpaPhoneme.Schwa: return "\u0259"; // ə
                case FrenchIpaPhoneme.ANasal: return "\u0251\u0303"; // ɑ̃
                case FrenchIpaPhoneme.ONasal: return "\u0254\u0303"; // ɔ̃
                case FrenchIpaPhoneme.ENasal: return "\u025B\u0303"; // ɛ̃
                case FrenchIpaPhoneme.OeNasal: return "\u025B\u0303"; // œ̃→ɛ̃ Metropolitan中和
                case FrenchIpaPhoneme.J: return "j";
                case FrenchIpaPhoneme.W: return "w";
                case FrenchIpaPhoneme.Uj: return "\u0265"; // ɥ
                case FrenchIpaPhoneme.P: return "p";
                case FrenchIpaPhoneme.B: return "b";
                case FrenchIpaPhoneme.T: return "t";
                case FrenchIpaPhoneme.D: return "d";
                case FrenchIpaPhoneme.K: return "k";
                case FrenchIpaPhoneme.G: return "\u0261"; // ɡ
                case FrenchIpaPhoneme.F: return "f";
                case FrenchIpaPhoneme.V: return "v";
                case FrenchIpaPhoneme.S: return "s";
                case FrenchIpaPhoneme.Z: return "z";
                case FrenchIpaPhoneme.Sh: return "\u0283"; // ʃ
                case FrenchIpaPhoneme.Zh: return "\u0292"; // ʒ
                case FrenchIpaPhoneme.M: return "m";
                case FrenchIpaPhoneme.N: return "n";
                case FrenchIpaPhoneme.Ny: return "\u0272"; // ɲ
                case FrenchIpaPhoneme.L: return "l";
                case FrenchIpaPhoneme.R:
                case FrenchIpaPhoneme.Rh: return "\u0281"; // ʁ（Rh異音→基底形R）
                case FrenchIpaPhoneme.Ng: return "\u014B"; // ŋ
                case FrenchIpaPhoneme.Ts: return "ts";
                case FrenchIpaPhoneme.Dz: return "dz";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }

        /// <summary>
        /// リファレンスIPA文字列を正規化してトークン配列に変換する。
        /// </summary>
        private static string[] NormalizeReference(string transcription, SourceKind sourceKind)
        {
            var raw = sourceKind == SourceKind.WikiPron
                ? transcription.Replace("|", " ").Trim()
                : transcription.Replace("/", string.Empty)
                    .Replace("\u02C8", string.Empty) // ˈ
                    .Replace("\u02CC", string.Empty) // ˌ
                    .Replace(".", string.Empty)
                    .Replace(" ", string.Empty)
                    .Trim();

            var tokens = sourceKind == SourceKind.WikiPron
                ? raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : TokenizeIpa(raw);

            for (var i = 0; i < tokens.Length; i++)
                tokens[i] = NormalizeReferenceToken(tokens[i]);

            return tokens.Where(token => token.Length > 0).ToArray();
        }

        /// <summary>
        /// リファレンスのIPA記号を正規化する。Metropolitan方言の中和を反映。
        /// </summary>
        private static string NormalizeReferenceToken(string token)
        {
            switch (token)
            {
                // /ɑ/→/a/ Metropolitan中和
                case "\u0251": return "a";
                // /g/→/ɡ/ (ASCII g → IPA ɡ)
                case "g": return "\u0261";
                // /œ̃/→/ɛ̃/ Metropolitan中和
                case "\u0153\u0303": return "\u025B\u0303";
                default: return token;
            }
        }

        /// <summary>
        /// IPA文字列をトークンに分割する。結合ダイアクリティカルマークを考慮。
        /// </summary>
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

                // 2文字シーケンスチェック（破擦音等）
                if (TryMatch(text, i, "ts", out var consumed)
                    || TryMatch(text, i, "dz", out consumed)
                    || TryMatch(text, i, "t\u0283", out consumed)  // tʃ
                    || TryMatch(text, i, "d\u0292", out consumed)) // dʒ
                {
                    tokens.Add(text.Substring(i, consumed));
                    i += consumed;
                    continue;
                }

                // 基本文字 + 後続の結合マーク（鼻母音のチルダ等）
                var start = i;
                i++;
                while (i < text.Length && IsCombiningMark(text[i]))
                    i++;

                tokens.Add(text.Substring(start, i - start));
            }

            return tokens.ToArray();
        }

        /// <summary>
        /// Unicode結合マーク（Combining Mark）かどうか判定する。
        /// </summary>
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

        private enum SourceKind : byte
        {
            WikiPron = 0,
            IpaDict = 1,
        }
    }
}
