using System;
using System.Collections.Generic;
using DotNetG2P.English.LTS;
using DotNetG2P.English.Normalization;

namespace DotNetG2P.English
{
    /// <summary>
    /// 英語G2P（Grapheme-to-Phoneme）エンジン。
    /// CMU Pronouncing Dictionaryを使用して英語テキストをARPAbet音素列に変換する。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフです。辞書はコンストラクタで読み込まれ、以後は読み取り専用です。
    /// </remarks>
    public sealed class EnglishG2PEngine : IDisposable
    {
        private CmuDictionary? _dictionary;
        private readonly EnglishG2POptions _options;
        private bool _disposed;

        /// <summary>
        /// 埋め込みCMU辞書を使用してエンジンを初期化する。
        /// </summary>
        public EnglishG2PEngine()
            : this(CmuDictionary.LoadEmbedded(), EnglishG2POptions.Default)
        {
        }

        /// <summary>
        /// 埋め込みCMU辞書とオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="options">処理オプション</param>
        public EnglishG2PEngine(EnglishG2POptions options)
            : this(CmuDictionary.LoadEmbedded(), options)
        {
        }

        /// <summary>
        /// 外部辞書ファイルを使用してエンジンを初期化する。
        /// </summary>
        /// <param name="dictPath">CMU辞書ファイルパス</param>
        public EnglishG2PEngine(string dictPath)
            : this(CmuDictionary.LoadFromFile(dictPath), EnglishG2POptions.Default)
        {
        }

        /// <summary>
        /// 外部辞書ファイルとオプションを指定してエンジンを初期化する。
        /// </summary>
        /// <param name="dictPath">CMU辞書ファイルパス</param>
        /// <param name="options">処理オプション</param>
        public EnglishG2PEngine(string dictPath, EnglishG2POptions options)
            : this(CmuDictionary.LoadFromFile(dictPath), options)
        {
        }

        /// <summary>
        /// CmuDictionaryインスタンスとオプションを指定してエンジンを初期化する（内部用）。
        /// </summary>
        internal EnglishG2PEngine(CmuDictionary dictionary, EnglishG2POptions options)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// テキストをARPAbet音素文字列に変換する。
        /// 単語をスペースで区切り、句読点を除去して辞書検索を行う。
        /// 例: "Hello world" → "HH AH0 L OW1 W ER1 L D"
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>スペース区切りのARPAbet音素文字列</returns>
        public string ToPhonemes(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (_options.EnableNormalization)
                text = EnglishNormalizer.Normalize(text);

            var words = Tokenize(text);
            var parts = new List<string>(words.Count);

            foreach (var word in words)
            {
                var phonemes = LookupWordInternal(word);
                if (phonemes == null)
                    continue;

                var phonemeStr = FormatPhonemes(phonemes);
                if (phonemeStr.Length > 0)
                {
                    parts.Add(phonemeStr);
                }
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// テキストを <see cref="EnglishPhoneme"/> のリストに変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>音素リスト</returns>
        public IReadOnlyList<EnglishPhoneme> ToPhonemeList(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<EnglishPhoneme>();

            if (_options.EnableNormalization)
                text = EnglishNormalizer.Normalize(text);

            var words = Tokenize(text);
            var result = new List<EnglishPhoneme>();

            foreach (var word in words)
            {
                var phonemes = LookupWordInternal(word);
                if (phonemes != null)
                {
                    result.AddRange(phonemes);
                }
            }

            return result;
        }

        /// <summary>
        /// 単一単語の発音を検索する（最初のバリアントを返す）。
        /// </summary>
        /// <param name="word">単語</param>
        /// <returns>音素リスト</returns>
        /// <exception cref="KeyNotFoundException">辞書に単語が存在せず、UnknownWordHandling=Throwの場合</exception>
        public IReadOnlyList<EnglishPhoneme> LookupWord(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(word))
                return Array.Empty<EnglishPhoneme>();

            var phonemes = LookupWordInternal(word);
            if (phonemes != null)
                return phonemes;

            return Array.Empty<EnglishPhoneme>();
        }

        /// <summary>
        /// 単一単語の全発音バリアントを検索する。
        /// </summary>
        /// <param name="word">単語</param>
        /// <returns>発音バリアント配列。見つからない場合は空配列。</returns>
        public IReadOnlyList<EnglishPronunciation> LookupAllPronunciations(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(word))
                return Array.Empty<EnglishPronunciation>();

            if (_dictionary!.TryLookup(word, out var pronunciations))
                return pronunciations;

            return Array.Empty<EnglishPronunciation>();
        }

        /// <summary>
        /// 単語が辞書に登録されているかを返す。
        /// </summary>
        /// <param name="word">単語（大文字小文字不問）</param>
        /// <returns>登録されている場合 true</returns>
        public bool ContainsWord(string word)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(word))
                return false;

            return _dictionary!.ContainsWord(word);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _dictionary = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 単語の音素列を検索する内部メソッド。
        /// OOV時はLTSフォールバック→オプションに従って処理する。
        /// </summary>
        private EnglishPhoneme[]? LookupWordInternal(string word)
        {
            if (_dictionary!.TryLookup(word, out var pronunciations))
            {
                // 最初のバリアントを使用
                return pronunciations[0].PhonemesInternal;
            }

            // LTSフォールバック
            if (_options.EnableLts)
            {
                var ltsResult = LtsEngine.Predict(word);
                if (ltsResult != null && ltsResult.Length > 0)
                    return ltsResult;
            }

            // OOV処理
            if (_options.UnknownWordHandling == UnknownWordStrategy.Throw)
            {
                throw new KeyNotFoundException($"辞書に登録されていない単語です: '{word}'");
            }

            // Skip: nullを返す
            return null;
        }

        /// <summary>
        /// 音素列をスペース区切り文字列にフォーマットする。
        /// </summary>
        private string FormatPhonemes(EnglishPhoneme[] phonemes)
        {
            if (phonemes.Length == 0)
                return "";

            var parts = new string[phonemes.Length];
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (_options.IncludeStress)
                {
                    parts[i] = phonemes[i].ToString();
                }
                else
                {
                    // ストレスなしの場合は音素名のみ
                    parts[i] = ArpabetParser.PhonemeToString(phonemes[i].Phoneme);
                }
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// テキストを単語リストにトークン化する。
        /// 句読点を除去し、アルファベットとアポストロフィのみの単語を抽出する。
        /// </summary>
        private static List<string> Tokenize(string text)
        {
            var words = new List<string>();
            var start = -1;

            for (var i = 0; i <= text.Length; i++)
            {
                var isWordChar = i < text.Length && IsWordChar(text[i]);

                if (isWordChar)
                {
                    if (start < 0)
                        start = i;
                }
                else
                {
                    if (start >= 0)
                    {
                        var word = text.Substring(start, i - start);
                        // 先頭・末尾のアポストロフィをトリム、末尾のピリオドをトリム
                        word = word.Trim('\'', '\u2019');
                        word = word.TrimEnd('.');
                        if (word.Length > 0)
                        {
                            words.Add(word);
                        }
                        start = -1;
                    }
                }
            }

            return words;
        }

        /// <summary>
        /// 単語を構成する文字かどうかを返す（英字・アポストロフィ・スマートクォート・ピリオド）。
        /// </summary>
        private static bool IsWordChar(char c)
        {
            return (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || c == '\''
                || c == '\u2019'
                || c == '.';
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnglishG2PEngine));
        }
    }
}
