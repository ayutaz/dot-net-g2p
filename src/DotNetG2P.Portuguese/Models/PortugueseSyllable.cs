using System;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// 単語中の音節情報。
    /// </summary>
    public readonly struct PortugueseSyllable : IEquatable<PortugueseSyllable>
    {
        /// <summary>元単語内の開始位置。</summary>
        public int StartIndex { get; }

        /// <summary>音節長。</summary>
        public int Length { get; }

        /// <summary>音節テキスト。</summary>
        public string Text { get; }

        /// <summary>この音節が強勢か。</summary>
        public bool IsStressed { get; }

        /// <summary>
        /// PortugueseSyllable を初期化する。
        /// </summary>
        public PortugueseSyllable(int startIndex, int length, string text, bool isStressed = false)
        {
            StartIndex = startIndex;
            Length = length;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            IsStressed = isStressed;
        }

        /// <inheritdoc />
        public override string ToString() => Text;

        /// <inheritdoc />
        public bool Equals(PortugueseSyllable other)
            => StartIndex == other.StartIndex && Length == other.Length && Text == other.Text && IsStressed == other.IsStressed;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is PortugueseSyllable other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(StartIndex, Length, Text, IsStressed);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(PortugueseSyllable left, PortugueseSyllable right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(PortugueseSyllable left, PortugueseSyllable right) => !left.Equals(right);
    }
}
