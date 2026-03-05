using System;

namespace DotNetG2P.English
{
    /// <summary>
    /// 英語の発音を表す。ストレス付きARPAbet音素の配列を保持する。
    /// </summary>
    public sealed class EnglishPronunciation
    {
        /// <summary>音素列</summary>
        public EnglishPhoneme[] Phonemes { get; }

        /// <summary>
        /// EnglishPronunciationを初期化する。
        /// </summary>
        /// <param name="phonemes">音素列</param>
        public EnglishPronunciation(EnglishPhoneme[] phonemes)
        {
            Phonemes = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
        }

        /// <summary>
        /// ARPAbet表記文字列を返す（例: "HH AH0 L OW1"）。
        /// </summary>
        public override string ToString()
        {
            if (Phonemes.Length == 0)
                return "";

            // 要素数の概算で容量を確保
            var parts = new string[Phonemes.Length];
            for (var i = 0; i < Phonemes.Length; i++)
            {
                parts[i] = Phonemes[i].ToString();
            }

            return string.Join(" ", parts);
        }
    }
}
