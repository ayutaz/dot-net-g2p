using System;
using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Rules;
using Xunit;

namespace DotNetG2P.Tests.PortugueseG2P
{
    /// <summary>
    /// ポルトガル語 AllophoneProcessor の単体テスト。
    /// </summary>
    public class AllophoneProcessorTests
    {
        // ===== ヘルパー =====

        /// <summary>
        /// ストレスなしの PortuguesePronunciation を構築する。
        /// </summary>
        private static PortuguesePronunciation MakePron(
            PortugueseIpaPhoneme[] phonemes, int[] syllableOffsets, int stressIndex)
        {
            var p = new PortuguesePhoneme[phonemes.Length];
            for (var i = 0; i < phonemes.Length; i++)
                p[i] = new PortuguesePhoneme(phonemes[i], false);
            return new PortuguesePronunciation(p, syllableOffsets, stressIndex);
        }

        /// <summary>
        /// ストレス位置指定付きの PortuguesePronunciation を構築する。
        /// </summary>
        private static PortuguesePronunciation MakePronWithStress(
            PortugueseIpaPhoneme[] phonemes, int[] syllableOffsets, int stressIndex, int[] stressedPositions)
        {
            var p = new PortuguesePhoneme[phonemes.Length];
            for (var i = 0; i < phonemes.Length; i++)
                p[i] = new PortuguesePhoneme(phonemes[i], Array.IndexOf(stressedPositions, i) >= 0);
            return new PortuguesePronunciation(p, syllableOffsets, stressIndex);
        }

        /// <summary>
        /// PortuguesePronunciation から音素 enum 配列を取り出す。
        /// </summary>
        private static PortugueseIpaPhoneme[] GetPhonemes(PortuguesePronunciation pron)
        {
            var result = new PortugueseIpaPhoneme[pron.Phonemes.Count];
            for (var i = 0; i < pron.Phonemes.Count; i++)
                result[i] = pron.Phonemes[i].Phoneme;
            return result;
        }

        // ================================================================
        // 1. None → 変更なし
        // ================================================================

        [Fact]
        public void Apply_NoneFeatures_ReturnsIdenticalPronunciation()
        {
            // /k a z a/ — None フラグでは何も変わらない
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.None, PortugueseDialect.Brazilian);
            Assert.Equal(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                GetPhonemes(result));
        }

        // ================================================================
        // 2. VowelReduction (BP)
        // ================================================================

        [Fact]
        public void VowelReduction_BP_UnstressedE_BecomesI()
        {
            // BP: 非ストレス E → I
            // /n o m e/ (ストレスは o=index 1)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.E },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            // 非ストレスの E(index 3) は I になる（BP）
            Assert.Equal(PortugueseIpaPhoneme.I, phonemes[3]);
        }

        [Fact]
        public void VowelReduction_BP_UnstressedO_BecomesU()
        {
            // BP: 非ストレス O → U
            // /g a t o/ (ストレスは a=index 1)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.G, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.O },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.U, phonemes[3]);
        }

        [Fact]
        public void VowelReduction_BP_StressedVowel_NotChanged()
        {
            // ストレス付き母音は変化しない
            // /k a z a/ (ストレスは最初の a=index 1)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            // ストレス付き A(index 1) は保持される
            Assert.Equal(PortugueseIpaPhoneme.A, result.Phonemes[1].Phoneme);
            Assert.True(result.Phonemes[1].IsStressed);
        }

        [Fact]
        public void VowelReduction_BP_UnstressedFinalA_BecomesSchwa()
        {
            // BP: 語末の非ストレス A → Schwa (/ɐ/) (例: casa→[ˈkazɐ])
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Schwa, phonemes[3]);
        }

        // ================================================================
        // 3. VowelReduction (EP)
        // ================================================================

        [Fact]
        public void VowelReduction_EP_UnstressedE_BecomesHighCentral()
        {
            // EP: 非ストレス E → HighCentral (/ɨ/)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.E },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.HighCentral, phonemes[3]);
        }

        [Fact]
        public void VowelReduction_EP_UnstressedO_BecomesU()
        {
            // EP: 非ストレス O → U (両方言共通)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.G, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.O },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.U, phonemes[3]);
        }

        [Fact]
        public void VowelReduction_EP_UnstressedFinalA_BecomesSchwa()
        {
            // EP: 語末の非ストレス A → Schwa (/ɐ/)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Schwa, phonemes[3]);
        }

        [Fact]
        public void VowelReduction_EP_NonFinalUnstressedA_BecomesSchwa()
        {
            // EP: 語末でない非ストレス A も弱化する (例: falar→[fɐˈlaɾ])
            // /a m a r/ (ストレスは 2番目の a=index 2)
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.R },
                new[] { 0, 2 }, stressIndex: 1, stressedPositions: new[] { 2 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            // EP では全非ストレス位置で A→Schwa。最初の a(index 0) は非ストレス語中なので弱化する
            Assert.Equal(PortugueseIpaPhoneme.Schwa, phonemes[0]);
        }

        [Fact]
        public void VowelReduction_BP_NonFinalUnstressedA_Preserved()
        {
            // BP: 語末でない非ストレス A は保持される
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.R },
                new[] { 0, 2 }, stressIndex: 1, stressedPositions: new[] { 2 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.A, phonemes[0]);
        }

        // ================================================================
        // 4. TDPalatalization (BP固有)
        // ================================================================

        [Fact]
        public void TDPalatalization_T_BeforeI_BecomesCh()
        {
            // /t i/ → /tʃ i/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.I },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[0]);
        }

        [Fact]
        public void TDPalatalization_D_BeforeI_BecomesJh()
        {
            // /d i/ → /dʒ i/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.I },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Jh, phonemes[0]);
        }

        [Fact]
        public void TDPalatalization_T_BeforeINasal_BecomesCh()
        {
            // /t ĩ/ → /tʃ ĩ/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.INasal },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[0]);
        }

        [Fact]
        public void TDPalatalization_D_BeforeINasal_BecomesJh()
        {
            // /d ĩ/ → /dʒ ĩ/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.INasal },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Jh, phonemes[0]);
        }

        [Fact]
        public void TDPalatalization_T_BeforeA_NoChange()
        {
            // /t a/ → /t a/ （変化なし）
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.T, phonemes[0]);
        }

        [Fact]
        public void TDPalatalization_EP_NotApplied_WhenFlagEnabled()
        {
            // EP方言でもフラグ制御で適用される（方言による自動無効化はない）
            // フラグを明示的に有効にすれば EP でも適用される
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.I },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.TDPalatalization, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            // フラグ制御なので、EP でもフラグが有効なら適用される
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[0]);
        }

        // ================================================================
        // 5. VowelReduction + TDPalatalization の連鎖 (BP)
        // ================================================================

        [Fact]
        public void VowelReduction_Then_TDPalatalization_Chain_BP()
        {
            // "gente" 的パターン: /ʒ e t e/
            // 母音弱化(BP): E(index 3, 非ストレス語末) → I (BPでは E→I)
            // 破擦音化: T(index 2) + I(index 3) → Ch
            // ただし語中の E(index 1) はストレス音節内なので変化しない
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.Zh, PortugueseIpaPhoneme.E, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.E },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var features = PortugueseAllophoneFeatures.VowelReduction | PortugueseAllophoneFeatures.TDPalatalization;
            var result = AllophoneProcessor.Apply(input, features, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            // 母音弱化で語末 E(index 3) → I。T+I なので破擦音化が適用される
            Assert.Equal(PortugueseIpaPhoneme.I, phonemes[3]);
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[2]);
        }

        [Fact]
        public void VowelReduction_Then_TDPalatalization_Chain_WithI()
        {
            // /a t i/ (ストレスは a、i は非ストレス) — i はすでに I なので弱化せず、T+I で破擦音化
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.I },
                new[] { 0, 1 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var features = PortugueseAllophoneFeatures.VowelReduction | PortugueseAllophoneFeatures.TDPalatalization;
            var result = AllophoneProcessor.Apply(input, features, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[1]);
            Assert.Equal(PortugueseIpaPhoneme.I, phonemes[2]);
        }

        // ================================================================
        // 6. Lenition (EP向け)
        // ================================================================

        [Fact]
        public void Lenition_IntervocalicB_BecomesBeta()
        {
            // /a b a/ → /a β a/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0, 1 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.Lenition, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Beta, phonemes[1]);
        }

        [Fact]
        public void Lenition_IntervocalicD_BecomesDh()
        {
            // /a d a/ → /a ð a/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.A },
                new[] { 0, 1 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.Lenition, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Dh, phonemes[1]);
        }

        [Fact]
        public void Lenition_IntervocalicG_BecomesGh()
        {
            // /a g a/ → /a ɣ a/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.G, PortugueseIpaPhoneme.A },
                new[] { 0, 1 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.Lenition, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Gh, phonemes[1]);
        }

        [Fact]
        public void Lenition_WordInitialB_NoChange()
        {
            // 語頭の b は母音間でないので保持
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.Lenition, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.B, phonemes[0]);
        }

        [Fact]
        public void Lenition_AfterConsonantB_NoChange()
        {
            // 子音後（鼻音後）の b は保持: /a m b a/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.Lenition, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.B, phonemes[2]);
        }

        // ================================================================
        // 7. NasalAssimilation
        // ================================================================

        [Fact]
        public void NasalAssimilation_N_BeforeP_BecomesM()
        {
            // /a n p a/ → /a m p a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.P, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.M, phonemes[1]);
        }

        [Fact]
        public void NasalAssimilation_N_BeforeK_BecomesNg()
        {
            // /a n k a/ → /a ŋ k a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Ng, phonemes[1]);
        }

        [Fact]
        public void NasalAssimilation_N_BeforeF_BecomesNLabiodental()
        {
            // /a n f a/ → /a ɱ f a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.F, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.NLabiodental, phonemes[1]);
        }

        [Fact]
        public void NasalAssimilation_N_BeforeT_BecomesNDental()
        {
            // /a n t a/ → /a n̪ t a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.NDental, phonemes[1]);
        }

        [Fact]
        public void NasalAssimilation_N_BeforeVowel_NoChange()
        {
            // /n a/ → /n a/ (母音前は同化しない)
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.N, phonemes[0]);
        }

        // ================================================================
        // 8. SibilantVoicingAssimilation
        // ================================================================

        [Fact]
        public void SibilantVoicing_S_BeforeVoicedConsonant_BecomesZ()
        {
            // /a s b a/ → /a z b a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Z, phonemes[1]);
        }

        [Fact]
        public void SibilantVoicing_Z_BeforeVoicelessConsonant_BecomesS()
        {
            // /a z p a/ → /a s p a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.P, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[1]);
        }

        [Fact]
        public void SibilantVoicing_S_BeforeVowel_NoChange()
        {
            // /s a/ → /s a/ (母音前は変化しない)
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        // ================================================================
        // 9. SibilantPalatalization (EP)
        // ================================================================

        [Fact]
        public void SibilantPalatalization_CodaS_BecomesSh()
        {
            // 音節末 S → Sh (EP): /a s t a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantPalatalization, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[1]);
        }

        [Fact]
        public void SibilantPalatalization_CodaZ_BecomesZh()
        {
            // 音節末 Z → Zh (EP): /a z d a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantPalatalization, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[1]);
        }

        [Fact]
        public void SibilantPalatalization_WordFinalS_BecomesSh()
        {
            // 語末 S → Sh (EP): /a s/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.S },
                new[] { 0 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantPalatalization, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[1]);
        }

        [Fact]
        public void SibilantPalatalization_OnsetS_NoChange()
        {
            // 音節頭 S → 変化なし: /s a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantPalatalization, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.S, phonemes[0]);
        }

        // ================================================================
        // 10. LAllophony (BP)
        // ================================================================

        [Fact]
        public void LAllophony_BP_CodaL_BecomesW()
        {
            // BP: 音節末 L → W: /s o l/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.L },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.W, phonemes[2]);
        }

        [Fact]
        public void LAllophony_BP_LBeforeConsonant_BecomesW()
        {
            // BP: 子音前の L → W: /a l t o/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.L, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.O },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.W, phonemes[1]);
        }

        // ================================================================
        // 11. LAllophony (EP)
        // ================================================================

        [Fact]
        public void LAllophony_EP_CodaL_BecomesDarkL()
        {
            // EP: 音節末 L → DarkL (/ɫ/): /s o l/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.L },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.DarkL, phonemes[2]);
        }

        [Fact]
        public void LAllophony_OnsetL_NoChange()
        {
            // 音節頭の L は保持: /l a/
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.L, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.L, phonemes[0]);
        }

        // ================================================================
        // 12. BrazilianDefault プリセット
        // ================================================================

        [Fact]
        public void BrazilianDefault_AppliesAllExpectedRules()
        {
            // BrazilianDefault = Obligatory + TDPalatalization + LAllophony
            // /a n p a t i s o l/
            // 期待: NasalAssimilation(N→M), TDPalatalization(T+I→Ch), LAllophony(L→W), VowelReduction(O→U)
            var input = MakePronWithStress(
                new[]
                {
                    PortugueseIpaPhoneme.A,  // 0: ストレス母音
                    PortugueseIpaPhoneme.N,  // 1: 鼻音
                    PortugueseIpaPhoneme.P,  // 2
                    PortugueseIpaPhoneme.A,  // 3: 非ストレス語末母音
                    PortugueseIpaPhoneme.T,  // 4
                    PortugueseIpaPhoneme.I,  // 5
                    PortugueseIpaPhoneme.S,  // 6
                    PortugueseIpaPhoneme.O,  // 7: 非ストレス
                    PortugueseIpaPhoneme.L,  // 8: コーダ
                },
                new[] { 0, 4, 6 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.BrazilianDefault, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);

            // NasalAssimilation: N(1) + P(2) → M
            Assert.Equal(PortugueseIpaPhoneme.M, phonemes[1]);
            // TDPalatalization: T(4) + I(5) → Ch
            Assert.Equal(PortugueseIpaPhoneme.Ch, phonemes[4]);
            // LAllophony: L(8) → W (語末コーダ)
            Assert.Equal(PortugueseIpaPhoneme.W, phonemes[8]);
            // VowelReduction: O(7) → U (非ストレス)
            Assert.Equal(PortugueseIpaPhoneme.U, phonemes[7]);
        }

        [Fact]
        public void BrazilianDefault_HasCorrectFlagBits()
        {
            var expected = PortugueseAllophoneFeatures.Obligatory
                | PortugueseAllophoneFeatures.TDPalatalization
                | PortugueseAllophoneFeatures.LAllophony;
            Assert.Equal(expected, PortugueseAllophoneFeatures.BrazilianDefault);
        }

        // ================================================================
        // 13. EuropeanDefault プリセット
        // ================================================================

        [Fact]
        public void EuropeanDefault_AppliesAllExpectedRules()
        {
            // EuropeanDefault = Obligatory + Lenition + SibilantPalatalization + LAllophony
            // /a b a s d a l/
            // 期待: Lenition(母音間B→Beta), SibilantVoicingAssimilation(S+D→Z+D),
            //        SibilantPalatalization(コーダZ→Zh), LAllophony(L→DarkL)
            var input = MakePronWithStress(
                new[]
                {
                    PortugueseIpaPhoneme.A,  // 0: ストレス母音
                    PortugueseIpaPhoneme.B,  // 1: 母音間 → Beta
                    PortugueseIpaPhoneme.A,  // 2
                    PortugueseIpaPhoneme.S,  // 3: 有声性同化 → Z → 後部歯茎化 → Zh
                    PortugueseIpaPhoneme.D,  // 4
                    PortugueseIpaPhoneme.A,  // 5
                    PortugueseIpaPhoneme.L,  // 6: 語末 → DarkL
                },
                new[] { 0, 3, 5 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.EuropeanDefault, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);

            // Lenition: 母音間 B(1) → Beta
            Assert.Equal(PortugueseIpaPhoneme.Beta, phonemes[1]);
            // SibilantVoicingAssimilation: S(3)+D(4) → Z+D
            // SibilantPalatalization: コーダ位置の Z → Zh
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[3]);
            // LAllophony: 語末 L(6) → DarkL
            Assert.Equal(PortugueseIpaPhoneme.DarkL, phonemes[6]);
        }

        [Fact]
        public void EuropeanDefault_HasCorrectFlagBits()
        {
            var expected = PortugueseAllophoneFeatures.Obligatory
                | PortugueseAllophoneFeatures.Lenition
                | PortugueseAllophoneFeatures.SibilantPalatalization
                | PortugueseAllophoneFeatures.LAllophony;
            Assert.Equal(expected, PortugueseAllophoneFeatures.EuropeanDefault);
        }

        [Fact]
        public void Presets_All_HasAllBits()
        {
            Assert.Equal((PortugueseAllophoneFeatures)0x7F, PortugueseAllophoneFeatures.All);
        }

        [Fact]
        public void Presets_Obligatory_HasExpectedFlags()
        {
            var expected = PortugueseAllophoneFeatures.VowelReduction
                | PortugueseAllophoneFeatures.NasalAssimilation
                | PortugueseAllophoneFeatures.SibilantVoicingAssimilation;
            Assert.Equal(expected, PortugueseAllophoneFeatures.Obligatory);
        }

        // ================================================================
        // 14. 空の入力
        // ================================================================

        [Fact]
        public void Apply_EmptyPronunciation_ReturnsEmpty()
        {
            var empty = new PortuguesePronunciation(
                Array.Empty<PortuguesePhoneme>(),
                Array.Empty<int>(), -1);
            var result = AllophoneProcessor.Apply(empty, PortugueseAllophoneFeatures.All, PortugueseDialect.Brazilian);
            Assert.Empty(result.Phonemes);
        }

        [Fact]
        public void Apply_EmptyPronunciation_EP_ReturnsEmpty()
        {
            var empty = new PortuguesePronunciation(
                Array.Empty<PortuguesePhoneme>(),
                Array.Empty<int>(), -1);
            var result = AllophoneProcessor.Apply(empty, PortugueseAllophoneFeatures.All, PortugueseDialect.European);
            Assert.Empty(result.Phonemes);
        }

        // ================================================================
        // 追加: フラグ制御テスト
        // ================================================================

        [Fact]
        public void FeatureControl_DisabledLenition_NoLenition()
        {
            // Lenitionフラグなしの場合、母音間 b は保持
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0, 1 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.B, phonemes[1]);
        }

        [Fact]
        public void FeatureControl_DisabledTDPalatalization_NoPalatalization()
        {
            // TDPalatalizationフラグなしの場合、T+I は保持
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.I },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.T, phonemes[0]);
        }

        // ================================================================
        // 追加: 方言差の比較テスト
        // ================================================================

        [Fact]
        public void DialectDifference_VowelReduction_BP_vs_EP()
        {
            // 同じ非ストレス E が BP=I, EP=HighCentral になる
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.N, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.M, PortugueseIpaPhoneme.E },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var bpResult = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            var epResult = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.European);
            Assert.Equal(PortugueseIpaPhoneme.I, bpResult.Phonemes[3].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.HighCentral, epResult.Phonemes[3].Phoneme);
        }

        [Fact]
        public void DialectDifference_LAllophony_BP_vs_EP()
        {
            // 同じコーダ L が BP=W, EP=DarkL になる
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.O, PortugueseIpaPhoneme.L },
                new[] { 0 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var bpResult = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.Brazilian);
            var epResult = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.LAllophony, PortugueseDialect.European);
            Assert.Equal(PortugueseIpaPhoneme.W, bpResult.Phonemes[2].Phoneme);
            Assert.Equal(PortugueseIpaPhoneme.DarkL, epResult.Phonemes[2].Phoneme);
        }

        // ================================================================
        // 追加: SyllableOffsets と StressedSyllableIndex の保持
        // ================================================================

        [Fact]
        public void Apply_PreservesSyllableOffsetsAndStressIndex()
        {
            var phonemes = new[]
            {
                new PortuguesePhoneme(PortugueseIpaPhoneme.K, false),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A, true),
                new PortuguesePhoneme(PortugueseIpaPhoneme.Z, false),
                new PortuguesePhoneme(PortugueseIpaPhoneme.A, false),
            };
            var offsets = new[] { 0, 2 };
            var input = new PortuguesePronunciation(phonemes, offsets, 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.VowelReduction, PortugueseDialect.Brazilian);
            Assert.Equal(0, result.StressedSyllableIndex);
            // 音素数は変わらない（置換のみ）
            Assert.Equal(4, result.Phonemes.Count);
        }

        [Fact]
        public void Apply_PreservesIsStressedOnNonAffectedPhonemes()
        {
            // ストレス付き母音の IsStressed フラグが保持されることを確認
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.K, PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Z, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 1 });
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.NasalAssimilation, PortugueseDialect.Brazilian);
            // NasalAssimilation は何も変更しない入力なので、ストレスフラグが保持されるべき
            Assert.True(result.Phonemes[1].IsStressed);
            Assert.False(result.Phonemes[0].IsStressed);
        }

        // ================================================================
        // 追加: 複合規則の適用順序テスト
        // ================================================================

        [Fact]
        public void OrderingTest_SibilantVoicingThenPalatalization_EP()
        {
            // EP: /a s d a/ → 有声性同化 S→Z、後部歯茎化 Z→Zh
            var input = MakePronWithStress(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.S, PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0, stressedPositions: new[] { 0 });
            var features = PortugueseAllophoneFeatures.SibilantVoicingAssimilation
                | PortugueseAllophoneFeatures.SibilantPalatalization;
            var result = AllophoneProcessor.Apply(input, features, PortugueseDialect.European);
            var phonemes = GetPhonemes(result);
            // 有声性同化 S→Z、その後後部歯茎化 Z→Zh
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[1]);
        }

        // ================================================================
        // 追加: 歯擦音有声性同化 Sh/Zh テスト (S1)
        // ================================================================

        [Fact]
        public void SibilantVoicing_Sh_BeforeVoicedConsonant_BecomesZh()
        {
            // /a ʃ b a/ → /a ʒ b a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Sh, PortugueseIpaPhoneme.B, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[1]);
        }

        [Fact]
        public void SibilantVoicing_Zh_BeforeVoicelessConsonant_BecomesSh()
        {
            // /a ʒ p a/ → /a ʃ p a/
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Zh, PortugueseIpaPhoneme.P, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[1]);
        }

        [Fact]
        public void SibilantVoicing_Sh_BeforeVowel_NoChange()
        {
            // /ʃ a/ → /ʃ a/ (母音前は変化しない)
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.Sh, PortugueseIpaPhoneme.A },
                new[] { 0 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[0]);
        }

        [Fact]
        public void SibilantVoicing_Zh_BeforeVoicedConsonant_NoChange()
        {
            // /a ʒ d a/ → /a ʒ d a/ (ʒ は有声、次も有声なので変化なし)
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Zh, PortugueseIpaPhoneme.D, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Zh, phonemes[1]);
        }

        [Fact]
        public void SibilantVoicing_Sh_BeforeVoicelessConsonant_NoChange()
        {
            // /a ʃ t a/ → /a ʃ t a/ (ʃ は無声、次も無声なので変化なし)
            var input = MakePron(
                new[] { PortugueseIpaPhoneme.A, PortugueseIpaPhoneme.Sh, PortugueseIpaPhoneme.T, PortugueseIpaPhoneme.A },
                new[] { 0, 2 }, stressIndex: 0);
            var result = AllophoneProcessor.Apply(input, PortugueseAllophoneFeatures.SibilantVoicingAssimilation, PortugueseDialect.Brazilian);
            var phonemes = GetPhonemes(result);
            Assert.Equal(PortugueseIpaPhoneme.Sh, phonemes[1]);
        }
    }
}
