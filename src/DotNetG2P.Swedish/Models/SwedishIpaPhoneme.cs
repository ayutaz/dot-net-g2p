namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語G2Pで使用するIPA音素。
    /// </summary>
    public enum SwedishIpaPhoneme : byte
    {
        // === 長母音 (0-8) ===
        /// <summary>/iː/ 非円唇前舌狭母音（長）</summary>
        LongI = 0,
        /// <summary>/yː/ 円唇前舌狭母音（長）</summary>
        LongY = 1,
        /// <summary>/ʉː/ 円唇中舌狭母音（長）</summary>
        LongUCentral = 2,
        /// <summary>/uː/ 円唇後舌狭母音（長）</summary>
        LongU = 3,
        /// <summary>/eː/ 非円唇前舌半狭母音（長）</summary>
        LongE = 4,
        /// <summary>/øː/ 円唇前舌半狭母音（長）</summary>
        LongOe = 5,
        /// <summary>/ɛː/ 非円唇前舌半広母音（長）</summary>
        LongEh = 6,
        /// <summary>/oː/ 円唇後舌半狭母音（長）</summary>
        LongO = 7,
        /// <summary>/ɑː/ 非円唇後舌広母音（長）</summary>
        LongA = 8,

        // === 短母音 (9-17) ===
        /// <summary>/ɪ/ 準前舌準狭母音（短）</summary>
        ShortI = 9,
        /// <summary>/ʏ/ 円唇準前舌準狭母音（短）</summary>
        ShortY = 10,
        /// <summary>/ɵ/ 円唇中舌半狭母音（短）</summary>
        ShortUCentral = 11,
        /// <summary>/ʊ/ 円唇準後舌準狭母音（短）</summary>
        ShortU = 12,
        /// <summary>/ɛ/ 非円唇前舌半広母音（短）</summary>
        ShortE = 13,
        /// <summary>/œ/ 円唇前舌半広母音（短）</summary>
        ShortOe = 14,
        /// <summary>/ɔ/ 円唇後舌半広母音（短）</summary>
        ShortO = 15,
        /// <summary>/a/ 非円唇前舌広母音（短）</summary>
        ShortA = 16,
        /// <summary>/ə/ シュワー（弱化母音）</summary>
        Schwa = 17,

        // === 破裂音 (18-23) ===
        /// <summary>/p/</summary>
        P = 18,
        /// <summary>/b/</summary>
        B = 19,
        /// <summary>/t/</summary>
        T = 20,
        /// <summary>/d/</summary>
        D = 21,
        /// <summary>/k/</summary>
        K = 22,
        /// <summary>/ɡ/</summary>
        G = 23,

        // === 摩擦音 (24-29) ===
        /// <summary>/f/</summary>
        F = 24,
        /// <summary>/v/</summary>
        V = 25,
        /// <summary>/s/</summary>
        S = 26,
        /// <summary>/h/</summary>
        H = 27,
        /// <summary>/ɧ/ sj音（無声硬口蓋軟口蓋摩擦音）</summary>
        Sj = 28,
        /// <summary>/ɕ/ tj音（無声歯茎硬口蓋摩擦音）</summary>
        Tj = 29,

        // === 鼻音 (30-32) ===
        /// <summary>/m/</summary>
        M = 30,
        /// <summary>/n/</summary>
        N = 31,
        /// <summary>/ŋ/</summary>
        Ng = 32,

        // === 接近音・ふるえ音 (33-35) ===
        /// <summary>/l/</summary>
        L = 33,
        /// <summary>/r/</summary>
        R = 34,
        /// <summary>/j/</summary>
        J = 35,

        // === そり舌音 (36-40) ===
        /// <summary>/ʈ/ 無声そり舌破裂音（rt）</summary>
        RetroT = 36,
        /// <summary>/ɖ/ 有声そり舌破裂音（rd）</summary>
        RetroD = 37,
        /// <summary>/ɳ/ そり舌鼻音（rn）</summary>
        RetroN = 38,
        /// <summary>/ɭ/ そり舌側面接近音（rl）</summary>
        RetroL = 39,
        /// <summary>/ʂ/ 無声そり舌摩擦音（rs）</summary>
        RetroS = 40,
    }
}
