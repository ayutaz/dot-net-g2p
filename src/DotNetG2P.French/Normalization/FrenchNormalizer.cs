using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetG2P.French.Normalization
{
    /// <summary>
    /// フランス語入力のテキスト正規化（F2拡張版）。
    /// </summary>
    internal static class FrenchNormalizer
    {
        private static readonly string[] s_monthNames =
        {
            "",
            "janvier",
            "février",
            "mars",
            "avril",
            "mai",
            "juin",
            "juillet",
            "août",
            "septembre",
            "octobre",
            "novembre",
            "décembre",
        };

        /// <summary>
        /// テキストを正規化し、数字・記号・略語等を読み上げ形式に展開する。
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. NFC正規化 + 小文字化
            var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // 2-10. 各展開パイプライン
            normalized = ExpandAbbreviations(normalized);
            normalized = ExpandDates(normalized);
            normalized = ExpandTimes(normalized);
            normalized = ExpandCurrencies(normalized);
            normalized = ExpandPercentages(normalized);
            normalized = ExpandUnits(normalized);
            normalized = ExpandDecimals(normalized);
            normalized = ExpandNumbers(normalized);
            normalized = ExpandSymbols(normalized);

            // 11. 空白正規化 + trim
            return NormalizeWhitespace(normalized);
        }

        /// <summary>
        /// テキストを空白で分割しトークン列を返す。
        /// アポストロフはトークン内に保持する（例: l'homme → "l'homme"）。
        /// 句読点・記号は除去する。
        /// </summary>
        public static string[] Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            return TokenizeNormalized(Normalize(text));
        }

        /// <summary>
        /// 正規化済みテキストをトークン列に分割する（内部用）。
        /// 二重正規化を避けるため、既に Normalize 済みのテキストを受け取る。
        /// </summary>
        internal static string[] TokenizeNormalized(string normalized)
        {
            if (string.IsNullOrEmpty(normalized))
                return Array.Empty<string>();

            var tokens = new List<string>();
            var builder = new StringBuilder();

            for (var i = 0; i < normalized.Length; i++)
            {
                var ch = normalized[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    if (builder.Length > 0)
                    {
                        tokens.Add(builder.ToString());
                        builder.Clear();
                    }
                    continue;
                }

                // アポストロフ（フランス語のエリジオン）はトークン内に保持
                if (ch == '\'' || ch == '\u2019')
                {
                    builder.Append('\'');
                    continue;
                }

                // ハイフンはトークン内に保持（複合語: peut-être 等）
                if (ch == '-' && builder.Length > 0 && i + 1 < normalized.Length && char.IsLetter(normalized[i + 1]))
                {
                    builder.Append(ch);
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    continue;
                }

                // その他の句読点・記号は区切りとして扱う
                if (builder.Length > 0)
                {
                    tokens.Add(builder.ToString());
                    builder.Clear();
                }
            }

            if (builder.Length > 0)
                tokens.Add(builder.ToString());

            return tokens.ToArray();
        }

        // --- 展開メソッド群 ---

        private static string ExpandAbbreviations(string text)
        {
            text = Regex.Replace(text, @"\bm\.\s", "monsieur ");
            text = Regex.Replace(text, @"\bmme\b\.?", "madame");
            text = Regex.Replace(text, @"\bmlle\b\.?", "mademoiselle");
            text = Regex.Replace(text, @"\bdr\b\.?", "docteur");
            text = Regex.Replace(text, @"\bme\b\.", "maître");
            text = Regex.Replace(text, @"\bprof\b\.?", "professeur");
            text = Regex.Replace(text, @"\betc\.", "et cetera");
            text = Regex.Replace(text, @"\bp\.\s*ex\.", "par exemple");
            text = Regex.Replace(text, @"\bn\s*°", "numéro");
            text = Regex.Replace(text, @"\bst\b\.", "saint");
            text = Regex.Replace(text, @"\bste\b\.", "sainte");
            text = Regex.Replace(text, @"\bav\.\s*j\.\s*-?\s*c\.", "avant jésus-christ");
            text = Regex.Replace(text, @"\bap\.\s*j\.\s*-?\s*c\.", "après jésus-christ");
            return text;
        }

        private static string ExpandDates(string text)
        {
            // DD/MM/YYYY or DD-MM-YYYY or DD.MM.YYYY
            text = Regex.Replace(text, @"\b(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{2,4})\b", m =>
                ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value));
            return text;
        }

        private static string ExpandDateParts(string dayText, string monthText, string yearText, string fallback)
        {
            if (!int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)
                || !long.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                || year < 1 || year > 9999
                || day < 1 || month < 1 || month > 12)
            {
                return fallback;
            }

            // 2桁年を4桁に展開
            if (year < 100)
                year += year < 50 ? 2000 : 1900;

            // 日数バリデーション
            if (day > DateTime.DaysInMonth((int)year, month))
                return fallback;

            // 1er→premier、それ以外は基数詞
            var spokenDay = day == 1 ? "le premier" : "le " + NumberToWords.Convert(day);
            return spokenDay + " " + s_monthNames[month] + " " + NumberToWords.Convert(year);
        }

        private static string ExpandTimes(string text)
        {
            // NNhNN format
            text = Regex.Replace(text, @"\b(\d{1,2})h(?:\s*(\d{1,2}))?\b", m =>
            {
                if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
                    return m.Value;

                if (hours > 23)
                    return m.Value;

                // 0h→minuit, 12h→midi
                if (!m.Groups[2].Success || string.IsNullOrEmpty(m.Groups[2].Value))
                {
                    if (hours == 0) return "minuit";
                    if (hours == 12) return "midi";
                    return NumberToWords.Convert(hours) + " heures";
                }

                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
                    return m.Value;

                if (minutes > 59)
                    return m.Value;

                string hourPart;
                if (hours == 0) hourPart = "minuit";
                else if (hours == 12) hourPart = "midi";
                else hourPart = NumberToWords.Convert(hours) + " heures";

                if (minutes == 0)
                    return hourPart;

                return hourPart + " " + NumberToWords.Convert(minutes);
            });

            return text;
        }

        private static string ExpandCurrencies(string text)
        {
            // N€ or N,NN€ (後置ユーロ)
            text = Regex.Replace(text, @"(\d+)(?:[,.](\d{1,2}))?\s*€", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "euro", "euros", "centime", "centimes"));

            // €N (前置ユーロ)
            text = Regex.Replace(text, @"€\s*(\d+)(?:[,.](\d{1,2}))?", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "euro", "euros", "centime", "centimes"));

            // $N (前置ドル)
            text = Regex.Replace(text, @"\$\s*(\d+)(?:[,.](\d{1,2}))?", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "dollar", "dollars", "cent", "cents"));

            // N$ (後置ドル)
            text = Regex.Replace(text, @"(\d+)(?:[,.](\d{1,2}))?\s*\$", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "dollar", "dollars", "cent", "cents"));

            return text;
        }

        private static string ExpandCurrencyValue(string wholeText, string? fractionText,
            string singular, string plural, string minorSingular, string minorPlural)
        {
            if (!long.TryParse(wholeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
                return wholeText;

            var builder = new StringBuilder();
            builder.Append(NumberToWords.Convert(whole));
            builder.Append(' ');
            builder.Append(whole == 1 ? singular : plural);

            if (!string.IsNullOrEmpty(fractionText))
            {
                // 小数部を2桁に正規化 (例: "5"→50)
                var normalized = fractionText;
                if (normalized.Length == 1)
                    normalized += "0";
                else if (normalized.Length > 2)
                    normalized = normalized.Substring(0, 2);

                if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cents) && cents > 0)
                {
                    builder.Append(' ');
                    builder.Append(NumberToWords.Convert(cents));
                    builder.Append(' ');
                    builder.Append(cents == 1 ? minorSingular : minorPlural);
                }
            }

            return builder.ToString();
        }

        private static string ExpandPercentages(string text)
        {
            return Regex.Replace(text, @"(\d+(?:[,.]\d+)?)\s*%", m =>
            {
                var numText = m.Groups[1].Value;
                // 小数を含む場合はExpandDecimalNumber
                var commaIdx = numText.IndexOf(',');
                var dotIdx = numText.IndexOf('.');
                if (commaIdx >= 0 || dotIdx >= 0)
                {
                    var sep = commaIdx >= 0 ? commaIdx : dotIdx;
                    var wholePart = numText.Substring(0, sep);
                    var fracPart = numText.Substring(sep + 1);
                    return NumberToWords.Convert(wholePart) + " virgule " + NumberToWords.ConvertDigits(fracPart) + " pour cent";
                }
                return NumberToWords.Convert(numText) + " pour cent";
            });
        }

        private static string ExpandUnits(string text)
        {
            // 温度 (°C) を先に処理
            text = Regex.Replace(text, @"(\d+)\s*°c\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " degré celsius" : " degrés celsius"));

            // 複合単位を先に（km, kg, cm, mm）
            text = Regex.Replace(text, @"(\d+)\s*km\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " kilomètre" : " kilomètres"));
            text = Regex.Replace(text, @"(\d+)\s*kg\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " kilogramme" : " kilogrammes"));
            text = Regex.Replace(text, @"(\d+)\s*cm\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " centimètre" : " centimètres"));
            text = Regex.Replace(text, @"(\d+)\s*mm\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " millimètre" : " millimètres"));

            // 単独単位（m, l）
            text = Regex.Replace(text, @"(\d+)\s*m\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " mètre" : " mètres"));
            text = Regex.Replace(text, @"(\d+)\s*l\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + (m.Groups[1].Value == "1" ? " litre" : " litres"));

            return text;
        }

        private static string ExpandDecimals(string text)
        {
            // N,N → N virgule N (フランス語では","が小数点)
            return Regex.Replace(text, @"\b(\d+),(\d+)\b", m =>
                NumberToWords.Convert(m.Groups[1].Value) + " virgule " + NumberToWords.ConvertDigits(m.Groups[2].Value));
        }

        private static string ExpandNumbers(string text)
        {
            return Regex.Replace(text, @"\b\d+\b", m => NumberToWords.Convert(m.Value));
        }

        private static string ExpandSymbols(string text)
        {
            return text
                .Replace("&", " et ")
                .Replace("@", " arobase ")
                .Replace("§", " paragraphe ")
                .Replace("#", " dièse ")
                .Replace("+", " plus ")
                .Replace("=", " égal ");
        }

        private static string NormalizeWhitespace(string text)
        {
            var builder = new StringBuilder(text.Length);
            var prevWasSpace = true;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevWasSpace)
                    {
                        builder.Append(' ');
                        prevWasSpace = true;
                    }
                    continue;
                }

                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '\'' || ch == '\u2019')
                {
                    builder.Append(ch);
                    prevWasSpace = false;
                    continue;
                }

                if (!prevWasSpace)
                {
                    builder.Append(' ');
                    prevWasSpace = true;
                }
            }

            return builder.ToString().Trim();
        }
    }
}
