namespace DotNetG2P.MeCab.Lattice
{
    /// <summary>
    /// ラティスグラフの1ノード。辞書エントリまたは未知語の1候補を表す。
    /// Viterbiデコーダによるコスト計算・最良前ノード追跡にも使用される。
    /// </summary>
    public sealed class LatticeNode
    {
        // 遅延Surface計算用: LatticeBuilder がセットする
        internal string? _sourceText;
        private string? _surface;

        /// <summary>表層形（遅延計算: ベストパス選択後に初めて文字列化）</summary>
        public string Surface
        {
            get => _surface ??= (_sourceText != null
                ? _sourceText.Substring(StartPos, EndPos - StartPos)
                : "");
            set => _surface = value;
        }

        /// <summary>開始位置 (文字インデックス、C# char単位)</summary>
        public int StartPos { get; set; }

        /// <summary>終了位置 (文字インデックス、C# char単位)</summary>
        public int EndPos { get; set; }

        /// <summary>左文脈ID (LcAttr)</summary>
        public ushort LeftCtxId { get; set; }

        /// <summary>右文脈ID (RcAttr)</summary>
        public ushort RightCtxId { get; set; }

        /// <summary>単語生起コスト</summary>
        public short WordCost { get; set; }

        /// <summary>素性文字列 (カンマ区切り)。遅延デコード時はベストパス選択後に設定される。</summary>
        public string Feature { get; set; } = "";

        /// <summary>素性バッファ内のオフセット (遅延デコード用)</summary>
        internal uint FeatureOffset { get; set; }

        /// <summary>Viterbi: 累積最小コスト</summary>
        public long BestCost { get; set; } = long.MaxValue;

        /// <summary>Viterbi: 最良前ノード</summary>
        public LatticeNode? BestPrev { get; set; }

        /// <summary>未知語フラグ</summary>
        public bool IsUnknown { get; set; }
    }
}
