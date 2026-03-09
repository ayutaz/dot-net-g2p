using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Spanish;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishAllophoneEvaluationTests : IDisposable
    {
        private readonly SpanishG2PEngine _obligatory = new SpanishG2PEngine(new SpanishG2POptions(
            enableAllophones: true,
            allophoneFeatures: SpanishAllophoneFeatures.Obligatory));
        private readonly SpanishG2PEngine _default = new SpanishG2PEngine(new SpanishG2POptions(
            enableAllophones: true,
            allophoneFeatures: SpanishAllophoneFeatures.Default));
        private readonly SpanishG2PEngine _all = new SpanishG2PEngine(new SpanishG2POptions(
            enableAllophones: true,
            allophoneFeatures: SpanishAllophoneFeatures.All));
        private readonly ITestOutputHelper _output;

        public SpanishAllophoneEvaluationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CuratedAllophoneCorpus_MatchesExpectedProfiles()
        {
            var rows = File.ReadAllLines(ResolvePath("spanish_allophone_reference.tsv"))
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .Select(parts => new Row(parts[0], parts[1], parts[2]))
                .ToArray();

            var grouped = rows.GroupBy(row => row.Profile, StringComparer.Ordinal);
            foreach (var group in grouped)
            {
                var engine = GetEngine(group.Key);
                var failures = new List<string>();

                foreach (var row in group)
                {
                    var actual = engine.ToIPA(row.Word);
                    if (!string.Equals(actual, row.Expected, StringComparison.Ordinal))
                        failures.Add($"{row.Word}: expected={row.Expected}, actual={actual}");
                }

                _output.WriteLine($"{group.Key}: {group.Count()} cases, failures={failures.Count}");
                foreach (var failure in failures)
                    _output.WriteLine("  " + failure);

                Assert.Empty(failures);
            }
        }

        public void Dispose()
        {
            _obligatory.Dispose();
            _default.Dispose();
            _all.Dispose();
        }

        private SpanishG2PEngine GetEngine(string profile)
        {
            switch (profile)
            {
                case "obligatory": return _obligatory;
                case "default": return _default;
                case "all": return _all;
                default: throw new InvalidOperationException("Unknown allophone profile: " + profile);
            }
        }

        private static string ResolvePath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData", "SpanishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "SpanishG2P", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "SpanishG2P", fileName),
                Path.GetFullPath(Path.Combine("tests", "TestData", "SpanishG2P", fileName)),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            throw new FileNotFoundException($"Spanish allophone reference not found: {fileName}");
        }

        private readonly struct Row
        {
            public string Word { get; }
            public string Profile { get; }
            public string Expected { get; }

            public Row(string word, string profile, string expected)
            {
                Word = word;
                Profile = profile;
                Expected = expected;
            }
        }
    }
}
