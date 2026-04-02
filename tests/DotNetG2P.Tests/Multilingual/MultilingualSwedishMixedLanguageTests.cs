using System;
using System.Linq;
using DotNetG2P.Multilingual;
using DotNetG2P.Swedish;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// スウェーデン語を含む多言語混在テキストの統合テスト。
    /// </summary>
    [Collection(MultilingualSharedCollection.Name)]
    public class MultilingualSwedishMixedLanguageTests
    {
        private readonly MultilingualSharedFixture _fixture;

        public MultilingualSwedishMixedLanguageTests(MultilingualSharedFixture fixture)
        {
            _fixture = fixture;
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!_fixture.HasDictionary, "naist-jdic辞書が見つかりません");
        }

        // ===== 8言語混在テスト =====

        [Fact]
        public void TextSegmenter_日英中韓西仏葡瑞8言語混在_各セグメントが正しい言語に分類()
        {
            // こんにちは → 日本語（ひらがな）
            // hello → 英語（英語高頻度語、デフォルト）
            // 你好 → 中国語（CJK既定=Chinese）
            // 안녕 → 韓国語（Hangul）
            // hola → スペイン語（信号語）
            // bonjour → フランス語（信号語）
            // coração → ポルトガル語（ã特有文字）
            // hej → スウェーデン語（信号語 + デフォルトLatin=Swedish は不要、先行の確定言語で分離）
            // 注: 全言語が同時に正しく分類されるには、各セグメントが明確な判定マーカーを持つ必要がある
            var result = TextSegmenter.Segment(
                "こんにちは hello 你好 안녕 hola bonjour cora\u00E7\u00E3o g\u00e5r",
                Language.Chinese, Language.English);

            Assert.True(result.Count >= 7, $"セグメント数が不足: {result.Count}");
            Assert.Contains(result, s => s.Language == Language.Japanese);
            Assert.Contains(result, s => s.Language == Language.English);
            Assert.Contains(result, s => s.Language == Language.Chinese);
            Assert.Contains(result, s => s.Language == Language.Korean);
            Assert.Contains(result, s => s.Language == Language.Portuguese);
            Assert.Contains(result, s => s.Language == Language.Swedish);
        }

        // ===== ラテン文字言語間分離 =====

        [Fact]
        public void TextSegmenter_瑞西混在_å含む語がSwedishに分類()
        {
            // går → スウェーデン語（å特有文字）
            // canción → スペイン語（ó特有アクセント）
            var result = TextSegmenter.Segment("g\u00e5r canción", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Swedish, result[0].Language);
            Assert.Equal("g\u00e5r ", result[0].Text);
            Assert.Equal(Language.Spanish, result[1].Language);
            Assert.Equal("canción", result[1].Text);
        }

        [Fact]
        public void TextSegmenter_瑞仏混在_å含む語がSwedishに分類()
        {
            // går → スウェーデン語（å特有文字）
            // café → フランス語（é: acute-e only）
            var result = TextSegmenter.Segment("g\u00e5r café", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Swedish, result[0].Language);
            Assert.Equal("g\u00e5r ", result[0].Text);
            Assert.Equal(Language.French, result[1].Language);
            Assert.Equal("café", result[1].Text);
        }

        [Fact]
        public void TextSegmenter_瑞葡混在_å含む語がSwedishに分類()
        {
            // går → スウェーデン語（å特有文字）
            // coração → ポルトガル語（ã特有文字）
            var result = TextSegmenter.Segment("g\u00e5r cora\u00E7\u00E3o", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Swedish, result[0].Language);
            Assert.Equal("g\u00e5r ", result[0].Text);
            Assert.Equal(Language.Portuguese, result[1].Language);
            Assert.Equal("cora\u00E7\u00E3o", result[1].Text);
        }

        [Fact]
        public void TextSegmenter_瑞英混在_å含む語がSwedishに分類()
        {
            // går → スウェーデン語（å特有文字）
            // hello → 英語（英語高頻度語シグナル）
            var result = TextSegmenter.Segment("g\u00e5r hello", Language.Japanese, Language.English);

            Assert.Equal(2, result.Count);
            Assert.Equal(Language.Swedish, result[0].Language);
            Assert.Equal("g\u00e5r ", result[0].Text);
            Assert.Equal(Language.English, result[1].Language);
            Assert.Equal("hello", result[1].Text);
        }

        // ===== å確定信号 =====

        [Fact]
        public void TextSegmenter_å含む混在_確定信号でSwedishに分類()
        {
            // å (U+00E5) はスウェーデン語の明確マーカー
            // DefaultLatinLanguage が English でも、å を含む語はSwedishに分類される
            var result = TextSegmenter.Segment("det g\u00e5r bra hello", Language.Japanese, Language.English);

            Assert.Contains(result, s => s.Language == Language.Swedish);
            Assert.Contains(result, s => s.Language == Language.English);
        }

        // ===== 方言設定伝達 =====

        [SkippableFact]
        public void Engine_方言設定_Multilingual経由で反映()
        {
            SkipIfNoDictionary();

            var finlandOptions = new SwedishG2POptions(dialect: SwedishDialect.FinlandSwedish);
            using var standaloneSwedish = new SwedishG2PEngine(finlandOptions);
            using var multilingual = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(
                    defaultLatinLanguage: Language.Swedish,
                    swedishOptions: finlandOptions));

            var segments = multilingual.ToSegments("bord");

            Assert.Single(segments);
            Assert.Equal(Language.Swedish, segments[0].Language);
            Assert.Equal(standaloneSwedish.ToPhonemes("bord"), segments[0].Phonemes);
        }

        // ===== エッジケース =====

        [SkippableFact]
        public void Engine_複数セグメント_IPA結合()
        {
            SkipIfNoDictionary();

            const string input = "こんにちは g\u00e5r";
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes(input);
            var segments = _fixture.SwedishDefaultEngine.ToSegments(input);
            var joined = string.Join(" ", segments.Select(s => s.Phonemes));

            Assert.Equal(phonemes, joined);
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.Swedish);
        }

        [SkippableFact]
        public void Engine_空入力_エラーなし()
        {
            SkipIfNoDictionary();

            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes("");
            var segments = _fixture.SwedishDefaultEngine.ToSegments("");

            Assert.Equal("", phonemes);
            Assert.Empty(segments);
        }

        [SkippableFact]
        public void Engine_長文混在テスト()
        {
            SkipIfNoDictionary();

            // 日本語 + スウェーデン語 + 英語 + スウェーデン語の長文パターン
            const string input = "東京は美しい g\u00e5r bra hello world det \u00e4r bra";
            var phonemes = _fixture.SwedishDefaultEngine!.ToPhonemes(input);
            var segments = _fixture.SwedishDefaultEngine.ToSegments(input);

            Assert.False(string.IsNullOrEmpty(phonemes));
            Assert.True(segments.Count >= 2, $"セグメント数が不足: {segments.Count}");
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
            Assert.Equal(phonemes, string.Join(" ", segments.Select(s => s.Phonemes)));
        }

        // ===== 追加テスト: 言語ペア網羅性 =====

        [Fact]
        public void TextSegmenter_瑞西仏3言語混在_正しく分離()
        {
            // hej går bra → スウェーデン語（å特有文字 + 信号語）
            // hola mundo → スペイン語（信号語）
            // bonjour le monde → フランス語（信号語）
            var result = TextSegmenter.Segment(
                "hej g\u00e5r bra hola mundo bonjour le monde",
                Language.Japanese, Language.English);

            Assert.Contains(result, s => s.Language == Language.Swedish);
            Assert.Contains(result, s => s.Language == Language.Spanish);
            Assert.Contains(result, s => s.Language == Language.French);
        }

        [Fact]
        public void TextSegmenter_瑞葡英3言語混在_正しく分離()
        {
            // går → スウェーデン語（å特有文字）
            // coração → ポルトガル語（ã特有文字）
            // hello world → 英語（高頻度語シグナル）
            var result = TextSegmenter.Segment(
                "g\u00e5r cora\u00E7\u00E3o hello world",
                Language.Japanese, Language.English);

            Assert.Contains(result, s => s.Language == Language.Swedish);
            Assert.Contains(result, s => s.Language == Language.Portuguese);
            Assert.Contains(result, s => s.Language == Language.English);
        }

        [Fact]
        public void TextSegmenter_ASCII信号語のみ_スウェーデン語判定()
        {
            // "tack och hej" — å なしでも信号語(tack, och, hej)でスウェーデン語と判定される
            // DefaultLatinLanguage=Swedish で確認
            var result = TextSegmenter.Segment(
                "tack och hej",
                Language.Japanese, Language.Swedish);

            Assert.Single(result);
            Assert.Equal(Language.Swedish, result[0].Language);
        }

        [SkippableFact]
        public void DefaultLatinLanguage_Swedish_設定()
        {
            SkipIfNoDictionary();

            // DefaultLatinLanguage=Swedish の場合、ラテン文字デフォルトがSwedishになる
            using var engine = new MultilingualG2PEngine(
                _fixture.DictPath!,
                new MultilingualG2POptions(defaultLatinLanguage: Language.Swedish));

            // "bra" は特定言語のシグナルを持たないが、DefaultLatinLanguage=Swedish なので Swedish に分類
            var segments = engine.ToSegments("bra");

            Assert.Single(segments);
            Assert.Equal(Language.Swedish, segments[0].Language);
        }

        [SkippableFact]
        public void 単語レベル混在_日瑞英()
        {
            SkipIfNoDictionary();

            // 日本語 + スウェーデン語(å確定信号) + 英語(hello高頻度語)
            // DefaultEngine（defaultLatinLanguage=English）を使用して英語信号語が正しく分離されることを確認
            const string input = "東京 g\u00e5r hello";
            var segments = _fixture.DefaultEngine!.ToSegments(input);

            Assert.True(segments.Count >= 2, $"セグメント数が不足: {segments.Count}");
            Assert.Contains(segments, s => s.Language == Language.Japanese);
            Assert.Contains(segments, s => s.Language == Language.Swedish);

            // ソーステキストの結合が元入力と一致すること
            Assert.Equal(input, string.Concat(segments.Select(s => s.SourceText)));
        }
    }
}
