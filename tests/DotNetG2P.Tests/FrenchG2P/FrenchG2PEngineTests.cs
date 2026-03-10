using DotNetG2P.French;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// FrenchG2PEngine の統合テスト。
    /// </summary>
    public class FrenchG2PEngineTests : IDisposable
    {
        private readonly FrenchG2PEngine _engine = new FrenchG2PEngine();

        // ========== コンストラクタ ==========

        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            using var engine = new FrenchG2PEngine();
            var result = engine.ToIPA("bonjour");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Constructor_WithOptions_CreatesInstance()
        {
            var options = new FrenchG2POptions(dialect: FrenchDialect.Conservative);
            using var engine = new FrenchG2PEngine(options);
            var result = engine.ToIPA("bonjour");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FrenchG2PEngine(null!));
        }

        // ========== ToIPA 基本動作 ==========

        [Fact]
        public void ToIPA_SimpleWord_ReturnsIPA()
        {
            // bonjour → bɔ̃ʒuʁ
            var result = _engine.ToIPA("bonjour");
            Assert.Equal("b\u0254\u0303\u0292u\u0281", result);
        }

        [Fact]
        public void ToIPA_MultipleWords_ReturnsSpaceSeparated()
        {
            // "bonjour le monde" → "bɔ̃ʒuʁ lə mɔ̃d"
            var result = _engine.ToIPA("Bonjour le monde");
            Assert.Equal("b\u0254\u0303\u0292u\u0281 l\u0259 m\u0254\u0303d", result);
        }

        [Fact]
        public void ToIPA_UpperCase_NormalizesToLower()
        {
            // "BONJOUR" → 小文字化後に変換 → "bɔ̃ʒuʁ"
            var result = _engine.ToIPA("BONJOUR");
            Assert.Equal("b\u0254\u0303\u0292u\u0281", result);
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

        // ========== ToPhonemes ==========

        [Fact]
        public void ToPhonemes_SimpleWord_ReturnsSpaceSeparated()
        {
            // bonjour → "b ɔ̃ ʒ u ʁ"
            var result = _engine.ToPhonemes("bonjour");
            Assert.Equal("b \u0254\u0303 \u0292 u \u0281", result);
        }

        [Fact]
        public void ToPhonemes_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, _engine.ToPhonemes(""));
        }

        // ========== ToIPAWithoutStress ==========

        [Fact]
        public void ToIPAWithoutStress_ReturnsIPA()
        {
            var result = _engine.ToIPAWithoutStress("bonjour");
            Assert.Equal("b\u0254\u0303\u0292u\u0281", result);
        }

        [Fact]
        public void ToIPAWithoutStress_SameAsToIPA_WhenStressDisabled()
        {
            // デフォルトでは IncludeStress=false なので ToIPA と同じ結果
            var ipa = _engine.ToIPA("merci");
            var ipaNoStress = _engine.ToIPAWithoutStress("merci");
            Assert.Equal(ipa, ipaNoStress);
        }

        // ========== ToPhonemeList ==========

        [Fact]
        public void ToPhonemeList_SimpleWord_ReturnsPhonemes()
        {
            var result = _engine.ToPhonemeList("bonjour");
            Assert.Equal(5, result.Count);
            Assert.Equal(FrenchIpaPhoneme.B, result[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ONasal, result[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Zh, result[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.U, result[3].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.R, result[4].Phoneme);
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

        // ========== バッチAPI ==========

        [Fact]
        public void ToIPABatch_ReturnsCorrectResults()
        {
            var results = _engine.ToIPABatch(new[] { "bonjour", "merci" });
            Assert.Equal(2, results.Count);
            Assert.Equal("b\u0254\u0303\u0292u\u0281", results[0]);
            Assert.Equal("m\u0259\u0281si", results[1]);
        }

        [Fact]
        public void ToPhonemesBatch_ReturnsCorrectResults()
        {
            var results = _engine.ToPhonemesBatch(new[] { "ami" });
            Assert.Single(results);
            Assert.Equal("a m i", results[0]);
        }

        [Fact]
        public void ToPhonemeListBatch_ReturnsCorrectResults()
        {
            var results = _engine.ToPhonemeListBatch(new[] { "ami" });
            Assert.Single(results);
            Assert.Equal(3, results[0].Count);
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

        // ========== Dispose ==========

        [Fact]
        public void Dispose_ThenToIPA_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("test"));
        }

        [Fact]
        public void Dispose_ThenToPhonemes_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("test"));
        }

        [Fact]
        public void Dispose_ThenToPhonemeList_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("test"));
        }

        [Fact]
        public void Dispose_ThenBatch_ThrowsObjectDisposedException()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "test" }));
        }

        [Fact]
        public void DoubleDispose_DoesNotThrow()
        {
            var engine = new FrenchG2PEngine();
            engine.Dispose();
            engine.Dispose(); // 二重Disposeは例外なし
        }

        // ========== 方言切替 ==========

        [Fact]
        public void Dialect_Metropolitan_MergesNasalVowels()
        {
            // "brun": Metropolitan → ɛ̃, Conservative → œ̃
            var metro = _engine.ToIPA("brun");
            Assert.Contains("\u025B\u0303", metro); // ɛ̃
        }

        [Fact]
        public void Dialect_Conservative_PreservesOeNasal()
        {
            using var conserv = new FrenchG2PEngine(
                new FrenchG2POptions(dialect: FrenchDialect.Conservative));
            var result = conserv.ToIPA("brun");
            Assert.Contains("\u0153\u0303", result); // œ̃
        }

        // ========== IncludeStress ==========

        [Fact]
        public void IncludeStress_DefaultFalse_NoStressMarks()
        {
            // デフォルト IncludeStress=false → ストレスマークなし
            var result = _engine.ToIPA("bonjour");
            Assert.DoesNotContain("\u02C8", result); // ˈ を含まない
        }

        // ========== テキスト正規化 ==========

        [Fact]
        public void TextNormalization_CaseFolding()
        {
            // 大文字は小文字に正規化
            Assert.Equal(_engine.ToIPA("ami"), _engine.ToIPA("AMI"));
        }

        [Fact]
        public void TextNormalization_PunctuationRemoved()
        {
            // 句読点はトークン区切りとして扱われ、除去される
            var result = _engine.ToIPA("bonjour, monde!");
            Assert.Equal("b\u0254\u0303\u0292u\u0281 m\u0254\u0303d", result);
        }

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}
