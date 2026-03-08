using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinToZhuyin の単体テスト。
    /// ピンイン→注音符号変換の全声母・韻母・声調・特殊ケースを検証する。
    /// </summary>
    public class ZhuyinConversionTests
    {
        // ===== 全声母テスト =====

        [Theory]
        [InlineData("bā", "ㄅㄚ")]       // b → ㄅ
        [InlineData("pá", "ㄆㄚˊ")]      // p → ㄆ
        [InlineData("mā", "ㄇㄚ")]       // m → ㄇ
        [InlineData("fā", "ㄈㄚ")]       // f → ㄈ
        [InlineData("dā", "ㄉㄚ")]       // d → ㄉ
        [InlineData("tā", "ㄊㄚ")]       // t → ㄊ
        [InlineData("nā", "ㄋㄚ")]       // n → ㄋ
        [InlineData("lā", "ㄌㄚ")]       // l → ㄌ
        [InlineData("gā", "ㄍㄚ")]       // g → ㄍ
        [InlineData("kā", "ㄎㄚ")]       // k → ㄎ
        [InlineData("hā", "ㄏㄚ")]       // h → ㄏ
        public void Convert_AllInitials_Group1(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("jī", "ㄐㄧ")]       // j → ㄐ
        [InlineData("qī", "ㄑㄧ")]       // q → ㄑ
        [InlineData("xī", "ㄒㄧ")]       // x → ㄒ
        public void Convert_AllInitials_Palatal(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("zhā", "ㄓㄚ")]      // zh → ㄓ
        [InlineData("chā", "ㄔㄚ")]      // ch → ㄔ
        [InlineData("shā", "ㄕㄚ")]      // sh → ㄕ
        [InlineData("rě", "ㄖㄜˇ")]      // r → ㄖ
        public void Convert_AllInitials_Retroflex(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("zā", "ㄗㄚ")]       // z → ㄗ
        [InlineData("cā", "ㄘㄚ")]       // c → ㄘ
        [InlineData("sā", "ㄙㄚ")]       // s → ㄙ
        public void Convert_AllInitials_Dental(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== 代表韻母テスト =====

        [Theory]
        [InlineData("ā", "ㄚ")]           // a → ㄚ
        [InlineData("ō", "ㄛ")]           // o → ㄛ
        [InlineData("ē", "ㄜ")]           // e → ㄜ
        [InlineData("ér", "ㄦˊ")]         // er → ㄦ
        public void Convert_SimpleFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("āi", "ㄞ")]          // ai → ㄞ
        [InlineData("éi", "ㄟˊ")]         // ei → ㄟ
        [InlineData("āo", "ㄠ")]          // ao → ㄠ
        [InlineData("ōu", "ㄡ")]          // ou → ㄡ
        public void Convert_CompoundFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("ān", "ㄢ")]          // an → ㄢ
        [InlineData("ēn", "ㄣ")]          // en → ㄣ
        [InlineData("āng", "ㄤ")]         // ang → ㄤ
        [InlineData("ēng", "ㄥ")]         // eng → ㄥ
        public void Convert_NasalFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== 声調テスト =====

        [Theory]
        [InlineData("mā", "ㄇㄚ")]       // 1声: 声調マーカーなし
        [InlineData("má", "ㄇㄚˊ")]      // 2声: ˊ 末尾
        [InlineData("mǎ", "ㄇㄚˇ")]      // 3声: ˇ 末尾
        [InlineData("mà", "ㄇㄚˋ")]      // 4声: ˋ 末尾
        [InlineData("ma", "˙ㄇㄚ")]       // 軽声: ˙ 先頭
        public void Convert_AllTones(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Fact]
        public void Convert_IncludeTonesfalse_NoToneMarker()
        {
            // 声調マーカーなし
            Assert.Equal("ㄇㄚ", PinyinToZhuyin.Convert("mā", false));
            Assert.Equal("ㄇㄚ", PinyinToZhuyin.Convert("má", false));
            Assert.Equal("ㄇㄚ", PinyinToZhuyin.Convert("mǎ", false));
            Assert.Equal("ㄇㄚ", PinyinToZhuyin.Convert("mà", false));
            Assert.Equal("ㄇㄚ", PinyinToZhuyin.Convert("ma", false));
        }

        // ===== zh/ch/sh/r/z/c/s + i 空韻母テスト =====

        [Theory]
        [InlineData("zhī", "ㄓ")]         // zhi → ㄓ（iは空韻母）
        [InlineData("chī", "ㄔ")]         // chi → ㄔ
        [InlineData("shī", "ㄕ")]         // shi → ㄕ
        [InlineData("rì", "ㄖˋ")]         // ri → ㄖ + 4声
        [InlineData("zī", "ㄗ")]          // zi → ㄗ
        [InlineData("cí", "ㄘˊ")]         // ci → ㄘ + 2声
        [InlineData("sī", "ㄙ")]          // si → ㄙ
        public void Convert_RetroflexDental_EmptyFinal(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== j/q/x + u → ü テスト =====

        [Theory]
        [InlineData("jū", "ㄐㄩ")]       // ju → ㄐㄩ (u→ü)
        [InlineData("qū", "ㄑㄩ")]       // qu → ㄑㄩ
        [InlineData("xū", "ㄒㄩ")]       // xu → ㄒㄩ
        [InlineData("jùn", "ㄐㄩㄣˋ")]   // jun → ㄐㄩㄣ
        [InlineData("quán", "ㄑㄩㄢˊ")]  // quan → ㄑㄩㄢ
        [InlineData("xuě", "ㄒㄩㄝˇ")]   // xue → ㄒㄩㄝ
        public void Convert_Palatal_UToV(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== ゼロ声母テスト =====

        [Theory]
        [InlineData("yī", "ㄧ")]          // yi → ㄧ
        [InlineData("wū", "ㄨ")]          // wu → ㄨ
        [InlineData("yǔ", "ㄩˇ")]        // yu → ㄩ
        public void Convert_ZeroInitial_Basic(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("yā", "ㄧㄚ")]       // ya → ㄧㄚ
        [InlineData("yè", "ㄧㄝˋ")]      // ye → ㄧㄝ
        [InlineData("yáo", "ㄧㄠˊ")]     // yao → ㄧㄠ
        [InlineData("yóu", "ㄧㄡˊ")]     // you → ㄧㄡ
        [InlineData("yán", "ㄧㄢˊ")]     // yan → ㄧㄢ
        [InlineData("yīn", "ㄧㄣ")]      // yin → ㄧㄣ
        [InlineData("yáng", "ㄧㄤˊ")]    // yang → ㄧㄤ
        [InlineData("yīng", "ㄧㄥ")]     // ying → ㄧㄥ
        public void Convert_ZeroInitial_Y(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("wā", "ㄨㄚ")]       // wa → ㄨㄚ
        [InlineData("wǒ", "ㄨㄛˇ")]     // wo → ㄨㄛ
        [InlineData("wài", "ㄨㄞˋ")]     // wai → ㄨㄞ
        [InlineData("wéi", "ㄨㄟˊ")]     // wei → ㄨㄟ
        [InlineData("wān", "ㄨㄢ")]      // wan → ㄨㄢ
        [InlineData("wén", "ㄨㄣˊ")]     // wen → ㄨㄣ
        [InlineData("wāng", "ㄨㄤ")]     // wang → ㄨㄤ
        public void Convert_ZeroInitial_W(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        [Theory]
        [InlineData("yuē", "ㄩㄝ")]      // yue → ㄩㄝ
        [InlineData("yuán", "ㄩㄢˊ")]    // yuan → ㄩㄢ
        [InlineData("yún", "ㄩㄣˊ")]     // yun → ㄩㄣ
        [InlineData("yǒng", "ㄩㄥˇ")]    // yong → ㄩㄥ
        public void Convert_ZeroInitial_Yu(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== ong 韻母テスト =====

        [Theory]
        [InlineData("dōng", "ㄉㄨㄥ")]   // dong → ㄉㄨㄥ
        [InlineData("tóng", "ㄊㄨㄥˊ")]  // tong → ㄊㄨㄥ
        [InlineData("gōng", "ㄍㄨㄥ")]   // gong → ㄍㄨㄥ
        [InlineData("hóng", "ㄏㄨㄥˊ")]  // hong → ㄏㄨㄥ
        [InlineData("zhōng", "ㄓㄨㄥ")]  // zhong → ㄓㄨㄥ
        public void Convert_OngFinal(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== i系韻母組み合わせ =====

        [Theory]
        [InlineData("jiā", "ㄐㄧㄚ")]    // jia → ㄐㄧㄚ
        [InlineData("liè", "ㄌㄧㄝˋ")]   // lie → ㄌㄧㄝ
        [InlineData("biǎo", "ㄅㄧㄠˇ")]  // biao → ㄅㄧㄠ
        [InlineData("liú", "ㄌㄧㄡˊ")]   // liu → ㄌㄧㄡ
        [InlineData("tiān", "ㄊㄧㄢ")]   // tian → ㄊㄧㄢ
        [InlineData("jīn", "ㄐㄧㄣ")]    // jin → ㄐㄧㄣ
        [InlineData("liáng", "ㄌㄧㄤˊ")] // liang → ㄌㄧㄤ
        [InlineData("míng", "ㄇㄧㄥˊ")]  // ming → ㄇㄧㄥ
        public void Convert_IFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== u系韻母組み合わせ =====

        [Theory]
        [InlineData("guā", "ㄍㄨㄚ")]    // gua → ㄍㄨㄚ
        [InlineData("guó", "ㄍㄨㄛˊ")]   // guo → ㄍㄨㄛ
        [InlineData("kuài", "ㄎㄨㄞˋ")]  // kuai → ㄎㄨㄞ
        [InlineData("guì", "ㄍㄨㄟˋ")]   // gui → ㄍㄨㄟ
        [InlineData("guān", "ㄍㄨㄢ")]   // guan → ㄍㄨㄢ
        [InlineData("gǔn", "ㄍㄨㄣˇ")]  // gun → ㄍㄨㄣ
        [InlineData("guāng", "ㄍㄨㄤ")]  // guang → ㄍㄨㄤ
        public void Convert_UFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== iong韻母テスト =====

        [Theory]
        [InlineData("xiōng", "ㄒㄩㄥ")]  // xiong → ㄒㄩㄥ (j/q/x + iong → ㄩㄥ)
        [InlineData("jiǒng", "ㄐㄩㄥˇ")]// jiong → ㄐㄩㄥ
        public void Convert_IongFinal(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== エッジケーステスト =====

        [Fact]
        public void Convert_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToZhuyin.Convert(""));
        }

        [Fact]
        public void Convert_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToZhuyin.Convert(null));
        }

        [Fact]
        public void Convert_NullWithTones_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PinyinToZhuyin.Convert(null, true));
            Assert.Equal(string.Empty, PinyinToZhuyin.Convert(null, false));
        }

        // ===== 完全な音節テスト（よく使う漢字のピンイン） =====

        [Theory]
        [InlineData("nǐ", "ㄋㄧˇ")]     // 你
        [InlineData("hǎo", "ㄏㄠˇ")]    // 好
        [InlineData("shì", "ㄕˋ")]       // 是 (sh+i → 空韻母)
        [InlineData("zhōng", "ㄓㄨㄥ")]  // 中
        [InlineData("guó", "ㄍㄨㄛˊ")]   // 国
        [InlineData("rén", "ㄖㄣˊ")]     // 人
        [InlineData("dà", "ㄉㄚˋ")]      // 大
        [InlineData("xué", "ㄒㄩㄝˊ")]   // 学
        public void Convert_CommonCharacters(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== ü系韻母テスト =====

        [Theory]
        [InlineData("lǜ", "ㄌㄩˋ")]     // 绿 lü → ㄌㄩ
        [InlineData("nǚ", "ㄋㄩˇ")]     // 女 nü → ㄋㄩ
        public void Convert_VFinal(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }

        // ===== zh+声調なしi以外の韻母 =====

        [Theory]
        [InlineData("zhuāng", "ㄓㄨㄤ")] // zhuang
        [InlineData("chuáng", "ㄔㄨㄤˊ")]// chuang
        [InlineData("shuāng", "ㄕㄨㄤ")] // shuang
        public void Convert_Retroflex_WithOtherFinals(string pinyin, string expected)
        {
            Assert.Equal(expected, PinyinToZhuyin.Convert(pinyin));
        }
    }
}
