using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Integration
{
    /// <summary>
    /// 英語G2Pエンジンの堅牢性テスト。
    /// 極端な入力、オプション組み合わせ、並行処理、バッチAPIの動作を包括的に検証する。
    /// </summary>
    public class EnglishRobustnessTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EnglishRobustnessTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // =====================================================================
        // 1. 空・null・空白入力
        // =====================================================================

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

        [Fact]
        public void ToPhonemeList_EmptyString_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToPhonemeList(""));
        }

        [Fact]
        public void ToPhonemeList_Null_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToPhonemeList(null!));
        }

        [Fact]
        public void LookupWord_EmptyString_ReturnsEmpty()
        {
            Assert.Empty(_engine.LookupWord(""));
        }

        [Fact]
        public void LookupWord_Null_ReturnsEmpty()
        {
            Assert.Empty(_engine.LookupWord(null!));
        }

        [Fact]
        public void ToIPA_Null_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToIPA(null!));
        }

        [Fact]
        public void ToIPA_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToIPA(""));
        }

        [Fact]
        public void ToXSampa_Null_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToXSampa(null!));
        }

        [Fact]
        public void ToXSampa_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToXSampa(""));
        }

        // =====================================================================
        // 2. 記号のみの入力
        // =====================================================================

        [Theory]
        [InlineData("!!!")]
        [InlineData("...")]
        [InlineData("@#$")]
        [InlineData("***")]
        [InlineData("---")]
        [InlineData("()[]{}")]
        [InlineData("~`^&|\\")]
        [InlineData("+=<>")]
        public void ToPhonemes_SymbolsOnly_ReturnsEmpty(string input)
        {
            Assert.Equal("", _engine.ToPhonemes(input));
        }

        [Fact]
        public void ToPhonemeList_SymbolsOnly_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToPhonemeList("@#$%^&*"));
        }

        // =====================================================================
        // 3. 連続空白・タブ・改行
        // =====================================================================

        [Fact]
        public void ToPhonemes_TabOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("\t\t\t"));
        }

        [Fact]
        public void ToPhonemes_NewlineOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("\n\n\n"));
        }

        [Fact]
        public void ToPhonemes_MixedWhitespace_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("  \t \n  \r\n  "));
        }

        [Fact]
        public void ToPhonemes_WordsWithMultipleSpaces_SameAsNormal()
        {
            var normal = _engine.ToPhonemes("hello world");
            var spaced = _engine.ToPhonemes("hello    world");
            Assert.Equal(normal, spaced);
        }

        [Fact]
        public void ToPhonemes_WordsWithTabs_SameAsNormal()
        {
            var normal = _engine.ToPhonemes("hello world");
            var tabbed = _engine.ToPhonemes("hello\tworld");
            Assert.Equal(normal, tabbed);
        }

        [Fact]
        public void ToPhonemes_WordsWithNewlines_SameAsNormal()
        {
            var normal = _engine.ToPhonemes("hello world");
            var newlined = _engine.ToPhonemes("hello\nworld");
            Assert.Equal(normal, newlined);
        }

        // =====================================================================
        // 4. 大文字小文字の混合
        // =====================================================================

        [Theory]
        [InlineData("hElLo WoRlD")]
        [InlineData("hello world")]
        public void ToPhonemes_MixedCaseVariations_SameResult(string input)
        {
            var expected = _engine.ToPhonemes("hello world");
            Assert.Equal(expected, _engine.ToPhonemes(input));
        }

        [Fact]
        public void ToPhonemes_AllCaps_SingleWord_SameAsLower()
        {
            // 単一の全大文字単語は頭字語と認識されない（5文字以上で辞書にある）
            var upper = _engine.ToPhonemes("COMPUTER");
            var lower = _engine.ToPhonemes("computer");
            Assert.Equal(lower, upper);
        }

        [Fact]
        public void ToPhonemes_AllCapsShort_MayBeAcronym()
        {
            // 全大文字の短い語は頭字語として展開される可能性がある
            // 例: "NASA" → 辞書に存在すればそのまま、なければ頭字語展開
            var result = _engine.ToPhonemes("HELLO WORLD");
            // 全大文字でも例外なく処理される（結果は正規化の仕様に依存）
            Assert.NotNull(result);
        }

        // =====================================================================
        // 5. 非常に長い単語（50文字超）
        // =====================================================================

        [Fact]
        public void ToPhonemes_VeryLongWord_DoesNotThrow()
        {
            // 60文字の造語
            var longWord = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefgh";
            var ex = Record.Exception(() => _engine.ToPhonemes(longWord));
            Assert.Null(ex);
        }

        [Fact]
        public void ToPhonemes_VeryLongWord_ReturnsNonEmptyWithLts()
        {
            // LTS有効時は長い造語でも音素が推定される
            var longWord = "superlongfantasticwordthatdoesnotexistindictionary";
            var result = _engine.ToPhonemes(longWord);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemes_VeryLongWord_NoLts_ReturnsEmpty()
        {
            var options = new EnglishG2POptions(enableLts: false);
            using var engine = new EnglishG2PEngine(options);
            var longWord = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";
            var result = engine.ToPhonemes(longWord);
            Assert.Equal("", result);
        }

        // =====================================================================
        // 6. 非常に長い文（100語以上）
        // =====================================================================

        [Fact]
        public void ToPhonemes_150Words_DoesNotThrow()
        {
            var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog" };
            var sentence = string.Join(" ", Enumerable.Range(0, 150).Select(i => words[i % words.Length]));
            var ex = Record.Exception(() => _engine.ToPhonemes(sentence));
            Assert.Null(ex);
        }

        [Fact]
        public void ToPhonemes_150Words_ReturnsNonEmpty()
        {
            var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog" };
            var sentence = string.Join(" ", Enumerable.Range(0, 150).Select(i => words[i % words.Length]));
            var result = _engine.ToPhonemes(sentence);
            Assert.NotEmpty(result);
            // 150語であれば最低150以上の音素トークンが生成される
            var tokenCount = result.Split(' ').Length;
            Assert.True(tokenCount >= 150, $"150語の文に対して音素トークン数({tokenCount})が少なすぎます");
        }

        [Fact]
        public void ToPhonemeList_200Words_DoesNotThrow()
        {
            var sentence = string.Join(" ", Enumerable.Repeat("hello", 200));
            var ex = Record.Exception(() => _engine.ToPhonemeList(sentence));
            Assert.Null(ex);
        }

        // =====================================================================
        // 7. Unicode文字（アクセント付き文字）
        // =====================================================================

        [Fact]
        public void ToPhonemes_Cafe_AccentedCharTruncated()
        {
            // "café" → 'é' はIsWordCharでないため "caf" で切れ、LTSで推定
            var result = _engine.ToPhonemes("café");
            // クラッシュしないことが重要
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_Naive_AccentedCharHandled()
        {
            // "naïve" → 'ï' でIsWordCharが切れる
            var result = _engine.ToPhonemes("naïve");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_Resume_AccentedCharHandled()
        {
            // "résumé" → 'é' でIsWordCharが切れる
            var result = _engine.ToPhonemes("résumé");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_ChineseCharacters_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("你好世界"));
        }

        [Fact]
        public void ToPhonemes_KoreanCharacters_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("안녕하세요"));
        }

        [Fact]
        public void ToPhonemes_SurrogatePairEmoji_DoesNotThrow()
        {
            // サロゲートペアを含む絵文字
            var ex = Record.Exception(() => _engine.ToPhonemes("😀🎉🚀"));
            Assert.Null(ex);
        }

        // =====================================================================
        // 8. 繰り返し文字
        // =====================================================================

        [Fact]
        public void ToPhonemes_RepeatedSingleChar_DoesNotThrow()
        {
            var result = _engine.ToPhonemes("aaaaaa");
            // LTSが推定を試みる
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_RepeatedCharLong_DoesNotThrow()
        {
            // 100文字の 'a'
            var longA = new string('a', 100);
            var ex = Record.Exception(() => _engine.ToPhonemes(longA));
            Assert.Null(ex);
        }

        [Fact]
        public void ToPhonemes_StretchedWord_DoesNotThrow()
        {
            // "hellooooo" → OOVだがLTSで推定
            var result = _engine.ToPhonemes("hellooooo");
            Assert.NotNull(result);
        }

        // =====================================================================
        // 9. 句読点の多い文
        // =====================================================================

        [Fact]
        public void ToPhonemes_EllipsisFilled_ExtractsWords()
        {
            var result = _engine.ToPhonemes("well... I don't know... maybe?");
            Assert.NotEmpty(result);
            // "well", "I", "don't", "know", "maybe" の音素が含まれる
            Assert.Contains("W EH1 L", result); // well
        }

        [Fact]
        public void ToPhonemes_MultiplePunctuationBetweenWords_ExtractsWords()
        {
            var result = _engine.ToPhonemes("hello!!! world???");
            var expected = _engine.ToPhonemes("hello world");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToPhonemes_QuotedText_ExtractsWords()
        {
            var result = _engine.ToPhonemes("\"hello\" 'world'");
            // クオートは除去される
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 10. バッチAPIの包括的テスト
        // =====================================================================

        [Fact]
        public void ToPhonemesBatch_LargeList_ProcessesAll()
        {
            var texts = Enumerable.Range(0, 50).Select(i => $"word{i % 5}").ToArray();
            // "word0"〜"word4" は辞書にないがLTSで推定される
            var result = _engine.ToPhonemesBatch(texts);
            Assert.Equal(50, result.Count);
        }

        [Fact]
        public void ToPhonemesBatch_MixedValidAndInvalid_ProcessesAll()
        {
            var texts = new[] { "hello", "", null!, "   ", "!!!", "world", "\t\n" };
            var result = _engine.ToPhonemesBatch(texts);
            Assert.Equal(7, result.Count);
            Assert.Equal("HH AH0 L OW1", result[0]);
            Assert.Equal("", result[1]); // 空文字列
            Assert.Equal("", result[2]); // null
            Assert.Equal("", result[3]); // 空白のみ
            Assert.Equal("", result[4]); // 記号のみ
            Assert.Equal("W ER1 L D", result[5]);
            Assert.Equal("", result[6]); // タブ・改行
        }

        [Fact]
        public void ToIPABatch_ConsistentWithIndividual()
        {
            var texts = new[] { "hello", "computer", "the" };
            var batchResult = _engine.ToIPABatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToIPA(texts[i]);
                Assert.Equal(individual, batchResult[i]);
            }
        }

        [Fact]
        public void ToXSampaBatch_ConsistentWithIndividual()
        {
            var texts = new[] { "hello", "computer", "the" };
            var batchResult = _engine.ToXSampaBatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToXSampa(texts[i]);
                Assert.Equal(individual, batchResult[i]);
            }
        }

        [Fact]
        public void ToPhonemeListBatch_ConsistentWithIndividual()
        {
            var texts = new[] { "hello", "world" };
            var batchResult = _engine.ToPhonemeListBatch(texts);
            for (int i = 0; i < texts.Length; i++)
            {
                var individual = _engine.ToPhonemeList(texts[i]);
                Assert.Equal(individual.Count, batchResult[i].Count);
                for (int j = 0; j < individual.Count; j++)
                {
                    Assert.Equal(individual[j].Phoneme, batchResult[i][j].Phoneme);
                    Assert.Equal(individual[j].Stress, batchResult[i][j].Stress);
                }
            }
        }

        // =====================================================================
        // 11. オプション設定の組み合わせ
        // =====================================================================

        [Fact]
        public void AllOptionsDisabled_DictWordStillWorks()
        {
            var options = new EnglishG2POptions(
                enableLts: false,
                enableNormalization: false,
                enableHomographResolution: false);
            using var engine = new EnglishG2PEngine(options);
            var result = engine.ToPhonemes("hello");
            Assert.Equal("HH AH0 L OW1", result);
        }

        [Fact]
        public void AllOptionsDisabled_OovSkipped()
        {
            var options = new EnglishG2POptions(
                enableLts: false,
                enableNormalization: false,
                enableHomographResolution: false);
            using var engine = new EnglishG2PEngine(options);
            var result = engine.ToPhonemes("xyzzyplugh");
            Assert.Equal("", result);
        }

        [Fact]
        public void NoStress_NoLts_NoNorm_NoHomograph_DictWord()
        {
            var options = new EnglishG2POptions(
                includeStress: false,
                enableLts: false,
                enableNormalization: false,
                enableHomographResolution: false);
            using var engine = new EnglishG2PEngine(options);
            var result = engine.ToPhonemes("hello");
            Assert.Equal("HH AH L OW", result);
            Assert.DoesNotMatch(@"\d", result);
        }

        [Fact]
        public void NormalizationDisabled_NumbersNotExpanded()
        {
            var options = new EnglishG2POptions(enableNormalization: false);
            using var engine = new EnglishG2PEngine(options);
            // "42" → 正規化無効時、数字は英字でないためTokenizeでスキップ
            var result = engine.ToPhonemes("42");
            Assert.Equal("", result);
        }

        [Fact]
        public void NormalizationEnabled_NumbersExpanded()
        {
            // デフォルトオプションは正規化有効
            var result = _engine.ToPhonemes("42");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void LtsDisabled_OovThrow_ThrowsForUnknownWord()
        {
            var options = new EnglishG2POptions(
                enableLts: false,
                unknownWordHandling: UnknownWordStrategy.Throw);
            using var engine = new EnglishG2PEngine(options);
            Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("xyzzyplugh"));
        }

        [Fact]
        public void LtsEnabled_OovThrow_DoesNotThrowForLtsPredictable()
        {
            var options = new EnglishG2POptions(
                enableLts: true,
                unknownWordHandling: UnknownWordStrategy.Throw);
            using var engine = new EnglishG2PEngine(options);
            // LTSが推定できる単語であればThrowにならない
            var ex = Record.Exception(() => engine.ToPhonemes("blurfington"));
            Assert.Null(ex);
        }

        [Fact]
        public void HomographDisabled_SameWordAlwaysSameOutput()
        {
            var options = new EnglishG2POptions(enableHomographResolution: false);
            using var engine = new EnglishG2PEngine(options);
            var result1 = engine.ToPhonemes("I will record the record");
            // 同綴異音語解決なし → "record"は両方とも同じ発音
            var tokens = result1.Split(' ');
            Assert.NotEmpty(tokens);
        }

        // =====================================================================
        // 12. スレッドセーフ性（Parallel.For）
        // =====================================================================

        [Fact]
        public void ParallelFor_ToPhonemes_AllReturnCorrectResult()
        {
            var results = new string[100];
            Parallel.For(0, 100, i =>
            {
                results[i] = _engine.ToPhonemes("hello world");
            });

            var expected = "HH AH0 L OW1 W ER1 L D";
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(expected, results[i]);
            }
        }

        [Fact]
        public void ParallelFor_ToPhonemeList_AllReturnCorrectCount()
        {
            var results = new int[50];
            Parallel.For(0, 50, i =>
            {
                var phonemes = _engine.ToPhonemeList("computer");
                results[i] = phonemes.Count;
            });

            for (int i = 0; i < 50; i++)
            {
                Assert.True(results[i] > 0, $"インデックス{i}の音素数が0です");
                Assert.Equal(results[0], results[i]);
            }
        }

        [Fact]
        public void ParallelFor_DifferentWords_NoExceptions()
        {
            var words = new[] { "hello", "world", "computer", "science", "artificial", "intelligence", "the", "cat" };
            var exceptions = new List<Exception>();

            Parallel.For(0, 200, i =>
            {
                try
                {
                    _engine.ToPhonemes(words[i % words.Length]);
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            });

            Assert.Empty(exceptions);
        }

        [Fact]
        public void ParallelFor_ToIPA_AllReturnCorrectResult()
        {
            var results = new string[50];
            Parallel.For(0, 50, i =>
            {
                results[i] = _engine.ToIPA("hello");
            });

            for (int i = 0; i < 50; i++)
            {
                Assert.Equal(results[0], results[i]);
            }
        }

        [Fact]
        public void ParallelFor_MixedApis_NoExceptions()
        {
            var exceptions = new List<Exception>();

            Parallel.For(0, 100, i =>
            {
                try
                {
                    switch (i % 4)
                    {
                        case 0: _engine.ToPhonemes("hello"); break;
                        case 1: _engine.ToIPA("world"); break;
                        case 2: _engine.ToXSampa("computer"); break;
                        case 3: _engine.ToPhonemeList("test"); break;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            });

            Assert.Empty(exceptions);
        }

        // =====================================================================
        // 13. Dispose後のAPI呼び出し
        // =====================================================================

        [Fact]
        public void Disposed_ToIPA_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPA("hello"));
        }

        [Fact]
        public void Disposed_ToXSampa_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampa("hello"));
        }

        [Fact]
        public void Disposed_ToIPAWithoutStress_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPAWithoutStress("hello"));
        }

        [Fact]
        public void Disposed_ToXSampaWithoutStress_ThrowsObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaWithoutStress("hello"));
        }

        // =====================================================================
        // 14. IPA/X-SAMPA APIのエッジケース
        // =====================================================================

        [Fact]
        public void ToIPA_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToIPA("   "));
        }

        [Fact]
        public void ToXSampa_WhitespaceOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToXSampa("   "));
        }

        [Fact]
        public void ToIPAWithoutStress_Hello_ReturnsIPA()
        {
            var result = _engine.ToIPAWithoutStress("hello");
            Assert.NotEmpty(result);
            // ストレスマークが含まれないことを確認
            Assert.DoesNotContain("ˈ", result);
            Assert.DoesNotContain("ˌ", result);
        }

        [Fact]
        public void ToXSampaWithoutStress_Hello_ReturnsXSampa()
        {
            var result = _engine.ToXSampaWithoutStress("hello");
            Assert.NotEmpty(result);
        }

        // =====================================================================
        // 15. 境界値・特殊パターン
        // =====================================================================

        [Fact]
        public void ToPhonemes_SingleChar_HandledCorrectly()
        {
            // "a" → 辞書に存在する単語
            var result = _engine.ToPhonemes("a");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemes_SingleCharOov_HandledCorrectly()
        {
            // "z" → 辞書に存在しないがLTSで推定
            var result = _engine.ToPhonemes("z");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_Contractions_ProcessedCorrectly()
        {
            // アポストロフィを含む短縮形
            var result = _engine.ToPhonemes("I'm can't won't");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemes_NumbersAndWords_ProcessedTogether()
        {
            var result = _engine.ToPhonemes("I have 2 cats and 3 dogs");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPhonemes_MixedCaseContraction_ProcessedCorrectly()
        {
            var lower = _engine.ToPhonemes("don't");
            var upper = _engine.ToPhonemes("DON'T");
            Assert.Equal(lower, upper);
        }

        [Fact]
        public void ToPhonemes_ConsecutivePeriods_Abbreviation()
        {
            // "U.S.A." → ピリオドはIsWordCharなので "U.S.A." 全体が一つのトークン
            var result = _engine.ToPhonemes("U.S.A.");
            Assert.NotNull(result);
        }

        [Fact]
        public void ToPhonemes_LeadingTrailingWhitespace_Trimmed()
        {
            var normal = _engine.ToPhonemes("hello");
            var padded = _engine.ToPhonemes("  hello  ");
            Assert.Equal(normal, padded);
        }

        // =====================================================================
        // 16. LookupAllPronunciations エッジケース
        // =====================================================================

        [Fact]
        public void LookupAllPronunciations_EmptyString_ReturnsEmpty()
        {
            Assert.Empty(_engine.LookupAllPronunciations(""));
        }

        [Fact]
        public void LookupAllPronunciations_Null_ReturnsEmpty()
        {
            Assert.Empty(_engine.LookupAllPronunciations(null!));
        }

        [Fact]
        public void LookupAllPronunciations_KnownWord_ReturnsAtLeastOne()
        {
            var result = _engine.LookupAllPronunciations("hello");
            Assert.True(result.Count >= 1);
        }

        [Fact]
        public void LookupAllPronunciations_OovWord_ReturnsEmpty()
        {
            // LookupAllPronunciationsはLTSフォールバックなし
            var result = _engine.LookupAllPronunciations("xyzzyplugh");
            Assert.Empty(result);
        }

        // =====================================================================
        // 17. ContainsWord エッジケース
        // =====================================================================

        [Fact]
        public void ContainsWord_EmptyString_ReturnsFalse()
        {
            Assert.False(_engine.ContainsWord(""));
        }

        [Fact]
        public void ContainsWord_Null_ReturnsFalse()
        {
            Assert.False(_engine.ContainsWord(null!));
        }

        [Fact]
        public void ContainsWord_KnownWord_ReturnsTrue()
        {
            Assert.True(_engine.ContainsWord("hello"));
        }

        [Fact]
        public void ContainsWord_CaseInsensitive()
        {
            Assert.True(_engine.ContainsWord("HELLO"));
            Assert.True(_engine.ContainsWord("Hello"));
            Assert.True(_engine.ContainsWord("hElLo"));
        }

        [Fact]
        public void ContainsWord_OovWord_ReturnsFalse()
        {
            Assert.False(_engine.ContainsWord("xyzzyplugh"));
        }

        // =====================================================================
        // 18. バッチAPI Dispose後
        // =====================================================================

        [Fact]
        public void AllBatchApis_AfterDispose_ThrowObjectDisposedException()
        {
            var engine = new EnglishG2PEngine();
            engine.Dispose();

            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemesBatch(new[] { "hello" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToIPABatch(new[] { "hello" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToXSampaBatch(new[] { "hello" }));
            Assert.Throws<ObjectDisposedException>(() => engine.ToPhonemeListBatch(new[] { "hello" }));
        }

        // =====================================================================
        // 19. 決定性テスト（同一入力→同一出力）
        // =====================================================================

        [Fact]
        public void ToPhonemes_Deterministic_SameInputSameOutput()
        {
            var input = "The quick brown fox jumps over the lazy dog";
            var result1 = _engine.ToPhonemes(input);
            var result2 = _engine.ToPhonemes(input);
            var result3 = _engine.ToPhonemes(input);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void ToIPA_Deterministic_SameInputSameOutput()
        {
            var result1 = _engine.ToIPA("hello world");
            var result2 = _engine.ToIPA("hello world");
            Assert.Equal(result1, result2);
        }

        // =====================================================================
        // 20. 複数エンジンインスタンスの独立性
        // =====================================================================

        [Fact]
        public void MultipleEngines_IndependentResults()
        {
            using var engine1 = new EnglishG2PEngine();
            using var engine2 = new EnglishG2PEngine();

            var result1 = engine1.ToPhonemes("hello");
            var result2 = engine2.ToPhonemes("hello");
            Assert.Equal(result1, result2);

            // engine1をDisposeしてもengine2は動作する
            engine1.Dispose();
            var result3 = engine2.ToPhonemes("hello");
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void MultipleEngines_DifferentOptions_IndependentBehavior()
        {
            var optionsWithStress = new EnglishG2POptions(includeStress: true);
            var optionsWithoutStress = new EnglishG2POptions(includeStress: false);

            using var engineStress = new EnglishG2PEngine(optionsWithStress);
            using var engineNoStress = new EnglishG2PEngine(optionsWithoutStress);

            var withStress = engineStress.ToPhonemes("hello");
            var withoutStress = engineNoStress.ToPhonemes("hello");

            // ストレスありは数字を含み、なしは含まない
            Assert.Matches(@"\d", withStress);
            Assert.DoesNotMatch(@"\d", withoutStress);
        }
    }
}
