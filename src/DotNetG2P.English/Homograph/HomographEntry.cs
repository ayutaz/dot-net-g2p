namespace DotNetG2P.English.Homograph
{
    /// <summary>
    /// 品詞→発音バリアントのマッピングルール。
    /// </summary>
    internal readonly struct HomographRule
    {
        /// <summary>対象品詞</summary>
        public PosTag Pos { get; }

        /// <summary>この品詞の場合に選択するCMU辞書バリアントインデックス</summary>
        public int VariantIndex { get; }

        public HomographRule(PosTag pos, int variantIndex)
        {
            Pos = pos;
            VariantIndex = variantIndex;
        }
    }

    /// <summary>
    /// 同綴異音語エントリ。単語ごとの品詞→発音バリアントマッピングを保持する。
    /// </summary>
    internal sealed class HomographEntry
    {
        /// <summary>対象単語（大文字）</summary>
        public string Word { get; }

        /// <summary>品詞が判定できない場合のデフォルトバリアントインデックス</summary>
        public int DefaultVariantIndex { get; }

        /// <summary>品詞→バリアントのルール配列</summary>
        public HomographRule[] Rules { get; }

        public HomographEntry(string word, int defaultVariantIndex, params HomographRule[] rules)
        {
            Word = word;
            DefaultVariantIndex = defaultVariantIndex;
            Rules = rules;
        }

        /// <summary>
        /// 指定品詞に対応するバリアントインデックスを返す。
        /// マッチするルールがなければデフォルトを返す。
        /// </summary>
        public int GetVariantIndex(PosTag pos)
        {
            for (int i = 0; i < Rules.Length; i++)
            {
                if (Rules[i].Pos == pos)
                    return Rules[i].VariantIndex;
            }
            return DefaultVariantIndex;
        }
    }
}
