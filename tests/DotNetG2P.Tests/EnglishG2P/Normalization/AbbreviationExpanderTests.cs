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
    }
}
