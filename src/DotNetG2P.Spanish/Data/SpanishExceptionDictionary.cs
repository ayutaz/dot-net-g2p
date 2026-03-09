using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.Spanish.Data
{
    internal static class SpanishExceptionDictionary
    {
        private const byte AnyDialectKey = byte.MaxValue;
        private static readonly Dictionary<string, Dictionary<byte, SpanishPronunciation>> s_entries = LoadEntries();

        public static bool TryLookup(string word, SpanishDialect dialect, out SpanishPronunciation pronunciation)
        {
            pronunciation = null!;
            if (word == null || !s_entries.TryGetValue(word, out var byDialect))
                return false;

            if (byDialect.TryGetValue((byte)dialect, out pronunciation))
                return true;

            return byDialect.TryGetValue(AnyDialectKey, out pronunciation);
        }

        private static Dictionary<string, Dictionary<byte, SpanishPronunciation>> LoadEntries()
        {
            var assembly = typeof(SpanishExceptionDictionary).Assembly;
            using var stream = assembly.GetManifestResourceStream("DotNetG2P.Spanish.Data.spanish_exceptions.master.tsv")
                ?? throw new InvalidOperationException("Embedded resource not found: spanish_exceptions.master.tsv");
            using var reader = new StreamReader(stream);

            var entries = new Dictionary<string, Dictionary<byte, SpanishPronunciation>>(StringComparer.Ordinal);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#' || line.StartsWith("surface\t", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 6)
                    continue;

                var word = parts[0];
                if (!TryParseDialect(parts[1], out var dialectKey)
                    || !int.TryParse(parts[3], out var stressIndex))
                {
                    continue;
                }

                var pronunciation = ParsePronunciation(parts[4], stressIndex);
                if (!entries.TryGetValue(word, out var byDialect))
                {
                    byDialect = new Dictionary<byte, SpanishPronunciation>();
                    entries[word] = byDialect;
                }

                byDialect[dialectKey] = pronunciation;
            }

            return entries;
        }

        private static bool TryParseDialect(string token, out byte dialect)
        {
            switch (token)
            {
                case "*":
                    dialect = AnyDialectKey;
                    return true;
                case "la":
                case "latin_american":
                    dialect = (byte)SpanishDialect.LatinAmerican;
                    return true;
                case "castilian":
                case "es":
                    dialect = (byte)SpanishDialect.Castilian;
                    return true;
                default:
                    dialect = AnyDialectKey;
                    return false;
            }
        }

        private static SpanishPronunciation ParsePronunciation(string value, int stressIndex)
        {
            var syllableSpecs = value.Split('|');
            var syllableOffsets = new int[syllableSpecs.Length];
            var phonemes = new List<SpanishPhoneme>(8);

            for (var i = 0; i < syllableSpecs.Length; i++)
            {
                syllableOffsets[i] = phonemes.Count;
                var tokens = syllableSpecs[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                    phonemes.Add(new SpanishPhoneme(ParsePhoneme(token), isStressed: false));
            }

            return new SpanishPronunciation(phonemes.ToArray(), syllableOffsets, stressIndex);
        }

        private static SpanishIpaPhoneme ParsePhoneme(string token)
        {
            switch (token)
            {
                case "a": return SpanishIpaPhoneme.A;
                case "e": return SpanishIpaPhoneme.E;
                case "i": return SpanishIpaPhoneme.I;
                case "o": return SpanishIpaPhoneme.O;
                case "u": return SpanishIpaPhoneme.U;
                case "j": return SpanishIpaPhoneme.J;
                case "w": return SpanishIpaPhoneme.W;
                case "p": return SpanishIpaPhoneme.P;
                case "b": return SpanishIpaPhoneme.B;
                case "t": return SpanishIpaPhoneme.T;
                case "d": return SpanishIpaPhoneme.D;
                case "k": return SpanishIpaPhoneme.K;
                case "ɡ": return SpanishIpaPhoneme.G;
                case "f": return SpanishIpaPhoneme.F;
                case "s": return SpanishIpaPhoneme.S;
                case "x": return SpanishIpaPhoneme.X;
                case "tʃ": return SpanishIpaPhoneme.Ch;
                case "ʝ": return SpanishIpaPhoneme.Y;
                case "m": return SpanishIpaPhoneme.M;
                case "n": return SpanishIpaPhoneme.N;
                case "ɲ": return SpanishIpaPhoneme.Ny;
                case "l": return SpanishIpaPhoneme.L;
                case "r": return SpanishIpaPhoneme.Rr;
                case "ɾ": return SpanishIpaPhoneme.R;
                case "θ": return SpanishIpaPhoneme.Th;
                case "ʎ": return SpanishIpaPhoneme.Ll;
                case "β": return SpanishIpaPhoneme.Beta;
                case "ð": return SpanishIpaPhoneme.Dh;
                case "ɣ": return SpanishIpaPhoneme.Gh;
                case "z": return SpanishIpaPhoneme.Z;
                case "ɱ": return SpanishIpaPhoneme.NLabiodental;
                case "ŋ": return SpanishIpaPhoneme.Eng;
                case "ʃ": return SpanishIpaPhoneme.Sh;
                case "ɟʝ": return SpanishIpaPhoneme.YAffricate;
                case "n̪": return SpanishIpaPhoneme.NDental;
                default:
                    throw new InvalidOperationException("Unknown phoneme token in exception dictionary: " + token);
            }
        }
    }
}
