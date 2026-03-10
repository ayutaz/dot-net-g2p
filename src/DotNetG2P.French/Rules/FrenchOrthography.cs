namespace DotNetG2P.French.Rules
{
    /// <summary>
    /// フランス語の正書法ユーティリティ。母音/子音判定、トレマ判定、アクセント記号除去等を提供する。
    /// </summary>
    internal static class FrenchOrthography
    {
        /// <summary>
        /// 指定された文字がフランス語の母音字（アクセント付きを含む）であるかどうかを判定する。
        /// </summary>
        public static bool IsVowelChar(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'a': case 'e': case 'i': case 'o': case 'u': case 'y':
                case '\u00E0': // à
                case '\u00E2': // â
                case '\u00E8': // è
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00EB': // ë
                case '\u00EE': // î
                case '\u00EF': // ï
                case '\u00F4': // ô
                case '\u00F9': // ù
                case '\u00FB': // û
                case '\u00FC': // ü
                case '\u00E6': // æ
                case '\u0153': // œ
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字が前舌母音字（c/g の軟音化をトリガーする文字）であるかどうかを判定する。
        /// </summary>
        public static bool IsFrontVowelChar(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'e': case 'i': case 'y':
                case '\u00E8': // è
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00EB': // ë
                case '\u00EE': // î
                case '\u00EF': // ï
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字がフランス語の子音字であるかどうかを判定する。
        /// </summary>
        public static bool IsConsonantChar(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'b': case 'c': case 'd': case 'f': case 'g':
                case 'h': case 'j': case 'k': case 'l': case 'm':
                case 'n': case 'p': case 'q': case 'r': case 's':
                case 't': case 'v': case 'w': case 'x': case 'z':
                case '\u00E7': // ç
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 指定された文字がトレマ（分音記号）付きであるかどうかを判定する。
        /// トレマはダイグラフ認識を抑制する。
        /// </summary>
        public static bool HasTrema(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == '\u00EB' || c == '\u00EF' || c == '\u00FC'; // ë, ï, ü
        }

        /// <summary>
        /// アクセント記号やセディーユを除去し、基底文字を返す。
        /// </summary>
        public static char StripAccent(char c)
        {
            var lower = char.ToLowerInvariant(c);
            char result;
            switch (lower)
            {
                case '\u00E0': // à
                case '\u00E2': // â
                    result = 'a';
                    break;
                case '\u00E8': // è
                case '\u00E9': // é
                case '\u00EA': // ê
                case '\u00EB': // ë
                    result = 'e';
                    break;
                case '\u00EE': // î
                case '\u00EF': // ï
                    result = 'i';
                    break;
                case '\u00F4': // ô
                    result = 'o';
                    break;
                case '\u00F9': // ù
                case '\u00FB': // û
                case '\u00FC': // ü
                    result = 'u';
                    break;
                case '\u00E7': // ç
                    result = 'c';
                    break;
                default:
                    return c;
            }

            return char.IsUpper(c) ? char.ToUpperInvariant(result) : result;
        }
    }
}
