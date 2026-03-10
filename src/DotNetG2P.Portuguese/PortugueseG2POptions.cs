using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class PortugueseG2POptions
    {
        /// <summary>デフォルトオプション。</summary>
        public static readonly PortugueseG2POptions Default = new PortugueseG2POptions();

        /// <summary>方言設定。</summary>
        public PortugueseDialect Dialect { get; }

        /// <summary>IPA出力にストレスマークを含めるか。</summary>
        public bool IncludeStress { get; }

        /// <summary>異音処理を有効化するか。</summary>
        public bool EnableAllophones { get; }

        /// <summary>適用する異音規則セット。</summary>
        public PortugueseAllophoneFeatures AllophoneFeatures { get; }

        /// <summary>テキスト正規化を有効化するか。</summary>
        public bool EnableTextNormalization { get; }

        /// <summary>例外辞書を有効化するか。</summary>
        public bool EnableExceptionDictionary { get; }

        /// <summary>音素列出力の区切り文字。</summary>
        public string Separator { get; }

        /// <summary>
        /// PortugueseG2POptions を初期化する。
        /// </summary>
        public PortugueseG2POptions(
            PortugueseDialect dialect = default,
            bool includeStress = true,
            bool enableAllophones = false,
            PortugueseAllophoneFeatures? allophoneFeatures = null,
            bool enableTextNormalization = true,
            bool enableExceptionDictionary = true,
            string separator = " ")
        {
            Dialect = dialect;
            IncludeStress = includeStress;
            EnableAllophones = enableAllophones;
            AllophoneFeatures = allophoneFeatures.HasValue
                ? allophoneFeatures.Value
                : (dialect == PortugueseDialect.European
                    ? PortugueseAllophoneFeatures.EuropeanDefault
                    : PortugueseAllophoneFeatures.BrazilianDefault);
            EnableTextNormalization = enableTextNormalization;
            EnableExceptionDictionary = enableExceptionDictionary;
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
        }
    }
}
