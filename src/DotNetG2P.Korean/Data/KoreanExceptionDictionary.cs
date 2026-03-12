using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.Korean.Data
{
    internal static class KoreanExceptionDictionary
    {
        private const byte AnyModeKey = byte.MaxValue;
        private static readonly Dictionary<string, Dictionary<byte, string>> s_entries = LoadEntries();

        public static bool TryLookup(string text, KoreanUiVariationMode uiVariationMode, out string pronunciation)
        {
            pronunciation = string.Empty;
            if (string.IsNullOrEmpty(text) || !s_entries.TryGetValue(text, out var byMode))
                return false;

            if (byMode.TryGetValue((byte)uiVariationMode, out pronunciation))
                return true;

            return byMode.TryGetValue(AnyModeKey, out pronunciation);
        }

        private static Dictionary<string, Dictionary<byte, string>> LoadEntries()
        {
            var assembly = typeof(KoreanExceptionDictionary).Assembly;
            using var stream = assembly.GetManifestResourceStream("DotNetG2P.Korean.Data.korean_exceptions.master.tsv")
                ?? throw new InvalidOperationException("Embedded resource not found: korean_exceptions.master.tsv");
            using var reader = new StreamReader(stream);

            return ParseEntries(ReadAllLines(reader));
        }

        internal static Dictionary<string, Dictionary<byte, string>> ParseEntries(IReadOnlyList<string> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var entries = new Dictionary<string, Dictionary<byte, string>>(StringComparer.Ordinal);
            for (var index = 0; index < lines.Count; index++)
            {
                var lineNumber = index + 1;
                var line = lines[index]?.Trim() ?? string.Empty;
                if (line.Length == 0 || line[0] == '#' || line.StartsWith("surface\t", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 6)
                    throw new InvalidDataException($"Expected at least 6 columns in exception dictionary line {lineNumber}, but found {parts.Length}: {line}");

                var surface = parts[0].Trim();
                var modeToken = parts[1].Trim();
                var pronunciation = parts[2].Trim();

                if (!TryParseMode(modeToken, out var modeKey))
                    throw new InvalidDataException($"Unknown ui_mode '{parts[1]}' in exception dictionary line {lineNumber}.");

                if (string.IsNullOrWhiteSpace(surface) || string.IsNullOrWhiteSpace(pronunciation))
                    throw new InvalidDataException($"surface/pronunciation must not be blank in exception dictionary line {lineNumber}.");

                if (!entries.TryGetValue(surface, out var byMode))
                {
                    byMode = new Dictionary<byte, string>();
                    entries[surface] = byMode;
                }

                if (byMode.ContainsKey(modeKey))
                    throw new InvalidDataException($"Duplicate exception dictionary entry for '{surface}' and mode '{modeToken}' at line {lineNumber}.");

                byMode.Add(modeKey, pronunciation);
            }

            return entries;
        }

        private static IReadOnlyList<string> ReadAllLines(StreamReader reader)
        {
            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
            return lines;
        }

        private static bool TryParseMode(string token, out byte mode)
        {
            switch (token)
            {
                case "*":
                    mode = AnyModeKey;
                    return true;
                case "standard":
                    mode = (byte)KoreanUiVariationMode.Standard;
                    return true;
                case "colloquial":
                    mode = (byte)KoreanUiVariationMode.Colloquial;
                    return true;
                default:
                    mode = AnyModeKey;
                    return false;
            }
        }
    }
}
