using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using DotNetG2P.Internal;
using DotNetG2P.English.Conversion;
using DotNetG2P.English.LTS;
using DotNetG2P.English.Homograph;
using DotNetG2P.English.Normalization;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace DotNetG2P.English
{
    /// <summary>
    /// 英語G2P（Grapheme-to-Phoneme）エンジン。
    /// CMU Pronouncing Dictionaryを使用して英語テキストをARPAbet音素列に変換する。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフです。辞書はコンストラクタで読み込まれ、以後は読み取り専用です。
    /// </remarks>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
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
        /// ストリームから辞書を読み込んで初期化する（Unity StreamingAssets対応）。
        /// </summary>
        /// <param name="dictionaryStream">CMU辞書ストリーム</param>
        /// <param name="options">処理オプション</param>
        public EnglishG2PEngine(Stream dictionaryStream, EnglishG2POptions options)
            : this(CmuDictionary.LoadFromStream(dictionaryStream), options)
        {
        }

        /// <summary>
        /// CmuDictionaryインスタンスとオプションを指定してエンジンを初期化する。
        /// </summary>
        public EnglishG2PEngine(CmuDictionary dictionary, EnglishG2POptions options)
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

            var result = new List<EnglishPhoneme>();
            ProcessPipelineCore(text, phonemes => result.AddRange(phonemes));
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
                return Array.AsReadOnly(phonemes);

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
                return Array.AsReadOnly(pronunciations);

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
            return BatchConversionHelper.ConvertToList(texts, ToPhonemes);
        }

        /// <summary>
        /// 複数テキストを一括でIPA文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するIPA文字列のリスト</returns>
        public IReadOnlyList<string> ToIPABatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToIPA);
        }

        /// <summary>
        /// 複数テキストを一括でX-SAMPA文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応するX-SAMPA文字列のリスト</returns>
        public IReadOnlyList<string> ToXSampaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList(texts, ToXSampa);
        }

        /// <summary>
        /// 複数テキストを一括で音素リストに変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する音素リストのリスト</returns>
        public IReadOnlyList<IReadOnlyList<EnglishPhoneme>> ToPhonemeListBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToList<IReadOnlyList<EnglishPhoneme>>(texts, ToPhonemeList);
        }

        // =====================================================================
        // piper-plus 互換 IPA API
        // =====================================================================

        /// <summary>
        /// piper-plus 互換の IPA 音素配列を返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>IPA 音素文字列の配列</returns>
        public string[] ToPiperIpaPhonemes(string text)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            var normalized = _options.EnableNormalization ? EnglishNormalizer.Normalize(text) : text;
            var words = normalized.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            foreach (var word in words)
            {
                var pron = LookupWordInternal(word);
                if (pron != null)
                {
                    var ipaPhonemes = PiperIpaConverter.Convert(pron, word);
                    result.AddRange(ipaPhonemes);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// piper-plus 互換 IPA 文字列を返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>スペース区切りの IPA 文字列</returns>
        public string ToPiperIpa(string text)
        {
            var phonemes = ToPiperIpaPhonemes(text);
            return string.Join(" ", phonemes);
        }

        /// <summary>
        /// 複数テキストを一括で piper-plus 互換 IPA 文字列に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する IPA 文字列のリスト</returns>
        public IReadOnlyList<string> ToPiperIpaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPiperIpa);
        }

        // =====================================================================
        // PUA API
        // =====================================================================

        /// <summary>
        /// piper-plus 互換 IPA 音素を PUA マッピングした配列を返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>PUA マッピング済み音素文字列の配列</returns>
        public string[] ToPuaPhonemes(string text)
        {
            ThrowIfDisposed();
            var ipaPhonemes = ToPiperIpaPhonemes(text);
            return EnglishPuaMapper.ApplyPuaMapping(ipaPhonemes);
        }

        /// <summary>
        /// PUA マッピング済み音素をセパレータ区切りで返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>スペース区切りの PUA マッピング済み音素文字列</returns>
        public string ToPuaString(string text)
        {
            var phonemes = ToPuaPhonemes(text);
            return phonemes.Length == 0 ? string.Empty : string.Join(" ", phonemes);
        }

        /// <summary>
        /// 複数テキストを一括で PUA 変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する PUA 文字列のリスト</returns>
        public IReadOnlyList<string> ToPuaStringBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToPuaString);
        }

        // =====================================================================
        // Prosody API
        // =====================================================================

        /// <summary>
        /// IPA 音素配列と韻律情報を返す。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>韻律情報付き IPA 音素結果</returns>
        public EnglishProsodyResult ToIpaWithProsody(string text)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(text))
                return new EnglishProsodyResult(Array.Empty<string>(), Array.Empty<EnglishProsodyInfo>());

            var normalized = _options.EnableNormalization ? EnglishNormalizer.Normalize(text) : text;
            var words = normalized.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var allPhonemes = new List<string>();
            var allProsody = new List<EnglishProsodyInfo>();

            foreach (var word in words)
            {
                var pron = LookupWordInternal(word);
                if (pron == null) continue;
                var ipaPhonemes = PiperIpaConverter.Convert(pron, word);
                var stressIndex = FindPrimaryStressIndex(pron);
                for (int i = 0; i < ipaPhonemes.Length; i++)
                {
                    allPhonemes.Add(ipaPhonemes[i]);
                    allProsody.Add(new EnglishProsodyInfo(0, stressIndex, ipaPhonemes.Length));
                }
            }

            return new EnglishProsodyResult(allPhonemes.ToArray(), allProsody.ToArray());
        }

        /// <summary>
        /// 複数テキストを一括で韻律情報付き IPA に変換する。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>各テキストに対応する韻律情報付き結果のリスト</returns>
        public IReadOnlyList<EnglishProsodyResult> ToIpaWithProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            return BatchConversionHelper.ConvertToArray(texts, ToIpaWithProsody);
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

            var parts = new List<string>();
            ProcessPipelineCore(text, phonemes =>
            {
                var formatted = formatter(phonemes);
                if (formatted != null)
                    parts.Add(formatted);
            });

            return string.Join(" ", parts);
        }

        /// <summary>
        /// パイプラインの共通コア処理: Normalize→Tokenize→LookupWithContext。
        /// 各単語の音素配列をactionに渡す。
        /// </summary>
        private void ProcessPipelineCore(string text, Action<EnglishPhoneme[]> action)
        {
            if (_options.EnableNormalization)
                text = EnglishNormalizer.Normalize(text);

            var wordsArray = Tokenize(text);

            for (var i = 0; i < wordsArray.Length; i++)
            {
                var phonemes = LookupWordWithContext(wordsArray, i);
                if (phonemes != null && phonemes.Length > 0)
                {
                    action(phonemes);
                }
            }
        }

        /// <summary>
        /// 文脈を考慮して単語の音素列を検索する内部メソッド。
        /// 同綴異音語の場合は前後の単語から品詞を推定し、適切なバリアントを選択する。
        /// 辞書に未登録の場合はLTSフォールバックを使用する。
        /// LTS予測結果にはSecondary stress (2) が含まれない点に注意
        /// （Flite LTSモデルはPrimary stress (1) と No stress (0) のみ出力）。
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
        /// LTSフォールバック時はSecondary stress (2) が出力されない。
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

            var sb = new StringBuilder();
            for (var i = 0; i < phonemes.Length; i++)
            {
                if (i > 0) sb.Append(' ');

                if (_options.IncludeStress)
                {
                    sb.Append(phonemes[i].ToString());
                }
                else
                {
                    sb.Append(ArpabetParser.PhonemeToString(phonemes[i].Phoneme));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// テキストを単語配列にトークン化する。
        /// 句読点を除去し、アルファベットとアポストロフィのみの単語を抽出する。
        /// </summary>
        private static string[] Tokenize(string text)
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

            return words.ToArray();
        }

        /// <summary>
        /// 単語を構成する文字かどうかを返す（英字・アポストロフィ・スマートクォート・ピリオド）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        /// <summary>
        /// 音素配列から Primary stress の位置（0始まり）を返す。
        /// Primary stress が見つからない場合は -1 を返す。
        /// </summary>
        private static int FindPrimaryStressIndex(EnglishPhoneme[] phonemes)
        {
            for (int i = 0; i < phonemes.Length; i++)
            {
                if (phonemes[i].Stress == Stress.Primary)
                    return i;
            }
            return -1;
        }
    }
}
