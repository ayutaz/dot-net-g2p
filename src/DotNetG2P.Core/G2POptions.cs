namespace DotNetG2P
{
    /// <summary>
    /// G2Pエンジンの処理オプション。
    /// NJDパイプラインの各処理段階をON/OFFできる。
    /// </summary>
    public sealed class G2POptions
    {
        /// <summary>テキスト正規化を有効にするか（デフォルト: true）</summary>
        public bool EnableTextNormalization { get; set; } = true;

        /// <summary>無声音化処理を有効にするか（デフォルト: true）</summary>
        public bool EnableUnvoicedVowel { get; set; } = true;

        /// <summary>数字読み変換を有効にするか（デフォルト: true）</summary>
        public bool EnableDigitProcessing { get; set; } = true;

        /// <summary>アクセント句結合を有効にするか（デフォルト: true）</summary>
        public bool EnableAccentPhrase { get; set; } = true;

        /// <summary>アクセント結合型処理を有効にするか（デフォルト: true）</summary>
        public bool EnableAccentType { get; set; } = true;

        /// <summary>デフォルトの全処理有効オプションを返す</summary>
        public static G2POptions Default => new G2POptions();
    }
}
