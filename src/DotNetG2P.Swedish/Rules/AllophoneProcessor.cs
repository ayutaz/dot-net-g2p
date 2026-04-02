using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語の異音規則を適用する。
    /// 適用順序:
    ///   1. Retroflexion制御 — FinlandSwedish時はそり舌音を歯茎音に戻す
    ///   2. TjAffrication — FinlandSwedish時はɕをt͡ɕに変換
    ///   3. VowelLengthMarking — 現在はPhase 3で処理済みのため追加処理なし（将来拡張ポイント）
    /// </summary>
    internal static class AllophoneProcessor
    {
        public static SwedishPronunciation Apply(
            SwedishPronunciation pronunciation,
            SwedishAllophoneFeatures features,
            SwedishDialect dialect)
        {
            if (pronunciation.PhonemesInternal.Length == 0 || features == SwedishAllophoneFeatures.None)
                return pronunciation;

            var source = pronunciation.PhonemesInternal;
            var syllableOffsets = pronunciation.SyllableOffsetsInternal;
            var needsRebuilding = false;

            // 1. FinlandSwedish de-retroflexion: Retroflexion フラグが OFF の場合、
            //    そり舌音を r+歯茎音に展開する（1→2音素展開のためList使用）
            if (!HasFeature(features, SwedishAllophoneFeatures.Retroflexion))
            {
                // そり舌音が存在するか事前チェック
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i].IsRetroflex)
                    {
                        needsRebuilding = true;
                        break;
                    }
                }
            }

            if (needsRebuilding)
            {
                return RebuildWithDeretroflexion(pronunciation, features, dialect);
            }

            // 2. TjAffrication: ɕ → t͡ɕ（FinlandSwedish用、in-place可能）
            if (HasFeature(features, SwedishAllophoneFeatures.TjAffrication))
            {
                var modified = false;
                var result = new SwedishPhoneme[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i].Phoneme == SwedishIpaPhoneme.Tj)
                    {
                        result[i] = new SwedishPhoneme(SwedishIpaPhoneme.TjAffricate, source[i].IsStressed, source[i].IsSyllableNucleus);
                        modified = true;
                    }
                    else
                    {
                        result[i] = source[i];
                    }
                }
                if (modified)
                {
                    return new SwedishPronunciation(result, syllableOffsets, pronunciation.StressedSyllableIndex, pronunciation.Accent);
                }
            }

            return pronunciation;
        }

        /// <summary>そり舌音をr+歯茎音に展開した新しいPronunciationを構築する。</summary>
        private static SwedishPronunciation RebuildWithDeretroflexion(
            SwedishPronunciation pronunciation, SwedishAllophoneFeatures features, SwedishDialect dialect)
        {
            var source = pronunciation.PhonemesInternal;
            var oldOffsets = pronunciation.SyllableOffsetsInternal;
            var phonemes = new List<SwedishPhoneme>(source.Length + 5);
            var newOffsets = new int[oldOffsets.Length];

            var currentSyllable = 0;
            for (var i = 0; i < source.Length; i++)
            {
                // 音節開始判定
                if (currentSyllable < oldOffsets.Length && i == oldOffsets[currentSyllable])
                {
                    newOffsets[currentSyllable] = phonemes.Count;
                    currentSyllable++;
                }

                var p = source[i];
                if (p.IsRetroflex)
                {
                    // そり舌音 → r + 歯茎音
                    var dental = GetDentalCounterpart(p.Phoneme);
                    phonemes.Add(new SwedishPhoneme(SwedishIpaPhoneme.R, p.IsStressed));
                    phonemes.Add(new SwedishPhoneme(dental, p.IsStressed, p.IsSyllableNucleus));
                }
                else if (HasFeature(features, SwedishAllophoneFeatures.TjAffrication)
                         && p.Phoneme == SwedishIpaPhoneme.Tj)
                {
                    phonemes.Add(new SwedishPhoneme(SwedishIpaPhoneme.TjAffricate, p.IsStressed, p.IsSyllableNucleus));
                }
                else
                {
                    phonemes.Add(p);
                }
            }

            return new SwedishPronunciation(
                phonemes.ToArray(), newOffsets, pronunciation.StressedSyllableIndex, pronunciation.Accent);
        }

        private static SwedishIpaPhoneme GetDentalCounterpart(SwedishIpaPhoneme retro)
        {
            switch (retro)
            {
                case SwedishIpaPhoneme.RetroT: return SwedishIpaPhoneme.T;
                case SwedishIpaPhoneme.RetroD: return SwedishIpaPhoneme.D;
                case SwedishIpaPhoneme.RetroN: return SwedishIpaPhoneme.N;
                case SwedishIpaPhoneme.RetroL: return SwedishIpaPhoneme.L;
                case SwedishIpaPhoneme.RetroS: return SwedishIpaPhoneme.S;
                default: return retro;
            }
        }

        private static bool HasFeature(SwedishAllophoneFeatures features, SwedishAllophoneFeatures flag)
            => (features & flag) == flag;
    }
}
