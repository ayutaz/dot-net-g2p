namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 中国語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class ChineseG2POptions
    {
        /// <summary>デフォルトオプション</summary>
        public static readonly ChineseG2POptions Default = new ChineseG2POptions();

        /// <summary>デフォルトの出力スタイル</summary>
        public PinyinStyle DefaultStyle { get; }

        /// <summary>声調変調（Tone Sandhi）を有効にするか</summary>
        public bool EnableToneSandhi { get; }

        /// <summary>音節区切り文字</summary>
        public string Separator { get; }

        /// <summary>多音字のフレーズ辞書マッチングを有効にするか</summary>
        public bool HandleHeteronyms { get; }

        /// <summary>
        /// ChineseG2POptionsを初期化する。
        /// </summary>
        /// <param name="defaultStyle">ピンイン出力スタイル（デフォルト: ToneMarked）</param>
        /// <param name="enableToneSandhi">声調変調を有効にするか（デフォルト: true）</param>
        /// <param name="separator">音節区切り文字（デフォルト: " "）</param>
        /// <param name="handleHeteronyms">多音字のフレーズ辞書マッチングを有効にするか（デフォルト: true）</param>
        public ChineseG2POptions(
            PinyinStyle defaultStyle = PinyinStyle.ToneMarked,
            bool enableToneSandhi = true,
            string separator = " ",
            bool handleHeteronyms = true)
        {
            DefaultStyle = defaultStyle;
            EnableToneSandhi = enableToneSandhi;
            Separator = separator;
            HandleHeteronyms = handleHeteronyms;
        }
    }
}
