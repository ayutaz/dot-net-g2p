using System;
using System.IO;
using System.Text;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCabシステム辞書 (sys.dic) を読み込み、Trie・トークン・素性データへのアクセスを提供する。
    /// </summary>
    public sealed class SystemDictionary
    {
        /// <summary>辞書ヘッダ</summary>
        public DictionaryHeader Header { get; }

        /// <summary>DoubleArrayTrieの生バイト列</summary>
        public byte[] TrieData { get; }

        /// <summary>トークンバッファ</summary>
        public byte[] TokenData { get; }

        /// <summary>素性文字列バッファ (UTF-8, null終端)</summary>
        public byte[] FeatureData { get; }

        private SystemDictionary(DictionaryHeader header, byte[] trieData, byte[] tokenData, byte[] featureData)
        {
            Header = header;
            TrieData = trieData;
            TokenData = tokenData;
            FeatureData = featureData;
        }

        /// <summary>
        /// トークンバッファから指定位置のトークンを取得する。
        /// NMeCab方式: Trieの検索結果value → 下位8ビット=トークン数, value >> 8 = トークン開始位置(バイト単位ではなくトークンインデックス)。
        /// </summary>
        /// <param name="position">トークン開始位置 (value >> 8)</param>
        /// <param name="index">トークンインデックス (0 .. count-1)</param>
        public DicToken GetToken(int position, int index)
        {
            long byteOffset = ((long)position + index) * DicToken.ByteSize;
            if (byteOffset < 0 || byteOffset > TokenData.Length - DicToken.ByteSize)
                throw new ArgumentOutOfRangeException(
                    $"トークンオフセットが範囲外です: position={position}, index={index}");
            return DicToken.Read(TokenData, (int)byteOffset);
        }

        /// <summary>
        /// 素性バッファから指定オフセットのUTF-8 null終端文字列を読み取る。
        /// </summary>
        public string GetFeature(uint offset)
        {
            if (offset >= FeatureData.Length)
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"素性オフセットが範囲外です: offset={offset}, バッファ長={FeatureData.Length}");

            int start = (int)offset;
            int end = start;
            while (end < FeatureData.Length && FeatureData[end] != 0)
            {
                end++;
            }

            return Encoding.UTF8.GetString(FeatureData, start, end - start);
        }

        /// <summary>
        /// 辞書ファイルを読み込む。
        /// </summary>
        /// <param name="filePath">辞書ファイルパス (sys.dic)</param>
        public static SystemDictionary Load(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"辞書ファイルが見つかりません: {filePath}", filePath);

            long fileSize = new FileInfo(filePath).Length;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // 1. ヘッダ (72バイト) 読み込み
            var header = DictionaryHeader.Read(reader);

            // 2. Magic検証
            header.ThrowIfInvalidMagic(fileSize);

            // 3. 各セクション読み込み
            byte[] trieData = ReadExact(reader, (int)header.DoubleArraySize, "DoubleArray");
            byte[] tokenData = ReadExact(reader, (int)header.TokenSize, "Token");
            byte[] featureData = ReadExact(reader, (int)header.FeatureSize, "Feature");

            return new SystemDictionary(header, trieData, tokenData, featureData);
        }

        private static byte[] ReadExact(BinaryReader reader, int count, string sectionName)
        {
            byte[] data = reader.ReadBytes(count);
            if (data.Length != count)
                throw new InvalidDataException(
                    $"辞書ファイルが途中で切れています。{sectionName}セクション: 期待={count}バイト, 実際={data.Length}バイト");
            return data;
        }
    }
}
