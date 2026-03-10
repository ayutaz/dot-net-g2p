using DotNetG2P.Portuguese;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// PortugueseG2PEngine の統合テスト。
    /// </summary>
    public class PortugueseG2PEngineTests : IDisposable
    {
        private readonly PortugueseG2PEngine _engine = new PortugueseG2PEngine();

        // ========== コンストラクタ ==========

        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            using var engine = new PortugueseG2PEngine();
            Assert.Equal("\u02C8kaza", engine.ToIPA("casa"));
        }

        [Fact]
        public void Constructor_WithOptions_CreatesInstance()
        {
            var options = new PortugueseG2POptions(dialect: PortugueseDialect.European);
            using var engine = new PortugueseG2PEngine(options);
            Assert.Equal("\u02C8kaza", engine.ToIPA("casa"));
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PortugueseG2PEngine(null!));
        }

        // ========== ToIPA 基本動作（T1: 具体的IPA期待値） ==========

        [Theory]
        [InlineData("casa", "\u02C8kaza")]
        [InlineData("gato", "\u02C8\u0261ato")]
        [InlineData("mundo", "\u02C8mu\u0303do")]
        [InlineData("tempo", "\u02C8te\u0303po")]
        [InlineData("bonito", "bo\u02C8nito")]
        [InlineData("feliz", "fe\u02C8liz")]
        [InlineData("sol", "\u02C8sol")]
        [InlineData("lua", "\u02C8lwa")]
        [InlineData("amor", "a\u02C8mo\u027E")]
        [InlineData("caf\u00E9", "k\u0250\u02C8f\u025B")]
        public void ToIPA_BasicWords_MatchExpectedOutput(string word, string expected)
        {
            Assert.Equal(expected, _engine.ToIPA(word));
        }

        [Fact]
        public void ToIPA_NasalDiphthong_ReturnsExpected()
        {
            // "n\u00E3o" -> /n\u0250\u0303w\u0303/
            Assert.Equal("\u02C8n\u0250\u0303w\u0303", _engine.ToIPA("n\u00E3o"));
        }

        [Fact]
        public void ToIPA_NasalWord_Pao_ReturnsExpected()
        {
            // "p\u00E3o" -> /p\u0250\u0303w\u0303/ (例外辞書により単音節語はストレスマーク省略)
            Assert.Equal("p\u0250\u0303w\u0303", _engine.ToIPA("p\u00E3o"));
        }

        [Fact]
        public void ToIPA_NasalWord_Coracao_ReturnsExpected()
        {
            Assert.Equal("ku\u027E\u0250\u02C8s\u0250\u0303w\u0303", _engine.ToIPA("cora\u00E7\u00E3o"));
        }

        [Fact]
        public void ToIPA_Agua_ReturnsExpected()
        {
            Assert.Equal("\u02C8a\u0261wa", _engine.ToIPA("\u00E1gua"));
        }

        [Fact]
        public void ToIPA_MultipleWords_ReturnsSpaceSeparated()
        {
            var result = _engine.ToIPA("o gato");
            Assert.Contains(" ", result);
            Assert.Equal("\u02C8o \u02C8\u0261ato", result);
        }

        [Fact]
        public void ToIPA_UpperCase_NormalizesToLower()
        {
            Assert.Equal(_engine.ToIPA("casa"), _engine.ToIPA("CASA"));
        }

        // ========== 空文字列・null・空白のみ ==========

        [Fact]
        public void ToIPA_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA(""));
        }

        [Fact]
        public void ToIPA_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA(null));
        }

        [Fact]
        public void ToIPA_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToIPA("   "));
        }

        // ========== ToIPAWithoutStress ==========

        [Theory]
        [InlineData("casa", "kaza")]
        [InlineData("gato", "\u0261ato")]
        [InlineData("bonito", "bonito")]
        [InlineData("feliz", "feliz")]
        public void ToIPAWithoutStress_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _engine.ToIPAWithoutStress(word));
        }

        [Fact]
        public void ToIPAWithoutStress_NoStressMarks()
        {
            var result = _engine.ToIPAWithoutStress("casa");
            Assert.DoesNotContain("\u02C8", result);
            Assert.DoesNotContain("\u02CC", result);
        }

        // ========== ToPhonemes ==========

        [Theory]
        [InlineData("gato", "\u02C8\u0261 a t o")]
        [InlineData("casa", "\u02C8k a z a")]
        [InlineData("sol", "\u02C8s o l")]
        public void ToPhonemes_MatchesExpected(string word, string expected)
        {
            Assert.Equal(expected, _engine.ToPhonemes(word));
        }

        [Fact]
        public void ToPhonemes_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToPhonemes(""));
        }

        [Fact]
        public void ToPhonemes_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToPhonemes(null));
        }

        [Fact]
        public void ToPhonemes_CustomSeparator_UsesIt()
        {
            var options = new PortugueseG2POptions(separator: "-");
            using var engine = new PortugueseG2PEngine(options);
            var result = engine.ToPhonemes("gato");
            Assert.Contains("-", result);
        }

        // ========== ToPhonemeList ==========

        [Fact]
        public void ToPhonemeList_SimpleWord_ReturnsPhonemes()
        {
            var result = _engine.ToPhonemeList("casa");
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void ToPhonemeList_EmptyString_ReturnsEmpty()
        {
            var result = _engine.ToPhonemeList("");
            Assert.Empty(result);
        }

        [Fact]
        public void ToPhonemeList_Null_ReturnsEmpty()
        {
            var result = _engine.ToPhonemeList(null);
            Assert.Empty(result);
        }

        [Fact]
        public void ToPhonemeList_ReturnsPortuguesePhonemes()
        {
            var result = _engine.ToPhonemeList("sol");
            Assert.Equal(3, result.Count);
            foreach (var ph in result)
            {
                Assert.True(Enum.IsDefined(typeof(PortugueseIpaPhoneme), ph.Phoneme));
            }
        }

        // ========== ToSyllables ==========

        [Fact]
        public void ToSyllables_SimpleWord_ReturnsSyllables()
        {
            var result = _engine.ToSyllables("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToSyllables_EmptyString_ReturnsEmpty()
        {
            var result = _engine.ToSyllables("");
            Assert.Empty(result);
        }

        [Fact]
        public void ToSyllables_Null_ReturnsEmpty()
        {
            var result = _engine.ToSyllables(null);
            Assert.Empty(result);
        }

        // ========== バッチAPI ==========

        [Fact]
        public void ToIPABatch_ReturnsCorrectResults()
        {
            var results = _engine.ToIPABatch(new[] { "casa", "gato" });
            Assert.Equal(2, results.Count);
            Assert.Equal("\u02C8kaza", results[0]);
            Assert.Equal("\u02C8\u0261ato", results[1]);
        }

        [Fact]
        public void ToPhonemesBatch_ReturnsCorrectResults()
        {
            var results = _engine.ToPhonemesBatch(new[] { "sol", "lua" });
            Assert.Equal(2, results.Count);
            Assert.Equal("\u02C8s o l", results[0]);
            Assert.Equal("\u02C8l w a", results[1]);
        }

        [Fact]
        public void ToPhonemeListBatch_ReturnsCorrectCount()
        {
            var results = _engine.ToPhonemeListBatch(new[] { "amor" });
            Assert.Single(results);
            Assert.Equal(4, results[0].Count);
        }

        [Fact]
        public void ToIPABatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToIPABatch(null!));
        }

        [Fact]
        public void ToPhonemesBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemesBatch(null!));
        }

        [Fact]
        public void ToPhonemeListBatch_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _engine.ToPhonemeListBatch(null!));
        }

        [Fact]
        public void ToIPABatch_EmptyList_ReturnsEmpty()
        {
            var results = _engine.ToIPABatch(Array.Empty<string>());
            Assert.Empty(results);
        }

        [Fact]
        public void BatchAndSingle_ReturnSameResults()
        {
            var texts = new[] { "casa", "gato", "mundo", "sol" };
            var batchResults = _engine.ToIPABatch(texts);
            for (var i = 0; i < texts.Length; i++)
            {
                Assert.Equal(_engine.ToIPA(texts[i]), batchResults[i]);
            }
        }

        // ========== Dispose ==========

        [Fact]
        public void Dispose_ThenToIPA_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("test"));
        }

        [Fact]
        public void Dispose_ThenToPhonemes_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("test"));
        }

        [Fact]
        public void Dispose_ThenToPhonemeList_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("test"));
        }

        [Fact]
        public void Dispose_ThenToSyllables_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToSyllables("test"));
        }

        [Fact]
        public void Dispose_ThenBatch_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "test" }));
        }

        [Fact]
        public void Dispose_ThenToPhonemesBatch_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(new[] { "test" }));
        }

        [Fact]
        public void Dispose_ThenToPhonemeListBatch_ThrowsObjectDisposedException()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeListBatch(new[] { "test" }));
        }

        [Fact]
        public void DoubleDispose_DoesNotThrow()
        {
            var engine = new PortugueseG2PEngine();
            engine.Dispose();
            engine.Dispose();
        }

        // ========== T2: 方言差の具体的検証（異音有効時のみ差が出る） ==========

        [Theory]
        [InlineData("tipo", "\u02C8t\u0361\u0283ipu", "\u02C8tipu")]
        [InlineData("cidade", "si\u02C8dad\u0361\u0292i", "si\u02C8\u00F0a\u00F0\u0268")]
        [InlineData("feliz", "fi\u02C8liz", "f\u0268\u02C8li\u0292")]
        [InlineData("sol", "\u02C8sow", "\u02C8so\u026B")]
        [InlineData("leite", "\u02C8lejt\u0361\u0283i", "\u02C8lejt\u0268")]
        [InlineData("gente", "\u02C8\u0292e\u0303t\u0361\u0283i", "\u02C8\u0292e\u0303t\u0268")]
        [InlineData("nome", "\u02C8nomi", "\u02C8nom\u0268")]
        [InlineData("grande", "\u02C8\u0261\u027E\u0250\u0303d\u0361\u0292i", "\u02C8\u0261\u027E\u0250\u0303\u00F0\u0268")]
        [InlineData("escola", "\u0268s\u02C8k\u0254l\u0250", "\u0268\u0283\u02C8k\u0254l\u0250")]
        public void Dialect_BP_vs_EP_AllophonesProduceDifferentOutput(
            string word, string expectedBP, string expectedEP)
        {
            using var bp = new PortugueseG2PEngine(new PortugueseG2POptions(
                dialect: PortugueseDialect.Brazilian, enableAllophones: true));
            using var ep = new PortugueseG2PEngine(new PortugueseG2POptions(
                dialect: PortugueseDialect.European, enableAllophones: true));

            var bpResult = bp.ToIPA(word);
            var epResult = ep.ToIPA(word);

            Assert.NotEqual(bpResult, epResult);
            Assert.Equal(expectedBP, bpResult);
            Assert.Equal(expectedEP, epResult);
        }

        [Fact]
        public void Dialect_BaseRules_SameForBothDialects()
        {
            using var bp = new PortugueseG2PEngine(new PortugueseG2POptions(dialect: PortugueseDialect.Brazilian));
            using var ep = new PortugueseG2PEngine(new PortugueseG2POptions(dialect: PortugueseDialect.European));

            var words = new[] { "casa", "gato", "mundo", "sol", "porta" };
            foreach (var word in words)
            {
                Assert.Equal(bp.ToIPA(word), ep.ToIPA(word));
            }
        }

        // ========== T3: EnableAllophones 統合テスト ==========

        [Theory]
        [InlineData("casa", "\u02C8kaza", "\u02C8kaz\u0250")]
        [InlineData("gato", "\u02C8\u0261ato", "\u02C8\u0261atu")]
        [InlineData("mundo", "\u02C8mu\u0303do", "\u02C8mu\u0303du")]
        [InlineData("bonito", "bo\u02C8nito", "bu\u02C8nitu")]
        [InlineData("mesmo", "\u02C8mesmo", "\u02C8mezmu")]
        [InlineData("sol", "\u02C8sol", "\u02C8sow")]
        public void EnableAllophones_BP_ChangeOutput(string word, string expectedBase, string expectedAllo)
        {
            using var baseEngine = new PortugueseG2PEngine(new PortugueseG2POptions(enableAllophones: false));
            using var alloEngine = new PortugueseG2PEngine(new PortugueseG2POptions(enableAllophones: true));

            Assert.Equal(expectedBase, baseEngine.ToIPA(word));
            Assert.Equal(expectedAllo, alloEngine.ToIPA(word));
            Assert.NotEqual(baseEngine.ToIPA(word), alloEngine.ToIPA(word));
        }

        [Theory]
        [InlineData("mundo", "\u02C8mu\u0303do", "\u02C8mu\u0303\u00F0u")]
        [InlineData("feliz", "fe\u02C8liz", "f\u0268\u02C8li\u0292")]
        [InlineData("sol", "\u02C8sol", "\u02C8so\u026B")]
        [InlineData("trabalho", "t\u027Ea\u02C8ba\u028Eo", "t\u027E\u0250\u02C8\u03B2a\u028Eu")]
        public void EnableAllophones_EP_ChangeOutput(string word, string expectedBase, string expectedAllo)
        {
            using var baseEngine = new PortugueseG2PEngine(new PortugueseG2POptions(
                dialect: PortugueseDialect.European, enableAllophones: false));
            using var alloEngine = new PortugueseG2PEngine(new PortugueseG2POptions(
                dialect: PortugueseDialect.European, enableAllophones: true));

            Assert.Equal(expectedBase, baseEngine.ToIPA(word));
            Assert.Equal(expectedAllo, alloEngine.ToIPA(word));
        }

        // ========== T4: EnableExceptionDictionary 統合テスト ==========

        [Fact]
        public void EnableExceptionDictionary_DictionaryChangesOutput()
        {
            using var withDict = new PortugueseG2PEngine(new PortugueseG2POptions(enableExceptionDictionary: true));
            using var noDict = new PortugueseG2PEngine(new PortugueseG2POptions(enableExceptionDictionary: false));

            // 例外辞書に登録されている語（例: belo, terra, festa）
            // 辞書有効時は開/閉母音の正確な区別が反映されるため出力が異なる
            var exceptionWords = new[] { "belo", "terra", "festa", "dedo", "bolo", "fogo" };
            var anyDiffers = false;
            foreach (var word in exceptionWords)
            {
                if (withDict.ToIPA(word) != noDict.ToIPA(word))
                {
                    anyDiffers = true;
                    break;
                }
            }
            Assert.True(anyDiffers, "例外辞書有効化により少なくとも1語の出力が変わるべき");
        }

        // ========== IncludeStress ==========

        [Fact]
        public void IncludeStress_True_ContainsStressMark()
        {
            var options = new PortugueseG2POptions(includeStress: true);
            using var engine = new PortugueseG2PEngine(options);
            Assert.Equal("\u02C8kaza", engine.ToIPA("casa"));
        }

        [Fact]
        public void IncludeStress_False_NoStressMark()
        {
            var options = new PortugueseG2POptions(includeStress: false);
            using var engine = new PortugueseG2PEngine(options);
            Assert.Equal("kaza", engine.ToIPA("casa"));
        }

        // ========== テキスト正規化 ==========

        [Fact]
        public void TextNormalization_CaseFolding()
        {
            Assert.Equal(_engine.ToIPA("gato"), _engine.ToIPA("GATO"));
        }

        [Fact]
        public void TextNormalization_PunctuationRemoved()
        {
            var result = _engine.ToIPA("ol\u00E1, mundo!");
            Assert.Equal("o\u02C8la \u02C8mu\u0303do", result);
        }

        // ========== オプションデフォルト値 ==========

        [Fact]
        public void Options_Default_HasExpectedValues()
        {
            var options = PortugueseG2POptions.Default;
            Assert.Equal(PortugueseDialect.Brazilian, options.Dialect);
            Assert.True(options.IncludeStress);
            Assert.False(options.EnableAllophones);
            Assert.True(options.EnableTextNormalization);
            Assert.True(options.EnableExceptionDictionary);
            Assert.Equal(" ", options.Separator);
        }

        [Fact]
        public void Options_NullSeparator_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PortugueseG2POptions(separator: null!));
        }

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}
