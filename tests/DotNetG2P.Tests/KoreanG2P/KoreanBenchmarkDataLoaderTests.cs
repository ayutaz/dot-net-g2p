using System;
using System.IO;
using System.Linq;
using DotNetG2P.Tests.KoreanG2P.Benchmarking;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanBenchmarkDataLoaderTests
    {
        [Fact]
        public void LoadCasesFromPath_ParsesExternalTsvWithExpectedSchema()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "DotNetG2P.Korean.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            var path = Path.Combine(tempDirectory, "external_gold.tsv");

            try
            {
                File.WriteAllText(
                    path,
                    "input\texpected\tsource\trule_tag\tnotes" + Environment.NewLine +
                    "좋다\t조타\tOfficial\th-aspiration\ttemp case" + Environment.NewLine +
                    "담임\t다밈\tOfficial\tnasalization\ttemp case");

                var cases = KoreanBenchmarkDataLoader.LoadCasesFromPath(path);

                Assert.Equal(2, cases.Count);
                Assert.All(cases, benchmarkCase => Assert.Equal("external_gold", benchmarkCase.DatasetName));
                Assert.Equal(new[] { "좋다", "담임" }, cases.Select(benchmarkCase => benchmarkCase.Input).ToArray());
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void LoadAllCases_WithExternalPaths_AppendsExternalDatasetsAfterSeedCases()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "DotNetG2P.Korean.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            var path = Path.Combine(tempDirectory, "external_gold.tsv");

            try
            {
                File.WriteAllText(
                    path,
                    "input\texpected\tsource\trule_tag\tnotes" + Environment.NewLine +
                    "좋다\t조타\tOfficial\th-aspiration\ttemp case");

                var allCases = KoreanBenchmarkDataLoader.LoadAllCases(new[] { path });

                Assert.Contains(allCases, benchmarkCase => string.Equals(benchmarkCase.DatasetName, "external_gold", StringComparison.Ordinal));
                Assert.Equal("external_gold", allCases.Last().DatasetName);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void LoadCasesFromPath_ResolvesRepoRelativePaths()
        {
            var cases = KoreanBenchmarkDataLoader.LoadCasesFromPath(Path.Combine("tests", "TestData", "KoreanG2P", "official_gold.tsv"));

            Assert.NotEmpty(cases);
            Assert.All(cases, benchmarkCase => Assert.Equal("official_gold", benchmarkCase.DatasetName));
        }
    }
}
