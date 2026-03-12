using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal static class KoreanBenchmarkDataLoader
    {
        private const string ExpectedHeader = "input\texpected\tsource\trule_tag\tnotes";

        private static readonly string[] s_datasetFiles =
        {
            "g2pk_parity.tsv",
            "official_gold.tsv",
            "weak_rules.tsv",
        };

        private static readonly string[] s_datasetNames = s_datasetFiles
            .Select(fileName => Path.GetFileNameWithoutExtension(fileName) ?? fileName)
            .ToArray();

        private static readonly Dictionary<string, int> s_datasetOrder = s_datasetNames
            .Select((name, index) => new KeyValuePair<string, int>(name, index))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        public static IReadOnlyList<string> DatasetFiles => s_datasetFiles;

        public static IReadOnlyList<string> DatasetNames => s_datasetNames;

        public static int GetDatasetOrder(string datasetName)
        {
            if (datasetName == null) throw new ArgumentNullException(nameof(datasetName));

            return s_datasetOrder.TryGetValue(datasetName, out var order)
                ? order
                : int.MaxValue;
        }

        public static IReadOnlyList<KoreanBenchmarkCase> LoadAllCases()
        {
            var cases = new List<KoreanBenchmarkCase>();
            foreach (var fileName in s_datasetFiles)
                cases.AddRange(LoadCases(fileName));

            return cases;
        }

        public static IReadOnlyList<KoreanBenchmarkCase> LoadAllCases(IEnumerable<string> externalPaths)
        {
            if (externalPaths == null) throw new ArgumentNullException(nameof(externalPaths));

            var cases = new List<KoreanBenchmarkCase>();
            foreach (var fileName in s_datasetFiles)
                cases.AddRange(LoadCases(fileName));

            foreach (var externalPath in externalPaths)
                cases.AddRange(LoadCasesFromPath(externalPath));

            return cases;
        }

        public static IReadOnlyList<KoreanBenchmarkCase> LoadCases(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Benchmark file name is required.", nameof(fileName));

            var path = KoreanBenchmarkPaths.ResolveDataPath(fileName);
            return LoadCasesFromPath(path);
        }

        public static IReadOnlyList<KoreanBenchmarkCase> LoadCasesFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Benchmark path is required.", nameof(path));

            var fullPath = ResolveExistingPath(path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Benchmark TSV not found: {fullPath}", fullPath);

            var cases = new List<KoreanBenchmarkCase>();
            using var iterator = File.ReadLines(fullPath).GetEnumerator();
            if (!iterator.MoveNext())
                throw new InvalidDataException($"Benchmark TSV is empty: {fullPath}");

            if (!string.Equals(iterator.Current, ExpectedHeader, StringComparison.Ordinal))
                throw new InvalidDataException($"Unexpected benchmark header in {fullPath}: {iterator.Current}");

            var datasetFileName = Path.GetFileName(fullPath);
            var lineIndex = 1;
            while (iterator.MoveNext())
            {
                lineIndex++;
                var line = iterator.Current;
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length != 5)
                    throw new InvalidDataException($"Expected 5 columns in {fullPath} line {lineIndex}, but found {parts.Length}: {line}");

                cases.Add(new KoreanBenchmarkCase(
                    datasetFileName,
                    parts[0],
                    parts[1],
                    parts[2],
                    parts[3],
                    parts[4]));
            }

            return cases;
        }

        private static string ResolveExistingPath(string path)
        {
            var candidate = Path.GetFullPath(path);
            if (File.Exists(candidate))
                return candidate;

            if (!Path.IsPathRooted(path))
            {
                var repoRelativeCandidate = Path.GetFullPath(Path.Combine(KoreanBenchmarkPaths.RepoRoot, path));
                if (File.Exists(repoRelativeCandidate))
                    return repoRelativeCandidate;
            }

            return candidate;
        }
    }
}
