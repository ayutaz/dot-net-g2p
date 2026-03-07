using System;
using DotNetG2P.Chinese;
using Xunit;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ChineseG2PEngine C3統合テスト。
    /// 声調変調（Tone Sandhi）がエンジンパイプラインで正しく適用されることを検証する。
    /// 三声連読変調、"一"変調、"不"変調、およびEnableToneSandhiオプション制御を網羅する。
    /// </summary>
    public class ChineseG2PEngineC3Tests : IDisposable
    {
        private readonly ChineseG2PEngine _engine;

        public ChineseG2PEngineC3Tests()
        {
            // 声調変調はデフォルトで有効（EnableToneSandhi=true）
            _engine = new ChineseG2PEngine();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        // =====================================================================
        // 1. 三声連読テスト（エンジン経由）
        // =====================================================================

        [Fact]
        public void ToPinyin_你好_三声連読で最初が2声()
        {
            // 你好: nǐ hǎo → ní hǎo（3声+3声 → 2声+3声）
            var result = _engine.ToPinyin("你好");
            Assert.Equal("ní hǎo", result);
        }

        [Fact]
        public void ToPinyinList_你好_三声連読で最初が2声()
        {
            var result = _engine.ToPinyinList("你好");
            Assert.Equal(new[] { "ní", "hǎo" }, result);
        }

        [Fact]
        public void ToPinyin_你好世界_三声連読は你好のみ()
        {
            // 你好(3+3→2+3) + 世界(4+4→変調なし)
            var result = _engine.ToPinyin("你好世界");
            Assert.Equal("ní hǎo shì jiè", result);
        }

        [Fact]
        public void ToPinyin_你也好_3連続三声()
        {
            // 你也好: nǐ yě hǎo → ní yé hǎo
            var result = _engine.ToPinyin("你也好");
            Assert.Equal("ní yé hǎo", result);
        }

        [Fact]
        public void ToPinyin_了解_三声連読()
        {
            // 了解: liǎo jiě → liáo jiě（3声+3声 → 2声+3声）
            var result = _engine.ToPinyin("了解");
            Assert.Equal("liáo jiě", result);
        }

        [Fact]
        public void ToPinyin_展览馆_3連続三声()
        {
            // 展览馆: zhǎn lǎn guǎn → zhán lán guǎn
            var result = _engine.ToPinyin("展览馆");
            Assert.Equal("zhán lán guǎn", result);
        }

        [Fact]
        public void ToPinyin_三声間に非三声_変調なし()
        {
            // 中国: zhōng guó → 三声なし → 変調なし
            var result = _engine.ToPinyin("中国");
            Assert.Equal("zhōng guó", result);
        }

        [Fact]
        public void ToPinyin_北京大学_三声連読なし()
        {
            // 北京大学: běi jīng dà xué → 連続三声なし
            var result = _engine.ToPinyin("北京大学");
            Assert.Equal("běi jīng dà xué", result);
        }

        // =====================================================================
        // 2. "一"変調テスト（エンジン経由）
        // =====================================================================

        [Fact]
        public void ToPinyin_一个_4声前で2声()
        {
            // 一个: yī gè → yí gè（4声前→2声）
            var result = _engine.ToPinyin("一个");
            Assert.Equal("yí gè", result);
        }

        [Fact]
        public void ToPinyin_一天_1声前で4声()
        {
            // 一天: yī tiān → yì tiān（1声前→4声）
            var result = _engine.ToPinyin("一天");
            Assert.Equal("yì tiān", result);
        }

        [Fact]
        public void ToPinyin_一年_2声前で4声()
        {
            // 一年: yī nián → yì nián（2声前→4声）
            var result = _engine.ToPinyin("一年");
            Assert.Equal("yì nián", result);
        }

        [Fact]
        public void ToPinyin_一起_3声前で4声()
        {
            // 一起: yī qǐ → yì qǐ（3声前→4声）
            var result = _engine.ToPinyin("一起");
            Assert.Equal("yì qǐ", result);
        }

        [Fact]
        public void ToPinyin_第一_序数例外で変調なし()
        {
            // 第一: dì yī → dì yī（序数例外）
            var result = _engine.ToPinyin("第一");
            Assert.Equal("dì yī", result);
        }

        [Fact]
        public void ToPinyin_统一_文末で変調なし()
        {
            // 统一: tǒng yī → tǒng yī（文末の一は変調しない）
            var result = _engine.ToPinyin("统一");
            Assert.Equal("tǒng yī", result);
        }

        [Fact]
        public void ToPinyin_一_単独で変調なし()
        {
            var result = _engine.ToPinyin("一");
            Assert.Equal("yī", result);
        }

        [Fact]
        public void ToPinyin_一二三_一変調適用()
        {
            // 一二三: 一の次は二(èr, 4声) → yí
            var result = _engine.ToPinyin("一二三");
            Assert.Equal("yí èr sān", result);
        }

        [Fact]
        public void ToPinyin_第一次_序数例外()
        {
            // 第一次: 第の直後の一は変調しない
            var result = _engine.ToPinyin("第一次");
            Assert.Equal("dì yī cì", result);
        }

        // =====================================================================
        // 3. "不"変調テスト（エンジン経由）
        // =====================================================================

        [Fact]
        public void ToPinyin_不要_4声前で2声()
        {
            // 不要: bù yào → bú yào（4声前→2声）
            var result = _engine.ToPinyin("不要");
            Assert.Equal("bú yào", result);
        }

        [Fact]
        public void ToPinyin_不对_4声前で2声()
        {
            // 不对: bù duì → bú duì（4声前→2声）
            var result = _engine.ToPinyin("不对");
            Assert.Equal("bú duì", result);
        }

        [Fact]
        public void ToPinyin_不能_2声前で変調なし()
        {
            // 不能: bù néng → bù néng（変調なし）
            var result = _engine.ToPinyin("不能");
            Assert.Equal("bù néng", result);
        }

        [Fact]
        public void ToPinyin_不好_3声前で変調なし()
        {
            // 不好: bù hǎo → bù hǎo（変調なし）
            var result = _engine.ToPinyin("不好");
            Assert.Equal("bù hǎo", result);
        }

        [Fact]
        public void ToPinyin_不_単独で変調なし()
        {
            var result = _engine.ToPinyin("不");
            Assert.Equal("bù", result);
        }

        // =====================================================================
        // 4. 組み合わせテスト
        // =====================================================================

        [Fact]
        public void ToPinyin_一不要_一と不の両方変調()
        {
            // フレーズ辞書: "不要"→"bú yào"（既に変調済み）
            // 一変調: 一の次は不(bú=2声) → yì(4声)
            var result = _engine.ToPinyin("一不要");
            Assert.Equal("yì bú yào", result);
        }

        [Fact]
        public void ToPinyin_不一定_不と一の組み合わせ()
        {
            // 不一定: 一→4声前(定)→yí、不→次は一(yí→2声)→変調なし
            var result = _engine.ToPinyin("不一定");
            Assert.Equal("bù yí dìng", result);
        }

        [Fact]
        public void ToPinyin_你好句読点世界_句読点で三声連読分断()
        {
            // 你好，世界: 句読点が区切り、你好は三声連読あり
            var result = _engine.ToPinyin("你好，世界");
            // 你好は句読点前でも三声連読が適用される（同じピンイン配列内）
            Assert.Contains("ní", result);
            Assert.Contains("shì", result);
        }

        [Fact]
        public void ToPinyin_不要不要_繰り返し()
        {
            // 不要不要: 両方の不が4声前→bú
            var result = _engine.ToPinyin("不要不要");
            Assert.Equal("bú yào bú yào", result);
        }

        // =====================================================================
        // 5. EnableToneSandhi=false テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_EnableToneSandhiFalse_你好_変調なし()
        {
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("nǐ hǎo", result);
        }

        [Fact]
        public void ToPinyin_EnableToneSandhiFalse_一个_フレーズ辞書の変調済みデータ使用()
        {
            // フレーズ辞書 "一个" → "yí gè" は既に変調済みデータ
            // EnableToneSandhi=false でもフレーズ辞書のデータはそのまま出力される
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("一个");
            Assert.Equal("yí gè", result);
        }

        [Fact]
        public void ToPinyin_EnableToneSandhiFalse_不要_フレーズ辞書の変調済みデータ使用()
        {
            // フレーズ辞書 "不要" → "bú yào" は既に変調済みデータ
            // EnableToneSandhi=false でもフレーズ辞書のデータはそのまま出力される
            var options = new ChineseG2POptions(enableToneSandhi: false);
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("不要");
            Assert.Equal("bú yào", result);
        }

        // =====================================================================
        // 6. スタイル変換 + 声調変調
        // =====================================================================

        [Fact]
        public void ToPinyin_你好_ToneNumber_三声連読適用()
        {
            // 你好 → ní hǎo → ni2 hao3
            var result = _engine.ToPinyin("你好", PinyinStyle.ToneNumber);
            Assert.Equal("ni2 hao3", result);
        }

        [Fact]
        public void ToPinyin_你好_Normal_声調除去()
        {
            // 声調変調後に声調除去 → 結果は同じ
            var result = _engine.ToPinyin("你好", PinyinStyle.Normal);
            Assert.Equal("ni hao", result);
        }

        [Fact]
        public void ToPinyin_一个_ToneNumber_変調適用()
        {
            // 一个 → yí gè → yi2 ge4
            var result = _engine.ToPinyin("一个", PinyinStyle.ToneNumber);
            Assert.Equal("yi2 ge4", result);
        }

        [Fact]
        public void ToPinyin_不要_ToneNumber_変調適用()
        {
            // 不要 → bú yào → bu2 yao4
            var result = _engine.ToPinyin("不要", PinyinStyle.ToneNumber);
            Assert.Equal("bu2 yao4", result);
        }

        // =====================================================================
        // 7. カスタムセパレータ + 声調変調
        // =====================================================================

        [Fact]
        public void ToPinyin_你好_カスタムセパレータ_三声連読適用()
        {
            var options = new ChineseG2POptions(separator: "-");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("ní-hǎo", result);
        }

        [Fact]
        public void ToPinyin_空セパレータ_三声連読適用()
        {
            var options = new ChineseG2POptions(separator: "");
            using var engine = new ChineseG2PEngine(options);
            var result = engine.ToPinyin("你好");
            Assert.Equal("níhǎo", result);
        }

        // =====================================================================
        // 8. バッチAPI + 声調変調
        // =====================================================================

        [Fact]
        public void ToPinyinBatch_声調変調適用()
        {
            var result = _engine.ToPinyinBatch(new[] { "你好", "一个", "不要" });
            Assert.Equal(3, result.Count);
            Assert.Equal("ní hǎo", result[0]);
            Assert.Equal("yí gè", result[1]);
            Assert.Equal("bú yào", result[2]);
        }

        // =====================================================================
        // 9. 多音字 + 声調変調の複合テスト
        // =====================================================================

        [Fact]
        public void ToPinyin_重要_声調変調影響なし()
        {
            // 重要: zhòng yào（4声+4声 → 声調変調なし）
            var result = _engine.ToPinyin("重要");
            Assert.Equal("zhòng yào", result);
        }

        [Fact]
        public void ToPinyin_了解_三声連読でliaoが2声()
        {
            // 了解: liǎo jiě → liáo jiě（フレーズ辞書 + 三声連読）
            var result = _engine.ToPinyin("了解");
            Assert.Equal("liáo jiě", result);
        }

        [Fact]
        public void ToPinyin_数不清_不変調適用()
        {
            // 数不清: shǔ bù qīng → 不の次は清(1声) → 変調なし
            var result = _engine.ToPinyin("数不清");
            Assert.Equal("shǔ bù qīng", result);
        }

        [Fact]
        public void ToPinyin_英字混在_你好World_三声連読適用()
        {
            var result = _engine.ToPinyin("你好World");
            Assert.StartsWith("ní", result);
            Assert.EndsWith("World", result);
        }

        [Fact]
        public void ToPinyin_長文_声調変調適用()
        {
            // 今天天气很好我们一起去公园
            var result = _engine.ToPinyin("今天天气很好我们一起去公园");
            Assert.NotEmpty(result);
            var parts = result.Split(' ');
            Assert.True(parts.Length >= 5,
                $"長い文のピンイン音節数が不足: {parts.Length}, 結果: {result}");
        }
    }
}
