namespace DotNetG2P.Chinese
{
    /// <summary>普通話の声調（4声 + 軽声）。</summary>
    public enum Tone : byte
    {
        /// <summary>軽声（第0声/第5声）</summary>
        Neutral = 0,
        /// <summary>第1声 陰平 ˥ (55) 高平</summary>
        First = 1,
        /// <summary>第2声 陽平 ˧˥ (35) 上昇</summary>
        Second = 2,
        /// <summary>第3声 上声 ˨˩˦ (214) 低降上昇</summary>
        Third = 3,
        /// <summary>第4声 去声 ˥˩ (51) 高降</summary>
        Fourth = 4,
    }
}
