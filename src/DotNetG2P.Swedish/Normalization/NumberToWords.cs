using System.Text;

namespace DotNetG2P.Swedish.Normalization
{
    /// <summary>数値をスウェーデン語の数詞文字列に変換する。</summary>
    internal static class NumberToWords
    {
        // 基本数詞 0-20（中性形: ett）
        private static readonly string[] s_unitsEtt =
        {
            "noll", "ett", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio",
            "tio", "elva", "tolv", "tretton", "fjorton", "femton", "sexton",
            "sjutton", "arton", "nitton", "tjugo"
        };

        // 基本数詞 0-20（通性形: en）
        private static readonly string[] s_unitsEn =
        {
            "noll", "en", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio",
            "tio", "elva", "tolv", "tretton", "fjorton", "femton", "sexton",
            "sjutton", "arton", "nitton", "tjugo"
        };

        // 十の位 (30-90)
        private static readonly string[] s_tens =
        {
            "", "tio", "tjugo", "trettio", "fyrtio", "femtio", "sextio",
            "sjuttio", "åttio", "nittio"
        };

        // 序数詞 1-12 (基本形)
        private static readonly string[] s_ordinalBase =
        {
            "", "första", "andra", "tredje", "fjärde", "femte", "sjätte",
            "sjunde", "åttonde", "nionde", "tionde", "elfte", "tolfte"
        };

        /// <summary>数値をスウェーデン語の基数詞に変換する。</summary>
        /// <param name="number">変換する数値。</param>
        /// <param name="useEn">true=通性形（en）、false=中性形（ett、デフォルト）。</param>
        /// <returns>スウェーデン語の基数詞文字列。</returns>
        public static string ToCardinal(long number, bool useEn = false)
        {
            if (number == 0)
                return "noll";

            if (number < 0)
                return "minus " + ToCardinal(-number, useEn);

            return ConvertPositive(number, useEn).ToString();
        }

        /// <summary>数値をスウェーデン語の序数詞に変換する。</summary>
        /// <param name="number">変換する数値（正の整数）。</param>
        /// <returns>スウェーデン語の序数詞文字列。</returns>
        public static string ToOrdinal(long number)
        {
            if (number <= 0)
                return ToCardinal(number) + "de";

            // 1-12は個別の不規則形
            if (number <= 12)
                return s_ordinalBase[number];

            // 13-19: 基数詞 + "de"
            if (number < 20)
                return s_unitsEtt[number] + "de";

            // 20: tjugonde
            if (number == 20)
                return "tjugonde";

            // 合成序数詞: 十の位 + 一の位の序数詞
            var unit = number % 10;
            var ten = number / 10;

            // 一の位が0の場合は十の位の序数詞
            if (unit == 0)
            {
                if (number < 100)
                    return s_tens[ten] + "nde";

                if (number == 100)
                    return "hundrade";

                if (number == 1000)
                    return "tusende";

                // 大きな端数は基数詞 + "de"
                return ToCardinal(number) + "de";
            }

            // 21-99: 十の位(基数) + 一の位(序数)
            if (number < 100)
            {
                var tenPart = s_tens[ten];
                var unitOrdinal = unit <= 12 ? s_ordinalBase[unit] : s_unitsEtt[unit] + "de";
                return tenPart + unitOrdinal;
            }

            // 100以上: 基数詞部分 + 序数接尾辞（スペースなしで直結）
            var remainder = number % 100;

            if (remainder == 0)
            {
                // 端数がない場合: 基数詞 + 序数接尾辞
                // 例: 200→"tvåhundrade", 1000→"tusende"（unit==0で上で処理済み）、2000→"tvåtusende"
                return ToCardinal(number) + "de";
            }

            // 百の位以上を基数詞で、残りを序数詞で（スペースなしで直結）
            // 例: 101→"etthundra"+"första"="etthundraförsta"
            var hundredsPart = number / 100 * 100;
            return ToCardinal(hundredsPart) + ToOrdinal(remainder);
        }

        /// <summary>小数文字列をスウェーデン語に変換する（例: "3,14" → "tre komma ett fyra"）。</summary>
        /// <param name="decimalString">小数点（コンマ）を含む数値文字列。</param>
        /// <returns>スウェーデン語の読み上げ文字列。</returns>
        public static string ToDecimal(string decimalString)
        {
            if (string.IsNullOrEmpty(decimalString))
                return string.Empty;

            // コンマまたはピリオドで分割
            var sepIdx = decimalString.IndexOf(',');
            if (sepIdx < 0)
                sepIdx = decimalString.IndexOf('.');
            if (sepIdx < 0)
            {
                // 小数点なし → 通常の基数詞変換
                return long.TryParse(decimalString, out var whole)
                    ? ToCardinal(whole)
                    : decimalString;
            }

            var wholePart = decimalString.Substring(0, sepIdx);
            var fracPart = decimalString.Substring(sepIdx + 1);

            var builder = new StringBuilder();

            // 整数部
            if (long.TryParse(wholePart, out var wholeValue))
                builder.Append(ToCardinal(wholeValue));
            else
                builder.Append(wholePart);

            builder.Append(" komma ");

            // 小数部: 1桁ずつ読む（先頭ゼロ保持: 05→noll fem）
            builder.Append(ConvertDigits(fracPart));

            return builder.ToString();
        }

        /// <summary>
        /// 年号をスウェーデン語に変換する。
        /// 1100-1999は百単位読み（例: 1985→"nittonhundraåttiofem"）、
        /// それ以外は通常の基数詞変換。
        /// </summary>
        /// <param name="year">変換する年号。</param>
        /// <returns>スウェーデン語の年号文字列。</returns>
        public static string ConvertYear(long year)
        {
            // 1100-1999: 百単位読み（スウェーデン語の慣用表現）
            if (year >= 1100 && year <= 1999)
            {
                var centuries = year / 100;
                var remainder = year % 100;
                var result = ConvertPositive(centuries, useEn: false).ToString() + "hundra";
                if (remainder > 0)
                    result += ConvertPositive(remainder, useEn: false).ToString();
                return result;
            }

            return ToCardinal(year);
        }

        /// <summary>数字列を1桁ずつ読み上げる（電話番号・年号等）。</summary>
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

                builder.Append(s_unitsEtt[digits[i] - '0']);
            }

            return builder.ToString();
        }

        // =================================================================
        // 内部変換メソッド
        // =================================================================

        private static StringBuilder ConvertPositive(long number, bool useEn)
        {
            if (number <= 20)
            {
                var units = useEn ? s_unitsEn : s_unitsEtt;
                return new StringBuilder(units[number]);
            }

            if (number < 100)
                return ConvertTens(number, useEn);

            if (number < 1_000)
                return ConvertHundreds(number, useEn);

            if (number < 1_000_000)
                return ConvertThousands(number, useEn);

            if (number < 1_000_000_000)
                return ConvertMillions(number, useEn);

            if (number < 1_000_000_000_000)
                return ConvertBillions(number, useEn);

            return ConvertTrillions(number, useEn);
        }

        /// <summary>21-99の合成数詞を生成する。スウェーデン語では一語（例: tjugoett）。</summary>
        private static StringBuilder ConvertTens(long number, bool useEn)
        {
            var ten = number / 10;
            var unit = number % 10;

            if (unit == 0)
                return new StringBuilder(s_tens[ten]);

            // スウェーデン語: 21-99は一語に合成する（tjugoett, trettiotre 等）
            var builder = new StringBuilder(s_tens[ten]);
            if (unit == 1)
            {
                // 通性: tjugoen, 中性: tjugoett
                builder.Append(useEn ? "en" : "ett");
            }
            else if (unit == 2)
            {
                builder.Append("två");
            }
            else
            {
                builder.Append(s_unitsEtt[(int)unit]);
            }

            return builder;
        }

        private static StringBuilder ConvertHundreds(long number, bool useEn)
        {
            var hundreds = number / 100;
            var rest = number % 100;

            var builder = new StringBuilder();
            if (hundreds == 1)
            {
                builder.Append("etthundra");
            }
            else
            {
                builder.Append(s_unitsEtt[hundreds]);
                builder.Append("hundra");
            }

            if (rest == 0)
                return builder;

            // スウェーデン語: 百の位と下位はスペースなしで直結
            // ただし読みやすさのため、一般的にはスペースを入れる場合もある
            // ここではスペースなし（etthundraett = 101）を採用
            builder.Append(ConvertPositive(rest, useEn));
            return builder;
        }

        private static StringBuilder ConvertThousands(long number, bool useEn)
        {
            var thousands = number / 1000;
            var rest = number % 1000;

            var builder = new StringBuilder();
            if (thousands == 1)
            {
                builder.Append("ettusen");
            }
            else
            {
                builder.Append(ConvertPositive(thousands, useEn: false));
                builder.Append("tusen");
            }

            if (rest == 0)
                return builder;

            // 千の位と下位の接続
            builder.Append(ConvertPositive(rest, useEn));
            return builder;
        }

        private static StringBuilder ConvertMillions(long number, bool useEn)
        {
            var millions = number / 1_000_000;
            var rest = number % 1_000_000;

            var builder = new StringBuilder();
            if (millions == 1)
            {
                // miljon は通性名詞 → "en miljon"
                builder.Append("en miljon");
            }
            else
            {
                builder.Append(ConvertPositive(millions, useEn: false));
                builder.Append(" miljoner");
            }

            if (rest == 0)
                return builder;

            builder.Append(' ');
            builder.Append(ConvertPositive(rest, useEn));
            return builder;
        }

        private static StringBuilder ConvertBillions(long number, bool useEn)
        {
            var billions = number / 1_000_000_000;
            var rest = number % 1_000_000_000;

            var builder = new StringBuilder();
            if (billions == 1)
            {
                // miljard は通性名詞 → "en miljard"
                builder.Append("en miljard");
            }
            else
            {
                builder.Append(ConvertPositive(billions, useEn: false));
                builder.Append(" miljarder");
            }

            if (rest == 0)
                return builder;

            builder.Append(' ');
            builder.Append(ConvertPositive(rest, useEn));
            return builder;
        }

        private static StringBuilder ConvertTrillions(long number, bool useEn)
        {
            var trillions = number / 1_000_000_000_000;
            var rest = number % 1_000_000_000_000;

            var builder = new StringBuilder();
            if (trillions == 1)
            {
                // biljon は通性名詞 → "en biljon" (long scale: 10^12)
                builder.Append("en biljon");
            }
            else
            {
                builder.Append(ConvertPositive(trillions, useEn: false));
                builder.Append(" biljoner");
            }

            if (rest == 0)
                return builder;

            builder.Append(' ');
            builder.Append(ConvertPositive(rest, useEn));
            return builder;
        }
    }
}
