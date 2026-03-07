using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            // LTSを無効にして辞書のみで検索する
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.LookupWord("xyzzyplugh");
                Assert.Empty(result);
            }
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
            // LTSを無効にして辞書のみのSkip動作を検証
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("hello xyzzyplugh world");
                Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
            }
        }

        [Fact]
        public void ToPhonemes_OovThrow_ThrowsKeyNotFoundException()
        {
            // LTSを無効にしてThrow動作を検証
            var options = new EnglishG2POptions(unknownWordHandling: UnknownWordStrategy.Throw, enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("xyzzyplugh"));
            }
        }

        // ===== 複数文の処理 (A5: 音素トークン数の十分性を検証) =====

        [Fact]
        public void ToPhonemes_MultipleSentences_ProcessesAll()
        {
            var result = _engine.ToPhonemes("I am a student, she is a teacher");
            // 各単語が変換され、スペースで結合される
            Assert.NotEmpty(result);
            Assert.Contains("AY1", result);  // I
            Assert.Contains("S T UW1 D AH0 N T", result);  // student

            // A5: 出力に含まれる音素トークン数が十分多いことを検証（8単語 → 最低15音素以上）
            var tokenCount = result.Split(' ').Length;
            Assert.True(tokenCount >= 15,
                $"8単語の文に対して音素トークン数({tokenCount})が少なすぎます");
        }

        // ===== T1: ObjectDisposedException テスト =====

        [Fact]
        public void Disposed_ToPhonemes_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemes("hello"));
        }

        [Fact]
        public void Disposed_ToPhonemeList_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeList("hello"));
        }

        [Fact]
        public void Disposed_LookupWord_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.LookupWord("hello"));
        }

        [Fact]
        public void Disposed_LookupAllPronunciations_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.LookupAllPronunciations("hello"));
        }

        [Fact]
        public void Disposed_ContainsWord_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ContainsWord("hello"));
        }

        [Fact]
        public void DoubleDispose_DoesNotThrow()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            engine.Dispose(); // 二重Disposeは例外なし
        }

        // ===== T3: CmuDictionary.LoadFromFile テスト =====

        [Fact]
        public void LoadFromFile_Null_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CmuDictionary.LoadFromFile(null!));
        }

        [Fact]
        public void LoadFromFile_EmptyString_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CmuDictionary.LoadFromFile(""));
        }

        [Fact]
        public void LoadFromFile_NonexistentFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => CmuDictionary.LoadFromFile("nonexistent_dict_file.dict"));
        }

        // ===== T4: スレッドセーフティテスト =====

        [Fact]
        public async Task ToPhonemes_ConcurrentAccess_AllReturnSameResult()
        {
            const int threadCount = 10;
            var results = new string[threadCount];
            var barrier = new Barrier(threadCount);

            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                barrier.SignalAndWait();
                results[i] = _engine.ToPhonemes("hello");
            })).ToArray();

            await Task.WhenAll(tasks);

            var expected = "HH AH0 L OW1";
            for (var i = 0; i < threadCount; i++)
            {
                Assert.Equal(expected, results[i]);
            }
        }

        // ===== T5: EnglishG2POptions全組み合わせテスト =====

        [Fact]
        public void Options_NoStress_Skip_NoLts_DictWord_ReturnsWithoutStress()
        {
            var options = new EnglishG2POptions(includeStress: false, unknownWordHandling: UnknownWordStrategy.Skip, enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("hello");
                Assert.NotEmpty(result);
                Assert.DoesNotMatch(@"\d", result);
            }
        }

        [Fact]
        public void Options_NoStress_Throw_LtsEnabled_DictWord_ReturnsWithoutStress()
        {
            var options = new EnglishG2POptions(includeStress: false, unknownWordHandling: UnknownWordStrategy.Throw, enableLts: true);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("hello");
                Assert.NotEmpty(result);
                Assert.DoesNotMatch(@"\d", result);
            }
        }

        [Fact]
        public void Options_NoStress_Throw_NoLts_OovWord_ThrowsException()
        {
            var options = new EnglishG2POptions(includeStress: false, unknownWordHandling: UnknownWordStrategy.Throw, enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("xyzzyplugh"));
            }
        }

        // ===== T6: LookupAllPronunciations専用テスト =====

        [Fact]
        public void LookupAllPronunciations_EmptyString_ReturnsEmpty()
        {
            var result = _engine.LookupAllPronunciations("");
            Assert.Empty(result);
        }

        [Fact]
        public void LookupAllPronunciations_Null_ReturnsEmpty()
        {
            var result = _engine.LookupAllPronunciations(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void LookupAllPronunciations_Lead_ReturnsMultipleVariants()
        {
            var result = _engine.LookupAllPronunciations("lead");
            Assert.True(result.Count >= 2, $"leadは2バリアント以上を期待: 実際={result.Count}");
        }

        [Fact]
        public void LookupAllPronunciations_UnknownWord_ReturnsEmpty()
        {
            // LookupAllPronunciationsはLTSフォールバックなし
            var result = _engine.LookupAllPronunciations("xyzzy");
            Assert.Empty(result);
        }

        // ===== T7: IncludeStress=false辞書語テスト =====

        [Fact]
        public void ToPhonemes_IncludeStressFalse_Hello_ReturnsWithoutStressNumbers()
        {
            var options = new EnglishG2POptions(includeStress: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("hello");
                // ストレス番号なし: "HH AH L OW"
                Assert.Equal("HH AH L OW", result);
            }
        }
    }
}
