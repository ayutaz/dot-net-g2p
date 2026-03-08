using System;
using System.Collections.Generic;
using DotNetG2P.MeCab.Dictionary;
using DotNetG2P.MeCab.Trie;

namespace DotNetG2P.MeCab.Lattice
{
    /// <summary>
    /// テキストからラティスグラフを構築する。
    /// Trie検索による辞書候補と、文字種プロパティに基づく未知語候補を生成する。
    /// バッファをインスタンスフィールドとして保持し、Build()呼び出し間で再利用する。
    /// </summary>
    /// <remarks>
    /// 内部バッファの再利用のため、このクラスはスレッドセーフではありません。
    /// 単一スレッドからの呼び出しを前提とします。
    /// </remarks>
    public sealed class LatticeBuilder
    {
        private readonly DictionaryBundle _dic;
        private readonly DoubleArrayTrie _trie;
        private readonly CharInfo _spaceCharInfo;

        // 再利用バッファ
        private List<LatticeNode>[] _endNodes;
        private readonly TrieResult[] _trieResults = new TrieResult[512];
        private bool[] _processedCharPositions;

        /// <param name="dic">辞書バンドル</param>
        public LatticeBuilder(DictionaryBundle dic)
        {
            _dic = dic;
            _trie = new DoubleArrayTrie(dic.SystemDic.TrieData);
            _spaceCharInfo = dic.CharProperty.GetCharInfo(' ');
        }

        /// <summary>
        /// テキストからラティスを構築する。
        /// </summary>
        /// <returns>endNodes配列。endNodes[i] = 位置iで終わるノードのリスト。endNodes[0]にBOS、endNodes[charLen+1]にEOS用。</returns>
        public List<LatticeNode>[] Build(string text)
        {
            using var charMap = new Utf8CharMap(text);
            int charLen = text.Length;
            int requiredEndNodesLen = charLen + 2;

            // endNodes バッファ再利用
            if (_endNodes == null || _endNodes.Length < requiredEndNodesLen)
            {
                _endNodes = new List<LatticeNode>[requiredEndNodesLen];
                for (int i = 0; i < _endNodes.Length; i++)
                    _endNodes[i] = new List<LatticeNode>(4);
            }
            else
            {
                for (int i = 0; i < requiredEndNodesLen; i++)
                {
                    if (_endNodes[i] == null)
                        _endNodes[i] = new List<LatticeNode>(4);
                    else
                    {
                        _endNodes[i].Clear();
                        if (_endNodes[i].Capacity > 1024)
                            _endNodes[i].TrimExcess();
                    }
                }
            }

            // processedCharPositions バッファ再利用
            if (_processedCharPositions == null || _processedCharPositions.Length < charLen)
            {
                _processedCharPositions = new bool[charLen];
            }
            else
            {
                Array.Clear(_processedCharPositions, 0, charLen);
            }

            // CharInfo プリキャッシュ
            Span<CharInfo> charInfoCache = charLen <= 256
                ? stackalloc CharInfo[charLen]
                : new CharInfo[charLen];
            var charProp = _dic.CharProperty;
            for (int i = 0; i < charLen; i++)
                charInfoCache[i] = charProp.GetCharInfo(text[i]);

            // BOS (contextId=0, cost=0)
            var bos = new LatticeNode
            {
                Surface = "",
                StartPos = 0,
                EndPos = 0,
                LeftCtxId = 0,
                RightCtxId = 0,
                WordCost = 0,
                BestCost = 0,
            };
            _endNodes[0].Add(bos);

            // 各バイト位置でTrie検索
            byte[] utf8 = charMap.Utf8Bytes;

            for (int bytePos = 0; bytePos < utf8.Length;)
            {
                int charPos = charMap.ByteToCharIndex(bytePos);

                // 既に処理済みの文字位置はスキップ
                if (_processedCharPositions[charPos])
                {
                    bytePos = NextBytePosition(charMap, charPos);
                    continue;
                }
                _processedCharPositions[charPos] = true;

                // MeCab互換: スペース文字をスキップ (seekToOtherType相当)
                // スペースカテゴリに属する文字を読み飛ばし、トークンとして生成しない
                {
                    var ci = charInfoCache[charPos];
                    if (_spaceCharInfo.IsKindOf(ci))
                    {
                        // スペース文字を連続してスキップ
                        int skipEnd = charPos + 1;
                        while (skipEnd < charLen)
                        {
                            var nextCi = charInfoCache[skipEnd];
                            if (!_spaceCharInfo.IsKindOf(nextCi))
                                break;
                            _processedCharPositions[skipEnd] = true;
                            skipEnd++;
                        }
                        // スキップ後の位置に、前のノードの参照先として使うために
                        // endNodes を同期する（スペースの終了位置 == 次のトークンの開始位置）
                        // endNodes[charPos] に入っているノードを endNodes[skipEnd] にコピー
                        if (skipEnd != charPos && skipEnd <= charLen)
                        {
                            foreach (var prevNode in _endNodes[charPos])
                            {
                                _endNodes[skipEnd].Add(prevNode);
                            }
                        }
                        if (skipEnd >= charLen)
                        {
                            bytePos = utf8.Length;
                        }
                        else
                        {
                            bytePos = charMap.CharToByteIndex(skipEnd);
                        }
                        continue;
                    }
                }

                bool hasDictHit = false;

                // 1. Trie検索（辞書候補）
                int matchCount = _trie.CommonPrefixSearch(utf8, bytePos, utf8.Length - bytePos, _trieResults);
                for (int m = 0; m < matchCount; m++)
                {
                    int value = _trieResults[m].Value;
                    int matchByteLen = _trieResults[m].Length;
                    if (matchByteLen <= 0 || bytePos + matchByteLen > utf8.Length)
                        continue;

                    // バイト長→文字インデックス変換
                    int endBytePos = bytePos + matchByteLen;
                    int endCharPos;
                    if (endBytePos >= utf8.Length)
                        endCharPos = charLen;
                    else
                        endCharPos = charMap.ByteToCharIndex(endBytePos);

                    // NMeCab方式: value下位8ビット=トークン数, value >> 8 = 開始位置
                    int tokenCount = value & 0xFF;
                    int tokenStart = value >> 8;

                    for (int t = 0; t < tokenCount; t++)
                    {
                        var dicToken = _dic.SystemDic.GetToken(tokenStart, t);

                        var node = new LatticeNode
                        {
                            _sourceText = text,
                            StartPos = charPos,
                            EndPos = endCharPos,
                            LeftCtxId = dicToken.LcAttr,
                            RightCtxId = dicToken.RcAttr,
                            WordCost = dicToken.WCost,
                            FeatureOffset = dicToken.FeatureOffset,
                            IsUnknown = false,
                        };
                        _endNodes[endCharPos].Add(node);
                        hasDictHit = true;
                    }
                }

                // 2. 未知語処理
                var cInfo = charInfoCache[charPos];

                if (cInfo.Invoke || !hasDictHit)
                {
                    AddUnknownNodes(text, charPos, cInfo, _endNodes, charInfoCache);
                }

                // 次の文字位置へ
                bytePos = NextBytePosition(charMap, charPos);
            }

            // EOS
            var eos = new LatticeNode
            {
                Surface = "",
                StartPos = charLen,
                EndPos = charLen,
                LeftCtxId = 0,
                RightCtxId = 0,
                WordCost = 0,
            };
            _endNodes[charLen + 1].Add(eos);

            return _endNodes;
        }

        /// <summary>
        /// 未知語ノードを追加する。文字種プロパティのGroup/Lengthに基づいて候補を生成する。
        /// </summary>
        private void AddUnknownNodes(string text, int charPos, CharInfo cInfo, List<LatticeNode>[] endNodes, Span<CharInfo> charInfoCache)
        {
            int categoryIndex = cInfo.DefaultType;
            int tokenCount = _dic.UnknownDic.GetTokenCount(categoryIndex);
            if (tokenCount == 0) return;

            int charLen = text.Length;

            // group=true の場合: 同カテゴリ文字を連続してグループ化
            if (cInfo.Group)
            {
                int groupEnd = charPos + 1;
                while (groupEnd < charLen)
                {
                    var nextInfo = charInfoCache[groupEnd];
                    if (!cInfo.IsKindOf(nextInfo))
                        break;
                    groupEnd++;
                }

                for (int t = 0; t < tokenCount; t++)
                {
                    var dicToken = _dic.UnknownDic.GetToken(categoryIndex, t);
                    string feature = _dic.UnknownDic.GetFeature(dicToken.FeatureOffset);

                    var node = new LatticeNode
                    {
                        _sourceText = text,
                        StartPos = charPos,
                        EndPos = groupEnd,
                        LeftCtxId = dicToken.LcAttr,
                        RightCtxId = dicToken.RcAttr,
                        WordCost = dicToken.WCost,
                        Feature = feature,
                        IsUnknown = true,
                    };
                    endNodes[groupEnd].Add(node);
                }
            }

            // length > 0 の場合: 1〜length文字の候補を個別に生成
            int maxLen = cInfo.Length;
            if (maxLen > 0)
            {
                for (int len = 1; len <= maxLen && charPos + len <= charLen; len++)
                {
                    // 各文字が同カテゴリか確認
                    bool sameCategory = true;
                    for (int k = charPos + 1; k < charPos + len; k++)
                    {
                        var ki = charInfoCache[k];
                        if (!cInfo.IsKindOf(ki))
                        {
                            sameCategory = false;
                            break;
                        }
                    }
                    if (!sameCategory) break;

                    int endPos = charPos + len;

                    for (int t = 0; t < tokenCount; t++)
                    {
                        var dicToken = _dic.UnknownDic.GetToken(categoryIndex, t);
                        string feature = _dic.UnknownDic.GetFeature(dicToken.FeatureOffset);

                        var node = new LatticeNode
                        {
                            _sourceText = text,
                            StartPos = charPos,
                            EndPos = endPos,
                            LeftCtxId = dicToken.LcAttr,
                            RightCtxId = dicToken.RcAttr,
                            WordCost = dicToken.WCost,
                            Feature = feature,
                            IsUnknown = true,
                        };
                        endNodes[endPos].Add(node);
                    }
                }
            }

            // group=falseかつlength=0の場合: 1文字だけの未知語
            if (!cInfo.Group && maxLen == 0)
            {
                int endPos = charPos + 1;

                for (int t = 0; t < tokenCount; t++)
                {
                    var dicToken = _dic.UnknownDic.GetToken(categoryIndex, t);
                    string feature = _dic.UnknownDic.GetFeature(dicToken.FeatureOffset);

                    var node = new LatticeNode
                    {
                        _sourceText = text,
                        StartPos = charPos,
                        EndPos = endPos,
                        LeftCtxId = dicToken.LcAttr,
                        RightCtxId = dicToken.RcAttr,
                        WordCost = dicToken.WCost,
                        Feature = feature,
                        IsUnknown = true,
                    };
                    endNodes[endPos].Add(node);
                }
            }
        }

        /// <summary>
        /// charPosの次の文字の先頭バイト位置を返す。サロゲートペアを考慮する。
        /// </summary>
        private static int NextBytePosition(Utf8CharMap map, int charPos)
        {
            int nextCharPos = charPos + 1;
            // サロゲートペアの場合: ローサロゲートをスキップ
            if (nextCharPos < map.CharLength && char.IsLowSurrogate(map.Text[nextCharPos]))
                nextCharPos++;

            if (nextCharPos >= map.CharLength)
                return map.ByteLength;
            return map.CharToByteIndex(nextCharPos);
        }
    }
}
