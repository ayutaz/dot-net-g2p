#nullable enable

namespace DotNetG2P.Multilingual
{
    /// <summary>Unicode文字種分類。</summary>
    internal enum ScriptKind : byte
    {
        /// <summary>ひらがな・カタカナ・半角カナ・CJK記号</summary>
        Japanese,

        /// <summary>CJK統合漢字（日中共用）</summary>
        CJKIdeograph,

        /// <summary>ASCII英字 (A-Z, a-z)</summary>
        English,

        /// <summary>ラテン拡張文字 (U+00C0-U+024F)</summary>
        Latin,

        /// <summary>ASCII数字 (0-9) および全角数字 (U+FF10-FF19)</summary>
        Digit,

        /// <summary>ASCII句読点・記号</summary>
        Punctuation,

        /// <summary>空白・タブ・改行</summary>
        Whitespace,

        /// <summary>上記以外</summary>
        Other,
    }
}
