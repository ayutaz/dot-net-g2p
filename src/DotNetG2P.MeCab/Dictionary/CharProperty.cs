using System;
using System.IO;
using System.Text;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab文字種情報。NMeCabのCharInfo構造体に準拠したビットレイアウト。
    /// </summary>
    public readonly struct CharInfo
    {
        private readonly uint _bits;

        /// <summary>生のビット値からCharInfoを構築する。</summary>
        public CharInfo(uint bits) => _bits = bits;

        /// <summary>Invoke: 未知語処理を呼び出すか (bit 31)</summary>
        public bool Invoke => (_bits >> 31) != 0;

        /// <summary>Group: 同種文字をまとめるか (bit 30)</summary>
        public bool Group => ((_bits >> 30) & 1) != 0;

        /// <summary>Length: 未知語最大長 (bits 29-26, 4ビット)</summary>
        public int Length => (int)((_bits >> 26) & 0xF);

        /// <summary>DefaultType: デフォルトカテゴリインデックス (bits 25-18, 8ビット)</summary>
        public int DefaultType => (int)((_bits >> 18) & 0xFF);

        /// <summary>Type: カテゴリ種別ビットマスク (bits 17-0, 18ビット)</summary>
        public uint Type => _bits & 0x3FFFFu;

        /// <summary>
        /// このCharInfoのカテゴリが、指定されたCharInfoのカテゴリに含まれるか判定する。
        /// </summary>
        public bool IsKindOf(CharInfo other) => (Type & other.Type) != 0;

        /// <summary>生のビット値</summary>
        public uint RawBits => _bits;
    }

    /// <summary>
    /// MeCab文字種プロパティ (char.bin) を読み込み、文字からCharInfoへの変換を提供する。
    /// </summary>
    public sealed class CharProperty
    {
        /// <summary>CharInfo配列のエントリ数 (Unicode BMP全体)</summary>
        private const int CharInfoTableSize = 0xFFFF;

        private readonly byte[][] _categoryNames;
        private readonly CharInfo[] _charInfoTable;

        private CharProperty(byte[][] categoryNames, CharInfo[] charInfoTable)
        {
            _categoryNames = categoryNames;
            _charInfoTable = charInfoTable;
        }

        /// <summary>カテゴリ数</summary>
        public int CategoryCount => _categoryNames.Length;

        /// <summary>
        /// 文字に対応するCharInfoを取得する。
        /// </summary>
        /// <remarks>BMP外の文字（サロゲートペア）はデフォルトカテゴリ（DEFAULT: invoke=false, group=false）として扱われます。</remarks>
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public CharInfo GetCharInfo(char c)
        {
            int index = c;
            if (index >= _charInfoTable.Length)
                return default; // BMP外はDEFAULT扱い
            return _charInfoTable[index];
        }

        /// <summary>
        /// カテゴリインデックスからカテゴリ名を取得する。
        /// </summary>
        public string GetCategoryName(int index)
        {
            if (index < 0 || index >= _categoryNames.Length)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"カテゴリインデックスが範囲外です: index={index}, カテゴリ数={_categoryNames.Length}");

            byte[] nameBytes = _categoryNames[index];
            int nullIndex = Array.IndexOf(nameBytes, (byte)0);
            int length = nullIndex >= 0 ? nullIndex : nameBytes.Length;
            return Encoding.ASCII.GetString(nameBytes, 0, length);
        }

        /// <summary>
        /// char.binファイルを読み込む。
        /// </summary>
        /// <param name="filePath">char.binファイルパス</param>
        public static CharProperty Load(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文字種定義ファイルが見つかりません: {filePath}", filePath);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // カテゴリ数
            uint cSize = reader.ReadUInt32();

            // カテゴリ名 (各32バイト)
            var categoryNames = new byte[cSize][];
            for (int i = 0; i < (int)cSize; i++)
            {
                categoryNames[i] = reader.ReadBytes(32);
            }

            // CharInfo配列 (0xFFFF個, 各4バイト) - 一括読み込みで高速化
            var charInfoTable = new CharInfo[CharInfoTableSize];
            int charInfoByteCount = CharInfoTableSize * sizeof(uint);
            byte[] charInfoBytes = reader.ReadBytes(charInfoByteCount);
            if (charInfoBytes.Length != charInfoByteCount)
                throw new InvalidDataException(
                    $"文字種定義データが不足しています: 期待={charInfoByteCount}バイト, 実際={charInfoBytes.Length}バイト");

            if (!BitConverter.IsLittleEndian)
                throw new PlatformNotSupportedException("ビッグエンディアン環境はサポートされていません。");

            // CharInfo は uint 1つだけの readonly struct なのでメモリレイアウトが同一
            Buffer.BlockCopy(charInfoBytes, 0, charInfoTable, 0, charInfoByteCount);

            return new CharProperty(categoryNames, charInfoTable);
        }
    }
}
