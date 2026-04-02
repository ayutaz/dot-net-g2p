using System;
using DotNetG2P.Swedish;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語G2P精度テスト。
    /// 基本語彙、長短母音、sj音、tj音、そり舌音、黙字、重子音、-tion/-sion、
    /// o+r変換、機能語のパターンを網羅的に検証する（50+テスト）。
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
        // 長短母音ミニマルペア（6テスト）
        // =====================================================================

        [Theory]
        [InlineData("mat", "\u02C8m\u0251\u02D0t")]     // ˈmɑːt（長母音：単子音末尾）
        [InlineData("matt", "\u02C8mat")]                // ˈmat（短母音：重子音で閉音節）
        [InlineData("bok", "\u02C8bu\u02D0k")]           // ˈbuːk（長母音）
        [InlineData("bock", "\u02C8b\u028Ak")]           // ˈbʊk（短母音：ck→短母音+k）
        [InlineData("hus", "\u02C8h\u0289\u02D0s")]     // ˈhʉːs（長母音）
        [InlineData("hund", "\u02C8h\u0275nd")]          // ˈhɵnd（短母音：子音連結nd）
        public void ToIPA_VowelLengthMinimalPairs_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // sj音パターン（8テスト）
        // =====================================================================

        [Theory]
        [InlineData("sjuk", "\u02C8\u0267\u0289\u02D0k")]           // ˈɧʉːk (sj→ɧ)
        [InlineData("skjorta", "\u02C8\u0267\u0254\u0288a")]        // ˈɧɔʈa (skj→ɧ, o+r→ɔ, rt→ʈ)
        [InlineData("stj\u00e4rna", "\u02C8\u0267\u025B\u0273a")]   // ˈɧɛɳa (stj→ɧ, rn→ɳ)
        [InlineData("sked", "\u02C8\u0267e\u02D0d")]                // ˈɧeːd (sk+軟母音→ɧ)
        [InlineData("station", "sta\u02C8\u0267u\u02D0n")]          // staˈɧuːn (-tion→ɧuːn)
        [InlineData("sk\u00f6n", "\u02C8\u0267\u00f8\u02D0n")]      // ˈɧøːn (sk+ö→ɧ)
        [InlineData("schema", "\u02C8\u0267e\u02D0ma")]             // ˈɧeːma (sch→ɧ)
        [InlineData("skjuta", "\u02C8\u0267\u0289\u02D0ta")]        // ˈɧʉːta (skj→ɧ)
        public void ToIPA_SjSound_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // tj音パターン（5テスト）
        // =====================================================================

        [Theory]
        [InlineData("tjock", "\u02C8\u0255\u028Ak")]                   // ˈɕʊk (tj→ɕ, o短→ʊ)
        [InlineData("k\u00f6k", "\u02C8\u0255\u00f8\u02D0k")]         // ˈɕøːk (k+軟母音→ɕ)
        [InlineData("kyrka", "\u02C8\u0255\u028Frka")]                 // ˈɕʏrka (k+y→ɕ)
        [InlineData("tjugo", "\u02C8\u0255\u0289\u02D0\u0261\u028A")] // ˈɕʉːɡʊ (tj→ɕ)
        [InlineData("k\u00f6pa", "\u02C8\u0255\u00f8\u02D0pa")]       // ˈɕøːpa (k+ö→ɕ)
        public void ToIPA_TjSound_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // そり舌音パターン（5テスト）
        // =====================================================================

        [Theory]
        [InlineData("bord", "\u02C8b\u0254\u0256")]       // ˈbɔɖ (rd→ɖ, o+r→ɔ)
        [InlineData("barn", "\u02C8ba\u0273")]             // ˈbaɳ (rn→ɳ)
        [InlineData("fors", "\u02C8f\u0254\u0282")]        // ˈfɔʂ (rs→ʂ, o+r→ɔ)
        [InlineData("karl", "\u02C8ka\u026D")]             // ˈkaɭ (rl→ɭ)
        [InlineData("ord", "\u02C8\u0254\u0256")]          // ˈɔɖ (o+rd→ɔɖ)
        public void ToIPA_Retroflexes_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // 黙字パターン（5テスト）
        // =====================================================================

        [Theory]
        [InlineData("djur", "\u02C8j\u0289\u02D0r")]              // ˈjʉːr (dj→j)
        [InlineData("ljus", "\u02C8j\u0289\u02D0s")]              // ˈjʉːs (lj→j)
        [InlineData("hj\u00e4rta", "\u02C8j\u025B\u02D0\u0288a")] // ˈjɛːʈa (hj→j, rt→ʈ)
        [InlineData("gnista", "\u02C8n\u026Asta")]                 // ˈnɪsta (gn→n)
        [InlineData("psalm", "\u02C8salm")]                        // ˈsalm (ps→s)
        public void ToIPA_SilentLetters_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // 重子音（ジェミネート）縮約（3テスト）
        // =====================================================================

        [Theory]
        [InlineData("hatt", "\u02C8hat")]             // ˈhat（tt→短母音+t）
        [InlineData("katt", "\u02C8kat")]             // ˈkat（tt→短母音+t）
        [InlineData("buss", "\u02C8b\u0275s")]        // ˈbɵs（ss→短母音+s）
        public void ToIPA_GeminateReduction_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // -tion/-sion語（2テスト）
        // =====================================================================

        [Theory]
        [InlineData("nation", "na\u02C8\u0267u\u02D0n")]    // naˈɧuːn (-tion→ɧuːn)
        [InlineData("passion", "pa\u02C8\u0267u\u02D0n")]   // paˈɧuːn (-sion→ɧuːn)
        public void ToIPA_TionSion_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // o+r変換パターン（3テスト）
        // =====================================================================

        [Theory]
        [InlineData("stor", "\u02C8sto\u02D0r")]     // ˈstoːr（o長母音+r、そり舌化なし）
        [InlineData("mor", "\u02C8mo\u02D0r")]        // ˈmoːr（o長母音+r）
        [InlineData("norr", "\u02C8n\u0254r")]        // ˈnɔr（o短母音+r→ɔ）
        public void ToIPA_OBeforeR_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // =====================================================================
        // 機能語（1テスト）
        // =====================================================================

        [Fact]
        public void ToIPA_Och_NoStressMark()
        {
            // "och" は例外辞書で弱形 /ɔ/ に変換、ストレスマークなし
            var result = _engine.ToIPA("och");
            Assert.Equal("\u0254", result); // ɔ（ストレスマーク無し）
        }

        // =====================================================================
        // 追加語彙（7テスト）
        // =====================================================================

        [Theory]
        [InlineData("son", "\u02C8su\u02D0n")]                           // ˈsuːn
        [InlineData("fin", "\u02C8fi\u02D0n")]                           // ˈfiːn
        [InlineData("l\u00e5ng", "\u02C8l\u0254\u014B")]                 // ˈlɔŋ（å→ɔ, ng→ŋ）
        [InlineData("\u00f6ga", "\u02C8\u00f8\u02D0\u0261a")]            // ˈøːɡa（ö長母音）
        [InlineData("hj\u00e4lp", "\u02C8j\u025Blp")]                   // ˈjɛlp（hj→j、ä短母音）
        [InlineData("ljud", "\u02C8j\u0289\u02D0d")]                     // ˈjʉːd（lj→j）
        [InlineData("glass", "\u02C8\u0261las")]                         // ˈɡlas（gl子音連結）
        public void ToIPA_AdditionalVocabulary_ReturnsExpectedIPA(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }
    }
}
