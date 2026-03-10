using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.French.Conversion;
using DotNetG2P.French.Normalization;
using DotNetG2P.French.Rules;

namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    public sealed class FrenchG2PEngine : IDisposable
    {
        private readonly FrenchG2POptions _options;
        private int _disposed;

        /// <summary>デフォルトオプションで初期化する。</summary>
        public FrenchG2PEngine()
            : this(FrenchG2POptions.Default)
        {
        }

        /// <summary>オプションを指定して初期化する。</summary>
        public FrenchG2PEngine(FrenchG2POptions options)
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
        public IReadOnlyList<FrenchPhoneme> ToPhonemeList(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return Array.Empty<FrenchPhoneme>();

            var result = new List<FrenchPhoneme>(words.Count * 6);
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                if (_options.EnableAllophones)
                    pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);
                result.AddRange(pronunciation.PhonemesInternal);
            }

            return result;
        }

        /// <summary>単語を音節分割し、各音節の音素配列を返す。</summary>
        public IReadOnlyList<FrenchPhoneme[]> ToSyllables(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(word))
                return Array.Empty<FrenchPhoneme[]>();

            var normalized = Normalize(word).Replace(" ", string.Empty);
            var pronunciation = GraphemeToPhonemeRules.ConvertWord(normalized, _options.Dialect, _options.EnableExceptionDictionary);
            if (_options.EnableAllophones)
                pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);
            var (syllableOffsets, phonemesWithNucleus) = FrenchSyllabifier.Syllabify(
                GetRawPhonemes(pronunciation));

            if (syllableOffsets.Length == 0)
                return Array.Empty<FrenchPhoneme[]>();

            var syllables = new FrenchPhoneme[syllableOffsets.Length][];
            for (var s = 0; s < syllableOffsets.Length; s++)
            {
                var start = syllableOffsets[s];
                var end = s + 1 < syllableOffsets.Length ? syllableOffsets[s + 1] : phonemesWithNucleus.Length;
                var length = end - start;
                var syllable = new FrenchPhoneme[length];
                Array.Copy(phonemesWithNucleus, start, syllable, 0, length);
                syllables[s] = syllable;
            }

            return syllables;
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

        /// <summary>入力テキストをX-SAMPA表記に変換する。</summary>
        public string ToXSampa(string text)
        {
            return ProcessText(text, pronunciation => XSampaConverter.Convert(pronunciation, _options.IncludeStress));
        }

        /// <summary>入力テキストをストレスマークなしのX-SAMPA表記に変換する。</summary>
        public string ToXSampaWithoutStress(string text)
        {
            return ProcessText(text, pronunciation => XSampaConverter.Convert(pronunciation, includeStress: false));
        }

        /// <summary>複数テキストを一括でX-SAMPAに変換する。</summary>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToXSampa(texts[i]);
            return results;
        }

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<FrenchPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new IReadOnlyList<FrenchPhoneme>[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToPhonemeList(texts[i]);
            return results;
        }

        /// <summary>リソースを解放する。</summary>
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        private string ProcessText(string text, Func<FrenchPronunciation, string> formatter)
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
                if (_options.EnableAllophones)
                    pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        private IReadOnlyList<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            return FrenchNormalizer.Tokenize(Normalize(text));
        }

        private string Normalize(string text)
        {
            if (_options.EnableTextNormalization)
                return FrenchNormalizer.Normalize(text);

            return text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        /// <summary>FrenchPronunciation から生のIPA音素配列を取得する。</summary>
        private static FrenchIpaPhoneme[] GetRawPhonemes(FrenchPronunciation pronunciation)
        {
            var internals = pronunciation.PhonemesInternal;
            var raw = new FrenchIpaPhoneme[internals.Length];
            for (var i = 0; i < internals.Length; i++)
                raw[i] = internals[i].Phoneme;
            return raw;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(FrenchG2PEngine));
        }
    }
}
