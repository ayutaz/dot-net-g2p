using System;
using System.Collections.Generic;
using DotNetG2P.Models;
using DotNetG2P.NJD;
using DotNetG2P.PhonemeConverter;

namespace DotNetG2P
{
    /// <summary>
    /// 日本語G2P（Grapheme-to-Phoneme）エンジン。
    /// テキストを形態素解析し、NJD処理を経て音素列を出力する。
    /// </summary>
    public sealed class G2PEngine : IDisposable
    {
        private readonly ITokenizer _tokenizer;
        private bool _disposed;

        /// <summary>
        /// G2PEngineを初期化する。
        /// </summary>
        /// <param name="tokenizer">使用する形態素解析器</param>
        public G2PEngine(ITokenizer tokenizer)
        {
            _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
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

            // 1. 形態素解析
            var tokens = _tokenizer.Tokenize(text);

            // 2. NjdNodeリスト構築
            var nodes = NjdNode.FromTokens(tokens);

            // 3. 発音設定（NJD処理第1段階）
            SetPronunciation.Process(nodes, tokens);

            // 4. 各ノードの発音を音素文字列に変換して結合
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

            var tokens = _tokenizer.Tokenize(text);
            var nodes = NjdNode.FromTokens(tokens);
            SetPronunciation.Process(nodes, tokens);

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
