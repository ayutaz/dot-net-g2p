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
    /// 外部TSVコーパスを使ったポルトガル語G2P PER閾値テスト。
    /// TSVが存在しない場合はSkipする。
    /// </summary>
    [Trait("Category", "DatasetEvaluation")]
    public class PortugueseDatasetEvaluationTests : IDisposable
    {
        private readonly PortugueseG2PEngine _baseBp = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false));
        private readonly PortugueseG2PEngine _allophonesBp = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableAllophones: true));
        private readonly PortugueseG2PEngine _baseEp = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.European, includeStress: false));
        private readonly PortugueseG2PEngine _noExceptions = new PortugueseG2PEngine(new PortugueseG2POptions(
            dialect: PortugueseDialect.Brazilian, includeStress: false, enableExceptionDictionary: false));
        private readonly ITestOutputHelper _output;

        public PortugueseDatasetEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ========== ipa-dict pt_BR サンプル ==========

        [SkippableFact]
        public void IpaDictSample_PtBr_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _baseBp, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"ipa-dict pt_BR sample base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_PtBr_Allophones_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _allophonesBp, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.04,
                $"ipa-dict pt_BR sample allophones PER ({result.PhonemeErrorRate:P2}) が閾値 4% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_PtBr_NoExceptions_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_sample.tsv", _noExceptions, SourceKind.IpaDict);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.08,
                $"ipa-dict pt_BR sample no_exceptions PER ({result.PhonemeErrorRate:P2}) が閾値 8% を超えています。");
        }

        // ========== ipa-dict pt_BR フル ==========

        [SkippableFact]
        public void IpaDictFull_PtBr_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_full.tsv", _baseBp, SourceKind.IpaDict);
            Assert.True(result.Cases >= 1000, $"フル語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"ipa-dict pt_BR full base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        [SkippableFact]
        public void IpaDictFull_PtBr_Allophones_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_pt_br_full.tsv", _allophonesBp, SourceKind.IpaDict);
            Assert.True(result.Cases >= 1000, $"フル語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.04,
                $"ipa-dict pt_BR full allophones PER ({result.PhonemeErrorRate:P2}) が閾値 4% を超えています。");
        }

        // ========== WikiPron pt サンプル ==========

        [SkippableFact]
        public void WikiPronSample_Pt_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_pt_sample.tsv", _baseBp, SourceKind.WikiPron);
            Assert.True(result.Cases >= 100, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"WikiPron pt sample base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        // ========== WikiPron pt フル ==========

        [SkippableFact]
        public void WikiPronFull_Pt_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_pt_full.tsv", _baseBp, SourceKind.WikiPron);
            Assert.True(result.Cases >= 1000, $"フル語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"WikiPron pt full base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        // ========== EP方言テスト ==========

        [SkippableFact]
        public void WikiPronFull_Pt_European_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_pt_ep_full.tsv", _baseEp, SourceKind.WikiPron);
            Assert.True(result.Cases >= 100, $"EP語数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.05,
                $"WikiPron pt EP full base PER ({result.PhonemeErrorRate:P2}) が閾値 5% を超えています。");
        }

        public void Dispose()
        {
            _baseBp.Dispose();
            _allophonesBp.Dispose();
            _baseEp.Dispose();
            _noExceptions.Dispose();
        }

        // ========== 共通ヘルパー ==========

        private CorpusResult EvaluateCorpus(string fileName, PortugueseG2PEngine engine, SourceKind sourceKind)
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
                // tests/TestData/PortugueseG2P/ からの相対パス
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "PortugueseG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "PortugueseG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "PortugueseG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "PortugueseG2P", fileName)),
                // tools/eval_data/ からの相対パス
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

        /// <summary>
        /// 予測音素を正規化する。
        /// 異音を基底形に正規化: Ch→t+ʃ, Jh→d+ʒ, DarkL→l, Rh→ʁ等
        /// </summary>
        private static string NormalizePredictedPhoneme(PortugueseIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 口母音
                case PortugueseIpaPhoneme.A: return "a";
                case PortugueseIpaPhoneme.E: return "e";
                case PortugueseIpaPhoneme.Eh: return "\u025B"; // ɛ
                case PortugueseIpaPhoneme.I: return "i";
                case PortugueseIpaPhoneme.O: return "o";
                case PortugueseIpaPhoneme.Oh: return "\u0254"; // ɔ
                case PortugueseIpaPhoneme.U: return "u";
                case PortugueseIpaPhoneme.Schwa: return "\u0250"; // ɐ
                case PortugueseIpaPhoneme.HighCentral: return "\u0268"; // ɨ

                // 鼻母音
                case PortugueseIpaPhoneme.ANasal: return "\u0250\u0303"; // ɐ̃
                case PortugueseIpaPhoneme.ENasal: return "e\u0303"; // ẽ
                case PortugueseIpaPhoneme.INasal: return "i\u0303"; // ĩ
                case PortugueseIpaPhoneme.ONasal: return "\u00F5"; // õ
                case PortugueseIpaPhoneme.UNasal: return "u\u0303"; // ũ

                // 半母音
                case PortugueseIpaPhoneme.J: return "j";
                case PortugueseIpaPhoneme.W: return "w";

                // 破裂音
                case PortugueseIpaPhoneme.P: return "p";
                case PortugueseIpaPhoneme.B: return "b";
                case PortugueseIpaPhoneme.T: return "t";
                case PortugueseIpaPhoneme.D: return "d";
                case PortugueseIpaPhoneme.K: return "k";
                case PortugueseIpaPhoneme.G: return "\u0261"; // ɡ

                // 摩擦音
                case PortugueseIpaPhoneme.F: return "f";
                case PortugueseIpaPhoneme.V: return "v";
                case PortugueseIpaPhoneme.S: return "s";
                case PortugueseIpaPhoneme.Z: return "z";
                case PortugueseIpaPhoneme.Sh: return "\u0283"; // ʃ
                case PortugueseIpaPhoneme.Zh: return "\u0292"; // ʒ

                // 鼻音
                case PortugueseIpaPhoneme.M: return "m";
                case PortugueseIpaPhoneme.N: return "n";
                case PortugueseIpaPhoneme.Ny: return "\u0272"; // ɲ

                // 側面音（異音→基底形）
                case PortugueseIpaPhoneme.L:
                case PortugueseIpaPhoneme.DarkL: return "l"; // ɫ→l
                case PortugueseIpaPhoneme.Lh: return "\u028E"; // ʎ

                // ロティック
                case PortugueseIpaPhoneme.R: return "\u027E"; // ɾ
                case PortugueseIpaPhoneme.Rr: return "\u0281"; // ʁ

                // BP固有異音（基底形に正規化）
                case PortugueseIpaPhoneme.Ch: return "t\u0283"; // tʃ（tie-barなし）
                case PortugueseIpaPhoneme.Jh: return "d\u0292"; // dʒ（tie-barなし）
                case PortugueseIpaPhoneme.X: return "\u0283"; // →ʃ
                case PortugueseIpaPhoneme.H: return "h";

                // EP固有異音
                case PortugueseIpaPhoneme.Xh: return "\u0283"; // →ʃ

                // 共通異音（基底形に正規化）
                case PortugueseIpaPhoneme.Ng: return "\u014B"; // ŋ
                case PortugueseIpaPhoneme.NLabiodental: return "\u0271"; // ɱ
                case PortugueseIpaPhoneme.NDental: return "n"; // n̪→n

                // 弱化異音（基底形に正規化）
                case PortugueseIpaPhoneme.Beta: return "b"; // β→b
                case PortugueseIpaPhoneme.Dh: return "d"; // ð→d
                case PortugueseIpaPhoneme.Gh: return "\u0261"; // ɣ→ɡ

                // 鼻わたり音
                case PortugueseIpaPhoneme.WNasal: return "w\u0303"; // w̃
                case PortugueseIpaPhoneme.JNasal: return "j\u0303"; // j̃

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
        /// リファレンスのIPA記号を正規化する。
        /// </summary>
        private static string NormalizeReferenceToken(string token)
        {
            switch (token)
            {
                // ASCII g → IPA ɡ
                case "g": return "\u0261";
                // ɫ → l（dark L正規化）
                case "\u026B": return "l";
                // β → b（弱化異音→基底形）
                case "\u03B2": return "b";
                // ð → d
                case "\u00F0": return "d";
                // ɣ → ɡ
                case "\u0263": return "\u0261";
                // n̪ → n
                case "n\u032A": return "n";
                // tie-bar付き破擦音 → tie-barなし
                case "t\u0361\u0283": return "t\u0283"; // t͡ʃ → tʃ
                case "d\u0361\u0292": return "d\u0292"; // d͡ʒ → dʒ
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

                // 破擦音シーケンスチェック
                if (TryMatch(text, i, "t\u0361\u0283", out var consumed)    // t͡ʃ
                    || TryMatch(text, i, "d\u0361\u0292", out consumed)     // d͡ʒ
                    || TryMatch(text, i, "t\u0283", out consumed)           // tʃ
                    || TryMatch(text, i, "d\u0292", out consumed))          // dʒ
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
