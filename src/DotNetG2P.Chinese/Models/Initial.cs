namespace DotNetG2P.Chinese
{
    /// <summary>普通話の声母（Initial）。21個 + ゼロ声母 + 半母音Y/W。</summary>
    public enum Initial : byte
    {
        /// <summary>ゼロ声母（声母なし）</summary>
        None = 0,

        // ── 両唇音・唇歯音 ──

        /// <summary>b [p] 無声無気両唇破裂音</summary>
        B,

        /// <summary>p [pʰ] 無声有気両唇破裂音</summary>
        P,

        /// <summary>m [m] 有声両唇鼻音</summary>
        M,

        /// <summary>f [f] 無声唇歯摩擦音</summary>
        F,

        // ── 歯茎音 ──

        /// <summary>d [t] 無声無気歯茎破裂音</summary>
        D,

        /// <summary>t [tʰ] 無声有気歯茎破裂音</summary>
        T,

        /// <summary>n [n] 有声歯茎鼻音</summary>
        N,

        /// <summary>l [l] 有声歯茎側面接近音</summary>
        L,

        // ── 軟口蓋音 ──

        /// <summary>g [k] 無声無気軟口蓋破裂音</summary>
        G,

        /// <summary>k [kʰ] 無声有気軟口蓋破裂音</summary>
        K,

        /// <summary>h [x] 無声軟口蓋摩擦音</summary>
        H,

        // ── 歯茎硬口蓋音 ──

        /// <summary>j [tɕ] 無声無気歯茎硬口蓋破擦音</summary>
        J,

        /// <summary>q [tɕʰ] 無声有気歯茎硬口蓋破擦音</summary>
        Q,

        /// <summary>x [ɕ] 無声歯茎硬口蓋摩擦音</summary>
        X,

        // ── そり舌音 ──

        /// <summary>zh [ʈʂ] 無声無気そり舌破擦音</summary>
        Zh,

        /// <summary>ch [ʈʂʰ] 無声有気そり舌破擦音</summary>
        Ch,

        /// <summary>sh [ʂ] 無声そり舌摩擦音</summary>
        Sh,

        /// <summary>r [ɻ] 有声そり舌接近音</summary>
        R,

        // ── 歯茎破擦音・摩擦音 ──

        /// <summary>z [ts] 無声無気歯茎破擦音</summary>
        Z,

        /// <summary>c [tsʰ] 無声有気歯茎破擦音</summary>
        C,

        /// <summary>s [s] 無声歯茎摩擦音</summary>
        S,

        // ── 半母音（ピンイン表記用） ──

        /// <summary>y [j] 硬口蓋接近音（ピンイン表記上の声母）</summary>
        Y,

        /// <summary>w [w] 両唇軟口蓋接近音（ピンイン表記上の声母）</summary>
        W,
    }
}
