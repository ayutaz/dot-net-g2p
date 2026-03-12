using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal sealed class KoreanBenchmarkCase
    {
        public string DatasetName { get; }

        public string DatasetFileName { get; }

        public string Input { get; }

        public string ExpectedDisplay { get; }

        public IReadOnlyList<string> AcceptedOutputs { get; }

        public string Source { get; }

        public string RuleTag { get; }

        public string Notes { get; }

        public KoreanBenchmarkCase(string datasetFileName, string input, string expected, string source, string ruleTag, string notes)
        {
            DatasetFileName = datasetFileName ?? throw new ArgumentNullException(nameof(datasetFileName));
            DatasetName = Path.GetFileNameWithoutExtension(datasetFileName);
            Input = input ?? throw new ArgumentNullException(nameof(input));
            ExpectedDisplay = expected ?? throw new ArgumentNullException(nameof(expected));
            AcceptedOutputs = expected
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Source = source ?? throw new ArgumentNullException(nameof(source));
            RuleTag = ruleTag ?? throw new ArgumentNullException(nameof(ruleTag));
            Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        }
    }

    internal sealed class KoreanBenchmarkCaseResult
    {
        public KoreanBenchmarkCase BenchmarkCase { get; }

        public string ActualOutput { get; }

        public bool IsMatch { get; }

        public KoreanBenchmarkCaseResult(KoreanBenchmarkCase benchmarkCase, string actualOutput, bool isMatch)
        {
            BenchmarkCase = benchmarkCase ?? throw new ArgumentNullException(nameof(benchmarkCase));
            ActualOutput = actualOutput ?? throw new ArgumentNullException(nameof(actualOutput));
            IsMatch = isMatch;
        }

        public KoreanBenchmarkMismatch ToMismatch()
        {
            return new KoreanBenchmarkMismatch(
                BenchmarkCase.DatasetName,
                BenchmarkCase.Input,
                ActualOutput,
                BenchmarkCase.ExpectedDisplay,
                BenchmarkCase.Source,
                BenchmarkCase.RuleTag,
                BenchmarkCase.Notes);
        }
    }

    internal sealed class KoreanBenchmarkRuleSummary
    {
        public string RuleTag { get; }

        public int TotalCases { get; }

        public int PassedCases { get; }

        public int FailedCases { get; }

        public double Accuracy { get; }

        public IReadOnlyList<string> Datasets { get; }

        public KoreanBenchmarkRuleSummary(string ruleTag, int totalCases, int passedCases, int failedCases, IReadOnlyList<string> datasets)
        {
            RuleTag = ruleTag ?? throw new ArgumentNullException(nameof(ruleTag));
            TotalCases = totalCases;
            PassedCases = passedCases;
            FailedCases = failedCases;
            Accuracy = totalCases == 0 ? 0d : (double)passedCases / totalCases;
            Datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));
        }
    }

    internal sealed class KoreanBenchmarkDatasetSummary
    {
        public string DatasetName { get; }

        public int TotalCases { get; }

        public int PassedCases { get; }

        public int FailedCases { get; }

        public double Accuracy { get; }

        public IReadOnlyList<KoreanBenchmarkRuleSummary> RuleSummaries { get; }

        public KoreanBenchmarkDatasetSummary(string datasetName, int totalCases, int passedCases, int failedCases, IReadOnlyList<KoreanBenchmarkRuleSummary> ruleSummaries)
        {
            DatasetName = datasetName ?? throw new ArgumentNullException(nameof(datasetName));
            TotalCases = totalCases;
            PassedCases = passedCases;
            FailedCases = failedCases;
            Accuracy = totalCases == 0 ? 0d : (double)passedCases / totalCases;
            RuleSummaries = ruleSummaries ?? throw new ArgumentNullException(nameof(ruleSummaries));
        }
    }

    internal sealed class KoreanBenchmarkMismatch
    {
        public string DatasetName { get; }

        public string Input { get; }

        public string ActualOutput { get; }

        public string ExpectedOutput { get; }

        public string Source { get; }

        public string RuleTag { get; }

        public string Notes { get; }

        public KoreanBenchmarkMismatch(string datasetName, string input, string actualOutput, string expectedOutput, string source, string ruleTag, string notes)
        {
            DatasetName = datasetName ?? throw new ArgumentNullException(nameof(datasetName));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            ActualOutput = actualOutput ?? throw new ArgumentNullException(nameof(actualOutput));
            ExpectedOutput = expectedOutput ?? throw new ArgumentNullException(nameof(expectedOutput));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            RuleTag = ruleTag ?? throw new ArgumentNullException(nameof(ruleTag));
            Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        }
    }

    internal sealed class KoreanBenchmarkRunResult
    {
        internal KoreanBenchmarkCaseResult[] CaseResultsInternal { get; }

        public int TotalCases { get; }

        public int PassedCases { get; }

        public int FailedCases { get; }

        public double Accuracy { get; }

        public IReadOnlyList<KoreanBenchmarkDatasetSummary> DatasetSummaries { get; }

        public IReadOnlyList<KoreanBenchmarkRuleSummary> RuleSummaries { get; }

        public IReadOnlyList<KoreanBenchmarkMismatch> Mismatches { get; }

        public KoreanBenchmarkRunResult(
            KoreanBenchmarkCaseResult[] caseResults,
            IReadOnlyList<KoreanBenchmarkDatasetSummary> datasetSummaries,
            IReadOnlyList<KoreanBenchmarkRuleSummary> ruleSummaries)
        {
            CaseResultsInternal = caseResults ?? throw new ArgumentNullException(nameof(caseResults));
            TotalCases = caseResults.Length;
            PassedCases = caseResults.Count(result => result.IsMatch);
            FailedCases = TotalCases - PassedCases;
            Accuracy = TotalCases == 0 ? 0d : (double)PassedCases / TotalCases;
            DatasetSummaries = datasetSummaries ?? throw new ArgumentNullException(nameof(datasetSummaries));
            RuleSummaries = ruleSummaries ?? throw new ArgumentNullException(nameof(ruleSummaries));
            Mismatches = caseResults
                .Where(result => !result.IsMatch)
                .Select(result => result.ToMismatch())
                .ToArray();
        }
    }

    internal sealed class KoreanBenchmarkReportPaths
    {
        public string SummaryJsonPath { get; }

        public string DatasetSummaryTsvPath { get; }

        public string RuleSummaryTsvPath { get; }

        public string MismatchTsvPath { get; }

        public KoreanBenchmarkReportPaths(string summaryJsonPath, string datasetSummaryTsvPath, string ruleSummaryTsvPath, string mismatchTsvPath)
        {
            SummaryJsonPath = summaryJsonPath ?? throw new ArgumentNullException(nameof(summaryJsonPath));
            DatasetSummaryTsvPath = datasetSummaryTsvPath ?? throw new ArgumentNullException(nameof(datasetSummaryTsvPath));
            RuleSummaryTsvPath = ruleSummaryTsvPath ?? throw new ArgumentNullException(nameof(ruleSummaryTsvPath));
            MismatchTsvPath = mismatchTsvPath ?? throw new ArgumentNullException(nameof(mismatchTsvPath));
        }
    }
}
