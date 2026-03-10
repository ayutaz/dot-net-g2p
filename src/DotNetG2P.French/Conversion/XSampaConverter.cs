using System;
using System.Text;

namespace DotNetG2P.French.Conversion
{
    /// <summary>
    /// フランス語IPA音素を X-SAMPA 文字列に変換する。
    /// </summary>
    internal static class XSampaConverter
    {
        /// <summary>
        /// 発音情報をX-SAMPA文字列に変換する。
        /// </summary>
        public static string Convert(FrenchPronunciation pronunciation, bool includeStress)
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

        /// <summary>
        /// 個別の音素をX-SAMPA記号文字列に変換する。
        /// </summary>
        public static string ToSymbol(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 口母音
                case FrenchIpaPhoneme.A: return "a";
                case FrenchIpaPhoneme.Ah: return "A";
                case FrenchIpaPhoneme.E: return "e";
                case FrenchIpaPhoneme.Eh: return "E";
                case FrenchIpaPhoneme.I: return "i";
                case FrenchIpaPhoneme.O: return "o";
                case FrenchIpaPhoneme.Oh: return "O";
                case FrenchIpaPhoneme.U: return "u";
                case FrenchIpaPhoneme.Y: return "y";
                case FrenchIpaPhoneme.Oe: return "2";
                case FrenchIpaPhoneme.Oeh: return "9";
                case FrenchIpaPhoneme.Schwa: return "@";

                // 鼻母音
                case FrenchIpaPhoneme.ANasal: return "A~";
                case FrenchIpaPhoneme.ONasal: return "O~";
                case FrenchIpaPhoneme.ENasal: return "E~";
                case FrenchIpaPhoneme.OeNasal: return "9~";

                // 半母音
                case FrenchIpaPhoneme.J: return "j";
                case FrenchIpaPhoneme.W: return "w";
                case FrenchIpaPhoneme.Uj: return "H";

                // 閉鎖音
                case FrenchIpaPhoneme.P: return "p";
                case FrenchIpaPhoneme.B: return "b";
                case FrenchIpaPhoneme.T: return "t";
                case FrenchIpaPhoneme.D: return "d";
                case FrenchIpaPhoneme.K: return "k";
                case FrenchIpaPhoneme.G: return "g";

                // 摩擦音
                case FrenchIpaPhoneme.F: return "f";
                case FrenchIpaPhoneme.V: return "v";
                case FrenchIpaPhoneme.S: return "s";
                case FrenchIpaPhoneme.Z: return "z";
                case FrenchIpaPhoneme.Sh: return "S";
                case FrenchIpaPhoneme.Zh: return "Z";

                // 鼻音
                case FrenchIpaPhoneme.M: return "m";
                case FrenchIpaPhoneme.N: return "n";
                case FrenchIpaPhoneme.Ny: return "J";

                // 側面音
                case FrenchIpaPhoneme.L: return "l";

                // 接近音
                case FrenchIpaPhoneme.R: return "R";

                // 異音
                case FrenchIpaPhoneme.Rh: return "X";
                case FrenchIpaPhoneme.Ng: return "N";
                case FrenchIpaPhoneme.Ts: return "ts";
                case FrenchIpaPhoneme.Dz: return "dz";

                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
