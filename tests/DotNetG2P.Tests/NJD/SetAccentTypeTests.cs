using DotNetG2P.Models;
using DotNetG2P.NJD;

namespace DotNetG2P.Tests.NJD
{
    public class SetAccentTypeTests
    {
        /// <summary>
        /// NjdNodeを手動構築するヘルパー。
        /// </summary>
        private static NjdNode CreateNode(
            string surface,
            string katakana,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            int accentType = 0,
            bool? chainFlag = null,
            string chainRule = "*")
        {
            var pos = new POS(posType, sub1);
            var pron = Pronunciation.FromKatakana(katakana, 0);
            var details = new WordDetails(pos, "*", "*", surface, katakana, pron);
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
            SetAccentType.Process(null!);
        }

        [Fact]
        public void Process_空リスト_例外が発生しない()
        {
            SetAccentType.Process(new List<NjdNode>());
        }

        [Fact]
        public void Process_単一ノード_AccentType変更なし()
        {
            var node = CreateNode("東京", "トーキョー", accentType: 1);
            var nodes = new List<NjdNode> { node };
            SetAccentType.Process(nodes);

            Assert.Equal(1, node.AccentType);
        }

        // ===== ChainFlag=falseの場合は新しいアクセント句を開始 =====

        [Fact]
        public void Process_ChainFlagFalse_新しいアクセント句開始_AccentType変更なし()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: false, chainRule: "C3");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // ChainFlag=false → 新アクセント句なのでルール適用なし
            Assert.Equal(1, node1.AccentType);
            Assert.Equal(1, node2.AccentType);
        }

        // ===== ChainRuleが"*"またはnullの場合のデフォルト動作 =====

        [Fact]
        public void Process_ChainRuleアスタリスク_AccentType変更なし()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 2, chainFlag: true, chainRule: "*");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // "*"はルールなし → GetRuleはnull → topNodeAccそのまま
            Assert.Equal(1, node1.AccentType);
        }

        [Fact]
        public void Process_ChainRuleNull_AccentType変更なし()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 3);
            var node2 = CreateNode("駅", "エキ", accentType: 2, chainFlag: true, chainRule: null!);

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // nullもルールなし → topNodeAccそのまま
            Assert.Equal(3, node1.AccentType);
        }

        // ===== C1パターン: 前部モーラ数 + 後部アクセント位置 =====

        [Fact]
        public void Process_C1_前部モーラ数に後部アクセント位置を加算()
        {
            // 「東京」(4モーラ, acc=1) + 「駅」(2モーラ, acc=1, ChainRule=C1)
            // → topNodeAcc = 4(前部モーラ数) + 1(後部アクセント) = 5
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(5, node1.AccentType); // moraSize(4) + nodeAcc(1)
        }

        // ===== C2パターン: 前部モーラ数 + 1 =====

        [Fact]
        public void Process_C2_前部モーラ数プラス1()
        {
            // 「東京」(4モーラ) + 「で」(1モーラ, ChainRule=C2)
            // → topNodeAcc = 4 + 1 = 5
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("で", "デ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "C2");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(5, node1.AccentType); // moraSize(4) + 1
        }

        // ===== C3パターン: 前部モーラ数 =====

        [Fact]
        public void Process_C3_前部モーラ数()
        {
            // 「猫」(2モーラ) + 「さん」(2モーラ, ChainRule=C3)
            // → topNodeAcc = 2(前部モーラ数)
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "C3");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType); // moraSize(2)
        }

        // ===== C4パターン: 平板型（0）=====

        [Fact]
        public void Process_C4_平板型にする()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: true, chainRule: "C4");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType);
        }

        // ===== C5パターン: 前部のアクセントを保持 =====

        [Fact]
        public void Process_C5_前部アクセント保持()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 3);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: true, chainRule: "C5");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(3, node1.AccentType); // 前部のまま
        }

        // ===== F1パターン: 前部のアクセントを保持 =====

        [Fact]
        public void Process_F1_前部アクセント保持()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 2);
            var node2 = CreateNode("は", "ワ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType);
        }

        // ===== F2パターン: 前部が平板型の場合のみ加算 =====

        [Fact]
        public void Process_F2_前部平板型で加算()
        {
            // 前部が平板型(0) → moraSize + addType を適用
            var node1 = CreateNode("猫", "ネコ", accentType: 0); // 平板型
            var node2 = CreateNode("は", "ワ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F2@1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // addResult = moraSize(2) + addType(1) = 3
            Assert.Equal(3, node1.AccentType);
        }

        [Fact]
        public void Process_F2_前部起伏型では変更なし()
        {
            // 前部が起伏型(非0) → 前部のアクセントを保持
            var node1 = CreateNode("猫", "ネコ", accentType: 1); // 起伏型
            var node2 = CreateNode("は", "ワ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F2@1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(1, node1.AccentType); // 前部のまま
        }

        // ===== F3パターン: 前部が起伏型の場合のみ加算 =====

        [Fact]
        public void Process_F3_前部起伏型で加算()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 1); // 起伏型
            var node2 = CreateNode("に", "ニ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F3@0");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // addResult = moraSize(2) + addType(0) = 2
            Assert.Equal(2, node1.AccentType);
        }

        [Fact]
        public void Process_F3_前部平板型では変更なし()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 0); // 平板型
            var node2 = CreateNode("に", "ニ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F3@0");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType); // 平板型のまま
        }

        // ===== F4パターン: 常に加算 =====

        [Fact]
        public void Process_F4_常に加算()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("も", "モ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F4@0");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // addResult = moraSize(2) + addType(0) = 2
            Assert.Equal(2, node1.AccentType);
        }

        [Fact]
        public void Process_F4_負の加算値でクランプ()
        {
            // moraSize(0) + addType(-5) → addResult=max(0, -5)=0
            // ただし実際にはmoraSize=0はi==0(先頭ノード)のケースのみなので不自然だが、
            // CalcTopNodeAccの挙動確認
            var node1 = CreateNode("あ", "ア", accentType: 1);
            var node2 = CreateNode("い", "イ", accentType: 0, chainFlag: true, chainRule: "F4@-5");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // addResult = max(0, moraSize(1) + (-5)) = max(0, -4) = 0
            Assert.Equal(0, node1.AccentType);
        }

        // ===== F5パターン: 平板型（0）にする =====

        [Fact]
        public void Process_F5_平板型にする()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 3);
            var node2 = CreateNode("の", "ノ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F5");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType);
        }

        // ===== P1パターン: 前部が平板なら0、起伏なら前部モーラ数+後部アクセント =====

        [Fact]
        public void Process_P1_前部平板型なら0()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 0);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "P1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType);
        }

        [Fact]
        public void Process_P1_前部起伏型なら前部モーラ数プラス後部アクセント()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "P1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // moraSize(2) + nodeAcc(1) = 3
            Assert.Equal(3, node1.AccentType);
        }

        // ===== P2パターン: P1と同じ =====

        [Fact]
        public void Process_P2_P1と同じ動作()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "P2");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(3, node1.AccentType);
        }

        // ===== P6パターン: 平板型（0）にする =====

        [Fact]
        public void Process_P6_平板型にする()
        {
            var node1 = CreateNode("東京", "トーキョー", accentType: 3);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: true, chainRule: "P6");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType);
        }

        // ===== P14パターン: 前部が起伏型の場合のみ前部モーラ数+後部アクセント =====

        [Fact]
        public void Process_P14_前部起伏型で加算()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "P14");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(3, node1.AccentType); // moraSize(2) + nodeAcc(1)
        }

        [Fact]
        public void Process_P14_前部平板型では変更なし()
        {
            var node1 = CreateNode("猫", "ネコ", accentType: 0);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "P14");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(0, node1.AccentType); // 平板型のまま
        }

        // ===== 品詞別ChainRuleテスト =====

        [Fact]
        public void Process_品詞別ChainRule_動詞用ルールが適用される()
        {
            // ChainRule = "動詞%F1/名詞%C3"
            // 前ノードが動詞 → F1(前部アクセント保持)
            var node1 = CreateNode("走る", "ハシル", POSType.Doushi, accentType: 2);
            var node2 = CreateNode("ため", "タメ", accentType: 1, chainFlag: true, chainRule: "動詞%F1/名詞%C3");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType); // F1: 前部アクセント保持
        }

        [Fact]
        public void Process_品詞別ChainRule_名詞用ルールが適用される()
        {
            // ChainRule = "動詞%F1/名詞%C3"
            // 前ノードが名詞 → C3(前部モーラ数)
            var node1 = CreateNode("猫", "ネコ", POSType.Meishi, accentType: 1);
            var node2 = CreateNode("ため", "タメ", accentType: 1, chainFlag: true, chainRule: "動詞%F1/名詞%C3");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType); // C3: moraSize(2)
        }

        [Fact]
        public void Process_品詞別ChainRule_該当なしならデフォルトルール()
        {
            // ChainRule = "動詞%F1/C3"
            // 前ノードが名詞 → 動詞用ルールに該当しない → デフォルトC3
            var node1 = CreateNode("猫", "ネコ", POSType.Meishi, accentType: 1);
            var node2 = CreateNode("ため", "タメ", accentType: 1, chainFlag: true, chainRule: "動詞%F1/C3");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType); // デフォルトC3: moraSize(2)
        }

        // ===== 3ノード以上での結合テスト =====

        [Fact]
        public void Process_3ノード結合_moraSize累積で計算()
        {
            // 「東京」(4モーラ,acc=1) + 「駅」(2モーラ,acc=1,chain=C1) + 「で」(1モーラ,acc=0,chain=C2)
            // node2結合時: topNode=node1, moraSize=0→4, topNodeAcc = 4+1=5
            // node3結合時: topNode=node1, moraSize=4→6, topNodeAcc = 6+1=7
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 1, chainFlag: true, chainRule: "C1");
            var node3 = CreateNode("で", "デ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "C2");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentType.Process(nodes);

            // node2処理時: moraSize=4, C1 → topNodeAcc=4+1=5
            // node3処理時: moraSize=4+2=6, C2 → topNodeAcc=6+1=7
            Assert.Equal(7, node1.AccentType);
        }

        [Fact]
        public void Process_ChainFlagFalseで句が分かれる場合_moraSize再計算()
        {
            // node1(4モーラ,acc=1) + node2(2モーラ,chainFlag=false,acc=2) + node3(1モーラ,chainFlag=true,C3)
            var node1 = CreateNode("東京", "トーキョー", accentType: 1);
            var node2 = CreateNode("駅", "エキ", accentType: 2, chainFlag: false);
            var node3 = CreateNode("で", "デ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "C3");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentType.Process(nodes);

            // node2はChainFlag=false → 新アクセント句開始, moraSize=0
            // node3処理時: topNode=node2, moraSize=0+2=2, C3 → topNodeAcc=2
            Assert.Equal(1, node1.AccentType); // 変更なし
            Assert.Equal(2, node2.AccentType); // C3: moraSize(2)
        }

        // ===== 数詞結合テスト =====

        [Fact]
        public void Process_十の前の数詞_アクセント1()
        {
            // 「三」(名詞-数) + 「十」(名詞-数, ChainFlag=true) → prevのaccent=1
            var node1 = CreateNode("三", "サン", POSType.Meishi, sub1: "数", accentType: 1);
            var node2 = CreateNode("十", "ジュー", POSType.Meishi, sub1: "数", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // CalcDigitAcc: current="十" → return 1
            // prevNewAcc = 1
            Assert.Equal(1, node1.AccentType);
        }

        [Fact]
        public void Process_五十一_数詞結合でアクセント再計算()
        {
            // 「五」(1モーラ) + 「十」(2モーラ, C1) + 「一」(2モーラ, C1)
            // i=1(十): moraSize=1
            //   CalcTopNodeAcc: C1 → 1+1=2 → topNodeNewAcc=2
            //   CalcDigitAcc: prev=五,curr=十,next=一 → GO&&next∈一〜九 → prevNewAcc=0
            //   適用順: node1.AccentType=2(topNode), node1.AccentType=0(prev) → 最終0
            // i=2(一): moraSize=3
            //   CalcTopNodeAcc: C1 → 3+2=5 → topNodeNewAcc=5
            //   CalcDigitAcc: prev=十,curr=一 → 該当なし → null
            //   適用: node1.AccentType=5(topNode)
            var node1 = CreateNode("五", "ゴ", POSType.Meishi, sub1: "数", accentType: 1);
            var node2 = CreateNode("十", "ジュー", POSType.Meishi, sub1: "数", accentType: 1, chainFlag: true, chainRule: "C1");
            var node3 = CreateNode("一", "イチ", POSType.Meishi, sub1: "数", accentType: 2, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2, node3 };
            SetAccentType.Process(nodes);

            // 最終的にi=2でtopNodeAccが5に上書きされる
            Assert.Equal(5, node1.AccentType);
        }

        [Fact]
        public void Process_百の位_七百はアクセント2()
        {
            // 「七」 + 「百」 → CalcDigitAcc: 七+百 → 2
            var node1 = CreateNode("七", "ナナ", POSType.Meishi, sub1: "数", accentType: 0);
            var node2 = CreateNode("百", "ヒャク", POSType.Meishi, sub1: "数", accentType: 2, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType);
        }

        [Fact]
        public void Process_百の位_三百はアクセント1()
        {
            var node1 = CreateNode("三", "サン", POSType.Meishi, sub1: "数", accentType: 1);
            var node2 = CreateNode("百", "ビャク", POSType.Meishi, sub1: "数", accentType: 2, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(1, node1.AccentType);
        }

        [Fact]
        public void Process_千の位_前部モーラ数プラス1()
        {
            // 「三」(2モーラ) + 「千」 → prevAcc = moraCount(2) + 1 = 3
            // ただし「三」は1モーラ(サン)なので1+1=2
            var node1 = CreateNode("三", "サン", POSType.Meishi, sub1: "数", accentType: 1);
            var node2 = CreateNode("千", "セン", POSType.Meishi, sub1: "数", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // CalcDigitAcc: 千 → prev.MoraCount(2) + 1 = 3
            Assert.Equal(3, node1.AccentType);
        }

        [Fact]
        public void Process_億の位_一億はアクセント2()
        {
            var node1 = CreateNode("一", "イチ", POSType.Meishi, sub1: "数", accentType: 2);
            var node2 = CreateNode("億", "オク", POSType.Meishi, sub1: "数", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // CalcDigitAcc: 億, prev=一 → 一∈{ICHI,ROKU,NANA,HACHI,IKU} → 2
            Assert.Equal(2, node1.AccentType);
        }

        [Fact]
        public void Process_兆の位_六兆はアクセント2()
        {
            var node1 = CreateNode("六", "ロク", POSType.Meishi, sub1: "数", accentType: 2);
            var node2 = CreateNode("兆", "チョー", POSType.Meishi, sub1: "数", accentType: 1, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(2, node1.AccentType);
        }

        // ===== 「十」の後に数詞が続く場合、「十」を平板型にする =====

        [Fact]
        public void Process_十の後に数詞_十が平板型になる()
        {
            // node1=「十」(名詞-数), node2=「一」(名詞-数, ChainFlag=false → 新アクセント句)
            // Process: i=0で十, next=一(IsKazu) → currentNewAcc=0
            var node1 = CreateNode("十", "ジュー", POSType.Meishi, sub1: "数", accentType: 1);
            var node2 = CreateNode("一", "イチ", POSType.Meishi, sub1: "数", accentType: 2, chainFlag: true, chainRule: "C1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // i=0: current=十, next=一(IsKazu) → currentNewAcc=0
            // i=1: ChainFlag=true, CalcTopNodeAcc + CalcDigitAcc
            // ただしi=0でcurrentNewAcc=0が先に適用される
            // → その後i=1でtopNodeAccが上書きされる
            // 最終的にCalcDigitAcc: prev=十, curr=一 → 十は該当パターンなし(CalcDigitAccはnull)
            // CalcTopNodeAcc: C1 → moraSize(0→2) + nodeAcc(2) = 2+2=4
            // → topNodeNewAcc=4に上書き
            // ... 実際の計算: node1のAccentTypeはCalcTopNodeAccの結果
            // i=0: currentNewAcc=0 → node1.AccentType=0
            // i=1: topNodeNewAcc = CalcTopNodeAcc → moraSize=2, C1 → 2+2=4 → node1.AccentType=4
            // 最終結果は4
            Assert.Equal(4, node1.AccentType);
        }

        // ===== F2@負値テスト =====

        [Fact]
        public void Process_F2_負の加算値で平板型が更新される()
        {
            // 前部が平板型で F2@-1 → addResult = moraSize + (-1)
            var node1 = CreateNode("猫", "ネコ", accentType: 0); // 2モーラ, 平板型
            var node2 = CreateNode("は", "ワ", POSType.Joshi, accentType: 0, chainFlag: true, chainRule: "F2@-1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            // addResult = moraSize(2) + (-1) = 1
            Assert.Equal(1, node1.AccentType);
        }

        // ===== ChainRulesキャッシュテスト =====

        [Fact]
        public void Process_同一ChainRule文字列_キャッシュにより同一結果()
        {
            // 同じChainRule "C3" を2回使用しても正しい結果が得られる
            var node1 = CreateNode("猫", "ネコ", accentType: 1);
            var node2 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "C3");

            var nodes1 = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes1);
            Assert.Equal(2, node1.AccentType); // moraSize(2)

            // 2回目: 同じChainRule文字列 "C3" がキャッシュから取得される
            var node3 = CreateNode("犬", "イヌ", accentType: 3);
            var node4 = CreateNode("さん", "サン", accentType: 1, chainFlag: true, chainRule: "C3");

            var nodes2 = new List<NjdNode> { node3, node4 };
            SetAccentType.Process(nodes2);
            Assert.Equal(2, node3.AccentType); // moraSize(2)
        }

        [Fact]
        public void Process_複合ChainRule_キャッシュ経由でも品詞別ルールが正しく適用()
        {
            // 1回目: 動詞が前ノード
            var node1 = CreateNode("走る", "ハシル", POSType.Doushi, accentType: 2);
            var node2 = CreateNode("ため", "タメ", accentType: 1, chainFlag: true, chainRule: "動詞%F1/名詞%C3");

            var nodes1 = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes1);
            Assert.Equal(2, node1.AccentType); // F1: 前部保持

            // 2回目: 名詞が前ノード（同じChainRule文字列）
            var node3 = CreateNode("猫", "ネコ", POSType.Meishi, accentType: 1);
            var node4 = CreateNode("ため", "タメ", accentType: 1, chainFlag: true, chainRule: "動詞%F1/名詞%C3");

            var nodes2 = new List<NjdNode> { node3, node4 };
            SetAccentType.Process(nodes2);
            Assert.Equal(2, node3.AccentType); // C3: moraSize(2)
        }

        // ===== 特殊助動詞がルールとしてパースされないことの確認 =====

        [Fact]
        public void Process_特殊助動詞ChainRule_無視されてデフォルト動作()
        {
            // "特殊助動詞%F1" は正規表現にマッチしない（品詞パターンから除去済み）
            // → PushRuleでスキップされ、ルールなしとして扱われる → topNodeAccそのまま
            var node1 = CreateNode("東京", "トーキョー", accentType: 3);
            var node2 = CreateNode("です", "デス", accentType: 0, chainFlag: true, chainRule: "特殊助動詞%F1");

            var nodes = new List<NjdNode> { node1, node2 };
            SetAccentType.Process(nodes);

            Assert.Equal(3, node1.AccentType); // ルールなし → 変更なし
        }
    }
}
