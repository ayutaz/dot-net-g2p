using System;
using System.Collections.Generic;
using DotNetG2P.Korean;
using DotNetG2P.Tests.KoreanG2P.Benchmarking;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanBenchmarkSeedEvaluationTests
    {
        public static IEnumerable<object[]> M2SeedCases()
        {
            foreach (var benchmarkCase in KoreanBenchmarkDataLoader.LoadAllCases())
            {
                yield return new object[]
                {
                    benchmarkCase.DatasetFileName,
                    benchmarkCase.Input,
                    benchmarkCase.ExpectedDisplay,
                    benchmarkCase.Source,
                    benchmarkCase.RuleTag,
                };
            }
        }

        [Theory]
        [MemberData(nameof(M2SeedCases))]
        public void SeedCases_M2SupportedRules_MatchExpectedHangul(string fileName, string input, string expected, string source, string ruleTag)
        {
            using var engine = new KoreanG2PEngine();

            var actual = engine.Analyze(input).ToHangulString();
            var accepted = expected.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            Assert.True(
                accepted.Any(candidate => string.Equals(candidate, actual, StringComparison.Ordinal)),
                $"Expected one of '{expected}' but got '{actual}' for '{input}' in {fileName} ({source}, {ruleTag}).");
        }
    }
}
