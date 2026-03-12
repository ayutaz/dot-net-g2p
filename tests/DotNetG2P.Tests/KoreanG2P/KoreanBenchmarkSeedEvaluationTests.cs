using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Korean;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanBenchmarkSeedEvaluationTests
    {
        public static IEnumerable<object[]> M2SeedCases()
        {
            foreach (var fileName in new[] { "g2pk_parity.tsv", "official_gold.tsv", "weak_rules.tsv" })
            {
                var path = ResolveTestDataPath(fileName);
                var rows = File.ReadAllLines(path)
                    .Skip(1)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                    .Select(line => line.Split('\t'))
                    .Where(parts => parts.Length == 5 && !string.Equals(parts[3], "ui-variation", StringComparison.Ordinal));

                foreach (var row in rows)
                    yield return new object[] { row[0], row[1], row[2], row[3] };
            }
        }

        [Theory]
        [MemberData(nameof(M2SeedCases))]
        public void SeedCases_M2SupportedRules_MatchExpectedHangul(string input, string expected, string source, string ruleTag)
        {
            using var engine = new KoreanG2PEngine();

            var actual = engine.Analyze(input).ToHangulString();

            Assert.True(
                string.Equals(expected, actual, StringComparison.Ordinal),
                $"Expected '{expected}' but got '{actual}' for '{input}' ({source}, {ruleTag}).");
        }

        private static string ResolveTestDataPath(string fileName)
        {
            var repoRoot = ResolveRepoRoot();
            return Path.Combine(repoRoot, "tests", "TestData", "KoreanG2P", fileName);
        }

        private static string ResolveRepoRoot()
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                Path.GetFullPath("."),
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(Path.Combine(candidate, "src"))
                    && Directory.Exists(Path.Combine(candidate, "tests")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Repository root could not be resolved.");
        }
    }
}
