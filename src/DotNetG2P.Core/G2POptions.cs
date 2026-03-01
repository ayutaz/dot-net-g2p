namespace DotNetG2P
{
    /// <summary>
    /// G2Pエンジンの処理オプション（イミュータブル）。
    /// NJDパイプラインの各処理段階をON/OFFできる。
    /// </summary>
    public sealed class G2POptions
    {
        /// <summary>テキスト正規化を有効にするか</summary>
        public bool EnableTextNormalization { get; }

        /// <summary>無声音化処理を有効にするか</summary>
        public bool EnableUnvoicedVowel { get; }

        /// <summary>数字読み変換を有効にするか</summary>
        public bool EnableDigitProcessing { get; }

        /// <summary>アクセント句結合を有効にするか</summary>
        public bool EnableAccentPhrase { get; }

        /// <summary>アクセント結合型処理を有効にするか</summary>
        public bool EnableAccentType { get; }

        /// <summary>デフォルトの全処理有効オプション</summary>
        public static readonly G2POptions Default = new G2POptions();

        public G2POptions(
            bool enableTextNormalization = true,
            bool enableUnvoicedVowel = true,
            bool enableDigitProcessing = true,
            bool enableAccentPhrase = true,
            bool enableAccentType = true)
        {
            EnableTextNormalization = enableTextNormalization;
            EnableUnvoicedVowel = enableUnvoicedVowel;
            EnableDigitProcessing = enableDigitProcessing;
            EnableAccentPhrase = enableAccentPhrase;
            EnableAccentType = enableAccentType;
        }
    }
}
