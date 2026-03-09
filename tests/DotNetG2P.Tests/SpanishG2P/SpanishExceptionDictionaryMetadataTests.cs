using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishExceptionDictionaryMetadataTests
    {
        [Fact]
        public void MasterDictionary_GeneratedRuntimeFile_IsInSync()
        {
            var repoRoot = ResolveRepoRoot();
            var masterPath = Path.Combine(repoRoot, "src", "DotNetG2P.Spanish", "Data", "spanish_exceptions.master.tsv");
            var generatedPath = Path.Combine(repoRoot, "src", "DotNetG2P.Spanish", "Data", "spanish_exceptions.txt");

            var master = File.ReadAllLines(masterPath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#") && !line.StartsWith("surface\t"))
                .Select(line => line.Split('\t'))
                .Select(parts => $"{parts[0]}\t{parts[3]}\t{parts[4]}")
                .ToArray();

            var generated = File.ReadAllLines(generatedPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            Assert.Equal(master, generated);
        }

        [Fact]
        public void MasterDictionary_ContainsCategoryAndSourceMetadata()
        {
            var repoRoot = ResolveRepoRoot();
            var masterPath = Path.Combine(repoRoot, "src", "DotNetG2P.Spanish", "Data", "spanish_exceptions.master.tsv");

            var rows = File.ReadAllLines(masterPath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#") && !line.StartsWith("surface\t"))
                .Select(line => line.Split('\t'))
                .ToArray();

            Assert.All(rows, parts =>
            {
                Assert.True(parts.Length >= 6);
                Assert.NotEmpty(parts[2]);
                Assert.NotEmpty(parts[5]);
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
