using DotNetG2P.Models;

namespace DotNetG2P.Tests.Models
{
    public class PronunciationTests
    {
        // ===== ParseMoraSegments テスト =====

        [Fact]
        public void ParseMoraSegments_SimpleKatakana_ReturnsSingleSegment()
        {
            var result = Pronunciation.ParseMoraSegments("コンニチワ");
            Assert.Single(result);
            Assert.Equal("コンニチワ", result[0].text);
            Assert.Equal(5, result[0].moras.Count);
            Assert.Equal(MoraKind.Ko, result[0].moras[0].Kind);
            Assert.Equal(MoraKind.N, result[0].moras[1].Kind);
            Assert.Equal(MoraKind.Ni, result[0].moras[2].Kind);
            Assert.Equal(MoraKind.Chi, result[0].moras[3].Kind);
            Assert.Equal(MoraKind.Wa, result[0].moras[4].Kind);
        }

        [Fact]
        public void ParseMoraSegments_Asterisk_ReturnsEmptyList()
        {
            var result = Pronunciation.ParseMoraSegments("*");
            Assert.Empty(result);
        }

        [Fact]
        public void ParseMoraSegments_FullwidthQuestion_ReturnsQuestionMora()
        {
            var result = Pronunciation.ParseMoraSegments("\uFF1F"); // ？
            Assert.Single(result);
            Assert.Single(result[0].moras);
            Assert.Equal(MoraKind.Question, result[0].moras[0].Kind);
        }

        [Fact]
        public void ParseMoraSegments_WithUnrecognizedChar_SplitsIntoSegments()
        {
            // "バリー・ペーン" → [("バリー", ...), ("・", Touten), ("ペーン", ...)]
            var result = Pronunciation.ParseMoraSegments("バリー・ペーン");
            Assert.Equal(3, result.Count);

            // 1番目: バリー
            Assert.Equal("バリー", result[0].text);
            Assert.Equal(3, result[0].moras.Count);
            Assert.Equal(MoraKind.Ba, result[0].moras[0].Kind);
            Assert.Equal(MoraKind.Ri, result[0].moras[1].Kind);
            Assert.Equal(MoraKind.Long, result[0].moras[2].Kind);

            // 2番目: ・ (中点は認識不能でTouten)
            Assert.Equal("・", result[1].text);
            Assert.Single(result[1].moras);
            Assert.Equal(MoraKind.Touten, result[1].moras[0].Kind);

            // 3番目: ペーン
            Assert.Equal("ペーン", result[2].text);
            Assert.Equal(3, result[2].moras.Count);
            Assert.Equal(MoraKind.Pe, result[2].moras[0].Kind);
            Assert.Equal(MoraKind.Long, result[2].moras[1].Kind);
            Assert.Equal(MoraKind.N, result[2].moras[2].Kind);
        }

        [Fact]
        public void ParseMoraSegments_YouonHandledCorrectly()
        {
            var result = Pronunciation.ParseMoraSegments("キャ");
            Assert.Single(result);
            Assert.Single(result[0].moras);
            Assert.Equal(MoraKind.Kya, result[0].moras[0].Kind);
        }

        [Fact]
        public void ParseMoraSegments_UnvoicedMarker_AppliesUnvoicing()
        {
            // ParseMoraSegmentsでは'はモーラの直後に付く無声化マーカー
            // "ス'キ" → スの後に'がある = スが無声化される
            var result = Pronunciation.ParseMoraSegments("ス'キ");
            Assert.Single(result);
            Assert.Equal(2, result[0].moras.Count);
            Assert.Equal(MoraKind.Su, result[0].moras[0].Kind);
            Assert.Equal(Vowel.U_Unvoiced, result[0].moras[0].Vowel); // スが無声化
            Assert.Equal(MoraKind.Ki, result[0].moras[1].Kind);
            Assert.Equal(Vowel.I, result[0].moras[1].Vowel); // キは有声のまま
        }

        // ===== FromKatakana テスト =====

        [Fact]
        public void FromKatakana_SimpleKatakana_CreatesCorrectPronunciation()
        {
            var pron = Pronunciation.FromKatakana("コンニチワ", 3);
            Assert.Equal(5, pron.MoraCount);
            Assert.Equal(3, pron.AccentPosition);
            Assert.Equal("k o N n i ch i w a", pron.ToPhonemeString());
        }

        [Fact]
        public void FromKatakana_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Pronunciation.FromKatakana(null!, 0));
        }

        [Fact]
        public void FromKatakana_WithUnvoicedMarker_AppliesUnvoicing()
        {
            // "デス'" → デ(d e) + ス(s U_Unvoiced) ... ' はスの直後に付くのでスが無声化
            var pron = Pronunciation.FromKatakana("デス'", 1);
            Assert.Equal(2, pron.MoraCount);
            Assert.Equal(Vowel.E, pron.Moras[0].Vowel);           // デ: 有声
            Assert.Equal(Vowel.U_Unvoiced, pron.Moras[1].Vowel);  // ス: 無声化
        }

        // ===== ToKatakana テスト =====

        [Fact]
        public void ToKatakana_BasicMoras_ReturnsCorrectKatakana()
        {
            var pron = Pronunciation.FromKatakana("コンニチワ", 0);
            Assert.Equal("コンニチワ", pron.ToKatakana());
        }

        [Fact]
        public void ToKatakana_WithLongVowel_ReturnsCorrectKatakana()
        {
            var pron = Pronunciation.FromKatakana("アリガトー", 0);
            Assert.Equal("アリガトー", pron.ToKatakana());
        }

        [Fact]
        public void ToKatakana_WithSokuon_ReturnsCorrectKatakana()
        {
            var pron = Pronunciation.FromKatakana("ガッコー", 0);
            Assert.Equal("ガッコー", pron.ToKatakana());
        }

        [Fact]
        public void ToKatakana_EmptyPronunciation_ReturnsEmpty()
        {
            var pron = new Pronunciation();
            Assert.Equal("", pron.ToKatakana());
        }

        // ===== MoraCount テスト =====

        [Fact]
        public void MoraCount_SimpleKatakana_ReturnsCorrectCount()
        {
            var pron = Pronunciation.FromKatakana("コンニチワ", 0);
            Assert.Equal(5, pron.MoraCount);
        }

        [Fact]
        public void MoraCount_WithSpecialMoras_CountsAll()
        {
            // ガッコー → ガ, ッ, コ, ー = 4モーラ
            var pron = Pronunciation.FromKatakana("ガッコー", 0);
            Assert.Equal(4, pron.MoraCount);
        }

        [Fact]
        public void MoraCount_EmptyPronunciation_ReturnsZero()
        {
            var pron = new Pronunciation();
            Assert.Equal(0, pron.MoraCount);
        }

        [Fact]
        public void MoraCount_ToutenNotCounted()
        {
            // Toutenはカウントされない
            var moras = new List<Mora>
            {
                new Mora(Consonant.K, Vowel.A, MoraKind.Ka),
                new Mora(null, null, MoraKind.Touten),
                new Mora(Consonant.K, Vowel.I, MoraKind.Ki),
            };
            var pron = new Pronunciation(moras, 0);
            Assert.Equal(2, pron.MoraCount); // ToutenはMoraCountに含まれない
        }

        [Fact]
        public void MoraCount_QuestionNotCounted()
        {
            var moras = new List<Mora>
            {
                new Mora(Consonant.K, Vowel.A, MoraKind.Ka),
                new Mora(null, null, MoraKind.Question),
            };
            var pron = new Pronunciation(moras, 0);
            Assert.Equal(1, pron.MoraCount); // QuestionはMoraCountに含まれない
        }

        // ===== ToPhonemeString テスト =====

        [Fact]
        public void ToPhonemeString_ReturnsSpaceSeparatedPhonemes()
        {
            var pron = Pronunciation.FromKatakana("スキ", 0);
            Assert.Equal("s u k i", pron.ToPhonemeString());
        }

        [Fact]
        public void ToPhonemeString_WithSpecialMoras_IncludesSpecial()
        {
            var pron = Pronunciation.FromKatakana("ガッコー", 0);
            Assert.Equal("g a cl k o -", pron.ToPhonemeString());
        }

        [Fact]
        public void ToPhonemeString_Empty_ReturnsEmpty()
        {
            var pron = new Pronunciation();
            Assert.Equal("", pron.ToPhonemeString());
        }

        // ===== IsEmpty / IsTouten / IsQuestion テスト =====

        [Fact]
        public void IsEmpty_NewPronunciation_ReturnsTrue()
        {
            var pron = new Pronunciation();
            Assert.True(pron.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WithMoras_ReturnsFalse()
        {
            var pron = Pronunciation.FromKatakana("ア", 0);
            Assert.False(pron.IsEmpty);
        }

        [Fact]
        public void IsTouten_SingleToutenMora_ReturnsTrue()
        {
            var moras = new List<Mora> { new Mora(null, null, MoraKind.Touten) };
            var pron = new Pronunciation(moras, 0);
            Assert.True(pron.IsTouten);
        }

        [Fact]
        public void IsQuestion_SingleQuestionMora_ReturnsTrue()
        {
            var moras = new List<Mora> { new Mora(null, null, MoraKind.Question) };
            var pron = new Pronunciation(moras, 0);
            Assert.True(pron.IsQuestion);
        }

        // ===== IsMoraConvertable テスト =====

        [Fact]
        public void IsMoraConvertable_ValidKatakana_ReturnsTrue()
        {
            Assert.True(Pronunciation.IsMoraConvertable("コンニチワ"));
        }

        [Fact]
        public void IsMoraConvertable_EmptyString_ReturnsFalse()
        {
            Assert.False(Pronunciation.IsMoraConvertable(""));
        }

        [Fact]
        public void IsMoraConvertable_NullString_ReturnsFalse()
        {
            Assert.False(Pronunciation.IsMoraConvertable(null!));
        }

        [Fact]
        public void IsMoraConvertable_Asterisk_ReturnsFalse()
        {
            Assert.False(Pronunciation.IsMoraConvertable("*"));
        }

        // ===== TransferFrom テスト =====

        [Fact]
        public void TransferFrom_AppendsMorasFromOther()
        {
            var pron1 = Pronunciation.FromKatakana("コン", 0);
            var pron2 = Pronunciation.FromKatakana("ニチワ", 0);

            pron1.TransferFrom(pron2);

            Assert.Equal(5, pron1.Moras.Count);
            Assert.Equal("k o N n i ch i w a", pron1.ToPhonemeString());
        }

        // ===== ToString テスト =====

        [Fact]
        public void ToString_ReturnsKatakanaWithAccentPosition()
        {
            var pron = Pronunciation.FromKatakana("コンニチワ", 3);
            Assert.Equal("コンニチワ [3]", pron.ToString());
        }
    }
}
