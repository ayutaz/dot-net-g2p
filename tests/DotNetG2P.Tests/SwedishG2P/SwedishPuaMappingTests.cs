using System;
using System.Linq;
using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Conversion;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class SwedishPuaMappingTests : IDisposable
    {
        private readonly SwedishG2PEngine _centralEngine = new SwedishG2PEngine();
        private readonly SwedishG2PEngine _finlandEngine = new SwedishG2PEngine(
            new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish));

        public void Dispose()
        {
            _centralEngine.Dispose();
            _finlandEngine.Dispose();
        }

        // =================================================================
        // SwedishPuaMapper 単体テスト
        // =================================================================

        [Theory]
        [InlineData("p")]
        [InlineData("b")]
        [InlineData("t")]
        [InlineData("s")]
        [InlineData("h")]
        [InlineData("n")]
        [InlineData("l")]
        [InlineData("a")]
        public void MapToPua_基本音素_そのまま返却(string phoneme)
        {
            var result = SwedishPuaMapper.MapToPua(phoneme);
            Assert.Equal(phoneme, result);
        }

        [Fact]
        public void MapToPua_FinlandSwedish_tj破擦音_0xE023()
        {
            // t͡ɕ (U+0074 U+0361 U+0255) → PUA 0xE023
            var result = SwedishPuaMapper.MapToPua("t\u0361\u0255");
            Assert.Equal("\uE023", result);
        }

        [Theory]
        [InlineData("\u0288")]  // ʈ (RetroT)
        [InlineData("\u0256")]  // ɖ (RetroD)
        [InlineData("\u0273")]  // ɳ (RetroN)
        [InlineData("\u026D")]  // ɭ (RetroL)
        [InlineData("\u0282")]  // ʂ (RetroS)
        public void MapToPua_そり舌音_IPA標準文字(string phoneme)
        {
            // そり舌音は単一IPA文字 → PUAマッピングなし → そのまま返る
            var result = SwedishPuaMapper.MapToPua(phoneme);
            Assert.Equal(phoneme, result);
        }

        [Fact]
        public void MapToPua_null_返却()
        {
            Assert.Null(SwedishPuaMapper.MapToPua(null!));
            Assert.Equal("", SwedishPuaMapper.MapToPua(""));
        }

        [Fact]
        public void ApplyPuaMapping_空配列_空配列()
        {
            Assert.Empty(SwedishPuaMapper.ApplyPuaMapping(null!));
            Assert.Empty(SwedishPuaMapper.ApplyPuaMapping(Array.Empty<string>()));
        }

        // =================================================================
        // エンジン経由 PUA 出力テスト
        // =================================================================

        [Fact]
        public void ToPuaPhonemes_配列長()
        {
            // "hej" のPUA変換結果が空でなく、ToPhonemeListと同数であること
            var puaResult = _centralEngine.ToPuaPhonemes("hej");
            var phonemeList = _centralEngine.ToPhonemeList("hej");

            Assert.True(puaResult.Length > 0);
            Assert.Equal(phonemeList.Count, puaResult.Length);
        }

        [Fact]
        public void ToPuaString_スペース区切り()
        {
            var puaString = _centralEngine.ToPuaString("hej");
            Assert.False(string.IsNullOrEmpty(puaString));
            // スペースで区切られた結果であること
            Assert.Contains(" ", puaString);
        }

        [Fact]
        public void ToPuaStringBatch_複数テキスト()
        {
            var result = _centralEngine.ToPuaStringBatch(new[] { "hej", "ja", "" });

            Assert.Equal(3, result.Count);
            Assert.False(string.IsNullOrEmpty(result[0]));
            Assert.False(string.IsNullOrEmpty(result[1]));
            Assert.Equal("", result[2]);
        }

        [Fact]
        public void ToPuaPhonemes_FinlandSwedish方言_差異()
        {
            // "kök" → Central: ɕ (PUA変換なし), Finland: t͡ɕ → PUA 0xE023
            var centralResult = _centralEngine.ToPuaPhonemes("kök");
            var finlandResult = _finlandEngine.ToPuaPhonemes("kök");

            // Central はPUA文字を含まない
            Assert.All(centralResult, p => Assert.True(p[0] < '\uE000' || p[0] > '\uF8FF',
                $"Central方言でPUA文字 '{p}' (U+{((int)p[0]):X4}) が検出された"));

            // Finland は PUA 0xE023 を含む
            Assert.Contains("\uE023", finlandResult);
        }

        [Fact]
        public void PuaMapper_Central_恒等変換()
        {
            // Central方言では t͡ɕ が出現しないため、全音素がPUA変換なしで通過する
            var phonemes = _centralEngine.ToPuaPhonemes("hej");
            var ipaPhonemes = _centralEngine.ToIpaWithProsody("hej").Phonemes;

            // PUA結果とIPA結果が同一であること（Central にはPUA対象音素がない）
            Assert.Equal(ipaPhonemes.Length, phonemes.Length);
            for (var i = 0; i < phonemes.Length; i++)
                Assert.Equal(ipaPhonemes[i], phonemes[i]);
        }
    }
}
