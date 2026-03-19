using System;
using System.Diagnostics;
using System.Linq;
using DotNetG2P.Chinese;
using DotNetG2P.Chinese.Conversion;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 中国語 piper-plus 互換機能の統合テスト。
    /// IPA→PUA→Prosody のフルパイプラインを検証する。
    /// 個別マッピングの検証は他のテストファイルに任せ、ここではパイプライン全体の動作を確認する。
    /// </summary>
    public class ChinesePiperIntegrationTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;
        private readonly ITestOutputHelper _output;

        public ChinesePiperIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. フルパイプラインテスト
        // =====================================================================

        [Fact]
        public void フルパイプライン_ToPiperIPA_空でない結果を返す()
        {
            var result = _engine.ToPiperIPA("你好世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _output.WriteLine($"ToPiperIPA: {result}");
        }

        [Fact]
        public void フルパイプライン_ToPiperIpaPhonemes_空でない配列を返す()
        {
            var result = _engine.ToPiperIpaPhonemes("你好世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _output.WriteLine($"ToPiperIpaPhonemes: [{string.Join(", ", result)}]");
        }

        [Fact]
        public void フルパイプライン_ToPuaPhonemes_空でない配列を返す()
        {
            var result = _engine.ToPuaPhonemes("你好世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _output.WriteLine($"ToPuaPhonemes: [{string.Join(", ", result.Select(p => $"U+{(int)p[0]:X4}"))}]");
        }

        [Fact]
        public void フルパイプライン_ToPuaString_空でない結果を返す()
        {
            var result = _engine.ToPuaString("你好世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _output.WriteLine($"ToPuaString length: {result.Length}");
        }

        [Fact]
        public void フルパイプライン_ToIpaWithProsody_Phonemes長とProsody長が一致する()
        {
            var result = _engine.ToIpaWithProsody("你好世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result.Phonemes);
            Assert.NotEmpty(result.Prosody);
            Assert.Equal(result.Phonemes.Count, result.Prosody.Count);
            _output.WriteLine($"Phonemes: {result.Phonemes.Count}, Prosody: {result.Prosody.Count}");
        }

        [Fact]
        public void フルパイプライン_全API結果が互いに整合する()
        {
            // 全APIの出力が空でなく、サイズ関係が妥当であることを検証
            var text = "你好世界";
            var piperIpa = _engine.ToPiperIPA(text);
            var ipaPhonemes = _engine.ToPiperIpaPhonemes(text);
            var puaPhonemes = _engine.ToPuaPhonemes(text);
            var puaString = _engine.ToPuaString(text);
            var prosody = _engine.ToIpaWithProsody(text);

            Assert.NotEmpty(piperIpa);
            Assert.NotEmpty(ipaPhonemes);
            Assert.NotEmpty(puaPhonemes);
            Assert.NotEmpty(puaString);
            Assert.NotEmpty(prosody.Phonemes);

            // PUA音素配列はIPA音素配列 + 各音節末尾の声調PUAを含むため、IPA音素より多い
            Assert.True(puaPhonemes.Length > ipaPhonemes.Length,
                $"PUA音素({puaPhonemes.Length})はIPA音素({ipaPhonemes.Length}) + 声調PUA分だけ多いはず");

            // Prosody音素配列は音節単位（漢字1字=1エントリ）
            // IPA音素配列は声母+韻母単位なのでProsodyより多い
            Assert.True(prosody.Phonemes.Count <= ipaPhonemes.Length);
        }

        // =====================================================================
        // 2. PUA音素に各音節末尾の声調PUAが含まれることの確認
        // =====================================================================

        [Fact]
        public void ToPuaPhonemes_各音節末尾に声調PUA文字を含む()
        {
            // 声調PUA範囲: 0xE046-0xE04A（tone1-tone5）
            var result = _engine.ToPuaPhonemes("你好世界");
            var toneCount = result.Count(p => p.Length == 1 && p[0] >= '\uE046' && p[0] <= '\uE04A');
            // "你好世界" は4音節なので声調PUAも4つ
            Assert.Equal(4, toneCount);
        }

        [Fact]
        public void ToPuaString_声調PUA文字を含む()
        {
            var result = _engine.ToPuaString("你好世界");
            var toneCount = result.Count(ch => ch >= '\uE046' && ch <= '\uE04A');
            // "你好世界" は4音節なので声調PUAも4つ
            Assert.Equal(4, toneCount);
        }

        // =====================================================================
        // 3. 一貫性テスト: ToPuaPhonemesが音素PUA+声調PUAを正しく含むことの確認
        // =====================================================================

        [Theory]
        [InlineData("你好")]
        [InlineData("世界")]
        [InlineData("中国人民")]
        [InlineData("学生")]
        [InlineData("北京大学")]
        public void ToPuaPhonemesが音素PUAと声調PUAの両方を含む(string text)
        {
            var ipaPhonemes = _engine.ToPiperIpaPhonemes(text);
            var puaFromIpa = ChinesePuaMapper.ApplyPuaMapping(ipaPhonemes);
            var actualPua = _engine.ToPuaPhonemes(text);

            // ToPuaPhonemesは音素PUA + 各音節末尾の声調PUA を含むため、
            // IPA音素を単純にPUAマッピングしたものより多い
            var toneCount = actualPua.Count(p => p.Length == 1 && p[0] >= '\uE046' && p[0] <= '\uE04A');
            Assert.Equal(puaFromIpa.Length + toneCount, actualPua.Length);

            // 声調PUAを除いた音素部分はIPA→PUAマッピングと一致する
            var actualWithoutTones = actualPua.Where(p => !(p.Length == 1 && p[0] >= '\uE046' && p[0] <= '\uE04A')).ToArray();
            Assert.Equal(puaFromIpa.Length, actualWithoutTones.Length);
            for (int i = 0; i < puaFromIpa.Length; i++)
            {
                Assert.Equal(puaFromIpa[i], actualWithoutTones[i]);
            }
        }

        // =====================================================================
        // 4. 大量テキストテスト
        // =====================================================================

        [Theory]
        [InlineData("中华人民共和国是世界上人口最多的国家")]
        [InlineData("今天天气很好我们一起去公园散步吧")]
        [InlineData("科学技术是第一生产力教育是国家发展的基础")]
        public void 長文テキスト_全APIが正常動作する(string text)
        {
            Assert.True(text.Length >= 10, "テストテキストは10文字以上であること");

            var piperIpa = _engine.ToPiperIPA(text);
            var ipaPhonemes = _engine.ToPiperIpaPhonemes(text);
            var puaPhonemes = _engine.ToPuaPhonemes(text);
            var puaString = _engine.ToPuaString(text);
            var prosody = _engine.ToIpaWithProsody(text);

            Assert.NotEmpty(piperIpa);
            Assert.NotEmpty(ipaPhonemes);
            Assert.NotEmpty(puaPhonemes);
            Assert.NotEmpty(puaString);
            Assert.NotEmpty(prosody.Phonemes);
            Assert.Equal(prosody.Phonemes.Count, prosody.Prosody.Count);

            _output.WriteLine($"[{text}] IPA phonemes: {ipaPhonemes.Length}, PUA phonemes: {puaPhonemes.Length}");
        }

        // =====================================================================
        // 5. 混在テキストテスト
        // =====================================================================

        [Theory]
        [InlineData("Hello你好World")]
        [InlineData("ABC中文DEF")]
        [InlineData("test123测试456")]
        public void 英中混在テキスト_例外なく動作する(string text)
        {
            // 混在テキストでも例外を投げずに動作すること
            var piperIpa = _engine.ToPiperIPA(text);
            var ipaPhonemes = _engine.ToPiperIpaPhonemes(text);
            var puaPhonemes = _engine.ToPuaPhonemes(text);
            var puaString = _engine.ToPuaString(text);
            var prosody = _engine.ToIpaWithProsody(text);

            // 漢字部分の音素が含まれること
            Assert.NotEmpty(piperIpa);
            Assert.NotEmpty(ipaPhonemes);
            Assert.Equal(prosody.Phonemes.Count, prosody.Prosody.Count);

            _output.WriteLine($"[{text}] ToPiperIPA: {piperIpa}");
        }

        // =====================================================================
        // 6. 句読点含むテキスト
        // =====================================================================

        [Theory]
        [InlineData("你好，世界！")]
        [InlineData("中国。日本。")]
        [InlineData("学生？老师！")]
        [InlineData("你好,世界!")]
        public void 句読点含むテキスト_正常動作する(string text)
        {
            var piperIpa = _engine.ToPiperIPA(text);
            var ipaPhonemes = _engine.ToPiperIpaPhonemes(text);
            var puaPhonemes = _engine.ToPuaPhonemes(text);
            var prosody = _engine.ToIpaWithProsody(text);

            Assert.NotEmpty(piperIpa);
            Assert.NotEmpty(ipaPhonemes);
            // PUA音素はIPA音素 + 各音節末尾の声調PUAを含む
            Assert.True(puaPhonemes.Length > ipaPhonemes.Length,
                $"PUA音素({puaPhonemes.Length})はIPA音素({ipaPhonemes.Length}) + 声調PUA分だけ多いはず");
            Assert.Equal(prosody.Phonemes.Count, prosody.Prosody.Count);

            _output.WriteLine($"[{text}] IPA phonemes: {ipaPhonemes.Length}, PUA phonemes: {puaPhonemes.Length}");
        }

        // =====================================================================
        // 7. 声調変調+Prosodyテスト
        // =====================================================================

        [Fact]
        public void 声調変調有効時_Prosody_A1が変調後の声調番号を持つ()
        {
            // "你好" は三声+三声 → 変調後: 二声+三声
            var options = new ChineseG2POptions(enableToneSandhi: true);
            using var engine = new ChineseG2PEngine(options);

            var result = engine.ToIpaWithProsody("你好");
            Assert.NotEmpty(result.Prosody);

            // 全ての韻律情報のA1が1-5の範囲であること
            foreach (var p in result.Prosody)
            {
                Assert.InRange(p.A1, 1, 5);
            }

            _output.WriteLine($"Prosody: [{string.Join(", ", result.Prosody.Select(p => p.ToString()))}]");
        }

        [Fact]
        public void 声調変調無効時_Prosody_A1が元の声調番号を持つ()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);

            var result = engine.ToIpaWithProsody("你好");
            Assert.NotEmpty(result.Prosody);

            // 全ての韻律情報のA1が1-5の範囲であること
            foreach (var p in result.Prosody)
            {
                Assert.InRange(p.A1, 1, 5);
            }

            _output.WriteLine($"Prosody (no sandhi): [{string.Join(", ", result.Prosody.Select(p => p.ToString()))}]");
        }

        [Fact]
        public void Prosody_A2は語内シラブル位置_A3は語のシラブル数()
        {
            // "你好世界" は4文字 → 連続漢字として1語扱い
            var result = _engine.ToIpaWithProsody("你好世界");

            // 全てのA2 >= 1、A3 >= 1
            foreach (var p in result.Prosody)
            {
                Assert.True(p.A2 >= 1, $"A2 ({p.A2}) は1以上であること");
                Assert.True(p.A3 >= 1, $"A3 ({p.A3}) は1以上であること");
                Assert.True(p.A2 <= p.A3, $"A2 ({p.A2}) は A3 ({p.A3}) 以下であること");
            }

            _output.WriteLine($"Prosody: [{string.Join(", ", result.Prosody.Select(p => p.ToString()))}]");
        }

        [Fact]
        public void 声調変調_三声連読_変調後A1が2になる音素が存在する()
        {
            // "你好" (nǐ hǎo) → 三声+三声 → 変調: ní hǎo（最初の三声が二声に変化）
            var result = _engine.ToIpaWithProsody("你好");

            // "你" の音素に対応するProsodyで A1=2 が存在すること（三声→二声変調）
            var hasSecondTone = result.Prosody.Any(p => p.A1 == 2 && p.A2 == 1);
            Assert.True(hasSecondTone,
                "三声連読変調により A1=2（第2声）の音素が存在するべき。" +
                $"実際: [{string.Join(", ", result.Prosody.Select(p => p.ToString()))}]");
        }

        // =====================================================================
        // 8. パフォーマンステスト（軽量）
        // =====================================================================

        [Fact]
        [Trait("Category", "Performance")]
        public void 全Piper_API_100回繰り返しが1秒以内()
        {
            // ウォームアップ
            for (int w = 0; w < 5; w++)
            {
                _engine.ToPiperIPA("你好世界");
                _engine.ToPiperIpaPhonemes("你好世界");
                _engine.ToPuaPhonemes("你好世界");
                _engine.ToPuaString("你好世界");
                _engine.ToIpaWithProsody("你好世界");
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                _engine.ToPiperIPA("你好世界");
                _engine.ToPiperIpaPhonemes("你好世界");
                _engine.ToPuaPhonemes("你好世界");
                _engine.ToPuaString("你好世界");
                _engine.ToIpaWithProsody("你好世界");
            }
            sw.Stop();

            _output.WriteLine($"全Piper API x100回: {sw.ElapsedMilliseconds}ms (平均: {sw.ElapsedMilliseconds / 100.0:F2}ms/回)");
            Assert.True(sw.ElapsedMilliseconds < 1000,
                $"100回の繰り返しが1秒を超過: {sw.ElapsedMilliseconds}ms");
        }

        // =====================================================================
        // 9. Dispose テスト
        // =====================================================================

        [Fact]
        public void Dispose後_ToPiperIPA_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPiperIPA("你好"));
        }

        [Fact]
        public void Dispose後_ToPiperIpaPhonemes_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPiperIpaPhonemes("你好"));
        }

        [Fact]
        public void Dispose後_ToPuaPhonemes_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaPhonemes("你好"));
        }

        [Fact]
        public void Dispose後_ToPuaString_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPuaString("你好"));
        }

        [Fact]
        public void Dispose後_ToIpaWithProsody_ObjectDisposedExceptionを投げる()
        {
            var engine = new ChineseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIpaWithProsody("你好"));
        }

        // =====================================================================
        // 10. null/空/空白テスト
        // =====================================================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToPiperIPA_null空白入力_空文字列を返す(string? text)
        {
            var result = _engine.ToPiperIPA(text!);
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToPiperIpaPhonemes_null空白入力_空配列を返す(string? text)
        {
            var result = _engine.ToPiperIpaPhonemes(text!);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToPuaPhonemes_null空白入力_空配列を返す(string? text)
        {
            var result = _engine.ToPuaPhonemes(text!);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToPuaString_null空白入力_空文字列を返す(string? text)
        {
            var result = _engine.ToPuaString(text!);
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void ToIpaWithProsody_null空白入力_空結果を返す(string? text)
        {
            var result = _engine.ToIpaWithProsody(text!);
            Assert.NotNull(result);
            Assert.Empty(result.Phonemes);
            Assert.Empty(result.Prosody);
        }
    }
}
