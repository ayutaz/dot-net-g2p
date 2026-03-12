using System;
using System.Collections.Generic;
using System.Linq;
using DotNetG2P.Korean;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal static class KoreanBenchmarkHarness
    {
        public static KoreanBenchmarkRunResult EvaluateAll()
        {
            return Evaluate(KoreanBenchmarkDataLoader.LoadAllCases(), KoreanG2POptions.Default);
        }

        public static KoreanBenchmarkRunResult EvaluateAll(KoreanG2POptions options)
        {
            return Evaluate(KoreanBenchmarkDataLoader.LoadAllCases(), options);
        }

        public static KoreanBenchmarkRunResult Evaluate(IReadOnlyList<KoreanBenchmarkCase> benchmarkCases)
        {
            return Evaluate(benchmarkCases, KoreanG2POptions.Default);
        }

        public static KoreanBenchmarkRunResult Evaluate(IReadOnlyList<KoreanBenchmarkCase> benchmarkCases, KoreanG2POptions options)
        {
            if (benchmarkCases == null) throw new ArgumentNullException(nameof(benchmarkCases));
            if (options == null) throw new ArgumentNullException(nameof(options));

            using var engine = new KoreanG2PEngine(options);

            var caseResults = new KoreanBenchmarkCaseResult[benchmarkCases.Count];
            for (var i = 0; i < benchmarkCases.Count; i++)
                caseResults[i] = EvaluateCase(engine, benchmarkCases[i]);

            var datasetSummaries = caseResults
                .GroupBy(result => result.BenchmarkCase.DatasetName, StringComparer.Ordinal)
                .OrderBy(group => KoreanBenchmarkDataLoader.GetDatasetOrder(group.Key))
                .Select(group =>
                {
                    var groupedResults = group.ToArray();
                    return new KoreanBenchmarkDatasetSummary(
                        group.Key,
                        groupedResults.Length,
                        groupedResults.Count(result => result.IsMatch),
                        groupedResults.Count(result => !result.IsMatch),
                        BuildRuleSummaries(groupedResults));
                })
                .ToArray();

            var ruleSummaries = BuildRuleSummaries(caseResults);

            return new KoreanBenchmarkRunResult(caseResults, datasetSummaries, ruleSummaries);
        }

        public static KoreanBenchmarkCaseResult EvaluateCase(KoreanG2PEngine engine, KoreanBenchmarkCase benchmarkCase)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (benchmarkCase == null) throw new ArgumentNullException(nameof(benchmarkCase));

            var actual = engine.Analyze(benchmarkCase.Input).ToHangulString();
            var isMatch = benchmarkCase.AcceptedOutputs.Any(candidate => string.Equals(candidate, actual, StringComparison.Ordinal));

            return new KoreanBenchmarkCaseResult(benchmarkCase, actual, isMatch);
        }

        private static KoreanBenchmarkRuleSummary[] BuildRuleSummaries(IEnumerable<KoreanBenchmarkCaseResult> caseResults)
        {
            return caseResults
                .GroupBy(result => result.BenchmarkCase.RuleTag, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var groupedResults = group.ToArray();
                    var datasets = groupedResults
                        .Select(result => result.BenchmarkCase.DatasetName)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(KoreanBenchmarkDataLoader.GetDatasetOrder)
                        .ToArray();

                    return new KoreanBenchmarkRuleSummary(
                        group.Key,
                        groupedResults.Length,
                        groupedResults.Count(result => result.IsMatch),
                        groupedResults.Count(result => !result.IsMatch),
                        datasets);
                })
                .ToArray();
        }
    }
}
