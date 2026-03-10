using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetG2P.Portuguese.Normalization
{
    /// <summary>
    /// ポルトガル語入力のテキスト正規化（P2拡張版）。
    /// 13段階パイプラインで数字・記号・略語等を読み上げ形式に展開する。
    /// </summary>
    internal static class PortugueseNormalizer
    {
        private static readonly string[] s_monthNames =
        {
            "",
            "janeiro",
            "fevereiro",
            "mar\u00e7o",
            "abril",
            "maio",
            "junho",
            "julho",
            "agosto",
            "setembro",
            "outubro",
            "novembro",
            "dezembro",
        };

        /// <summary>
        /// テキストを正規化し、数字・記号・略語等を読み上げ形式に展開する。
        /// </summary>
        public static string Normalize(string text, PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. NFKC正規化 + 小文字化
            var normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

            // 2. 略語展開
            normalized = ExpandAbbreviations(normalized);

            // 3. ISO日付展開 (YYYY-MM-DD)
            normalized = ExpandIsoDates(normalized, dialect);

            // 4. 日付展開 (DD/MM/YYYY)
            normalized = ExpandDates(normalized, dialect);

            // 5. 時刻展開
            normalized = ExpandTimes(normalized, dialect);

            // 6. 通貨展開
            normalized = ExpandCurrencies(normalized, dialect);

            // 7. パーセント展開
            normalized = ExpandPercentages(normalized, dialect);

            // 8. 単位展開
            normalized = ExpandUnits(normalized, dialect);

            // 9. 数値範囲展開
            normalized = ExpandNumericRanges(normalized, dialect);

            // 10. 小数展開
            normalized = ExpandDecimals(normalized, dialect);

            // 11. 独立数値展開
            normalized = ExpandNumbers(normalized, dialect);

            // 12. 記号展開
            normalized = ExpandSymbols(normalized);

            // 13. 空白正規化 + trim
            return NormalizeWhitespace(normalized);
        }

        /// <summary>
        /// テキストを正規化してからトークン列に分割する。
        /// </summary>
        public static string[] Tokenize(string text, PortugueseDialect dialect = PortugueseDialect.Brazilian)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            return TokenizeNormalized(Normalize(text, dialect));
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

                // アポストロフ（ポルトガル語のエリジオン: d'água 等）はトークン内に保持
                if (ch == '\'' || ch == '\u2019')
                {
                    builder.Append('\'');
                    continue;
                }

                // ハイフンはトークン内に保持（接語: fale-me, diga-lhe 等）
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
            text = Regex.Replace(text, @"\bsr\.\s", "senhor ");
            text = Regex.Replace(text, @"\bsra\.", "senhora");
            text = Regex.Replace(text, @"\bsrta\.", "senhorita");
            text = Regex.Replace(text, @"\bdr\.", "doutor");
            text = Regex.Replace(text, @"\bdra\.", "doutora");
            text = Regex.Replace(text, @"\bprof\.", "professor");
            text = Regex.Replace(text, @"\bprofa\.", "professora");
            text = Regex.Replace(text, @"\bav\.", "avenida");
            text = Regex.Replace(text, @"\br\.", "rua");
            text = Regex.Replace(text, @"\betc\.", "et c\u00e9tera");
            text = Regex.Replace(text, @"\bp\.\s*ex\.", "por exemplo");
            text = Regex.Replace(text, @"\bn\s*[.]*\s*[º°o]\b", "n\u00famero");
            text = Regex.Replace(text, @"\beng\.", "engenheiro");
            text = Regex.Replace(text, @"\barq\.", "arquiteto");
            text = Regex.Replace(text, @"\bpg\.", "p\u00e1gina");
            text = Regex.Replace(text, @"\bpgs\.", "p\u00e1ginas");
            text = Regex.Replace(text, @"\btels?\.", m => m.Value.StartsWith("tels") ? "telefones" : "telefone");
            text = Regex.Replace(text, @"\ba\.c\.", "antes de cristo");
            text = Regex.Replace(text, @"\bd\.c\.", "depois de cristo");
            return text;
        }

        private static string ExpandIsoDates(string text, PortugueseDialect dialect)
        {
            // YYYY-MM-DD
            return Regex.Replace(text, @"\b(\d{4})-(\d{1,2})-(\d{1,2})\b", m =>
                ExpandDateParts(m.Groups[3].Value, m.Groups[2].Value, m.Groups[1].Value, m.Value, dialect));
        }

        private static string ExpandDates(string text, PortugueseDialect dialect)
        {
            // DD/MM/YYYY or DD-MM-YYYY or DD.MM.YYYY
            text = Regex.Replace(text, @"\b(\d{1,2})/(\d{1,2})/(\d{2,4})\b", m =>
                ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value, dialect));
            text = Regex.Replace(text, @"\b(\d{1,2})\.(\d{1,2})\.(\d{2,4})\b", m =>
                ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value, dialect));
            return text;
        }

        private static string ExpandDateParts(string dayText, string monthText, string yearText, string fallback, PortugueseDialect dialect)
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

            // ポルトガル語: 日は基数詞 + "de" + 月名 + "de" + 年
            // 1日→"primeiro" (序数)、それ以外は基数詞
            var spokenDay = day == 1 ? "primeiro" : NumberToWords.Convert(day, dialect);
            return spokenDay + " de " + s_monthNames[month] + " de " + NumberToWords.Convert(year, dialect);
        }

        private static string ExpandTimes(string text, PortugueseDialect dialect)
        {
            // NNhNN format (ポルトガル語標準)
            text = Regex.Replace(text, @"\b(\d{1,2})h(?:\s*(\d{1,2}))?\b", m =>
            {
                if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
                    return m.Value;

                if (hours > 23)
                    return m.Value;

                if (!m.Groups[2].Success || string.IsNullOrEmpty(m.Groups[2].Value))
                {
                    if (hours == 0) return "meia-noite";
                    if (hours == 12) return "meio-dia";
                    return NumberToWords.Convert(hours, dialect) + (hours == 1 ? " hora" : " horas");
                }

                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
                    return m.Value;

                if (minutes > 59)
                    return m.Value;

                string hourPart;
                if (hours == 0) hourPart = "meia-noite";
                else if (hours == 12) hourPart = "meio-dia";
                else hourPart = NumberToWords.Convert(hours, dialect) + (hours == 1 ? " hora" : " horas");

                if (minutes == 0)
                    return hourPart;

                return hourPart + " e " + NumberToWords.Convert(minutes, dialect) + (minutes == 1 ? " minuto" : " minutos");
            });

            // NN:NN format
            text = Regex.Replace(text, @"\b(\d{1,2}):(\d{2})\b", m =>
            {
                if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
                    || !int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
                    return m.Value;

                if (hours > 23 || minutes > 59)
                    return m.Value;

                string hourPart;
                if (hours == 0) hourPart = "meia-noite";
                else if (hours == 12) hourPart = "meio-dia";
                else hourPart = NumberToWords.Convert(hours, dialect) + (hours == 1 ? " hora" : " horas");

                if (minutes == 0)
                    return hourPart;

                return hourPart + " e " + NumberToWords.Convert(minutes, dialect) + (minutes == 1 ? " minuto" : " minutos");
            });

            return text;
        }

        private static string ExpandCurrencies(string text, PortugueseDialect dialect)
        {
            // R$ (レアル: ブラジル通貨)
            text = Regex.Replace(text, @"r\$\s*(\d+)(?:[,.](\d{1,2}))?", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "real", "reais", "centavo", "centavos", dialect));

            // € (ユーロ: 後置)
            text = Regex.Replace(text, @"(\d+)(?:[,.](\d{1,2}))?\s*\u20ac", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "euro", "euros", "c\u00eantimo", "c\u00eantimos", dialect));

            // € (ユーロ: 前置)
            text = Regex.Replace(text, @"\u20ac\s*(\d+)(?:[,.](\d{1,2}))?", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "euro", "euros", "c\u00eantimo", "c\u00eantimos", dialect));

            // $ (ドル: 前置)
            text = Regex.Replace(text, @"\$\s*(\d+)(?:[,.](\d{1,2}))?", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "d\u00f3lar", "d\u00f3lares", "centavo", "centavos", dialect));

            // $ (ドル: 後置)
            text = Regex.Replace(text, @"(\d+)(?:[,.](\d{1,2}))?\s*\$", m =>
                ExpandCurrencyValue(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : null, "d\u00f3lar", "d\u00f3lares", "centavo", "centavos", dialect));

            return text;
        }

        private static string ExpandCurrencyValue(string wholeText, string? fractionText,
            string singular, string plural, string minorSingular, string minorPlural, PortugueseDialect dialect)
        {
            if (!long.TryParse(wholeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
                return wholeText;

            var builder = new StringBuilder();
            builder.Append(NumberToWords.Convert(whole, dialect));
            builder.Append(' ');
            builder.Append(whole == 1 ? singular : plural);

            if (!string.IsNullOrEmpty(fractionText))
            {
                var normalized = fractionText;
                if (normalized.Length == 1)
                    normalized += "0";
                else if (normalized.Length > 2)
                    normalized = normalized.Substring(0, 2);

                if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cents) && cents > 0)
                {
                    builder.Append(" e ");
                    builder.Append(NumberToWords.Convert(cents, dialect));
                    builder.Append(' ');
                    builder.Append(cents == 1 ? minorSingular : minorPlural);
                }
            }

            return builder.ToString();
        }

        private static string ExpandPercentages(string text, PortugueseDialect dialect)
        {
            return Regex.Replace(text, @"(\d+(?:[,.]\d+)?)\s*%", m =>
            {
                var numText = m.Groups[1].Value;
                var commaIdx = numText.IndexOf(',');
                var dotIdx = numText.IndexOf('.');
                if (commaIdx >= 0 || dotIdx >= 0)
                {
                    var sep = commaIdx >= 0 ? commaIdx : dotIdx;
                    var wholePart = numText.Substring(0, sep);
                    var fracPart = numText.Substring(sep + 1);
                    return NumberToWords.Convert(wholePart, dialect) + " v\u00edrgula " + NumberToWords.ConvertDigits(fracPart, dialect) + " por cento";
                }
                return NumberToWords.Convert(numText, dialect) + " por cento";
            });
        }

        private static string ExpandUnits(string text, PortugueseDialect dialect)
        {
            // 温度
            text = Regex.Replace(text, @"(\d+)\s*\u00b0c\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " grau celsius" : " graus celsius"));
            text = Regex.Replace(text, @"(\d+)\s*\u00b0f\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " grau fahrenheit" : " graus fahrenheit"));

            // 複合単位を先に
            text = Regex.Replace(text, @"(\d+)\s*km/h\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + " quil\u00f4metros por hora");
            text = Regex.Replace(text, @"(\d+)\s*km\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " quil\u00f4metro" : " quil\u00f4metros"));
            text = Regex.Replace(text, @"(\d+)\s*kg\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " quilograma" : " quilogramas"));
            text = Regex.Replace(text, @"(\d+)\s*cm\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " cent\u00edmetro" : " cent\u00edmetros"));
            text = Regex.Replace(text, @"(\d+)\s*mm\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " mil\u00edmetro" : " mil\u00edmetros"));
            text = Regex.Replace(text, @"(\d+)\s*mg\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " miligrama" : " miligramas"));
            text = Regex.Replace(text, @"(\d+)\s*ml\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " mililitro" : " mililitros"));

            // 単独単位（m, g, l）
            text = Regex.Replace(text, @"(\d+)\s*m\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " metro" : " metros"));
            text = Regex.Replace(text, @"(\d+)\s*g\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " grama" : " gramas"));
            text = Regex.Replace(text, @"(\d+)\s*l\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + (m.Groups[1].Value == "1" ? " litro" : " litros"));

            return text;
        }

        private static string ExpandNumericRanges(string text, PortugueseDialect dialect)
        {
            return Regex.Replace(text, @"\b(\d+)\s*[-\u2013]\s*(\d+)\b", m =>
            {
                var left = NumberToWords.Convert(m.Groups[1].Value, dialect);
                var right = NumberToWords.Convert(m.Groups[2].Value, dialect);
                return left + " a " + right;
            });
        }

        private static string ExpandDecimals(string text, PortugueseDialect dialect)
        {
            // N,N → N vírgula N (ポルトガル語では","が小数点)
            return Regex.Replace(text, @"\b(\d+),(\d+)\b", m =>
                NumberToWords.Convert(m.Groups[1].Value, dialect) + " v\u00edrgula " + NumberToWords.ConvertDigits(m.Groups[2].Value, dialect));
        }

        private static string ExpandNumbers(string text, PortugueseDialect dialect)
        {
            return Regex.Replace(text, @"\b\d+\b", m => NumberToWords.Convert(m.Value, dialect));
        }

        private static string ExpandSymbols(string text)
        {
            return text
                .Replace("&", " e ")
                .Replace("@", " arroba ")
                .Replace("+", " mais ")
                .Replace("=", " igual ")
                .Replace("#", " cardinal ")
                .Replace("\u00a7", " par\u00e1grafo ")
                .Replace("\u2116", " n\u00famero ");
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
