namespace DotNetG2P.Chinese
{
    /// <summary>普通話の韻母（Final）。36個 + None。</summary>
    public enum Final : byte
    {
        /// <summary>韻母なし</summary>
        None = 0,

        // ── 開口呼 (a/o/e系) ──

        /// <summary>a [a] 開口呼単韻母</summary>
        A,

        /// <summary>o [o] 開口呼単韻母</summary>
        O,

        /// <summary>e [ɤ] 開口呼単韻母</summary>
        E,

        /// <summary>ai [aɪ] 開口呼複韻母</summary>
        Ai,

        /// <summary>ei [eɪ] 開口呼複韻母</summary>
        Ei,

        /// <summary>ao [ɑʊ] 開口呼複韻母</summary>
        Ao,

        /// <summary>ou [oʊ] 開口呼複韻母</summary>
        Ou,

        /// <summary>an [an] 開口呼鼻韻母</summary>
        An,

        /// <summary>en [ən] 開口呼鼻韻母</summary>
        En,

        /// <summary>ang [ɑŋ] 開口呼鼻韻母</summary>
        Ang,

        /// <summary>eng [əŋ] 開口呼鼻韻母</summary>
        Eng,

        /// <summary>ong [ʊŋ] 開口呼鼻韻母</summary>
        Ong,

        // ── 齊齒呼 (i系) ──

        /// <summary>i [i] 齊齒呼単韻母</summary>
        I,

        /// <summary>ia [ia] 齊齒呼複韻母</summary>
        Ia,

        /// <summary>ie [iɛ] 齊齒呼複韻母</summary>
        Ie,

        /// <summary>iao [iɑʊ] 齊齒呼複韻母</summary>
        Iao,

        /// <summary>iu (iou) [ioʊ] 齊齒呼複韻母</summary>
        Iu,

        /// <summary>ian [iɛn] 齊齒呼鼻韻母</summary>
        Ian,

        /// <summary>in [in] 齊齒呼鼻韻母</summary>
        In,

        /// <summary>iang [iɑŋ] 齊齒呼鼻韻母</summary>
        Iang,

        /// <summary>ing [iŋ] 齊齒呼鼻韻母</summary>
        Ing,

        /// <summary>iong [iʊŋ] 齊齒呼鼻韻母</summary>
        Iong,

        // ── 合口呼 (u系) ──

        /// <summary>u [u] 合口呼単韻母</summary>
        U,

        /// <summary>ua [ua] 合口呼複韻母</summary>
        Ua,

        /// <summary>uo [uo] 合口呼複韻母</summary>
        Uo,

        /// <summary>uai [uaɪ] 合口呼複韻母</summary>
        Uai,

        /// <summary>ui (uei) [ueɪ] 合口呼複韻母</summary>
        Ui,

        /// <summary>uan [uan] 合口呼鼻韻母</summary>
        Uan,

        /// <summary>un (uen) [uən] 合口呼鼻韻母</summary>
        Un,

        /// <summary>uang [uɑŋ] 合口呼鼻韻母</summary>
        Uang,

        /// <summary>ueng [uəŋ] 合口呼鼻韻母</summary>
        Ueng,

        // ── 撮口呼 (ü系) ──

        /// <summary>ü [y] 撮口呼単韻母</summary>
        V,

        /// <summary>üe [yɛ] 撮口呼複韻母</summary>
        Ve,

        /// <summary>üan [yɛn] 撮口呼鼻韻母</summary>
        Van,

        /// <summary>ün [yn] 撮口呼鼻韻母</summary>
        Vn,

        // ── 特殊韻母 ──

        /// <summary>er [ɑɻ] 児化韻（特殊韻母）</summary>
        Er,
    }
}
