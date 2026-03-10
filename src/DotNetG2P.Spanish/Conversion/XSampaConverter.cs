using System;
using System.Text;

namespace DotNetG2P.Spanish.Conversion
{
    /// <summary>
    /// スペイン語IPA音素を X-SAMPA 文字列に変換する。
    /// </summary>
    internal static class XSampaConverter
    {
        public static string Convert(SpanishPronunciation pronunciation, bool includeStress)
        {
            if (pronunciation == null) throw new ArgumentNullException(nameof(pronunciation));
            if (pronunciation.PhonemesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.PhonemesInternal.Length * 3);
            for (var syllableIndex = 0; syllableIndex < pronunciation.SyllableOffsetsInternal.Length; syllableIndex++)
            {
                if (includeStress && syllableIndex == pronunciation.StressedSyllableIndex)
                    builder.Append('"');

                var start = pronunciation.SyllableOffsetsInternal[syllableIndex];
                var end = syllableIndex + 1 < pronunciation.SyllableOffsetsInternal.Length
                    ? pronunciation.SyllableOffsetsInternal[syllableIndex + 1]
                    : pronunciation.PhonemesInternal.Length;

                for (var i = start; i < end; i++)
                    builder.Append(ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
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
                case SpanishIpaPhoneme.G: return "g";
                case SpanishIpaPhoneme.F: return "f";
                case SpanishIpaPhoneme.S: return "s";
                case SpanishIpaPhoneme.X: return "x";
                case SpanishIpaPhoneme.Ch: return "tS";
                case SpanishIpaPhoneme.Y: return "j\\";
                case SpanishIpaPhoneme.M: return "m";
                case SpanishIpaPhoneme.N: return "n";
                case SpanishIpaPhoneme.Ny: return "J";
                case SpanishIpaPhoneme.L: return "l";
                case SpanishIpaPhoneme.Rr: return "r";
                case SpanishIpaPhoneme.R: return "4";
                case SpanishIpaPhoneme.Th: return "T";
                case SpanishIpaPhoneme.Ll: return "L";
                case SpanishIpaPhoneme.Beta: return "B";
                case SpanishIpaPhoneme.Dh: return "D";
                case SpanishIpaPhoneme.Gh: return "G";
                case SpanishIpaPhoneme.Z: return "z";
                case SpanishIpaPhoneme.NLabiodental: return "F";
                case SpanishIpaPhoneme.Eng: return "N";
                case SpanishIpaPhoneme.Sh: return "S";
                case SpanishIpaPhoneme.YAffricate: return "J\\j\\";
                case SpanishIpaPhoneme.NDental: return "n_d";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
