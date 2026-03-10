using DotNetG2P.French;
using DotNetG2P.French.Rules;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// NasalVowelizer の単体テスト。
    /// TryNasalize メソッドを直接テストする（InternalsVisibleTo により内部クラスにアクセス可能）。
    /// </summary>
    public class NasalVowelizerTests
    {
        // ========== 各母音 + n/m の鼻母音化（12パターン） ==========

        [Theory]
        // a + n → /ɑ̃/ (ANasal)
        [InlineData("an", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // a + m → /ɑ̃/ (ANasal)
        [InlineData("am", 0, 'm', FrenchIpaPhoneme.ANasal)]
        // e + n → /ɑ̃/ (ANasal) — 歴史的にenは/ɑ̃/
        [InlineData("en", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // e + m → /ɑ̃/ (ANasal)
        [InlineData("em", 0, 'm', FrenchIpaPhoneme.ANasal)]
        // o + n → /ɔ̃/ (ONasal)
        [InlineData("on", 0, 'n', FrenchIpaPhoneme.ONasal)]
        // o + m → /ɔ̃/ (ONasal)
        [InlineData("om", 0, 'm', FrenchIpaPhoneme.ONasal)]
        // i + n → /ɛ̃/ (ENasal)
        [InlineData("in", 0, 'n', FrenchIpaPhoneme.ENasal)]
        // i + m → /ɛ̃/ (ENasal)
        [InlineData("im", 0, 'm', FrenchIpaPhoneme.ENasal)]
        // u + n → /ɛ̃/ (ENasal) — Metropolitan
        [InlineData("un", 0, 'n', FrenchIpaPhoneme.ENasal)]
        // u + m → /ɛ̃/ (ENasal) — Metropolitan
        [InlineData("um", 0, 'm', FrenchIpaPhoneme.ENasal)]
        // y + n → /ɛ̃/ (ENasal)
        [InlineData("yn", 0, 'n', FrenchIpaPhoneme.ENasal)]
        // y + m → /ɛ̃/ (ENasal)
        [InlineData("ym", 0, 'm', FrenchIpaPhoneme.ENasal)]
        public void TryNasalize_VowelPlusNasalAtEnd_ReturnsTrue(
            string word, int vowelIndex, char nasal, FrenchIpaPhoneme expectedPhoneme)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out var phoneme, out var consumed);

            Assert.True(result);
            Assert.Equal(expectedPhoneme, phoneme);
            Assert.Equal(2, consumed);
        }

        // ========== 非鼻母音化: 後続母音（母音間のn/mは鼻母音化しない） ==========

        [Theory]
        [InlineData("ane", 0, 'n')]   // a + n + e → 非鼻母音化
        [InlineData("ami", 0, 'm')]   // a + m + i → 非鼻母音化
        [InlineData("one", 0, 'n')]   // o + n + e → 非鼻母音化
        [InlineData("une", 0, 'n')]   // u + n + e → 非鼻母音化
        public void TryNasalize_FollowedByVowel_ReturnsFalse(
            string word, int vowelIndex, char nasal)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out _, out _);

            Assert.False(result);
        }

        // ========== 非鼻母音化: nn/mm重複 ==========

        [Theory]
        [InlineData("anne", 0, 'n')]  // a + nn → 非鼻母音化
        [InlineData("amme", 0, 'm')] // a + mm → 非鼻母音化
        [InlineData("onne", 0, 'n')] // o + nn → 非鼻母音化
        public void TryNasalize_DoubledNasal_ReturnsFalse(
            string word, int vowelIndex, char nasal)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out _, out _);

            Assert.False(result);
        }

        // ========== 非鼻母音化: h+母音透過 ==========

        [Fact]
        public void TryNasalize_NasalPlusHPlusVowel_ReturnsFalse()
        {
            // "inhaler" パターン: i + n + h + a → h は透過的で実質母音間 → 非鼻母音化
            var result = NasalVowelizer.TryNasalize(
                "inha", 0, 'n', FrenchDialect.Metropolitan,
                out _, out _);

            Assert.False(result);
        }

        // ========== アクセント付き母音 + n/m ==========

        [Theory]
        // é + n → /ɑ̃/ (ANasal)
        [InlineData("\u00E9n", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // è + n → /ɑ̃/ (ANasal)
        [InlineData("\u00E8n", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // ê + n → /ɑ̃/ (ANasal)
        [InlineData("\u00EAn", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // à + n → /ɑ̃/ (ANasal)
        [InlineData("\u00E0n", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // â + n → /ɑ̃/ (ANasal)
        [InlineData("\u00E2n", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // î + n → /ɛ̃/ (ENasal)
        [InlineData("\u00EEn", 0, 'n', FrenchIpaPhoneme.ENasal)]
        // ô + n → /ɔ̃/ (ONasal)
        [InlineData("\u00F4n", 0, 'n', FrenchIpaPhoneme.ONasal)]
        // û + n → /ɛ̃/ (ENasal) — Metropolitan
        [InlineData("\u00FBn", 0, 'n', FrenchIpaPhoneme.ENasal)]
        public void TryNasalize_AccentedVowelPlusNasal_ReturnsCorrectPhoneme(
            string word, int vowelIndex, char nasal, FrenchIpaPhoneme expectedPhoneme)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out var phoneme, out var consumed);

            Assert.True(result);
            Assert.Equal(expectedPhoneme, phoneme);
            Assert.Equal(2, consumed);
        }

        // ========== Conservative方言: u+n → /œ̃/ (OeNasal) ==========

        [Theory]
        [InlineData("un", 0, 'n')]
        [InlineData("um", 0, 'm')]
        public void TryNasalize_Conservative_UnBecomesOeNasal(
            string word, int vowelIndex, char nasal)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Conservative,
                out var phoneme, out _);

            Assert.True(result);
            Assert.Equal(FrenchIpaPhoneme.OeNasal, phoneme);
        }

        // ========== Metropolitan方言: u+n → /ɛ̃/ (ENasal) ==========

        [Theory]
        [InlineData("un", 0, 'n')]
        [InlineData("um", 0, 'm')]
        public void TryNasalize_Metropolitan_UnBecomesENasal(
            string word, int vowelIndex, char nasal)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out var phoneme, out _);

            Assert.True(result);
            Assert.Equal(FrenchIpaPhoneme.ENasal, phoneme);
        }

        // ========== 鼻母音化 + 後続子音（語末でない場合） ==========

        [Theory]
        // a + n + 子音 → 鼻母音化する
        [InlineData("ant", 0, 'n', FrenchIpaPhoneme.ANasal)]
        // o + m + 子音 → 鼻母音化する
        [InlineData("omb", 0, 'm', FrenchIpaPhoneme.ONasal)]
        // i + n + 子音 → 鼻母音化する
        [InlineData("ind", 0, 'n', FrenchIpaPhoneme.ENasal)]
        public void TryNasalize_FollowedByConsonant_ReturnsTrue(
            string word, int vowelIndex, char nasal, FrenchIpaPhoneme expectedPhoneme)
        {
            var result = NasalVowelizer.TryNasalize(
                word, vowelIndex, nasal, FrenchDialect.Metropolitan,
                out var phoneme, out _);

            Assert.True(result);
            Assert.Equal(expectedPhoneme, phoneme);
        }
    }
}
