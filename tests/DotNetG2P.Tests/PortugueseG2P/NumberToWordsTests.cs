using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Normalization;

namespace DotNetG2P.Tests.PortugueseG2P
{
    public class NumberToWordsTests
    {
        // =====================================================================
        // 基本数詞 (0-19) BP
        // =====================================================================

        [Theory]
        [InlineData(0, "zero")]
        [InlineData(1, "um")]
        [InlineData(2, "dois")]
        [InlineData(3, "tr\u00eas")]
        [InlineData(4, "quatro")]
        [InlineData(5, "cinco")]
        [InlineData(6, "seis")]
        [InlineData(7, "sete")]
        [InlineData(8, "oito")]
        [InlineData(9, "nove")]
        [InlineData(10, "dez")]
        [InlineData(11, "onze")]
        [InlineData(12, "doze")]
        [InlineData(13, "treze")]
        [InlineData(14, "quatorze")]
        [InlineData(15, "quinze")]
        [InlineData(16, "dezesseis")]
        [InlineData(17, "dezessete")]
        [InlineData(18, "dezoito")]
        [InlineData(19, "dezenove")]
        public void Convert_BasicNumbers_BP(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number, PortugueseDialect.Brazilian));
        }

        // =====================================================================
        // 方言差 (BP vs EP)
        // =====================================================================

        [Theory]
        [InlineData(14, "catorze")]
        [InlineData(16, "dezasseis")]
        [InlineData(17, "dezassete")]
        [InlineData(19, "dezanove")]
        public void Convert_DialectDifference_EP(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number, PortugueseDialect.European));
        }

        [Fact]
        public void Convert_DefaultDialect_IsBrazilian()
        {
            // デフォルト引数はBrazilian
            Assert.Equal("quatorze", NumberToWords.Convert(14));
            Assert.Equal("dezesseis", NumberToWords.Convert(16));
        }

        // =====================================================================
        // 十の位 (20-99)
        // =====================================================================

        [Theory]
        [InlineData(20, "vinte")]
        [InlineData(21, "vinte e um")]
        [InlineData(25, "vinte e cinco")]
        [InlineData(30, "trinta")]
        [InlineData(33, "trinta e tr\u00eas")]
        [InlineData(40, "quarenta")]
        [InlineData(42, "quarenta e dois")]
        [InlineData(50, "cinquenta")]
        [InlineData(55, "cinquenta e cinco")]
        [InlineData(60, "sessenta")]
        [InlineData(70, "setenta")]
        [InlineData(77, "setenta e sete")]
        [InlineData(80, "oitenta")]
        [InlineData(88, "oitenta e oito")]
        [InlineData(90, "noventa")]
        [InlineData(99, "noventa e nove")]
        public void Convert_Tens(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // =====================================================================
        // 百の位 (100-999): cem vs cento 規則
        // =====================================================================

        [Theory]
        [InlineData(100, "cem")]
        [InlineData(101, "cento e um")]
        [InlineData(110, "cento e dez")]
        [InlineData(111, "cento e onze")]
        [InlineData(150, "cento e cinquenta")]
        [InlineData(199, "cento e noventa e nove")]
        [InlineData(200, "duzentos")]
        [InlineData(201, "duzentos e um")]
        [InlineData(222, "duzentos e vinte e dois")]
        [InlineData(300, "trezentos")]
        [InlineData(400, "quatrocentos")]
        [InlineData(500, "quinhentos")]
        [InlineData(600, "seiscentos")]
        [InlineData(700, "setecentos")]
        [InlineData(800, "oitocentos")]
        [InlineData(900, "novecentos")]
        [InlineData(999, "novecentos e noventa e nove")]
        public void Convert_Hundreds(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // =====================================================================
        // 千 (1000-999999): 「e」接続規則
        // =====================================================================

        [Theory]
        [InlineData(1000, "mil")]
        [InlineData(1001, "mil e um")]
        [InlineData(1033, "mil e trinta e tr\u00eas")]
        [InlineData(1099, "mil e noventa e nove")]
        [InlineData(1100, "mil e cem")]
        [InlineData(1200, "mil e duzentos")]
        [InlineData(1101, "mil cento e um")]
        [InlineData(1122, "mil cento e vinte e dois")]
        [InlineData(2000, "dois mil")]
        [InlineData(2001, "dois mil e um")]
        [InlineData(2100, "dois mil e cem")]
        [InlineData(2122, "dois mil cento e vinte e dois")]
        [InlineData(5000, "cinco mil")]
        [InlineData(10000, "dez mil")]
        [InlineData(100000, "cem mil")]
        [InlineData(100001, "cem mil e um")]
        [InlineData(100100, "cem mil e cem")]
        [InlineData(100101, "cem mil cento e um")]
        [InlineData(999999, "novecentos e noventa e nove mil novecentos e noventa e nove")]
        public void Convert_Thousands(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // =====================================================================
        // 百万以上
        // =====================================================================

        [Theory]
        [InlineData(1_000_000, "um milh\u00e3o")]
        [InlineData(1_000_001, "um milh\u00e3o e um")]
        [InlineData(1_000_100, "um milh\u00e3o e cem")]
        [InlineData(1_001_000, "um milh\u00e3o mil")]
        [InlineData(1_001_001, "um milh\u00e3o mil e um")]
        [InlineData(2_000_000, "dois milh\u00f5es")]
        [InlineData(5_000_000, "cinco milh\u00f5es")]
        [InlineData(10_000_000, "dez milh\u00f5es")]
        public void Convert_Millions(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // =====================================================================
        // 十億 (bilhao BP vs mil milhoes EP)
        // =====================================================================

        [Theory]
        [InlineData(1_000_000_000, "um bilh\u00e3o")]
        [InlineData(2_000_000_000, "dois bilh\u00f5es")]
        [InlineData(3_000_000_001, "tr\u00eas bilh\u00f5es e um")]
        public void Convert_Billions_BP(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number, PortugueseDialect.Brazilian));
        }

        [Theory]
        [InlineData(1_000_000_000, "mil milh\u00f5es")]
        [InlineData(2_000_000_000, "dois mil milh\u00f5es")]
        [InlineData(3_000_000_001, "tr\u00eas mil milh\u00f5es e um")]
        public void Convert_Billions_EP(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number, PortugueseDialect.European));
        }

        // =====================================================================
        // 負の数
        // =====================================================================

        [Theory]
        [InlineData(-1, "menos um")]
        [InlineData(-42, "menos quarenta e dois")]
        [InlineData(-1000, "menos mil")]
        public void Convert_NegativeNumbers(long number, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(number));
        }

        // =====================================================================
        // 文字列オーバーロード
        // =====================================================================

        [Theory]
        [InlineData("0", "zero")]
        [InlineData("42", "quarenta e dois")]
        [InlineData("1000", "mil")]
        [InlineData("abc", "abc")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Convert_StringOverload(string? input, string expected)
        {
            Assert.Equal(expected, NumberToWords.Convert(input!, PortugueseDialect.Brazilian));
        }

        // =====================================================================
        // 序数詞 (ConvertOrdinal)
        // =====================================================================

        [Theory]
        [InlineData(1, "primeiro")]
        [InlineData(2, "segundo")]
        [InlineData(3, "terceiro")]
        [InlineData(4, "quarto")]
        [InlineData(5, "quinto")]
        [InlineData(6, "sexto")]
        [InlineData(7, "s\u00e9timo")]
        [InlineData(8, "oitavo")]
        [InlineData(9, "nono")]
        [InlineData(10, "d\u00e9cimo")]
        [InlineData(15, "d\u00e9cimo quinto")]
        [InlineData(20, "vig\u00e9simo")]
        [InlineData(21, "vig\u00e9simo primeiro")]
        [InlineData(25, "vig\u00e9simo quinto")]
        [InlineData(30, "trig\u00e9simo")]
        [InlineData(31, "trig\u00e9simo primeiro")]
        public void ConvertOrdinal_DayNumbers(int number, string expected)
        {
            Assert.Equal(expected, NumberToWords.ConvertOrdinal(number));
        }

        [Fact]
        public void ConvertOrdinal_OutOfRange_FallbackToCardinal()
        {
            // 32以上は基数詞にフォールバック
            Assert.Equal("trinta e dois", NumberToWords.ConvertOrdinal(32));
            Assert.Equal("cem", NumberToWords.ConvertOrdinal(100));
        }

        [Fact]
        public void ConvertOrdinal_Zero_FallbackToCardinal()
        {
            Assert.Equal("zero", NumberToWords.ConvertOrdinal(0));
        }

        // =====================================================================
        // ConvertDigits
        // =====================================================================

        [Theory]
        [InlineData("123", "um dois tr\u00eas")]
        [InlineData("0", "zero")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ConvertDigits_BP(string? input, string expected)
        {
            Assert.Equal(expected, NumberToWords.ConvertDigits(input!, PortugueseDialect.Brazilian));
        }

        [Fact]
        public void ConvertDigits_EP_DialectDifference()
        {
            // 数字1桁では方言差は出ないが、メソッドが正しくunits配列を使うことを検証
            Assert.Equal("zero um dois", NumberToWords.ConvertDigits("012", PortugueseDialect.European));
        }

        // =====================================================================
        // 「e」接続規則の境界ケース
        // =====================================================================

        [Fact]
        public void Convert_EConnector_ThousandPlusSmall()
        {
            // 千の位+下位1-99 → 「e」あり
            Assert.Equal("mil e um", NumberToWords.Convert(1001));
            Assert.Equal("mil e noventa e nove", NumberToWords.Convert(1099));
        }

        [Fact]
        public void Convert_EConnector_ThousandPlusRoundHundred()
        {
            // 千の位+端数百 → 「e」あり
            Assert.Equal("mil e cem", NumberToWords.Convert(1100));
            Assert.Equal("mil e duzentos", NumberToWords.Convert(1200));
            Assert.Equal("dois mil e quinhentos", NumberToWords.Convert(2500));
        }

        [Fact]
        public void Convert_EConnector_ThousandPlusNonRound()
        {
            // 千の位+101以上で端数百でない → 「e」なし（スペース区切り）
            Assert.Equal("mil cento e um", NumberToWords.Convert(1101));
            Assert.Equal("dois mil cento e vinte e dois", NumberToWords.Convert(2122));
            Assert.Equal("cinco mil trezentos e quarenta e cinco", NumberToWords.Convert(5345));
        }

        [Fact]
        public void Convert_LargeNumber()
        {
            // 複合的な大きな数値
            Assert.Equal(
                "um milh\u00e3o duzentos e trinta e quatro mil quinhentos e sessenta e sete",
                NumberToWords.Convert(1_234_567));
        }

        [Fact]
        public void Convert_MillionPlusSmall()
        {
            // 百万+下位1-99 → 「e」あり
            Assert.Equal("um milh\u00e3o e cinquenta", NumberToWords.Convert(1_000_050));
        }

        [Fact]
        public void Convert_MillionPlusRoundHundred()
        {
            // 百万+端数百 → 「e」あり
            Assert.Equal("um milh\u00e3o e trezentos", NumberToWords.Convert(1_000_300));
        }

        [Fact]
        public void Convert_MillionPlusThousand()
        {
            // 百万+千（端数百でない、かつ99超） → 「e」なし
            Assert.Equal("um milh\u00e3o mil", NumberToWords.Convert(1_001_000));
        }
    }
}
