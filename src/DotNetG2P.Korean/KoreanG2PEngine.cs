using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語 G2P エンジン。
    /// M1 では Hangul を分解して Jamo / 音素列へ変換する最小スキャフォールドを提供する。
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
            for (var i = 0; i < pronunciation.SyllablesInternal.Length; i++)
            {
                if (i > 0)
                    builder.Append(_options.SyllableSeparator);

                builder.Append(pronunciation.SyllablesInternal[i].ToJamoString());
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

            var normalizedText = Normalize(text);
            var syllables = new List<KoreanSyllable>(normalizedText.Length);
            var phonemes = new List<KoreanPhoneme>(normalizedText.Length * 3);

            for (var i = 0; i < normalizedText.Length; i++)
            {
                var c = normalizedText[i];

                if (char.IsWhiteSpace(c))
                    continue;

                if (KoreanSyllable.TryDecompose(c, out var syllable))
                {
                    syllables.Add(syllable);
                    phonemes.AddRange(syllable.ToPhonemes());
                    continue;
                }

                if (IsCompatibilityJamo(c))
                {
                    var standalone = KoreanSyllable.FromStandaloneJamo(c);
                    syllables.Add(standalone);
                    phonemes.Add(new KoreanPhoneme(c));
                    continue;
                }

                if (_options.PreserveNonHangul)
                {
                    var standalone = KoreanSyllable.FromStandaloneJamo(c);
                    syllables.Add(standalone);
                    phonemes.Add(new KoreanPhoneme(c));
                }
            }

            return new KoreanPronunciation(
                text,
                normalizedText,
                syllables.ToArray(),
                phonemes.ToArray());
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
            if (!_options.EnableUnicodeNormalization)
                return text;

            return text.Normalize(NormalizationForm.FormC);
        }

        private static bool IsCompatibilityJamo(char c)
        {
            return c >= '\u3131' && c <= '\u318E';
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(KoreanG2PEngine));
        }
    }
}
