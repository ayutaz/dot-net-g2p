using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DotNetG2P.Spanish.Data
{
    internal static class SpanishExceptionDictionary
    {
        private static readonly Dictionary<string, SpanishPronunciation> s_entries = LoadEntries();

        public static bool TryLookup(string word, out SpanishPronunciation pronunciation)
        {
            if (word == null)
            {
                pronunciation = null!;
                return false;
            }

            return s_entries.TryGetValue(word, out pronunciation!);
        }

        private static Dictionary<string, SpanishPronunciation> LoadEntries()
        {
            var assembly = typeof(SpanishExceptionDictionary).Assembly;
            using var stream = assembly.GetManifestResourceStream("DotNetG2P.Spanish.Data.spanish_exceptions.txt")
                ?? throw new InvalidOperationException("Embedded resource not found: spanish_exceptions.txt");
            using var reader = new StreamReader(stream);

            var entries = new Dictionary<string, SpanishPronunciation>(StringComparer.Ordinal);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                var parts = line.Split('\t');
                if (parts.Length != 3)
                    continue;

                var word = parts[0];
                if (!int.TryParse(parts[1], out var stressIndex))
                    continue;

                var syllableSpecs = parts[2].Split('|');
                var syllableOffsets = new int[syllableSpecs.Length];
                var phonemes = new List<SpanishPhoneme>(8);

                for (var i = 0; i < syllableSpecs.Length; i++)
                {
                    syllableOffsets[i] = phonemes.Count;
                    var tokens = syllableSpecs[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in tokens)
                        phonemes.Add(new SpanishPhoneme(ParsePhoneme(token), isStressed: false));
                }

                entries[word] = new SpanishPronunciation(phonemes.ToArray(), syllableOffsets, stressIndex);
            }

            return entries;
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
                default:
                    throw new InvalidOperationException("Unknown phoneme token in exception dictionary: " + token);
            }
        }
    }
}
