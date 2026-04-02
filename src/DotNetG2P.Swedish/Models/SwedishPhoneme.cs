using System;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// ストレス情報と音節核マーク付きのスウェーデン語音素。
    /// </summary>
    public readonly struct SwedishPhoneme : IEquatable<SwedishPhoneme>
    {
        /// <summary>IPA音素。</summary>
        public SwedishIpaPhoneme Phoneme { get; }

        /// <summary>この音素がストレス音節に属するか。</summary>
        public bool IsStressed { get; }

        /// <summary>この音素が音節の核（母音）であるか。</summary>
        public bool IsSyllableNucleus { get; }

        /// <summary>
        /// SwedishPhoneme を初期化する。
        /// </summary>
        public SwedishPhoneme(SwedishIpaPhoneme phoneme, bool isStressed = false, bool isSyllableNucleus = false)
        {
            Phoneme = phoneme;
            IsStressed = isStressed;
            IsSyllableNucleus = isSyllableNucleus;
        }

        /// <summary>この音素が母音かどうかを判定する。</summary>
        public bool IsVowel => Phoneme <= SwedishIpaPhoneme.Schwa;

        /// <summary>この音素が子音かどうかを判定する。</summary>
        public bool IsConsonant => Phoneme >= SwedishIpaPhoneme.P;

        /// <summary>この音素がそり舌音かどうかを判定する。</summary>
        public bool IsRetroflex => Phoneme >= SwedishIpaPhoneme.RetroT && Phoneme <= SwedishIpaPhoneme.RetroS;

        /// <summary>この音素が長母音かどうかを判定する。</summary>
        public bool IsLongVowel => Phoneme <= SwedishIpaPhoneme.LongA;

        /// <inheritdoc />
        public bool Equals(SwedishPhoneme other) =>
            Phoneme == other.Phoneme && IsStressed == other.IsStressed && IsSyllableNucleus == other.IsSyllableNucleus;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SwedishPhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Phoneme, IsStressed, IsSyllableNucleus);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(SwedishPhoneme left, SwedishPhoneme right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(SwedishPhoneme left, SwedishPhoneme right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"{Phoneme}{(IsStressed ? "\u02c8" : "")}{(IsSyllableNucleus ? "*" : "")}";
    }
}
