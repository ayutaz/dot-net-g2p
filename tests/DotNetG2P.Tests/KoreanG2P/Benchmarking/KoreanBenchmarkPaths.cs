using System;
using System.IO;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal static class KoreanBenchmarkPaths
    {
        private static readonly string s_repoRoot = ResolveRepoRoot();

        public static string RepoRoot => s_repoRoot;

        public static string DataDirectory => Path.Combine(RepoRoot, "tests", "TestData", "KoreanG2P");

        public static string ResultsDirectory => Path.Combine(RepoRoot, "tests", "DotNetG2P.Tests", "TestResults", "KoreanG2P");

        public static string ResolveDataPath(string fileName)
        {
            return Path.Combine(DataDirectory, fileName);
        }

        public static string EnsureResultsDirectory()
        {
            Directory.CreateDirectory(ResultsDirectory);
            return ResultsDirectory;
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
