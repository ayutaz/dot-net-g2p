using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal static class KoreanBenchmarkReportWriter
    {
        public static KoreanBenchmarkReportPaths Write(KoreanBenchmarkRunResult result)
        {
            return Write(result, KoreanBenchmarkPaths.EnsureResultsDirectory());
        }

        public static KoreanBenchmarkReportPaths Write(KoreanBenchmarkRunResult result, string outputDirectory)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

            Directory.CreateDirectory(outputDirectory);
            var reportPaths = new KoreanBenchmarkReportPaths(
                Path.Combine(outputDirectory, "korean-benchmark-summary.json"),
                Path.Combine(outputDirectory, "korean-benchmark-dataset-summary.tsv"),
                Path.Combine(outputDirectory, "korean-benchmark-rule-summary.tsv"),
                Path.Combine(outputDirectory, "korean-benchmark-mismatches.tsv"));

            WriteSummaryJson(reportPaths.SummaryJsonPath, result);
            WriteDatasetSummaryTsv(reportPaths.DatasetSummaryTsvPath, result);
            WriteRuleSummaryTsv(reportPaths.RuleSummaryTsvPath, result);
            WriteMismatchTsv(reportPaths.MismatchTsvPath, result);

            return reportPaths;
        }

        private static void WriteSummaryJson(string path, KoreanBenchmarkRunResult result)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            File.WriteAllText(path, json);
        }

        private static void WriteDatasetSummaryTsv(string path, KoreanBenchmarkRunResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("dataset\ttotal\tpassed\tfailed\taccuracy");

            foreach (var summary in result.DatasetSummaries)
            {
                builder
                    .Append(SanitizeTsvField(summary.DatasetName)).Append('\t')
                    .Append(summary.TotalCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(summary.PassedCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(summary.FailedCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(FormatAccuracy(summary.Accuracy))
                    .AppendLine();
            }

            File.WriteAllText(path, builder.ToString());
        }

        private static void WriteRuleSummaryTsv(string path, KoreanBenchmarkRunResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("rule_tag\ttotal\tpassed\tfailed\taccuracy\tdatasets");

            foreach (var summary in result.RuleSummaries)
            {
                builder
                    .Append(SanitizeTsvField(summary.RuleTag)).Append('\t')
                    .Append(summary.TotalCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(summary.PassedCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(summary.FailedCases.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(FormatAccuracy(summary.Accuracy)).Append('\t')
                    .Append(SanitizeTsvField(string.Join("|", summary.Datasets)))
                    .AppendLine();
            }

            File.WriteAllText(path, builder.ToString());
        }

        private static void WriteMismatchTsv(string path, KoreanBenchmarkRunResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("dataset\tinput\tactual\texpected\tsource\trule_tag\tnotes");

            foreach (var mismatch in result.Mismatches)
            {
                builder
                    .Append(SanitizeTsvField(mismatch.DatasetName)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.Input)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.ActualOutput)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.ExpectedOutput)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.Source)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.RuleTag)).Append('\t')
                    .Append(SanitizeTsvField(mismatch.Notes))
                    .AppendLine();
            }

            File.WriteAllText(path, builder.ToString());
        }

        private static string FormatAccuracy(double accuracy)
        {
            return accuracy.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private static string SanitizeTsvField(string value)
        {
            return (value ?? string.Empty)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Replace("\t", " ", StringComparison.Ordinal);
        }
    }
}
