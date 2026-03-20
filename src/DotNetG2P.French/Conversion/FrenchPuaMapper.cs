using System;
using System.Collections.Generic;

namespace DotNetG2P.French.Conversion
{
    /// <summary>
    /// piper-plus 互換の フランス語 PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する。
    /// </summary>
    /// <remarks>
    /// uPiper の PuaTokenMapper.FixedPuaMapping に準拠。
    /// フランス語固有の鼻母音 3 エントリ + 多言語共有 3 エントリ（計 6 エントリ）。
    /// </remarks>
    internal static class FrenchPuaMapper
    {
        // ── フランス語固有 PUA (0xE056-0xE058) ──
        //
        // 0xE056: ɛ̃  (鼻母音、vin / pain / main)
        // 0xE057: ɑ̃  (鼻母音、France / temps / vent)
        // 0xE058: ɔ̃  (鼻母音、bon / nom / long)
        //
        // ── 多言語共有 PUA ──
        //
        // 0xE01E: y_vowel  (前舌円唇狭母音 [y]、lune / tu / vu)
        // 0xE054: tʃ       (無声後部歯茎破擦音、スペイン語/ポルトガル語と共有)
        // 0xE055: dʒ       (有声後部歯茎破擦音、スペイン語/ポルトガル語と共有)

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // フランス語固有: 鼻母音
            { "\u025B\u0303", '\uE056' },    // ɛ̃  → 0xE056
            { "\u0251\u0303", '\uE057' },    // ɑ̃  → 0xE057
            { "\u0254\u0303", '\uE058' },    // ɔ̃  → 0xE058

            // 多言語共有
            { "y_vowel", '\uE01E' },         // y_vowel → 0xE01E (前舌円唇狭母音 [y])
            { "t\u0283", '\uE054' },         // tʃ → 0xE054 (無声後部歯茎破擦音)
            { "d\u0292", '\uE055' },         // dʒ → 0xE055 (有声後部歯茎破擦音)
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
