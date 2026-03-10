using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.French.Data
{
    internal static class FrenchExceptionDictionary
    {
        private const byte AnyDialectKey = byte.MaxValue;
        private static readonly Dictionary<string, Dictionary<byte, FrenchPronunciation>> s_entries = LoadEntries();

        public static bool TryLookup(string word, FrenchDialect dialect, out FrenchPronunciation pronunciation)
        {
            pronunciation = null!;
            if (word == null || !s_entries.TryGetValue(word, out var byDialect))
                return false;

            if (byDialect.TryGetValue((byte)dialect, out pronunciation))
                return true;

            return byDialect.TryGetValue(AnyDialectKey, out pronunciation);
        }

        private static Dictionary<string, Dictionary<byte, FrenchPronunciation>> LoadEntries()
        {
            var assembly = typeof(FrenchExceptionDictionary).Assembly;
            using var stream = assembly.GetManifestResourceStream("DotNetG2P.French.Data.french_exceptions.master.tsv")
                ?? throw new InvalidOperationException("Embedded resource not found: french_exceptions.master.tsv");
            using var reader = new StreamReader(stream);

            var entries = new Dictionary<string, Dictionary<byte, FrenchPronunciation>>(StringComparer.Ordinal);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#' || line.StartsWith("surface\t", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 6)
                    continue;

                var w = parts[0];
                if (!TryParseDialect(parts[1], out var dialectKey)
                    || !int.TryParse(parts[3], out var stressIndex))
                {
                    continue;
                }

                var pron = ParsePronunciation(parts[4], stressIndex);
                if (!entries.TryGetValue(w, out var byDialect))
                {
                    byDialect = new Dictionary<byte, FrenchPronunciation>();
                    entries[w] = byDialect;
                }

                byDialect[dialectKey] = pron;
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
                case "metropolitan":
                    dialect = (byte)FrenchDialect.Metropolitan;
                    return true;
                case "conservative":
                    dialect = (byte)FrenchDialect.Conservative;
                    return true;
                default:
                    dialect = AnyDialectKey;
                    return false;
            }
        }

        private static FrenchPronunciation ParsePronunciation(string value, int stressIndex)
        {
            var syllableSpecs = value.Split('|');
            var syllableOffsets = new int[syllableSpecs.Length];
            var phonemes = new List<FrenchPhoneme>(8);

            for (var i = 0; i < syllableSpecs.Length; i++)
            {
                syllableOffsets[i] = phonemes.Count;
                var tokens = syllableSpecs[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool nucleusSet = false;
                foreach (var token in tokens)
                {
                    var ipa = ParsePhoneme(token);
                    bool isNucleus = false;
                    if (!nucleusSet && IsVowelPhoneme(ipa))
                    {
                        isNucleus = true;
                        nucleusSet = true;
                    }
                    phonemes.Add(new FrenchPhoneme(ipa, isNucleus));
                }
            }

            return new FrenchPronunciation(phonemes.ToArray(), syllableOffsets, stressIndex);
        }

        private static bool IsVowelPhoneme(FrenchIpaPhoneme phoneme)
        {
            return phoneme <= FrenchIpaPhoneme.OeNasal;
        }

        private static FrenchIpaPhoneme ParsePhoneme(string token)
        {
            switch (token)
            {
                case "a": return FrenchIpaPhoneme.A;
                case "\u0251": return FrenchIpaPhoneme.Ah;          // ɑ
                case "e": return FrenchIpaPhoneme.E;
                case "\u025B": return FrenchIpaPhoneme.Eh;          // ɛ
                case "i": return FrenchIpaPhoneme.I;
                case "o": return FrenchIpaPhoneme.O;
                case "\u0254": return FrenchIpaPhoneme.Oh;          // ɔ
                case "u": return FrenchIpaPhoneme.U;
                case "y": return FrenchIpaPhoneme.Y;
                case "\u00F8": return FrenchIpaPhoneme.Oe;          // ø
                case "\u0153": return FrenchIpaPhoneme.Oeh;         // œ
                case "\u0259": return FrenchIpaPhoneme.Schwa;       // ə
                case "\u0251\u0303": return FrenchIpaPhoneme.ANasal; // ɑ̃
                case "\u0254\u0303": return FrenchIpaPhoneme.ONasal; // ɔ̃
                case "\u025B\u0303": return FrenchIpaPhoneme.ENasal; // ɛ̃
                case "\u0153\u0303": return FrenchIpaPhoneme.OeNasal;// œ̃
                case "j": return FrenchIpaPhoneme.J;
                case "w": return FrenchIpaPhoneme.W;
                case "\u0265": return FrenchIpaPhoneme.Uj;          // ɥ
                case "p": return FrenchIpaPhoneme.P;
                case "b": return FrenchIpaPhoneme.B;
                case "t": return FrenchIpaPhoneme.T;
                case "d": return FrenchIpaPhoneme.D;
                case "k": return FrenchIpaPhoneme.K;
                case "\u0261": return FrenchIpaPhoneme.G;           // ɡ
                case "f": return FrenchIpaPhoneme.F;
                case "v": return FrenchIpaPhoneme.V;
                case "s": return FrenchIpaPhoneme.S;
                case "z": return FrenchIpaPhoneme.Z;
                case "\u0283": return FrenchIpaPhoneme.Sh;          // ʃ
                case "\u0292": return FrenchIpaPhoneme.Zh;          // ʒ
                case "m": return FrenchIpaPhoneme.M;
                case "n": return FrenchIpaPhoneme.N;
                case "\u0272": return FrenchIpaPhoneme.Ny;          // ɲ
                case "l": return FrenchIpaPhoneme.L;
                case "\u0281": return FrenchIpaPhoneme.R;           // ʁ
                case "\u03C7": return FrenchIpaPhoneme.Rh;          // χ
                case "\u014B": return FrenchIpaPhoneme.Ng;          // ŋ
                case "ts": return FrenchIpaPhoneme.Ts;
                case "dz": return FrenchIpaPhoneme.Dz;
                default:
                    throw new InvalidOperationException("Unknown phoneme token in exception dictionary: " + token);
            }
        }
    }
}
