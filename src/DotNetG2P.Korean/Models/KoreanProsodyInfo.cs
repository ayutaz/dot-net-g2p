using System;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語の音素に付随する韻律情報。
    /// piper-plus 互換の a1/a2/a3 値を保持する。
    /// </summary>
    public readonly struct KoreanProsodyInfo : IEquatable<KoreanProsodyInfo>
    {
        /// <summary>声調番号（韓国語では常に 0）。</summary>
        public int A1 { get; }

        /// <summary>語内のモーラ位置（韓国語では常に 0）。</summary>
        public int A2 { get; }

        /// <summary>語の音節数（max(syllable_count, 1)）。</summary>
        public int A3 { get; }

        /// <summary>
        /// 韻律情報を初期化する。
        /// </summary>
        public KoreanProsodyInfo(int a1, int a2, int a3)
        {
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        /// <inheritdoc />
        public bool Equals(KoreanProsodyInfo other)
        {
            return A1 == other.A1
                && A2 == other.A2
                && A3 == other.A3;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is KoreanProsodyInfo other && Equals(other);

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
        public static bool operator ==(KoreanProsodyInfo left, KoreanProsodyInfo right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(KoreanProsodyInfo left, KoreanProsodyInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"(a1={A1}, a2={A2}, a3={A3})";
    }
}
