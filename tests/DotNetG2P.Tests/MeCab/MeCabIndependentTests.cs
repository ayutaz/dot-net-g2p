using System;
using System.IO;
using System.Linq;
using DotNetG2P.MeCab;
using Xunit;
using Xunit.Abstractions;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// MeCabTokenizerの独立仕様検証テスト。
    /// NMeCabとの比較ではなく、MeCab形態素解析の仕様に基づく期待値で検証する。
    /// </summary>
    public class MeCabIndependentTests : IDisposable
    {
        private readonly MeCabTokenizer? _tokenizer;
        private readonly ITestOutputHelper _output;
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        public MeCabIndependentTests(ITestOutputHelper output)
        {
            _output = output;
            if (DictionaryExists)
                _tokenizer = new MeCabTokenizer(DicPath!);
        }

        public void Dispose() => _tokenizer?.Dispose();

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません");
        }

        // =====================================================================
        // 1. 品詞分類テスト
        // =====================================================================

        [SkippableFact]
        public void Tokenize_こんにちは_感動詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("こんにちは");
            Assert.Single(tokens);
            Assert.Equal("こんにちは", tokens[0].Surface);
            Assert.Equal("感動詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_猫_名詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("猫");
            Assert.Single(tokens);
            Assert.Equal("猫", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("一般", tokens[0].POSGroup1);
        }

        [SkippableFact]
        public void Tokenize_食べる_動詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("食べる");
            Assert.Single(tokens);
            Assert.Equal("食べる", tokens[0].Surface);
            Assert.Equal("動詞", tokens[0].POS);
            Assert.Equal("食べる", tokens[0].OriginalForm);
        }

        [SkippableFact]
        public void Tokenize_美しい_形容詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("美しい");
            Assert.Single(tokens);
            Assert.Equal("美しい", tokens[0].Surface);
            Assert.Equal("形容詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_東京_名詞固有名詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("東京");
            Assert.Single(tokens);
            Assert.Equal("東京", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("固有名詞", tokens[0].POSGroup1);
        }

        [SkippableFact]
        public void Tokenize_の_助詞()
        {
            SkipIfNoDictionary();
            // 「猫の」として助詞を検証（単体だと文脈がない）
            var tokens = _tokenizer!.Tokenize("猫の");
            Assert.Equal(2, tokens.Count);
            Assert.Equal("の", tokens[1].Surface);
            Assert.Equal("助詞", tokens[1].POS);
        }

        [SkippableFact]
        public void Tokenize_です_助動詞()
        {
            SkipIfNoDictionary();
            // 「猫です」として助動詞を検証
            var tokens = _tokenizer!.Tokenize("猫です");
            Assert.Equal(2, tokens.Count);
            Assert.Equal("です", tokens[1].Surface);
            Assert.Equal("助動詞", tokens[1].POS);
        }

        [SkippableFact]
        public void Tokenize_走る_動詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("走る");
            Assert.Single(tokens);
            Assert.Equal("走る", tokens[0].Surface);
            Assert.Equal("動詞", tokens[0].POS);
            Assert.Equal("走る", tokens[0].OriginalForm);
        }

        [SkippableFact]
        public void Tokenize_高い_形容詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("高い");
            Assert.Single(tokens);
            Assert.Equal("高い", tokens[0].Surface);
            Assert.Equal("形容詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_ゆっくり_副詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("ゆっくり");
            Assert.Single(tokens);
            Assert.Equal("ゆっくり", tokens[0].Surface);
            Assert.Equal("副詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_句点_記号()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("。");
            Assert.Single(tokens);
            Assert.Equal("。", tokens[0].Surface);
            Assert.Equal("記号", tokens[0].POS);
        }

        // =====================================================================
        // 2. 文分割テスト
        // =====================================================================

        [SkippableFact]
        public void Tokenize_私は猫です_4トークン以上()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("私は猫です");
            // 私/は/猫/です
            Assert.True(tokens.Count >= 4, $"トークン数が期待未満: {tokens.Count}");
            Assert.Equal("私", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("は", tokens[1].Surface);
            Assert.Equal("助詞", tokens[1].POS);
            Assert.Equal("猫", tokens[2].Surface);
            Assert.Equal("名詞", tokens[2].POS);
            Assert.Equal("です", tokens[3].Surface);
            Assert.Equal("助動詞", tokens[3].POS);
        }

        [SkippableFact]
        public void Tokenize_東京タワー_2トークン()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("東京タワー");
            Assert.Equal(2, tokens.Count);
            Assert.Equal("東京", tokens[0].Surface);
            Assert.Equal("タワー", tokens[1].Surface);
        }

        // =====================================================================
        // 3. 境界値テスト
        // =====================================================================

        [SkippableFact]
        public void Tokenize_空文字列_0トークン()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("");
            Assert.Empty(tokens);
        }

        [SkippableFact]
        public void Tokenize_数字_名詞()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("123");
            Assert.NotEmpty(tokens);
            // 数字は名詞（数）として解析されることを期待
            Assert.True(tokens.Any(t => t.POS == "名詞"),
                "数字は少なくとも1つの名詞トークンを含むはず");
        }

        [SkippableFact]
        public void Tokenize_英字_トークンが返る()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("ABC");
            Assert.NotEmpty(tokens);
            // 英字はトークンとして返される（未知語を含む可能性あり）
            Assert.Equal("ABC", string.Concat(tokens.Select(t => t.Surface)));
        }

        // =====================================================================
        // 4. プロパティ検証テスト
        // =====================================================================

        [SkippableFact]
        public void Token_辞書語_Reading非null()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("東京");
            Assert.Single(tokens);
            // 辞書に登録された語はReadingが設定される
            Assert.NotNull(tokens[0].Reading);
            Assert.NotEmpty(tokens[0].Reading);
            Assert.Equal("トウキョウ", tokens[0].Reading);
        }

        [SkippableFact]
        public void Token_辞書語_Pronunciation非null()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("東京");
            Assert.Single(tokens);
            // 辞書に登録された語はPronunciationが設定される
            Assert.NotNull(tokens[0].Pronunciation);
            Assert.NotEmpty(tokens[0].Pronunciation);
        }

        [SkippableFact]
        public void Token_表層形結合_元テキストに一致()
        {
            SkipIfNoDictionary();
            var input = "東京タワーに行きたい";
            var tokens = _tokenizer!.Tokenize(input);
            Assert.NotEmpty(tokens);
            // 全トークンのSurfaceを結合すると元のテキストに一致する
            var reconstructed = string.Concat(tokens.Select(t => t.Surface));
            Assert.Equal(input, reconstructed);
            _output.WriteLine($"トークン数: {tokens.Count}");
            foreach (var t in tokens)
                _output.WriteLine($"  {t.Surface}\t{t.POS}");
        }

        [SkippableFact]
        public void Token_Features_15フィールド()
        {
            SkipIfNoDictionary();
            var tokens = _tokenizer!.Tokenize("猫");
            Assert.Single(tokens);
            Assert.Equal(15, tokens[0].Features.Count);
        }
    }
}
