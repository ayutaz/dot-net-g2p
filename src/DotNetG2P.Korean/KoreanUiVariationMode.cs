namespace DotNetG2P.Korean
{
    /// <summary>
    /// 例外辞書で管理する発音変異の返却方針。
    /// </summary>
    public enum KoreanUiVariationMode : byte
    {
        /// <summary>
        /// 規範寄りの表層を優先する。
        /// </summary>
        Standard = 0,

        /// <summary>
        /// 許容される縮約形・口語寄りの表層を優先する。
        /// </summary>
        Colloquial = 1,
    }
}
