using System;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// スペイン語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class SpanishG2POptions
    {
        /// <summary>デフォルトオプション。</summary>
        public static readonly SpanishG2POptions Default = new SpanishG2POptions();

        /// <summary>方言設定。</summary>
        public SpanishDialect Dialect { get; }

        /// <summary>IPA / X-SAMPA 出力にストレスマークを含めるか。</summary>
        public bool IncludeStress { get; }

        /// <summary>将来の異音処理を有効化するか。</summary>
        public bool EnableAllophones { get; }

        /// <summary>テキスト正規化を有効化するか。</summary>
        public bool EnableTextNormalization { get; }

        /// <summary>有効な異音規則セット。</summary>
        public SpanishAllophoneFeatures AllophoneFeatures { get; }

        /// <summary>音素列出力の区切り文字。</summary>
        public string Separator { get; }

        /// <summary>
        /// SpanishG2POptions を初期化する。
        /// </summary>
        public SpanishG2POptions(
            SpanishDialect dialect = SpanishDialect.LatinAmerican,
            bool includeStress = true,
            bool enableAllophones = false,
            bool enableTextNormalization = true,
            string separator = " ",
            SpanishAllophoneFeatures allophoneFeatures = SpanishAllophoneFeatures.Default)
        {
            Dialect = dialect;
            IncludeStress = includeStress;
            EnableAllophones = enableAllophones;
            EnableTextNormalization = enableTextNormalization;
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
            AllophoneFeatures = allophoneFeatures;
        }
    }
}
