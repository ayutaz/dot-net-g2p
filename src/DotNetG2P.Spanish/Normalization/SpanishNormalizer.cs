using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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

        private static readonly Dictionary<string, UnitDefinition> s_units = new Dictionary<string, UnitDefinition>(StringComparer.Ordinal)
        {
            ["km/h"] = new UnitDefinition("kilómetro por hora", "kilómetros por hora", SpanishNumberGender.Masculine, apocopate: true),
            ["m/s"] = new UnitDefinition("metro por segundo", "metros por segundo", SpanishNumberGender.Masculine, apocopate: true),
            ["km2"] = new UnitDefinition("kilómetro cuadrado", "kilómetros cuadrados", SpanishNumberGender.Masculine, apocopate: true),
            ["m2"] = new UnitDefinition("metro cuadrado", "metros cuadrados", SpanishNumberGender.Masculine, apocopate: true),
            ["cm2"] = new UnitDefinition("centímetro cuadrado", "centímetros cuadrados", SpanishNumberGender.Masculine, apocopate: true),
            ["gb"] = new UnitDefinition("gigabyte", "gigabytes", SpanishNumberGender.Masculine, apocopate: true),
            ["mb"] = new UnitDefinition("megabyte", "megabytes", SpanishNumberGender.Masculine, apocopate: true),
            ["kb"] = new UnitDefinition("kilobyte", "kilobytes", SpanishNumberGender.Masculine, apocopate: true),
            ["ghz"] = new UnitDefinition("gigahercio", "gigahercios", SpanishNumberGender.Masculine, apocopate: true),
            ["mhz"] = new UnitDefinition("megahercio", "megahercios", SpanishNumberGender.Masculine, apocopate: true),
            ["khz"] = new UnitDefinition("kilohercio", "kilohercios", SpanishNumberGender.Masculine, apocopate: true),
            ["hz"] = new UnitDefinition("hercio", "hercios", SpanishNumberGender.Masculine, apocopate: true),
            ["kg"] = new UnitDefinition("kilogramo", "kilogramos", SpanishNumberGender.Masculine, apocopate: true),
            ["mg"] = new UnitDefinition("miligramo", "miligramos", SpanishNumberGender.Masculine, apocopate: true),
            ["km"] = new UnitDefinition("kilómetro", "kilómetros", SpanishNumberGender.Masculine, apocopate: true),
            ["cm"] = new UnitDefinition("centímetro", "centímetros", SpanishNumberGender.Masculine, apocopate: true),
            ["mm"] = new UnitDefinition("milímetro", "milímetros", SpanishNumberGender.Masculine, apocopate: true),
            ["ml"] = new UnitDefinition("mililitro", "mililitros", SpanishNumberGender.Masculine, apocopate: true),
            ["min"] = new UnitDefinition("minuto", "minutos", SpanishNumberGender.Masculine, apocopate: true),
            ["ms"] = new UnitDefinition("milisegundo", "milisegundos", SpanishNumberGender.Masculine, apocopate: true),
            ["us"] = new UnitDefinition("microsegundo", "microsegundos", SpanishNumberGender.Masculine, apocopate: true),
            ["ns"] = new UnitDefinition("nanosegundo", "nanosegundos", SpanishNumberGender.Masculine, apocopate: true),
            ["°c"] = new UnitDefinition("grado celsius", "grados celsius", SpanishNumberGender.Masculine, apocopate: true),
            ["°f"] = new UnitDefinition("grado fahrenheit", "grados fahrenheit", SpanishNumberGender.Masculine, apocopate: true),
            ["m"] = new UnitDefinition("metro", "metros", SpanishNumberGender.Masculine, apocopate: true),
            ["g"] = new UnitDefinition("gramo", "gramos", SpanishNumberGender.Masculine, apocopate: true),
            ["l"] = new UnitDefinition("litro", "litros", SpanishNumberGender.Masculine, apocopate: true),
            ["h"] = new UnitDefinition("hora", "horas", SpanishNumberGender.Feminine, apocopate: false),
            ["s"] = new UnitDefinition("segundo", "segundos", SpanishNumberGender.Masculine, apocopate: true),
        };

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            normalized = ProtectMeasurementGlyphs(normalized);
            normalized = ExpandAbbreviations(normalized);
            normalized = ExpandIsoDates(normalized);
            normalized = ExpandDates(normalized);
            normalized = ExpandTimes(normalized);
            normalized = ExpandPercentages(normalized);
            normalized = ExpandCurrencies(normalized);
            normalized = ExpandMeasurements(normalized);
            normalized = ExpandNumericRanges(normalized);
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

        private static string ProtectMeasurementGlyphs(string text)
        {
            return text
                .Replace("μs", "us")
                .Replace("µs", "us");
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
            text = Regex.Replace(text, @"\bart\.", "artículo");
            text = Regex.Replace(text, @"\bcap\.", "capítulo");
            text = Regex.Replace(text, @"\bd(?:e)?pto\.", "departamento");
            text = Regex.Replace(text, @"\buds?\.", m => m.Value == "ud." ? "usted" : "ustedes");
            text = Regex.Replace(text, @"\btel\.", "teléfono");
            text = Regex.Replace(text, @"\bav\.", "avenida");
            text = Regex.Replace(text, @"\b(?:núms|nums)\.", "números");
            text = Regex.Replace(text, @"\b(?:núm|num)\.", "número");
            text = Regex.Replace(text, @"\bn\s*[. ]*[º°o]\b", "número");
            text = Regex.Replace(text, @"\bpágs\.", "páginas");
            text = Regex.Replace(text, @"\bpág\.", "página");
            text = Regex.Replace(text, @"\betc\.", "etcétera");
            text = Regex.Replace(text, @"\baprox\.", "aproximadamente");
            text = Regex.Replace(text, @"\bp\.\s*ej\.", "por ejemplo");
            text = Regex.Replace(text, @"\ba\.\s*m\.", "a eme");
            text = Regex.Replace(text, @"\bp\.\s*m\.", "pe eme");
            text = Regex.Replace(text, @"\bee\.\s*uu\.", "estados unidos");
            return text;
        }

        private static string ExpandIsoDates(string text)
        {
            return Regex.Replace(text, @"\b(\d{4})-(\d{1,2})-(\d{1,2})\b", m => ExpandDateParts(m.Groups[3].Value, m.Groups[2].Value, m.Groups[1].Value, m.Value));
        }

        private static string ExpandDates(string text)
        {
            text = Regex.Replace(text, @"\b(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})\b", m => ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value));
            text = Regex.Replace(text, @"\b(\d{1,2})\.(\d{1,2})\.(\d{2,4})\b", m => ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value));
            return text;
        }

        private static string ExpandDateParts(string dayText, string monthText, string yearText, string fallback)
        {
            if (!int.TryParse(dayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day)
                || !int.TryParse(monthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)
                || !long.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                || day < 1
                || day > 31
                || month < 1
                || month > 12)
            {
                return fallback;
            }

            var spokenDay = day == 1 ? "primero" : NumberToWords.Convert(day);
            return spokenDay + " de " + s_monthNames[month] + " de " + NumberToWords.Convert(year);
        }

        private static string ExpandTimes(string text)
        {
            text = Regex.Replace(text, @"\b(\d{1,2})h(?:\s*(\d{1,2}))?\b", m =>
            {
                var hours = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!m.Groups[2].Success)
                    return ExpandMeasuredValue(m.Groups[1].Value, s_units["h"]);

                var minutes = long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                return NumberToWords.ConvertAttributed(hours, SpanishNumberGender.Feminine, apocopate: false) + " y " + NumberToWords.Convert(minutes);
            });

            return Regex.Replace(text, @"\b(\d{1,2}):(\d{2})\b", m =>
            {
                var hours = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var minutes = long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                var spokenHours = NumberToWords.ConvertAttributed(hours, SpanishNumberGender.Feminine, apocopate: false);
                return minutes == 0
                    ? spokenHours + " en punto"
                    : spokenHours + " y " + NumberToWords.Convert(minutes);
            });
        }

        private static string ExpandDecimals(string text)
        {
            return Regex.Replace(text, @"\b(\d+)([.,])(\d+)\b", m => ExpandDecimalNumber(m.Groups[1].Value, m.Groups[3].Value));
        }

        private static string ExpandPercentages(string text)
        {
            return Regex.Replace(text, @"(\d+(?:[.,]\d+)?)\s*%", m => ExpandNumberToken(m.Groups[1].Value) + " por ciento");
        }

        private static string ExpandCurrencies(string text)
        {
            text = Regex.Replace(text, @"([$€])\s*(\d+(?:[.,]\d+)?)", m => ExpandCurrency(m.Groups[2].Value, m.Groups[1].Value[0]));
            text = Regex.Replace(text, @"(\d+(?:[.,]\d+)?)\s*([$€])", m => ExpandCurrency(m.Groups[1].Value, m.Groups[2].Value[0]));
            return text;
        }

        private static string ExpandMeasurements(string text)
        {
            return Regex.Replace(text, @"\b(\d+(?:[.,]\d+)?)(?:\s*(km/h|m/s|km2|m2|cm2|ghz|mhz|khz|hz|gb|mb|kb|km|cm|mm|kg|mg|ml|min|ms|us|ns|°c|°f)|\s+(m|g|l|h|s))\b", m =>
            {
                var unitKey = m.Groups[2].Length > 0 ? m.Groups[2].Value : m.Groups[3].Value;
                if (!s_units.TryGetValue(unitKey, out var definition))
                    return m.Value;

                return ExpandMeasuredValue(m.Groups[1].Value, definition);
            });
        }

        private static string ExpandNumericRanges(string text)
        {
            return Regex.Replace(text, @"\b(\d+)\s*[-–]\s*(\d+)\b", m =>
            {
                var left = NumberToWords.Convert(m.Groups[1].Value);
                var right = NumberToWords.Convert(m.Groups[2].Value);
                return left + " a " + right;
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
                .Replace("#", " número ")
                .Replace("§", " sección ")
                .Replace("№", " número ")
                .Replace("×", " por ");
        }

        private static string ExpandMeasuredValue(string token, UnitDefinition definition)
        {
            if (!TrySplitNumber(token, out var wholePart, out var fractionalDigits))
                return token + " " + definition.Plural;

            var spoken = fractionalDigits == null
                ? NumberToWords.ConvertAttributed(wholePart, definition.Gender, definition.Apocopate)
                : ExpandDecimalNumber(wholePart.ToString(CultureInfo.InvariantCulture), fractionalDigits);

            return spoken + " " + (wholePart == 1 && string.IsNullOrEmpty(fractionalDigits) ? definition.Singular : definition.Plural);
        }

        private static string ExpandNumberToken(string token)
        {
            if (!TrySplitNumber(token, out var wholePart, out var fractionalDigits))
                return token;

            return fractionalDigits == null
                ? NumberToWords.Convert(wholePart)
                : ExpandDecimalNumber(wholePart.ToString(CultureInfo.InvariantCulture), fractionalDigits);
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
            builder.Append(NumberToWords.ConvertAttributed(wholePart, SpanishNumberGender.Masculine, apocopate: true));
            builder.Append(' ');
            builder.Append(wholePart == 1 ? singularCurrency : pluralCurrency);

            if (!string.IsNullOrEmpty(fractionalDigits))
            {
                var normalizedFraction = NormalizeCurrencyMinorUnits(fractionalDigits);
                if (normalizedFraction > 0)
                {
                    builder.Append(" con ");
                    builder.Append(NumberToWords.ConvertAttributed(normalizedFraction, SpanishNumberGender.Masculine, apocopate: true));
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

            return int.TryParse(fractionalDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static bool TrySplitNumber(string token, out long wholePart, out string? fractionalDigits)
        {
            wholePart = 0;
            fractionalDigits = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var lastSeparator = Math.Max(token.LastIndexOf('.'), token.LastIndexOf(','));
            if (lastSeparator < 0)
                return long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out wholePart);

            var integerDigits = ExtractDigits(token.Substring(0, lastSeparator));
            fractionalDigits = ExtractDigits(token.Substring(lastSeparator + 1));
            if (integerDigits.Length == 0 || fractionalDigits.Length == 0)
                return false;

            return long.TryParse(integerDigits, NumberStyles.Integer, CultureInfo.InvariantCulture, out wholePart);
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

        private readonly struct UnitDefinition
        {
            public string Singular { get; }
            public string Plural { get; }
            public SpanishNumberGender Gender { get; }
            public bool Apocopate { get; }

            public UnitDefinition(string singular, string plural, SpanishNumberGender gender, bool apocopate)
            {
                Singular = singular;
                Plural = plural;
                Gender = gender;
                Apocopate = apocopate;
            }
        }
    }
}
