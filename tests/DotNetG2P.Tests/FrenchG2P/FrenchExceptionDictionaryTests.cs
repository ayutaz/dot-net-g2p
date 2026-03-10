using DotNetG2P.French;
using DotNetG2P.French.Data;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// FrenchExceptionDictionary のユニットテスト。
    /// </summary>
    public class FrenchExceptionDictionaryTests
    {
        // ========== ロード検証 ==========

        [Fact]
        public void TryLookup_Football_ReturnsTrue()
        {
            var found = FrenchExceptionDictionary.TryLookup("football", FrenchDialect.Metropolitan, out var pron);
            Assert.True(found);
            Assert.NotNull(pron);
            Assert.True(pron.Phonemes.Count > 0);
        }

        [Fact]
        public void TryLookup_UnknownWord_ReturnsFalse()
        {
            var found = FrenchExceptionDictionary.TryLookup("xyzzyplugh", FrenchDialect.Metropolitan, out _);
            Assert.False(found);
        }

        [Fact]
        public void TryLookup_NullWord_ReturnsFalse()
        {
            var found = FrenchExceptionDictionary.TryLookup(null!, FrenchDialect.Metropolitan, out _);
            Assert.False(found);
        }

        // ========== 外来語 ==========

        [Fact]
        public void TryLookup_Weekend_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("weekend", FrenchDialect.Metropolitan, out var pron));
            // w i k|ɛ̃ d → 5 phonemes
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.W, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.I, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.K, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ENasal, pron.Phonemes[3].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.D, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Pizza_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("pizza", FrenchDialect.Metropolitan, out var pron));
            // p i d|z a → 5 phonemes
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.P, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.I, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.D, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Z, pron.Phonemes[3].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Sushi_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("sushi", FrenchDialect.Metropolitan, out var pron));
            // s u|ʃ i → 4 phonemes
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.S, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.U, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Sh, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.I, pron.Phonemes[3].Phoneme);
        }

        // ========== 不規則語 ==========

        [Fact]
        public void TryLookup_Monsieur_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("monsieur", FrenchDialect.Metropolitan, out var pron));
            // m ə|s j ø → 5 phonemes
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.M, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Schwa, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.S, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.J, pron.Phonemes[3].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Oe, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Femme_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("femme", FrenchDialect.Metropolitan, out var pron));
            // f a m → 3 phonemes
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.F, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.M, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Oignon_CorrectPronunciation()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("oignon", FrenchDialect.Metropolitan, out var pron));
            // ɔ|ɲ ɔ̃ → 3 phonemes
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.Oh, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Ny, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ONasal, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Fils_FinalSPronounced()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("fils", FrenchDialect.Metropolitan, out var pron));
            // f i s → 3 phonemes
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.F, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.I, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.S, pron.Phonemes[2].Phoneme);
        }

        // ========== 動詞3人称複数 ==========

        [Fact]
        public void TryLookup_Parlent_SilentEnt()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("parlent", FrenchDialect.Metropolitan, out var pron));
            // p a ʁ l → 4 phonemes (no -ent)
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.P, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.R, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.L, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_Chantent_SilentEnt()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("chantent", FrenchDialect.Metropolitan, out var pron));
            // ʃ ɑ̃ t → 3 phonemes
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.Sh, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ANasal, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.T, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Sont_Irregular3pl()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("sont", FrenchDialect.Metropolitan, out var pron));
            // s ɔ̃ → 2 phonemes
            Assert.Equal(2, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.S, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ONasal, pron.Phonemes[1].Phoneme);
        }

        // ========== 学術語 ==========

        [Fact]
        public void TryLookup_Bus_FinalSPronounced()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("bus", FrenchDialect.Metropolitan, out var pron));
            // b y s → 3 phonemes
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.B, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Y, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.S, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Album_FinalMPronounced()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("album", FrenchDialect.Metropolitan, out var pron));
            // a l|b ɔ m → 5 phonemes
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.L, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.B, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Oh, pron.Phonemes[3].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.M, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Fusil_SilentL()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("fusil", FrenchDialect.Metropolitan, out var pron));
            // f y|z i → 4 phonemes (no final -l)
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.F, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Y, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.Z, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.I, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_Tabac_SilentC()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("tabac", FrenchDialect.Metropolitan, out var pron));
            // t a|b a → 4 phonemes (no final -c)
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.T, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[1].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.B, pron.Phonemes[2].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, pron.Phonemes[3].Phoneme);
        }

        // ========== 方言 ==========

        [Fact]
        public void TryLookup_WildcardDialect_MatchesMetropolitan()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("football", FrenchDialect.Metropolitan, out var pron));
            Assert.NotNull(pron);
        }

        [Fact]
        public void TryLookup_WildcardDialect_MatchesConservative()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("football", FrenchDialect.Conservative, out var pron));
            Assert.NotNull(pron);
        }

        // ========== 音節核 ==========

        [Fact]
        public void TryLookup_SyllableNucleus_SetCorrectly()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("pizza", FrenchDialect.Metropolitan, out var pron));
            // p i d|z a → syllable 1: nucleus = i (index 1), syllable 2: nucleus = a (index 4)
            Assert.False(pron.Phonemes[0].IsSyllableNucleus); // p
            Assert.True(pron.Phonemes[1].IsSyllableNucleus);  // i = nucleus
            Assert.False(pron.Phonemes[2].IsSyllableNucleus); // d
            Assert.False(pron.Phonemes[3].IsSyllableNucleus); // z
            Assert.True(pron.Phonemes[4].IsSyllableNucleus);  // a = nucleus
        }

        // ========== StressIndex ==========

        [Fact]
        public void TryLookup_StressIndex_MinusOne()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("monsieur", FrenchDialect.Metropolitan, out var pron));
            Assert.Equal(-1, pron.StressedSyllableIndex);
        }

        // ========== 同綴異音語 ==========

        [Fact]
        public void TryLookup_Est_Homograph()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("est", FrenchDialect.Metropolitan, out var pron));
            // ɛ → 1 phoneme
            Assert.Equal(1, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.Eh, pron.Phonemes[0].Phoneme);
        }

        [Fact]
        public void TryLookup_Content_Homograph()
        {
            Assert.True(FrenchExceptionDictionary.TryLookup("content", FrenchDialect.Metropolitan, out var pron));
            // k ɔ̃|t ɑ̃ → 4 phonemes
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(FrenchIpaPhoneme.K, pron.Phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.ONasal, pron.Phonemes[1].Phoneme);
        }

        // ========== エントリ数検証 ==========

        [Fact]
        public void Dictionary_HasMinimum500Entries()
        {
            // 辞書が正常にロードされ、500語以上含むことを確認
            int count = 0;
            var testWords = new[]
            {
                "football", "weekend", "pizza", "monsieur", "femme", "parlent", "bus", "album",
                "est", "content", "campus", "virus", "tabac", "fusil", "sept", "chef"
            };
            foreach (var word in testWords)
            {
                if (FrenchExceptionDictionary.TryLookup(word, FrenchDialect.Metropolitan, out _))
                    count++;
            }
            Assert.Equal(testWords.Length, count);
        }
    }
}
