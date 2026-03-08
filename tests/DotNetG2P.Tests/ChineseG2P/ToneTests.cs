using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Tone enum の単体テスト。
    /// </summary>
    public class ToneTests
    {
        [Fact]
        public void Tone_HasFiveValues()
        {
            // Neutral(0) + First(1) + Second(2) + Third(3) + Fourth(4) = 5
            var values = Enum.GetValues(typeof(Tone)).Cast<Tone>().ToArray();
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public void Tone_UnderlyingTypeIsByte()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Tone)));
        }

        [Theory]
        [InlineData(Tone.Neutral, 0)]
        [InlineData(Tone.First, 1)]
        [InlineData(Tone.Second, 2)]
        [InlineData(Tone.Third, 3)]
        [InlineData(Tone.Fourth, 4)]
        public void Tone_HasExpectedNumericValues(Tone tone, int expected)
        {
            Assert.Equal(expected, (int)tone);
        }

        [Fact]
        public void Tone_Neutral_IsDefault()
        {
            // default(Tone) は 0 = Neutral
            Assert.Equal(Tone.Neutral, default(Tone));
        }
    }
}
