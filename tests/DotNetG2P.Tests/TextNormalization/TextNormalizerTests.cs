using DotNetG2P.TextNormalization;

namespace DotNetG2P.Tests.TextNormalization
{
    public class TextNormalizerTests
    {
        // ===== 全角→半角ASCII変換テスト =====

        [Theory]
        [InlineData("abc", "\uFF41\uFF42\uFF43")]      // 半角英小文字 → 全角英小文字
        [InlineData("ABC", "\uFF21\uFF22\uFF23")]      // 半角英大文字 → 全角英大文字
        [InlineData("123", "\uFF11\uFF12\uFF13")]      // 半角数字 → 全角数字
        public void Normalize_HalfwidthAscii_ConvertsToFullwidth(string input, string expected)
        {
            var result = TextNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_HalfwidthSymbols_ConvertsToFullwidth()
        {
            // ! → ！ (0x21 + 0xFEE0 = 0xFF01)
            Assert.Equal("\uFF01", TextNormalizer.Normalize("!"));
            // ( → （
            Assert.Equal("\uFF08", TextNormalizer.Normalize("("));
            // ) → ）
            Assert.Equal("\uFF09", TextNormalizer.Normalize(")"));
        }

        // ===== 半角カタカナ→全角変換テスト =====

        [Theory]
        [InlineData("\uFF71", "\u30A2")]    // ｱ → ア
        [InlineData("\uFF72", "\u30A4")]    // ｲ → イ
        [InlineData("\uFF73", "\u30A6")]    // ｳ → ウ
        [InlineData("\uFF74", "\u30A8")]    // ｴ → エ
        [InlineData("\uFF75", "\u30AA")]    // ｵ → オ
        [InlineData("\uFF76", "\u30AB")]    // ｶ → カ
        [InlineData("\uFF9D", "\u30F3")]    // ﾝ → ン
        public void Normalize_HalfwidthKatakana_ConvertsToFullwidth(string input, string expected)
        {
            var result = TextNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_HalfwidthKatakanaSequence_ConvertsToFullwidth()
        {
            // ｱｲｳ → アイウ
            var result = TextNormalizer.Normalize("\uFF71\uFF72\uFF73");
            Assert.Equal("\u30A2\u30A4\u30A6", result);
        }

        // ===== 濁点結合テスト =====

        [Theory]
        [InlineData("\uFF76\uFF9E", "\u30AC")]   // ｶﾞ(半角カ+半角濁点) → ガ
        [InlineData("\uFF77\uFF9E", "\u30AE")]   // ｷﾞ → ギ
        [InlineData("\uFF7B\uFF9E", "\u30B6")]   // ｻﾞ → ザ
        public void Normalize_HalfwidthKatakanaWithDakuten_CombinesToFullwidth(string input, string expected)
        {
            var result = TextNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_FullwidthKatakanaWithCombiningDakuten_Combines()
        {
            // カ + U+3099(Combining Voiced Sound Mark) → ガ
            var result = TextNormalizer.Normalize("\u30AB\u3099");
            Assert.Equal("\u30AC", result);
        }

        [Fact]
        public void Normalize_FullwidthKatakanaWithCombiningHandakuten_Combines()
        {
            // ハ + U+309A(Combining Semi-Voiced Sound Mark) → パ
            var result = TextNormalizer.Normalize("\u30CF\u309A");
            Assert.Equal("\u30D1", result);
        }

        [Theory]
        [InlineData("\uFF8A\uFF9F", "\u30D1")]   // ﾊﾟ(半角ハ+半角半濁点) → パ
        [InlineData("\uFF8B\uFF9F", "\u30D4")]   // ﾋﾟ → ピ
        [InlineData("\uFF8C\uFF9F", "\u30D7")]   // ﾌﾟ → プ
        public void Normalize_HalfwidthKatakanaWithHandakuten_CombinesToFullwidth(string input, string expected)
        {
            var result = TextNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // ===== 特殊記号変換テスト =====

        [Fact]
        public void Normalize_Backslash_ConvertsToYenSign()
        {
            // \ → ￥ (U+FFE5)
            var result = TextNormalizer.Normalize("\\");
            Assert.Equal("\uFFE5", result);
        }

        [Fact]
        public void Normalize_Hyphen_ConvertsToMinusSign()
        {
            // - → − (U+2212)
            var result = TextNormalizer.Normalize("-");
            Assert.Equal("\u2212", result);
        }

        [Fact]
        public void Normalize_Tilde_ConvertsToWaveDash()
        {
            // ~ → 〜 (U+301C)
            var result = TextNormalizer.Normalize("~");
            Assert.Equal("\u301C", result);
        }

        [Fact]
        public void Normalize_HalfwidthSpace_ConvertsToFullwidthSpace()
        {
            // 半角スペース → 全角スペース (U+3000)
            var result = TextNormalizer.Normalize(" ");
            Assert.Equal("\u3000", result);
        }

        // ===== 空文字列・nullテスト =====

        [Fact]
        public void Normalize_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", TextNormalizer.Normalize(""));
        }

        [Fact]
        public void Normalize_Null_ReturnsEmpty()
        {
            Assert.Equal("", TextNormalizer.Normalize(null!));
        }

        // ===== 全角文字はそのまま保持 =====

        [Fact]
        public void Normalize_FullwidthKatakana_PreservesAsIs()
        {
            Assert.Equal("アイウエオ", TextNormalizer.Normalize("アイウエオ"));
        }

        [Fact]
        public void Normalize_Hiragana_PreservesAsIs()
        {
            Assert.Equal("あいうえお", TextNormalizer.Normalize("あいうえお"));
        }

        [Fact]
        public void Normalize_Kanji_PreservesAsIs()
        {
            Assert.Equal("漢字", TextNormalizer.Normalize("漢字"));
        }

        // ===== 混合テスト =====

        [Fact]
        public void Normalize_MixedInput_HandlesCorrectly()
        {
            // "ABCアイウ123" → 全角ABC + アイウ + 全角123
            var result = TextNormalizer.Normalize("ABC\u30A2\u30A4\u30A6123");
            Assert.Equal("\uFF21\uFF22\uFF23\u30A2\u30A4\u30A6\uFF11\uFF12\uFF13", result);
        }

        // ===== ひらがな濁点結合テスト =====

        [Fact]
        public void Normalize_HiraganaWithCombiningDakuten_Combines()
        {
            // か + U+3099 → が
            var result = TextNormalizer.Normalize("\u304B\u3099");
            Assert.Equal("\u304C", result);
        }

        [Fact]
        public void Normalize_HiraganaWithCombiningHandakuten_Combines()
        {
            // は + U+309A → ぱ
            var result = TextNormalizer.Normalize("\u306F\u309A");
            Assert.Equal("\u3071", result);
        }
    }
}
