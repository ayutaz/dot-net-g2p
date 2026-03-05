using System;
using System.Collections.Generic;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// EnglishG2PEngine パイプライン統合テスト。
    /// </summary>
    public class EnglishPipelineTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EnglishPipelineTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // ===== ToPhonemes =====

        [Fact]
        public void ToPhonemes_HelloWorld_ReturnsCorrectArpabet()
        {
            var result = _engine.ToPhonemes("hello world");
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        [Fact]
        public void ToPhonemes_WithPunctuation_IgnoresPunctuation()
        {
            var result = _engine.ToPhonemes("Hello, world!");
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        // ===== ToPhonemeList =====

        [Fact]
        public void ToPhonemeList_Hello_ReturnsCorrectStructs()
        {
            var result = _engine.ToPhonemeList("hello");

            Assert.Equal(4, result.Count);

            // HH
            Assert.Equal(ArpabetPhoneme.HH, result[0].Phoneme);
            Assert.Equal(Stress.None, result[0].Stress);
            Assert.False(result[0].IsVowel);

            // AH0
            Assert.Equal(ArpabetPhoneme.AH, result[1].Phoneme);
            Assert.Equal(Stress.NoStress, result[1].Stress);
            Assert.True(result[1].IsVowel);

            // L
            Assert.Equal(ArpabetPhoneme.L, result[2].Phoneme);
            Assert.Equal(Stress.None, result[2].Stress);

            // OW1
            Assert.Equal(ArpabetPhoneme.OW, result[3].Phoneme);
            Assert.Equal(Stress.Primary, result[3].Stress);
            Assert.True(result[3].IsVowel);
        }

        // ===== LookupWord =====

        [Fact]
        public void LookupWord_KnownWord_ReturnsPhonemesArray()
        {
            var result = _engine.LookupWord("hello");
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void LookupWord_UnknownWord_ReturnsEmpty()
        {
            var result = _engine.LookupWord("xyzzyplugh");
            Assert.Empty(result);
        }

        // ===== 空・null入力 =====

        [Fact]
        public void ToPhonemes_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes(""));
        }

        [Fact]
        public void ToPhonemes_Null_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes(null!));
        }

        [Fact]
        public void ToPhonemes_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("   "));
        }

        // ===== OOV処理 =====

        [Fact]
        public void ToPhonemes_OovSkip_SkipsUnknownWords()
        {
            // デフォルトはSkip
            var result = _engine.ToPhonemes("hello xyzzyplugh world");
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        [Fact]
        public void ToPhonemes_OovThrow_ThrowsKeyNotFoundException()
        {
            var options = new EnglishG2POptions(unknownWordHandling: UnknownWordStrategy.Throw);
            using (var engine = new EnglishG2PEngine(options))
            {
                Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("xyzzyplugh"));
            }
        }

        // ===== 複数文の処理 =====

        [Fact]
        public void ToPhonemes_MultipleSentences_ProcessesAll()
        {
            var result = _engine.ToPhonemes("I am a student, she is a teacher");
            // 各単語が変換され、スペースで結合される
            Assert.NotEmpty(result);
            Assert.Contains("AY1", result);  // I
            Assert.Contains("S T UW1 D AH0 N T", result);  // student
        }
    }
}
