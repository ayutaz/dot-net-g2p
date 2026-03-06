using System.Linq;
using DotNetG2P.Multilingual;
using Xunit;

namespace DotNetG2P.Tests.Multilingual
{
    /// <summary>
    /// TextSegmenter.Segment の言語検出一貫性テスト。
    /// 辞書不要のため、すべてFact/Theoryで実行可能。
    /// </summary>
    public class LanguageConsistencyTests
    {
        // ===== 基本一貫性 =====

        [Fact]
        public void 同じ入力を複数回_同じ結果()
        {
            var text = "こんにちは Hello 世界 World";
            var first = TextSegmenter.Segment(text);

            for (int i = 0; i < 10; i++)
            {
                var result = TextSegmenter.Segment(text);
                Assert.Equal(first.Count, result.Count);
                for (int j = 0; j < first.Count; j++)
                {
                    Assert.Equal(first[j].Text, result[j].Text);
                    Assert.Equal(first[j].Language, result[j].Language);
                }
            }
        }

        [Fact]
        public void 日本語文字列_全セグメントがJapanese()
        {
            var result = TextSegmenter.Segment("東京タワーは美しいです。");
            Assert.All(result, seg => Assert.Equal(Language.Japanese, seg.Language));
        }

        [Fact]
        public void 英語文字列_全セグメントがEnglish()
        {
            var result = TextSegmenter.Segment("The quick brown fox jumps over the lazy dog.");
            Assert.All(result, seg => Assert.Equal(Language.English, seg.Language));
        }

        // ===== 空白の扱い =====

        [Fact]
        public void 空白の分割一貫性_英語内空白で分割されない()
        {
            var result = TextSegmenter.Segment("hello world");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        // ===== 数字の吸収 =====

        [Fact]
        public void 数字は隣接言語に吸収_英語隣接()
        {
            // "test123" → 英語に吸収
            var result = TextSegmenter.Segment("test123");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("test123", result[0].Text);
        }

        [Fact]
        public void 数字は隣接言語に吸収_日本語隣接()
        {
            // "テスト123" → 日本語に吸収
            var result = TextSegmenter.Segment("テスト123");
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Equal("テスト123", result[0].Text);
        }

        // ===== CJK句読点 =====

        [Fact]
        public void CJK句読点はJapanese_句点がJapaneseセグメントに含まれる()
        {
            // "テスト。" の "。" はCJK記号でJapaneseに分類される
            var result = TextSegmenter.Segment("テスト。");
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
            Assert.Contains("。", result[0].Text);
        }

        // ===== ASCII句読点 =====

        [Fact]
        public void ASCII句読点は前のセグメントに付属()
        {
            // "Hello, こんにちは" → "Hello, " (EN) + "こんにちは" (JA)
            var result = TextSegmenter.Segment("Hello, こんにちは");
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Contains(",", result[0].Text);
            Assert.Equal(Language.Japanese, result[1].Language);
        }

        // ===== 全角・半角 =====

        [Fact]
        public void 全角英字はEnglish()
        {
            // 全角英字 "Ａ"（U+FF21）はEnglishと判定される
            var result = TextSegmenter.Segment("Ａ");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
        }

        [Fact]
        public void 半角カナはJapanese()
        {
            // 半角カナ "ｱ"（U+FF71）はJapaneseと判定される
            var result = TextSegmenter.Segment("ｱ");
            Assert.Single(result);
            Assert.Equal(Language.Japanese, result[0].Language);
        }

        // ===== アポストロフィ・ハイフン =====

        [Fact]
        public void アポストロフィ英語内ルール_1セグメント()
        {
            // "don't" は英語文字間のアポストロフィなので1つのEnglishセグメント
            var result = TextSegmenter.Segment("don't");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("don't", result[0].Text);
        }

        [Fact]
        public void ハイフン英語内ルール_1セグメント()
        {
            // "well-known" は英語文字間のハイフンなので1つのEnglishセグメント
            var result = TextSegmenter.Segment("well-known");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("well-known", result[0].Text);
        }

        // ===== 言語境界の空白 =====

        [Fact]
        public void 言語境界の空白_前のセグメントに付属()
        {
            // "Hello 世界" → "Hello " (EN) + "世界" (JA)
            var result = TextSegmenter.Segment("Hello 世界");
            Assert.Equal(2, result.Count);
            Assert.Equal(Language.English, result[0].Language);
            Assert.True(result[0].Text.EndsWith(" "),
                $"英語セグメントの末尾に空白がない: '{result[0].Text}'");
            Assert.Equal(Language.Japanese, result[1].Language);
            Assert.Equal("世界", result[1].Text);
        }

        // ===== 先頭・末尾の記号 =====

        [Fact]
        public void 先頭の記号_後方のセグメントに付属()
        {
            // "!Hello" → "!" は後方のEnglishに付属
            var result = TextSegmenter.Segment("!Hello");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("!Hello", result[0].Text);
        }

        [Fact]
        public void 末尾の記号_前のセグメントに付属()
        {
            // "Hello!" → "!" は前方のEnglishに付属
            var result = TextSegmenter.Segment("Hello!");
            Assert.Single(result);
            Assert.Equal(Language.English, result[0].Language);
            Assert.Equal("Hello!", result[0].Text);
        }

        // ===== セグメント結合一貫性 =====

        [Fact]
        public void セグメント結合_全セグメントのTextを結合すると元テキストと一致()
        {
            var text = "今日はgood dayですね。Let's go!";
            var result = TextSegmenter.Segment(text);
            var reconstructed = string.Concat(result.Select(s => s.Text));
            Assert.Equal(text, reconstructed);
        }

        [Theory]
        [InlineData("こんにちは")]
        [InlineData("Hello World")]
        [InlineData("日本語Englishまた日本語")]
        [InlineData("test123テスト")]
        [InlineData("Hello, world! こんにちは。")]
        [InlineData("don't stop believingそうです")]
        [InlineData("  先頭空白")]
        [InlineData("末尾空白  ")]
        [InlineData("100人のchildren")]
        public void 複数入力で結合一致テスト(string text)
        {
            var result = TextSegmenter.Segment(text);
            var reconstructed = string.Concat(result.Select(s => s.Text));
            Assert.Equal(text, reconstructed);
        }

        // ===== セグメント言語ラベルの妥当性 =====

        [Fact]
        public void セグメント言語ラベルの妥当性_各セグメントに対応する文字種確認()
        {
            // 日英混在テキストで各セグメントが正しい言語タグを持つことを確認
            var result = TextSegmenter.Segment("東京Tokyoは日本のcapital");
            foreach (var seg in result)
            {
                // 各セグメント内の言語確定文字（English/Japanese）がセグメントの言語と一致
                foreach (var c in seg.Text)
                {
                    var kind = LanguageDetector.Classify(c);
                    var lang = LanguageDetector.ToLanguage(kind);
                    // 数字・句読点・空白・Otherは言語なし（null）なのでスキップ
                    if (lang == null) continue;
                    Assert.Equal(seg.Language, lang.Value);
                }
            }
        }

        // ===== 連続する同一言語はマージ =====

        [Fact]
        public void 連続する同一言語はマージされる_隣接セグメントは異なる言語()
        {
            // 複数パターンで検証
            var inputs = new[]
            {
                "こんにちは Hello 世界 World",
                "Test テスト Test",
                "あいうえおABCかきくけこ",
                "Hello, world! こんにちは。Goodbye!",
            };

            foreach (var text in inputs)
            {
                var result = TextSegmenter.Segment(text);
                // 隣接する2つのセグメントが同一言語でないことを確認
                for (int i = 0; i < result.Count - 1; i++)
                {
                    Assert.NotEqual(result[i].Language, result[i + 1].Language);
                }
            }
        }
    }
}
