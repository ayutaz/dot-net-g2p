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

                if (IsCompatibilityJamo(c))
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

            var result = new List<KoreanPhoneme>(syllables.Count * 3);
            for (var i = 0; i < syllables.Count; i++)
            {
                if (syllables[i].IsBoundary)
                    continue;

                result.AddRange(syllables[i].ToPhonemes());
            }
            return result.ToArray();
        }

        public static bool IsCompatibilityJamo(char c)
        {
            return c >= '\u3131' && c <= '\u318E';
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

        public static bool IsUiVariationCandidate(KoreanSyllable syllable)
        {
            return syllable.Onset == 'ㅇ'
                && syllable.Nucleus == 'ㅢ'
                && !syllable.HasCoda;
        }
    }
}
