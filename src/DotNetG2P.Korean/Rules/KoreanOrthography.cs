using System;
using System.Collections.Generic;
namespace DotNetG2P.Korean.Rules
{
    internal static class KoreanOrthography
    {
        public static KoreanSyllable[] DecomposeText(string text, bool preserveNonHangul)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<KoreanSyllable>();

            var syllables = new List<KoreanSyllable>(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    syllables.Add(KoreanSyllable.FromBoundary(c));
                    continue;
                }

                if (KoreanSyllable.TryDecompose(c, out var syllable))
                {
                    syllables.Add(syllable);
                    continue;
                }

                if (IsStandaloneJamo(c))
                {
                    syllables.Add(KoreanSyllable.FromStandaloneJamo(c));
                    continue;
                }

                if (preserveNonHangul)
                    syllables.Add(KoreanSyllable.FromStandaloneJamo(c));
            }

            return syllables.ToArray();
        }

        public static KoreanPhoneme[] FlattenPhonemes(IReadOnlyList<KoreanSyllable> syllables)
        {
            if (syllables.Count == 0)
                return Array.Empty<KoreanPhoneme>();

            var totalPhonemes = 0;
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                if (syllable.IsBoundary)
                    continue;

                totalPhonemes += syllable.HasNucleus
                    ? (syllable.HasCoda ? 3 : 2)
                    : 1;
            }

            if (totalPhonemes == 0)
                return Array.Empty<KoreanPhoneme>();

            var result = new KoreanPhoneme[totalPhonemes];
            var index = 0;
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                if (syllable.IsBoundary)
                    continue;

                result[index++] = new KoreanPhoneme(syllable.Onset);
                if (!syllable.HasNucleus)
                    continue;

                result[index++] = new KoreanPhoneme(syllable.Nucleus);
                if (syllable.HasCoda)
                    result[index++] = new KoreanPhoneme(syllable.Coda);
            }

            return result;
        }

        public static bool IsCompatibilityJamo(char c)
        {
            return c >= '\u3131' && c <= '\u318E';
        }

        public static bool IsStandaloneJamo(char c)
        {
            return IsCompatibilityJamo(c)
                || (c >= '\u1100' && c <= '\u11FF');
        }

        public static bool IsHangulSyllable(KoreanSyllable syllable)
        {
            return syllable.HasNucleus && !syllable.IsBoundary;
        }

        public static bool IsSilentIeung(KoreanSyllable syllable)
        {
            return syllable.HasNucleus && syllable.Onset == 'ㅇ';
        }

        public static bool IsIotizedVowel(char nucleus)
        {
            switch (nucleus)
            {
                case 'ㅣ':
                case 'ㅑ':
                case 'ㅒ':
                case 'ㅕ':
                case 'ㅖ':
                case 'ㅛ':
                case 'ㅠ':
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsNInsertionTarget(KoreanSyllable next)
        {
            return next.Onset == 'ㅇ'
                && IsNInsertionVowel(next.Nucleus);
        }

        public static bool IsNInsertionVowel(char nucleus)
        {
            switch (nucleus)
            {
                case 'ㅣ':
                case 'ㅑ':
                case 'ㅕ':
                case 'ㅛ':
                case 'ㅠ':
                    return true;

                default:
                    return false;
            }
        }

    }
}
