using System;
using System.Collections.Generic;

namespace DotNetG2P.Spanish.Conversion
{
    /// <summary>
    /// スペイン語の機能語リスト（ストレス除去対象）。
    /// piper-plus の spanish.py の _UNSTRESSED_FUNCTION_WORDS に準拠。
    /// 連続発話中で韻律的に弱化し、Prosody の A2 でストレスマーカーが付与されない
    /// 単音節の機能語 27 語。
    /// </summary>
    internal static class FunctionWordList
    {
        private static readonly HashSet<string> s_words = new HashSet<string>(StringComparer.Ordinal)
        {
            // 冠詞
            "el", "la", "los", "las", "un", "una",

            // 前置詞・縮約形
            "de", "del", "al", "a", "en", "con", "por",

            // 接続詞
            "y", "o",

            // 関係詞・接続詞
            "que",

            // 代名詞（弱形）
            "se", "me", "te", "le", "lo", "nos",

            // 所有形容詞・人称形容詞
            "su", "mi", "tu",

            // その他
            "es", "no", "si",
        };

        /// <summary>
        /// 指定された語が機能語かどうかを判定する。
        /// 比較は大文字小文字を区別する（スペイン語はアクセント記号で区別するため）。
        /// </summary>
        /// <param name="word">判定対象の語。</param>
        /// <returns>機能語であれば <c>true</c>。</returns>
        public static bool Contains(string word) => s_words.Contains(word);
    }
}
