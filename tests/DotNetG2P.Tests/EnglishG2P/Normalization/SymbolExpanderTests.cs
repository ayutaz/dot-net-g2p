// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// SymbolExpander の単体テスト。
    /// </summary>
    public class SymbolExpanderTests
    {
        [Theory]
        [InlineData("@", "at")]
        [InlineData("#", "hash")]
        [InlineData("&", "and")]
        [InlineData("%", "percent")]
        [InlineData("+", "plus")]
        [InlineData("=", "equals")]
        [InlineData("*", "asterisk")]
        [InlineData("/", "slash")]
        public void TryExpand_KnownSymbols_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, SymbolExpander.TryExpand(input));
        }

        [Fact]
        public void TryExpand_AlphabeticToken_ReturnsNull()
        {
            Assert.Null(SymbolExpander.TryExpand("hello"));
        }

        [Fact]
        public void TryExpand_EmptyString_ReturnsNull()
        {
            Assert.Null(SymbolExpander.TryExpand(""));
        }

        [Fact]
        public void TryExpand_NumericToken_ReturnsNull()
        {
            Assert.Null(SymbolExpander.TryExpand("123"));
        }

        [Fact]
        public void TryExpand_Null_ReturnsNull()
        {
            Assert.Null(SymbolExpander.TryExpand(null!));
        }

        [Fact]
        public void TryExpand_UnknownSingleSymbol_ReturnsNull()
        {
            // "!" は対象外（文末句読点として頻出するため）
            Assert.Null(SymbolExpander.TryExpand("!"));
        }

        [Fact]
        public void TryExpand_MultiCharSymbolString_ReturnsNull()
        {
            // 複数文字は対象外
            Assert.Null(SymbolExpander.TryExpand("@@"));
        }

        [Fact]
        public void TryExpand_SingleCharDigit_ReturnsNull()
        {
            // 数字1文字は記号テーブルにないためnull
            Assert.Null(SymbolExpander.TryExpand("5"));
        }
    }
}
