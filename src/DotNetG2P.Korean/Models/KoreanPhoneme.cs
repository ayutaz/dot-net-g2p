using System;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語音素の最小表現。M1 では compatibility jamo を表面形として保持する。
    /// </summary>
    public readonly struct KoreanPhoneme : IEquatable<KoreanPhoneme>
    {
        /// <summary>音素記号。</summary>
        public char Symbol { get; }

        /// <summary>
        /// 音素を初期化する。
        /// </summary>
        public KoreanPhoneme(char symbol)
        {
            Symbol = symbol;
        }

        /// <inheritdoc />
        public override string ToString() => Symbol.ToString();

        /// <inheritdoc />
        public bool Equals(KoreanPhoneme other) => Symbol == other.Symbol;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is KoreanPhoneme other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Symbol;

        /// <summary>等価演算子。</summary>
        public static bool operator ==(KoreanPhoneme left, KoreanPhoneme right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(KoreanPhoneme left, KoreanPhoneme right) => !left.Equals(right);
    }
}
