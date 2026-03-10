using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// ストレス情報付きのスペイン語IPA音素。
    /// </summary>
    public readonly struct SpanishPhoneme : IEquatable<SpanishPhoneme>
    {
        /// <summary>音素種別。</summary>
        public SpanishIpaPhoneme Phoneme { get; }

        /// <summary>この音素が強勢母音か。</summary>
        public bool IsStressed { get; }

        /// <summary>
        /// SpanishPhoneme を初期化する。
        /// </summary>
        public SpanishPhoneme(SpanishIpaPhoneme phoneme, bool isStressed = false)
        {
            Phoneme = phoneme;
            IsStressed = isStressed;
        }

        /// <summary>この音素が音節主核となる母音か。</summary>
        public bool IsSyllabicVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme <= SpanishIpaPhoneme.U;
        }

        /// <summary>この音素が半母音か。</summary>
        public bool IsSemivowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme == SpanishIpaPhoneme.J || Phoneme == SpanishIpaPhoneme.W;
        }

        /// <inheritdoc />
        public override string ToString() => Conversion.IpaConverter.ToSymbol(Phoneme);

        /// <inheritdoc />
        public bool Equals(SpanishPhoneme other) => Phoneme == other.Phoneme && IsStressed == other.IsStressed;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SpanishPhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Phoneme << 8) | (IsStressed ? 1 : 0);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(SpanishPhoneme left, SpanishPhoneme right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(SpanishPhoneme left, SpanishPhoneme right) => !left.Equals(right);
    }
}
