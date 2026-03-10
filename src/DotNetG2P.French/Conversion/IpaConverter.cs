using System;
using System.Text;

namespace DotNetG2P.French.Conversion
{
    /// <summary>
    /// フランス語音素列をIPA文字列に変換する。
    /// </summary>
    internal static class IpaConverter
    {
        /// <summary>
        /// 発音情報をIPA文字列に変換する。
        /// </summary>
        public static string Convert(FrenchPronunciation pronunciation, bool includeStress)
        {
            if (pronunciation == null) throw new ArgumentNullException(nameof(pronunciation));
            if (pronunciation.PhonemesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.PhonemesInternal.Length * 2);
            for (var syllableIndex = 0; syllableIndex < pronunciation.SyllableOffsetsInternal.Length; syllableIndex++)
            {
                if (includeStress && syllableIndex == pronunciation.StressedSyllableIndex)
                    builder.Append('\u02C8'); // ˈ

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
        /// 発音情報を区切り文字付きの音素列文字列に変換する。
        /// </summary>
        public static string ConvertPhonemeSequence(FrenchPronunciation pronunciation, bool includeStress, string separator)
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
                        builder.Append('\u02C8'); // ˈ

                    builder.Append(ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
                    first = false;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 個別の音素をIPA記号文字列に変換する。
        /// </summary>
        public static string ToSymbol(FrenchIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 口母音
                case FrenchIpaPhoneme.A: return "a";
                case FrenchIpaPhoneme.Ah: return "\u0251"; // ɑ
                case FrenchIpaPhoneme.E: return "e";
                case FrenchIpaPhoneme.Eh: return "\u025B"; // ɛ
                case FrenchIpaPhoneme.I: return "i";
                case FrenchIpaPhoneme.O: return "o";
                case FrenchIpaPhoneme.Oh: return "\u0254"; // ɔ
                case FrenchIpaPhoneme.U: return "u";
                case FrenchIpaPhoneme.Y: return "y";
                case FrenchIpaPhoneme.Oe: return "\u00F8"; // ø
                case FrenchIpaPhoneme.Oeh: return "\u0153"; // œ
                case FrenchIpaPhoneme.Schwa: return "\u0259"; // ə

                // 鼻母音
                case FrenchIpaPhoneme.ANasal: return "\u0251\u0303"; // ɑ̃
                case FrenchIpaPhoneme.ONasal: return "\u0254\u0303"; // ɔ̃
                case FrenchIpaPhoneme.ENasal: return "\u025B\u0303"; // ɛ̃
                case FrenchIpaPhoneme.OeNasal: return "\u0153\u0303"; // œ̃

                // 半母音
                case FrenchIpaPhoneme.J: return "j";
                case FrenchIpaPhoneme.W: return "w";
                case FrenchIpaPhoneme.Uj: return "\u0265"; // ɥ

                // 閉鎖音
                case FrenchIpaPhoneme.P: return "p";
                case FrenchIpaPhoneme.B: return "b";
                case FrenchIpaPhoneme.T: return "t";
                case FrenchIpaPhoneme.D: return "d";
                case FrenchIpaPhoneme.K: return "k";
                case FrenchIpaPhoneme.G: return "\u0261"; // ɡ (U+0261)

                // 摩擦音
                case FrenchIpaPhoneme.F: return "f";
                case FrenchIpaPhoneme.V: return "v";
                case FrenchIpaPhoneme.S: return "s";
                case FrenchIpaPhoneme.Z: return "z";
                case FrenchIpaPhoneme.Sh: return "\u0283"; // ʃ
                case FrenchIpaPhoneme.Zh: return "\u0292"; // ʒ

                // 鼻音
                case FrenchIpaPhoneme.M: return "m";
                case FrenchIpaPhoneme.N: return "n";
                case FrenchIpaPhoneme.Ny: return "\u0272"; // ɲ

                // 側面音
                case FrenchIpaPhoneme.L: return "l";

                // 接近音
                case FrenchIpaPhoneme.R: return "\u0281"; // ʁ

                // 異音
                case FrenchIpaPhoneme.Rh: return "\u03C7"; // χ
                case FrenchIpaPhoneme.Ng: return "\u014B"; // ŋ
                case FrenchIpaPhoneme.Ts: return "ts";
                case FrenchIpaPhoneme.Dz: return "dz";

                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
