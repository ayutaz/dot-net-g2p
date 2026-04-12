using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToMisaki.Convert / ConvertSyllable の単体テスト。
    /// 声母・韻母・声調・半母音省略・そり舌/歯茎母音など、
    /// PinyinToIpa と同一の変換ロジックが Misaki マッピングでも正しく機能することを検証する。
    /// </summary>
    public class PinyinToMisakiConvertTests
    {
        // ===== 声調マーカーのテスト（4声 + 軽声） =====

        [Theory]
        [InlineData("mā", "ma\u2192")]   // 1声 →
        [InlineData("má", "ma\u2197")]   // 2声 ↗
        [InlineData("mǎ", "ma\u2193")]   // 3声 ↓
        [InlineData("mà", "ma\u2198")]   // 4声 ↘
        public void Convert_AllTones_ReturnsCorrectArrow(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("ma", "ma")]        // 声調記号なし = 軽声 → 矢印なし
        [InlineData("de", "t\u0264")]   // 助詞「的」の軽声読み → tɤ (矢印なし)
        public void Convert_NeutralTone_OmitsArrow(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("ma1", "ma\u2192")] // 数字声調形式 1声
        [InlineData("ma2", "ma\u2197")] // 数字声調形式 2声
        [InlineData("ma3", "ma\u2193")] // 数字声調形式 3声
        [InlineData("ma4", "ma\u2198")] // 数字声調形式 4声
        public void Convert_NumericToneFormat_NormalizedCorrectly(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ===== includeTones=false で声調を省略 =====

        [Theory]
        [InlineData("mā", "ma")]
        [InlineData("nǐ", "ni")]
        [InlineData("hǎo", "xau\u032F")]
        [InlineData("wū", "u")]         // w + u → u (半母音 w 省略)
        public void Convert_IncludeTonesFalse_OmitsToneMarker(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin, includeTones: false));
        }

        // ===== Misaki 固有の声母差異 (j/q/z/c) =====

        [Fact]
        public void Convert_J_UsesTcLigature()
        {
            // 標準IPA: "tɕi˥˥" → Misaki: "ʨi→"
            Assert.Equal("\u02A8i\u2192", PinyinToMisaki.Convert("jī"));
        }

        [Fact]
        public void Convert_Q_UsesTcLigatureWithAspiration()
        {
            // 標準IPA: "tɕʰi˥˥" → Misaki: "ʨʰi→"
            Assert.Equal("\u02A8\u02B0i\u2192", PinyinToMisaki.Convert("qī"));
        }

        [Fact]
        public void Convert_Z_UsesTsLigatureBeforeVowel()
        {
            // 標準IPA: "tsa˥˥" → Misaki: "ʦa→"
            Assert.Equal("\u02A6a\u2192", PinyinToMisaki.Convert("zā"));
        }

        [Fact]
        public void Convert_C_UsesTsLigatureWithAspiration()
        {
            // 標準IPA: "tsʰa˥˥" → Misaki: "ʦʰa→"
            Assert.Equal("\u02A6\u02B0a\u2192", PinyinToMisaki.Convert("cā"));
        }

        // ===== Misaki 固有の韻母差異 (二重母音の非音節化符号) =====

        [Theory]
        [InlineData("bái", "pai\u032F\u2197")]     // b+ai 2声 → pai̯↗
        [InlineData("mèi", "mei\u032F\u2198")]     // m+ei 4声 → mei̯↘
        [InlineData("māo", "mau\u032F\u2192")]     // m+ao 1声 → mau̯→
        [InlineData("dòu", "tou\u032F\u2198")]     // d+ou 4声 → tou̯↘
        public void Convert_Diphthongs_UseNonSyllabicMark(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("miáo", "miau\u032F\u2197")]   // m+iao 2声 → miau̯↗
        [InlineData("liù", "liou\u032F\u2198")]    // l+iu(iou) 4声 → liou̯↘
        [InlineData("guāi", "kuai\u032F\u2192")]   // g+uai 1声 → kuai̯→
        [InlineData("duì", "tuei\u032F\u2198")]    // d+ui(uei) 4声 → tuei̯↘
        public void Convert_ComplexDiphthongs_UseNonSyllabicMark(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Fact]
        public void Convert_Ong_UsesNonSyllabicUBeforeNg()
        {
            // t + ong + 1声 → t + u̯ŋ + → = "tu̯ŋ→"
            Assert.Equal("tu\u032F\u014B\u2192", PinyinToMisaki.Convert("dōng"));
        }

        [Fact]
        public void Convert_Iong_UsesNonSyllabicUBeforeNg()
        {
            // x + iong + 2声 → ɕ + iu̯ŋ + ↗ = "ɕiu̯ŋ↗"
            Assert.Equal("\u0255iu\u032F\u014B\u2197", PinyinToMisaki.Convert("xióng"));
        }

        // ===== そり舌母音 (zh/ch/sh/r + i) =====

        [Theory]
        [InlineData("zhī", "\u0288\u0282\u027B\u0329\u2192")]    // ʈʂɻ̩→
        [InlineData("chī", "\u0288\u0282\u02B0\u027B\u0329\u2192")] // ʈʂʰɻ̩→
        [InlineData("shī", "\u0282\u027B\u0329\u2192")]           // ʂɻ̩→
        [InlineData("rì",  "\u027B\u027B\u0329\u2198")]           // ɻɻ̩↘
        public void Convert_RetroflexPlusI_UsesRetroflexApical(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ===== 歯茎母音 (z/c/s + i) =====

        [Theory]
        [InlineData("zī", "\u02A6\u0279\u0329\u2192")]            // ʦɹ̩→ (Misaki 合字 ʦ)
        [InlineData("cī", "\u02A6\u02B0\u0279\u0329\u2192")]      // ʦʰɹ̩→
        [InlineData("sī", "s\u0279\u0329\u2192")]                 // sɹ̩→
        public void Convert_AlveolarPlusI_UsesAlveolarApical(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ===== 半母音省略 (y/w + 対応韻母) =====

        [Theory]
        [InlineData("yī",   "i\u2192")]             // y + i → i (半母音省略)
        [InlineData("yīn",  "in\u2192")]            // y + in → in
        [InlineData("yīng", "i\u014B\u2192")]       // y + ing → iŋ
        public void Convert_YPlusIFinals_OmitsSemivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("yū",    "y\u2192")]            // y + ü → y (撮口呼は PinyinParser が Final.V として解釈)
        [InlineData("yuē",   "y\u025B\u2192")]      // y + üe → yɛ
        [InlineData("yuán",  "yan\u2197")]          // y + üan → yan
        [InlineData("yūn",   "yn\u2192")]           // y + ün → yn
        public void Convert_YPlusUFinals_OmitsSemivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("wū", "u\u2192")]               // w + u → u (半母音 w 省略)
        [InlineData("wù", "u\u2198")]               // w + u 4声 → u↘
        public void Convert_WPlusUFinals_OmitsSemivowel(string pinyin, string expected)
        {
            // PinyinParser は "wen" を Initial.W + Final.En としてパースするため、
            // 半母音省略は "wu" 系列のみが対象となる。
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("yā",  "ja\u2192")]             // y + a → ja (省略しない)
        [InlineData("wǒ",  "wo\u2193")]             // w + o → wo (PinyinParser: W + O)
        [InlineData("wài", "wai\u032F\u2198")]      // w + ai → wai̯ (PinyinParser: W + Ai)
        [InlineData("wēn", "w\u0259n\u2192")]       // w + en → wən (PinyinParser: W + En)
        public void Convert_YWPlusOtherFinals_KeepsSemivowel(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToMisaki.Convert(pinyin));
        }

        // ===== Issue #56 由来の参照例 =====

        [Fact]
        public void Convert_NiHao_ReturnsMisakiCompatibleOutput()
        {
            // PinyinToMisaki は個別音節のみ変換する（声調変調なし）
            // nǐ (3声) → ni↓
            // hǎo (3声) → xau̯↓
            Assert.Equal("ni\u2193", PinyinToMisaki.Convert("nǐ"));
            Assert.Equal("xau\u032F\u2193", PinyinToMisaki.Convert("hǎo"));
        }

        // ===== エッジケース =====

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Convert_NullOrEmpty_ReturnsEmpty(string pinyin)
        {
            Assert.Equal(string.Empty, PinyinToMisaki.Convert(pinyin));
        }

        [Theory]
        [InlineData("xyz")]         // 不正ピンイン
        [InlineData("123")]         // 数字のみ
        [InlineData("!!!")]         // 記号のみ
        public void Convert_InvalidPinyin_ReturnsEmpty(string pinyin)
        {
            Assert.Equal(string.Empty, PinyinToMisaki.Convert(pinyin));
        }

        // ===== ConvertSyllable 直接テスト =====

        [Fact]
        public void ConvertSyllable_BasicCase_ReturnsExpected()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.First);
            Assert.Equal("ma\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        [Fact]
        public void ConvertSyllable_WithoutTone_OmitsToneArrow()
        {
            var syllable = new PinyinSyllable(Initial.N, Final.I, Tone.Third);
            Assert.Equal("ni", PinyinToMisaki.ConvertSyllable(syllable, includeTones: false));
        }

        [Fact]
        public void ConvertSyllable_NeutralTone_NoArrowRegardlessOfFlag()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.Neutral);
            Assert.Equal("ma", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
            Assert.Equal("ma", PinyinToMisaki.ConvertSyllable(syllable, includeTones: false));
        }

        [Fact]
        public void ConvertSyllable_ZeroInitial_OnlyFinal()
        {
            // ゼロ声母 (a, e, o など): 韻母のみ
            var syllable = new PinyinSyllable(Initial.None, Final.A, Tone.First);
            Assert.Equal("a\u2192", PinyinToMisaki.ConvertSyllable(syllable, includeTones: true));
        }

        // ===== PinyinToIpa との差分が期待通りに現れることの確認 =====

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtJInitial()
        {
            string misaki = PinyinToMisaki.Convert("jī", includeTones: false);
            string standardIpa = PinyinToIpa.Convert("jī", includeTones: false);
            // Misaki: "\u02A8i" (ʨi), 標準IPA: "t\u0255i" (tɕi)
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("\u02A8i", misaki);
            Assert.Equal("t\u0255i", standardIpa);
        }

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtAiFinal()
        {
            string misaki = PinyinToMisaki.Convert("bái", includeTones: false);
            string standardIpa = PinyinToIpa.Convert("bái", includeTones: false);
            // Misaki: "pai̯" (pai + U+032F), 標準IPA: "paɪ" (pa + U+026A)
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("pai\u032F", misaki);
            Assert.Equal("pa\u026A", standardIpa);
        }

        [Fact]
        public void Convert_DiffersFromStandardIpa_AtToneMarker()
        {
            string misaki = PinyinToMisaki.Convert("mā", includeTones: true);
            string standardIpa = PinyinToIpa.Convert("mā", includeTones: true);
            // Misaki: "ma→" (U+2192), 標準IPA: "ma˥˥" (U+02E5 U+02E5)
            Assert.NotEqual(standardIpa, misaki);
            Assert.Equal("ma\u2192", misaki);
            Assert.Equal("ma\u02E5\u02E5", standardIpa);
        }
    }
}
