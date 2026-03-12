using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotNetG2P.Korean.Normalization
{
    internal static class KoreanNormalizer
    {
        public static string Normalize(string text, bool enableUnicodeNormalization = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = enableUnicodeNormalization
                ? text.Normalize(NormalizationForm.FormKC)
                : text;

            var builder = new StringBuilder(normalized.Length);
            var prevWasSpace = true;

            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                if (char.IsWhiteSpace(c))
                {
                    AppendSpace(builder, ref prevWasSpace);
                    continue;
                }

                var category = char.GetUnicodeCategory(c);
                if (IsBoundaryCategory(category) || IsBoundaryCharacter(c))
                {
                    AppendSpace(builder, ref prevWasSpace);
                    continue;
                }

                if (category == UnicodeCategory.Control
                    || category == UnicodeCategory.Format
                    || category == UnicodeCategory.Surrogate)
                {
                    continue;
                }

                builder.Append(c);
                prevWasSpace = false;
            }

            return builder.ToString().Trim();
        }

        public static IReadOnlyList<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsBoundaryCategory(UnicodeCategory category)
        {
            switch (category)
            {
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.OtherSymbol:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsBoundaryCharacter(char c)
        {
            switch (c)
            {
                case '\u00B7':
                case '\u30FB':
                case '\u318D':
                case '/':
                case '\\':
                    return true;

                default:
                    return false;
            }
        }

        private static void AppendSpace(StringBuilder builder, ref bool prevWasSpace)
        {
            if (prevWasSpace)
                return;

            builder.Append(' ');
            prevWasSpace = true;
        }
    }
}
