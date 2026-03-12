using System;
using System.Linq;
using DotNetG2P.Korean;
using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual の韓国語統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualKoreanTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualKoreanTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        [Fact]
        public void Language_Korean_値は6()
        {
            Assert.Equal((byte)6, (byte)Language.Korean);
        }

        [Fact]
        public void Language_Korean_ToString()
        {
            Assert.Equal("Korean", Language.Korean.ToString());
        }

        [Theory]
        [InlineData('가')]
        [InlineData('각')]
        [InlineData('ㄱ')]
        [InlineData('ᄀ')]
        public void Classify_Hangul文字_Koreanを返す(char c)
        {
            Assert.Equal(ScriptKind.Korean, LanguageDetector.Classify(c));
        }

        [Fact]
        public void ToLanguage_Korean_LanguageKoreanを返す()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.Korean);

            Assert.NotNull(result);
            Assert.Equal(Language.Korean, result!.Value);
        }

        [Fact]
        public void Segment_韓国語のみ_1セグメントKorean()
        {
            var result = TextSegmenter.Segment("안녕하세요");

            Assert.Single(result);
            Assert.Equal("안녕하세요", result[0].Text);
            Assert.Equal(Language.Korean, result[0].Language);
        }

        [Fact]
        public void Segment_日韓英混在_3セグメント()
        {
            var result = TextSegmenter.Segment("こんにちは안녕하세요hello");

            Assert.Equal(3, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("こんにちは", result[0].Text);
            Assert.Equal(Language.Korean, result[1].Language);
            Assert.Equal("안녕하세요", result[1].Text);
            Assert.Equal(Language.English, result[2].Language);
            Assert.Equal("hello", result[2].Text);
        }

        [Fact]
        public void Segment_韓国語に隣接する数字と句読点_Koreanに吸収()
        {
            var result = TextSegmenter.Segment("2026년!");

            Assert.Single(result);
            Assert.Equal(Language.Korean, result[0].Language);
            Assert.Equal("2026년!", result[0].Text);
        }

        [Fact]
        public void Segment_セグメント結合で元テキスト復元_韓国語混在()
        {
            var inputs = new[]
            {
                "안녕하세요 hello",
                "東京안녕world",
                "hello 한국어 테스트",
            };

            foreach (var input in inputs)
            {
                var result = TextSegmenter.Segment(input);
                var combined = string.Concat(result.Select(s => s.Text));
                Assert.Equal(input, combined);
            }
        }

        [Fact]
        public void Options_KoreanOptions指定_保持される()
        {
            var koreanOptions = new KoreanG2POptions(separator: "|");
            var options = new MultilingualG2POptions(koreanOptions: koreanOptions);

            Assert.Same(koreanOptions, options.KoreanOptions);
        }

        [SkippableFact]
        public void Engine_韓国語のみ_Koreanセグメントとして返す()
        {
            SkipIfNoDictionary();

            var result = _fixture.DefaultEngine!.ToSegments("안녕하세요");

            Assert.Single(result);
            Assert.Equal(Language.Korean, result[0].Language);
            Assert.Equal(_fixture.KoreanEngine.ToPhonemes("안녕하세요"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_韓国語音素は単独KoreanEngineと一致()
        {
            SkipIfNoDictionary();

            const string input = "좋다";
            var result = _fixture.DefaultEngine!.ToSegments(input);

            Assert.Single(result);
            Assert.Equal(Language.Korean, result[0].Language);
            Assert.Equal(_fixture.KoreanEngine.ToPhonemes(input), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_日韓英混在_各言語に分割される()
        {
            SkipIfNoDictionary();

            var result = _fixture.DefaultEngine!.ToSegments("東京 안녕하세요 hello");

            Assert.True(result.Count >= 3, $"セグメント数が3未満: {result.Count}");
            Assert.Contains(result, s => s.Language == Language.Japanese);
            Assert.Contains(result, s => s.Language == Language.Korean);
            Assert.Contains(result, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_韓国語混在()
        {
            SkipIfNoDictionary();

            const string input = "hello 안녕하세요";
            var phonemes = _fixture.DefaultEngine!.ToPhonemes(input);
            var segments = _fixture.DefaultEngine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Contains(segments, s => s.Language == Language.Korean);
        }
    }
}
