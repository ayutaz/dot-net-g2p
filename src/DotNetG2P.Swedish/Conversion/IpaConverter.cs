using System;
using System.Text;

namespace DotNetG2P.Swedish.Conversion
{
    /// <summary>
    /// スウェーデン語音素のIPA文字列変換。
    /// </summary>
    internal static class IpaConverter
    {
        internal static string ToSymbol(SwedishIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 長母音
                case SwedishIpaPhoneme.LongI: return "i\u02D0";
                case SwedishIpaPhoneme.LongY: return "y\u02D0";
                case SwedishIpaPhoneme.LongUCentral: return "\u0289\u02D0";
                case SwedishIpaPhoneme.LongU: return "u\u02D0";
                case SwedishIpaPhoneme.LongE: return "e\u02D0";
                case SwedishIpaPhoneme.LongOe: return "\u00F8\u02D0";
                case SwedishIpaPhoneme.LongEh: return "\u025B\u02D0";
                case SwedishIpaPhoneme.LongO: return "o\u02D0";
                case SwedishIpaPhoneme.LongA: return "\u0251\u02D0";
                // 短母音
                case SwedishIpaPhoneme.ShortI: return "\u026A";
                case SwedishIpaPhoneme.ShortY: return "\u028F";
                case SwedishIpaPhoneme.ShortUCentral: return "\u0275";
                case SwedishIpaPhoneme.ShortU: return "\u028A";
                case SwedishIpaPhoneme.ShortE: return "\u025B";
                case SwedishIpaPhoneme.ShortOe: return "\u0153";
                case SwedishIpaPhoneme.ShortO: return "\u0254";
                case SwedishIpaPhoneme.ShortA: return "a";
                case SwedishIpaPhoneme.Schwa: return "\u0259";
                // 破裂音
                case SwedishIpaPhoneme.P: return "p";
                case SwedishIpaPhoneme.B: return "b";
                case SwedishIpaPhoneme.T: return "t";
                case SwedishIpaPhoneme.D: return "d";
                case SwedishIpaPhoneme.K: return "k";
                case SwedishIpaPhoneme.G: return "\u0261";
                // 摩擦音
                case SwedishIpaPhoneme.F: return "f";
                case SwedishIpaPhoneme.V: return "v";
                case SwedishIpaPhoneme.S: return "s";
                case SwedishIpaPhoneme.H: return "h";
                case SwedishIpaPhoneme.Sj: return "\u0267";
                case SwedishIpaPhoneme.Tj: return "\u0255";
                // 鼻音
                case SwedishIpaPhoneme.M: return "m";
                case SwedishIpaPhoneme.N: return "n";
                case SwedishIpaPhoneme.Ng: return "\u014B";
                // 接近音・ふるえ音
                case SwedishIpaPhoneme.L: return "l";
                case SwedishIpaPhoneme.R: return "r";
                case SwedishIpaPhoneme.J: return "j";
                // そり舌音
                case SwedishIpaPhoneme.RetroT: return "\u0288";
                case SwedishIpaPhoneme.RetroD: return "\u0256";
                case SwedishIpaPhoneme.RetroN: return "\u0273";
                case SwedishIpaPhoneme.RetroL: return "\u026D";
                case SwedishIpaPhoneme.RetroS: return "\u0282";
                // 破擦音
                case SwedishIpaPhoneme.TjAffricate: return "t\u0361\u0255";
                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }

        /// <summary>発音情報をIPA連続文字列に変換する。</summary>
        internal static string Convert(SwedishPronunciation pronunciation, bool includeStress)
        {
            var phonemes = pronunciation.PhonemesInternal;
            var syllableOffsets = pronunciation.SyllableOffsetsInternal;
            var stressedIndex = pronunciation.StressedSyllableIndex;

            if (phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(phonemes.Length * 2);

            for (var syllableIndex = 0; syllableIndex < syllableOffsets.Length; syllableIndex++)
            {
                if (includeStress && syllableIndex == stressedIndex)
                    sb.Append('\u02C8');

                var start = syllableOffsets[syllableIndex];
                var end = syllableIndex + 1 < syllableOffsets.Length
                    ? syllableOffsets[syllableIndex + 1]
                    : phonemes.Length;

                for (var i = start; i < end; i++)
                    sb.Append(ToSymbol(phonemes[i].Phoneme));
            }

            return sb.ToString();
        }

        /// <summary>発音情報を区切り文字付きIPA音素列に変換する。</summary>
        internal static string ConvertPhonemeSequence(
            SwedishPronunciation pronunciation, bool includeStress, string separator)
        {
            var phonemes = pronunciation.PhonemesInternal;
            var syllableOffsets = pronunciation.SyllableOffsetsInternal;
            var stressedIndex = pronunciation.StressedSyllableIndex;

            if (phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(phonemes.Length * 3);
            var first = true;

            for (var syllableIndex = 0; syllableIndex < syllableOffsets.Length; syllableIndex++)
            {
                var start = syllableOffsets[syllableIndex];
                var end = syllableIndex + 1 < syllableOffsets.Length
                    ? syllableOffsets[syllableIndex + 1]
                    : phonemes.Length;

                for (var i = start; i < end; i++)
                {
                    if (!first)
                        sb.Append(separator);

                    if (includeStress && syllableIndex == stressedIndex && i == start)
                        sb.Append('\u02C8');

                    sb.Append(ToSymbol(phonemes[i].Phoneme));
                    first = false;
                }
            }

            return sb.ToString();
        }
    }
}
