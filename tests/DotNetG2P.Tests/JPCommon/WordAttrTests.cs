using DotNetG2P.JPCommon;
using DotNetG2P.Models;

namespace DotNetG2P.Tests.JPCommon
{
    public class WordAttrTests
    {
        // ====== PosToId (POSオブジェクト経由) ======

        [Fact]
        public void PosToId_名詞一般_Returns39()
        {
            var pos = new POS(POSType.Meishi, "一般");
            Assert.Equal(39, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_動詞自立_Returns32()
        {
            var pos = new POS(POSType.Doushi, "自立");
            Assert.Equal(32, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_動詞非自立_Returns34()
        {
            var pos = new POS(POSType.Doushi, "非自立");
            Assert.Equal(34, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_助詞格助詞一般_Returns14()
        {
            var pos = new POS(POSType.Joshi, "格助詞", "一般");
            Assert.Equal(14, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_助詞係助詞_Returns17()
        {
            var pos = new POS(POSType.Joshi, "係助詞");
            Assert.Equal(17, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_助詞終助詞_Returns18()
        {
            var pos = new POS(POSType.Joshi, "終助詞");
            Assert.Equal(18, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_助詞接続助詞_Returns19()
        {
            var pos = new POS(POSType.Joshi, "接続助詞");
            Assert.Equal(19, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_助動詞_Returns26()
        {
            var pos = new POS(POSType.Jodoushi);
            Assert.Equal(26, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_感動詞_Returns3()
        {
            var pos = new POS(POSType.Kandoushi);
            Assert.Equal(3, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_形容詞自立_Returns11()
        {
            var pos = new POS(POSType.Keiyoushi, "自立");
            Assert.Equal(11, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_接続詞_Returns27()
        {
            var pos = new POS(POSType.Setsuzokushi);
            Assert.Equal(27, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_接頭詞名詞接続_Returns31()
        {
            var pos = new POS(POSType.Settoushi, "名詞接続");
            Assert.Equal(31, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞サ変接続_Returns37()
        {
            var pos = new POS(POSType.Meishi, "サ変接続");
            Assert.Equal(37, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞固有名詞一般_Returns42()
        {
            var pos = new POS(POSType.Meishi, "固有名詞", "一般");
            Assert.Equal(42, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞数_Returns49()
        {
            var pos = new POS(POSType.Meishi, "数");
            Assert.Equal(49, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞非自立一般_Returns64()
        {
            var pos = new POS(POSType.Meishi, "非自立", "一般");
            Assert.Equal(64, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞代名詞一般_Returns60()
        {
            var pos = new POS(POSType.Meishi, "代名詞", "一般");
            Assert.Equal(60, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_名詞接尾一般_Returns52()
        {
            var pos = new POS(POSType.Meishi, "接尾", "一般");
            Assert.Equal(52, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_副詞一般_Returns35()
        {
            var pos = new POS(POSType.Fukushi, "一般");
            Assert.Equal(35, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_連体詞_Returns69()
        {
            var pos = new POS(POSType.Rentaishi);
            Assert.Equal(69, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_フィラー_Returns2()
        {
            var pos = new POS(POSType.Filler);
            Assert.Equal(2, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_記号一般_Returns5()
        {
            var pos = new POS(POSType.Kigou, "一般");
            Assert.Equal(5, WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_Unknown_ReturnsNull()
        {
            var pos = new POS(POSType.Unknown);
            Assert.Null(WordAttr.PosToId(pos));
        }

        [Fact]
        public void PosToId_Null_ReturnsNull()
        {
            Assert.Null(WordAttr.PosToId(null));
        }

        // ====== GetPosId (文字列直接指定) ======

        [Theory]
        [InlineData("名詞", "一般", "*", "*", 39)]
        [InlineData("動詞", "自立", "*", "*", 32)]
        [InlineData("助詞", "格助詞", "一般", "*", 14)]
        [InlineData("記号", "句点", "*", "*", 8)]
        [InlineData("名詞", "固有名詞", "人名", "姓", 44)]
        public void GetPosId_正常変換(string pos, string sub1, string sub2, string sub3, int expected)
        {
            Assert.Equal(expected, WordAttr.GetPosId(pos, sub1, sub2, sub3));
        }

        [Fact]
        public void GetPosId_未知の組み合わせ_ReturnsNull()
        {
            Assert.Null(WordAttr.GetPosId("未知語", "*", "*", "*"));
        }

        // ====== CTypeToId / GetCTypeId ======

        [Theory]
        [InlineData("五段・カ行促音便", 21)]
        [InlineData("一段", 7)]
        [InlineData("サ変・スル", 5)]
        [InlineData("カ変・クル", 1)]
        [InlineData("形容詞・イ段", 18)]
        [InlineData("特殊・マス", 47)]
        [InlineData("不変化型", 49)]
        public void CTypeToId_正常変換(string ctype, int expected)
        {
            Assert.Equal(expected, WordAttr.CTypeToId(ctype));
        }

        [Theory]
        [InlineData("*")]
        [InlineData(null)]
        [InlineData("")]
        public void CTypeToId_該当なし_ReturnsNull(string? ctype)
        {
            Assert.Null(WordAttr.CTypeToId(ctype));
        }

        [Fact]
        public void CTypeToId_未知の活用型_ReturnsNull()
        {
            Assert.Null(WordAttr.CTypeToId("存在しない活用型"));
        }

        // ====== CFormToId / GetCFormId ======

        [Theory]
        [InlineData("連用形", 25)]
        [InlineData("連用タ接続", 21)]
        [InlineData("基本形", 5)]
        [InlineData("未然形", 18)]
        [InlineData("仮定形", 2)]
        [InlineData("命令ｅ", 11)]
        [InlineData("体言接続", 8)]
        [InlineData("ガル接続", 1)]
        public void CFormToId_正常変換(string cform, int expected)
        {
            Assert.Equal(expected, WordAttr.CFormToId(cform));
        }

        [Theory]
        [InlineData("*")]
        [InlineData(null)]
        [InlineData("")]
        public void CFormToId_該当なし_ReturnsNull(string? cform)
        {
            Assert.Null(WordAttr.CFormToId(cform));
        }

        [Fact]
        public void CFormToId_未知の活用形_ReturnsNull()
        {
            Assert.Null(WordAttr.CFormToId("存在しない活用形"));
        }

        // ====== FormatPosId / FormatId ======

        [Theory]
        [InlineData(1, "01")]
        [InlineData(39, "39")]
        [InlineData(null, "xx")]
        public void FormatPosId_正常フォーマット(int? id, string expected)
        {
            Assert.Equal(expected, WordAttr.FormatPosId(id));
        }

        [Theory]
        [InlineData(1, "1")]
        [InlineData(25, "25")]
        [InlineData(null, "xx")]
        public void FormatId_正常フォーマット(int? id, string expected)
        {
            Assert.Equal(expected, WordAttr.FormatId(id));
        }
    }
}
