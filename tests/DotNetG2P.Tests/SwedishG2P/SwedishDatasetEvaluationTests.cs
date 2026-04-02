using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Conversion;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// 外部TSVコーパスを使ったスウェーデン語G2P PER閾値テスト。
    /// TSVが存在しない場合はSkipする。
    /// </summary>
    [Trait("Category", "DatasetEvaluation")]
    public class SwedishDatasetEvaluationTests : IDisposable
    {
        private readonly SwedishG2PEngine _base;
        private readonly SwedishG2PEngine _noExceptions;
        private readonly ITestOutputHelper _output;

        public SwedishDatasetEvaluationTests(ITestOutputHelper output)
        {
            _output = output;

            // 辞書有効・ストレスなし・正規化なし
            _base = new SwedishG2PEngine(new SwedishG2POptions(
                dialect: SwedishDialect.Central,
                includeStress: false,
                enableTextNormalization: false,
                enableExceptionDictionary: true));

            // 辞書無効・ストレスなし・正規化なし
            _noExceptions = new SwedishG2PEngine(new SwedishG2POptions(
                dialect: SwedishDialect.Central,
                includeStress: false,
                enableTextNormalization: false,
                enableExceptionDictionary: false));
        }

        public void Dispose()
        {
            _base.Dispose();
            _noExceptions.Dispose();
        }

        // =================================================================
        // ipa-dict サンプル
        // =================================================================

        [SkippableFact]
        public void IpaDictSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_sv_se_sample.tsv", _base, SourceKind.IpaDict);
            Assert.True(result.Cases >= 50, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.21,
                $"ipa-dict sv sample base PER ({result.PhonemeErrorRate:P2}) が閾値 21% を超えています。");
        }

        [SkippableFact]
        public void IpaDictSample_NoExceptions_PerBelowThreshold()
        {
            var result = EvaluateCorpus("ipa_dict_sv_se_sample.tsv", _noExceptions, SourceKind.IpaDict);
            Assert.True(result.Cases >= 50, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.25,
                $"ipa-dict sv sample no_exceptions PER ({result.PhonemeErrorRate:P2}) が閾値 25% を超えています。");
        }

        // =================================================================
        // WikiPron サンプル
        // =================================================================

        [SkippableFact]
        public void WikiPronSample_Base_PerBelowThreshold()
        {
            var result = EvaluateCorpus("wikipron_swe_latn_broad_filtered_sample.tsv", _base, SourceKind.WikiPron);
            Assert.True(result.Cases >= 50, $"サンプル数が少なすぎます: {result.Cases}");
            Assert.True(result.PhonemeErrorRate < 0.25,
                $"WikiPron sv sample base PER ({result.PhonemeErrorRate:P2}) が閾値 25% を超えています。");
        }

        // =================================================================
        // ipa-dict PER改善テスト（辞書拡充後）
        // =================================================================

        [SkippableFact]
        public void IpaDictSample_Base_PerImprovedFromBaseline()
        {
            // 辞書拡充前の基準PER 23%からの改善を確認
            var result = EvaluateCorpus("ipa_dict_sv_se_sample.tsv", _base, SourceKind.IpaDict);
            Assert.True(result.Cases >= 50, $"サンプル数が少なすぎます: {result.Cases}");
            // 辞書拡充により PER < 21% に改善されていること
            Assert.True(result.PhonemeErrorRate < 0.21,
                $"ipa-dict PER ({result.PhonemeErrorRate:P2}) が改善目標 21% に未達。辞書拡充の効果が不十分です。");
        }

        // =================================================================
        // フォーマット検証テスト
        // =================================================================

        [SkippableFact]
        public void IpaDictSample_FileExists_ValidFormat()
        {
            var path = TryResolveTestDataPath("ipa_dict_sv_se_sample.tsv");
            Skip.If(path == null, "評価用TSVが見つかりません: ipa_dict_sv_se_sample.tsv");

            var lines = File.ReadAllLines(path!)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToArray();

            Assert.True(lines.Length > 0, "TSVファイルにデータ行がありません");

            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                Assert.True(parts.Length >= 2,
                    $"TSV行のフィールド数が不足しています: {line}");
                // ipa-dict形式: /IPA/ （スラッシュで囲まれている）
                Assert.True(parts[1].TrimStart().StartsWith("/") && parts[1].TrimEnd().EndsWith("/"),
                    $"ipa-dict形式ではありません（スラッシュなし）: {parts[1]}");
            }
        }

        [SkippableFact]
        public void WikiPronSample_FileExists_ValidFormat()
        {
            var path = TryResolveTestDataPath("wikipron_swe_latn_broad_filtered_sample.tsv");
            Skip.If(path == null, "評価用TSVが見つかりません: wikipron_swe_latn_broad_filtered_sample.tsv");

            var lines = File.ReadAllLines(path!)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToArray();

            Assert.True(lines.Length > 0, "TSVファイルにデータ行がありません");

            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                Assert.True(parts.Length >= 2,
                    $"TSV行のフィールド数が不足しています: {line}");
                // WikiPron形式: スペース区切りの音素（スラッシュなし）
                Assert.DoesNotContain("/", parts[1]);
            }
        }

        // =================================================================
        // 上位エラー語出力テスト（デバッグ用）
        // =================================================================

        [SkippableFact]
        public void IpaDictSample_Base_OutputsTopErrors()
        {
            // PER閾値の検証ではなく、上位エラー語をテストログに出力する（デバッグ支援）
            var result = EvaluateCorpus("ipa_dict_sv_se_sample.tsv", _base, SourceKind.IpaDict);
            _output.WriteLine($"=== ipa-dict base 評価結果: PER={result.PhonemeErrorRate:P2}, cases={result.Cases} ===");
        }

        [SkippableFact]
        public void WikiPronSample_Base_OutputsTopErrors()
        {
            var result = EvaluateCorpus("wikipron_swe_latn_broad_filtered_sample.tsv", _base, SourceKind.WikiPron);
            _output.WriteLine($"=== WikiPron base 評価結果: PER={result.PhonemeErrorRate:P2}, cases={result.Cases} ===");
        }

        // =================================================================
        // 共通ヘルパー
        // =================================================================

        private CorpusResult EvaluateCorpus(string fileName, SwedishG2PEngine engine, SourceKind sourceKind)
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
                // tests/TestData/SwedishG2P/ からの相対パス
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "SwedishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "SwedishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "SwedishG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "SwedishG2P", fileName)),
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

        /// <summary>
        /// 予測音素をIPA文字列配列に正規化する。
        /// IpaConverter.ToSymbol() を使用して音素 → IPA文字列マッピングを行う。
        /// </summary>
        private static string[] NormalizePredicted(IReadOnlyList<SwedishPhoneme> phonemes)
        {
            var result = new string[phonemes.Count];
            for (var i = 0; i < phonemes.Count; i++)
                result[i] = NormalizePredictedPhoneme(phonemes[i].Phoneme);
            return result;
        }

        /// <summary>
        /// 予測音素を正規化する。IpaConverter.ToSymbol() と同じマッピングを使用。
        /// </summary>
        private static string NormalizePredictedPhoneme(SwedishIpaPhoneme phoneme)
        {
            return IpaConverter.ToSymbol(phoneme);
        }

        /// <summary>
        /// リファレンスIPA文字列を正規化してトークン配列に変換する。
        /// </summary>
        private static string[] NormalizeReference(string transcription, SourceKind sourceKind)
        {
            var raw = sourceKind == SourceKind.WikiPron
                ? transcription
                    .Replace("|", " ")
                    .Replace("\u02C8", string.Empty) // ˈ（第一ストレスマーク除去）
                    .Replace("\u02CC", string.Empty) // ˌ（第二ストレスマーク除去）
                    .Trim()
                : transcription.Replace("/", string.Empty)
                    .Replace("\u02C8", string.Empty) // ˈ（ストレスマーク除去）
                    .Replace("\u02CC", string.Empty) // ˌ（第二ストレスマーク除去）
                    .Replace("\u00B2", string.Empty) // ²（スウェーデン語トーンマーク除去）
                    .Replace("\u00B9", string.Empty) // ¹（スウェーデン語トーンマーク除去）
                    .Replace(".", string.Empty)       // 音節区切り除去
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
        /// 表記揺れを統一してPER比較の精度を上げる。
        /// </summary>
        private static string NormalizeReferenceToken(string token)
        {
            switch (token)
            {
                // ASCII g → IPA ɡ (U+0261)
                case "g": return "\u0261";

                // ストレスマーク（トークン分割後に残った場合）
                case "\u02C8": return string.Empty; // ˈ
                case "\u02CC": return string.Empty; // ˌ

                // トーンマーク
                case "\u00B2": return string.Empty; // ²
                case "\u00B9": return string.Empty; // ¹

                // ː（長音記号）は母音と結合済みのはずだが、単独で残った場合は除去
                case "\u02D0": return string.Empty; // ː
                case ":": return string.Empty;

                default: return token;
            }
        }

        /// <summary>
        /// IPA文字列をトークンに分割する。結合ダイアクリティカルマークと長母音を考慮。
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

                // 基本文字
                var start = i;
                i++;

                // 後続の結合マーク（鼻母音のチルダ等）
                while (i < text.Length && IsCombiningMark(text[i]))
                    i++;

                // 長音記号 ː (U+02D0) や : が後続する場合は長母音として結合
                if (i < text.Length && (text[i] == '\u02D0' || text[i] == ':'))
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
