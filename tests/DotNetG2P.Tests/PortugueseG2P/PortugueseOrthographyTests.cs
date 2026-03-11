using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// PortugueseOrthography の単体テスト。
    /// InternalsVisibleTo によりテストプロジェクトから直接アクセス可能。
    /// </summary>
    public class PortugueseOrthographyTests
    {
        // ========== IsVowel ==========

        [Theory]
        // 基本母音
        [InlineData('a', true)]
        [InlineData('e', true)]
        [InlineData('i', true)]
        [InlineData('o', true)]
        [InlineData('u', true)]
        // アクセント付き母音
        [InlineData('\u00E1', true)]  // á
        [InlineData('\u00E0', true)]  // à
        [InlineData('\u00E2', true)]  // â
        [InlineData('\u00E3', true)]  // ã
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00ED', true)]  // í
        [InlineData('\u00F3', true)]  // ó
        [InlineData('\u00F4', true)]  // ô
        [InlineData('\u00F5', true)]  // õ
        [InlineData('\u00FA', true)]  // ú
        [InlineData('\u00FC', true)]  // ü
        // 子音 → false
        [InlineData('b', false)]
        [InlineData('c', false)]
        [InlineData('d', false)]
        [InlineData('f', false)]
        [InlineData('g', false)]
        [InlineData('h', false)]
        [InlineData('k', false)]
        [InlineData('l', false)]
        [InlineData('m', false)]
        [InlineData('n', false)]
        [InlineData('p', false)]
        [InlineData('r', false)]
        [InlineData('s', false)]
        [InlineData('t', false)]
        [InlineData('z', false)]
        // 数字・記号 → false
        [InlineData('0', false)]
        [InlineData('9', false)]
        [InlineData(' ', false)]
        public void IsVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsVowel(c));
        }

        [Theory]
        // 大文字も母音として判定される
        [InlineData('A', true)]
        [InlineData('E', true)]
        [InlineData('I', true)]
        [InlineData('O', true)]
        [InlineData('U', true)]
        public void IsVowel_UpperCase_ReturnsTrue(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsVowel(c));
        }

        // ========== IsStrongVowel ==========

        [Theory]
        // 強母音 → true
        [InlineData('a', true)]
        [InlineData('e', true)]
        [InlineData('o', true)]
        [InlineData('\u00E1', true)]  // á
        [InlineData('\u00E0', true)]  // à
        [InlineData('\u00E2', true)]  // â
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00F3', true)]  // ó
        [InlineData('\u00F4', true)]  // ô
        // 弱母音 → false
        [InlineData('i', false)]
        [InlineData('u', false)]
        [InlineData('\u00ED', false)] // í (アクセント付きだが弱母音)
        [InlineData('\u00FA', false)] // ú (アクセント付きだが弱母音)
        // チルダ付き → false (ã,õ はIsStrongVowelに含めない)
        [InlineData('\u00E3', false)] // ã
        [InlineData('\u00F5', false)] // õ
        // 子音 → false
        [InlineData('b', false)]
        public void IsStrongVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsStrongVowel(c));
        }

        // ========== IsWeakVowel ==========

        [Theory]
        // 弱母音（アクセントなし） → true
        [InlineData('i', true)]
        [InlineData('u', true)]
        // アクセント付き í,ú → false (hiatus形成)
        [InlineData('\u00ED', false)] // í
        [InlineData('\u00FA', false)] // ú
        // 強母音 → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        [InlineData('o', false)]
        // 子音 → false
        [InlineData('b', false)]
        public void IsWeakVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsWeakVowel(c));
        }

        // ========== HasAcuteAccent ==========

        [Theory]
        [InlineData('\u00E1', true)]  // á
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00ED', true)]  // í
        [InlineData('\u00F3', true)]  // ó
        [InlineData('\u00FA', true)]  // ú
        // 曲折アクセント → false
        [InlineData('\u00E2', false)] // â
        [InlineData('\u00EA', false)] // ê
        [InlineData('\u00F4', false)] // ô
        // チルダ → false
        [InlineData('\u00E3', false)] // ã
        [InlineData('\u00F5', false)] // õ
        // グレイヴ → false
        [InlineData('\u00E0', false)] // à
        // 基本文字 → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        public void HasAcuteAccent_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.HasAcuteAccent(c));
        }

        // ========== HasCircumflexAccent ==========

        [Theory]
        [InlineData('\u00E2', true)]  // â
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00F4', true)]  // ô
        // 鋭アクセント → false
        [InlineData('\u00E1', false)] // á
        [InlineData('\u00E9', false)] // é
        // 基本文字 → false
        [InlineData('a', false)]
        [InlineData('o', false)]
        public void HasCircumflexAccent_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.HasCircumflexAccent(c));
        }

        // ========== HasTilde ==========

        [Theory]
        [InlineData('\u00E3', true)]  // ã
        [InlineData('\u00F5', true)]  // õ
        // 他のアクセント → false
        [InlineData('\u00E1', false)] // á
        [InlineData('\u00E2', false)] // â
        [InlineData('\u00E0', false)] // à
        // 基本文字 → false
        [InlineData('a', false)]
        [InlineData('o', false)]
        public void HasTilde_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.HasTilde(c));
        }

        // ========== HasGraveAccent ==========

        [Theory]
        [InlineData('\u00E0', true)]  // à
        // 他のアクセント → false
        [InlineData('\u00E1', false)] // á
        [InlineData('\u00E2', false)] // â
        [InlineData('\u00E3', false)] // ã
        // 基本文字 → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        public void HasGraveAccent_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.HasGraveAccent(c));
        }

        // ========== HasAnyAccent ==========

        [Theory]
        // すべてのアクセント → true
        [InlineData('\u00E1', true)]  // á (acute)
        [InlineData('\u00E2', true)]  // â (circumflex)
        [InlineData('\u00E3', true)]  // ã (tilde)
        [InlineData('\u00E0', true)]  // à (grave)
        [InlineData('\u00E9', true)]  // é (acute)
        [InlineData('\u00EA', true)]  // ê (circumflex)
        [InlineData('\u00ED', true)]  // í (acute)
        [InlineData('\u00F3', true)]  // ó (acute)
        [InlineData('\u00F4', true)]  // ô (circumflex)
        [InlineData('\u00F5', true)]  // õ (tilde)
        [InlineData('\u00FA', true)]  // ú (acute)
        // アクセントなし → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        [InlineData('i', false)]
        [InlineData('o', false)]
        [InlineData('u', false)]
        [InlineData('\u00FC', false)] // ü (トレマ/分音記号、アクセントではない)
        [InlineData('b', false)]
        public void HasAnyAccent_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.HasAnyAccent(c));
        }

        // ========== StripAccent ==========

        [Theory]
        // 全アクセント文字の除去
        [InlineData('\u00E1', 'a')]  // á → a
        [InlineData('\u00E0', 'a')]  // à → a
        [InlineData('\u00E2', 'a')]  // â → a
        [InlineData('\u00E3', 'a')]  // ã → a
        [InlineData('\u00E9', 'e')]  // é → e
        [InlineData('\u00EA', 'e')]  // ê → e
        [InlineData('\u00ED', 'i')]  // í → i
        [InlineData('\u00F3', 'o')]  // ó → o
        [InlineData('\u00F4', 'o')]  // ô → o
        [InlineData('\u00F5', 'o')]  // õ → o
        [InlineData('\u00FA', 'u')]  // ú → u
        [InlineData('\u00FC', 'u')]  // ü → u
        // 非アクセント文字はそのまま
        [InlineData('a', 'a')]
        [InlineData('e', 'e')]
        [InlineData('z', 'z')]
        [InlineData('1', '1')]
        public void StripAccent_ReturnsExpected(char input, char expected)
        {
            Assert.Equal(expected, PortugueseOrthography.StripAccent(input));
        }

        [Theory]
        // 大文字保持
        [InlineData('\u00C1', 'A')]  // Á → A
        [InlineData('\u00C0', 'A')]  // À → A
        [InlineData('\u00C9', 'E')]  // É → E
        [InlineData('\u00CD', 'I')]  // Í → I
        [InlineData('\u00D3', 'O')]  // Ó → O
        [InlineData('\u00DA', 'U')]  // Ú → U
        public void StripAccent_UpperCase_PreservesCase(char input, char expected)
        {
            Assert.Equal(expected, PortugueseOrthography.StripAccent(input));
        }

        // ========== IsConsonant ==========

        [Theory]
        // 子音 → true
        [InlineData('b', true)]
        [InlineData('c', true)]
        [InlineData('d', true)]
        [InlineData('f', true)]
        [InlineData('g', true)]
        [InlineData('h', true)]
        [InlineData('j', true)]
        [InlineData('k', true)]
        [InlineData('l', true)]
        [InlineData('m', true)]
        [InlineData('n', true)]
        [InlineData('p', true)]
        [InlineData('q', true)]
        [InlineData('r', true)]
        [InlineData('s', true)]
        [InlineData('t', true)]
        [InlineData('v', true)]
        [InlineData('w', true)]
        [InlineData('x', true)]
        [InlineData('z', true)]
        // ç → true
        [InlineData('\u00E7', true)]  // ç
        // 母音 → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        [InlineData('i', false)]
        [InlineData('o', false)]
        [InlineData('u', false)]
        // 数字・記号 → false
        [InlineData('0', false)]
        [InlineData(' ', false)]
        public void IsConsonant_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsConsonant(c));
        }

        // ========== IsFrontVowel ==========

        [Theory]
        // 前舌母音 → true
        [InlineData('e', true)]
        [InlineData('i', true)]
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00ED', true)]  // í
        // 非前舌母音 → false
        [InlineData('a', false)]
        [InlineData('o', false)]
        [InlineData('u', false)]
        [InlineData('\u00E1', false)] // á
        [InlineData('\u00F3', false)] // ó
        [InlineData('\u00FA', false)] // ú
        // 子音 → false
        [InlineData('b', false)]
        [InlineData('k', false)]
        public void IsFrontVowel_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsFrontVowel(c));
        }

        // ========== IsDigraph ==========

        [Theory]
        // 有効なダイグラフ
        [InlineData("chave", 0, true)]   // ch
        [InlineData("filho", 2, true)]   // lh
        [InlineData("ninho", 2, true)]   // nh
        [InlineData("carro", 2, true)]   // rr
        [InlineData("passo", 2, true)]   // ss
        [InlineData("quero", 0, true)]   // qu
        [InlineData("guerra", 0, true)]  // gu
        // 非ダイグラフ
        [InlineData("casa", 0, false)]   // ca
        [InlineData("gato", 0, false)]   // ga (g+a, not g+u)
        [InlineData("rosa", 2, false)]   // sa
        // 語末文字（index+1 >= word.Length）
        [InlineData("ar", 1, false)]     // r at end
        [InlineData("x", 0, false)]      // single char
        public void IsDigraph_ReturnsExpected(string word, int index, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsDigraph(word, index));
        }

        // ========== CanFormDiphthong ==========

        [Theory]
        // 弱+強 → 二重母音
        [InlineData('i', 'a', true)]
        [InlineData('u', 'e', true)]
        [InlineData('i', 'o', true)]
        // 強+弱 → 二重母音
        [InlineData('a', 'i', true)]
        [InlineData('e', 'u', true)]
        [InlineData('o', 'i', true)]
        // 弱+弱 → 二重母音
        [InlineData('i', 'u', true)]
        [InlineData('u', 'i', true)]
        // 強+強 → false (hiatus)
        [InlineData('a', 'e', false)]
        [InlineData('e', 'o', false)]
        [InlineData('a', 'o', false)]
        // アクセント付き í/ú → false (hiatus)
        [InlineData('\u00ED', 'a', false)]  // í+a
        [InlineData('a', '\u00FA', false)]  // a+ú
        [InlineData('\u00ED', 'o', false)]  // í+o
        // 非母音 → false
        [InlineData('b', 'a', false)]
        [InlineData('a', 'b', false)]
        public void CanFormDiphthong_ReturnsExpected(char v1, char v2, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.CanFormDiphthong(v1, v2));
        }

        // ========== CanFormTriphthong ==========

        [Theory]
        // 弱+強+弱 → 三重母音
        [InlineData('u', 'a', 'i', true)]   // Uruguai
        [InlineData('u', 'e', 'i', true)]   // quei
        [InlineData('i', 'a', 'u', true)]
        // 強+強+弱 → false
        [InlineData('a', 'e', 'i', false)]
        // 弱+弱+弱 → false (中央が強母音でない)
        [InlineData('i', 'u', 'i', false)]
        // 強+弱+強 → false
        [InlineData('a', 'i', 'o', false)]
        // アクセント付き弱母音 → false (IsWeakVowel が false)
        [InlineData('\u00ED', 'a', 'u', false)] // í+a+u
        public void CanFormTriphthong_ReturnsExpected(char v1, char v2, char v3, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.CanFormTriphthong(v1, v2, v3));
        }

        // ========== IsHiatus ==========

        [Theory]
        // 強+強 → hiatus
        [InlineData('a', 'e', true)]
        [InlineData('e', 'o', true)]
        [InlineData('a', 'o', true)]
        [InlineData('o', 'a', true)]
        // アクセント付き í/ú → hiatus
        [InlineData('\u00ED', 'a', true)]   // í+a
        [InlineData('a', '\u00FA', true)]   // a+ú
        [InlineData('\u00ED', 'u', true)]   // í+u
        // 同一母音 → hiatus
        [InlineData('a', 'a', true)]
        [InlineData('o', 'o', true)]
        [InlineData('i', 'i', true)]
        // 弱+強（アクセントなし） → false (二重母音)
        [InlineData('i', 'a', false)]
        [InlineData('u', 'e', false)]
        // 弱+弱 → false (二重母音)
        [InlineData('i', 'u', false)]
        // 非母音 → false
        [InlineData('b', 'a', false)]
        public void IsHiatus_ReturnsExpected(char v1, char v2, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsHiatus(v1, v2));
        }

        // ========== IsHiatus (3引数オーバーロード) ==========

        [Theory]
        // v2HasAccent=true で弱母音 → hiatus
        [InlineData('a', 'i', true, true)]    // a + stressed i → hiatus
        [InlineData('e', 'u', true, true)]    // e + stressed u → hiatus
        [InlineData('o', 'i', true, true)]    // o + stressed i → hiatus
        // v2HasAccent=false → 通常の IsHiatus と同じ
        [InlineData('a', 'i', false, false)]  // a+i (二重母音)
        [InlineData('a', 'e', false, true)]   // a+e (強+強 → hiatus)
        // v2HasAccent=true だが v2 が強母音 → 通常のIsHiatus判定
        [InlineData('a', 'e', true, true)]    // a+e (強+強 → hiatus regardless)
        [InlineData('i', 'a', true, false)]   // i+a (弱+強 → diphthong, v2 is strong)
        public void IsHiatus_WithAccentFlag_ReturnsExpected(char v1, char v2, bool v2HasAccent, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsHiatus(v1, v2, v2HasAccent));
        }

        // ========== RemoveAccentMarks ==========

        [Fact]
        public void RemoveAccentMarks_RemovesAllAccents()
        {
            // cora\u00E7\u00E3o → coraçao (ç はアクセントではなく cedilla なので保持)
            Assert.Equal("cora\u00E7ao", PortugueseOrthography.RemoveAccentMarks("cora\u00E7\u00E3o"));
        }

        [Fact]
        public void RemoveAccentMarks_PreservesNonAccentedChars()
        {
            Assert.Equal("hello", PortugueseOrthography.RemoveAccentMarks("hello"));
        }

        [Fact]
        public void RemoveAccentMarks_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", PortugueseOrthography.RemoveAccentMarks(""));
        }

        [Fact]
        public void RemoveAccentMarks_Null_ReturnsNull()
        {
            Assert.Null(PortugueseOrthography.RemoveAccentMarks(null!));
        }

        [Fact]
        public void RemoveAccentMarks_MixedAccents()
        {
            // á,â,ã,à,é,ê,í,ó,ô,õ,ú,ü → base letters
            Assert.Equal("aaaaeeiooouu", PortugueseOrthography.RemoveAccentMarks("\u00E1\u00E2\u00E3\u00E0\u00E9\u00EA\u00ED\u00F3\u00F4\u00F5\u00FA\u00FC"));
        }

        [Fact]
        public void RemoveAccentMarks_PreservesCase()
        {
            // Á → A (大文字保持)
            Assert.Equal("Agua", PortugueseOrthography.RemoveAccentMarks("\u00C1gua"));
        }

        [Fact]
        public void RemoveAccentMarks_PreservesCedilla()
        {
            // ç はアクセントではなく子音なので StripAccent が処理しない → そのまま残る
            Assert.Equal("a\u00E7ao", PortugueseOrthography.RemoveAccentMarks("a\u00E7\u00E3o"));
        }

        // ========== IsSilentU ==========

        [Theory]
        // que/qui → 黙字u
        [InlineData("quero", 1, true)]    // qu+e
        [InlineData("aqui", 2, true)]     // qu+i
        // gue/gui → 黙字u
        [InlineData("guerra", 1, true)]   // gu+e
        [InlineData("guia", 1, true)]     // gu+i
        // qua/quo → u は発音する
        [InlineData("quando", 1, false)]  // qu+a
        [InlineData("quota", 1, false)]   // qu+o (next is not front vowel)
        // gu+a → u は発音する
        [InlineData("guardar", 1, false)] // gu+a
        // güe → ü は旧正書法で発音する(黙字ではない)
        [InlineData("ling\u00FCeta", 4, false)] // güe: ü at index 4
        // 語頭の u → false (no preceding char)
        [InlineData("um", 0, false)]
        // 語末の u → false (no following char)
        [InlineData("peru", 3, false)]
        // u ではない文字 → false
        [InlineData("quero", 0, false)]   // q is not u
        public void IsSilentU_ReturnsExpected(string word, int index, bool expected)
        {
            Assert.Equal(expected, PortugueseOrthography.IsSilentU(word, index));
        }
    }
}
