using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// NasalVowelizer の単体テスト。
    /// TryNasalize メソッドを直接テストする（InternalsVisibleTo により内部クラスにアクセス可能）。
    /// </summary>
    public class NasalVowelizerTests
    {
        // =====================================================================
        // 1. チルダ付き母音（常に鼻母音化）
        // =====================================================================

        [Fact]
        public void TryNasalize_TildeA_ReturnsANasal()
        {
            // ã → [ANasal]（単独チルダ母音）
            var result = NasalVowelizer.TryNasalize(
                "l\u00E3", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(1, consumed);
        }

        [Fact]
        public void TryNasalize_TildeO_ReturnsONasal()
        {
            // õ → [ONasal]（語中のチルダ母音）
            var result = NasalVowelizer.TryNasalize(
                "p\u00F5r", 1, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(1, consumed);
        }

        // =====================================================================
        // 2. チルダ付き鼻二重母音（ão, ãe, ãi, õe）
        // =====================================================================

        [Fact]
        public void TryNasalize_Ao_ReturnsNasalDiphthong()
        {
            // ão → [ANasal, WNasal]（"não" の ã 位置）
            var result = NasalVowelizer.TryNasalize(
                "n\u00E3o", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Ae_TildeE_ReturnsNasalDiphthong()
        {
            // ãe → [ANasal, JNasal]（"mãe" の ã 位置）
            var result = NasalVowelizer.TryNasalize(
                "m\u00E3e", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Ai_TildeI_ReturnsNasalDiphthong()
        {
            // ãi → [ANasal, JNasal]
            var result = NasalVowelizer.TryNasalize(
                "\u00E3is", 0, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Oe_Tilde_ReturnsNasalDiphthong()
        {
            // õe → [ONasal, JNasal]（"põe" の õ 位置）
            var result = NasalVowelizer.TryNasalize(
                "p\u00F5e", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Aes_WordFinal_ReturnsNasalDiphthongPlusS()
        {
            // ães(語末) → [ANasal, JNasal, S]（"pães"）
            var result = NasalVowelizer.TryNasalize(
                "p\u00E3es", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }

        [Fact]
        public void TryNasalize_Oes_WordFinal_ReturnsNasalDiphthongPlusS()
        {
            // ões(語末) → [ONasal, JNasal, S]（"canções" の õ 位置）
            var result = NasalVowelizer.TryNasalize(
                "can\u00E7\u00F5es", 4, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }

        [Fact]
        public void TryNasalize_Ao_MidWord_ReturnsNasalDiphthong()
        {
            // ão は語中でも鼻二重母音（"não" のチルダは常に鼻母音化）
            var result = NasalVowelizer.TryNasalize(
                "s\u00E3o", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 3. 語末鼻二重母音（am, em, ens, om）
        // =====================================================================

        [Fact]
        public void TryNasalize_Am_WordFinal_ReturnsNasalDiphthong()
        {
            // am(語末) → [ANasal, WNasal]（"falam" の a 位置）
            var result = NasalVowelizer.TryNasalize(
                "falam", 3, true, false,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Em_WordFinal_ReturnsNasalDiphthong()
        {
            // em(語末) → [ENasal, JNasal]（"bem" の e 位置）
            var result = NasalVowelizer.TryNasalize(
                "bem", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Ens_WordFinal_ReturnsNasalDiphthongPlusS()
        {
            // ens(語末) → [ENasal, JNasal, S]（"jovens" の e 位置）
            var result = NasalVowelizer.TryNasalize(
                "jovens", 3, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }

        [Fact]
        public void TryNasalize_Om_WordFinal_Stressed_ReturnsNasalDiphthong()
        {
            // om(語末, 強勢) → [ONasal, WNasal]（"bom" の o 位置）
            var result = NasalVowelizer.TryNasalize(
                "bom", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Om_WordFinal_Unstressed_ReturnsSingleNasal()
        {
            // om(語末, 非強勢) → [ONasal]（単純鼻母音）
            var result = NasalVowelizer.TryNasalize(
                "random", 4, true, false,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Im_WordFinal_ReturnsSingleNasal()
        {
            // im(語末) → [INasal]
            var result = NasalVowelizer.TryNasalize(
                "fim", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.INasal, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Um_WordFinal_ReturnsSingleNasal()
        {
            // um(語末) → [UNasal]
            var result = NasalVowelizer.TryNasalize(
                "um", 0, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.UNasal, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 4. 単純鼻母音（母音 + n/m + 子音）
        // =====================================================================

        [Theory]
        // a + m + 子音 → ANasal
        [InlineData("campo", 1, PortugueseIpaPhoneme.ANasal)]
        // a + n + 子音 → ANasal
        [InlineData("canto", 1, PortugueseIpaPhoneme.ANasal)]
        public void TryNasalize_A_PlusNasalPlusConsonant_ReturnsANasal(
            string word, int index, PortugueseIpaPhoneme expected)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(expected, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Theory]
        // e + m + 子音 → ENasal
        [InlineData("tempo", 1, PortugueseIpaPhoneme.ENasal)]
        // e + n + 子音 → ENasal
        [InlineData("vento", 1, PortugueseIpaPhoneme.ENasal)]
        public void TryNasalize_E_PlusNasalPlusConsonant_ReturnsENasal(
            string word, int index, PortugueseIpaPhoneme expected)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(expected, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Theory]
        // i + n + 子音 → INasal
        [InlineData("cinco", 1, PortugueseIpaPhoneme.INasal)]
        // i + m + 子音 → INasal
        [InlineData("limpo", 1, PortugueseIpaPhoneme.INasal)]
        public void TryNasalize_I_PlusNasalPlusConsonant_ReturnsINasal(
            string word, int index, PortugueseIpaPhoneme expected)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(expected, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Theory]
        // o + n + 子音 → ONasal
        [InlineData("conta", 1, PortugueseIpaPhoneme.ONasal)]
        // o + n + 子音 → ONasal
        [InlineData("onda", 0, PortugueseIpaPhoneme.ONasal)]
        public void TryNasalize_O_PlusNasalPlusConsonant_ReturnsONasal(
            string word, int index, PortugueseIpaPhoneme expected)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(expected, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Theory]
        // u + n + 子音 → UNasal
        [InlineData("mundo", 1, PortugueseIpaPhoneme.UNasal)]
        // u + n + 子音 → UNasal
        [InlineData("junto", 1, PortugueseIpaPhoneme.UNasal)]
        public void TryNasalize_U_PlusNasalPlusConsonant_ReturnsUNasal(
            string word, int index, PortugueseIpaPhoneme expected)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(expected, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 5. 非鼻母音化（母音 + n/m + 母音 → 母音間）
        // =====================================================================

        [Theory]
        // a + m + a → false（母音間: cama）
        [InlineData("cama", 1)]
        // a + n + a → false（母音間: cana）
        [InlineData("cana", 1)]
        // a + m + i → false（母音間: ami）
        [InlineData("amigo", 0)]
        // o + n + e → false（母音間: bone）
        [InlineData("boneca", 1)]
        // u + n + a → false（母音間: una）
        [InlineData("luna", 1)]
        public void TryNasalize_VowelBetweenVowels_ReturnsFalse(string word, int index)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out _, out _);

            Assert.False(result);
        }

        // =====================================================================
        // 6. 非鼻母音化（nn, mm → 二重子音）
        // =====================================================================

        [Theory]
        [InlineData("anne", 0)]   // a + nn → false
        [InlineData("amma", 0)]  // a + mm → false
        public void TryNasalize_DoubledNasalConsonant_ReturnsFalse(string word, int index)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, false, true,
                out _, out _);

            Assert.False(result);
        }

        // =====================================================================
        // 7. 非母音文字 → false
        // =====================================================================

        [Fact]
        public void TryNasalize_NonVowelChar_ReturnsFalse()
        {
            var result = NasalVowelizer.TryNasalize(
                "bnt", 0, false, true,
                out _, out _);

            Assert.False(result);
        }

        // =====================================================================
        // 8. 母音の後に鼻子音がない → false
        // =====================================================================

        [Fact]
        public void TryNasalize_VowelNotFollowedByNasal_ReturnsFalse()
        {
            var result = NasalVowelizer.TryNasalize(
                "ato", 0, false, true,
                out _, out _);

            Assert.False(result);
        }

        // =====================================================================
        // 9. 境界テスト
        // =====================================================================

        [Fact]
        public void TryNasalize_IndexAtEndOfWord_ReturnsFalse()
        {
            var result = NasalVowelizer.TryNasalize(
                "a", 1, true, true,
                out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryNasalize_EmptyString_ReturnsFalse()
        {
            var result = NasalVowelizer.TryNasalize(
                "", 0, true, true,
                out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryNasalize_SingleVowelAtEnd_ReturnsFalse()
        {
            // 母音1文字のみで後続なし → false
            var result = NasalVowelizer.TryNasalize(
                "a", 0, true, true,
                out _, out _);

            Assert.False(result);
        }

        // =====================================================================
        // 10. 実際の単語での統合テスト
        // =====================================================================

        [Fact]
        public void TryNasalize_Mao_HandWord()
        {
            // mão → ã位置で [ANasal, WNasal]
            var result = NasalVowelizer.TryNasalize(
                "m\u00E3o", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Mae_MotherWord()
        {
            // mãe → ã位置で [ANasal, JNasal]
            var result = NasalVowelizer.TryNasalize(
                "m\u00E3e", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Cancoes_PluralsWord()
        {
            // canções → ç位置の後の õ で [ONasal, JNasal, S]
            // "canções" = c(0) a(1) n(2) ç(3) õ(4) e(5) s(6)
            var result = NasalVowelizer.TryNasalize(
                "can\u00E7\u00F5es", 4, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }

        [Fact]
        public void TryNasalize_Paes_PluralsWord()
        {
            // pães → ã位置で [ANasal, JNasal, S]
            // "pães" = p(0) ã(1) e(2) s(3)
            var result = NasalVowelizer.TryNasalize(
                "p\u00E3es", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }

        [Fact]
        public void TryNasalize_Tambem_AlsoWord()
        {
            // também → e + m(語末) → [ENasal, JNasal]
            // "também" = t(0) a(1) m(2) b(3) é(4) m(5)
            var result = NasalVowelizer.TryNasalize(
                "tamb\u00E9m", 4, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Tom_ToneWord()
        {
            // tom → o + m(語末, 強勢) → [ONasal, WNasal]
            var result = NasalVowelizer.TryNasalize(
                "tom", 1, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Compra_BuyWord()
        {
            // compra → o + m + p → [ONasal], consumed=2
            // "compra" = c(0) o(1) m(2) p(3) r(4) a(5)
            var result = NasalVowelizer.TryNasalize(
                "compra", 1, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ONasal, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        [Fact]
        public void TryNasalize_Irma_SisterWord()
        {
            // irmã → ã(語末) → [ANasal]
            // "irmã" = i(0) r(1) m(2) ã(3)
            var result = NasalVowelizer.TryNasalize(
                "irm\u00E3", 3, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(1, consumed);
        }

        [Fact]
        public void TryNasalize_Dizem_SayWord()
        {
            // dizem → e + m(語末) → [ENasal, JNasal]
            // "dizem" = d(0) i(1) z(2) e(3) m(4)
            var result = NasalVowelizer.TryNasalize(
                "dizem", 3, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(2, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 11. ヘルパーメソッドテスト
        // =====================================================================

        [Theory]
        [InlineData('n', true)]
        [InlineData('m', true)]
        [InlineData('N', true)]
        [InlineData('M', true)]
        [InlineData('l', false)]
        [InlineData('r', false)]
        [InlineData('a', false)]
        public void IsNasalConsonant_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, NasalVowelizer.IsNasalConsonant(c));
        }

        [Theory]
        [InlineData('a', PortugueseIpaPhoneme.ANasal)]
        [InlineData('e', PortugueseIpaPhoneme.ENasal)]
        [InlineData('i', PortugueseIpaPhoneme.INasal)]
        [InlineData('o', PortugueseIpaPhoneme.ONasal)]
        [InlineData('u', PortugueseIpaPhoneme.UNasal)]
        [InlineData('\u00E1', PortugueseIpaPhoneme.ANasal)] // á → ANasal
        [InlineData('\u00E9', PortugueseIpaPhoneme.ENasal)] // é → ENasal
        [InlineData('\u00ED', PortugueseIpaPhoneme.INasal)] // í → INasal
        [InlineData('\u00F3', PortugueseIpaPhoneme.ONasal)] // ó → ONasal
        [InlineData('\u00FA', PortugueseIpaPhoneme.UNasal)] // ú → UNasal
        public void GetNasalVowel_ReturnsCorrectPhoneme(char vowel, PortugueseIpaPhoneme expected)
        {
            Assert.Equal(expected, NasalVowelizer.GetNasalVowel(vowel));
        }

        // =====================================================================
        // 12. アクセント付き母音の鼻母音化
        // =====================================================================

        [Fact]
        public void TryNasalize_AccentedE_PlusNasal_ReturnsENasal()
        {
            // é + m(語末) → [ENasal, JNasal]
            var result = NasalVowelizer.TryNasalize(
                "tamb\u00E9m", 4, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
        }

        [Fact]
        public void TryNasalize_AccentedA_PlusNasal_ReturnsANasal()
        {
            // á + n + 子音 → [ANasal]（語中）
            var result = NasalVowelizer.TryNasalize(
                "\u00E1nto", 0, false, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Single(phonemes);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 13. 語末 an/en/in/on/un テスト
        // =====================================================================

        [Theory]
        [InlineData("an", 0, PortugueseIpaPhoneme.ANasal)]
        [InlineData("en", 0, PortugueseIpaPhoneme.ENasal)]
        [InlineData("in", 0, PortugueseIpaPhoneme.INasal)]
        [InlineData("on", 0, PortugueseIpaPhoneme.ONasal)]
        [InlineData("un", 0, PortugueseIpaPhoneme.UNasal)]
        public void TryNasalize_VowelPlusN_AtEnd_ReturnsNasal(
            string word, int index, PortugueseIpaPhoneme expectedFirst)
        {
            var result = NasalVowelizer.TryNasalize(
                word, index, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            // 語末 am/em は鼻二重母音、an/en は単純鼻母音 or 鼻二重母音
            // am → [ANasal, WNasal], em → [ENasal, JNasal]
            // an/in/on/un は語末単純鼻母音のフォールバック
            Assert.Equal(expectedFirst, phonemes[0]);
            Assert.Equal(2, consumed);
        }

        // =====================================================================
        // 14. Parabens テスト（複雑な語末 ens）
        // =====================================================================

        [Fact]
        public void TryNasalize_Parabens_ReturnsNasalDiphthongPlusS()
        {
            // parabéns → é + n + s(語末) → [ENasal, JNasal, S]
            // "parabéns" = p(0) a(1) r(2) a(3) b(4) é(5) n(6) s(7)
            var result = NasalVowelizer.TryNasalize(
                "parab\u00E9ns", 5, true, true,
                out var phonemes, out var consumed);

            Assert.True(result);
            Assert.Equal(3, phonemes.Length);
            Assert.Equal(PortugueseIpaPhoneme.ENasal, phonemes[0]);
            Assert.Equal(PortugueseIpaPhoneme.JNasal, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[2]);
            Assert.Equal(3, consumed);
        }
    }
}
