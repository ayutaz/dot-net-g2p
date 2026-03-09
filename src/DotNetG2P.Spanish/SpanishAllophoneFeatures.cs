using System;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// スペイン語の異音規則セット。
    /// </summary>
    [Flags]
    public enum SpanishAllophoneFeatures : byte
    {
        /// <summary>異音規則を適用しない。</summary>
        None = 0,
        /// <summary>/b d g/ の弱化を適用する。</summary>
        Lenition = 1 << 0,
        /// <summary>鼻音同化を適用する。</summary>
        NasalAssimilation = 1 << 1,
        /// <summary>語頭・鼻音後の /ʝ/ 強化を適用する。</summary>
        YeAffrication = 1 << 2,
        /// <summary>有声子音前の /s/ 有声化を適用する。</summary>
        SVoicing = 1 << 3,
        /// <summary>語末 /d/ の軟化を適用する。</summary>
        FinalDSoftening = 1 << 4,

        /// <summary>ルールベースでほぼ必須とみなす異音規則セット。</summary>
        Obligatory = Lenition | NasalAssimilation | YeAffrication,
        /// <summary>既定の異音規則セット。</summary>
        Default = Obligatory | SVoicing,
        /// <summary>実装済みの異音規則をすべて適用する。</summary>
        All = Default | FinalDSoftening,
    }
}
