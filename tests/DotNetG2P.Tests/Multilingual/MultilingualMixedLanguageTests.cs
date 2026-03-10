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

        // ===== 仏西 2言語混在テスト =====

        [Fact]
        public void TextSegmenter_仏西2言語混在_正しく分割される()
        {
            // DefaultLatinLanguage = English の場合:
            // café は é のみ（acute-e only）→ フランス語と判定
            // canción は ó（スペイン語特有アクセント）→ スペイン語と判定
            var result = TextSegmenter.Segment("café canción", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal("café ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("canción", result[1].Text);
        }

        [Fact]
        public void Engine_仏西混在_bonjour_señor()
        {
            // DefaultLatinLanguage = English で、bonjour はフランス語高頻度語シグナル、
            // señor は ñ（スペイン語特有文字）でスペイン語に分割される
            var result = TextSegmenter.Segment("bonjour señor", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal("bonjour ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("señor", result[1].Text);
        }

        [Fact]
        public void Engine_DefaultLatinEnglish_仏西混在_アクセント付きフランス語はFrenchに分類()
        {
            // DefaultLatinLanguage = English で:
            // résumé は é のみ（acute-e only）→ フランス語と判定
            // amigo はスペイン語高頻度語シグナル → スペイン語と判定
            var result = TextSegmenter.Segment("résumé amigo", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal("résumé ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("amigo", result[1].Text);
        }

        // ===== 3言語混在テスト =====

        [Fact]
        public void TextSegmenter_日仏英3言語混在_正しく分割される()
        {
            // DefaultLatinLanguage = English:
            // 東京で → 日本語（ひらがな・漢字）
            // bonjour → フランス語（高頻度語シグナル）
            // hello → 英語（英語高頻度語シグナル）
            var result = TextSegmenter.Segment("東京で bonjour hello", Language.Japanese, Language.English);

            Assert.Equal(3, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("東京で ", result[0].Text);
            Assert.Equal(Language.French, result[1].Language);
            Assert.Equal("bonjour ", result[1].Text);
            Assert.Equal(Language.English, result[2].Language);
            Assert.Equal("hello", result[2].Text);
        }

        [Fact]
        public void TextSegmenter_仏英中3言語混在_正しく分割される()
        {
            // DefaultLatinLanguage = English, DefaultCjkLanguage = Chinese:
            // café → フランス語（é: acute-e only）
            // hello → 英語（英語高頻度語シグナル）
            // 你好 → 中国語（CJK既定 = Chinese）
            var result = TextSegmenter.Segment("café hello 你好", Language.Chinese, Language.English);

            Assert.Equal(3, result.Count);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal("café ", result[0].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal("hello ", result[1].Text);
            Assert.Equal(Language.Chinese, result[2].Language);
            Assert.Equal("你好", result[2].Text);
        }

        [Fact]
        public void TextSegmenter_仏西中3言語混在_正しく分割される()
        {
            // DefaultLatinLanguage = English, DefaultCjkLanguage = Chinese:
            // café → フランス語（é: acute-e only）
            // canción → スペイン語（ó: スペイン語特有アクセント）
            // 你好 → 中国語（CJK既定 = Chinese）
            var result = TextSegmenter.Segment("café canción 你好", Language.Chinese, Language.English);

            Assert.Equal(3, result.Count);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal("café ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("canción ", result[1].Text);
            Assert.Equal(Language.Chinese, result[2].Language);
            Assert.Equal("你好", result[2].Text);
        }

        // ===== 仏西・3言語混在エンジン統合テスト =====

        [SkippableFact]
        public void Engine_仏西混在_各セグメントが単独エンジンと一致()
        {
            SkipIfNoDictionary();

            // DefaultEngine（English既定）で café → French、señor → Spanish
            var segments = _fixture.DefaultEngine!.ToSegments("café señor");

            Assert.Equal(2, segments.Count);
            Assert.Equal(Language.French, segments[0].Language);
            Assert.Equal(Language.Spanish, segments[1].Language);

            Assert.Equal(_fixture.FrenchEngine.ToPhonemes(segments[0].SourceText), segments[0].Phonemes);
            Assert.Equal(_fixture.SpanishEngine.ToPhonemes(segments[1].SourceText), segments[1].Phonemes);
        }

        [SkippableFact]
        public void Engine_日仏英3言語混在_ToPhonemesとToSegmentsが整合()
        {
            SkipIfNoDictionary();

            const string input = "東京で café hello";
            var engine = _fixture.ChineseDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.Contains(segments, s => s.Language == Language.English);
        }

        [SkippableFact]
        public void Engine_仏英中3言語混在_ToPhonemesとToSegmentsが整合()
        {
            SkipIfNoDictionary();

            const string input = "café hello 你好";
            var engine = _fixture.ChineseDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.Contains(segments, s => s.Language == Language.English);
            Assert.Contains(segments, s => s.Language == Language.Chinese);
        }

        [SkippableFact]
        public void Engine_仏西中3言語混在_ToPhonemesとToSegmentsが整合()
        {
            SkipIfNoDictionary();

            const string input = "café canción 你好";
            var engine = _fixture.ChineseDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.Contains(segments, s => s.Language == Language.Spanish);
            Assert.Contains(segments, s => s.Language == Language.Chinese);
        }
    }
}
