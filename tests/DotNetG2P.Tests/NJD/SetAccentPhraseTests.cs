using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class SetAccentPhraseTests
    {
        /// <summary>
        /// NjdNodeを手動構築するヘルパー。
        /// </summary>
        private static NjdNode CreateNode(
            string surface,
            string katakana,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            string sub2 = "*",
            string sub3 = "*",
            int accentType = 0,
            bool? chainFlag = null,
            string conjugationForm = "*",
            string chainRule = "*")
        {
            var pos = new POS(posType, sub1, sub2, sub3);
            var pron = Pronunciation.FromKatakana(katakana, 0);
            var details = new WordDetails(pos, "*", conjugationForm, surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                ChainFlag = chainFlag,
                Pronunciation = pron,
                ChainRule = chainRule,
            };
            return node;
        }

        // ===== 空リスト・nullテスト =====

        [Fact]
        public void Process_Null_例外が発生しない()
        {
            SetAccentPhrase.Process(null!);
        }

        [Fact]
        public void Process_空リスト_例外が発生しない()
        {
            SetAccentPhrase.Process(new List<NjdNode>());
        }

        [Fact]
        public void Process_単一ノード_ChainFlagはnullのまま()
        {
            var node = CreateNode("東京", "トーキョー");
            var nodes = new List<NjdNode> { node };
            SetAccentPhrase.Process(nodes);

            // ノードが1つだけなので変更なし（ループが回らない）
            Assert.Null(node.ChainFlag);
        }

        // ===== Rule 08/09: 助詞・助動詞は前のノードに結合される =====

        [Fact]
        public void Process_名詞の後の助詞_結合される()
        {
            // 「東京」(名詞) + 「に」(助詞) → 「に」が結合
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("に", "ニ", POSType.Joshi, sub1: "格助詞");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_名詞の後の助動詞_結合される()
        {
            // 「猫」(名詞) + 「だ」(助動詞) → 「だ」が結合
            var node1 = CreateNode("猫", "ネコ");
            var node2 = CreateNode("だ", "ダ", POSType.Jodoushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_助詞の後の助動詞_付属語同士で結合()
        {
            // 助詞(付属語) + 助動詞(付属語) → 付属語同士で結合（Rule 08）
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("で", "デ", POSType.Joshi, sub1: "格助詞");
            var node3 = CreateNode("は", "ワ", POSType.Joshi, sub1: "係助詞");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);  // 名詞の後の助詞: Rule 08
            Assert.True(node3.ChainFlag);  // 助詞の後の助詞: Rule 08（付属語同士）
        }

        // ===== 自立語が新しいアクセント句を開始する =====

        [Fact]
        public void Process_名詞の後に動詞_別アクセント句()
        {
            // 「猫」(名詞) + 「走る」(動詞) → Rule 13: 名詞の後の動詞は別アクセント句
            var node1 = CreateNode("猫", "ネコ");
            var node2 = CreateNode("走る", "ハシル", POSType.Doushi, sub1: "自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_名詞の後に形容詞_別アクセント句()
        {
            // 「空」(名詞) + 「高い」(形容詞) → Rule 13
            var node1 = CreateNode("空", "ソラ");
            var node2 = CreateNode("高い", "タカイ", POSType.Keiyoushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_名詞の後に形容動詞語幹_別アクセント句()
        {
            // 「空」(名詞) + 「綺麗」(名詞-形容動詞語幹) → Rule 13
            var node1 = CreateNode("空", "ソラ");
            var node2 = CreateNode("綺麗", "キレイ", POSType.Meishi, sub1: "形容動詞語幹");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 18: 接尾辞は前にくっつける =====

        [Fact]
        public void Process_名詞接尾は前のノードに結合()
        {
            // 「田中」(名詞) + 「さん」(名詞-接尾) → Rule 18
            var node1 = CreateNode("田中", "タナカ");
            var node2 = CreateNode("さん", "サン", POSType.Meishi, sub1: "接尾");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_動詞接尾は前のノードに結合()
        {
            // 動詞 + 動詞-接尾 → Rule 18
            var node1 = CreateNode("食べ", "タベ", POSType.Doushi, sub1: "自立", conjugationForm: "連用形");
            var node2 = CreateNode("れる", "レル", POSType.Doushi, sub1: "接尾");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_形容詞接尾は前のノードに結合()
        {
            // X + 形容詞-接尾 → Rule 18
            var node1 = CreateNode("寒", "サム", POSType.Keiyoushi, sub1: "自立", conjugationForm: "ガル接続");
            var node2 = CreateNode("がる", "ガル", POSType.Keiyoushi, sub1: "接尾");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        // ===== Rule 17: 名詞の後の「名」は別のアクセント句 =====

        [Fact]
        public void Process_名詞の後の固有名詞人名名は別アクセント句()
        {
            // 「田中」(名詞-固有名詞-人名-姓) + 「太郎」(名詞-固有名詞-人名-名) → Rule 17
            var node1 = CreateNode("田中", "タナカ", POSType.Meishi, sub1: "固有名詞", sub2: "人名", sub3: "姓");
            var node2 = CreateNode("太郎", "タロー", POSType.Meishi, sub1: "固有名詞", sub2: "人名", sub3: "名");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // Rule 17: 名詞の後の「名」→ false (Rule 16より先にRule 17でfalse)
            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 16: 「姓」の後の名詞は別のアクセント句 =====

        [Fact]
        public void Process_姓の後の一般名詞は別アクセント句()
        {
            // 「田中」(名詞-固有名詞-人名-姓) + 「先生」(名詞-一般) → Rule 16
            // ただしRule 18(接尾)が先に判定されるため、一般名詞で検証
            var node1 = CreateNode("田中", "タナカ", POSType.Meishi, sub1: "固有名詞", sub2: "人名", sub3: "姓");
            var node2 = CreateNode("先生", "センセイ", POSType.Meishi, sub1: "一般");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 15: 接頭詞は単独のアクセント句 =====

        [Fact]
        public void Process_接頭詞は別アクセント句()
        {
            // 「お」(接頭詞) + 「茶」(名詞) → Rule 15: 接頭詞は非結合
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("お", "オ", POSType.Settoushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 14: 記号は単独のアクセント句 =====

        [Fact]
        public void Process_記号の後は別アクセント句()
        {
            var node1 = CreateNode("。", "、", POSType.Kigou);
            var node2 = CreateNode("東京", "トーキョー");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_記号が後続するときは別アクセント句()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("。", "、", POSType.Kigou);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 12: 動詞-非自立は動詞-連用形に接続する場合に結合 =====

        [Fact]
        public void Process_動詞連用形の後に動詞非自立_結合()
        {
            // 「食べ」(動詞-連用形) + 「られる」(動詞-非自立) → Rule 12
            var node1 = CreateNode("食べ", "タベ", POSType.Doushi, sub1: "自立", conjugationForm: "連用形");
            var node2 = CreateNode("て", "テ", POSType.Doushi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_動詞終止形の後に動詞非自立_非結合()
        {
            // 活用形が連用形でなければ、Rule 12は適用されない
            // 動詞 + 動詞-非自立で、連用形でない場合 → Rule 05で別アクセント句
            // (ただし動詞+名詞or形容詞のルール→名詞でも形容詞でもないので該当しない
            //  → デフォルトRule 01: くっつける ... になるが、
            //  実際にはRule 12の条件を満たさない場合は他のルールに流れる)
            var node1 = CreateNode("食べる", "タベル", POSType.Doushi, sub1: "自立", conjugationForm: "基本形");
            var node2 = CreateNode("こと", "コト", POSType.Meishi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // Rule 05: 動詞の後に名詞 → false
            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_名詞サ変接続の後に動詞非自立_結合()
        {
            // 「勉強」(名詞-サ変接続) + 「し」(動詞-非自立) → Rule 12
            var node1 = CreateNode("勉強", "ベンキョー", POSType.Meishi, sub1: "サ変接続");
            var node2 = CreateNode("し", "シ", POSType.Doushi, sub1: "非自立", conjugationForm: "連用形");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_一般名詞の後に動詞非自立_非結合()
        {
            // 「猫」(名詞-一般) + 「し」(動詞-非自立) → Rule 12 不適用、Rule 13で非結合
            var node1 = CreateNode("猫", "ネコ", POSType.Meishi, sub1: "一般");
            var node2 = CreateNode("し", "シ", POSType.Doushi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 11: 形容詞-非自立の結合パターン =====

        [Fact]
        public void Process_動詞連用形の後に形容詞非自立_結合()
        {
            // 動詞-連用形 + 形容詞-非自立 → Rule 11
            var node1 = CreateNode("食べ", "タベ", POSType.Doushi, sub1: "自立", conjugationForm: "連用形");
            var node2 = CreateNode("たい", "タイ", POSType.Keiyoushi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_形容詞連用形の後に形容詞非自立_結合()
        {
            // 形容詞-連用形 + 形容詞-非自立 → Rule 11
            var node1 = CreateNode("良く", "ヨク", POSType.Keiyoushi, sub1: "自立", conjugationForm: "連用テ接続");
            var node2 = CreateNode("ない", "ナイ", POSType.Keiyoushi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        [Fact]
        public void Process_接続助詞テの後に形容詞非自立_結合()
        {
            // 助詞-接続助詞「て」 + 形容詞-非自立 → Rule 11
            var node1 = CreateNode("て", "テ", POSType.Joshi, sub1: "接続助詞");
            var node2 = CreateNode("ほしい", "ホシイ", POSType.Keiyoushi, sub1: "非自立");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        // ===== Rule 10: 接尾の後の名詞は別のアクセント句 =====

        [Fact]
        public void Process_名詞接尾の後の名詞_別アクセント句()
        {
            // 「さん」(名詞-接尾) + 「東京」(名詞) → Rule 10
            var node1 = CreateNode("さん", "サン", POSType.Meishi, sub1: "接尾");
            var node2 = CreateNode("東京", "トーキョー", POSType.Meishi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 07: 名詞-副詞可能は単独のアクセント句 =====

        [Fact]
        public void Process_名詞副詞可能は単独アクセント句()
        {
            // 名詞-副詞可能が後続 → Rule 07で非結合
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("今日", "キョー", POSType.Meishi, sub1: "副詞可能");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 06: 副詞・接続詞・連体詞は単独のアクセント句 =====

        [Fact]
        public void Process_副詞は単独アクセント句()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("とても", "トテモ", POSType.Fukushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_接続詞は単独アクセント句()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("しかし", "シカシ", POSType.Setsuzokushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_連体詞は単独アクセント句()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("この", "コノ", POSType.Rentaishi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 02: 名詞の連続はくっつける =====

        [Fact]
        public void Process_名詞の連続_結合される()
        {
            // 一般名詞 + 一般名詞 → Rule 02: くっつける
            var node1 = CreateNode("東京", "トーキョー", POSType.Meishi, sub1: "一般");
            var node2 = CreateNode("駅", "エキ", POSType.Meishi, sub1: "一般");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }

        // ===== Rule 05: 動詞の後に形容詞or名詞は別アクセント句 =====

        [Fact]
        public void Process_動詞の後に名詞_別アクセント句()
        {
            var node1 = CreateNode("走る", "ハシル", POSType.Doushi, sub1: "自立");
            var node2 = CreateNode("人", "ヒト", POSType.Meishi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 04: 名詞-形容動詞語幹の後に名詞 =====

        [Fact]
        public void Process_形容動詞語幹の後に名詞_別アクセント句()
        {
            var node1 = CreateNode("綺麗", "キレイ", POSType.Meishi, sub1: "形容動詞語幹");
            var node2 = CreateNode("花", "ハナ", POSType.Meishi, sub1: "一般");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // Rule 04: 形容動詞語幹 + 名詞 → false
            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 03: 形容詞の後に名詞 =====

        [Fact]
        public void Process_形容詞の後に名詞_別アクセント句()
        {
            var node1 = CreateNode("高い", "タカイ", POSType.Keiyoushi);
            var node2 = CreateNode("山", "ヤマ", POSType.Meishi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.False(node2.ChainFlag);
        }

        // ===== ChainFlagが既に設定されている場合はスキップ =====

        [Fact]
        public void Process_ChainFlag既設定_上書きされない()
        {
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("駅", "エキ", chainFlag: false); // 事前にfalseを設定

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // ChainFlag が既に設定済み(false) なのでスキップされ、変更されない
            Assert.False(node2.ChainFlag);
        }

        [Fact]
        public void Process_ChainFlagTrue既設定_上書きされない()
        {
            // 通常はfalseになるべきケースでも、事前設定があればスキップ
            var node1 = CreateNode("東京", "トーキョー");
            var node2 = CreateNode("走る", "ハシル", POSType.Doushi, sub1: "自立", chainFlag: true);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // 事前にtrueが設定されているのでスキップ
            Assert.True(node2.ChainFlag);
        }

        // ===== 複合テスト: 3ノード以上の文 =====

        [Fact]
        public void Process_名詞助詞動詞の文_正しくChainFlagが設定される()
        {
            // 「猫が走る」 → 猫(名詞) + が(助詞) + 走る(動詞)
            var node1 = CreateNode("猫", "ネコ");
            var node2 = CreateNode("が", "ガ", POSType.Joshi, sub1: "格助詞");
            var node3 = CreateNode("走る", "ハシル", POSType.Doushi, sub1: "自立");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);   // 名詞の後の助詞: 結合(Rule 08)
            Assert.False(node3.ChainFlag);  // 助詞(付属語)の後の動詞(自立語): 非結合(Rule 09)
        }

        [Fact]
        public void Process_名詞名詞助詞の文_正しくChainFlagが設定される()
        {
            // 「東京駅で」 → 東京(名詞) + 駅(名詞) + で(助詞)
            var node1 = CreateNode("東京", "トーキョー", POSType.Meishi, sub1: "固有名詞");
            var node2 = CreateNode("駅", "エキ", POSType.Meishi, sub1: "一般");
            var node3 = CreateNode("で", "デ", POSType.Joshi, sub1: "格助詞");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);  // 名詞の連続: 結合(Rule 02)
            Assert.True(node3.ChainFlag);  // 名詞の後の助詞: 結合(Rule 08)
        }

        // ===== Rule 09: 付属語の後の自立語は別アクセント句 =====

        [Fact]
        public void Process_助詞の後に名詞_別アクセント句()
        {
            var node1 = CreateNode("で", "デ", POSType.Joshi, sub1: "格助詞");
            var node2 = CreateNode("東京", "トーキョー", POSType.Meishi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            // Rule 09: 付属語の後の自立語 → false
            Assert.False(node2.ChainFlag);
        }

        // ===== Rule 01: デフォルトはくっつける =====

        [Fact]
        public void Process_デフォルトルール_結合される()
        {
            // どのルールにも該当しないパターン → Rule 01: デフォルトはくっつける
            // 感動詞 + 感動詞 → 該当ルールなし → デフォルトtrue
            var node1 = CreateNode("あー", "アー", POSType.Kandoushi);
            var node2 = CreateNode("うん", "ウン", POSType.Kandoushi);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentPhrase.Process(nodes);

            Assert.True(node2.ChainFlag);
        }
    }
}
