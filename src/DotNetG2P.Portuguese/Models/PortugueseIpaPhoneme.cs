namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語G2Pで使用するIPA音素。
    /// </summary>
    public enum PortugueseIpaPhoneme : byte
    {
        // --- 口母音 (0-8) ---

        /// <summary>/a/ 前舌開母音。</summary>
        A = 0,
        /// <summary>/e/ 前舌半狭母音。</summary>
        E = 1,
        /// <summary>/ɛ/ 前舌半広母音。</summary>
        Eh = 2,
        /// <summary>/i/ 前舌狭母音。</summary>
        I = 3,
        /// <summary>/o/ 後舌半狭母音。</summary>
        O = 4,
        /// <summary>/ɔ/ 後舌半広母音。</summary>
        Oh = 5,
        /// <summary>/u/ 後舌狭母音。</summary>
        U = 6,
        /// <summary>/ɐ/ 中舌ほぼ開母音。</summary>
        Schwa = 7,
        /// <summary>/ɨ/ 中舌ほぼ狭母音（EP固有）。</summary>
        HighCentral = 8,

        // --- 鼻母音 (9-13) ---

        /// <summary>/ɐ̃/ 鼻母音。</summary>
        ANasal = 9,
        /// <summary>/ẽ/ 鼻母音。</summary>
        ENasal = 10,
        /// <summary>/ĩ/ 鼻母音。</summary>
        INasal = 11,
        /// <summary>/õ/ 鼻母音。</summary>
        ONasal = 12,
        /// <summary>/ũ/ 鼻母音。</summary>
        UNasal = 13,

        // --- 半母音 (14-15) ---

        /// <summary>/j/ 硬口蓋接近音。</summary>
        J = 14,
        /// <summary>/w/ 軟口蓋唇接近音。</summary>
        W = 15,

        // --- 破裂音 (16-21) ---

        /// <summary>/p/ 無声両唇破裂音。</summary>
        P = 16,
        /// <summary>/b/ 有声両唇破裂音。</summary>
        B = 17,
        /// <summary>/t/ 無声歯茎破裂音。</summary>
        T = 18,
        /// <summary>/d/ 有声歯茎破裂音。</summary>
        D = 19,
        /// <summary>/k/ 無声軟口蓋破裂音。</summary>
        K = 20,
        /// <summary>/ɡ/ 有声軟口蓋破裂音。</summary>
        G = 21,

        // --- 摩擦音 (22-27) ---

        /// <summary>/f/ 無声唇歯摩擦音。</summary>
        F = 22,
        /// <summary>/v/ 有声唇歯摩擦音。</summary>
        V = 23,
        /// <summary>/s/ 無声歯茎摩擦音。</summary>
        S = 24,
        /// <summary>/z/ 有声歯茎摩擦音。</summary>
        Z = 25,
        /// <summary>/ʃ/ 無声後部歯茎摩擦音。</summary>
        Sh = 26,
        /// <summary>/ʒ/ 有声後部歯茎摩擦音。</summary>
        Zh = 27,

        // --- 鼻音 (28-30) ---

        /// <summary>/m/ 両唇鼻音。</summary>
        M = 28,
        /// <summary>/n/ 歯茎鼻音。</summary>
        N = 29,
        /// <summary>/ɲ/ 硬口蓋鼻音。</summary>
        Ny = 30,

        // --- 側面音 (31-32) ---

        /// <summary>/l/ 歯茎側面接近音。</summary>
        L = 31,
        /// <summary>/ʎ/ 硬口蓋側面接近音。</summary>
        Lh = 32,

        // --- ロティック (33-34) ---

        /// <summary>/ɾ/ 歯茎はじき音。</summary>
        R = 33,
        /// <summary>/ʁ/ 有声口蓋垂摩擦音（ふるえ音）。</summary>
        Rr = 34,

        // --- BP固有異音 (35-38) ---

        /// <summary>/tʃ/ 無声後部歯茎破擦音（BP: /t/ + /i/ 環境）。</summary>
        Ch = 35,
        /// <summary>/dʒ/ 有声後部歯茎破擦音（BP: /d/ + /i/ 環境）。</summary>
        Jh = 36,
        /// <summary>/ʃ/ 音節末 /s/ の異音（リオ方言）。</summary>
        X = 37,
        /// <summary>/h/ 無声声門摩擦音（BP: /ʁ/ の異音）。</summary>
        H = 38,

        // --- EP固有異音 (39-40) ---

        /// <summary>/ɫ/ 軟口蓋化側面接近音（EP: 音節末 /l/）。</summary>
        DarkL = 39,
        /// <summary>/ʃ/ 音節末 /s/ の異音（EP標準）。</summary>
        Xh = 40,

        // --- 共通異音 (41-43) ---

        /// <summary>/ŋ/ 軟口蓋鼻音。</summary>
        Ng = 41,
        /// <summary>/ɱ/ 唇歯鼻音。</summary>
        NLabiodental = 42,
        /// <summary>/n̪/ 歯鼻音。</summary>
        NDental = 43,

        // --- 弱化異音 (44-46) ---

        /// <summary>/β/ 有声両唇摩擦音（/b/ の弱化異音）。</summary>
        Beta = 44,
        /// <summary>/ð/ 有声歯摩擦音（/d/ の弱化異音）。</summary>
        Dh = 45,
        /// <summary>/ɣ/ 有声軟口蓋摩擦音（/ɡ/ の弱化異音）。</summary>
        Gh = 46,

        // --- 鼻わたり音 (47-48) ---

        /// <summary>/w̃/ 鼻音化軟口蓋唇接近音。</summary>
        WNasal = 47,
        /// <summary>/j̃/ 鼻音化硬口蓋接近音。</summary>
        JNasal = 48,
    }
}
