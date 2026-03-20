#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.French;
using DotNetG2P.Korean;
using DotNetG2P.MeCab;
using DotNetG2P.Multilingual.Internal;
using DotNetG2P.Portuguese;
using DotNetG2P.Spanish;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// 多言語混在テキスト対応の多言語G2Pエンジン。
    /// テキストを言語セグメントに自動分割し、各言語のG2Pエンジンで変換する。
    /// </summary>
    /// <remarks>
    /// 日本語エンジンは辞書パスが必要なため即時初期化される。
    /// その他の言語エンジン（英語・中国語・韓国語・スペイン語・フランス語・ポルトガル語）は
    /// 遅延初期化され、初回アクセス時にのみ生成される。
    /// 日本語エンジンはスレッドセーフでないためlockで保護。
    /// </remarks>
    public sealed class MultilingualG2PEngine : IDisposable
    {
        private readonly G2PEngine _japaneseEngine;
        private readonly Lazy<EnglishG2PEngine> _lazyEnglishEngine;
        private readonly Lazy<ChineseG2PEngine> _lazyChineseEngine;
        private readonly Lazy<KoreanG2PEngine> _lazyKoreanEngine;
        private readonly Lazy<SpanishG2PEngine> _lazySpanishEngine;
        private readonly Lazy<FrenchG2PEngine> _lazyFrenchEngine;
        private readonly Lazy<PortugueseG2PEngine> _lazyPortugueseEngine;
        private readonly MultilingualG2POptions _options;
        private readonly LanguageCapabilityRouter _capabilityRouter;
        private readonly object _japaneseLock = new object();
        private int _disposed;

        /// <summary>
        /// 既定の辞書検索パスを用いて多言語G2Pエンジンを初期化する。
        /// </summary>
        public MultilingualG2PEngine()
            : this(NaistJdicLocator.ResolveOrThrow(), MultilingualG2POptions.Default)
        {
        }

        /// <summary>
        /// 既定の辞書検索パスとオプションを用いて多言語G2Pエンジンを初期化する。
        /// </summary>
        public MultilingualG2PEngine(MultilingualG2POptions options)
            : this(NaistJdicLocator.ResolveOrThrow(), options)
        {
        }

        /// <summary>
        /// 日本語辞書パスを指定して多言語G2Pエンジンを初期化する（デフォルトオプション）。
        /// </summary>
        /// <param name="japaneseDictPath">naist-jdic辞書ディレクトリのパス</param>
        /// <exception cref="DirectoryNotFoundException">辞書パスが存在しない場合</exception>
        public MultilingualG2PEngine(string japaneseDictPath)
            : this(japaneseDictPath, MultilingualG2POptions.Default)
        {
        }

        /// <summary>
        /// 日本語辞書パスとオプションを指定して多言語G2Pエンジンを初期化する。
        /// </summary>
        /// <param name="japaneseDictPath">naist-jdic辞書ディレクトリのパス</param>
        /// <param name="options">多言語G2Pオプション</param>
        /// <exception cref="DirectoryNotFoundException">辞書パスが存在しない場合</exception>
        /// <exception cref="ArgumentNullException">引数がnullの場合</exception>
        public MultilingualG2PEngine(string japaneseDictPath, MultilingualG2POptions options)
        {
            if (japaneseDictPath == null)
                throw new ArgumentNullException(nameof(japaneseDictPath));
            if (!Directory.Exists(japaneseDictPath))
                throw new DirectoryNotFoundException($"辞書ディレクトリが見つかりません: {japaneseDictPath}");

            _options = options ?? throw new ArgumentNullException(nameof(options));

            // 日本語エンジンは辞書パスが必要なため即時初期化
            _japaneseEngine = new G2PEngine(
                new MeCabTokenizer(japaneseDictPath),
                options.JapaneseOptions ?? G2POptions.Default);

            // その他の言語エンジンは遅延初期化（Lazy<T>はデフォルトでスレッドセーフ）
            _lazyEnglishEngine = new Lazy<EnglishG2PEngine>(() =>
            {
                var opts = options.EnglishOptions ?? EnglishG2POptions.Default;
                if (options.EnglishDictionaryPath != null && options.EnglishLtsModelPath != null)
                    return new EnglishG2PEngine(options.EnglishDictionaryPath, options.EnglishLtsModelPath, opts);
                if (options.EnglishDictionaryPath != null)
                    return new EnglishG2PEngine(options.EnglishDictionaryPath, opts);
                return new EnglishG2PEngine(opts);
            });

            _lazyChineseEngine = new Lazy<ChineseG2PEngine>(() =>
            {
                if (options.ChineseCharDictionaryPath != null)
                {
                    if (options.ChinesePhraseDictionaryPath != null)
                        return new ChineseG2PEngine(options.ChineseCharDictionaryPath, options.ChinesePhraseDictionaryPath, options.ChineseOptions ?? ChineseG2POptions.Default);
                    return new ChineseG2PEngine(options.ChineseCharDictionaryPath, options.ChineseOptions ?? ChineseG2POptions.Default);
                }
                return new ChineseG2PEngine(options.ChineseOptions ?? ChineseG2POptions.Default);
            });

            _lazyKoreanEngine = new Lazy<KoreanG2PEngine>(
                () => new KoreanG2PEngine(options.KoreanOptions ?? KoreanG2POptions.Default));

            _lazySpanishEngine = new Lazy<SpanishG2PEngine>(
                () => new SpanishG2PEngine(options.SpanishOptions ?? SpanishG2POptions.Default));

            _lazyFrenchEngine = new Lazy<FrenchG2PEngine>(
                () => new FrenchG2PEngine(options.FrenchOptions ?? FrenchG2POptions.Default));

            _lazyPortugueseEngine = new Lazy<PortugueseG2PEngine>(
                () => new PortugueseG2PEngine(options.PortugueseOptions ?? PortugueseG2POptions.Default));

            _capabilityRouter = LanguageCapabilityRouter.CreateLazy(
                _japaneseEngine,
                _japaneseLock,
                _lazyEnglishEngine,
                _lazyChineseEngine,
                _lazyKoreanEngine,
                _lazySpanishEngine,
                _lazyFrenchEngine,
                _lazyPortugueseEngine);
        }

        /// <summary>
        /// 多言語混在テキストを音素文字列に変換する。
        /// テキストを自動的に言語セグメントに分割し、各言語のG2Pエンジンで変換後、結合して返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>スペース区切りの音素文字列</returns>
        /// <exception cref="ObjectDisposedException">Dispose済みの場合</exception>
        public string ToPhonemes(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            var segments = TextSegmenter.Segment(text, _options.DefaultCjkLanguage, _options.DefaultLatinLanguage);
            if (segments.Count == 0)
                return "";

            var parts = new List<string>(segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                var phonemes = ConvertSegment(segments[i]);
                if (phonemes.Length > 0)
                    parts.Add(phonemes);
            }

            return string.Join(_options.SegmentSeparator, parts);
        }

        /// <summary>
        /// 多言語混在テキストを言語タグ付きG2Pセグメントのリストとして変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>G2Pセグメントのリスト</returns>
        /// <exception cref="ObjectDisposedException">Dispose済みの場合</exception>
        public IReadOnlyList<G2PSegment> ToSegments(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<G2PSegment>();

            var segments = TextSegmenter.Segment(text, _options.DefaultCjkLanguage, _options.DefaultLatinLanguage);
            if (segments.Count == 0)
                return Array.Empty<G2PSegment>();

            var result = new List<G2PSegment>(segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                var phonemes = ConvertSegment(seg);
                if (!string.IsNullOrEmpty(phonemes))
                    result.Add(new G2PSegment(seg.Language, seg.Text, phonemes));
            }

            return result;
        }

        /// <summary>
        /// 複数テキストを一括で音素文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する音素文字列のリスト</returns>
        /// <exception cref="ArgumentNullException">textsがnullの場合</exception>
        /// <exception cref="ObjectDisposedException">Dispose済みの場合</exception>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return ConvertBatch(texts, ToPhonemes);
        }

        /// <summary>
        /// 複数テキストを一括でG2Pセグメントリストに変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するG2Pセグメントリストのリスト</returns>
        /// <exception cref="ArgumentNullException">textsがnullの場合</exception>
        /// <exception cref="ObjectDisposedException">Dispose済みの場合</exception>
        public IReadOnlyList<IReadOnlyList<G2PSegment>> ToSegmentsBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return ConvertBatch<IReadOnlyList<G2PSegment>>(texts, ToSegments);
        }

        internal ITextBatchProcessor<string> GetTextBatchProcessor(Language language)
        {
            ThrowIfDisposed();
            return _capabilityRouter.GetRequired(language);
        }

        internal bool TryGetIpaTextBatchProcessor(Language language, out IIpaTextBatchProcessor? processor)
        {
            ThrowIfDisposed();
            return _capabilityRouter.TryGetIpa(language, out processor);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _japaneseEngine.Dispose();

            // 遅延初期化されたエンジンは実際に生成された場合のみDispose
            if (_lazyEnglishEngine.IsValueCreated)
                _lazyEnglishEngine.Value.Dispose();
            if (_lazyChineseEngine.IsValueCreated)
                _lazyChineseEngine.Value.Dispose();
            if (_lazyKoreanEngine.IsValueCreated)
                _lazyKoreanEngine.Value.Dispose();
            if (_lazySpanishEngine.IsValueCreated)
                _lazySpanishEngine.Value.Dispose();
            if (_lazyFrenchEngine.IsValueCreated)
                _lazyFrenchEngine.Value.Dispose();
            if (_lazyPortugueseEngine.IsValueCreated)
                _lazyPortugueseEngine.Value.Dispose();
        }

        /// <summary>
        /// セグメントを対応する言語のG2Pエンジンで変換する。
        /// </summary>
        private string ConvertSegment(TextSegment segment)
        {
            return _capabilityRouter.GetRequired(segment.Language).Convert(segment.Text);
        }

        private static List<TResult> ConvertBatch<TResult>(IReadOnlyList<string> texts, Func<string, TResult> converter)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            var results = new List<TResult>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(converter(texts[i]));

            return results;
        }

        /// <summary>
        /// Dispose済みの場合にObjectDisposedExceptionをスローする。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(MultilingualG2PEngine));
        }
    }
}
