using System;
using System.Collections.Generic;

namespace DotNetG2P.Korean.Conversion
{
    /// <summary>
    /// piper-plus 互換の PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する。
    /// </summary>
    internal static class PuaMapper
    {
        // ── 韓国語固有 PUA (0xE04B-0xE052) ──
        //
        // 0xE04B: p͈  (ㅃ の IPA)
        // 0xE04C: t͈  (ㄸ の IPA)
        // 0xE04D: k͈  (ㄲ の IPA)
        // 0xE04E: s͈  (ㅆ の IPA)
        // 0xE04F: t͈ɕ (ㅉ の IPA)
        // 0xE050: k̚  (終声 ㄱ/ㄲ/ㄳ/ㅋ の IPA)
        // 0xE051: t̚  (終声 ㄷ/ㅅ/ㅆ/ㅈ/ㅊ/ㅌ の IPA)
        // 0xE052: p̚  (終声 ㅂ/ㅄ/ㅍ の IPA)
        //
        // ── 中国語と共有 PUA (0xE020-0xE024) ──
        //
        // 0xE020: pʰ  (ㅍ の IPA)
        // 0xE021: tʰ  (ㅌ の IPA)
        // 0xE022: kʰ  (ㅋ の IPA)
        // 0xE023: tɕ  (ㅈ の IPA)
        // 0xE024: tɕʰ (ㅊ の IPA)

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // 韓国語固有 PUA
            { "p\u0348", '\uE04B' },        // p͈  → 0xE04B
            { "t\u0348", '\uE04C' },        // t͈  → 0xE04C
            { "k\u0348", '\uE04D' },        // k͈  → 0xE04D
            { "s\u0348", '\uE04E' },        // s͈  → 0xE04E
            { "t\u0348\u0255", '\uE04F' },  // t͈ɕ → 0xE04F
            { "k\u031A", '\uE050' },        // k̚  → 0xE050
            { "t\u031A", '\uE051' },        // t̚  → 0xE051
            { "p\u031A", '\uE052' },        // p̚  → 0xE052

            // 中国語と共有 PUA
            { "p\u02B0", '\uE020' },        // pʰ  → 0xE020
            { "t\u02B0", '\uE021' },        // tʰ  → 0xE021
            { "k\u02B0", '\uE022' },        // kʰ  → 0xE022
            { "t\u0255", '\uE023' },        // tɕ  → 0xE023
            { "t\u0255\u02B0", '\uE024' },  // tɕʰ → 0xE024
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
    }
}
