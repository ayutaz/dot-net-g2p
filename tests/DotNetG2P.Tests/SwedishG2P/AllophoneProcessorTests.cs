using System;
using DotNetG2P.Swedish;
using DotNetG2P.Swedish.Rules;
using Xunit;

namespace DotNetG2P.Tests.SwedishG2P
{
    public class AllophoneProcessorTests
    {
        // ヘルパー: SwedishPronunciation を簡易構築する
        private static SwedishPronunciation MakePron(SwedishPhoneme[] phonemes, int[] syllableOffsets, int stressedIndex = 0, byte accent = 1)
        {
            return new SwedishPronunciation(phonemes, syllableOffsets, stressedIndex, accent);
        }

        private static SwedishPhoneme Ph(SwedishIpaPhoneme p, bool stressed = false, bool nucleus = false)
        {
            return new SwedishPhoneme(p, stressed, nucleus);
        }

        // =================================================================
        // 1. Retroflexion有効 — そり舌音維持
        // =================================================================

        [Fact]
        public void Apply_Retroflexion有効_そり舌音維持()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.K),
                Ph(SwedishIpaPhoneme.RetroT, stressed: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.CentralDefault, SwedishDialect.Central);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.RetroT, result.Phonemes[1].Phoneme);
        }

        // =================================================================
        // 2-6. Retroflexion無効 — 各そり舌音の展開
        // =================================================================

        [Fact]
        public void Apply_Retroflexion無効_rt展開()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroT) };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.T, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_Retroflexion無効_rd展開()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroD) };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.D, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_Retroflexion無効_rn展開()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroN) };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.N, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_Retroflexion無効_rl展開()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroL) };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.L, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_Retroflexion無効_rs展開()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroS) };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.S, result.Phonemes[1].Phoneme);
        }

        // =================================================================
        // 7-8. TjAffrication
        // =================================================================

        [Fact]
        public void Apply_TjAffrication有効_変換()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.Tj, stressed: true),
                Ph(SwedishIpaPhoneme.ShortE, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.TjAffrication, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.TjAffricate, result.Phonemes[0].Phoneme);
            Assert.True(result.Phonemes[0].IsStressed);
        }

        [Fact]
        public void Apply_TjAffrication無効_維持()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.Tj),
                Ph(SwedishIpaPhoneme.ShortE, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.Retroflexion, SwedishDialect.Central);

            Assert.Equal(2, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.Tj, result.Phonemes[0].Phoneme);
        }

        // =================================================================
        // 9-10. 方言プリセット
        // =================================================================

        [Fact]
        public void Apply_CentralDefault_そり舌化維持_tj摩擦音()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT),
                Ph(SwedishIpaPhoneme.Tj),
                Ph(SwedishIpaPhoneme.LongI, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.CentralDefault, SwedishDialect.Central);

            Assert.Equal(3, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.RetroT, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.Tj, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_FinlandDefault_そり舌展開_tj破擦音()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT),
                Ph(SwedishIpaPhoneme.Tj),
                Ph(SwedishIpaPhoneme.LongI, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(4, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.T, result.Phonemes[1].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.TjAffricate, result.Phonemes[2].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.LongI, result.Phonemes[3].Phoneme);
        }

        // =================================================================
        // 11-13. エッジケース
        // =================================================================

        [Fact]
        public void Apply_None_全処理スキップ()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT),
                Ph(SwedishIpaPhoneme.Tj),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.None, SwedishDialect.Central);

            Assert.Same(pron, result);
        }

        [Fact]
        public void Apply_All_全処理適用()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT),
                Ph(SwedishIpaPhoneme.Tj),
                Ph(SwedishIpaPhoneme.ShortA, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.All, SwedishDialect.Central);

            Assert.Equal(3, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.RetroT, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.TjAffricate, result.Phonemes[1].Phoneme);
        }

        [Fact]
        public void Apply_空音素列_変更なし()
        {
            var pron = MakePron(Array.Empty<SwedishPhoneme>(), Array.Empty<int>(), -1);

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.All, SwedishDialect.Central);

            Assert.Same(pron, result);
        }

        // =================================================================
        // 14-15. 音節オフセット・複数そり舌音
        // =================================================================

        [Fact]
        public void Apply_Deretroflexion後_音節オフセット正しい()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT, stressed: true),
                Ph(SwedishIpaPhoneme.LongA, stressed: true, nucleus: true),
                Ph(SwedishIpaPhoneme.RetroS),
                Ph(SwedishIpaPhoneme.ShortI, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0, 2 }, stressedIndex: 0);

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(6, result.Phonemes.Count);
            Assert.Equal(0, result.SyllableOffsets[0]);
            Assert.Equal(3, result.SyllableOffsets[1]);
        }

        [Fact]
        public void Apply_複数そり舌音_全て展開()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.RetroT),
                Ph(SwedishIpaPhoneme.RetroD),
                Ph(SwedishIpaPhoneme.RetroN),
                Ph(SwedishIpaPhoneme.RetroL),
                Ph(SwedishIpaPhoneme.RetroS),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.VowelLengthMarking, SwedishDialect.Central);

            Assert.Equal(10, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.T, result.Phonemes[1].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[8].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.S, result.Phonemes[9].Phoneme);
        }

        // =================================================================
        // 16-20. 追加テスト
        // =================================================================

        [Fact]
        public void Apply_Accent値が保持される()
        {
            var phonemes = new[] { Ph(SwedishIpaPhoneme.RetroT), Ph(SwedishIpaPhoneme.LongA, nucleus: true) };
            var pron = MakePron(phonemes, new[] { 0 }, accent: 2);

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(2, result.Accent);
        }

        [Fact]
        public void Apply_TjAffrication有効_Tj音なし_元オブジェクト返却()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.K),
                Ph(SwedishIpaPhoneme.ShortA, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.TjAffrication, SwedishDialect.FinlandSwedish);

            Assert.Same(pron, result);
        }

        [Fact]
        public void Apply_StressedSyllableIndex保持()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.P),
                Ph(SwedishIpaPhoneme.ShortA, nucleus: true),
                Ph(SwedishIpaPhoneme.RetroT, stressed: true),
                Ph(SwedishIpaPhoneme.LongA, stressed: true, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0, 2 }, stressedIndex: 1);

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(1, result.StressedSyllableIndex);
        }

        [Fact]
        public void Apply_Deretroflexion_TjAffrication同時適用()
        {
            var phonemes = new[]
            {
                Ph(SwedishIpaPhoneme.Tj),
                Ph(SwedishIpaPhoneme.RetroS),
                Ph(SwedishIpaPhoneme.ShortA, nucleus: true),
            };
            var pron = MakePron(phonemes, new[] { 0 });

            var result = AllophoneProcessor.Apply(pron, SwedishAllophoneFeatures.FinlandDefault, SwedishDialect.FinlandSwedish);

            Assert.Equal(4, result.Phonemes.Count);
            Assert.Equal(SwedishIpaPhoneme.TjAffricate, result.Phonemes[0].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.R, result.Phonemes[1].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.S, result.Phonemes[2].Phoneme);
            Assert.Equal(SwedishIpaPhoneme.ShortA, result.Phonemes[3].Phoneme);
        }
    }
}
