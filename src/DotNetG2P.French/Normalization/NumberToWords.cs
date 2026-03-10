using System.Text;

namespace DotNetG2P.French.Normalization
{
    internal static class NumberToWords
    {
        private static readonly string[] s_units =
        {
            "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
            "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
            "dix-sept", "dix-huit", "dix-neuf"
        };

        private static readonly string[] s_tens =
        {
            "", "dix", "vingt", "trente", "quarante", "cinquante", "soixante",
            "soixante", // 70 = soixante-dix
            "quatre-vingt", // 80 = quatre-vingts (handled specially)
            "quatre-vingt" // 90 = quatre-vingt-dix (handled specially)
        };

        public static string Convert(long number)
        {
            if (number == 0)
                return s_units[0];

            if (number < 0)
                return "moins " + Convert(-number);

            return ConvertPositive(number).ToString();
        }

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return long.TryParse(text, out var value)
                ? Convert(value)
                : text;
        }

        public static string ConvertOrdinal(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 1er, 1ère → premier/première
            if (text == "1er")
                return "premier";
            if (text == "1ère" || text == "1ere")
                return "première";

            // Ne, Nème → Nième (例: 2e→deuxième, 3ème→troisième)
            var digitEnd = 0;
            while (digitEnd < text.Length && char.IsDigit(text[digitEnd]))
                digitEnd++;

            if (digitEnd == 0 || digitEnd == text.Length)
                return text;

            var suffix = text.Substring(digitEnd);
            if (suffix != "e" && suffix != "è" && suffix != "ème" && suffix != "eme"
                && suffix != "ième" && suffix != "ieme"
                && suffix != "er" && suffix != "ère" && suffix != "ere")
                return text;

            if (!long.TryParse(text.Substring(0, digitEnd), out var value) || value <= 0)
                return text;

            if (value == 1)
            {
                return (suffix == "ère" || suffix == "ere") ? "première" : "premier";
            }

            var cardinal = Convert(value);

            // 末尾の"e"を落としてから"ième"を付ける
            if (cardinal.EndsWith("e"))
                cardinal = cardinal.Substring(0, cardinal.Length - 1);

            // "neuf" → "neuv" (9e → neuvième)
            if (cardinal.EndsWith("neuf"))
                cardinal = cardinal.Substring(0, cardinal.Length - 1) + "v";

            // "cinq" → "cinqu" (5e → cinquième)
            if (cardinal.EndsWith("cinq"))
                cardinal += "u";

            return cardinal + "ième";
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

        private static StringBuilder ConvertPositive(long number)
        {
            if (number < 20)
                return new StringBuilder(s_units[number]);

            if (number < 100)
                return ConvertTens(number);

            if (number < 1000)
                return ConvertHundreds(number);

            if (number < 1000000)
                return ConvertThousands(number);

            if (number < 1000000000)
                return ConvertMillions(number);

            return ConvertBillions(number);
        }

        private static StringBuilder ConvertTens(long number)
        {
            var ten = number / 10;
            var unit = number % 10;

            // 70-79: soixante-dix系列 (60+10〜60+19)
            if (ten == 7)
            {
                var sub = number - 60; // 10〜19
                if (sub == 11)
                {
                    // 71 = soixante et onze
                    return new StringBuilder("soixante et onze");
                }
                return new StringBuilder("soixante-" + s_units[sub]);
            }

            // 80: quatre-vingts (末尾s、ただし後続数字がある場合はsなし)
            if (number == 80)
                return new StringBuilder("quatre-vingts");

            // 81-89: quatre-vingt-N (sなし、etなし)
            if (ten == 8)
                return new StringBuilder("quatre-vingt-" + s_units[unit]);

            // 90-99: quatre-vingt-dix系列 (80+10〜80+19)
            if (ten == 9)
            {
                var sub = number - 80; // 10〜19
                return new StringBuilder("quatre-vingt-" + s_units[sub]);
            }

            // 20-69の通常パターン
            if (unit == 0)
                return new StringBuilder(s_tens[ten]);

            // et挿入: 21, 31, 41, 51, 61 (71は上で処理済み)
            if (unit == 1)
                return new StringBuilder(s_tens[ten] + " et un");

            return new StringBuilder(s_tens[ten] + "-" + s_units[unit]);
        }

        private static StringBuilder ConvertHundreds(long number)
        {
            var hundreds = number / 100;
            var rest = number % 100;

            var builder = new StringBuilder();
            if (hundreds == 1)
            {
                if (rest == 0)
                    return builder.Append("cent");
                builder.Append("cent ");
            }
            else
            {
                builder.Append(s_units[hundreds]);
                if (rest == 0)
                {
                    // 200=deux cents, 300=trois cents, etc. (末尾s)
                    builder.Append(" cents");
                    return builder;
                }
                // 201=deux cent un (sなし)
                builder.Append(" cent ");
            }

            builder.Append(ConvertPositive(rest));
            return builder;
        }

        private static StringBuilder ConvertThousands(long number)
        {
            var thousands = number / 1000;
            var rest = number % 1000;

            var builder = new StringBuilder();
            if (thousands == 1)
            {
                builder.Append("mille");
            }
            else
            {
                builder.Append(ConvertPositive(thousands));
                builder.Append(" mille");
            }

            if (rest > 0)
            {
                builder.Append(' ');
                builder.Append(ConvertPositive(rest));
            }

            return builder;
        }

        private static StringBuilder ConvertMillions(long number)
        {
            var millions = number / 1000000;
            var rest = number % 1000000;

            var builder = new StringBuilder();
            if (millions == 1)
            {
                builder.Append("un million");
            }
            else
            {
                builder.Append(ConvertPositive(millions));
                builder.Append(" millions");
            }

            if (rest > 0)
            {
                builder.Append(' ');
                builder.Append(ConvertPositive(rest));
            }

            return builder;
        }

        private static StringBuilder ConvertBillions(long number)
        {
            var billions = number / 1000000000;
            var rest = number % 1000000000;

            var builder = new StringBuilder();
            if (billions == 1)
            {
                builder.Append("un milliard");
            }
            else
            {
                builder.Append(ConvertPositive(billions));
                builder.Append(" milliards");
            }

            if (rest > 0)
            {
                builder.Append(' ');
                builder.Append(ConvertPositive(rest));
            }

            return builder;
        }
    }
}
