#nullable enable

namespace DotNetG2P.Multilingual
{
    /// <summary>言語タグ付きG2P結果セグメント。</summary>
    public sealed class G2PSegment
    {
        /// <summary>セグメントの言語。</summary>
        public Language Language { get; }

        /// <summary>変換元テキスト。</summary>
        public string SourceText { get; }

        /// <summary>音素変換結果。</summary>
        public string Phonemes { get; }

        /// <summary>
        /// G2PSegmentを初期化する。
        /// </summary>
        /// <param name="language">言語</param>
        /// <param name="sourceText">変換元テキスト</param>
        /// <param name="phonemes">音素変換結果</param>
        public G2PSegment(Language language, string sourceText, string phonemes)
        {
            Language = language;
            SourceText = sourceText;
            Phonemes = phonemes;
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{Language}] {SourceText} → {Phonemes}";
    }
}
