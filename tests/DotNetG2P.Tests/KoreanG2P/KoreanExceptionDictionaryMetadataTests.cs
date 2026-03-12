using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanExceptionDictionaryMetadataTests
    {
        [Fact]
        public void MasterDictionary_ContainsModeCategoryAndSourceMetadata()
        {
            var repoRoot = ResolveRepoRoot();
            var masterPath = Path.Combine(repoRoot, "src", "DotNetG2P.Korean", "Data", "korean_exceptions.master.tsv");

            var rows = File.ReadAllLines(masterPath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", System.StringComparison.Ordinal) && !line.StartsWith("surface\t", System.StringComparison.Ordinal))
                .Select(line => line.Split('\t'))
                .ToArray();

            Assert.NotEmpty(rows);
            Assert.All(rows, parts =>
            {
                Assert.True(parts.Length >= 6);
                Assert.NotEmpty(parts[1]);
                Assert.NotEmpty(parts[3]);
                Assert.NotEmpty(parts[4]);
            });
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
