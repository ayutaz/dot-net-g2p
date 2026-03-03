using System;
using System.Collections.Generic;
using DotNetG2P.Internal;
using DotNetG2P.JPCommon;
using DotNetG2P.Models;
using DotNetG2P.NJD;
using DotNetG2P.PhonemeConverter;
using DotNetG2P.TextNormalization;

namespace DotNetG2P
{
    /// <summary>
    /// 日本語G2P（Grapheme-to-Phoneme）エンジン。
    /// テキストを形態素解析し、NJD処理パイプラインを経て音素列を出力する。
    ///
    /// パイプライン実行順序（OpenJTalk準拠）:
    ///   テキスト
    ///     → TextNormalizer.Normalize()     ← テキスト正規化
    ///     → ITokenizer.Tokenize()          ← 形態素解析
    ///     → NjdNode.FromTokens()           ← NjdNode構築
    ///     → SetPronunciation.Process()     ← 1. 発音生成
    ///     → DigitSequenceProcessor.Process()← 2a. 数字列処理
    ///     → SetDigit.Process()             ← 2b. 数字発音補正
    ///     → SetAccentPhrase.Process()      ← 3. アクセント句結合
    ///     → SetAccentType.Process()        ← 4. アクセント結合型
    ///     → SetUnvoicedVowel.Process()     ← 5. 無声音化
    /// </summary>
    public sealed class G2PEngine : IDisposable
    {
        private readonly ITokenizer _tokenizer;
        private readonly G2POptions _options;
        private bool _disposed;

        /// <summary>
        /// G2PEngineを初期化する（デフォルトオプション）。
        /// </summary>
        /// <param name="tokenizer">使用する形態素解析器</param>
        public G2PEngine(ITokenizer tokenizer)
            : this(tokenizer, G2POptions.Default)
        {
        }

        /// <summary>
        /// G2PEngineをオプション指定で初期化する。
        /// </summary>
        /// <param name="tokenizer">使用する形態素解析器</param>
        /// <param name="options">処理オプション</param>
        public G2PEngine(ITokenizer tokenizer, G2POptions options)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// NJDパイプラインを実行してNjdNodeリストを返す（内部共通処理）。
        /// </summary>
        private List<NjdNode> RunPipeline(string text)
        {
            // 1. テキスト正規化
            if (_options.EnableTextNormalization)
            {
                text = TextNormalizer.Normalize(text);
            }

            // 2. 形態素解析
            var tokens = _tokenizer.Tokenize(text);

            // 3. NjdNodeリスト構築
            var nodes = NjdNode.FromTokens(tokens);

            // 4. 発音設定（NJD処理第1段階）
            SetPronunciation.Process(nodes);

            // 5. 数字列処理（NJD処理第2段階a: 数字列の検出・変換）
            if (_options.EnableDigitProcessing)
            {
                DigitSequenceProcessor.Process(nodes);
            }

            // 5b. 数字発音補正（NJD処理第2段階b: 助数詞音便・日付特殊読み等）
            if (_options.EnableDigitProcessing)
            {
                SetDigit.Process(nodes);
            }

            // 6. アクセント句結合（NJD処理第3段階）
            if (_options.EnableAccentPhrase)
            {
                SetAccentPhrase.Process(nodes);
            }

            // 7. アクセント結合型（NJD処理第4段階）
            if (_options.EnableAccentType)
            {
                SetAccentType.Process(nodes);
            }

            // 8. 無声音化（NJD処理第5段階）
            if (_options.EnableUnvoicedVowel)
            {
                SetUnvoicedVowel.Process(nodes);
            }

            return nodes;
        }

        /// <summary>
        /// テキストを音素列に変換する。
        /// 例: "こんにちは" → "k o N n i ch i w a"
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>スペース区切りの音素文字列</returns>
        public string ToPhonemes(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return "";

            var nodes = RunPipeline(text);

            // 各ノードの発音を音素文字列に変換して結合
            var parts = new List<string>(nodes.Count);
            foreach (var node in nodes)
            {
                if (node.Pronunciation != null && node.Pronunciation.MoraCount > 0)
                {
                    var phonemes = MoraMapping.MorasToPhonemeString(node.Pronunciation.Moras, _options.ExpandLongVowels);
                    if (!string.IsNullOrEmpty(phonemes))
                    {
                        parts.Add(phonemes);
                    }
                }
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// テキストをカタカナ読みに変換する。
        /// 例: "今日は天気です" → "キョウワテンキデス"
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>カタカナ文字列</returns>
        public string ToKana(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return "";

            var nodes = RunPipeline(text);

            var sb = new ValueStringBuilder(nodes.Count * 4);
            foreach (var node in nodes)
            {
                if (node.Pronunciation != null)
                {
                    sb.Append(node.Pronunciation.ToKatakana());
                }
            }

            return sb.ToStringAndDispose();
        }

        /// <summary>
        /// テキストをESPnet韻律記号付き文字列に変換する。
        /// 例: "こんにちは" → "^ k o [ N n i ch i w a $"
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>ESPnet韻律記号付き音素文字列</returns>
        public string ToProsody(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return "";

            var nodes = RunPipeline(text);
            return ProsodyExtractor.Extract(nodes, _options.ExpandLongVowels);
        }

        /// <summary>
        /// テキストをVOICEVOX互換のAccentPhraseリストに変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>AccentPhraseのリスト</returns>
        public IReadOnlyList<AccentPhrase> ToAccentPhrases(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return Array.Empty<AccentPhrase>();

            var nodes = RunPipeline(text);
            return AccentPhraseConverter.Convert(nodes);
        }

        /// <summary>
        /// テキストをHTSフルコンテキストラベル列に変換する。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>フルコンテキストラベル文字列のリスト</returns>
        public IReadOnlyList<string> ToFullContextLabels(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

            var nodes = RunPipeline(text);
            var utterance = JPCommonBuilder.Build(nodes);
            return FullContextLabel.Generate(utterance);
        }

        /// <summary>
        /// テキストをNJDパイプライン処理後のNjdNodeリストとして返す。
        /// NJD処理の中間結果を取得するための高度なAPI。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>NJD処理済みのNjdNodeリスト</returns>
        public IReadOnlyList<NjdNode> Analyze(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text)) return Array.Empty<NjdNode>();

            return RunPipeline(text);
        }

        /// <summary>
        /// 複数テキストをバッチ処理で音素列に変換する。
        /// 形態素解析器の内部バッファ（ラティス等）が文間で再利用されるため、個別呼び出しよりGC効率が良い。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>スペース区切りの音素文字列のリスト</returns>
        public IReadOnlyList<string> ToPhonemesBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
            {
                results.Add(ToPhonemes(texts[i]));
            }
            return results;
        }

        /// <summary>
        /// 複数テキストをバッチ処理でカタカナ読みに変換する。
        /// 形態素解析器の内部バッファ（ラティス等）が文間で再利用されるため、個別呼び出しよりGC効率が良い。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>カタカナ文字列のリスト</returns>
        public IReadOnlyList<string> ToKanaBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
            {
                results.Add(ToKana(texts[i]));
            }
            return results;
        }

        /// <summary>
        /// 複数テキストをバッチ処理でESPnet韻律記号付き文字列に変換する。
        /// 形態素解析器の内部バッファ（ラティス等）が文間で再利用されるため、個別呼び出しよりGC効率が良い。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>ESPnet韻律記号付き音素文字列のリスト</returns>
        public IReadOnlyList<string> ToProsodyBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<string>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
            {
                results.Add(ToProsody(texts[i]));
            }
            return results;
        }

        /// <summary>
        /// 複数テキストをバッチ処理でHTSフルコンテキストラベル列に変換する。
        /// 形態素解析器の内部バッファ（ラティス等）が文間で再利用されるため、個別呼び出しよりGC効率が良い。
        /// </summary>
        /// <param name="texts">入力テキストのリスト</param>
        /// <returns>フルコンテキストラベル文字列のリストのリスト</returns>
        public IReadOnlyList<IReadOnlyList<string>> ToFullContextLabelsBatch(IReadOnlyList<string> texts)
        {
            ThrowIfDisposed();
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var results = new List<IReadOnlyList<string>>(texts.Count);
            for (int i = 0; i < texts.Count; i++)
            {
                results.Add(ToFullContextLabels(texts[i]));
            }
            return results;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _tokenizer.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(G2PEngine));
        }
    }
}
