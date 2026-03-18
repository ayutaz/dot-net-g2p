using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using UnityEngine.Scripting;
using DotNetG2P.Korean.Conversion;
using DotNetG2P.Korean.Data;
using DotNetG2P.Korean.Normalization;
using DotNetG2P.Korean.Rules;

namespace DotNetG2P.Korean
{
    /// <summary>
    /// 韓国語 G2P エンジン。
    /// Hangul-first の規則ベース処理で Jamo / 音素列へ変換する。
    /// </summary>
    [Preserve]
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
        /// 入力テキストを IPA 文字列へ変換する。
        /// </summary>
        public string ToIPA(string text)
        {
            var pronunciation = Analyze(text);
            if (pronunciation.SyllablesInternal.Length == 0)
                return string.Empty;

            var ipaSegments = JamoToIpa.ConvertSyllables(pronunciation.SyllablesInternal);

            var builder = new StringBuilder(ipaSegments.Length * 3);
            var appendedSyllable = false;
            for (var i = 0; i < ipaSegments.Length; i++)
            {
                var syllable = pronunciation.SyllablesInternal[i];
                if (syllable.IsBoundary)
                {
                    builder.Append(' ');
                    appendedSyllable = false;
                    continue;
                }

                if (appendedSyllable)
                    builder.Append(_options.Separator);

                builder.Append(ipaSegments[i]);
                appendedSyllable = true;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 入力テキストを IPA 音素配列として返す。
        /// </summary>
        public string[] ToIpaPhonemes(string text)
        {
            var pronunciation = Analyze(text);
            return JamoToIpa.ConvertSyllables(pronunciation.SyllablesInternal);
        }

        /// <summary>
        /// 入力テキストを IPA 音素文字列として返す。
        /// </summary>
        public string ToIpa(string text)
        {
            var phonemes = ToIpaPhonemes(text);
            if (phonemes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(phonemes.Length * 3);
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (i > 0)
                    builder.Append(_options.Separator);

                builder.Append(phonemes[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素配列として返す。
        /// 多文字 IPA 音素を PUA 単一文字に置換した形式。
        /// </summary>
        public string[] ToPuaPhonemes(string text)
        {
            var ipaPhonemes = ToIpaPhonemes(text);
            return PuaMapper.ApplyPuaMapping(ipaPhonemes);
        }

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素文字列として返す。
        /// </summary>
        public string ToPuaString(string text)
        {
            var phonemes = ToPuaPhonemes(text);
            if (phonemes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(phonemes.Length * 2);
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (i > 0)
                    builder.Append(_options.Separator);

                builder.Append(phonemes[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 入力テキストを IPA 音素配列と韻律情報（a1, a2, a3）のペアとして返す。
        /// piper-plus 互換の韻律情報を含む。
        /// </summary>
        public KoreanProsodyResult ToIpaWithProsody(string text)
        {
            var pronunciation = Analyze(text);
            var syllables = pronunciation.SyllablesInternal;

            if (syllables.Length == 0)
                return new KoreanProsodyResult(Array.Empty<string>(), Array.Empty<KoreanProsodyInfo>());

            var ipaList = new List<string>();
            var wordSyllableCounts = new List<int>();
            var phonemeWordIndices = new List<int>();
            var currentWordSyllableCount = 0;
            var currentWordIndex = 0;

            for (var i = 0; i < syllables.Length; i++)
            {
                var syllable = syllables[i];

                if (syllable.IsBoundary)
                {
                    if (currentWordSyllableCount > 0 || currentWordIndex == 0)
                    {
                        wordSyllableCounts.Add(currentWordSyllableCount);
                        currentWordIndex = wordSyllableCounts.Count;
                        currentWordSyllableCount = 0;
                    }
                    continue;
                }

                if (syllable.HasNucleus)
                {
                    currentWordSyllableCount++;

                    var onsetIpa = JamoToIpa.ConvertOnset(syllable.Onset);
                    if (onsetIpa.Length > 0)
                    {
                        ipaList.Add(onsetIpa);
                        phonemeWordIndices.Add(currentWordIndex);
                    }

                    var nucleusIpa = JamoToIpa.ConvertNucleus(syllable.Nucleus);
                    ipaList.Add(nucleusIpa);
                    phonemeWordIndices.Add(currentWordIndex);

                    if (syllable.HasCoda)
                    {
                        var codaIpa = JamoToIpa.ConvertCoda(syllable.Coda);
                        ipaList.Add(codaIpa);
                        phonemeWordIndices.Add(currentWordIndex);
                    }
                }
                else
                {
                    var standaloneIpa = JamoToIpa.ConvertOnset(syllable.Onset);
                    ipaList.Add(standaloneIpa);
                    phonemeWordIndices.Add(currentWordIndex);
                }
            }

            wordSyllableCounts.Add(currentWordSyllableCount);

            var phonemes = ipaList.ToArray();
            var prosody = new KoreanProsodyInfo[phonemes.Length];
            for (var i = 0; i < phonemes.Length; i++)
            {
                var wordIndex = phonemeWordIndices[i];
                var syllableCount = wordIndex < wordSyllableCounts.Count
                    ? wordSyllableCounts[wordIndex]
                    : 0;
                var a3 = Math.Max(syllableCount, 1);
                prosody[i] = new KoreanProsodyInfo(0, 0, a3);
            }

            return new KoreanProsodyResult(phonemes, prosody);
        }

        /// <summary>
        /// 複数テキストを一括で IPA 文字列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIPA);
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素文字列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToIpaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpa);
        }

        /// <summary>
        /// 複数テキストを一括で PUA 音素文字列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToPuaStringBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPuaString);
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素配列と韻律情報のペアへ変換する。
        /// </summary>
        public IReadOnlyList<KoreanProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
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
            return BatchConversionHelper.ConvertToArray(texts, ToPhonemes);
        }

        /// <summary>
        /// 複数テキストを一括で Jamo 列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToJamoBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToJamo);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        private string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (_options.EnableTextNormalization)
                return KoreanNormalizer.Normalize(text, _options.EnableUnicodeNormalization);

            return _options.EnableUnicodeNormalization
                ? text.Normalize(NormalizationForm.FormKC)
                : text;
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

            builder.Append(text, tokenStart, tokenEnd - tokenStart);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(KoreanG2PEngine));
        }
    }
}
