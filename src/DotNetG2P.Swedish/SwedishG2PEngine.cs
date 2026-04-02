using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using DotNetG2P.Swedish.Conversion;
using DotNetG2P.Swedish.Data;
using DotNetG2P.Swedish.Normalization;
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

        /// <summary>入力テキストをX-SAMPA表記に変換する。</summary>
        public string ToXSampa(string text)
        {
            return ProcessText(text, pronunciation =>
                XSampaConverter.Convert(pronunciation, _options.IncludeStress));
        }

        /// <summary>入力テキストをストレスなしX-SAMPA表記に変換する。</summary>
        public string ToXSampaWithoutStress(string text)
        {
            return ProcessText(text, pronunciation =>
                XSampaConverter.Convert(pronunciation, includeStress: false));
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
                var pronunciation = ConvertWord(words[i]);
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

            var lower = word.Normalize(NormalizationForm.FormC).ToLowerInvariant();
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

        /// <summary>複数テキストをX-SAMPA表記に一括変換する。</summary>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToXSampa);
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

                var pronunciation = ConvertWord(words[i]);
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 単語をG2P変換する。例外辞書が有効な場合は辞書を優先し、ヒットしなければルールベースG2Pにフォールバックする。
        /// </summary>
        private SwedishPronunciation ConvertWord(string word)
        {
            SwedishPronunciation pronunciation;

            // 例外辞書が有効な場合、辞書を先に検索
            if (_options.EnableExceptionDictionary &&
                SwedishExceptionDictionary.TryLookup(word, _options.Dialect, out var dictPron))
            {
                pronunciation = dictPron;
            }
            else
            {
                // ルールベースG2Pにフォールバック
                pronunciation = GraphemeToPhonemeRules.ConvertWord(word, _options.Dialect);
            }

            // 異音処理（EnableAllophones有効時）
            if (_options.EnableAllophones)
            {
                pronunciation = AllophoneProcessor.Apply(pronunciation, _options.AllophoneFeatures, _options.Dialect);
            }

            // FinlandSwedish方言: ピッチアクセント無効化（共有インスタンスを破壊しないよう新規生成）
            if (_options.Dialect == SwedishDialect.FinlandSwedish && pronunciation.Accent != 0)
            {
                pronunciation = new SwedishPronunciation(
                    pronunciation.PhonemesInternal, pronunciation.SyllableOffsetsInternal,
                    pronunciation.StressedSyllableIndex, accent: 0);
            }

            // 機能語のストレス除去（出力フォーマット設定に関わらず、音韻モデルからストレスを除去する）
            if (FunctionWordList.Contains(word))
            {
                pronunciation = pronunciation.WithoutStress();
            }

            return pronunciation;
        }

        // =================================================================
        // PUA API
        // =================================================================

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素配列として返す。
        /// 多文字 IPA 音素を PUA 単一文字に置換した形式。
        /// </summary>
        public string[] ToPuaPhonemes(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>();
            for (var i = 0; i < words.Length; i++)
            {
                var pron = ConvertWord(words[i]);
                foreach (var p in pron.PhonemesInternal)
                {
                    var ipa = IpaConverter.ToSymbol(p.Phoneme);
                    result.Add(SwedishPuaMapper.MapToPua(ipa));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 入力テキストを piper-plus 互換 PUA 音素文字列として返す。
        /// </summary>
        public string ToPuaString(string text)
        {
            var phonemes = ToPuaPhonemes(text);
            return string.Join(" ", phonemes);
        }

        /// <summary>
        /// 複数テキストを一括で PUA 音素文字列へ変換する。
        /// </summary>
        public IReadOnlyList<string> ToPuaStringBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPuaString);
        }

        // =================================================================
        // Prosody API
        // =================================================================

        /// <summary>
        /// 入力テキストを IPA 音素配列と韻律情報（a1, a2, a3）のペアとして返す。
        /// piper-plus 互換の韻律情報を含む。
        /// </summary>
        public SwedishProsodyResult ToIpaWithProsody(string text)
        {
            ThrowIfDisposed();

            var words = GetWords(text);
            if (words.Length == 0)
                return new SwedishProsodyResult(Array.Empty<string>(), Array.Empty<SwedishProsodyInfo>());

            var allPhonemes = new List<string>();
            var allProsody = new List<SwedishProsodyInfo>();

            for (var i = 0; i < words.Length; i++)
            {
                var pron = ConvertWord(words[i]);
                var syllableCount = pron.SyllableOffsetsInternal.Length;
                var stressLevel = pron.StressedSyllableIndex >= 0 ? 1 : 0;

                var info = new SwedishProsodyInfo(pron.Accent, stressLevel, syllableCount);

                foreach (var p in pron.PhonemesInternal)
                {
                    allPhonemes.Add(IpaConverter.ToSymbol(p.Phoneme));
                    allProsody.Add(info);
                }
            }

            return new SwedishProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        /// <summary>
        /// 複数テキストを一括で IPA 音素配列と韻律情報のペアへ変換する。
        /// </summary>
        public IReadOnlyList<SwedishProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
        }

        /// <summary>テキストをトークン化する。正規化が有効な場合は11段階パイプラインを適用する。</summary>
        private string[] GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            if (_options.EnableTextNormalization)
                return SwedishNormalizer.Tokenize(text);

            // NFC正規化 + 小文字化のみ（正規化パイプライン無効時）
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
