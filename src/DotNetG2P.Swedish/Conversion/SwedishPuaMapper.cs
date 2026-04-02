using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish.Conversion
{
    /// <summary>
    /// piper-plus互換のPUA文字マッピング。多文字IPA音素を単一PUA文字に変換する。
    /// スウェーデン語ではFinlandSwedish方言の t͡ɕ のみPUA変換が必要。
    /// </summary>
    internal static class SwedishPuaMapper
    {
        // t͡ɕ → PUA 0xE023 (韓国語/中国語と共有)
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            { "t\u0361\u0255", '\uE023' },  // t͡ɕ → 0xE023
        };

        /// <summary>
        /// IPA音素配列をPUAマッピング済み音素配列に変換する。
        /// 多文字IPA音素がPUAマッピングに存在する場合、単一PUA文字に置換する。
        /// </summary>
        public static string[] ApplyPuaMapping(string[] ipaPhonemes)
        {
            if (ipaPhonemes == null || ipaPhonemes.Length == 0)
                return Array.Empty<string>();

            var result = new string[ipaPhonemes.Length];
            for (var i = 0; i < ipaPhonemes.Length; i++)
                result[i] = MapToPua(ipaPhonemes[i]);
            return result;
        }

        /// <summary>
        /// 単一IPA音素文字列をPUA文字に変換する。
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
