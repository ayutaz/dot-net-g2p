using System;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// 単語中の音節情報。
    /// </summary>
    public readonly struct SpanishSyllable : IEquatable<SpanishSyllable>
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
        /// SpanishSyllable を初期化する。
        /// </summary>
        public SpanishSyllable(int startIndex, int length, string text, bool isStressed = false)
        {
            StartIndex = startIndex;
            Length = length;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            IsStressed = isStressed;
        }

        /// <inheritdoc />
        public override string ToString() => Text;

        /// <inheritdoc />
        public bool Equals(SpanishSyllable other)
            => StartIndex == other.StartIndex && Length == other.Length && Text == other.Text && IsStressed == other.IsStressed;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SpanishSyllable other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(StartIndex, Length, Text, IsStressed);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(SpanishSyllable left, SpanishSyllable right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(SpanishSyllable left, SpanishSyllable right) => !left.Equals(right);
    }
}
