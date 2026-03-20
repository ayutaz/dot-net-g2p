using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Conversion
{
    /// <summary>
    /// piper-plus 互換の ARPAbet→IPA 変換。
    /// <para>
    /// piper-plus の english.py / uPiper の ArpabetToIPAConverter に準拠し、
    /// 以下の差分を既存の <see cref="IpaConverter"/> に対して適用する:
    /// </para>
    /// <list type="bullet">
    /// <item>IY → i + ː（長音マーク付き）</item>
    /// <item>UW → u + ː</item>
    /// <item>AO → ɔ + ː</item>
    /// <item>CH → t + ʃ（分割出力）</item>
    /// <item>JH → d + ʒ（分割出力）</item>
    /// <item>AA の後に R が続く場合 → ɑ + ː + ɹ（マージ）</item>
    /// <item>AH unstressed → ə、stressed → ʌ</item>
    /// <item>ER unstressed → ɚ、stressed → ɜ + ː</item>
    /// <item>二重母音を個別文字に分割（AW→a+ʊ, AY→a+ɪ, EY→e+ɪ, OW→o+ʊ, OY→ɔ+ɪ）</item>
    /// <item>機能語のストレスマーク除去</item>
    /// </list>
    /// </summary>
    internal static class PiperIpaConverter
    {
        // ===== 母音マッピング =====
        // piper-plus ではすべての IPA 文字を個別トークンとして出力する。
        // 長母音は母音 + "ː" の2トークン、二重母音は各要素の2トークン。

        // 母音の IPA（ストレス非依存分）
        // インデックスは ArpabetPhoneme の値に対応 (0-14)
        // AH(2), ER(7) はストレス依存のため null にし、専用処理で扱う。
        private static readonly string[][] s_vowelIpa =
        {
            new[] { "\u0251" },                 // AA (0): ɑ
            new[] { "\u00E6" },                 // AE (1): æ
            null!,                              // AH (2): ストレス依存（専用処理）
            new[] { "\u0254", "\u02D0" },        // AO (3): ɔ + ː
            new[] { "a", "\u028A" },             // AW (4): a + ʊ
            new[] { "a", "\u026A" },             // AY (5): a + ɪ
            new[] { "\u025B" },                 // EH (6): ɛ
            null!,                              // ER (7): ストレス依存（専用処理）
            new[] { "e", "\u026A" },             // EY (8): e + ɪ
            new[] { "\u026A" },                 // IH (9): ɪ
            new[] { "i", "\u02D0" },             // IY (10): i + ː
            new[] { "o", "\u028A" },             // OW (11): o + ʊ
            new[] { "\u0254", "\u026A" },         // OY (12): ɔ + ɪ
            new[] { "\u028A" },                 // UH (13): ʊ
            new[] { "u", "\u02D0" },             // UW (14): u + ː
        };

        // AH: stressed → ʌ, unstressed → ə
        private static readonly string s_ahStressed = "\u028C";   // ʌ
        private static readonly string s_ahUnstressed = "\u0259"; // ə

        // ER: stressed → ɜ + ː (2トークン), unstressed → ɚ (1トークン)
        private static readonly string[] s_erStressed = { "\u025C", "\u02D0" }; // ɜ + ː
        private static readonly string s_erUnstressed = "\u025A";               // ɚ

        // ===== 子音マッピング =====
        // インデックスは ArpabetPhoneme の値 - 15 に対応 (B=15 → index 0)
        // CH(16), JH(22) は分割出力のため null にし、専用処理で扱う。
        private static readonly string[][] s_consonantIpa =
        {
            new[] { "b" },                      // B  (15)
            null!,                              // CH (16): 分割出力（専用処理）
            new[] { "d" },                      // D  (17)
            new[] { "\u00F0" },                 // DH (18): ð
            new[] { "f" },                      // F  (19)
            new[] { "\u0261" },                 // G  (20): ɡ (U+0261)
            new[] { "h" },                      // HH (21)
            null!,                              // JH (22): 分割出力（専用処理）
            new[] { "k" },                      // K  (23)
            new[] { "l" },                      // L  (24)
            new[] { "m" },                      // M  (25)
            new[] { "n" },                      // N  (26)
            new[] { "\u014B" },                 // NG (27): ŋ
            new[] { "p" },                      // P  (28)
            new[] { "\u0279" },                 // R  (29): ɹ
            new[] { "s" },                      // S  (30)
            new[] { "\u0283" },                 // SH (31): ʃ
            new[] { "t" },                      // T  (32)
            new[] { "\u03B8" },                 // TH (33): θ
            new[] { "v" },                      // V  (34)
            new[] { "w" },                      // W  (35)
            new[] { "j" },                      // Y  (36)
            new[] { "z" },                      // Z  (37)
            new[] { "\u0292" },                 // ZH (38): ʒ
        };

        // CH → t + ʃ
        private static readonly string[] s_chIpa = { "t", "\u0283" };
        // JH → d + ʒ
        private static readonly string[] s_jhIpa = { "d", "\u0292" };

        // AA+R マージ結果: ɑ + ː + ɹ
        private static readonly string[] s_aaRMerged = { "\u0251", "\u02D0", "\u0279" };

        // ストレスマーカー
        private static readonly string s_primaryStress = "\u02C8";   // ˈ
        private static readonly string s_secondaryStress = "\u02CC"; // ˌ

        /// <summary>
        /// <see cref="EnglishPronunciation"/> を piper-plus 互換の IPA 音素配列に変換する。
        /// </summary>
        /// <param name="pronunciation">変換対象の発音。</param>
        /// <param name="word">元の語（機能語判定に使用）。</param>
        /// <param name="removeFunctionWordStress">機能語のストレスを除去するかどうか。デフォルト: true。</param>
        /// <returns>piper-plus 互換の IPA 音素トークン配列。</returns>
        public static string[] Convert(EnglishPronunciation pronunciation, string word, bool removeFunctionWordStress = true)
        {
            if (pronunciation == null)
                throw new ArgumentNullException(nameof(pronunciation));

            return Convert(pronunciation.PhonemesInternal, word, removeFunctionWordStress);
        }

        /// <summary>
        /// 音素配列を piper-plus 互換の IPA 音素配列に変換する。
        /// </summary>
        /// <param name="phonemes">変換対象の音素配列。</param>
        /// <param name="word">元の語（機能語判定に使用）。</param>
        /// <param name="removeFunctionWordStress">機能語のストレスを除去するかどうか。デフォルト: true。</param>
        /// <returns>piper-plus 互換の IPA 音素トークン配列。</returns>
        public static string[] Convert(EnglishPhoneme[] phonemes, string word, bool removeFunctionWordStress = true)
        {
            if (phonemes == null)
                throw new ArgumentNullException(nameof(phonemes));

            if (phonemes.Length == 0)
                return Array.Empty<string>();

            bool isFunctionWord = removeFunctionWordStress && word != null && FunctionWordList.Contains(word);
            var result = new List<string>(phonemes.Length * 2);

            for (int i = 0; i < phonemes.Length; i++)
            {
                var p = phonemes[i];

                if (p.IsVowel)
                {
                    ConvertVowel(p, phonemes, ref i, isFunctionWord, result);
                }
                else
                {
                    ConvertConsonant(p, result);
                }
            }

            return result.ToArray();
        }

        /// <summary>母音の変換処理。</summary>
        private static void ConvertVowel(
            EnglishPhoneme phoneme,
            EnglishPhoneme[] allPhonemes,
            ref int index,
            bool isFunctionWord,
            List<string> output)
        {
            // 機能語ではストレスを除去
            var stress = isFunctionWord ? Stress.NoStress : phoneme.Stress;
            bool isStressed = stress == Stress.Primary || stress == Stress.Secondary;

            // ストレスマーク出力（母音の前に配置）
            if (stress == Stress.Primary)
                output.Add(s_primaryStress);
            else if (stress == Stress.Secondary)
                output.Add(s_secondaryStress);

            var arpabet = phoneme.Phoneme;

            // AH: ストレス依存
            if (arpabet == ArpabetPhoneme.AH)
            {
                output.Add(isStressed ? s_ahStressed : s_ahUnstressed);
                return;
            }

            // ER: ストレス依存
            if (arpabet == ArpabetPhoneme.ER)
            {
                if (isStressed)
                {
                    // ɜ + ː
                    output.Add(s_erStressed[0]);
                    output.Add(s_erStressed[1]);
                }
                else
                {
                    // ɚ
                    output.Add(s_erUnstressed);
                }
                return;
            }

            // AA + R マージ: 次の音素が R なら ɑːɹ にマージ
            if (arpabet == ArpabetPhoneme.AA &&
                index + 1 < allPhonemes.Length &&
                allPhonemes[index + 1].Phoneme == ArpabetPhoneme.R)
            {
                output.Add(s_aaRMerged[0]); // ɑ
                output.Add(s_aaRMerged[1]); // ː
                output.Add(s_aaRMerged[2]); // ɹ
                index++; // R を消費
                return;
            }

            // 通常の母音マッピング
            var ipa = s_vowelIpa[(int)arpabet];
            for (int j = 0; j < ipa.Length; j++)
                output.Add(ipa[j]);
        }

        /// <summary>子音の変換処理。</summary>
        private static void ConvertConsonant(EnglishPhoneme phoneme, List<string> output)
        {
            var arpabet = phoneme.Phoneme;

            // CH → t + ʃ
            if (arpabet == ArpabetPhoneme.CH)
            {
                output.Add(s_chIpa[0]);
                output.Add(s_chIpa[1]);
                return;
            }

            // JH → d + ʒ
            if (arpabet == ArpabetPhoneme.JH)
            {
                output.Add(s_jhIpa[0]);
                output.Add(s_jhIpa[1]);
                return;
            }

            // 通常の子音マッピング
            var ipa = s_consonantIpa[(int)arpabet - 15];
            output.Add(ipa[0]);
        }
    }
}
