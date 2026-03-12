using System.IO;
using System.Linq;
using DotNetG2P.Tests.KoreanG2P.Benchmarking;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanBenchmarkHarnessTests
    {
        [Fact]
        public void EvaluateAll_CurrentSeedsHaveZeroMismatches()
        {
            var result = KoreanBenchmarkHarness.EvaluateAll();

            Assert.Equal(KoreanBenchmarkDataLoader.DatasetNames, result.DatasetSummaries.Select(summary => summary.DatasetName).ToArray());
            Assert.Equal(result.TotalCases, result.PassedCases);
            Assert.Equal(0, result.FailedCases);
            Assert.Empty(result.Mismatches);
            Assert.All(result.DatasetSummaries, summary => Assert.Equal(summary.TotalCases, summary.PassedCases));
        }

        [Fact]
        public void EvaluateAll_AggregatesRuleTagsAcrossDatasets()
        {
            var benchmarkCases = KoreanBenchmarkDataLoader.LoadAllCases();
            var result = KoreanBenchmarkHarness.Evaluate(benchmarkCases);

            var expectedCounts = benchmarkCases
                .GroupBy(benchmarkCase => benchmarkCase.RuleTag, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            Assert.Equal(expectedCounts.Count, result.RuleSummaries.Count);
            Assert.Equal(result.TotalCases, result.RuleSummaries.Sum(summary => summary.TotalCases));

            Assert.All(result.RuleSummaries, summary =>
            {
                Assert.Equal(expectedCounts[summary.RuleTag], summary.TotalCases);
                Assert.Equal(summary.TotalCases, summary.PassedCases);
                Assert.Equal(0, summary.FailedCases);
                Assert.NotEmpty(summary.Datasets);
            });
        }

        [Fact]
        public void WriteReports_CreatesSummaryAndMismatchArtifacts()
        {
            var result = KoreanBenchmarkHarness.EvaluateAll();

            var reportPaths = KoreanBenchmarkReportWriter.Write(result);

            Assert.True(File.Exists(reportPaths.SummaryJsonPath));
            Assert.True(File.Exists(reportPaths.DatasetSummaryTsvPath));
            Assert.True(File.Exists(reportPaths.RuleSummaryTsvPath));
            Assert.True(File.Exists(reportPaths.MismatchTsvPath));

            var datasetSummaryLines = File.ReadAllLines(reportPaths.DatasetSummaryTsvPath);
            Assert.Equal("dataset\ttotal\tpassed\tfailed\taccuracy", datasetSummaryLines[0]);
            Assert.Equal(result.DatasetSummaries.Count + 1, datasetSummaryLines.Length);

            var ruleSummaryLines = File.ReadAllLines(reportPaths.RuleSummaryTsvPath);
            Assert.Equal("rule_tag\ttotal\tpassed\tfailed\taccuracy\tdatasets", ruleSummaryLines[0]);
            Assert.Equal(result.RuleSummaries.Count + 1, ruleSummaryLines.Length);

            var mismatchLines = File.ReadAllLines(reportPaths.MismatchTsvPath);
            Assert.Equal("dataset\tinput\tactual\texpected\tsource\trule_tag\tnotes", mismatchLines[0]);
            Assert.Single(mismatchLines);
        }
    }
}
