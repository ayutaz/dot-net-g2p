using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese.Conversion
{
    /// <summary>
    /// ポルトガル語の機能語リスト（ストレス除去対象）。
    /// piper-plus には未実装のため、ポルトガル語の一般的な機能語（冠詞・前置詞・
    /// 代名詞・接続詞等）に基づいて構成。
    /// 連続発話中で韻律的に弱化し、Prosody の A2 でストレスマーカーが付与されない。
    /// </summary>
    /// <remarks>
    /// ポルトガル語の機能語は piper-plus のスペイン語リストと同等の粒度で構成。
    /// アクセント記号付きの語（é, à 等）は内容語としてストレスを保持するため除外。
    /// </remarks>
    internal static class FunctionWordList
    {
        private static readonly HashSet<string> s_words = new HashSet<string>(StringComparer.Ordinal)
        {
            // 定冠詞
            "o", "a", "os", "as",

            // 不定冠詞
            "um", "uma", "uns", "umas",

            // 前置詞
            "de", "em", "por", "para", "com", "sem", "sob", "sobre",
            "entre", "contra",

            // 前置詞＋冠詞 縮約形
            "do", "da", "dos", "das",
            "no", "na", "nos", "nas",
            "ao", "aos",
            "pelo", "pela", "pelos", "pelas",

            // 代名詞（弱形・接語）
            "me", "te", "se", "lhe", "lhes",

            // 主格代名詞（弱形として機能する場合）
            "eu", "tu", "ele", "ela",

            // 所有形容詞（前置詞的に弱化する形）
            "meu", "minha", "teu", "tua",
            "seu", "sua", "seus", "suas",

            // 指示形容詞
            "este", "esta", "esse", "essa",

            // 接続詞
            "e", "ou", "mas", "nem", "que", "se",

            // 関係代名詞
            "quem",

            // 否定
            "não",
        };

        /// <summary>
        /// 指定された語が機能語かどうかを判定する。
        /// 比較は大文字小文字を区別する（ポルトガル語はアクセント記号で区別するため）。
        /// </summary>
        /// <param name="word">判定対象の語。</param>
        /// <returns>機能語であれば <c>true</c>。</returns>
        public static bool Contains(string word) => s_words.Contains(word);
    }
}
