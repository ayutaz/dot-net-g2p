namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// 軽量品詞タグ。同綴異音語解決に必要最低限の品詞分類。
    /// </summary>
    internal enum PosTag : byte
    {
        /// <summary>不明</summary>
        Unknown = 0,
        /// <summary>名詞</summary>
        Noun = 1,
        /// <summary>動詞</summary>
        Verb = 2,
        /// <summary>形容詞</summary>
        Adjective = 3,
        /// <summary>副詞</summary>
        Adverb = 4,
        /// <summary>前置詞</summary>
        Preposition = 5,
        /// <summary>限定詞</summary>
        Determiner = 6,
        /// <summary>代名詞</summary>
        Pronoun = 7,
        /// <summary>接続詞</summary>
        Conjunction = 8,
    }
}
