using System;
using System.Collections.Generic;
using System.IO;
using NMeCab;

namespace DotNetG2P.NMeCab
{
    /// <summary>
    /// LibNMeCabを使用した形態素解析器。
    /// naist-jdic辞書を使い、テキストをITokenのリストに変換する。
    /// </summary>
    public sealed class NMeCabTokenizer : ITokenizer
    {
        private readonly MeCabTagger _tagger;
        private bool _disposed;

        /// <summary>
        /// NMeCabTokenizerを初期化する。
        /// </summary>
        /// <param name="dictionaryPath">naist-jdic辞書のディレクトリパス</param>
        public NMeCabTokenizer(string dictionaryPath)
        {
            if (dictionaryPath == null)
                throw new ArgumentNullException(nameof(dictionaryPath));
            if (!Directory.Exists(dictionaryPath))
                throw new DirectoryNotFoundException($"辞書ディレクトリが見つかりません: {dictionaryPath}");

            _tagger = MeCabTagger.Create(dictionaryPath);
        }

        /// <summary>
        /// テキストを形態素解析し、トークン列を返す。
        /// BOS/EOSノードは除外される。
        /// </summary>
        public IReadOnlyList<IToken> Tokenize(string text)
        {
            ThrowIfDisposed();

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var nodes = _tagger.Parse(text);
            var tokens = new List<IToken>(nodes.Length);

            foreach (var node in nodes)
            {
                // BOS（文頭）/EOS（文末）ノードは除外
                if (node.Stat == MeCabNodeStat.Bos || node.Stat == MeCabNodeStat.Eos)
                    continue;

                tokens.Add(new NMeCabToken(node.Surface, node.Feature));
            }

            return tokens;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                try
                {
                    _tagger.Dispose();
                }
                catch
                {
                    // Dispose中の例外は無視する
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NMeCabTokenizer));
        }

        /// <summary>
        /// NMeCabの解析結果をITokenとして公開する内部クラス。
        /// Featureのカンマ区切りを分割し、各フィールドへのアクセスを提供する。
        /// </summary>
        private sealed class NMeCabToken : IToken
        {
            /// <summary>naist-jdic辞書の期待フィールド数</summary>
            private const int ExpectedFieldCount = 15;

            /// <summary>フィールドが存在しない場合のデフォルト値</summary>
            private const string DefaultValue = "*";

            private readonly string[] _features;

            public NMeCabToken(string surface, string feature)
            {
                Surface = surface;

                // Featureをカンマで分割し、15フィールドに満たない場合は"*"で埋める
                var raw = feature?.Split(',') ?? Array.Empty<string>();
                if (raw.Length >= ExpectedFieldCount)
                {
                    _features = raw;
                }
                else
                {
                    _features = new string[ExpectedFieldCount];
                    for (int i = 0; i < ExpectedFieldCount; i++)
                    {
                        _features[i] = i < raw.Length ? raw[i] : DefaultValue;
                    }
                }
            }

            /// <summary>表層形（原文中の文字列）</summary>
            public string Surface { get; }

            /// <summary>素性配列</summary>
            public IReadOnlyList<string> Features => _features;

            /// <summary>フィールド0: 品詞</summary>
            public string POS => _features[0];

            /// <summary>フィールド1: 品詞細分類1</summary>
            public string POSGroup1 => _features[1];

            /// <summary>フィールド2: 品詞細分類2</summary>
            public string POSGroup2 => _features[2];

            /// <summary>フィールド3: 品詞細分類3</summary>
            public string POSGroup3 => _features[3];

            /// <summary>フィールド4: 活用型</summary>
            public string ConjugationType => _features[4];

            /// <summary>フィールド5: 活用形</summary>
            public string ConjugationForm => _features[5];

            /// <summary>フィールド6: 原形</summary>
            public string OriginalForm => _features[6];

            /// <summary>フィールド7: 読み</summary>
            public string Reading => _features[7];

            /// <summary>フィールド8: 発音</summary>
            public string Pronunciation => _features[8];

            /// <summary>フィールド9: アクセント核位置/モーラ数</summary>
            public string AccentInfo => _features[9];

            /// <summary>フィールド10: アクセント結合タイプ</summary>
            public string ChainRule => _features[10];
        }
    }
}
