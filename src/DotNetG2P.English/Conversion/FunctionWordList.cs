using System;
using System.Collections.Generic;

namespace DotNetG2P.English.Conversion
{
    /// <summary>
    /// 英語の機能語リスト（ストレス除去対象）。
    /// piper-plus の english.py の _FUNCTION_WORDS に準拠。
    /// 機能語は連続発話中で韻律的に弱化し、ストレスマーカーが付与されない。
    /// </summary>
    internal static class FunctionWordList
    {
        private static readonly HashSet<string> s_words = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 冠詞・限定詞
            "a", "an", "the",

            // 代名詞
            "i", "me", "my", "mine", "myself",
            "you", "your", "yours", "yourself",
            "he", "him", "his", "himself",
            "she", "her", "hers", "herself",
            "it", "its", "itself",
            "we", "us", "our", "ours", "ourselves",
            "they", "them", "their", "theirs", "themselves",

            // be 動詞
            "am", "is", "are", "was", "were",
            "be", "been", "being",

            // 助動詞
            "have", "has", "had", "having",
            "do", "does", "did",
            "will", "would", "shall", "should",
            "can", "could", "may", "might", "must",

            // 前置詞
            "at", "by", "for", "from", "in", "of", "on", "to", "with",
            "about", "after", "before", "between", "into", "through", "under",

            // 接続詞
            "and", "but", "or", "nor", "so", "yet",
            "if", "that", "than", "when", "while", "as", "because", "since",

            // その他
            "not", "no",
        };

        /// <summary>
        /// 指定された語が機能語かどうかを判定する。
        /// </summary>
        /// <param name="word">判定対象の語（大文字小文字不問）。</param>
        /// <returns>機能語であれば <c>true</c>。</returns>
        public static bool Contains(string word) => s_words.Contains(word);
    }
}
