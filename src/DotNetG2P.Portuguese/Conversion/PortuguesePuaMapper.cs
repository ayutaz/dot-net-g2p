using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese.Conversion
{
    /// <summary>
    /// piper-plus 互換のポルトガル語 PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する。
    /// </summary>
    internal static class PortuguesePuaMapper
    {
        // ── スペイン語と共有 PUA (0xE054-0xE055) ──
        //
        // 0xE054: tʃ  (無声後部歯茎破擦音、BP: /ti/ → [tʃi])
        // 0xE055: dʒ  (有声後部歯茎破擦音、BP: /di/ → [dʒi])

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // スペイン語と共有 PUA
            { "t\u0283", '\uE054' },        // tʃ  → 0xE054
            { "d\u0292", '\uE055' },        // dʒ  → 0xE055
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
