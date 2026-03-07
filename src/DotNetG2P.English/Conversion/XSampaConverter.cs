using System.Text;

namespace DotNetG2P.English.Conversion
{
    /// <summary>
    /// ARPAbet音素をX-SAMPA表記に変換する。
    /// </summary>
    internal static class XSampaConverter
    {
        // 子音のX-SAMPAマッピング（ArpabetPhoneme.B=15 から ArpabetPhoneme.ZH=38）
        // インデックス = (int)phoneme - 15
        private static readonly string[] ConsonantXSampa =
        {
            "b",    // B  (15)
            "tS",   // CH (16)
            "d",    // D  (17)
            "D",    // DH (18)
            "f",    // F  (19)
            "g",    // G  (20)
            "h",    // HH (21)
            "dZ",   // JH (22)
            "k",    // K  (23)
            "l",    // L  (24)
            "m",    // M  (25)
            "n",    // N  (26)
            "N",    // NG (27)
            "p",    // P  (28)
            "r\\",  // R  (29)
            "s",    // S  (30)
            "S",    // SH (31)
            "t",    // T  (32)
            "T",    // TH (33)
            "v",    // V  (34)
            "w",    // W  (35)
            "j",    // Y  (36)
            "z",    // Z  (37)
            "Z",    // ZH (38)
        };

        // 母音のX-SAMPAマッピング（unstressed/NoStress版）
        // インデックス = (int)phoneme (AA=0 .. UW=14)
        private static readonly string[] VowelXSampaUnstressed =
        {
            "A",    // AA (0)
            "{",    // AE (1)
            "@",    // AH (2) - unstressed = schwa
            "O",    // AO (3)
            "aU",   // AW (4)
            "aI",   // AY (5)
            "E",    // EH (6)
            "@`",   // ER (7) - unstressed = r-colored schwa
            "eI",   // EY (8)
            "I",    // IH (9)
            "i",    // IY (10)
            "oU",   // OW (11)
            "OI",   // OY (12)
            "U",    // UH (13)
            "u",    // UW (14)
        };

        // 母音のX-SAMPAマッピング（stressed版）
        // AH→V、ER→3` 以外はunstressedと同じ
        private static readonly string[] VowelXSampaStressed =
        {
            "A",    // AA (0)
            "{",    // AE (1)
            "V",    // AH (2) - stressed = open-mid back
            "O",    // AO (3)
            "aU",   // AW (4)
            "aI",   // AY (5)
            "E",    // EH (6)
            "3`",   // ER (7) - stressed = r-colored open-mid
            "eI",   // EY (8)
            "I",    // IH (9)
            "i",    // IY (10)
            "oU",   // OW (11)
            "OI",   // OY (12)
            "U",    // UH (13)
            "u",    // UW (14)
        };

        /// <summary>
        /// 単一の <see cref="EnglishPhoneme"/> をX-SAMPA文字列に変換する。
        /// ストレス情報に応じて母音のマッピングを切り替える。
        /// </summary>
        /// <param name="phoneme">変換対象の音素</param>
        /// <returns>X-SAMPA表記文字列</returns>
        internal static string PhonemeToXSampa(EnglishPhoneme phoneme)
        {
            int idx = (int)phoneme.Phoneme;

            if (phoneme.IsVowel)
            {
                bool stressed = phoneme.Stress == Stress.Primary || phoneme.Stress == Stress.Secondary;
                return stressed ? VowelXSampaStressed[idx] : VowelXSampaUnstressed[idx];
            }

            return ConsonantXSampa[idx - 15];
        }

        /// <summary>
        /// 音素配列をX-SAMPA文字列に変換する（ストレスマーク付き）。
        /// Primary stress は <c>"</c>、Secondary stress は <c>%</c> を母音の直前に配置する。
        /// </summary>
        /// <param name="phonemes">変換対象の音素配列</param>
        /// <returns>スペース区切りのX-SAMPA文字列</returns>
        internal static string Convert(EnglishPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < phonemes.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');

                var p = phonemes[i];

                // ストレスマークを母音の直前に付与
                if (p.IsVowel)
                {
                    if (p.Stress == Stress.Primary)
                        sb.Append('"');
                    else if (p.Stress == Stress.Secondary)
                        sb.Append('%');
                }

                sb.Append(PhonemeToXSampa(p));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 音素配列をX-SAMPA文字列に変換する（ストレスマークなし）。
        /// AH は常に <c>@</c>（schwa）、ER は常に <c>@`</c>（r-colored schwa）として出力する。
        /// </summary>
        /// <param name="phonemes">変換対象の音素配列</param>
        /// <returns>スペース区切りのX-SAMPA文字列（ストレスマークなし）</returns>
        internal static string ConvertWithoutStress(EnglishPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < phonemes.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');

                int idx = (int)phonemes[i].Phoneme;

                if (phonemes[i].IsVowel)
                {
                    // ストレスなし版を常に使用（AH→@、ER→@`）
                    sb.Append(VowelXSampaUnstressed[idx]);
                }
                else
                {
                    sb.Append(ConsonantXSampa[idx - 15]);
                }
            }

            return sb.ToString();
        }
    }
}
