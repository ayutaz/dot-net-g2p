using System;
using DotNetG2P.Korean;

namespace DotNetG2P.Tests.KoreanG2P
{
    public class KoreanG2PEngineTests
    {
        [Fact]
        public void ToPhonemes_NullOrEmpty_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal("", engine.ToPhonemes(""));
            Assert.Equal("", engine.ToPhonemes(null!));
            Assert.Equal("", engine.ToPhonemes("   "));
        }

        [Fact]
        public void ToJamo_NullOrEmpty_ReturnsEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Equal("", engine.ToJamo(""));
            Assert.Equal("", engine.ToJamo(null!));
            Assert.Equal("", engine.ToJamo("\t"));
        }

        [Fact]
        public void ToJamo_HangulSyllables_AreDecomposedPerSyllable()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToJamo("한글");

            Assert.Equal("ㅎㅏㄴ ㄱㅡㄹ", result);
        }

        [Fact]
        public void ToPhonemes_HangulSyllables_AreFlattened()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPhonemes("한글");

            Assert.Equal("ㅎ ㅏ ㄴ ㄱ ㅡ ㄹ", result);
        }

        [Fact]
        public void ToPhonemes_WithCustomSeparator_UsesConfiguredSeparator()
        {
            using var engine = new KoreanG2PEngine(new KoreanG2POptions(separator: "/", syllableSeparator: "|"));

            Assert.Equal("ㅎ/ㅏ/ㄴ", engine.ToPhonemes("한"));
            Assert.Equal("ㅎㅏㄴ", engine.ToJamo("한"));
        }

        [Fact]
        public void ToPhonemes_NonHangulPreservedByDefault()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.ToPhonemes("한A");

            Assert.Equal("ㅎ ㅏ ㄴ A", result);
        }

        [Fact]
        public void ToPhonemes_NonHangulCanBeDropped()
        {
            using var engine = new KoreanG2PEngine(new KoreanG2POptions(preserveNonHangul: false));

            var result = engine.ToPhonemes("한A");

            Assert.Equal("ㅎ ㅏ ㄴ", result);
        }

        [Fact]
        public void Analyze_ReturnsPronunciationModel()
        {
            using var engine = new KoreanG2PEngine();

            var result = engine.Analyze("한글");

            Assert.Equal("한글", result.OriginalText);
            Assert.Equal("한글", result.NormalizedText);
            Assert.Equal(2, result.Syllables.Count);
            Assert.Equal(6, result.Phonemes.Count);
            Assert.Equal("한글", result.ToHangulString());
            Assert.Equal("ㅎㅏㄴ", result.GetJamoSyllables()[0]);
            Assert.Equal("ㄱㅡㄹ", result.GetJamoSyllables()[1]);
        }

        [Fact]
        public void ToPhonemesBatch_Null_ThrowsArgumentNullException()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Throws<ArgumentNullException>(() => engine.ToPhonemesBatch(null!));
        }

        [Fact]
        public void ToJamoBatch_Null_ThrowsArgumentNullException()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Throws<ArgumentNullException>(() => engine.ToJamoBatch(null!));
        }

        [Fact]
        public void BatchApis_EmptyInput_ReturnEmpty()
        {
            using var engine = new KoreanG2PEngine();

            Assert.Empty(engine.ToPhonemesBatch(Array.Empty<string>()));
            Assert.Empty(engine.ToJamoBatch(Array.Empty<string>()));
        }

        [Fact]
        public void BatchApis_MixedInput_HandleAllElements()
        {
            using var engine = new KoreanG2PEngine();

            var phonemes = engine.ToPhonemesBatch(new[] { "한글", "", null! });
            var jamo = engine.ToJamoBatch(new[] { "한글", "", null! });

            Assert.Equal(3, phonemes.Count);
            Assert.Equal("ㅎ ㅏ ㄴ ㄱ ㅡ ㄹ", phonemes[0]);
            Assert.Equal("", phonemes[1]);
            Assert.Equal("", phonemes[2]);

            Assert.Equal(3, jamo.Count);
            Assert.Equal("ㅎㅏㄴ ㄱㅡㄹ", jamo[0]);
            Assert.Equal("", jamo[1]);
            Assert.Equal("", jamo[2]);
        }

        [Fact]
        public void Dispose_ThenAllApis_ThrowObjectDisposedException()
        {
            var engine = new KoreanG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("한글"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToJamo("한글"));
            Assert.Throws<ObjectDisposedException>(() => engine.Analyze("한글"));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(new[] { "한글" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToJamoBatch(new[] { "한글" }));
        }
    }
}
