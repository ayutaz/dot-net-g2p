using System;
using System.IO;
using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinPhraseDictionary の単体テスト。
    /// 埋め込み辞書の読み込み、TryLookup、FindLongestMatch、エッジケースを検証する。
    /// </summary>
    public class PinyinPhraseDictionaryTests
    {
        private readonly PinyinPhraseDictionary _dict;

        public PinyinPhraseDictionaryTests()
        {
            _dict = PinyinPhraseDictionary.LoadEmbedded();
        }

        // =====================================================================
        // LoadEmbedded テスト
        // =====================================================================

        [Fact]
        public void LoadEmbedded_辞書がnullでない()
        {
            Assert.NotNull(_dict);
        }

        [Fact]
        public void LoadEmbedded_エントリ数が0より大きい()
        {
            Assert.True(_dict.Count > 0,
                $"辞書エントリ数が0: {_dict.Count}");
        }

        [Fact]
        public void LoadEmbedded_エントリ数が100000以上()
        {
            Assert.True(_dict.Count >= 100000,
                $"辞書エントリ数が100000未満: {_dict.Count}");
        }

        // =====================================================================
        // TryLookup テスト
        // =====================================================================

        [Fact]
        public void TryLookup_上海_存在する()
        {
            var found = _dict.TryLookup("上海", out var pinyins);

            Assert.True(found);
            Assert.Equal(2, pinyins.Length);
        }

        [Fact]
        public void TryLookup_重要_存在する()
        {
            var found = _dict.TryLookup("重要", out var pinyins);

            Assert.True(found);
            Assert.Equal(2, pinyins.Length);
        }

        [Fact]
        public void TryLookup_中国_存在する()
        {
            var found = _dict.TryLookup("中国", out var pinyins);

            Assert.True(found);
            Assert.Equal(2, pinyins.Length);
        }

        [Fact]
        public void TryLookup_你好_存在する()
        {
            var found = _dict.TryLookup("你好", out var pinyins);

            Assert.True(found);
            Assert.Equal(2, pinyins.Length);
        }

        [Fact]
        public void TryLookup_存在しないフレーズ_false()
        {
            var found = _dict.TryLookup("鑫鑫鑫鑫", out var pinyins);

            Assert.False(found);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void TryLookup_空文字列_false()
        {
            var found = _dict.TryLookup("", out var pinyins);

            Assert.False(found);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void TryLookup_単一文字_false()
        {
            // フレーズ辞書は2文字以上のフレーズのみ格納する
            var found = _dict.TryLookup("中", out var pinyins);

            Assert.False(found);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void TryLookup_ピンイン配列の文字数がフレーズ文字数と一致()
        {
            var found = _dict.TryLookup("中国", out var pinyins);

            Assert.True(found);
            // "中国" は2文字なのでピンインも2要素
            Assert.Equal("中国".Length, pinyins.Length);
        }

        [Fact]
        public void TryLookup_三文字フレーズ_ピンイン3要素()
        {
            // 三文字以上のフレーズを検証
            var found = _dict.TryLookup("中国人", out var pinyins);

            if (found)
            {
                Assert.Equal(3, pinyins.Length);
            }
            // 辞書に含まれない可能性もあるため、見つからなくてもOK
        }

        // =====================================================================
        // FindLongestMatch テスト
        // =====================================================================

        [Fact]
        public void FindLongestMatch_テキスト先頭からマッチ()
        {
            // "中国人民" のうち最長マッチを確認
            var text = "中国人民";
            var matchLen = _dict.FindLongestMatch(text, 0, out var pinyins);

            Assert.True(matchLen >= 2,
                $"最長一致が2文字未満: {matchLen}");
            Assert.Equal(matchLen, pinyins.Length);
        }

        [Fact]
        public void FindLongestMatch_テキスト途中からマッチ()
        {
            // startIndex=1 から検索
            var text = "我中国好";
            var matchLen = _dict.FindLongestMatch(text, 1, out var pinyins);

            if (matchLen > 0)
            {
                Assert.True(matchLen >= 2);
                Assert.Equal(matchLen, pinyins.Length);
            }
        }

        [Fact]
        public void FindLongestMatch_マッチなし_0を返す()
        {
            var text = "ABCDE";
            var matchLen = _dict.FindLongestMatch(text, 0, out var pinyins);

            Assert.Equal(0, matchLen);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void FindLongestMatch_startIndexがテキスト末尾付近_残り1文字()
        {
            // 残り1文字しかない場合、フレーズ最小長2に満たないので0を返す
            var text = "你好中";
            var matchLen = _dict.FindLongestMatch(text, 2, out var pinyins);

            Assert.Equal(0, matchLen);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void FindLongestMatch_startIndexがテキスト長と同じ()
        {
            var text = "你好";
            var matchLen = _dict.FindLongestMatch(text, 2, out var pinyins);

            Assert.Equal(0, matchLen);
            Assert.Empty(pinyins);
        }

        [Fact]
        public void FindLongestMatch_最長一致が短い一致より優先される()
        {
            // "中国人" が辞書にあれば "中国" より優先される
            var found2 = _dict.TryLookup("中国", out _);
            var found3 = _dict.TryLookup("中国人", out _);

            if (found2 && found3)
            {
                var text = "中国人好";
                var matchLen = _dict.FindLongestMatch(text, 0, out _);

                Assert.True(matchLen >= 3,
                    "最長一致で3文字フレーズが優先されるべき");
            }
        }

        // =====================================================================
        // Clear テスト
        // =====================================================================

        [Fact]
        public void Clear_Count0になる()
        {
            // Clearテスト用に別インスタンスを作成
            var dict = PinyinPhraseDictionary.LoadEmbedded();
            Assert.True(dict.Count > 0);

            dict.Clear();

            Assert.Equal(0, dict.Count);
        }

        [Fact]
        public void Clear_後にTryLookupがfalse()
        {
            var dict = PinyinPhraseDictionary.LoadEmbedded();
            Assert.True(dict.TryLookup("中国", out _));

            dict.Clear();

            Assert.False(dict.TryLookup("中国", out var pinyins));
            Assert.Empty(pinyins);
        }

        // =====================================================================
        // LoadFromFile テスト
        // =====================================================================

        [Fact]
        public void LoadFromFile_nullパス_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PinyinPhraseDictionary.LoadFromFile(null!));
        }

        [Fact]
        public void LoadFromFile_空文字列パス_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PinyinPhraseDictionary.LoadFromFile(""));
        }

        [Fact]
        public void LoadFromFile_存在しないファイル_FileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() =>
                PinyinPhraseDictionary.LoadFromFile("/nonexistent/path/dict.txt"));
        }
    }
}
