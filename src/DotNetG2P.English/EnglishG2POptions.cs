namespace DotNetG2P.English
{
    /// <summary>
    /// OOV（辞書未登録語）の処理方針。
    /// </summary>
    public enum UnknownWordStrategy
    {
        /// <summary>未知語をスキップして出力に含めない</summary>
        Skip = 0,
        /// <summary>未知語を検出した場合に例外をスローする</summary>
        Throw = 1,
    }

    /// <summary>
    /// 英語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class EnglishG2POptions
    {
        /// <summary>ストレス番号を出力に含めるか</summary>
        public bool IncludeStress { get; }

        /// <summary>OOV（辞書未登録語）の処理方針</summary>
        public UnknownWordStrategy UnknownWordHandling { get; }

        /// <summary>デフォルトオプション</summary>
        public static readonly EnglishG2POptions Default = new EnglishG2POptions();

        /// <summary>
        /// EnglishG2POptionsを初期化する。
        /// </summary>
        /// <param name="includeStress">ストレス番号を出力に含めるか（デフォルト: true）</param>
        /// <param name="unknownWordHandling">OOV処理方針（デフォルト: Skip）</param>
        public EnglishG2POptions(
            bool includeStress = true,
            UnknownWordStrategy unknownWordHandling = UnknownWordStrategy.Skip)
        {
            IncludeStress = includeStress;
            UnknownWordHandling = unknownWordHandling;
        }
    }
}
