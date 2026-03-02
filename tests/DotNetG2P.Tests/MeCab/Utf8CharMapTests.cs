using System;
using System.Text;
using DotNetG2P.MeCab.Trie;
using Xunit;

namespace DotNetG2P.Tests.MeCab
{
    /// <summary>
    /// Utf8CharMapのユニットテスト（辞書不要）。
    /// UTF-8バイトオフセットとC#文字インデックスの双方向変換を検証する。
    /// </summary>
    public class Utf8CharMapTests
    {
        // =====================================================================
        // 1. ASCII
        // =====================================================================

        [Fact]
        public void ASCII_各文字が1バイト()
        {
            var map = new Utf8CharMap("abc");

            Assert.Equal(3, map.ByteLength);
            Assert.Equal(3, map.CharLength);
        }

        [Fact]
        public void ASCII_ByteToChar_一対一対応()
        {
            var map = new Utf8CharMap("abc");

            Assert.Equal(0, map.ByteToCharIndex(0)); // 'a'
            Assert.Equal(1, map.ByteToCharIndex(1)); // 'b'
            Assert.Equal(2, map.ByteToCharIndex(2)); // 'c'
        }

        [Fact]
        public void ASCII_CharToByte_一対一対応()
        {
            var map = new Utf8CharMap("abc");

            Assert.Equal(0, map.CharToByteIndex(0)); // 'a'
            Assert.Equal(1, map.CharToByteIndex(1)); // 'b'
            Assert.Equal(2, map.CharToByteIndex(2)); // 'c'
        }

        // =====================================================================
        // 2. 日本語（3バイトUTF-8文字）
        // =====================================================================

        [Fact]
        public void 日本語_各文字が3バイト()
        {
            var map = new Utf8CharMap("あいう");

            Assert.Equal(9, map.ByteLength);  // 3文字 * 3バイト
            Assert.Equal(3, map.CharLength);
        }

        [Fact]
        public void 日本語_ByteToChar_3バイト文字の各バイトが同一charIndex()
        {
            var map = new Utf8CharMap("あい");

            // 'あ' = byte[0..2] → charIndex 0
            Assert.Equal(0, map.ByteToCharIndex(0));
            Assert.Equal(0, map.ByteToCharIndex(1));
            Assert.Equal(0, map.ByteToCharIndex(2));

            // 'い' = byte[3..5] → charIndex 1
            Assert.Equal(1, map.ByteToCharIndex(3));
            Assert.Equal(1, map.ByteToCharIndex(4));
            Assert.Equal(1, map.ByteToCharIndex(5));
        }

        [Fact]
        public void 日本語_CharToByte_先頭バイトオフセット()
        {
            var map = new Utf8CharMap("あい");

            Assert.Equal(0, map.CharToByteIndex(0)); // 'あ' → byte 0
            Assert.Equal(3, map.CharToByteIndex(1)); // 'い' → byte 3
        }

        // =====================================================================
        // 3. ASCII + 日本語 混在
        // =====================================================================

        [Fact]
        public void 混在_正しいバイト長()
        {
            // "aあb" = 1 + 3 + 1 = 5 bytes, 3 chars
            var map = new Utf8CharMap("aあb");

            Assert.Equal(5, map.ByteLength);
            Assert.Equal(3, map.CharLength);
        }

        [Fact]
        public void 混在_ByteToChar()
        {
            var map = new Utf8CharMap("aあb");

            Assert.Equal(0, map.ByteToCharIndex(0)); // 'a' → char 0
            Assert.Equal(1, map.ByteToCharIndex(1)); // 'あ' byte0 → char 1
            Assert.Equal(1, map.ByteToCharIndex(2)); // 'あ' byte1 → char 1
            Assert.Equal(1, map.ByteToCharIndex(3)); // 'あ' byte2 → char 1
            Assert.Equal(2, map.ByteToCharIndex(4)); // 'b' → char 2
        }

        [Fact]
        public void 混在_CharToByte()
        {
            var map = new Utf8CharMap("aあb");

            Assert.Equal(0, map.CharToByteIndex(0)); // 'a' → byte 0
            Assert.Equal(1, map.CharToByteIndex(1)); // 'あ' → byte 1
            Assert.Equal(4, map.CharToByteIndex(2)); // 'b' → byte 4
        }

        // =====================================================================
        // 4. サロゲートペア（4バイトUTF-8）
        // =====================================================================

        [Fact]
        public void サロゲートペア_正しいバイト長()
        {
            // U+1F600 (😀) = 4 bytes UTF-8, 2 chars in C# (surrogate pair)
            var text = "\U0001F600";  // 😀
            var map = new Utf8CharMap(text);

            Assert.Equal(4, map.ByteLength);
            Assert.Equal(2, map.CharLength);  // high + low surrogate
        }

        [Fact]
        public void サロゲートペア_ByteToChar_4バイトが同一charIndex()
        {
            var text = "\U0001F600";
            var map = new Utf8CharMap(text);

            Assert.Equal(0, map.ByteToCharIndex(0));
            Assert.Equal(0, map.ByteToCharIndex(1));
            Assert.Equal(0, map.ByteToCharIndex(2));
            Assert.Equal(0, map.ByteToCharIndex(3));
        }

        [Fact]
        public void サロゲートペア_CharToByte_ハイローともに同一バイトオフセット()
        {
            var text = "\U0001F600";
            var map = new Utf8CharMap(text);

            Assert.Equal(0, map.CharToByteIndex(0)); // high surrogate → byte 0
            Assert.Equal(0, map.CharToByteIndex(1)); // low surrogate → byte 0
        }

        [Fact]
        public void サロゲートペア混在_ASCIIとサロゲート()
        {
            // "a😀b" = 1 + 4 + 1 = 6 bytes, 4 chars (a + high + low + b)
            var text = "a\U0001F600b";
            var map = new Utf8CharMap(text);

            Assert.Equal(6, map.ByteLength);
            Assert.Equal(4, map.CharLength);

            Assert.Equal(0, map.CharToByteIndex(0)); // 'a' → byte 0
            Assert.Equal(1, map.CharToByteIndex(1)); // high surrogate → byte 1
            Assert.Equal(1, map.CharToByteIndex(2)); // low surrogate → byte 1
            Assert.Equal(5, map.CharToByteIndex(3)); // 'b' → byte 5
        }

        // =====================================================================
        // 5. ラウンドトリップ
        // =====================================================================

        [Theory]
        [InlineData("hello")]
        [InlineData("こんにちは")]
        [InlineData("Hello世界ABC")]
        [InlineData("漢字カタカナひらがなABC123")]
        public void ラウンドトリップ_CharToByte_ByteToChar(string text)
        {
            var map = new Utf8CharMap(text);

            for (int c = 0; c < text.Length; c++)
            {
                int b = map.CharToByteIndex(c);
                int roundTrip = map.ByteToCharIndex(b);
                Assert.Equal(c, roundTrip);
            }
        }

        // =====================================================================
        // 6. 境界条件
        // =====================================================================

        [Fact]
        public void 単一文字_ASCII()
        {
            var map = new Utf8CharMap("x");

            Assert.Equal(1, map.ByteLength);
            Assert.Equal(1, map.CharLength);
            Assert.Equal(0, map.ByteToCharIndex(0));
            Assert.Equal(0, map.CharToByteIndex(0));
        }

        [Fact]
        public void 単一文字_日本語()
        {
            var map = new Utf8CharMap("あ");

            Assert.Equal(3, map.ByteLength);
            Assert.Equal(1, map.CharLength);
            Assert.Equal(0, map.CharToByteIndex(0));
        }

        [Fact]
        public void 空文字列_バイト長ゼロ()
        {
            var map = new Utf8CharMap("");

            Assert.Equal(0, map.ByteLength);
            Assert.Equal(0, map.CharLength);
        }

        [Fact]
        public void null_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Utf8CharMap(null!));
        }

        [Fact]
        public void ByteToCharIndex_範囲外_例外()
        {
            var map = new Utf8CharMap("ab");

            Assert.Throws<ArgumentOutOfRangeException>(() => map.ByteToCharIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.ByteToCharIndex(2));
        }

        [Fact]
        public void CharToByteIndex_範囲外_例外()
        {
            var map = new Utf8CharMap("ab");

            Assert.Throws<ArgumentOutOfRangeException>(() => map.CharToByteIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.CharToByteIndex(2));
        }

        // =====================================================================
        // 7. 2バイトUTF-8文字
        // =====================================================================

        [Fact]
        public void 二バイト文字_正しいマッピング()
        {
            // U+00E9 (é) = 2 bytes UTF-8
            var map = new Utf8CharMap("\u00E9");

            Assert.Equal(2, map.ByteLength);
            Assert.Equal(1, map.CharLength);
            Assert.Equal(0, map.ByteToCharIndex(0));
            Assert.Equal(0, map.ByteToCharIndex(1));
            Assert.Equal(0, map.CharToByteIndex(0));
        }

        // =====================================================================
        // 8. Utf8Bytesプロパティ
        // =====================================================================

        [Fact]
        public void Utf8Bytes_エンコード結果と一致()
        {
            var text = "テスト";
            var map = new Utf8CharMap(text);
            var expected = Encoding.UTF8.GetBytes(text);

            Assert.Equal(expected, map.Utf8Bytes);
        }

        [Fact]
        public void Text_元のテキストを保持()
        {
            var text = "テスト文字列";
            var map = new Utf8CharMap(text);

            Assert.Equal(text, map.Text);
        }
    }
}
