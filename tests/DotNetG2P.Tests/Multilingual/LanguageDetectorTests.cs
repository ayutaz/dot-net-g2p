using DotNetG2P.Multilingual;

namespace DotNetG2P.Tests.Multilingual
{
    public class LanguageDetectorTests
    {
        // ScriptKindはinternalなので、Theoryパラメータではint経由でキャストする
        // ScriptKind: Japanese=0, English=1, Digit=2, Punctuation=3, Whitespace=4, Other=5

        // ===== Classify: 日本語文字種 =====

        [Theory]
        [InlineData('あ', 0)]  // ひらがな → Japanese
        [InlineData('ア', 0)]  // カタカナ → Japanese
        [InlineData('漢', 0)]  // CJK統合漢字 → Japanese
        [InlineData('ｱ', 0)]   // 半角カナ (U+FF71) → Japanese
        [InlineData('。', 0)]   // CJK記号・句読点 (U+3002) → Japanese
        [InlineData('ー', 0)]   // 長音記号 (U+30FC、カタカナ範囲) → Japanese
        [InlineData('・', 0)]   // 中黒 (U+30FB、カタカナ範囲) → Japanese
        public void Classify_日本語文字_Japaneseを返す(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        // ===== Classify: 英字 =====

        [Theory]
        [InlineData('A', 1)]  // ASCII英大文字 → English
        [InlineData('z', 1)]  // ASCII英小文字 → English
        [InlineData('\uFF21', 1)]  // 全角英字 'Ａ' (U+FF21) → English
        public void Classify_英字_Englishを返す(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        // ===== Classify: ASCII数字 =====

        [Fact]
        public void Classify_ASCII数字_Digitを返す()
        {
            Assert.Equal(ScriptKind.Digit, LanguageDetector.Classify('5'));
        }

        // ===== Classify: 全角数字は全角英数字範囲（U+FF01-FF5E）に含まれEnglishを返す =====

        [Fact]
        public void Classify_全角数字_Englishを返す()
        {
            Assert.Equal(ScriptKind.English, LanguageDetector.Classify('１'));
        }

        // ===== Classify: 空白文字 =====

        [Theory]
        [InlineData(' ', 4)]   // ASCII空白 → Whitespace
        [InlineData('\t', 4)]  // タブ → Whitespace
        [InlineData('\n', 4)]  // 改行 → Whitespace
        public void Classify_空白文字_Whitespaceを返す(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        // ===== Classify: ASCII句読点 =====

        [Theory]
        [InlineData('!', 3)]  // Punctuation
        [InlineData(',', 3)]  // Punctuation
        public void Classify_ASCII句読点_Punctuationを返す(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        // ===== Classify: その他 =====

        [Fact]
        public void Classify_絵文字系BMP_Otherを返す()
        {
            // '♪' (U+266A) はどの範囲にも該当しない
            Assert.Equal(ScriptKind.Other, LanguageDetector.Classify('♪'));
        }

        // ===== Classify: CJK拡張A漢字 =====

        [Fact]
        public void Classify_CJK拡張A漢字_Japaneseを返す()
        {
            // U+3400 はCJK拡張Aの先頭
            Assert.Equal(ScriptKind.Japanese, LanguageDetector.Classify('\u3400'));
        }

        // ===== Classify: Unicode境界テスト =====

        [Theory]
        [InlineData('\u3040', 0)]  // ひらがな開始 → Japanese
        [InlineData('\u309F', 0)]  // ひらがな終了 → Japanese
        [InlineData('\u30A0', 0)]  // カタカナ開始 → Japanese
        [InlineData('\u9FFF', 0)]  // CJK統合漢字終了 → Japanese
        public void Classify_Unicode境界値_正しいScriptKindを返す(char c, int expected)
        {
            Assert.Equal((ScriptKind)expected, LanguageDetector.Classify(c));
        }

        // ===== ToLanguage: 言語系ScriptKind =====

        [Fact]
        public void ToLanguage_Japanese_LanguageJapaneseを返す()
        {
            Language? result = LanguageDetector.ToLanguage(ScriptKind.Japanese);
            Assert.NotNull(result);
            Assert.Equal(Language.Japanese, result!.Value);
        }

        [Fact]
        public void ToLanguage_English_LanguageEnglishを返す()
        {
            Language? result = LanguageDetector.ToLanguage(ScriptKind.English);
            Assert.NotNull(result);
            Assert.Equal(Language.English, result!.Value);
        }

        // ===== ToLanguage: 非言語系ScriptKind =====

        [Fact]
        public void ToLanguage_Digit_nullを返す()
        {
            Assert.Null(LanguageDetector.ToLanguage(ScriptKind.Digit));
        }

        [Fact]
        public void ToLanguage_Whitespace_nullを返す()
        {
            Assert.Null(LanguageDetector.ToLanguage(ScriptKind.Whitespace));
        }

        [Fact]
        public void ToLanguage_Punctuation_nullを返す()
        {
            Assert.Null(LanguageDetector.ToLanguage(ScriptKind.Punctuation));
        }

        [Fact]
        public void ToLanguage_Other_nullを返す()
        {
            Assert.Null(LanguageDetector.ToLanguage(ScriptKind.Other));
        }
    }
}
