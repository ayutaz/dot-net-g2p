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

        /// <summary>ピッチアクセント（0=不明, 1=accent 1/acute, 2=accent 2/grave）。</summary>
        public byte Accent { get; internal set; }

        internal SwedishPronunciation(SwedishPhoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex, byte accent = 0)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            SyllableOffsetsInternal = syllableOffsets ?? throw new ArgumentNullException(nameof(syllableOffsets));
            StressedSyllableIndex = stressedSyllableIndex;
            Accent = accent;
        }

        /// <summary>ストレスを除去した新しいSwedishPronunciationを返す。個々の音素のIsStressedも全てfalseにする。Accentは維持。</summary>
        public SwedishPronunciation WithoutStress()
        {
            var unstressed = new SwedishPhoneme[PhonemesInternal.Length];
            for (var i = 0; i < PhonemesInternal.Length; i++)
            {
                var p = PhonemesInternal[i];
                unstressed[i] = new SwedishPhoneme(p.Phoneme, isStressed: false, p.IsSyllableNucleus);
            }

            return new SwedishPronunciation(unstressed, SyllableOffsetsInternal, -1, Accent);
        }

        internal static readonly SwedishPronunciation Empty =
            new SwedishPronunciation(Array.Empty<SwedishPhoneme>(), Array.Empty<int>(), -1, 0);
    }
}
