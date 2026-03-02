using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab未知語辞書 (unk.dic) を読み込む。
    /// SystemDictionaryと同形式 (type=2) だが、カテゴリインデックスからトークン候補へのマッピングを提供する。
    /// </summary>
    public sealed class UnknownDictionary
    {
        /// <summary>辞書ヘッダ</summary>
        public DictionaryHeader Header { get; }

        /// <summary>トークンバッファ</summary>
        public byte[] TokenData { get; }

        /// <summary>素性文字列バッファ (UTF-8, null終端)</summary>
        public byte[] FeatureData { get; }

        /// <summary>
        /// カテゴリインデックスごとのトークン情報。
        /// _categoryTokens[categoryIndex] = (startPosition, count)
        /// </summary>
        private readonly (int startPosition, int count)[] _categoryTokens;

        private UnknownDictionary(
            DictionaryHeader header,
            byte[] tokenData,
            byte[] featureData,
            (int startPosition, int count)[] categoryTokens)
        {
            Header = header;
            TokenData = tokenData;
            FeatureData = featureData;
            _categoryTokens = categoryTokens;
        }

        /// <summary>
        /// 指定カテゴリインデックスに対応するトークン数を取得する。
        /// </summary>
        public int GetTokenCount(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= _categoryTokens.Length)
                return 0;
            return _categoryTokens[categoryIndex].count;
        }

        /// <summary>
        /// 指定カテゴリインデックスの指定番目のトークンを取得する。
        /// </summary>
        public DicToken GetToken(int categoryIndex, int index)
        {
            if (categoryIndex < 0 || categoryIndex >= _categoryTokens.Length)
                throw new ArgumentOutOfRangeException(nameof(categoryIndex));

            var (startPosition, count) = _categoryTokens[categoryIndex];
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            int byteOffset = (startPosition + index) * DicToken.ByteSize;
            return DicToken.Read(TokenData, byteOffset);
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
        /// unk.dicファイルを読み込む。
        /// CharPropertyからカテゴリ名を取得し、TrieDataを使ってカテゴリ→トークンのマッピングを構築する。
        /// </summary>
        /// <param name="filePath">unk.dicファイルパス</param>
        /// <param name="charProperty">文字種プロパティ (カテゴリ名取得用)</param>
        public static UnknownDictionary Load(string filePath, CharProperty charProperty)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (charProperty == null) throw new ArgumentNullException(nameof(charProperty));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"未知語辞書ファイルが見つかりません: {filePath}", filePath);

            long fileSize = new FileInfo(filePath).Length;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // 1. ヘッダ読み込み
            var header = DictionaryHeader.Read(reader);
            header.ThrowIfInvalidMagic(fileSize);

            // 2. 各セクション読み込み
            byte[] trieData = ReadExact(reader, (int)header.DoubleArraySize, "DoubleArray");
            byte[] tokenData = ReadExact(reader, (int)header.TokenSize, "Token");
            byte[] featureData = ReadExact(reader, (int)header.FeatureSize, "Feature");

            // 3. Trieからカテゴリ名→トークン位置のマッピングを構築
            //    unk.dicのTrieにはカテゴリ名がキーとして格納されている。
            //    各カテゴリ名でTrieを検索し、対応するトークン情報を取得する。
            var categoryTokens = BuildCategoryTokenMap(trieData, charProperty);

            return new UnknownDictionary(header, tokenData, featureData, categoryTokens);
        }

        /// <summary>
        /// TrieDataからカテゴリ名→トークン位置のマッピングを構築する。
        /// unk.dicのTrieはDoubleArray形式で、キーはカテゴリ名(ASCII)。
        /// </summary>
        private static (int startPosition, int count)[] BuildCategoryTokenMap(
            byte[] trieData, CharProperty charProperty)
        {
            int categoryCount = charProperty.CategoryCount;
            var result = new (int startPosition, int count)[categoryCount];

            // DoubleArrayの各ユニットは8バイト (base: 4, check: 4)
            // 検索: カテゴリ名の各文字でTrieを走査
            for (int catIdx = 0; catIdx < categoryCount; catIdx++)
            {
                string categoryName = charProperty.GetCategoryName(catIdx);
                int value = SearchTrie(trieData, categoryName);

                if (value >= 0)
                {
                    // NMeCab方式: value下位8ビット = トークン数, value >> 8 = 開始位置
                    int count = value & 0xFF;
                    int startPosition = value >> 8;
                    result[catIdx] = (startPosition, count);
                }
                // valueが見つからない場合は(0, 0)のまま
            }

            return result;
        }

        /// <summary>
        /// DoubleArrayTrieからキー文字列を完全一致検索し、対応するvalueを返す。
        /// NMeCabのExactMatchSearchと同じアルゴリズム。
        /// </summary>
        /// <param name="trieData">DoubleArrayの生バイト列 (各ユニット8バイト: base[4] + check[4])</param>
        /// <param name="key">検索キー (ASCII文字列、カテゴリ名)</param>
        /// <returns>見つかった場合はvalue、見つからない場合は-1</returns>
        private static int SearchTrie(byte[] trieData, string key)
        {

            // NMeCab方式のDoubleArray検索:
            // 遷移: p = base[current] + key[i] + 1
            // チェック: check[p] == base[current]
            // 終端: base[p] < 0 → value = -base[p] - 1

            int b = GetBase(trieData, 0); // ルートノードのBase値

            // キーの各バイトで遷移
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            for (int i = 0; i < keyBytes.Length; i++)
            {
                int p = b + keyBytes[i] + 1;

                if (p < 0 || p * 8 + 8 > trieData.Length)
                    return -1;

                uint checkVal = GetCheck(trieData, p);
                if (checkVal != (uint)b)
                    return -1;

                b = GetBase(trieData, p);
            }

            // 終端チェック: ノード自体がリーフか確認
            {
                int p = b;
                if (p < 0 || p * 8 + 8 > trieData.Length)
                    return -1;

                int n = GetBase(trieData, p);
                uint checkVal = GetCheck(trieData, p);
                if ((uint)b == checkVal && n < 0)
                {
                    return -n - 1;
                }
            }

            return -1;
        }

        private static int GetBase(byte[] data, int nodeIndex)
        {
            int offset = nodeIndex * 8;
            return BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
        }

        private static uint GetCheck(byte[] data, int nodeIndex)
        {
            int offset = nodeIndex * 8 + 4;
            return BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
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
