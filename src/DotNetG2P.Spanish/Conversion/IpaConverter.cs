using System;
using System.Text;

namespace DotNetG2P.Spanish.Conversion
{
    internal static class IpaConverter
    {
        public static string Convert(SpanishPronunciation pronunciation, bool includeStress)
        {
            if (pronunciation.PhonemesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.PhonemesInternal.Length * 2);
            for (var syllableIndex = 0; syllableIndex < pronunciation.SyllableOffsetsInternal.Length; syllableIndex++)
            {
                if (includeStress && syllableIndex == pronunciation.StressedSyllableIndex)
                    builder.Append('ˈ');

                var start = pronunciation.SyllableOffsetsInternal[syllableIndex];
                var end = syllableIndex + 1 < pronunciation.SyllableOffsetsInternal.Length
                    ? pronunciation.SyllableOffsetsInternal[syllableIndex + 1]
                    : pronunciation.PhonemesInternal.Length;

                for (var i = start; i < end; i++)
                    builder.Append(ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
            }

            return builder.ToString();
        }

        public static string ConvertPhonemeSequence(SpanishPronunciation pronunciation, bool includeStress, string separator)
        {
            if (pronunciation.PhonemesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.PhonemesInternal.Length * 3);
            var first = true;
            for (var syllableIndex = 0; syllableIndex < pronunciation.SyllableOffsetsInternal.Length; syllableIndex++)
            {
                var start = pronunciation.SyllableOffsetsInternal[syllableIndex];
                var end = syllableIndex + 1 < pronunciation.SyllableOffsetsInternal.Length
                    ? pronunciation.SyllableOffsetsInternal[syllableIndex + 1]
                    : pronunciation.PhonemesInternal.Length;

                for (var i = start; i < end; i++)
                {
                    if (!first)
                        builder.Append(separator);

                    if (includeStress && syllableIndex == pronunciation.StressedSyllableIndex && i == start)
                        builder.Append('ˈ');

                    builder.Append(ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
                    first = false;
                }
            }

            return builder.ToString();
        }

        public static string ToSymbol(SpanishIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case SpanishIpaPhoneme.A: return "a";
                case SpanishIpaPhoneme.E: return "e";
                case SpanishIpaPhoneme.I: return "i";
                case SpanishIpaPhoneme.O: return "o";
                case SpanishIpaPhoneme.U: return "u";
                case SpanishIpaPhoneme.J: return "j";
                case SpanishIpaPhoneme.W: return "w";
                case SpanishIpaPhoneme.P: return "p";
                case SpanishIpaPhoneme.B: return "b";
                case SpanishIpaPhoneme.T: return "t";
                case SpanishIpaPhoneme.D: return "d";
                case SpanishIpaPhoneme.K: return "k";
                case SpanishIpaPhoneme.G: return "ɡ";
                case SpanishIpaPhoneme.F: return "f";
                case SpanishIpaPhoneme.S: return "s";
                case SpanishIpaPhoneme.X: return "x";
                case SpanishIpaPhoneme.Ch: return "tʃ";
                case SpanishIpaPhoneme.Y: return "ʝ";
                case SpanishIpaPhoneme.M: return "m";
                case SpanishIpaPhoneme.N: return "n";
                case SpanishIpaPhoneme.Ny: return "ɲ";
                case SpanishIpaPhoneme.L: return "l";
                case SpanishIpaPhoneme.Rr: return "r";
                case SpanishIpaPhoneme.R: return "ɾ";
                case SpanishIpaPhoneme.Th: return "θ";
                case SpanishIpaPhoneme.Ll: return "ʎ";
                case SpanishIpaPhoneme.Beta: return "β";
                case SpanishIpaPhoneme.Dh: return "ð";
                case SpanishIpaPhoneme.Gh: return "ɣ";
                case SpanishIpaPhoneme.Z: return "z";
                case SpanishIpaPhoneme.NLabiodental: return "ɱ";
                case SpanishIpaPhoneme.Eng: return "ŋ";
                case SpanishIpaPhoneme.Sh: return "ʃ";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
