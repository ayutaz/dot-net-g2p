using System;
using System.Collections.Generic;
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
            SetPronunciation.Process(nodes, tokens);

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

            if (string.IsNullOrEmpty(text))
                return "";

            var nodes = RunPipeline(text);

            // 各ノードの発音を音素文字列に変換して結合
            var parts = new List<string>();
            foreach (var node in nodes)
            {
                if (node.Pronunciation != null && node.Pronunciation.MoraCount > 0)
                {
                    var phonemes = MoraMapping.MorasToPhonemeString(node.Pronunciation.Moras);
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

            if (string.IsNullOrEmpty(text))
                return "";

            var nodes = RunPipeline(text);

            var sb = new System.Text.StringBuilder();
            foreach (var node in nodes)
            {
                if (node.Pronunciation != null)
                {
                    sb.Append(node.Pronunciation.ToKatakana());
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// テキストをNJDパイプライン処理後のNjdNodeリストとして返す。
        /// デバッグ・検証用途。
        /// </summary>
        /// <param name="text">入力テキスト</param>
        /// <returns>NJD処理済みのNjdNodeリスト</returns>
        public IReadOnlyList<NjdNode> Analyze(string text)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text))
                return Array.Empty<NjdNode>();

            return RunPipeline(text);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _tokenizer.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(G2PEngine));
        }
    }
}
