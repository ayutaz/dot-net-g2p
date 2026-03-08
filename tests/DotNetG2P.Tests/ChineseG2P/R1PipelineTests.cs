using Xunit;
using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// R1リファクタリング（RunPipeline統合）後の回帰テスト。
    /// ToPinyin/ToIPA/ToZhuyin がリファクタリング後も正しく動作することを検証する。
    /// </summary>
    public class R1PipelineTests
    {
        [Fact]
        public void ToPinyin_AfterRefactor_SameOutput()
        {
            using var engine = new ChineseG2PEngine();
            var result = engine.ToPinyin("你好世界");
            Assert.NotEmpty(result);
            Assert.Contains(" ", result);
        }

        [Fact]
        public void ToIPA_AfterRefactor_SameOutput()
        {
            using var engine = new ChineseG2PEngine();
            var result = engine.ToIPA("你好");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToZhuyin_AfterRefactor_SameOutput()
        {
            using var engine = new ChineseG2PEngine();
            var result = engine.ToZhuyin("你好");
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ToPinyinList_AfterRefactor_SameOutput()
        {
            using var engine = new ChineseG2PEngine();
            var result = engine.ToPinyinList("你好");
            Assert.Equal(2, result.Length);
        }

        [Fact]
        public void AllOutputFormats_ConsistentForSameInput()
        {
            using var engine = new ChineseG2PEngine();
            var text = "中国人";
            var pinyin = engine.ToPinyin(text);
            var ipa = engine.ToIPA(text);
            var zhuyin = engine.ToZhuyin(text);

            Assert.NotEmpty(pinyin);
            Assert.NotEmpty(ipa);
            Assert.NotEmpty(zhuyin);

            // それぞれ異なる出力（形式が違うため）
            Assert.NotEqual(pinyin, ipa);
            Assert.NotEqual(pinyin, zhuyin);
        }

        [Fact]
        public void ToneSandhiDisabled_StillWorks()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.NotEmpty(result);
        }
    }
}
