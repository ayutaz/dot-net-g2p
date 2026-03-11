using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanBenchmarkMetadataTests
    {
        private static readonly HashSet<string> s_allowedRuleTags = new HashSet<string>(StringComparer.Ordinal)
        {
            "neutralization",
            "resyllabification",
            "tensification",
            "nasalization",
            "liquidization",
            "h-deletion",
            "n-insertion",
            "ui-variation",
            "place-assimilation",
        };

        [Theory]
        [InlineData("g2pk_parity.tsv", 8)]
        [InlineData("official_gold.tsv", 6)]
        [InlineData("weak_rules.tsv", 5)]
        public void BenchmarkFile_HasExpectedSchema(string fileName, int minimumRows)
        {
            var path = ResolveTestDataPath(fileName);
            Assert.True(File.Exists(path), $"Benchmark TSV not found: {path}");

            var lines = File.ReadAllLines(path);
            Assert.NotEmpty(lines);
            Assert.Equal("input\texpected\tsource\trule_tag\tnotes", lines[0]);

            var rows = lines
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                .Select(line => new { Line = line, Parts = line.Split('\t') })
                .ToArray();

            Assert.True(rows.Length >= minimumRows, $"Expected at least {minimumRows} data rows in {fileName}, but found {rows.Length}.");

            Assert.All(rows, row =>
            {
                Assert.Equal(5, row.Parts.Length);
                Assert.All(row.Parts, part => Assert.False(string.IsNullOrWhiteSpace(part), $"TSV row contains blank field: {row.Line}"));
                Assert.Contains(row.Parts[3], s_allowedRuleTags);
            });
        }

        [Fact]
        public void Readme_DescribesExpectedFormatAndBenchmarkFiles()
        {
            var path = ResolveTestDataPath("README.md");
            Assert.True(File.Exists(path), $"README not found: {path}");

            var text = File.ReadAllText(path);
            Assert.Contains("g2pk_parity.tsv", text, StringComparison.Ordinal);
            Assert.Contains("official_gold.tsv", text, StringComparison.Ordinal);
            Assert.Contains("weak_rules.tsv", text, StringComparison.Ordinal);
            Assert.Contains("expected", text, StringComparison.Ordinal);
            Assert.Contains("pronunciation in Hangul", text, StringComparison.Ordinal);
        }

        [Fact]
        public void SeedFiles_CoverAllPlannedRuleTags()
        {
            var observed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var fileName in new[] { "g2pk_parity.tsv", "official_gold.tsv", "weak_rules.tsv" })
            {
                var path = ResolveTestDataPath(fileName);
                var tags = File.ReadAllLines(path)
                    .Skip(1)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                    .Select(line => line.Split('\t')[3]);

                foreach (var tag in tags)
                    observed.Add(tag);
            }

            Assert.Equal(s_allowedRuleTags.Count, observed.Count);
            Assert.All(s_allowedRuleTags, expectedTag => Assert.Contains(expectedTag, observed));
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
