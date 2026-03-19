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

        /// <summary>LTS（Letter-to-Sound）ルールによるOOVフォールバックを有効にするか</summary>
        public bool EnableLts { get; }

        /// <summary>テキスト正規化（数字・通貨・時刻・略語等の展開）を有効にするか</summary>
        public bool EnableNormalization { get; }

        /// <summary>同綴異音語（Homograph）の文脈依存解決を有効にするか</summary>
        public bool EnableHomographResolution { get; }

        /// <summary>機能語のストレスを除去するかどうか（piper-plus 互換）。デフォルト: true。</summary>
        public bool RemoveFunctionWordStress { get; }

        /// <summary>piper-plus 互換の長音マーク付き IPA 出力を使用するかどうか。デフォルト: true。</summary>
        public bool UsePiperIpaStyle { get; }

        /// <summary>デフォルトオプション</summary>
        public static readonly EnglishG2POptions Default = new EnglishG2POptions();

        /// <summary>
        /// EnglishG2POptionsを初期化する。
        /// </summary>
        /// <param name="includeStress">ストレス番号を出力に含めるか（デフォルト: true）</param>
        /// <param name="unknownWordHandling">OOV処理方針（デフォルト: Skip）</param>
        /// <param name="enableLts">LTSフォールバックを有効にするか（デフォルト: true）</param>
        /// <param name="enableNormalization">テキスト正規化を有効にするか（デフォルト: true）</param>
        /// <param name="enableHomographResolution">同綴異音語解決を有効にするか（デフォルト: true）</param>
        /// <param name="removeFunctionWordStress">機能語のストレスを除去するか（デフォルト: true）</param>
        /// <param name="usePiperIpaStyle">piper-plus 互換の長音マーク付き IPA 出力を使用するか（デフォルト: true）</param>
        public EnglishG2POptions(
            bool includeStress = true,
            UnknownWordStrategy unknownWordHandling = UnknownWordStrategy.Skip,
            bool enableLts = true,
            bool enableNormalization = true,
            bool enableHomographResolution = true,
            bool removeFunctionWordStress = true,
            bool usePiperIpaStyle = true)
        {
            IncludeStress = includeStress;
            UnknownWordHandling = unknownWordHandling;
            EnableLts = enableLts;
            EnableNormalization = enableNormalization;
            EnableHomographResolution = enableHomographResolution;
            RemoveFunctionWordStress = removeFunctionWordStress;
            UsePiperIpaStyle = usePiperIpaStyle;
        }
    }
}
