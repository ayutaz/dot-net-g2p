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

            // 3. CJK統合漢字 U+4E00-9FFF
            if (c >= '\u4E00' && c <= '\u9FFF') return ScriptKind.Japanese;

            // 4. CJK拡張A U+3400-4DBF
            if (c >= '\u3400' && c <= '\u4DBF') return ScriptKind.Japanese;

            // 5. 半角カナ U+FF65-FF9F
            if (c >= '\uFF65' && c <= '\uFF9F') return ScriptKind.Japanese;

            // 6. CJK記号・句読点 U+3000-303F
            if (c >= '\u3000' && c <= '\u303F') return ScriptKind.Japanese;

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

            // 13. 全角英数字 U+FF01-FF5E (全角数字を除く)
            if (c >= '\uFF01' && c <= '\uFF5E') return ScriptKind.English;

            // 14. 上記以外
            return ScriptKind.Other;
        }

        /// <summary>ScriptKindからLanguageへの変換。Digit/Punctuation/Whitespace/Otherはnullを返す。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Language? ToLanguage(ScriptKind kind)
        {
            switch (kind)
            {
                case ScriptKind.Japanese:
                    return Language.Japanese;
                case ScriptKind.English:
                case ScriptKind.Latin:
                    return Language.English;
                default:
                    return null;
            }
        }
    }
}
