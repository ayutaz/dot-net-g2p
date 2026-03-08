using System;

namespace DotNetG2P.Spanish.Rules
{
    /// <summary>
    /// スペイン語の異音規則を適用する。
    /// </summary>
    internal static class AllophoneProcessor
    {
        public static SpanishPronunciation Apply(SpanishPronunciation pronunciation)
        {
            if (pronunciation.PhonemesInternal.Length == 0)
                return pronunciation;

            var result = new SpanishPhoneme[pronunciation.PhonemesInternal.Length];
            for (var i = 0; i < pronunciation.PhonemesInternal.Length; i++)
            {
                var current = pronunciation.PhonemesInternal[i];
                var previous = i > 0 ? pronunciation.PhonemesInternal[i - 1] : default;
                var next = i + 1 < pronunciation.PhonemesInternal.Length ? pronunciation.PhonemesInternal[i + 1] : default;
                var transformed = current.Phoneme;

                switch (current.Phoneme)
                {
                    case SpanishIpaPhoneme.B:
                        transformed = IsWordInitial(i) || IsNasal(previous.Phoneme)
                            ? SpanishIpaPhoneme.B
                            : SpanishIpaPhoneme.Beta;
                        break;

                    case SpanishIpaPhoneme.D:
                        transformed = IsWordInitial(i) || IsNasal(previous.Phoneme) || previous.Phoneme == SpanishIpaPhoneme.L
                            ? SpanishIpaPhoneme.D
                            : SpanishIpaPhoneme.Dh;
                        break;

                    case SpanishIpaPhoneme.G:
                        transformed = IsWordInitial(i) || IsNasal(previous.Phoneme)
                            ? SpanishIpaPhoneme.G
                            : SpanishIpaPhoneme.Gh;
                        break;

                    case SpanishIpaPhoneme.N:
                        transformed = AssimilateNasal(next.Phoneme);
                        break;

                    case SpanishIpaPhoneme.S:
                        if (IsVoicedConsonant(next.Phoneme))
                            transformed = SpanishIpaPhoneme.Z;
                        break;
                }

                result[i] = new SpanishPhoneme(transformed, current.IsStressed);
            }

            return new SpanishPronunciation(result, pronunciation.SyllableOffsetsInternal, pronunciation.StressedSyllableIndex);
        }

        private static bool IsWordInitial(int index) => index == 0;

        private static bool IsNasal(SpanishIpaPhoneme phoneme)
        {
            return phoneme == SpanishIpaPhoneme.M
                || phoneme == SpanishIpaPhoneme.N
                || phoneme == SpanishIpaPhoneme.Ny
                || phoneme == SpanishIpaPhoneme.NLabiodental
                || phoneme == SpanishIpaPhoneme.Eng;
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
                case SpanishIpaPhoneme.Ll:
                case SpanishIpaPhoneme.Ny:
                case SpanishIpaPhoneme.Sh:
                    return SpanishIpaPhoneme.Ny;

                default:
                    return SpanishIpaPhoneme.N;
            }
        }
    }
}
