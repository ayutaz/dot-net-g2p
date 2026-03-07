namespace DotNetG2P.English
{
    /// <summary>
    /// ARPAbet音素セット。CMU Pronouncing Dictionaryで使用される39音素。
    /// </summary>
    public enum ArpabetPhoneme : byte
    {
        // ===== 母音 15種 =====

        /// <summary>母音 (odd, father)</summary>
        AA,
        /// <summary>母音 (at, bat)</summary>
        AE,
        /// <summary>母音 (hut, but)</summary>
        AH,
        /// <summary>母音 (ought, caught)</summary>
        AO,
        /// <summary>母音 (cow, how)</summary>
        AW,
        /// <summary>母音 (hide, my)</summary>
        AY,
        /// <summary>母音 (ed, bet)</summary>
        EH,
        /// <summary>母音 (hurt, bird)</summary>
        ER,
        /// <summary>母音 (ate, say)</summary>
        EY,
        /// <summary>母音 (it, big)</summary>
        IH,
        /// <summary>母音 (eat, see)</summary>
        IY,
        /// <summary>母音 (oat, show)</summary>
        OW,
        /// <summary>母音 (toy, boy)</summary>
        OY,
        /// <summary>母音 (hood, could)</summary>
        UH,
        /// <summary>母音 (two, food)</summary>
        UW,

        // ===== 子音 24種 =====

        /// <summary>子音 (be)</summary>
        B,
        /// <summary>子音 (cheese)</summary>
        CH,
        /// <summary>子音 (dee)</summary>
        D,
        /// <summary>子音 (thee)</summary>
        DH,
        /// <summary>子音 (fee)</summary>
        F,
        /// <summary>子音 (green)</summary>
        G,
        /// <summary>子音 (he)</summary>
        HH,
        /// <summary>子音 (gee)</summary>
        JH,
        /// <summary>子音 (key)</summary>
        K,
        /// <summary>子音 (lee)</summary>
        L,
        /// <summary>子音 (me)</summary>
        M,
        /// <summary>子音 (knee)</summary>
        N,
        /// <summary>子音 (ping)</summary>
        NG,
        /// <summary>子音 (pee)</summary>
        P,
        /// <summary>子音 (read)</summary>
        R,
        /// <summary>子音 (sea)</summary>
        S,
        /// <summary>子音 (she)</summary>
        SH,
        /// <summary>子音 (tea)</summary>
        T,
        /// <summary>子音 (theta)</summary>
        TH,
        /// <summary>子音 (vee)</summary>
        V,
        /// <summary>子音 (we)</summary>
        W,
        /// <summary>子音 (yield)</summary>
        Y,
        /// <summary>子音 (zee)</summary>
        Z,
        /// <summary>子音 (seizure)</summary>
        ZH,
    }
}
