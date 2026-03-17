using System;

namespace DotNetG2P.Chinese
{
    /// <summary>
    /// 中国語音節の韻律情報（piper-plus準拠）。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A1: 声調番号（1-5）。声調変調適用後の値。</description></item>
    /// <item><description>A2: 語内シラブル位置（1ベース）。</description></item>
    /// <item><description>A3: 語のシラブル数。</description></item>
    /// </list>
    /// 語の定義: 連続する漢字がひとつの「語」を構成する。句読点/スペース/非漢字で区切られる。
    /// </remarks>
    public readonly struct ChineseProsodyInfo : IEquatable<ChineseProsodyInfo>
    {
        /// <summary>
        /// 声調番号（1-5）。声調変調適用後の値。
        /// 軽声は5として扱う。
        /// </summary>
        public int A1 { get; }

        /// <summary>
        /// 語内シラブル位置（1ベース）。
        /// </summary>
        public int A2 { get; }

        /// <summary>
        /// 語のシラブル数。
        /// </summary>
        public int A3 { get; }

        /// <summary>
        /// ChineseProsodyInfoを初期化する。
        /// </summary>
        /// <param name="a1">声調番号（1-5）</param>
        /// <param name="a2">語内シラブル位置（1ベース）</param>
        /// <param name="a3">語のシラブル数</param>
        public ChineseProsodyInfo(int a1, int a2, int a3)
        {
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        /// <inheritdoc />
        public bool Equals(ChineseProsodyInfo other)
            => A1 == other.A1 && A2 == other.A2 && A3 == other.A3;

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is ChineseProsodyInfo other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
            => (A1 << 16) | (A2 << 8) | A3;

        /// <summary>等値演算子。</summary>
        public static bool operator ==(ChineseProsodyInfo left, ChineseProsodyInfo right) => left.Equals(right);

        /// <summary>非等値演算子。</summary>
        public static bool operator !=(ChineseProsodyInfo left, ChineseProsodyInfo right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"a1={A1}, a2={A2}, a3={A3}";
    }
}
