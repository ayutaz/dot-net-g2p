using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish.Rules
{
    /// <summary>
    /// スウェーデン語の正書法ベース音節分割。
    /// Onset最大化原則に基づき、有効なOnsetクラスタを次音節に割り当てる。
    /// </summary>
    internal static class SwedishSyllabifier
    {
        /// <summary>
        /// 単語を音節に分割する。
        /// </summary>
        internal static IReadOnlyList<SwedishSyllable> Syllabify(string word)
        {
            if (string.IsNullOrEmpty(word))
                return Array.Empty<SwedishSyllable>();

            var lower = word.ToLowerInvariant();
            var syllables = new List<SwedishSyllable>(4);
            var start = 0;

            while (start < lower.Length)
            {
                var vowelStart = FindNextVowel(lower, start);
                if (vowelStart < 0)
                {
                    // 残り子音のみ → 最後の音節に統合
                    if (syllables.Count > 0)
                    {
                        var last = syllables[syllables.Count - 1];
                        syllables[syllables.Count - 1] = new SwedishSyllable(
                            last.StartIndex,
                            lower.Length - last.StartIndex,
                            word.Substring(last.StartIndex, lower.Length - last.StartIndex));
                    }
                    break;
                }

                var nucleusEnd = FindNucleusEnd(lower, vowelStart);
                var nextVowel = FindNextVowel(lower, nucleusEnd);
                if (nextVowel < 0)
                {
                    // 最後の音節
                    syllables.Add(new SwedishSyllable(start, lower.Length - start,
                        word.Substring(start, lower.Length - start)));
                    break;
                }

                var clusterLength = nextVowel - nucleusEnd;
                var onsetLength = GetMaxOnsetLength(lower, nucleusEnd, clusterLength);
                var syllableEnd = nucleusEnd + (clusterLength - onsetLength);

                syllables.Add(new SwedishSyllable(start, syllableEnd - start,
                    word.Substring(start, syllableEnd - start)));
                start = syllableEnd;
            }

            return syllables;
        }

        private static int FindNextVowel(string word, int from)
        {
            for (var i = from; i < word.Length; i++)
            {
                if (SwedishOrthography.IsVowelChar(word[i]))
                    return i;
            }
            return -1;
        }

        private static int FindNucleusEnd(string word, int vowelStart)
        {
            // 単母音のみ（スウェーデン語は音韻的二重母音なし）
            return vowelStart + 1;
        }

        private static int GetMaxOnsetLength(string word, int clusterStart, int clusterLength)
        {
            if (clusterLength <= 1) return clusterLength;

            // 3子音Onsetチェック
            if (clusterLength >= 3)
            {
                var onset3 = word.Substring(clusterStart + clusterLength - 3, 3);
                if (IsValid3ConsonantOnset(onset3))
                    return 3;
            }

            // 2子音Onsetチェック
            var onset2 = word.Substring(clusterStart + clusterLength - 2, 2);
            if (IsValid2ConsonantOnset(onset2))
                return 2;

            return 1;
        }

        /// <summary>
        /// 有効な2子音Onsetか判定する。
        /// pl, bl, pr, br, tr, dr, kl, gl, kr, gr, fr, fl, sl, sm, sn, sp, st, sk, sv, kv, tv, gn
        /// </summary>
        private static bool IsValid2ConsonantOnset(string cluster)
        {
            return cluster == "pl" || cluster == "bl"
                || cluster == "pr" || cluster == "br"
                || cluster == "tr" || cluster == "dr"
                || cluster == "kl" || cluster == "gl"
                || cluster == "kr" || cluster == "gr"
                || cluster == "fr" || cluster == "fl"
                || cluster == "sl" || cluster == "sm"
                || cluster == "sn" || cluster == "sp"
                || cluster == "st" || cluster == "sk"
                || cluster == "sv" || cluster == "kv"
                || cluster == "tv" || cluster == "gn";
        }

        /// <summary>
        /// 有効な3子音Onsetか判定する。
        /// spr, spl, spj, str, skr, skv
        /// </summary>
        private static bool IsValid3ConsonantOnset(string cluster)
        {
            return cluster == "spr" || cluster == "spl"
                || cluster == "spj" || cluster == "str"
                || cluster == "skr" || cluster == "skv";
        }
    }
}
