namespace DotNetG2P.Korean.Rules
{
    internal static class AssimilationProcessor
    {
        public static void ApplyHTransformations(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 1; i++)
            {
                var current = syllables[i];
                var next = syllables[i + 1];
                if (!CanInspectPair(current, next) || !current.HasCoda || !BatchimProcessor.IsHFamily(current.Coda))
                    continue;

                if (KoreanOrthography.IsSilentIeung(next))
                {
                    var movedOnset = BatchimProcessor.RemoveHComponent(current.Coda);
                    syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, '\0');
                    if (movedOnset != '\0')
                        syllables[i + 1] = new KoreanSyllable(movedOnset, next.Nucleus, next.Coda);
                    continue;
                }

                if (BatchimProcessor.TryAspirateOnsetAfterH(next.Onset, out var aspiratedOnset))
                {
                    var residualCoda = BatchimProcessor.RemoveHComponent(current.Coda);
                    syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, residualCoda);
                    syllables[i + 1] = new KoreanSyllable(aspiratedOnset, next.Nucleus, next.Coda);
                    continue;
                }

                if (next.Onset == 'ㄴ' || next.Onset == 'ㅁ')
                {
                    var resolvedCoda = current.Coda == 'ㅎ'
                        ? 'ㄴ'
                        : BatchimProcessor.RemoveHComponent(current.Coda);
                    syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, resolvedCoda);
                }
            }
        }

        public static void ApplyNInsertion(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 2; i++)
            {
                var current = syllables[i];
                var boundary = syllables[i + 1];
                var next = syllables[i + 2];
                if (!KoreanOrthography.IsHangulSyllable(current)
                    || !boundary.IsBoundary
                    || !KoreanOrthography.IsHangulSyllable(next)
                    || !current.HasCoda)
                    continue;

                if (!KoreanOrthography.IsNInsertionTarget(next))
                    continue;

                var insertedOnset = BatchimProcessor.GetNInsertionOnset(current.Coda);
                syllables[i + 2] = new KoreanSyllable(insertedOnset, next.Nucleus, next.Coda);
            }
        }

        public static void ApplyResyllabification(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 1; i++)
            {
                var current = syllables[i];
                var next = syllables[i + 1];
                if (!CanInspectPair(current, next) || !current.HasCoda || !KoreanOrthography.IsSilentIeung(next))
                    continue;

                if (!BatchimProcessor.TrySplitForLiaison(current.Coda, out var retainedCoda, out var movedOnset))
                    continue;

                movedOnset = ApplyPalatalization(movedOnset, next.Nucleus);
                syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, retainedCoda);
                syllables[i + 1] = new KoreanSyllable(movedOnset, next.Nucleus, next.Coda);
            }
        }

        public static void ApplyLiquidization(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 1; i++)
            {
                var current = syllables[i];
                var next = syllables[i + 1];
                if (!CanInspectPair(current, next) || !current.HasCoda)
                    continue;

                var surfaceCoda = BatchimProcessor.GetSurfaceCodaBeforeConsonant(current, next);
                if (surfaceCoda == 'ㄴ' && next.Onset == 'ㄹ')
                {
                    syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, 'ㄹ');
                    continue;
                }

                if (surfaceCoda == 'ㄹ' && next.Onset == 'ㄴ')
                    syllables[i + 1] = new KoreanSyllable('ㄹ', next.Nucleus, next.Coda);
            }
        }

        public static void ApplyNasalization(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 1; i++)
            {
                var current = syllables[i];
                var next = syllables[i + 1];
                if (!CanInspectPair(current, next) || !current.HasCoda)
                    continue;

                if (next.Onset != 'ㄴ' && next.Onset != 'ㅁ')
                    continue;

                var nasalCoda = BatchimProcessor.ToNasalCoda(current, next);
                if (nasalCoda != current.Coda)
                    syllables[i] = new KoreanSyllable(current.Onset, current.Nucleus, nasalCoda);
            }
        }

        public static void ApplyTensification(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length - 1; i++)
            {
                var current = syllables[i];
                var next = syllables[i + 1];
                if (!CanInspectPair(current, next) || !current.HasCoda)
                    continue;

                var triggerCoda = BatchimProcessor.GetTensificationTriggerCoda(current, next);
                var surfaceCoda = BatchimProcessor.GetSurfaceCodaBeforeConsonant(current, next);
                if (surfaceCoda != current.Coda)
                {
                    current = new KoreanSyllable(current.Onset, current.Nucleus, surfaceCoda);
                    syllables[i] = current;
                }

                if (!BatchimProcessor.CanTriggerTensification(triggerCoda))
                    continue;

                var tensified = BatchimProcessor.TensifyOnset(next.Onset);
                if (tensified != next.Onset)
                    syllables[i + 1] = new KoreanSyllable(tensified, next.Nucleus, next.Coda);
            }
        }

        public static void ApplyFinalNeutralization(KoreanSyllable[] syllables)
        {
            for (var i = 0; i < syllables.Length; i++)
            {
                var syllable = syllables[i];
                if (!syllable.HasNucleus || !syllable.HasCoda)
                    continue;

                var representative = BatchimProcessor.ToRepresentativeCoda(syllable.Coda);
                if (representative != syllable.Coda)
                    syllables[i] = new KoreanSyllable(syllable.Onset, syllable.Nucleus, representative);
            }
        }

        private static bool CanInspectPair(KoreanSyllable current, KoreanSyllable next)
        {
            return KoreanOrthography.IsHangulSyllable(current)
                && KoreanOrthography.IsHangulSyllable(next);
        }

        private static char ApplyPalatalization(char onset, char nextNucleus)
        {
            if (!KoreanOrthography.IsIotizedVowel(nextNucleus))
                return onset;

            switch (onset)
            {
                case 'ㄷ':
                    return 'ㅈ';

                case 'ㅌ':
                    return 'ㅊ';

                default:
                    return onset;
            }
        }
    }
}
