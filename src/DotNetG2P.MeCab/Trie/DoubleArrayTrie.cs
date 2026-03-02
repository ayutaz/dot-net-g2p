// DoubleArrayTrie.cs - NMeCab互換のDouble-Array Trie実装
// NMeCabのDoubleArray.csを参考に、byte[]ベースの安全な実装とする

using System;
using System.Buffers.Binary;

namespace DotNetG2P.MeCab.Trie
{
    /// <summary>
    /// CommonPrefixSearchおよびExactMatchSearchの結果を格納する構造体。
    /// </summary>
    public readonly struct TrieResult
    {
        /// <summary>トークンバッファへの参照 (下位8ビット=数、上位24ビット=位置)</summary>
        public readonly int Value;

        /// <summary>マッチしたバイト数</summary>
        public readonly int Length;

        public TrieResult(int value, int length)
        {
            Value = value;
            Length = length;
        }
    }

    /// <summary>
    /// NMeCab互換のDouble-Array Trie。
    /// sys.dicのTrieセクションからBase/Check配列を復元し、共通接頭辞検索・完全一致検索を提供する。
    /// </summary>
    public sealed class DoubleArrayTrie
    {
        private readonly int[] _bases;
        private readonly uint[] _checks;

        /// <summary>
        /// sys.dicのTrieセクションの生バイト列からDoubleArrayTrieを構築する。
        /// 8バイトずつ Base(int, LE) + Check(uint, LE) のペアとして読み込む。
        /// </summary>
        public DoubleArrayTrie(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length % 8 != 0)
                throw new ArgumentException("Trie data length must be a multiple of 8.", nameof(data));

            int unitCount = data.Length / 8;
            _bases = new int[unitCount];
            _checks = new uint[unitCount];

            for (int i = 0; i < unitCount; i++)
            {
                int offset = i * 8;
                _bases[i] = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
                _checks[i] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4));
            }
        }

        /// <summary>
        /// 共通接頭辞検索。keyのoffset位置からkeyLength分のバイト列に対して
        /// 前方一致するすべてのエントリを返す。
        /// </summary>
        /// <param name="key">UTF-8エンコードされたバイト列</param>
        /// <param name="offset">keyの開始位置</param>
        /// <param name="keyLength">検索対象バイト数</param>
        /// <param name="results">結果を格納するバッファ</param>
        /// <returns>マッチ数</returns>
        public int CommonPrefixSearch(byte[] key, int offset, int keyLength, TrieResult[] results)
        {
            int count = 0;
            int b = _bases[0]; // ルートノードのBase

            for (int i = 0; i < keyLength; i++)
            {
                // 中間マッチチェック: 現在のノード自体が終端かどうか
                int p = b;
                if ((uint)p < (uint)_bases.Length)
                {
                    int n = _bases[p];
                    if (b == (int)_checks[p] && n < 0)
                    {
                        if (count < results.Length)
                        {
                            results[count] = new TrieResult(-n - 1, i);
                        }
                        count++;
                    }
                }

                // 次のノードへ遷移
                p = b + key[offset + i] + 1;
                if ((uint)p >= (uint)_checks.Length || _checks[p] != (uint)b)
                    return count;

                b = _bases[p];
            }

            // 最終マッチチェック
            {
                int p = b;
                if ((uint)p < (uint)_bases.Length)
                {
                    int n = _bases[p];
                    if (b == (int)_checks[p] && n < 0)
                    {
                        if (count < results.Length)
                        {
                            results[count] = new TrieResult(-n - 1, keyLength);
                        }
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 完全一致検索。keyのoffset位置からkeyLength分のバイト列に完全一致するエントリを検索する。
        /// </summary>
        /// <param name="key">UTF-8エンコードされたバイト列</param>
        /// <param name="offset">keyの開始位置</param>
        /// <param name="keyLength">検索対象バイト数</param>
        /// <returns>マッチした場合はvalue値、マッチしなければ-1</returns>
        public int ExactMatchSearch(byte[] key, int offset, int keyLength)
        {
            int b = _bases[0]; // ルートノードのBase

            for (int i = 0; i < keyLength; i++)
            {
                int p = b + key[offset + i] + 1;
                if ((uint)p >= (uint)_checks.Length || _checks[p] != (uint)b)
                    return -1;

                b = _bases[p];
            }

            // 終端チェック
            {
                int p = b;
                if ((uint)p < (uint)_bases.Length)
                {
                    int n = _bases[p];
                    if (b == (int)_checks[p] && n < 0)
                        return -n - 1;
                }
            }

            return -1;
        }
    }
}
