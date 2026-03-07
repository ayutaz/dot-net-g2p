using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 多音字（ポリフォン）がフレーズ辞書で正しく解決されるかを検証するテスト。
    /// フレーズ辞書による文脈依存の読み分け、単字フォールバック、HandleHeteronymsオプション制御を網羅する。
    /// </summary>
    public class PolyphoneResolutionTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public PolyphoneResolutionTests()
        {
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 基本多音字解決（フレーズ辞書マッチ）
        // =====================================================================

        [Fact]
        public void ToPinyin_重要_zhongYao()
        {
            // 重: zhòng（"重要"フレーズ辞書） vs chóng
            var result = _engine.ToPinyin("重要");
            Assert.Equal("zhòng yào", result);
        }

        [Fact]
        public void ToPinyin_重复_chongFu()
        {
            // 重: chóng（"重复"フレーズ辞書） vs zhòng
            var result = _engine.ToPinyin("重复");
            Assert.Equal("chóng fù", result);
        }

        [Fact]
        public void ToPinyin_银行_yinHang()
        {
            // 行: háng（"银行"フレーズ辞書） vs xíng
            var result = _engine.ToPinyin("银行");
            Assert.Equal("yín háng", result);
        }

        [Fact]
        public void ToPinyin_行为_xingWei()
        {
            // 行: xíng（"行为"フレーズ辞書） vs háng
            var result = _engine.ToPinyin("行为");
            Assert.Equal("xíng wéi", result);
        }

        [Fact]
        public void ToPinyin_了解_liaoJie()
        {
            // 了: liǎo（"了解"フレーズ辞書） vs le
            var result = _engine.ToPinyin("了解");
            Assert.Equal("liǎo jiě", result);
        }

        [Fact]
        public void ToPinyin_长大_zhangDa()
        {
            // 长: zhǎng（"长大"フレーズ辞書） vs cháng
            var result = _engine.ToPinyin("长大");
            Assert.Equal("zhǎng dà", result);
        }

        [Fact]
        public void ToPinyin_长城_changCheng()
        {
            // 长: cháng（"长城"フレーズ辞書） vs zhǎng
            var result = _engine.ToPinyin("长城");
            Assert.Equal("cháng chéng", result);
        }

        [Fact]
        public void ToPinyin_地方_diFang()
        {
            // 地: dì（"地方"フレーズ辞書） vs de
            var result = _engine.ToPinyin("地方");
            Assert.Equal("dì fāng", result);
        }

        [Fact]
        public void ToPinyin_大夫_daFu()
        {
            // 大: dà（"大夫"フレーズ辞書）
            var result = _engine.ToPinyin("大夫");
            Assert.Equal("dà fū", result);
        }

        [Fact]
        public void ToPinyin_音乐_yinYue()
        {
            // 乐: yuè（"音乐"フレーズ辞書） vs lè
            var result = _engine.ToPinyin("音乐");
            Assert.Equal("yīn yuè", result);
        }

        [Fact]
        public void ToPinyin_快乐_kuaiLe()
        {
            // 乐: lè（"快乐"フレーズ辞書） vs yuè
            var result = _engine.ToPinyin("快乐");
            Assert.Equal("kuài lè", result);
        }

        [Fact]
        public void ToPinyin_还是_haiShi()
        {
            // 还: hái（"还是"フレーズ辞書） vs huán
            var result = _engine.ToPinyin("还是");
            Assert.Equal("hái shì", result);
        }

        [Fact]
        public void ToPinyin_数字_shuZi()
        {
            // 数: shù（"数字"フレーズ辞書） vs shǔ
            var result = _engine.ToPinyin("数字");
            Assert.Equal("shù zì", result);
        }

        [Fact]
        public void ToPinyin_数不清_shuBuQing()
        {
            // 数: shǔ（"数不清"フレーズ辞書） vs shù
            var result = _engine.ToPinyin("数不清");
            Assert.Equal("shǔ bù qīng", result);
        }

        [Fact]
        public void ToPinyin_教室_jiaoShi()
        {
            // 教: jiào（"教室"フレーズ辞書） vs jiāo
            var result = _engine.ToPinyin("教室");
            Assert.Equal("jiào shì", result);
        }

        // =====================================================================
        // 2. 3文字以上のフレーズ
        // =====================================================================

        [Fact]
        public void ToPinyin_三国演义_4文字フレーズ()
        {
            var result = _engine.ToPinyin("三国演义");
            Assert.Equal("sān guó yǎn yì", result);
        }

        [Fact]
        public void ToPinyin_中华人民共和国_7文字フレーズ()
        {
            var result = _engine.ToPinyin("中华人民共和国");
            Assert.Equal("zhōng huá rén mín gòng hé guó", result);
        }

        [Fact]
        public void ToPinyin_图书馆_3文字フレーズ()
        {
            var result = _engine.ToPinyin("图书馆");
            Assert.Equal("tú shū guǎn", result);
        }

        [Fact]
        public void ToPinyin_计算机_3文字フレーズ()
        {
            var result = _engine.ToPinyin("计算机");
            Assert.Equal("jì suàn jī", result);
        }

        [Fact]
        public void ToPinyin_大学生_3文字フレーズ()
        {
            var result = _engine.ToPinyin("大学生");
            Assert.Equal("dà xué shēng", result);
        }

        [Fact]
        public void ToPinyin_北京大学_4文字フレーズ()
        {
            var result = _engine.ToPinyin("北京大学");
            Assert.Equal("běi jīng dà xué", result);
        }

        [Fact]
        public void ToPinyin_共产党_3文字フレーズ()
        {
            var result = _engine.ToPinyin("共产党");
            Assert.Equal("gòng chǎn dǎng", result);
        }

        [Fact]
        public void ToPinyin_全国人大_4文字フレーズ()
        {
            // 大: dà（"全国人大"フレーズ辞書）
            var result = _engine.ToPinyin("全国人大");
            Assert.Equal("quán guó rén dà", result);
        }

        [Fact]
        public void ToPinyin_联合国_3文字フレーズ()
        {
            var result = _engine.ToPinyin("联合国");
            Assert.Equal("lián hé guó", result);
        }

        [Fact]
        public void ToPinyin_自行车_3文字フレーズ多音字()
        {
            // 行: xíng（"自行车"フレーズ辞書） vs háng
            var result = _engine.ToPinyin("自行车");
            Assert.Equal("zì xíng chē", result);
        }

        // =====================================================================
        // 3. HandleHeteronyms無効化テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_HandleHeteronymsFalse_重要_単字最優先読み()
        {
            // HandleHeteronyms=false: フレーズ辞書を使わず単字辞書の最優先読み
            // 重の単字最優先読み: zhòng（pinyin_char: zhòng,chóng,tóng）
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("重要");
            // 単字辞書の最優先読みで処理される
            Assert.Contains("yào", result);
        }

        [Fact]
        public void ToPinyin_HandleHeteronymsFalse_重复_単字最優先読み()
        {
            // HandleHeteronyms=false: 重の最優先読みはzhòng（フレーズ辞書ならchóng）
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("重复");
            // フレーズ辞書なし→重はzhòng（最優先）になる（chóngではない）
            Assert.StartsWith("zhòng", result);
        }

        [Fact]
        public void ToPinyin_HandleHeteronymsFalse_银行_単字最優先読み()
        {
            // HandleHeteronyms=false: 行の最優先読みはxíng（フレーズ辞書ならháng）
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("银行");
            // フレーズ辞書なし→行はxíng（最優先）になる（hángではない）
            Assert.EndsWith("xíng", result);
        }

        [Fact]
        public void ToPinyin_HandleHeteronymsFalse_音乐_単字最優先読み()
        {
            // HandleHeteronyms=false: 乐の最優先読みはlè（フレーズ辞書ならyuè）
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("音乐");
            // フレーズ辞書なし→乐はlè（最優先）になる（yuèではない）
            Assert.EndsWith("lè", result);
        }

        [Fact]
        public void ToPinyin_HandleHeteronymsFalse_了解_単字最優先読み()
        {
            // HandleHeteronyms=false: 了の最優先読みはle（フレーズ辞書ならliǎo）
            var options = new ChineseG2POptions(handleHeteronyms: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("了解");
            // フレーズ辞書なし→了はle（最優先）になる（liǎoではない）
            Assert.StartsWith("le", result);
        }

        // =====================================================================
        // 4. フレーズ境界テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_重要行为_連続フレーズ解決()
        {
            // "重要" + "行为" が連続して正しく解決される
            var result = _engine.ToPinyin("重要行为");
            Assert.Equal("zhòng yào xíng wéi", result);
        }

        [Fact]
        public void ToPinyin_银行行为_重複文字連続フレーズ()
        {
            // "银行"(háng) + "行为"(xíng) の境界テスト
            // 最長一致で "银行" がまずマッチし、残りの "行为" がマッチする
            var result = _engine.ToPinyin("银行行为");
            Assert.Equal("yín háng xíng wéi", result);
        }

        [Fact]
        public void ToPinyin_音乐快乐_連続フレーズ異なる読み()
        {
            // "音乐"(yuè) + "快乐"(lè) : 同じ文字「乐」が異なる読みに解決される
            var result = _engine.ToPinyin("音乐快乐");
            Assert.Equal("yīn yuè kuài lè", result);
        }

        [Fact]
        public void ToPinyin_长城长大_連続フレーズ異なる読み()
        {
            // "长城"(cháng) + "长大"(zhǎng) : 同じ文字「长」が異なる読みに解決される
            var result = _engine.ToPinyin("长城长大");
            Assert.Equal("cháng chéng zhǎng dà", result);
        }

        [Fact]
        public void ToPinyin_重要重复_連続フレーズ異なる読み()
        {
            // "重要"(zhòng) + "重复"(chóng) : 同じ文字「重」が異なる読みに解決される
            var result = _engine.ToPinyin("重要重复");
            Assert.Equal("zhòng yào chóng fù", result);
        }

        [Fact]
        public void ToPinyin_数字数不清_連続フレーズ異なる読み()
        {
            // "数字"(shù) + "数不清"(shǔ) : 同じ文字「数」が異なる読みに解決される
            var result = _engine.ToPinyin("数字数不清");
            Assert.Equal("shù zì shǔ bù qīng", result);
        }

        [Fact]
        public void ToPinyin_了解了_フレーズ後に残余文字()
        {
            // "了解"(liǎo jiě) + "了"(le: 単字最優先)
            var result = _engine.ToPinyin("了解了");
            Assert.StartsWith("liǎo jiě", result);
        }

        [Fact]
        public void ToPinyin_还是还是_同一フレーズ繰り返し()
        {
            // "还是"(hái shì) が2回繰り返し
            var result = _engine.ToPinyin("还是还是");
            Assert.Equal("hái shì hái shì", result);
        }

        [Fact]
        public void ToPinyin_大学生大夫_異なるフレーズ境界()
        {
            // "大学生"(dà xué shēng) + "大夫"(dà fū)
            var result = _engine.ToPinyin("大学生大夫");
            Assert.Equal("dà xué shēng dà fū", result);
        }

        [Fact]
        public void ToPinyin_自行车行为_行の異なる読み()
        {
            // "自行车"(xíng) + "行为"(xíng) : 両方ともxíng
            var result = _engine.ToPinyin("自行车行为");
            Assert.Equal("zì xíng chē xíng wéi", result);
        }

        // =====================================================================
        // 5. 単字フォールバック
        // =====================================================================

        [Fact]
        public void ToPinyin_重_単独_最優先読み()
        {
            // 重 単独: フレーズマッチなし→単字辞書フォールバック→最優先読み zhòng
            var result = _engine.ToPinyin("重");
            Assert.Equal("zhòng", result);
        }

        [Fact]
        public void ToPinyin_行_単独_最優先読み()
        {
            // 行 単独: フレーズマッチなし→単字辞書フォールバック→最優先読み xíng
            var result = _engine.ToPinyin("行");
            Assert.Equal("xíng", result);
        }

        [Fact]
        public void ToPinyin_了_単独_最優先読み()
        {
            // 了 単独: フレーズマッチなし→単字辞書フォールバック→最優先読み le
            var result = _engine.ToPinyin("了");
            Assert.Equal("le", result);
        }

        [Fact]
        public void ToPinyin_长_単独_最優先読み()
        {
            // 长 単独: フレーズマッチなし→単字辞書フォールバック→最優先読み zhǎng
            var result = _engine.ToPinyin("长");
            Assert.Equal("zhǎng", result);
        }

        [Fact]
        public void ToPinyin_乐_単独_最優先読み()
        {
            // 乐 単独: フレーズマッチなし→単字辞書フォールバック→最優先読み lè
            var result = _engine.ToPinyin("乐");
            Assert.Equal("lè", result);
        }

        // =====================================================================
        // 6. ToPinyinList 多音字テスト
        // =====================================================================

        [Fact]
        public void ToPinyinList_重要_フレーズ解決済み配列()
        {
            // ToPinyinListでもフレーズ辞書マッチが機能すること
            var result = _engine.ToPinyinList("重要");
            Assert.Equal(new[] { "zhòng", "yào" }, result);
        }

        [Fact]
        public void ToPinyinList_重复_フレーズ解決済み配列()
        {
            var result = _engine.ToPinyinList("重复");
            Assert.Equal(new[] { "chóng", "fù" }, result);
        }

        [Fact]
        public void ToPinyinList_银行_フレーズ解決済み配列()
        {
            var result = _engine.ToPinyinList("银行");
            Assert.Equal(new[] { "yín", "háng" }, result);
        }

        [Fact]
        public void ToPinyinList_行为_フレーズ解決済み配列()
        {
            var result = _engine.ToPinyinList("行为");
            Assert.Equal(new[] { "xíng", "wéi" }, result);
        }

        [Fact]
        public void ToPinyinList_音乐快乐_異なる読み連続()
        {
            // ToPinyinListでも「乐」が文脈により異なる読みに解決される
            var result = _engine.ToPinyinList("音乐快乐");
            Assert.Equal(new[] { "yīn", "yuè", "kuài", "lè" }, result);
        }

        // =====================================================================
        // 7. スタイル変換と多音字の組み合わせ
        // =====================================================================

        [Fact]
        public void ToPinyin_重要_ToneNumber_フレーズ解決()
        {
            // ToneNumberスタイルでもフレーズ辞書が正しく機能する
            var result = _engine.ToPinyin("重要", PinyinStyle.ToneNumber);
            Assert.Equal("zhong4 yao4", result);
        }

        [Fact]
        public void ToPinyin_重复_ToneNumber_フレーズ解決()
        {
            var result = _engine.ToPinyin("重复", PinyinStyle.ToneNumber);
            Assert.Equal("chong2 fu4", result);
        }

        [Fact]
        public void ToPinyin_银行_Normal_フレーズ解決()
        {
            // Normalスタイルでもフレーズ辞書が正しく機能する
            var result = _engine.ToPinyin("银行", PinyinStyle.Normal);
            Assert.Equal("yin hang", result);
        }

        [Fact]
        public void ToPinyin_行为_Normal_フレーズ解決()
        {
            var result = _engine.ToPinyin("行为", PinyinStyle.Normal);
            Assert.Equal("xing wei", result);
        }
    }
}
