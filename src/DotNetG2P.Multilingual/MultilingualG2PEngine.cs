#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DotNetG2P.Chinese;
using DotNetG2P.English;
using DotNetG2P.MeCab;
using DotNetG2P.Spanish;

namespace DotNetG2P.Multilingual
{
    /// <summary>
    /// 日英混在テキスト対応の多言語G2Pエンジン。
    /// テキストを言語セグメントに自動分割し、各言語のG2Pエンジンで変換する。
    /// </summary>
    /// <remarks>
    /// 英語エンジンはスレッドセーフ（共有）。日本語エンジンはスレッドセーフでないためlockで保護。
    /// </remarks>
    public sealed class MultilingualG2PEngine : IDisposable
    {
        private readonly G2PEngine _japaneseEngine;
        private readonly EnglishG2PEngine _englishEngine;
        private readonly ChineseG2PEngine _chineseEngine;
        private readonly SpanishG2PEngine _spanishEngine;
        private readonly MultilingualG2POptions _options;
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

            G2PEngine? japaneseEngine = null;
            EnglishG2PEngine? englishEngine = null;
            ChineseG2PEngine? chineseEngine = null;
            SpanishG2PEngine? spanishEngine = null;
            try
            {
                japaneseEngine = new G2PEngine(
                    new MeCabTokenizer(japaneseDictPath),
                    options.JapaneseOptions ?? G2POptions.Default);

                englishEngine = new EnglishG2PEngine(
                    options.EnglishOptions ?? EnglishG2POptions.Default);

                chineseEngine = new ChineseG2PEngine(
                    options.ChineseOptions ?? ChineseG2POptions.Default);

                spanishEngine = new SpanishG2PEngine(
                    options.SpanishOptions ?? SpanishG2POptions.Default);

                _japaneseEngine = japaneseEngine;
                _englishEngine = englishEngine;
                _chineseEngine = chineseEngine;
                _spanishEngine = spanishEngine;
            }
            catch
            {
                japaneseEngine?.Dispose();
                englishEngine?.Dispose();
                chineseEngine?.Dispose();
                spanishEngine?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 日英混在テキストを音素文字列に変換する。
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
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
                results.Add(ToPhonemes(texts[i]));
            return results;
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
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<IReadOnlyList<G2PSegment>>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
                results.Add(ToSegments(texts[i]));
            return results;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            _japaneseEngine.Dispose();
            _englishEngine.Dispose();
            _chineseEngine.Dispose();
            _spanishEngine.Dispose();
        }

        /// <summary>
        /// セグメントを対応する言語のG2Pエンジンで変換する。
        /// </summary>
        private string ConvertSegment(TextSegment segment)
        {
            switch (segment.Language)
            {
                case Language.Japanese:
                    lock (_japaneseLock)
                    {
                        return _japaneseEngine.ToPhonemes(segment.Text);
                    }

                case Language.English:
                    return _englishEngine.ToPhonemes(segment.Text);

                case Language.Chinese:
                    return _chineseEngine.ToPinyin(segment.Text);

                case Language.Spanish:
                    return _spanishEngine.ToPhonemes(segment.Text);

                default:
                    return "";
            }
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
