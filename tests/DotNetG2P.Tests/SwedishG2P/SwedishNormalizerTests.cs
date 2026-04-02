using System;
using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Normalization;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    /// <summary>
    /// スウェーデン語テキスト正規化パイプラインのテスト。
    /// SwedishNormalizer.Normalize() および Tokenize() を検証する。
    /// </summary>
    public class SwedishNormalizerTests
    {
        // =================================================================
        // NFC正規化・小文字化テスト
        // =================================================================

        [Fact]
        public void Normalize_NFD_aa_ReturnsNFC()
        {
            // NFD分解形の å (a + U+030A) → NFC合成形の å (U+00E5) に正規化
            var result = SwedishNormalizer.Normalize("a\u030A");
            Assert.Contains("\u00E5", result); // å
        }

        [Fact]
        public void Normalize_Uppercase_LowercaseReturned()
        {
            var result = SwedishNormalizer.Normalize("HEJ");
            Assert.Equal("hej", result);
        }

        // =================================================================
        // 略語展開テスト
        // =================================================================

        [Theory]
        [InlineData("t.ex.", "till exempel")]
        [InlineData("dvs.", "det vill s\u00E4ga")]       // det vill säga
        [InlineData("bl.a.", "bland annat")]
        [InlineData("kl.", "klockan")]
        [InlineData("ca.", "cirka")]
        [InlineData("osv.", "och s\u00E5 vidare")]        // och så vidare
        [InlineData("m.m.", "med mera")]
        [InlineData("nr.", "nummer")]
        public void Normalize_Abbreviation_ExpandedCorrectly(string input, string expected)
        {
            var result = SwedishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 序数展開テスト
        // =================================================================

        [Theory]
        [InlineData("1:a", "f\u00F6rsta")]     // första
        [InlineData("2:a", "andra")]
        [InlineData("3:e", "tredje")]
        [InlineData("10:e", "tionde")]
        public void Normalize_Ordinal_ExpandedCorrectly(string input, string expected)
        {
            var result = SwedishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 日付展開テスト
        // =================================================================

        [Fact]
        public void Normalize_IsoDate_ContainsMonthName()
        {
            // "2026-04-02" → "andra april ..."
            var result = SwedishNormalizer.Normalize("2026-04-02");
            Assert.Contains("april", result);
            Assert.Contains("andra", result);
        }

        // =================================================================
        // 時刻展開テスト
        // =================================================================

        [Fact]
        public void Normalize_Time_ContainsHoursAndMinutes()
        {
            // "15:30" → "femton trettio"
            var result = SwedishNormalizer.Normalize("15:30");
            Assert.Contains("femton", result);
            Assert.Contains("trettio", result);
        }

        // =================================================================
        // 通貨展開テスト
        // =================================================================

        [Fact]
        public void Normalize_CurrencyKr_ContainsNumberAndKronor()
        {
            // "5 kr" → "fem kronor"
            var result = SwedishNormalizer.Normalize("5 kr");
            Assert.Contains("fem", result);
            Assert.Contains("kronor", result);
        }

        // =================================================================
        // パーセント展開テスト
        // =================================================================

        [Fact]
        public void Normalize_Percentage_ContainsNumberAndProcent()
        {
            // "50%" → "femtio procent"
            var result = SwedishNormalizer.Normalize("50%");
            Assert.Contains("femtio", result);
            Assert.Contains("procent", result);
        }

        // =================================================================
        // 小数展開テスト
        // =================================================================

        [Fact]
        public void Normalize_Decimal_ContainsKomma()
        {
            // "3,14" → "tre komma ett fyra"（小数部は桁ごとに読み上げ）
            var result = SwedishNormalizer.Normalize("3,14");
            Assert.Contains("tre", result);
            Assert.Contains("komma", result);
            Assert.Contains("ett fyra", result);
        }

        // =================================================================
        // 数字展開テスト
        // =================================================================

        [Fact]
        public void Normalize_Number_ExpandedToWords()
        {
            // "42" → "fyrtiotvå"
            var result = SwedishNormalizer.Normalize("42");
            Assert.Contains("fyrtiotv\u00E5", result); // fyrtiotvå
        }

        // =================================================================
        // 記号展開テスト
        // =================================================================

        [Theory]
        [InlineData("@", "snabel-a")]
        [InlineData("&", "och")]
        [InlineData("+", "plus")]
        public void Normalize_Symbol_ExpandedCorrectly(string input, string expected)
        {
            var result = SwedishNormalizer.Normalize(input);
            Assert.Equal(expected, result);
        }

        // =================================================================
        // 空白正規化テスト
        // =================================================================

        [Fact]
        public void Normalize_MultipleSpaces_CollapsedToSingle()
        {
            // 連続空白は1つに圧縮、末尾トリム
            var result = SwedishNormalizer.Normalize("hej   v\u00E4rlden  ");
            Assert.Equal("hej v\u00E4rlden", result); // "hej världen"
        }

        // =================================================================
        // Tokenize テスト
        // =================================================================

        [Fact]
        public void Tokenize_BasicSentence_ReturnsNormalizedTokens()
        {
            var result = SwedishNormalizer.Tokenize("Hej v\u00E4rlden");
            Assert.Equal(new[] { "hej", "v\u00E4rlden" }, result);
        }

        [Fact]
        public void Tokenize_Empty_ReturnsEmptyArray()
        {
            var result = SwedishNormalizer.Tokenize("");
            Assert.Empty(result);
        }

        [Fact]
        public void Tokenize_Null_ReturnsEmptyArray()
        {
            var result = SwedishNormalizer.Tokenize(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void Tokenize_WhitespaceOnly_ReturnsEmptyArray()
        {
            var result = SwedishNormalizer.Tokenize("   ");
            Assert.Empty(result);
        }

        // =================================================================
        // パイプライン順序テスト（通貨→数字の順で処理されるか）
        // =================================================================

        [Fact]
        public void Normalize_CurrencyBeforeNumbers_CorrectExpansion()
        {
            // "5 kr" は通貨として処理される（数字単独展開より先）
            var result = SwedishNormalizer.Normalize("5 kr");
            Assert.Contains("kronor", result);
            // 数字が「fem」として通貨内で処理されていること
            Assert.Contains("fem", result);
        }

        // =================================================================
        // エンジン経由の正規化無効化テスト
        // =================================================================

        [Fact]
        public void Engine_NormalizationDisabled_DoesNotExpandNumber()
        {
            // EnableTextNormalization=false の場合、数字は展開されない
            using var engine = new SwedishG2PEngine(new SwedishG2POptions(
                enableTextNormalization: false,
                includeStress: false));

            // "42" はそのまま文字として処理される（数詞展開されない）
            var result = engine.ToIPA("42");
            // 正規化無効なので「fyrtiotvå」にはならない
            Assert.DoesNotContain("f", result); // 「fyrtiotvå」の先頭fが出ない
        }

        // =================================================================
        // 複合テスト（略語+数字）
        // =================================================================

        [Fact]
        public void Normalize_AbbreviationWithNumber_BothExpanded()
        {
            // "kl. 15" → "klockan femton"
            var result = SwedishNormalizer.Normalize("kl. 15");
            Assert.Contains("klockan", result);
            Assert.Contains("femton", result);
        }

        // =================================================================
        // 等号記号テスト
        // =================================================================

        [Fact]
        public void Normalize_EqualsSign_ExpandedToLikaMed()
        {
            var result = SwedishNormalizer.Normalize("=");
            Assert.Equal("lika med", result);
        }

        // =================================================================
        // 薄いスペース・ノーブレークスペースの正規化テスト
        // =================================================================

        [Fact]
        public void Normalize_ThinSpace_NormalizedToRegularSpace()
        {
            // U+2009（薄いスペース）→ 通常スペース
            var result = SwedishNormalizer.Normalize("hej\u2009d\u00E5");
            Assert.Equal("hej d\u00E5", result); // "hej då"
        }

        [Fact]
        public void Normalize_NonBreakingSpace_NormalizedToRegularSpace()
        {
            // U+00A0（ノーブレークスペース）→ 通常スペース
            var result = SwedishNormalizer.Normalize("hej\u00A0d\u00E5");
            Assert.Equal("hej d\u00E5", result); // "hej då"
        }
    }
}
