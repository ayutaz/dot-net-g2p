// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// TimeExpander の時刻展開テスト。
    /// </summary>
    public class TimeExpanderTests
    {
        // ===== 通常の時刻パターン =====

        [Fact]
        public void TryExpand_StandardTime()
        {
            // 3:14 → "three fourteen"
            Assert.Equal("three fourteen", TimeExpander.TryExpand("3:14"));
        }

        [Fact]
        public void TryExpand_OnTheHour()
        {
            // 3:00 → "three o'clock"
            Assert.Equal("three o'clock", TimeExpander.TryExpand("3:00"));
        }

        [Fact]
        public void TryExpand_TwelveThirty()
        {
            Assert.Equal("twelve thirty", TimeExpander.TryExpand("12:30"));
        }

        [Fact]
        public void TryExpand_OhMinutes()
        {
            // 3:05 → "three oh five"（分が1-9の場合は "oh" を挿入）
            Assert.Equal("three oh five", TimeExpander.TryExpand("3:05"));
        }

        [Fact]
        public void TryExpand_TenFifteen()
        {
            Assert.Equal("ten fifteen", TimeExpander.TryExpand("10:15"));
        }

        // ===== 0時 → 12として読む =====

        [Fact]
        public void TryExpand_Midnight()
        {
            // 0:00 → 0時は12として表示: "twelve o'clock"
            Assert.Equal("twelve o'clock", TimeExpander.TryExpand("0:00"));
        }

        // ===== 24時間制（13時以降） =====

        [Fact]
        public void TryExpand_TwentyFourHourFormat()
        {
            // 23:59 → 13時以降は24時間制読み: "twenty three fifty nine"
            Assert.Equal("twenty three fifty nine", TimeExpander.TryExpand("23:59"));
        }

        [Fact]
        public void TryExpand_ThirteenHundred()
        {
            // 13:00 → "thirteen hundred"
            Assert.Equal("thirteen hundred", TimeExpander.TryExpand("13:00"));
        }

        // ===== 非時刻・エッジケース =====

        [Fact]
        public void TryExpand_NonTime_ReturnsNull()
        {
            Assert.Null(TimeExpander.TryExpand("hello"));
        }

        [Fact]
        public void TryExpand_OutOfRange_ReturnsNull()
        {
            // 25:00 → 範囲外（時間0-23）
            Assert.Null(TimeExpander.TryExpand("25:00"));
        }
    }
}
