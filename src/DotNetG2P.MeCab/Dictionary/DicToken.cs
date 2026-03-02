using System;
using System.Buffers.Binary;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab辞書のトークンエントリ (16バイト)。
    /// </summary>
    public readonly struct DicToken
    {
        /// <summary>1トークンあたりのバイトサイズ</summary>
        public const int ByteSize = 16;

        /// <summary>左文脈ID</summary>
        public readonly ushort LcAttr;

        /// <summary>右文脈ID</summary>
        public readonly ushort RcAttr;

        /// <summary>形態素ID</summary>
        public readonly ushort PosId;

        /// <summary>単語生起コスト</summary>
        public readonly short WCost;

        /// <summary>素性情報バッファ内のオフセット</summary>
        public readonly uint FeatureOffset;

        /// <summary>複合語情報（MeCab互換で読み込むが、本実装では未使用）。将来の複合語処理拡張用に予約。</summary>
        public readonly uint Compound;

        /// <summary>フィールドを指定してDicTokenを構築する。</summary>
        public DicToken(ushort lcAttr, ushort rcAttr, ushort posId, short wCost, uint featureOffset, uint compound)
        {
            LcAttr = lcAttr;
            RcAttr = rcAttr;
            PosId = posId;
            WCost = wCost;
            FeatureOffset = featureOffset;
            Compound = compound;
        }

        /// <summary>
        /// バイト配列の指定オフセットから1トークンを読み取る。
        /// </summary>
        public static DicToken Read(byte[] buffer, int offset)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset + ByteSize > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"バッファサイズ不足: offset={offset}, 必要={ByteSize}, バッファ長={buffer.Length}");

            var span = buffer.AsSpan(offset, ByteSize);
            ushort lcAttr = BinaryPrimitives.ReadUInt16LittleEndian(span);
            ushort rcAttr = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));
            ushort posId = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));
            short wCost = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(6));
            uint featureOffset = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8));
            uint compound = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12));

            return new DicToken(lcAttr, rcAttr, posId, wCost, featureOffset, compound);
        }
    }
}
