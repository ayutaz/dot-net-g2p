using System;
using System.IO;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.Portuguese;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual のポルトガル語統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualPortugueseTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualPortugueseTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        // ===== 基本テスト =====

        [Fact]
        public void Language_Portuguese_値は5()
        {
            Assert.Equal((byte)5, (byte)Language.Portuguese);
        }

        [Fact]
        public void LanguageDetector_ToLanguage_LatinにPortuguese既定を渡すとPortuguese()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.Latin, Language.Portuguese);

            Assert.NotNull(result);
            Assert.Equal(Language.Portuguese, result!.Value);
        }

        [Fact]
        public void Segment_DefaultLatinPortuguese_ASCIIポルトガル語をPortugueseに分類()
        {
            var result = TextSegmenter.Segment("obrigado muito bom", Language.Japanese, Language.Portuguese);

            Assert.Single(result);
            Assert.Equal("obrigado muito bom", result[0].Text);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_チルダ付きポルトガル語はPortugueseに分類()
        {
            // ã (U+00E3) はポルトガル語特有文字としてPortugueseに分類される
            var result = TextSegmenter.Segment("cora\u00E7\u00E3o", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_英語語彙はEnglishのまま()
        {
            var result = TextSegmenter.Segment("hello", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_高頻度ポルトガル語語はPortugueseに分類()
        {
            // "obrigado" と "muito" はポルトガル語シグナル語彙
            var result = TextSegmenter.Segment("obrigado muito bom", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_ポルトガル語セディーユ付き_ão_Portugueseに分類()
        {
            // ç + ão の組み合わせはポルトガル語特有（ã がキー）
            var result = TextSegmenter.Segment("cora\u00E7\u00E3o", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_õ付き語はPortugueseに分類()
        {
            // õ (U+00F5) はポルトガル語特有文字
            var result = TextSegmenter.Segment("sim\u00F5es", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultPortuguese_ASCII数字はPortuguese()
        {
            var result = TextSegmenter.Segment("2026", Language.Japanese, Language.Portuguese);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_ASCII数字はEnglish()
        {
            var result = TextSegmenter.Segment("2026", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Options_DefaultLatinLanguage_既定はEnglish()
        {
            var options = new MultilingualG2POptions();

            Assert.Equal(Language.English, options.DefaultLatinLanguage);
            Assert.Null(options.PortugueseOptions);
        }

        [Fact]
        public void Options_Portuguese指定_保持される()
        {
            var portugueseOptions = new PortugueseG2POptions(enableAllophones: true, includeStress: false);
            var options = new MultilingualG2POptions(
                portugueseOptions: portugueseOptions,
                defaultLatinLanguage: Language.Portuguese);

            Assert.NotNull(options.PortugueseOptions);
            Assert.True(options.PortugueseOptions!.EnableAllophones);
            Assert.False(options.PortugueseOptions.IncludeStress);
            Assert.Equal(Language.Portuguese, options.DefaultLatinLanguage);
        }

        [Fact]
        public void Options_PortugueseOptions_Null時はデフォルト()
        {
            var options = new MultilingualG2POptions();

            Assert.Null(options.PortugueseOptions);
        }

        [Fact]
        public void Segment_ポルトガル語ハイフン_Portugueseセグメント()
        {
            var result = TextSegmenter.Segment("guarda-chuva", Language.Japanese, Language.Portuguese);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultLatinPortuguese_obrigado_Portugueseに分類()
        {
            var result = TextSegmenter.Segment("obrigado", Language.Japanese, Language.Portuguese);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        // ===== エンジン統合テスト =====

        [SkippableFact]
        public void Engine_DefaultLatinPortuguese_ポルトガル語のみ_Portugueseセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.PortugueseDefaultEngine!.ToSegments("obrigado muito bom");

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
            Assert.Equal(_fixture.PortugueseEngine.ToPhonemes("obrigado muito bom"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultLatinPortuguese_日葡混在_日本語とPortugueseに分割()
        {
            SkipIfNoDictionary();

            var result = _fixture.PortugueseDefaultEngine!.ToSegments("東京obrigado");

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.Portuguese, result[1].Language);
            Assert.Equal(_fixture.PortugueseEngine.ToPhonemes(result[1].SourceText), result[1].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultEnglish_チルダ付きポルトガル語はPortugueseセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.DefaultEngine!.ToSegments("cora\u00E7\u00E3o");

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
            Assert.Equal(_fixture.PortugueseEngine.ToPhonemes("cora\u00E7\u00E3o"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_日葡混在()
        {
            SkipIfNoDictionary();

            const string input = "obrigado \u4E16\u754C";
            var engine = _fixture.PortugueseDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Contains(segments, s => s.Language == Language.Portuguese);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
        }

        [SkippableFact]
        public void Engine_DefaultLatinPortuguese_Dispose後_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Portuguese));
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("obrigado"));
        }

        // ===== 追加テスト =====

        [Fact]
        public void Segment_DefaultEnglish_ã付きポルトガル語名詞はPortugueseに分類()
        {
            // "informação" は ã を含むのでポルトガル語と判定される
            var result = TextSegmenter.Segment("informa\u00E7\u00E3o", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_õ付きポルトガル語複数形はPortugueseに分類()
        {
            // "informações" は õ を含むのでポルトガル語と判定される
            var result = TextSegmenter.Segment("informa\u00E7\u00F5es", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Portuguese, result[0].Language);
        }

        [SkippableFact]
        public void Engine_BatchAPI_ポルトガル語複数テキスト_全て変換可能()
        {
            SkipIfNoDictionary();

            var texts = new[] { "obrigado", "muito", "bom dia" };
            var results = _fixture.PortugueseDefaultEngine!.ToPhonemesBatch(texts);

            Assert.Equal(3, results.Count);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.False(string.IsNullOrEmpty(results[i]),
                    $"テキスト '{texts[i]}' の変換結果が空です");
                Assert.Equal(_fixture.PortugueseEngine.ToPhonemes(texts[i]), results[i]);
            }
        }
    }
}
