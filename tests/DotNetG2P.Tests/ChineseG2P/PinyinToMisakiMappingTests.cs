using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToMisaki のマッピングテーブル単体テスト。
    /// 声母 22 エントリ + 韻母 36 エントリ + 声調 5 エントリの全マッピングを検証する。
    /// Convert メソッドのロジックテストは T02 の PinyinToMisakiConvertTests 側で行う。
    /// </summary>
    public class PinyinToMisakiMappingTests
    {
        // ===== 声母マッピング =====

        [Theory]
        [InlineData(Initial.B, "p")]
        [InlineData(Initial.P, "p\u02B0")]      // pʰ
        [InlineData(Initial.M, "m")]
        [InlineData(Initial.F, "f")]
        public void InitialMapping_Labials_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        [Theory]
        [InlineData(Initial.D, "t")]
        [InlineData(Initial.T, "t\u02B0")]      // tʰ
        [InlineData(Initial.N, "n")]
        [InlineData(Initial.L, "l")]
        public void InitialMapping_Alveolars_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        [Theory]
        [InlineData(Initial.G, "k")]
        [InlineData(Initial.K, "k\u02B0")]      // kʰ
        [InlineData(Initial.H, "x")]
        public void InitialMapping_Velars_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── Misaki 差異: j/q は合字 ʨ/ʨʰ (U+02A8) を使用 ──

        [Fact]
        public void InitialMapping_J_UsesTcLigature()
        {
            // DotNetG2P 標準IPA: "t\u0255" (tɕ)
            // Misaki: "\u02A8" (ʨ、合字)
            Assert.Equal("\u02A8", PinyinToMisaki.GetInitialMisaki(Initial.J));
        }

        [Fact]
        public void InitialMapping_Q_UsesTcLigatureWithAspiration()
        {
            // DotNetG2P 標準IPA: "t\u0255\u02B0" (tɕʰ)
            // Misaki: "\u02A8\u02B0" (ʨʰ)
            Assert.Equal("\u02A8\u02B0", PinyinToMisaki.GetInitialMisaki(Initial.Q));
        }

        [Fact]
        public void InitialMapping_X_UnchangedFromStandardIpa()
        {
            Assert.Equal("\u0255", PinyinToMisaki.GetInitialMisaki(Initial.X));
        }

        [Theory]
        [InlineData(Initial.Zh, "\u0288\u0282")]        // ʈʂ
        [InlineData(Initial.Ch, "\u0288\u0282\u02B0")]  // ʈʂʰ
        [InlineData(Initial.Sh, "\u0282")]              // ʂ
        [InlineData(Initial.R,  "\u027B")]              // ɻ
        public void InitialMapping_Retroflex_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── Misaki 差異: z/c は合字 ʦ/ʦʰ (U+02A6) を使用 ──

        [Fact]
        public void InitialMapping_Z_UsesTsLigature()
        {
            // DotNetG2P 標準IPA: "ts"
            // Misaki: "\u02A6" (ʦ、合字)
            Assert.Equal("\u02A6", PinyinToMisaki.GetInitialMisaki(Initial.Z));
        }

        [Fact]
        public void InitialMapping_C_UsesTsLigatureWithAspiration()
        {
            // DotNetG2P 標準IPA: "ts\u02B0" (tsʰ)
            // Misaki: "\u02A6\u02B0" (ʦʰ)
            Assert.Equal("\u02A6\u02B0", PinyinToMisaki.GetInitialMisaki(Initial.C));
        }

        [Fact]
        public void InitialMapping_S_UnchangedFromStandardIpa()
        {
            Assert.Equal("s", PinyinToMisaki.GetInitialMisaki(Initial.S));
        }

        [Theory]
        [InlineData(Initial.Y, "j")]
        [InlineData(Initial.W, "w")]
        public void InitialMapping_Semivowels_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        [Fact]
        public void InitialMapping_None_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToMisaki.GetInitialMisaki(Initial.None));
        }

        // ===== 韻母マッピング =====

        [Theory]
        [InlineData(Final.A, "a")]
        [InlineData(Final.O, "o")]
        [InlineData(Final.E, "\u0264")]     // ɤ
        public void FinalMapping_SimpleVowels_ReturnsExpectedMisakiIpa(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        // ── Misaki 差異: 二重母音に非音節化符号 U+032F を付与 ──

        [Theory]
        [InlineData(Final.Ai, "ai\u032F")]      // ai̯   (標準IPA: aɪ)
        [InlineData(Final.Ei, "ei\u032F")]      // ei̯   (標準IPA: eɪ)
        [InlineData(Final.Ao, "au\u032F")]      // au̯   (標準IPA: aʊ)
        [InlineData(Final.Ou, "ou\u032F")]      // ou̯   (標準IPA: oʊ)
        public void FinalMapping_OpenDiphthongs_UseNonSyllabicMark(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Theory]
        [InlineData(Final.Iao, "iau\u032F")]    // iau̯
        [InlineData(Final.Iu,  "iou\u032F")]    // iou̯  (iu = iou)
        [InlineData(Final.Uai, "uai\u032F")]    // uai̯
        [InlineData(Final.Ui,  "uei\u032F")]    // uei̯  (ui = uei)
        public void FinalMapping_ComplexDiphthongs_UseNonSyllabicMark(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Ong_UsesNonSyllabicUBeforeNg()
        {
            // 標準IPA: "\u028A\u014B" (ʊŋ)
            // Misaki:  "u\u032F\u014B" (u̯ŋ)
            Assert.Equal("u\u032F\u014B", PinyinToMisaki.GetFinalMisaki(Final.Ong));
        }

        [Fact]
        public void FinalMapping_Iong_UsesNonSyllabicUBeforeNg()
        {
            // 標準IPA: "i\u028A\u014B" (iʊŋ)
            // Misaki:  "iu\u032F\u014B" (iu̯ŋ)
            Assert.Equal("iu\u032F\u014B", PinyinToMisaki.GetFinalMisaki(Final.Iong));
        }

        // ── PinyinToIpa と同一の韻母（差異なし） ──

        [Theory]
        [InlineData(Final.An,   "an")]
        [InlineData(Final.En,   "\u0259n")]         // ən
        [InlineData(Final.Ang,  "a\u014B")]         // aŋ
        [InlineData(Final.Eng,  "\u0259\u014B")]    // əŋ
        public void FinalMapping_OpenNasals_UnchangedFromStandardIpa(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Theory]
        [InlineData(Final.I,    "i")]
        [InlineData(Final.Ia,   "ia")]
        [InlineData(Final.Ie,   "i\u025B")]         // iɛ
        [InlineData(Final.Ian,  "i\u025Bn")]        // iɛn
        [InlineData(Final.In,   "in")]
        [InlineData(Final.Iang, "ia\u014B")]        // iaŋ
        [InlineData(Final.Ing,  "i\u014B")]         // iŋ
        public void FinalMapping_FrontVowelFinals_UnchangedFromStandardIpa(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Theory]
        [InlineData(Final.U,    "u")]
        [InlineData(Final.Ua,   "ua")]
        [InlineData(Final.Uo,   "uo")]
        [InlineData(Final.Uan,  "uan")]
        [InlineData(Final.Un,   "u\u0259n")]        // uən
        [InlineData(Final.Uang, "ua\u014B")]        // uaŋ
        [InlineData(Final.Ueng, "u\u0259\u014B")]   // uəŋ
        public void FinalMapping_BackVowelFinals_UnchangedFromStandardIpa(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Theory]
        [InlineData(Final.V,    "y")]
        [InlineData(Final.Ve,   "y\u025B")]         // yɛ
        [InlineData(Final.Van,  "yan")]
        [InlineData(Final.Vn,   "yn")]
        public void FinalMapping_CloseFrontRoundedFinals_UnchangedFromStandardIpa(Final final_, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Er_UnchangedFromStandardIpa()
        {
            Assert.Equal("\u0259\u027B", PinyinToMisaki.GetFinalMisaki(Final.Er));
        }

        [Fact]
        public void FinalMapping_None_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToMisaki.GetFinalMisaki(Final.None));
        }

        // ===== 特殊母音（そり舌・歯茎） =====

        [Fact]
        public void RetroflexApical_MatchesStandardIpa()
        {
            // zh/ch/sh/r + i のそり舌母音 ɻ̩ (U+027B + U+0329)
            Assert.Equal("\u027B\u0329", PinyinToMisaki.GetRetroflexApical());
        }

        [Fact]
        public void AlveolarApical_MatchesStandardIpa()
        {
            // z/c/s + i の歯茎母音 ɹ̩ (U+0279 + U+0329)
            Assert.Equal("\u0279\u0329", PinyinToMisaki.GetAlveolarApical());
        }

        // ===== 声調マッピング =====

        [Fact]
        public void ToneMapping_Neutral_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToMisaki.GetToneArrow(Tone.Neutral));
        }

        [Fact]
        public void ToneMapping_First_ReturnsRightArrow()
        {
            Assert.Equal("\u2192", PinyinToMisaki.GetToneArrow(Tone.First));  // →
        }

        [Fact]
        public void ToneMapping_Second_ReturnsNorthEastArrow()
        {
            Assert.Equal("\u2197", PinyinToMisaki.GetToneArrow(Tone.Second)); // ↗
        }

        [Fact]
        public void ToneMapping_Third_ReturnsDownArrow()
        {
            Assert.Equal("\u2193", PinyinToMisaki.GetToneArrow(Tone.Third));  // ↓
        }

        [Fact]
        public void ToneMapping_Fourth_ReturnsSouthEastArrow()
        {
            Assert.Equal("\u2198", PinyinToMisaki.GetToneArrow(Tone.Fourth)); // ↘
        }

        // ===== 網羅性検証 =====

        [Theory]
        [InlineData(Initial.B)]
        [InlineData(Initial.P)]
        [InlineData(Initial.M)]
        [InlineData(Initial.F)]
        [InlineData(Initial.D)]
        [InlineData(Initial.T)]
        [InlineData(Initial.N)]
        [InlineData(Initial.L)]
        [InlineData(Initial.G)]
        [InlineData(Initial.K)]
        [InlineData(Initial.H)]
        [InlineData(Initial.J)]
        [InlineData(Initial.Q)]
        [InlineData(Initial.X)]
        [InlineData(Initial.Zh)]
        [InlineData(Initial.Ch)]
        [InlineData(Initial.Sh)]
        [InlineData(Initial.R)]
        [InlineData(Initial.Z)]
        [InlineData(Initial.C)]
        [InlineData(Initial.S)]
        [InlineData(Initial.Y)]
        [InlineData(Initial.W)]
        public void InitialMapping_HasEntryForAllNonNoneInitials(Initial initial)
        {
            // None 以外の全 Initial がマッピングテーブルに存在すること
            Assert.NotEqual(string.Empty, PinyinToMisaki.GetInitialMisaki(initial));
        }

        [Theory]
        [InlineData(Final.A)]
        [InlineData(Final.O)]
        [InlineData(Final.E)]
        [InlineData(Final.Ai)]
        [InlineData(Final.Ei)]
        [InlineData(Final.Ao)]
        [InlineData(Final.Ou)]
        [InlineData(Final.An)]
        [InlineData(Final.En)]
        [InlineData(Final.Ang)]
        [InlineData(Final.Eng)]
        [InlineData(Final.Ong)]
        [InlineData(Final.I)]
        [InlineData(Final.Ia)]
        [InlineData(Final.Ie)]
        [InlineData(Final.Iao)]
        [InlineData(Final.Iu)]
        [InlineData(Final.Ian)]
        [InlineData(Final.In)]
        [InlineData(Final.Iang)]
        [InlineData(Final.Ing)]
        [InlineData(Final.Iong)]
        [InlineData(Final.U)]
        [InlineData(Final.Ua)]
        [InlineData(Final.Uo)]
        [InlineData(Final.Uai)]
        [InlineData(Final.Ui)]
        [InlineData(Final.Uan)]
        [InlineData(Final.Un)]
        [InlineData(Final.Uang)]
        [InlineData(Final.Ueng)]
        [InlineData(Final.V)]
        [InlineData(Final.Ve)]
        [InlineData(Final.Van)]
        [InlineData(Final.Vn)]
        [InlineData(Final.Er)]
        public void FinalMapping_HasEntryForAllNonNoneFinals(Final final_)
        {
            // None 以外の全 Final がマッピングテーブルに存在すること
            Assert.NotEqual(string.Empty, PinyinToMisaki.GetFinalMisaki(final_));
        }
    }
}
