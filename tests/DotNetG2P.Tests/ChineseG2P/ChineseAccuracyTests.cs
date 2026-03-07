using System;
using System.Linq;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// 中国語G2P精度・回帰テスト。
    /// 高頻度多音字、声調変調、一般フレーズ、スタイル一貫性、回帰ケースを網羅する。
    /// 全期待値はエンジン実出力を事前検証の上で設定済み。
    /// </summary>
    public class ChineseAccuracyTests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseAccuracyTests()
        {
            // デフォルト: 声調変調有効、フレーズ辞書有効
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 高頻度多音字テスト (22件)
        // フレーズ辞書による文脈依存の読み分けを検証する。
        // =====================================================================

        [Fact]
        public void 多音字_行_银行はhang_行为はxing()
        {
            // 行: háng（銀行）vs xíng（行為）
            Assert.Equal("yín háng", _engine.ToPinyin("银行"));
            Assert.Equal("xíng wéi", _engine.ToPinyin("行为"));
        }

        [Fact]
        public void 多音字_了_好了はle_了解はliao()
        {
            // 了: le（助詞）vs liǎo（了解）; 了解は三声連読でliáo
            Assert.Equal("hǎo le", _engine.ToPinyin("好了"));
            Assert.Equal("liáo jiě", _engine.ToPinyin("了解"));
        }

        [Fact]
        public void 多音字_长_长城はchang_长大はzhang()
        {
            // 长: cháng（長い）vs zhǎng（成長する）
            Assert.Equal("cháng chéng", _engine.ToPinyin("长城"));
            Assert.Equal("zhǎng dà", _engine.ToPinyin("长大"));
        }

        [Fact]
        public void 多音字_地_地方はdi_慢慢地はde()
        {
            // 地: dì（名詞用法）vs de（助詞用法）
            Assert.Equal("dì fāng", _engine.ToPinyin("地方"));
            Assert.Equal("màn màn de", _engine.ToPinyin("慢慢地"));
        }

        [Fact]
        public void 多音字_还_还是はhai_还钱はhuan()
        {
            // 还: hái（まだ）vs huán（返す）
            Assert.Equal("hái shì", _engine.ToPinyin("还是"));
            Assert.Equal("huán qián", _engine.ToPinyin("还钱"));
        }

        [Fact]
        public void 多音字_都_都是はdou_首都はdu()
        {
            // 都: dōu（全て）vs dū（首都）
            Assert.Equal("dōu shì", _engine.ToPinyin("都是"));
            Assert.Equal("shǒu dū", _engine.ToPinyin("首都"));
        }

        [Fact]
        public void 多音字_为_因为はwei4声_为了はwei4声()
        {
            // 为: wèi（4声、ために）— 因为・為了ともにwèi
            Assert.Equal("yīn wèi", _engine.ToPinyin("因为"));
            Assert.Equal("wèi le", _engine.ToPinyin("为了"));
        }

        [Fact]
        public void 多音字_数_数字はshu4声_数不清はshu3声()
        {
            // 数: shù（数）vs shǔ（数える）
            Assert.Equal("shù zì", _engine.ToPinyin("数字"));
            Assert.Equal("shǔ bù qīng", _engine.ToPinyin("数不清"));
        }

        [Fact]
        public void 多音字_只_只有はzhi2声_一只はzhi1声()
        {
            // 只: zhǐ→zhí（フレーズ辞書の変調済みデータ）/ zhī（量詞）
            Assert.Equal("zhí yǒu", _engine.ToPinyin("只有"));
            // 一只: 一変調(yì) + zhī
            Assert.Equal("yì zhī", _engine.ToPinyin("一只"));
        }

        [Fact]
        public void 多音字_重_重要はzhong_重复はchong()
        {
            // 重: zhòng（重い）vs chóng（繰り返す）
            Assert.Equal("zhòng yào", _engine.ToPinyin("重要"));
            Assert.Equal("chóng fù", _engine.ToPinyin("重复"));
        }

        [Fact]
        public void 多音字_乐_快乐はle_音乐はyue()
        {
            // 乐: lè（楽しい）vs yuè（音楽）
            Assert.Equal("kuài lè", _engine.ToPinyin("快乐"));
            Assert.Equal("yīn yuè", _engine.ToPinyin("音乐"));
        }

        [Fact]
        public void 多音字_种_种类はzhong3声_种植はzhong4声()
        {
            // 种: zhǒng（種類）vs zhòng（植える）
            Assert.Equal("zhǒng lèi", _engine.ToPinyin("种类"));
            Assert.Equal("zhòng zhí", _engine.ToPinyin("种植"));
        }

        [Fact]
        public void 多音字_干_干净はgan1声_干活はgan4声()
        {
            // 干: gān（きれい）vs gàn（仕事する）
            Assert.Equal("gān jìng", _engine.ToPinyin("干净"));
            Assert.Equal("gàn huó", _engine.ToPinyin("干活"));
        }

        [Fact]
        public void 多音字_相_相信はxiang1声_照相はxiang4声()
        {
            // 相: xiāng（互い）vs xiàng（写真）
            Assert.Equal("xiāng xìn", _engine.ToPinyin("相信"));
            Assert.Equal("zhào xiàng", _engine.ToPinyin("照相"));
        }

        [Fact]
        public void 多音字_少_多少はshao3声_少年はshao4声()
        {
            // 少: shǎo（少ない）vs shào（若い）
            Assert.Equal("duō shǎo", _engine.ToPinyin("多少"));
            Assert.Equal("shào nián", _engine.ToPinyin("少年"));
        }

        [Fact]
        public void 多音字_看_看见はkan4声_看守はkan1声()
        {
            // 看: kàn（見る）vs kān（看守）
            Assert.Equal("kàn jiàn", _engine.ToPinyin("看见"));
            Assert.Equal("kān shǒu", _engine.ToPinyin("看守"));
        }

        [Fact]
        public void 多音字_大_大学はda_大夫はda()
        {
            // 大: dà（大きい）、大夫もdà fū（フレーズ辞書）
            Assert.Equal("dà xué", _engine.ToPinyin("大学"));
            Assert.Equal("dà fū", _engine.ToPinyin("大夫"));
        }

        [Fact]
        public void 多音字_得_得到はde2声_跑得快はde2声_得亏はdei3声()
        {
            // 得: dé（得る）/ dé（助詞、フレーズ辞書）/ děi（得亏）
            Assert.Equal("dé dào", _engine.ToPinyin("得到"));
            Assert.Equal("pǎo dé kuài", _engine.ToPinyin("跑得快"));
            Assert.Equal("děi kuī", _engine.ToPinyin("得亏"));
        }

        [Fact]
        public void 多音字_觉_感觉はjue_睡觉はjiao()
        {
            // 觉: jué（感じる）vs jiào（眠る）
            Assert.Equal("gǎn jué", _engine.ToPinyin("感觉"));
            Assert.Equal("shuì jiào", _engine.ToPinyin("睡觉"));
        }

        [Fact]
        public void 多音字_教_教书はjiao1声_教室はjiao4声()
        {
            // 教: jiāo（教える）vs jiào（教室）
            Assert.Equal("jiāo shū", _engine.ToPinyin("教书"));
            Assert.Equal("jiào shì", _engine.ToPinyin("教室"));
        }

        [Fact]
        public void 多音字_連続フレーズ_同じ漢字が異なる読み()
        {
            // 同じ文中で同一漢字が異なるフレーズ解決される
            Assert.Equal("zhòng yào chóng fù", _engine.ToPinyin("重要重复"));
            Assert.Equal("cháng chéng zhǎng dà", _engine.ToPinyin("长城长大"));
            Assert.Equal("yīn yuè kuài lè", _engine.ToPinyin("音乐快乐"));
        }

        [Fact]
        public void 多音字_数字数不清_連続フレーズ境界()
        {
            // "数字"(shù) + "数不清"(shǔ) 連続
            Assert.Equal("shù zì shǔ bù qīng", _engine.ToPinyin("数字数不清"));
        }

        // =====================================================================
        // 2. 声調変調正確性テスト (16件)
        // =====================================================================

        [Fact]
        public void 声調変調_三声連読_你好()
        {
            // 你好: nǐ hǎo → ní hǎo（3声+3声 → 2声+3声）
            Assert.Equal("ní hǎo", _engine.ToPinyin("你好"));
        }

        [Fact]
        public void 声調変調_三声連読_展览馆()
        {
            // 展览馆: zhǎn lǎn guǎn → zhán lán guǎn（3連続三声）
            Assert.Equal("zhán lán guǎn", _engine.ToPinyin("展览馆"));
        }

        [Fact]
        public void 声調変調_三声連読_你也好()
        {
            // 你也好: nǐ yě hǎo → ní yé hǎo（3連続三声）
            Assert.Equal("ní yé hǎo", _engine.ToPinyin("你也好"));
        }

        [Fact]
        public void 声調変調_三声連読_我也好()
        {
            // 我也好: wǒ yě hǎo → wó yé hǎo（3連続三声）
            Assert.Equal("wó yé hǎo", _engine.ToPinyin("我也好"));
        }

        [Fact]
        public void 声調変調_三声連読_买马()
        {
            // 买马: mǎi mǎ → mái mǎ
            Assert.Equal("mái mǎ", _engine.ToPinyin("买马"));
        }

        [Fact]
        public void 声調変調_三声連読_选举()
        {
            // 选举: xuǎn jǔ → xuán jǔ
            Assert.Equal("xuán jǔ", _engine.ToPinyin("选举"));
        }

        [Fact]
        public void 声調変調_三声連読_管理()
        {
            // 管理: guǎn lǐ → guán lǐ
            Assert.Equal("guán lǐ", _engine.ToPinyin("管理"));
        }

        [Fact]
        public void 声調変調_一変調_4声前で2声()
        {
            // 一个: yī gè → yí gè（4声前→2声）
            Assert.Equal("yí gè", _engine.ToPinyin("一个"));
        }

        [Fact]
        public void 声調変調_一変調_1声前で4声()
        {
            // 一天: yī tiān → yì tiān（1声前→4声）
            Assert.Equal("yì tiān", _engine.ToPinyin("一天"));
            // 一杯: yī bēi → yì bēi
            Assert.Equal("yì bēi", _engine.ToPinyin("一杯"));
        }

        [Fact]
        public void 声調変調_一変調_2声前で4声()
        {
            // 一年: yī nián → yì nián（2声前→4声）
            Assert.Equal("yì nián", _engine.ToPinyin("一年"));
        }

        [Fact]
        public void 声調変調_一変調_3声前で4声()
        {
            // 一起: yī qǐ → yì qǐ（3声前→4声）
            Assert.Equal("yì qǐ", _engine.ToPinyin("一起"));
            // 一本: yī běn → yì běn
            Assert.Equal("yì běn", _engine.ToPinyin("一本"));
        }

        [Fact]
        public void 声調変調_一変調_序数例外()
        {
            // 第一: dì yī（序数例外で変調なし）
            Assert.Equal("dì yī", _engine.ToPinyin("第一"));
        }

        [Fact]
        public void 声調変調_一変調_文末で変調なし()
        {
            // 统一: tǒng yī（文末の一は変調しない）
            Assert.Equal("tǒng yī", _engine.ToPinyin("统一"));
        }

        [Fact]
        public void 声調変調_不変調_4声前で2声()
        {
            // 不要: bù yào → bú yào（4声前→2声）
            Assert.Equal("bú yào", _engine.ToPinyin("不要"));
            // 不错: bù cuò → bú cuò
            Assert.Equal("bú cuò", _engine.ToPinyin("不错"));
        }

        [Fact]
        public void 声調変調_不変調_非4声前は変調なし()
        {
            // 不能: bù néng（2声前→変調なし）
            Assert.Equal("bù néng", _engine.ToPinyin("不能"));
            // 不好: bù hǎo（3声前→変調なし）
            Assert.Equal("bù hǎo", _engine.ToPinyin("不好"));
            // 不行: bù xíng（2声前→変調なし）
            Assert.Equal("bù xíng", _engine.ToPinyin("不行"));
        }

        [Fact]
        public void 声調変調_複合_不一定()
        {
            // 不一定: 一→4声前(定dìng)→yí、不→次は一(yí=2声)→変調なし→bù
            Assert.Equal("bù yí dìng", _engine.ToPinyin("不一定"));
        }

        // =====================================================================
        // 3. 一般的なフレーズ正確性テスト (22件)
        // =====================================================================

        [Fact]
        public void フレーズ_挨拶_你好()
        {
            Assert.Equal("ní hǎo", _engine.ToPinyin("你好"));
        }

        [Fact]
        public void フレーズ_挨拶_再见()
        {
            Assert.Equal("zài jiàn", _engine.ToPinyin("再见"));
        }

        [Fact]
        public void フレーズ_挨拶_谢谢()
        {
            // 2文字目が軽声→xiè xie
            Assert.Equal("xiè xie", _engine.ToPinyin("谢谢"));
        }

        [Fact]
        public void フレーズ_挨拶_对不起()
        {
            Assert.Equal("duì bù qǐ", _engine.ToPinyin("对不起"));
        }

        [Fact]
        public void フレーズ_地名_中国()
        {
            Assert.Equal("zhōng guó", _engine.ToPinyin("中国"));
        }

        [Fact]
        public void フレーズ_地名_北京()
        {
            Assert.Equal("běi jīng", _engine.ToPinyin("北京"));
        }

        [Fact]
        public void フレーズ_地名_上海()
        {
            Assert.Equal("shàng hǎi", _engine.ToPinyin("上海"));
        }

        [Fact]
        public void フレーズ_数字_一から十の単字()
        {
            Assert.Equal("yī", _engine.ToPinyin("一"));
            Assert.Equal("èr", _engine.ToPinyin("二"));
            Assert.Equal("sān", _engine.ToPinyin("三"));
            Assert.Equal("sì", _engine.ToPinyin("四"));
            Assert.Equal("wǔ", _engine.ToPinyin("五"));
            Assert.Equal("liù", _engine.ToPinyin("六"));
            Assert.Equal("qī", _engine.ToPinyin("七"));
            Assert.Equal("bā", _engine.ToPinyin("八"));
            Assert.Equal("jiǔ", _engine.ToPinyin("九"));
            Assert.Equal("shí", _engine.ToPinyin("十"));
        }

        [Fact]
        public void フレーズ_数字_一二三四五六七八九十()
        {
            // 一が先頭: 次の二(èr=4声)→一変調→yí
            Assert.Equal("yí èr sān sì wǔ liù qī bā jiǔ shí",
                _engine.ToPinyin("一二三四五六七八九十"));
        }

        [Fact]
        public void フレーズ_日常_学生()
        {
            // xué sheng（軽声）
            Assert.Equal("xué sheng", _engine.ToPinyin("学生"));
        }

        [Fact]
        public void フレーズ_日常_老师()
        {
            Assert.Equal("lǎo shī", _engine.ToPinyin("老师"));
        }

        [Fact]
        public void フレーズ_日常_朋友()
        {
            // péng you（軽声）
            Assert.Equal("péng you", _engine.ToPinyin("朋友"));
        }

        [Fact]
        public void フレーズ_日常_吃饭()
        {
            Assert.Equal("chī fàn", _engine.ToPinyin("吃饭"));
        }

        [Fact]
        public void フレーズ_日常_工作()
        {
            Assert.Equal("gōng zuò", _engine.ToPinyin("工作"));
        }

        [Fact]
        public void フレーズ_日常_学习()
        {
            Assert.Equal("xué xí", _engine.ToPinyin("学习"));
        }

        [Fact]
        public void フレーズ_食事_米饭面条饺子()
        {
            Assert.Equal("mǐ fàn", _engine.ToPinyin("米饭"));
            Assert.Equal("miàn tiáo", _engine.ToPinyin("面条"));
            Assert.Equal("jiǎo zi", _engine.ToPinyin("饺子"));
        }

        [Fact]
        public void フレーズ_食事_包子豆腐()
        {
            Assert.Equal("bāo zi", _engine.ToPinyin("包子"));
            Assert.Equal("dòu fǔ", _engine.ToPinyin("豆腐"));
        }

        [Fact]
        public void フレーズ_家族_爸爸妈妈()
        {
            // 2文字目が軽声
            Assert.Equal("bà ba", _engine.ToPinyin("爸爸"));
            Assert.Equal("mā ma", _engine.ToPinyin("妈妈"));
        }

        [Fact]
        public void フレーズ_家族_哥哥姐姐()
        {
            Assert.Equal("gē ge", _engine.ToPinyin("哥哥"));
            Assert.Equal("jiě jie", _engine.ToPinyin("姐姐"));
        }

        [Fact]
        public void フレーズ_家族_弟弟妹妹()
        {
            Assert.Equal("dì di", _engine.ToPinyin("弟弟"));
            Assert.Equal("mèi mei", _engine.ToPinyin("妹妹"));
        }

        [Fact]
        public void フレーズ_物_电脑手机飞机()
        {
            Assert.Equal("diàn nǎo", _engine.ToPinyin("电脑"));
            Assert.Equal("shǒu jī", _engine.ToPinyin("手机"));
            Assert.Equal("fēi jī", _engine.ToPinyin("飞机"));
        }

        [Fact]
        public void フレーズ_日常_汽车()
        {
            Assert.Equal("qì chē", _engine.ToPinyin("汽车"));
        }

        // =====================================================================
        // 4. スタイル一貫性テスト (6件)
        // =====================================================================

        [Fact]
        public void スタイル一貫性_你好世界_3スタイル音節数一致()
        {
            var marked = _engine.ToPinyinList("你好世界");
            var number = _engine.ToPinyinList("你好世界", PinyinStyle.ToneNumber);
            var normal = _engine.ToPinyinList("你好世界", PinyinStyle.Normal);

            Assert.Equal(4, marked.Length);
            Assert.Equal(marked.Length, number.Length);
            Assert.Equal(marked.Length, normal.Length);
        }

        [Fact]
        public void スタイル一貫性_中华人民共和国_3スタイル音節数一致()
        {
            var marked = _engine.ToPinyinList("中华人民共和国");
            var number = _engine.ToPinyinList("中华人民共和国", PinyinStyle.ToneNumber);
            var normal = _engine.ToPinyinList("中华人民共和国", PinyinStyle.Normal);

            Assert.Equal(7, marked.Length);
            Assert.Equal(marked.Length, number.Length);
            Assert.Equal(marked.Length, normal.Length);
        }

        [Fact]
        public void スタイル一貫性_ToneNumber末尾数字が1から4()
        {
            // 声調を持つ音節のToneNumber結果は末尾が1-4
            var result = _engine.ToPinyinList("你好世界", PinyinStyle.ToneNumber);
            // "ni2", "hao3", "shi4", "jie4"
            foreach (var syllable in result)
            {
                var lastChar = syllable[syllable.Length - 1];
                Assert.True(lastChar >= '1' && lastChar <= '4',
                    $"ToneNumber音節 '{syllable}' の末尾が1-4ではありません");
            }
        }

        [Fact]
        public void スタイル一貫性_Normal結果に声調記号なし()
        {
            var result = _engine.ToPinyinList("你好世界", PinyinStyle.Normal);
            var toneMarks = "āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ";
            foreach (var syllable in result)
            {
                Assert.True(syllable.All(c => !toneMarks.Contains(c)),
                    $"Normal音節 '{syllable}' に声調記号が含まれています");
            }
        }

        [Fact]
        public void スタイル一貫性_北京大学_全スタイル正確()
        {
            Assert.Equal("běi jīng dà xué", _engine.ToPinyin("北京大学"));
            Assert.Equal("bei3 jing1 da4 xue2", _engine.ToPinyin("北京大学", PinyinStyle.ToneNumber));
            Assert.Equal("bei jing da xue", _engine.ToPinyin("北京大学", PinyinStyle.Normal));
        }

        [Fact]
        public void スタイル一貫性_学生老师_軽声もスタイル間で一貫()
        {
            // 軽声を含む場合もスタイル間で音節数が一致すること
            var marked = _engine.ToPinyinList("学生老师");
            var number = _engine.ToPinyinList("学生老师", PinyinStyle.ToneNumber);
            var normal = _engine.ToPinyinList("学生老师", PinyinStyle.Normal);

            Assert.Equal(4, marked.Length);
            Assert.Equal(marked.Length, number.Length);
            Assert.Equal(marked.Length, normal.Length);
        }

        // =====================================================================
        // 5. 回帰テスト (7件)
        // C1-C3で修正済みの既知バグパターンの回帰防止テスト。
        // =====================================================================

        [Fact]
        public void 回帰_フレーズ辞書の変調済みデータがそのまま使われる()
        {
            // フレーズ辞書に声調変調済みのデータが入っている場合、
            // EnableToneSandhi=false でもフレーズ辞書のデータがそのまま出力される
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);

            // "一个" → フレーズ辞書に "yí gè" が登録済み（変調済み）
            Assert.Equal("yí gè", engine.ToPinyin("一个"));
            // "一起" → フレーズ辞書に "yì qǐ" が登録済み（変調済み）
            Assert.Equal("yì qǐ", engine.ToPinyin("一起"));
        }

        [Fact]
        public void 回帰_三声連読OFF時は変調されない()
        {
            // EnableToneSandhi=false で 你好 は変調されない（nǐ hǎo のまま）
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("nǐ hǎo", engine.ToPinyin("你好"));
        }

        [Fact]
        public void 回帰_連続フレーズ境界での正しい多音字解決()
        {
            // "重要重复" → "重要"(zhòng yào) + "重复"(chóng fù)
            Assert.Equal("zhòng yào chóng fù", _engine.ToPinyin("重要重复"));
        }

        [Fact]
        public void 回帰_句読点混在でも正しく変換される()
        {
            var result = _engine.ToPinyin("你好，世界！");
            Assert.Contains("ní", result);
            Assert.Contains("hǎo", result);
            Assert.Contains("shì", result);
            Assert.Contains("jiè", result);
            Assert.DoesNotContain("，", result);
            Assert.DoesNotContain("！", result);
        }

        [Fact]
        public void 回帰_空文字列と特殊入力で例外が発生しない()
        {
            Assert.Equal("", _engine.ToPinyin(""));
            Assert.Equal("", _engine.ToPinyin("，。！？"));
            Assert.Equal("Hello", _engine.ToPinyin("Hello"));
            Assert.Equal("123", _engine.ToPinyin("123"));
        }

        [Fact]
        public void 回帰_不变調_フレーズ辞書に変調済みデータがある場合()
        {
            // "不对" → フレーズ辞書に "bú duì" が登録済み
            // EnableToneSandhi=false でもフレーズ辞書データが使われる
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("bú duì", engine.ToPinyin("不对"));
        }

        [Fact]
        public void 回帰_不错_フレーズ辞書なし_声調変調で変調()
        {
            // "不错" はフレーズ辞書になし→声調変調で bú cuò
            Assert.Equal("bú cuò", _engine.ToPinyin("不错"));

            // 声調変調OFF: bù cuò
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("bù cuò", engine.ToPinyin("不错"));
        }

        // =====================================================================
        // 6. 声調変調ON/OFF比較テスト (5件)
        // =====================================================================

        [Fact]
        public void 声調変調比較_只有_ONはzhi2声_OFFはzhi3声()
        {
            Assert.Equal("zhí yǒu", _engine.ToPinyin("只有"));

            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("zhǐ yǒu", engine.ToPinyin("只有"));
        }

        [Fact]
        public void 声調変調比較_一只_ONはyi4声_OFFはyi1声()
        {
            Assert.Equal("yì zhī", _engine.ToPinyin("一只"));

            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("yī zhī", engine.ToPinyin("一只"));
        }

        [Fact]
        public void 声調変調比較_了解_ONはliao2声_OFFはliao3声()
        {
            Assert.Equal("liáo jiě", _engine.ToPinyin("了解"));

            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("liǎo jiě", engine.ToPinyin("了解"));
        }

        [Fact]
        public void 声調変調比較_一二三_ONはyi2声_OFFはyi1声()
        {
            Assert.Equal("yí èr sān", _engine.ToPinyin("一二三"));

            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("yī èr sān", engine.ToPinyin("一二三"));
        }

        [Fact]
        public void 声調変調比較_我也好_ONはwo2声_OFFはwo3声()
        {
            Assert.Equal("wó yé hǎo", _engine.ToPinyin("我也好"));

            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            Assert.Equal("wǒ yě hǎo", engine.ToPinyin("我也好"));
        }
    }
}
