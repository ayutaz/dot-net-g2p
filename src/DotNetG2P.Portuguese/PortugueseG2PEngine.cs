using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Portuguese.Conversion;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    public sealed class PortugueseG2PEngine : IDisposable
    {
        private readonly PortugueseG2POptions _options;
        private int _disposed;

        /// <summary>デフォルトオプションで初期化する。</summary>
        public PortugueseG2PEngine()
            : this(PortugueseG2POptions.Default)
        {
        }

        /// <summary>オプションを指定して初期化する。</summary>
        public PortugueseG2PEngine(PortugueseG2POptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>入力テキストをスペース区切りのIPA音素列に変換する。</summary>
        public string ToPhonemes(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.ConvertPhonemeSequence(pronunciation, _options.IncludeStress, _options.Separator));
        }

        /// <summary>入力テキストをIPA表記に変換する。</summary>
        public string ToIPA(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.Convert(pronunciation, _options.IncludeStress));
        }

        /// <summary>入力テキストをストレスマークなしのIPA表記に変換する。</summary>
        public string ToIPAWithoutStress(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.Convert(pronunciation, includeStress: false));
        }

        /// <summary>入力テキストを音素リストに変換する。</summary>
        public IReadOnlyList<PortuguesePhoneme> ToPhonemeList(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return Array.Empty<PortuguesePhoneme>();

            var result = new List<PortuguesePhoneme>(words.Count * 6);
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                result.AddRange(pronunciation.PhonemesInternal);
            }

            return result;
        }

        /// <summary>単語を音節分割し、各音節情報を返す。</summary>
        public IReadOnlyList<PortugueseSyllable> ToSyllables(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<PortugueseSyllable>();

            var normalized = Normalize(text);
            return PortugueseSyllabifier.Syllabify(normalized);
        }

        /// <summary>複数テキストを一括でIPAに変換する。</summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToIPA(texts[i]);
            return results;
        }

        /// <summary>複数テキストを一括で音素列に変換する。</summary>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToPhonemes(texts[i]);
            return results;
        }

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<PortuguesePhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new IReadOnlyList<PortuguesePhoneme>[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToPhonemeList(texts[i]);
            return results;
        }

        /// <summary>リソースを解放する。</summary>
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        private string ProcessText(string text, Func<PortuguesePronunciation, string> formatter)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return string.Empty;

            var builder = new StringBuilder(text.Length + 8);
            for (var i = 0; i < words.Count; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                // P2でAllophoneProcessor追加予定
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        private IReadOnlyList<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var normalized = Normalize(text);
            // P1ではシンプルな空白・句読点分割。P2でPortugueseNormalizer.Tokenize()に置換
            var parts = normalized.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0 && ContainsAlpha(trimmed))
                    result.Add(trimmed);
            }
            return result;
        }

        private string Normalize(string text)
        {
            // P1ではNormalizer未実装。基本的なNFC正規化+小文字化のみ
            return text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private static bool ContainsAlpha(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (char.IsLetter(s[i])) return true;
            return false;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(PortugueseG2PEngine));
        }
    }
}
