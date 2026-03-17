using System;
using DotNetG2P.Korean;
using DotNetG2P.Korean.Conversion;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanPuaMappingTests
    {
        // ──────────────────────────────────────────────
        //  韓国語固有 PUA (0xE04B-0xE052) 個別検証
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToPua_FortisP_ReturnsPuaE04B()
        {
            // p͈ (ㅃ の IPA) → 0xE04B
            var result = PuaMapper.MapToPua("p\u0348");

            Assert.Equal("\uE04B", result);
        }

        [Fact]
        public void MapToPua_FortisT_ReturnsPuaE04C()
        {
            // t͈ (ㄸ の IPA) → 0xE04C
            var result = PuaMapper.MapToPua("t\u0348");

            Assert.Equal("\uE04C", result);
        }

        [Fact]
        public void MapToPua_FortisK_ReturnsPuaE04D()
        {
            // k͈ (ㄲ の IPA) → 0xE04D
            var result = PuaMapper.MapToPua("k\u0348");

            Assert.Equal("\uE04D", result);
        }

        [Fact]
        public void MapToPua_FortisS_ReturnsPuaE04E()
        {
            // s͈ (ㅆ の IPA) → 0xE04E
            var result = PuaMapper.MapToPua("s\u0348");

            Assert.Equal("\uE04E", result);
        }

        [Fact]
        public void MapToPua_FortisAffricate_ReturnsPuaE04F()
        {
            // t͈ɕ (ㅉ の IPA) → 0xE04F
            var result = PuaMapper.MapToPua("t\u0348\u0255");

            Assert.Equal("\uE04F", result);
        }

        [Fact]
        public void MapToPua_UnreleasedK_ReturnsPuaE050()
        {
            // k̚ (終声 ㄱ の IPA) → 0xE050
            var result = PuaMapper.MapToPua("k\u031A");

            Assert.Equal("\uE050", result);
        }

        [Fact]
        public void MapToPua_UnreleasedT_ReturnsPuaE051()
        {
            // t̚ (終声 ㄷ の IPA) → 0xE051
            var result = PuaMapper.MapToPua("t\u031A");

            Assert.Equal("\uE051", result);
        }

        [Fact]
        public void MapToPua_UnreleasedP_ReturnsPuaE052()
        {
            // p̚ (終声 ㅂ の IPA) → 0xE052
            var result = PuaMapper.MapToPua("p\u031A");

            Assert.Equal("\uE052", result);
        }

        // ──────────────────────────────────────────────
        //  共有 PUA (0xE020-0xE024) 個別検証
        // ──────────────────────────────────────────────

        [Fact]
        public void MapToPua_AspiratedP_ReturnsPuaE020()
        {
            // pʰ (ㅍ の IPA) → 0xE020
            var result = PuaMapper.MapToPua("p\u02B0");

            Assert.Equal("\uE020", result);
        }

        [Fact]
        public void MapToPua_AspiratedT_ReturnsPuaE021()
        {
            // tʰ (ㅌ の IPA) → 0xE021
            var result = PuaMapper.MapToPua("t\u02B0");

            Assert.Equal("\uE021", result);
        }

        [Fact]
        public void MapToPua_AspiratedK_ReturnsPuaE022()
        {
            // kʰ (ㅋ の IPA) → 0xE022
            var result = PuaMapper.MapToPua("k\u02B0");

            Assert.Equal("\uE022", result);
        }

        [Fact]
        public void MapToPua_AlveoloPalatalAffricate_ReturnsPuaE023()
        {
            // tɕ (ㅈ の IPA) → 0xE023
            var result = PuaMapper.MapToPua("t\u0255");

            Assert.Equal("\uE023", result);
        }

        [Fact]
        public void MapToPua_AspiratedAffricate_ReturnsPuaE024()
        {
            // tɕʰ (ㅊ の IPA) → 0xE024
            var result = PuaMapper.MapToPua("t\u0255\u02B0");

            Assert.Equal("\uE024", result);
        }

        // ──────────────────────────────────────────────
        //  PUA マッピングされない音素はそのまま出力
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData("a")]
        [InlineData("e")]
        [InlineData("i")]
        [InlineData("o")]
        [InlineData("u")]
        [InlineData("n")]
        [InlineData("m")]
        [InlineData("h")]
        [InlineData("s")]
        [InlineData("k")]
        [InlineData("l")]
        public void MapToPua_NonPuaPhoneme_ReturnsUnchanged(string phoneme)
        {
            var result = PuaMapper.MapToPua(phoneme);

            Assert.Equal(phoneme, result);
        }

        [Fact]
        public void MapToPua_NullOrEmpty_ReturnsAsIs()
        {
            Assert.Null(PuaMapper.MapToPua(null!));
            Assert.Equal("", PuaMapper.MapToPua(""));
        }

        // ──────────────────────────────────────────────
        //  ApplyPuaMapping 配列変換
        // ──────────────────────────────────────────────

        [Fact]
        public void ApplyPuaMapping_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(PuaMapper.ApplyPuaMapping(null!));
            Assert.Empty(PuaMapper.ApplyPuaMapping(Array.Empty<string>()));
        }

        [Fact]
        public void ApplyPuaMapping_MixedPhonemes_OnlyPuaMappedAreReplaced()
        {
            // "한" = ㅎㅏㄴ → IPA: h a n → PUA: h a n (変化なし)
            var input = new[] { "h", "a", "n" };

            var result = PuaMapper.ApplyPuaMapping(input);

            Assert.Equal(3, result.Length);
            Assert.Equal("h", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("n", result[2]);
        }

        [Fact]
        public void ApplyPuaMapping_AllPuaEntries_AreReplaced()
        {
            var input = new[]
            {
                "p\u0348",         // p͈  → E04B
                "t\u0348",         // t͈  → E04C
                "k\u0348",         // k͈  → E04D
                "s\u0348",         // s͈  → E04E
                "t\u0348\u0255",   // t͈ɕ → E04F
                "k\u031A",         // k̚  → E050
                "t\u031A",         // t̚  → E051
                "p\u031A",         // p̚  → E052
                "p\u02B0",         // pʰ → E020
                "t\u02B0",         // tʰ → E021
                "k\u02B0",         // kʰ → E022
                "t\u0255",         // tɕ → E023
                "t\u0255\u02B0",   // tɕʰ→ E024
            };

            var result = PuaMapper.ApplyPuaMapping(input);

            Assert.Equal(13, result.Length);
            Assert.Equal("\uE04B", result[0]);
            Assert.Equal("\uE04C", result[1]);
            Assert.Equal("\uE04D", result[2]);
            Assert.Equal("\uE04E", result[3]);
            Assert.Equal("\uE04F", result[4]);
            Assert.Equal("\uE050", result[5]);
            Assert.Equal("\uE051", result[6]);
            Assert.Equal("\uE052", result[7]);
            Assert.Equal("\uE020", result[8]);
            Assert.Equal("\uE021", result[9]);
            Assert.Equal("\uE022", result[10]);
            Assert.Equal("\uE023", result[11]);
            Assert.Equal("\uE024", result[12]);
        }

        // ──────────────────────────────────────────────
        //  エンジン経由 IPA 出力テスト
        // ──────────────────────────────────────────────

        [Fact]
        public void ToIpaPhonemes_Han_ReturnsCorrectIpa()
        {
            // 한 = ㅎㅏㄴ → IPA: h a n
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaPhonemes("한");

            Assert.Equal(3, result.Length);
            Assert.Equal("h", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("n", result[2]);
        }

        [Fact]
        public void ToIpa_Han_ReturnsSeparatedString()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpa("한");

            Assert.Equal("h a n", result);
        }

        [Fact]
        public void ToIpa_EmptyOrNull_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal("", engine.ToIpa(""));
            Assert.Equal("", engine.ToIpa(null!));
            Assert.Equal("", engine.ToIpa("   "));
        }

        [Fact]
        public void ToIpaPhonemes_EmptyOrNull_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Empty(engine.ToIpaPhonemes(""));
            Assert.Empty(engine.ToIpaPhonemes(null!));
            Assert.Empty(engine.ToIpaPhonemes("   "));
        }

        // ──────────────────────────────────────────────
        //  エンジン経由 PUA 出力テスト
        // ──────────────────────────────────────────────

        [Fact]
        public void ToPuaPhonemes_EmptyOrNull_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Empty(engine.ToPuaPhonemes(""));
            Assert.Empty(engine.ToPuaPhonemes(null!));
            Assert.Empty(engine.ToPuaPhonemes("   "));
        }

        [Fact]
        public void ToPuaString_EmptyOrNull_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal("", engine.ToPuaString(""));
            Assert.Equal("", engine.ToPuaString(null!));
            Assert.Equal("", engine.ToPuaString("   "));
        }

        [Fact]
        public void ToPuaPhonemes_Han_NoPuaMappingNeeded()
        {
            // 한 = ㅎㅏㄴ → IPA: h a n → PUA: h a n (単文字なので変化なし)
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("한");

            Assert.Equal(3, result.Length);
            Assert.Equal("h", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("n", result[2]);
        }

        [Fact]
        public void ToPuaPhonemes_Bba_ContainsFortisP()
        {
            // 빠 = ㅃㅏ → IPA: p͈ a → PUA: E04B a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("빠");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE04B", result[0]);  // p͈ → PUA
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Dda_ContainsFortisT()
        {
            // 따 = ㄸㅏ → IPA: t͈ a → PUA: E04C a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("따");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE04C", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Gga_ContainsFortisK()
        {
            // 까 = ㄲㅏ → IPA: k͈ a → PUA: E04D a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("까");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE04D", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Ssa_ContainsFortisS()
        {
            // 싸 = ㅆㅏ → IPA: s͈ a → PUA: E04E a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("싸");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE04E", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Jja_ContainsFortisAffricate()
        {
            // 짜 = ㅉㅏ → IPA: t͈ɕ a → PUA: E04F a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("짜");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE04F", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Pa_ContainsAspiratedP()
        {
            // 파 = ㅍㅏ → IPA: pʰ a → PUA: E020 a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("파");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE020", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Ta_ContainsAspiratedT()
        {
            // 타 = ㅌㅏ → IPA: tʰ a → PUA: E021 a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("타");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE021", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Ka_ContainsAspiratedK()
        {
            // 카 = ㅋㅏ → IPA: kʰ a → PUA: E022 a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("카");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE022", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Ja_ContainsAlveoloPalatalAffricate()
        {
            // 자 = ㅈㅏ → IPA: tɕ a → PUA: E023 a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("자");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE023", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Cha_ContainsAspiratedAffricate()
        {
            // 차 = ㅊㅏ → IPA: tɕʰ a → PUA: E024 a
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("차");

            Assert.Equal(2, result.Length);
            Assert.Equal("\uE024", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ToPuaPhonemes_Guk_ContainsUnreleasedK()
        {
            // 국 = ㄱㅜㄱ → G2P 後の終声 ㄱ → IPA: k u k̚ → PUA: k u E050
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("국");

            Assert.Equal(3, result.Length);
            Assert.Equal("k", result[0]);
            Assert.Equal("u", result[1]);
            Assert.Equal("\uE050", result[2]);
        }

        [Fact]
        public void ToPuaPhonemes_Bat_ContainsUnreleasedT()
        {
            // 밭 = ㅂㅏㅌ → G2P 後の終声代表化 ㅌ→ㄷ → IPA: p a t̚ → PUA: p a E051
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("밭");

            Assert.Equal(3, result.Length);
            Assert.Equal("p", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("\uE051", result[2]);
        }

        [Fact]
        public void ToPuaPhonemes_Bap_ContainsUnreleasedP()
        {
            // 밥 = ㅂㅏㅂ → G2P 後の終声 ㅂ → IPA: p a p̚ → PUA: p a E052
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("밥");

            Assert.Equal(3, result.Length);
            Assert.Equal("p", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("\uE052", result[2]);
        }

        // ──────────────────────────────────────────────
        //  完全な韓国語文の PUA 変換
        // ──────────────────────────────────────────────

        [Fact]
        public void ToPuaString_Hangugeo_ProducesNonEmptyResult()
        {
            // 한국어 → PUA 変換が空でないことを確認
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaString("한국어");

            Assert.False(string.IsNullOrEmpty(result));
            // 한국어 は「ㅎㅏㄴ ㄱㅜㄱ ㅇㅓ」→ 連音化等適用後 IPA → PUA 変換
        }

        [Fact]
        public void ToPuaString_Hangugeo_ContainsExpectedPuaCharacters()
        {
            // 한국어: 연음화で 한구거 [hanguɡʌ] 相当
            // ㅎㅏㄴ → h a n, ㄱㅜ → k u, ㄱㅓ → k ʌ (連音化後)
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaString("한국어");

            Assert.False(string.IsNullOrEmpty(result));
            // 少なくとも h と a と n が含まれる
            Assert.Contains("h", result);
            Assert.Contains("a", result);
            Assert.Contains("n", result);
        }

        [Fact]
        public void ToPuaPhonemes_MultiSyllableWithPuaOnset_MixesPlainAndPua()
        {
            // 짜장 = ㅉㅏ ㅈㅏㅇ → 짜장
            // ㅉ→t͈ɕ(PUA E04F), ㅏ→a, ㅈ→tɕ(PUA E023), ㅏ→a, ㅇ→ŋ
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaPhonemes("짜장");

            Assert.True(result.Length >= 4);
            Assert.Equal("\uE04F", result[0]);   // t͈ɕ → PUA
            Assert.Equal("a", result[1]);
        }

        // ──────────────────────────────────────────────
        //  バッチ API
        // ──────────────────────────────────────────────

        [Fact]
        public void ToPuaStringBatch_Null_ThrowsArgumentNullException()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Throws<ArgumentNullException>(() => engine.ToPuaStringBatch(null!));
        }

        [Fact]
        public void ToPuaStringBatch_EmptyInput_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Empty(engine.ToPuaStringBatch(Array.Empty<string>()));
        }

        [Fact]
        public void ToPuaStringBatch_MultipleTexts_ReturnsCorrectCount()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPuaStringBatch(new[] { "한글", "빠른", "" });

            Assert.Equal(3, result.Count);
            Assert.False(string.IsNullOrEmpty(result[0]));
            Assert.False(string.IsNullOrEmpty(result[1]));
            Assert.Equal("", result[2]);
        }

        [Fact]
        public void ToIpaBatch_Null_ThrowsArgumentNullException()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Throws<ArgumentNullException>(() => engine.ToIpaBatch(null!));
        }

        [Fact]
        public void ToIpaBatch_EmptyInput_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Empty(engine.ToIpaBatch(Array.Empty<string>()));
        }

        [Fact]
        public void ToIpaBatch_MultipleTexts_ReturnsCorrectCount()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToIpaBatch(new[] { "한", "글", "" });

            Assert.Equal(3, result.Count);
            Assert.Equal("h a n", result[0]);
            Assert.False(string.IsNullOrEmpty(result[1]));
            Assert.Equal("", result[2]);
        }

        // ──────────────────────────────────────────────
        //  Dispose 後の throw 確認
        // ──────────────────────────────────────────────

        [Fact]
        public void Dispose_ThenIpaApis_ThrowObjectDisposedException()
        {
            var engine = new KoreanG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIpa("한"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaPhonemes("한"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaBatch(new[] { "한" }));
        }

        [Fact]
        public void Dispose_ThenPuaApis_ThrowObjectDisposedException()
        {
            var engine = new KoreanG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaPhonemes("한"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaString("한"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaStringBatch(new[] { "한" }));
        }

        // ──────────────────────────────────────────────
        //  JamoToIpa 内部テスト（InternalsVisibleTo 経由）
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData('ㄱ', "k")]
        [InlineData('ㄴ', "n")]
        [InlineData('ㄷ', "t")]
        [InlineData('ㄹ', "\u027E")]      // ɾ
        [InlineData('ㅁ', "m")]
        [InlineData('ㅂ', "p")]
        [InlineData('ㅅ', "s")]
        [InlineData('ㅇ', "")]             // 初声ㅇは無音
        [InlineData('ㅎ', "h")]
        public void ConvertOnset_BasicConsonants_ReturnsCorrectIpa(char jamo, string expected)
        {
            Assert.Equal(expected, JamoToIpa.ConvertOnset(jamo));
        }

        [Theory]
        [InlineData('ㅏ', "a")]
        [InlineData('ㅓ', "\u028C")]       // ʌ
        [InlineData('ㅗ', "o")]
        [InlineData('ㅜ', "u")]
        [InlineData('ㅡ', "\u026F")]       // ɯ
        [InlineData('ㅣ', "i")]
        [InlineData('ㅐ', "\u025B")]       // ɛ
        [InlineData('ㅔ', "e")]
        public void ConvertNucleus_BasicVowels_ReturnsCorrectIpa(char jamo, string expected)
        {
            Assert.Equal(expected, JamoToIpa.ConvertNucleus(jamo));
        }

        [Theory]
        [InlineData('ㅑ', "ja")]
        [InlineData('ㅕ', "j\u028C")]      // jʌ
        [InlineData('ㅛ', "jo")]
        [InlineData('ㅠ', "ju")]
        [InlineData('ㅖ', "je")]
        [InlineData('ㅒ', "j\u025B")]      // jɛ
        public void ConvertNucleus_IotizedVowels_ReturnsCorrectIpa(char jamo, string expected)
        {
            Assert.Equal(expected, JamoToIpa.ConvertNucleus(jamo));
        }

        [Theory]
        [InlineData('ㅘ', "wa")]
        [InlineData('ㅝ', "w\u028C")]      // wʌ
        [InlineData('ㅙ', "w\u025B")]      // wɛ
        [InlineData('ㅚ', "we")]
        [InlineData('ㅞ', "we")]
        [InlineData('ㅟ', "wi")]
        [InlineData('ㅢ', "\u0270i")]      // ɰi
        public void ConvertNucleus_DiphthongVowels_ReturnsCorrectIpa(char jamo, string expected)
        {
            Assert.Equal(expected, JamoToIpa.ConvertNucleus(jamo));
        }

        [Theory]
        [InlineData('ㄱ', "k\u031A")]      // k̚
        [InlineData('ㄴ', "n")]
        [InlineData('ㄷ', "t\u031A")]      // t̚
        [InlineData('ㄹ', "l")]
        [InlineData('ㅁ', "m")]
        [InlineData('ㅂ', "p\u031A")]      // p̚
        [InlineData('ㅇ', "\u014B")]       // ŋ
        public void ConvertCoda_RepresentativeConsonants_ReturnsCorrectIpa(char jamo, string expected)
        {
            Assert.Equal(expected, JamoToIpa.ConvertCoda(jamo));
        }

        [Fact]
        public void ConvertSyllables_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(JamoToIpa.ConvertSyllables(null!));
            Assert.Empty(JamoToIpa.ConvertSyllables(Array.Empty<KoreanSyllable>()));
        }

        [Fact]
        public void ConvertSyllables_SingleSyllableWithCoda_ReturnsOnsetNucleusCoda()
        {
            // 한 = ㅎㅏㄴ
            var syllables = new[] { new KoreanSyllable('ㅎ', 'ㅏ', 'ㄴ') };

            var result = JamoToIpa.ConvertSyllables(syllables);

            Assert.Equal(3, result.Length);
            Assert.Equal("h", result[0]);
            Assert.Equal("a", result[1]);
            Assert.Equal("n", result[2]);
        }

        [Fact]
        public void ConvertSyllables_SyllableWithoutCoda_ReturnsOnsetNucleus()
        {
            // 가 = ㄱㅏ
            var syllables = new[] { new KoreanSyllable('ㄱ', 'ㅏ') };

            var result = JamoToIpa.ConvertSyllables(syllables);

            Assert.Equal(2, result.Length);
            Assert.Equal("k", result[0]);
            Assert.Equal("a", result[1]);
        }

        [Fact]
        public void ConvertSyllables_SilentIeung_OmitsEmptyOnset()
        {
            // 아 = ㅇㅏ → 初声 ㅇ は無音なので音素は母音のみ
            var syllables = new[] { new KoreanSyllable('ㅇ', 'ㅏ') };

            var result = JamoToIpa.ConvertSyllables(syllables);

            Assert.Single(result);
            Assert.Equal("a", result[0]);
        }

        [Fact]
        public void ConvertSyllables_BoundarySkipped()
        {
            var syllables = new[]
            {
                new KoreanSyllable('ㅎ', 'ㅏ', 'ㄴ'),
                KoreanSyllable.FromBoundary(' '),
                new KoreanSyllable('ㄱ', 'ㅡ', 'ㄹ'),
            };

            var result = JamoToIpa.ConvertSyllables(syllables);

            // 境界をスキップして 한 + 글 の 6 音素
            Assert.Equal(6, result.Length);
        }
    }
}
