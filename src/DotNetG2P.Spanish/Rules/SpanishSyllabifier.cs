using System;
using System.Collections.Generic;

namespace DotNetG2P.Spanish.Rules
{
    /// <summary>
    /// スペイン語の正書法ベース音節分割。
    /// </summary>
    internal static class SpanishSyllabifier
    {
        public static IReadOnlyList<SpanishSyllable> Syllabify(string word)
        {
            if (string.IsNullOrEmpty(word))
                return Array.Empty<SpanishSyllable>();

            var syllables = new List<SpanishSyllable>(4);
            var start = 0;

            while (start < word.Length)
            {
                var vowelStart = FindNextVowel(word, start);
                if (vowelStart < 0)
                {
                    if (syllables.Count == 0)
                    {
                        syllables.Add(new SpanishSyllable(start, word.Length - start, word.Substring(start)));
                    }
                    else
                    {
                        var last = syllables[syllables.Count - 1];
                        var merged = word.Substring(last.StartIndex, word.Length - last.StartIndex);
                        syllables[syllables.Count - 1] = new SpanishSyllable(last.StartIndex, merged.Length, merged, last.IsStressed);
                    }

                    break;
                }

                var nucleusEnd = FindNucleusEnd(word, vowelStart);
                var nextVowel = FindNextVowel(word, nucleusEnd);
                if (nextVowel < 0)
                {
                    syllables.Add(new SpanishSyllable(start, word.Length - start, word.Substring(start)));
                    break;
                }

                var clusterLength = nextVowel - nucleusEnd;
                var codaLength = GetCodaLength(word, nucleusEnd, clusterLength);
                var syllableEnd = nucleusEnd + codaLength;
                syllables.Add(new SpanishSyllable(start, syllableEnd - start, word.Substring(start, syllableEnd - start)));
                start = syllableEnd;
            }

            return syllables;
        }

        private static int FindNextVowel(string word, int start)
        {
            for (var i = start; i < word.Length; i++)
            {
                if (SpanishOrthography.IsPronouncedVowel(word, i))
                    return i;
            }

            return -1;
        }

        private static int FindNucleusEnd(string word, int vowelStart)
        {
            var first = word[vowelStart];
            var secondIndex = vowelStart + 1;
            if (secondIndex >= word.Length || !SpanishOrthography.IsPronouncedVowel(word, secondIndex))
                return secondIndex;

            var second = word[secondIndex];
            var thirdIndex = secondIndex + 1;
            if (thirdIndex < word.Length && SpanishOrthography.IsPronouncedVowel(word, thirdIndex))
            {
                var third = word[thirdIndex];
                if (SpanishOrthography.CanFormTriphthong(first, second, third))
                    return thirdIndex + 1;
            }

            return SpanishOrthography.CanFormDiphthong(first, second) ? secondIndex + 1 : secondIndex;
        }

        private static int GetCodaLength(string word, int clusterStart, int clusterLength)
        {
            if (clusterLength <= 1)
                return 0;

            var cluster = word.Substring(clusterStart, clusterLength);
            var onsetLength = GetOnsetLength(cluster);
            return clusterLength - onsetLength;
        }

        private static int GetOnsetLength(string cluster)
        {
            if (string.IsNullOrEmpty(cluster))
                return 0;

            if (cluster.Length == 1)
                return 1;

            var suffix2 = cluster.Length >= 2 ? cluster.Substring(cluster.Length - 2, 2) : "";
            if (IsValidDigraphOnset(suffix2) || IsValidConsonantClusterOnset(suffix2))
                return 2;

            return 1;
        }

        private static bool IsValidDigraphOnset(string cluster)
        {
            return cluster == "ch" || cluster == "ll" || cluster == "rr" || cluster == "qu" || cluster == "gu" || cluster == "gü";
        }

        private static bool IsValidConsonantClusterOnset(string cluster)
        {
            return cluster == "bl" || cluster == "br"
                || cluster == "cl" || cluster == "cr"
                || cluster == "dr"
                || cluster == "fl" || cluster == "fr"
                || cluster == "gl" || cluster == "gr"
                || cluster == "pl" || cluster == "pr"
                || cluster == "tr" || cluster == "tl";
        }
    }
}
