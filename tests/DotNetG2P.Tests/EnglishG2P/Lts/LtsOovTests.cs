using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DotNetG2P.English;
using DotNetG2P.English.LTS;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Lts
{
    /// <summary>
    /// OOV（辞書未登録語）に対するLTSフォールバックテスト。
    /// LtsEngine単体およびEnglishG2PEngine経由の統合テストを含む。
    /// </summary>
    public class LtsOovTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public LtsOovTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // ===== LtsEngine直接呼び出しによるOOV単語テスト =====

        [Theory]
        [InlineData("blurfington")]
        [InlineData("unmicrowaveable")]
        [InlineData("chatgpt")]
        [InlineData("snorkelwax")]
        [InlineData("flimflammer")]
        [InlineData("quizzify")]
        [InlineData("blobfish")]
        [InlineData("splinterkeg")]
        public void Predict_FabricatedWords_ReturnsNonNull(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
        }

        [Theory]
        [InlineData("googleapis")]
        [InlineData("kubernetes")]
        [InlineData("tensorflow")]
        [InlineData("stackoverflow")]
        [InlineData("github")]
        public void Predict_TechTerms_ReturnsNonNull(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
            // A2: 音素数の下限チェック（単語長の1/4以上の音素が返ること）
            var minExpected = word.Length / 4;
            Assert.True(result!.Length >= minExpected,
                $"'{word}' (長さ{word.Length}) は最低{minExpected}音素以上返すべき（実際: {result.Length}）");
        }

        [Theory]
        [InlineData("googled")]
        [InlineData("tweeted")]
        [InlineData("youtubed")]
        [InlineData("ubered")]
        [InlineData("venmod")]
        public void Predict_Neologisms_ReturnsNonNull(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
        }

        // ===== OOV単語の結果の品質検証 =====

        [Theory]
        [InlineData("blurfington")]
        [InlineData("googleapis")]
        [InlineData("tensorflow")]
        public void Predict_OovWords_ContainVowels(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        [Theory]
        [InlineData("blurfington")]
        [InlineData("unmicrowaveable")]
        public void Predict_OovWords_AllPhonemesValid(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);

            foreach (var phoneme in result!)
            {
                Assert.True(Enum.IsDefined(typeof(ArpabetPhoneme), phoneme.Phoneme),
                    $"無効なArpabetPhoneme: {phoneme.Phoneme} (word='{word}')");
                Assert.True(Enum.IsDefined(typeof(Stress), phoneme.Stress),
                    $"無効なStress: {phoneme.Stress} (word='{word}')");
            }
        }

        // ===== EnglishG2PEngine統合: EnableLts=true（デフォルト） =====

        [Theory]
        [InlineData("blurfington")]
        [InlineData("snorkelwax")]
        [InlineData("quizzify")]
        public void ToPhonemes_EnableLtsTrue_OovReturnsNonEmpty(string oovWord)
        {
            // デフォルトはEnableLts=true
            var result = _engine.ToPhonemes(oovWord);
            Assert.NotEmpty(result);
        }

        [Theory]
        [InlineData("blurfington")]
        [InlineData("snorkelwax")]
        public void ToPhonemeList_EnableLtsTrue_OovReturnsNonEmpty(string oovWord)
        {
            var result = _engine.ToPhonemeList(oovWord);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void LookupWord_EnableLtsTrue_OovReturnsNonEmpty()
        {
            var result = _engine.LookupWord("blurfington");
            Assert.NotEmpty(result);
        }

        // ===== EnglishG2PEngine統合: EnableLts=false =====

        [Fact]
        public void ToPhonemes_EnableLtsFalse_OovSkipped()
        {
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("blurfington");
                Assert.Equal("", result);
            }
        }

        [Fact]
        public void ToPhonemeList_EnableLtsFalse_OovReturnsEmpty()
        {
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemeList("blurfington");
                Assert.Empty(result);
            }
        }

        [Fact]
        public void ToPhonemes_EnableLtsFalse_Throw_OovThrowsException()
        {
            var options = new EnglishG2POptions(enableLts: false, unknownWordHandling: UnknownWordStrategy.Throw);
            using (var engine = new EnglishG2PEngine(options))
            {
                Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("blurfington"));
            }
        }

        [Fact]
        public void ToPhonemes_EnableLtsTrue_Throw_OovDoesNotThrow()
        {
            // LTSが有効ならOOVでも例外なし（LTSで解決されるため）
            var options = new EnglishG2POptions(enableLts: true, unknownWordHandling: UnknownWordStrategy.Throw);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("blurfington");
                Assert.NotEmpty(result);
            }
        }

        // ===== 辞書語+OOV語の混在テキスト =====

        [Fact]
        public void ToPhonemes_MixedKnownAndOov_BothProcessed()
        {
            // "hello" は辞書語、"blurfington" はOOV
            var result = _engine.ToPhonemes("Hello blurfington");
            Assert.NotEmpty(result);
            // 辞書語の音素（HH）が含まれていること
            Assert.Contains("HH", result);
            // A3: "Hello blurfington"の結果が"hello"単体（4音素）より長いことを検証
            var helloOnly = _engine.ToPhonemes("hello");
            var mixedTokenCount = result.Split(' ').Length;
            var helloTokenCount = helloOnly.Split(' ').Length;
            Assert.True(mixedTokenCount > helloTokenCount,
                $"混在テキストの音素数({mixedTokenCount})がhello単体({helloTokenCount})より多くあるべき");
        }

        [Fact]
        public void ToPhonemes_MixedKnownAndOov_LtsDisabled_OnlyKnownProcessed()
        {
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("Hello blurfington world");
                // 辞書語のみ処理される
                Assert.Contains("HH", result); // hello
                Assert.Contains("W ER1 L D", result); // world
            }
        }

        [Fact]
        public void ToPhonemeList_MixedText_ContainsMorePhonemes_WhenLtsEnabled()
        {
            var ltsEnabled = _engine.ToPhonemeList("Hello blurfington");

            var options = new EnglishG2POptions(enableLts: false);
            using (var noLtsEngine = new EnglishG2PEngine(options))
            {
                var ltsDisabled = noLtsEngine.ToPhonemeList("Hello blurfington");
                // LTS有効の方がOOV分の音素も含まれるため、より多い
                Assert.True(ltsEnabled.Count > ltsDisabled.Count,
                    $"LTS有効({ltsEnabled.Count}) > LTS無効({ltsDisabled.Count}) であるべき");
            }
        }

        // ===== IncludeStress=false時のLTS出力テスト =====

        [Fact]
        public void ToPhonemes_NoStress_OovWord_NoStressNumbers()
        {
            var options = new EnglishG2POptions(includeStress: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("blurfington");
                Assert.NotEmpty(result);
                // ストレス番号（0, 1, 2）が含まれないこと
                Assert.DoesNotMatch(@"\d", result);
            }
        }

        [Fact]
        public void ToPhonemes_WithStress_OovWord_ContainsStressNumbers()
        {
            var options = new EnglishG2POptions(includeStress: true);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("blurfington");
                Assert.NotEmpty(result);
                // 母音にストレス番号が付くはず
                Assert.Matches(@"\d", result);
            }
        }

        // ===== 長いOOV単語・特殊パターン =====

        [Fact]
        public void Predict_VeryLongWord_ReturnsResult()
        {
            var result = LtsEngine.Predict("supercalifragilisticexpialidocious");
            Assert.NotNull(result);
            Assert.True(result!.Length >= 10, $"長い単語は10音素以上返すべき（実際: {result.Length}）");
        }

        [Fact]
        public void Predict_RepeatedLetters_ReturnsResult()
        {
            var result = LtsEngine.Predict("aaaaaa");
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
        }

        [Fact]
        public void Predict_AllConsonants_ReturnsResult()
        {
            // 子音のみの造語
            var result = LtsEngine.Predict("bcd");
            // 結果が返るかどうか（母音なしでもツリーが何かを返す可能性）
            // nullでもOK（英語の発音ルールに従わないため）
            if (result != null)
            {
                foreach (var p in result)
                {
                    Assert.True(Enum.IsDefined(typeof(ArpabetPhoneme), p.Phoneme));
                }
            }
        }

        // ===== ハイフン語のLTS処理テスト =====

        [Theory]
        [InlineData("well-known")]
        [InlineData("self-driving")]
        [InlineData("twenty-one")]
        public void Predict_HyphenatedWords_ReturnsNull(string word)
        {
            // LtsEngine.Predictは英字以外の文字（ハイフン含む）を含む単語にはnullを返す
            var result = LtsEngine.Predict(word);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("well-known")]
        [InlineData("self-driving")]
        [InlineData("twenty-one")]
        public void ToPhonemes_HyphenatedWords_ProcessedViaTokenizer(string word)
        {
            // EnglishG2PEngineのTokenizerがハイフンで単語を分割し、各パーツを個別に処理する
            var result = _engine.ToPhonemes(word);
            // ハイフン語の各パーツが辞書に存在すればトークナイザ経由で処理されるため、結果は空でない
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemes_HyphenatedOovParts_ProcessedByLts()
        {
            // 辞書にない造語パーツを含むハイフン語でもLTS経由で処理される
            var result = _engine.ToPhonemes("blurfington-snorkelwax");
            Assert.NotEmpty(result);
        }

        // ===== Secondary stressの不存在確認テスト =====

        [Theory]
        [InlineData("blurfington")]
        [InlineData("unmicrowaveable")]
        [InlineData("googleapis")]
        [InlineData("tensorflow")]
        [InlineData("kubernetes")]
        [InlineData("stackoverflow")]
        [InlineData("supercalifragilisticexpialidocious")]
        public void Predict_NeverProducesSecondaryStress(string word)
        {
            // Flite LTSモデルはPrimary stress (1) と No stress (0) のみ出力し、
            // Secondary stress (2) は生成されない
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);

            foreach (var phoneme in result!)
            {
                Assert.NotEqual(Stress.Secondary, phoneme.Stress);
            }
        }

        [Fact]
        public void Predict_AllPhoneTableEntries_NoSecondaryStress()
        {
            // LtsPhoneMappingの全エントリにSecondary stressが含まれないことを確認
            var phoneToArpabet = LtsPhoneMapping.PhoneToArpabet;
            for (var i = 0; i < phoneToArpabet.Length; i++)
            {
                var mapped = phoneToArpabet[i];
                if (mapped == null) continue; // epsilon

                foreach (var phoneme in mapped)
                {
                    Assert.NotEqual(Stress.Secondary, phoneme.Stress);
                }
            }
        }

        // ===== バイナリモデル整合性テスト =====

        [Fact]
        public void LtsModel_BinarySize_MatchesExpectedNodeCount()
        {
            // LtsData.LetterIndexの最大ノードインデックス + 実際のツリーサイズから
            // cmu_lts_model.binのサイズが25505ノード * 6バイト = 153030バイトであることを検証
            var expectedNodeCount = 25505;
            var expectedSize = expectedNodeCount * LtsData.NodeSize; // 25505 * 6 = 153030

            var modelData = LtsData.LoadModelData();
            Assert.Equal(expectedSize, modelData.Length);
        }

        [Fact]
        public void LtsModel_NodeSize_IsConsistent()
        {
            // ノードサイズ定数が6バイトであること
            Assert.Equal(6, LtsData.NodeSize);
        }

        [Fact]
        public void LtsModel_LetterIndex_AllWithinRange()
        {
            // 全文字(a-z)のツリー開始ノードインデックスがモデルデータ範囲内であることを検証
            var modelData = LtsData.LoadModelData();
            var maxNodeIdx = modelData.Length / LtsData.NodeSize;

            Assert.Equal(26, LtsData.LetterIndex.Length);
            for (var i = 0; i < LtsData.LetterIndex.Length; i++)
            {
                Assert.True(LtsData.LetterIndex[i] < maxNodeIdx,
                    $"文字'{(char)('a' + i)}'のツリー開始インデックス({LtsData.LetterIndex[i]})がモデルの範囲外({maxNodeIdx})");
            }
        }

        [Fact]
        public void LtsModel_EmbeddedResourceExists()
        {
            // 埋め込みリソースが正しく読み込めることを検証
            var assembly = typeof(LtsData).Assembly;
            using (var stream = assembly.GetManifestResourceStream("DotNetG2P.English.LTS.cmu_lts_model.bin"))
            {
                Assert.NotNull(stream);
                Assert.True(stream!.Length > 0, "埋め込みリソースが空です");
            }
        }
    }
}
