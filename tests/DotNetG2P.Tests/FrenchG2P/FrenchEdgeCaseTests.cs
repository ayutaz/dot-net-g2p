using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DotNetG2P.French;
using Xunit;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class FrenchEdgeCaseTests : IDisposable
    {
        private readonly FrenchG2PEngine _engine = new FrenchG2PEngine();

        // --- 入力バリデーション ---

        [Fact]
        public void EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA(string.Empty));
            Assert.Equal(string.Empty, _engine.ToXSampa(string.Empty));
            Assert.Equal(string.Empty, _engine.ToPhonemes(string.Empty));
            Assert.Empty(_engine.ToPhonemeList(string.Empty));
        }

        [Fact]
        public void Null_ReturnsEmptyOrThrows()
        {
            // エンジンはnull入力を空文字列として扱う（IsNullOrWhiteSpaceチェック）
            Assert.Equal(string.Empty, _engine.ToIPA(null!));
            Assert.Equal(string.Empty, _engine.ToXSampa(null!));
            Assert.Equal(string.Empty, _engine.ToPhonemes(null!));
            Assert.Empty(_engine.ToPhonemeList(null!));
        }

        [Fact]
        public void WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA("   "));
            Assert.Equal(string.Empty, _engine.ToXSampa("   \t\n  "));
            Assert.Empty(_engine.ToPhonemeList("  "));
        }

        [Fact]
        public void PunctuationOnly_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA("...!!!"));
            Assert.Equal(string.Empty, _engine.ToXSampa("...!!!"));
            Assert.Empty(_engine.ToPhonemeList("???..."));
        }

        // --- バッチAPI ---

        [Fact]
        public void BatchApis_EmptyInput_ReturnEmptyCollection()
        {
            Assert.Empty(_engine.ToPhonemesBatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToIPABatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToXSampaBatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToPhonemeListBatch(Array.Empty<string>()));
        }

        [Fact]
        public void BatchApis_Null_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToXSampaBatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemeListBatch(null!));
        }

        [Fact]
        public void BatchAndSingleApis_ReturnSameResults()
        {
            var texts = new[] { "bonjour", "merci", "château", "français" };
            var batchIpa = _engine.ToIPABatch(texts);
            var batchXsampa = _engine.ToXSampaBatch(texts);
            var batchPhonemes = _engine.ToPhonemesBatch(texts);

            for (var i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToIPA(texts[i]), batchIpa[i]);
                Assert.Equal(_engine.ToXSampa(texts[i]), batchXsampa[i]);
                Assert.Equal(_engine.ToPhonemes(texts[i]), batchPhonemes[i]);
            }
        }

        // --- Unicode NFC/NFD正規化 ---

        [Fact]
        public void UnicodeNfc_And_Nfd_ProduceSameOutput()
        {
            // é = U+00E9 (NFC) vs e + ´ = U+0065 U+0301 (NFD)
            var nfc = "\u00E9t\u00E9";           // été (NFC)
            var nfd = "e\u0301te\u0301";          // été (NFD)

            Assert.Equal(_engine.ToIPA(nfc), _engine.ToIPA(nfd));
            Assert.Equal(_engine.ToXSampa(nfc), _engine.ToXSampa(nfd));
        }

        [Theory]
        [InlineData("\u00E9", "e\u0301")]     // é
        [InlineData("\u00E8", "e\u0300")]     // è
        [InlineData("\u00EA", "e\u0302")]     // ê
        [InlineData("\u00EB", "e\u0308")]     // ë
        [InlineData("\u00E0", "a\u0300")]     // à
        [InlineData("\u00E2", "a\u0302")]     // â
        [InlineData("\u00EE", "i\u0302")]     // î
        [InlineData("\u00EF", "i\u0308")]     // ï
        [InlineData("\u00F4", "o\u0302")]     // ô
        [InlineData("\u00F9", "u\u0300")]     // ù
        [InlineData("\u00FB", "u\u0302")]     // û
        [InlineData("\u00FC", "u\u0308")]     // ü
        [InlineData("\u00E7", "c\u0327")]     // ç
        public void AllAccentedChars_NfdEquivalence(string nfc, string nfd)
        {
            // 単独文字は意味のある語にならないことがあるので、テスト用に前後に文字を付加
            var wordNfc = "l" + nfc;
            var wordNfd = "l" + nfd;
            Assert.Equal(_engine.ToIPA(wordNfc), _engine.ToIPA(wordNfd));
        }

        // --- 大文字/小文字/全角数字 ---

        [Fact]
        public void MixedCase_NormalizesConsistently()
        {
            var lower = _engine.ToIPA("bonjour");
            var upper = _engine.ToIPA("BONJOUR");
            var mixed = _engine.ToIPA("Bonjour");

            Assert.Equal(lower, upper);
            Assert.Equal(lower, mixed);
        }

        [Fact]
        public void FullWidthDigits_DoNotCrash()
        {
            // 全角数字のみの入力でクラッシュしないことを確認
            // 正規化で数字が除去される場合は空文字列が返る
            var ipa = _engine.ToIPA("１２３");
            Assert.NotNull(ipa);
        }

        // --- Dispose ---

        [Fact]
        public void DisposedEngine_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToSyllables("bonjour"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(new[] { "bonjour" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "bonjour" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(new[] { "bonjour" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeListBatch(new[] { "bonjour" }));
        }

        [Fact]
        public void DoubleDispose_DoesNotThrow()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            engine.Dispose(); // 二重Disposeが例外を投げないことを確認
        }

        // --- オプション ---

        [Fact]
        public void EnableAllophones_False_ProducesOutput()
        {
            using var engine = new FrenchG2PEngine(new FrenchG2POptions(enableAllophones: false));
            var result = engine.ToIPA("bonjour");
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void EnableExceptionDictionary_False_ProducesOutput()
        {
            using var engine = new FrenchG2PEngine(new FrenchG2POptions(enableExceptionDictionary: false));
            var result = engine.ToIPA("monsieur");
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void Dialect_Conservative_ProducesOutput()
        {
            using var engine = new FrenchG2PEngine(new FrenchG2POptions(dialect: FrenchDialect.Conservative));
            var result = engine.ToIPA("bonjour");
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void Dialect_Switch_MayProduceDifferentOutput()
        {
            using var metro = new FrenchG2PEngine(new FrenchG2POptions(dialect: FrenchDialect.Metropolitan));
            using var conservative = new FrenchG2PEngine(new FrenchG2POptions(dialect: FrenchDialect.Conservative));

            // 両方とも有効な出力であることを確認（出力が同一か異なるかは実装依存）
            var metroResult = metro.ToIPA("pâte");
            var conservativeResult = conservative.ToIPA("pâte");

            Assert.False(string.IsNullOrEmpty(metroResult));
            Assert.False(string.IsNullOrEmpty(conservativeResult));
        }

        [Fact]
        public void IncludeStressFalse_AffectsXSampaOutput()
        {
            using var engine = new FrenchG2PEngine(new FrenchG2POptions(includeStress: false));
            var xsampa = engine.ToXSampa("bonjour");
            var xsampaNoStress = engine.ToXSampaWithoutStress("bonjour");

            // includeStress=false なので両方同じ結果
            Assert.Equal(xsampa, xsampaNoStress);
        }

        // --- ToSyllables ---

        [Fact]
        public void ToSyllables_Whitespace_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToSyllables("   "));
        }

        [Fact]
        public void ToSyllables_ValidWord_ReturnsNonEmpty()
        {
            var syllables = _engine.ToSyllables("bonjour");
            Assert.NotEmpty(syllables);
            Assert.All(syllables, s => Assert.NotEmpty(s));
        }

        // --- 長文入力 ---

        [Fact]
        public void LongInput_100Words_ProducesStableOutput()
        {
            var words = Enumerable.Repeat("bonjour le monde est magnifique aujourd'hui avec du soleil et des nuages blancs", 10);
            var longText = string.Join(" ", words);

            var result = _engine.ToIPA(longText);
            Assert.False(string.IsNullOrEmpty(result));
            // 再実行で同じ結果であることを確認
            Assert.Equal(result, _engine.ToIPA(longText));
        }

        // --- 大量バッチ ---

        [Fact]
        public void LargeBatch_10000Items_StableOutput()
        {
            var texts = Enumerable.Range(0, 10000).Select(_ => "bonjour").ToArray();
            var results = _engine.ToIPABatch(texts);

            Assert.Equal(10000, results.Count);
            var expected = _engine.ToIPA("bonjour");
            Assert.All(results, r => Assert.Equal(expected, r));
        }

        // --- 特殊文字 ---

        [Fact]
        public void SpecialCharacters_HandleGracefully()
        {
            // 記号のみや数字のみの入力でクラッシュしないことを確認
            Assert.NotNull(_engine.ToIPA("@#$%^&*"));
            Assert.NotNull(_engine.ToIPA("12345"));
            Assert.NotNull(_engine.ToIPA("café-crème"));
            Assert.NotNull(_engine.ToIPA("l'homme"));
        }

        [Fact]
        public void Ligatures_OeAe_HandleCorrectly()
        {
            // œ と æ が正しく処理されることを確認
            var resultOe = _engine.ToIPA("œuf");
            var resultAe = _engine.ToIPA("ex æquo");

            Assert.False(string.IsNullOrEmpty(resultOe));
            Assert.False(string.IsNullOrEmpty(resultAe));
        }

        public void Dispose() => _engine.Dispose();
    }
}
