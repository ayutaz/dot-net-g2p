using System;
using System.Collections.Generic;

namespace DotNetG2P.English
{
    /// <summary>
    /// 英語の発音を表す。ストレス付きARPAbet音素の配列を保持する。
    /// </summary>
    public sealed class EnglishPronunciation
    {
        /// <summary>内部音素配列（内部からの直接アクセス用）</summary>
        internal EnglishPhoneme[] PhonemesInternal { get; }

        /// <summary>音素列</summary>
        public IReadOnlyList<EnglishPhoneme> Phonemes => PhonemesInternal;

        /// <summary>
        /// EnglishPronunciationを初期化する。
        /// </summary>
        /// <param name="phonemes">音素列</param>
        public EnglishPronunciation(EnglishPhoneme[] phonemes)
        {
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
        }

        /// <summary>
        /// ARPAbet表記文字列を返す（例: "HH AH0 L OW1"）。
        /// </summary>
        public override string ToString()
        {
            if (PhonemesInternal.Length == 0)
                return "";

            // 要素数の概算で容量を確保
            var parts = new string[PhonemesInternal.Length];
            for (var i = 0; i < PhonemesInternal.Length; i++)
            {
                parts[i] = PhonemesInternal[i].ToString();
            }

            return string.Join(" ", parts);
        }
    }
}
