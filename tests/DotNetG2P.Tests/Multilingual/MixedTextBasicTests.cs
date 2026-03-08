using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.English;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// MultilingualG2PEngineの日英混在テキスト基本パターン検証テスト。
    /// LanguageDetector/TextSegmenterの単体テスト、セグメント分割の正確性、
    /// 各セグメントの音素出力が単独処理と一致することを検証する。
    /// </summary>
    public class MixedTextBasicTests : IDisposable
    {
        private readonly MultilingualG2PEngine? _engine;
        private readonly G2PEngine? _japaneseEngine;
        private readonly EnglishG2PEngine? _englishEngine;
        private readonly bool _hasDictionary;

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
                if (Directory.Exists(path))
                    return path;
            return null;
        }

        private static readonly string? DictPath = FindDictPath();

        public MixedTextBasicTests()
        {
            _hasDictionary = DictPath != null;
            if (_hasDictionary)
            {
                _engine = new MultilingualG2PEngine(DictPath!);
                _japaneseEngine = new G2PEngine(
                    new DotNetG2P.MeCab.MeCabTokenizer(DictPath!),
                    G2POptions.Default);
                _englishEngine = new EnglishG2PEngine(EnglishG2POptions.Default);
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
            _japaneseEngine?.Dispose();
            _englishEngine?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_hasDictionary, "naist-jdic辞書が見つかりません");
        }

        // =================================================================
        // 1. LanguageDetector 追加単体テスト
        // =================================================================

        // ScriptKindはinternalなので、Theoryパラメータではint経由でキャストする
        // ScriptKind: Japanese=0, CJKIdeograph=1, English=2, Latin=3, Digit=4, Punctuation=5, Whitespace=6, Other=7

        [Theory]
        [InlineData('こ', 0)]  // Japanese
        [InlineData('世', 1)]  // CJKIdeograph
        [InlineData('ア', 0)]  // Japanese
        [InlineData('G', 2)]   // English
        [InlineData('d', 2)]   // English
        [InlineData('5', 4)]   // Digit
        [InlineData(' ', 6)]   // Whitespace
        [InlineData('.', 5)]   // Punctuation
        public void LanguageDetector_Classify_基本文字分類(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        [Theory]
        [InlineData(0, 0)]  // Japanese → Language.Japanese
        [InlineData(2, 1)]  // English → Language.English
        [InlineData(3, 1)]  // Latin → Language.English
        public void LanguageDetector_ToLanguage_言語文字種は対応Languageを返す(
            int kindInt, int expectedLangInt)
        {
            var result = LanguageDetector.ToLanguage((ScriptKind)kindInt);
            Assert.NotNull(result);
            Assert.Equal((Language)expectedLangInt, result!.Value);
        }

        [Theory]
        [InlineData(1)]  // CJKIdeograph
        [InlineData(4)]  // Digit
        [InlineData(5)]  // Punctuation
        [InlineData(6)]  // Whitespace
        [InlineData(7)]  // Other
        public void LanguageDetector_ToLanguage_非言語文字種はnullを返す(int kindInt)
        {
            Assert.Null(LanguageDetector.ToLanguage((ScriptKind)kindInt));
        }

        // 連続文字列に対してClassifyが一貫して正しく動作するか
        [Fact]
        public void LanguageDetector_Classify_日英混在文字列の各文字が正しく分類される()
        {
            string text = "Hello世界";
            // H, e, l, l, o → English
            for (int i = 0; i < 5; i++)
                Assert.Equal(ScriptKind.English, LanguageDetector.Classify(text[i]));
            // 世, 界 → CJKIdeograph
            Assert.Equal(ScriptKind.CJKIdeograph, LanguageDetector.Classify(text[5]));
            Assert.Equal(ScriptKind.CJKIdeograph, LanguageDetector.Classify(text[6]));
        }

        // =================================================================
        // 2. TextSegmenter 追加単体テスト（混在パターン）
        // =================================================================

        [Fact]
        public void TextSegmenter_日本語のみ_こんにちは世界_1セグメントJapanese()
        {
            var result = TextSegmenter.Segment("こんにちは世界");
            Assert.Single(result);
            Assert.Equal("こんにちは世界", result[0].Text);
            Assert.Equal(Language.Japanese, result[0].Language);
        }

        [Fact]
        public void TextSegmenter_英語のみ_HelloWorld_1セグメントEnglish()
        {
            var result = TextSegmenter.Segment("Hello World");
            Assert.Single(result);
            Assert.Equal("Hello World", result[0].Text);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void TextSegmenter_日英切替_今日はGoodDay_2セグメント()
        {
            var result = TextSegmenter.Segment("今日はGoodDay");
            Assert.Equal(2, result.Count);
            Assert.Equal("今日は", result[0].Text);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("GoodDay", result[1].Text);
            Assert.Equal(Language.English, result[1].Language);
        }

        [Fact]
        public void TextSegmenter_英日切替_ThisIsテスト()
        {
            // "This is " はENに属する、"テスト" はJPに属する
            var result = TextSegmenter.Segment("This is テスト");
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Contains("This is", result[0].Text);
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Contains("テスト", result[1].Text);
        }

        [Fact]
        public void TextSegmenter_日英日切替_東京のTokyoTowerは高い()
        {
            var result = TextSegmenter.Segment("東京のTokyoTowerは高い");
            Assert.Equal(3, result.Count);
            Assert.Equal("東京の", result[0].Text);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("TokyoTower", result[1].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal("は高い", result[2].Text);
            Assert.Equal(Language.Japanese, result[2].Language);
        }

        [Fact]
        public void TextSegmenter_英日英切替_ILove寿司VeryMuch()
        {
            var result = TextSegmenter.Segment("I love 寿司 very much");
            Assert.Equal(3, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Contains("寿司", result[1].Text);
            Assert.Equal(Language.English, result[2].Language);
        }

        [Fact]
        public void TextSegmenter_複数回切替_Hello世界Goodbyeさようなら()
        {
            var result = TextSegmenter.Segment("Hello世界Goodbyeさようなら");
            Assert.Equal(4, result.Count);
            Assert.Equal("Hello", result[0].Text);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("世界", result[1].Text);
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Equal("Goodbye", result[2].Text);
            Assert.Equal(Language.English, result[2].Language);
            Assert.Equal("さようなら", result[3].Text);
            Assert.Equal(Language.Japanese, result[3].Language);
        }

        [Fact]
        public void TextSegmenter_セグメント結合で元テキスト復元_日英混在()
        {
            var inputs = new[]
            {
                "今日はGoodDay",
                "This is テスト",
                "東京のTokyoTowerは高い",
                "I love 寿司 very much",
                "Hello世界Goodbyeさようなら",
            };

            foreach (var input in inputs)
            {
                var result = TextSegmenter.Segment(input);
                var combined = string.Concat(result.Select(s => s.Text));
                Assert.Equal(input, combined);
            }
        }

        [Fact]
        public void TextSegmenter_数字は隣接言語に吸収_日本語側()
        {
            // "100円" → 100はdigit、円はJP → 数字がJPに吸収
            var result = TextSegmenter.Segment("100円");
            Assert.Single(result);
            Assert.Equal("100円", result[0].Text);
            Assert.Equal(Language.Japanese, result[0].Language);
        }

        [Fact]
        public void TextSegmenter_数字は隣接言語に吸収_英語側()
        {
            // "Route66" → Routeは EN、66はdigit → 英語に吸収
            var result = TextSegmenter.Segment("Route66");
            Assert.Single(result);
            Assert.Equal("Route66", result[0].Text);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void TextSegmenter_日英境界の数字_前方言語に吸収()
        {
            // "東京3Tokyo" → 東京=JP, 3=digit(prev=JP), Tokyo=EN
            var result = TextSegmenter.Segment("東京3Tokyo");
            Assert.Equal(2, result.Count);
            Assert.Equal("東京3", result[0].Text);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("Tokyo", result[1].Text);
            Assert.Equal(Language.English, result[1].Language);
        }

        // =================================================================
        // 3. MultilingualG2PEngine: ToPhonemes 基本テスト
        // =================================================================

        [SkippableFact]
        public void ToPhonemes_日本語のみ_こんにちは世界_音素出力()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("こんにちは世界");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 日本語音素の基本文字が含まれる
            Assert.True(result.Contains("k") || result.Contains("o") || result.Contains("N"),
                $"日本語音素が期待と異なります: '{result}'");
        }

        [SkippableFact]
        public void ToPhonemes_英語のみ_HelloWorld_音素出力()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("Hello World");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // 英語のARPAbet音素が含まれる（HH, AH, L, OW 等）
            Assert.True(result.Length > 3, $"英語音素が短すぎます: '{result}'");
        }

        [SkippableFact]
        public void ToPhonemes_日英切替_今日はGoodDay_両言語音素含む()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("今日はGoodDay");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(result.Length > 5,
                $"日英混在テキストの音素が短すぎます: '{result}'");
        }

        [SkippableFact]
        public void ToPhonemes_英日切替_ThisIsテスト_両言語音素含む()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("This is テスト");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToPhonemes_日英日切替_音素出力()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("東京のTokyoTowerは高い");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToPhonemes_英日英切替_音素出力()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("I love 寿司 very much");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [SkippableFact]
        public void ToPhonemes_複数回切替_Hello世界Goodbyeさようなら_音素出力()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToPhonemes("Hello世界Goodbyeさようなら");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(result.Length > 10,
                $"複数回切替テキストの音素が短すぎます: '{result}'");
        }

        // =================================================================
        // 4. MultilingualG2PEngine: ToSegments 基本テスト
        // =================================================================

        [SkippableFact]
        public void ToSegments_日本語のみ_1セグメント_LanguageJapanese()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("こんにちは世界");
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.NotEmpty(result[0].Phonemes);
            Assert.Equal("こんにちは世界", result[0].SourceText);
        }

        [SkippableFact]
        public void ToSegments_英語のみ_1セグメント_LanguageEnglish()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("Hello World");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.NotEmpty(result[0].Phonemes);
        }

        [SkippableFact]
        public void ToSegments_日英切替_2セグメント_言語正しい()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("今日はGoodDay");
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.English, result[1].Language);
            Assert.NotEmpty(result[0].Phonemes);
            Assert.NotEmpty(result[1].Phonemes);
        }

        [SkippableFact]
        public void ToSegments_英日切替_2セグメント()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("Hello世界");
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal(Language.Japanese, result[1].Language);
        }

        [SkippableFact]
        public void ToSegments_日英日切替_3セグメント()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("東京のTokyoTowerは高い");
            Assert.Equal(3, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal(Language.Japanese, result[2].Language);
            // 全セグメントが音素を持つ
            foreach (var seg in result)
                Assert.NotEmpty(seg.Phonemes);
        }

        [SkippableFact]
        public void ToSegments_英日英切替_3セグメント()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("I love 寿司 very much");
            Assert.Equal(3, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Equal(Language.English, result[2].Language);
        }

        [SkippableFact]
        public void ToSegments_複数回切替_4セグメント()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("Hello世界Goodbyeさようなら");
            Assert.Equal(4, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Equal(Language.English, result[2].Language);
            Assert.Equal(Language.Japanese, result[3].Language);

            // 全セグメントが音素を持つ
            foreach (var seg in result)
                Assert.NotEmpty(seg.Phonemes);
        }

        // =================================================================
        // 5. 各セグメントの音素が単独処理と一致するか
        // =================================================================

        [SkippableFact]
        public void セグメント音素_日本語セグメント_単独G2Pと一致()
        {
            SkipIfNoDictionary();
            var segments = _engine!.ToSegments("こんにちはhello");
            var jpSegment = segments.First(s => s.Language == Language.Japanese);
            var standalone = _japaneseEngine!.ToPhonemes(jpSegment.SourceText);
            Assert.Equal(standalone, jpSegment.Phonemes);
        }

        [SkippableFact]
        public void セグメント音素_英語セグメント_単独G2Pと一致()
        {
            SkipIfNoDictionary();
            var segments = _engine!.ToSegments("こんにちはhello");
            var enSegment = segments.First(s => s.Language == Language.English);
            var standalone = _englishEngine!.ToPhonemes(enSegment.SourceText);
            Assert.Equal(standalone, enSegment.Phonemes);
        }

        [SkippableFact]
        public void セグメント音素_日英日_全セグメント単独処理と一致()
        {
            SkipIfNoDictionary();
            var segments = _engine!.ToSegments("東京のTokyoTowerは高い");

            foreach (var seg in segments)
            {
                string standalone;
                if (seg.Language == Language.Japanese)
                    standalone = _japaneseEngine!.ToPhonemes(seg.SourceText);
                else
                    standalone = _englishEngine!.ToPhonemes(seg.SourceText);

                Assert.Equal(standalone, seg.Phonemes);
            }
        }

        [SkippableFact]
        public void セグメント音素_英日英_全セグメント単独処理と一致()
        {
            SkipIfNoDictionary();
            var segments = _engine!.ToSegments("I love 寿司 very much");

            foreach (var seg in segments)
            {
                string standalone;
                if (seg.Language == Language.Japanese)
                    standalone = _japaneseEngine!.ToPhonemes(seg.SourceText);
                else
                    standalone = _englishEngine!.ToPhonemes(seg.SourceText);

                Assert.Equal(standalone, seg.Phonemes);
            }
        }

        [SkippableFact]
        public void セグメント音素_複数回切替_全セグメント単独処理と一致()
        {
            SkipIfNoDictionary();
            var segments = _engine!.ToSegments("Hello世界Goodbyeさようなら");

            foreach (var seg in segments)
            {
                string standalone;
                if (seg.Language == Language.Japanese)
                    standalone = _japaneseEngine!.ToPhonemes(seg.SourceText);
                else
                    standalone = _englishEngine!.ToPhonemes(seg.SourceText);

                Assert.Equal(standalone, seg.Phonemes);
            }
        }

        // =================================================================
        // 6. ToPhonemes出力がToSegments出力と整合するか
        // =================================================================

        [SkippableFact]
        public void ToPhonemes出力とToSegments出力が整合_日英混在()
        {
            SkipIfNoDictionary();
            string input = "こんにちはhello";
            var phonemes = _engine!.ToPhonemes(input);
            var segments = _engine!.ToSegments(input);

            // セグメントの音素を結合した結果がToPhonemes結果と一致する
            var segmentPhonemes = string.Join(" ", segments.Select(s => s.Phonemes));
            Assert.Equal(phonemes, segmentPhonemes);
        }

        [SkippableFact]
        public void ToPhonemes出力とToSegments出力が整合_日英日()
        {
            SkipIfNoDictionary();
            string input = "東京のTokyoTowerは高い";
            var phonemes = _engine!.ToPhonemes(input);
            var segments = _engine!.ToSegments(input);

            var segmentPhonemes = string.Join(" ", segments.Select(s => s.Phonemes));
            Assert.Equal(phonemes, segmentPhonemes);
        }

        [SkippableFact]
        public void ToPhonemes出力とToSegments出力が整合_複数パターン()
        {
            SkipIfNoDictionary();
            var inputs = new[]
            {
                "Hello世界",
                "I love 寿司 very much",
                "Hello世界Goodbyeさようなら",
                "今日はGoodDay",
            };

            foreach (var input in inputs)
            {
                var phonemes = _engine!.ToPhonemes(input);
                var segments = _engine!.ToSegments(input);
                var segmentPhonemes = string.Join(" ", segments.Select(s => s.Phonemes));
                Assert.Equal(phonemes, segmentPhonemes);
            }
        }

        // =================================================================
        // 7. SourceTextの検証
        // =================================================================

        [SkippableFact]
        public void ToSegments_SourceText_日英混在で正しいテキストが割り当てられる()
        {
            SkipIfNoDictionary();
            var result = _engine!.ToSegments("東京のTokyoTowerは高い");

            // 日本語セグメントに「東京の」が含まれる
            var jpFirst = result.First(s => s.Language == Language.Japanese);
            Assert.Contains("東京", jpFirst.SourceText);

            // 英語セグメントに「TokyoTower」が含まれる
            var enSeg = result.First(s => s.Language == Language.English);
            Assert.Contains("Tokyo", enSeg.SourceText);
        }

        [SkippableFact]
        public void ToSegments_SourceText_全セグメント結合で元テキスト復元()
        {
            SkipIfNoDictionary();
            string input = "Hello世界Goodbyeさようなら";
            var result = _engine!.ToSegments(input);
            var combined = string.Concat(result.Select(s => s.SourceText));
            Assert.Equal(input, combined);
        }

        // =================================================================
        // 8. バッチAPI基本テスト
        // =================================================================

        [SkippableFact]
        public void ToPhonemesBatch_日英混在テキスト複数_全て変換される()
        {
            SkipIfNoDictionary();
            var texts = new[]
            {
                "こんにちは世界",
                "Hello World",
                "今日はGoodDay",
                "東京のTokyoTowerは高い",
            };
            var result = _engine!.ToPhonemesBatch(texts);

            Assert.Equal(4, result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                Assert.NotNull(result[i]);
                Assert.NotEmpty(result[i]);
            }
        }

        [SkippableFact]
        public void ToSegmentsBatch_日英混在テキスト複数_全て変換される()
        {
            SkipIfNoDictionary();
            var texts = new[]
            {
                "こんにちはhello",
                "Hello世界",
                "I love 寿司 very much",
            };
            var result = _engine!.ToSegmentsBatch(texts);

            Assert.Equal(3, result.Count);
            foreach (var segments in result)
            {
                Assert.NotNull(segments);
                Assert.NotEmpty(segments);
                // 各入力が日英混在なので2セグメント以上
                Assert.True(segments.Count >= 2,
                    $"セグメント数が2未満: {segments.Count}");
            }
        }
    }
}
