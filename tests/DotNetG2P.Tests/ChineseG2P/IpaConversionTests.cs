using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToIpa の単体テスト。
    /// ピンイン→IPA変換の全声母・代表韻母・声調・特殊ケースを検証する。
    /// </summary>
    public class IpaConversionTests
    {
        // ===== 全声母のIPA変換 =====

        [Theory]
        [InlineData("bā", "pa\u02E5\u02E5")]           // b→p, 1声
        [InlineData("pá", "p\u02B0a\u02E7\u02E5")]     // p→pʰ, 2声
        [InlineData("mǎ", "ma\u02E8\u02E9\u02E6")]     // m→m, 3声
        [InlineData("fà", "fa\u02E5\u02E9")]            // f→f, 4声
        public void Convert_LabialInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("dā", "ta\u02E5\u02E5")]           // d→t
        [InlineData("tā", "t\u02B0a\u02E5\u02E5")]     // t→tʰ
        [InlineData("nā", "na\u02E5\u02E5")]           // n→n
        [InlineData("lā", "la\u02E5\u02E5")]           // l→l
        public void Convert_AlveolarInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("gā", "ka\u02E5\u02E5")]            // g→k
        [InlineData("kā", "k\u02B0a\u02E5\u02E5")]      // k→kʰ
        [InlineData("hā", "xa\u02E5\u02E5")]            // h→x
        public void Convert_VelarInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("jī", "t\u0255i\u02E5\u02E5")]       // j→tɕ
        [InlineData("qī", "t\u0255\u02B0i\u02E5\u02E5")] // q→tɕʰ
        [InlineData("xī", "\u0255i\u02E5\u02E5")]        // x→ɕ
        public void Convert_PalatalInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("zhā", "\u0288\u0282a\u02E5\u02E5")]       // zh→ʈʂ
        [InlineData("chā", "\u0288\u0282\u02B0a\u02E5\u02E5")] // ch→ʈʂʰ
        [InlineData("shā", "\u0282a\u02E5\u02E5")]        // sh→ʂ
        [InlineData("rè", "\u027B\u0264\u02E5\u02E9")]    // r→ɻ, e→ɤ
        public void Convert_RetroflexInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("zā", "tsa\u02E5\u02E5")]           // z→ts
        [InlineData("cā", "ts\u02B0a\u02E5\u02E5")]     // c→tsʰ
        [InlineData("sā", "sa\u02E5\u02E5")]            // s→s
        public void Convert_AlveolarAffricateInitials_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== 代表韻母のIPA変換 =====

        [Theory]
        [InlineData("ā", "a\u02E5\u02E5")]              // a→a
        [InlineData("ō", "o\u02E5\u02E5")]              // o→o
        [InlineData("ē", "\u0264\u02E5\u02E5")]         // e→ɤ
        public void Convert_SimpleVowels_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("āi", "a\u026A\u02E5\u02E5")]       // ai→aɪ
        [InlineData("éi", "e\u026A\u02E7\u02E5")]       // ei→eɪ
        [InlineData("āo", "a\u028A\u02E5\u02E5")]       // ao→aʊ
        [InlineData("ōu", "o\u028A\u02E5\u02E5")]       // ou→oʊ
        public void Convert_CompoundVowels_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("ān", "an\u02E5\u02E5")]            // an→an
        [InlineData("ēn", "\u0259n\u02E5\u02E5")]       // en→ən
        [InlineData("āng", "a\u014B\u02E5\u02E5")]      // ang→aŋ
        [InlineData("ēng", "\u0259\u014B\u02E5\u02E5")] // eng→əŋ
        public void Convert_NasalFinals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("ér", "\u0259\u027B\u02E7\u02E5")]  // er→əɻ
        public void Convert_ErFinal_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== i系韻母 =====

        [Theory]
        [InlineData("biē", "pi\u025B\u02E5\u02E5")]     // ie→iɛ
        [InlineData("biāo", "pia\u028A\u02E5\u02E5")]   // iao→iaʊ
        [InlineData("liú", "lio\u028A\u02E7\u02E5")]    // iu→ioʊ
        [InlineData("biān", "pi\u025Bn\u02E5\u02E5")]   // ian→iɛn
        [InlineData("bīn", "pin\u02E5\u02E5")]          // in→in
        [InlineData("liáng", "lia\u014B\u02E7\u02E5")]  // iang→iaŋ
        [InlineData("bīng", "pi\u014B\u02E5\u02E5")]    // ing→iŋ
        public void Convert_ISeriesFinals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== u系韻母 =====

        [Theory]
        [InlineData("guā", "kua\u02E5\u02E5")]          // ua→ua
        [InlineData("guō", "kuo\u02E5\u02E5")]          // uo→uo
        [InlineData("guāi", "kua\u026A\u02E5\u02E5")]   // uai→uaɪ
        [InlineData("guī", "kue\u026A\u02E5\u02E5")]    // ui→ueɪ
        [InlineData("guān", "kuan\u02E5\u02E5")]        // uan→uan
        [InlineData("gǔn", "ku\u0259n\u02E8\u02E9\u02E6")] // un→uən
        [InlineData("guāng", "kua\u014B\u02E5\u02E5")]  // uang→uaŋ
        public void Convert_USeriesFinals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== ü系韻母 =====

        [Theory]
        [InlineData("lǜ", "ly\u02E5\u02E9")]           // ü→y (l + ü)
        [InlineData("nǚ", "ny\u02E8\u02E9\u02E6")]     // ü→y (n + ü)
        [InlineData("jū", "t\u0255y\u02E5\u02E5")]      // j + u → tɕy (j後のuはü)
        [InlineData("qū", "t\u0255\u02B0y\u02E5\u02E5")]// q + u → tɕʰy
        [InlineData("xū", "\u0255y\u02E5\u02E5")]       // x + u → ɕy
        public void Convert_VSeriesFinals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("juē", "t\u0255y\u025B\u02E5\u02E5")]    // j + ue → tɕyɛ
        [InlineData("juān", "t\u0255yan\u02E5\u02E5")]        // j + uan → tɕyan
        [InlineData("jūn", "t\u0255yn\u02E5\u02E5")]          // j + un → tɕyn
        public void Convert_PalatalWithVFinals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== zhi/chi/shi/ri/zi/ci/si の特殊 i (ɨ) =====

        [Theory]
        [InlineData("zhī", "\u0288\u0282\u027B\u0329\u02E5\u02E5")]       // zhi→ʈʂɻ̩
        [InlineData("chī", "\u0288\u0282\u02B0\u027B\u0329\u02E5\u02E5")] // chi→ʈʂʰɻ̩
        [InlineData("shī", "\u0282\u027B\u0329\u02E5\u02E5")]             // shi→ʂɻ̩
        [InlineData("rì", "\u027B\u027B\u0329\u02E5\u02E9")]              // ri→ɻɻ̩
        [InlineData("zī", "ts\u0279\u0329\u02E5\u02E5")]                  // zi→tsɹ̩
        [InlineData("cī", "ts\u02B0\u0279\u0329\u02E5\u02E5")]            // ci→tsʰɹ̩
        [InlineData("sī", "s\u0279\u0329\u02E5\u02E5")]                   // si→sɹ̩
        public void Convert_ApicalVowel_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== 声調テスト =====

        [Theory]
        [InlineData("mā", "ma\u02E5\u02E5")]                  // 1声 ˥˥
        [InlineData("má", "ma\u02E7\u02E5")]                  // 2声 ˧˥
        [InlineData("mǎ", "ma\u02E8\u02E9\u02E6")]            // 3声 ˨˩˦
        [InlineData("mà", "ma\u02E5\u02E9")]                  // 4声 ˥˩
        [InlineData("ma", "ma")]                               // 軽声 - マーカーなし
        public void Convert_AllTones_ReturnsCorrectToneLetters(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== 声調なしモード =====

        [Theory]
        [InlineData("mā", "ma")]
        [InlineData("má", "ma")]
        [InlineData("mǎ", "ma")]
        [InlineData("mà", "ma")]
        [InlineData("ma", "ma")]
        public void Convert_IncludeTonesFalse_OmitsToneLetters(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin, false));
        }

        // ===== ゼロ声母（声母なし） =====

        [Theory]
        [InlineData("ā", "a\u02E5\u02E5")]              // a
        [InlineData("ō", "o\u02E5\u02E5")]              // o
        [InlineData("ē", "\u0264\u02E5\u02E5")]         // e→ɤ
        [InlineData("āi", "a\u026A\u02E5\u02E5")]       // ai
        [InlineData("ān", "an\u02E5\u02E5")]            // an
        public void Convert_ZeroInitial_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== y/w 半母音の処理 =====

        [Theory]
        [InlineData("yī", "i\u02E5\u02E5")]             // yi→i (yのjは省略)
        [InlineData("yīn", "in\u02E5\u02E5")]           // yin→in
        [InlineData("yīng", "i\u014B\u02E5\u02E5")]     // ying→iŋ
        [InlineData("yā", "ja\u02E5\u02E5")]            // ya→ja (y+a系はjを出力)
        [InlineData("yáo", "ja\u028A\u02E7\u02E5")]     // yao→jaʊ
        [InlineData("yé", "j\u0264\u02E7\u02E5")]       // ye→jɤ (NOTE: PinyinParser parses ye as Y+E)
        public void Convert_YSemivowel_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        [Theory]
        [InlineData("wū", "u\u02E5\u02E5")]             // wu→u (wは省略)
        [InlineData("wā", "wa\u02E5\u02E5")]            // wa→wa (w+a系はwを出力)
        [InlineData("wǒ", "wo\u02E8\u02E9\u02E6")]      // wo→wo
        public void Convert_WSemivowel_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== 数字声調入力 =====

        [Theory]
        [InlineData("ma1", "ma\u02E5\u02E5")]           // 数字形式1声
        [InlineData("ma2", "ma\u02E7\u02E5")]           // 数字形式2声
        [InlineData("ma3", "ma\u02E8\u02E9\u02E6")]     // 数字形式3声
        [InlineData("ma4", "ma\u02E5\u02E9")]           // 数字形式4声
        public void Convert_ToneNumberInput_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== ong韻母 =====

        [Theory]
        [InlineData("dōng", "t\u028A\u014B\u02E5\u02E5")]   // dong→tʊŋ
        [InlineData("hóng", "x\u028A\u014B\u02E7\u02E5")]   // hong→xʊŋ
        public void Convert_OngFinal_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== iong韻母 =====

        [Theory]
        [InlineData("xiōng", "\u0255i\u028A\u014B\u02E5\u02E5")]  // xiong→ɕiʊŋ
        public void Convert_IongFinal_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== エッジケース =====

        [Fact]
        public void Convert_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToIpa.Convert(""));
        }

        [Fact]
        public void Convert_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToIpa.Convert(null!));
        }

        [Fact]
        public void Convert_InvalidPinyin_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToIpa.Convert("xyz"));
        }

        [Fact]
        public void Convert_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToIpa.Convert("   "));
        }

        // ===== 実用的な音節テスト =====

        [Theory]
        [InlineData("zhōng", "\u0288\u0282\u028A\u014B\u02E5\u02E5")]  // zhong→ʈʂʊŋ
        [InlineData("guó", "kuo\u02E7\u02E5")]                       // guo→kuo
        [InlineData("rén", "\u027B\u0259n\u02E7\u02E5")]             // ren→ɻən
        [InlineData("mín", "min\u02E7\u02E5")]                       // min→min
        [InlineData("gòng", "k\u028A\u014B\u02E5\u02E9")]            // gong→kʊŋ
        [InlineData("hé", "x\u0264\u02E7\u02E5")]                    // he→xɤ
        public void Convert_CommonSyllables_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== 全声母×a韻母の網羅テスト =====

        [Theory]
        [InlineData("ba", "pa")]
        [InlineData("pa", "p\u02B0a")]
        [InlineData("ma", "ma")]
        [InlineData("fa", "fa")]
        [InlineData("da", "ta")]
        [InlineData("ta", "t\u02B0a")]
        [InlineData("na", "na")]
        [InlineData("la", "la")]
        [InlineData("ga", "ka")]
        [InlineData("ka", "k\u02B0a")]
        [InlineData("ha", "xa")]
        [InlineData("za", "tsa")]
        [InlineData("ca", "ts\u02B0a")]
        [InlineData("sa", "sa")]
        public void Convert_AllInitialsWithA_NoTone_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin, false));
        }

        // ===== lüe/nüe テスト =====

        [Theory]
        [InlineData("lüè", "ly\u025B\u02E5\u02E9")]     // lüe→lyɛ
        [InlineData("nüè", "ny\u025B\u02E5\u02E9")]     // nüe→nyɛ
        public void Convert_LueNue_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== ConvertSyllable直接テスト =====

        [Fact]
        public void ConvertSyllable_BasicSyllable_ReturnsCorrectIpa()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.First);
            string result = PinyinToIpa.ConvertSyllable(syllable, true);
            Assert.Equal("ma\u02E5\u02E5", result);
        }

        [Fact]
        public void ConvertSyllable_NoTone_ReturnsWithoutMarker()
        {
            var syllable = new PinyinSyllable(Initial.M, Final.A, Tone.First);
            string result = PinyinToIpa.ConvertSyllable(syllable, false);
            Assert.Equal("ma", result);
        }

        // ===== yu系ゼロ声母テスト =====

        [Theory]
        [InlineData("yù", "y\u02E5\u02E9")]             // yu→y (ü)
        [InlineData("yuē", "y\u025B\u02E5\u02E5")]      // yue→yɛ
        [InlineData("yuán", "yan\u02E7\u02E5")]          // yuan→yan
        [InlineData("yún", "yn\u02E7\u02E5")]            // yun→yn
        public void Convert_YuSeries_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== R1修正: そり舌声母ʈʂの追加検証 =====

        [Theory]
        [InlineData("zhě", "\u0288\u0282\u0264\u02E8\u02E9\u02E6")]  // zhe→ʈʂɤ (3声)
        [InlineData("chéng", "\u0288\u0282\u02B0\u0259\u014B\u02E7\u02E5")] // cheng→ʈʂʰəŋ (2声)
        public void Convert_R1_RetroflexWithVowels_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== R1修正: iong韻母の追加検証 =====

        [Theory]
        [InlineData("jiōng", "t\u0255i\u028A\u014B\u02E5\u02E5")]  // jiong→tɕiʊŋ
        [InlineData("qióng", "t\u0255\u02B0i\u028A\u014B\u02E7\u02E5")] // qiong→tɕʰiʊŋ
        public void Convert_R1_IongWithAllPalatals_ReturnsCorrectIpa(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== R1修正: そり舌母音ɻ̩ の声調バリエーション =====

        [Theory]
        [InlineData("zhí", "\u0288\u0282\u027B\u0329\u02E7\u02E5")]   // zhi 2声→ʈʂɻ̩˧˥
        [InlineData("zhǐ", "\u0288\u0282\u027B\u0329\u02E8\u02E9\u02E6")] // zhi 3声→ʈʂɻ̩˨˩˦
        [InlineData("zhì", "\u0288\u0282\u027B\u0329\u02E5\u02E9")]   // zhi 4声→ʈʂɻ̩˥˩
        public void Convert_R1_RetroflexApical_ToneVariants(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }

        // ===== R1修正: 歯茎母音ɹ̩ の声調バリエーション =====

        [Theory]
        [InlineData("zí", "ts\u0279\u0329\u02E7\u02E5")]              // zi 2声→tsɹ̩˧˥
        [InlineData("cì", "ts\u02B0\u0279\u0329\u02E5\u02E9")]        // ci 4声→tsʰɹ̩˥˩
        [InlineData("sì", "s\u0279\u0329\u02E5\u02E9")]               // si 4声→sɹ̩˥˩
        public void Convert_R1_AlveolarApical_ToneVariants(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToIpa.Convert(pinyin));
        }
    }
}
