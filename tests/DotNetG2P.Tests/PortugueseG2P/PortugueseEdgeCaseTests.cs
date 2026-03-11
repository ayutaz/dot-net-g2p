using System;
using DotNetG2P.Portuguese;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// PortugueseG2PEngine のエッジケーステスト。
    /// </summary>
    public class PortugueseEdgeCaseTests : IDisposable
    {
        private readonly PortugueseG2PEngine _engine = new PortugueseG2PEngine();

        // ========== 空入力・特殊入力 ==========

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void EmptyOrWhitespace_ReturnsEmpty(string input)
        {
            Assert.Equal(string.Empty, _engine.ToIPA(input));
            Assert.Equal(string.Empty, _engine.ToPhonemes(input));
            Assert.Empty(_engine.ToPhonemeList(input));
            Assert.Empty(_engine.ToSyllables(input));
        }

        [Fact]
        public void Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA(null));
            Assert.Equal(string.Empty, _engine.ToPhonemes(null));
            Assert.Empty(_engine.ToPhonemeList(null));
            Assert.Empty(_engine.ToSyllables(null));
        }

        // ========== 記号のみ ==========

        [Theory]
        [InlineData("!!!")]
        [InlineData("...")]
        [InlineData("---")]
        public void PunctuationOnly_ReturnsEmpty(string input)
        {
            Assert.Equal(string.Empty, _engine.ToIPA(input));
        }

        [Fact]
        public void SymbolsExpanded_ViaNormalization()
        {
            // @#$% は正規化で「arroba cardinal」等に展開される
            var result = _engine.ToIPA("@#$%");
            Assert.NotEmpty(result);
        }

        // ========== 数字 ==========

        [Fact]
        public void Numbers_ProcessedViaNormalization()
        {
            var result = _engine.ToIPA("100");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void LargeNumber_ProcessedWithoutException()
        {
            var result = _engine.ToIPA("12345");
            Assert.NotEmpty(result);
        }

        // ========== 単一文字入力 ==========

        [Theory]
        [InlineData("a", "\u02C8a")]
        [InlineData("\u00E9", "\u025B")]
        public void SingleCharacter_ProcessedCorrectly(string input, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(input));
        }

        // ========== 超長文 ==========

        [Fact]
        public void VeryLongText_ProcessesWithoutException()
        {
            var words = new string[120];
            for (var i = 0; i < words.Length; i++)
                words[i] = "casa";
            var longText = string.Join(" ", words);

            var result = _engine.ToIPA(longText);
            Assert.NotEmpty(result);
        }

        // ========== 混在スクリプト ==========

        [Fact]
        public void MixedScript_EnglishWords_SkippedOrProcessed()
        {
            // 英語混入は例外なく処理される
            var result = _engine.ToIPA("hello world");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void MixedScript_CJKCharacters_NoException()
        {
            // CJK文字が含まれてもクラッシュしない
            var ex = Record.Exception(() => _engine.ToIPA("\u4F60\u597D casa"));
            Assert.Null(ex);
        }

        // ========== バッチAPI エッジケース ==========

        [Fact]
        public void BatchApis_EmptyInput_ReturnEmptyCollection()
        {
            Assert.Empty(_engine.ToIPABatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToPhonemesBatch(Array.Empty<string>()));
            Assert.Empty(_engine.ToPhonemeListBatch(Array.Empty<string>()));
        }

        [Fact]
        public void BatchApis_Null_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemeListBatch(null!));
        }

        [Fact]
        public void BatchAndSingleApis_ReturnSameResults()
        {
            var texts = new[] { "casa", "gato", "mundo", "sol" };
            var batchIpa = _engine.ToIPABatch(texts);
            var batchPhonemes = _engine.ToPhonemesBatch(texts);

            for (var i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToIPA(texts[i]), batchIpa[i]);
                Assert.Equal(_engine.ToPhonemes(texts[i]), batchPhonemes[i]);
            }
        }

        // ========== 大文字・全角数字 ==========

        [Fact]
        public void UpperCase_NormalizesConsistently()
        {
            Assert.Equal(_engine.ToIPA("casa"), _engine.ToIPA("CASA"));
            Assert.Equal(_engine.ToIPA("gato"), _engine.ToIPA("Gato"));
        }

        // ========== IncludeStress設定 ==========

        [Fact]
        public void IncludeStressFalse_NoStressMarkers()
        {
            using var engine = new PortugueseG2PEngine(new PortugueseG2POptions(includeStress: false));
            var result = engine.ToIPA("bonito");
            Assert.DoesNotContain("\u02C8", result);
            Assert.DoesNotContain("\u02CC", result);
        }

        // ========== ToSyllables エッジケース ==========

        [Fact]
        public void ToSyllables_Whitespace_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToSyllables("   "));
        }

        // ========== Dispose パターン ==========

        [Fact]
        public void DoubleDispose_DoesNotThrow()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            engine.Dispose();
        }

        [Fact]
        public void Dispose_ThenAllApis_ThrowObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("test"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPAWithoutStress("test"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("test"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("test"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToSyllables("test"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "test" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(new[] { "test" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeListBatch(new[] { "test" }));
        }

        public void Dispose() => _engine.Dispose();
    }
}
