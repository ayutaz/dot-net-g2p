using System;
using System.Collections.Generic;

namespace DotNetG2P.Spanish.Conversion
{
    /// <summary>
    /// piper-plus 互換の PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する。
    /// </summary>
    /// <remarks>
    /// uPiper の <c>PuaTokenMapper.FixedPuaMapping</c> および
    /// <c>SpanishPhonemizerBackend</c> に基づく。
    /// <list type="bullet">
    /// <item><description>0xE01D: rr（スペイン語ふるえ音 /r/）— 多言語共有</description></item>
    /// <item><description>0xE054: t&#x0283;（無声後部歯茎破擦音 /t&#x0283;/）— ES/PT 共有</description></item>
    /// <item><description>0xE055: d&#x0292;（有声後部歯茎破擦音 /d&#x0292;/）— ES/PT 共有</description></item>
    /// </list>
    /// </remarks>
    internal static class SpanishPuaMapper
    {
        // ── スペイン語 PUA マッピング ──
        //
        // 0xE01D: rr  — スペイン語ふるえ音（多言語共有スロット）
        // 0xE054: tʃ  — 無声後部歯茎破擦音（ES/PT 共有）
        // 0xE055: dʒ  — 有声後部歯茎破擦音（ES/PT 共有）

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // 多言語共有 PUA
            { "rr", '\uE01D' },                // rr（ふるえ音 /r/）→ 0xE01D

            // ES/PT 共有 PUA
            { "t\u0283", '\uE054' },           // tʃ（無声後部歯茎破擦音）→ 0xE054
            { "d\u0292", '\uE055' },           // dʒ（有声後部歯茎破擦音）→ 0xE055
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
