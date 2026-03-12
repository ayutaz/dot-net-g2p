using System;
using System.Collections.Generic;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語発音の中間表現。
    /// </summary>
    public sealed class KoreanPronunciation
    {
        internal KoreanPhoneme[] PhonemesInternal { get; }
        internal KoreanSyllable[] SyllablesInternal { get; }

        /// <summary>元の入力テキスト。</summary>
        public string OriginalText { get; }

        /// <summary>Unicode 正規化後のテキスト。</summary>
        public string NormalizedText { get; }

        /// <summary>展開済み音素列。</summary>
        public IReadOnlyList<KoreanPhoneme> Phonemes => PhonemesInternal;

        /// <summary>音節表現。</summary>
        public IReadOnlyList<KoreanSyllable> Syllables => SyllablesInternal;

        internal KoreanPronunciation(
            string originalText,
            string normalizedText,
            KoreanSyllable[] syllables,
            KoreanPhoneme[] phonemes)
        {
            OriginalText = originalText ?? throw new ArgumentNullException(nameof(originalText));
            NormalizedText = normalizedText ?? throw new ArgumentNullException(nameof(normalizedText));
            SyllablesInternal = syllables ?? throw new ArgumentNullException(nameof(syllables));
            PhonemesInternal = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
        }

        /// <summary>
        /// 音節ごとの Jamo 文字列を返す。
        /// </summary>
        public IReadOnlyList<string> GetJamoSyllables()
        {
            if (SyllablesInternal.Length == 0)
                return Array.Empty<string>();

            var result = new string[SyllablesInternal.Length];
            for (var i = 0; i < SyllablesInternal.Length; i++)
                result[i] = SyllablesInternal[i].ToJamoString();
            return result;
        }

        /// <summary>
        /// 発音を Hangul 表層文字列として返す。
        /// </summary>
        public string ToHangulString()
        {
            if (SyllablesInternal.Length == 0)
                return string.Empty;

            var result = new string[SyllablesInternal.Length];
            for (var i = 0; i < SyllablesInternal.Length; i++)
                result[i] = SyllablesInternal[i].ToHangulString();
            return string.Concat(result);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToHangulString();
        }
    }
}
