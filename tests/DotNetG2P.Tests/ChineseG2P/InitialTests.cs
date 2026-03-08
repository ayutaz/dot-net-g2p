using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Initial enum の単体テスト。
    /// </summary>
    public class InitialTests
    {
        [Fact]
        public void Initial_HasCorrectCount()
        {
            // None(1) + 21声母 + Y + W = 24
            var values = Enum.GetValues(typeof(Initial)).Cast<Initial>().ToArray();
            Assert.Equal(24, values.Length);
        }

        [Fact]
        public void Initial_UnderlyingTypeIsByte()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Initial)));
        }

        [Fact]
        public void Initial_None_IsZero()
        {
            Assert.Equal((byte)0, (byte)Initial.None);
        }

        [Theory]
        [InlineData(Initial.B, 1)]
        [InlineData(Initial.P, 2)]
        [InlineData(Initial.M, 3)]
        [InlineData(Initial.F, 4)]
        [InlineData(Initial.D, 5)]
        [InlineData(Initial.T, 6)]
        [InlineData(Initial.N, 7)]
        [InlineData(Initial.L, 8)]
        [InlineData(Initial.G, 9)]
        [InlineData(Initial.K, 10)]
        [InlineData(Initial.H, 11)]
        public void Initial_ConsonantsHaveSequentialValues(Initial initial, int expected)
        {
            Assert.Equal(expected, (int)initial);
        }

        [Theory]
        [InlineData(Initial.J)]
        [InlineData(Initial.Q)]
        [InlineData(Initial.X)]
        public void Initial_PalatalInitials_AreDefined(Initial initial)
        {
            Assert.True(Enum.IsDefined(typeof(Initial), initial));
        }

        [Theory]
        [InlineData(Initial.Zh)]
        [InlineData(Initial.Ch)]
        [InlineData(Initial.Sh)]
        [InlineData(Initial.R)]
        public void Initial_RetroflexInitials_AreDefined(Initial initial)
        {
            Assert.True(Enum.IsDefined(typeof(Initial), initial));
        }

        [Theory]
        [InlineData(Initial.Z)]
        [InlineData(Initial.C)]
        [InlineData(Initial.S)]
        public void Initial_AlveolarAffricates_AreDefined(Initial initial)
        {
            Assert.True(Enum.IsDefined(typeof(Initial), initial));
        }

        [Theory]
        [InlineData(Initial.Y)]
        [InlineData(Initial.W)]
        public void Initial_SemiVowels_AreDefined(Initial initial)
        {
            Assert.True(Enum.IsDefined(typeof(Initial), initial));
        }

        [Fact]
        public void Initial_W_IsLastValue()
        {
            // W が最後の値（23）
            Assert.Equal(23, (int)Initial.W);
        }
    }
}
