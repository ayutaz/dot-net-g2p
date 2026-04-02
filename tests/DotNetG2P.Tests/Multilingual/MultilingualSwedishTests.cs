using System;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.Swedish;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual のスウェーデン語統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualSwedishTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualSwedishTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        // ===== 基本テスト =====

        [Fact]
        public void Language_Swedish_値は7()
        {
            Assert.Equal((byte)7, (byte)Language.Swedish);
        }

        [Fact]
        public void Segment_å含む_Swedishに分類()
        {
            // å (U+00E5) はスウェーデン語特有文字としてSwedishに分類される
            // "går" は å を含むためSwedishに分類。前後のASCII語はデフォルト言語に従う
            var result = TextSegmenter.Segment("det g\u00e5r bra", Language.Japanese, Language.English);

            Assert.Contains(result, s => s.Language == Language.Swedish);
            Assert.True(result.Any(s => s.Language == Language.Swedish && s.Text.Contains("g\u00e5r")),
                "å を含む 'går' がSwedishに分類されること");
        }

        [Fact]
        public void Segment_ochキーワード_Swedishに分類()
        {
            // "och" はスウェーデン語信号語。DefaultLatinLanguage=Swedish なら
            // "jag och" がSwedishとして分類される
            var result = TextSegmenter.Segment("jag och du", Language.Japanese, Language.Swedish);

            Assert.Contains(result, s => s.Language == Language.Swedish);
            Assert.True(result.Any(s => s.Language == Language.Swedish && s.Text.Contains("och")),
                "'och' を含むセグメントがSwedishに分類されること");
        }

        [Fact]
        public void Segment_tackキーワード_Swedishに分類()
        {
            // "tack" はスウェーデン語信号語
            var result = TextSegmenter.Segment("tack", Language.Japanese, Language.Swedish);

            Assert.Single(result);
            Assert.Equal(Language.Swedish, result[0].Language);
        }

        [SkippableFact]
        public void Engine_Swedish_ToIPA()
        {
            SkipIfNoDictionary();

            var segments = _fixture.SwedishDefaultEngine!.ToSegments("hej");

            Assert.Single(segments);
            Assert.Equal(Language.Swedish, segments[0].Language);
            // "hej" のIPA出力: ˈheːj
            Assert.Equal("\u02C8he\u02D0j", _fixture.SwedishEngine.ToIPA("hej"));
        }

        [SkippableFact]
        public void Engine_Swedish_ToPhonemes()
        {
            SkipIfNoDictionary();

            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("hej");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("hej"), phonemes);
        }

        [SkippableFact]
        public void Engine_Swedish_複数語()
        {
            SkipIfNoDictionary();

            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("god dag");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("god dag"), phonemes);
        }

        // ===== 混在テキスト =====

        [SkippableFact]
        public void Engine_日瑞混在()
        {
            SkipIfNoDictionary();

            var segments = _fixture.SwedishDefaultEngine!.ToSegments("こんにちは hej");

            Assert.Equal(2, segments.Count);
            Assert.Equal(Language.Japanese, segments[0].Language);
            Assert.Equal(Language.Swedish, segments[1].Language);
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes(segments[1].SourceText), segments[1].Phonemes);
        }

        [SkippableFact]
        public void Engine_英瑞混在()
        {
            SkipIfNoDictionary();

            // å を含む語でスウェーデン語を確定させる
            var segments = _fixture.DefaultEngine!.ToSegments("hello g\u00e5r");

            Assert.Equal(2, segments.Count);
            Assert.Equal(Language.English, segments[0].Language);
            Assert.Equal(Language.Swedish, segments[1].Language);
        }

        [SkippableFact]
        public void Engine_中瑞混在()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultCjkLanguage: Language.Chinese, defaultLatinLanguage: Language.Swedish));
            try
            {
                var segments = engine.ToSegments("你好 hej");

                Assert.Equal(2, segments.Count);
                Assert.Equal(Language.Chinese, segments[0].Language);
                Assert.Equal(Language.Swedish, segments[1].Language);
            }
            finally
            {
                engine.Dispose();
            }
        }

        [SkippableFact]
        public void Engine_韓瑞混在()
        {
            SkipIfNoDictionary();

            var segments = _fixture.SwedishDefaultEngine!.ToSegments("안녕 hej");

            Assert.Equal(2, segments.Count);
            Assert.Equal(Language.Korean, segments[0].Language);
            Assert.Equal(Language.Swedish, segments[1].Language);
        }

        // ===== バッチ変換 =====

        [SkippableFact]
        public void Engine_バッチ変換()
        {
            SkipIfNoDictionary();

            var texts = new[] { "hej", "tack", "god dag" };
            var results = _fixture.SwedishDefaultEngine!.ToPhonemesBatch(texts);

            Assert.Equal(3, results.Count);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.False(string.IsNullOrEmpty(results[i]),
                    $"テキスト '{texts[i]}' の変換結果が空です");
                Assert.Equal(_fixture.SwedishEngine.ToPhonemes(texts[i]), results[i]);
            }
        }

        // ===== Options =====

        [Fact]
        public void Options_SwedishOptions_保持される()
        {
            var swedishOptions = new SwedishG2POptions(enableAllophones: true, includeStress: false);
            var options = new MultilingualG2POptions(
                swedishOptions: swedishOptions,
                defaultLatinLanguage: Language.Swedish);

            Assert.NotNull(options.SwedishOptions);
            Assert.True(options.SwedishOptions!.EnableAllophones);
            Assert.False(options.SwedishOptions.IncludeStress);
            Assert.Equal(Language.Swedish, options.DefaultLatinLanguage);
        }

        [Fact]
        public void Options_SwedishOptions_null時デフォルト()
        {
            var options = new MultilingualG2POptions();

            Assert.Null(options.SwedishOptions);
        }

        // ===== Dispose =====

        [SkippableFact]
        public void Engine_Dispose後_例外()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Swedish));
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("hej"));
        }

        // ===== 音韻特徴テスト =====

        [SkippableFact]
        public void Engine_Swedish_sj音()
        {
            SkipIfNoDictionary();

            // "sjuk" → sj音 ɧ が含まれることを検証
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("sjuk");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("sjuk"), phonemes);
            // IPA出力にɧ (U+0267) が含まれる
            var ipa = _fixture.SwedishEngine.ToIPA("sjuk");
            Assert.Contains("\u0267", ipa); // ɧ
        }

        [SkippableFact]
        public void Engine_Swedish_そり舌音()
        {
            SkipIfNoDictionary();

            // "bord" → Central方言でそり舌音 ɖ (U+0256) が含まれる
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("bord");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("bord"), phonemes);
            // IPA出力にɖ (U+0256) が含まれる
            var ipa = _fixture.SwedishEngine.ToIPA("bord");
            Assert.Contains("\u0256", ipa); // ɖ
        }

        [SkippableFact]
        public void Engine_Swedish_黙字()
        {
            SkipIfNoDictionary();

            // "ljus" → lj→j の黙字規則
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("ljus");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("ljus"), phonemes);
            // IPA出力: ˈjʉːs
            var ipa = _fixture.SwedishEngine.ToIPA("ljus");
            Assert.Equal("\u02C8j\u0289\u02D0s", ipa);
        }

        [SkippableFact]
        public void Engine_Swedish_tion()
        {
            SkipIfNoDictionary();

            // "station" → -tion は /ɧuːn/ と発音される
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("station");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("station"), phonemes);
            // IPA出力にɧ (U+0267) が含まれる（-tion→ɧuːn）
            var ipa = _fixture.SwedishEngine.ToIPA("station");
            Assert.Contains("\u0267", ipa); // ɧ
        }

        [SkippableFact]
        public void Engine_Swedish_機能語()
        {
            SkipIfNoDictionary();

            // "och" は弱形でストレスマークなし
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("och");

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.Equal(_fixture.SwedishEngine.ToPhonemes("och"), phonemes);
            // IPA出力: ɔ（ストレスマーク ˈ を含まない）
            var ipa = _fixture.SwedishEngine.ToIPA("och");
            Assert.DoesNotContain("\u02C8", ipa); // ˈ なし
        }
    }
}
