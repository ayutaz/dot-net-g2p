using System;

namespace DotNetG2P.French.Rules
{
    /// <summary>
    /// フランス語IPA音素列の音節分割。Onset Maximization アルゴリズムを使用。
    /// </summary>
    internal static class FrenchSyllabifier
    {
        /// <summary>
        /// 音素列を音節に分割し、音節オフセット配列と音節核マーク付き音素配列を返す。
        /// </summary>
        /// <param name="phonemes">G2Pルールで生成されたIPA音素列。</param>
        /// <returns>音節オフセット配列と音節核マーク付き音素配列のタプル。</returns>
        public static (int[] syllableOffsets, FrenchPhoneme[] phonemesWithNucleus) Syllabify(FrenchIpaPhoneme[] phonemes)
        {
            if (phonemes == null || phonemes.Length == 0)
                return (Array.Empty<int>(), Array.Empty<FrenchPhoneme>());

            // 母音位置を特定
            var vowelIndices = FindVowelIndices(phonemes);

            if (vowelIndices.Length == 0)
            {
                // 母音がない場合: 全体を1音節として扱い、核マークなし
                var result = new FrenchPhoneme[phonemes.Length];
                for (var i = 0; i < phonemes.Length; i++)
                    result[i] = new FrenchPhoneme(phonemes[i]);
                return (new[] { 0 }, result);
            }

            // 音節境界を決定
            var boundaries = ComputeSyllableBoundaries(phonemes, vowelIndices);

            // 音節オフセット配列を構築
            var offsets = new int[boundaries.Length + 1];
            offsets[0] = 0;
            for (var i = 0; i < boundaries.Length; i++)
                offsets[i + 1] = boundaries[i];

            // 音節核マーク付き音素配列を構築
            var phonemesWithNucleus = BuildPhonemesWithNucleus(phonemes, offsets, vowelIndices);

            return (offsets, phonemesWithNucleus);
        }

        /// <summary>母音（口母音 + 鼻母音）の位置を特定する。半母音は母音として扱わない。</summary>
        private static int[] FindVowelIndices(FrenchIpaPhoneme[] phonemes)
        {
            var count = 0;
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (IsVowel(phonemes[i]))
                    count++;
            }

            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            var idx = 0;
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (IsVowel(phonemes[i]))
                    indices[idx++] = i;
            }

            return indices;
        }

        /// <summary>音節間の境界位置を計算する。Onset Maximization で子音クラスタを次の音節に割り当てる。</summary>
        private static int[] ComputeSyllableBoundaries(FrenchIpaPhoneme[] phonemes, int[] vowelIndices)
        {
            if (vowelIndices.Length <= 1)
                return Array.Empty<int>();

            var boundaries = new int[vowelIndices.Length - 1];

            for (var vi = 0; vi < vowelIndices.Length - 1; vi++)
            {
                var currentVowelEnd = vowelIndices[vi] + 1;
                var nextVowel = vowelIndices[vi + 1];

                // 2つの母音間に子音がない場合: 次の母音が新しい音節の開始
                if (currentVowelEnd >= nextVowel)
                {
                    boundaries[vi] = nextVowel;
                    continue;
                }

                // 2つの母音間の子音クラスタを取得
                var clusterStart = currentVowelEnd;
                var clusterLength = nextVowel - clusterStart;

                // Onset Maximization: 右側からできるだけ多くの子音を次の音節のonsetに割り当てる
                var onsetLength = GetMaxOnsetLength(phonemes, clusterStart, clusterLength);

                boundaries[vi] = nextVowel - onsetLength;
            }

            return boundaries;
        }

        /// <summary>子音クラスタから有効なonsetの最大長を返す。</summary>
        private static int GetMaxOnsetLength(FrenchIpaPhoneme[] phonemes, int clusterStart, int clusterLength)
        {
            // 右端から最大3音素まで試す
            for (var len = Math.Min(clusterLength, 3); len >= 1; len--)
            {
                if (IsValidOnset(phonemes, clusterStart + clusterLength - len, len))
                    return len;
            }

            // フォールバック: 最後の1子音を次の音節に
            return 1;
        }

        /// <summary>
        /// 指定位置からの子音列が有効なonset clusterかを判定する。
        /// </summary>
        /// <param name="phonemes">音素配列。</param>
        /// <param name="start">クラスタ開始位置。</param>
        /// <param name="length">クラスタ長。</param>
        internal static bool IsValidOnset(FrenchIpaPhoneme[] phonemes, int start, int length)
        {
            switch (length)
            {
                case 1:
                    // 単子音: すべて有効
                    return true;

                case 2:
                {
                    var c1 = phonemes[start];
                    var c2 = phonemes[start + 1];

                    // 閉鎖音/F/V + 流音(L/R) ただし TL, DL は不可
                    if (IsLiquid(c2) && IsObstruentForOnset(c1))
                    {
                        // TL, DL を除外
                        if (c2 == FrenchIpaPhoneme.L && (c1 == FrenchIpaPhoneme.T || c1 == FrenchIpaPhoneme.D))
                            return false;
                        return true;
                    }

                    return false;
                }

                case 3:
                {
                    // S + 閉鎖音 + R
                    var c1 = phonemes[start];
                    var c2 = phonemes[start + 1];
                    var c3 = phonemes[start + 2];

                    return c1 == FrenchIpaPhoneme.S
                        && IsStop(c2)
                        && c3 == FrenchIpaPhoneme.R;
                }

                default:
                    return false;
            }
        }

        /// <summary>各音節の最初の母音に音節核マークを付けた音素配列を構築する。</summary>
        private static FrenchPhoneme[] BuildPhonemesWithNucleus(
            FrenchIpaPhoneme[] phonemes,
            int[] syllableOffsets,
            int[] vowelIndices)
        {
            var result = new FrenchPhoneme[phonemes.Length];

            // まず全音素を核マークなしでコピー
            for (var i = 0; i < phonemes.Length; i++)
                result[i] = new FrenchPhoneme(phonemes[i]);

            // 各音節の最初の母音を特定して核マークを付ける
            var vowelIdx = 0;
            for (var s = 0; s < syllableOffsets.Length; s++)
            {
                var syllStart = syllableOffsets[s];
                var syllEnd = s + 1 < syllableOffsets.Length ? syllableOffsets[s + 1] : phonemes.Length;

                // この音節範囲内にある最初の母音を探す
                while (vowelIdx < vowelIndices.Length && vowelIndices[vowelIdx] < syllStart)
                    vowelIdx++;

                if (vowelIdx < vowelIndices.Length && vowelIndices[vowelIdx] < syllEnd)
                {
                    var nucleusPos = vowelIndices[vowelIdx];
                    result[nucleusPos] = new FrenchPhoneme(phonemes[nucleusPos], isSyllableNucleus: true);
                    vowelIdx++;
                }
            }

            return result;
        }

        /// <summary>母音かどうか（口母音 + 鼻母音、半母音を含まない）。</summary>
        private static bool IsVowel(FrenchIpaPhoneme phoneme)
        {
            return phoneme <= FrenchIpaPhoneme.OeNasal;
        }

        /// <summary>流音（L または R）かどうか。</summary>
        private static bool IsLiquid(FrenchIpaPhoneme phoneme)
        {
            return phoneme == FrenchIpaPhoneme.L || phoneme == FrenchIpaPhoneme.R;
        }

        /// <summary>閉鎖音かどうか。</summary>
        private static bool IsStop(FrenchIpaPhoneme phoneme)
        {
            return phoneme >= FrenchIpaPhoneme.P && phoneme <= FrenchIpaPhoneme.G;
        }

        /// <summary>onset の最初の位置に立てる阻害音（閉鎖音 + F + V）かどうか。</summary>
        private static bool IsObstruentForOnset(FrenchIpaPhoneme phoneme)
        {
            return IsStop(phoneme) || phoneme == FrenchIpaPhoneme.F || phoneme == FrenchIpaPhoneme.V;
        }
    }
}
