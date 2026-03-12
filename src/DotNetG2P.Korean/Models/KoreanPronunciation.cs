using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語発音の中間表現。
    /// </summary>
    public sealed class KoreanPronunciation
    {
        internal KoreanPhoneme[] PhonemesInternal { get; }
        internal KoreanSyllable[] SyllablesInternal { get; }
        private readonly ReadOnlyCollection<KoreanPhoneme> _phonemesView;
        private readonly ReadOnlyCollection<KoreanSyllable> _syllablesView;

        /// <summary>元の入力テキスト。</summary>
        public string OriginalText { get; }

        /// <summary>解析に投入した正規化済みテキスト。</summary>
        public string NormalizedText { get; }

        /// <summary>展開済み音素列。</summary>
        public IReadOnlyList<KoreanPhoneme> Phonemes => _phonemesView;

        /// <summary>音節表現。</summary>
        public IReadOnlyList<KoreanSyllable> Syllables => _syllablesView;

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
            _syllablesView = Array.AsReadOnly(SyllablesInternal);
            _phonemesView = Array.AsReadOnly(PhonemesInternal);
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

            var builder = new StringBuilder(SyllablesInternal.Length);
            for (var i = 0; i < SyllablesInternal.Length; i++)
                builder.Append(SyllablesInternal[i].ToHangulString());

            return builder.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToHangulString();
        }
    }
}
