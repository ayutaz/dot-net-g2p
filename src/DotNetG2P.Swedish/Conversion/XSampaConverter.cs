using System;
using System.Text;

namespace DotNetG2P.Swedish.Conversion
{
    /// <summary>
    /// スウェーデン語IPA音素を X-SAMPA 文字列に変換する。
    /// </summary>
    internal static class XSampaConverter
    {
        /// <summary>
        /// 発音情報をX-SAMPA文字列に変換する。
        /// </summary>
        public static string Convert(SwedishPronunciation pronunciation, bool includeStress)
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
        public static string ConvertPhonemeSequence(SwedishPronunciation pronunciation, bool includeStress, string separator)
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
        public static string ToSymbol(SwedishIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 長母音
                case SwedishIpaPhoneme.LongI: return "i:";
                case SwedishIpaPhoneme.LongY: return "y:";
                case SwedishIpaPhoneme.LongUCentral: return "u\\`:";
                case SwedishIpaPhoneme.LongU: return "u:";
                case SwedishIpaPhoneme.LongE: return "e:";
                case SwedishIpaPhoneme.LongOe: return "2:";
                case SwedishIpaPhoneme.LongEh: return "E:";
                case SwedishIpaPhoneme.LongO: return "o:";
                case SwedishIpaPhoneme.LongA: return "A:";

                // 短母音
                case SwedishIpaPhoneme.ShortI: return "I";
                case SwedishIpaPhoneme.ShortY: return "Y";
                case SwedishIpaPhoneme.ShortUCentral: return "8";
                case SwedishIpaPhoneme.ShortU: return "U";
                case SwedishIpaPhoneme.ShortE: return "E";
                case SwedishIpaPhoneme.ShortOe: return "9";
                case SwedishIpaPhoneme.ShortO: return "O";
                case SwedishIpaPhoneme.ShortA: return "a";
                case SwedishIpaPhoneme.Schwa: return "@";

                // 破裂音
                case SwedishIpaPhoneme.P: return "p";
                case SwedishIpaPhoneme.B: return "b";
                case SwedishIpaPhoneme.T: return "t";
                case SwedishIpaPhoneme.D: return "d";
                case SwedishIpaPhoneme.K: return "k";
                case SwedishIpaPhoneme.G: return "g";

                // 摩擦音
                case SwedishIpaPhoneme.F: return "f";
                case SwedishIpaPhoneme.V: return "v";
                case SwedishIpaPhoneme.S: return "s";
                case SwedishIpaPhoneme.H: return "h";
                case SwedishIpaPhoneme.Sj: return "x\\";
                case SwedishIpaPhoneme.Tj: return "s\\";

                // 鼻音
                case SwedishIpaPhoneme.M: return "m";
                case SwedishIpaPhoneme.N: return "n";
                case SwedishIpaPhoneme.Ng: return "N";

                // 接近音・ふるえ音
                case SwedishIpaPhoneme.L: return "l";
                case SwedishIpaPhoneme.R: return "r";
                case SwedishIpaPhoneme.J: return "j";

                // そり舌音
                case SwedishIpaPhoneme.RetroT: return "t`";
                case SwedishIpaPhoneme.RetroD: return "d`";
                case SwedishIpaPhoneme.RetroN: return "n`";
                case SwedishIpaPhoneme.RetroL: return "l`";
                case SwedishIpaPhoneme.RetroS: return "s`";

                // 破擦音
                case SwedishIpaPhoneme.TjAffricate: return "ts\\";

                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
