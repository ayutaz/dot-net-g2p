using DotNetG2P.Models;
using DotNetG2P.PhonemeConverter;

namespace DotNetG2P.Tests.PhonemeConverter
{
    public class MoraMappingTests
    {
        // ===== 基本モーラ: カタカナ→音素変換 =====

        [Theory]
        [InlineData("ア", "a")]
        [InlineData("イ", "i")]
        [InlineData("ウ", "u")]
        [InlineData("エ", "e")]
        [InlineData("オ", "o")]
        public void KatakanaToPhonemeString_Vowels_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("カ", "k a")]
        [InlineData("キ", "k i")]
        [InlineData("ク", "k u")]
        [InlineData("ケ", "k e")]
        [InlineData("コ", "k o")]
        public void KatakanaToPhonemeString_KaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("サ", "s a")]
        [InlineData("シ", "sh i")]
        [InlineData("ス", "s u")]
        [InlineData("セ", "s e")]
        [InlineData("ソ", "s o")]
        public void KatakanaToPhonemeString_SaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("タ", "t a")]
        [InlineData("チ", "ch i")]
        [InlineData("ツ", "ts u")]
        [InlineData("テ", "t e")]
        [InlineData("ト", "t o")]
        public void KatakanaToPhonemeString_TaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("ハ", "h a")]
        [InlineData("ヒ", "h i")]
        [InlineData("フ", "f u")]
        [InlineData("ヘ", "h e")]
        [InlineData("ホ", "h o")]
        public void KatakanaToPhonemeString_HaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== 拗音テスト =====

        [Theory]
        [InlineData("キャ", "ky a")]
        [InlineData("キュ", "ky u")]
        [InlineData("キョ", "ky o")]
        public void KatakanaToPhonemeString_KyaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("シャ", "sh a")]
        [InlineData("シュ", "sh u")]
        [InlineData("ショ", "sh o")]
        public void KatakanaToPhonemeString_ShaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("チャ", "ch a")]
        [InlineData("チュ", "ch u")]
        [InlineData("チョ", "ch o")]
        public void KatakanaToPhonemeString_ChaRow_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        [Theory]
        [InlineData("ニャ", "ny a")]
        [InlineData("ヒャ", "hy a")]
        [InlineData("ミャ", "my a")]
        [InlineData("リャ", "ry a")]
        [InlineData("ギャ", "gy a")]
        [InlineData("ビャ", "by a")]
        [InlineData("ピャ", "py a")]
        public void KatakanaToPhonemeString_OtherYouon_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== 特殊モーラテスト =====

        [Fact]
        public void KatakanaToPhonemeString_N_ReturnsNn()
        {
            Assert.Equal("N", MoraMapping.KatakanaToPhonemeString("ン"));
        }

        [Fact]
        public void KatakanaToPhonemeString_Xtsu_ReturnsCl()
        {
            Assert.Equal("cl", MoraMapping.KatakanaToPhonemeString("ッ"));
        }

        [Fact]
        public void KatakanaToPhonemeString_Long_ReturnsDash()
        {
            Assert.Equal("-", MoraMapping.KatakanaToPhonemeString("ー"));
        }

        // ===== 外来音テスト =====

        [Theory]
        [InlineData("ファ", "f a")]
        [InlineData("フィ", "f i")]
        [InlineData("フェ", "f e")]
        [InlineData("フォ", "f o")]
        [InlineData("ティ", "t i")]
        [InlineData("ディ", "d i")]
        [InlineData("ヴァ", "v a")]
        [InlineData("ヴィ", "v i")]
        [InlineData("ヴ", "v u")]
        public void KatakanaToPhonemeString_ForeignSounds_ReturnsCorrectPhonemes(string katakana, string expected)
        {
            Assert.Equal(expected, MoraMapping.KatakanaToPhonemeString(katakana));
        }

        // ===== 複合カタカナ→音素変換テスト =====

        [Fact]
        public void KatakanaToPhonemeString_Konnichiwa_ReturnsCorrectPhonemes()
        {
            Assert.Equal("k o N n i ch i w a", MoraMapping.KatakanaToPhonemeString("コンニチワ"));
        }

        [Fact]
        public void KatakanaToPhonemeString_Arigatou_ReturnsCorrectPhonemes()
        {
            Assert.Equal("a r i g a t o -", MoraMapping.KatakanaToPhonemeString("アリガトー"));
        }

        [Fact]
        public void KatakanaToPhonemeString_Gakkou_ReturnsCorrectPhonemes()
        {
            // ガッコー → g a cl k o -
            Assert.Equal("g a cl k o -", MoraMapping.KatakanaToPhonemeString("ガッコー"));
        }

        // ===== MorasToPhonemeString テスト =====

        [Fact]
        public void MorasToPhonemeString_EmptyList_ReturnsEmpty()
        {
            var moras = new List<Mora>();
            Assert.Equal("", MoraMapping.MorasToPhonemeString(moras));
        }

        [Fact]
        public void MorasToPhonemeString_SingleMora_ReturnsPhoneme()
        {
            var moras = new List<Mora> { new Mora(Consonant.K, Vowel.A, MoraKind.Ka) };
            Assert.Equal("k a", MoraMapping.MorasToPhonemeString(moras));
        }

        [Fact]
        public void MorasToPhonemeString_MultipleMoras_SpaceSeparated()
        {
            var moras = new List<Mora>
            {
                new Mora(Consonant.K, Vowel.A, MoraKind.Ka),
                new Mora(Consonant.Nn, null, MoraKind.N),
                new Mora(Consonant.J, Vowel.I, MoraKind.Ji),
            };
            Assert.Equal("k a N j i", MoraMapping.MorasToPhonemeString(moras));
        }

        [Fact]
        public void MorasToPhonemeString_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MoraMapping.MorasToPhonemeString(null!));
        }

        [Fact]
        public void MorasToPhonemeString_ToutenMora_Skipped()
        {
            var moras = new List<Mora>
            {
                new Mora(Consonant.K, Vowel.A, MoraKind.Ka),
                new Mora(null, null, MoraKind.Touten),
                new Mora(Consonant.K, Vowel.I, MoraKind.Ki),
            };
            Assert.Equal("k a k i", MoraMapping.MorasToPhonemeString(moras));
        }

        // ===== KatakanaToMoras テスト =====

        [Fact]
        public void KatakanaToMoras_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MoraMapping.KatakanaToMoras(null!));
        }

        [Fact]
        public void KatakanaToMoras_EmptyString_ReturnsEmptyList()
        {
            var result = MoraMapping.KatakanaToMoras("");
            Assert.Empty(result);
        }

        [Fact]
        public void KatakanaToMoras_UnknownCharacter_ThrowsArgumentException()
        {
            // ひらがなは未知文字として扱われる
            Assert.Throws<ArgumentException>(() => MoraMapping.KatakanaToMoras("あ"));
        }

        [Fact]
        public void KatakanaToMoras_YouonPriority_MatchesLongestFirst()
        {
            // "キャ" は2文字モーラとして1個で返るべき（"キ"+"ャ"の2個ではない）
            var result = MoraMapping.KatakanaToMoras("キャ");
            Assert.Single(result);
            Assert.Equal(MoraKind.Kya, result[0].Kind);
            Assert.Equal(Consonant.Ky, result[0].Consonant);
            Assert.Equal(Vowel.A, result[0].Vowel);
        }

        // ===== GetPhonemes テスト =====

        [Theory]
        [InlineData(MoraKind.Ka, "k", "a")]
        [InlineData(MoraKind.N, "N", null)]
        [InlineData(MoraKind.Xtsu, "cl", null)]
        [InlineData(MoraKind.A, null, "a")]
        public void GetPhonemes_KnownMoraKind_ReturnsCorrectPhonemes(
            MoraKind kind, string? expectedConsonant, string? expectedVowel)
        {
            var (consonant, vowel) = MoraMapping.GetPhonemes(kind);

            if (expectedConsonant == null)
                Assert.Null(consonant);
            else
                Assert.Equal(expectedConsonant, consonant!.Value.ToSymbol());

            if (expectedVowel == null)
                Assert.Null(vowel);
            else
                Assert.Equal(expectedVowel, vowel!.Value.ToSymbol());
        }

        // ===== CreateMora テスト =====

        [Fact]
        public void CreateMora_ReturnsCorrectMora()
        {
            var mora = MoraMapping.CreateMora(MoraKind.Shi);
            Assert.Equal(Consonant.Sh, mora.Consonant);
            Assert.Equal(Vowel.I, mora.Vowel);
            Assert.Equal(MoraKind.Shi, mora.Kind);
        }
    }
}
