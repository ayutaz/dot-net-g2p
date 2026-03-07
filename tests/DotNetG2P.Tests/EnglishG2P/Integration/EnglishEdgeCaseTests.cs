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
    /// EnglishG2PEngine 包括的エッジケーステスト。
    /// 空入力・大文字小文字・句読点・特殊文字・OOV・長文・正規化連携・同綴異音語を検証する。
    /// </summary>
    public class EnglishEdgeCaseTests : IDisposable
    {
        private readonly EnglishG2PEngine _engine;

        public EnglishEdgeCaseTests()
        {
            _engine = new EnglishG2PEngine();
        }

        public void Dispose() => _engine.Dispose();

        // ===== 1. 空入力・null近似 (5件) =====

        [Fact]
        public void ToPhonemes_TabAndNewlineOnly_ReturnsEmpty()
        {
            Assert.Equal("", _engine.ToPhonemes("\t\n"));
        }

        [Fact]
        public void ToPhonemes_PunctuationOnly_ReturnsEmpty()
        {
            // 句読点・記号のみ → IsWordCharにマッチせず空
            Assert.Equal("", _engine.ToPhonemes("!@#$%"));
        }

        [Fact]
        public void ToPhonemeList_PunctuationOnly_ReturnsEmpty()
        {
            Assert.Empty(_engine.ToPhonemeList("!@#$%"));
        }

        [Fact]
        public void ToPhonemes_DigitsOnly_ExpandedByNormalization()
        {
            // "12345" → 正規化で "twelve thousand three hundred forty five" に展開
            var result = _engine.ToPhonemes("12345");
            Assert.Equal(
                "T W EH1 L V TH AW1 Z AH0 N D TH R IY1 HH AH1 N D R AH0 D F AO1 R T IY0 F AY1 V",
                result);
        }

        [Fact]
        public void ToPhonemes_SimpleNumber_ExpandedToWords()
        {
            // "42" → "forty two"
            var result = _engine.ToPhonemes("42");
            Assert.Equal("F AO1 R T IY0 T UW1", result);
        }

        // ===== 2. 大文字小文字 (3件) =====

        [Fact]
        public void ToPhonemes_AllUpperCase_SameAsLowerCase()
        {
            var upper = _engine.ToPhonemes("HELLO");
            var lower = _engine.ToPhonemes("hello");
            Assert.Equal(lower, upper);
        }

        [Fact]
        public void ToPhonemes_MixedCase_SameAsLowerCase()
        {
            var mixed = _engine.ToPhonemes("HeLLo");
            var lower = _engine.ToPhonemes("hello");
            Assert.Equal(lower, mixed);
        }

        [Fact]
        public void ContainsWord_CaseInsensitive()
        {
            Assert.True(_engine.ContainsWord("hello"));
            Assert.True(_engine.ContainsWord("HELLO"));
            Assert.True(_engine.ContainsWord("Hello"));
        }

        // ===== 3. 句読点・記号 (5件) =====

        [Fact]
        public void ToPhonemes_TrailingPeriod_SameAsWithout()
        {
            var withPeriod = _engine.ToPhonemes("hello.");
            var without = _engine.ToPhonemes("hello");
            Assert.Equal(without, withPeriod);
        }

        [Fact]
        public void ToPhonemes_CommaSeparated_SameAsPlainWords()
        {
            var withComma = _engine.ToPhonemes("hello, world");
            var plain = _engine.ToPhonemes("hello world");
            Assert.Equal(plain, withComma);
        }

        [Fact]
        public void ToPhonemes_ExclamationMark_PunctuationRemoved()
        {
            var result = _engine.ToPhonemes("hello!");
            Assert.Equal("HH AH0 L OW1", result);
        }

        [Fact]
        public void ToPhonemes_QuestionMark_PunctuationRemoved()
        {
            var result = _engine.ToPhonemes("hello?");
            Assert.Equal("HH AH0 L OW1", result);
        }

        [Fact]
        public void ToPhonemes_Hyphenated_SplitsIntoWords()
        {
            // "well-known" → ハイフンはIsWordCharでないため分割される
            var result = _engine.ToPhonemes("well-known");
            Assert.Equal("W EH1 L N OW1 N", result);
        }

        [Fact]
        public void ToPhonemes_Apostrophe_TreatedAsOneWord()
        {
            // "don't" → アポストロフィは単語内文字として扱われる
            var result = _engine.ToPhonemes("don't");
            Assert.Equal("D OW1 N T", result);
        }

        // ===== 4. 特殊文字・Unicode (4件) =====

        [Fact]
        public void ToPhonemes_JapaneseMixed_SkipsNonAscii()
        {
            // 日本語文字はIsWordCharでマッチせずスキップされる
            var result = _engine.ToPhonemes("hello こんにちは world");
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        [Fact]
        public void ToPhonemes_FullWidthChars_NormalizedToHalfWidth()
        {
            // 全角英字は正規化で半角に変換され、通常どおり音素変換される
            var result = _engine.ToPhonemes("\uFF28\uFF25\uFF2C\uFF2C\uFF2F"); // ＨＥＬＬＯ
            Assert.Equal("HH AH0 L OW1", result);
        }

        [Fact]
        public void ToPhonemes_AccentedChar_PartialMatch()
        {
            // "café" → 'é' はIsWordCharでないため "caf" で切れる
            // "caf" はOOVだがLTS有効時は推定される
            var result = _engine.ToPhonemes("café");
            Assert.Equal("K AE1 F", result);
        }

        [Fact]
        public void ToPhonemes_EmojiMixed_SkipsEmoji()
        {
            // 絵文字はIsWordCharでない
            // サロゲートペアを含む絵文字: Tokenizeでスキップされる
            var result = _engine.ToPhonemes("hello world");
            // 絵文字なしと同じ結果
            Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
        }

        // ===== 5. OOV処理 (4件) =====

        [Fact]
        public void ToPhonemes_OovWithLts_ReturnsLtsEstimation()
        {
            // LTS有効時は辞書にない単語もスペルから推定
            var result = _engine.ToPhonemes("blurfington");
            Assert.Equal("B L ER1 F IH0 NG T AH0 N", result);
        }

        [Fact]
        public void ToPhonemes_OovNoLtsSkip_ReturnsEmpty()
        {
            var options = new EnglishG2POptions(enableLts: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("xyzzqk");
                Assert.Equal("", result);
            }
        }

        [Fact]
        public void ToPhonemes_OovNoLtsThrow_ThrowsKeyNotFoundException()
        {
            var options = new EnglishG2POptions(
                enableLts: false,
                unknownWordHandling: UnknownWordStrategy.Throw);
            using (var engine = new EnglishG2PEngine(options))
            {
                Assert.Throws<KeyNotFoundException>(() => engine.ToPhonemes("xyzzqk"));
            }
        }

        [Fact]
        public void LookupWord_OovWithLts_ReturnsEstimation()
        {
            // LookupWordもLTSフォールバックが効く
            var result = _engine.LookupWord("blurfington");
            Assert.NotEmpty(result);
        }

        // ===== 6. 長文・大量入力 (3件) =====

        [Fact]
        public void ToPhonemes_100Words_ProcessesAll()
        {
            var words = Enumerable.Range(0, 100)
                .Select(i => i % 2 == 0 ? "hello" : "world");
            var text = string.Join(" ", words);

            var result = _engine.ToPhonemes(text);
            var tokenCount = result.Split(' ').Length;

            // 100単語 × 4音素/単語 = 400音素トークン
            Assert.Equal(400, tokenCount);
        }

        [Fact]
        public void ToPhonemes_RepeatedWord_ConsistentResult()
        {
            var singleResult = _engine.ToPhonemes("hello");
            var repeatedText = string.Join(" ", Enumerable.Repeat("hello", 50));
            var repeatedResult = _engine.ToPhonemes(repeatedText);

            // 各単語が同じ音素列に変換されることを確認
            var expected = string.Join(" ", Enumerable.Repeat(singleResult, 50));
            Assert.Equal(expected, repeatedResult);
        }

        [Fact]
        public void ToPhonemes_VeryLongOovWord_LtsHandles()
        {
            // 非常に長い造語でもLTSがクラッシュしない
            var result = _engine.ToPhonemes("supercalifragilisticexpialidocious");
            Assert.NotEmpty(result);
        }

        // ===== 7. 正規化との連携 (3件) =====

        [Fact]
        public void ToPhonemes_NumberInSentence_ExpandedToWords()
        {
            // "I have 3 cats" → "I have three cats"
            var result = _engine.ToPhonemes("I have 3 cats");
            var expected = _engine.ToPhonemes("I have three cats");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToPhonemes_Currency_ExpandedToWords()
        {
            // "$100" → "one hundred dollars"
            var result = _engine.ToPhonemes("$100");
            Assert.Equal("W AH1 N HH AH1 N D R AH0 D D AA1 L ER0 Z", result);
        }

        [Fact]
        public void ToPhonemes_Abbreviation_ExpandedToWords()
        {
            // "Dr. Smith" → "doctor Smith"
            var result = _engine.ToPhonemes("Dr. Smith");
            Assert.Equal("D AA1 K T ER0 S M IH1 TH", result);
        }

        // ===== 8. 同綴異音語との連携 (3件) =====

        [Fact]
        public void ToPhonemes_Homograph_RecordVerbAndNoun()
        {
            // "I will record the record" → 動詞 record (rɪˈkɔːrd) と名詞 record (ˈrekɔːrd)
            var result = _engine.ToPhonemes("I will record the record");
            // 動詞: R AH0 K AO1 R D, 名詞: R EH1 K ER0 D
            Assert.Equal("AY1 W IH1 L R AH0 K AO1 R D DH AH0 R EH1 K ER0 D", result);
        }

        [Fact]
        public void ToPhonemes_HomographDisabled_UsesFirstVariant()
        {
            var options = new EnglishG2POptions(enableHomographResolution: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                var result = engine.ToPhonemes("I will record the record");
                // 同綴異音語解決なし → 両方とも最初のバリアント
                Assert.Equal("AY1 W IH1 L R AH0 K AO1 R D DH AH0 R AH0 K AO1 R D", result);
            }
        }

        [Fact]
        public void ToPhonemes_HomographWithNormalization_ReadAndRecords()
        {
            // "He read 3 records" → 正規化で "He read three records"
            // read は文脈で過去形（R IY1 D）
            var result = _engine.ToPhonemes("He read 3 records");
            Assert.Equal("HH IY1 R IY1 D TH R IY1 R AH0 K AO1 R D Z", result);
        }

        // ===== 追加: ContainsWord エッジケース =====

        [Fact]
        public void ContainsWord_EmptyString_ReturnsFalse()
        {
            Assert.False(_engine.ContainsWord(""));
        }

        [Fact]
        public void ContainsWord_OovWord_ReturnsFalse()
        {
            Assert.False(_engine.ContainsWord("xyzzqk"));
        }

        // ===== 追加: 正規化無効時の動作 =====

        [Fact]
        public void ToPhonemes_NormalizationDisabled_NumbersNotExpanded()
        {
            var options = new EnglishG2POptions(enableNormalization: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                // 正規化なしの場合、"42" はTokenizeで数字が除去される
                // (IsWordCharは英字とアポストロフィのみ)
                var result = engine.ToPhonemes("42");
                Assert.Equal("", result);
            }
        }

        [Fact]
        public void ToPhonemes_NormalizationDisabled_AbbreviationNotExpanded()
        {
            var options = new EnglishG2POptions(enableNormalization: false);
            using (var engine = new EnglishG2PEngine(options))
            {
                // 正規化なしの場合、"Dr." は "Dr" として辞書検索
                // ピリオドはIsWordCharなのでトークンに含まれ、TrimEndで除去される
                var result = engine.ToPhonemes("Dr. Smith");
                Assert.NotEmpty(result);
                Assert.Contains("S M IH1 TH", result); // "Smith" は変換される
            }
        }

        // ===== 並行テスト =====

        [Fact]
        public void ConcurrentDispose_NoException()
        {
            // 複数スレッドから同時にDispose()を呼び出しても例外が発生しないこと
            var engine = new EnglishG2PEngine();
            var tasks = new Task[10];
            var barrier = new Barrier(10);

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    var ex = Record.Exception(() => engine.Dispose());
                    Assert.Null(ex);
                });
            }

            Task.WaitAll(tasks);
        }

        [Fact]
        public void ConcurrentAccess_NoException()
        {
            // 複数スレッドから同時にToPhonemes()を呼び出しても例外が発生しないこと
            // （辞書は読み取り専用のためスレッドセーフ）
            var tasks = new Task[8];
            var barrier = new Barrier(8);

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < 50; j++)
                    {
                        var result = _engine.ToPhonemes("hello world");
                        Assert.Equal("HH AH0 L OW1 W ER1 L D", result);
                    }
                });
            }

            Task.WaitAll(tasks);
        }
    }
}
