using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DotNetG2P.MeCab.Dictionary;
using DotNetG2P.MeCab.Lattice;

namespace DotNetG2P.MeCab
{
    /// <summary>
    /// 独自MeCabエンジンによるITokenizer実装。
    /// naist-jdic辞書を読み込み、DoubleArrayTrie + Viterbiデコードで形態素解析を行う。
    /// </summary>
    /// <remarks>
    /// このクラスはスレッドセーフではありません。Tokenize() の同時呼び出しはサポートされません。
    /// マルチスレッド環境ではスレッドごとにインスタンスを作成してください。
    /// 辞書データは <see cref="DictionaryBundle"/> の WeakReference キャッシュにより共有されるため、
    /// 複数インスタンスのメモリオーバーヘッドは最小限です。
    /// </remarks>
    public sealed class MeCabTokenizer : ITokenizer
    {
        private readonly DictionaryBundle _dic;
        private readonly Lazy<LatticeBuilder> _lazyBuilder;
        private readonly Lazy<ViterbiDecoder> _lazyDecoder;
        private int _disposed;

        /// <param name="dictionaryPath">naist-jdic辞書ディレクトリのパス</param>
        public MeCabTokenizer(string dictionaryPath)
        {
            if (dictionaryPath == null)
                throw new ArgumentNullException(nameof(dictionaryPath));
            if (!Directory.Exists(dictionaryPath))
                throw new DirectoryNotFoundException($"辞書ディレクトリが見つかりません: {dictionaryPath}");

            _dic = DictionaryBundle.Load(dictionaryPath);
            _lazyBuilder = new Lazy<LatticeBuilder>(() => new LatticeBuilder(_dic));
            _lazyDecoder = new Lazy<ViterbiDecoder>(() => new ViterbiDecoder(_dic.Matrix));
        }

        /// <inheritdoc/>
        public IReadOnlyList<IToken> Tokenize(string text)
        {
            ThrowIfDisposed();
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0) return Array.Empty<IToken>();

            var endNodes = _lazyBuilder.Value.Build(text);
            var bestPath = _lazyDecoder.Value.Decode(endNodes, text.Length);

            var tokens = new List<IToken>(bestPath.Count);
            for (int i = 0; i < bestPath.Count; i++)
            {
                var node = bestPath[i];
                // システム辞書ノードはFeatureが遅延デコードされている（Feature==""のまま）
                // ベストパスに選ばれたノードのみGetFeatureでUTF-8→stringデコードする
                if (!node.IsUnknown && node.Feature.Length == 0)
                {
                    node.Feature = _dic.SystemDic.GetFeature(node.FeatureOffset);
                }
                tokens.Add(new MeCabToken(node.Surface, node.Feature));
            }
            return tokens;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;
            _dic?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// IToken実装。遅延パーサによりカンマ分割を必要時まで遅延させる。
        /// 個別フィールドアクセスでは Substring のみ、Features プロパティは初回アクセス時に string[] を構築してキャッシュ。
        /// </summary>
        private sealed class MeCabToken : IToken
        {
            private const int ExpectedFieldCount = 15;
            private const string DefaultValue = "*";

            private readonly string _rawFeature;
            private readonly int[] _commaPositions;
            private readonly int _fieldCount;
            private string[]? _cachedFeatures;

            public MeCabToken(string surface, string feature)
            {
                Surface = surface;
                _rawFeature = feature ?? string.Empty;
                _commaPositions = FindCommaPositions(_rawFeature);
                _fieldCount = _commaPositions.Length + 1;
            }

            public string Surface { get; }

            public IReadOnlyList<string> Features => _cachedFeatures ??= BuildFeatures();

            public string POS => GetField(0);
            public string POSGroup1 => GetField(1);
            public string POSGroup2 => GetField(2);
            public string POSGroup3 => GetField(3);
            public string ConjugationType => GetField(4);
            public string ConjugationForm => GetField(5);
            public string OriginalForm => GetField(6);
            public string Reading => GetField(7);
            public string Pronunciation => GetField(8);
            public string AccentInfo => GetField(9);
            public string ChainRule => GetField(10);

            private string GetField(int index)
            {
                if (index >= _fieldCount)
                    return DefaultValue;

                int start = index == 0 ? 0 : _commaPositions[index - 1] + 1;
                int end = index < _commaPositions.Length ? _commaPositions[index] : _rawFeature.Length;
                string value = _rawFeature.Substring(start, end - start);

                // 品詞（0-3）、活用型（4）、活用形（5）は頻出文字列なのでintern
                if (index <= 5)
                    return string.Intern(value);

                return value;
            }

            private string[] BuildFeatures()
            {
                var features = new string[ExpectedFieldCount];
                for (int i = 0; i < ExpectedFieldCount; i++)
                    features[i] = GetField(i);
                return features;
            }

            private static int[] FindCommaPositions(string s)
            {
                // カンマの数をまず数える
                int count = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == ',')
                        count++;
                }

                if (count == 0)
                    return Array.Empty<int>();

                var positions = new int[count];
                int idx = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == ',')
                        positions[idx++] = i;
                }
                return positions;
            }
        }
    }
}
