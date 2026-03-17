using System;
using System.Collections.Generic;

namespace DotNetG2P.Korean.Conversion
{
    /// <summary>
    /// 韓国語 Jamo (compatibility jamo) を IPA 文字列に変換する。
    /// 音節構造（初声・中声・終声）に応じた IPA マッピングを提供する。
    /// </summary>
    internal static class JamoToIpa
    {
        // ── 初声 (onset) マッピング ──
        // 19 初声: ㄱ ㄲ ㄴ ㄷ ㄸ ㄹ ㅁ ㅂ ㅃ ㅅ ㅆ ㅇ ㅈ ㅉ ㅊ ㅋ ㅌ ㅍ ㅎ
        private static readonly Dictionary<char, string> OnsetMap = new Dictionary<char, string>
        {
            { 'ㄱ', "k" },
            { 'ㄲ', "k\u0348" },       // k͈ (fortis)
            { 'ㄴ', "n" },
            { 'ㄷ', "t" },
            { 'ㄸ', "t\u0348" },       // t͈ (fortis)
            { 'ㄹ', "\u027E" },        // ɾ (flap)
            { 'ㅁ', "m" },
            { 'ㅂ', "p" },
            { 'ㅃ', "p\u0348" },       // p͈ (fortis)
            { 'ㅅ', "s" },
            { 'ㅆ', "s\u0348" },       // s͈ (fortis)
            { 'ㅇ', "" },              // 初声 ㅇ は無音
            { 'ㅈ', "t\u0255" },       // tɕ (alveolo-palatal affricate)
            { 'ㅉ', "t\u0348\u0255" }, // t͈ɕ (fortis affricate)
            { 'ㅊ', "t\u0255\u02B0" }, // tɕʰ (aspirated affricate)
            { 'ㅋ', "k\u02B0" },       // kʰ (aspirated)
            { 'ㅌ', "t\u02B0" },       // tʰ (aspirated)
            { 'ㅍ', "p\u02B0" },       // pʰ (aspirated)
            { 'ㅎ', "h" },
        };

        // ── 中声 (nucleus / vowel) マッピング ──
        // 21 中声: ㅏ ㅐ ㅑ ㅒ ㅓ ㅔ ㅕ ㅖ ㅗ ㅘ ㅙ ㅚ ㅛ ㅜ ㅝ ㅞ ㅟ ㅠ ㅡ ㅢ ㅣ
        private static readonly Dictionary<char, string> NucleusMap = new Dictionary<char, string>
        {
            { 'ㅏ', "a" },
            { 'ㅐ', "\u025B" },             // ɛ
            { 'ㅑ', "ja" },
            { 'ㅒ', "j\u025B" },            // jɛ
            { 'ㅓ', "\u028C" },             // ʌ
            { 'ㅔ', "e" },
            { 'ㅕ', "j\u028C" },            // jʌ
            { 'ㅖ', "je" },
            { 'ㅗ', "o" },
            { 'ㅘ', "wa" },
            { 'ㅙ', "w\u025B" },            // wɛ
            { 'ㅚ', "we" },
            { 'ㅛ', "jo" },
            { 'ㅜ', "u" },
            { 'ㅝ', "w\u028C" },            // wʌ
            { 'ㅞ', "we" },
            { 'ㅟ', "wi" },
            { 'ㅠ', "ju" },
            { 'ㅡ', "\u026F" },             // ɯ
            { 'ㅢ', "\u0270i" },            // ɰi
            { 'ㅣ', "i" },
        };

        // ── 終声 (coda) マッピング ──
        // 規則適用後は代表終声のみ出現する:
        //   ㄱ ㄴ ㄷ ㄹ ㅁ ㅂ ㅇ (+ 二重子音は G2P 規則で除去済み)
        // 終声の IPA は内破音として記録する
        private static readonly Dictionary<char, string> CodaMap = new Dictionary<char, string>
        {
            { 'ㄱ', "k\u031A" },    // k̚ (unreleased)
            { 'ㄲ', "k\u031A" },    // k̚
            { 'ㄴ', "n" },
            { 'ㄷ', "t\u031A" },    // t̚ (unreleased)
            { 'ㄹ', "l" },
            { 'ㅁ', "m" },
            { 'ㅂ', "p\u031A" },    // p̚ (unreleased)
            { 'ㅇ', "\u014B" },     // ŋ (velar nasal)
            { 'ㅅ', "t\u031A" },    // t̚ (代表化済みの場合)
            { 'ㅆ', "t\u031A" },    // t̚
            { 'ㅈ', "t\u031A" },    // t̚
            { 'ㅊ', "t\u031A" },    // t̚
            { 'ㅋ', "k\u031A" },    // k̚
            { 'ㅌ', "t\u031A" },    // t̚
            { 'ㅍ', "p\u031A" },    // p̚
        };

        /// <summary>
        /// 音節配列を IPA 音素文字列配列に変換する。
        /// 各音節を初声・中声・終声に分けて IPA へマッピングし、
        /// 空でないすべてのセグメントを順に返す。
        /// </summary>
        public static string[] ConvertSyllables(KoreanSyllable[] syllables)
        {
            if (syllables == null || syllables.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>(syllables.Length * 3);

            for (var i = 0; i < syllables.Length; i++)
            {
                var syllable = syllables[i];

                if (syllable.IsBoundary)
                    continue;

                if (!syllable.HasNucleus)
                {
                    // standalone jamo / 非 Hangul
                    var standalone = ConvertStandalone(syllable.Onset);
                    if (!string.IsNullOrEmpty(standalone))
                        result.Add(standalone);
                    continue;
                }

                // 初声
                var onset = ConvertOnset(syllable.Onset);
                if (!string.IsNullOrEmpty(onset))
                    result.Add(onset);

                // 中声
                var nucleus = ConvertNucleus(syllable.Nucleus);
                if (!string.IsNullOrEmpty(nucleus))
                    result.Add(nucleus);

                // 終声
                if (syllable.HasCoda)
                {
                    var coda = ConvertCoda(syllable.Coda);
                    if (!string.IsNullOrEmpty(coda))
                        result.Add(coda);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 初声 Jamo を IPA に変換する。
        /// </summary>
        internal static string ConvertOnset(char jamo)
        {
            return OnsetMap.TryGetValue(jamo, out var ipa) ? ipa : string.Empty;
        }

        /// <summary>
        /// 中声 Jamo を IPA に変換する。
        /// </summary>
        internal static string ConvertNucleus(char jamo)
        {
            return NucleusMap.TryGetValue(jamo, out var ipa) ? ipa : string.Empty;
        }

        /// <summary>
        /// 終声 Jamo を IPA に変換する。
        /// </summary>
        internal static string ConvertCoda(char jamo)
        {
            return CodaMap.TryGetValue(jamo, out var ipa) ? ipa : string.Empty;
        }

        /// <summary>
        /// standalone jamo（初声のみ）を IPA に変換する。
        /// 初声マッピングを利用する。
        /// </summary>
        private static string ConvertStandalone(char jamo)
        {
            return OnsetMap.TryGetValue(jamo, out var ipa) ? ipa : jamo.ToString();
        }
    }
}
