namespace DotNetG2P
{
    /// <summary>
    /// 形態素解析結果の1トークンを表すインターフェース。
    /// naist-jdic辞書の15フィールドへのアクセサを提供する。
    /// </summary>
    public interface IToken
    {
        /// <summary>表層形（原文中の文字列）</summary>
        string Surface { get; }

        /// <summary>素性配列（辞書エントリのカンマ区切りフィールド）</summary>
        string[] Features { get; }

        /// <summary>フィールド0: 品詞</summary>
        string POS { get; }

        /// <summary>フィールド1: 品詞細分類1</summary>
        string POSGroup1 { get; }

        /// <summary>フィールド2: 品詞細分類2</summary>
        string POSGroup2 { get; }

        /// <summary>フィールド3: 品詞細分類3</summary>
        string POSGroup3 { get; }

        /// <summary>フィールド4: 活用型</summary>
        string ConjugationType { get; }

        /// <summary>フィールド5: 活用形</summary>
        string ConjugationForm { get; }

        /// <summary>フィールド6: 原形</summary>
        string OriginalForm { get; }

        /// <summary>フィールド7: 読み</summary>
        string Reading { get; }

        /// <summary>フィールド8: 発音</summary>
        string Pronunciation { get; }

        /// <summary>フィールド9: アクセント修飾型</summary>
        string AccentModificationType { get; }

        /// <summary>フィールド13: アクセント核位置/モーラ数</summary>
        string AccentInfo { get; }

        /// <summary>フィールド14: アクセント結合タイプ</summary>
        string ChainRule { get; }
    }
}
