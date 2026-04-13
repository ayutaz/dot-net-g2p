using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToMisaki のマッピングテーブル単体テスト。
    /// 声母 21 エントリ + 韻母 36 エントリ(Prefix+Suffix 分離) + 声調 5 エントリ + Y/W 複合韻母 23 エントリの
    /// 全マッピングを検証する。
    ///
    /// Misaki 仕様 (`.claude/tmp/misaki-spec.md`) は uv misaki 0.9.4 の実測値で検証済み:
    /// - j/q → ligature ʨ (U+02A8) / ʨʰ
    /// - zh/ch → ligature ꭧ (U+AB67) / ꭧʰ  (NOT ʈʂ)
    /// - z/c → ligature ʦ (U+02A6) / ʦʰ
    /// - apical vowel は zh/ch/sh/r/z/c/s + i で共通 "ɨ" (U+0268)
    /// - ɥ (U+0265) は ü 系韻母用半母音 (NOT y)
    /// - 二重母音の非音節化符号 U+032F は**事前除去済み**テンプレートを使う
    /// - 声調は中間位置 (prefix + tone + suffix 方式)
    /// - Y/W は s_initialMisaki には存在せず、(Initial, Final) 複合テーブル側で解決
    ///
    /// Convert メソッドのロジックテストは PinyinToMisakiConvertTests 側で行う。
    /// </summary>
    public class PinyinToMisakiMappingTests
    {
        // ============================================================
        // 声母マッピング (21 エントリ、Y/W は compound final 層で処理)
        // ============================================================

        // ── 両唇音・唇歯音 (4) ──

        [Theory]
        [InlineData(Initial.B, "p")]            // p  U+0070
        [InlineData(Initial.P, "p\u02B0")]      // pʰ
        [InlineData(Initial.M, "m")]            // m  U+006D
        [InlineData(Initial.F, "f")]            // f  U+0066
        public void InitialMapping_Labials_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── 歯茎音 (4) ──

        [Theory]
        [InlineData(Initial.D, "t")]            // t  U+0074
        [InlineData(Initial.T, "t\u02B0")]      // tʰ
        [InlineData(Initial.N, "n")]            // n  U+006E
        [InlineData(Initial.L, "l")]            // l  U+006C
        public void InitialMapping_Alveolars_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── 軟口蓋音 (3) ──

        [Theory]
        [InlineData(Initial.G, "k")]            // k  U+006B
        [InlineData(Initial.K, "k\u02B0")]      // kʰ
        [InlineData(Initial.H, "x")]            // x  U+0078
        public void InitialMapping_Velars_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── 歯茎硬口蓋音 (3) ── Misaki 差異: j/q は合字 ʨ (U+02A8) を使用

        [Fact]
        public void InitialMapping_J_UsesTcLigature()
        {
            // 標準IPA の tɕ (U+0074 U+0255) ではなく、Misaki は ligature ʨ (U+02A8) を使う。
            // uv misaki 0.9.4 の実測で confirmed (gh api + uv run で検証済)。
            Assert.Equal("\u02A8", PinyinToMisaki.GetInitialMisaki(Initial.J));
        }

        [Fact]
        public void InitialMapping_Q_UsesTcLigatureWithAspiration()
        {
            // Misaki: ʨʰ (U+02A8 U+02B0)
            Assert.Equal("\u02A8\u02B0", PinyinToMisaki.GetInitialMisaki(Initial.Q));
        }

        [Fact]
        public void InitialMapping_X_ReturnsPalatalFricative()
        {
            // x → ɕ (U+0255、歯茎硬口蓋摩擦音)。標準IPAと同一。
            Assert.Equal("\u0255", PinyinToMisaki.GetInitialMisaki(Initial.X));
        }

        // ── そり舌音 (4) ── Misaki 差異: zh/ch は合字 ꭧ (U+AB67) を使用

        [Fact]
        public void InitialMapping_Zh_UsesTsRetroflexLigature()
        {
            // 標準IPA の ʈʂ (U+0288 U+0282) ではなく、Misaki は ligature ꭧ (U+AB67) を使う。
            // uv misaki 0.9.4 の実測で confirmed。Kokoro 82M vocab にも含まれる。
            Assert.Equal("\uAB67", PinyinToMisaki.GetInitialMisaki(Initial.Zh));
        }

        [Fact]
        public void InitialMapping_Ch_UsesTsRetroflexLigatureWithAspiration()
        {
            // Misaki: ꭧʰ (U+AB67 U+02B0)
            Assert.Equal("\uAB67\u02B0", PinyinToMisaki.GetInitialMisaki(Initial.Ch));
        }

        [Theory]
        [InlineData(Initial.Sh, "\u0282")]      // ʂ  標準IPAと同一
        [InlineData(Initial.R,  "\u027B")]      // ɻ  標準IPAと同一
        public void InitialMapping_RetroflexFricatives_ReturnsExpectedMisakiIpa(Initial initial, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.GetInitialMisaki(initial));
        }

        // ── 歯茎破擦音・摩擦音 (3) ── Misaki 差異: z/c は合字 ʦ (U+02A6) を使用

        [Fact]
        public void InitialMapping_Z_UsesTsLigature()
        {
            // 標準IPA の ts (U+0074 U+0073) ではなく、Misaki は ligature ʦ (U+02A6) を使う。
            Assert.Equal("\u02A6", PinyinToMisaki.GetInitialMisaki(Initial.Z));
        }

        [Fact]
        public void InitialMapping_C_UsesTsLigatureWithAspiration()
        {
            // Misaki: ʦʰ (U+02A6 U+02B0)
            Assert.Equal("\u02A6\u02B0", PinyinToMisaki.GetInitialMisaki(Initial.C));
        }

        [Fact]
        public void InitialMapping_S_ReturnsAlveolarFricative()
        {
            // s → s (U+0073)。標準IPAと同一。
            Assert.Equal("s", PinyinToMisaki.GetInitialMisaki(Initial.S));
        }

        // ── Y/W と None は s_initialMisaki に含まれない ──

        [Fact]
        public void InitialMapping_Y_NotInInitialTable_ReturnsEmpty()
        {
            // Y は compound final テーブル (TryGetYWCompound) で処理される。
            // GetInitialMisaki では空文字を返すこと。
            Assert.Equal(string.Empty, PinyinToMisaki.GetInitialMisaki(Initial.Y));
        }

        [Fact]
        public void InitialMapping_W_NotInInitialTable_ReturnsEmpty()
        {
            // W は compound final テーブル (TryGetYWCompound) で処理される。
            // GetInitialMisaki では空文字を返すこと。
            Assert.Equal(string.Empty, PinyinToMisaki.GetInitialMisaki(Initial.W));
        }

        [Fact]
        public void InitialMapping_None_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToMisaki.GetInitialMisaki(Initial.None));
        }

        // ============================================================
        // 韻母 Prefix/Suffix マッピング (36 エントリ)
        // Misaki の韻母は (prefix, suffix) に分離し、間に声調矢印を挟む方式。
        // 例: Final.An → ("a", "n") + tone → "a→n"
        // ============================================================

        // ── 開口呼 単韻母 (3) ──

        [Theory]
        [InlineData(Final.A,        "a",        "")]     // a
        [InlineData(Final.O,        "wo",       "")]     // wo (bpmf + o は pwo/pʰwo 形式)
        [InlineData(Final.E,        "\u0264",   "")]     // ɤ  U+0264
        public void FinalMapping_OpenSimpleVowels_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        // ── 開口呼 複韻母 (4) ── Misaki: U+032F **なし** (事前除去済み)

        [Theory]
        [InlineData(Final.Ai,       "ai",       "")]     // ai  (NO U+032F)
        [InlineData(Final.Ei,       "ei",       "")]     // ei
        [InlineData(Final.Ao,       "au",       "")]     // au
        [InlineData(Final.Ou,       "ou",       "")]     // ou
        public void FinalMapping_OpenDiphthongs_NoNonSyllabicMark(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Ai_ContainsNoCombiningInvertedBreveBelow()
        {
            // U+032F (COMBINING INVERTED BREVE BELOW) は事前除去済み。
            // Kokoro vocab に含まれないため、テンプレに残してはならない。
            var result = PinyinToMisaki.GetFinalMisaki(Final.Ai);
            Assert.DoesNotContain("\u032F", result.Prefix);
            Assert.DoesNotContain("\u032F", result.Suffix);
        }

        // ── 開口呼 鼻韻母 (5) ── Prefix + Suffix 分離、間に声調を挟む

        [Theory]
        [InlineData(Final.An,       "a",            "n")]         // a→n
        [InlineData(Final.En,       "\u0259",       "n")]         // ə→n
        [InlineData(Final.Ang,      "a",            "\u014B")]    // a→ŋ
        [InlineData(Final.Eng,      "\u0259",       "\u014B")]    // ə→ŋ
        public void FinalMapping_OpenNasals_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Ong_UsesUpsilonBeforeNg()
        {
            // Misaki: ʊ (U+028A) + ŋ (U+014B)。標準IPA と同一だが U+032F は使わない。
            // 旧 "u̯ŋ" (U+0075 U+032F U+014B) は仕様誤認、正しくは "ʊŋ"。
            Assert.Equal(("\u028A", "\u014B"), PinyinToMisaki.GetFinalMisaki(Final.Ong));
        }

        // ── 齊齒呼 単韻母 (1) ──

        [Theory]
        [InlineData(Final.I,        "i",            "")]          // i
        public void FinalMapping_I_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        // ── 齊齒呼 複韻母 (5) ──

        [Theory]
        [InlineData(Final.Ia,       "ja",           "")]          // ja  (j 半母音)
        [InlineData(Final.Iao,      "jau",          "")]          // jau (NO U+032F)
        [InlineData(Final.Iu,       "jou",          "")]          // jou (Misaki "iou"、NO U+032F)
        public void FinalMapping_FrontDiphthongs_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Ie_UsesJePlainE()
        {
            // Misaki: je (j + e)。標準IPA の iɛ (j + ɛ) とは異なる。
            // uv misaki 0.9.4 の実測で confirmed: "ie" → "je"
            Assert.Equal(("je", ""), PinyinToMisaki.GetFinalMisaki(Final.Ie));
        }

        [Fact]
        public void FinalMapping_Ian_UsesJeEpsilonN()
        {
            // Misaki: jɛ + n → ("jɛ", "n")
            // j (U+006A) + ɛ (U+025B) を prefix、n (U+006E) を suffix として分離。
            Assert.Equal(("j\u025B", "n"), PinyinToMisaki.GetFinalMisaki(Final.Ian));
        }

        // ── 齊齒呼 鼻韻母 (4) ──

        [Theory]
        [InlineData(Final.In,       "i",            "n")]         // i→n (j なし)
        [InlineData(Final.Iang,     "ja",           "\u014B")]    // ja→ŋ (j 半母音)
        [InlineData(Final.Ing,      "i",            "\u014B")]    // i→ŋ (j なし)
        public void FinalMapping_IFrontNasals_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Iong_UsesJUpsilonNg()
        {
            // Misaki: j + ʊ + ŋ → ("jʊ", "ŋ")
            // j (U+006A) + ʊ (U+028A) を prefix、ŋ (U+014B) を suffix。
            Assert.Equal(("j\u028A", "\u014B"), PinyinToMisaki.GetFinalMisaki(Final.Iong));
        }

        // ── 合口呼 単韻母・複韻母 (4) ──

        [Theory]
        [InlineData(Final.U,        "u",            "")]          // u
        [InlineData(Final.Ua,       "wa",           "")]          // wa
        [InlineData(Final.Uo,       "wo",           "")]          // wo
        public void FinalMapping_USimpleFinals_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Theory]
        [InlineData(Final.Uai,      "wai",          "")]          // wai (NO U+032F)
        [InlineData(Final.Ui,       "wei",          "")]          // wei (Misaki "uei"、NO U+032F)
        public void FinalMapping_UDiphthongs_NoNonSyllabicMark(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        // ── 合口呼 鼻韻母 (4) ──

        [Theory]
        [InlineData(Final.Uan,      "wa",           "n")]         // wa→n
        [InlineData(Final.Un,       "w\u0259",      "n")]         // wə→n (Misaki "uen")
        [InlineData(Final.Uang,     "wa",           "\u014B")]    // wa→ŋ
        [InlineData(Final.Ueng,     "w\u0259",      "\u014B")]    // wə→ŋ
        public void FinalMapping_UNasals_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        // ── 撮口呼 (ü系) 4 エントリ ──

        [Theory]
        [InlineData(Final.V,        "y",            "")]          // y  (U+0079) ü 単独
        public void FinalMapping_V_ReturnsExpectedPrefixSuffix(
            Final final_, string expectedPrefix, string expectedSuffix)
        {
            Assert.Equal((expectedPrefix, expectedSuffix), PinyinToMisaki.GetFinalMisaki(final_));
        }

        [Fact]
        public void FinalMapping_Ve_UsesHturnedEAsSemivowel()
        {
            // Misaki: ɥe (U+0265 + U+0065)。
            // 標準IPA の yɛ (U+0079 + U+025B) とは異なり、半母音 ɥ を使う。
            // uv misaki 0.9.4 の実測で confirmed: "üe" → "ɥe"
            Assert.Equal(("\u0265e", ""), PinyinToMisaki.GetFinalMisaki(Final.Ve));
        }

        [Fact]
        public void FinalMapping_Van_UsesHturnedEpsilonN()
        {
            // Misaki: ɥɛ + n → ("ɥɛ", "n")
            // 標準IPA の yan ではなく、ɥ (U+0265) + ɛ (U+025B) を prefix、n を suffix。
            Assert.Equal(("\u0265\u025B", "n"), PinyinToMisaki.GetFinalMisaki(Final.Van));
        }

        [Fact]
        public void FinalMapping_Vn_UsesYN()
        {
            // Misaki: y + n → ("y", "n")
            Assert.Equal(("y", "n"), PinyinToMisaki.GetFinalMisaki(Final.Vn));
        }

        // ── 特殊韻母 Er (1) ──

        [Fact]
        public void FinalMapping_Er_UsesRhoticSchwa()
        {
            // Misaki: ɚ (U+025A、RHOTIC SCHWA) 単独。
            // 標準IPA の əɻ (U+0259 U+027B) ではない。uv misaki 0.9.4 で confirmed。
            Assert.Equal(("\u025A", ""), PinyinToMisaki.GetFinalMisaki(Final.Er));
        }

        // ── None ──

        [Fact]
        public void FinalMapping_None_ReturnsEmptyTuple()
        {
            // Final.None は prefix/suffix とも空文字。
            Assert.Equal((string.Empty, string.Empty), PinyinToMisaki.GetFinalMisaki(Final.None));
        }

        // ============================================================
        // 特殊母音: apical vowel ɨ (U+0268)
        // ============================================================
        // Misaki は retroflex (zh/ch/sh/r + i) と alveolar (z/c/s + i) の
        // 両方で apical vowel ɨ (U+0268) を直接使う。
        // 標準IPA の ɻ̩ (U+027B U+0329) や ɹ̩ (U+0279 U+0329) ではない。

        [Fact]
        public void GetApicalMisaki_ReturnsClosedCentralUnroundedVowel()
        {
            // ɨ U+0268 CLOSE CENTRAL UNROUNDED VOWEL
            Assert.Equal("\u0268", PinyinToMisaki.GetApicalMisaki());
        }

        [Fact]
        public void ApicalMisaki_IsSingleCodepoint()
        {
            // 1文字であること (U+0329 の結合記号は付かない)
            Assert.Single(PinyinToMisaki.GetApicalMisaki());
        }

        // ============================================================
        // 声調矢印マッピング (5 エントリ)
        // ============================================================

        [Fact]
        public void ToneMapping_Neutral_ReturnsEmpty()
        {
            // 軽声は矢印なし
            Assert.Equal(string.Empty, PinyinToMisaki.GetToneArrow(Tone.Neutral));
        }

        [Fact]
        public void ToneMapping_First_ReturnsRightArrow()
        {
            // 第1声 陰平 → U+2192 RIGHTWARDS ARROW
            Assert.Equal("\u2192", PinyinToMisaki.GetToneArrow(Tone.First));
        }

        [Fact]
        public void ToneMapping_Second_ReturnsNorthEastArrow()
        {
            // 第2声 陽平 ↗ U+2197 NORTH EAST ARROW
            Assert.Equal("\u2197", PinyinToMisaki.GetToneArrow(Tone.Second));
        }

        [Fact]
        public void ToneMapping_Third_ReturnsDownArrow()
        {
            // 第3声 上声 ↓ U+2193 DOWNWARDS ARROW
            Assert.Equal("\u2193", PinyinToMisaki.GetToneArrow(Tone.Third));
        }

        [Fact]
        public void ToneMapping_Fourth_ReturnsSouthEastArrow()
        {
            // 第4声 去声 ↘ U+2198 SOUTH EAST ARROW
            Assert.Equal("\u2198", PinyinToMisaki.GetToneArrow(Tone.Fourth));
        }

        // ============================================================
        // Y/W 複合韻母マッピング (23 エントリ)
        // PinyinParser は "wang" を Initial.W + Final.Ang に parse する。
        // Misaki の "uang" とは違う構造なので、ConvertSyllable で
        // (Initial.W, Final.Ang) → ("wa", "ŋ", omitInitial=false) 等の変換を行う。
        // ============================================================

        // ── Y + 開口/齊齒系 ──

        [Fact]
        public void YWCompound_YA_ReturnsIaPattern()
        {
            // ya (Y + A) → ia (j + a) 展開、initial は省略しない
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.A, out var result));
            Assert.Equal(("ja", "", false), result);
        }

        [Fact]
        public void YWCompound_YAn_ReturnsIanPattern()
        {
            // yan → ian → jɛn: prefix "jɛ", suffix "n"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.An, out var result));
            Assert.Equal(("j\u025B", "n", false), result);
        }

        [Fact]
        public void YWCompound_YAng_ReturnsIangPattern()
        {
            // yang → iang → jaŋ: prefix "ja", suffix "ŋ"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ang, out var result));
            Assert.Equal(("ja", "\u014B", false), result);
        }

        [Fact]
        public void YWCompound_YAo_ReturnsIaoPattern()
        {
            // yao → iao → jau: prefix "jau", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ao, out var result));
            Assert.Equal(("jau", "", false), result);
        }

        [Fact]
        public void YWCompound_YE_ReturnsIePattern()
        {
            // ye → ie → je: prefix "je", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.E, out var result));
            Assert.Equal(("je", "", false), result);
        }

        [Fact]
        public void YWCompound_YI_ReturnsIPatternWithInitialOmitted()
        {
            // yi → i: j は省略 (omitInitial=true)
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.I, out var result));
            Assert.Equal(("i", "", true), result);
        }

        [Fact]
        public void YWCompound_YIn_ReturnsInPatternWithInitialOmitted()
        {
            // yin → in: j は省略
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.In, out var result));
            Assert.Equal(("i", "n", true), result);
        }

        [Fact]
        public void YWCompound_YIng_ReturnsIngPatternWithInitialOmitted()
        {
            // ying → iŋ: j は省略
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ing, out var result));
            Assert.Equal(("i", "\u014B", true), result);
        }

        [Fact]
        public void YWCompound_YOng_ReturnsIongPattern()
        {
            // yong → iong → jʊŋ: prefix "jʊ", suffix "ŋ"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ong, out var result));
            Assert.Equal(("j\u028A", "\u014B", false), result);
        }

        [Fact]
        public void YWCompound_YOu_ReturnsIuPattern()
        {
            // you → iu (iou) → jou: prefix "jou", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ou, out var result));
            Assert.Equal(("jou", "", false), result);
        }

        // ── Y + 撮口系 ──

        [Fact]
        public void YWCompound_YV_ReturnsVPatternWithInitialOmitted()
        {
            // yu → ü → y (U+0079): ɥ は省略
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.V, out var result));
            Assert.Equal(("y", "", true), result);
        }

        [Fact]
        public void YWCompound_YVe_ReturnsVePattern()
        {
            // yue → üe → ɥe: prefix "ɥe", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ve, out var result));
            Assert.Equal(("\u0265e", "", false), result);
        }

        [Fact]
        public void YWCompound_YVan_ReturnsVanPattern()
        {
            // yuan → üan → ɥɛn: prefix "ɥɛ", suffix "n"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Van, out var result));
            Assert.Equal(("\u0265\u025B", "n", false), result);
        }

        [Fact]
        public void YWCompound_YVn_ReturnsVnPatternWithInitialOmitted()
        {
            // yun → ün → yn: ɥ は省略
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Vn, out var result));
            Assert.Equal(("y", "n", true), result);
        }

        // ── W + 開口/合口系 ──

        [Fact]
        public void YWCompound_WA_ReturnsUaPattern()
        {
            // wa → ua → wa: prefix "wa", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.A, out var result));
            Assert.Equal(("wa", "", false), result);
        }

        [Fact]
        public void YWCompound_WAi_ReturnsUaiPattern()
        {
            // wai → uai → wai: prefix "wai", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.Ai, out var result));
            Assert.Equal(("wai", "", false), result);
        }

        [Fact]
        public void YWCompound_WAn_ReturnsUanPattern()
        {
            // wan → uan → wan: prefix "wa", suffix "n"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.An, out var result));
            Assert.Equal(("wa", "n", false), result);
        }

        [Fact]
        public void YWCompound_WAng_ReturnsUangPattern()
        {
            // wang → uang → waŋ: prefix "wa", suffix "ŋ"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.Ang, out var result));
            Assert.Equal(("wa", "\u014B", false), result);
        }

        [Fact]
        public void YWCompound_WEi_ReturnsUiPattern()
        {
            // wei → ui (uei) → wei: prefix "wei", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.Ei, out var result));
            Assert.Equal(("wei", "", false), result);
        }

        [Fact]
        public void YWCompound_WEn_ReturnsUnPattern()
        {
            // wen → un (uen) → wən: prefix "wə", suffix "n"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.En, out var result));
            Assert.Equal(("w\u0259", "n", false), result);
        }

        [Fact]
        public void YWCompound_WEng_ReturnsUengPattern()
        {
            // weng → ueng → wəŋ: prefix "wə", suffix "ŋ"
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.Eng, out var result));
            Assert.Equal(("w\u0259", "\u014B", false), result);
        }

        [Fact]
        public void YWCompound_WO_ReturnsUoPattern()
        {
            // wo → uo → wo: prefix "wo", suffix ""
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.O, out var result));
            Assert.Equal(("wo", "", false), result);
        }

        [Fact]
        public void YWCompound_WU_ReturnsUPatternWithInitialOmitted()
        {
            // wu → u: w は省略 (omitInitial=true)
            Assert.True(PinyinToMisaki.TryGetYWCompound(Initial.W, Final.U, out var result));
            Assert.Equal(("u", "", true), result);
        }

        // ── miss ケース: 非 Y/W initial は false を返す ──

        [Fact]
        public void YWCompound_NonYWInitial_ReturnsFalse()
        {
            // Initial.B + Final.A は Y/W テーブルに載らないので false
            Assert.False(PinyinToMisaki.TryGetYWCompound(Initial.B, Final.A, out var _));
        }

        [Fact]
        public void YWCompound_None_ReturnsFalse()
        {
            Assert.False(PinyinToMisaki.TryGetYWCompound(Initial.None, Final.A, out var _));
        }

        [Fact]
        public void YWCompound_Y_NoMatchingFinal_ReturnsFalse()
        {
            // Y + Ei のような未定義の組み合わせはテーブルにないので false
            Assert.False(PinyinToMisaki.TryGetYWCompound(Initial.Y, Final.Ei, out _));
        }

        // ============================================================
        // 網羅性テスト
        // ============================================================

        /// <summary>全 Initial (None/Y/W を除く) がマッピングテーブルにエントリを持つこと。</summary>
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
        public void InitialMapping_HasNonEmptyEntryForAllNonSemivowelInitials(Initial initial)
        {
            // Y/W 以外の None 以外の全 Initial は空文字を返してはならない
            Assert.NotEqual(string.Empty, PinyinToMisaki.GetInitialMisaki(initial));
        }

        /// <summary>全 Final (None を除く) が Prefix/Suffix マッピングテーブルにエントリを持つこと。</summary>
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
        public void FinalMapping_HasNonNullEntryForAllNonNoneFinals(Final final_)
        {
            // None 以外の全 Final は Prefix が null/empty でないこと
            var result = PinyinToMisaki.GetFinalMisaki(final_);
            Assert.NotNull(result.Prefix);
            Assert.NotNull(result.Suffix);
            // Prefix のみは必ず非空である (Suffix は空でも良い、例: Final.A の ("a", ""))
            Assert.NotEqual(string.Empty, result.Prefix);
        }

        /// <summary>全 Final に U+032F (COMBINING INVERTED BREVE BELOW) が含まれないこと。</summary>
        [Theory]
        [InlineData(Final.Ai)]
        [InlineData(Final.Ei)]
        [InlineData(Final.Ao)]
        [InlineData(Final.Ou)]
        [InlineData(Final.Iao)]
        [InlineData(Final.Iu)]
        [InlineData(Final.Uai)]
        [InlineData(Final.Ui)]
        [InlineData(Final.Ong)]
        [InlineData(Final.Iong)]
        public void FinalMapping_NoCombiningInvertedBreveBelow(Final final_)
        {
            // U+032F は Kokoro vocab 非含有のため、どの韻母テンプレにも含まれてはならない。
            var result = PinyinToMisaki.GetFinalMisaki(final_);
            Assert.DoesNotContain("\u032F", result.Prefix);
            Assert.DoesNotContain("\u032F", result.Suffix);
        }

        /// <summary>全 Tone に対して GetToneArrow が呼び出せること (例外を投げない)。</summary>
        [Theory]
        [InlineData(Tone.Neutral)]
        [InlineData(Tone.First)]
        [InlineData(Tone.Second)]
        [InlineData(Tone.Third)]
        [InlineData(Tone.Fourth)]
        public void ToneMapping_AllTonesCanBeQueried(Tone tone)
        {
            // 例外を投げないこと + 戻り値が null でないこと
            var arrow = PinyinToMisaki.GetToneArrow(tone);
            Assert.NotNull(arrow);
        }

        // ============================================================
        // PinyinToIpa との差分検証 (重要な差分のみ、構造テスト)
        // ============================================================
        // 既存 PinyinToIpa.cs は internal だが InternalsVisibleTo により呼び出し可能。
        // 本質的な差分のみサンプリング検証する。

        [Fact]
        public void Convert_Ji_DiffersFromStandardIpa()
        {
            // Misaki: "ʨi→"  (ligature + 声調中間)
            // Standard: "tɕi˥" のような TSS-letter 表現
            // 少なくとも出力が非空で、かつ標準IPA と異なることを確認。
            var misaki = PinyinToMisaki.Convert("jī");
            var standard = PinyinToIpa.Convert("jī");
            Assert.NotEqual(string.Empty, misaki);
            Assert.NotEqual(string.Empty, standard);
            Assert.NotEqual(standard, misaki);
        }

        [Fact]
        public void Convert_Ji_ContainsTcLigature()
        {
            // Misaki 出力は U+02A8 (ʨ) を含む。標準IPA は tɕ (U+0074 U+0255)。
            var misaki = PinyinToMisaki.Convert("jī");
            Assert.Contains("\u02A8", misaki);
        }

        [Fact]
        public void Convert_Zhi_ContainsTsRetroflexLigature()
        {
            // Misaki 出力は U+AB67 (ꭧ) を含む。標準IPA は ʈʂ (U+0288 U+0282)。
            var misaki = PinyinToMisaki.Convert("zhī");
            Assert.Contains("\uAB67", misaki);
        }

        [Fact]
        public void Convert_Zhi_ContainsApicalVowel()
        {
            // zhi の母音は apical ɨ (U+0268)。標準IPA の ɻ̩ (U+027B U+0329) ではない。
            var misaki = PinyinToMisaki.Convert("zhī");
            Assert.Contains("\u0268", misaki);
            Assert.DoesNotContain("\u0329", misaki); // COMBINING VERTICAL LINE BELOW は含まない
        }

        [Fact]
        public void Convert_Zi_ContainsApicalVowel()
        {
            // zi も同じ apical ɨ (U+0268) を使う。歯茎/そり舌で共通。
            var misaki = PinyinToMisaki.Convert("zī");
            Assert.Contains("\u0268", misaki);
            Assert.DoesNotContain("\u0329", misaki);
        }
    }
}
