using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using DotNetG2P.Spanish.Conversion;
using DotNetG2P.Spanish.Normalization;
using DotNetG2P.Spanish.Rules;
using UnityEngine.Scripting;

namespace DotNetG2P.Spanish
{
    /// <summary>
    /// スペイン語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    [Preserve]
    public sealed class SpanishG2PEngine : IDisposable
    {
        private readonly SpanishG2POptions _options;
        private int _disposed;

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

        /// <summary>入力テキストをストレスマークなしのIPA表記に変換する。</summary>
        public string ToIPAWithoutStress(string text)
        {
            return ProcessText(text, pronunciation => IpaConverter.Convert(ApplyAllophonesIfNeeded(pronunciation), includeStress: false));
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
            return BatchConversionHelper.ConvertToArray(texts, ToPhonemes);
        }

        /// <summary>複数テキストを一括でIPAに変換する。</summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIPA);
        }

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<SpanishPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray<IReadOnlyList<SpanishPhoneme>>(texts, ToPhonemeList);
        }

        /// <summary>複数テキストを一括で X-SAMPA に変換する。</summary>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToXSampa);
        }

        // =====================================================================
        // PUA API
        // =====================================================================

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素配列として返す。
        /// 多文字 IPA 音素を PUA 単一文字に置換した形式。
        /// </summary>
        public string[] ToPuaPhonemes(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return Array.Empty<string>();

            var ipaPhonemes = CollectIpaPhonemeStrings(words);
            return SpanishPuaMapper.ApplyPuaMapping(ipaPhonemes);
        }

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素文字列として返す。
        /// </summary>
        public string ToPuaString(string text)
        {
            var phonemes = ToPuaPhonemes(text);
            return phonemes.Length == 0 ? string.Empty : string.Join(" ", phonemes);
        }

        /// <summary>複数テキストを一括で PUA 音素文字列に変換する。</summary>
        public IReadOnlyList<string> ToPuaStringBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPuaString);
        }

        // =====================================================================
        // Prosody API
        // =====================================================================

        /// <summary>
        /// 入力テキストの IPA 音素配列と韻律情報（a1, a2, a3）を返す。
        /// piper-plus 互換の韻律情報を含む。
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>A1: 0（スペイン語では未使用）</description></item>
        /// <item><description>A2: ストレス音節位置（1ベース）。ストレスなしの場合は0。</description></item>
        /// <item><description>A3: 語の音節数。</description></item>
        /// </list>
        /// </remarks>
        public SpanishProsodyResult ToIpaWithProsody(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return new SpanishProsodyResult(Array.Empty<string>(), Array.Empty<SpanishProsodyInfo>());

            var allPhonemes = new List<string>();
            var allProsody = new List<SpanishProsodyInfo>();

            for (var w = 0; w < words.Count; w++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[w], _options.Dialect, _options.EnableExceptionDictionary);
                var applied = ApplyAllophonesIfNeeded(pronunciation);

                if (applied.PhonemesInternal.Length == 0)
                    continue;

                var syllableCount = applied.SyllableOffsetsInternal.Length;
                // ストレス音節位置（1ベース）。ストレスなしの場合は0。
                var stressPosition = applied.StressedSyllableIndex >= 0
                    ? applied.StressedSyllableIndex + 1
                    : 0;

                for (var i = 0; i < applied.PhonemesInternal.Length; i++)
                {
                    var symbol = IpaConverter.ToSymbol(applied.PhonemesInternal[i].Phoneme);
                    allPhonemes.Add(symbol);
                    allProsody.Add(new SpanishProsodyInfo(0, stressPosition, syllableCount));
                }
            }

            return new SpanishProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        /// <summary>複数テキストを一括で IPA 音素配列と韻律情報のペアに変換する。</summary>
        public IReadOnlyList<SpanishProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
        }

        /// <summary>リソースを解放する。</summary>
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
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

        private string[] CollectIpaPhonemeStrings(IReadOnlyList<string> words)
        {
            var result = new List<string>(words.Count * 6);
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                var applied = ApplyAllophonesIfNeeded(pronunciation);
                for (var j = 0; j < applied.PhonemesInternal.Length; j++)
                    result.Add(IpaConverter.ToSymbol(applied.PhonemesInternal[j].Phoneme));
            }

            return result.ToArray();
        }

        private SpanishPronunciation ApplyAllophonesIfNeeded(SpanishPronunciation pronunciation)
        {
            return _options.EnableAllophones
                ? AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures)
                : pronunciation;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(SpanishG2PEngine));
        }
    }
}
