// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;

namespace DotNetG2P.English.Normalization
{
    /// <summary>
    /// 数値を英語の読み文字列に変換するユーティリティ。
    /// 基数（Cardinal）、序数（Ordinal）、小数展開（ExpandDecimal）をサポート。
    /// </summary>
    internal static class NumberToWords
    {
        // 0-19の基数読み
        private static readonly string[] Ones =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
            "eighteen", "nineteen"
        };

        // 20,30,...,90の十の位
        private static readonly string[] Tens =
        {
            "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
        };

        // 大きな単位（thousand以上）
        private static readonly string[] ScaleWords =
        {
            "", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion"
        };

        // 各桁の数字読み（小数部展開用）
        private static readonly string[] DigitWords =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"
        };

        /// <summary>
        /// 基数変換: 数値を英語の基数読みに変換する。
        /// 例: 1234 → "one thousand two hundred thirty four"
        /// </summary>
        public static string Cardinal(long number)
        {
            if (number == 0)
            {
                return "zero";
            }

            // 負数処理（long.MinValueのオーバーフローも安全に処理）
            if (number < 0)
            {
                // long.MinValueの場合 -number がオーバーフローするため、
                // unsigned演算で絶対値を取得する
                ulong abs = number == long.MinValue
                    ? ((ulong)long.MaxValue) + 1
                    : (ulong)(-number);
                return string.Concat("negative ", ConvertPositiveUnsigned(abs));
            }

            return ConvertPositiveUnsigned((ulong)number);
        }

        /// <summary>
        /// 序数変換: 数値を英語の序数読みに変換する。
        /// 例: 1 → "first", 21 → "twenty first", 100 → "one hundredth"
        /// </summary>
        public static string Ordinal(long number)
        {
            var cardinal = Cardinal(number);
            return ConvertToOrdinal(cardinal);
        }

        /// <summary>
        /// 小数展開: 整数部をCardinal変換し、"point"の後に小数部の各桁を個別に読む。
        /// 例: ("3","14") → "three point one four"
        /// </summary>
        public static string ExpandDecimal(string intPart, string fracPart)
        {
            // 整数部をパースしてCardinal変換（パース失敗時は元テキストをそのまま返す）
            if (!long.TryParse(intPart, out long intValue))
                return intPart + "." + fracPart;

            var sb = new StringBuilder(Cardinal(intValue));
            sb.Append(" point");

            // 小数部は各桁を個別に読み上げ
            for (int i = 0; i < fracPart.Length; i++)
            {
                int digit = fracPart[i] - '0';
                sb.Append(' ').Append(DigitWords[digit]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 正の整数を英語読みに変換する内部メソッド。
        /// 3桁ずつグループ化して thousand/million/billion... の単位を付与する。
        /// ulongを使用してlong.MinValueの絶対値も安全に処理する。
        /// </summary>
        private static string ConvertPositiveUnsigned(ulong number)
        {
            if (number == 0)
            {
                return "";
            }

            // 3桁ずつのグループに分割
            var parts = new string[ScaleWords.Length];
            int groupIndex = 0;

            ulong remaining = number;
            while (remaining > 0 && groupIndex < ScaleWords.Length)
            {
                int group = (int)(remaining % 1000);
                if (group != 0)
                {
                    var groupText = ConvertBelowThousand(group);
                    if (groupIndex > 0)
                    {
                        parts[groupIndex] = string.Concat(groupText, " ", ScaleWords[groupIndex]);
                    }
                    else
                    {
                        parts[groupIndex] = groupText;
                    }
                }
                remaining /= 1000;
                groupIndex++;
            }

            // 上位グループから結合（StringBuilderで効率化）
            var sb = new StringBuilder();
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i] != null)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(parts[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 0-999の数値を英語読みに変換する。
        /// </summary>
        private static string ConvertBelowThousand(int number)
        {
            if (number == 0)
            {
                return "";
            }

            var sb = new StringBuilder(32);

            // 百の位
            if (number >= 100)
            {
                sb.Append(Ones[number / 100]).Append(" hundred");
                number %= 100;
                if (number > 0)
                {
                    sb.Append(' ');
                }
            }

            // 十の位と一の位
            if (number >= 20)
            {
                sb.Append(Tens[number / 10]);
                if (number % 10 > 0)
                {
                    sb.Append(' ').Append(Ones[number % 10]);
                }
            }
            else if (number > 0)
            {
                sb.Append(Ones[number]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 基数文字列を序数形に変換する。
        /// 末尾の単語に応じて適切な序数接尾辞を適用する。
        /// </summary>
        private static string ConvertToOrdinal(string cardinal)
        {
            // 末尾の単語を取得
            int lastSpace = cardinal.LastIndexOf(' ');
            string prefix;
            string lastWord;

            if (lastSpace >= 0)
            {
                prefix = cardinal.Substring(0, lastSpace + 1);
                lastWord = cardinal.Substring(lastSpace + 1);
            }
            else
            {
                prefix = "";
                lastWord = cardinal;
            }

            // 不規則変換
            string ordinalWord;
            switch (lastWord)
            {
                case "one":
                    ordinalWord = "first";
                    break;
                case "two":
                    ordinalWord = "second";
                    break;
                case "three":
                    ordinalWord = "third";
                    break;
                case "five":
                    ordinalWord = "fifth";
                    break;
                case "eight":
                    ordinalWord = "eighth";
                    break;
                case "nine":
                    ordinalWord = "ninth";
                    break;
                case "twelve":
                    ordinalWord = "twelfth";
                    break;
                default:
                    // yで終わる場合（twenty, thirty等）→ yをiethに置換
                    if (lastWord.EndsWith("y"))
                    {
                        ordinalWord = lastWord.Substring(0, lastWord.Length - 1) + "ieth";
                    }
                    else
                    {
                        // その他は末尾に "th" を付加
                        ordinalWord = lastWord + "th";
                    }
                    break;
            }

            return prefix + ordinalWord;
        }
    }
}
