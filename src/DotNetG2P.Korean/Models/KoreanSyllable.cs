using System;
using System.Collections.Generic;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// Hangul 1 音節の onset / nucleus / coda 構造。
    /// </summary>
    public readonly struct KoreanSyllable : IEquatable<KoreanSyllable>
    {
        private const int HangulSyllableBase = 0xAC00;
        private const int HangulSyllableEnd = 0xD7A3;
        private const int VowelCount = 21;
        private const int CodaCount = 28;

        private static readonly char[] Initials =
        {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ',
        };

        private static readonly char[] Medials =
        {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ',
        };

        private static readonly char[] Finals =
        {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ',
        };

        private static readonly Dictionary<char, int> InitialIndices = CreateIndexMap(Initials);
        private static readonly Dictionary<char, int> MedialIndices = CreateIndexMap(Medials);
        private static readonly Dictionary<char, int> FinalIndices = CreateIndexMap(Finals);

        /// <summary>初声。</summary>
        public char Onset { get; }

        /// <summary>中声。</summary>
        public char Nucleus { get; }

        /// <summary>終声。終声がなければ <c>'\0'</c>。</summary>
        public char Coda { get; }

        /// <summary>
        /// 音節を初期化する。
        /// </summary>
        public KoreanSyllable(char onset, char nucleus, char coda = '\0')
        {
            Onset = onset;
            Nucleus = nucleus;
            Coda = coda;
        }

        /// <summary>終声を持つか。</summary>
        public bool HasCoda => Coda != '\0';

        /// <summary>中声を持つか。</summary>
        public bool HasNucleus => Nucleus != '\0';

        /// <summary>空白などの語境界マーカーか。</summary>
        public bool IsBoundary => !HasNucleus && char.IsWhiteSpace(Onset);

        /// <summary>
        /// Jamo 文字列へ変換する。
        /// </summary>
        public string ToJamoString()
        {
            if (!HasNucleus)
                return Onset.ToString();

            return HasCoda
                ? string.Concat(Onset, Nucleus, Coda)
                : string.Concat(Onset, Nucleus);
        }

        /// <summary>
        /// Hangul 音節 문자열へ変換する。
        /// </summary>
        public string ToHangulString()
        {
            if (!HasNucleus)
                return Onset.ToString();

            if (!InitialIndices.TryGetValue(Onset, out var onsetIndex)
                || !MedialIndices.TryGetValue(Nucleus, out var medialIndex)
                || !FinalIndices.TryGetValue(Coda, out var finalIndex))
            {
                return ToJamoString();
            }

            var codePoint = HangulSyllableBase + ((onsetIndex * VowelCount) + medialIndex) * CodaCount + finalIndex;
            return ((char)codePoint).ToString();
        }

        /// <summary>
        /// 音節から音素列へ展開する。
        /// </summary>
        public KoreanPhoneme[] ToPhonemes()
        {
            if (IsBoundary)
                return Array.Empty<KoreanPhoneme>();

            if (!HasNucleus)
                return new[] { new KoreanPhoneme(Onset) };

            return HasCoda
                ? new[] { new KoreanPhoneme(Onset), new KoreanPhoneme(Nucleus), new KoreanPhoneme(Coda) }
                : new[] { new KoreanPhoneme(Onset), new KoreanPhoneme(Nucleus) };
        }

        /// <summary>
        /// Hangul 音節を分解できた場合 true を返す。
        /// </summary>
        public static bool TryDecompose(char c, out KoreanSyllable syllable)
        {
            if (c < HangulSyllableBase || c > HangulSyllableEnd)
            {
                syllable = default;
                return false;
            }

            var syllableIndex = c - HangulSyllableBase;
            var initialIndex = syllableIndex / (VowelCount * CodaCount);
            var medialIndex = (syllableIndex % (VowelCount * CodaCount)) / CodaCount;
            var finalIndex = syllableIndex % CodaCount;

            syllable = new KoreanSyllable(
                Initials[initialIndex],
                Medials[medialIndex],
                Finals[finalIndex]);

            return true;
        }

        /// <summary>
        /// compatibility jamo の単一文字から音節相当の表現を構成する。
        /// </summary>
        public static KoreanSyllable FromStandaloneJamo(char jamo)
        {
            return new KoreanSyllable(jamo, '\0', '\0');
        }

        /// <summary>
        /// 空白などの語境界マーカーを構成する。
        /// </summary>
        public static KoreanSyllable FromBoundary(char boundary)
        {
            return new KoreanSyllable(boundary, '\0', '\0');
        }

        private static Dictionary<char, int> CreateIndexMap(char[] source)
        {
            var result = new Dictionary<char, int>(source.Length);
            for (var i = 0; i < source.Length; i++)
                result[source[i]] = i;
            return result;
        }

        /// <inheritdoc />
        public override string ToString() => ToJamoString();

        /// <inheritdoc />
        public bool Equals(KoreanSyllable other)
        {
            return Onset == other.Onset
                && Nucleus == other.Nucleus
                && Coda == other.Coda;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is KoreanSyllable other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Onset;
                hash = (hash * 397) ^ Nucleus;
                hash = (hash * 397) ^ Coda;
                return hash;
            }
        }

        /// <summary>等価演算子。</summary>
        public static bool operator ==(KoreanSyllable left, KoreanSyllable right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(KoreanSyllable left, KoreanSyllable right) => !left.Equals(right);
    }
}
