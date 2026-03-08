using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinSyllable readonly struct の単体テスト。
    /// </summary>
    public class PinyinSyllableTests
    {
        // ===== コンストラクタ =====

        [Fact]
        public void Constructor_SetsProperties()
        {
            var s = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            Assert.Equal(Initial.Zh, s.Initial);
            Assert.Equal(Final.Ong, s.Final);
            Assert.Equal(Tone.First, s.Tone);
        }

        [Fact]
        public void Constructor_NoneInitial_SetsCorrectly()
        {
            var s = new PinyinSyllable(Initial.None, Final.A, Tone.Fourth);
            Assert.Equal(Initial.None, s.Initial);
            Assert.Equal(Final.A, s.Final);
            Assert.Equal(Tone.Fourth, s.Tone);
        }

        // ===== HasInitial =====

        [Fact]
        public void HasInitial_WithInitial_ReturnsTrue()
        {
            var s = new PinyinSyllable(Initial.B, Final.A, Tone.First);
            Assert.True(s.HasInitial);
        }

        [Fact]
        public void HasInitial_WithoutInitial_ReturnsFalse()
        {
            var s = new PinyinSyllable(Initial.None, Final.A, Tone.First);
            Assert.False(s.HasInitial);
        }

        // ===== IsNeutralTone =====

        [Fact]
        public void IsNeutralTone_Neutral_ReturnsTrue()
        {
            var s = new PinyinSyllable(Initial.M, Final.A, Tone.Neutral);
            Assert.True(s.IsNeutralTone);
        }

        [Fact]
        public void IsNeutralTone_NonNeutral_ReturnsFalse()
        {
            var s = new PinyinSyllable(Initial.M, Final.A, Tone.Third);
            Assert.False(s.IsNeutralTone);
        }

        // ===== ToString: 基本ケース =====

        [Theory]
        [InlineData(Initial.Zh, Final.Ong, Tone.First, "zhong1")]
        [InlineData(Initial.G, Final.Uo, Tone.Second, "guo2")]
        [InlineData(Initial.None, Final.A, Tone.Fourth, "a4")]
        [InlineData(Initial.B, Final.A, Tone.First, "ba1")]
        [InlineData(Initial.M, Final.Ei, Tone.Second, "mei2")]
        [InlineData(Initial.H, Final.Ao, Tone.Third, "hao3")]
        [InlineData(Initial.D, Final.E, Tone.Fourth, "de4")]
        public void ToString_BasicCases(Initial initial, Final final_, Tone tone, string expected)
        {
            var s = new PinyinSyllable(initial, final_, tone);
            Assert.Equal(expected, s.ToString());
        }

        // ===== ToString: 軽声（声調番号なし） =====

        [Fact]
        public void ToString_NeutralTone_NoToneNumber()
        {
            var s = new PinyinSyllable(Initial.D, Final.E, Tone.Neutral);
            Assert.Equal("de", s.ToString());
        }

        // ===== ToString: j/q/x + ü系 → u表記 =====

        [Theory]
        [InlineData(Initial.J, Final.V, Tone.Third, "ju3")]
        [InlineData(Initial.Q, Final.Ve, Tone.Fourth, "que4")]
        [InlineData(Initial.X, Final.Van, Tone.Second, "xuan2")]
        [InlineData(Initial.J, Final.Vn, Tone.First, "jun1")]
        [InlineData(Initial.Q, Final.V, Tone.First, "qu1")]
        [InlineData(Initial.X, Final.Ve, Tone.Second, "xue2")]
        public void ToString_PalatalWithV_UsesUNotation(Initial initial, Final final_, Tone tone, string expected)
        {
            var s = new PinyinSyllable(initial, final_, tone);
            Assert.Equal(expected, s.ToString());
        }

        // ===== ToString: l/n + ü系 → ü表記 =====

        [Theory]
        [InlineData(Initial.L, Final.V, Tone.Third, "l\u00fc3")]     // lü3
        [InlineData(Initial.N, Final.Ve, Tone.Fourth, "n\u00fce4")]  // nüe4
        [InlineData(Initial.L, Final.Van, Tone.Second, "l\u00fcan2")] // lüan2
        [InlineData(Initial.N, Final.Vn, Tone.First, "n\u00fcn1")]   // nün1
        public void ToString_NonPalatalWithV_UsesUmlautNotation(Initial initial, Final final_, Tone tone, string expected)
        {
            var s = new PinyinSyllable(initial, final_, tone);
            Assert.Equal(expected, s.ToString());
        }

        // ===== ToString: そり舌音 =====

        [Theory]
        [InlineData(Initial.Zh, Final.I, Tone.First, "zhi1")]
        [InlineData(Initial.Ch, Final.Ang, Tone.Second, "chang2")]
        [InlineData(Initial.Sh, Final.En, Tone.First, "shen1")]
        [InlineData(Initial.R, Final.En, Tone.Second, "ren2")]
        public void ToString_RetroflexInitials(Initial initial, Final final_, Tone tone, string expected)
        {
            var s = new PinyinSyllable(initial, final_, tone);
            Assert.Equal(expected, s.ToString());
        }

        // ===== ToString: 特殊韻母 er =====

        [Fact]
        public void ToString_Er_Syllable()
        {
            var s = new PinyinSyllable(Initial.None, Final.Er, Tone.Fourth);
            Assert.Equal("er4", s.ToString());
        }

        // ===== Equality =====

        [Fact]
        public void Equals_SameSyllables_ReturnsTrue()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
        }

        [Fact]
        public void Equals_DifferentInitial_ReturnsFalse()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.Ch, Final.Ong, Tone.First);
            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void Equals_DifferentFinal_ReturnsFalse()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.Zh, Final.Eng, Tone.First);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentTone_ReturnsFalse()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.Fourth);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_ObjectOverload_WorksCorrectly()
        {
            var a = new PinyinSyllable(Initial.B, Final.A, Tone.First);
            object b = new PinyinSyllable(Initial.B, Final.A, Tone.First);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var a = new PinyinSyllable(Initial.B, Final.A, Tone.First);
            Assert.False(a.Equals(null));
        }

        // ===== GetHashCode =====

        [Fact]
        public void GetHashCode_SameSyllables_ReturnsSameHash()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentSyllables_ReturnsDifferentHash()
        {
            var a = new PinyinSyllable(Initial.Zh, Final.Ong, Tone.First);
            var b = new PinyinSyllable(Initial.G, Final.Uo, Tone.Second);
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        // ===== Default struct =====

        [Fact]
        public void Default_IsNoneNoneNeutral()
        {
            var s = default(PinyinSyllable);
            Assert.Equal(Initial.None, s.Initial);
            Assert.Equal(Final.None, s.Final);
            Assert.Equal(Tone.Neutral, s.Tone);
            Assert.False(s.HasInitial);
            Assert.True(s.IsNeutralTone);
        }
    }
}
