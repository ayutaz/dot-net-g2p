using System;
using System.Collections.Generic;
using System.Text;
using DotNetG2P.Spanish.Conversion;
using DotNetG2P.Spanish.Normalization;
using DotNetG2P.Spanish.Rules;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// スペイン語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    public sealed class SpanishG2PEngine : IDisposable
    {
        private readonly SpanishG2POptions _options;
        private bool _disposed;

        /// <summary>デフォルトオプションで初期化する。</summary>
        public SpanishG2PEngine()
            : this(SpanishG2POptions.Default)
        {
        }

        /// <summary>オプションを指定して初期化する。</summary>
        public SpanishG2PEngine(SpanishG2POptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>入力テキストをスペース区切りのIPA音素列に変換する。</summary>
        public string ToPhonemes(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.ConvertPhonemeSequence(ApplyAllophonesIfNeeded(pronunciation), _options.IncludeStress, _options.Separator));
        }

        /// <summary>入力テキストをIPA表記に変換する。</summary>
        public string ToIPA(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.Convert(ApplyAllophonesIfNeeded(pronunciation), _options.IncludeStress));
        }

        /// <summary>入力テキストを X-SAMPA 表記に変換する。</summary>
        public string ToXSampa(string text)
        {
            return ProcessText(text, pronunciation => XSampaConverter.Convert(ApplyAllophonesIfNeeded(pronunciation), _options.IncludeStress));
        }

        /// <summary>入力テキストをストレスマークなしの X-SAMPA 表記に変換する。</summary>
        public string ToXSampaWithoutStress(string text)
        {
            return ProcessText(text, pronunciation => XSampaConverter.Convert(ApplyAllophonesIfNeeded(pronunciation), includeStress: false));
        }

        /// <summary>入力テキストを音素リストに変換する。</summary>
        public IReadOnlyList<SpanishPhoneme> ToPhonemeList(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return Array.Empty<SpanishPhoneme>();

            var result = new List<SpanishPhoneme>(words.Count * 6);
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                result.AddRange(ApplyAllophonesIfNeeded(pronunciation).PhonemesInternal);
            }

            return result;
        }

        /// <summary>単語を音節分割する。</summary>
        public IReadOnlyList<SpanishSyllable> ToSyllables(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(word))
                return Array.Empty<SpanishSyllable>();

            var normalized = Normalize(word).Replace(" ", string.Empty);
            return StressAssigner.MarkStress(normalized, SpanishSyllabifier.Syllabify(normalized));
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

        /// <summary>複数テキストを一括で X-SAMPA に変換する。</summary>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToXSampa(texts[i]);
            return results;
        }

        /// <summary>リソースを解放する。</summary>
        public void Dispose()
        {
            _disposed = true;
        }

        private string ProcessText(string text, Func<SpanishPronunciation, string> formatter)
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
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        private IReadOnlyList<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            return SpanishNormalizer.Tokenize(Normalize(text));
        }

        private string Normalize(string text)
        {
            if (_options.EnableTextNormalization)
                return SpanishNormalizer.Normalize(text);

            return text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private SpanishPronunciation ApplyAllophonesIfNeeded(SpanishPronunciation pronunciation)
        {
            return _options.EnableAllophones
                ? AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures)
                : pronunciation;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SpanishG2PEngine));
        }
    }
}
