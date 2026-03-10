using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.French
{
    /// <summary>
    /// 音節核情報付きのフランス語IPA音素。
    /// </summary>
    public readonly struct FrenchPhoneme : IEquatable<FrenchPhoneme>
    {
        /// <summary>音素種別。</summary>
        public FrenchIpaPhoneme Phoneme { get; }

        /// <summary>この音素が音節核か。</summary>
        public bool IsSyllableNucleus { get; }

        /// <summary>
        /// FrenchPhoneme を初期化する。
        /// </summary>
        public FrenchPhoneme(FrenchIpaPhoneme phoneme, bool isSyllableNucleus = false)
        {
            Phoneme = phoneme;
            IsSyllableNucleus = isSyllableNucleus;
        }

        /// <summary>この音素が母音（口母音または鼻母音）か。</summary>
        public bool IsVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme <= FrenchIpaPhoneme.OeNasal;
        }

        /// <summary>この音素が口母音か。</summary>
        public bool IsOralVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme <= FrenchIpaPhoneme.Schwa;
        }

        /// <summary>この音素が鼻母音か。</summary>
        public bool IsNasalVowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme >= FrenchIpaPhoneme.ANasal && Phoneme <= FrenchIpaPhoneme.OeNasal;
        }

        /// <summary>この音素が半母音か。</summary>
        public bool IsSemivowel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Phoneme >= FrenchIpaPhoneme.J && Phoneme <= FrenchIpaPhoneme.Uj;
        }

        /// <inheritdoc />
        public override string ToString() => Conversion.IpaConverter.ToSymbol(Phoneme);

        /// <inheritdoc />
        public bool Equals(FrenchPhoneme other) => Phoneme == other.Phoneme && IsSyllableNucleus == other.IsSyllableNucleus;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FrenchPhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Phoneme << 8) | (IsSyllableNucleus ? 1 : 0);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(FrenchPhoneme left, FrenchPhoneme right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(FrenchPhoneme left, FrenchPhoneme right) => !left.Equals(right);
    }
}
