using System;
using System.Collections.Generic;
using DotNetG2P.Spanish.Data;

namespace DotNetG2P.Spanish.Rules
{
    /// <summary>
    /// スペイン語のルールベース書記素→音素変換。
    /// </summary>
    internal static class GraphemeToPhonemeRules
    {
        public static SpanishPronunciation ConvertWord(string word, SpanishDialect dialect)
        {
            if (string.IsNullOrEmpty(word))
                return new SpanishPronunciation(Array.Empty<SpanishPhoneme>(), Array.Empty<int>(), -1);

            if (SpanishExceptionDictionary.TryLookup(word, out var exception))
                return exception;

            var stressedSyllables = StressAssigner.MarkStress(word, SpanishSyllabifier.Syllabify(word));
            var phonemes = new List<SpanishPhoneme>(word.Length + 2);
            var syllableOffsets = new int[stressedSyllables.Count];
            var stressedIndex = -1;

            for (var i = 0; i < stressedSyllables.Count; i++)
            {
                syllableOffsets[i] = phonemes.Count;
                if (stressedSyllables[i].IsStressed)
                    stressedIndex = i;
                AppendSyllable(word, stressedSyllables[i], phonemes, dialect);
            }

            return new SpanishPronunciation(phonemes.ToArray(), syllableOffsets, stressedIndex);
        }

        internal static IReadOnlyList<SpanishPhoneme> ConvertWordToPhonemes(string word, SpanishDialect dialect)
        {
            return ConvertWord(word, dialect).PhonemesInternal;
        }

        private static void AppendSyllable(string word, SpanishSyllable syllable, List<SpanishPhoneme> output, SpanishDialect dialect)
        {
            var start = syllable.StartIndex;
            var end = start + syllable.Length;
            var nucleusStart = FindNucleusStart(word, start, end);
            if (nucleusStart < 0)
            {
                AppendConsonants(word, start, end, output, dialect);
                return;
            }

            var nucleusEnd = FindNucleusEnd(word, nucleusStart, end);
            AppendConsonants(word, start, nucleusStart, output, dialect);
            AppendVowelGroup(word, nucleusStart, nucleusEnd, syllable.IsStressed, output);
            AppendConsonants(word, nucleusEnd, end, output, dialect);
        }

        private static int FindNucleusStart(string word, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                if (SpanishOrthography.IsPronouncedVowel(word, i))
                    return i;
            }

            return -1;
        }

        private static int FindNucleusEnd(string word, int start, int end)
        {
            var i = start + 1;
            while (i < end && SpanishOrthography.IsPronouncedVowel(word, i))
                i++;
            return i;
        }

        private static void AppendVowelGroup(string word, int start, int end, bool stressed, List<SpanishPhoneme> output)
        {
            var count = end - start;
            if (count <= 0)
                return;

            var first = char.ToLowerInvariant(word[start]);
            if (count == 1)
            {
                output.Add(new SpanishPhoneme(ToVowelPhoneme(first), stressed));
                return;
            }

            var second = char.ToLowerInvariant(word[start + 1]);
            if (count >= 3)
            {
                var third = char.ToLowerInvariant(word[start + 2]);
                if (SpanishOrthography.CanFormTriphthong(first, second, third))
                {
                    output.Add(new SpanishPhoneme(ToSemivowelPhoneme(first)));
                    output.Add(new SpanishPhoneme(ToVowelPhoneme(second), stressed));
                    output.Add(new SpanishPhoneme(ToSemivowelPhoneme(third)));
                    return;
                }
            }

            if (SpanishOrthography.CanFormDiphthong(first, second))
            {
                if (SpanishOrthography.IsWeakUnaccentedVowel(first))
                {
                    output.Add(new SpanishPhoneme(ToSemivowelPhoneme(first)));
                    output.Add(new SpanishPhoneme(ToVowelPhoneme(second), stressed));
                }
                else if (SpanishOrthography.IsWeakUnaccentedVowel(second))
                {
                    output.Add(new SpanishPhoneme(ToVowelPhoneme(first), stressed));
                    output.Add(new SpanishPhoneme(ToSemivowelPhoneme(second)));
                }
                else
                {
                    output.Add(new SpanishPhoneme(ToSemivowelPhoneme(first)));
                    output.Add(new SpanishPhoneme(ToVowelPhoneme(second), stressed));
                }

                return;
            }

            for (var i = start; i < end; i++)
                output.Add(new SpanishPhoneme(ToVowelPhoneme(word[i]), stressed && i == start));
        }

        private static void AppendConsonants(string word, int start, int end, List<SpanishPhoneme> output, SpanishDialect dialect)
        {
            var i = start;
            while (i < end)
            {
                var current = char.ToLowerInvariant(word[i]);
                if (current == 'h')
                {
                    i++;
                    continue;
                }

                if (i + 1 < word.Length)
                {
                    var next = char.ToLowerInvariant(word[i + 1]);
                    if (current == 'c' && next == 'h')
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.Ch));
                        i += 2;
                        continue;
                    }

                    if (current == 'l' && next == 'l')
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.Y));
                        i += 2;
                        continue;
                    }

                    if (current == 'r' && next == 'r')
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.Rr));
                        i += 2;
                        continue;
                    }

                    if (current == 'q' && next == 'u' && i + 2 < word.Length && IsSoftVowel(word[i + 2]))
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.K));
                        i += 2;
                        continue;
                    }

                    if (current == 'g' && next == 'ü' && i + 2 < word.Length && IsSoftVowel(word[i + 2]))
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.G));
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.W));
                        i += 2;
                        continue;
                    }

                    if (current == 'g' && next == 'u' && i + 2 < word.Length && IsSoftVowel(word[i + 2]))
                    {
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.G));
                        i += 2;
                        continue;
                    }
                }

                switch (current)
                {
                    case 'b':
                    case 'v':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.B));
                        break;
                    case 'c':
                        output.Add(new SpanishPhoneme(IsSoftVowelAt(word, i + 1)
                            ? (dialect == SpanishDialect.Castilian ? SpanishIpaPhoneme.Th : SpanishIpaPhoneme.S)
                            : SpanishIpaPhoneme.K));
                        break;
                    case 'd':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.D));
                        break;
                    case 'f':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.F));
                        break;
                    case 'g':
                        output.Add(new SpanishPhoneme(IsSoftVowelAt(word, i + 1) ? SpanishIpaPhoneme.X : SpanishIpaPhoneme.G));
                        break;
                    case 'j':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.X));
                        break;
                    case 'k':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.K));
                        break;
                    case 'l':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.L));
                        break;
                    case 'm':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.M));
                        break;
                    case 'n':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.N));
                        break;
                    case 'ñ':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.Ny));
                        break;
                    case 'p':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.P));
                        break;
                    case 'r':
                        output.Add(new SpanishPhoneme(IsTrillPosition(word, i) ? SpanishIpaPhoneme.Rr : SpanishIpaPhoneme.R));
                        break;
                    case 's':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.S));
                        break;
                    case 't':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.T));
                        break;
                    case 'w':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.W));
                        break;
                    case 'x':
                        if (i == 0)
                        {
                            output.Add(new SpanishPhoneme(SpanishIpaPhoneme.S));
                        }
                        else
                        {
                            output.Add(new SpanishPhoneme(SpanishIpaPhoneme.K));
                            output.Add(new SpanishPhoneme(SpanishIpaPhoneme.S));
                        }
                        break;
                    case 'y':
                        output.Add(new SpanishPhoneme(SpanishIpaPhoneme.Y));
                        break;
                    case 'z':
                        output.Add(new SpanishPhoneme(dialect == SpanishDialect.Castilian ? SpanishIpaPhoneme.Th : SpanishIpaPhoneme.S));
                        break;
                }

                i++;
            }
        }

        private static bool IsSoftVowelAt(string word, int index)
        {
            if ((uint)index >= (uint)word.Length)
                return false;

            return IsSoftVowel(word[index]);
        }

        private static bool IsSoftVowel(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'e' || c == 'é' || c == 'i' || c == 'í';
        }

        private static bool IsTrillPosition(string word, int index)
        {
            if (index == 0)
                return true;

            var prev = char.ToLowerInvariant(word[index - 1]);
            return prev == 'n' || prev == 'l' || prev == 's';
        }

        private static SpanishIpaPhoneme ToVowelPhoneme(char c)
        {
            c = char.ToLowerInvariant(c);
            switch (c)
            {
                case 'a':
                case 'á':
                    return SpanishIpaPhoneme.A;
                case 'e':
                case 'é':
                    return SpanishIpaPhoneme.E;
                case 'i':
                case 'í':
                case 'y':
                    return SpanishIpaPhoneme.I;
                case 'o':
                case 'ó':
                    return SpanishIpaPhoneme.O;
                default:
                    return SpanishIpaPhoneme.U;
            }
        }

        private static SpanishIpaPhoneme ToSemivowelPhoneme(char c)
        {
            c = char.ToLowerInvariant(c);
            return c == 'i' || c == 'y' ? SpanishIpaPhoneme.J : SpanishIpaPhoneme.W;
        }
    }
}
