using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese.Rules
{
    /// <summary>
    /// ポルトガル語の正書法ベース音節分割。
    /// Onset Maximization 原則に基づき、単語を音節に分割する。
    /// </summary>
    internal static class PortugueseSyllabifier
    {
        /// <summary>
        /// 指定された単語を音節に分割する。
        /// </summary>
        public static IReadOnlyList<PortugueseSyllable> Syllabify(string word)
        {
            if (string.IsNullOrEmpty(word))
                return Array.Empty<PortugueseSyllable>();

            var syllables = new List<PortugueseSyllable>(4);
            var start = 0;

            while (start < word.Length)
            {
                var vowelStart = FindNextVowel(word, start);
                if (vowelStart < 0)
                {
                    // 残りに母音がない場合、前の音節に結合するか単独音節にする
                    if (syllables.Count == 0)
                    {
                        syllables.Add(new PortugueseSyllable(start, word.Length - start, word.Substring(start)));
                    }
                    else
                    {
                        var last = syllables[syllables.Count - 1];
                        var merged = word.Substring(last.StartIndex, word.Length - last.StartIndex);
                        syllables[syllables.Count - 1] = new PortugueseSyllable(last.StartIndex, merged.Length, merged, last.IsStressed);
                    }

                    break;
                }

                var nucleusEnd = GetNucleusEnd(word, vowelStart);
                var nextVowel = FindNextVowel(word, nucleusEnd);
                if (nextVowel < 0)
                {
                    // 最後の音節: 残り全部を含む
                    syllables.Add(new PortugueseSyllable(start, word.Length - start, word.Substring(start)));
                    break;
                }

                var clusterLength = nextVowel - nucleusEnd;
                var codaLength = GetCodaLength(word, nucleusEnd, clusterLength);
                var syllableEnd = nucleusEnd + codaLength;
                syllables.Add(new PortugueseSyllable(start, syllableEnd - start, word.Substring(start, syllableEnd - start)));
                start = syllableEnd;
            }

            return syllables;
        }

        /// <summary>
        /// 指定位置以降の最初の発音母音の位置を返す。見つからない場合は -1。
        /// </summary>
        private static int FindNextVowel(string word, int start)
        {
            for (var i = start; i < word.Length; i++)
            {
                if (IsPronouncedVowel(word, i))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 母音開始位置から核の終了位置（排他）を返す。
        /// 二重母音・三重母音を考慮する。
        /// </summary>
        private static int GetNucleusEnd(string word, int vowelStart)
        {
            var first = word[vowelStart];
            var secondIndex = vowelStart + 1;
            if (secondIndex >= word.Length || !IsPronouncedVowel(word, secondIndex))
                return secondIndex;

            var second = word[secondIndex];

            // 三重母音チェック: 弱 + 強 + 弱
            var thirdIndex = secondIndex + 1;
            if (thirdIndex < word.Length && IsPronouncedVowel(word, thirdIndex))
            {
                var third = word[thirdIndex];
                if (CanFormTriphthong(first, second, third))
                    return thirdIndex + 1;
            }

            // 二重母音チェック
            return PortugueseOrthography.CanFormDiphthong(first, second) ? secondIndex + 1 : secondIndex;
        }

        /// <summary>
        /// 3文字が三重母音を形成できるか判定する（弱+強+弱）。
        /// </summary>
        private static bool CanFormTriphthong(char first, char second, char third)
        {
            return PortugueseOrthography.IsWeakVowel(first)
                && PortugueseOrthography.IsStrongVowel(second)
                && PortugueseOrthography.IsWeakVowel(third);
        }

        /// <summary>
        /// 母音間の子音列から coda 長を計算する。
        /// Onset Maximization 原則: 可能な限り多くの子音を次の音節の onset に割り当てる。
        /// </summary>
        private static int GetCodaLength(string word, int clusterStart, int clusterLength)
        {
            if (clusterLength <= 1)
                return 0;

            var cluster = word.Substring(clusterStart, clusterLength);
            var onsetLength = GetOnsetLength(cluster);
            return clusterLength - onsetLength;
        }

        /// <summary>
        /// 子音列末尾から有効な onset の長さを返す。
        /// </summary>
        private static int GetOnsetLength(string cluster)
        {
            if (string.IsNullOrEmpty(cluster))
                return 0;

            if (cluster.Length == 1)
                return 1;

            // 末尾2文字がダイグラフ onset または有効な子音クラスタ onset かチェック
            var suffix2 = cluster.Length >= 2 ? cluster.Substring(cluster.Length - 2, 2) : "";
            if (IsDigraphOnset(suffix2) || IsValidConsonantClusterOnset(suffix2))
                return 2;

            return 1;
        }

        /// <summary>
        /// 分割不可のダイグラフ onset を判定する。
        /// ch, lh, nh は単一の音素を表すダイグラフで分割してはならない。
        /// rr, ss も語中ダイグラフとして分割時に前の音節と次の音節で分かれるが、
        /// ここでは onset として扱わない（rr/ss は音節境界で分割される）。
        /// qu, gu (+前舌母音) は別の仕組みで処理されるためここには含めない。
        /// </summary>
        private static bool IsDigraphOnset(string cluster)
        {
            if (cluster.Length != 2)
                return false;

            var c1 = char.ToLowerInvariant(cluster[0]);
            var c2 = char.ToLowerInvariant(cluster[1]);

            // ch, lh, nh は分割不可のダイグラフ onset
            if (c2 == 'h' && (c1 == 'c' || c1 == 'l' || c1 == 'n'))
                return true;

            // qu, gu は onset ダイグラフ
            if (c2 == 'u' && (c1 == 'q' || c1 == 'g'))
                return true;

            return false;
        }

        /// <summary>
        /// 有効な子音クラスタ onset を判定する。
        /// ポルトガル語では obstruent + liquid (/ɾ/ or /l/) の組み合わせのみ許容。
        /// tl, dl は不許容。
        /// </summary>
        private static bool IsValidConsonantClusterOnset(string cluster)
        {
            if (cluster.Length != 2)
                return false;

            var c1 = char.ToLowerInvariant(cluster[0]);
            var c2 = char.ToLowerInvariant(cluster[1]);

            // 流音は r または l
            if (c2 != 'r' && c2 != 'l')
                return false;

            // obstruent + r: pr, br, tr, dr, cr, gr, fr, vr
            if (c2 == 'r')
            {
                return c1 == 'p' || c1 == 'b' || c1 == 't' || c1 == 'd'
                    || c1 == 'c' || c1 == 'g' || c1 == 'f' || c1 == 'v';
            }

            // obstruent + l: pl, bl, cl, gl, fl (tl, dl は不許容)
            return c1 == 'p' || c1 == 'b' || c1 == 'c' || c1 == 'g' || c1 == 'f';
        }

        /// <summary>
        /// 指定された位置が発音される母音であるかどうかを判定する。
        /// qu/gu + 前舌母音の u は黙字のため false。
        /// </summary>
        private static bool IsPronouncedVowel(string word, int index)
        {
            var c = word[index];
            if (!PortugueseOrthography.IsVowel(c))
                return false;

            return !PortugueseOrthography.IsSilentU(word, index);
        }
    }
}
