using System;
using System.Collections.Generic;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// 単語単位のスペイン語発音情報。
    /// </summary>
    public sealed class SpanishPronunciation
    {
        internal SpanishPhoneme[] PhonemesInternal { get; }
        internal int[] SyllableOffsetsInternal { get; }

        /// <summary>音素列。</summary>
        public IReadOnlyList<SpanishPhoneme> Phonemes => PhonemesInternal;

        /// <summary>強勢音節インデックス。</summary>
        public int StressedSyllableIndex { get; }

        internal SpanishPronunciation(SpanishPhoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            SyllableOffsetsInternal = syllableOffsets ?? throw new ArgumentNullException(nameof(syllableOffsets));
            StressedSyllableIndex = stressedSyllableIndex;
        }

        /// <summary>
        /// IPA文字列を返す。
        /// </summary>
        public override string ToString() => Conversion.IpaConverter.Convert(this, includeStress: true);
    }
}
