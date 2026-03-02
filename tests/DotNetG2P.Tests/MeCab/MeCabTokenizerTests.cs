using System;
using System.IO;
using System.Linq;
using DotNetG2P;
using DotNetG2P.MeCab;
using Xunit;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// MeCabTokenizerの基本動作テスト。
    /// 辞書依存テストは環境変数 NAIST_JDIC_PATH が必要。
    /// </summary>
    public class MeCabTokenizerTests : IDisposable
    {
        private static string? DicPath => Environment.GetEnvironmentVariable("NAIST_JDIC_PATH");
        private static bool DictionaryExists => !string.IsNullOrEmpty(DicPath) && Directory.Exists(DicPath);

        private readonly MeCabTokenizer? _tokenizer;

        public MeCabTokenizerTests()
        {
            if (DictionaryExists)
            {
                _tokenizer = new MeCabTokenizer(DicPath!);
            }
        }

        public void Dispose()
        {
            _tokenizer?.Dispose();
        }

        private void SkipIfNoDictionary()
        {
            Skip.If(!DictionaryExists, "naist-jdic辞書が見つかりません（環境変数 NAIST_JDIC_PATH を設定してください）");
        }

        // =====================================================================
        // 1. 基本テスト
        // =====================================================================

        [SkippableFact]
        public void Tokenize_こんにちは_感動詞1トークン()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("こんにちは");

            Assert.Single(tokens);
            Assert.Equal("こんにちは", tokens[0].Surface);
            Assert.Equal("感動詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_東京タワー_2トークン()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("東京タワー");

            Assert.Equal(2, tokens.Count);
            Assert.Equal("東京", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("タワー", tokens[1].Surface);
            Assert.Equal("名詞", tokens[1].POS);
        }

        [SkippableFact]
        public void Tokenize_今日は天気です_4トークン()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("今日は天気です");

            // 今日/は/天気/です
            Assert.Equal(4, tokens.Count);
            Assert.Equal("今日", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("は", tokens[1].Surface);
            Assert.Equal("助詞", tokens[1].POS);
            Assert.Equal("天気", tokens[2].Surface);
            Assert.Equal("名詞", tokens[2].POS);
            Assert.Equal("です", tokens[3].Surface);
            Assert.Equal("助動詞", tokens[3].POS);
        }

        [SkippableFact]
        public void Tokenize_空文字列_空リスト()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("");

            Assert.Empty(tokens);
        }

        [SkippableFact]
        public void Tokenize_null_ArgumentNullException()
        {
            SkipIfNoDictionary();

            Assert.Throws<ArgumentNullException>(() => _tokenizer!.Tokenize(null!));
        }

        // =====================================================================
        // 2. ITokenインターフェーステスト
        // =====================================================================

        [SkippableFact]
        public void Token_Features_15フィールド()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("東京");

            Assert.NotEmpty(tokens);
            foreach (var t in tokens)
            {
                Assert.Equal(15, t.Features.Count);
            }
        }

        [SkippableFact]
        public void Token_東京_名詞固有名詞()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("東京");

            Assert.Single(tokens);
            Assert.Equal("名詞", tokens[0].POS);
            Assert.Equal("固有名詞", tokens[0].POSGroup1);
        }

        [SkippableFact]
        public void Token_食べる_動詞プロパティ検証()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("食べる");

            Assert.NotEmpty(tokens);
            var t = tokens[0];
            Assert.Equal("食べる", t.Surface);
            Assert.Equal("動詞", t.POS);
            Assert.Equal("自立", t.POSGroup1);
            Assert.Equal("食べる", t.OriginalForm);
            Assert.Equal("タベル", t.Reading);
            Assert.NotNull(t.ConjugationType);
            Assert.NotEmpty(t.ConjugationType);
            Assert.NotNull(t.ConjugationForm);
            Assert.NotEmpty(t.ConjugationForm);
            Assert.NotNull(t.Pronunciation);
            Assert.NotNull(t.AccentInfo);
            Assert.NotNull(t.ChainRule);
        }

        // =====================================================================
        // 3. Dispose テスト
        // =====================================================================

        [SkippableFact]
        public void Dispose後_Tokenize_ObjectDisposedException()
        {
            SkipIfNoDictionary();

            var tokenizer = new MeCabTokenizer(DicPath!);
            tokenizer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => tokenizer.Tokenize("テスト"));
        }

        [SkippableFact]
        public void Dispose_二重呼び出し_例外なし()
        {
            SkipIfNoDictionary();

            var tokenizer = new MeCabTokenizer(DicPath!);
            tokenizer.Dispose();
            tokenizer.Dispose(); // 二重Dispose
        }

        // =====================================================================
        // 4. 各種入力パターン
        // =====================================================================

        [SkippableTheory]
        [InlineData("。、！？")]
        [InlineData("...")]
        [InlineData("〜")]
        [InlineData("・")]
        [InlineData("―")]
        public void Tokenize_記号のみ_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize(input);
            Assert.NotNull(tokens);
        }

        [SkippableTheory]
        [InlineData("ABC")]
        [InlineData("hello")]
        [InlineData("x86")]
        [InlineData("MP3")]
        public void Tokenize_英字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize(input);
            Assert.NotNull(tokens);
        }

        [SkippableTheory]
        [InlineData("123")]
        [InlineData("0")]
        [InlineData("999999")]
        public void Tokenize_数字_クラッシュしない(string input)
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize(input);
            Assert.NotNull(tokens);
        }

        [SkippableFact]
        public void Tokenize_ひらがな_トークンが返る()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("あいうえお");
            Assert.NotEmpty(tokens);
            // 表層形を連結すると元の文字列に一致する
            Assert.Equal("あいうえお", string.Concat(tokens.Select(t => t.Surface)));
        }

        [SkippableFact]
        public void Tokenize_カタカナ_名詞()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("カタカナ");
            Assert.NotEmpty(tokens);
            Assert.Equal("カタカナ", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_漢字_名詞()
        {
            SkipIfNoDictionary();

            var tokens = _tokenizer!.Tokenize("漢字");
            Assert.Single(tokens);
            Assert.Equal("漢字", tokens[0].Surface);
            Assert.Equal("名詞", tokens[0].POS);
        }

        [SkippableFact]
        public void Tokenize_混在テキスト_表層形結合が元テキストに一致()
        {
            SkipIfNoDictionary();

            var input = "今日はDocker入門";
            var tokens = _tokenizer!.Tokenize(input);
            Assert.NotEmpty(tokens);
            // 表層形を連結すると元の文字列に一致する
            Assert.Equal(input, string.Concat(tokens.Select(t => t.Surface)));
        }

        [SkippableFact]
        public void Tokenize_長い入力_クラッシュしない()
        {
            SkipIfNoDictionary();

            var longInput = new string('あ', 500);
            var tokens = _tokenizer!.Tokenize(longInput);
            Assert.NotNull(tokens);
            Assert.NotEmpty(tokens);
        }
    }
}
