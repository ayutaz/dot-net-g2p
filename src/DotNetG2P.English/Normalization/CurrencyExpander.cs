// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 通貨記号付きトークンを英語読みに展開する。
    /// 対応通貨: $(ドル), £(ポンド), €(ユーロ), ¥(円)
    /// </summary>
    internal static class CurrencyExpander
    {
        /// <summary>
        /// 通貨パターンを検出して英語読みに展開する。
        /// 非通貨トークンの場合はnullを返す。
        /// </summary>
        public static string? TryExpand(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 2)
            {
                return null;
            }

            char symbol = token[0];
            string rest = token.Substring(1);

            switch (symbol)
            {
                case '$':
                    return ExpandWithCents(rest, "dollar", "dollars", "cent", "cents");
                case '£':
                    return ExpandWithCents(rest, "pound", "pounds", "penny", "pence");
                case '€':
                    return ExpandWithCents(rest, "euro", "euros", "cent", "cents");
                case '¥':
                    return ExpandYen(rest);
                default:
                    return null;
            }
        }

        /// <summary>
        /// ドル/ポンド/ユーロなど、整数部+小数部（セント等）を持つ通貨を展開する。
        /// </summary>
        private static string? ExpandWithCents(
            string amount,
            string singularMain,
            string pluralMain,
            string singularSub,
            string pluralSub)
        {
            // カンマ区切りを除去
            amount = amount.Replace(",", "");

            // 小数点で整数部と小数部を分離
            int dotIndex = amount.IndexOf('.');
            long integerPart;
            long fractionalPart = -1; // -1は小数部なしを示す

            if (dotIndex >= 0)
            {
                string intStr = dotIndex > 0 ? amount.Substring(0, dotIndex) : "0";
                string fracStr = amount.Substring(dotIndex + 1);

                if (!long.TryParse(intStr, NumberStyles.None, CultureInfo.InvariantCulture, out integerPart))
                {
                    return null;
                }

                // 小数部を2桁に正規化（例: "5" → 50, "50" → 50, "099" → 99）
                if (fracStr.Length == 0)
                {
                    fractionalPart = 0;
                }
                else if (fracStr.Length == 1)
                {
                    if (!long.TryParse(fracStr, NumberStyles.None, CultureInfo.InvariantCulture, out fractionalPart))
                    {
                        return null;
                    }
                    fractionalPart *= 10;
                }
                else
                {
                    // 先頭2桁のみ使用
                    if (!long.TryParse(fracStr.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out fractionalPart))
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (!long.TryParse(amount, NumberStyles.None, CultureInfo.InvariantCulture, out integerPart))
                {
                    return null;
                }
            }

            // 整数部が0で小数部がある場合: セント/ペンス部分のみ出力
            if (integerPart == 0 && fractionalPart > 0)
            {
                string subUnit = fractionalPart == 1 ? singularSub : pluralSub;
                return string.Concat(NumberToWords.Cardinal(fractionalPart), " ", subUnit);
            }

            // 整数部の出力
            string mainUnit = integerPart == 1 ? singularMain : pluralMain;
            var mainText = NumberToWords.Cardinal(integerPart);

            // 小数部の出力（0より大きい場合のみ）
            if (fractionalPart > 0)
            {
                string subUnit = fractionalPart == 1 ? singularSub : pluralSub;
                return string.Concat(mainText, " ", mainUnit, " ", NumberToWords.Cardinal(fractionalPart), " ", subUnit);
            }

            return string.Concat(mainText, " ", mainUnit);
        }

        /// <summary>
        /// 円は小数部なし。整数部のみを展開する。
        /// </summary>
        private static string? ExpandYen(string amount)
        {
            // カンマ区切りを除去
            amount = amount.Replace(",", "");

            if (!long.TryParse(amount, NumberStyles.None, CultureInfo.InvariantCulture, out long value))
            {
                return null;
            }

            return string.Concat(NumberToWords.Cardinal(value), " yen");
        }
    }
}
