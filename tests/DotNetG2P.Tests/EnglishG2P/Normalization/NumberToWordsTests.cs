// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// NumberToWords の基数・序数・小数展開テスト。
    /// </summary>
    public class NumberToWordsTests
    {
        // ===== Cardinal: 基本的な数値 =====

        [Theory]
        [InlineData(0, "zero")]
        [InlineData(1, "one")]
        [InlineData(5, "five")]
        [InlineData(10, "ten")]
        [InlineData(11, "eleven")]
        [InlineData(13, "thirteen")]
        [InlineData(15, "fifteen")]
        [InlineData(19, "nineteen")]
        public void Cardinal_SmallNumbers(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        [Theory]
        [InlineData(20, "twenty")]
        [InlineData(21, "twenty one")]
        [InlineData(42, "forty two")]
        [InlineData(99, "ninety nine")]
        public void Cardinal_TwoDigitNumbers(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        [Theory]
        [InlineData(100, "one hundred")]
        [InlineData(101, "one hundred one")]
        [InlineData(110, "one hundred ten")]
        [InlineData(999, "nine hundred ninety nine")]
        public void Cardinal_ThreeDigitNumbers(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        [Theory]
        [InlineData(1000, "one thousand")]
        [InlineData(1001, "one thousand one")]
        [InlineData(1234, "one thousand two hundred thirty four")]
        [InlineData(10000, "ten thousand")]
        [InlineData(100000, "one hundred thousand")]
        public void Cardinal_ThousandsRange(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        [Theory]
        [InlineData(1000000, "one million")]
        [InlineData(1000000000, "one billion")]
        [InlineData(1000000000000, "one trillion")]
        public void Cardinal_LargeScales(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        // ===== Cardinal: 負数 =====

        [Theory]
        [InlineData(-5, "negative five")]
        [InlineData(-100, "negative one hundred")]
        public void Cardinal_NegativeNumbers(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Cardinal(input));
        }

        // ===== Ordinal: 不規則変換 =====

        [Theory]
        [InlineData(1, "first")]
        [InlineData(2, "second")]
        [InlineData(3, "third")]
        [InlineData(4, "fourth")]
        [InlineData(5, "fifth")]
        [InlineData(8, "eighth")]
        [InlineData(9, "ninth")]
        public void Ordinal_IrregularForms(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Ordinal(input));
        }

        [Theory]
        [InlineData(11, "eleventh")]
        [InlineData(12, "twelfth")]
        [InlineData(13, "thirteenth")]
        public void Ordinal_Teens(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Ordinal(input));
        }

        [Theory]
        [InlineData(20, "twentieth")]
        [InlineData(21, "twenty first")]
        [InlineData(100, "one hundredth")]
        [InlineData(1000, "one thousandth")]
        public void Ordinal_LargerNumbers(long input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Ordinal(input));
        }

        // ===== ExpandDecimal: 小数展開 =====

        [Theory]
        [InlineData("3", "14", "three point one four")]
        [InlineData("0", "5", "zero point five")]
        [InlineData("1", "005", "one point zero zero five")]
        public void ExpandDecimal_DigitByDigit(string intPart, string fracPart, string expected)
        {
            Assert.Equal(expected, NumberToWords.ExpandDecimal(intPart, fracPart));
        }
    }
}
