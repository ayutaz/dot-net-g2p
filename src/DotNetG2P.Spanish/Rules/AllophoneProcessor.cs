namespace DotNetG2P.Spanish.Rules
{
    /// <summary>
    /// スペイン語の異音規則を適用する。
    /// </summary>
    internal static class AllophoneProcessor
    {
        public static SpanishPronunciation Apply(SpanishPronunciation pronunciation, SpanishAllophoneFeatures features)
        {
            if (pronunciation.PhonemesInternal.Length == 0 || features == SpanishAllophoneFeatures.None)
                return pronunciation;

            var result = new SpanishPhoneme[pronunciation.PhonemesInternal.Length];
            for (var i = 0; i < pronunciation.PhonemesInternal.Length; i++)
            {
                var current = pronunciation.PhonemesInternal[i];
                var previous = i > 0 ? pronunciation.PhonemesInternal[i - 1] : default;
                var hasNext = i + 1 < pronunciation.PhonemesInternal.Length;
                var next = hasNext ? pronunciation.PhonemesInternal[i + 1] : default;
                var transformed = current.Phoneme;

                if (HasFeature(features, SpanishAllophoneFeatures.Lenition))
                {
                    transformed = ApplyLenition(i, previous.Phoneme, hasNext ? next.Phoneme : (SpanishIpaPhoneme?)null, transformed);
                }

                if (hasNext && HasFeature(features, SpanishAllophoneFeatures.NasalAssimilation) && transformed == SpanishIpaPhoneme.N)
                {
                    transformed = AssimilateNasal(next.Phoneme);
                }

                if (HasFeature(features, SpanishAllophoneFeatures.SVoicing)
                    && transformed == SpanishIpaPhoneme.S
                    && hasNext
                    && IsVoicedConsonant(next.Phoneme))
                {
                    transformed = SpanishIpaPhoneme.Z;
                }

                if (HasFeature(features, SpanishAllophoneFeatures.YeAffrication)
                    && transformed == SpanishIpaPhoneme.Y
                    && (IsWordInitial(i) || IsNasal(previous.Phoneme)))
                {
                    transformed = SpanishIpaPhoneme.YAffricate;
                }

                if (HasFeature(features, SpanishAllophoneFeatures.FinalDSoftening)
                    && transformed == SpanishIpaPhoneme.D
                    && i == pronunciation.PhonemesInternal.Length - 1)
                {
                    transformed = SpanishIpaPhoneme.Dh;
                }

                result[i] = new SpanishPhoneme(transformed, current.IsStressed);
            }

            return new SpanishPronunciation(result, pronunciation.SyllableOffsetsInternal, pronunciation.StressedSyllableIndex);
        }

        private static SpanishIpaPhoneme ApplyLenition(int index, SpanishIpaPhoneme previous, SpanishIpaPhoneme? next, SpanishIpaPhoneme current)
        {
            switch (current)
            {
                case SpanishIpaPhoneme.B:
                    return IsWordInitial(index) || IsNasal(previous)
                        ? SpanishIpaPhoneme.B
                        : SpanishIpaPhoneme.Beta;

                case SpanishIpaPhoneme.D:
                    if (index == 0)
                        return SpanishIpaPhoneme.D;

                    if (previous == SpanishIpaPhoneme.L || IsNasal(previous))
                        return SpanishIpaPhoneme.D;

                    return next == null
                        ? SpanishIpaPhoneme.D
                        : SpanishIpaPhoneme.Dh;

                case SpanishIpaPhoneme.G:
                    return IsWordInitial(index) || IsNasal(previous)
                        ? SpanishIpaPhoneme.G
                        : SpanishIpaPhoneme.Gh;

                default:
                    return current;
            }
        }

        private static bool HasFeature(SpanishAllophoneFeatures value, SpanishAllophoneFeatures feature)
        {
            return (value & feature) == feature;
        }

        private static bool IsWordInitial(int index) => index == 0;

        private static bool IsNasal(SpanishIpaPhoneme phoneme)
        {
            return phoneme == SpanishIpaPhoneme.M
                || phoneme == SpanishIpaPhoneme.N
                || phoneme == SpanishIpaPhoneme.Ny
                || phoneme == SpanishIpaPhoneme.NLabiodental
                || phoneme == SpanishIpaPhoneme.Eng
                || phoneme == SpanishIpaPhoneme.NDental;
        }

        private static bool IsVoicedConsonant(SpanishIpaPhoneme phoneme)
        {
            switch (phoneme)
            {
                case SpanishIpaPhoneme.B:
                case SpanishIpaPhoneme.Beta:
                case SpanishIpaPhoneme.D:
                case SpanishIpaPhoneme.Dh:
                case SpanishIpaPhoneme.G:
                case SpanishIpaPhoneme.Gh:
                case SpanishIpaPhoneme.M:
                case SpanishIpaPhoneme.N:
                case SpanishIpaPhoneme.Ny:
                case SpanishIpaPhoneme.NLabiodental:
                case SpanishIpaPhoneme.Eng:
                case SpanishIpaPhoneme.L:
                case SpanishIpaPhoneme.R:
                case SpanishIpaPhoneme.Rr:
                case SpanishIpaPhoneme.Y:
                case SpanishIpaPhoneme.YAffricate:
                case SpanishIpaPhoneme.Ll:
                    return true;
                default:
                    return false;
            }
        }

        private static SpanishIpaPhoneme AssimilateNasal(SpanishIpaPhoneme next)
        {
            switch (next)
            {
                case SpanishIpaPhoneme.P:
                case SpanishIpaPhoneme.B:
                case SpanishIpaPhoneme.Beta:
                case SpanishIpaPhoneme.M:
                    return SpanishIpaPhoneme.M;

                case SpanishIpaPhoneme.F:
                    return SpanishIpaPhoneme.NLabiodental;

                case SpanishIpaPhoneme.K:
                case SpanishIpaPhoneme.G:
                case SpanishIpaPhoneme.Gh:
                case SpanishIpaPhoneme.X:
                    return SpanishIpaPhoneme.Eng;

                case SpanishIpaPhoneme.Ch:
                case SpanishIpaPhoneme.Y:
                case SpanishIpaPhoneme.YAffricate:
                case SpanishIpaPhoneme.Ll:
                case SpanishIpaPhoneme.Ny:
                case SpanishIpaPhoneme.Sh:
                    return SpanishIpaPhoneme.Ny;

                case SpanishIpaPhoneme.T:
                case SpanishIpaPhoneme.D:
                case SpanishIpaPhoneme.Dh:
                case SpanishIpaPhoneme.Th:
                    return SpanishIpaPhoneme.NDental;

                default:
                    return SpanishIpaPhoneme.N;
            }
        }
    }
}
