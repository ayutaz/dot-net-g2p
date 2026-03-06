// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 英語テキストの正規化ファサード。
    /// 数字・通貨・時刻・略語・頭字語・記号をサブ正規化クラスに委譲して英語読みに変換する。
    /// </summary>
    internal static class EnglishNormalizer
    {
        // 序数パターン: "1st", "23rd", "100th" 等
        private static readonly Regex s_ordinalRegex = new Regex(
            @"^\d+(st|nd|rd|th)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 小数パターン: "3.14", "0.5" 等
        private static readonly Regex s_decimalRegex = new Regex(
            @"^(\d+)\.(\d+)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 整数パターン: "123", "-5", "1,000,000" 等（オプションのマイナス記号 + 数字/カンマ）
        private static readonly Regex s_integerRegex = new Regex(
            @"^-?\d[\d,]*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // ピリオド区切り頭字語パターン: "U.S.A.", "U.S." 等
        private static readonly Regex s_periodAcronymRegex = new Regex(
            @"^([A-Z]\.){2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// テキスト全体を正規化する。非アルファベット要素を英語読みに変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>正規化済みテキスト</returns>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            // 空白文字でトークン分割
            var tokens = text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new string[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                results[i] = NormalizeToken(tokens[i]);
            }

            return string.Join(" ", results);
        }

        /// <summary>
        /// 個別トークンを正規化する。
        /// サブ正規化クラスを順番に試行し、最初にマッチしたものを使う。
        /// </summary>
        private static string NormalizeToken(string token)
        {
            // (a) 略語展開（ピリオド付き/なし対応）— 最優先で試行
            var abbr = AbbreviationExpander.TryExpand(token);
            if (abbr != null)
                return abbr;

            // ピリオド区切り頭字語の処理: "U.S.A." → "USA" → AcronymDetectorへ
            var periodAcronymMatch = s_periodAcronymRegex.Match(token);
            if (periodAcronymMatch.Success)
            {
                var stripped = token.Replace(".", "");
                return ProcessAcronym(stripped);
            }

            // 末尾の句読点を分離して処理
            string core = token;
            if (core.Length > 1)
            {
                char last = core[core.Length - 1];
                if (IsTrailingPunctuation(last))
                {
                    // 句読点は後続Tokenizerが処理するため再付与しない
                    core = core.Substring(0, core.Length - 1);

                    // 句読点除去後に再度略語チェック
                    abbr = AbbreviationExpander.TryExpand(core);
                    if (abbr != null)
                        return abbr;
                }
            }

            // (b) 通貨展開
            var currency = CurrencyExpander.TryExpand(core);
            if (currency != null)
                return currency;

            // (c) 時刻展開
            var time = TimeExpander.TryExpand(core);
            if (time != null)
                return time;

            // (d) 序数パターン: "1st", "23rd" 等
            var ordinalMatch = s_ordinalRegex.Match(core);
            if (ordinalMatch.Success)
            {
                // 末尾の接尾辞（2文字）を除去して数値をパース
                string numStr = core.Substring(0, core.Length - 2);
                if (long.TryParse(numStr, NumberStyles.None, CultureInfo.InvariantCulture, out long ordinalNum))
                {
                    return NumberToWords.Ordinal(ordinalNum);
                }
            }

            // (e) 小数パターン: "3.14" 等
            var decimalMatch = s_decimalRegex.Match(core);
            if (decimalMatch.Success)
            {
                return NumberToWords.ExpandDecimal(decimalMatch.Groups[1].Value, decimalMatch.Groups[2].Value);
            }

            // (f) 整数パターン: "123", "-5", "1,000" 等
            var integerMatch = s_integerRegex.Match(core);
            if (integerMatch.Success)
            {
                string cleaned = core.Replace(",", "");
                if (long.TryParse(cleaned, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long intVal))
                {
                    return NumberToWords.Cardinal(intVal);
                }
            }

            // (g) 頭字語判定
            if (AcronymDetector.IsAllUpperCase(core))
            {
                return ProcessAcronym(core);
            }

            // (h) 記号展開
            var symbol = SymbolExpander.TryExpand(core);
            if (symbol != null)
                return symbol;

            // (i) いずれにも該当しない → そのまま通過
            return core;
        }

        /// <summary>
        /// 頭字語をスペルアウトすべきか判定し、適切な形式で返す。
        /// </summary>
        private static string ProcessAcronym(string upperToken)
        {
            if (AcronymDetector.ShouldSpellOut(upperToken))
                return AcronymDetector.SpellOut(upperToken);

            // 1語読み（CMU辞書に任せる）→ そのまま返す
            return upperToken;
        }

        /// <summary>
        /// 文字が末尾句読点であるか判定する。
        /// </summary>
        private static bool IsTrailingPunctuation(char c)
        {
            return c == ',' || c == '.' || c == '!' || c == '?' || c == ';' || c == ':';
        }
    }
}
