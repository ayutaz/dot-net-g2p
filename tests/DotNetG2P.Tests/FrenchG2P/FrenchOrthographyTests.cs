using DotNetG2P.French.Rules;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// FrenchOrthography の単体テスト。
    /// InternalsVisibleTo によりテストプロジェクトから直接アクセス可能。
    /// </summary>
    public class FrenchOrthographyTests
    {
        // ========== IsVowelChar ==========

        [Theory]
        // 基本母音
        [InlineData('a', true)]
        [InlineData('e', true)]
        [InlineData('i', true)]
        [InlineData('o', true)]
        [InlineData('u', true)]
        [InlineData('y', true)]
        // アクセント付き母音
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00E8', true)]  // è
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00EB', true)]  // ë
        [InlineData('\u00E0', true)]  // à
        [InlineData('\u00E2', true)]  // â
        [InlineData('\u00EE', true)]  // î
        [InlineData('\u00EF', true)]  // ï
        [InlineData('\u00F4', true)]  // ô
        [InlineData('\u00F9', true)]  // ù
        [InlineData('\u00FB', true)]  // û
        [InlineData('\u00FC', true)]  // ü
        // 合字
        [InlineData('\u00E6', true)]  // æ
        [InlineData('\u0153', true)]  // œ
        // 子音 → false
        [InlineData('b', false)]
        [InlineData('c', false)]
        [InlineData('d', false)]
        [InlineData('f', false)]
        [InlineData('g', false)]
        [InlineData('h', false)]
        [InlineData('j', false)]
        [InlineData('k', false)]
        [InlineData('l', false)]
        [InlineData('m', false)]
        [InlineData('n', false)]
        [InlineData('p', false)]
        [InlineData('q', false)]
        [InlineData('r', false)]
        [InlineData('s', false)]
        [InlineData('t', false)]
        [InlineData('v', false)]
        [InlineData('w', false)]
        [InlineData('x', false)]
        [InlineData('z', false)]
        // 数字 → false
        [InlineData('0', false)]
        [InlineData('9', false)]
        public void IsVowelChar_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, FrenchOrthography.IsVowelChar(c));
        }

        [Theory]
        // 大文字も母音として判定される（内部で ToLowerInvariant）
        [InlineData('A', true)]
        [InlineData('E', true)]
        [InlineData('I', true)]
        [InlineData('O', true)]
        [InlineData('U', true)]
        [InlineData('Y', true)]
        public void IsVowelChar_UpperCase_ReturnsTrue(char c, bool expected)
        {
            Assert.Equal(expected, FrenchOrthography.IsVowelChar(c));
        }

        // ========== IsFrontVowelChar ==========

        [Theory]
        // 前舌母音 → true
        [InlineData('e', true)]
        [InlineData('i', true)]
        [InlineData('y', true)]
        [InlineData('\u00E8', true)]  // è
        [InlineData('\u00E9', true)]  // é
        [InlineData('\u00EA', true)]  // ê
        [InlineData('\u00EB', true)]  // ë
        [InlineData('\u00EE', true)]  // î
        [InlineData('\u00EF', true)]  // ï
        // 非前舌母音 → false
        [InlineData('a', false)]
        [InlineData('o', false)]
        [InlineData('u', false)]
        [InlineData('\u00E0', false)] // à
        [InlineData('\u00E2', false)] // â
        [InlineData('\u00F4', false)] // ô
        [InlineData('\u00F9', false)] // ù
        [InlineData('\u00FB', false)] // û
        // 子音 → false
        [InlineData('b', false)]
        [InlineData('k', false)]
        public void IsFrontVowelChar_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, FrenchOrthography.IsFrontVowelChar(c));
        }

        // ========== IsConsonantChar ==========

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
        [InlineData('y', false)]
        public void IsConsonantChar_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, FrenchOrthography.IsConsonantChar(c));
        }

        // ========== HasTrema ==========

        [Theory]
        // トレマ付き → true
        [InlineData('\u00EB', true)]  // ë
        [InlineData('\u00EF', true)]  // ï
        [InlineData('\u00FC', true)]  // ü
        // アクセント付き（トレマではない）→ false
        [InlineData('\u00E9', false)] // é
        [InlineData('\u00E8', false)] // è
        [InlineData('\u00EA', false)] // ê
        [InlineData('\u00E0', false)] // à
        [InlineData('\u00E2', false)] // â
        [InlineData('\u00EE', false)] // î
        [InlineData('\u00F4', false)] // ô
        [InlineData('\u00F9', false)] // ù
        [InlineData('\u00FB', false)] // û
        // 基本文字 → false
        [InlineData('a', false)]
        [InlineData('e', false)]
        [InlineData('i', false)]
        public void HasTrema_ReturnsExpected(char c, bool expected)
        {
            Assert.Equal(expected, FrenchOrthography.HasTrema(c));
        }

        // ========== StripAccent ==========

        [Theory]
        // アクセント除去
        [InlineData('\u00E9', 'e')]  // é → e
        [InlineData('\u00E8', 'e')]  // è → e
        [InlineData('\u00EA', 'e')]  // ê → e
        [InlineData('\u00EB', 'e')]  // ë → e
        [InlineData('\u00E0', 'a')]  // à → a
        [InlineData('\u00E2', 'a')]  // â → a
        [InlineData('\u00EE', 'i')]  // î → i
        [InlineData('\u00EF', 'i')]  // ï → i
        [InlineData('\u00F4', 'o')]  // ô → o
        [InlineData('\u00F9', 'u')]  // ù → u
        [InlineData('\u00FB', 'u')]  // û → u
        [InlineData('\u00FC', 'u')]  // ü → u
        [InlineData('\u00E7', 'c')]  // ç → c
        // 基本文字はそのまま
        [InlineData('a', 'a')]
        [InlineData('e', 'e')]
        [InlineData('z', 'z')]
        public void StripAccent_ReturnsExpected(char input, char expected)
        {
            Assert.Equal(expected, FrenchOrthography.StripAccent(input));
        }

        [Theory]
        // 大文字保持: É → E
        [InlineData('\u00C9', 'E')]  // É → E
        [InlineData('\u00C0', 'A')]  // À → A
        [InlineData('\u00C7', 'C')]  // Ç → C
        public void StripAccent_UpperCase_PreservesCase(char input, char expected)
        {
            Assert.Equal(expected, FrenchOrthography.StripAccent(input));
        }

        [Fact]
        public void StripAccent_NonAccented_ReturnsSameChar()
        {
            // 非アクセント文字はそのまま返る
            Assert.Equal('x', FrenchOrthography.StripAccent('x'));
            Assert.Equal('1', FrenchOrthography.StripAccent('1'));
            Assert.Equal(' ', FrenchOrthography.StripAccent(' '));
        }
    }
}
