using DotNetG2P.French.Normalization;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class FrenchNormalizerTests
    {
        // --- 基本動作 ---

        [Fact]
        public void Normalize_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, FrenchNormalizer.Normalize(null!));
        }

        [Fact]
        public void Normalize_Empty_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, FrenchNormalizer.Normalize(""));
        }

        [Fact]
        public void Normalize_PlainText_Lowercase()
        {
            Assert.Equal("bonjour le monde", FrenchNormalizer.Normalize("Bonjour le monde"));
        }

        // --- 略語展開 ---

        [Theory]
        [InlineData("M. Dupont", "monsieur dupont")]
        [InlineData("Mme Curie", "madame curie")]
        [InlineData("Dr Martin", "docteur martin")]
        [InlineData("etc.", "et cetera")]
        [InlineData("p. ex.", "par exemple")]
        public void ExpandAbbreviations_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 日付展開 ---

        [Theory]
        [InlineData("01/01/2025", "le premier janvier deux mille vingt-cinq")]
        [InlineData("15/03/2024", "le quinze mars deux mille vingt-quatre")]
        [InlineData("25/12/1999", "le vingt-cinq décembre mille neuf cent quatre-vingt-dix-neuf")]
        public void ExpandDates_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        [Fact]
        public void ExpandDates_InvalidDate_Unchanged()
        {
            // 月が13は無効
            var result = FrenchNormalizer.Normalize("32/13/2025");
            // 数字が残りExpandNumbersで変換される
            Assert.DoesNotContain("janvier", result);
        }

        // --- 時刻展開 ---

        [Theory]
        [InlineData("14h30", "quatorze heures trente")]
        [InlineData("0h", "minuit")]
        [InlineData("12h", "midi")]
        [InlineData("8h00", "huit heures")]
        [InlineData("9h15", "neuf heures quinze")]
        public void ExpandTimes_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 通貨展開 ---

        [Theory]
        [InlineData("10€", "dix euros")]
        [InlineData("1€", "un euro")]
        [InlineData("5,50€", "cinq euros cinquante centimes")]
        [InlineData("$20", "vingt dollars")]
        [InlineData("$1", "un dollar")]
        public void ExpandCurrencies_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- パーセンテージ展開 ---

        [Theory]
        [InlineData("50%", "cinquante pour cent")]
        [InlineData("100%", "cent pour cent")]
        [InlineData("3,5%", "trois virgule cinq pour cent")]
        public void ExpandPercentages_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 単位展開 ---

        [Theory]
        [InlineData("5km", "cinq kilomètres")]
        [InlineData("1km", "un kilomètre")]
        [InlineData("10kg", "dix kilogrammes")]
        [InlineData("3m", "trois mètres")]
        [InlineData("50cm", "cinquante centimètres")]
        [InlineData("2mm", "deux millimètres")]
        [InlineData("5L", "cinq litres")]
        [InlineData("25°C", "vingt-cinq degrés celsius")]
        public void ExpandUnits_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 小数展開 ---

        [Theory]
        [InlineData("3,14", "trois virgule un quatre")]
        [InlineData("0,5", "zéro virgule cinq")]
        public void ExpandDecimals_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 数字展開 ---

        [Theory]
        [InlineData("42", "quarante-deux")]
        [InlineData("1000", "mille")]
        [InlineData("2025", "deux mille vingt-cinq")]
        public void ExpandNumbers_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 記号展開 ---

        [Theory]
        [InlineData("A & B", "a et b")]
        [InlineData("@", "arobase")]
        [InlineData("§", "paragraphe")]
        [InlineData("#", "dièse")]
        [InlineData("+", "plus")]
        [InlineData("=", "égal")]
        public void ExpandSymbols_ReturnsExpanded(string input, string expected)
        {
            Assert.Equal(expected, FrenchNormalizer.Normalize(input));
        }

        // --- 空白正規化 ---

        [Fact]
        public void Normalize_MultipleSpaces_Collapsed()
        {
            Assert.Equal("un deux", FrenchNormalizer.Normalize("un   deux"));
        }

        // --- 複合テスト ---

        [Fact]
        public void Normalize_MixedContent_AllExpanded()
        {
            var result = FrenchNormalizer.Normalize("Dr Martin a 3 enfants & 2 chats");
            Assert.Equal("docteur martin a trois enfants et deux chats", result);
        }

        [Fact]
        public void Normalize_DateTimeCombo_Expanded()
        {
            var result = FrenchNormalizer.Normalize("RDV le 01/06/2025 à 14h30");
            Assert.Contains("premier juin deux mille vingt-cinq", result);
            Assert.Contains("quatorze heures trente", result);
        }

        // --- Tokenize ---

        [Fact]
        public void Tokenize_Empty_ReturnsEmpty()
        {
            Assert.Empty(FrenchNormalizer.Tokenize(""));
        }

        [Fact]
        public void Tokenize_Elision_PreservesApostrophe()
        {
            var tokens = FrenchNormalizer.Tokenize("l'homme");
            Assert.Contains("l'homme", tokens);
        }

        [Fact]
        public void Tokenize_CompoundWord_PreservesHyphen()
        {
            var tokens = FrenchNormalizer.Tokenize("peut-être");
            Assert.Contains("peut-être", tokens);
        }

        // --- n° 展開 ---

        [Fact]
        public void ExpandAbbreviations_NumeroSign_Expanded()
        {
            var result = FrenchNormalizer.Normalize("n° 5");
            Assert.Contains("numéro", result);
            Assert.Contains("cinq", result);
        }
    }
}
