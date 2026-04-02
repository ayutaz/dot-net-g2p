using System;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語G2Pエンジンの処理オプション（イミュータブル）。
    /// </summary>
    public sealed class SwedishG2POptions
    {
        /// <summary>デフォルトオプション。</summary>
        public static readonly SwedishG2POptions Default = new SwedishG2POptions();

        /// <summary>方言設定。</summary>
        public SwedishDialect Dialect { get; }

        /// <summary>IPA出力にストレスマークを含めるか。</summary>
        public bool IncludeStress { get; }

        /// <summary>音素列出力の区切り文字。</summary>
        public string Separator { get; }

        /// <summary>
        /// SwedishG2POptions を初期化する。
        /// </summary>
        public SwedishG2POptions(
            SwedishDialect dialect = SwedishDialect.Central,
            bool includeStress = true,
            string separator = " ")
        {
            Dialect = dialect;
            IncludeStress = includeStress;
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
        }
    }
}
