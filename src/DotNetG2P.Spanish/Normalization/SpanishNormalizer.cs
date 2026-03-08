using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace DotNetG2P.Spanish.Normalization
{
    /// <summary>
    /// スペイン語入力の軽量正規化。
    /// </summary>
    internal static class SpanishNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            normalized = ExpandAbbreviations(normalized);
            normalized = ExpandTimes(normalized);
            normalized = ExpandDecimals(normalized);
            normalized = ExpandPercentages(normalized);
            normalized = ExpandCurrencies(normalized);
            normalized = ExpandStandaloneNumbers(normalized);
            normalized = ExpandSymbols(normalized);

            var builder = new StringBuilder(normalized.Length);
            var prevWasSpace = true;

            for (var i = 0; i < normalized.Length; i++)
            {
                var ch = normalized[i];
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevWasSpace)
                    {
                        builder.Append(' ');
                        prevWasSpace = true;
                    }

                    continue;
                }

                if (char.IsLetter(ch) || ch == '\'' || ch == 'á' || ch == 'é' || ch == 'í' || ch == 'ó' || ch == 'ú' || ch == 'ü' || ch == 'ñ')
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

        public static IReadOnlyList<string> Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string ExpandAbbreviations(string text)
        {
            text = Regex.Replace(text, @"\bsr\.", "señor");
            text = Regex.Replace(text, @"\bsra\.", "señora");
            text = Regex.Replace(text, @"\bsres\.", "señores");
            text = Regex.Replace(text, @"\bdr\.", "doctor");
            text = Regex.Replace(text, @"\bdra\.", "doctora");
            text = Regex.Replace(text, @"\buds?\.", m => m.Value == "ud." ? "usted" : "ustedes");
            text = Regex.Replace(text, @"\btel\.", "teléfono");
            text = Regex.Replace(text, @"\bav\.", "avenida");
            text = Regex.Replace(text, @"\b(?:núm|num)\.", "número");
            text = Regex.Replace(text, @"\bpág\.", "página");
            text = Regex.Replace(text, @"\bee\.\s*uu\.", "estados unidos");
            return text;
        }

        private static string ExpandTimes(string text)
        {
            return Regex.Replace(text, @"\b(\d{1,2}):(\d{2})\b", m =>
            {
                var hours = long.Parse(m.Groups[1].Value);
                var minutes = long.Parse(m.Groups[2].Value);
                return minutes == 0
                    ? NumberToWords.Convert(hours) + " en punto"
                    : NumberToWords.Convert(hours) + " y " + NumberToWords.Convert(minutes);
            });
        }

        private static string ExpandDecimals(string text)
        {
            return Regex.Replace(text, @"\b(\d+)([.,])(\d+)\b", m =>
            {
                var integerPart = NumberToWords.Convert(m.Groups[1].Value);
                var fractionalDigits = NumberToWords.ConvertDigits(m.Groups[3].Value);
                return integerPart + " coma " + fractionalDigits;
            });
        }

        private static string ExpandPercentages(string text)
        {
            return Regex.Replace(text, @"(\d+)\s*%", m => NumberToWords.Convert(m.Groups[1].Value) + " por ciento");
        }

        private static string ExpandCurrencies(string text)
        {
            text = Regex.Replace(text, @"\$(\d+)", m =>
            {
                var value = long.Parse(m.Groups[1].Value);
                return NumberToWords.Convert(value) + " " + (value == 1 ? "dólar" : "dólares");
            });

            text = Regex.Replace(text, @"€(\d+)", m =>
            {
                var value = long.Parse(m.Groups[1].Value);
                return NumberToWords.Convert(value) + " " + (value == 1 ? "euro" : "euros");
            });

            return text;
        }

        private static string ExpandStandaloneNumbers(string text)
        {
            return Regex.Replace(text, @"\b\d+\b", m => NumberToWords.Convert(m.Value));
        }

        private static string ExpandSymbols(string text)
        {
            return text
                .Replace("&", " y ")
                .Replace("+", " más ")
                .Replace("@", " arroba ");
        }
    }
}
