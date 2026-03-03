// Utf8CharMap.cs - UTF-8バイトオフセット⇔文字インデックスの双方向変換
// サロゲートペア（4バイトUTF-8文字）にも対応

using System;
using System.Buffers;
using System.Text;

namespace DotNetG2P.MeCab.Trie
{
    /// <summary>
    /// テキストをUTF-8にエンコードし、バイトオフセットと文字インデックスの双方向変換を提供する。
    /// MeCab辞書のTrieはUTF-8バイト列で検索するため、
    /// 結果のバイトオフセットをC#のstring charインデックスに変換する必要がある。
    /// 内部バッファにArrayPoolを使用するため、使用後はDisposeすること。
    /// </summary>
    public sealed class Utf8CharMap : IDisposable
    {
        /// <summary>事前エンコード済みUTF-8バイト列</summary>
        public byte[] Utf8Bytes { get; }

        /// <summary>元のテキスト</summary>
        public string Text { get; }

        // バイトオフセット→文字インデックス
        // _byteToChar[byteIndex] = そのバイトが属する文字のcharIndex
        private int[] _byteToChar;

        // 文字インデックス→バイトオフセット
        // _charToByte[charIndex] = その文字の先頭バイトのオフセット
        private int[] _charToByte;

        // ArrayPoolからレンタルした実際のサイズ（Return時に必要）
        private readonly int _byteToCharRentedLength;
        private readonly int _charToByteRentedLength;

        public Utf8CharMap(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Utf8Bytes = Encoding.UTF8.GetBytes(text);

            int byteLen = Utf8Bytes.Length;
            int charLen = text.Length;

            if (byteLen > 0)
            {
                _byteToChar = ArrayPool<int>.Shared.Rent(byteLen);
                _byteToCharRentedLength = _byteToChar.Length;
            }
            else
            {
                _byteToChar = Array.Empty<int>();
                _byteToCharRentedLength = 0;
            }

            if (charLen > 0)
            {
                _charToByte = ArrayPool<int>.Shared.Rent(charLen);
                _charToByteRentedLength = _charToByte.Length;
            }
            else
            {
                _charToByte = Array.Empty<int>();
                _charToByteRentedLength = 0;
            }

            BuildMapping(text);
        }

        private void BuildMapping(string text)
        {
            int charIndex = 0;
            int byteIndex = 0;

            while (charIndex < text.Length)
            {
                _charToByte[charIndex] = byteIndex;

                char c = text[charIndex];

                if (char.IsHighSurrogate(c))
                {
                    // サロゲートペア: C#では2 char、UTF-8では4 bytes
                    _byteToChar[byteIndex] = charIndex;
                    _byteToChar[byteIndex + 1] = charIndex;
                    _byteToChar[byteIndex + 2] = charIndex;
                    _byteToChar[byteIndex + 3] = charIndex;

                    // ローサロゲートも同じバイト位置を指す
                    if (charIndex + 1 < text.Length)
                    {
                        _charToByte[charIndex + 1] = byteIndex;
                    }

                    charIndex += 2;
                    byteIndex += 4;
                }
                else if (c < 0x80)
                {
                    // ASCII: 1 byte
                    _byteToChar[byteIndex] = charIndex;
                    charIndex += 1;
                    byteIndex += 1;
                }
                else if (c < 0x800)
                {
                    // 2 bytes
                    _byteToChar[byteIndex] = charIndex;
                    _byteToChar[byteIndex + 1] = charIndex;
                    charIndex += 1;
                    byteIndex += 2;
                }
                else
                {
                    // 3 bytes (日本語の大半: U+0800〜U+FFFF)
                    _byteToChar[byteIndex] = charIndex;
                    _byteToChar[byteIndex + 1] = charIndex;
                    _byteToChar[byteIndex + 2] = charIndex;
                    charIndex += 1;
                    byteIndex += 3;
                }
            }
        }

        /// <summary>
        /// バイトオフセットから文字インデックスに変換する。
        /// </summary>
        public int ByteToCharIndex(int byteOffset)
        {
            if (byteOffset < 0 || byteOffset >= Utf8Bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteOffset));
            return _byteToChar[byteOffset];
        }

        /// <summary>
        /// 文字インデックスからバイトオフセットに変換する。
        /// </summary>
        public int CharToByteIndex(int charIndex)
        {
            if (charIndex < 0 || charIndex >= Text.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex));
            return _charToByte[charIndex];
        }

        /// <summary>テキストの文字数 (char単位)</summary>
        public int CharLength => Text.Length;

        /// <summary>UTF-8バイト長</summary>
        public int ByteLength => Utf8Bytes.Length;

        /// <summary>
        /// ArrayPoolにレンタルしたバッファを返却する。
        /// </summary>
        public void Dispose()
        {
            if (_byteToChar != null && _byteToCharRentedLength > 0)
            {
                ArrayPool<int>.Shared.Return(_byteToChar, clearArray: false);
                _byteToChar = null;
            }
            if (_charToByte != null && _charToByteRentedLength > 0)
            {
                ArrayPool<int>.Shared.Return(_charToByte, clearArray: false);
                _charToByte = null;
            }
        }
    }
}
