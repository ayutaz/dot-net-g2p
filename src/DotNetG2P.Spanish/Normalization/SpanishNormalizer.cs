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
        private static readonly string[] s_monthNames =
        {
            "",
            "enero",
            "febrero",
            "marzo",
            "abril",
            "mayo",
            "junio",
            "julio",
            "agosto",
            "septiembre",
            "octubre",
            "noviembre",
            "diciembre",
        };

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            normalized = ExpandAbbreviations(normalized);
            normalized = ExpandDates(normalized);
            normalized = ExpandTimes(normalized);
            normalized = ExpandPercentages(normalized);
            normalized = ExpandCurrencies(normalized);
            normalized = ExpandMeasurements(normalized);
            normalized = ExpandDecimals(normalized);
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
            text = Regex.Replace(text, @"\bsrta\.", "señorita");
            text = Regex.Replace(text, @"\bsres\.", "señores");
            text = Regex.Replace(text, @"\bdr\.", "doctor");
            text = Regex.Replace(text, @"\bdra\.", "doctora");
            text = Regex.Replace(text, @"\bing\.", "ingeniero");
            text = Regex.Replace(text, @"\blic\.", "licenciado");
            text = Regex.Replace(text, @"\buds?\.", m => m.Value == "ud." ? "usted" : "ustedes");
            text = Regex.Replace(text, @"\btel\.", "teléfono");
            text = Regex.Replace(text, @"\bav\.", "avenida");
            text = Regex.Replace(text, @"\b(?:núm|num)\.", "número");
            text = Regex.Replace(text, @"\bpág\.", "página");
            text = Regex.Replace(text, @"\betc\.", "etcétera");
            text = Regex.Replace(text, @"\baprox\.", "aproximadamente");
            text = Regex.Replace(text, @"\bp\.\s*ej\.", "por ejemplo");
            text = Regex.Replace(text, @"\bee\.\s*uu\.", "estados unidos");
            return text;
        }

        private static string ExpandDates(string text)
        {
            return Regex.Replace(text, @"\b(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})\b", m =>
            {
                if (!int.TryParse(m.Groups[1].Value, out var day)
                    || !int.TryParse(m.Groups[2].Value, out var month)
                    || !long.TryParse(m.Groups[3].Value, out var year)
                    || day < 1
                    || day > 31
                    || month < 1
                    || month > 12)
                {
                    return m.Value;
                }

                return NumberToWords.Convert(day)
                    + " de "
                    + s_monthNames[month]
                    + " de "
                    + NumberToWords.Convert(year);
            });
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
                return ExpandDecimalNumber(m.Groups[1].Value, m.Groups[3].Value);
            });
        }

        private static string ExpandPercentages(string text)
        {
            return Regex.Replace(text, @"(\d+(?:[.,]\d+)?)\s*%", m =>
            {
                return ExpandNumberToken(m.Groups[1].Value) + " por ciento";
            });
        }

        private static string ExpandCurrencies(string text)
        {
            text = Regex.Replace(text, @"([$€])\s*(\d+(?:[.,]\d+)?)", m =>
            {
                return ExpandCurrency(m.Groups[2].Value, m.Groups[1].Value[0]);
            });

            text = Regex.Replace(text, @"(\d+(?:[.,]\d+)?)\s*([$€])", m =>
            {
                return ExpandCurrency(m.Groups[1].Value, m.Groups[2].Value[0]);
            });

            return text;
        }

        private static string ExpandMeasurements(string text)
        {
            return Regex.Replace(text, @"\b(\d+(?:[.,]\d+)?)(?:\s*(km/h|ghz|mhz|khz|hz|km|cm|mm|kg|mg|ml|min)|\s+(m|g|l|h|s))\b", m =>
            {
                var unit = m.Groups[2].Length > 0 ? m.Groups[2].Value : m.Groups[3].Value;
                return ExpandNumberToken(m.Groups[1].Value) + " " + GetUnitName(unit, IsSingularNumericValue(m.Groups[1].Value));
            });
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
                .Replace("@", " arroba ")
                .Replace("=", " igual a ")
                .Replace("#", " número ");
        }

        private static string ExpandNumberToken(string token)
        {
            if (!TrySplitNumber(token, out var wholePart, out var fractionalDigits))
                return token;

            if (fractionalDigits == null)
                return NumberToWords.Convert(wholePart);

            return ExpandDecimalNumber(wholePart.ToString(), fractionalDigits);
        }

        private static string ExpandDecimalNumber(string wholePart, string fractionalDigits)
        {
            return NumberToWords.Convert(wholePart) + " coma " + NumberToWords.ConvertDigits(fractionalDigits);
        }

        private static string ExpandCurrency(string valueToken, char symbol)
        {
            if (!TrySplitNumber(valueToken, out var wholePart, out var fractionalDigits))
                return valueToken + symbol;

            var singularCurrency = symbol == '€' ? "euro" : "dólar";
            var pluralCurrency = symbol == '€' ? "euros" : "dólares";
            var singularMinor = symbol == '€' ? "céntimo" : "centavo";
            var pluralMinor = symbol == '€' ? "céntimos" : "centavos";

            var builder = new StringBuilder();
            builder.Append(NumberToWords.Convert(wholePart));
            builder.Append(' ');
            builder.Append(wholePart == 1 && fractionalDigits == null ? singularCurrency : pluralCurrency);

            if (!string.IsNullOrEmpty(fractionalDigits))
            {
                var normalizedFraction = NormalizeCurrencyMinorUnits(fractionalDigits);
                if (normalizedFraction > 0)
                {
                    builder.Append(" con ");
                    builder.Append(NumberToWords.Convert(normalizedFraction));
                    builder.Append(' ');
                    builder.Append(normalizedFraction == 1 ? singularMinor : pluralMinor);
                }
            }

            return builder.ToString();
        }

        private static int NormalizeCurrencyMinorUnits(string fractionalDigits)
        {
            if (fractionalDigits.Length == 1)
                fractionalDigits += "0";
            else if (fractionalDigits.Length > 2)
                fractionalDigits = fractionalDigits.Substring(0, 2);

            return int.TryParse(fractionalDigits, out var value)
                ? value
                : 0;
        }

        private static bool IsSingularNumericValue(string token)
        {
            return TrySplitNumber(token, out var wholePart, out var fractionalDigits)
                && wholePart == 1
                && string.IsNullOrEmpty(fractionalDigits);
        }

        private static bool TrySplitNumber(string token, out long wholePart, out string? fractionalDigits)
        {
            wholePart = 0;
            fractionalDigits = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var lastSeparator = Math.Max(token.LastIndexOf('.'), token.LastIndexOf(','));
            if (lastSeparator < 0)
                return long.TryParse(token, out wholePart);

            var integerDigits = ExtractDigits(token.Substring(0, lastSeparator));
            fractionalDigits = ExtractDigits(token.Substring(lastSeparator + 1));
            if (integerDigits.Length == 0 || fractionalDigits.Length == 0)
                return false;

            return long.TryParse(integerDigits, out wholePart);
        }

        private static string ExtractDigits(string text)
        {
            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                    builder.Append(text[i]);
            }

            return builder.ToString();
        }

        private static string GetUnitName(string unit, bool singular)
        {
            switch (unit)
            {
                case "km": return singular ? "kilómetro" : "kilómetros";
                case "cm": return singular ? "centímetro" : "centímetros";
                case "mm": return singular ? "milímetro" : "milímetros";
                case "m": return singular ? "metro" : "metros";
                case "kg": return singular ? "kilogramo" : "kilogramos";
                case "mg": return singular ? "miligramo" : "miligramos";
                case "g": return singular ? "gramo" : "gramos";
                case "l": return singular ? "litro" : "litros";
                case "ml": return singular ? "mililitro" : "mililitros";
                case "h": return singular ? "hora" : "horas";
                case "min": return singular ? "minuto" : "minutos";
                case "s": return singular ? "segundo" : "segundos";
                case "hz": return singular ? "hercio" : "hercios";
                case "khz": return singular ? "kilohercio" : "kilohertzios";
                case "mhz": return singular ? "megahercio" : "megahercios";
                case "ghz": return singular ? "gigahercio" : "gigahercios";
                case "km/h": return "kilómetros por hora";
                default: return unit;
            }
        }
    }
}
