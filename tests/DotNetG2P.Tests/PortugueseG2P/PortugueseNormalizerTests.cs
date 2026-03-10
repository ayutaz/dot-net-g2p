using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Normalization;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// PortugueseNormalizer の13段階正規化パイプラインのテスト。
    /// 1.NFKC正規化+小文字化 → 2.略語展開 → 3.ISO日付 → 4.日付 → 5.時刻 →
    /// 6.通貨 → 7.パーセント → 8.単位 → 9.数値範囲 → 10.小数 → 11.数値 →
    /// 12.記号 → 13.空白正規化
    /// </summary>
    public class PortugueseNormalizerTests
    {
        // ====== Null/空/空白 ======

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("  ", "")]
        [InlineData("\t\n", "")]
        public void Normalize_NullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input!));
        }

        // ====== NFKC正規化 + 小文字化 ======

        [Fact]
        public void Normalize_FullWidthToHalfWidth()
        {
            // 全角 "Ａ" (U+FF21) → NFKC → "A" → 小文字 "a"
            var result = PortugueseNormalizer.Normalize("\uff21");
            Assert.Equal("a", result);
        }

        [Fact]
        public void Normalize_FullWidthDigits_Normalized()
        {
            // 全角数字 "１２３" → NFKC → "123" → 数値展開
            var result = PortugueseNormalizer.Normalize("\uff11\uff12\uff13");
            Assert.Contains("cento e vinte e tr\u00eas", result);
        }

        [Fact]
        public void Normalize_PlainText_Lowercase()
        {
            Assert.Equal("ol\u00e1 mundo", PortugueseNormalizer.Normalize("Ol\u00e1 Mundo"));
        }

        // ====== 略語展開 ======

        [Theory]
        [InlineData("Sr. Silva", "senhor silva")]
        [InlineData("Dr. Paulo", "doutor paulo")]
        [InlineData("Sra. Maria", "senhora maria")]
        [InlineData("Srta. Ana", "senhorita ana")]
        [InlineData("Dra. Maria", "doutora maria")]
        [InlineData("Prof. Carlos", "professor carlos")]
        [InlineData("Profa. Lima", "professora lima")]
        [InlineData("etc.", "et c\u00e9tera")]
        [InlineData("p. ex.", "por exemplo")]
        [InlineData("Av. Brasil", "avenida brasil")]
        [InlineData("Eng. Paulo", "engenheiro paulo")]
        [InlineData("Arq. Santos", "arquiteto santos")]
        public void Normalize_Abbreviations_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 日付展開 (DD/MM/YYYY) ======

        [Theory]
        [InlineData("01/01/2024", "primeiro de janeiro de dois mil e vinte e quatro")]
        [InlineData("15/03/2023", "quinze de mar\u00e7o de dois mil e vinte e tr\u00eas")]
        [InlineData("25/12/1999", "vinte e cinco de dezembro de mil novecentos e noventa e nove")]
        public void Normalize_Dates_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== ISO日付展開 (YYYY-MM-DD) ======

        [Theory]
        [InlineData("2024-01-01", "primeiro de janeiro de dois mil e vinte e quatro")]
        [InlineData("2024-03-15", "quinze de mar\u00e7o de dois mil e vinte e quatro")]
        [InlineData("1999-12-25", "vinte e cinco de dezembro de mil novecentos e noventa e nove")]
        public void Normalize_IsoDates_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 日付バリデーション ======

        [Fact]
        public void Normalize_InvalidDate_Day32_PassesThrough()
        {
            // 日=32 は無効 → 日付として展開されない
            var result = PortugueseNormalizer.Normalize("32/01/2024");
            Assert.DoesNotContain("janeiro", result);
        }

        [Fact]
        public void Normalize_InvalidDate_Month13_PassesThrough()
        {
            // 月=13 は無効 → 日付として展開されない
            var result = PortugueseNormalizer.Normalize("01/13/2024");
            Assert.DoesNotContain("janeiro", result);
        }

        [Fact]
        public void Normalize_Feb29_LeapYear_Valid()
        {
            // 2024年はうるう年 → 2/29は有効
            var result = PortugueseNormalizer.Normalize("29/02/2024");
            Assert.Contains("fevereiro", result);
        }

        [Fact]
        public void Normalize_Feb29_NonLeapYear_Fallback()
        {
            // 2025年はうるう年ではない → 2/29は無効
            var result = PortugueseNormalizer.Normalize("29/02/2025");
            Assert.DoesNotContain("fevereiro", result);
        }

        // ====== 時刻展開 ======

        [Theory]
        [InlineData("14h30", "quatorze horas e trinta minutos")]
        [InlineData("8h", "oito horas")]
        [InlineData("1h", "um hora")]
        [InlineData("0h", "meia-noite")]
        [InlineData("12h", "meio-dia")]
        [InlineData("14:30", "quatorze horas e trinta minutos")]
        [InlineData("00:00", "meia-noite")]
        [InlineData("12:00", "meio-dia")]
        [InlineData("8h00", "oito horas")]
        [InlineData("9h15", "nove horas e quinze minutos")]
        [InlineData("1h01", "um hora e um minuto")]
        public void Normalize_Times_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 時刻バリデーション ======

        [Fact]
        public void Normalize_InvalidTime_Hour25_PassesThrough()
        {
            // 25h00 は無効 → 時刻として展開されない
            var result = PortugueseNormalizer.Normalize("25h00");
            Assert.DoesNotContain("horas", result);
        }

        [Fact]
        public void Normalize_InvalidTime_Minute61_PassesThrough()
        {
            // 12:61 は無効 → 時刻として展開されない
            var result = PortugueseNormalizer.Normalize("12:61");
            Assert.DoesNotContain("horas", result);
            Assert.DoesNotContain("meio-dia", result);
        }

        // ====== 通貨展開 ======

        [Theory]
        [InlineData("R$ 10", "dez reais")]
        [InlineData("R$ 1", "um real")]
        [InlineData("R$ 10,50", "dez reais e cinquenta centavos")]
        [InlineData("R$ 1,01", "um real e um centavo")]
        [InlineData("$5", "cinco d\u00f3lares")]
        [InlineData("$1", "um d\u00f3lar")]
        public void Normalize_Currencies_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        [Theory]
        [InlineData("10\u20ac", "dez euros")]
        [InlineData("1\u20ac", "um euro")]
        [InlineData("5,50\u20ac", "cinco euros e cinquenta c\u00eantimos")]
        public void Normalize_Currencies_Euros_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== パーセント ======

        [Theory]
        [InlineData("50%", "cinquenta por cento")]
        [InlineData("100%", "cem por cento")]
        [InlineData("3,5%", "tr\u00eas v\u00edrgula cinco por cento")]
        public void Normalize_Percentages_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 単位展開 ======

        [Theory]
        [InlineData("5km", "cinco quil\u00f4metros")]
        [InlineData("1km", "um quil\u00f4metro")]
        [InlineData("100km/h", "cem quil\u00f4metros por hora")]
        [InlineData("3kg", "tr\u00eas quilogramas")]
        [InlineData("1kg", "um quilograma")]
        [InlineData("25\u00b0C", "vinte e cinco graus celsius")]
        [InlineData("1\u00b0C", "um grau celsius")]
        [InlineData("50cm", "cinquenta cent\u00edmetros")]
        [InlineData("2mm", "dois mil\u00edmetros")]
        [InlineData("100ml", "cem mililitros")]
        [InlineData("3m", "tr\u00eas metros")]
        [InlineData("1m", "um metro")]
        public void Normalize_Units_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 数値範囲 ======

        [Theory]
        [InlineData("10-20", "dez a vinte")]
        [InlineData("1-5", "um a cinco")]
        public void Normalize_NumericRange_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 小数 ======

        [Theory]
        [InlineData("3,14", "tr\u00eas v\u00edrgula um quatro")]
        [InlineData("0,5", "zero v\u00edrgula cinco")]
        public void Normalize_Decimals_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 独立数値 ======

        [Theory]
        [InlineData("42", "quarenta e dois")]
        [InlineData("100", "cem")]
        [InlineData("1000", "mil")]
        [InlineData("123", "cento e vinte e tr\u00eas")]
        [InlineData("2025", "dois mil e vinte e cinco")]
        public void Normalize_StandaloneNumbers_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        // ====== 記号 ======

        [Theory]
        [InlineData("A & B", "a e b")]
        [InlineData("user@email", "user arroba email")]
        [InlineData("+", "mais")]
        [InlineData("=", "igual")]
        [InlineData("#", "cardinal")]
        public void Normalize_Symbols_Expanded(string input, string expected)
        {
            Assert.Equal(expected, PortugueseNormalizer.Normalize(input));
        }

        [Fact]
        public void Normalize_SymbolPlus_InArithmeticContext()
        {
            // "1 + 1" → 数値が先に展開 → "um + um" → 記号展開 → "um mais um"
            var result = PortugueseNormalizer.Normalize("1 + 1");
            Assert.Equal("um mais um", result);
        }

        // ====== 空白正規化 ======

        [Fact]
        public void Normalize_MultipleSpaces_Collapsed()
        {
            Assert.Equal("um dois", PortugueseNormalizer.Normalize("um   dois"));
        }

        [Fact]
        public void Normalize_LeadingTrailingSpaces_Trimmed()
        {
            Assert.Equal("ol\u00e1", PortugueseNormalizer.Normalize("  ol\u00e1  "));
        }

        // ====== EP方言 ======

        [Fact]
        public void Normalize_EP_UsesEPNumbers_14()
        {
            // BP: "quatorze", EP: "catorze"
            var result = PortugueseNormalizer.Normalize("14", PortugueseDialect.European);
            Assert.Equal("catorze", result);
        }

        [Fact]
        public void Normalize_BP_UsesBPNumbers_14()
        {
            var result = PortugueseNormalizer.Normalize("14", PortugueseDialect.Brazilian);
            Assert.Equal("quatorze", result);
        }

        [Fact]
        public void Normalize_EP_UsesDezasseis()
        {
            // BP: "dezesseis", EP: "dezasseis"
            var result = PortugueseNormalizer.Normalize("16", PortugueseDialect.European);
            Assert.Equal("dezasseis", result);
        }

        [Fact]
        public void Normalize_BP_UsesDezesseis()
        {
            var result = PortugueseNormalizer.Normalize("16", PortugueseDialect.Brazilian);
            Assert.Equal("dezesseis", result);
        }

        // ====== 複合テスト ======

        [Fact]
        public void Normalize_MixedContent_AllExpanded()
        {
            var result = PortugueseNormalizer.Normalize("Dr. Silva tem 3 filhos & 2 gatos");
            Assert.Equal("doutor silva tem tr\u00eas filhos e dois gatos", result);
        }

        [Fact]
        public void Normalize_DateTimeCombo_Expanded()
        {
            var result = PortugueseNormalizer.Normalize("Reuni\u00e3o em 01/06/2025 \u00e0s 14h30");
            Assert.Contains("primeiro de junho de dois mil e vinte e cinco", result);
            Assert.Contains("quatorze horas e trinta minutos", result);
        }

        [Fact]
        public void Normalize_CurrencyWithUnits_Expanded()
        {
            var result = PortugueseNormalizer.Normalize("Comprei 5kg por R$ 20");
            Assert.Contains("cinco quilogramas", result);
            Assert.Contains("vinte reais", result);
        }

        [Fact]
        public void Normalize_MixedSymbolsAndText_Expanded()
        {
            var result = PortugueseNormalizer.Normalize("A+B=C");
            Assert.Equal("a mais b igual c", result);
        }

        // ====== Tokenize ======

        [Theory]
        [InlineData("ol\u00e1 mundo", new[] { "ol\u00e1", "mundo" })]
        [InlineData("", new string[0])]
        public void Tokenize_ReturnsExpectedTokens(string input, string[] expected)
        {
            var tokens = PortugueseNormalizer.Tokenize(input);
            Assert.Equal(expected, tokens);
        }

        [Fact]
        public void Tokenize_Null_ReturnsEmpty()
        {
            Assert.Empty(PortugueseNormalizer.Tokenize(null!));
        }

        [Fact]
        public void Tokenize_Elision_PreservesApostrophe()
        {
            // ポルトガル語のエリジオン: d'agua → トークン内にアポストロフ保持
            var tokens = PortugueseNormalizer.Tokenize("d'\u00e1gua");
            Assert.Contains("d'\u00e1gua", tokens);
        }

        [Fact]
        public void Tokenize_Clitic_PreservesHyphen()
        {
            // 接語のハイフン保持: fale-me
            var tokens = PortugueseNormalizer.Tokenize("fale-me");
            Assert.Contains("fale-me", tokens);
        }

        [Fact]
        public void Tokenize_MultipleClitics_PreservesHyphens()
        {
            var tokens = PortugueseNormalizer.Tokenize("diga-lhe");
            Assert.Contains("diga-lhe", tokens);
        }

        [Fact]
        public void Tokenize_Punctuation_Removed()
        {
            var tokens = PortugueseNormalizer.Tokenize("Ol\u00e1, mundo!");
            Assert.DoesNotContain(",", tokens);
            Assert.DoesNotContain("!", tokens);
        }

        [Fact]
        public void Tokenize_NumbersExpanded()
        {
            // Tokenize は内部で Normalize を呼ぶので数値も展開される
            var tokens = PortugueseNormalizer.Tokenize("tenho 3 gatos");
            Assert.Contains("tr\u00eas", tokens);
            Assert.DoesNotContain("3", tokens);
        }

        // ====== TokenizeNormalized ======

        [Fact]
        public void TokenizeNormalized_DoesNotDoubleNormalize()
        {
            // 正規化済みテキストを再度正規化しないことを確認
            var normalized = PortugueseNormalizer.Normalize("Dr. Silva");
            var tokens = PortugueseNormalizer.TokenizeNormalized(normalized);
            Assert.Contains("doutor", tokens);
            Assert.Contains("silva", tokens);
        }

        [Fact]
        public void TokenizeNormalized_Empty_ReturnsEmpty()
        {
            Assert.Empty(PortugueseNormalizer.TokenizeNormalized(""));
        }

        [Fact]
        public void TokenizeNormalized_Null_ReturnsEmpty()
        {
            Assert.Empty(PortugueseNormalizer.TokenizeNormalized(null!));
        }

        // ====== エッジケース ======

        [Fact]
        public void Normalize_PortugueseSpecialCharacters_Preserved()
        {
            // ポルトガル語特有の文字（ç, ã, à, ê, õ）はそのまま保持
            var result = PortugueseNormalizer.Normalize("\u00e7\u00e3o \u00e0 \u00ea \u00f5es");
            Assert.Equal("\u00e7\u00e3o \u00e0 \u00ea \u00f5es", result);
        }

        [Fact]
        public void Normalize_OnlyDigits_ConvertedToWords()
        {
            Assert.Equal("cento e vinte e tr\u00eas", PortugueseNormalizer.Normalize("123"));
        }
    }
}
