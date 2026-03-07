using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// Final enum の単体テスト。
    /// </summary>
    public class FinalTests
    {
        [Fact]
        public void Final_HasCorrectCount()
        {
            // None(1) + 35韻母 + Er = 37
            var values = Enum.GetValues(typeof(Final)).Cast<Final>().ToArray();
            Assert.Equal(37, values.Length);
        }

        [Fact]
        public void Final_UnderlyingTypeIsByte()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Final)));
        }

        [Fact]
        public void Final_None_IsZero()
        {
            Assert.Equal((byte)0, (byte)Final.None);
        }

        [Theory]
        [InlineData(Final.A, 1)]
        [InlineData(Final.O, 2)]
        [InlineData(Final.E, 3)]
        public void Final_SimpleVowels_HaveSequentialValues(Final final_, int expected)
        {
            Assert.Equal(expected, (int)final_);
        }

        [Theory]
        [InlineData(Final.Ai)]
        [InlineData(Final.Ei)]
        [InlineData(Final.Ao)]
        [InlineData(Final.Ou)]
        public void Final_CompoundVowels_AreDefined(Final final_)
        {
            Assert.True(Enum.IsDefined(typeof(Final), final_));
        }

        [Theory]
        [InlineData(Final.An)]
        [InlineData(Final.En)]
        [InlineData(Final.Ang)]
        [InlineData(Final.Eng)]
        [InlineData(Final.Ong)]
        public void Final_NasalFinals_AreDefined(Final final_)
        {
            Assert.True(Enum.IsDefined(typeof(Final), final_));
        }

        [Theory]
        [InlineData(Final.I)]
        [InlineData(Final.Ia)]
        [InlineData(Final.Ie)]
        [InlineData(Final.Iao)]
        [InlineData(Final.Iu)]
        [InlineData(Final.Ian)]
        [InlineData(Final.In)]
        [InlineData(Final.Iang)]
        [InlineData(Final.Ing)]
        [InlineData(Final.Iong)]
        public void Final_ISeriesFinals_AreDefined(Final final_)
        {
            Assert.True(Enum.IsDefined(typeof(Final), final_));
        }

        [Theory]
        [InlineData(Final.U)]
        [InlineData(Final.Ua)]
        [InlineData(Final.Uo)]
        [InlineData(Final.Uai)]
        [InlineData(Final.Ui)]
        [InlineData(Final.Uan)]
        [InlineData(Final.Un)]
        [InlineData(Final.Uang)]
        [InlineData(Final.Ueng)]
        public void Final_USeriesFinals_AreDefined(Final final_)
        {
            Assert.True(Enum.IsDefined(typeof(Final), final_));
        }

        [Theory]
        [InlineData(Final.V)]
        [InlineData(Final.Ve)]
        [InlineData(Final.Van)]
        [InlineData(Final.Vn)]
        public void Final_VSeriesFinals_AreDefined(Final final_)
        {
            Assert.True(Enum.IsDefined(typeof(Final), final_));
        }

        [Fact]
        public void Final_Er_IsLastValue()
        {
            Assert.Equal(36, (int)Final.Er);
        }
    }
}
