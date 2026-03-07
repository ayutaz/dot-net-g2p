namespace DotNetG2P.Chinese
{
    /// <summary>ピンイン出力スタイル。</summary>
    public enum PinyinStyle : byte
    {
        /// <summary>声調記号付き（例: zhōng）</summary>
        ToneMarked = 0,
        /// <summary>声調数字末尾（例: zhong1）</summary>
        ToneNumber = 1,
        /// <summary>声調なし（例: zhong）</summary>
        Normal = 2,
    }
}
