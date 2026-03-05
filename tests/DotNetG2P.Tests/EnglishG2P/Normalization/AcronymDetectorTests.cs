// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English.Normalization;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// AcronymDetector の単体テスト。
    /// </summary>
    public class AcronymDetectorTests
    {
        // --- IsAllUpperCase ---

        [Theory]
        [InlineData("NASA", true)]
        [InlineData("AB", true)]
        [InlineData("API", true)]
        public void IsAllUpperCase_AllUpperLetters_ReturnsTrue(string input, bool expected)
        {
            Assert.Equal(expected, AcronymDetector.IsAllUpperCase(input));
        }

        [Theory]
        [InlineData("Api", false)]    // 小文字混在
        [InlineData("A", false)]      // 1文字
        [InlineData("abc", false)]    // 全小文字
        [InlineData("A1B", false)]    // 数字混在
        [InlineData("", false)]       // 空文字列
        public void IsAllUpperCase_NonAllUpper_ReturnsFalse(string input, bool expected)
        {
            Assert.Equal(expected, AcronymDetector.IsAllUpperCase(input));
        }

        [Fact]
        public void IsAllUpperCase_Null_ReturnsFalse()
        {
            Assert.False(AcronymDetector.IsAllUpperCase(null!));
        }

        // --- ShouldSpellOut ---

        [Theory]
        [InlineData("API", true)]   // スペルアウト辞書
        [InlineData("FBI", true)]   // スペルアウト辞書
        [InlineData("CPU", true)]   // スペルアウト辞書
        [InlineData("AI", true)]    // スペルアウト辞書 + 2文字
        public void ShouldSpellOut_KnownSpellOutTokens_ReturnsTrue(string input, bool expected)
        {
            Assert.Equal(expected, AcronymDetector.ShouldSpellOut(input));
        }

        [Theory]
        [InlineData("NASA", false)]  // 1語読み辞書
        [InlineData("NATO", false)]  // 1語読み辞書
        [InlineData("FEMA", false)]  // 1語読み辞書
        public void ShouldSpellOut_KnownAcronymTokens_ReturnsFalse(string input, bool expected)
        {
            Assert.Equal(expected, AcronymDetector.ShouldSpellOut(input));
        }

        [Fact]
        public void ShouldSpellOut_TwoLetterUnknown_ReturnsTrue()
        {
            // 2文字は常にスペルアウト（ヒューリスティック）
            Assert.True(AcronymDetector.ShouldSpellOut("XY"));
        }

        [Fact]
        public void ShouldSpellOut_HeuristicWithVowelAndShortConsonantRun_ReturnsFalse()
        {
            // YOLO: 母音あり、子音クラスター短い → 1語読み
            Assert.False(AcronymDetector.ShouldSpellOut("YOLO"));
        }

        [Fact]
        public void ShouldSpellOut_NoVowels_ReturnsTrue()
        {
            // FPS: 母音なし → スペルアウト
            Assert.True(AcronymDetector.ShouldSpellOut("FPS"));
        }

        [Fact]
        public void ShouldSpellOut_LongConsonantCluster_ReturnsTrue()
        {
            // CTRL: 子音連続3文字以上 → スペルアウト
            Assert.True(AcronymDetector.ShouldSpellOut("CTRL"));
        }

        [Fact]
        public void ShouldSpellOut_NonUpperCase_ReturnsFalse()
        {
            // IsAllUpperCase がfalseならShouldSpellOutもfalse
            Assert.False(AcronymDetector.ShouldSpellOut("Api"));
        }

        // --- SpellOut ---

        [Theory]
        [InlineData("API", "A P I")]
        [InlineData("FBI", "F B I")]
        [InlineData("AB", "A B")]
        public void SpellOut_ValidTokens_ReturnsSpaceSeparated(string input, string expected)
        {
            Assert.Equal(expected, AcronymDetector.SpellOut(input));
        }

        [Fact]
        public void SpellOut_SingleChar_ReturnsSameChar()
        {
            Assert.Equal("A", AcronymDetector.SpellOut("A"));
        }

        [Fact]
        public void SpellOut_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, AcronymDetector.SpellOut(""));
        }

        [Fact]
        public void SpellOut_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, AcronymDetector.SpellOut(null!));
        }
    }
}
