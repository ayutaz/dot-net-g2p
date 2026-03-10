namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語G2Pで使用するIPA音素。
    /// </summary>
    public enum FrenchIpaPhoneme : byte
    {
        // --- 口母音 (0-11) ---

        /// <summary>/a/ 前舌開母音。</summary>
        A = 0,
        /// <summary>/ɑ/ 後舌開母音。</summary>
        Ah = 1,
        /// <summary>/e/ 前舌半狭母音。</summary>
        E = 2,
        /// <summary>/ɛ/ 前舌半広母音。</summary>
        Eh = 3,
        /// <summary>/i/ 前舌狭母音。</summary>
        I = 4,
        /// <summary>/o/ 後舌半狭母音。</summary>
        O = 5,
        /// <summary>/ɔ/ 後舌半広母音。</summary>
        Oh = 6,
        /// <summary>/u/ 後舌狭母音。</summary>
        U = 7,
        /// <summary>/y/ 前舌円唇狭母音。</summary>
        Y = 8,
        /// <summary>/ø/ 前舌円唇半狭母音。</summary>
        Oe = 9,
        /// <summary>/œ/ 前舌円唇半広母音。</summary>
        Oeh = 10,
        /// <summary>/ə/ あいまい母音（シュワー）。</summary>
        Schwa = 11,

        // --- 鼻母音 (12-15) ---

        /// <summary>/ɑ̃/ 鼻母音。</summary>
        ANasal = 12,
        /// <summary>/ɔ̃/ 鼻母音。</summary>
        ONasal = 13,
        /// <summary>/ɛ̃/ 鼻母音。</summary>
        ENasal = 14,
        /// <summary>/œ̃/ 鼻母音。</summary>
        OeNasal = 15,

        // --- 半母音 (16-18) ---

        /// <summary>/j/ 硬口蓋接近音。</summary>
        J = 16,
        /// <summary>/w/ 軟口蓋唇接近音。</summary>
        W = 17,
        /// <summary>/ɥ/ 唇硬口蓋接近音。</summary>
        Uj = 18,

        // --- 閉鎖音 (19-24) ---

        /// <summary>/p/ 無声両唇閉鎖音。</summary>
        P = 19,
        /// <summary>/b/ 有声両唇閉鎖音。</summary>
        B = 20,
        /// <summary>/t/ 無声歯茎閉鎖音。</summary>
        T = 21,
        /// <summary>/d/ 有声歯茎閉鎖音。</summary>
        D = 22,
        /// <summary>/k/ 無声軟口蓋閉鎖音。</summary>
        K = 23,
        /// <summary>/ɡ/ 有声軟口蓋閉鎖音。</summary>
        G = 24,

        // --- 摩擦音 (25-30) ---

        /// <summary>/f/ 無声唇歯摩擦音。</summary>
        F = 25,
        /// <summary>/v/ 有声唇歯摩擦音。</summary>
        V = 26,
        /// <summary>/s/ 無声歯茎摩擦音。</summary>
        S = 27,
        /// <summary>/z/ 有声歯茎摩擦音。</summary>
        Z = 28,
        /// <summary>/ʃ/ 無声後部歯茎摩擦音。</summary>
        Sh = 29,
        /// <summary>/ʒ/ 有声後部歯茎摩擦音。</summary>
        Zh = 30,

        // --- 鼻音 (31-33) ---

        /// <summary>/m/ 両唇鼻音。</summary>
        M = 31,
        /// <summary>/n/ 歯茎鼻音。</summary>
        N = 32,
        /// <summary>/ɲ/ 硬口蓋鼻音。</summary>
        Ny = 33,

        // --- 側面音 (34) ---

        /// <summary>/l/ 歯茎側面接近音。</summary>
        L = 34,

        // --- 接近音 (35) ---

        /// <summary>/ʁ/ 有声口蓋垂摩擦音。</summary>
        R = 35,

        // --- 異音 (36-39) ---

        /// <summary>/χ/ 無声口蓋垂摩擦音（/ʁ/の無声化異音）。</summary>
        Rh = 36,
        /// <summary>/ŋ/ 軟口蓋鼻音（借用語）。</summary>
        Ng = 37,
        /// <summary>/ts/ 無声歯茎破擦音（借用語）。</summary>
        Ts = 38,
        /// <summary>/dz/ 有声歯茎破擦音（借用語）。</summary>
        Dz = 39,
    }
}
