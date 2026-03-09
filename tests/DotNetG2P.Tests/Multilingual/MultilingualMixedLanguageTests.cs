using System;
using System.IO;
using System.Linq;
using DotNetG2P;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual;
using DotNetG2P.Spanish;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// 日英中西の4言語同時混在パターンを検証する。
    /// </summary>
    public class MultilingualMixedLanguageTests : IDisposable
    {
        private static string? FindDictPath()
        {
            var envPath = Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
                return envPath;

            var candidates = new[]
            {
                @"C:\Users\yuta\Desktop\Private\open_jtalk_dic_utf_8-1.11",
                @"C:\naist-jdic",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "naist-jdic"),
                "/usr/local/share/naist-jdic",
                "/usr/share/naist-jdic",
            };

            foreach (var path in candidates)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return null;
        }

        private static readonly string? DictPath = FindDictPath();

        private readonly bool _hasDictionary;
        private readonly MultilingualG2PEngine? _engine;
        private readonly MultilingualG2PEngine? _spanishDefaultEngine;
        private readonly G2PEngine? _japaneseEngine;
        private readonly EnglishG2PEngine _englishEngine = new EnglishG2PEngine();
        private readonly ChineseG2PEngine _chineseEngine = new ChineseG2PEngine();
        private readonly SpanishG2PEngine _spanishEngine = new SpanishG2PEngine();

        public MultilingualMixedLanguageTests()
        {
            _hasDictionary = DictPath != null;
            if (_hasDictionary)
            {
                var chineseCjkOptions = new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese);
                _engine = new MultilingualG2PEngine(DictPath!, chineseCjkOptions);

                var chineseAndSpanishDefaults = new MultilingualG2POptions(
                    defaultCjkLanguage: Language.Chinese,
                    defaultLatinLanguage: Language.Spanish);
                _spanishDefaultEngine = new MultilingualG2PEngine(DictPath!, chineseAndSpanishDefaults);

                _japaneseEngine = new G2PEngine(new MeCabTokenizer(DictPath!), G2POptions.Default);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
            _spanishDefaultEngine?.Dispose();
            _japaneseEngine?.Dispose();
            _englishEngine.Dispose();
            _chineseEngine.Dispose();
            _spanishEngine.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_hasDictionary, "naist-jdic辞書が見つかりません");
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

            var segments = _engine!.ToSegments("今日は canción 你好 world");
            Assert.Equal(4, segments.Count);

            foreach (var segment in segments)
            {
                var standalone = segment.Language switch
                {
                    Language.Japanese => _japaneseEngine!.ToPhonemes(segment.SourceText),
                    Language.English => _englishEngine.ToPhonemes(segment.SourceText),
                    Language.Chinese => _chineseEngine.ToPinyin(segment.SourceText),
                    Language.Spanish => _spanishEngine.ToPhonemes(segment.SourceText),
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
            var phonemes = _engine!.ToPhonemes(input);
            var segments = _engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
        }

        [SkippableFact]
        public void Engine_句読点と数字を含む4言語混在_全言語を保持する()
        {
            SkipIfNoDictionary();

            const string input = "今日は canción, 你好 API2026";
            var segments = _engine!.ToSegments(input);

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
            var segments = _spanishDefaultEngine!.ToSegments(input);

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

            var phonemeResults = _engine!.ToPhonemesBatch(texts);
            var segmentResults = _engine.ToSegmentsBatch(texts);

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
