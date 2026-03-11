using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語の異音規則セット。
    /// </summary>
    [Flags]
    public enum PortugueseAllophoneFeatures : byte
    {
        /// <summary>異音規則を適用しない。</summary>
        None = 0,

        /// <summary>母音弱化（無ストレス母音の弱化）を適用する。</summary>
        VowelReduction = 1 << 0,

        /// <summary>鼻音の調音位置同化を適用する。</summary>
        NasalAssimilation = 1 << 1,

        /// <summary>歯擦音の有声性同化を適用する。</summary>
        SibilantVoicingAssimilation = 1 << 2,

        /// <summary>閉鎖音弱化 [b→β, d→ð, g→ɣ]（EP向け）を適用する。</summary>
        Lenition = 1 << 3,

        /// <summary>歯擦音の後部歯茎化 [s→ʃ, z→ʒ]（EP・リオ向け）を適用する。</summary>
        SibilantPalatalization = 1 << 4,

        /// <summary>t/d の破擦音化 [t→tʃ, d→dʒ] + /i/（BP向け）を適用する。</summary>
        TDPalatalization = 1 << 5,

        /// <summary>コーダ /l/ の異音: BP=半母音化[w] / EP=軟口蓋化[ɫ]（方言自動切替）を適用する。</summary>
        LAllophony = 1 << 6,

        /// <summary>ほぼ必須とみなす異音規則セット。</summary>
        Obligatory = VowelReduction | NasalAssimilation | SibilantVoicingAssimilation,

        /// <summary>ブラジルポルトガル語の既定プリセット。</summary>
        BrazilianDefault = Obligatory | TDPalatalization | LAllophony,

        /// <summary>ヨーロッパポルトガル語の既定プリセット。</summary>
        EuropeanDefault = Obligatory | Lenition | SibilantPalatalization | LAllophony,

        /// <summary>実装済みの異音規則をすべて適用する。</summary>
        All = VowelReduction | NasalAssimilation | SibilantVoicingAssimilation | Lenition | SibilantPalatalization | TDPalatalization | LAllophony,
    }
}
