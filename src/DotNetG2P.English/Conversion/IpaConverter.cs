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
        /// IPA標準に準拠し、ストレスマークは音節先頭（先行する子音群の前）に配置する。
        /// 2パス方式: パス1で各音素のIPA文字列とストレスマーク挿入位置を事前計算し、
        /// パス2で順方向Appendのみで構築する。
        /// </summary>
        internal static string Convert(EnglishPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return string.Empty;

            int len = phonemes.Length;

            // パス1: 各音素のIPA文字列を事前計算し、ストレスマーク挿入位置を決定
            var ipaStrings = new string[len];
            // stressMarks[i] != '\0' なら、ipaStrings[i]の前にストレスマークを挿入
            var stressMarks = new char[len];
            int totalLength = 0;

            for (int i = 0; i < len; i++)
            {
                var p = phonemes[i];
                int index = (int)p.Phoneme;

                if (p.IsVowel)
                {
                    bool stressed = p.Stress == Stress.Primary || p.Stress == Stress.Secondary;
                    ipaStrings[i] = stressed ? VowelIpaStressed[index] : VowelIpaUnstressed[index];

                    if (stressed)
                    {
                        char mark = p.Stress == Stress.Primary ? 'ˈ' : 'ˌ';

                        // 先行する連続子音群を遡り、音節先頭（onset）を求める
                        int onset = i;
                        while (onset > 0 && !phonemes[onset - 1].IsVowel)
                            onset--;

                        // onset位置にストレスマークを記録
                        stressMarks[onset] = mark;
                        totalLength++; // ストレスマーク1文字分
                    }
                }
                else
                {
                    ipaStrings[i] = ConsonantIpa[index - 15];
                }

                totalLength += ipaStrings[i].Length;
            }

            // パス2: 順方向Appendのみで構築
            var sb = new StringBuilder(totalLength);

            for (int i = 0; i < len; i++)
            {
                if (stressMarks[i] != '\0')
                    sb.Append(stressMarks[i]);

                sb.Append(ipaStrings[i]);
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
