using System.Collections.Generic;
using System.Text;

namespace DotNetG2P.Swedish.Conversion
{
    /// <summary>
    /// スウェーデン語音素のIPA文字列変換。
    /// </summary>
    internal static class IpaConverter
    {
        private static readonly string[] s_ipaSymbols = new string[42]
        {
            // 長母音 (0-8)
            "i\u02D0", "y\u02D0", "\u0289\u02D0", "u\u02D0", "e\u02D0", "\u00F8\u02D0", "\u025B\u02D0", "o\u02D0", "\u0251\u02D0",
            // 短母音 (9-17)
            "\u026A", "\u028F", "\u0275", "\u028A", "\u025B", "\u0153", "\u0254", "a", "\u0259",
            // 破裂音 (18-23)
            "p", "b", "t", "d", "k", "\u0261",
            // 摩擦音 (24-29)
            "f", "v", "s", "h", "\u0267", "\u0255",
            // 鼻音 (30-32)
            "m", "n", "\u014B",
            // 接近音・ふるえ音 (33-35)
            "l", "r", "j",
            // そり舌音 (36-40)
            "\u0288", "\u0256", "\u0273", "\u026D", "\u0282",
            // 破擦音 (41)
            "t\u0361\u0255",  // t͡ɕ (U+0361 = combining tie bar)
        };

        internal static string ToSymbol(SwedishIpaPhoneme phoneme) =>
            s_ipaSymbols[(int)phoneme];

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
                    sb.Append(s_ipaSymbols[(int)phonemes[i].Phoneme]);
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

                    sb.Append(s_ipaSymbols[(int)phonemes[i].Phoneme]);
                    first = false;
                }
            }

            return sb.ToString();
        }
    }
}
