using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using DotNetG2P.Swedish.Conversion;
using DotNetG2P.Swedish.Rules;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace DotNetG2P.Swedish
{
    /// <summary>
    /// スウェーデン語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
    public sealed class SwedishG2PEngine : IDisposable
    {
        private readonly SwedishG2POptions _options;
        private int _disposed;

        /// <summary>デフォルトオプションで初期化する。</summary>
        public SwedishG2PEngine()
            : this(SwedishG2POptions.Default)
        {
        }

        /// <summary>オプションを指定して初期化する。</summary>
        public SwedishG2PEngine(SwedishG2POptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>入力テキストをスペース区切りのIPA音素列に変換する。</summary>
        public string ToPhonemes(string text)
        {
            return ProcessText(text, pronunciation =>
                IpaConverter.ConvertPhonemeSequence(pronunciation, _options.IncludeStress, _options.Separator));
        }

        /// <summary>入力テキストをIPA表記に変換する。</summary>
        public string ToIPA(string text)
        {
            return ProcessText(text, pronunciation =>
                IpaConverter.Convert(pronunciation, _options.IncludeStress));
        }

        /// <summary>入力テキストをストレスなしIPA表記に変換する。</summary>
        public string ToIPAWithoutStress(string text)
        {
            return ProcessText(text, pronunciation =>
                IpaConverter.Convert(pronunciation, includeStress: false));
        }

        /// <summary>入力テキストを音素リストに変換する。</summary>
        public IReadOnlyList<SwedishPhoneme> ToPhonemeList(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Length == 0)
                return Array.Empty<SwedishPhoneme>();

            var result = new List<SwedishPhoneme>(words.Length * 6);
            for (var i = 0; i < words.Length; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect);
                result.AddRange(pronunciation.PhonemesInternal);
            }

            return result;
        }

        /// <summary>入力テキストを音節リストに変換する。</summary>
        public IReadOnlyList<SwedishSyllable> ToSyllables(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(word))
                return Array.Empty<SwedishSyllable>();

            var lower = word.ToLowerInvariant();
            return StressAssigner.MarkStress(lower, SwedishSyllabifier.Syllabify(lower));
        }

        /// <summary>複数テキストをスペース区切りIPA音素列に一括変換する。</summary>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPhonemes);
        }

        /// <summary>複数テキストをIPA表記に一括変換する。</summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIPA);
        }

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<SwedishPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray<IReadOnlyList<SwedishPhoneme>>(texts, ToPhonemeList);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        // =================================================================
        // 内部メソッド
        // =================================================================

        /// <summary>
        /// テキストをトークン化し、各単語を変換してフォーマッタで文字列化する共通パイプライン。
        /// </summary>
        private string ProcessText(string text, Func<SwedishPronunciation, string> formatter)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(text.Length + 8);
            for (var i = 0; i < words.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect);
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        /// <summary>テキストをNFC正規化・小文字化し、空白で分割してトークン配列を返す。</summary>
        private string[] GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            // NFC正規化 + 小文字化（パイプライン全体でこの1回のみ）
            var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
            return normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(SwedishG2PEngine));
        }
    }
}
