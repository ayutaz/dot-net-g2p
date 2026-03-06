// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 英語テキストの正規化ファサード。
    /// 数字・通貨・時刻・略語・頭字語・記号をサブ正規化クラスに委譲して英語読みに変換する。
    /// </summary>
    internal static class EnglishNormalizer
    {
        private static readonly char[] s_whitespace = { ' ', '\t', '\r', '\n' };

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
            var tokens = text.Split(s_whitespace, StringSplitOptions.RemoveEmptyEntries);
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
            if (IsPeriodAcronym(token))
            {
                var stripped = StripPeriods(token);
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

            // 早期リターン: 先頭・末尾が共に英字なら数字/通貨/時刻系チェックをスキップ
            if (core.Length > 0)
            {
                char first = core[0];
                char lastCh = core[core.Length - 1];
                if (IsAsciiLetter(first) && IsAsciiLetter(lastCh))
                {
                    // 頭字語判定
                    if (AcronymDetector.IsAllUpperCase(core))
                        return ProcessAcronym(core);

                    // 記号展開
                    var symbol = SymbolExpander.TryExpand(core);
                    if (symbol != null)
                        return symbol;

                    return core;
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
            if (TryParseOrdinal(core, out string ordinalNumStr))
            {
                if (long.TryParse(ordinalNumStr, NumberStyles.None, CultureInfo.InvariantCulture, out long ordinalNum))
                {
                    return NumberToWords.Ordinal(ordinalNum);
                }
            }

            // (e) 小数パターン: "3.14" 等
            if (TryParseDecimal(core, out string integerPart, out string decimalPart))
            {
                return NumberToWords.ExpandDecimal(integerPart, decimalPart);
            }

            // (f) 整数パターン: "123", "-5", "1,000" 等
            if (IsIntegerPattern(core))
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
            {
                var symbol = SymbolExpander.TryExpand(core);
                if (symbol != null)
                    return symbol;
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTrailingPunctuation(char c)
        {
            return c == ',' || c == '.' || c == '!' || c == '?' || c == ';' || c == ':';
        }

        /// <summary>
        /// ASCII英字かどうか判定する。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAsciiLetter(char c)
        {
            return (uint)((c | 0x20) - 'a') <= 'z' - 'a';
        }

        /// <summary>
        /// 序数パターン ("1st", "23rd", "100th" 等) を手動パースする。
        /// 成功時はnumberPartに数字部分の文字列を返す。
        /// </summary>
        private static bool TryParseOrdinal(string token, out string numberPart)
        {
            numberPart = null!;
            // 最低3文字（数字1 + 接尾辞2）
            if (token.Length < 3)
                return false;

            // 末尾2文字を取得
            char s0 = token[token.Length - 2];
            char s1 = token[token.Length - 1];

            // st, nd, rd, th のいずれかを判定（小文字のみ）
            bool validSuffix = (s0 == 's' && s1 == 't')
                            || (s0 == 'n' && s1 == 'd')
                            || (s0 == 'r' && s1 == 'd')
                            || (s0 == 't' && s1 == 'h');
            if (!validSuffix)
                return false;

            // 接尾辞前の部分が全て数字であること
            for (int i = 0; i < token.Length - 2; i++)
            {
                if (token[i] < '0' || token[i] > '9')
                    return false;
            }

            numberPart = token.Substring(0, token.Length - 2);
            return true;
        }

        /// <summary>
        /// 小数パターン ("3.14", "0.5" 等) を手動パースする。
        /// 成功時はintegerPart/decimalPartに分割文字列を返す。
        /// </summary>
        private static bool TryParseDecimal(string token, out string integerPart, out string decimalPart)
        {
            integerPart = null!;
            decimalPart = null!;

            // '.'の位置を探す
            int dotIndex = token.IndexOf('.');
            if (dotIndex <= 0 || dotIndex == token.Length - 1)
                return false;

            // '.'が1つだけであること
            if (token.IndexOf('.', dotIndex + 1) >= 0)
                return false;

            // 整数部分が全て数字
            for (int i = 0; i < dotIndex; i++)
            {
                if (token[i] < '0' || token[i] > '9')
                    return false;
            }

            // 小数部分が全て数字
            for (int i = dotIndex + 1; i < token.Length; i++)
            {
                if (token[i] < '0' || token[i] > '9')
                    return false;
            }

            integerPart = token.Substring(0, dotIndex);
            decimalPart = token.Substring(dotIndex + 1);
            return true;
        }

        /// <summary>
        /// 整数パターン ("123", "-5", "1,000,000" 等) を手動判定する。
        /// ^-?\d[\d,]*$ と等価。
        /// </summary>
        private static bool IsIntegerPattern(string token)
        {
            if (token.Length == 0)
                return false;

            int start = 0;
            if (token[0] == '-')
            {
                start = 1;
            }

            // 先頭（'-'の次）は数字が必須
            if (start >= token.Length)
                return false;
            if (token[start] < '0' || token[start] > '9')
                return false;

            // 残りは数字またはカンマ
            for (int i = start + 1; i < token.Length; i++)
            {
                char c = token[i];
                if ((c < '0' || c > '9') && c != ',')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// ピリオド区切り頭字語パターン ("U.S.A.", "U.S." 等) を手動判定する。
        /// ^([A-Z]\.){2,}$ と等価。長さ4以上の偶数、奇数位置が大文字、偶数位置が'.'。
        /// </summary>
        private static bool IsPeriodAcronym(string token)
        {
            // 最低 "X.Y." = 4文字、かつ偶数長
            if (token.Length < 4 || (token.Length & 1) != 0)
                return false;

            for (int i = 0; i < token.Length; i += 2)
            {
                char letter = token[i];
                if (letter < 'A' || letter > 'Z')
                    return false;

                char dot = token[i + 1];
                if (dot != '.')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// ピリオドを除去した文字列を返す。IsPeriodAcronym通過後に使用。
        /// </summary>
        private static string StripPeriods(string token)
        {
            // "X.Y.Z." → "XYZ" : 長さの半分が英字数
            int letterCount = token.Length / 2;
            var chars = new char[letterCount];
            for (int i = 0; i < letterCount; i++)
            {
                chars[i] = token[i * 2];
            }
            return new string(chars);
        }
    }
}
