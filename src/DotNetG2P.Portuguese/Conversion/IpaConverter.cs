using System;
using System.Text;

namespace DotNetG2P.Portuguese.Conversion
{
    /// <summary>
    /// ポルトガル語音素列をIPA文字列に変換する。
    /// </summary>
    internal static class IpaConverter
    {
        /// <summary>
        /// 発音情報をIPA文字列に変換する。
        /// </summary>
        public static string Convert(PortuguesePronunciation pronunciation, bool includeStress)
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
        public static string ToSymbol(PortugueseIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                // 口母音
                case PortugueseIpaPhoneme.A: return "a";
                case PortugueseIpaPhoneme.E: return "e";
                case PortugueseIpaPhoneme.Eh: return "\u025B"; // ɛ
                case PortugueseIpaPhoneme.I: return "i";
                case PortugueseIpaPhoneme.O: return "o";
                case PortugueseIpaPhoneme.Oh: return "\u0254"; // ɔ
                case PortugueseIpaPhoneme.U: return "u";
                case PortugueseIpaPhoneme.Schwa: return "\u0250"; // ɐ
                case PortugueseIpaPhoneme.HighCentral: return "\u0268"; // ɨ

                // 鼻母音
                case PortugueseIpaPhoneme.ANasal: return "\u0250\u0303"; // ɐ̃
                case PortugueseIpaPhoneme.ENasal: return "e\u0303"; // ẽ
                case PortugueseIpaPhoneme.INasal: return "i\u0303"; // ĩ
                case PortugueseIpaPhoneme.ONasal: return "\u00F5"; // õ
                case PortugueseIpaPhoneme.UNasal: return "u\u0303"; // ũ

                // 半母音
                case PortugueseIpaPhoneme.J: return "j";
                case PortugueseIpaPhoneme.W: return "w";

                // 破裂音
                case PortugueseIpaPhoneme.P: return "p";
                case PortugueseIpaPhoneme.B: return "b";
                case PortugueseIpaPhoneme.T: return "t";
                case PortugueseIpaPhoneme.D: return "d";
                case PortugueseIpaPhoneme.K: return "k";
                case PortugueseIpaPhoneme.G: return "\u0261"; // ɡ (U+0261, IPA g)

                // 摩擦音
                case PortugueseIpaPhoneme.F: return "f";
                case PortugueseIpaPhoneme.V: return "v";
                case PortugueseIpaPhoneme.S: return "s";
                case PortugueseIpaPhoneme.Z: return "z";
                case PortugueseIpaPhoneme.Sh: return "\u0283"; // ʃ
                case PortugueseIpaPhoneme.Zh: return "\u0292"; // ʒ

                // 鼻音
                case PortugueseIpaPhoneme.M: return "m";
                case PortugueseIpaPhoneme.N: return "n";
                case PortugueseIpaPhoneme.Ny: return "\u0272"; // ɲ

                // 側面音
                case PortugueseIpaPhoneme.L: return "l";
                case PortugueseIpaPhoneme.Lh: return "\u028E"; // ʎ

                // ロティック
                case PortugueseIpaPhoneme.R: return "\u027E"; // ɾ
                case PortugueseIpaPhoneme.Rr: return "\u0281"; // ʁ

                // BP固有異音
                case PortugueseIpaPhoneme.Ch: return "t\u0361\u0283"; // t͡ʃ
                case PortugueseIpaPhoneme.Jh: return "d\u0361\u0292"; // d͡ʒ
                case PortugueseIpaPhoneme.X: return "x";
                case PortugueseIpaPhoneme.H: return "h";

                // EP固有異音
                case PortugueseIpaPhoneme.DarkL: return "\u026B"; // ɫ
                case PortugueseIpaPhoneme.Xh: return "\u03C7"; // χ

                // 共通異音
                case PortugueseIpaPhoneme.Ng: return "\u014B"; // ŋ
                case PortugueseIpaPhoneme.NLabiodental: return "\u0271"; // ɱ
                case PortugueseIpaPhoneme.NDental: return "n\u032A"; // n̪

                // 弱化異音
                case PortugueseIpaPhoneme.Beta: return "\u03B2"; // β
                case PortugueseIpaPhoneme.Dh: return "\u00F0"; // ð
                case PortugueseIpaPhoneme.Gh: return "\u0263"; // ɣ

                // 鼻わたり音
                case PortugueseIpaPhoneme.WNasal: return "w\u0303"; // w̃
                case PortugueseIpaPhoneme.JNasal: return "j\u0303"; // j̃

                default: throw new ArgumentOutOfRangeException(nameof(phoneme), phoneme, null);
            }
        }
    }
}
