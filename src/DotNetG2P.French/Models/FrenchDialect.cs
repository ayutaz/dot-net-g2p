namespace DotNetG2P.French
{
    /// <summary>フランス語の方言モード。</summary>
    public enum FrenchDialect : byte
    {
        /// <summary>パリ標準（/a/-/ɑ/ 統合、/œ̃/-/ɛ̃/ 合流）。</summary>
        Metropolitan = 0,

        /// <summary>保守的標準（/a/-/ɑ/ 区別、/œ̃/-/ɛ̃/ 区別）。</summary>
        Conservative = 1,
    }
}
