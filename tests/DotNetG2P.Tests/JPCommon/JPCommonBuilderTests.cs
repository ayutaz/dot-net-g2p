using System.Collections.Generic;
using DotNetG2P.JPCommon;
using DotNetG2P.Models;
using Xunit;

namespace DotNetG2P.Tests.JPCommon
{
    public class JPCommonBuilderTests
    {
        /// <summary>
        /// ヘルパー: 指定されたカタカナ発音を持つNjdNodeを作成する。
        /// </summary>
        private static NjdNode CreateNode(
            string surface,
            string katakana,
            int accentType = 0,
            bool? chainFlag = null,
            POSType posType = POSType.Meishi,
            string sub1 = "*",
            string sub2 = "*",
            string sub3 = "*",
            string conjugationType = "*",
            string conjugationForm = "*")
        {
            var pos = new POS(posType, sub1, sub2, sub3);
            var pron = Pronunciation.FromKatakana(katakana, accentType);
            var details = new WordDetails(pos, conjugationType, conjugationForm, surface, katakana, pron);
            var node = new NjdNode(surface, details)
            {
                AccentType = accentType,
                ChainFlag = chainFlag
            };
            // WordDetailsからPronunciationをコピー
            node.Pronunciation = pron;
            return node;
        }

        /// <summary>
        /// ヘルパー: Toutenノードを作成する。
        /// </summary>
        private static NjdNode CreateToutenNode()
        {
            var pos = new POS(POSType.Kigou);
            var pron = new Pronunciation(
                new List<Mora> { new Mora(null, null, MoraKind.Touten) }, 0);
            var details = new WordDetails(pos, "*", "*", "、", "、", pron);
            return new NjdNode("、", details) { Pronunciation = pron };
        }

        /// <summary>
        /// ヘルパー: Questionノードを作成する。
        /// </summary>
        private static NjdNode CreateQuestionNode()
        {
            var pos = new POS(POSType.Kigou);
            var pron = new Pronunciation(
                new List<Mora> { new Mora(null, null, MoraKind.Question) }, 0);
            var details = new WordDetails(pos, "*", "*", "？", "？", pron);
            return new NjdNode("？", details) { Pronunciation = pron };
        }

        [Fact]
        public void Build_EmptyNodeList_ReturnsEmptyUtterance()
        {
            var nodes = new List<NjdNode>();

            var utt = JPCommonBuilder.Build(nodes);

            Assert.NotNull(utt);
            Assert.Equal(0, utt.BreathGroupCount);
            Assert.Equal(0, utt.AccentPhraseCount);
            Assert.Equal(0, utt.MoraCount);
        }

        [Fact]
        public void Build_NullNodeList_ReturnsEmptyUtterance()
        {
            var utt = JPCommonBuilder.Build(null);

            Assert.NotNull(utt);
            Assert.Equal(0, utt.BreathGroupCount);
        }

        [Fact]
        public void Build_SingleNode_CreatesOneBreathGroupOneAccentPhraseOneWord()
        {
            // "盆栽" → ボンサイ (4モーラ)
            var node = CreateNode("盆栽", "ボンサイ", accentType: 0);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);

            Assert.Equal(1, utt.BreathGroupCount);
            Assert.Equal(1, utt.AccentPhraseCount);
            Assert.Equal(4, utt.MoraCount);

            var bg = utt.BreathGroups[0];
            Assert.Equal(0, bg.IndexInUtterance);
            Assert.Same(utt, bg.ParentUtterance);

            var ap = bg.AccentPhrases[0];
            Assert.Equal(0, ap.IndexInBreathGroup);
            Assert.Same(bg, ap.ParentBreathGroup);
            Assert.Equal(1, ap.WordCount);
            Assert.Equal(0, ap.AccentType);
            Assert.False(ap.IsInterrogative);

            var word = ap.Words[0];
            Assert.Equal(0, word.IndexInAccentPhrase);
            Assert.Same(ap, word.ParentAccentPhrase);
            Assert.Equal(4, word.MoraCount);
        }

        [Fact]
        public void Build_MultipleNodesWithChainFlag_CombinesIntoSingleAccentPhrase()
        {
            // "こんにち" + "は" (ChainFlag=true) → 1AP, 2Words
            var node1 = CreateNode("こんにち", "コンニチ", accentType: 3, chainFlag: null);
            var node2 = CreateNode("は", "ワ", accentType: 0, chainFlag: true);
            var nodes = new List<NjdNode> { node1, node2 };

            var utt = JPCommonBuilder.Build(nodes);

            Assert.Equal(1, utt.BreathGroupCount);
            Assert.Equal(1, utt.AccentPhraseCount);

            var ap = utt.BreathGroups[0].AccentPhrases[0];
            Assert.Equal(2, ap.WordCount);
            Assert.Equal(5, ap.MoraCount); // コ+ン+ニ+チ + ワ

            // Word indices
            Assert.Equal(0, ap.Words[0].IndexInAccentPhrase);
            Assert.Equal(1, ap.Words[1].IndexInAccentPhrase);

            // モーラインデックスの確認
            var moras = new List<JPMora>(ap.AllMoras());
            for (int i = 0; i < moras.Count; i++)
            {
                Assert.Equal(i, moras[i].IndexInAccentPhrase);
            }
        }

        [Fact]
        public void Build_ToutenSplitsBreathGroups()
        {
            // "これは" + "、" + "盆栽です" → 2BG
            var node1 = CreateNode("これは", "コレワ", accentType: 0);
            var toutenNode = CreateToutenNode();
            var node2 = CreateNode("盆栽です", "ボンサイデス", accentType: 0);
            var nodes = new List<NjdNode> { node1, toutenNode, node2 };

            var utt = JPCommonBuilder.Build(nodes);

            Assert.Equal(2, utt.BreathGroupCount);
            Assert.Equal(1, utt.BreathGroups[0].AccentPhraseCount);
            Assert.Equal(1, utt.BreathGroups[1].AccentPhraseCount);
        }

        [Fact]
        public void Build_QuestionNodeSetsInterrogativeFlag()
        {
            // "何？" → IsInterrogative = true
            var node1 = CreateNode("何", "ナニ", accentType: 1);
            var questionNode = CreateQuestionNode();
            var nodes = new List<NjdNode> { node1, questionNode };

            var utt = JPCommonBuilder.Build(nodes);

            // Questionノードで呼気グループ境界が作られる
            Assert.Equal(1, utt.BreathGroupCount);
            var ap = utt.BreathGroups[0].AccentPhrases[0];
            Assert.True(ap.IsInterrogative);
        }

        [Fact]
        public void Build_PhonemeLinksAreCorrectlySet()
        {
            // "ア" → 1音素 "a"
            var node = CreateNode("あ", "ア", accentType: 0);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);

            var phoneme = utt.BreathGroups[0].AccentPhrases[0].Words[0].Moras[0].Phonemes[0];
            Assert.Equal("a", phoneme.Phoneme);
            Assert.Null(phoneme.Prev);
            Assert.Null(phoneme.Next);
        }

        [Fact]
        public void Build_MultiPhonemeLinksAreCorrectlyChained()
        {
            // "カキ" → k a k i (4音素)
            var node = CreateNode("柿", "カキ", accentType: 0);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);

            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];
            var k1 = word.Moras[0].Phonemes[0]; // k
            var a = word.Moras[0].Phonemes[1];   // a
            var k2 = word.Moras[1].Phonemes[0];  // k
            var i = word.Moras[1].Phonemes[1];   // i

            // 先頭
            Assert.Null(k1.Prev);
            Assert.Same(a, k1.Next);

            // a
            Assert.Same(k1, a.Prev);
            Assert.Same(k2, a.Next);

            // k2
            Assert.Same(a, k2.Prev);
            Assert.Same(i, k2.Next);

            // 末尾
            Assert.Same(k2, i.Prev);
            Assert.Null(i.Next);
        }

        [Fact]
        public void Build_PosIdMapping_Keiyoushi()
        {
            var node = CreateNode("美しい", "ウツクシイ", posType: POSType.Keiyoushi, sub1: "自立");
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];
            Assert.Equal(11, word.PosId);  // WordAttr: "形容詞,自立,*,*" → 11
        }

        [Fact]
        public void Build_PosIdMapping_Kigou_ReturnsNull()
        {
            var node = CreateNode("、", "、", posType: POSType.Kigou);
            // Kigouノードだがtoutenではない（普通の記号）
            node.Pronunciation = Pronunciation.FromKatakana("ア", 0); // ダミー発音
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];
            Assert.Null(word.PosId);
        }

        [Fact]
        public void Build_CTypeIdMapping()
        {
            // 五段活用
            var node = CreateNode("書く", "カク", conjugationType: "五段・カ行イ音便", conjugationForm: "基本形",
                posType: POSType.Doushi, sub1: "自立");
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];
            Assert.Equal(20, word.CTypeId);  // WordAttr: "五段・カ行イ音便" → 20
            Assert.Equal(5, word.CFormId);   // WordAttr: "基本形" → 5
            Assert.Equal(32, word.PosId);    // WordAttr: "動詞,自立,*,*" → 32
        }

        [Fact]
        public void Build_SpecialMora_Xtsu_ProducesCl()
        {
            // "カッ" → k a cl
            var node = CreateNode("カッ", "カッ", accentType: 0);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];

            Assert.Equal(2, word.MoraCount);
            Assert.Equal("cl", word.Moras[1].Phonemes[0].Phoneme);
        }

        [Fact]
        public void Build_SpecialMora_N_ProducesNN()
        {
            // "カン" → k a N
            var node = CreateNode("缶", "カン", accentType: 1);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];

            Assert.Equal(2, word.MoraCount);
            Assert.Equal("N", word.Moras[1].Phonemes[0].Phoneme);
        }

        [Fact]
        public void Build_SpecialMora_Long_ProducesDash()
        {
            // "カー" → k a -
            var node = CreateNode("カー", "カー", accentType: 0);
            var nodes = new List<NjdNode> { node };

            var utt = JPCommonBuilder.Build(nodes);
            var word = utt.BreathGroups[0].AccentPhrases[0].Words[0];

            Assert.Equal(2, word.MoraCount);
            Assert.Equal("-", word.Moras[1].Phonemes[0].Phoneme);
        }

        [Fact]
        public void Build_MultipleAccentPhrasesWithoutChain()
        {
            // 2つのノードでChainFlag=false → 2AP
            var node1 = CreateNode("今日", "キョウ", accentType: 1, chainFlag: null);
            var node2 = CreateNode("天気", "テンキ", accentType: 1, chainFlag: false);
            var nodes = new List<NjdNode> { node1, node2 };

            var utt = JPCommonBuilder.Build(nodes);

            Assert.Equal(1, utt.BreathGroupCount);
            Assert.Equal(2, utt.AccentPhraseCount);

            var ap1 = utt.BreathGroups[0].AccentPhrases[0];
            var ap2 = utt.BreathGroups[0].AccentPhrases[1];
            Assert.Equal(0, ap1.IndexInBreathGroup);
            Assert.Equal(1, ap2.IndexInBreathGroup);
        }

        [Fact]
        public void Build_JoshiPosIdMapping()
        {
            // 助詞-格助詞-一般 → WordAttr: "助詞,格助詞,一般,*" → 14
            var node = CreateNode("が", "ガ", posType: POSType.Joshi, sub1: "格助詞", sub2: "一般");
            var nodes = new List<NjdNode> { node };
            var utt = JPCommonBuilder.Build(nodes);
            Assert.Equal(14, utt.BreathGroups[0].AccentPhrases[0].Words[0].PosId);
        }

        [Fact]
        public void Build_MeishiSubcategoryMapping()
        {
            // 名詞-固有名詞-一般 → WordAttr: "名詞,固有名詞,一般,*" → 42
            var node = CreateNode("東京", "トウキョウ", posType: POSType.Meishi, sub1: "固有名詞", sub2: "一般");
            var nodes = new List<NjdNode> { node };
            var utt = JPCommonBuilder.Build(nodes);
            Assert.Equal(42, utt.BreathGroups[0].AccentPhrases[0].Words[0].PosId);
        }

        [Fact]
        public void Build_AccentTypeFromFirstNode()
        {
            // ChainFlagで結合されたアクセント句のAccentTypeは最初のノードのものを使う
            var node1 = CreateNode("東京", "トウキョウ", accentType: 0, chainFlag: null);
            var node2 = CreateNode("の", "ノ", accentType: 0, chainFlag: true);
            var nodes = new List<NjdNode> { node1, node2 };

            var utt = JPCommonBuilder.Build(nodes);
            var ap = utt.BreathGroups[0].AccentPhrases[0];
            Assert.Equal(0, ap.AccentType); // 最初のノードのAccentType
        }
    }
}
