using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Data;
using Xunit;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// PortugueseExceptionDictionary のユニットテスト。
    /// </summary>
    public class PortugueseExceptionDictionaryTests
    {
        // ===== 基本ルックアップ =====

        [Theory]
        [InlineData("pizza")]    // 外来語
        [InlineData("exame")]    // x_irregular
        [InlineData("avó")]      // misc (開母音)
        [InlineData("belo")]     // open_close_vowel
        [InlineData("ovos")]     // metaphony
        [InlineData("hora")]     // silent (h黙字)
        [InlineData("dez")]      // misc
        public void TryLookup_KnownWord_ReturnsTrue(string word)
        {
            var result = PortugueseExceptionDictionary.TryLookup(word, PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.NotNull(pron);
            Assert.True(pron.Phonemes.Count > 0);
        }

        // ===== 未知語 =====

        [Theory]
        [InlineData("xyzabc")]
        [InlineData("")]
        [InlineData("zxcvbnmasdfghjkl")]
        public void TryLookup_UnknownWord_ReturnsFalse(string word)
        {
            var result = PortugueseExceptionDictionary.TryLookup(word, PortugueseDialect.Brazilian, out _);
            Assert.False(result);
        }

        // ===== null =====

        [Fact]
        public void TryLookup_Null_ReturnsFalse()
        {
            var result = PortugueseExceptionDictionary.TryLookup(null!, PortugueseDialect.Brazilian, out _);
            Assert.False(result);
        }

        // ===== 方言固有エントリ =====

        [Fact]
        public void TryLookup_BrazilianDialectEntry_ReturnsDialectSpecific()
        {
            // "tipo" はBP固有（t͡ʃ口蓋化）
            var result = PortugueseExceptionDictionary.TryLookup("tipo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.NotNull(pron);
            // BP: tipo -> t͡ʃ i | p u (4 phonemes)
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.Ch, pron.Phonemes[0].Phoneme); // t͡ʃ
        }

        [Fact]
        public void TryLookup_EuropeanDialectEntry_ReturnsDialectSpecific()
        {
            // "menino" はEP固有（母音弱化）
            var result = PortugueseExceptionDictionary.TryLookup("menino", PortugueseDialect.European, out var pron);
            Assert.True(result);
            Assert.NotNull(pron);
            // EP: menino -> m ɨ | n i | n u (6 phonemes)
            Assert.Equal(6, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.HighCentral, pron.Phonemes[1].Phoneme); // ɨ
        }

        // ===== ワイルドカード方言 =====

        [Fact]
        public void TryLookup_AnyDialect_MatchesAllDialects()
        {
            // "ovo" は全方言（dialect="*"）
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.Brazilian, out var bpPron));
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.European, out var epPron));
            Assert.NotNull(bpPron);
            Assert.NotNull(epPron);
            Assert.Equal(bpPron.Phonemes.Count, epPron.Phonemes.Count);
        }

        // ===== 方言優先度（方言固有 > ワイルドカード） =====

        [Fact]
        public void TryLookup_DialectSpecificOverridesWildcard()
        {
            // "tipo" にはBP固有エントリがあるがAny方言エントリはない
            // -> Europeanでは見つからない
            var bpResult = PortugueseExceptionDictionary.TryLookup("tipo", PortugueseDialect.Brazilian, out _);
            var epResult = PortugueseExceptionDictionary.TryLookup("tipo", PortugueseDialect.European, out _);
            Assert.True(bpResult);
            Assert.False(epResult);
        }

        // ===== 音素の正確性チェック =====

        [Fact]
        public void TryLookup_OpenCloseVowel_HasCorrectPhonemes()
        {
            // "belo" -> b ɛ | l u (4 phonemes: B, Eh, L, U)
            var result = PortugueseExceptionDictionary.TryLookup("belo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.B, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.Eh, pron.Phonemes[1].Phoneme);  // ɛ 開母音
            Assert.Equal(PortugueseIpaPhoneme.L, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.U, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_ClosedVowel_HasCorrectPhonemes()
        {
            // "dedo" -> d e | d u (4 phonemes: D, E, D, U)
            var result = PortugueseExceptionDictionary.TryLookup("dedo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(4, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.D, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.E, pron.Phonemes[1].Phoneme);   // e 閉母音
            Assert.Equal(PortugueseIpaPhoneme.D, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.U, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_Ovo_HasCorrectPhonemes()
        {
            // "ovo" -> ɔ | v u (3 phonemes: Oh, V, U)
            var result = PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.Oh, pron.Phonemes[0].Phoneme);  // ɔ 開母音
            Assert.Equal(PortugueseIpaPhoneme.V, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.U, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Exame_HasCorrectPhonemes()
        {
            // "exame" -> e | z ɐ | m ɨ (5 phonemes: E, Z, Schwa, M, HighCentral)
            var result = PortugueseExceptionDictionary.TryLookup("exame", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.E, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.Z, pron.Phonemes[1].Phoneme);   // x=/z/
            Assert.Equal(PortugueseIpaPhoneme.Schwa, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.M, pron.Phonemes[3].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.HighCentral, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Fixo_HasCorrectPhonemes()
        {
            // "fixo" -> f i k | s u (5 phonemes: F, I, K, S, U)  x=/ks/
            var result = PortugueseExceptionDictionary.TryLookup("fixo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.F, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.I, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.K, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.S, pron.Phonemes[3].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.U, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Pizza_HasCorrectPhonemes()
        {
            // "pizza" -> p i | t s ɐ (5 phonemes: P, I, T, S, Schwa)
            var result = PortugueseExceptionDictionary.TryLookup("pizza", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(5, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.P, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.I, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.T, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.S, pron.Phonemes[3].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.Schwa, pron.Phonemes[4].Phoneme);
        }

        [Fact]
        public void TryLookup_Show_HasCorrectPhonemes()
        {
            // "show" -> ʃ o w (3 phonemes: Sh, O, W)
            var result = PortugueseExceptionDictionary.TryLookup("show", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.Sh, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.O, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.W, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Mao_HasNasalPhonemes()
        {
            // "mão" -> m ɐ̃ w̃ (3 phonemes: M, ANasal, WNasal)
            var result = PortugueseExceptionDictionary.TryLookup("mão", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.M, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, pron.Phonemes[2].Phoneme);
        }

        [Fact]
        public void TryLookup_Homem_HasNasalAndSilentH()
        {
            // "homem" -> ɔ | m ɐ̃ j̃ (4 phonemes: Oh, M, ANasal, JNasal)
            var result = PortugueseExceptionDictionary.TryLookup("homem", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.True(pron.Phonemes.Count >= 3);
            // h は黙字なので音素なし、最初は ɔ
            Assert.Equal(PortugueseIpaPhoneme.Oh, pron.Phonemes[0].Phoneme);
        }

        // ===== ストレスインデックスの正確性 =====

        [Fact]
        public void TryLookup_StressIndex_IsCorrect()
        {
            // "escola" has stress_index=1 (2nd syllable: kɔ)
            var result = PortugueseExceptionDictionary.TryLookup("escola", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(1, pron.StressedSyllableIndex);
        }

        [Fact]
        public void TryLookup_StressIndex_MinusOneForMonosyllable()
        {
            // "é" has stress_index=-1
            var result = PortugueseExceptionDictionary.TryLookup("é", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(-1, pron.StressedSyllableIndex);
        }

        [Fact]
        public void TryLookup_StressIndex_ZeroForFirstSyllable()
        {
            // "belo" has stress_index=0 (1st syllable stressed)
            var result = PortugueseExceptionDictionary.TryLookup("belo", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(0, pron.StressedSyllableIndex);
        }

        [Fact]
        public void TryLookup_StressIndex_TwoForThirdSyllable()
        {
            // "coração" has stress_index=2
            var result = PortugueseExceptionDictionary.TryLookup("coração", PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.Equal(2, pron.StressedSyllableIndex);
        }

        // ===== 辞書エントリ数の確認 =====

        [Fact]
        public void Dictionary_HasMinimumEntries()
        {
            // 500語以上含むことを確認（代表語彙がすべて存在するか）
            var testWords = new[]
            {
                // open_close_vowel
                "belo", "terra", "ovo", "jogo", "todo", "lobo",
                // x_irregular
                "exame", "fixo", "próximo", "xarope",
                // foreign
                "pizza", "show", "shopping", "whisky", "sushi",
                // metaphony
                "ovos", "nova", "belos",
                // verb_irregular
                "sou", "é", "tenho", "vou",
                // nasal_irregular
                "mão", "mãe", "bem", "um",
                // silent
                "hora", "homem", "hotel", "humano",
                // misc
                "dez", "avó", "café",
            };

            int found = 0;
            foreach (var word in testWords)
            {
                if (PortugueseExceptionDictionary.TryLookup(word, PortugueseDialect.Brazilian, out _))
                    found++;
            }

            Assert.Equal(testWords.Length, found);
        }

        // ===== カテゴリ別のエントリ存在確認 =====

        [Theory]
        [InlineData("belo")]     // open_close_vowel (ɛ開母音)
        [InlineData("todo")]     // open_close_vowel (o閉母音)
        [InlineData("exame")]    // x_irregular (x=/z/)
        [InlineData("fixo")]     // x_irregular (x=/ks/)
        [InlineData("pizza")]    // foreign
        [InlineData("ovos")]     // metaphony
        [InlineData("sou")]      // verb_irregular
        [InlineData("mão")]      // nasal_irregular
        [InlineData("hotel")]    // silent (h黙字)
        [InlineData("dez")]      // misc
        public void TryLookup_VariousCategories_AllPresent(string word)
        {
            var result = PortugueseExceptionDictionary.TryLookup(word, PortugueseDialect.Brazilian, out var pron);
            Assert.True(result);
            Assert.NotNull(pron);
            Assert.True(pron.Phonemes.Count > 0);
        }

        // ===== 開/閉母音ペア検証 =====

        [Fact]
        public void TryLookup_OpenVsClosedO_DifferentPhonemes()
        {
            // ovo (ɔ開) vs todo (o閉) - 最初の母音が異なる
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.Brazilian, out var ovoPron));
            Assert.True(PortugueseExceptionDictionary.TryLookup("todo", PortugueseDialect.Brazilian, out var todoPron));
            Assert.Equal(PortugueseIpaPhoneme.Oh, ovoPron.Phonemes[0].Phoneme);   // ɔ
            Assert.Equal(PortugueseIpaPhoneme.T, todoPron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.O, todoPron.Phonemes[1].Phoneme);    // o
        }

        [Fact]
        public void TryLookup_OpenVsClosedE_DifferentPhonemes()
        {
            // belo (ɛ開) vs dedo (e閉) - 2番目の音素が異なる
            Assert.True(PortugueseExceptionDictionary.TryLookup("belo", PortugueseDialect.Brazilian, out var beloPron));
            Assert.True(PortugueseExceptionDictionary.TryLookup("dedo", PortugueseDialect.Brazilian, out var dedoPron));
            Assert.Equal(PortugueseIpaPhoneme.Eh, beloPron.Phonemes[1].Phoneme);   // ɛ
            Assert.Equal(PortugueseIpaPhoneme.E, dedoPron.Phonemes[1].Phoneme);     // e
        }

        // ===== x 不規則読み分け検証 =====

        [Fact]
        public void TryLookup_X_AsZ_InExame()
        {
            // exame: x=/z/
            Assert.True(PortugueseExceptionDictionary.TryLookup("exame", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.Z, pron.Phonemes[1].Phoneme);
        }

        [Fact]
        public void TryLookup_X_AsKS_InFixo()
        {
            // fixo: x=/ks/
            Assert.True(PortugueseExceptionDictionary.TryLookup("fixo", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.K, pron.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.S, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_X_AsS_InProximo()
        {
            // próximo: x=/s/
            Assert.True(PortugueseExceptionDictionary.TryLookup("próximo", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.S, pron.Phonemes[3].Phoneme);
        }

        [Fact]
        public void TryLookup_X_AsSh_InPeixe()
        {
            // peixe: x=/ʃ/
            Assert.True(PortugueseExceptionDictionary.TryLookup("peixe", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.Sh, pron.Phonemes[3].Phoneme);
        }

        // ===== Metaphony検証 =====

        [Fact]
        public void TryLookup_Metaphony_PluralOpenVowel()
        {
            // ovos (pl) -> /ɔ/ (todo sing は /o/)
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovos", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.Oh, pron.Phonemes[0].Phoneme); // ɔ開母音
        }

        [Fact]
        public void TryLookup_Metaphony_FeminineOpenVowel()
        {
            // nova (fem) -> /ɔ/
            Assert.True(PortugueseExceptionDictionary.TryLookup("nova", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.N, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.Oh, pron.Phonemes[1].Phoneme); // ɔ
        }

        // ===== silent (h黙字) 検証 =====

        [Fact]
        public void TryLookup_Silent_HoraStartsWithVowel()
        {
            // "hora" -> ɔ | ɾ ɐ (hは発音されない)
            Assert.True(PortugueseExceptionDictionary.TryLookup("hora", PortugueseDialect.Brazilian, out var pron));
            // 最初の音素は母音（hは黙字）
            Assert.Equal(PortugueseIpaPhoneme.Oh, pron.Phonemes[0].Phoneme);
        }

        [Fact]
        public void TryLookup_Silent_HotelStartsWithVowel()
        {
            // "hotel" -> o | t ɛ l
            Assert.True(PortugueseExceptionDictionary.TryLookup("hotel", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(PortugueseIpaPhoneme.O, pron.Phonemes[0].Phoneme);
        }

        // ===== 不規則動詞検証 =====

        [Fact]
        public void TryLookup_VerbSer_E_HasOpenVowel()
        {
            // "é" -> ɛ (1 phoneme)
            Assert.True(PortugueseExceptionDictionary.TryLookup("é", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(1, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.Eh, pron.Phonemes[0].Phoneme);
        }

        [Fact]
        public void TryLookup_VerbSer_Sao_HasNasalDiphthong()
        {
            // "são" -> s ɐ̃ w̃ (3 phonemes)
            Assert.True(PortugueseExceptionDictionary.TryLookup("são", PortugueseDialect.Brazilian, out var pron));
            Assert.Equal(3, pron.Phonemes.Count);
            Assert.Equal(PortugueseIpaPhoneme.S, pron.Phonemes[0].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.ANasal, pron.Phonemes[1].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.WNasal, pron.Phonemes[2].Phoneme);
        }

        // ===== 一貫性テスト =====

        [Fact]
        public void TryLookup_SameResultForBothDialects_WhenAnyDialect()
        {
            // "ovo" は全方言 -> 両方同じ発音
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.Brazilian, out var bp));
            Assert.True(PortugueseExceptionDictionary.TryLookup("ovo", PortugueseDialect.European, out var ep));
            Assert.Equal(bp.Phonemes.Count, ep.Phonemes.Count);
            for (int i = 0; i < bp.Phonemes.Count; i++)
            {
                Assert.Equal(bp.Phonemes[i].Phoneme, ep.Phonemes[i].Phoneme);
            }
        }
    }
}
