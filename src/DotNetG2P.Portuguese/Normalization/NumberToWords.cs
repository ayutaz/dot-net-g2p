using System.Text;

namespace DotNetG2P.Portuguese.Normalization
{
    /// <summary>数値をポルトガル語の数詞文字列に変換する。</summary>
    internal static class NumberToWords
    {
        // ブラジルポルトガル語 (BP) 基本数詞 0-19
        private static readonly string[] s_unitsBP =
        {
            "zero", "um", "dois", "tr\u00eas", "quatro", "cinco", "seis", "sete", "oito", "nove",
            "dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis",
            "dezessete", "dezoito", "dezenove"
        };

        // ヨーロッパポルトガル語 (EP) 基本数詞 0-19（方言差がある箇所のみ異なる）
        private static readonly string[] s_unitsEP =
        {
            "zero", "um", "dois", "tr\u00eas", "quatro", "cinco", "seis", "sete", "oito", "nove",
            "dez", "onze", "doze", "treze", "catorze", "quinze", "dezasseis",
            "dezassete", "dezoito", "dezanove"
        };

        // 十の位 (20-90)
        private static readonly string[] s_tens =
        {
            "", "dez", "vinte", "trinta", "quarenta", "cinquenta", "sessenta",
            "setenta", "oitenta", "noventa"
        };

        // 百の位（男性形）: 200-900
        private static readonly string[] s_hundreds =
        {
            "", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos",
            "seiscentos", "setecentos", "oitocentos", "novecentos"
        };

        // 序数詞 (1-31: 日付用)
        private static readonly string[] s_ordinals =
        {
            "", "primeiro", "segundo", "terceiro", "quarto", "quinto",
            "sexto", "s\u00e9timo", "oitavo", "nono", "d\u00e9cimo",
            "d\u00e9cimo primeiro", "d\u00e9cimo segundo", "d\u00e9cimo terceiro",
            "d\u00e9cimo quarto", "d\u00e9cimo quinto", "d\u00e9cimo sexto",
            "d\u00e9cimo s\u00e9timo", "d\u00e9cimo oitavo", "d\u00e9cimo nono",
            "vig\u00e9simo",
            "vig\u00e9simo primeiro", "vig\u00e9simo segundo", "vig\u00e9simo terceiro",
            "vig\u00e9simo quarto", "vig\u00e9simo quinto", "vig\u00e9simo sexto",
            "vig\u00e9simo s\u00e9timo", "vig\u00e9simo oitavo", "vig\u00e9simo nono",
            "trig\u00e9simo", "trig\u00e9simo primeiro"
        };

        /// <summary>数値をポルトガル語の基数詞に変換する。</summary>
        /// <param name="number">変換する数値。</param>
        /// <param name="dialect">方言（デフォルト: Brazilian）。</param>
        /// <returns>ポルトガル語の数詞文字列。</returns>
        public static string Convert(long number, PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            var units = dialect == PortugueseDialect.European ? s_unitsEP : s_unitsBP;

            if (number == 0)
                return units[0];

            if (number < 0)
                return "menos " + Convert(-number, dialect);

            return ConvertPositive(number, units, dialect).ToString();
        }

        /// <summary>文字列形式の数値をポルトガル語に変換する。</summary>
        public static string Convert(string text, PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return long.TryParse(text, out var value)
                ? Convert(value, dialect)
                : text;
        }

        /// <summary>序数詞に変換する（1-31、日付用）。</summary>
        /// <param name="number">変換する数値（1-31）。</param>
        /// <returns>序数詞文字列。範囲外の場合は基数詞を返す。</returns>
        public static string ConvertOrdinal(int number)
        {
            if (number >= 1 && number < s_ordinals.Length)
                return s_ordinals[number];

            // 範囲外は基数詞にフォールバック
            return Convert(number);
        }

        /// <summary>数字列を1桁ずつ読み上げる。</summary>
        public static string ConvertDigits(string digits, PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            if (string.IsNullOrEmpty(digits))
                return string.Empty;

            var units = dialect == PortugueseDialect.European ? s_unitsEP : s_unitsBP;
            var builder = new StringBuilder(digits.Length * 6);
            for (var i = 0; i < digits.Length; i++)
            {
                if (!char.IsDigit(digits[i]))
                    continue;

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(units[digits[i] - '0']);
            }

            return builder.ToString();
        }

        private static StringBuilder ConvertPositive(long number, string[] units, PortugueseDialect dialect)
        {
            if (number < 20)
                return new StringBuilder(units[number]);

            if (number < 100)
                return ConvertTens(number, units);

            if (number < 1000)
                return ConvertHundreds(number, units, dialect);

            if (number < 1_000_000)
                return ConvertThousands(number, units, dialect);

            if (number < 1_000_000_000)
                return ConvertMillions(number, units, dialect);

            if (number < 1_000_000_000_000)
                return ConvertBillions(number, units, dialect);

            return ConvertTrillions(number, units, dialect);
        }

        private static StringBuilder ConvertTens(long number, string[] units)
        {
            var ten = number / 10;
            var unit = number % 10;

            if (unit == 0)
                return new StringBuilder(s_tens[ten]);

            // ポルトガル語: 十の位と一の位は常に「e」で接続
            var builder = new StringBuilder(s_tens[ten]);
            builder.Append(" e ");
            builder.Append(units[unit]);
            return builder;
        }

        private static StringBuilder ConvertHundreds(long number, string[] units, PortugueseDialect dialect)
        {
            var hundreds = number / 100;
            var rest = number % 100;

            var builder = new StringBuilder();

            if (hundreds == 1)
            {
                if (rest == 0)
                {
                    // 100 = "cem"
                    return builder.Append("cem");
                }
                // 101+ = "cento e ..."
                builder.Append("cento");
            }
            else
            {
                builder.Append(s_hundreds[hundreds]);
                if (rest == 0)
                    return builder;
            }

            // 百の位と下位は「e」で接続
            builder.Append(" e ");
            builder.Append(ConvertPositive(rest, units, dialect));
            return builder;
        }

        private static StringBuilder ConvertThousands(long number, string[] units, PortugueseDialect dialect)
        {
            var thousands = number / 1000;
            var rest = number % 1000;

            var builder = new StringBuilder();
            if (thousands == 1)
            {
                builder.Append("mil");
            }
            else
            {
                builder.Append(ConvertPositive(thousands, units, dialect));
                builder.Append(" mil");
            }

            if (rest == 0)
                return builder;

            AppendWithConnector(builder, rest, units, dialect);
            return builder;
        }

        private static StringBuilder ConvertMillions(long number, string[] units, PortugueseDialect dialect)
        {
            var millions = number / 1_000_000;
            var rest = number % 1_000_000;

            var builder = new StringBuilder();
            if (millions == 1)
            {
                builder.Append("um milh\u00e3o");
            }
            else
            {
                builder.Append(ConvertPositive(millions, units, dialect));
                builder.Append(" milh\u00f5es");
            }

            if (rest == 0)
                return builder;

            // milhao/milhoesの後の「e」接続規則
            AppendWithConnector(builder, rest, units, dialect);
            return builder;
        }

        private static StringBuilder ConvertBillions(long number, string[] units, PortugueseDialect dialect)
        {
            var billions = number / 1_000_000_000;
            var rest = number % 1_000_000_000;

            var builder = new StringBuilder();
            if (dialect == PortugueseDialect.European)
            {
                // EP: mil milhoes (10億 = mil milhoes)
                if (billions == 1)
                {
                    builder.Append("mil milh\u00f5es");
                }
                else
                {
                    builder.Append(ConvertPositive(billions, units, dialect));
                    builder.Append(" mil milh\u00f5es");
                }
            }
            else
            {
                // BP: bilhao/bilhoes
                if (billions == 1)
                {
                    builder.Append("um bilh\u00e3o");
                }
                else
                {
                    builder.Append(ConvertPositive(billions, units, dialect));
                    builder.Append(" bilh\u00f5es");
                }
            }

            if (rest == 0)
                return builder;

            AppendWithConnector(builder, rest, units, dialect);
            return builder;
        }

        private static StringBuilder ConvertTrillions(long number, string[] units, PortugueseDialect dialect)
        {
            var trillions = number / 1_000_000_000_000;
            var rest = number % 1_000_000_000_000;

            var builder = new StringBuilder();
            if (dialect == PortugueseDialect.European)
            {
                // EP long scale: 10^12 = bilião/biliões
                if (trillions == 1)
                {
                    builder.Append("um bili\u00e3o");
                }
                else
                {
                    builder.Append(ConvertPositive(trillions, units, dialect));
                    builder.Append(" bili\u00f5es");
                }
            }
            else
            {
                // BP short scale: 10^12 = trilhão/trilhões
                if (trillions == 1)
                {
                    builder.Append("um trilh\u00e3o");
                }
                else
                {
                    builder.Append(ConvertPositive(trillions, units, dialect));
                    builder.Append(" trilh\u00f5es");
                }
            }

            if (rest == 0)
                return builder;

            AppendWithConnector(builder, rest, units, dialect);
            return builder;
        }

        /// <summary>
        /// 「e」接続規則に基づいて残余部分を結合する。
        /// - 下位が1-99: 「e」あり
        /// - 下位が100-900の端数百(100,200,...,900): 「e」あり
        /// - それ以外: スペースのみ
        /// </summary>
        private static void AppendWithConnector(StringBuilder builder, long rest, string[] units, PortugueseDialect dialect)
        {
            if (rest < 100 || (rest < 1000 && rest % 100 == 0))
            {
                builder.Append(" e ");
            }
            else
            {
                builder.Append(' ');
            }

            builder.Append(ConvertPositive(rest, units, dialect));
        }
    }
}
