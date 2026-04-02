using System;
using System.Collections.Generic;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語の単語発音情報。
    /// </summary>
    public sealed class SwedishPronunciation
    {
        internal SwedishPhoneme[] PhonemesInternal { get; }
        internal int[] SyllableOffsetsInternal { get; }

        /// <summary>音素配列。</summary>
        public IReadOnlyList<SwedishPhoneme> Phonemes => PhonemesInternal;

        /// <summary>各音節の音素配列内オフセット。</summary>
        public IReadOnlyList<int> SyllableOffsets => SyllableOffsetsInternal;

        /// <summary>ストレスが置かれている音節のインデックス（-1は未指定）。</summary>
        public int StressedSyllableIndex { get; }

        internal SwedishPronunciation(SwedishPhoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            SyllableOffsetsInternal = syllableOffsets ?? throw new ArgumentNullException(nameof(syllableOffsets));
            StressedSyllableIndex = stressedSyllableIndex;
        }

        internal static readonly SwedishPronunciation Empty =
            new SwedishPronunciation(Array.Empty<SwedishPhoneme>(), Array.Empty<int>(), -1);
    }
}
