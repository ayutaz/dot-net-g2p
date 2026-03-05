using DotNetG2P.English;
using Xunit;

namespace DotNetG2P.Tests.EnglishG2P.Dictionary
{
    /// <summary>
    /// CmuDictionary のバリアント（複数発音）テスト。
    /// </summary>
    public class CmuDictVariantTests
    {
        private static readonly CmuDictionary Dict = CmuDictionary.LoadEmbedded();

        [Fact]
        public void Lead_HasMultipleVariants()
        {
            Assert.True(Dict.TryLookup("lead", out var prons));
            Assert.True(prons.Length >= 2, $"leadは2バリアント以上を期待: 実際={prons.Length}");
        }

        [Fact]
        public void Lead_FirstVariant_IsLEH1D()
        {
            Assert.True(Dict.TryLookup("lead", out var prons));
            Assert.Equal("L EH1 D", prons[0].ToString());
        }

        [Fact]
        public void Lead_SecondVariant_IsLIY1D()
        {
            Assert.True(Dict.TryLookup("lead", out var prons));
            Assert.Equal("L IY1 D", prons[1].ToString());
        }

        [Fact]
        public void Read_HasMultipleVariants()
        {
            Assert.True(Dict.TryLookup("read", out var prons));
            Assert.True(prons.Length >= 2, $"readは2バリアント以上を期待: 実際={prons.Length}");
        }

        [Fact]
        public void Read_VariantsAreDifferent()
        {
            Assert.True(Dict.TryLookup("read", out var prons));
            Assert.NotEqual(prons[0].ToString(), prons[1].ToString());
        }

        [Fact]
        public void Close_HasMultipleVariants()
        {
            Assert.True(Dict.TryLookup("close", out var prons));
            Assert.True(prons.Length >= 2, $"closeは2バリアント以上を期待: 実際={prons.Length}");
        }

        [Fact]
        public void A_HasMultipleVariants()
        {
            Assert.True(Dict.TryLookup("a", out var prons));
            Assert.True(prons.Length >= 2, $"aは2バリアント以上を期待: 実際={prons.Length}");
        }

        [Fact]
        public void A_FirstVariant_IsMainEntry()
        {
            // 主エントリ（バリアント番号なし）が配列の先頭
            Assert.True(Dict.TryLookup("a", out var prons));
            Assert.Equal("AH0", prons[0].ToString());
        }

        [Fact]
        public void A_SecondVariant_IsEY1()
        {
            Assert.True(Dict.TryLookup("a", out var prons));
            Assert.Equal("EY1", prons[1].ToString());
        }

        [Fact]
        public void SingleVariantWord_HasExactlyOneEntry()
        {
            // "world" は1バリアントのみ
            Assert.True(Dict.TryLookup("world", out var prons));
            Assert.Single(prons);
        }
    }
}
