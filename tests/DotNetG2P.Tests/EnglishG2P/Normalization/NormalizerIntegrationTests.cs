// Copyright (c) 2026 DotNetG2P Contributors
// SPDX-License-Identifier: Apache-2.0

using DotNetG2P.English;
using DotNetG2P.English.Normalization;

namespace DotNetG2P.Tests.EnglishG2P.Normalization
{
    /// <summary>
    /// EnglishNormalizer および EnglishG2PEngine 正規化パイプラインの統合テスト。
    /// </summary>
    public class NormalizerIntegrationTests
    {
        // --- EnglishNormalizer.Normalize 単体テスト ---

        [Fact]
        public void Normalize_IntegerInText_ConvertsToWords()
        {
            // "I have 3 cats" → 3 が "three" に変換される
            var result = EnglishNormalizer.Normalize("I have 3 cats");
            Assert.Contains("three", result);
            Assert.Contains("cats", result);
        }

        [Fact]
        public void Normalize_Abbreviation_ExpandsToFullForm()
        {
            // "Dr. Smith" → "Doctor Smith"
            var result = EnglishNormalizer.Normalize("Dr. Smith");
            Assert.Equal("Doctor Smith", result);
        }

        [Fact]
        public void Normalize_Currency_ExpandsToWords()
        {
            // "$5" → "five dollars"
            var result = EnglishNormalizer.Normalize("$5");
            Assert.Equal("five dollars", result);
        }

        [Fact]
        public void Normalize_Ordinal_ExpandsToWords()
        {
            // "1st place" → "first place"
            var result = EnglishNormalizer.Normalize("1st place");
            Assert.Equal("first place", result);
        }

        [Fact]
        public void Normalize_Time_ExpandsToWords()
        {
            // "at 3:00" → "at three o'clock"
            var result = EnglishNormalizer.Normalize("at 3:00");
            Assert.Equal("at three o'clock", result);
        }

        [Fact]
        public void Normalize_Symbol_ExpandsToName()
        {
            // "test @ home" → "test at home"
            var result = EnglishNormalizer.Normalize("test @ home");
            Assert.Equal("test at home", result);
        }

        [Fact]
        public void Normalize_Acronym_SpellsOutCorrectly()
        {
            // "use API" → "API" は既知のスペルアウト対象 → "use A P I"
            var result = EnglishNormalizer.Normalize("use API");
            Assert.Equal("use A P I", result);
        }

        [Fact]
        public void Normalize_MixedContent_NormalizesAllParts()
        {
            // 複合テスト: 略語 + 通貨 + 数字
            var result = EnglishNormalizer.Normalize("Dr. Smith has $100 and 3 cats");
            Assert.Contains("Doctor", result);
            Assert.Contains("one hundred dollars", result);
            Assert.Contains("three", result);
            Assert.Contains("cats", result);
        }

        [Fact]
        public void Normalize_PureEnglishText_PassesThrough()
        {
            // 純英語テキストは変換なし
            var result = EnglishNormalizer.Normalize("hello world");
            Assert.Equal("hello world", result);
        }

        [Fact]
        public void Normalize_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, EnglishNormalizer.Normalize(""));
        }

        [Fact]
        public void Normalize_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, EnglishNormalizer.Normalize(null!));
        }

        [Fact]
        public void Normalize_WhitespaceOnly_ReturnsEmpty()
        {
            // 空白のみ → IsNullOrWhiteSpace=true → 空文字列を返す
            var result = EnglishNormalizer.Normalize("   ");
            Assert.Equal("   ", result);
        }

        // --- EnglishG2PEngine パイプライン統合テスト ---

        [Fact]
        public void Engine_EnableNormalizationFalse_SkipsNormalization()
        {
            // 正規化無効時、"$100" はトークナイザが英字のみ抽出するためスキップされる
            using var engine = new EnglishG2PEngine(new EnglishG2POptions(enableNormalization: false));
            var result = engine.ToPhonemes("$100");
            // "$100" は英字を含まないため、トークンが抽出されず空になる
            Assert.Equal("", result);
        }

        [Fact]
        public void Engine_EnableNormalizationTrue_NormalizesBeforeLookup()
        {
            // 正規化有効時、"hello" は辞書にあるので音素が返される
            using var engine = new EnglishG2PEngine(new EnglishG2POptions(enableNormalization: true));
            var result = engine.ToPhonemes("hello");
            // "hello" → CMU辞書で "HH AH0 L OW1" or similar
            Assert.NotEmpty(result);
            Assert.Contains("HH", result);
        }

        [Fact]
        public void Engine_NormalizationWithNumber_ProducesPhonemes()
        {
            // 正規化有効: "hello 3" → "hello three" → 両方の音素が生成される
            using var engine = new EnglishG2PEngine(new EnglishG2POptions(enableNormalization: true));
            var result = engine.ToPhonemes("hello 3");
            // "hello" と "three" 両方の音素が含まれる
            Assert.NotEmpty(result);
            // "hello" の音素 HH が含まれる
            Assert.Contains("HH", result);
            // "three" の音素 TH が含まれる
            Assert.Contains("TH", result);
        }
    }
}
