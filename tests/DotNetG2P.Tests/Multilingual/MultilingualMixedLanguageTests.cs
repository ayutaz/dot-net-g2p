using System;
using System.IO;
using System.Linq;
using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.French;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// 日英中西仏の多言語同時混在パターンを検証する。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualMixedLanguageTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualMixedLanguageTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        [Fact]
        public void TextSegmenter_日英中西4言語混在_期待順に分割される()
        {
            var result = TextSegmenter.Segment("今日は canción 你好 world", Language.Chinese, Language.English);

            Assert.Equal(4, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal(Language.Chinese, result[2].Language);
            Assert.Equal(Language.English, result[3].Language);
            Assert.Equal("今日は ", result[0].Text);
            Assert.Equal("canción ", result[1].Text);
            Assert.Equal("你好 ", result[2].Text);
            Assert.Equal("world", result[3].Text);
        }

        [SkippableFact]
        public void Engine_ToSegments_4言語混在_各セグメントが単独エンジンと一致()
        {
            SkipIfNoDictionary();

            var segments = _fixture.ChineseDefaultEngine!.ToSegments("今日は canción 你好 world");
            Assert.Equal(4, segments.Count);

            foreach (var segment in segments)
            {
                var standalone = segment.Language switch
                {
                    Language.Japanese => _fixture.JapaneseEngine!.ToPhonemes(segment.SourceText),
                    Language.English => _fixture.EnglishEngine.ToPhonemes(segment.SourceText),
                    Language.Chinese => _fixture.ChineseEngine!.ToPinyin(segment.SourceText),
                    Language.Spanish => _fixture.SpanishEngine.ToPhonemes(segment.SourceText),
                    _ => throw new InvalidOperationException($"Unexpected language: {segment.Language}"),
                };

                Assert.Equal(standalone, segment.Phonemes);
            }
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_4言語混在()
        {
            SkipIfNoDictionary();

            const string input = "今日は canción 你好 world";
            var phonemes = _fixture.ChineseDefaultEngine!.ToPhonemes(input);
            var segments = _fixture.ChineseDefaultEngine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
        }

        [SkippableFact]
        public void Engine_句読点と数字を含む4言語混在_全言語を保持する()
        {
            SkipIfNoDictionary();

            const string input = "今日は canción, 你好 API2026";
            var segments = _fixture.ChineseDefaultEngine!.ToSegments(input);

            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.Spanish);
            Assert.Contains(segments, s => s.Language == Language.Chinese);
            Assert.Contains(segments, s => s.Language == Language.English);
            Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Phonemes)));
        }

        [SkippableFact]
        public void Engine_DefaultLatinSpanish_ASCIIスペイン語と中国語と日本語が混在しても分割できる()
        {
            SkipIfNoDictionary();

            const string input = "東京で hola 你好 señor";
            var segments = _fixture.ChineseSpanishDefaultEngine!.ToSegments(input);

            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Equal(Language.Japanese, segments[0].Language);
            Assert.Equal(Language.Spanish, segments[1].Language);
            Assert.Equal(Language.Chinese, segments[2].Language);
            Assert.Equal(Language.Spanish, segments[3].Language);
        }

        [SkippableFact]
        public void BatchAPI_4言語混在パターン複数_全て整合する()
        {
            SkipIfNoDictionary();

            var texts = new[]
            {
                "今日は canción 你好 world",
                "東京で hola 你好 señor",
                "今日は canción, 你好 API2026",
            };

            var phonemeResults = _fixture.ChineseDefaultEngine!.ToPhonemesBatch(texts);
            var segmentResults = _fixture.ChineseDefaultEngine.ToSegmentsBatch(texts);

            Assert.Equal(texts.Length, phonemeResults.Count);
            Assert.Equal(texts.Length, segmentResults.Count);

            for (int i = 0; i < texts.Length; i++)
            {
                Assert.NotEmpty(phonemeResults[i]);
                Assert.NotEmpty(segmentResults[i]);
                Assert.Equal(texts[i], string.Concat(segmentResults[i].Select(s => s.SourceText)));
                Assert.Equal(phonemeResults[i], string.Join(" ", segmentResults[i].Select(s => s.Phonemes)));
            }
        }

        // ===== 5言語混在テスト（日英中西仏） =====

        [Fact]
        public void TextSegmenter_日英中西仏5言語混在_期待順に分割される()
        {
            // café はアクセント付きラテン文字でフランス語と判別される
            var result = TextSegmenter.Segment("今日は café canción 你好 world", Language.Chinese, Language.English);

            Assert.Equal(5, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("今日は ", result[0].Text);
            Assert.Equal(Language.French, result[1].Language);
            Assert.Equal("café ", result[1].Text);
            Assert.Equal(Language.Spanish, result[2].Language);
            Assert.Equal("canción ", result[2].Text);
            Assert.Equal(Language.Chinese, result[3].Language);
            Assert.Equal("你好 ", result[3].Text);
            Assert.Equal(Language.English, result[4].Language);
            Assert.Equal("world", result[4].Text);
        }

        [SkippableFact]
        public void Engine_ToSegments_5言語混在_各セグメントが単独エンジンと一致()
        {
            SkipIfNoDictionary();

            var segments = _fixture.ChineseDefaultEngine!.ToSegments("今日は café canción 你好 world");
            Assert.Equal(5, segments.Count);

            foreach (var segment in segments)
            {
                var standalone = segment.Language switch
                {
                    Language.Japanese => _fixture.JapaneseEngine!.ToPhonemes(segment.SourceText),
                    Language.English => _fixture.EnglishEngine.ToPhonemes(segment.SourceText),
                    Language.Chinese => _fixture.ChineseEngine!.ToPinyin(segment.SourceText),
                    Language.Spanish => _fixture.SpanishEngine.ToPhonemes(segment.SourceText),
                    Language.French => _fixture.FrenchEngine.ToPhonemes(segment.SourceText),
                    _ => throw new InvalidOperationException($"Unexpected language: {segment.Language}"),
                };

                Assert.Equal(standalone, segment.Phonemes);
            }
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_5言語混在()
        {
            SkipIfNoDictionary();

            const string input = "今日は café canción 你好 world";
            var phonemes = _fixture.ChineseDefaultEngine!.ToPhonemes(input);
            var segments = _fixture.ChineseDefaultEngine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
        }

        [SkippableFact]
        public void Engine_日仏混在_東京la_tour_Eiffel()
        {
            SkipIfNoDictionary();

            // FrenchDefaultEngineではラテン文字のデフォルトがフランス語
            const string input = "東京の la tour Eiffel";
            var segments = _fixture.FrenchDefaultEngine!.ToSegments(input);

            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Phonemes)));
        }

        [SkippableFact]
        public void Engine_英仏混在_the_café()
        {
            SkipIfNoDictionary();

            // DefaultEngine（English既定）で変換 — café のアクセント付きéでフランス語と判別
            const string input = "the café";
            var segments = _fixture.DefaultEngine!.ToSegments(input);

            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.English);
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Phonemes)));
        }

        [SkippableFact]
        public void Engine_中仏混在_巴黎est_magnifique()
        {
            SkipIfNoDictionary();

            // ChineseDefaultEngine + フランス語はアクセント文字なしでも文脈で判別は困難なので
            // FrenchDefaultEngine（Latin既定=French）+ CJK既定=Chineseの組み合わせが必要
            const string input = "巴黎 est magnifique";
            var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese, defaultLatinLanguage: Language.French));
            try
            {
                var segments = engine.ToSegments(input);

                Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
                Assert.Contains(segments, s => s.Language == Language.Chinese);
                Assert.Contains(segments, s => s.Language == Language.French);
                Assert.All(segments, s => Assert.False(string.IsNullOrWhiteSpace(s.Phonemes)));
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public void BatchAPI_5言語混在パターン複数_全て整合する()
        {
            SkipIfNoDictionary();

            var texts = new[]
            {
                "今日は café canción 你好 world",
                "東京の café 你好 señor hello",
                "café 今日は canción 你好 world",
            };

            var phonemeResults = _fixture.ChineseDefaultEngine!.ToPhonemesBatch(texts);
            var segmentResults = _fixture.ChineseDefaultEngine.ToSegmentsBatch(texts);

            Assert.Equal(texts.Length, phonemeResults.Count);
            Assert.Equal(texts.Length, segmentResults.Count);

            for (int i = 0; i < texts.Length; i++)
            {
                Assert.NotEmpty(phonemeResults[i]);
                Assert.NotEmpty(segmentResults[i]);
                Assert.Equal(texts[i], string.Concat(segmentResults[i].Select(s => s.SourceText)));
                Assert.Equal(phonemeResults[i], string.Join(" ", segmentResults[i].Select(s => s.Phonemes)));
            }
        }
    }
}
