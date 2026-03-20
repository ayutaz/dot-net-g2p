using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetG2P.Portuguese.Data
{
    internal static class PortugueseExceptionDictionary
    {
        private const byte AnyDialectKey = byte.MaxValue;
        private static readonly Dictionary<string, Dictionary<byte, PortuguesePronunciation>> s_entries = LoadEntries();

        public static bool TryLookup(string word, PortugueseDialect dialect, out PortuguesePronunciation pronunciation)
        {
            pronunciation = null!;
            if (word == null || !s_entries.TryGetValue(word, out var byDialect))
                return false;

            if (byDialect.TryGetValue((byte)dialect, out pronunciation))
                return true;

            return byDialect.TryGetValue(AnyDialectKey, out pronunciation);
        }

        private static Dictionary<string, Dictionary<byte, PortuguesePronunciation>> LoadEntries()
        {
            var entries = new Dictionary<string, Dictionary<byte, PortuguesePronunciation>>(StringComparer.Ordinal);
            try
            {
                var assembly = typeof(PortugueseExceptionDictionary).Assembly;
                using var stream = assembly.GetManifestResourceStream("DotNetG2P.Portuguese.Data.portuguese_exceptions.master.tsv");
                if (stream == null) return entries;
                using var reader = new StreamReader(stream);

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
                        byDialect = new Dictionary<byte, PortuguesePronunciation>();
                        entries[w] = byDialect;
                    }

                    byDialect[dialectKey] = pron;
                }
            }
            catch
            {
                return entries;
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
                case "brazilian":
                    dialect = (byte)PortugueseDialect.Brazilian;
                    return true;
                case "european":
                    dialect = (byte)PortugueseDialect.European;
                    return true;
                default:
                    dialect = AnyDialectKey;
                    return false;
            }
        }

        private static PortuguesePronunciation ParsePronunciation(string value, int stressIndex)
        {
            var syllableSpecs = value.Split('|');
            var syllableOffsets = new int[syllableSpecs.Length];
            var phonemes = new List<PortuguesePhoneme>(8);

            for (var i = 0; i < syllableSpecs.Length; i++)
            {
                syllableOffsets[i] = phonemes.Count;
                var tokens = syllableSpecs[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens)
                {
                    var ipa = ParsePhoneme(token);
                    phonemes.Add(new PortuguesePhoneme(ipa, false));
                }
            }

            return new PortuguesePronunciation(phonemes.ToArray(), syllableOffsets, stressIndex);
        }

        private static PortugueseIpaPhoneme ParsePhoneme(string token)
        {
            switch (token)
            {
                // 口母音
                case "a": return PortugueseIpaPhoneme.A;
                case "e": return PortugueseIpaPhoneme.E;
                case "\u025B": return PortugueseIpaPhoneme.Eh;          // ɛ
                case "i": return PortugueseIpaPhoneme.I;
                case "o": return PortugueseIpaPhoneme.O;
                case "\u0254": return PortugueseIpaPhoneme.Oh;          // ɔ
                case "u": return PortugueseIpaPhoneme.U;
                case "\u0250": return PortugueseIpaPhoneme.Schwa;       // ɐ
                case "\u0268": return PortugueseIpaPhoneme.HighCentral;  // ɨ

                // 鼻母音 (NFD: base + combining tilde)
                case "\u0250\u0303": return PortugueseIpaPhoneme.ANasal; // ɐ̃
                case "e\u0303": return PortugueseIpaPhoneme.ENasal;      // ẽ (NFD)
                case "i\u0303": return PortugueseIpaPhoneme.INasal;      // ĩ (NFD)
                case "\u00F5": return PortugueseIpaPhoneme.ONasal;       // õ (NFC precomposed)
                case "o\u0303": return PortugueseIpaPhoneme.ONasal;      // õ (NFD)
                case "u\u0303": return PortugueseIpaPhoneme.UNasal;      // ũ (NFD)
                // 鼻母音 (NFC precomposed)
                case "\u1EBD": return PortugueseIpaPhoneme.ENasal;       // ẽ (NFC)
                case "\u0129": return PortugueseIpaPhoneme.INasal;       // ĩ (NFC)
                case "\u0169": return PortugueseIpaPhoneme.UNasal;       // ũ (NFC)

                // 半母音
                case "j": return PortugueseIpaPhoneme.J;
                case "w": return PortugueseIpaPhoneme.W;

                // 鼻わたり音
                case "w\u0303": return PortugueseIpaPhoneme.WNasal;      // w̃
                case "j\u0303": return PortugueseIpaPhoneme.JNasal;      // j̃

                // 破裂音
                case "p": return PortugueseIpaPhoneme.P;
                case "b": return PortugueseIpaPhoneme.B;
                case "t": return PortugueseIpaPhoneme.T;
                case "d": return PortugueseIpaPhoneme.D;
                case "k": return PortugueseIpaPhoneme.K;
                case "\u0261": return PortugueseIpaPhoneme.G;           // ɡ (U+0261)
                case "g": return PortugueseIpaPhoneme.G;               // g (U+0067, ASCII fallback)

                // 摩擦音
                case "f": return PortugueseIpaPhoneme.F;
                case "v": return PortugueseIpaPhoneme.V;
                case "s": return PortugueseIpaPhoneme.S;
                case "z": return PortugueseIpaPhoneme.Z;
                case "\u0283": return PortugueseIpaPhoneme.Sh;          // ʃ
                case "\u0292": return PortugueseIpaPhoneme.Zh;          // ʒ

                // 鼻音
                case "m": return PortugueseIpaPhoneme.M;
                case "n": return PortugueseIpaPhoneme.N;
                case "\u0272": return PortugueseIpaPhoneme.Ny;          // ɲ

                // 側面音
                case "l": return PortugueseIpaPhoneme.L;
                case "\u028E": return PortugueseIpaPhoneme.Lh;          // ʎ

                // ロティック
                case "\u027E": return PortugueseIpaPhoneme.R;           // ɾ
                case "\u0281": return PortugueseIpaPhoneme.Rr;          // ʁ

                // BP固有異音
                case "t\u0361\u0283": return PortugueseIpaPhoneme.Ch;   // t͡ʃ
                case "d\u0361\u0292": return PortugueseIpaPhoneme.Jh;   // d͡ʒ
                case "x": return PortugueseIpaPhoneme.X;
                case "h": return PortugueseIpaPhoneme.H;

                // EP固有異音
                case "\u026B": return PortugueseIpaPhoneme.DarkL;       // ɫ
                case "\u03C7": return PortugueseIpaPhoneme.Xh;          // χ

                // 共通異音
                case "\u014B": return PortugueseIpaPhoneme.Ng;          // ŋ
                case "\u0271": return PortugueseIpaPhoneme.NLabiodental; // ɱ
                case "n\u032A": return PortugueseIpaPhoneme.NDental;    // n̪

                // 弱化異音
                case "\u03B2": return PortugueseIpaPhoneme.Beta;        // β
                case "\u00F0": return PortugueseIpaPhoneme.Dh;          // ð
                case "\u0263": return PortugueseIpaPhoneme.Gh;          // ɣ

                default:
                    throw new InvalidOperationException("Unknown phoneme token in exception dictionary: " + token);
            }
        }
    }
}
