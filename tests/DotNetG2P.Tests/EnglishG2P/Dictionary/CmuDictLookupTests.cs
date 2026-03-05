using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Dictionary
{
    /// <summary>
    /// CmuDictionary の基本ルックアップテスト。
    /// </summary>
    public class CmuDictLookupTests
    {
        private static readonly CmuDictionary Dict = CmuDictionary.LoadEmbedded();

        // ===== 既知語のルックアップ =====

        [Fact]
        public void TryLookup_Hello_ReturnsCorrectPhonemes()
        {
            Assert.True(Dict.TryLookup("hello", out var prons));
            Assert.Equal("HH AH0 L OW1", prons[0].ToString());
        }

        [Fact]
        public void TryLookup_World_ReturnsCorrectPhonemes()
        {
            Assert.True(Dict.TryLookup("world", out var prons));
            Assert.Equal("W ER1 L D", prons[0].ToString());
        }

        [Fact]
        public void TryLookup_The_ReturnsCorrectPhonemes()
        {
            Assert.True(Dict.TryLookup("the", out var prons));
            Assert.Equal("DH AH0", prons[0].ToString());
        }

        [Fact]
        public void TryLookup_Computer_ReturnsCorrectPhonemes()
        {
            Assert.True(Dict.TryLookup("computer", out var prons));
            Assert.Equal("K AH0 M P Y UW1 T ER0", prons[0].ToString());
        }

        [Fact]
        public void TryLookup_Beautiful_ReturnsCorrectPhonemes()
        {
            Assert.True(Dict.TryLookup("beautiful", out var prons));
            Assert.Equal("B Y UW1 T AH0 F AH0 L", prons[0].ToString());
        }

        // ===== 大文字小文字の無視 =====

        [Theory]
        [InlineData("Hello")]
        [InlineData("HELLO")]
        [InlineData("hello")]
        [InlineData("hElLo")]
        public void TryLookup_CaseInsensitive_ReturnsSameResult(string word)
        {
            Assert.True(Dict.TryLookup(word, out var prons));
            Assert.Equal("HH AH0 L OW1", prons[0].ToString());
        }

        // ===== 存在しない単語 =====

        [Fact]
        public void TryLookup_NonexistentWord_ReturnsFalse()
        {
            Assert.False(Dict.TryLookup("xyzzyplugh", out _));
        }

        [Fact]
        public void TryLookup_GibberishWord_ReturnsFalse()
        {
            Assert.False(Dict.TryLookup("qqqqq", out _));
        }

        // ===== ContainsWord =====

        [Fact]
        public void ContainsWord_ExistingWord_ReturnsTrue()
        {
            Assert.True(Dict.ContainsWord("hello"));
        }

        [Fact]
        public void ContainsWord_NonexistentWord_ReturnsFalse()
        {
            Assert.False(Dict.ContainsWord("xyzzyplugh"));
        }

        [Fact]
        public void ContainsWord_CaseInsensitive_ReturnsTrue()
        {
            Assert.True(Dict.ContainsWord("HeLLo"));
        }

        // ===== アポストロフィ付き語 =====

        [Fact]
        public void TryLookup_Dont_ReturnsPhonemes()
        {
            Assert.True(Dict.TryLookup("don't", out var prons));
            Assert.NotEmpty(prons);
        }

        [Fact]
        public void TryLookup_Cant_ReturnsPhonemes()
        {
            Assert.True(Dict.TryLookup("can't", out var prons));
            Assert.NotEmpty(prons);
        }

        // ===== ストレスマーカーの正確性 =====

        [Fact]
        public void TryLookup_StressMarkers_AreCorrect()
        {
            Assert.True(Dict.TryLookup("hello", out var prons));
            var phonemes = prons[0].Phonemes;

            // HH = 子音、ストレスなし
            Assert.Equal(ArpabetPhoneme.HH, phonemes[0].Phoneme);
            Assert.Equal(Stress.None, phonemes[0].Stress);

            // AH0 = 母音、NoStress
            Assert.Equal(ArpabetPhoneme.AH, phonemes[1].Phoneme);
            Assert.Equal(Stress.NoStress, phonemes[1].Stress);

            // L = 子音、ストレスなし
            Assert.Equal(ArpabetPhoneme.L, phonemes[2].Phoneme);
            Assert.Equal(Stress.None, phonemes[2].Stress);

            // OW1 = 母音、Primary
            Assert.Equal(ArpabetPhoneme.OW, phonemes[3].Phoneme);
            Assert.Equal(Stress.Primary, phonemes[3].Stress);
        }

        [Fact]
        public void TryLookup_SecondaryStress_IsCorrectlyParsed()
        {
            // "about" は AH0 B AW1 T で Secondary ストレスを含まないが、
            // "autobiography" は Secondary を含む
            Assert.True(Dict.TryLookup("autobiography", out var prons));
            var phonemes = prons[0].Phonemes;

            // 少なくとも1つの Secondary ストレスを持つ
            var hasSecondary = false;
            foreach (var p in phonemes)
            {
                if (p.Stress == Stress.Secondary)
                {
                    hasSecondary = true;
                    break;
                }
            }
            Assert.True(hasSecondary, "autobiographyにSecondaryストレスが含まれるべき");
        }

        // ===== 辞書エントリ数 =====

        [Fact]
        public void Count_IsOver100000()
        {
            Assert.True(Dict.Count > 100000, $"辞書エントリ数が10万件未満です: {Dict.Count}");
        }

        // ===== 空文字列・null =====

        [Fact]
        public void TryLookup_EmptyString_ReturnsFalse()
        {
            // 空文字列は ToUpperInvariant() しても空文字列なので辞書に一致しないはず
            Assert.False(Dict.TryLookup("", out _));
        }

        [Fact]
        public void TryLookup_Null_ReturnsFalse()
        {
            Assert.False(Dict.TryLookup(null!, out _));
        }

        [Fact]
        public void ContainsWord_Null_ReturnsFalse()
        {
            Assert.False(Dict.ContainsWord(null!));
        }
    }
}
