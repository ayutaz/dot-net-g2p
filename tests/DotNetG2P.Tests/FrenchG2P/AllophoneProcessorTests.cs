using DotNetG2P.French;
using DotNetG2P.French.Rules;
using Xunit;

namespace DotNetG2P.Tests.FrenchG2P
{
    public class AllophoneProcessorTests
    {
        private static FrenchPronunciation MakePron(params FrenchIpaPhoneme[] phonemes)
        {
            var fps = new FrenchPhoneme[phonemes.Length];
            for (var i = 0; i < phonemes.Length; i++)
                fps[i] = new FrenchPhoneme(phonemes[i]);
            return new FrenchPronunciation(fps, new[] { 0 }, stressedSyllableIndex: -1);
        }

        private static FrenchIpaPhoneme[] Ipa(FrenchPronunciation pron)
        {
            var result = new FrenchIpaPhoneme[pron.PhonemesInternal.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = pron.PhonemesInternal[i].Phoneme;
            return result;
        }

        // --- RDevoicing ---

        [Fact]
        public void RDevoicing_RBeforeVoicelessObstruent_BecomesRh()
        {
            // /ʁt/ -> [χt]
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.T);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.Rh, FrenchIpaPhoneme.T },
                Ipa(result));
        }

        [Fact]
        public void RDevoicing_RAfterVoicelessObstruent_BecomesRh()
        {
            // /pʁ/ -> [pχ] (prendre-like)
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.Rh, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        [Fact]
        public void RDevoicing_RBetweenVowels_StaysR()
        {
            // /aʁa/ -> [aʁa]
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        [Fact]
        public void RDevoicing_RWordFinal_StaysR()
        {
            // /paʁ/ -> [paʁ] (語末のRは無声化しない)
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.A, FrenchIpaPhoneme.R);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.A, FrenchIpaPhoneme.R },
                Ipa(result));
        }

        [Fact]
        public void RDevoicing_RBeforeVoicedObstruent_StaysR()
        {
            // /ʁb/ -> [ʁb]
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.B);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.B },
                Ipa(result));
        }

        [Fact]
        public void RDevoicing_RBeforeNasal_StaysR()
        {
            // /ʁm/ -> [ʁm] (鼻音は阻害音ではない)
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.M);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.M },
                Ipa(result));
        }

        // --- ObstruentVoicingAssimilation ---

        [Fact]
        public void Assimilation_VoicedBeforeVoiceless_Devoiced()
        {
            // /bs/ -> [ps] (absent-like: b無声化)
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.B, FrenchIpaPhoneme.S, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.P, FrenchIpaPhoneme.S, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        [Fact]
        public void Assimilation_VoicelessBeforeVoiced_Voiced()
        {
            // /kd/ -> [gd] (anecdote-like: k有声化)
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.K, FrenchIpaPhoneme.D, FrenchIpaPhoneme.Oh);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.G, FrenchIpaPhoneme.D, FrenchIpaPhoneme.Oh },
                Ipa(result));
        }

        [Fact]
        public void Assimilation_SameVoicing_NoChange()
        {
            // /pt/ -> [pt] (両方無声なので変化なし)
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.T);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.T },
                Ipa(result));
        }

        [Fact]
        public void Assimilation_NonObstruentBetween_NoSpread()
        {
            // /b.n.s/ -> 非阻害音(n)で分断されるので同化しない
            var pron = MakePron(FrenchIpaPhoneme.B, FrenchIpaPhoneme.N, FrenchIpaPhoneme.S);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.B, FrenchIpaPhoneme.N, FrenchIpaPhoneme.S },
                Ipa(result));
        }

        [Fact]
        public void Assimilation_ThreeObstruentCluster_CascadesBackward()
        {
            // /d.k.t/ -> /d.k.t/ -> k,tは同voicing -> d,kで逆行同化 -> [t.k.t]
            var pron = MakePron(FrenchIpaPhoneme.D, FrenchIpaPhoneme.K, FrenchIpaPhoneme.T);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.T, FrenchIpaPhoneme.K, FrenchIpaPhoneme.T },
                Ipa(result));
        }

        // --- フラグ制御 ---

        [Fact]
        public void Apply_FeaturesNone_NoChange()
        {
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.None);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        [Fact]
        public void Apply_OnlyRDevoicing_NoAssimilation()
        {
            // RDevoicingのみ有効、有声性同化は無効
            // /pʁ/ -> [pχ] (RDevoicing適用)
            // /bs/ はそのまま (同化なし)
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.B, FrenchIpaPhoneme.S);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.Rh, FrenchIpaPhoneme.B, FrenchIpaPhoneme.S },
                Ipa(result));
        }

        [Fact]
        public void Apply_OnlyAssimilation_NoRDevoicing()
        {
            // 有声性同化のみ有効、RDevoicingは無効
            // /pʁ/ はそのまま (RDevoicingなし)
            // /bs/ -> [ps] (同化適用)
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.B, FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);
            // bはsに合わせて無声化 -> p, Rは変わらない
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.P, FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        // --- 空入力 ---

        [Fact]
        public void Apply_EmptyPronunciation_ReturnsEmpty()
        {
            var pron = new FrenchPronunciation(new FrenchPhoneme[0], new[] { 0 }, stressedSyllableIndex: -1);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.Default);
            Assert.Empty(result.Phonemes);
        }

        // --- 統合 ---

        [Fact]
        public void Apply_DefaultFeatures_AppliesBothObligatoryRules()
        {
            // Default = RDevoicing | ObstruentVoicingAssimilation
            // /a.b.s.p.ʁ.a/ -> 同化: b->p (bがsに合わせて無声化), R無声化: pの後のR -> Rh
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.B, FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.R, FrenchIpaPhoneme.A);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.Default);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.P, FrenchIpaPhoneme.S, FrenchIpaPhoneme.P, FrenchIpaPhoneme.Rh, FrenchIpaPhoneme.A },
                Ipa(result));
        }

        [Fact]
        public void Apply_PreservesSyllableOffsetsAndStress()
        {
            var phonemes = new[]
            {
                new FrenchPhoneme(FrenchIpaPhoneme.P),
                new FrenchPhoneme(FrenchIpaPhoneme.R),
                new FrenchPhoneme(FrenchIpaPhoneme.A, isSyllableNucleus: true),
            };
            var offsets = new[] { 0 };
            var pron = new FrenchPronunciation(phonemes, offsets, stressedSyllableIndex: -1);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.RDevoicing);

            Assert.Equal(-1, result.StressedSyllableIndex);
            Assert.Equal(offsets, result.SyllableOffsetsInternal);
            // R -> Rh, but syllable nucleus preserved
            Assert.True(result.PhonemesInternal[2].IsSyllableNucleus);
        }

        // --- 未実装フラグ確認（有効にしても入力が変わらない） ---

        [Fact]
        public void Apply_VowelLengtheningFlag_NoChangeToInput()
        {
            // VowelLengthening は未実装なので、有効にしても出力は変わらない
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.T);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.VowelLengthening);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.T },
                Ipa(result));
        }

        [Fact]
        public void Apply_LVelarizationFlag_NoChangeToInput()
        {
            // LVelarization は未実装なので、有効にしても出力は変わらない
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.L, FrenchIpaPhoneme.T);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.LVelarization);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.L, FrenchIpaPhoneme.T },
                Ipa(result));
        }

        [Fact]
        public void Apply_FinalDevoicingFlag_NoChangeToInput()
        {
            // FinalDevoicing は未実装なので、有効にしても出力は変わらない
            var pron = MakePron(FrenchIpaPhoneme.A, FrenchIpaPhoneme.B);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.FinalDevoicing);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.A, FrenchIpaPhoneme.B },
                Ipa(result));
        }

        [Fact]
        public void Apply_AllUnimplementedFlags_NoChangeToInput()
        {
            // 未実装フラグ3つ全て有効にしても出力は変わらない
            var features = FrenchAllophoneFeatures.VowelLengthening
                         | FrenchAllophoneFeatures.LVelarization
                         | FrenchAllophoneFeatures.FinalDevoicing;
            var pron = MakePron(FrenchIpaPhoneme.P, FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.L, FrenchIpaPhoneme.B);
            var result = AllophoneProcessor.Apply(pron, features);
            Assert.Equal(
                new[] { FrenchIpaPhoneme.P, FrenchIpaPhoneme.A, FrenchIpaPhoneme.R, FrenchIpaPhoneme.L, FrenchIpaPhoneme.B },
                Ipa(result));
        }

        // --- 属性保持確認 ---

        [Fact]
        public void Apply_PreservesIsSyllableNucleus()
        {
            var phonemes = new[]
            {
                new FrenchPhoneme(FrenchIpaPhoneme.A, isSyllableNucleus: true),
                new FrenchPhoneme(FrenchIpaPhoneme.B),
                new FrenchPhoneme(FrenchIpaPhoneme.S),
                new FrenchPhoneme(FrenchIpaPhoneme.A, isSyllableNucleus: true),
            };
            var pron = new FrenchPronunciation(phonemes, new[] { 0, 3 }, stressedSyllableIndex: -1);
            var result = AllophoneProcessor.Apply(pron, FrenchAllophoneFeatures.ObstruentVoicingAssimilation);

            // b -> p (同化), but IsSyllableNucleus on vowels preserved
            Assert.True(result.PhonemesInternal[0].IsSyllableNucleus);
            Assert.False(result.PhonemesInternal[1].IsSyllableNucleus);
            Assert.True(result.PhonemesInternal[3].IsSyllableNucleus);
        }
    }
}
