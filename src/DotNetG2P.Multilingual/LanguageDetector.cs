using System.Runtime.CompilerServices;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// Unicode文字種ベースの言語判定。
    /// 各文字をScriptKind（Japanese/English/Digit/Punctuation/Whitespace/Other）に分類する。
    /// </summary>
    internal static class LanguageDetector
    {
        /// <summary>1文字のスクリプト種別を判定する。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptKind Classify(char c)
        {
            // 1. ひらがな U+3040-309F
            if (c >= '\u3040' && c <= '\u309F') return ScriptKind.Japanese;

            // 2. カタカナ U+30A0-30FF
            if (c >= '\u30A0' && c <= '\u30FF') return ScriptKind.Japanese;

            // 3. CJK統合漢字 U+4E00-9FFF（日中共用、文脈で判定）
            if (c >= '\u4E00' && c <= '\u9FFF') return ScriptKind.CJKIdeograph;

            // 4. CJK拡張A U+3400-4DBF（日中共用、文脈で判定）
            if (c >= '\u3400' && c <= '\u4DBF') return ScriptKind.CJKIdeograph;

            // 5. 半角カナ U+FF65-FF9F
            if (c >= '\uFF65' && c <= '\uFF9F') return ScriptKind.Japanese;

            // 6. CJK記号・句読点 U+3000-303F（U+3000 イデオグラフィックスペースはWhitespace扱い）
            if (c == '\u3000') return ScriptKind.Whitespace;
            if (c >= '\u3001' && c <= '\u303F') return ScriptKind.Japanese;

            // 7. ASCII英字 A-Z, a-z
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) return ScriptKind.English;

            // 8. ラテン拡張文字 U+00C0-U+024F (Latin Extended-A/B)
            if (c >= '\u00C0' && c <= '\u024F') return ScriptKind.Latin;

            // 9. ASCII数字 0-9
            if (c >= '0' && c <= '9') return ScriptKind.Digit;

            // 10. 空白・タブ・改行
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r') return ScriptKind.Whitespace;

            // 11. その他ASCII記号 (0x21-0x7E)
            if (c >= '\u0021' && c <= '\u007E') return ScriptKind.Punctuation;

            // 12. 全角数字 U+FF10-FF19
            if (c >= '\uFF10' && c <= '\uFF19') return ScriptKind.Digit;

            // 13. 全角英字 U+FF21-FF3A (A-Z), U+FF41-FF5A (a-z)
            if ((c >= '\uFF21' && c <= '\uFF3A') || (c >= '\uFF41' && c <= '\uFF5A'))
                return ScriptKind.English;

            // 14. 全角記号 U+FF01-FF0F, U+FF1A-FF20, U+FF3B-FF40, U+FF5B-FF5E
            if (c >= '\uFF01' && c <= '\uFF5E') return ScriptKind.Punctuation;

            // 15. 上記以外
            return ScriptKind.Other;
        }

        /// <summary>サロゲートペアかどうかを判定し、サロゲートペアならOtherを返す。BMP文字ならClassify(char)に委譲する。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptKind Classify(string text, int index, out int charCount)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                // サロゲートペア（絵文字、CJK拡張B以降等）はOther扱い
                charCount = 2;
                return ScriptKind.Other;
            }
            charCount = 1;
            return Classify(text[index]);
        }

        /// <summary>ScriptKindからLanguageへの変換。Digit/Punctuation/Whitespace/Otherはnullを返す。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Language? ToLanguage(ScriptKind kind)
        {
            return ToLanguage(kind, Language.English);
        }

        /// <summary>ScriptKindからLanguageへの変換。Latin系は既定言語に従う。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Language? ToLanguage(ScriptKind kind, Language defaultLatinLanguage)
        {
            switch (kind)
            {
                case ScriptKind.Japanese:
                    return Language.Japanese;
                case ScriptKind.English:
                case ScriptKind.Latin:
                    return defaultLatinLanguage == Language.Spanish
                        ? Language.Spanish
                        : Language.English;
                case ScriptKind.CJKIdeograph:
                    // CJK漢字は日中共用のため、文脈で判定（TextSegmenterに委譲）
                    return null;
                default:
                    return null;
            }
        }
    }
}
