using System.Text;

namespace DotNetG2P.English.Conversion
{
    /// <summary>
    /// ARPAbet音素をIPA（国際音声記号）に変換する。
    /// </summary>
    internal static class IpaConverter
    {
        // 母音のIPA表現（ストレスなし / ストレスあり）
        // インデックスは ArpabetPhoneme の値に対応 (0-14)
        private static readonly string[] VowelIpaUnstressed =
        {
            "ɑ",  // AA (0)
            "æ",  // AE (1)
            "ə",  // AH (2) - unstressed = schwa
            "ɔ",  // AO (3)
            "aʊ", // AW (4)
            "aɪ", // AY (5)
            "ɛ",  // EH (6)
            "ɚ",  // ER (7) - unstressed = r-colored schwa
            "eɪ", // EY (8)
            "ɪ",  // IH (9)
            "i",  // IY (10)
            "oʊ", // OW (11)
            "ɔɪ", // OY (12)
            "ʊ",  // UH (13)
            "u",  // UW (14)
        };

        private static readonly string[] VowelIpaStressed =
        {
            "ɑ",  // AA (0)
            "æ",  // AE (1)
            "ʌ",  // AH (2) - stressed = open-mid back
            "ɔ",  // AO (3)
            "aʊ", // AW (4)
            "aɪ", // AY (5)
            "ɛ",  // EH (6)
            "ɝ",  // ER (7) - stressed = r-colored open-mid
            "eɪ", // EY (8)
            "ɪ",  // IH (9)
            "i",  // IY (10)
            "oʊ", // OW (11)
            "ɔɪ", // OY (12)
            "ʊ",  // UH (13)
            "u",  // UW (14)
        };

        // 子音のIPA表現
        // インデックスは ArpabetPhoneme の値 - 15 に対応 (B=15 → index 0)
        private static readonly string[] ConsonantIpa =
        {
            "b",  // B  (15)
            "tʃ", // CH (16)
            "d",  // D  (17)
            "ð",  // DH (18)
            "f",  // F  (19)
            "ɡ",  // G  (20) - U+0261
            "h",  // HH (21)
            "dʒ", // JH (22)
            "k",  // K  (23)
            "l",  // L  (24)
            "m",  // M  (25)
            "n",  // N  (26)
            "ŋ",  // NG (27)
            "p",  // P  (28)
            "ɹ",  // R  (29)
            "s",  // S  (30)
            "ʃ",  // SH (31)
            "t",  // T  (32)
            "θ",  // TH (33)
            "v",  // V  (34)
            "w",  // W  (35)
            "j",  // Y  (36)
            "z",  // Z  (37)
            "ʒ",  // ZH (38)
        };

        /// <summary>
        /// 単一の <see cref="EnglishPhoneme"/> をIPA文字列に変換する。
        /// ストレスマークは含まない。ストレスによるIPA文字の切り替え（AH, ER）は行う。
        /// </summary>
        internal static string PhonemeToIpa(EnglishPhoneme phoneme)
        {
            int index = (int)phoneme.Phoneme;

            if (phoneme.IsVowel)
            {
                bool stressed = phoneme.Stress == Stress.Primary || phoneme.Stress == Stress.Secondary;
                return stressed ? VowelIpaStressed[index] : VowelIpaUnstressed[index];
            }

            return ConsonantIpa[index - 15];
        }

        /// <summary>
        /// 音素配列をIPA文字列に変換する（ストレスマーク付き）。
        /// Primary stress → ˈ、Secondary stress → ˌ を母音IPA表現の直前に配置する。
        /// </summary>
        internal static string Convert(EnglishPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(phonemes.Length * 2);

            for (int i = 0; i < phonemes.Length; i++)
            {
                var p = phonemes[i];
                int index = (int)p.Phoneme;

                if (p.IsVowel)
                {
                    // ストレスマーク配置
                    if (p.Stress == Stress.Primary)
                        sb.Append('ˈ'); // U+02C8
                    else if (p.Stress == Stress.Secondary)
                        sb.Append('ˌ'); // U+02CC

                    bool stressed = p.Stress == Stress.Primary || p.Stress == Stress.Secondary;
                    sb.Append(stressed ? VowelIpaStressed[index] : VowelIpaUnstressed[index]);
                }
                else
                {
                    sb.Append(ConsonantIpa[index - 15]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 音素配列をIPA文字列に変換する（ストレスマークなし）。
        /// AH は常に ə、ER は常に ɚ を使用する。
        /// </summary>
        internal static string ConvertWithoutStress(EnglishPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(phonemes.Length * 2);

            for (int i = 0; i < phonemes.Length; i++)
            {
                var p = phonemes[i];
                int index = (int)p.Phoneme;

                if (p.IsVowel)
                {
                    // ストレスなし版を常に使用
                    sb.Append(VowelIpaUnstressed[index]);
                }
                else
                {
                    sb.Append(ConsonantIpa[index - 15]);
                }
            }

            return sb.ToString();
        }
    }
}
