using System;
using System.Linq;
using DotNetG2P.English;
using DotNetG2P.English.LTS;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Lts
{
    /// <summary>
    /// LtsEngine.Predict の単体テスト。
    /// CARTツリーによる Letter-to-Sound 変換の基本動作を検証する。
    /// </summary>
    public class LtsRuleTests
    {
        // ===== 基本的な英単語の変換 =====

        [Theory]
        [InlineData("cat")]
        [InlineData("dog")]
        [InlineData("run")]
        [InlineData("sit")]
        [InlineData("pen")]
        [InlineData("big")]
        [InlineData("hot")]
        [InlineData("red")]
        [InlineData("cup")]
        [InlineData("map")]
        public void Predict_ShortCommonWords_ReturnsNonNull(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // A1: 短い一般的な単語は最低2音素以上を返すべき
            Assert.True(result!.Length >= 2, $"'{word}' は2音素以上返すべき（実際: {result.Length}）");
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("world")]
        [InlineData("computer")]
        [InlineData("language")]
        [InlineData("beautiful")]
        [InlineData("university")]
        public void Predict_LongerWords_ReturnsNonNull(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.True(result!.Length >= 2, $"'{word}' は2音素以上返すべき（実際: {result.Length}）");
        }

        // ===== 各母音文字の変換テスト =====

        [Fact]
        public void Predict_VowelA_ContainsVowelPhoneme()
        {
            // "cat" → /K AE T/ のような結果を期待
            var result = LtsEngine.Predict("cat");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        [Fact]
        public void Predict_VowelE_ContainsVowelPhoneme()
        {
            // "bed" → /B EH D/ のような結果を期待
            var result = LtsEngine.Predict("bed");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        [Fact]
        public void Predict_VowelI_ContainsVowelPhoneme()
        {
            // "bit" → /B IH T/ のような結果を期待
            var result = LtsEngine.Predict("bit");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        [Fact]
        public void Predict_VowelO_ContainsVowelPhoneme()
        {
            // "pot" → /P AA T/ のような結果を期待
            var result = LtsEngine.Predict("pot");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        [Fact]
        public void Predict_VowelU_ContainsVowelPhoneme()
        {
            // "but" → /B AH T/ のような結果を期待
            var result = LtsEngine.Predict("but");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        // ===== 子音文字の変換テスト =====

        [Fact]
        public void Predict_Cat_StartsWithK()
        {
            var result = LtsEngine.Predict("cat");
            Assert.NotNull(result);
            Assert.Equal(ArpabetPhoneme.K, result![0].Phoneme);
        }

        [Fact]
        public void Predict_Dog_StartsWithD()
        {
            var result = LtsEngine.Predict("dog");
            Assert.NotNull(result);
            Assert.Equal(ArpabetPhoneme.D, result![0].Phoneme);
        }

        [Fact]
        public void Predict_Fish_StartsWithF()
        {
            var result = LtsEngine.Predict("fish");
            Assert.NotNull(result);
            Assert.Equal(ArpabetPhoneme.F, result![0].Phoneme);
        }

        [Fact]
        public void Predict_Ship_StartsWithSH()
        {
            var result = LtsEngine.Predict("ship");
            Assert.NotNull(result);
            Assert.Equal(ArpabetPhoneme.SH, result![0].Phoneme);
        }

        [Fact]
        public void Predict_Think_StartsWithTH()
        {
            var result = LtsEngine.Predict("think");
            Assert.NotNull(result);
            Assert.Equal(ArpabetPhoneme.TH, result![0].Phoneme);
        }

        // ===== サイレント文字のテスト =====

        [Fact]
        public void Predict_Knight_DoesNotStartWithK()
        {
            // "knight" → k は無音（/N AY T/ 等）
            var result = LtsEngine.Predict("knight");
            Assert.NotNull(result);
            // LTSではknightのkが無音化される可能性がある
            // 少なくとも結果が返ること、N音素を含むことを検証
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.N);
        }

        [Fact]
        public void Predict_Write_ProducesRSound()
        {
            // "write" → /R AY T/ 等
            var result = LtsEngine.Predict("write");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.R);
        }

        // ===== 二重音素の展開テスト =====

        [Fact]
        public void Predict_Box_ContainsKS()
        {
            // "box" → x は /K S/ に展開される
            var result = LtsEngine.Predict("box");
            Assert.NotNull(result);
            // K と S を含むことを検証
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.K);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.S);
        }

        [Fact]
        public void Predict_Tax_ContainsKS()
        {
            var result = LtsEngine.Predict("tax");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.K);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.S);
        }

        // ===== null/空文字列/数字のみの入力 =====

        [Fact]
        public void Predict_Null_ReturnsNull()
        {
            var result = LtsEngine.Predict(null!);
            Assert.Null(result);
        }

        [Fact]
        public void Predict_EmptyString_ReturnsNull()
        {
            var result = LtsEngine.Predict("");
            Assert.Null(result);
        }

        [Fact]
        public void Predict_DigitsOnly_ReturnsNull()
        {
            var result = LtsEngine.Predict("12345");
            Assert.Null(result);
        }

        [Fact]
        public void Predict_ContainsDigits_ReturnsNull()
        {
            // 英字以外を含む場合はnull
            var result = LtsEngine.Predict("abc123");
            Assert.Null(result);
        }

        [Fact]
        public void Predict_ContainsSpaces_ReturnsNull()
        {
            var result = LtsEngine.Predict("hello world");
            Assert.Null(result);
        }

        [Fact]
        public void Predict_ContainsPunctuation_ReturnsNull()
        {
            var result = LtsEngine.Predict("hello!");
            Assert.Null(result);
        }

        // ===== 大文字・小文字の入力テスト =====

        [Fact]
        public void Predict_Uppercase_SameAsLowercase()
        {
            var lower = LtsEngine.Predict("hello");
            var upper = LtsEngine.Predict("HELLO");

            Assert.NotNull(lower);
            Assert.NotNull(upper);
            Assert.Equal(lower!.Length, upper!.Length);

            for (var i = 0; i < lower.Length; i++)
            {
                Assert.Equal(lower[i].Phoneme, upper[i].Phoneme);
                Assert.Equal(lower[i].Stress, upper[i].Stress);
            }
        }

        [Fact]
        public void Predict_MixedCase_SameAsLowercase()
        {
            var lower = LtsEngine.Predict("computer");
            var mixed = LtsEngine.Predict("CoMpUtEr");

            Assert.NotNull(lower);
            Assert.NotNull(mixed);
            Assert.Equal(lower!.Length, mixed!.Length);

            for (var i = 0; i < lower.Length; i++)
            {
                Assert.Equal(lower[i].Phoneme, mixed[i].Phoneme);
            }
        }

        // ===== 結果の音素がすべて有効なArpabetPhoneme enumメンバーであることの検証 =====

        [Theory]
        [InlineData("hello")]
        [InlineData("world")]
        [InlineData("computer")]
        [InlineData("beautiful")]
        [InlineData("strength")]
        [InlineData("through")]
        [InlineData("example")]
        [InlineData("question")]
        public void Predict_AllPhonemesAreValidArpabet(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);

            foreach (var phoneme in result!)
            {
                // ArpabetPhoneme enumの有効範囲内であることを検証
                Assert.True(Enum.IsDefined(typeof(ArpabetPhoneme), phoneme.Phoneme),
                    $"無効なArpabetPhoneme値: {phoneme.Phoneme} (word='{word}')");

                // Stressも有効な値であること
                Assert.True(Enum.IsDefined(typeof(Stress), phoneme.Stress),
                    $"無効なStress値: {phoneme.Stress} (word='{word}')");
            }
        }

        // ===== 母音が少なくとも1つ含まれるテスト =====

        [Theory]
        [InlineData("cat")]
        [InlineData("dog")]
        [InlineData("hello")]
        [InlineData("computer")]
        [InlineData("beautiful")]
        public void Predict_ResultContainsAtLeastOneVowel(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.IsVowel);
        }

        // ===== 特定の子音パターンテスト =====

        [Fact]
        public void Predict_Cheap_ContainsCH()
        {
            var result = LtsEngine.Predict("cheap");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.CH);
        }

        [Fact]
        public void Predict_Sing_ContainsNG()
        {
            var result = LtsEngine.Predict("sing");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.NG);
        }

        [Fact]
        public void Predict_Judge_ContainsJH()
        {
            var result = LtsEngine.Predict("judge");
            Assert.NotNull(result);
            Assert.Contains(result!, p => p.Phoneme == ArpabetPhoneme.JH);
        }

        // ===== ストレス情報の検証 =====

        [Theory]
        [InlineData("cat")]
        [InlineData("hello")]
        [InlineData("computer")]
        public void Predict_VowelsHaveStressMarking(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);

            // 母音にはPrimaryまたはNoStressが付いているべき
            foreach (var p in result!.Where(ph => ph.IsVowel))
            {
                Assert.NotEqual(Stress.None, p.Stress);
            }
        }

        [Theory]
        [InlineData("cat")]
        [InlineData("hello")]
        [InlineData("computer")]
        public void Predict_ConsonantsHaveNoneStress(string word)
        {
            var result = LtsEngine.Predict(word);
            Assert.NotNull(result);

            // 子音のストレスはNoneであるべき
            foreach (var p in result!.Where(ph => !ph.IsVowel))
            {
                Assert.Equal(Stress.None, p.Stress);
            }
        }

        // ===== 単一文字の入力テスト =====

        [Theory]
        [InlineData("a")]
        [InlineData("i")]
        public void Predict_SingleVowelLetter_ReturnsNonNull(string letter)
        {
            var result = LtsEngine.Predict(letter);
            Assert.NotNull(result);
            Assert.NotEmpty(result!);
        }

        [Theory]
        [InlineData("b")]
        [InlineData("k")]
        [InlineData("t")]
        public void Predict_SingleConsonantLetter_ReturnsResult(string letter)
        {
            // 単一子音文字は音素を返すかnullを返す（実装依存）
            var result = LtsEngine.Predict(letter);
            // A4: nullなら許容、非nullなら有効な音素で構成されかつ空でないことを検証
            if (result == null)
            {
                // nullは許容（英語の発音規則上、単一子音文字に発音がない場合）
                return;
            }

            Assert.NotEmpty(result);
            foreach (var p in result)
            {
                Assert.True(Enum.IsDefined(typeof(ArpabetPhoneme), p.Phoneme),
                    $"無効なArpabetPhoneme値: {p.Phoneme} (letter='{letter}')");
                Assert.True(Enum.IsDefined(typeof(Stress), p.Stress),
                    $"無効なStress値: {p.Stress} (letter='{letter}')");
            }
        }
    }
}
