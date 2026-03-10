using DotNetG2P.French;
using DotNetG2P.French.Rules;

namespace DotNetG2P.Tests.FrenchG2P
{
    /// <summary>
    /// FrenchSyllabifier の単体テスト。
    /// Syllabify() と IsValidOnset() は internal だが InternalsVisibleTo 設定済み。
    /// </summary>
    public class FrenchSyllabifierTests
    {
        // ========== 基本音節分割 ==========

        [Fact]
        public void Syllabify_Null_ReturnsEmpty()
        {
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(null);
            Assert.Empty(offsets);
            Assert.Empty(phonemes);
        }

        [Fact]
        public void Syllabify_EmptyArray_ReturnsEmpty()
        {
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(Array.Empty<FrenchIpaPhoneme>());
            Assert.Empty(offsets);
            Assert.Empty(phonemes);
        }

        [Fact]
        public void Syllabify_SingleVowel_OneSyllable()
        {
            // /a/ → 1音節
            var input = new[] { FrenchIpaPhoneme.A };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.Equal(0, offsets[0]);
            Assert.Single(phonemes);
            Assert.True(phonemes[0].IsSyllableNucleus);
        }

        [Fact]
        public void Syllabify_CV_OneSyllable()
        {
            // /pa/ → 1音節 [pa]
            var input = new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.A };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(2, phonemes.Length);
            Assert.False(phonemes[0].IsSyllableNucleus); // p
            Assert.True(phonemes[1].IsSyllableNucleus);  // a
        }

        [Fact]
        public void Syllabify_CVC_OneSyllable()
        {
            // /sol/ → 1音節 [sɔl]
            var input = new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.Oh, FrenchIpaPhoneme.L };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.Equal(3, phonemes.Length);
            Assert.True(phonemes[1].IsSyllableNucleus); // ɔ
        }

        [Fact]
        public void Syllabify_CVCV_TwoSyllables()
        {
            // /papa/ → 2音節 [pa.pa]
            var input = new[]
            {
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.A,
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.A
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(2, offsets[1]); // 2番目の音節は index 2 から
        }

        [Fact]
        public void Syllabify_CVCCV_OnsetMaximization()
        {
            // /asta/ → 2音節 [as.ta]（onset maximization: tは次の音節へ）
            var input = new[]
            {
                FrenchIpaPhoneme.A, FrenchIpaPhoneme.S,
                FrenchIpaPhoneme.T, FrenchIpaPhoneme.A
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            Assert.Equal(0, offsets[0]);
            // s + t: t は単子音で有効なonset → [a.sta]
            // ただし st が有効なonsetでないので [as.ta]
            // IsValidOnset(2 chars: s,t): s は IsObstruentForOnset? No (S=27, not stop/F/V)
            // → st は invalid onset → t のみ (length=1) → [as.ta]
            Assert.Equal(2, offsets[1]); // [a.sta]? or [as.ta]?
        }

        [Fact]
        public void Syllabify_CCVCV_OnsetCluster()
        {
            // /plase/ → 2音節 [pla.sə]
            var input = new[]
            {
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.L, FrenchIpaPhoneme.A,
                FrenchIpaPhoneme.S, FrenchIpaPhoneme.Schwa
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(3, offsets[1]); // sə は index 3 から
        }

        [Fact]
        public void Syllabify_VowelSequence_SeparateSyllables()
        {
            // /ai/ → 2音節 [a.i]（隣接母音は別音節）
            var input = new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.I };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(1, offsets[1]);
        }

        [Fact]
        public void Syllabify_NasalVowel_TreatedAsVowel()
        {
            // /bɔ̃/ → 1音節
            var input = new[] { FrenchIpaPhoneme.B, FrenchIpaPhoneme.ONasal };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.True(phonemes[1].IsSyllableNucleus);
        }

        [Fact]
        public void Syllabify_NasalVowelInMiddle_SyllableBoundary()
        {
            // /bɔ̃ʒuʁ/ → 2音節 [bɔ̃.ʒuʁ]
            var input = new[]
            {
                FrenchIpaPhoneme.B, FrenchIpaPhoneme.ONasal,
                FrenchIpaPhoneme.Zh, FrenchIpaPhoneme.U, FrenchIpaPhoneme.R
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(2, offsets[1]);
        }

        [Fact]
        public void Syllabify_NoVowels_ReturnsOneSyllable()
        {
            // 全部子音 → 1音節（母音なし）
            var input = new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.S, FrenchIpaPhoneme.T };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(3, phonemes.Length);
            // 核マークなし
            Assert.False(phonemes[0].IsSyllableNucleus);
            Assert.False(phonemes[1].IsSyllableNucleus);
            Assert.False(phonemes[2].IsSyllableNucleus);
        }

        [Fact]
        public void Syllabify_ThreeSyllables()
        {
            // /patate/ → [pa.ta.tə] (3音節)
            var input = new[]
            {
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.A,
                FrenchIpaPhoneme.T, FrenchIpaPhoneme.A,
                FrenchIpaPhoneme.T, FrenchIpaPhoneme.Schwa
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(3, offsets.Length);
            Assert.Equal(0, offsets[0]);
            Assert.Equal(2, offsets[1]);
            Assert.Equal(4, offsets[2]);
        }

        // ========== IsValidOnset テスト ==========

        [Theory]
        // 有効な2子音onset: 閉鎖音/F/V + L/R
        [InlineData(new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.L }, true)]   // pl
        [InlineData(new[] { FrenchIpaPhoneme.B, FrenchIpaPhoneme.L }, true)]   // bl
        [InlineData(new[] { FrenchIpaPhoneme.K, FrenchIpaPhoneme.L }, true)]   // kl
        [InlineData(new[] { FrenchIpaPhoneme.G, FrenchIpaPhoneme.L }, true)]   // gl
        [InlineData(new[] { FrenchIpaPhoneme.F, FrenchIpaPhoneme.L }, true)]   // fl
        [InlineData(new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.R }, true)]   // pr
        [InlineData(new[] { FrenchIpaPhoneme.B, FrenchIpaPhoneme.R }, true)]   // br
        [InlineData(new[] { FrenchIpaPhoneme.T, FrenchIpaPhoneme.R }, true)]   // tr
        [InlineData(new[] { FrenchIpaPhoneme.D, FrenchIpaPhoneme.R }, true)]   // dr
        [InlineData(new[] { FrenchIpaPhoneme.K, FrenchIpaPhoneme.R }, true)]   // kr
        [InlineData(new[] { FrenchIpaPhoneme.G, FrenchIpaPhoneme.R }, true)]   // gr
        [InlineData(new[] { FrenchIpaPhoneme.F, FrenchIpaPhoneme.R }, true)]   // fr
        [InlineData(new[] { FrenchIpaPhoneme.V, FrenchIpaPhoneme.R }, true)]   // vr
        public void IsValidOnset_TwoConsonants_ValidClusters(FrenchIpaPhoneme[] cluster, bool expected)
        {
            Assert.Equal(expected, FrenchSyllabifier.IsValidOnset(cluster, 0, cluster.Length));
        }

        [Theory]
        // 無効な2子音onset: TL, DL
        [InlineData(new[] { FrenchIpaPhoneme.T, FrenchIpaPhoneme.L }, false)]  // tl
        [InlineData(new[] { FrenchIpaPhoneme.D, FrenchIpaPhoneme.L }, false)]  // dl
        public void IsValidOnset_TwoConsonants_InvalidClusters(FrenchIpaPhoneme[] cluster, bool expected)
        {
            Assert.Equal(expected, FrenchSyllabifier.IsValidOnset(cluster, 0, cluster.Length));
        }

        [Fact]
        public void IsValidOnset_SingleConsonant_AlwaysValid()
        {
            var input = new[] { FrenchIpaPhoneme.S };
            Assert.True(FrenchSyllabifier.IsValidOnset(input, 0, 1));
        }

        [Theory]
        // 3子音onset: S + 閉鎖音 + R
        [InlineData(new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.R }, true)]   // spr
        [InlineData(new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.T, FrenchIpaPhoneme.R }, true)]   // str
        [InlineData(new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.K, FrenchIpaPhoneme.R }, true)]   // skr
        // 無効: S + 摩擦音 + R
        [InlineData(new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.F, FrenchIpaPhoneme.R }, false)]  // sfr
        // 無効: S + 閉鎖音 + L
        [InlineData(new[] { FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.L }, false)]  // spl
        public void IsValidOnset_ThreeConsonants(FrenchIpaPhoneme[] cluster, bool expected)
        {
            Assert.Equal(expected, FrenchSyllabifier.IsValidOnset(cluster, 0, cluster.Length));
        }

        // ========== 音節核マーク ==========

        [Fact]
        public void Syllabify_NucleusMarking_FirstVowelInSyllable()
        {
            // /pa/ → 1音節、a が核
            var input = new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.A };
            var (_, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.False(phonemes[0].IsSyllableNucleus);
            Assert.True(phonemes[1].IsSyllableNucleus);
            Assert.Equal(FrenchIpaPhoneme.P, phonemes[0].Phoneme);
            Assert.Equal(FrenchIpaPhoneme.A, phonemes[1].Phoneme);
        }

        [Fact]
        public void Syllabify_MultipleSyllables_EachHasNucleus()
        {
            // /papa/ → [pa.pa]、各音節に核が1つ
            var input = new[]
            {
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.A,
                FrenchIpaPhoneme.P, FrenchIpaPhoneme.A
            };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Equal(2, offsets.Length);
            // 第1音節: p(非核) + a(核)
            Assert.False(phonemes[0].IsSyllableNucleus);
            Assert.True(phonemes[1].IsSyllableNucleus);
            // 第2音節: p(非核) + a(核)
            Assert.False(phonemes[2].IsSyllableNucleus);
            Assert.True(phonemes[3].IsSyllableNucleus);
        }

        // ========== 半母音は母音として扱わない ==========

        [Fact]
        public void Syllabify_SemivowelNotVowel()
        {
            // /ja/ → j は半母音（母音ではない）、a が核
            var input = new[] { FrenchIpaPhoneme.J, FrenchIpaPhoneme.A };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets); // 1音節
            Assert.False(phonemes[0].IsSyllableNucleus); // j
            Assert.True(phonemes[1].IsSyllableNucleus);  // a
        }

        [Fact]
        public void Syllabify_SemivowelW_NotVowel()
        {
            // /wa/ → w は半母音、a が核
            var input = new[] { FrenchIpaPhoneme.W, FrenchIpaPhoneme.A };
            var (offsets, phonemes) = FrenchSyllabifier.Syllabify(input);

            Assert.Single(offsets);
            Assert.False(phonemes[0].IsSyllableNucleus);
            Assert.True(phonemes[1].IsSyllableNucleus);
        }
    }
}
