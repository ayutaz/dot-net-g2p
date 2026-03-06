using System;
using System.Collections.Generic;
using System.Threading;
using DotNetG2P.English.Conversion;
using DotNetG2P.English.LTS;
using DotNetG2P.English.Homograph;
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
        private readonly CmuDictionary _dictionary;
        private readonly EnglishG2POptions _options;
        private int _disposed;

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
            return ProcessPipeline(text, phonemes =>
            {
                var s = FormatPhonemes(phonemes);
                return s.Length > 0 ? s : null;
            });
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
            var wordsArray = words.ToArray();
            var result = new List<EnglishPhoneme>();

            for (var i = 0; i < wordsArray.Length; i++)
            {
                var phonemes = LookupWordWithContext(wordsArray, i);
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

            if (_dictionary.TryLookup(word, out var pronunciations))
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

            return _dictionary.ContainsWord(word);
        }

        // =====================================================================
        // IPA変換API
        // =====================================================================

        /// <summary>
        /// テキストをIPA（国際音声記号）文字列に変換する。
        /// 単語はスペースで区切られる。
        /// 例: "Hello world" → "həˈloʊ wˈɝld"
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>IPA文字列</returns>
        public string ToIPA(string text)
        {
            return ProcessPipeline(text, IpaConverter.Convert);
        }

        /// <summary>
        /// テキストをIPA文字列に変換する（ストレスマークなし）。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>ストレスマークなしのIPA文字列</returns>
        public string ToIPAWithoutStress(string text)
        {
            return ProcessPipeline(text, IpaConverter.ConvertWithoutStress);
        }

        // =====================================================================
        // X-SAMPA変換API
        // =====================================================================

        /// <summary>
        /// テキストをX-SAMPA表記に変換する。
        /// 単語はスペースで区切られ、各単語内の音素もスペース区切り。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>X-SAMPA文字列</returns>
        public string ToXSampa(string text)
        {
            return ProcessPipeline(text, XSampaConverter.Convert);
        }

        /// <summary>
        /// テキストをX-SAMPA表記に変換する（ストレスマークなし）。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>ストレスマークなしのX-SAMPA文字列</returns>
        public string ToXSampaWithoutStress(string text)
        {
            return ProcessPipeline(text, XSampaConverter.ConvertWithoutStress);
        }

        // =====================================================================
        // バッチAPI
        // =====================================================================

        /// <summary>
        /// 複数テキストを一括でARPAbet音素列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するARPAbet音素文字列のリスト</returns>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(ToPhonemes(texts[i]));
            return results;
        }

        /// <summary>
        /// 複数テキストを一括でIPA文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するIPA文字列のリスト</returns>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(ToIPA(texts[i]));
            return results;
        }

        /// <summary>
        /// 複数テキストを一括でX-SAMPA文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するX-SAMPA文字列のリスト</returns>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(ToXSampa(texts[i]));
            return results;
        }

        /// <summary>
        /// 複数テキストを一括で音素リストに変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する音素リストのリスト</returns>
        public IReadOnlyList<IReadOnlyList<EnglishPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<IReadOnlyList<EnglishPhoneme>>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(ToPhonemeList(texts[i]));
            return results;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.CompareExchange(ref _disposed, 1, 0);
        }

        /// <summary>
        /// 共通パイプライン: Normalize→Tokenize→LookupWithContext→Format。
        /// formatterは音素配列を文字列に変換する。nullを返した場合はスキップされる。
        /// </summary>
        private string ProcessPipeline(string text, Func<EnglishPhoneme[], string?> formatter)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (_options.EnableNormalization)
                text = EnglishNormalizer.Normalize(text);

            var words = Tokenize(text);
            var wordsArray = words.ToArray();
            var parts = new List<string>(wordsArray.Length);

            for (var i = 0; i < wordsArray.Length; i++)
            {
                var phonemes = LookupWordWithContext(wordsArray, i);
                if (phonemes != null && phonemes.Length > 0)
                {
                    var formatted = formatter(phonemes);
                    if (formatted != null)
                        parts.Add(formatted);
                }
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// 文脈を考慮して単語の音素列を検索する内部メソッド。
        /// 同綴異音語の場合は前後の単語から品詞を推定し、適切なバリアントを選択する。
        /// </summary>
        private EnglishPhoneme[]? LookupWordWithContext(string[] words, int index)
        {
            var word = words[index];
            if (_dictionary.TryLookup(word, out var pronunciations))
            {
                if (_options.EnableHomographResolution && pronunciations.Length > 1)
                {
                    var variantIndex = HomographResolver.ResolveVariantIndex(words, index);
                    if (variantIndex >= 0 && variantIndex < pronunciations.Length)
                        return pronunciations[variantIndex].PhonemesInternal;
                }
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
                throw new KeyNotFoundException($"辞書に登録されていない単語です: '{word}'");

            // Skip: nullを返す
            return null;
        }

        /// <summary>
        /// 単語の音素列を検索する内部メソッド。
        /// OOV時はLTSフォールバック→オプションに従って処理する。
        /// </summary>
        private EnglishPhoneme[]? LookupWordInternal(string word)
        {
            if (_dictionary.TryLookup(word, out var pronunciations))
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
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(EnglishG2PEngine));
        }
    }
}
