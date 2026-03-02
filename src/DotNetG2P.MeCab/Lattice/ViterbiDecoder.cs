using System.Collections.Generic;
using DotNetG2P.MeCab.Dictionary;

namespace DotNetG2P.MeCab.Lattice
{
    /// <summary>
    /// Viterbiデコーダ。前向きパスで累積コストを計算し、後ろ向きトレースで最小コスト経路を復元する。
    /// </summary>
    public sealed class ViterbiDecoder
    {
        private readonly ConnectionMatrix _matrix;

        /// <param name="matrix">連接コスト行列</param>
        public ViterbiDecoder(ConnectionMatrix matrix)
        {
            _matrix = matrix;
        }

        /// <summary>
        /// ビタビデコード: 前向きパス + 後ろ向きトレースで最小コスト経路を返す。
        /// </summary>
        /// <param name="endNodes">LatticeBuilder.Buildの結果</param>
        /// <param name="textLength">テキストの文字数</param>
        /// <returns>BOS/EOS除外のノード列（順方向）</returns>
        public List<LatticeNode> Decode(List<LatticeNode>[] endNodes, int textLength)
        {
            // 前向きパス
            // endNodes[i] = 位置iで**終わる**ノードのリスト
            // endNodes[textLength+1] にEOS
            for (int endPos = 1; endPos < endNodes.Length; endPos++)
            {
                foreach (var node in endNodes[endPos])
                {
                    int startPos = node.StartPos;

                    // startPosで終わるノード群から最小コスト遷移を探す
                    foreach (var prev in endNodes[startPos])
                    {
                        if (prev == node)
                            continue; // 自己参照防止（スペーススキップでBOSが複数位置に存在する場合）

                        if (prev.BestCost == long.MaxValue)
                            continue;

                        // matrix[prev.RCAttr + LSize * node.LCAttr] + node.WCost
                        long connectionCost = _matrix.GetCost(prev.RightCtxId, node.LeftCtxId);
                        long totalCost = prev.BestCost + connectionCost + node.WordCost;

                        if (totalCost < node.BestCost)
                        {
                            node.BestCost = totalCost;
                            node.BestPrev = prev;
                        }
                    }
                }
            }

            // 後ろ向きトレース: EOSから逆順にBestPrevを辿る
            LatticeNode? eosNode = null;
            foreach (var n in endNodes[textLength + 1])
            {
                if (eosNode == null || n.BestCost < eosNode.BestCost)
                    eosNode = n;
            }

            if (eosNode == null || eosNode.BestCost == long.MaxValue)
                return new List<LatticeNode>();

            var path = new List<LatticeNode>();
            var current = eosNode.BestPrev; // EOS自体は除外
            while (current != null && current.BestPrev != null) // BOS（BestPrev==null）も除外
            {
                if (current.BestPrev == current)
                    break; // サイクル検出（安全装置）
                path.Add(current);
                current = current.BestPrev;
            }

            path.Reverse();
            return path;
        }
    }
}
