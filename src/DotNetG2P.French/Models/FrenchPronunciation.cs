using System;
using System.Collections.Generic;

namespace DotNetG2P.French
{
    /// <summary>
    /// 単語単位のフランス語発音情報。
    /// </summary>
    public sealed class FrenchPronunciation
    {
        internal FrenchPhoneme[] PhonemesInternal { get; }
        internal int[] SyllableOffsetsInternal { get; }

        /// <summary>音素列。</summary>
        public IReadOnlyList<FrenchPhoneme> Phonemes => PhonemesInternal;

        /// <summary>
        /// 強勢音節インデックス。
        /// フランス語は語レベルの強勢を持たないため、常に -1 を返す。
        /// </summary>
        public int StressedSyllableIndex { get; }

        internal FrenchPronunciation(FrenchPhoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            SyllableOffsetsInternal = syllableOffsets ?? throw new ArgumentNullException(nameof(syllableOffsets));
            StressedSyllableIndex = stressedSyllableIndex;
        }

        /// <summary>
        /// IPA文字列を返す。
        /// </summary>
        public override string ToString() => Conversion.IpaConverter.Convert(this, includeStress: false);
    }
}
