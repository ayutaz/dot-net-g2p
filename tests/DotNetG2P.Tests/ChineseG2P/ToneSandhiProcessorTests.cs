using DotNetG2P.Chinese;

namespace DotNetG2P.Tests.ChineseG2P
{
    /// <summary>
    /// ToneSandhiProcessor の単体テスト。
    /// 三声連読変調、"一"変調、"不"変調の3ルールを検証する。
    /// </summary>
    public class ToneSandhiProcessorTests
    {
        // =====================================================================
        // ヘルパーメソッド
        // =====================================================================

        /// <summary>
        /// ToneSandhiProcessor.Apply を呼び出し、変調後のピンイン配列を返す。
        /// </summary>
        private static string[] ApplySandhi(string[] pinyins, char[] originalChars)
        {
            ToneSandhiProcessor.Apply(pinyins, originalChars);
            return pinyins;
        }

        // =====================================================================
        // 1. 三声連読テスト
        // =====================================================================

        [Fact]
        public void Apply_三声2連続_最初が2声に変調()
        {
            // 你好: nǐ hǎo → ní hǎo
            var pinyins = new[] { "nǐ", "hǎo" };
            var chars = new[] { '你', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("ní", pinyins[0]);
            Assert.Equal("hǎo", pinyins[1]);
        }

        [Fact]
        public void Apply_三声3連続_最後以外が2声に変調()
        {
            // 你也好: nǐ yě hǎo → ní yé hǎo
            var pinyins = new[] { "nǐ", "yě", "hǎo" };
            var chars = new[] { '你', '也', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("ní", pinyins[0]);
            Assert.Equal("yé", pinyins[1]);
            Assert.Equal("hǎo", pinyins[2]);
        }

        [Fact]
        public void Apply_展览馆_3連続三声()
        {
            // 展览馆: zhǎn lǎn guǎn → zhán lán guǎn
            var pinyins = new[] { "zhǎn", "lǎn", "guǎn" };
            var chars = new[] { '展', '览', '馆' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("zhán", pinyins[0]);
            Assert.Equal("lán", pinyins[1]);
            Assert.Equal("guǎn", pinyins[2]);
        }

        [Fact]
        public void Apply_三声1つだけ_変調なし()
        {
            // 好: hǎo → hǎo（変調なし）
            var pinyins = new[] { "hǎo" };
            var chars = new[] { '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("hǎo", pinyins[0]);
        }

        [Fact]
        public void Apply_三声間に非三声_変調なし()
        {
            // 你是好 (nǐ shì hǎo): 3声+4声+3声 → 変調なし（3声が連続していない）
            var pinyins = new[] { "nǐ", "shì", "hǎo" };
            var chars = new[] { '你', '是', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("nǐ", pinyins[0]);
            Assert.Equal("shì", pinyins[1]);
            Assert.Equal("hǎo", pinyins[2]);
        }

        [Fact]
        public void Apply_買酒_非三声と三声_変調なし()
        {
            // 买(mǎi)は3声、酒(jiǔ)も3声 → mái jiǔ
            var pinyins = new[] { "mǎi", "jiǔ" };
            var chars = new[] { '买', '酒' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("mái", pinyins[0]);
            Assert.Equal("jiǔ", pinyins[1]);
        }

        [Fact]
        public void Apply_你好吗_三声三声軽声()
        {
            // 你好吗: nǐ hǎo ma → ní hǎo ma（3声+3声 → 2声+3声、軽声はそのまま）
            var pinyins = new[] { "nǐ", "hǎo", "ma" };
            var chars = new[] { '你', '好', '吗' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("ní", pinyins[0]);
            Assert.Equal("hǎo", pinyins[1]);
            Assert.Equal("ma", pinyins[2]);
        }

        [Fact]
        public void Apply_4連続三声_最後以外すべて2声()
        {
            // 我也很好: wǒ yě hěn hǎo → wó yé hén hǎo
            var pinyins = new[] { "wǒ", "yě", "hěn", "hǎo" };
            var chars = new[] { '我', '也', '很', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("wó", pinyins[0]);
            Assert.Equal("yé", pinyins[1]);
            Assert.Equal("hén", pinyins[2]);
            Assert.Equal("hǎo", pinyins[3]);
        }

        [Fact]
        public void Apply_非三声のみ_変調なし()
        {
            // 中国: zhōng guó (1声+2声) → そのまま
            var pinyins = new[] { "zhōng", "guó" };
            var chars = new[] { '中', '国' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("zhōng", pinyins[0]);
            Assert.Equal("guó", pinyins[1]);
        }

        [Fact]
        public void Apply_三声2組が分離_各組で独立変調()
        {
            // 你好 世界好 → nǐ hǎo は変調、shì jiè hǎo は変調なし
            // ここでは: 你好 + 他好 → ní hǎo tā hǎo（他は1声なので後半は変調なし）
            var pinyins = new[] { "nǐ", "hǎo", "tā", "hǎo" };
            var chars = new[] { '你', '好', '他', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("ní", pinyins[0]);
            Assert.Equal("hǎo", pinyins[1]);
            Assert.Equal("tā", pinyins[2]);
            Assert.Equal("hǎo", pinyins[3]);
        }

        // =====================================================================
        // 2. "一"変調テスト
        // =====================================================================

        [Fact]
        public void Apply_一個_4声前で2声に変調()
        {
            // 一个: yī gè → yí gè（4声前→2声）
            var pinyins = new[] { "yī", "gè" };
            var chars = new[] { '一', '个' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yí", pinyins[0]);
            Assert.Equal("gè", pinyins[1]);
        }

        [Fact]
        public void Apply_一天_1声前で4声に変調()
        {
            // 一天: yī tiān → yì tiān（1声前→4声）
            var pinyins = new[] { "yī", "tiān" };
            var chars = new[] { '一', '天' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yì", pinyins[0]);
            Assert.Equal("tiān", pinyins[1]);
        }

        [Fact]
        public void Apply_一年_2声前で4声に変調()
        {
            // 一年: yī nián → yì nián（2声前→4声）
            var pinyins = new[] { "yī", "nián" };
            var chars = new[] { '一', '年' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yì", pinyins[0]);
            Assert.Equal("nián", pinyins[1]);
        }

        [Fact]
        public void Apply_一起_3声前で4声に変調()
        {
            // 一起: yī qǐ → yì qǐ（3声前→4声）
            var pinyins = new[] { "yī", "qǐ" };
            var chars = new[] { '一', '起' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yì", pinyins[0]);
            Assert.Equal("qǐ", pinyins[1]);
        }

        [Fact]
        public void Apply_第一_序数例外で変調なし()
        {
            // 第一: dì yī → dì yī（序数例外、変調なし）
            var pinyins = new[] { "dì", "yī" };
            var chars = new[] { '第', '一' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("dì", pinyins[0]);
            Assert.Equal("yī", pinyins[1]);
        }

        [Fact]
        public void Apply_統一_文末で変調なし()
        {
            // 统一: tǒng yī → tǒng yī（文末、変調なし）
            // ただし三声連読は影響なし（yīは1声）
            var pinyins = new[] { "tǒng", "yī" };
            var chars = new[] { '统', '一' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("tǒng", pinyins[0]);
            Assert.Equal("yī", pinyins[1]);
        }

        [Fact]
        public void Apply_一単独_変調なし()
        {
            // "一" 単独 → yī（変調なし）
            var pinyins = new[] { "yī" };
            var chars = new[] { '一' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yī", pinyins[0]);
        }

        [Fact]
        public void Apply_一一列举_連続一()
        {
            // 一一列举: yī yī liè jǔ
            // 最初の一: 次の漢字スロットは一(1声) → yì
            // 2番目の一: 次の漢字スロットは列(4声) → yí
            var pinyins = new[] { "yī", "yī", "liè", "jǔ" };
            var chars = new[] { '一', '一', '列', '举' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yì", pinyins[0]);  // 1声前→4声
            Assert.Equal("yí", pinyins[1]);  // 4声前→2声
            Assert.Equal("liè", pinyins[2]);
            Assert.Equal("jǔ", pinyins[3]);
        }

        [Fact]
        public void Apply_一百_1声前で4声()
        {
            // 一百: yī bǎi → yì bǎi（3声前→4声）
            var pinyins = new[] { "yī", "bǎi" };
            var chars = new[] { '一', '百' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yì", pinyins[0]);
            Assert.Equal("bǎi", pinyins[1]);
        }

        [Fact]
        public void Apply_第一次_序数例外()
        {
            // 第一次: dì yī cì → dì yī cì（第の直後の一は変調しない）
            var pinyins = new[] { "dì", "yī", "cì" };
            var chars = new[] { '第', '一', '次' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("dì", pinyins[0]);
            Assert.Equal("yī", pinyins[1]);
            Assert.Equal("cì", pinyins[2]);
        }

        // =====================================================================
        // 3. "不"変調テスト
        // =====================================================================

        [Fact]
        public void Apply_不要_4声前で2声に変調()
        {
            // 不要: bù yào → bú yào（4声前→2声）
            var pinyins = new[] { "bù", "yào" };
            var chars = new[] { '不', '要' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bú", pinyins[0]);
            Assert.Equal("yào", pinyins[1]);
        }

        [Fact]
        public void Apply_不対_4声前で2声に変調()
        {
            // 不对: bù duì → bú duì（4声前→2声）
            var pinyins = new[] { "bù", "duì" };
            var chars = new[] { '不', '对' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bú", pinyins[0]);
            Assert.Equal("duì", pinyins[1]);
        }

        [Fact]
        public void Apply_不能_2声前で変調なし()
        {
            // 不能: bù néng → bù néng（2声前→変調なし）
            var pinyins = new[] { "bù", "néng" };
            var chars = new[] { '不', '能' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bù", pinyins[0]);
            Assert.Equal("néng", pinyins[1]);
        }

        [Fact]
        public void Apply_不好_3声前で変調なし()
        {
            // 不好: bù hǎo → bù hǎo（3声前→変調なし）
            var pinyins = new[] { "bù", "hǎo" };
            var chars = new[] { '不', '好' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bù", pinyins[0]);
            Assert.Equal("hǎo", pinyins[1]);
        }

        [Fact]
        public void Apply_不単独_変調なし()
        {
            // "不" 単独 → bù（変調なし）
            var pinyins = new[] { "bù" };
            var chars = new[] { '不' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bù", pinyins[0]);
        }

        [Fact]
        public void Apply_不知_1声前で変調なし()
        {
            // 不知: bù zhī → bù zhī（1声前→変調なし）
            var pinyins = new[] { "bù", "zhī" };
            var chars = new[] { '不', '知' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bù", pinyins[0]);
            Assert.Equal("zhī", pinyins[1]);
        }

        // =====================================================================
        // 4. 組み合わせテスト
        // =====================================================================

        [Fact]
        public void Apply_一不_組み合わせ変調()
        {
            // 一不: yī bù
            // 一の次は不(4声) → yí
            // 不は文末 → bù（変調なし）
            var pinyins = new[] { "yī", "bù" };
            var chars = new[] { '一', '不' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yí", pinyins[0]);   // 4声前→2声
            Assert.Equal("bù", pinyins[1]);   // 文末→変調なし
        }

        [Fact]
        public void Apply_不一定_不と一の組み合わせ()
        {
            // 不一定: bù yī dìng
            // 一変調: 一の次は定(4声) → yí
            // 不変調: 不の次は一(もともと1声だがyí変調後?) → 処理順は一変調→不変調
            // 実装: ApplyYiSandhi先 → pinyins[1]="yí"(2声)、ApplyBuSandhi → 不の次は一(2声) → 変調なし
            var pinyins = new[] { "bù", "yī", "dìng" };
            var chars = new[] { '不', '一', '定' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("bù", pinyins[0]);    // 次が一(→yí、2声) → 変調なし
            Assert.Equal("yí", pinyins[1]);     // 4声前→2声
            Assert.Equal("dìng", pinyins[2]);
        }

        [Fact]
        public void Apply_空配列_エラーなし()
        {
            var pinyins = new string[0];
            var chars = new char[0];

            var exception = Record.Exception(() => ApplySandhi(pinyins, chars));
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_null入力_エラーなし()
        {
            var exception = Record.Exception(() => ToneSandhiProcessor.Apply(null, null));
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_pinyinsのみnull_エラーなし()
        {
            var exception = Record.Exception(() => ToneSandhiProcessor.Apply(null, new[] { '你' }));
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_originalCharsのみnull_エラーなし()
        {
            var exception = Record.Exception(() => ToneSandhiProcessor.Apply(new[] { "nǐ" }, null));
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_配列長不一致_エラーなし()
        {
            // pinyins.Length != originalChars.Length → 早期リターン
            var pinyins = new[] { "nǐ", "hǎo" };
            var chars = new[] { '你' };

            var exception = Record.Exception(() => ToneSandhiProcessor.Apply(pinyins, chars));
            Assert.Null(exception);
            // 変更されないことも確認
            Assert.Equal("nǐ", pinyins[0]);
            Assert.Equal("hǎo", pinyins[1]);
        }

        [Fact]
        public void Apply_非漢字スロット含む_漢字のみ処理()
        {
            // nullChar('\0') を挟む → 非漢字スロットはスキップ
            var pinyins = new[] { "nǐ", "X", "hǎo" };
            var chars = new[] { '你', '\0', '好' };

            ApplySandhi(pinyins, chars);

            // 非漢字スロットをスキップしつつ、你と好は3声連続として変調
            Assert.Equal("ní", pinyins[0]);
            Assert.Equal("X", pinyins[1]);
            Assert.Equal("hǎo", pinyins[2]);
        }

        [Fact]
        public void Apply_一不要_一と不の組み合わせ()
        {
            // 一不要: yī bù yào
            // 一変調: 一の次の漢字スロットは不(4声) → yí
            // 不変調: 不の次は要(4声) → bú
            var pinyins = new[] { "yī", "bù", "yào" };
            var chars = new[] { '一', '不', '要' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("yí", pinyins[0]);     // 4声前→2声
            Assert.Equal("bú", pinyins[1]);     // 4声前→2声
            Assert.Equal("yào", pinyins[2]);
        }

        [Fact]
        public void Apply_軽声は三声連読に影響しない()
        {
            // 我的好: wǒ de hǎo（3声+軽声+3声 → 三声が連続していないので変調なし）
            var pinyins = new[] { "wǒ", "de", "hǎo" };
            var chars = new[] { '我', '的', '好' };

            ApplySandhi(pinyins, chars);

            // 的は漢字スロット(CJK範囲)だが軽声。
            // ApplyThirdToneSandhi: 我(3声)→的(軽声≠3声)→breakで連続範囲終了。
            // 我は1つだけなので変調なし。好も単独。
            Assert.Equal("wǒ", pinyins[0]);
            Assert.Equal("de", pinyins[1]);
            Assert.Equal("hǎo", pinyins[2]);
        }

        [Fact]
        public void Apply_すべて1声_変調なし()
        {
            // 天天开心: tiān tiān kāi xīn（全て1声→変調なし）
            var pinyins = new[] { "tiān", "tiān", "kāi", "xīn" };
            var chars = new[] { '天', '天', '开', '心' };

            ApplySandhi(pinyins, chars);

            Assert.Equal("tiān", pinyins[0]);
            Assert.Equal("tiān", pinyins[1]);
            Assert.Equal("kāi", pinyins[2]);
            Assert.Equal("xīn", pinyins[3]);
        }
    }
}
