using System;
using System.Text;

namespace DotNetG2P.Spanish.Normalization
{
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

            if (!long.TryParse(text, out var value))
                return text;

            return Convert(value);
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
    }
}
