using System;
using System.IO;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual のスペイン語統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualSpanishTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualSpanishTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        [Fact]
        public void Language_Spanish_値は3()
        {
            Assert.Equal((byte)3, (byte)Language.Spanish);
        }

        [Fact]
        public void LanguageDetector_ToLanguage_LatinにSpanish既定を渡すとSpanish()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.Latin, Language.Spanish);

            Assert.NotNull(result);
            Assert.Equal(Language.Spanish, result!.Value);
        }

        [Fact]
        public void Segment_DefaultLatinSpanish_ASCIIスペイン語をSpanishに分類()
        {
            var result = TextSegmenter.Segment("hola mundo", Language.Japanese, Language.Spanish);

            Assert.Single(result);
            Assert.Equal("hola mundo", result[0].Text);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_アクセント付きスペイン語はSpanishに分類()
        {
            var result = TextSegmenter.Segment("canción", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal("canción", result[0].Text);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_英語語彙はEnglishのまま()
        {
            var result = TextSegmenter.Segment("hello", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_CommonASCIISpanishWordsはSpanishに分類()
        {
            var result = TextSegmenter.Segment("hola mundo", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal("hola mundo", result[0].Text);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_ASCIISpanishLoanPhraseはSpanishに分類()
        {
            var result = TextSegmenter.Segment("wifi gratis", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_GermanUmlautWordはSpanishに誤分類しない()
        {
            var result = TextSegmenter.Segment("über", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_GueiPatternWithDiaeresisはSpanishに分類()
        {
            var result = TextSegmenter.Segment("pingüino", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_スペイン語記号付き_1セグメントSpanish()
        {
            var result = TextSegmenter.Segment("¡hola!", Language.Japanese, Language.Spanish);

            Assert.Single(result);
            Assert.Equal("¡hola!", result[0].Text);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Segment_ASCII数字のみ_DefaultEnglishではEnglish()
        {
            var result = TextSegmenter.Segment("2026", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_ASCII数字のみ_DefaultLatinSpanishではSpanish()
        {
            var result = TextSegmenter.Segment("2026", Language.Japanese, Language.Spanish);

            Assert.Single(result);
            Assert.Equal(Language.Spanish, result[0].Language);
        }

        [Fact]
        public void Options_DefaultLatinLanguage_既定はEnglish()
        {
            var options = new MultilingualG2POptions();

            Assert.Equal(Language.English, options.DefaultLatinLanguage);
            Assert.Null(options.SpanishOptions);
        }

        [Fact]
        public void Options_Spanish指定_保持される()
        {
            var spanishOptions = new SpanishG2POptions(enableAllophones: true, includeStress: false);
            var options = new MultilingualG2POptions(
                spanishOptions: spanishOptions,
                defaultLatinLanguage: Language.Spanish);

            Assert.NotNull(options.SpanishOptions);
            Assert.True(options.SpanishOptions!.EnableAllophones);
            Assert.False(options.SpanishOptions.IncludeStress);
            Assert.Equal(Language.Spanish, options.DefaultLatinLanguage);
        }

        [Fact]
        public void Options_DefaultLatinLanguage_日本語指定はArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MultilingualG2POptions(defaultLatinLanguage: Language.Japanese));
        }

        [SkippableFact]
        public void Engine_DefaultLatinSpanish_スペイン語のみ_Spanishセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.SpanishDefaultEngine!.ToSegments("hola mundo");

            Assert.Single(result);
            Assert.Equal(Language.Spanish, result[0].Language);
            Assert.Equal(_fixture.SpanishEngine.ToPhonemes("hola mundo"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultLatinSpanish_日西混在_日本語とSpanishに分割()
        {
            SkipIfNoDictionary();

            var result = _fixture.SpanishDefaultEngine!.ToSegments("東京hola");

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal(_fixture.SpanishEngine.ToPhonemes(result[1].SourceText), result[1].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultEnglish_アクセント付きスペイン語はSpanishセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.DefaultEngine!.ToSegments("canción");

            Assert.Single(result);
            Assert.Equal(Language.Spanish, result[0].Language);
            Assert.Equal(_fixture.SpanishEngine.ToPhonemes("canción"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_日西混在()
        {
            SkipIfNoDictionary();

            const string input = "hola 世界";
            var engine = _fixture.SpanishDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Contains(segments, s => s.Language == Language.Spanish);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
        }
    }
}
