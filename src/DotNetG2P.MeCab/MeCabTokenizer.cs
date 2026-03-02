using System;
using System.Collections.Generic;
using System.IO;
using DotNetG2P.MeCab.Dictionary;
using DotNetG2P.MeCab.Lattice;

namespace DotNetG2P.MeCab
{
    /// <summary>
    /// 独自MeCabエンジンによるITokenizer実装。
    /// naist-jdic辞書を読み込み、DoubleArrayTrie + Viterbiデコードで形態素解析を行う。
    /// </summary>
    public sealed class MeCabTokenizer : ITokenizer
    {
        private readonly DictionaryBundle _dic;
        private readonly LatticeBuilder _builder;
        private readonly ViterbiDecoder _decoder;
        private volatile bool _disposed;

        /// <param name="dictionaryPath">naist-jdic辞書ディレクトリのパス</param>
        public MeCabTokenizer(string dictionaryPath)
        {
            if (dictionaryPath == null)
                throw new ArgumentNullException(nameof(dictionaryPath));
            if (!Directory.Exists(dictionaryPath))
                throw new DirectoryNotFoundException($"辞書ディレクトリが見つかりません: {dictionaryPath}");

            _dic = DictionaryBundle.Load(dictionaryPath);
            _builder = new LatticeBuilder(_dic);
            _decoder = new ViterbiDecoder(_dic.Matrix);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IToken> Tokenize(string text)
        {
            ThrowIfDisposed();
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return Array.Empty<IToken>();

            var endNodes = _builder.Build(text);
            var bestPath = _decoder.Decode(endNodes, text.Length);

            var tokens = new List<IToken>(bestPath.Count);
            foreach (var node in bestPath)
            {
                tokens.Add(new MeCabToken(node.Surface, node.Feature));
            }
            return tokens;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dic.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MeCabTokenizer));
        }

        /// <summary>
        /// IToken実装。NMeCabTokenと同一のカンマ分割、15フィールド"*"パディング。
        /// </summary>
        private sealed class MeCabToken : IToken
        {
            private const int ExpectedFieldCount = 15;
            private const string DefaultValue = "*";

            private readonly string[] _features;

            public MeCabToken(string surface, string feature)
            {
                Surface = surface;
                var raw = feature?.Split(',') ?? Array.Empty<string>();
                if (raw.Length >= ExpectedFieldCount)
                {
                    _features = raw;
                }
                else
                {
                    _features = new string[ExpectedFieldCount];
                    for (int i = 0; i < ExpectedFieldCount; i++)
                        _features[i] = i < raw.Length ? raw[i] : DefaultValue;
                }
            }

            public string Surface { get; }
            public IReadOnlyList<string> Features => _features;
            public string POS => _features[0];
            public string POSGroup1 => _features[1];
            public string POSGroup2 => _features[2];
            public string POSGroup3 => _features[3];
            public string ConjugationType => _features[4];
            public string ConjugationForm => _features[5];
            public string OriginalForm => _features[6];
            public string Reading => _features[7];
            public string Pronunciation => _features[8];
            public string AccentInfo => _features[9];
            public string ChainRule => _features[10];
        }
    }
}
