using System;

namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語の異音規則セット。
    /// </summary>
    [Flags]
    public enum FrenchAllophoneFeatures : byte
    {
        /// <summary>異音規則を適用しない。</summary>
        None = 0,
        /// <summary>/ʁ/ の無声化（無声阻害音後）を適用する。</summary>
        RDevoicing = 1 << 0,
        /// <summary>阻害音の有声性同化を適用する。</summary>
        ObstruentVoicingAssimilation = 1 << 1,
        /// <summary>閉音節での母音長化を適用する。</summary>
        VowelLengthening = 1 << 2,
        /// <summary>/l/ の軟口蓋化を適用する。</summary>
        LVelarization = 1 << 3,
        /// <summary>語末阻害音の無声化を適用する。</summary>
        FinalDevoicing = 1 << 4,

        /// <summary>ルールベースでほぼ必須とみなす異音規則セット。</summary>
        Obligatory = RDevoicing | ObstruentVoicingAssimilation,
        /// <summary>既定の異音規則セット。</summary>
        Default = Obligatory,
        /// <summary>実装済みの異音規則をすべて適用する。</summary>
        All = Default | VowelLengthening | LVelarization | FinalDevoicing,
    }
}
