using System;

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語の音節情報。
    /// </summary>
    public readonly struct SwedishSyllable : IEquatable<SwedishSyllable>
    {
        /// <summary>元テキスト内での開始インデックス。</summary>
        public int StartIndex { get; }

        /// <summary>音節の文字数。</summary>
        public int Length { get; }

        /// <summary>音節のテキスト。</summary>
        public string Text { get; }

        /// <summary>この音節にストレスがあるか。</summary>
        public bool IsStressed { get; }

        /// <summary>
        /// SwedishSyllable を初期化する。
        /// </summary>
        public SwedishSyllable(int startIndex, int length, string text, bool isStressed = false)
        {
            StartIndex = startIndex;
            Length = length;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            IsStressed = isStressed;
        }

        /// <inheritdoc />
        public override string ToString() => IsStressed ? $"\u02c8{Text}" : Text;

        /// <inheritdoc />
        public bool Equals(SwedishSyllable other)
            => StartIndex == other.StartIndex && Length == other.Length && Text == other.Text && IsStressed == other.IsStressed;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SwedishSyllable other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(StartIndex, Length, Text, IsStressed);

        /// <summary>等価演算子。</summary>
        public static bool operator ==(SwedishSyllable left, SwedishSyllable right) => left.Equals(right);

        /// <summary>非等価演算子。</summary>
        public static bool operator !=(SwedishSyllable left, SwedishSyllable right) => !left.Equals(right);
    }
}
