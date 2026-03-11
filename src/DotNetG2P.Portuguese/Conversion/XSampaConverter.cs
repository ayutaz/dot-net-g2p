using System;
using System.Text;

namespace DotNetG2P.Portuguese.Conversion
{
    /// <summary>
    /// ポルトガル語IPA音素を X-SAMPA 文字列に変換する。
    /// </summary>
    internal static class XSampaConverter
    {
        /// <summary>
        /// 発音情報をX-SAMPA文字列に変換する。
        /// </summary>
        public static string Convert(PortuguesePronunciation pronunciation, bool includeStress)
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
        /// 発音情報を区切り文字付きのX-SAMPA音素列文字列に変換する。
        /// </summary>
        public static string ConvertPhonemeSequence(PortuguesePronunciation pronunciation, bool includeStress, string separator)
        {
            if (pronunciation == null) throw new ArgumentNullException(nameof(pronunciation));
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
                        builder.Append('"');

                    builder.Append(ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
                    first = false;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 個別の音素をX-SAMPA記号文字列に変換する。
        /// </summary>
        public static string ToSymbol(PortugueseIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 口母音
                case PortugueseIpaPhoneme.A: return "a";
                case PortugueseIpaPhoneme.E: return "e";
                case PortugueseIpaPhoneme.Eh: return "E";
                case PortugueseIpaPhoneme.I: return "i";
                case PortugueseIpaPhoneme.O: return "o";
                case PortugueseIpaPhoneme.Oh: return "O";
                case PortugueseIpaPhoneme.U: return "u";
                case PortugueseIpaPhoneme.Schwa: return "6";
                case PortugueseIpaPhoneme.HighCentral: return "1";

                // 鼻母音
                case PortugueseIpaPhoneme.ANasal: return "6~";
                case PortugueseIpaPhoneme.ENasal: return "e~";
                case PortugueseIpaPhoneme.INasal: return "i~";
                case PortugueseIpaPhoneme.ONasal: return "o~";
                case PortugueseIpaPhoneme.UNasal: return "u~";

                // 半母音
                case PortugueseIpaPhoneme.J: return "j";
                case PortugueseIpaPhoneme.W: return "w";

                // 鼻わたり音
                case PortugueseIpaPhoneme.WNasal: return "w~";
                case PortugueseIpaPhoneme.JNasal: return "j~";

                // 破裂音
                case PortugueseIpaPhoneme.P: return "p";
                case PortugueseIpaPhoneme.B: return "b";
                case PortugueseIpaPhoneme.T: return "t";
                case PortugueseIpaPhoneme.D: return "d";
                case PortugueseIpaPhoneme.K: return "k";
                case PortugueseIpaPhoneme.G: return "g";

                // 摩擦音
                case PortugueseIpaPhoneme.F: return "f";
                case PortugueseIpaPhoneme.V: return "v";
                case PortugueseIpaPhoneme.S: return "s";
                case PortugueseIpaPhoneme.Z: return "z";
                case PortugueseIpaPhoneme.Sh: return "S";
                case PortugueseIpaPhoneme.Zh: return "Z";

                // 鼻音
                case PortugueseIpaPhoneme.M: return "m";
                case PortugueseIpaPhoneme.N: return "n";
                case PortugueseIpaPhoneme.Ny: return "J";

                // 側面音
                case PortugueseIpaPhoneme.L: return "l";
                case PortugueseIpaPhoneme.Lh: return "L";

                // ロティック
                case PortugueseIpaPhoneme.R: return "4";
                case PortugueseIpaPhoneme.Rr: return "R";

                // BP固有異音
                case PortugueseIpaPhoneme.Ch: return "tS";
                case PortugueseIpaPhoneme.Jh: return "dZ";
                case PortugueseIpaPhoneme.X: return "x";
                case PortugueseIpaPhoneme.H: return "h";

                // EP固有異音
                case PortugueseIpaPhoneme.DarkL: return "5";
                case PortugueseIpaPhoneme.Xh: return "X";

                // 共通異音
                case PortugueseIpaPhoneme.Ng: return "N";
                case PortugueseIpaPhoneme.NLabiodental: return "F";
                case PortugueseIpaPhoneme.NDental: return "n_d";

                // 弱化異音
                case PortugueseIpaPhoneme.Beta: return "B";
                case PortugueseIpaPhoneme.Dh: return "D";
                case PortugueseIpaPhoneme.Gh: return "G";

                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
