namespace DotNetG2P.English
{
    /// <summary>
    /// ARPAbetストレス（強勢）マーカー。
    /// CMU辞書では母音にのみ付与される。
    /// </summary>
    public enum Stress : byte
    {
        /// <summary>ストレスなし（子音に使用）</summary>
        None = 0,
        /// <summary>ストレスなし母音 (0)</summary>
        NoStress = 1,
        /// <summary>第一強勢 (1)</summary>
        Primary = 2,
        /// <summary>第二強勢 (2)</summary>
        Secondary = 3,
    }
}
