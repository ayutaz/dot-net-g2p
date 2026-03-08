using System;
using System.Collections.Generic;

namespace DotNetG2P.Spanish.Rules
{
    /// <summary>
    /// スペイン語の強勢位置決定。
    /// </summary>
    internal static class StressAssigner
    {
        public static int GetStressedSyllableIndex(string word, IReadOnlyList<SpanishSyllable> syllables)
        {
            if (string.IsNullOrEmpty(word) || syllables.Count == 0)
                return -1;

            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                for (var j = 0; j < syllable.Length; j++)
                {
                    if (SpanishOrthography.HasWrittenAccent(word[syllable.StartIndex + j]))
                        return i;
                }
            }

            if (syllables.Count == 1)
                return 0;

            var last = char.ToLowerInvariant(word[word.Length - 1]);
            var endsWithVowelOrNS = SpanishOrthography.IsPronouncedVowelChar(last) || last == 'n' || last == 's';
            return endsWithVowelOrNS ? syllables.Count - 2 : syllables.Count - 1;
        }

        public static IReadOnlyList<SpanishSyllable> MarkStress(string word, IReadOnlyList<SpanishSyllable> syllables)
        {
            if (syllables.Count == 0)
                return syllables;

            var stressed = GetStressedSyllableIndex(word, syllables);
            var result = new SpanishSyllable[syllables.Count];
            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                result[i] = new SpanishSyllable(syllable.StartIndex, syllable.Length, syllable.Text, i == stressed);
            }

            return result;
        }
    }
}
