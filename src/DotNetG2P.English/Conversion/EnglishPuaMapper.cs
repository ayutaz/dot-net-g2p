using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Conversion
{
    /// <summary>
    /// piper-plus 互換の英語 PUA (Private Use Area) 文字マッピング。
    /// 多文字 IPA 音素を単一 PUA 文字に変換する。
    /// </summary>
    /// <remarks>
    /// マッピングは uPiper の PuaTokenMapper.cs に基づく。
    /// 英語 IPA 音素のうち多文字表記のものを PUA 文字に変換する。
    /// 中国語・スペイン語・ポルトガル語と共有するコードポイントを含む。
    /// </remarks>
    internal static class EnglishPuaMapper
    {
        // ── 英語で使用する PUA マッピング ──
        //
        // 二重母音（中国語と共有 0xE028-0xE02B）:
        //   0xE028: aɪ  (AY - as in "buy")
        //   0xE029: eɪ  (EY - as in "say")
        //   0xE02A: aʊ  (AW - as in "how")
        //   0xE02B: oʊ  (OW - as in "go")
        //
        // 英語固有 (0xE053):
        //   0xE053: ɔɪ  (OY - as in "boy")
        //
        // スペイン語・ポルトガル語と共有 (0xE054-0xE055):
        //   0xE054: tʃ  (CH - as in "church")
        //   0xE055: dʒ  (JH - as in "judge")

        /// <summary>IPA 文字列 → PUA 文字 のマッピング。</summary>
        private static readonly Dictionary<string, char> IpaToPuaMap = new Dictionary<string, char>
        {
            // 二重母音（中国語と共有 PUA）
            { "a\u026A", '\uE028' },        // aɪ → 0xE028
            { "e\u026A", '\uE029' },        // eɪ → 0xE029
            { "a\u028A", '\uE02A' },        // aʊ → 0xE02A
            { "o\u028A", '\uE02B' },        // oʊ → 0xE02B

            // 英語固有二重母音
            { "\u0254\u026A", '\uE053' },   // ɔɪ → 0xE053

            // 破擦音（スペイン語・ポルトガル語と共有 PUA）
            { "t\u0283", '\uE054' },        // tʃ → 0xE054
            { "d\u0292", '\uE055' },        // dʒ → 0xE055
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
