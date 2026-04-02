using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語G2P精度テスト。
    /// 基本語彙、sj音、tj音、そり舌音、黙字のパターンを検証する。
    /// </summary>
    public class SwedishAccuracyTests : IDisposable
    {
        private readonly SwedishG2PEngine _engine = new SwedishG2PEngine();

        public void Dispose() => _engine.Dispose();

        // =====================================================================
        // 基本語彙（12テスト）
        // =====================================================================

        [Theory]
        [InlineData("hej", "\u02C8he\u02D0j")]         // ˈheːj
        [InlineData("ja", "\u02C8j\u0251\u02D0")]       // ˈjɑː
        [InlineData("nej", "\u02C8ne\u02D0j")]           // ˈneːj
        [InlineData("hus", "\u02C8h\u0289\u02D0s")]     // ˈhʉːs
        [InlineData("bil", "\u02C8bi\u02D0l")]           // ˈbiːl
        [InlineData("sol", "\u02C8su\u02D0l")]           // ˈsuːl
        [InlineData("mat", "\u02C8m\u0251\u02D0t")]      // ˈmɑːt
        [InlineData("bok", "\u02C8bu\u02D0k")]           // ˈbuːk
        [InlineData("dag", "\u02C8d\u0251\u02D0\u0261")] // ˈdɑːɡ
        [InlineData("man", "\u02C8m\u0251\u02D0n")]      // ˈmɑːn
        [InlineData("ful", "\u02C8f\u0289\u02D0l")]      // ˈfʉːl
        [InlineData("god", "\u02C8\u0261u\u02D0d")]      // ˈɡuːd
        public void ToIPA_BasicVocabulary_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // sj音パターン（4テスト）
        // =====================================================================

        [Theory]
        [InlineData("sjuk", "\u02C8\u0267\u0289\u02D0k")]       // ˈɧʉːk
        [InlineData("skjorta", "\u02C8\u0267\u028A\u0288a")]    // ˈɧʊʈa (skj→ɧ, o短→ʊ, rt→ʈ, 非ストレス末尾a短)
        [InlineData("stjärna", "\u02C8\u0267\u025B\u0273a")]    // ˈɧɛɳa (stj→ɧ, rn→ɳ, 非ストレス末尾a短)
        [InlineData("sked", "\u02C8\u0267e\u02D0d")]              // ˈɧeːd (sk+軟母音→ɧ)
        public void ToIPA_SjSound_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // tj音パターン（3テスト）
        // =====================================================================

        [Theory]
        [InlineData("tjock", "\u02C8\u0255\u028Ak")]             // ˈɕʊk (tj→ɕ, o短→ʊ)
        [InlineData("k\u00f6k", "\u02C8\u0255\u00f8\u02D0k")]   // ˈɕøːk (k+軟母音→ɕ)
        [InlineData("kyrka", "\u02C8\u0255\u028Frka")]           // ˈɕʏrka (k+y→ɕ, 非ストレス末尾a短)
        public void ToIPA_TjSound_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // そり舌音パターン（3テスト）
        // =====================================================================

        [Theory]
        [InlineData("bord", "\u02C8b\u028A\u0256")]       // ˈbʊɖ (rd→ɖ, o短→ʊ)
        [InlineData("barn", "\u02C8ba\u0273")]             // ˈbaɳ (rn→ɳ)
        [InlineData("fors", "\u02C8f\u028A\u0282")]        // ˈfʊʂ (rs→ʂ, o短→ʊ)
        public void ToIPA_Retroflexes_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // 黙字パターン（3テスト）
        // =====================================================================

        [Theory]
        [InlineData("djur", "\u02C8j\u0289\u02D0r")]     // ˈjʉːr (dj→j)
        [InlineData("ljus", "\u02C8j\u0289\u02D0s")]     // ˈjʉːs (lj→j)
        [InlineData("hj\u00e4rta", "\u02C8j\u025B\u0288a")] // ˈjɛʈa (hj→j, rt→ʈ, 非ストレス末尾a短)
        public void ToIPA_SilentLetters_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }
    }
}
