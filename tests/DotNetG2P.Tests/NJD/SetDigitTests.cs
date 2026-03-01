using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class SetDigitTests
    {
        // ===== ヘルパー =====

        /// <summary>
        /// 数字の名詞-数ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateKazuNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi, "数");
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        /// <summary>
        /// 助数詞（名詞-接尾-助数詞）ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateJosuushiNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi, "接尾", "助数詞");
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        /// <summary>
        /// 副詞可能（名詞-副詞可能）ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateFukushiKanouNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi, "副詞可能");
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        /// <summary>
        /// 一般名詞ノードを作成するヘルパー。
        /// </summary>
        private static NjdNode CreateMeishiNode(string surface, string katakana, int accentType = 0)
        {
            var pos = new POS(POSType.Meishi);
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                Pronunciation = pron,
            };
            return node;
        }

        // ===== 空リスト・例外テスト =====

        [Fact]
        public void Process_空リスト_例外が発生しない()
        {
            var nodes = new List<NjdNode>();
            SetDigit.Process(nodes);
            Assert.Empty(nodes);
        }

        // ===== フェーズ2: 助数詞との読み変化 (class1) =====

        [Fact]
        public void Process_一個_イチがイッに変化()
        {
            // 「一」+「個」→ 一がイッに変化（class1G）
            var ichi = CreateKazuNode("一", "イチ", 2);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { ichi, ko };
            SetDigit.Process(nodes);

            // class1Gにより「一」→「イッ」
            Assert.Equal("イッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_六個_ロクがロッに変化()
        {
            // 「六」+「個」→ 六がロッに変化（class1G）
            var roku = CreateKazuNode("六", "ロク", 2);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { roku, ko };
            SetDigit.Process(nodes);

            Assert.Equal("ロッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_八個_ハチがハッに変化()
        {
            // 「八」+「個」→ 八がハッに変化（class1G）
            var hachi = CreateKazuNode("八", "ハチ", 1);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { hachi, ko };
            SetDigit.Process(nodes);

            Assert.Equal("ハッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_十個_ジュウがジュッに変化()
        {
            // 「十」+「個」→ 十がジュッに変化（class1G）
            var juu = CreateKazuNode("十", "ジュウ", 1);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { juu, ko };
            SetDigit.Process(nodes);

            Assert.Equal("ジュッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_四年_ヨンがヨに変化()
        {
            // 「四」+「年」→ 四がヨに変化（class1B）
            var yon = CreateKazuNode("四", "ヨン", 1);
            var nen = CreateJosuushiNode("年", "ネン", 1);

            var nodes = new List<NjdNode> { yon, nen };
            SetDigit.Process(nodes);

            Assert.Equal("ヨ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_四月_シに変化()
        {
            // 「四」+「月」→ 四がシに変化（class1E）
            var yon = CreateKazuNode("四", "ヨン", 1);
            var gatsu = CreateJosuushiNode("月", "ガツ", 1);

            var nodes = new List<NjdNode> { yon, gatsu };
            SetDigit.Process(nodes);

            Assert.Equal("シ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_九時_クに変化()
        {
            // 「九」+「時」→ 九がクに変化（class1C2）
            var kyuu = CreateKazuNode("九", "キュウ", 1);
            var ji = CreateJosuushiNode("時", "ジ", 1);

            var nodes = new List<NjdNode> { kyuu, ji };
            SetDigit.Process(nodes);

            Assert.Equal("ク", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== フェーズ2: 助数詞側の音便変化 (class2) =====

        [Fact]
        public void Process_三本_ホンがボンに濁音化()
        {
            // 「三」+「本」→ 本がボンに連濁（class2C: 三→Voiced）
            var san = CreateKazuNode("三", "サン", 1);
            var hon = CreateJosuushiNode("本", "ホン", 1);

            var nodes = new List<NjdNode> { san, hon };
            SetDigit.Process(nodes);

            // 本(ホン)の最初のモーラが濁音化: ホ→ボ
            Assert.Equal("ボン", nodes[1].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_一本_ホンがポンに半濁音化()
        {
            // 「一」+「本」→ 本がポンに半濁（class2C: 一→SemiVoiced）
            var ichi = CreateKazuNode("一", "イチ", 2);
            var hon = CreateJosuushiNode("本", "ホン", 1);

            var nodes = new List<NjdNode> { ichi, hon };
            SetDigit.Process(nodes);

            // 本(ホン) → ポン（半濁音化）
            Assert.Equal("ポン", nodes[1].Pronunciation.ToKatakana());
            // さらにclass1Gにより一はイッに変化
            Assert.Equal("イッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_一分_フンがプンに半濁音化()
        {
            // 「一」+「分」→ 分がプンに半濁（class2B: 一→SemiVoiced）
            var ichi = CreateKazuNode("一", "イチ", 2);
            var fun = CreateJosuushiNode("分", "フン", 1);

            var nodes = new List<NjdNode> { ichi, fun };
            SetDigit.Process(nodes);

            // 分(フン) → プン（半濁音化: フ→プ）
            Assert.Equal("プン", nodes[1].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_三階_カイがガイに濁音化()
        {
            // 「三」+「階」→ 階がガイに連濁（class2F: 三→Voiced）
            var san = CreateKazuNode("三", "サン", 1);
            var kai = CreateJosuushiNode("階", "カイ", 1);

            var nodes = new List<NjdNode> { san, kai };
            SetDigit.Process(nodes);

            Assert.Equal("ガイ", nodes[1].Pronunciation.ToKatakana());
        }

        // ===== フェーズ3: 位取りの読み変化 (numeral) =====

        [Fact]
        public void Process_六百_ロクがロッに変化()
        {
            // 「六」+「百」→ 六がロッ（DigitConvTable: 百の前の六→ロッ）
            var roku = CreateKazuNode("六", "ロク", 2);
            var hyaku = CreateKazuNode("百", "ヒャク", 2);

            var nodes = new List<NjdNode> { roku, hyaku };
            SetDigit.Process(nodes);

            Assert.Equal("ロッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_八百_ハチがハッに変化()
        {
            // 「八」+「百」→ 八がハッ
            var hachi = CreateKazuNode("八", "ハチ", 1);
            var hyaku = CreateKazuNode("百", "ヒャク", 2);

            var nodes = new List<NjdNode> { hachi, hyaku };
            SetDigit.Process(nodes);

            Assert.Equal("ハッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_三百_ヒャクがビャクに濁音化()
        {
            // 「三」+「百」→ 百がビャク（NumerativeConvTable: 三→Voiced）
            var san = CreateKazuNode("三", "サン", 1);
            var hyaku = CreateKazuNode("百", "ヒャク", 2);

            var nodes = new List<NjdNode> { san, hyaku };
            SetDigit.Process(nodes);

            Assert.Equal("ビャク", nodes[1].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_六百_ヒャクがピャクに半濁音化()
        {
            // 「六」+「百」→ 百がピャク（NumerativeConvTable: 六→SemiVoiced）
            var roku = CreateKazuNode("六", "ロク", 2);
            var hyaku = CreateKazuNode("百", "ヒャク", 2);

            var nodes = new List<NjdNode> { roku, hyaku };
            SetDigit.Process(nodes);

            Assert.Equal("ピャク", nodes[1].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_一千_イチがイッに変化()
        {
            // 「一」+「千」→ 一がイッ（DigitConvTable: 千の前の一→イッ）
            var ichi = CreateKazuNode("一", "イチ", 2);
            var sen = CreateKazuNode("千", "セン", 1);

            var nodes = new List<NjdNode> { ichi, sen };
            SetDigit.Process(nodes);

            Assert.Equal("イッ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_三千_センがゼンに濁音化()
        {
            // 「三」+「千」→ 千がゼン（NumerativeConvTable: 三→Voiced）
            var san = CreateKazuNode("三", "サン", 1);
            var sen = CreateKazuNode("千", "セン", 1);

            var nodes = new List<NjdNode> { san, sen };
            SetDigit.Process(nodes);

            // セン → ゼン（サ行→ザ行の濁音化: セ→ゼ）
            Assert.Equal("ゼン", nodes[1].Pronunciation.ToKatakana());
        }

        // ===== フェーズ4: 特殊読み (others) =====

        [Fact]
        public void Process_一人_ヒトリに変化()
        {
            // 「一」+「人」→ 「一人」ヒトリ
            var ichi = CreateKazuNode("一", "イチ", 2);
            var nin = CreateJosuushiNode("人", "ニン", 1);

            var nodes = new List<NjdNode> { ichi, nin };
            SetDigit.Process(nodes);

            // 一人ノードに統合: Surface="一人"、発音="ヒトリ"
            Assert.Equal("一人", nodes[0].Surface);
            Assert.Equal("ヒトリ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_二人_フタリに変化()
        {
            // 「二」+「人」→ 「二人」フタリ
            var ni = CreateKazuNode("二", "ニ", 1);
            var nin = CreateJosuushiNode("人", "ニン", 1);

            var nodes = new List<NjdNode> { ni, nin };
            SetDigit.Process(nodes);

            Assert.Equal("二人", nodes[0].Surface);
            Assert.Equal("フタリ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_二日_フツカに変化()
        {
            // 「二」+「日」→ 「二日」フツカ
            var ni = CreateKazuNode("二", "ニ", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { ni, nichi };
            SetDigit.Process(nodes);

            Assert.Equal("二日", nodes[0].Surface);
            Assert.Equal("フツカ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_三日_ミッカに変化()
        {
            var san = CreateKazuNode("三", "サン", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { san, nichi };
            SetDigit.Process(nodes);

            Assert.Equal("三日", nodes[0].Surface);
            Assert.Equal("ミッカ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_四日_ヨッカに変化()
        {
            var yon = CreateKazuNode("四", "ヨン", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { yon, nichi };
            SetDigit.Process(nodes);

            Assert.Equal("四日", nodes[0].Surface);
            Assert.Equal("ヨッカ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_十日_トーカに変化()
        {
            var juu = CreateKazuNode("十", "ジュウ", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { juu, nichi };
            SetDigit.Process(nodes);

            Assert.Equal("十日", nodes[0].Surface);
            Assert.Equal("トーカ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== フェーズ4: 和語助数詞 (class3) =====

        [Fact]
        public void Process_一つ_ヒトツに変化()
        {
            // 「一」+「つ」→ 一がヒトに変化（class3: 月(ツキ)の読み...ではなく「つ」を探す）
            // class3テーブルには「つ」が無いので、直接テストしない
            // 代わりに「棟」で確認: 「一」+「棟（ムネ）」→ 一がヒトに変化
            var ichi = CreateKazuNode("一", "イチ", 2);
            var mune = CreateJosuushiNode("棟", "ムネ", 1);

            var nodes = new List<NjdNode> { ichi, mune };
            SetDigit.Process(nodes);

            Assert.Equal("ヒト", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_二棟_フタに変化()
        {
            // 「二」+「棟（ムネ）」→ 二がフタに変化（class3）
            var ni = CreateKazuNode("二", "ニ", 1);
            var mune = CreateJosuushiNode("棟", "ムネ", 1);

            var nodes = new List<NjdNode> { ni, mune };
            SetDigit.Process(nodes);

            Assert.Equal("フタ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== フェーズ5: 複合日付パターン =====

        [Fact]
        public void Process_二十日_ハツカに変化()
        {
            // 「二」+「十」+「日」→ 二十日（ハツカ）
            var ni = CreateKazuNode("二", "ニ", 1);
            var juu = CreateKazuNode("十", "ジュウ", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { ni, juu, nichi };
            SetDigit.Process(nodes);

            // ノード[0]が「二十日」(ハツカ)に変換されているはず
            Assert.Equal("二十日", nodes[0].Surface);
            Assert.Equal("ハツカ", nodes[0].Pronunciation.ToKatakana());
        }

        [Fact]
        public void Process_十四日_ジューヨッカに変化()
        {
            // 「十」+「四」+「日」→ 十四日（ジューヨッカ）
            var juu = CreateKazuNode("十", "ジュウ", 1);
            var yon = CreateKazuNode("四", "ヨン", 1);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { juu, yon, nichi };
            SetDigit.Process(nodes);

            Assert.Equal("十四日", nodes[0].Surface);
            Assert.Equal("ジューヨッカ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== フェーズ1: 小数点処理 =====

        [Fact]
        public void Process_小数点_ピリオドがテンに変換される()
        {
            // 「三」+「．」+「一」→ ピリオドが「テン」に変換
            var san = CreateKazuNode("三", "サン", 1);
            // ピリオドは記号として構築
            var period = new NjdNode("\uFF0E", new WordDetails(
                new POS(POSType.Kigou), "*", "*", "\uFF0E", "*", null))
            {
                Pronunciation = new Pronunciation(),
            };
            var ichi = CreateKazuNode("一", "イチ", 2);

            var nodes = new List<NjdNode> { san, period, ichi };
            SetDigit.Process(nodes);

            // ピリオドが「テン」に変換される
            Assert.Equal("テン", nodes[1].Pronunciation.ToKatakana());
        }

        // ===== チェインフラグの設定 =====

        [Fact]
        public void Process_助数詞_チェインフラグが設定される()
        {
            // 数字+助数詞で、数字側がfalse、助数詞側がtrue
            var san = CreateKazuNode("三", "サン", 1);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { san, ko };
            SetDigit.Process(nodes);

            Assert.False(nodes[0].ChainFlag);
            Assert.True(nodes[1].ChainFlag);
        }

        // ===== 非数字ノードは影響されない =====

        [Fact]
        public void Process_非数字ノード_変更されない()
        {
            var node = CreateMeishiNode("東京", "トウキョウ");
            var nodes = new List<NjdNode> { node };

            SetDigit.Process(nodes);

            Assert.Single(nodes);
            Assert.Equal("東京", nodes[0].Surface);
            Assert.Equal("トウキョウ", nodes[0].Pronunciation.ToKatakana());
        }

        // ===== 位取りチェインフラグ =====

        [Fact]
        public void Process_数字と位取り_チェインフラグが設定される()
        {
            // 「三」+「百」→ 三が非チェイン、百がチェイン
            var san = CreateKazuNode("三", "サン", 1);
            var hyaku = CreateKazuNode("百", "ヒャク", 2);

            var nodes = new List<NjdNode> { san, hyaku };
            SetDigit.Process(nodes);

            Assert.False(nodes[0].ChainFlag);
            Assert.True(nodes[1].ChainFlag);
        }

        // ===== null安全性テスト =====

        [Fact]
        public void Process_Detailsがnullのノードがあっても例外が発生しない()
        {
            // Detailsがnullのノードを含むリストで処理してもNREが発生しないことを確認
            var nullNode = new NjdNode("テスト", null)
            {
                Pronunciation = new Pronunciation(),
            };
            var ichi = CreateKazuNode("一", "イチ", 2);
            var ko = CreateJosuushiNode("個", "コ", 1);

            var nodes = new List<NjdNode> { nullNode, ichi, ko };
            SetDigit.Process(nodes);

            // 例外が発生せずに完了すること
            Assert.True(nodes.Count >= 2);
        }

        [Fact]
        public void Process_月の後の一日_ツイタチに変化()
        {
            // 「月」の後の「一」+「日」→ ツイタチに変換されるべき
            var gatsu = CreateJosuushiNode("月", "ガツ", 1);
            var ichi = CreateKazuNode("一", "イチ", 2);
            var nichi = CreateFukushiKanouNode("日", "ニチ", 1);

            var nodes = new List<NjdNode> { gatsu, ichi, nichi };
            SetDigit.Process(nodes);

            // 月の後の一+日はツイタチに変換
            Assert.Equal("一日", nodes[1].Surface);
            Assert.Equal("ツイタチ", nodes[1].Pronunciation.ToKatakana());
        }
    }
}
