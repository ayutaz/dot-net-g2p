using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DotNetG2P.Tests.KoreanG2P.Benchmarking
{
    internal sealed class KoreanExternalBenchmarkConfiguration
    {
        public const string CorpusPathsEnvironmentVariable = "DOTNETG2P_KOREAN_EXTERNAL_CORPUS_PATHS";
        public const string MinimumCasesEnvironmentVariable = "DOTNETG2P_KOREAN_EXTERNAL_MIN_CASES";
        public const string AccuracyThresholdEnvironmentVariable = "DOTNETG2P_KOREAN_EXTERNAL_ACCURACY_THRESHOLD";

        public IReadOnlyList<string> CorpusPaths { get; }

        public int? MinimumCases { get; }

        public double? AccuracyThreshold { get; }

        public bool IsConfigured => CorpusPaths.Count > 0;

        private KoreanExternalBenchmarkConfiguration(
            IReadOnlyList<string> corpusPaths,
            int? minimumCases,
            double? accuracyThreshold)
        {
            CorpusPaths = corpusPaths ?? throw new ArgumentNullException(nameof(corpusPaths));
            MinimumCases = minimumCases;
            AccuracyThreshold = accuracyThreshold;
        }

        public static KoreanExternalBenchmarkConfiguration LoadFromEnvironment()
        {
            var corpusPaths = (Environment.GetEnvironmentVariable(CorpusPathsEnvironmentVariable) ?? string.Empty)
                .Split(new[] { Path.PathSeparator, '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeCorpusPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new KoreanExternalBenchmarkConfiguration(
                corpusPaths,
                ParseNullableInt(Environment.GetEnvironmentVariable(MinimumCasesEnvironmentVariable), MinimumCasesEnvironmentVariable),
                ParseNullableDouble(Environment.GetEnvironmentVariable(AccuracyThresholdEnvironmentVariable), AccuracyThresholdEnvironmentVariable));
        }

        private static int? ParseNullableInt(string? value, string environmentVariableName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) || parsedValue <= 0)
                throw new InvalidOperationException($"Environment variable '{environmentVariableName}' must be a positive integer.");

            return parsedValue;
        }

        private static double? ParseNullableDouble(string? value, string environmentVariableName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue)
                || parsedValue < 0d
                || parsedValue > 1d)
            {
                throw new InvalidOperationException($"Environment variable '{environmentVariableName}' must be a floating-point value between 0.0 and 1.0.");
            }

            return parsedValue;
        }

        private static string NormalizeCorpusPath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            return path;
        }
    }
}
