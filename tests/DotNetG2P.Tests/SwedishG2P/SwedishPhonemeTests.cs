using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishPhonemeTests
    {
        // =====================================================================
        // SwedishIpaPhoneme enum 値テスト
        // =====================================================================

        [Fact]
        public void SwedishIpaPhoneme_LongI_HasValue0()
        {
            Assert.Equal((byte)0, (byte)SwedishIpaPhoneme.LongI);
        }

        [Fact]
        public void SwedishIpaPhoneme_RetroS_HasValue40()
        {
            Assert.Equal((byte)40, (byte)SwedishIpaPhoneme.RetroS);
        }

        [Fact]
        public void SwedishIpaPhoneme_TotalCount_Is42()
        {
            Assert.Equal(42, Enum.GetValues(typeof(SwedishIpaPhoneme)).Length);
        }

        // =====================================================================
        // SwedishPhoneme struct プロパティテスト
        // =====================================================================

        [Fact]
        public void SwedishPhoneme_IsVowel_LongI_ReturnsTrue()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.LongI);
            Assert.True(p.IsVowel);
        }

        [Fact]
        public void SwedishPhoneme_IsVowel_P_ReturnsFalse()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.P);
            Assert.False(p.IsVowel);
        }

        [Fact]
        public void SwedishPhoneme_IsConsonant_P_ReturnsTrue()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.P);
            Assert.True(p.IsConsonant);
        }

        [Fact]
        public void SwedishPhoneme_IsRetroflex_RetroT_ReturnsTrue()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.RetroT);
            Assert.True(p.IsRetroflex);
        }

        [Fact]
        public void SwedishPhoneme_IsRetroflex_T_ReturnsFalse()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.T);
            Assert.False(p.IsRetroflex);
        }

        [Fact]
        public void SwedishPhoneme_IsLongVowel_LongA_ReturnsTrue()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.LongA);
            Assert.True(p.IsLongVowel);
        }

        [Fact]
        public void SwedishPhoneme_IsLongVowel_ShortA_ReturnsFalse()
        {
            var p = new SwedishPhoneme(SwedishIpaPhoneme.ShortA);
            Assert.False(p.IsLongVowel);
        }

        // =====================================================================
        // SwedishPhoneme 等価性テスト
        // =====================================================================

        [Fact]
        public void SwedishPhoneme_Equals_SameValues_ReturnsTrue()
        {
            var a = new SwedishPhoneme(SwedishIpaPhoneme.LongI, isStressed: true);
            var b = new SwedishPhoneme(SwedishIpaPhoneme.LongI, isStressed: true);
            Assert.Equal(a, b);
        }

        [Fact]
        public void SwedishPhoneme_Equals_DifferentStress_ReturnsFalse()
        {
            var a = new SwedishPhoneme(SwedishIpaPhoneme.LongI, isStressed: true);
            var b = new SwedishPhoneme(SwedishIpaPhoneme.LongI, isStressed: false);
            Assert.NotEqual(a, b);
        }

        // =====================================================================
        // SwedishDialect enum テスト
        // =====================================================================

        [Fact]
        public void SwedishDialect_Central_HasValue0()
        {
            Assert.Equal((byte)0, (byte)SwedishDialect.Central);
        }

        [Fact]
        public void SwedishDialect_FinlandSwedish_HasValue1()
        {
            Assert.Equal((byte)1, (byte)SwedishDialect.FinlandSwedish);
        }
    }
}
