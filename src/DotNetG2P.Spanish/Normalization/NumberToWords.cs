using System.Text;

namespace DotNetG2P.Spanish.Normalization
{
    internal enum SpanishNumberGender : byte
    {
        Masculine = 0,
        Feminine = 1,
    }

    internal static class NumberToWords
    {
        private static readonly string[] s_units =
        {
            "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
            "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve",
            "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro", "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
        };

        private static readonly string[] s_tens =
        {
            "", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"
        };

        private static readonly string[] s_hundreds =
        {
            "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
        };

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return long.TryParse(text, out var value)
                ? Convert(value)
                : text;
        }

        public static string ConvertDigits(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;

            var builder = new StringBuilder(digits.Length * 6);
            for (var i = 0; i < digits.Length; i++)
            {
                if (!char.IsDigit(digits[i]))
                    continue;

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(s_units[digits[i] - '0']);
            }

            return builder.ToString();
        }

        public static string Convert(long value)
        {
            if (value == 0)
                return s_units[0];

            if (value < 0)
                return "menos " + Convert(-value);

            if (value < 30)
                return s_units[value];

            if (value < 100)
            {
                var tens = value / 10;
                var units = value % 10;
                return units == 0 ? s_tens[tens] : s_tens[tens] + " y " + s_units[units];
            }

            if (value == 100)
                return "cien";

            if (value < 1000)
            {
                var hundreds = value / 100;
                var rest = value % 100;
                return rest == 0 ? s_hundreds[hundreds] : s_hundreds[hundreds] + " " + Convert(rest);
            }

            if (value < 1000000)
            {
                var thousands = value / 1000;
                var rest = value % 1000;
                var prefix = thousands == 1 ? "mil" : Convert(thousands) + " mil";
                return rest == 0 ? prefix : prefix + " " + Convert(rest);
            }

            var millions = value / 1000000;
            var remainder = value % 1000000;
            var millionPrefix = millions == 1 ? "un millón" : Convert(millions) + " millones";
            return remainder == 0 ? millionPrefix : millionPrefix + " " + Convert(remainder);
        }

        public static string ConvertAttributed(long value, SpanishNumberGender gender, bool apocopate)
        {
            var converted = Convert(value);
            return ApplyAgreement(converted, gender, apocopate);
        }

        private static string ApplyAgreement(string text, SpanishNumberGender gender, bool apocopate)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (gender == SpanishNumberGender.Feminine)
            {
                text = text
                    .Replace("doscientos", "doscientas")
                    .Replace("trescientos", "trescientas")
                    .Replace("cuatrocientos", "cuatrocientas")
                    .Replace("quinientos", "quinientas")
                    .Replace("seiscientos", "seiscientas")
                    .Replace("setecientos", "setecientas")
                    .Replace("ochocientos", "ochocientas")
                    .Replace("novecientos", "novecientas");

                if (text == "uno")
                    return "una";
                if (text == "veintiuno")
                    return "veintiuna";
                if (text.EndsWith(" y uno"))
                    return text.Substring(0, text.Length - " y uno".Length) + " y una";
                if (text.EndsWith(" uno"))
                    return text.Substring(0, text.Length - " uno".Length) + " una";

                return text;
            }

            if (!apocopate)
                return text;

            if (text == "uno")
                return "un";
            if (text == "veintiuno")
                return "veintiún";
            if (text.EndsWith(" y uno"))
                return text.Substring(0, text.Length - " y uno".Length) + " y un";
            if (text.EndsWith(" uno"))
                return text.Substring(0, text.Length - " uno".Length) + " un";

            return text;
        }
    }
}
