using System;
using System.Collections.Generic;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// 単語単位のポルトガル語発音情報。
    /// </summary>
    public sealed class PortuguesePronunciation
    {
        internal PortuguesePhoneme[] PhonemesInternal { get; }
        internal int[] SyllableOffsetsInternal { get; }

        /// <summary>音素列。</summary>
        public IReadOnlyList<PortuguesePhoneme> Phonemes => PhonemesInternal;

        /// <summary>強勢音節インデックス。</summary>
        public int StressedSyllableIndex { get; }

        internal PortuguesePronunciation(PortuguesePhoneme[] phonemes, int[] syllableOffsets, int stressedSyllableIndex)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
            SyllableOffsetsInternal = syllableOffsets ?? throw new ArgumentNullException(nameof(syllableOffsets));
            StressedSyllableIndex = stressedSyllableIndex;
        }

        /// <summary>
        /// 音素列の文字列表現を返す。
        /// </summary>
        public override string ToString()
        {
            if (PhonemesInternal.Length == 0)
                return string.Empty;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < PhonemesInternal.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.Append(PhonemesInternal[i].ToString());
            }
            return sb.ToString();
        }
    }
}
