using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotNetG2P.French.Normalization
{
    /// <summary>
    /// フランス語入力の軽量正規化（F1基本バージョン）。
    /// </summary>
    internal static class FrenchNormalizer
    {
        /// <summary>
        /// テキストをNFC正規化し、小文字化する。
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // NFC正規化 + 小文字化
            var normalized = text.Normalize(NormalizationForm.FormC);
            return normalized.ToLowerInvariant();
        }

        /// <summary>
        /// テキストを空白で分割しトークン列を返す。
        /// アポストロフはトークン内に保持する（例: l'homme → "l'homme"）。
        /// 句読点・記号は除去する。
        /// </summary>
        public static string[] Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            var normalized = Normalize(text);
            var tokens = new List<string>();
            var builder = new StringBuilder();

            for (var i = 0; i < normalized.Length; i++)
            {
                var ch = normalized[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    // 空白: 現在のトークンを確定
                    if (builder.Length > 0)
                    {
                        tokens.Add(builder.ToString());
                        builder.Clear();
                    }
                    continue;
                }

                // アポストロフ（フランス語のエリジオン）はトークン内に保持
                if (ch == '\'' || ch == '\u2019') // ASCII apostrophe または RIGHT SINGLE QUOTATION MARK
                {
                    builder.Append('\'');
                    continue;
                }

                // ハイフンはトークン内に保持（複合語: peut-être 等）
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

            // 最後のトークンを確定
            if (builder.Length > 0)
                tokens.Add(builder.ToString());

            return tokens.ToArray();
        }
    }
}
