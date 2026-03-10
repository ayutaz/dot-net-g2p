using System;
using System.Collections.Generic;
using DotNetG2P.Portuguese;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Tests.PortugueseG2P
{
    public class StressAssignerTests
    {
        // ヘルパー: 音節リストを手動で作成
        private static IReadOnlyList<PortugueseSyllable> MakeSyllables(string word, params string[] parts)
        {
            var result = new PortugueseSyllable[parts.Length];
            var offset = 0;
            for (var i = 0; i < parts.Length; i++)
            {
                var idx = word.IndexOf(parts[i], offset, StringComparison.Ordinal);
                if (idx < 0)
                    throw new ArgumentException($"Part '{parts[i]}' not found in '{word}' starting at {offset}");
                result[i] = new PortugueseSyllable(idx, parts[i].Length, parts[i]);
                offset = idx + parts[i].Length;
            }
            return result;
        }

        // ================================================================
        // Phase 1: 鋭アクセント（acento agudo）
        // ================================================================

        [Theory]
        [InlineData("café", new[] { "ca", "fé" }, 1)]        // é → 最終音節
        [InlineData("avó", new[] { "a", "vó" }, 1)]          // ó → 最終音節
        [InlineData("máquina", new[] { "má", "qui", "na" }, 0)] // á → 前々末音節
        [InlineData("último", new[] { "úl", "ti", "mo" }, 0)]  // ú → 前々末音節
        [InlineData("médico", new[] { "mé", "di", "co" }, 0)]  // é → 前々末音節
        public void Phase1_AcuteAccent_ReturnsCorrectIndex(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // Phase 1: 曲折アクセント（acento circunflexo）
        // ================================================================

        [Theory]
        [InlineData("você", new[] { "vo", "cê" }, 1)]        // ê → 最終音節
        [InlineData("avô", new[] { "a", "vô" }, 1)]          // ô → 最終音節
        public void Phase1_CircumflexAccent_ReturnsCorrectIndex(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // Phase 2: チルダ（til）
        // ================================================================

        [Theory]
        [InlineData("irmã", new[] { "ir", "mã" }, 1)]        // ã → 最終音節
        [InlineData("irmão", new[] { "ir", "mão" }, 1)]      // ã → 最終音節（鼻二重母音内）
        public void Phase2_Tilde_ReturnsCorrectIndex(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Fact]
        public void Phase2_Tilde_IgnoredWhenAcutePresent()
        {
            // órfão: ó は Phase1 で検出、ã は無視される → 前々末音節
            var word = "órfão";
            var syllables = MakeSyllables(word, "ór", "fão");

            Assert.Equal(0, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Fact]
        public void Phase2_Tilde_CancaoStressOnTilde()
        {
            // canção: アクセント記号なし → Phase 2 で ã を検出
            var word = "canção";
            var syllables = MakeSyllables(word, "can", "ção");

            Assert.Equal(1, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // Phase 3: デフォルトルール - Paroxytone
        // ================================================================

        [Theory]
        [InlineData("casa", new[] { "ca", "sa" }, 0)]        // -a 末尾 → 次末
        [InlineData("gente", new[] { "gen", "te" }, 0)]      // -e 末尾 → 次末
        [InlineData("bonito", new[] { "bo", "ni", "to" }, 1)] // -o 末尾 → 次末
        public void Phase3_VowelEnding_Paroxytone(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Theory]
        [InlineData("casas", new[] { "ca", "sas" }, 0)]      // -as 末尾 → 次末
        [InlineData("gentes", new[] { "gen", "tes" }, 0)]    // -es 末尾 → 次末
        [InlineData("bonitos", new[] { "bo", "ni", "tos" }, 1)] // -os 末尾 → 次末
        public void Phase3_VowelPlusSEnding_Paroxytone(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Theory]
        [InlineData("falam", new[] { "fa", "lam" }, 0)]      // -am 末尾 → 次末
        [InlineData("dizem", new[] { "di", "zem" }, 0)]      // -em 末尾 → 次末
        [InlineData("jovens", new[] { "jo", "vens" }, 0)]    // -ens 末尾 → 次末
        public void Phase3_NasalEnding_Paroxytone(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // Phase 3: デフォルトルール - Oxytone
        // ================================================================

        [Theory]
        [InlineData("falar", new[] { "fa", "lar" }, 1)]       // -r 末尾 → 最終
        [InlineData("animal", new[] { "a", "ni", "mal" }, 2)] // -l 末尾 → 最終
        [InlineData("rapaz", new[] { "ra", "paz" }, 1)]       // -z 末尾 → 最終
        public void Phase3_ConsonantEnding_Oxytone(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Theory]
        [InlineData("compreendi", new[] { "com", "pre", "en", "di" }, 3)] // -i 末尾 → 最終
        [InlineData("peru", new[] { "pe", "ru" }, 1)]                     // -u 末尾 → 最終
        public void Phase3_WeakVowelEnding_Oxytone(string word, string[] parts, int expected)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(expected, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // 単音節語
        // ================================================================

        [Theory]
        [InlineData("sol", new[] { "sol" })]
        [InlineData("mar", new[] { "mar" })]
        public void Monosyllable_ReturnsZero(string word, string[] parts)
        {
            var syllables = MakeSyllables(word, parts);

            Assert.Equal(0, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        [Fact]
        public void Monosyllable_WithTilde_ReturnsZero()
        {
            // pão: ã で Phase 2 で検出 → 0
            var word = "pão";
            var syllables = MakeSyllables(word, "pão");

            Assert.Equal(0, StressAssigner.GetStressedSyllableIndex(word, syllables));
        }

        // ================================================================
        // エッジケース
        // ================================================================

        [Fact]
        public void EmptySyllables_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StressAssigner.GetStressedSyllableIndex("", Array.Empty<PortugueseSyllable>()));
        }

        [Fact]
        public void NullWord_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StressAssigner.GetStressedSyllableIndex(null!, Array.Empty<PortugueseSyllable>()));
        }

        // ================================================================
        // MarkStress テスト
        // ================================================================

        [Fact]
        public void MarkStress_EmptySyllables_ReturnsEmpty()
        {
            var result = StressAssigner.MarkStress("", Array.Empty<PortugueseSyllable>());

            Assert.Empty(result);
        }

        [Fact]
        public void MarkStress_TwoSyllable_SetsCorrectStress()
        {
            // casa → ca|sa → 音節0にストレス
            var word = "casa";
            var syllables = MakeSyllables(word, "ca", "sa");

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(2, result.Count);
            Assert.True(result[0].IsStressed);
            Assert.False(result[1].IsStressed);
        }

        [Fact]
        public void MarkStress_ThreeSyllable_SetsCorrectStress()
        {
            // médico → mé|di|co → 音節0にストレス (Phase 1)
            var word = "médico";
            var syllables = MakeSyllables(word, "mé", "di", "co");

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(3, result.Count);
            Assert.True(result[0].IsStressed);
            Assert.False(result[1].IsStressed);
            Assert.False(result[2].IsStressed);
        }

        [Fact]
        public void MarkStress_PreservesTextAndPositionInfo()
        {
            var word = "bonito";
            var syllables = MakeSyllables(word, "bo", "ni", "to");

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(syllables.Count, result.Count);
            for (var i = 0; i < syllables.Count; i++)
            {
                Assert.Equal(syllables[i].Text, result[i].Text);
                Assert.Equal(syllables[i].StartIndex, result[i].StartIndex);
                Assert.Equal(syllables[i].Length, result[i].Length);
            }
        }

        [Fact]
        public void MarkStress_Monosyllable_SingleSyllableIsStressed()
        {
            var word = "sol";
            var syllables = MakeSyllables(word, "sol");

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Single(result);
            Assert.True(result[0].IsStressed);
        }

        [Fact]
        public void MarkStress_Oxytone_LastSyllableStressed()
        {
            // falar → fa|lar → 音節1にストレス
            var word = "falar";
            var syllables = MakeSyllables(word, "fa", "lar");

            var result = StressAssigner.MarkStress(word, syllables);

            Assert.Equal(2, result.Count);
            Assert.False(result[0].IsStressed);
            Assert.True(result[1].IsStressed);
        }
    }
}
