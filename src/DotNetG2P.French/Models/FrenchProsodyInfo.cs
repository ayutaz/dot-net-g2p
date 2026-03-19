using System;

namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語音素の韻律情報（piper-plus 準拠）。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A1: 予約（常に 0）。</description></item>
    /// <item><description>A2: ストレスレベル（0=ストレスなし、2=ストレスあり）。</description></item>
    /// <item><description>A3: 語の音素数。</description></item>
    /// </list>
    /// </remarks>
    public readonly struct FrenchProsodyInfo : IEquatable<FrenchProsodyInfo>
    {
        /// <summary>予約（常に 0）。</summary>
        public int A1 { get; }

        /// <summary>ストレスレベル（0=ストレスなし、2=ストレスあり）。</summary>
        public int A2 { get; }

        /// <summary>語の音素数。</summary>
        public int A3 { get; }

        /// <summary>
        /// 韻律情報を初期化する。
        /// </summary>
        /// <param name="a1">予約（常に 0）。</param>
        /// <param name="a2">ストレスレベル（0=ストレスなし、2=ストレスあり）。</param>
        /// <param name="a3">語の音素数。</param>
        public FrenchProsodyInfo(int a1, int a2, int a3)
        {
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        /// <inheritdoc />
        public bool Equals(FrenchProsodyInfo other)
            => A1 == other.A1 && A2 == other.A2 && A3 == other.A3;

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is FrenchProsodyInfo other && Equals(other);

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

        /// <summary>等値演算子。</summary>
        public static bool operator ==(FrenchProsodyInfo left, FrenchProsodyInfo right) => left.Equals(right);

        /// <summary>非等値演算子。</summary>
        public static bool operator !=(FrenchProsodyInfo left, FrenchProsodyInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"(a1={A1}, a2={A2}, a3={A3})";
    }
}
