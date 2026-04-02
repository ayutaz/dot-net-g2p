using System;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語の正書法ユーティリティ。
    /// 軟母音/硬母音の判定、子音判定、二重子音検出などを提供する。
    /// </summary>
    internal static class SwedishOrthography
    {
        /// <summary>
        /// 軟母音（e, i, y, ä, ö）か判定する。k/g/sk の軟化トリガーとなる。
        /// </summary>
        internal static bool IsSoftVowel(char c)
        {
            return c == 'e' || c == 'i' || c == 'y' || c == '\u00e4' || c == '\u00f6'
                || c == 'E' || c == 'I' || c == 'Y' || c == '\u00c4' || c == '\u00d6';
        }

        /// <summary>
        /// 硬母音（a, o, u, å）か判定する。
        /// </summary>
        internal static bool IsHardVowel(char c)
        {
            return c == 'a' || c == 'o' || c == 'u' || c == '\u00e5'
                || c == 'A' || c == 'O' || c == 'U' || c == '\u00c5';
        }

        /// <summary>
        /// 全母音字（a, e, i, o, u, y, å, ä, ö + 大文字）か判定する。
        /// </summary>
        internal static bool IsVowelChar(char c)
        {
            return IsSoftVowel(c) || IsHardVowel(c);
        }

        /// <summary>
        /// 子音字か判定する。
        /// </summary>
        internal static bool IsConsonantChar(char c)
        {
            return c >= 'a' && c <= 'z' && !IsVowelChar(c);
        }

        /// <summary>
        /// アクセント記号付き文字（外来語の é, è, à 等）か判定する。
        /// </summary>
        internal static bool HasWrittenAccent(char c)
        {
            return c == '\u00e9' || c == '\u00e8' || c == '\u00e0'; // é, è, à
        }

        /// <summary>
        /// 指定位置の母音の後に二重子音（同一子音連続または子音クラスタ）が続くか判定する。
        /// 相補的数量法則による短母音判定に使用する。
        /// </summary>
        internal static bool IsFollowedByDoubleConsonant(string word, int vowelIndex)
        {
            if (vowelIndex + 1 >= word.Length) return false;

            // x は /ks/ の2子音相当
            if (word[vowelIndex + 1] == 'x') return true;

            if (vowelIndex + 2 >= word.Length) return false;
            var c1 = char.ToLowerInvariant(word[vowelIndex + 1]);
            var c2 = char.ToLowerInvariant(word[vowelIndex + 2]);

            if (!IsConsonantChar(c1)) return false;

            // ck は重子音 k の正書法
            if (c1 == 'c' && c2 == 'k') return true;

            // 同一子音の連続、または子音クラスタ
            return IsConsonantChar(c2);
        }
    }
}
