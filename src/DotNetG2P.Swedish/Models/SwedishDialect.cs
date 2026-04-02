namespace DotNetG2P.Swedish
{
    /// <summary>スウェーデン語の方言モード。</summary>
    public enum SwedishDialect : byte
    {
        /// <summary>中央標準スウェーデン語（rikssvenska）。そり舌化あり、ピッチアクセントあり。</summary>
        Central = 0,

        /// <summary>フィンランド・スウェーデン語（finlandssvenska）。そり舌音なし、ピッチアクセントなし。</summary>
        FinlandSwedish = 1,
    }
}
