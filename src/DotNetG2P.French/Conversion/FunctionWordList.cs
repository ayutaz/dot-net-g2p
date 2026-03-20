using System;
using System.Collections.Generic;

namespace DotNetG2P.French.Conversion
{
    /// <summary>
    /// フランス語の機能語リスト（ストレス除去対象）。
    /// piper-plus には未実装のため、フランス語の一般的な機能語（冠詞・前置詞・
    /// 代名詞・接続詞・助動詞等）に基づいて構成。
    /// 連続発話中で韻律的に弱化し、Prosody の A2 でストレスマーカーが付与されない。
    /// </summary>
    /// <remarks>
    /// フランス語ではストレスは常に語末音節に置かれるが、機能語は
    /// 韻律句内で弱化しストレスを失う。このリストは piper-plus の
    /// スペイン語・英語の機能語リストと同等の粒度で構成している。
    /// </remarks>
    internal static class FunctionWordList
    {
        private static readonly HashSet<string> s_words = new HashSet<string>(StringComparer.Ordinal)
        {
            // 定冠詞
            "le", "la", "les",

            // 不定冠詞
            "un", "une", "des",

            // 縮約冠詞
            "du", "au", "aux",

            // 前置詞
            "de", "à", "en", "dans", "par", "pour", "sur", "avec",
            "sans", "sous", "vers", "chez",

            // 代名詞（弱形・接語）
            "je", "tu", "il", "elle", "on", "nous", "vous", "ils", "elles",
            "me", "te", "se", "lui", "leur",
            "y", "en",

            // 所有形容詞
            "mon", "ma", "mes", "ton", "ta", "tes",
            "son", "sa", "ses", "notre", "votre", "nos", "vos",

            // 指示形容詞
            "ce", "cet", "cette", "ces",

            // 接続詞
            "et", "ou", "mais", "ni", "car", "que", "si",

            // 関係代名詞
            "qui", "dont",

            // 否定
            "ne", "pas",

            // be 動詞（être 活用・高頻度形）
            "est", "sont",

            // 助動詞（avoir 活用・高頻度形）
            "a",
        };

        /// <summary>
        /// 指定された語が機能語かどうかを判定する。
        /// 比較は大文字小文字を区別する（フランス語はアクセント記号で区別するため）。
        /// </summary>
        /// <param name="word">判定対象の語。</param>
        /// <returns>機能語であれば <c>true</c>。</returns>
        public static bool Contains(string word) => s_words.Contains(word);
    }
}
