using System;
using System.IO;
using System.Linq;
using DotNetG2P.Tests.KoreanG2P.Benchmarking;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.KoreanG2P
{
    [Trait("Category", "DatasetEvaluation")]
    public class KoreanExternalBenchmarkTests
    {
        private readonly ITestOutputHelper _output;

        public KoreanExternalBenchmarkTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SkippableFact]
        public void ConfiguredExternalCorpora_ExactAccuracy_IsReportedAndCanBeGated()
        {
            var configuration = KoreanExternalBenchmarkConfiguration.LoadFromEnvironment();
            Skip.If(
                !configuration.IsConfigured,
                $"External corpora are not configured. Set '{KoreanExternalBenchmarkConfiguration.CorpusPathsEnvironmentVariable}'.");

            var benchmarkCases = configuration.CorpusPaths
                .SelectMany(KoreanBenchmarkDataLoader.LoadCasesFromPath)
                .ToArray();

            Assert.NotEmpty(benchmarkCases);

            if (configuration.MinimumCases.HasValue)
            {
                Assert.True(
                    benchmarkCases.Length >= configuration.MinimumCases.Value,
                    $"External corpus size {benchmarkCases.Length} is below configured minimum {configuration.MinimumCases.Value}.");
            }

            var result = KoreanBenchmarkHarness.Evaluate(benchmarkCases);
            var outputDirectory = Path.Combine(Path.GetTempPath(), "DotNetG2P.Korean.ExternalBenchmark", Guid.NewGuid().ToString("N"));

            try
            {
                var reportPaths = KoreanBenchmarkReportWriter.Write(result, outputDirectory);
                _output.WriteLine($"external cases={result.TotalCases}, accuracy={result.Accuracy:P2}, mismatches={result.FailedCases}");
                _output.WriteLine($"summary={reportPaths.SummaryJsonPath}");
                _output.WriteLine($"mismatches={reportPaths.MismatchTsvPath}");

                foreach (var datasetSummary in result.DatasetSummaries.OrderBy(summary => summary.DatasetName, StringComparer.Ordinal))
                    _output.WriteLine($"  {datasetSummary.DatasetName}: {datasetSummary.PassedCases}/{datasetSummary.TotalCases} ({datasetSummary.Accuracy:P2})");

                foreach (var mismatch in result.Mismatches.Take(20))
                    _output.WriteLine($"  mismatch: {mismatch.Input} => actual={mismatch.ActualOutput}, expected={mismatch.ExpectedOutput}");

                if (configuration.AccuracyThreshold.HasValue)
                {
                    Assert.True(
                        result.Accuracy >= configuration.AccuracyThreshold.Value,
                        $"External exact accuracy {result.Accuracy:P2} is below configured threshold {configuration.AccuracyThreshold.Value:P2}.");
                }
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
