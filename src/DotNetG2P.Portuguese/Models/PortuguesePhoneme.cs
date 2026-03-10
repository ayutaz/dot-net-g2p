using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ストレス情報付きのポルトガル語IPA音素。
    /// </summary>
    public readonly struct PortuguesePhoneme : IEquatable<PortuguesePhoneme>
    {
        /// <summary>音素種別。</summary>
        public PortugueseIpaPhoneme Phoneme { get; }

        /// <summary>この音素が強勢母音か。</summary>
        public bool IsStressed { get; }

        /// <summary>
        /// PortuguesePhoneme を初期化する。
        /// </summary>
        public PortuguesePhoneme(PortugueseIpaPhoneme phoneme, bool isStressed = false)
        {
            Phoneme = phoneme;
            IsStressed = isStressed;
        }

        /// <summary>この音素が音節主核となる母音（口母音または鼻母音）か。</summary>
        public bool IsSyllabicVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme <= PortugueseIpaPhoneme.HighCentral
                || (Phoneme >= PortugueseIpaPhoneme.ANasal && Phoneme <= PortugueseIpaPhoneme.UNasal);
        }

        /// <summary>この音素が鼻母音か。</summary>
        public bool IsNasalVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme >= PortugueseIpaPhoneme.ANasal && Phoneme <= PortugueseIpaPhoneme.UNasal;
        }

        /// <summary>この音素が半母音か。</summary>
        public bool IsSemivowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme == PortugueseIpaPhoneme.J || Phoneme == PortugueseIpaPhoneme.W;
        }

        /// <inheritdoc />
        public override string ToString() => Phoneme.ToString();

        /// <inheritdoc />
        public bool Equals(PortuguesePhoneme other) => Phoneme == other.Phoneme && IsStressed == other.IsStressed;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is PortuguesePhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Phoneme << 8) | (IsStressed ? 1 : 0);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(PortuguesePhoneme left, PortuguesePhoneme right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(PortuguesePhoneme left, PortuguesePhoneme right) => !left.Equals(right);
    }
}
