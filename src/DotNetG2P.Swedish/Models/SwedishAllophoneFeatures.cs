using System;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語の異音規則セット。
    /// </summary>
    [Flags]
    public enum SwedishAllophoneFeatures : byte
    {
        /// <summary>異音処理なし。</summary>
        None = 0,

        /// <summary>r + 歯茎子音 → そり舌音（rt→ʈ, rd→ɖ, rn→ɳ, rl→ɭ, rs→ʂ）。</summary>
        Retroflexion = 1 << 0,

        /// <summary>ストレス母音の長母音にːマークを付与（Phase 3と連携）。</summary>
        VowelLengthMarking = 1 << 1,

        /// <summary>FinlandSwedishのtj音を破擦音[t͡ɕ]に変換。</summary>
        TjAffrication = 1 << 2,

        /// <summary>Central方言のデフォルト。</summary>
        CentralDefault = Retroflexion | VowelLengthMarking,

        /// <summary>FinlandSwedish方言のデフォルト。</summary>
        FinlandDefault = VowelLengthMarking | TjAffrication,

        /// <summary>全異音処理を有効化。</summary>
        All = Retroflexion | VowelLengthMarking | TjAffrication,
    }
}
