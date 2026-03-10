using System;

namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class FrenchG2POptions
    {
        /// <summary>デフォルトオプション。</summary>
        public static readonly FrenchG2POptions Default = new FrenchG2POptions();

        /// <summary>方言設定。</summary>
        public FrenchDialect Dialect { get; }

        /// <summary>IPA出力にストレスマークを含めるか。フランス語では通常 false。</summary>
        public bool IncludeStress { get; }

        /// <summary>異音処理を有効化するか。</summary>
        public bool EnableAllophones { get; }

        /// <summary>テキスト正規化を有効化するか。</summary>
        public bool EnableTextNormalization { get; }

        /// <summary>例外辞書を有効化するか。</summary>
        public bool EnableExceptionDictionary { get; }

        /// <summary>音素列出力の区切り文字。</summary>
        public string Separator { get; }

        /// <summary>有効な異音規則セット。</summary>
        public FrenchAllophoneFeatures AllophoneFeatures { get; }

        /// <summary>
        /// FrenchG2POptions を初期化する。
        /// </summary>
        public FrenchG2POptions(
            FrenchDialect dialect = FrenchDialect.Metropolitan,
            bool includeStress = false,
            bool enableAllophones = false,
            bool enableTextNormalization = true,
            bool enableExceptionDictionary = true,
            string separator = " ",
            FrenchAllophoneFeatures allophoneFeatures = FrenchAllophoneFeatures.Default)
        {
            Dialect = dialect;
            IncludeStress = includeStress;
            EnableAllophones = enableAllophones;
            EnableTextNormalization = enableTextNormalization;
            EnableExceptionDictionary = enableExceptionDictionary;
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
            AllophoneFeatures = allophoneFeatures;
        }
    }
}
