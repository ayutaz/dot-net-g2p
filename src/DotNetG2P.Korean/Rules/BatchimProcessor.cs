namespace DotNetG2P.Korean.Rules
{
    internal static class BatchimProcessor
    {
        public static char ToRepresentativeCoda(char coda)
        {
            switch (coda)
            {
                case '\0':
                    return '\0';

                case 'ㄱ':
                case 'ㄲ':
                case 'ㅋ':
                case 'ㄳ':
                case 'ㄺ':
                    return 'ㄱ';

                case 'ㄴ':
                case 'ㄵ':
                case 'ㄶ':
                    return 'ㄴ';

                case 'ㄷ':
                case 'ㅅ':
                case 'ㅆ':
                case 'ㅈ':
                case 'ㅊ':
                case 'ㅌ':
                case 'ㅎ':
                    return 'ㄷ';

                case 'ㄹ':
                case 'ㄼ':
                case 'ㄽ':
                case 'ㄾ':
                case 'ㅀ':
                    return 'ㄹ';

                case 'ㄻ':
                case 'ㅁ':
                    return 'ㅁ';

                case 'ㅂ':
                case 'ㅄ':
                case 'ㅍ':
                case 'ㄿ':
                    return 'ㅂ';

                case 'ㅇ':
                    return 'ㅇ';

                default:
                    return coda;
            }
        }

        public static char ToNasalCoda(KoreanSyllable current, KoreanSyllable next)
        {
            return ToNasalCoda(GetSurfaceCodaBeforeConsonant(current, next));
        }

        public static char ToNasalCoda(char coda)
        {
            switch (ToRepresentativeCoda(coda))
            {
                case 'ㄱ':
                    return 'ㅇ';

                case 'ㄷ':
                    return 'ㄴ';

                case 'ㅂ':
                    return 'ㅁ';

                default:
                    return ToRepresentativeCoda(coda);
            }
        }

        public static bool TrySplitForLiaison(char coda, out char retainedCoda, out char movedOnset)
        {
            switch (coda)
            {
                case 'ㄳ':
                    retainedCoda = 'ㄱ';
                    movedOnset = 'ㅅ';
                    return true;

                case 'ㄵ':
                    retainedCoda = 'ㄴ';
                    movedOnset = 'ㅈ';
                    return true;

                case 'ㄺ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㄱ';
                    return true;

                case 'ㄻ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㅁ';
                    return true;

                case 'ㄼ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㅂ';
                    return true;

                case 'ㄽ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㅅ';
                    return true;

                case 'ㄾ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㅌ';
                    return true;

                case 'ㄿ':
                    retainedCoda = 'ㄹ';
                    movedOnset = 'ㅍ';
                    return true;

                case 'ㅄ':
                    retainedCoda = 'ㅂ';
                    movedOnset = 'ㅅ';
                    return true;

                case 'ㄱ':
                case 'ㄲ':
                case 'ㅋ':
                case 'ㄴ':
                case 'ㄷ':
                case 'ㄹ':
                case 'ㅁ':
                case 'ㅂ':
                case 'ㅅ':
                case 'ㅆ':
                case 'ㅈ':
                case 'ㅊ':
                case 'ㅌ':
                case 'ㅍ':
                    retainedCoda = '\0';
                    movedOnset = coda;
                    return true;

                default:
                    retainedCoda = '\0';
                    movedOnset = '\0';
                    return false;
            }
        }

        public static bool IsHFamily(char coda)
        {
            return coda == 'ㅎ'
                || coda == 'ㄶ'
                || coda == 'ㅀ';
        }

        public static char RemoveHComponent(char coda)
        {
            switch (coda)
            {
                case 'ㅎ':
                    return '\0';

                case 'ㄶ':
                    return 'ㄴ';

                case 'ㅀ':
                    return 'ㄹ';

                default:
                    return coda;
            }
        }

        public static bool TryAspirateOnsetAfterH(char onset, out char aspiratedOnset)
        {
            switch (onset)
            {
                case 'ㄱ':
                    aspiratedOnset = 'ㅋ';
                    return true;

                case 'ㄷ':
                    aspiratedOnset = 'ㅌ';
                    return true;

                case 'ㅈ':
                    aspiratedOnset = 'ㅊ';
                    return true;

                case 'ㅅ':
                    aspiratedOnset = 'ㅆ';
                    return true;

                default:
                    aspiratedOnset = '\0';
                    return false;
            }
        }

        public static char GetNInsertionOnset(char currentCoda)
        {
            return ToRepresentativeCoda(currentCoda) == 'ㄹ'
                ? 'ㄹ'
                : 'ㄴ';
        }

        public static char GetSurfaceCodaBeforeConsonant(KoreanSyllable current, KoreanSyllable next)
        {
            if (!current.HasCoda || !next.HasNucleus || KoreanOrthography.IsSilentIeung(next))
                return current.Coda;

            switch (current.Coda)
            {
                case 'ㄼ':
                    if (UsesBieupDominantSurface(current, next))
                        return 'ㅂ';

                    return 'ㄹ';

                default:
                    return ToRepresentativeCoda(current.Coda);
            }
        }

        public static bool CanTriggerTensification(char coda)
        {
            switch (ToRepresentativeCoda(coda))
            {
                case 'ㄱ':
                case 'ㄷ':
                case 'ㅂ':
                    return true;

                default:
                    return false;
            }
        }

        public static char TensifyOnset(char onset)
        {
            switch (onset)
            {
                case 'ㄱ':
                    return 'ㄲ';

                case 'ㄷ':
                    return 'ㄸ';

                case 'ㅂ':
                    return 'ㅃ';

                case 'ㅅ':
                    return 'ㅆ';

                case 'ㅈ':
                    return 'ㅉ';

                default:
                    return onset;
            }
        }

        private static bool UsesBieupDominantSurface(KoreanSyllable current, KoreanSyllable next)
        {
            if (current.Onset == 'ㅂ' && current.Nucleus == 'ㅏ')
                return true;

            if (current.Onset != 'ㄴ' || current.Nucleus != 'ㅓ')
                return false;

            return (next.Onset == 'ㅈ' && next.Nucleus == 'ㅜ' && next.Coda == 'ㄱ')
                || (next.Onset == 'ㄷ' && next.Nucleus == 'ㅜ' && next.Coda == 'ㅇ');
        }
    }
}
