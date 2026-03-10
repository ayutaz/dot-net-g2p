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
            var result = engine.ToIPA("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Constructor_WithOptions_CreatesInstance()
        {
            var options = new PortugueseG2POptions(dialect: PortugueseDialect.European);
            using var engine = new PortugueseG2PEngine(options);
            var result = engine.ToIPA("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PortugueseG2PEngine(null!));
        }

        // ========== ToIPA 基本動作 ==========

        [Fact]
        public void ToIPA_SimpleWord_ReturnsNonEmpty()
        {
            var result = _engine.ToIPA("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToIPA_NasalDiphthong_ReturnsNonEmpty()
        {
            // "não" には鼻母音が含まれる
            var result = _engine.ToIPA("não");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToIPA_AccentedWord_ReturnsNonEmpty()
        {
            var result = _engine.ToIPA("café");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToIPA_MultipleWords_ReturnsSpaceSeparated()
        {
            var result = _engine.ToIPA("o gato");
            Assert.NotEmpty(result);
            // 複数単語はスペースで区切られる
            Assert.Contains(" ", result);
        }

        [Fact]
        public void ToIPA_UpperCase_NormalizesToLower()
        {
            var lower = _engine.ToIPA("casa");
            var upper = _engine.ToIPA("CASA");
            Assert.Equal(lower, upper);
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

        [Fact]
        public void ToIPAWithoutStress_ReturnsNonEmpty()
        {
            var result = _engine.ToIPAWithoutStress("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToIPAWithoutStress_NoStressMarks()
        {
            var result = _engine.ToIPAWithoutStress("casa");
            Assert.DoesNotContain("\u02C8", result); // primary stress mark
            Assert.DoesNotContain("\u02CC", result); // secondary stress mark
        }

        // ========== ToPhonemes ==========

        [Fact]
        public void ToPhonemes_SimpleWord_ReturnsSpaceSeparated()
        {
            var result = _engine.ToPhonemes("gato");
            Assert.NotEmpty(result);
            // 音素がスペースで区切られている
            Assert.Contains(" ", result);
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
            Assert.NotEmpty(result);
            Assert.Contains("-", result);
        }

        // ========== ToPhonemeList ==========

        [Fact]
        public void ToPhonemeList_SimpleWord_ReturnsPhonemes()
        {
            var result = _engine.ToPhonemeList("casa");
            Assert.NotEmpty(result);
            Assert.True(result.Count >= 2);
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
            Assert.NotEmpty(result);
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
        public void ToIPABatch_ReturnsCorrectCount()
        {
            var results = _engine.ToIPABatch(new[] { "casa", "gato" });
            Assert.Equal(2, results.Count);
            Assert.NotEmpty(results[0]);
            Assert.NotEmpty(results[1]);
        }

        [Fact]
        public void ToPhonemesBatch_ReturnsCorrectCount()
        {
            var results = _engine.ToPhonemesBatch(new[] { "sol", "lua" });
            Assert.Equal(2, results.Count);
            Assert.NotEmpty(results[0]);
            Assert.NotEmpty(results[1]);
        }

        [Fact]
        public void ToPhonemeListBatch_ReturnsCorrectCount()
        {
            var results = _engine.ToPhonemeListBatch(new[] { "amor" });
            Assert.Single(results);
            Assert.NotEmpty(results[0]);
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
            engine.Dispose(); // 二重Disposeは例外なし
        }

        // ========== 方言切替 ==========

        [Fact]
        public void Dialect_Brazilian_ReturnsNonEmpty()
        {
            var options = new PortugueseG2POptions(dialect: PortugueseDialect.Brazilian);
            using var engine = new PortugueseG2PEngine(options);
            var result = engine.ToIPA("casa");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Dialect_European_ReturnsNonEmpty()
        {
            var options = new PortugueseG2POptions(dialect: PortugueseDialect.European);
            using var engine = new PortugueseG2PEngine(options);
            var result = engine.ToIPA("casa");
            Assert.NotEmpty(result);
        }

        // ========== IncludeStress ==========

        [Fact]
        public void IncludeStress_True_ContainsStressMark()
        {
            var options = new PortugueseG2POptions(includeStress: true);
            using var engine = new PortugueseG2PEngine(options);
            // "casa" は2音節語なのでストレスマークが含まれるはず
            var result = engine.ToIPA("casa");
            Assert.Contains("\u02C8", result); // ˈ
        }

        [Fact]
        public void IncludeStress_False_NoStressMark()
        {
            var options = new PortugueseG2POptions(includeStress: false);
            using var engine = new PortugueseG2PEngine(options);
            var result = engine.ToIPA("casa");
            Assert.DoesNotContain("\u02C8", result); // ˈ を含まない
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
            var result = _engine.ToIPA("olá, mundo!");
            Assert.NotEmpty(result);
            // 句読点は除去されて2単語の結果が返る
            Assert.Contains(" ", result);
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
