using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using DotNetG2P.French.Conversion;
using DotNetG2P.French.Normalization;
using DotNetG2P.French.Rules;
using UnityEngine.Scripting;

namespace DotNetG2P.French
{
    /// <summary>
    /// フランス語G2P（Grapheme-to-Phoneme）エンジン。
    /// </summary>
    [Preserve]
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
            return BatchConversionHelper.ConvertToArray(texts, ToPhonemes);
        }

        /// <summary>複数テキストを一括でIPAに変換する。</summary>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIPA);
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

        /// <summary>複数テキストを一括で音素リストに変換する。</summary>
        public IReadOnlyList<IReadOnlyList<FrenchPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray<IReadOnlyList<FrenchPhoneme>>(texts, ToPhonemeList);
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
            var ipaPhonemes = ToIpaPhonemeArray(text);
            return FrenchPuaMapper.ApplyPuaMapping(ipaPhonemes);
        }

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素文字列として返す。
        /// </summary>
        public string ToPuaString(string text)
        {
            var puaPhonemes = ToPuaPhonemes(text);
            if (puaPhonemes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(puaPhonemes.Length * 2);
            for (var i = 0; i < puaPhonemes.Length; i++)
            {
                if (i > 0)
                    builder.Append(_options.Separator);

                builder.Append(puaPhonemes[i]);
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
        /// テキストの IPA 音素と韻律情報（A1=0固定、A2=語内音節位置、A3=語の音節数）を返す。
        /// フランス語はストレスが語末固定のため A1 は常に 0。
        /// </summary>
        public FrenchProsodyResult ToIpaWithProsody(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Count == 0)
                return new FrenchProsodyResult(Array.Empty<string>(), Array.Empty<FrenchProsodyInfo>());

            var allPhonemes = new List<string>();
            var allProsody = new List<FrenchProsodyInfo>();

            for (var w = 0; w < words.Count; w++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[w], _options.Dialect, _options.EnableExceptionDictionary);
                if (_options.EnableAllophones)
                    pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);

                // 音節数を算出
                var syllableCount = pronunciation.SyllableOffsetsInternal.Length;
                if (syllableCount == 0)
                    syllableCount = 1;

                // 音節ごとに音素を収集し韻律情報を付与
                for (var s = 0; s < pronunciation.SyllableOffsetsInternal.Length; s++)
                {
                    var start = pronunciation.SyllableOffsetsInternal[s];
                    var end = s + 1 < pronunciation.SyllableOffsetsInternal.Length
                        ? pronunciation.SyllableOffsetsInternal[s + 1]
                        : pronunciation.PhonemesInternal.Length;

                    var syllablePosition = s + 1; // 1ベース

                    for (var i = start; i < end; i++)
                    {
                        allPhonemes.Add(IpaConverter.ToSymbol(pronunciation.PhonemesInternal[i].Phoneme));
                        allProsody.Add(new FrenchProsodyInfo(0, syllablePosition, syllableCount));
                    }
                }
            }

            return new FrenchProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素配列と韻律情報のペアへ変換する。
        /// </summary>
        public IReadOnlyList<FrenchProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
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

            return FrenchNormalizer.TokenizeNormalized(Normalize(text));
        }

        private string Normalize(string text)
        {
            if (_options.EnableTextNormalization)
                return FrenchNormalizer.Normalize(text);

            return text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        /// <summary>テキスト全体のIPA音素を文字列配列として返す内部ヘルパー。</summary>
        private string[] ToIpaPhonemeArray(string text)
        {
            var words = GetWords(text);
            if (words.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>();
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = GraphemeToPhonemeRules.ConvertWord(words[i], _options.Dialect, _options.EnableExceptionDictionary);
                if (_options.EnableAllophones)
                    pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures);

                for (var j = 0; j < pronunciation.PhonemesInternal.Length; j++)
                    result.Add(IpaConverter.ToSymbol(pronunciation.PhonemesInternal[j].Phoneme));
            }

            return result.ToArray();
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
