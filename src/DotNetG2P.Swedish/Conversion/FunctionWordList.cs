using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish.Conversion
{
    /// <summary>
    /// スウェーデン語の機能語リスト（ストレス除去対象）。
    /// 連続発話中で韻律的に弱化し、ストレスマーカーが付与されない語を列挙する。
    /// 人称代名詞・目的格代名詞・所有代名詞・冠詞・前置詞・接続詞・助動詞・副詞（弱形）を含む。
    /// </summary>
    internal static class FunctionWordList
    {
        private static readonly HashSet<string> s_words = new HashSet<string>(StringComparer.Ordinal)
        {
            // 人称代名詞
            "jag", "du", "han", "hon", "den", "det", "vi", "ni", "de",

            // 目的格代名詞
            "mig", "dig", "sig", "honom", "henne", "oss", "er", "dem",

            // 所有代名詞
            "min", "din", "sin", "hans", "hennes", "vår", "deras",

            // 冠詞
            "en", "ett",

            // 前置詞
            "i", "på", "av", "till", "med", "för", "om", "ur", "vid", "hos",
            "mot", "från", "under", "över", "efter", "utan", "mellan", "genom",

            // 接続詞
            "och", "att", "eller", "men", "som", "när", "medan", "fast", "ty",

            // 助動詞
            "är", "var", "har", "hade", "ska", "skulle", "kan", "kunde",
            "vill", "ville", "måste", "bör", "får", "fick",

            // 副詞（弱形）
            "inte", "så", "då", "ju", "nog", "väl", "här", "där",
        };

        /// <summary>
        /// 指定された語が機能語かどうかを判定する。
        /// 比較は大文字小文字を区別する（正規化後の小文字形を想定）。
        /// </summary>
        /// <param name="word">判定対象の語。</param>
        /// <returns>機能語であれば <c>true</c>。</returns>
        public static bool Contains(string word) => s_words.Contains(word);
    }
}
