using System;
using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// PinyinCharDictionary の単体テスト。
    /// 埋め込み辞書の読み込み、TryLookup/TryLookupAll、多音字、エッジケースを検証する。
    /// </summary>
    public class PinyinCharDictionaryTests
    {
        private readonly PinyinCharDictionary _dict;

        public PinyinCharDictionaryTests()
        {
            _dict = PinyinCharDictionary.LoadEmbedded();
        }

        // =====================================================================
        // LoadEmbedded テスト
        // =====================================================================

        [Fact]
        public void LoadEmbedded_辞書エントリ数が40000以上()
        {
            Assert.True(_dict.Count > 40000,
                $"辞書エントリ数が40000未満: {_dict.Count}");
        }

        [Fact]
        public void LoadEmbedded_辞書がnullでない()
        {
            Assert.NotNull(_dict);
        }

        // =====================================================================
        // TryLookup テスト
        // =====================================================================

        [Fact]
        public void TryLookup_中_zhong()
        {
            var found = _dict.TryLookup(0x4E2D, out var pinyin);

            Assert.True(found);
            Assert.Equal("zhōng", pinyin);
        }

        [Fact]
        public void TryLookup_你_ni()
        {
            var found = _dict.TryLookup(0x4F60, out var pinyin);

            Assert.True(found);
            Assert.Equal("nǐ", pinyin);
        }

        [Fact]
        public void TryLookup_国_guo()
        {
            var found = _dict.TryLookup(0x56FD, out var pinyin);

            Assert.True(found);
            Assert.Equal("guó", pinyin);
        }

        [Fact]
        public void TryLookup_非漢字_false()
        {
            var found = _dict.TryLookup(0x0041, out _); // 'A'

            Assert.False(found);
        }

        [Fact]
        public void TryLookup_CJK範囲外_false()
        {
            var found = _dict.TryLookup(0x0030, out _); // '0'

            Assert.False(found);
        }

        // =====================================================================
        // TryLookupAll テスト（多音字）
        // =====================================================================

        [Fact]
        public void TryLookupAll_中_多音字で複数候補()
        {
            var found = _dict.TryLookupAll(0x4E2D, out var pinyins);

            Assert.True(found);
            Assert.True(pinyins.Length >= 2,
                $"'中' のピンイン候補数が2未満: {pinyins.Length}");
            Assert.Equal("zhōng", pinyins[0]);
            Assert.Contains("zhòng", pinyins);
        }

        [Fact]
        public void TryLookupAll_好_多音字()
        {
            var found = _dict.TryLookupAll(0x597D, out var pinyins);

            Assert.True(found);
            Assert.True(pinyins.Length >= 1);
            Assert.Equal("hǎo", pinyins[0]);
        }

        [Fact]
        public void TryLookupAll_非漢字_false_空配列()
        {
            var found = _dict.TryLookupAll(0x0041, out var pinyins); // 'A'

            Assert.False(found);
            Assert.Empty(pinyins);
        }

        // =====================================================================
        // 追加の漢字ルックアップテスト
        // =====================================================================

        [Fact]
        public void TryLookup_一_yi()
        {
            var found = _dict.TryLookup(0x4E00, out var pinyin);

            Assert.True(found);
            Assert.Equal("yī", pinyin);
        }

        [Fact]
        public void TryLookup_世_shi()
        {
            var found = _dict.TryLookup(0x4E16, out var pinyin);

            Assert.True(found);
            Assert.Equal("shì", pinyin);
        }

        [Fact]
        public void TryLookup_界_jie()
        {
            var found = _dict.TryLookup(0x754C, out var pinyin);

            Assert.True(found);
            Assert.Equal("jiè", pinyin);
        }
    }
}
