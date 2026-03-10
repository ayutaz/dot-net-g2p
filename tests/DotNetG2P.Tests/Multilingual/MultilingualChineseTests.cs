using System;
using System.IO;
using System.Linq;
using DotNetG2P.Chinese;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual中国語統合テスト。
    /// Language enum、ScriptKind/LanguageDetector、TextSegmenter、
    /// MultilingualG2PEngine、MultilingualG2POptionsの中国語対応を検証する。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualChineseTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualChineseTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        // =================================================================
        // 1. Language enum テスト (3件)
        // =================================================================

        [Fact]
        public void Language_Chinese_値は2()
        {
            Assert.Equal((byte)2, (byte)Language.Chinese);
        }

        [Fact]
        public void Language_全値の一意性()
        {
            var values = Enum.GetValues(typeof(Language)).Cast<Language>().ToArray();
            var distinctCount = values.Select(v => (byte)v).Distinct().Count();
            Assert.Equal(values.Length, distinctCount);
        }

        [Fact]
        public void Language_Chinese_ToString()
        {
            Assert.Equal("Chinese", Language.Chinese.ToString());
        }

        // =================================================================
        // 2. ScriptKind/LanguageDetector テスト (8件)
        // =================================================================

        // ScriptKind: Japanese=0, CJKIdeograph=1, English=2, Latin=3,
        //             Digit=4, Punctuation=5, Whitespace=6, Other=7

        [Fact]
        public void Classify_CJK統合漢字_你_CJKIdeographを返す()
        {
            Assert.Equal(ScriptKind.CJKIdeograph, LanguageDetector.Classify('\u4F60')); // 你
        }

        [Fact]
        public void Classify_ひらがな_あ_Japaneseを返す()
        {
            Assert.Equal(ScriptKind.Japanese, LanguageDetector.Classify('あ'));
        }

        [Fact]
        public void Classify_カタカナ_ア_Japaneseを返す()
        {
            Assert.Equal(ScriptKind.Japanese, LanguageDetector.Classify('ア'));
        }

        [Fact]
        public void Classify_ASCII英字_A_Englishを返す()
        {
            Assert.Equal(ScriptKind.English, LanguageDetector.Classify('A'));
        }

        [Fact]
        public void Classify_CJK拡張A文字_CJKIdeographを返す()
        {
            Assert.Equal(ScriptKind.CJKIdeograph, LanguageDetector.Classify('\u3400'));
        }

        [Fact]
        public void ToLanguage_CJKIdeograph_nullを返す()
        {
            Assert.Null(LanguageDetector.ToLanguage(ScriptKind.CJKIdeograph));
        }

        [Fact]
        public void ToLanguage_Japanese_LanguageJapaneseを返す()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.Japanese);
            Assert.NotNull(result);
            Assert.Equal(Language.Japanese, result!.Value);
        }

        [Fact]
        public void ToLanguage_English_LanguageEnglishを返す()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.English);
            Assert.NotNull(result);
            Assert.Equal(Language.English, result!.Value);
        }

        // =================================================================
        // 3. TextSegmenter テスト (10件)
        // =================================================================

        [Fact]
        public void Segment_漢字のみ_DefaultJapanese_中国語語彙はChinese()
        {
            var result = TextSegmenter.Segment("你好世界", Language.Japanese);
            Assert.Single(result);
            Assert.Equal(Language.Chinese, result[0].Language);
            Assert.Equal("你好世界", result[0].Text);
        }

        [Fact]
        public void Segment_漢字のみ_DefaultChinese_全Chinese()
        {
            var result = TextSegmenter.Segment("你好世界", Language.Chinese);
            Assert.Single(result);
            Assert.Equal(Language.Chinese, result[0].Language);
            Assert.Equal("你好世界", result[0].Text);
        }

        [Fact]
        public void Segment_漢字とひらがな_Japanese()
        {
            // ひらがながあるので漢字もJapaneseに吸収
            var result = TextSegmenter.Segment("漢字とひらがな", Language.Chinese);
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
        }

        [Fact]
        public void Segment_漢字と英語_DefaultJapanese_JapaneseとEnglish()
        {
            var result = TextSegmenter.Segment("你好Hello", Language.Japanese);
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Contains("你好", result[0].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Contains("Hello", result[1].Text);
        }

        [Fact]
        public void Segment_漢字と英語_DefaultChinese_ChineseとEnglish()
        {
            var result = TextSegmenter.Segment("你好Hello", Language.Chinese);
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Chinese, result[0].Language);
            Assert.Contains("你好", result[0].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Contains("Hello", result[1].Text);
        }

        [Fact]
        public void Segment_ひらがなと漢字と英字_DefaultChinese_ひらがな近接漢字はJapanese()
        {
            // "こんにちは你好hello" → ひらがなの隣の漢字はJapanese
            var result = TextSegmenter.Segment("こんにちは你好hello", Language.Chinese);
            // こんにちは你好 = Japanese (ひらがなが近接するため漢字もJapanese), hello = English
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.English, result[1].Language);
        }

        [Fact]
        public void Segment_空文字列_空リスト()
        {
            var result = TextSegmenter.Segment("", Language.Chinese);
            Assert.Empty(result);
        }

        [Fact]
        public void Segment_数字と漢字_DefaultChinese_Chinese()
        {
            var result = TextSegmenter.Segment("123你好", Language.Chinese);
            Assert.Single(result);
            Assert.Equal(Language.Chinese, result[0].Language);
        }

        [Fact]
        public void Segment_漢字のみテキスト_中国語語彙はデフォルトオーバーロードでもChinese()
        {
            // Segment(string) でも中国語語彙の証拠が強い run は Chinese に寄せる
            var result = TextSegmenter.Segment("东京大学");
            Assert.Single(result);
            Assert.Equal(Language.Chinese, result[0].Language);
        }

        [Fact]
        public void Segment_日本語純漢字_DefaultChineseでも日本語語彙ヒントでJapanese()
        {
            var result = TextSegmenter.Segment("東京大学", Language.Chinese);
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
        }

        [Fact]
        public void Segment_日中英混在_DefaultChinese_正しくセグメント分割()
        {
            // "テスト中文hello" → テスト=Japanese(kana), 中文=Japanese(kana隣接), hello=English
            // ただし テスト(カタカナ)と中文(漢字)が隣接しているのでJapaneseに吸収される
            var result = TextSegmenter.Segment("テスト中文hello", Language.Chinese);
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.English, result[1].Language);
        }

        // =================================================================
        // 4. MultilingualG2PEngine テスト (辞書依存、10件)
        // =================================================================

        [SkippableFact]
        public void Engine_DefaultChinese_漢字テキスト_ピンイン出力()
        {
            SkipIfNoDictionary();
            var result = _fixture.ChineseDefaultEngine!.ToPhonemes("你好");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // ピンインのアルファベットが含まれる
            Assert.True(result.Contains("n") || result.Contains("h"),
                $"ピンイン出力が期待と異なります: '{result}'");
        }

        [SkippableFact]
        public void Engine_DefaultJapanese_漢字テキスト_日本語音素出力()
        {
            SkipIfNoDictionary();
            var result = _fixture.DefaultEngine!.ToPhonemes("東京");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 日本語音素が含まれる
            Assert.True(result.Contains("o") || result.Contains("k"),
                $"日本語音素出力が期待と異なります: '{result}'");
        }

        [SkippableFact]
        public void Engine_DefaultChinese_英語と中国語混在()
        {
            SkipIfNoDictionary();
            var result = _fixture.ChineseDefaultEngine!.ToPhonemes("Hello你好");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(result.Length > 3,
                $"英中混在テキストの音素が短すぎます: '{result}'");
        }

        [SkippableFact]
        public void Engine_ToSegments_DefaultChinese_漢字セグメントがChinese()
        {
            SkipIfNoDictionary();
            var result = _fixture.ChineseDefaultEngine!.ToSegments("Hello你好");
            Assert.True(result.Count >= 2, $"セグメント数が2未満: {result.Count}");
            Assert.Contains(result, s => s.Language == Language.English);
            Assert.Contains(result, s => s.Language == Language.Chinese);
        }

        [SkippableFact]
        public void Engine_ToSegments_DefaultJapanese_漢字セグメントがJapanese()
        {
            SkipIfNoDictionary();
            var result = _fixture.DefaultEngine!.ToSegments("Hello東京");
            Assert.True(result.Count >= 2, $"セグメント数が2未満: {result.Count}");
            Assert.Contains(result, s => s.Language == Language.English);
            Assert.Contains(result, s => s.Language == Language.Japanese);
        }

        [SkippableFact]
        public void Engine_ToPhonemesBatch_中国語テキスト混在()
        {
            SkipIfNoDictionary();
            var texts = new[] { "Hello", "你好", "こんにちは" };
            var result = _fixture.ChineseDefaultEngine!.ToPhonemesBatch(texts);
            Assert.Equal(3, result.Count);
            foreach (var phonemes in result)
            {
                Assert.NotNull(phonemes);
                Assert.NotEmpty(phonemes);
            }
        }

        [SkippableFact]
        public void Engine_ToSegmentsBatch_中国語テキスト混在()
        {
            SkipIfNoDictionary();
            var texts = new[] { "Hello你好", "こんにちはworld" };
            var result = _fixture.ChineseDefaultEngine!.ToSegmentsBatch(texts);
            Assert.Equal(2, result.Count);
            foreach (var segments in result)
            {
                Assert.NotNull(segments);
                Assert.NotEmpty(segments);
            }
        }

        [SkippableFact]
        public void Engine_DefaultChinese_繰り返し変換_同じ結果()
        {
            SkipIfNoDictionary();
            var input = "你好世界";
            var result1 = _fixture.ChineseDefaultEngine!.ToPhonemes(input);
            var result2 = _fixture.ChineseDefaultEngine.ToPhonemes(input);
            var result3 = _fixture.ChineseDefaultEngine.ToPhonemes(input);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [SkippableFact]
        public void Engine_DefaultChinese_空文字列_空文字列()
        {
            SkipIfNoDictionary();
            var result = _fixture.ChineseDefaultEngine!.ToPhonemes("");
            Assert.Equal("", result);
        }

        [SkippableFact]
        public void Engine_DefaultChinese_null_空文字列()
        {
            SkipIfNoDictionary();
            var result = _fixture.ChineseDefaultEngine!.ToPhonemes(null!);
            Assert.Equal("", result);
        }

        // =================================================================
        // 5. オプションテスト (5件)
        // =================================================================

        [Fact]
        public void Options_デフォルト_DefaultCjkLanguageはJapanese()
        {
            var options = new MultilingualG2POptions();
            Assert.Equal(Language.Japanese, options.DefaultCjkLanguage);
        }

        [Fact]
        public void Options_デフォルト_ChineseOptionsはnull()
        {
            var options = new MultilingualG2POptions();
            Assert.Null(options.ChineseOptions);
        }

        [Fact]
        public void Options_ChineseOptions指定_保持される()
        {
            var chineseOpts = new ChineseG2POptions(
                defaultStyle: PinyinStyle.ToneNumber,
                enableToneSandhi: false);
            var options = new MultilingualG2POptions(chineseOptions: chineseOpts);
            Assert.NotNull(options.ChineseOptions);
            Assert.Equal(PinyinStyle.ToneNumber, options.ChineseOptions!.DefaultStyle);
            Assert.False(options.ChineseOptions.EnableToneSandhi);
        }

        [Fact]
        public void Options_DefaultCjkLanguageChinese指定_保持される()
        {
            var options = new MultilingualG2POptions(
                defaultCjkLanguage: Language.Chinese);
            Assert.Equal(Language.Chinese, options.DefaultCjkLanguage);
        }

        [Fact]
        public void Options_後方互換_従来パラメータのみ指定_動作する()
        {
            // 従来の2パラメータ（japaneseOptions, englishOptions）のみ指定
            var options = new MultilingualG2POptions(
                japaneseOptions: G2POptions.Default,
                englishOptions: DotNetG2P.English.EnglishG2POptions.Default);
            Assert.Equal(Language.Japanese, options.DefaultCjkLanguage);
            Assert.Null(options.ChineseOptions);
            Assert.Equal(" ", options.SegmentSeparator);
        }

        // =================================================================
        // 6. Dispose テスト (4件)
        // =================================================================

        [SkippableFact]
        public void Dispose後_ToPhonemes_ObjectDisposedException_Chinese()
        {
            SkipIfNoDictionary();
            var options = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
            var engine = new MultilingualG2PEngine(_fixture.DictPath!, options);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("你好"));
        }

        [SkippableFact]
        public void Dispose後_ToSegments_ObjectDisposedException_Chinese()
        {
            SkipIfNoDictionary();
            var options = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
            var engine = new MultilingualG2PEngine(_fixture.DictPath!, options);
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToSegments("你好Hello"));
        }

        [SkippableFact]
        public void 二重Dispose_Chinese設定_例外なし()
        {
            SkipIfNoDictionary();
            var options = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
            var engine = new MultilingualG2PEngine(_fixture.DictPath!, options);
            engine.Dispose();
            var exception = Record.Exception(() => engine.Dispose());
            Assert.Null(exception);
        }

        [SkippableFact]
        public void Chinese設定_正常動作後Dispose_安全()
        {
            SkipIfNoDictionary();
            var options = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
            using var engine = new MultilingualG2PEngine(_fixture.DictPath!, options);

            // 正常動作を確認
            var phonemes = engine.ToPhonemes("你好Hello");
            Assert.NotNull(phonemes);
            Assert.NotEmpty(phonemes);

            var segments = engine.ToSegments("你好Hello");
            Assert.NotNull(segments);
            Assert.NotEmpty(segments);
        }

        // =================================================================
        // 7. 追加テスト: セグメント結合一貫性 (3件)
        // =================================================================

        [Fact]
        public void Segment_セグメント結合で元テキスト復元_中国語混在()
        {
            var inputs = new[]
            {
                "你好Hello",
                "Hello你好世界",
                "こんにちは你好hello",
            };

            foreach (var input in inputs)
            {
                var result = TextSegmenter.Segment(input, Language.Chinese);
                var combined = string.Concat(result.Select(s => s.Text));
                Assert.Equal(input, combined);
            }
        }

        [Fact]
        public void Segment_隣接セグメントは異なる言語_中国語混在()
        {
            var result = TextSegmenter.Segment("Hello你好world", Language.Chinese);
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.NotEqual(result[i].Language, result[i + 1].Language);
            }
        }

        [SkippableFact]
        public void Engine_ToPhonemes出力とToSegments出力が整合_中国語()
        {
            SkipIfNoDictionary();
            string input = "Hello你好world";
            var phonemes = _fixture.ChineseDefaultEngine!.ToPhonemes(input);
            var segments = _fixture.ChineseDefaultEngine.ToSegments(input);
            var segmentPhonemes = string.Join(" ", segments.Select(s => s.Phonemes));
            Assert.Equal(phonemes, segmentPhonemes);
        }
    }
}


