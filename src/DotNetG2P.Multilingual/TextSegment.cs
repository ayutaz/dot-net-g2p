#nullable enable

namespace DotNetG2P.Multilingual
{
    /// <summary>言語タグ付きテキストセグメント。</summary>
    public readonly struct TextSegment
    {
        /// <summary>セグメントのテキスト。</summary>
        public string Text { get; }

        /// <summary>セグメントの言語。</summary>
        public Language Language { get; }

        /// <summary>
        /// TextSegmentを初期化する。
        /// </summary>
        /// <param name="text">テキスト</param>
        /// <param name="language">言語</param>
        public TextSegment(string text, Language language)
        {
            Text = text;
            Language = language;
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{Language}] {Text}";
    }
}
