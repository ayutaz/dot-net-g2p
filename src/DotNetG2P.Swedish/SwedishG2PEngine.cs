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
            if (words.Count == 0)
                return Array.Empty<SwedishPhoneme>();

            var result = new List<SwedishPhoneme>(words.Count * 6);
            for (var i = 0; i < words.Count; i++)
            {
                var pronunciation = ConvertWordFull(words[i]);
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

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;
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
            if (words.Count == 0)
                return string.Empty;

            var builder = new StringBuilder(text.Length + 8);
            for (var i = 0; i < words.Count; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                var pronunciation = ConvertWordFull(words[i]);
                builder.Append(formatter(pronunciation));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 単語に対してG2Pフルパイプラインを実行し、音節分割・ストレス付きのSwedishPronunciationを返す。
        /// 1. GraphemeToPhonemeRules.ConvertWord で音素列を取得
        /// 2. SwedishSyllabifier.Syllabify で正書法ベースの音節分割
        /// 3. StressAssigner.MarkStress でストレス位置決定
        /// 4. 音節ごとの音素数を算出して SyllableOffsets を構築
        /// 5. 完成した SwedishPronunciation を返す
        /// </summary>
        private static SwedishPronunciation ConvertWordFull(string word)
        {
            // Phase 1: G2Pルールで全体の音素列を取得
            var rawPronunciation = GraphemeToPhonemeRules.ConvertWord(word);
            var phonemes = rawPronunciation.PhonemesInternal;

            if (phonemes.Length == 0)
                return rawPronunciation;

            // Phase 2-3: 正書法ベースの音節分割 + ストレス付与
            var lower = word.ToLowerInvariant();
            var syllables = StressAssigner.MarkStress(lower, SwedishSyllabifier.Syllabify(lower));

            if (syllables.Count == 0)
                return rawPronunciation;

            // Phase 4: 各音節の音素数を算出して SyllableOffsets を構築
            // 各音節テキストを個別にConvertWordして音素数を数え、累積オフセットを計算する。
            var syllableOffsets = new int[syllables.Count];
            var stressedIndex = -1;
            var offset = 0;

            for (var i = 0; i < syllables.Count; i++)
            {
                syllableOffsets[i] = offset;

                if (syllables[i].IsStressed)
                    stressedIndex = i;

                // 音節テキストを個別変換して音素数を取得
                var syllablePronunciation = GraphemeToPhonemeRules.ConvertWord(syllables[i].Text);
                offset += syllablePronunciation.PhonemesInternal.Length;
            }

            // 個別変換の合計と全体変換の音素数が異なる場合
            // （そり舌化・語末g黙字は全体変換のみで適用されるため差異が出うる）
            // → オフセット末尾を全体音素数に収まるよう補正
            if (offset != phonemes.Length)
            {
                // 差分を最後の音節で吸収（そり舌化でrが消えた分などの調整）
                // オフセットが音素配列を超えないよう各値をクランプ
                for (var i = 0; i < syllableOffsets.Length; i++)
                {
                    if (syllableOffsets[i] > phonemes.Length)
                        syllableOffsets[i] = phonemes.Length;
                }
            }

            return new SwedishPronunciation(phonemes, syllableOffsets, stressedIndex);
        }

        /// <summary>テキストを小文字化し、空白で分割してトークンリストを返す。</summary>
        private static IReadOnlyList<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var lower = text.ToLowerInvariant();
            var parts = lower.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(SwedishG2PEngine));
        }
    }
}
