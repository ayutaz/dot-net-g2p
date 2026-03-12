using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Korean.Data;
using DotNetG2P.Korean.Normalization;
using DotNetG2P.Korean.Rules;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語 G2P エンジン。
    /// Hangul-first の規則ベース処理で Jamo / 音素列へ変換する。
    /// </summary>
    public sealed class KoreanG2PEngine : IDisposable
    {
        private readonly KoreanG2POptions _options;
        private int _disposed;

        /// <summary>デフォルトオプションで初期化する。</summary>
        public KoreanG2PEngine()
            : this(KoreanG2POptions.Default)
        {
        }

        /// <summary>オプションを指定して初期化する。</summary>
        public KoreanG2PEngine(KoreanG2POptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 入力テキストを space-separated phoneme sequence に変換する。
        /// </summary>
        public string ToPhonemes(string text)
        {
            var pronunciation = Analyze(text);
            if (pronunciation.PhonemesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.PhonemesInternal.Length * 2);
            for (var i = 0; i < pronunciation.PhonemesInternal.Length; i++)
            {
                if (i > 0)
                    builder.Append(_options.Separator);

                builder.Append(pronunciation.PhonemesInternal[i].Symbol);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 入力テキストを音節単位の Jamo 列へ変換する。
        /// </summary>
        public string ToJamo(string text)
        {
            var pronunciation = Analyze(text);
            if (pronunciation.SyllablesInternal.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(pronunciation.SyllablesInternal.Length * 3);
            var appendedSyllable = false;
            for (var i = 0; i < pronunciation.SyllablesInternal.Length; i++)
            {
                var syllable = pronunciation.SyllablesInternal[i];
                if (syllable.IsBoundary)
                {
                    builder.Append(syllable.ToJamoString());
                    appendedSyllable = false;
                    continue;
                }

                if (appendedSyllable)
                    builder.Append(_options.SyllableSeparator);

                builder.Append(syllable.ToJamoString());
                appendedSyllable = true;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 発音の中間表現を返す。
        /// </summary>
        public KoreanPronunciation Analyze(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return new KoreanPronunciation(string.Empty, string.Empty, Array.Empty<KoreanSyllable>(), Array.Empty<KoreanPhoneme>());

            var normalizedText = ApplyExceptionDictionary(Normalize(text));
            if (string.IsNullOrWhiteSpace(normalizedText))
                return new KoreanPronunciation(text, normalizedText, Array.Empty<KoreanSyllable>(), Array.Empty<KoreanPhoneme>());

            var decomposed = KoreanOrthography.DecomposeText(normalizedText, _options.PreserveNonHangul);
            var transformed = GraphemeToPhonemeRules.Convert(decomposed);
            var phonemes = KoreanOrthography.FlattenPhonemes(transformed);

            return new KoreanPronunciation(
                text,
                normalizedText,
                transformed,
                phonemes);
        }

        /// <summary>
        /// 複数テキストを一括で音素列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToPhonemes(texts[i]);
            return results;
        }

        /// <summary>
        /// 複数テキストを一括で Jamo 列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToJamoBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new string[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = ToJamo(texts[i]);
            return results;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        private string Normalize(string text)
        {
            if (_options.EnableTextNormalization)
                return KoreanNormalizer.Normalize(text, _options.EnableUnicodeNormalization);

            if (!_options.EnableUnicodeNormalization)
                return text;

            return text.Normalize(NormalizationForm.FormC);
        }

        private string ApplyExceptionDictionary(string text)
        {
            if (!_options.EnableExceptionDictionary || string.IsNullOrWhiteSpace(text))
                return text;

            if (KoreanExceptionDictionary.TryLookup(text, _options.UiVariationMode, out var wholeMatch))
                return wholeMatch;

            var builder = new StringBuilder(text.Length);
            var tokenStart = -1;

            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    AppendResolvedToken(text, tokenStart, i, builder);
                    tokenStart = -1;
                    builder.Append(text[i]);
                    continue;
                }

                if (tokenStart < 0)
                    tokenStart = i;
            }

            AppendResolvedToken(text, tokenStart, text.Length, builder);
            return builder.ToString();
        }

        private void AppendResolvedToken(string text, int tokenStart, int tokenEnd, StringBuilder builder)
        {
            if (tokenStart < 0 || tokenEnd <= tokenStart)
                return;

            var token = text.Substring(tokenStart, tokenEnd - tokenStart);
            if (KoreanExceptionDictionary.TryLookup(token, _options.UiVariationMode, out var replacement))
            {
                builder.Append(replacement);
                return;
            }

            builder.Append(token);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(KoreanG2PEngine));
        }
    }
}
