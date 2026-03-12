using System;
using System.Linq;
using DotNetG2P.Korean;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualKoreanMixedLanguageTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualKoreanMixedLanguageTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        [Fact]
        public void TextSegmenter_韓英中仏4言語混在_期待順に分割される()
        {
            var result = TextSegmenter.Segment("안녕하세요 hello 你好 café", Language.Chinese, Language.English);

            Assert.Equal(4, result.Count);
            Assert.Equal(Language.Korean, result[0].Language);
            Assert.Equal("안녕하세요 ", result[0].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal("hello ", result[1].Text);
            Assert.Equal(Language.Chinese, result[2].Language);
            Assert.Equal("你好 ", result[2].Text);
            Assert.Equal(Language.French, result[3].Language);
            Assert.Equal("café", result[3].Text);
        }

        [Fact]
        public void TextSegmenter_韓西葡3言語混在_期待順に分割される()
        {
            var result = TextSegmenter.Segment("안녕하세요 señor obrigado", Language.Japanese, Language.Portuguese);

            Assert.Equal(3, result.Count);
            Assert.Equal(Language.Korean, result[0].Language);
            Assert.Equal("안녕하세요 ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("señor ", result[1].Text);
            Assert.Equal(Language.Portuguese, result[2].Language);
            Assert.Equal("obrigado", result[2].Text);
        }

        [SkippableFact]
        public void Engine_韓英中仏4言語混在_各セグメントが単独エンジンと一致する()
        {
            SkipIfNoDictionary();

            const string input = "안녕하세요 hello 你好 café";
            var segments = _fixture.ChineseDefaultEngine!.ToSegments(input);

            Assert.Equal(4, segments.Count);
            Assert.Equal(input, string.Concat(segments.Select(segment => segment.SourceText)));
            Assert.Equal(_fixture.ChineseDefaultEngine.ToPhonemes(input), string.Join(" ", segments.Select(segment => segment.Phonemes)));

            foreach (var segment in segments)
            {
                var standalone = segment.Language switch
                {
                    Language.Korean => _fixture.KoreanEngine.ToPhonemes(segment.SourceText),
                    Language.English => _fixture.EnglishEngine.ToPhonemes(segment.SourceText),
                    Language.Chinese => _fixture.ChineseEngine!.ToPinyin(segment.SourceText),
                    Language.French => _fixture.FrenchEngine.ToPhonemes(segment.SourceText),
                    _ => throw new InvalidOperationException($"Unexpected language: {segment.Language}"),
                };

                Assert.Equal(standalone, segment.Phonemes);
            }
        }

        [SkippableFact]
        public void Engine_韓西葡3言語混在_各セグメントが単独エンジンと一致する()
        {
            SkipIfNoDictionary();

            const string input = "안녕하세요 señor obrigado";
            var segments = _fixture.PortugueseDefaultEngine!.ToSegments(input);

            Assert.Equal(3, segments.Count);
            Assert.Equal(input, string.Concat(segments.Select(segment => segment.SourceText)));

            foreach (var segment in segments)
            {
                var standalone = segment.Language switch
                {
                    Language.Korean => _fixture.KoreanEngine.ToPhonemes(segment.SourceText),
                    Language.Spanish => _fixture.SpanishEngine.ToPhonemes(segment.SourceText),
                    Language.Portuguese => _fixture.PortugueseEngine.ToPhonemes(segment.SourceText),
                    _ => throw new InvalidOperationException($"Unexpected language: {segment.Language}"),
                };

                Assert.Equal(standalone, segment.Phonemes);
            }
        }

        [SkippableFact]
        public void Engine_KoreanOptionsがMultilingual経由でも反映される()
        {
            SkipIfNoDictionary();

            var koreanOptions = new KoreanG2POptions(uiVariationMode: KoreanUiVariationMode.Colloquial);
            using var standaloneKorean = new KoreanG2PEngine(koreanOptions);
            using var multilingual = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(
                    defaultCjkLanguage: Language.Chinese,
                    koreanOptions: koreanOptions));

            var segments = multilingual.ToSegments("나의 hello");

            Assert.Equal(2, segments.Count);
            Assert.Equal(Language.Korean, segments[0].Language);
            Assert.Equal(Language.English, segments[1].Language);
            Assert.Equal(standaloneKorean.ToPhonemes(segments[0].SourceText), segments[0].Phonemes);
        }
    }
}
