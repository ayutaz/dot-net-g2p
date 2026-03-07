// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// AbbreviationExpander の単体テスト。
    /// </summary>
    public class AbbreviationExpanderTests
    {
        [Theory]
        [InlineData("Dr.", "Doctor")]
        [InlineData("Mr.", "Mister")]
        [InlineData("Mrs.", "Misses")]
        [InlineData("etc.", "etcetera")]
        [InlineData("vs.", "versus")]
        public void TryExpand_KnownAbbreviationsWithPeriod_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        [Theory]
        [InlineData("Dr", "Doctor")]
        [InlineData("Mr", "Mister")]
        [InlineData("vs", "versus")]
        public void TryExpand_KnownAbbreviationsWithoutPeriod_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        [Theory]
        [InlineData("dr.", "Doctor")]
        [InlineData("DR.", "Doctor")]
        [InlineData("dR", "Doctor")]
        public void TryExpand_CaseInsensitive_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        [Theory]
        [InlineData("Jan.", "January")]
        [InlineData("Feb.", "February")]
        [InlineData("Aug.", "August")]
        [InlineData("Dec.", "December")]
        public void TryExpand_MonthAbbreviations_ReturnsFullMonth(string input, string expected)
        {
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        [Fact]
        public void TryExpand_UnknownAbbreviation_ReturnsNull()
        {
            Assert.Null(AbbreviationExpander.TryExpand("xyz"));
        }

        [Fact]
        public void TryExpand_EmptyString_ReturnsNull()
        {
            Assert.Null(AbbreviationExpander.TryExpand(""));
        }

        [Fact]
        public void TryExpand_Null_ReturnsNull()
        {
            Assert.Null(AbbreviationExpander.TryExpand(null!));
        }

        [Fact]
        public void TryExpand_PeriodOnly_ReturnsNull()
        {
            // "." → TrimEnd('.') → "" → Length==0 → null
            Assert.Null(AbbreviationExpander.TryExpand("."));
        }

        [Theory]
        [InlineData("Prof.", "Professor")]
        [InlineData("inc.", "Incorporated")]
        [InlineData("ltd.", "Limited")]
        public void TryExpand_VariousAbbreviations_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        [Theory]
        [InlineData("Ms.", "Miz")]
        [InlineData("ms.", "Miz")]
        [InlineData("Ms", "Miz")]
        public void TryExpand_Ms_ReturnsMiz(string input, string expected)
        {
            // "Ms." は発音 "Miz" に展開される（"Miss" ではない）
            Assert.Equal(expected, AbbreviationExpander.TryExpand(input));
        }

        // ===== 追加エッジケース =====

        [Fact]
        public void TryExpand_NoPeriod_ReturnsNumber()
        {
            // "No." → 大文字始まりなので "Number"
            Assert.Equal("Number", AbbreviationExpander.TryExpand("No."));
        }

        [Fact]
        public void TryExpand_NoWithoutPeriod_ReturnsNumber()
        {
            // "No" → 大文字始まりなので "Number"
            Assert.Equal("Number", AbbreviationExpander.TryExpand("No"));
        }

        [Fact]
        public void TryExpand_LowercaseNo_ReturnsNull()
        {
            // "no" → 小文字始まりなのでnull（"no"は一般単語として扱う）
            Assert.Null(AbbreviationExpander.TryExpand("no"));
        }

        [Fact]
        public void TryExpand_St_ReturnsStreet()
        {
            // "St" → "Street"
            Assert.Equal("Street", AbbreviationExpander.TryExpand("St"));
        }

        [Fact]
        public void TryExpand_StWithPeriod_ReturnsStreet()
        {
            // "St." → "Street"
            Assert.Equal("Street", AbbreviationExpander.TryExpand("St."));
        }
    }
}
