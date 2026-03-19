using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語の音素に付随する韻律情報。
    /// piper-plus 互換の a1/a2/a3 値を保持する。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A1: 予約（ポルトガル語では常に 0）。</description></item>
    /// <item><description>A2: ストレス音節位置（1ベース）。ストレスなしの場合は 0。</description></item>
    /// <item><description>A3: 語の音節数。</description></item>
    /// </list>
    /// </remarks>
    public readonly struct PortugueseProsodyInfo : IEquatable<PortugueseProsodyInfo>
    {
        /// <summary>予約（ポルトガル語では常に 0）。</summary>
        public int A1 { get; }

        /// <summary>ストレス音節位置（1ベース）。ストレスなしの場合は 0。</summary>
        public int A2 { get; }

        /// <summary>語の音節数。</summary>
        public int A3 { get; }

        /// <summary>
        /// 韻律情報を初期化する。
        /// </summary>
        /// <param name="a1">予約（常に 0）。</param>
        /// <param name="a2">ストレス音節位置（1ベース）。</param>
        /// <param name="a3">語の音節数。</param>
        public PortugueseProsodyInfo(int a1, int a2, int a3)
        {
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        /// <inheritdoc />
        public bool Equals(PortugueseProsodyInfo other)
        {
            return A1 == other.A1
                && A2 == other.A2
                && A3 == other.A3;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is PortugueseProsodyInfo other && Equals(other);

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
        public static bool operator ==(PortugueseProsodyInfo left, PortugueseProsodyInfo right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(PortugueseProsodyInfo left, PortugueseProsodyInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"(a1={A1}, a2={A2}, a3={A3})";
    }
}
