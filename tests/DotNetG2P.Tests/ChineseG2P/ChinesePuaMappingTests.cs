using System;
using DotNetG2P.Chinese;
using DotNetG2P.Chinese.Conversion;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 中国語PUAマッピング（43エントリ）の正確性を検証するテスト。
    /// ChinesePuaMapper の MapToPua / ToneToPua / ApplyPuaMapping の直接テストと、
    /// ChineseG2PEngine 経由の ToPuaPhonemes / ToPuaString テストを含む。
    /// </summary>
    public class ChinesePuaMappingTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChinesePuaMappingTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 声母PUA個別検証（8エントリ）
        // =====================================================================

        [Fact]
        public void MapToPua_ph_0xE020()
        {
            // pʰ → 0xE020
            var result = ChinesePuaMapper.MapToPua("p\u02B0");
            Assert.Equal("\uE020", result);
        }

        [Fact]
        public void MapToPua_th_0xE021()
        {
            // tʰ → 0xE021
            var result = ChinesePuaMapper.MapToPua("t\u02B0");
            Assert.Equal("\uE021", result);
        }

        [Fact]
        public void MapToPua_kh_0xE022()
        {
            // kʰ → 0xE022
            var result = ChinesePuaMapper.MapToPua("k\u02B0");
            Assert.Equal("\uE022", result);
        }

        [Fact]
        public void MapToPua_tc_0xE023()
        {
            // tɕ → 0xE023
            var result = ChinesePuaMapper.MapToPua("t\u0255");
            Assert.Equal("\uE023", result);
        }

        [Fact]
        public void MapToPua_tch_0xE024()
        {
            // tɕʰ → 0xE024
            var result = ChinesePuaMapper.MapToPua("t\u0255\u02B0");
            Assert.Equal("\uE024", result);
        }

        [Fact]
        public void MapToPua_trs_0xE025()
        {
            // tʂ → 0xE025
            var result = ChinesePuaMapper.MapToPua("t\u0282");
            Assert.Equal("\uE025", result);
        }

        [Fact]
        public void MapToPua_trsh_0xE026()
        {
            // tʂʰ → 0xE026
            var result = ChinesePuaMapper.MapToPua("t\u0282\u02B0");
            Assert.Equal("\uE026", result);
        }

        [Fact]
        public void MapToPua_tsh_0xE027()
        {
            // tsʰ → 0xE027
            var result = ChinesePuaMapper.MapToPua("ts\u02B0");
            Assert.Equal("\uE027", result);
        }

        // =====================================================================
        // 2. 二重母音PUA（4エントリ）
        // =====================================================================

        [Fact]
        public void MapToPua_ai_0xE028()
        {
            // aɪ → 0xE028
            var result = ChinesePuaMapper.MapToPua("a\u026A");
            Assert.Equal("\uE028", result);
        }

        [Fact]
        public void MapToPua_ei_0xE029()
        {
            // eɪ → 0xE029
            var result = ChinesePuaMapper.MapToPua("e\u026A");
            Assert.Equal("\uE029", result);
        }

        [Fact]
        public void MapToPua_au_0xE02A()
        {
            // aʊ → 0xE02A
            var result = ChinesePuaMapper.MapToPua("a\u028A");
            Assert.Equal("\uE02A", result);
        }

        [Fact]
        public void MapToPua_ou_0xE02B()
        {
            // oʊ → 0xE02B
            var result = ChinesePuaMapper.MapToPua("o\u028A");
            Assert.Equal("\uE02B", result);
        }

        // =====================================================================
        // 3. 鼻音韻尾PUA（5エントリ）
        // =====================================================================

        [Fact]
        public void MapToPua_an_0xE02C()
        {
            var result = ChinesePuaMapper.MapToPua("an");
            Assert.Equal("\uE02C", result);
        }

        [Fact]
        public void MapToPua_en_0xE02D()
        {
            // ən → 0xE02D
            var result = ChinesePuaMapper.MapToPua("\u0259n");
            Assert.Equal("\uE02D", result);
        }

        [Fact]
        public void MapToPua_ang_0xE02E()
        {
            // aŋ → 0xE02E
            var result = ChinesePuaMapper.MapToPua("a\u014B");
            Assert.Equal("\uE02E", result);
        }

        [Fact]
        public void MapToPua_eng_0xE02F()
        {
            // əŋ → 0xE02F
            var result = ChinesePuaMapper.MapToPua("\u0259\u014B");
            Assert.Equal("\uE02F", result);
        }

        [Fact]
        public void MapToPua_ung_0xE030()
        {
            // uŋ → 0xE030
            var result = ChinesePuaMapper.MapToPua("u\u014B");
            Assert.Equal("\uE030", result);
        }

        // =====================================================================
        // 4. i系・u系・ü系複合韻母PUA（18エントリ）0xE031-0xE044
        // =====================================================================

        [Fact]
        public void MapToPua_ia_0xE031()
        {
            var result = ChinesePuaMapper.MapToPua("ia");
            Assert.Equal("\uE031", result);
        }

        [Fact]
        public void MapToPua_ie_0xE032()
        {
            // iɛ → 0xE032
            var result = ChinesePuaMapper.MapToPua("i\u025B");
            Assert.Equal("\uE032", result);
        }

        [Fact]
        public void MapToPua_iou_0xE033()
        {
            var result = ChinesePuaMapper.MapToPua("iou");
            Assert.Equal("\uE033", result);
        }

        [Fact]
        public void MapToPua_iau_0xE034()
        {
            // iaʊ → 0xE034
            var result = ChinesePuaMapper.MapToPua("ia\u028A");
            Assert.Equal("\uE034", result);
        }

        [Fact]
        public void MapToPua_ien_0xE035()
        {
            // iɛn → 0xE035
            var result = ChinesePuaMapper.MapToPua("i\u025Bn");
            Assert.Equal("\uE035", result);
        }

        [Fact]
        public void MapToPua_in_0xE036()
        {
            var result = ChinesePuaMapper.MapToPua("in");
            Assert.Equal("\uE036", result);
        }

        [Fact]
        public void MapToPua_iang_0xE037()
        {
            // iaŋ → 0xE037
            var result = ChinesePuaMapper.MapToPua("ia\u014B");
            Assert.Equal("\uE037", result);
        }

        [Fact]
        public void MapToPua_ing_0xE038()
        {
            // iŋ → 0xE038
            var result = ChinesePuaMapper.MapToPua("i\u014B");
            Assert.Equal("\uE038", result);
        }

        [Fact]
        public void MapToPua_iung_0xE039()
        {
            // iuŋ → 0xE039
            var result = ChinesePuaMapper.MapToPua("iu\u014B");
            Assert.Equal("\uE039", result);
        }

        [Fact]
        public void MapToPua_ua_0xE03A()
        {
            var result = ChinesePuaMapper.MapToPua("ua");
            Assert.Equal("\uE03A", result);
        }

        [Fact]
        public void MapToPua_uo_0xE03B()
        {
            var result = ChinesePuaMapper.MapToPua("uo");
            Assert.Equal("\uE03B", result);
        }

        [Fact]
        public void MapToPua_uai_0xE03C()
        {
            // uaɪ → 0xE03C
            var result = ChinesePuaMapper.MapToPua("ua\u026A");
            Assert.Equal("\uE03C", result);
        }

        [Fact]
        public void MapToPua_uei_0xE03D()
        {
            // ueɪ → 0xE03D
            var result = ChinesePuaMapper.MapToPua("ue\u026A");
            Assert.Equal("\uE03D", result);
        }

        [Fact]
        public void MapToPua_uan_0xE03E()
        {
            var result = ChinesePuaMapper.MapToPua("uan");
            Assert.Equal("\uE03E", result);
        }

        [Fact]
        public void MapToPua_uen_0xE03F()
        {
            // uən → 0xE03F
            var result = ChinesePuaMapper.MapToPua("u\u0259n");
            Assert.Equal("\uE03F", result);
        }

        [Fact]
        public void MapToPua_uang_0xE040()
        {
            // uaŋ → 0xE040
            var result = ChinesePuaMapper.MapToPua("ua\u014B");
            Assert.Equal("\uE040", result);
        }

        [Fact]
        public void MapToPua_ueng_0xE041()
        {
            // uəŋ → 0xE041
            var result = ChinesePuaMapper.MapToPua("u\u0259\u014B");
            Assert.Equal("\uE041", result);
        }

        [Fact]
        public void MapToPua_ye_0xE042()
        {
            // yɛ → 0xE042
            var result = ChinesePuaMapper.MapToPua("y\u025B");
            Assert.Equal("\uE042", result);
        }

        [Fact]
        public void MapToPua_yen_0xE043()
        {
            // yɛn → 0xE043
            var result = ChinesePuaMapper.MapToPua("y\u025Bn");
            Assert.Equal("\uE043", result);
        }

        [Fact]
        public void MapToPua_yn_0xE044()
        {
            var result = ChinesePuaMapper.MapToPua("yn");
            Assert.Equal("\uE044", result);
        }

        // =====================================================================
        // 5. 音節子音PUA
        // =====================================================================

        [Fact]
        public void MapToPua_retroflexApical_0xE045()
        {
            // ɻ̩ (\u027B\u0329) → 0xE045
            var result = ChinesePuaMapper.MapToPua("\u027B\u0329");
            Assert.Equal("\uE045", result);
        }

        // =====================================================================
        // 6. 声調PUA（5エントリ）
        // =====================================================================

        [Fact]
        public void ToneToPua_tone1_0xE046()
        {
            var result = ChinesePuaMapper.ToneToPua(1);
            Assert.Equal("\uE046", result);
        }

        [Fact]
        public void ToneToPua_tone2_0xE047()
        {
            var result = ChinesePuaMapper.ToneToPua(2);
            Assert.Equal("\uE047", result);
        }

        [Fact]
        public void ToneToPua_tone3_0xE048()
        {
            var result = ChinesePuaMapper.ToneToPua(3);
            Assert.Equal("\uE048", result);
        }

        [Fact]
        public void ToneToPua_tone4_0xE049()
        {
            var result = ChinesePuaMapper.ToneToPua(4);
            Assert.Equal("\uE049", result);
        }

        [Fact]
        public void ToneToPua_tone5_0xE04A()
        {
            var result = ChinesePuaMapper.ToneToPua(5);
            Assert.Equal("\uE04A", result);
        }

        [Fact]
        public void ToneToPua_範囲外_空文字列()
        {
            Assert.Equal(string.Empty, ChinesePuaMapper.ToneToPua(0));
            Assert.Equal(string.Empty, ChinesePuaMapper.ToneToPua(6));
            Assert.Equal(string.Empty, ChinesePuaMapper.ToneToPua(-1));
        }

        // =====================================================================
        // 7. PUA対象外の音素はそのまま出力
        // =====================================================================

        [Theory]
        [InlineData("a")]
        [InlineData("i")]
        [InlineData("u")]
        [InlineData("m")]
        [InlineData("n")]
        [InlineData("l")]
        [InlineData("s")]
        [InlineData("x")]
        [InlineData("\u0255")]   // ɕ
        [InlineData("\u0282")]   // ʂ
        [InlineData("\u027B")]   // ɻ
        [InlineData("\u0264")]   // ɤ
        [InlineData("\u025A")]   // ɚ
        [InlineData("\u0268")]   // ɨ
        [InlineData("y")]       // y_vowel
        public void MapToPua_PUA対象外_そのまま返す(string phoneme)
        {
            var result = ChinesePuaMapper.MapToPua(phoneme);
            Assert.Equal(phoneme, result);
        }

        [Fact]
        public void MapToPua_null_そのまま返す()
        {
            var result = ChinesePuaMapper.MapToPua(null!);
            Assert.Null(result);
        }

        [Fact]
        public void MapToPua_空文字列_そのまま返す()
        {
            var result = ChinesePuaMapper.MapToPua("");
            Assert.Equal("", result);
        }

        // =====================================================================
        // ApplyPuaMapping 配列テスト
        // =====================================================================

        [Fact]
        public void ApplyPuaMapping_混合配列_PUA対象のみ変換()
        {
            // 声母(PUA対象) + 韻母(PUA対象) + 単純音素(対象外)
            var input = new[] { "p\u02B0", "a\u026A", "a", "m" };
            var result = ChinesePuaMapper.ApplyPuaMapping(input);

            Assert.Equal(4, result.Length);
            Assert.Equal("\uE020", result[0]); // pʰ → PUA
            Assert.Equal("\uE028", result[1]); // aɪ → PUA
            Assert.Equal("a", result[2]);       // そのまま
            Assert.Equal("m", result[3]);       // そのまま
        }

        [Fact]
        public void ApplyPuaMapping_null_空配列を返す()
        {
            var result = ChinesePuaMapper.ApplyPuaMapping(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ApplyPuaMapping_空配列_空配列を返す()
        {
            var result = ChinesePuaMapper.ApplyPuaMapping(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        public void ApplyPuaMapping_全43エントリ_正しいPUA範囲()
        {
            // 全PUAマッピング対象38エントリ（声調は別）をテスト
            var allMappedInputs = new[]
            {
                "p\u02B0",          // 0xE020
                "t\u02B0",          // 0xE021
                "k\u02B0",          // 0xE022
                "t\u0255",          // 0xE023
                "t\u0255\u02B0",    // 0xE024
                "t\u0282",          // 0xE025
                "t\u0282\u02B0",    // 0xE026
                "ts\u02B0",         // 0xE027
                "a\u026A",          // 0xE028
                "e\u026A",          // 0xE029
                "a\u028A",          // 0xE02A
                "o\u028A",          // 0xE02B
                "an",               // 0xE02C
                "\u0259n",          // 0xE02D
                "a\u014B",          // 0xE02E
                "\u0259\u014B",     // 0xE02F
                "u\u014B",          // 0xE030
                "ia",               // 0xE031
                "i\u025B",          // 0xE032
                "iou",              // 0xE033
                "ia\u028A",         // 0xE034
                "i\u025Bn",         // 0xE035
                "in",               // 0xE036
                "ia\u014B",         // 0xE037
                "i\u014B",          // 0xE038
                "iu\u014B",         // 0xE039
                "ua",               // 0xE03A
                "uo",               // 0xE03B
                "ua\u026A",         // 0xE03C
                "ue\u026A",         // 0xE03D
                "uan",              // 0xE03E
                "u\u0259n",         // 0xE03F
                "ua\u014B",         // 0xE040
                "u\u0259\u014B",    // 0xE041
                "y\u025B",          // 0xE042
                "y\u025Bn",         // 0xE043
                "yn",               // 0xE044
                "\u027B\u0329",     // 0xE045
            };

            var result = ChinesePuaMapper.ApplyPuaMapping(allMappedInputs);
            Assert.Equal(38, result.Length);

            // 全結果がPUA範囲 0xE020-0xE045 の単一文字であることを検証
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Single(result[i]); // 単一文字
                var c = result[i][0];
                int codePoint = (int)c;
                Assert.True(
                    codePoint >= 0xE020 && codePoint <= 0xE045,
                    $"Index {i}: expected PUA range 0xE020-0xE045, got 0x{codePoint:X4}");

                // 連番であることを検証
                Assert.Equal(0xE020 + i, codePoint);
            }
        }

        // =====================================================================
        // 8. エンジン経由テスト
        // =====================================================================

        [Fact]
        public void ToPuaPhonemes_你好_非空配列を返す()
        {
            var result = _engine.ToPuaPhonemes("\u4F60\u597D"); // 你好
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPuaString_你好_非空文字列を返す()
        {
            var result = _engine.ToPuaString("\u4F60\u597D"); // 你好
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPuaPhonemes_你好_PUA音素を含む()
        {
            var result = _engine.ToPuaPhonemes("\u4F60\u597D"); // 你好

            // ToPuaPhonemes は各音節ごとに IPA音素→PUAマッピング + 声調PUA追加。
            // 音素PUA範囲(0xE020-0xE045)と声調PUA範囲(0xE046-0xE04A)の両方を含む。
            // ここでは音素PUA(0xE020-0xE045)が含まれることを検証
            Assert.NotEmpty(result);
            bool hasPua = false;
            foreach (var p in result)
            {
                if (p.Length == 1)
                {
                    int c = (int)p[0];
                    if (c >= 0xE020 && c <= 0xE045)
                    {
                        hasPua = true;
                        break;
                    }
                }
            }
            Assert.True(hasPua, "PUA音素配列にPUA変換済み音素が含まれるべき");
        }

        [Fact]
        public void ToPuaPhonemes_null_空配列を返す()
        {
            var result = _engine.ToPuaPhonemes(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ToPuaPhonemes_空文字列_空配列を返す()
        {
            var result = _engine.ToPuaPhonemes("");
            Assert.Empty(result);
        }

        [Fact]
        public void ToPuaPhonemes_空白のみ_空配列を返す()
        {
            var result = _engine.ToPuaPhonemes("   ");
            Assert.Empty(result);
        }

        [Fact]
        public void ToPuaString_null_空文字列を返す()
        {
            var result = _engine.ToPuaString(null!);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ToPuaString_空文字列_空文字列を返す()
        {
            var result = _engine.ToPuaString("");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ToPuaStringBatch_複数テキスト()
        {
            var texts = new[] { "\u4F60\u597D", "\u4E16\u754C" }; // 你好, 世界
            var results = _engine.ToPuaStringBatch(texts);

            Assert.Equal(2, results.Count);
            Assert.NotEmpty(results[0]);
            Assert.NotEmpty(results[1]);
        }

        [Fact]
        public void ToPuaStringBatch_空配列_空リストを返す()
        {
            var results = _engine.ToPuaStringBatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void ToPuaPhonemes_Dispose後_ObjectDisposedExceptionをスロー()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaPhonemes("\u4F60\u597D"));
        }

        [Fact]
        public void ToPuaString_Dispose後_ObjectDisposedExceptionをスロー()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaString("\u4F60\u597D"));
        }

        [Fact]
        public void ToPuaStringBatch_Dispose後_ObjectDisposedExceptionをスロー()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaStringBatch(new[] { "\u4F60\u597D" }));
        }
    }
}
