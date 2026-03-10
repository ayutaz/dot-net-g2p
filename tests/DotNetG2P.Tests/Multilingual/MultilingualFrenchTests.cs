using System;
using System.IO;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.French;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// Multilingual のフランス語統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualFrenchTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualFrenchTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        // ===== 基本テスト =====

        [Fact]
        public void Language_French_値は4()
        {
            Assert.Equal((byte)4, (byte)Language.French);
        }

        [Fact]
        public void LanguageDetector_ToLanguage_LatinにFrench既定を渡すとFrench()
        {
            var result = LanguageDetector.ToLanguage(ScriptKind.Latin, Language.French);

            Assert.NotNull(result);
            Assert.Equal(Language.French, result!.Value);
        }

        [Fact]
        public void Segment_DefaultLatinFrench_ASCIIフランス語をFrenchに分類()
        {
            var result = TextSegmenter.Segment("bonjour le monde", Language.Japanese, Language.French);

            Assert.Single(result);
            Assert.Equal("bonjour le monde", result[0].Text);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_アクセント付きフランス語はFrenchに分類()
        {
            // e-acute (é) はフランス語特有文字としてFrenchに分類される
            var result = TextSegmenter.Segment("caf\u00E9", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_英語語彙はEnglishのまま()
        {
            var result = TextSegmenter.Segment("hello", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_高頻度フランス語語はFrenchに分類()
        {
            // "bonjour" と "le" はフランス語シグナル語彙
            var result = TextSegmenter.Segment("bonjour le monde", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_ドイツ語ウムラウトはFrenchに誤分類しない()
        {
            var result = TextSegmenter.Segment("\u00FCber", Language.Japanese, Language.English);

            Assert.Single(result);
            // ü はドイツ語 → English のまま（Frenchに誤分類しない）
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void Segment_フランス語セディーユ付き_Frenchに分類()
        {
            // c-cedilla (ç) はフランス語特有文字
            var result = TextSegmenter.Segment("fran\u00E7ais", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultFrench_ASCII数字はFrench()
        {
            var result = TextSegmenter.Segment("2026", Language.Japanese, Language.French);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
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
            Assert.Null(options.FrenchOptions);
        }

        [Fact]
        public void Options_French指定_保持される()
        {
            var frenchOptions = new FrenchG2POptions(enableAllophones: true, includeStress: false);
            var options = new MultilingualG2POptions(
                frenchOptions: frenchOptions,
                defaultLatinLanguage: Language.French);

            Assert.NotNull(options.FrenchOptions);
            Assert.True(options.FrenchOptions!.EnableAllophones);
            Assert.False(options.FrenchOptions.IncludeStress);
            Assert.Equal(Language.French, options.DefaultLatinLanguage);
        }

        [Fact]
        public void Options_FrenchOptions_Null時はデフォルト()
        {
            var options = new MultilingualG2POptions();

            Assert.Null(options.FrenchOptions);
        }

        [Fact]
        public void Segment_フランス語アポストロフィ_Frenchセグメント()
        {
            var result = TextSegmenter.Segment("l'homme", Language.Japanese, Language.French);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultLatinFrench_r\u00E9sum\u00E9_Frenchに分類()
        {
            var result = TextSegmenter.Segment("r\u00E9sum\u00E9", Language.Japanese, Language.French);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        // ===== エンジン統合テスト =====

        [SkippableFact]
        public void Engine_DefaultLatinFrench_フランス語のみ_Frenchセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.FrenchDefaultEngine!.ToSegments("bonjour le monde");

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal(_fixture.FrenchEngine.ToPhonemes("bonjour le monde"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultLatinFrench_日仏混在_日本語とFrenchに分割()
        {
            SkipIfNoDictionary();

            var result = _fixture.FrenchDefaultEngine!.ToSegments("東京bonjour");

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal(Language.French, result[1].Language);
            Assert.Equal(_fixture.FrenchEngine.ToPhonemes(result[1].SourceText), result[1].Phonemes);
        }

        [SkippableFact]
        public void Engine_DefaultEnglish_アクセント付きフランス語はFrenchセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.DefaultEngine!.ToSegments("caf\u00E9");

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
            Assert.Equal(_fixture.FrenchEngine.ToPhonemes("caf\u00E9"), result[0].Phonemes);
        }

        [SkippableFact]
        public void Engine_ToPhonemesとToSegmentsが整合_日仏混在()
        {
            SkipIfNoDictionary();

            const string input = "bonjour 世界";
            var engine = _fixture.FrenchDefaultEngine!;
            var phonemes = engine.ToPhonemes(input);
            var segments = engine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Contains(segments, s => s.Language == Language.French);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
        }

        [SkippableFact]
        public void Engine_フランス語アポストロフィ_lhomme_Frenchセグメント()
        {
            SkipIfNoDictionary();

            var result = _fixture.FrenchDefaultEngine!.ToSegments("l'homme");

            // アポストロフィを含むフランス語テキストがFrenchセグメントとして処理される
            Assert.Contains(result, s => s.Language == Language.French);
        }

        [SkippableFact]
        public void Engine_DefaultLatinFrench_Dispose後_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultLatinLanguage: Language.French));
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("bonjour"));
        }

        // ===== 追加テスト =====

        [Fact]
        public void Segment_DefaultEnglish_アクセント付きフランス語教育はFrenchに分類()
        {
            // "education" は英仏共有だが "éducation" はアクセント付きでフランス語
            var result = TextSegmenter.Segment("\u00E9ducation", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [Fact]
        public void Segment_DefaultEnglish_œリガチャはFrenchに分類()
        {
            // œ (U+0153) はフランス語特有のリガチャ
            var result = TextSegmenter.Segment("c\u0153ur", Language.Japanese, Language.English);

            Assert.Single(result);
            Assert.Equal(Language.French, result[0].Language);
        }

        [SkippableFact]
        public void Engine_BatchAPI_フランス語複数テキスト_全て変換可能()
        {
            SkipIfNoDictionary();

            var texts = new[] { "bonjour", "merci", "au revoir" };
            var results = _fixture.FrenchDefaultEngine!.ToPhonemesBatch(texts);

            Assert.Equal(3, results.Count);
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.False(string.IsNullOrEmpty(results[i]),
                    $"テキスト '{texts[i]}' の変換結果が空です");
                Assert.Equal(_fixture.FrenchEngine.ToPhonemes(texts[i]), results[i]);
            }
        }
    }
}
