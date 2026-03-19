using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using UnityEngine.Scripting;
using DotNetG2P.Portuguese.Conversion;
using DotNetG2P.Portuguese.Normalization;
using DotNetG2P.Portuguese.Rules;

namespace DotNetG2P.Portuguese
{
    /// <summary>
    /// ポルトガル語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    [Preserve]
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
                var pronunciation = ConvertWordWithAllophones(words[i]);
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

            var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
            return PortugueseSyllabifier.Syllabify(normalized);
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
            return BatchConversionHelper.ConvertToArray(texts, ToXSampa);
        }

        /// <summary>複数テキストを一括でIPAに変換する。</summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIPA);
        }

        /// <summary>複数テキストを一括で音素列に変換する。</summary>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPhonemes);
        }

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<PortuguesePhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray<IReadOnlyList<PortuguesePhoneme>>(texts, ToPhonemeList);
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

            var ipaList = new List<string>();
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = ConvertWordWithAllophones(words[i]);
                for (var j = 0; j < pronunciation.PhonemesInternal.Length; j++)
                    ipaList.Add(IpaConverter.ToSymbol(pronunciation.PhonemesInternal[j].Phoneme));
            }

            return PortuguesePuaMapper.ApplyPuaMapping(ipaList.ToArray());
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
        /// 複数テキストを一括で PUA 音素文字列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToPuaStringBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPuaString);
        }

        // =====================================================================
        // Prosody API
        // =====================================================================

        /// <summary>
        /// 入力テキストを IPA 音素配列と韻律情報（A1=0, A2=ストレス音節位置, A3=語の音節数）のペアとして返す。
        /// piper-plus 互換の韻律情報を含む。
        /// </summary>
        public PortugueseProsodyResult ToIpaWithProsody(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return new PortugueseProsodyResult(Array.Empty<string>(), Array.Empty<PortugueseProsodyInfo>());

            var allPhonemes = new List<string>();
            var allProsody = new List<PortugueseProsodyInfo>();

            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = ConvertWordWithAllophones(words[i]);
                if (pronunciation.PhonemesInternal.Length == 0)
                    continue;

                var syllableCount = pronunciation.SyllableOffsetsInternal.Length;
                // ストレス音節位置 (1ベース)。-1 の場合は 0 とする。
                var stressPosition = pronunciation.StressedSyllableIndex >= 0
                    ? pronunciation.StressedSyllableIndex + 1
                    : 0;

                for (var j = 0; j < pronunciation.PhonemesInternal.Length; j++)
                {
                    allPhonemes.Add(IpaConverter.ToSymbol(pronunciation.PhonemesInternal[j].Phoneme));
                    allProsody.Add(new PortugueseProsodyInfo(0, stressPosition, syllableCount));
                }
            }

            return new PortugueseProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素配列と韻律情報のペアへ変換する。
        /// </summary>
        public IReadOnlyList<PortugueseProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
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

                var pronunciation = ConvertWordWithAllophones(words[i]);
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        private PortuguesePronunciation ConvertWordWithAllophones(string word)
        {
            var pronunciation = GraphemeToPhonemeRules.ConvertWord(word, _options.Dialect, _options.EnableExceptionDictionary);

            if (_options.EnableAllophones)
                pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures, _options.Dialect);

            return pronunciation;
        }

        private IReadOnlyList<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            if (_options.EnableTextNormalization)
                return PortugueseNormalizer.Tokenize(text, _options.Dialect);

            // テキスト正規化無効時: 基本的なNFC正規化+小文字化+空白分割
            var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
            return PortugueseNormalizer.TokenizeNormalized(normalized);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(PortugueseG2PEngine));
        }
    }
}
