using System;
using System.IO;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab連接コスト行列 (matrix.bin) を読み込み、左右文脈IDから遷移コストを返す。
    /// </summary>
    public sealed class ConnectionMatrix
    {
        private readonly short[] _matrix;
        private readonly int _lSize;

        /// <summary>左文脈サイズ</summary>
        public int LeftSize => _lSize;

        /// <summary>右文脈サイズ</summary>
        public int RightSize { get; }

        private ConnectionMatrix(short[] matrix, int lSize, int rSize)
        {
            _matrix = matrix;
            _lSize = lSize;
            RightSize = rSize;
        }

        /// <summary>
        /// 連接コストを取得する。
        /// NMeCab方式: matrix[rightContextId + lSize * leftContextId]
        /// </summary>
        /// <param name="rightContextId">右ノードの右文脈ID (RcAttr)</param>
        /// <param name="leftContextId">左ノードの左文脈ID (LcAttr)</param>
        /// <returns>連接コスト (rNode.WCostは含まない)</returns>
        public short GetCost(ushort rightContextId, ushort leftContextId)
        {
            int index = rightContextId + _lSize * leftContextId;
            if ((uint)index >= (uint)_matrix.Length)
                throw new ArgumentOutOfRangeException(
                    $"連接コスト行列のインデックスが範囲外です: rightCtxId={rightContextId}, leftCtxId={leftContextId}");
            return _matrix[index];
        }

        /// <summary>
        /// matrix.binファイルを読み込む。
        /// </summary>
        /// <param name="filePath">matrix.binファイルパス</param>
        public static ConnectionMatrix Load(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"連接コスト行列ファイルが見つかりません: {filePath}", filePath);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            ushort lSize = reader.ReadUInt16();
            ushort rSize = reader.ReadUInt16();

            int totalEntries = lSize * rSize;
            var matrix = new short[totalEntries];

            for (int i = 0; i < totalEntries; i++)
            {
                matrix[i] = reader.ReadInt16();
            }

            return new ConnectionMatrix(matrix, lSize, rSize);
        }
    }
}
