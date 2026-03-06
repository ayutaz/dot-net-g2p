using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    public class TextSegmenterTests
    {
        // ヘルパー: セグメントのテキスト・言語を検証する
        private static void AssertSegment(
            IReadOnlyList<TextSegment> segments,
            int index,
            string expectedText,
            Language expectedLanguage)
        {
            Assert.True(index < segments.Count,
                $"セグメントインデックス {index} が範囲外（セグメント数: {segments.Count}）");
            Assert.Equal(expectedText, segments[index].Text);
            Assert.Equal(expectedLanguage, segments[index].Language);
        }

        // ===== 空入力・空白のみ（テスト1-3） =====

        /// <summary>1. null入力 → 空リスト</summary>
        [Fact]
        public void Segment_null入力_空リストを返す()
        {
            var result = TextSegmenter.Segment(null!);
            Assert.Empty(result);
        }

        /// <summary>2. 空文字列 → 空リスト</summary>
        [Fact]
        public void Segment_空文字列_空リストを返す()
        {
            var result = TextSegmenter.Segment("");
            Assert.Empty(result);
        }

        /// <summary>3. 空白のみ → 空リスト</summary>
        [Fact]
        public void Segment_空白のみ_空リストを返す()
        {
            var result = TextSegmenter.Segment("   ");
            Assert.Empty(result);
        }

        // ===== 単一言語（テスト4-5） =====

        /// <summary>4. 日本語のみ "こんにちは" → [Japanese:"こんにちは"]</summary>
        [Fact]
        public void Segment_日本語のみ_1セグメントJapanese()
        {
            var result = TextSegmenter.Segment("こんにちは");
            Assert.Single(result);
            AssertSegment(result, 0, "こんにちは", Language.Japanese);
        }

        /// <summary>5. 英語のみ "hello" → [English:"hello"]</summary>
        [Fact]
        public void Segment_英語のみ_1セグメントEnglish()
        {
            var result = TextSegmenter.Segment("hello");
            Assert.Single(result);
            AssertSegment(result, 0, "hello", Language.English);
        }

        // ===== 日英混在（テスト6-9） =====

        /// <summary>6. 日英混在 "今日はgoodday" → [Japanese:"今日は", English:"goodday"]</summary>
        [Fact]
        public void Segment_日英混在_2セグメント()
        {
            var result = TextSegmenter.Segment("今日はgoodday");
            Assert.Equal(2, result.Count);
            AssertSegment(result, 0, "今日は", Language.Japanese);
            AssertSegment(result, 1, "goodday", Language.English);
        }

        /// <summary>7. 英日混在 "hello世界" → [English:"hello", Japanese:"世界"]</summary>
        [Fact]
        public void Segment_英日混在_2セグメント()
        {
            var result = TextSegmenter.Segment("hello世界");
            Assert.Equal(2, result.Count);
            AssertSegment(result, 0, "hello", Language.English);
            AssertSegment(result, 1, "世界", Language.Japanese);
        }

        /// <summary>8. 日英日 "東京のTokyoタワー" → 3セグメント</summary>
        [Fact]
        public void Segment_日英日_3セグメント()
        {
            var result = TextSegmenter.Segment("東京のTokyoタワー");
            Assert.Equal(3, result.Count);
            AssertSegment(result, 0, "東京の", Language.Japanese);
            AssertSegment(result, 1, "Tokyo", Language.English);
            AssertSegment(result, 2, "タワー", Language.Japanese);
        }

        /// <summary>9. 英日英 "I love 寿司 very much" → セグメント確認</summary>
        [Fact]
        public void Segment_英日英_3セグメント()
        {
            // I love =EN(空白は前方EN付属), 寿司 =JA(空白は前方JA付属), very much=EN
            var result = TextSegmenter.Segment("I love 寿司 very much");
            Assert.Equal(3, result.Count);
            AssertSegment(result, 0, "I love ", Language.English);
            AssertSegment(result, 1, "寿司 ", Language.Japanese);
            AssertSegment(result, 2, "very much", Language.English);
        }

        // ===== 数字の扱い（テスト10-12） =====

        /// <summary>10. 数字が日本語に隣接 "3月" → [Japanese:"3月"]</summary>
        [Fact]
        public void Segment_数字が日本語に隣接_日本語に吸収()
        {
            var result = TextSegmenter.Segment("3月");
            Assert.Single(result);
            AssertSegment(result, 0, "3月", Language.Japanese);
        }

        /// <summary>11. 数字が英語に隣接 "test123" → [English:"test123"]</summary>
        [Fact]
        public void Segment_数字が英語に隣接_英語に吸収()
        {
            var result = TextSegmenter.Segment("test123");
            Assert.Single(result);
            AssertSegment(result, 0, "test123", Language.English);
        }

        /// <summary>12. 数字のみ "123" → デフォルト日本語</summary>
        [Fact]
        public void Segment_数字のみ_デフォルトJapanese()
        {
            var result = TextSegmenter.Segment("123");
            Assert.Single(result);
            AssertSegment(result, 0, "123", Language.Japanese);
        }

        // ===== 句読点・記号の扱い（テスト13-15） =====

        /// <summary>13. ASCII句読点が日本語後 "こんにちは!" → 句読点が前のセグメントに付属</summary>
        [Fact]
        public void Segment_ASCII句読点が日本語後_前セグメントに付属()
        {
            var result = TextSegmenter.Segment("こんにちは!");
            Assert.Single(result);
            AssertSegment(result, 0, "こんにちは!", Language.Japanese);
        }

        /// <summary>14. ASCII句読点が英語後 "hello!" → [English:"hello!"]</summary>
        [Fact]
        public void Segment_ASCII句読点が英語後_英語に付属()
        {
            var result = TextSegmenter.Segment("hello!");
            Assert.Single(result);
            AssertSegment(result, 0, "hello!", Language.English);
        }

        /// <summary>15. CJK句読点 "こんにちは。" → [Japanese:"こんにちは。"]</summary>
        [Fact]
        public void Segment_CJK句読点_日本語に直接分類()
        {
            // 。(U+3002)はCJK記号範囲なのでLanguageDetectorでJapaneseに分類される
            var result = TextSegmenter.Segment("こんにちは。");
            Assert.Single(result);
            AssertSegment(result, 0, "こんにちは。", Language.Japanese);
        }

        // ===== アポストロフィ・ハイフン（テスト16-17） =====

        /// <summary>16. アポストロフィ（英語間） "don't" → [English:"don't"]</summary>
        [Fact]
        public void Segment_アポストロフィ英語間_1セグメントEnglish()
        {
            var result = TextSegmenter.Segment("don't");
            Assert.Single(result);
            AssertSegment(result, 0, "don't", Language.English);
        }

        /// <summary>17. ハイフン（英語間） "well-known" → [English:"well-known"]</summary>
        [Fact]
        public void Segment_ハイフン英語間_1セグメントEnglish()
        {
            var result = TextSegmenter.Segment("well-known");
            Assert.Single(result);
            AssertSegment(result, 0, "well-known", Language.English);
        }

        // ===== 空白の扱い（テスト18-21） =====

        /// <summary>18. 英語間の空白 "hello world" → [English:"hello world"]</summary>
        [Fact]
        public void Segment_英語間の空白_1セグメントEnglish()
        {
            var result = TextSegmenter.Segment("hello world");
            Assert.Single(result);
            AssertSegment(result, 0, "hello world", Language.English);
        }

        /// <summary>19. 日本語間の空白 "東京 大阪" → [Japanese:"東京 大阪"]</summary>
        [Fact]
        public void Segment_日本語間の空白_1セグメントJapanese()
        {
            var result = TextSegmenter.Segment("東京 大阪");
            Assert.Single(result);
            AssertSegment(result, 0, "東京 大阪", Language.Japanese);
        }

        /// <summary>20. 言語境界の空白 "hello 世界" → 空白は前（英語側）に付属</summary>
        [Fact]
        public void Segment_言語境界の空白_前セグメントに付属()
        {
            // hello=EN, " "=WS(prev=EN,next=JA → 前方ENに付属), 世界=JA
            var result = TextSegmenter.Segment("hello 世界");
            Assert.Equal(2, result.Count);
            AssertSegment(result, 0, "hello ", Language.English);
            AssertSegment(result, 1, "世界", Language.Japanese);
        }

        /// <summary>21. 先頭が記号 "!hello" → 記号は後ろ（英語）に付属</summary>
        [Fact]
        public void Segment_先頭記号_後続言語に付属()
        {
            // !=Punct(prev=null, next=EN → EN), hello=EN
            var result = TextSegmenter.Segment("!hello");
            Assert.Single(result);
            AssertSegment(result, 0, "!hello", Language.English);
        }

        // ===== 複数言語切替（テスト22） =====

        /// <summary>22. 複数言語切替 "aあbい" → 4セグメント</summary>
        [Fact]
        public void Segment_複数言語切替_4セグメント()
        {
            var result = TextSegmenter.Segment("aあbい");
            Assert.Equal(4, result.Count);
            AssertSegment(result, 0, "a", Language.English);
            AssertSegment(result, 1, "あ", Language.Japanese);
            AssertSegment(result, 2, "b", Language.English);
            AssertSegment(result, 3, "い", Language.Japanese);
        }

        // ===== 全角・半角（テスト23-24） =====

        /// <summary>23. 全角英字 "Ｈｅｌｌｏ" → English判定</summary>
        [Fact]
        public void Segment_全角英字_English判定()
        {
            // 全角英字(U+FF01-FF5E範囲)はLanguageDetectorでEnglishに分類される
            var result = TextSegmenter.Segment("Ｈｅｌｌｏ");
            Assert.Single(result);
            AssertSegment(result, 0, "Ｈｅｌｌｏ", Language.English);
        }

        /// <summary>24. 半角カナ "ｱｲｳ" → Japanese判定</summary>
        [Fact]
        public void Segment_半角カナ_Japanese判定()
        {
            // 半角カナ(U+FF65-FF9F)はLanguageDetectorでJapaneseに分類される
            var result = TextSegmenter.Segment("ｱｲｳ");
            Assert.Single(result);
            AssertSegment(result, 0, "ｱｲｳ", Language.Japanese);
        }

        // ===== 記号のみ（テスト25） =====

        /// <summary>25. 記号のみ "!@#" → デフォルト日本語</summary>
        [Fact]
        public void Segment_記号のみ_デフォルトJapanese()
        {
            // 全てPunctuation、前後に言語なし → デフォルトJapanese
            var result = TextSegmenter.Segment("!@#");
            Assert.Single(result);
            AssertSegment(result, 0, "!@#", Language.Japanese);
        }

        // ===== 長い混在テキスト（テスト26） =====

        /// <summary>26. 長い混在テキスト "私はAliceです。Nice to meet you。"</summary>
        [Fact]
        public void Segment_長い混在テキスト_正しくセグメント分割()
        {
            // 私は=JA, Alice=EN, です。=JA(。はCJK記号→JA直接分類),
            // Nice to meet you=EN, 。=JA(CJK記号)
            var result = TextSegmenter.Segment("私はAliceです。Nice to meet you。");
            Assert.Equal(5, result.Count);
            AssertSegment(result, 0, "私は", Language.Japanese);
            AssertSegment(result, 1, "Alice", Language.English);
            AssertSegment(result, 2, "です。", Language.Japanese);
            AssertSegment(result, 3, "Nice to meet you", Language.English);
            AssertSegment(result, 4, "。", Language.Japanese);
        }

        // ===== 数字と記号の組み合わせ（テスト27） =====

        /// <summary>27. 数字と記号のみ "123-456" → ハイフンは英語間ではないため確認</summary>
        [Fact]
        public void Segment_数字と記号のみ_適切に処理()
        {
            // 数字はデフォルトJA、ハイフンは前後が英語でないのでPunct扱い→前方JAに付属
            // 結果: 全体が1つのJapaneseセグメント
            var result = TextSegmenter.Segment("123-456");
            Assert.Single(result);
            AssertSegment(result, 0, "123-456", Language.Japanese);
        }

        // ===== 改行・タブ（テスト28-29） =====

        /// <summary>28. 改行を含むテキスト "hello\nworld" → 空白扱い、同言語間</summary>
        [Fact]
        public void Segment_改行含み_同言語間で結合()
        {
            // hello=EN, \n=WS(prev=EN,next=EN → EN), world=EN → 1セグメント
            var result = TextSegmenter.Segment("hello\nworld");
            Assert.Single(result);
            AssertSegment(result, 0, "hello\nworld", Language.English);
        }

        /// <summary>29. タブ文字含み "hello\t世界" → 空白は前（英語）に付属</summary>
        [Fact]
        public void Segment_タブ含み_前セグメントに付属()
        {
            // hello=EN, \t=WS(prev=EN,next=JA → 前方ENに付属), 世界=JA
            var result = TextSegmenter.Segment("hello\t世界");
            Assert.Equal(2, result.Count);
            AssertSegment(result, 0, "hello\t", Language.English);
            AssertSegment(result, 1, "世界", Language.Japanese);
        }

        // ===== 連続空白（テスト30） =====

        /// <summary>30. 連続空白 "hello   world" → [English:"hello   world"]</summary>
        [Fact]
        public void Segment_連続空白_同言語間で結合()
        {
            // hello=EN, "   "=WS(全てprev=EN,next=EN → EN), world=EN → 1セグメント
            var result = TextSegmenter.Segment("hello   world");
            Assert.Single(result);
            AssertSegment(result, 0, "hello   world", Language.English);
        }
    }
}
