using System;
using System.Collections.Generic;

namespace DotNetG2P.Chinese.Conversion
{
    /// <summary>
    /// piper-plus 互換の中国語 PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する（0xE020-0xE04A、43エントリ）。
    /// </summary>
    internal static class ChinesePuaMapper
    {
        // ── 声母（有気/破擦） 0xE020-0xE027 ──
        //
        // 0xE020: pʰ
        // 0xE021: tʰ
        // 0xE022: kʰ
        // 0xE023: tɕ
        // 0xE024: tɕʰ
        // 0xE025: tʂ
        // 0xE026: tʂʰ
        // 0xE027: tsʰ
        //
        // ── 二重母音 0xE028-0xE02B ──
        //
        // 0xE028: aɪ
        // 0xE029: eɪ
        // 0xE02A: aʊ
        // 0xE02B: oʊ
        //
        // ── 鼻音韻尾 0xE02C-0xE030 ──
        //
        // 0xE02C: an
        // 0xE02D: ən
        // 0xE02E: aŋ
        // 0xE02F: əŋ
        // 0xE030: uŋ
        //
        // ── i系複合韻母 0xE031-0xE039 ──
        //
        // 0xE031: ia
        // 0xE032: iɛ
        // 0xE033: iou
        // 0xE034: iaʊ
        // 0xE035: iɛn
        // 0xE036: in
        // 0xE037: iaŋ
        // 0xE038: iŋ
        // 0xE039: iuŋ
        //
        // ── u系複合韻母 0xE03A-0xE041 ──
        //
        // 0xE03A: ua
        // 0xE03B: uo
        // 0xE03C: uaɪ
        // 0xE03D: ueɪ
        // 0xE03E: uan
        // 0xE03F: uən
        // 0xE040: uaŋ
        // 0xE041: uəŋ
        //
        // ── ü系複合韻母 0xE042-0xE044 ──
        //
        // 0xE042: yɛ
        // 0xE043: yɛn
        // 0xE044: yn
        //
        // ── 音節子音 0xE045 ──
        //
        // 0xE045: ɻ̩
        //
        // ── 声調マーカー 0xE046-0xE04A ──
        //
        // 0xE046: tone1
        // 0xE047: tone2
        // 0xE048: tone3
        // 0xE049: tone4
        // 0xE04A: tone5

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // 声母（有気/破擦）
            { "p\u02B0", '\uE020' },              // pʰ  → 0xE020
            { "t\u02B0", '\uE021' },              // tʰ  → 0xE021
            { "k\u02B0", '\uE022' },              // kʰ  → 0xE022
            { "t\u0255", '\uE023' },              // tɕ  → 0xE023
            { "t\u0255\u02B0", '\uE024' },        // tɕʰ → 0xE024
            { "t\u0282", '\uE025' },              // tʂ  → 0xE025
            { "t\u0282\u02B0", '\uE026' },        // tʂʰ → 0xE026
            { "ts\u02B0", '\uE027' },             // tsʰ → 0xE027

            // 二重母音
            { "a\u026A", '\uE028' },              // aɪ  → 0xE028
            { "e\u026A", '\uE029' },              // eɪ  → 0xE029
            { "a\u028A", '\uE02A' },              // aʊ  → 0xE02A
            { "o\u028A", '\uE02B' },              // oʊ  → 0xE02B

            // 鼻音韻尾
            { "an", '\uE02C' },                   // an  → 0xE02C
            { "\u0259n", '\uE02D' },              // ən  → 0xE02D
            { "a\u014B", '\uE02E' },              // aŋ  → 0xE02E
            { "\u0259\u014B", '\uE02F' },         // əŋ  → 0xE02F
            { "u\u014B", '\uE030' },              // uŋ  → 0xE030

            // i系複合韻母
            { "ia", '\uE031' },                   // ia  → 0xE031
            { "i\u025B", '\uE032' },              // iɛ  → 0xE032
            { "iou", '\uE033' },                  // iou → 0xE033
            { "ia\u028A", '\uE034' },             // iaʊ → 0xE034
            { "i\u025Bn", '\uE035' },             // iɛn → 0xE035
            { "in", '\uE036' },                   // in  → 0xE036
            { "ia\u014B", '\uE037' },             // iaŋ → 0xE037
            { "i\u014B", '\uE038' },              // iŋ  → 0xE038
            { "iu\u014B", '\uE039' },             // iuŋ → 0xE039

            // u系複合韻母
            { "ua", '\uE03A' },                   // ua  → 0xE03A
            { "uo", '\uE03B' },                   // uo  → 0xE03B
            { "ua\u026A", '\uE03C' },             // uaɪ → 0xE03C
            { "ue\u026A", '\uE03D' },             // ueɪ → 0xE03D
            { "uan", '\uE03E' },                  // uan → 0xE03E
            { "u\u0259n", '\uE03F' },             // uən → 0xE03F
            { "ua\u014B", '\uE040' },             // uaŋ → 0xE040
            { "u\u0259\u014B", '\uE041' },        // uəŋ → 0xE041

            // ü系複合韻母
            { "y\u025B", '\uE042' },              // yɛ  → 0xE042
            { "y\u025Bn", '\uE043' },             // yɛn → 0xE043
            { "yn", '\uE044' },                   // yn  → 0xE044

            // 音節子音
            { "\u027B\u0329", '\uE045' },         // ɻ̩   → 0xE045
        };

        /// <summary>声調番号 → PUA 文字 のマッピング（1-5）。</summary>
        private static readonly char[] TonePuaChars =
        {
            '\uE046', // tone1 → 0xE046
            '\uE047', // tone2 → 0xE047
            '\uE048', // tone3 → 0xE048
            '\uE049', // tone4 → 0xE049
            '\uE04A', // tone5 → 0xE04A
        };

        /// <summary>
        /// IPA 音素配列を PUA マッピング済み音素配列に変換する。
        /// 多文字 IPA 音素が PUA マッピングに存在する場合、単一 PUA 文字に置換する。
        /// </summary>
        public static string[] ApplyPuaMapping(string[] ipaPhonemes)
        {
            if (ipaPhonemes == null || ipaPhonemes.Length == 0)
                return Array.Empty<string>();

            var result = new string[ipaPhonemes.Length];
            for (var i = 0; i < ipaPhonemes.Length; i++)
            {
                result[i] = MapToPua(ipaPhonemes[i]);
            }

            return result;
        }

        /// <summary>
        /// 単一 IPA 音素文字列を PUA 文字に変換する。
        /// マッピングが存在しない場合はそのまま返す。
        /// </summary>
        public static string MapToPua(string ipaPhoneme)
        {
            if (string.IsNullOrEmpty(ipaPhoneme))
                return ipaPhoneme;

            return IpaToPuaMap.TryGetValue(ipaPhoneme, out var pua)
                ? pua.ToString()
                : ipaPhoneme;
        }

        /// <summary>
        /// 声調番号（1-5）を PUA 声調トークンに変換する。
        /// 範囲外の場合は空文字列を返す。
        /// </summary>
        public static string ToneToPua(int toneNumber)
        {
            if (toneNumber < 1 || toneNumber > 5)
                return string.Empty;

            return TonePuaChars[toneNumber - 1].ToString();
        }
    }
}
