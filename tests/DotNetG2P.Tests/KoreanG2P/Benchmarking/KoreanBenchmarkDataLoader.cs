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

        public static IReadOnlyList<KoreanBenchmarkCase> LoadCases(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Benchmark file name is required.", nameof(fileName));

            var path = KoreanBenchmarkPaths.ResolveDataPath(fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Benchmark TSV not found: {path}", path);

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                throw new InvalidDataException($"Benchmark TSV is empty: {path}");

            if (!string.Equals(lines[0], ExpectedHeader, StringComparison.Ordinal))
                throw new InvalidDataException($"Unexpected benchmark header in {path}: {lines[0]}");

            var cases = new List<KoreanBenchmarkCase>();
            for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length != 5)
                    throw new InvalidDataException($"Expected 5 columns in {path} line {lineIndex + 1}, but found {parts.Length}: {line}");

                cases.Add(new KoreanBenchmarkCase(
                    fileName,
                    parts[0],
                    parts[1],
                    parts[2],
                    parts[3],
                    parts[4]));
            }

            return cases;
        }
    }
}
