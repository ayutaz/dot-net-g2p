using System;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語の音素に付随する韻律情報。
    /// </summary>
    public readonly struct SwedishProsodyInfo : IEquatable<SwedishProsodyInfo>
    {
        /// <summary>ピッチアクセント番号。0=不明, 1=accent 1, 2=accent 2。</summary>
        public int A1 { get; }

        /// <summary>ストレスレベル。0=なし, 1=primary, 2=secondary。</summary>
        public int A2 { get; }

        /// <summary>語の音節数。</summary>
        public int A3 { get; }

        /// <summary>
        /// 韻律情報を初期化する。
        /// </summary>
        public SwedishProsodyInfo(int a1, int a2, int a3)
        {
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        /// <inheritdoc />
        public bool Equals(SwedishProsodyInfo other)
        {
            return A1 == other.A1
                && A2 == other.A2
                && A3 == other.A3;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SwedishProsodyInfo other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = A1;
                hash = (hash * 397) ^ A2;
                hash = (hash * 397) ^ A3;
                return hash;
            }
        }

        /// <summary>等価演算子。</summary>
        public static bool operator ==(SwedishProsodyInfo left, SwedishProsodyInfo right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(SwedishProsodyInfo left, SwedishProsodyInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"(a1={A1}, a2={A2}, a3={A3})";
    }
}
