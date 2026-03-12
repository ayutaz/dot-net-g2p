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

            var entries = new Dictionary<string, Dictionary<byte, string>>(StringComparer.Ordinal);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#' || line.StartsWith("surface\t", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 6)
                    continue;

                if (!TryParseMode(parts[1], out var modeKey))
                    continue;

                if (!entries.TryGetValue(parts[0], out var byMode))
                {
                    byMode = new Dictionary<byte, string>();
                    entries[parts[0]] = byMode;
                }

                byMode[modeKey] = parts[2];
            }

            return entries;
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
