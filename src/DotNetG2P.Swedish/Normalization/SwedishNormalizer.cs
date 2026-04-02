using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetG2P.Swedish.Normalization
{
    /// <summary>
    /// スウェーデン語テキスト正規化パイプライン（11段階）。
    /// 数字・略語・日付・通貨・記号等を読み上げ形式に展開する。
    /// </summary>
    internal static class SwedishNormalizer
    {
        // 月名テーブル
        private static readonly string[] s_monthNames =
        {
            "",
            "januari",
            "februari",
            "mars",
            "april",
            "maj",
            "juni",
            "juli",
            "augusti",
            "september",
            "oktober",
            "november",
            "december",
        };

        // 略語テーブル（パイプラインの早い段階で展開）
        // 注: ピリオド付き略語は正規表現でマッチする
        // 注: 長い略語を先にマッチさせるため、複合パターンを先に配置

        // コンパイル済み正規表現（パフォーマンス最適化）
        // 略語パターン: 最低1つのピリオドを要求し、通常単語との誤マッチを防止
        private static readonly Regex s_abbreviationTex = new Regex(@"\bt\.\s*ex\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationDvs = new Regex(@"\b(?:d\.\s*v\.\s*s\.?|dvs\.)", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationBla = new Regex(@"\bbl\.\s*a\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationKl = new Regex(@"\bkl\.", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationCa = new Regex(@"\bca\.", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationOsv = new Regex(@"\bosv\.", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationMm = new Regex(@"\bm\.\s*m\.?(?=\s|$)", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationSk = new Regex(@"\bs\.\s*k\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationFn = new Regex(@"\bf\.\s*n\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationTom = new Regex(@"\bt\.\s*o\.\s*m\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationFo = new Regex(@"\bf\.\s*ö\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationOd = new Regex(@"\bo\.\s*d\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationMfl = new Regex(@"\bm\.\s*fl\.?", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationNr = new Regex(@"\bnr\.", RegexOptions.Compiled);
        private static readonly Regex s_abbreviationSt = new Regex(@"\bst\.", RegexOptions.Compiled);

        // 序数パターン: N:a, N:e（スウェーデン語の序数接尾辞）
        private static readonly Regex s_ordinalPattern = new Regex(@"\b(\d+):([ae])\b", RegexOptions.Compiled);

        // 日付パターン: YYYY-MM-DD（ISO形式）
        private static readonly Regex s_dateIsoPattern = new Regex(
            @"\b(\d{4})-(\d{1,2})-(\d{1,2})\b", RegexOptions.Compiled);

        // 日付パターン: DD/MM/YYYY
        private static readonly Regex s_dateDmyPattern = new Regex(
            @"\b(\d{1,2})/(\d{1,2})/(\d{2,4})\b", RegexOptions.Compiled);

        // 時刻パターン: HH:MM（ただし通貨の NN:NN kr と区別するため後読みで kr がない場合のみ）
        private static readonly Regex s_timePattern = new Regex(
            @"\b(\d{1,2}):(\d{2})\b(?!\s*kr)", RegexOptions.Compiled);

        // 通貨パターン: N,NN kr（コンマ区切り、小数パターンより先にマッチさせる）
        private static readonly Regex s_currencyKronaCommaPattern = new Regex(
            @"(\d+),(\d{2})\s*kr\b", RegexOptions.Compiled);

        // 通貨パターン: N:NN kr / N kr / N:- kr（:- も含めて消費し、残留を防止）
        private static readonly Regex s_currencyKronaPattern = new Regex(
            @"(\d+)(?::-|:(\d{2}))?\s*kr\b", RegexOptions.Compiled);
        private static readonly Regex s_currencyKronaExactPattern = new Regex(
            @"(\d+):-", RegexOptions.Compiled);

        // 通貨パターン: €N, $N, £N（前置記号）
        private static readonly Regex s_currencyEuroPattern = new Regex(
            @"€\s*(\d+)(?:[,.](\d{1,2}))?", RegexOptions.Compiled);
        private static readonly Regex s_currencyDollarPattern = new Regex(
            @"\$\s*(\d+)(?:[,.](\d{1,2}))?", RegexOptions.Compiled);
        private static readonly Regex s_currencyPoundPattern = new Regex(
            @"£\s*(\d+)(?:[,.](\d{1,2}))?", RegexOptions.Compiled);

        // パーセントパターン
        private static readonly Regex s_percentPattern = new Regex(
            @"(\d+(?:[,.]\d+)?)\s*%", RegexOptions.Compiled);

        // 小数パターン（コンマ区切り）
        private static readonly Regex s_decimalPattern = new Regex(
            @"\b(\d+),(\d+)\b", RegexOptions.Compiled);

        // 数字パターン（単独の数字列）
        private static readonly Regex s_numberPattern = new Regex(
            @"\b\d+\b", RegexOptions.Compiled);

        /// <summary>
        /// テキストを正規化し、数字・記号・略語等を読み上げ形式に展開する。
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. NFC正規化 + 小文字化
            var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // 2. 略語展開
            normalized = ExpandAbbreviations(normalized);

            // 3. 序数展開（1:a→första 等）
            normalized = ExpandOrdinals(normalized);

            // 4. 日付展開（ISO/DMY形式）
            normalized = ExpandDates(normalized);

            // 5. 時刻展開（15:30→femton trettio）
            normalized = ExpandTimes(normalized);

            // 6. 通貨展開（5 kr→fem kronor 等）— 数字展開より先に処理
            normalized = ExpandCurrencies(normalized);

            // 7. パーセント展開（50%→femtio procent）
            normalized = ExpandPercentages(normalized);

            // 8. 小数展開（3,14→tre komma fjorton）
            normalized = ExpandDecimals(normalized);

            // 9. 数字展開（42→fyrtiotvå）
            normalized = ExpandNumbers(normalized);

            // 10. 記号展開（@→snabel-a 等）
            normalized = ExpandSymbols(normalized);

            // 11. 空白正規化 + trim
            return NormalizeWhitespace(normalized);
        }

        /// <summary>
        /// テキストを正規化してトークン列に分割する。
        /// 内部で Normalize() を呼ぶため、呼び出し側で二重正規化しないこと。
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

                // ハイフンはトークン内に保持（複合語: snabel-a 等）
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

        // =================================================================
        // パイプライン各段階の実装
        // =================================================================

        /// <summary>スウェーデン語略語を展開する。</summary>
        private static string ExpandAbbreviations(string text)
        {
            // 長い略語を先にマッチさせる（t.o.m. は t.ex. より先に処理）
            text = s_abbreviationTom.Replace(text, "till och med");
            text = s_abbreviationDvs.Replace(text, "det vill säga");
            text = s_abbreviationTex.Replace(text, "till exempel");
            text = s_abbreviationBla.Replace(text, "bland annat");
            text = s_abbreviationMfl.Replace(text, "med flera");
            text = s_abbreviationMm.Replace(text, "med mera");
            text = s_abbreviationSk.Replace(text, "så kallad");
            text = s_abbreviationFn.Replace(text, "för närvarande");
            text = s_abbreviationFo.Replace(text, "för övrigt");
            text = s_abbreviationOd.Replace(text, "och dylikt");
            text = s_abbreviationKl.Replace(text, "klockan");
            text = s_abbreviationCa.Replace(text, "cirka");
            text = s_abbreviationOsv.Replace(text, "och så vidare");
            text = s_abbreviationNr.Replace(text, "nummer");
            text = s_abbreviationSt.Replace(text, "stycken");
            return text;
        }

        /// <summary>序数接尾辞（1:a, 2:a, 3:e 等）を展開する。</summary>
        private static string ExpandOrdinals(string text)
        {
            return s_ordinalPattern.Replace(text, m =>
            {
                if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value) || value <= 0)
                {
                    return m.Value;
                }

                return NumberToWords.ToOrdinal(value);
            });
        }

        /// <summary>日付を展開する（ISO: YYYY-MM-DD, DMY: DD/MM/YYYY）。</summary>
        private static string ExpandDates(string text)
        {
            // ISO形式: YYYY-MM-DD → "DAG MÅNAD ÅR"
            text = s_dateIsoPattern.Replace(text, m =>
                ExpandDateParts(m.Groups[3].Value, m.Groups[2].Value, m.Groups[1].Value, m.Value));

            // DMY形式: DD/MM/YYYY
            text = s_dateDmyPattern.Replace(text, m =>
                ExpandDateParts(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Value));

            return text;
        }

        /// <summary>日付の各部分（日・月・年）をスウェーデン語に変換する。</summary>
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

            // 日は序数詞、月名、年は年号読み（1100-1999は百単位読み）
            var spokenDay = NumberToWords.ToOrdinal(day);
            var spokenYear = NumberToWords.ConvertYear(year);
            return spokenDay + " " + s_monthNames[month] + " " + spokenYear;
        }

        /// <summary>時刻を展開する（15:30→femton trettio）。</summary>
        private static string ExpandTimes(string text)
        {
            return s_timePattern.Replace(text, m =>
            {
                if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var hours))
                    return m.Value;

                if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var minutes))
                    return m.Value;

                if (hours > 23 || minutes > 59)
                    return m.Value;

                if (minutes == 0)
                    return NumberToWords.ToCardinal(hours);

                return NumberToWords.ToCardinal(hours) + " " + NumberToWords.ToCardinal(minutes);
            });
        }

        /// <summary>通貨表記を展開する。</summary>
        private static string ExpandCurrencies(string text)
        {
            // コンマ区切りクローナ: N,NN kr（小数パターンより先に処理）
            text = s_currencyKronaCommaPattern.Replace(text, m =>
            {
                if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var kronor))
                    return m.Value;

                var builder = new StringBuilder();
                builder.Append(NumberToWords.ToCardinal(kronor, useEn: true));
                builder.Append(kronor == 1 ? " krona" : " kronor");

                if (long.TryParse(m.Groups[2].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var ore) && ore > 0)
                {
                    builder.Append(" och ");
                    builder.Append(NumberToWords.ToCardinal(ore, useEn: false));
                    builder.Append(ore == 1 ? " öre" : " öre");
                }

                return builder.ToString();
            });

            // スウェーデン・クローナ: N:NN kr / N kr / N:- kr
            text = s_currencyKronaPattern.Replace(text, m =>
            {
                if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var kronor))
                    return m.Value;

                var builder = new StringBuilder();
                builder.Append(NumberToWords.ToCardinal(kronor, useEn: true));
                builder.Append(kronor == 1 ? " krona" : " kronor");

                if (m.Groups[2].Success && !string.IsNullOrEmpty(m.Groups[2].Value))
                {
                    if (long.TryParse(m.Groups[2].Value, NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var ore) && ore > 0)
                    {
                        builder.Append(" och ");
                        builder.Append(NumberToWords.ToCardinal(ore, useEn: false));
                        builder.Append(ore == 1 ? " öre" : " öre");
                    }
                }

                return builder.ToString();
            });

            // 正確な金額: N:- （100:- = hundra kronor）
            text = s_currencyKronaExactPattern.Replace(text, m =>
            {
                if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var kronor))
                    return m.Value;

                return NumberToWords.ToCardinal(kronor, useEn: true) +
                       (kronor == 1 ? " krona" : " kronor");
            });

            // ユーロ
            text = s_currencyEuroPattern.Replace(text, m =>
                ExpandForeignCurrency(m, "euro", "euro", "cent", "cent"));

            // ドル
            text = s_currencyDollarPattern.Replace(text, m =>
                ExpandForeignCurrency(m, "dollar", "dollar", "cent", "cent"));

            // ポンド
            text = s_currencyPoundPattern.Replace(text, m =>
                ExpandForeignCurrency(m, "pund", "pund", "pence", "pence"));

            return text;
        }

        /// <summary>外貨通貨を展開する共通ヘルパー。</summary>
        private static string ExpandForeignCurrency(Match m, string singular, string plural,
            string minorSingular, string minorPlural)
        {
            if (!long.TryParse(m.Groups[1].Value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var whole))
                return m.Value;

            var builder = new StringBuilder();
            builder.Append(NumberToWords.ToCardinal(whole, useEn: true));
            builder.Append(' ');
            builder.Append(whole == 1 ? singular : plural);

            if (m.Groups[2].Success && !string.IsNullOrEmpty(m.Groups[2].Value))
            {
                var normalized = m.Groups[2].Value;
                if (normalized.Length == 1)
                    normalized += "0";
                else if (normalized.Length > 2)
                    normalized = normalized.Substring(0, 2);

                if (long.TryParse(normalized, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var cents) && cents > 0)
                {
                    builder.Append(' ');
                    builder.Append(NumberToWords.ToCardinal(cents, useEn: false));
                    builder.Append(' ');
                    builder.Append(cents == 1 ? minorSingular : minorPlural);
                }
            }

            return builder.ToString();
        }

        /// <summary>パーセント表記を展開する。</summary>
        private static string ExpandPercentages(string text)
        {
            return s_percentPattern.Replace(text, m =>
            {
                var numText = m.Groups[1].Value;
                var commaIdx = numText.IndexOf(',');
                var dotIdx = numText.IndexOf('.');

                if (commaIdx >= 0 || dotIdx >= 0)
                {
                    // 小数を含む場合
                    var sep = commaIdx >= 0 ? commaIdx : dotIdx;
                    var wholePart = numText.Substring(0, sep);
                    var fracPart = numText.Substring(sep + 1);
                    return NumberToWords.ToCardinal(long.TryParse(wholePart, out var w) ? w : 0)
                           + " komma "
                           + NumberToWords.ConvertDigits(fracPart)
                           + " procent";
                }

                if (long.TryParse(numText, out var value))
                    return NumberToWords.ToCardinal(value) + " procent";

                return m.Value;
            });
        }

        /// <summary>小数表記を展開する（スウェーデン語ではコンマが小数点）。</summary>
        private static string ExpandDecimals(string text)
        {
            return s_decimalPattern.Replace(text, m =>
            {
                var wholePart = m.Groups[1].Value;
                var fracPart = m.Groups[2].Value;

                var builder = new StringBuilder();
                if (long.TryParse(wholePart, out var wholeValue))
                    builder.Append(NumberToWords.ToCardinal(wholeValue));
                else
                    builder.Append(wholePart);

                builder.Append(" komma ");

                // 小数部は1桁ずつ読む（先頭ゼロ保持: 3,05→tre komma noll fem）
                builder.Append(NumberToWords.ConvertDigits(fracPart));

                return builder.ToString();
            });
        }

        /// <summary>単独の数字列を基数詞に展開する。</summary>
        private static string ExpandNumbers(string text)
        {
            return s_numberPattern.Replace(text, m =>
            {
                if (long.TryParse(m.Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value))
                    return NumberToWords.ToCardinal(value);

                // long.MaxValue を超える数字列は1桁ずつ読み上げる
                return NumberToWords.ConvertDigits(m.Value);
            });
        }

        /// <summary>記号をスウェーデン語に展開する。</summary>
        private static string ExpandSymbols(string text)
        {
            return text
                .Replace("@", " snabel-a ")
                .Replace("&", " och ")
                .Replace("%", " procent ")
                .Replace("+", " plus ")
                .Replace("=", " lika med ")
                .Replace("€", " euro ")
                .Replace("$", " dollar ")
                .Replace("£", " pund ");
        }

        /// <summary>
        /// 空白を正規化する。
        /// 薄いスペース(U+2009)・ノーブレークスペース(U+00A0)をASCIIスペースに変換し、
        /// 連続する空白を1つに圧縮してtrimする。
        /// </summary>
        private static string NormalizeWhitespace(string text)
        {
            var builder = new StringBuilder(text.Length);
            var prevWasSpace = true;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                // 薄いスペース・ノーブレークスペース・通常の空白文字
                if (ch == '\u2009' || ch == '\u00A0' || char.IsWhiteSpace(ch))
                {
                    if (!prevWasSpace)
                    {
                        builder.Append(' ');
                        prevWasSpace = true;
                    }
                    continue;
                }

                // 文字・数字・ハイフン（複合語用）・アポストロフィ（O'Brien等）はそのまま保持
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '\'')
                {
                    builder.Append(ch);
                    prevWasSpace = false;
                    continue;
                }

                // その他の記号は空白区切りに変換
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
