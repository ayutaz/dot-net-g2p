using System;
using System.IO;
using System.Text;

namespace DotNetG2P.MeCab.Dictionary
{
    /// <summary>
    /// MeCab辞書ファイルの72バイトヘッダを解析する。
    /// </summary>
    public sealed class DictionaryHeader
    {
        private const uint MagicXor = 0xef718f77u;
        private const uint ExpectedVersion = 0x66; // UTF-8

        /// <summary>ファイルサイズ XOR 0xef718f77</summary>
        public uint Magic { get; }

        /// <summary>辞書バージョン (0x66 = UTF-8)</summary>
        public uint Version { get; }

        /// <summary>辞書タイプ (0=sys, 1=usr, 2=unk)</summary>
        public uint Type { get; }

        /// <summary>語彙サイズ</summary>
        public uint LexiconSize { get; }

        /// <summary>左文脈サイズ</summary>
        public uint LeftContextSize { get; }

        /// <summary>右文脈サイズ</summary>
        public uint RightContextSize { get; }

        /// <summary>DoubleArrayデータサイズ (バイト)</summary>
        public uint DoubleArraySize { get; }

        /// <summary>トークンデータサイズ (バイト)</summary>
        public uint TokenSize { get; }

        /// <summary>素性データサイズ (バイト)</summary>
        public uint FeatureSize { get; }

        /// <summary>予約フィールド</summary>
        public uint Reserved { get; }

        /// <summary>文字コード名</summary>
        public string Charset { get; }

        private DictionaryHeader(
            uint magic, uint version, uint type, uint lexiconSize,
            uint leftContextSize, uint rightContextSize,
            uint doubleArraySize, uint tokenSize, uint featureSize,
            uint reserved, string charset)
        {
            Magic = magic;
            Version = version;
            Type = type;
            LexiconSize = lexiconSize;
            LeftContextSize = leftContextSize;
            RightContextSize = rightContextSize;
            DoubleArraySize = doubleArraySize;
            TokenSize = tokenSize;
            FeatureSize = featureSize;
            Reserved = reserved;
            Charset = charset;
        }

        /// <summary>
        /// BinaryReaderから72バイトのヘッダを読み取る。
        /// </summary>
        public static DictionaryHeader Read(BinaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            uint magic = reader.ReadUInt32();
            uint version = reader.ReadUInt32();

            if (version != ExpectedVersion)
                throw new InvalidDataException(
                    $"辞書バージョンが不正です。期待値: 0x{ExpectedVersion:X2}, 実際: 0x{version:X2}。UTF-8辞書のみサポートしています。");

            uint type = reader.ReadUInt32();
            uint lexiconSize = reader.ReadUInt32();
            uint leftContextSize = reader.ReadUInt32();
            uint rightContextSize = reader.ReadUInt32();
            uint doubleArraySize = reader.ReadUInt32();
            uint tokenSize = reader.ReadUInt32();
            uint featureSize = reader.ReadUInt32();
            uint reserved = reader.ReadUInt32();

            byte[] charsetBytes = reader.ReadBytes(32);
            int nullIndex = Array.IndexOf(charsetBytes, (byte)0);
            string charset = nullIndex >= 0
                ? Encoding.ASCII.GetString(charsetBytes, 0, nullIndex)
                : Encoding.ASCII.GetString(charsetBytes);

            if (charset.Length > 0 &&
                !charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase) &&
                !charset.Equals("utf8", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"辞書のCharsetがUTF-8ではありません: '{charset}'。UTF-8辞書のみサポートしています。");
            }

            return new DictionaryHeader(
                magic, version, type, lexiconSize,
                leftContextSize, rightContextSize,
                doubleArraySize, tokenSize, featureSize,
                reserved, charset);
        }

        /// <summary>
        /// Magicフィールドを使ってファイルサイズの整合性を検証する。
        /// </summary>
        /// <param name="fileSize">実際のファイルサイズ (バイト)</param>
        /// <returns>検証が成功した場合true</returns>
        public bool ValidateMagic(long fileSize)
        {
            uint expected = (uint)(fileSize ^ MagicXor);
            return Magic == expected;
        }

        /// <summary>
        /// Magicフィールドを使ってファイルサイズの整合性を検証し、失敗時に例外を投げる。
        /// </summary>
        /// <param name="fileSize">実際のファイルサイズ (バイト)</param>
        public void ThrowIfInvalidMagic(long fileSize)
        {
            if (!ValidateMagic(fileSize))
                throw new InvalidDataException(
                    $"辞書ファイルのmagicが不正です。ファイルが破損しているか、MeCab辞書ではない可能性があります。");
        }
    }
}
